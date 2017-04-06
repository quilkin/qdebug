using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ArdDebug
{
#if __GDB__
    class GDB
    {
        private Arduino _arduino;
        private string lastCommand = null;
        private List<string> responses;

        public enum State : byte
        {
            init, connected, typesFound, varsFound, funcsFound, main
        }
        public State CurrentState { get; private set; }
        public GDB(Arduino ard)
        {
            _arduino = ard;
            responses = new List<string>();
            CurrentState = State.init;
        }
        private Process AvrGdb; 

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
            AvrGdb.OutputDataReceived += AvrGdb_OutputDataReceived;

            AvrGdb.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            AvrGdb.StartInfo.Arguments = elfPath;

            if (AvrGdb.Start()== false)
            {
                MessageBox.Show("Cannot find 'avr-gdb.exe'");
                return false;
            }
            AvrGdb.BeginOutputReadLine();
            AvrGdb.BeginErrorReadLine();

            

            //AvrGdb.WaitForExit();
            return true;
        }
        //[DllImport("kernel32.dll", SetLastError = true)]
        //static extern bool GenerateConsoleCtrlEvent(int sigevent, int dwProcessGroupId);
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
                    CtrlC.OutputDataReceived += AvrGdb_OutputDataReceived;
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
        private void AvrGdb_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            lock (outData)
            {
                string connection = "target remote " + _arduino.spmanager.CurrentSerialSettings.PortName;
                string baud = "set serial baud " + _arduino.spmanager.CurrentSerialSettings.BaudRate;
                string reply = e.Data;
                if (reply == null)
                    return;
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
                        CurrentState = State.connected;
                        AvrGdb.StandardInput.WriteLine("info types");
                    }
                }
                if (CurrentState < State.funcsFound)
                {
                    // need to collect some info
                    if (reply.Contains("All defined types"))
                    {
                        // start of list of types, remove previous stuff
                        //responses.Clear();
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
                            CurrentState = State.typesFound;
                            // go on to read variables
                            AvrGdb.StandardInput.WriteLine("info variables");
                        }
                        else if (CurrentState < State.varsFound)
                        {
                            // this is the last useful line in the list of variables, so parse them
                            ParseVariables();
                            responses.Clear();
                            CurrentState = State.varsFound;
                            AvrGdb.StandardInput.WriteLine("info functions");
                        }
                        else if (CurrentState < State.funcsFound)
                        {
                            // this is the last useful line in the list of variables, so parse them
                            ParseFunctions();
                            responses.Clear();
                            CurrentState = State.funcsFound;
                            //AvrGdb.StandardInput.WriteLine("step");
                            _arduino.GDBReady();
                        }
                    }

                }
                else
                {
                    // startup completed.
                    char[] delimiters = new char[] { ' ', '\t' };
                    string[] parts = reply.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                    int lineNum = 0;
                    if (parts.Length > 1 && int.TryParse(parts[1],out lineNum))
                    {
                        if (CurrentState < State.main)
                        {
                            CurrentState = State.main;
                            //_arduino.GDBReady();
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
                else if (line.Contains("Non - debugging symbols"))
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
                        else if (part == "struct")
                        {
                            type.isStruct = true;
                        }
                        else
                        {
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
                }
                else if (line.Contains("<unknown>"))
                {
                    // last line we're interrested in, so can ignore rest
                    break;
                }
                else if (line.Length > 2)
                {
                    Variable var = new Variable(_arduino);
                    char[] delimiters = new char[] { ' ', ';' };
                    string[] parts = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                    var.Name = parts[parts.Length - 1];
                    var.File = currentFile;
                    string typeName = parts[parts.Length - 2];
                    _arduino.AddVariable(var,typeName);


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

                    func.Name = parts[parts.Length - 1];
                    func.File = currentFile;
                    string typeName = parts[parts.Length - 2];
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
