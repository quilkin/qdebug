using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArdDebug
{
#if __GDB__
    class GDB
    {
        private Arduino _arduino;
        private string lastCommand = null;
        private List<string> responses;
        private State state;

        public enum State : byte
        {
            init, connected, typesFound, varsFound, funcsFound, setGlobals, ready, step,  getGlobals, getLocals, getArgs, breakpoint, next, stepout,run
        }
        public State CurrentState { get { return state; } }
        public void SetState(State s) { state = s;  PromptReady = false; }
        public GDB(Arduino ard)
        {
            _arduino = ard;
            responses = new List<string>();
            SetState(State.init);
        }
        private Process AvrGdb;

        public class Interaction : EventArgs
        {
            //public enum Ev : byte
            //{
            //    ready, newline, dispvar, getvar, getlocal
            //}
            public State state { get; set; }
            public string var { get; set; }
            public int linenum { get; set; }
            public Interaction(State ev)
            {
                this.state = ev;
            }
        }
        public event InteractionHandler iHandler;
        public delegate void InteractionHandler(GDB m, Interaction e);

        public bool PromptReady { get; set;  }

        private string recdData = "";
        private string[] lineEnd = { "\r\n" };

        private string connection;
        private string baud;
        public bool Open()
        {
            string elfPath = _arduino.FindElfPath();
            if (elfPath == null)
                return false;

            //elfPath = elfPath.Replace('\\', '/');

            AvrGdb = new Process();
            AvrGdb.StartInfo.CreateNoWindow = true;
            AvrGdb.StartInfo.UseShellExecute = false;
            AvrGdb.StartInfo.FileName = "avr-gdb.exe";
            AvrGdb.StartInfo.RedirectStandardOutput = true;
            AvrGdb.StartInfo.RedirectStandardInput = true;
            AvrGdb.StartInfo.RedirectStandardError = true;

            AvrGdb.ErrorDataReceived += AvrGdb_ErrorDataReceived;
           // AvrGdb.OutputDataReceived += AvrGdb_OutputDataReceived;

            AvrGdb.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            AvrGdb.StartInfo.Arguments = elfPath;


            connection = "target remote " + _arduino.spmanager.CurrentSerialSettings.PortName;
            baud = "set serial baud " + _arduino.spmanager.CurrentSerialSettings.BaudRate;

            if (AvrGdb.Start()== false)
            {
                MessageBox.Show("Cannot find 'avr-gdb.exe'");
                return false;
            }
            //AvrGdb.BeginOutputReadLine();
            AvrGdb.BeginErrorReadLine();

            Task task = ConsumeOutput(AvrGdb.StandardOutput, s =>
            {
                recdData += s;
                if (s.EndsWith("(gdb) "))
                {
                    string[] lines = recdData.Split(lineEnd, StringSplitOptions.RemoveEmptyEntries);
                    for (int i=0; i < lines.Length; i++)
                    {
                        string str = lines[i];

                        if (i == lines.Length - 1)
                        {
                            PromptReady = true;
                        }
                        else
                        {
                            if (str.StartsWith("(gdb)"))
                            {
                                // two (gdb)'s in one response....
                                str = str.Substring(5);
                                AvrGdb_OutputDataReceived("(gdb)");

                            }
                        }

                         AvrGdb_OutputDataReceived(str);

                    }
                    recdData = string.Empty;
                }

            });

            //AvrGdb.WaitForExit();
            return true;
        }

        /// <summary>
        /// thanks to http://stackoverflow.com/questions/33716580/process-output-redirection-on-a-single-console-line
        /// </summary>
        async Task ConsumeOutput(TextReader reader, Action<string> callback)
        {
            char[] buffer = new char[256];
            int cch;

            while ((cch = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                callback(new string(buffer, 0, cch));
            }
        }


        public void Kill()
        {
            if (AvrGdb.HasExited == false)
                AvrGdb.Kill();
        }

        public bool Write(string line)
        {

            // get ready for  a new reply (maybe several lines)
            responses.Clear();
            try
            {
                if (line == "break")
                {
                    int id = AvrGdb.Id;
                    // start a new process to send SIGINT to the gdb process....
                    Process CtrlC = new Process();
                    CtrlC.StartInfo.CreateNoWindow = true;
                    CtrlC.StartInfo.UseShellExecute = false;
                    CtrlC.StartInfo.FileName = "SendCtrlC.exe";
                    CtrlC.StartInfo.Arguments = id.ToString();
                    CtrlC.StartInfo.RedirectStandardOutput = true;
                     CtrlC.StartInfo.RedirectStandardError = true;

                    CtrlC.ErrorDataReceived += AvrGdb_ErrorDataReceived;
                    //CtrlC.OutputDataReceived += AvrGdb_OutputDataReceived;
                    if (CtrlC.Start() == false)
                    {
                        MessageBox.Show("Cannot find 'SendCtrlC.exe'");
                        return false;
                    }

                    //Process avrgdb = Process.GetProcessesByName("avr-gdb")[0];
                    //GenerateConsoleCtrlEvent(0 /*CTRL_C_EVENT*/, id);

                    //AvrGdb.StandardInput.Write(char.ConvertFromUtf32(3));
                }
                else
                {
                    PromptReady = false;
                    _arduino.UpdateCommsBox(line, true);
                    AvrGdb.StandardInput.WriteLine(line);
                    lastCommand = line;

                }
                return true;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }

        }

        static StringBuilder outData = new StringBuilder();
        static StringBuilder errData = new StringBuilder();
        //private void AvrGdb_OutputDataReceived(object sender, DataReceivedEventArgs e)
        private void AvrGdb_OutputDataReceived(string reply)
        {
            lock (outData)
            {
                //string connection = "target remote " + _arduino.spmanager.CurrentSerialSettings.PortName;
                //string baud = "set serial baud " + _arduino.spmanager.CurrentSerialSettings.BaudRate;
                //string reply = e.Data;
                //if (reply == null)
                //    return;
                _arduino.UpdateCommsBox(reply, false);

                if (CurrentState == State.init)
                {
                    // need to connect to the target

                    if (reply.Contains("Reading symbols"))
                    {
                        //_arduino.UpdateCommsBox(reply, false);
                        AvrGdb.StandardInput.WriteLine(baud);
                        AvrGdb.StandardInput.WriteLine(connection);
                    }
                    if (reply.Contains("Remote debugging"))
                    {
                        // can start to collect variable  types
                        SetState(State.connected);
                        AvrGdb.StandardInput.WriteLine("info types");
                    }
                }
                if (CurrentState < State.funcsFound)
                {
                    // need to collect some info
                    if (reply.Contains("All defined types"))
                    {
                        // start of list of types, remove previous stuff
                        responses.Clear();
                    }
                    else if (reply.Contains("All defined variables"))
                    {
                        // start of list of variables, remove previous stuff
                        //responses.Clear();
                    }
                    else if (reply.Contains("All defined functions"))
                    {
                        // start of list of functions, remove previous stuff
                        //responses.Clear();
                    }
                    else if (reply.Contains("Non-debugging symbols"))
                    {
                        if (CurrentState < State.typesFound)
                        {
                            // this is the last useful line in the list of types, so parse them
                            ParseTypes();
                            responses.Clear();
                            SetState(State.typesFound);
                            // go on to read variables
                            AvrGdb.StandardInput.WriteLine("info variables");
                        }
                        else if (CurrentState < State.varsFound)
                        {
                            // this is the last useful line in the list of variables, so parse them
                            ParseVariables();
                            responses.Clear();
                            SetState(State.varsFound);
                            AvrGdb.StandardInput.WriteLine("info functions");
                        }
                        else if (CurrentState < State.funcsFound)
                        {
                            // this is the last useful line in the list of variables, so parse them
                            ParseFunctions();
                            responses.Clear();
                            SetState(State.setGlobals);
                            Interaction ready = new Interaction(State.setGlobals);
                            iHandler(this, ready);

                        }
                    }

                }
                else
                {
                    // startup completed.
                    // remove the prompt before parsing rest of input
                    // reply = reply.Replace("(gdb)", "");
                    char[] delimiters = new char[] { ' ', '\t' };
                    string[] parts = reply.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1 || PromptReady)
                    {
                        Interaction i =  new Interaction(CurrentState);
                        switch (CurrentState) {
                            case State.setGlobals:
                                // just telling gdb which vars we want to display in future; not interested in values 
                                //i = new Interaction(State.setGlobals);

                                iHandler(this, i);
                                break;
                            case State.getLocals:
                                //i = new Interaction(State.getLocals);
                                // locals are returned simply like this "a = 3"
                                i.var = reply;
                                iHandler(this, i);
                                break;
                            case State.getArgs:
                                // locals are returned simply like this "a = 3"
                                // = new Interaction(State.getArgs);
                                i.var = reply;
                                iHandler(this, i);
                                break;
                            case State.breakpoint:
                               // i = new Interaction(State.breakpoint);
                                i.var = reply;
                                iHandler(this, i);
                                break;
                            case State.getGlobals:
                                //i = new Interaction(State.getGlobals);

                                if (parts[0].EndsWith(":"))
                                {
                                    // return of a variable value
                                   
                                    i.var = "";
                                    for (int part = 1; part < parts.Length; part++)
                                        i.var += parts[part];
                                    iHandler(this, i);
                                }
                                if (PromptReady)
                                {
                                    iHandler(this, i);
                                    break;
                                }
                                break;
                            case State.step:
                            case State.next:
                            case State.run:
                            case State.stepout:
                                //if (PromptReady)
                                //{

                                //    iHandler(this, i);
                                //    break;
                                //}
                                int linenum = 0;
                                if (int.TryParse(parts[0], out linenum))
                                {
                                    
                                    i.linenum = linenum;
                                    iHandler(this, i);
                                }

                                else if (reply.Contains("__table"))
                                {
                                    SetState(State.getGlobals);
                                    _arduino.extraStepNeeded = true;
                                }
                                else if (parts[0].StartsWith("0x"))
                                {

                                }
                                else
                                {
                                    //// not sure about this.....
                                    //iHandler(this, i);
                                    break;
                                }
                                break;
                        }
                    }
                }
                responses.Add(reply);


            }

        }

        private void ParseTypes()
        {
            foreach (string line in responses)
            {
                if (line.Contains("File"))
                {
                    // not interested (yet)
                }
                else if (line.Contains("All defined types"))
                {
                    // just the intro line
                }
                else if (line.Contains("Non-debugging symbols"))
                {
                    // last line of interest
                    break;
                }
                else if (line.Length > 2)
                {
                    VariableType type = new VariableType(0);
                    string typename = string.Empty;
                    char[] delimiters = new char[] { ' ', ';' };
                    string[] parts = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                    for (int p = 0; p < parts.Length; p++)
                    {
                        string part = parts[p];
                        if (part == "typedef")
                        {
                            continue;
                        }
                        //else if (part == "struct")
                        //{
                        //    type.isStruct = true;
                        //}
                        else
                        {
                            if (p > 1 || typename == "struct")
                                typename += " ";
                            typename += part;
                        }
                    }
                    type.Name = typename;
                    _arduino.AddType(type);
                }

            }

        }

        private void ParseVariables()
        {
            string currentFile = string.Empty;
            foreach (string line in responses)
            {
                if (line.Contains("File"))
                {
                    char[] delimiters = new char[] { ' ', ':', '/'};
                    string[] parts = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                    currentFile = parts[parts.Length - 1];
                }
                else if (line.Contains("All defined variables"))
                {
                    // just the intro line
                    continue;
                }
                else if (line.Contains("Non-debugging symbols"))
                {
                    continue;
                }
                else if (line.StartsWith("0x00"))
                {
                    continue;
                }
                else if (line.Contains("<unknown>"))
                {
                    // last line we're interrested in, so can ignore rest
                    break;
                }
                else if (line.Length > 1)
                {
                    Variable var = new Variable(_arduino);
                    var.isGlobal = true;
                    char[] delimiters = new char[] { ' ', ';' };
                    string[] parts = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                        continue;
                    
                    var.File = currentFile;
                    _arduino.AddVariable(var,parts);


                }

            }

        }

        private void ParseFunctions()
        {
            string currentFile = string.Empty;
            foreach (string line in responses)
            {
                if (line.Contains("File"))
                {
                    char[] delimiters = new char[] { ' ', ':', '/' };
                    string[] parts = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                        currentFile = parts[parts.Length - 1];
                }
                else if (line.Contains("All defined functions"))
                {
                    // just the intro line
                }
                //else if (line.Contains("Non-debugging symbols"))
                //{
                //    // last line we're interrested in, so can ignore rest
                //    break;
                //}
                else if (line.Length > 2)
                {
                    // strip off the arguments, we don't need them here
                    int bracket = line.IndexOf('(');
                    if (bracket < 0)
                    {
                        continue;
                    }
                    string shortFunc = line.Substring(0, bracket);
                    Function func = new ArdDebug.Function(_arduino);
                    char[] delimiters = new char[] { ' ', ';' };
                    string[] parts = shortFunc.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                        func.Name = parts[parts.Length - 1];
                    func.File = currentFile;
                    //if (parts.Length > 1)
                    //    // string typeName = parts[parts.Length - 2];
                    //    func.Type.Name = parts[parts.Length - 2];
                    _arduino.AddFunction(func);

                }

            }

        }
        private void AvrGdb_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            lock (errData)
            {
                _arduino.UpdateCommsBox("***" + e.Data + "***", true);
                System.Media.SystemSounds.Exclamation.Play();
            }
        }
        // typical startup conversation:
        // (gdb) target remote com7
        // Remote debugging using com7
        // 0x00001022 in myAdd(a= 20771, b= 5, c= 1) at../src/main.c:55
        // 55              for (index = 0; index< 5; index++)
        // (gdb) step
        // 57                      a += b;
        // (gdb) step
        // 58                      a += c;
        // (gdb)

        //[DllImport("kernel32.dll")]
        //static extern bool GenerateConsoleCtrlEvent(
        //    uint dwCtrlEvent,
        //     uint dwProcessGroupId);




    }
#endif
}
