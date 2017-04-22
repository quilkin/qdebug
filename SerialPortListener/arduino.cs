using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.ComponentModel;

namespace ArdDebug
{
    partial class Arduino
    {
        public Arduino(MainForm form)
        {
            GUI = form;
            GUI.RunButtons(false);
            CurrentState = State.stopped;
        }
        private MainForm GUI;
        private ListView source, disassembly, varView;
        private TextBox comms;
        public String ShortFilename { private set; get; }
        public String FullFilename { private set; get; }

        private List<Breakpoint> Breakpoints = new List<Breakpoint>();
        private List<Variable> Variables = new List<Variable>();
        private List<Variable> MyVariables = new List<Variable>();

#if __GDB__
        private List<Variable> LocalVariables = new List<Variable>();
        private List<Variable> PrevLocalVariables = new List<Variable>();
        private List<Variable> ArgVariables = new List<Variable>();
        private List<Variable> PrevArgVariables = new List<Variable>();
#else

#endif
        private List<VariableType> VariableTypes = new List<VariableType>();
        private List<Function> Functions = new List<Function>();


        public Breakpoint currentBreakpoint { private set; get; }
        public int MaxBreakpoints = 4;
        public int currentVariable{ private set; get; }
        public int currentLine = -1;
        /// <summary>
        /// next place to stop if we skip over a function call
        /// </summary>
        private Breakpoint nextBreakpoint = null;
        private String comString = String.Empty;
        private enum State : byte
        {
            init, running, stopped
        }
        private State CurrentState ;

        /// <summary>
        /// source file reference from .elf file
        /// </summary>
        public int SourceFileRef { private set; get; }
        public void AddVariable(Variable var, string[] typeParts)
        {
            int len = typeParts.Length;
            var.Name = typeParts[len - 1];
            string typename = typeParts[len - 2];
            if (typeParts[0] == "struct")
                typename = "struct " + typename;
            VariableType type = VariableTypes.Find(x => x.Name == typename);
            if (type != null)
                var.Type = type;
            foreach (string part in typeParts)
            {
                if (part == "struct")
                    var.isStruct = true;
                if (part == "*")
                    var.isPointer = true;
            }
            if (var.Name.Contains("["))
            {
                int arrayBracket = var.Name.IndexOf('[');
                if (arrayBracket > 0)
                {
                    var.Name = var.Name.Substring(0, arrayBracket);
                    var.isArray = true;
                }
                
            }
            Variables.Add(var);
        }
        public void AddType(VariableType type)
        {
            if (VariableTypes.Find(x => x.Name == type.Name) == null)
            {
                VariableTypes.Add(type);
            }
        }
        public void AddFunction(Function func)
        {

               Functions.Add(func);

        }
        public Serial.SerialPortManager spmanager { private set; get; }

        public bool pauseReqd { set; get; }
        /// <summary>
        /// ascii chars used for interaction strings
        /// (can't use under 128 because normal Serial.print will use them)
        /// </summary>
        //public enum Chars : byte { PROGCOUNT_CHAR = 248, TARGET_CHAR, STEPPING_CHAR, ADDRESS_CHAR, DATA_CHAR, NO_CHAR, YES_CHAR };

        // varaible types built in to gcc or typedefs defined by Arduino
        public readonly IList<string> ReservedTypeWords = new List<string> {
            "signed",
            "char",
            "unsigned",
            "int",
            "float",
            "double",
            "long",
            "volatile",
            "const",
        };
        public  IList<string> TypedefWords = new List<string> {
            "word",
            "boolean",
            "bool",
            "byte",
            "uint8_t",
            "uint16_t",
            "uint32_t",
            "uint64_t"
        };

        System.Drawing.Color sourceLineColour = System.Drawing.SystemColors.GradientInactiveCaption;
        System.Drawing.Color breakpointColour = System.Drawing.Color.Red;
        System.Drawing.Color breakpointHitColour = System.Drawing.Color.Orange;

#if __GDB__
        private GDB gdb;
        public static AutoResetEvent resultEvent = new AutoResetEvent(false);

        public void NewCommand(string input)
        {
            if (input == "step")
            {
                gdb.SetState(GDB.State.step);
                GUI.RunButtons(false);
            }

            gdb.Write(input);
        }

        public void Stop()
        {
            CurrentState = State.stopped;
            gdb.Kill();
        }

        delegate void readyDelegate();
        private void GDBReady()
        {
            if (varView.InvokeRequired)
            {
                readyDelegate d = new readyDelegate(GDBReady);
                varView.Invoke(d, new object[] { });
            }
            else
            {
                //MessageBox.Show(string.Format("Found {0} variables and {1} functions", Variables.Count, Functions.Count));
                GUI.RunButtons(true);
                varView.Enabled = true;
                source.Enabled = true;
                foreach (Variable var in Variables)
                {
                    if (var.File == this.ShortFilename)
                    {
                        ListViewItem lvi = var.CreateVarViewItem();
                        if (lvi != null)
                            varView.Items.Add(lvi);
                    }
                }
                gdb.PromptReady = false;
                GUI.StopTimer();
            }

        }

        private void DisplayNextVariable()
        {
            while (Variables.Count > currentVariable)
            {
                Variable var = Variables[currentVariable++];
                if (var.File == ShortFilename)
                {
                    // send request to GDB
                    string name = var.Name;
                    int arrayBracket = name.IndexOf('[');
                    if (arrayBracket > 0)
                    {
                        name = name.Substring(0, arrayBracket);
                    }
                    NewCommand("display " + name);
                    // wait for reply
                    return;
                }
            }
            currentVariable = -1;
        }

        private void GotResponse(GDB gdb, GDB.Interaction e)
        {
            Variable var;
            string[] parts;
            char[] delimiters = new char[] { '=',' ' };
            switch (e.state)
            {
                case GDB.State.setGlobals:

                    if (currentVariable >= 0 && currentVariable < Variables.Count)
                    {
                        var = Variables[currentVariable];
                        DisplayNextVariable();
                    }
                    else
                    {
                        // dealt with all the vars, can start debugging
                        gdb.SetState(GDB.State.ready);
                        GDBReady();
                    }
                    break;

                case GDB.State.getGlobals:

                    if (gdb.PromptReady)
                    {
                        // got all globals; ask for any locals
                        gdb.SetState(GDB.State.getLocals);
                        NewCommand("info locals");
                        return;
                    }
                    // e.var contains something like "var=3" or "var={3,4,5}"
                    parts = e.var.Split('=');
                    var = Variables.Find(x => parts[0] == x.Name);
                    if (var != null)
                    {
                        var.currentValue = parts[1];
                        UpdateVariableInWindow(var);
                    }
                    break;

                case GDB.State.getLocals:

                    if (e.var.Contains("No locals"))
                    {
                        LocalVariables.Clear();
                        NewCommand("info args");
                        return;
                    }
                    if (e.var.Contains("No symbol table"))
                    {
                        gdb.SetState(GDB.State.ready);
                        GUI.RunButtons(true);
                        return;
                    }
                    if (gdb.PromptReady)
                    {
                        UpdateLocals();
                        gdb.SetState(GDB.State.getArgs);
                        NewCommand("info args");
                        return;
                    }
                    // e.var contains something like "var=3" or "var={3,4,5}"
                    parts = e.var.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                        return;

                    var = LocalVariables.Find(x => x.Name == parts[0]);
                    if (var == null)
                    {
                        var = new Variable(this);
                        var.Name = parts[0];
                        if (var.Name.Contains(':'))
                        {
                            // error here....
                            return;
                        }
                        if (var.Name.Contains("(gdb)"))
                        {
                            // error here....
                            return;
                        }
                        var.isGlobal = false;
                        var.currentValue = parts[1];
                        LocalVariables.Add(var);
                    }
                    else
                    {
                        var.currentValue = parts[1];
                    }

                    break;


                case GDB.State.getArgs:

                    if (e.var.Contains("No arguments"))
                    {
                        ArgVariables.Clear();
                        gdb.SetState(GDB.State.ready);
                        GUI.RunButtons(true);
                        return;
                    }
                    if (e.var.Contains("No symbol table"))
                    {
                        gdb.SetState(GDB.State.ready);
                        GUI.RunButtons(true);
                        return;
                    }
                    if (gdb.PromptReady)
                    {
                        UpdateArgs();
                        gdb.SetState(GDB.State.ready);
                        GUI.RunButtons(true);
                        return;
                        // wait for new command from user
                    }
                    // e.var contains something like "var=3" or "var={3,4,5}"
                    
                    parts = e.var.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                    var = ArgVariables.Find(x => x.Name == parts[0]);
                    if (var == null)
                    {
                        var = new Variable(this);
                        var.Name = parts[0];
                        if (var.Name.Contains("(gdb)"))
                        {
                            // error here....
                            return;
                        }
                        var.isGlobal = false;
                        var.currentValue = parts[1];
                        ArgVariables.Add(var);
                    }
                    else
                    {
                        var.currentValue = parts[1];
                    }
                    break;


                case GDB.State.step:
                case GDB.State.next:
                case GDB.State.run:
                    if (gdb.PromptReady)
                    {
                        return;
                        // wait for new command from user
                    }
                    UpdateSource(e.linenum - 1);
                    {
                        gdb.SetState(GDB.State.getGlobals);
                    }
                    break;
                case GDB.State.stepout:
                    if (gdb.PromptReady)
                    {
                        return;
                        // wait for new command from user
                    }
                    UpdateSource(e.linenum - 1);
                    {
                        gdb.SetState(GDB.State.getGlobals);
                    }
                    break;
                case GDB.State.breakpoint:
                    if (gdb.PromptReady)
                    {
                        return;
                        // wait for new command from user
                    }
                    if (e.var.StartsWith("Deleted"))
                    {
                        Breakpoints.Remove(currentBreakpoint);
                    }
                    else if (e.var.StartsWith("Breakpoint"))
                    {
                        Breakpoints.Add(currentBreakpoint);
                    }
                    break;
                default:
                    MessageBox.Show("Unknown state: " + e.state, "Error");
                    break;
               }
        }

#endif

        public void Startup(Serial.SerialPortManager _spmanager)
        {
            CurrentState = State.init;
            currentBreakpoint = null;
            nextBreakpoint = null;

            this.spmanager = _spmanager;

#if __GDB__
            varView.Items.Clear();
            Variables.Clear();
            MyVariables.Clear();
            LocalVariables.Clear();
            PrevLocalVariables.Clear();
            ArgVariables.Clear();
            PrevArgVariables.Clear();
            Functions.Clear();

            gdb = new GDB(this);
            gdb.Open();
            gdb.iHandler += new GDB.InteractionHandler(GotResponse);
           
            // wait for gdb to invoke GDBReady()

#else

            spmanager.StartListening();  // this will reset the Arduino
            comString = string.Empty;

            Send("startup\n");
            // might take  a while for a reset etc
            comString = ReadLine(5000);
            if (comString.Length < 2)
            {
                MessageBox.Show("Cannot find device, please check connection");
                GUI.RunButtons(true);
                return;
            }
            GetVariables();
            SingleStep();

            GUI.RunButtons(true);
            varView.Enabled = true;
#endif
        }

#if __GDB__
        public void FindBreakpoint()
        {
            GUI.RunButtons(false);
            gdb.SetState(GDB.State.run);
            NewCommand("continue");
        }
        public void StepOver()
        {
            gdb.SetState(GDB.State.next);
            NewCommand("next");
        }
        public void StepOut()
        {
            gdb.SetState(GDB.State.stepout);
            NewCommand("finish");
        }
#else
        delegate Function FindFuncDelegate();
        /// <summary>
        /// See if this line calls a  function; if so is it one of ours?
        /// </summary>
        /// <returns></returns>
        Function FindFunctionWithinLine()
        {
            if (source.InvokeRequired)
            {
                FindFuncDelegate d = new FindFuncDelegate(FindFunctionWithinLine);
                varView.Invoke(d, new object[] {  });
            }
            else
            {
                if (currentBreakpoint != null)
                {

                    int line = currentBreakpoint.SourceLine;
                    ListViewItem sourceItem = source.Items[line - 1];
                    string sourceLine = sourceItem.SubItems[2].Text;
                    //FunctionType fType = FunctionType.None;
                    foreach (Function func in Functions)
                    {
                        if (sourceLine.Contains(func.Name))
                        {
                            //if (func.fileRef == SourceFileRef)
                            //    func.Owner = Function.FunctionOwner.Mine;
                            //else
                            //    func.Owner = Function.FunctionOwner.Other;
                            return func;
                        }
                    }
                    return null;

                }
                return null;
            }
            return null;
        }

        delegate void SingleStepDelegate(bool funcChecked);

        public void SingleStep(bool funcChecked = false)
        {

            if (source.InvokeRequired)
            {
                SingleStepDelegate d = new SingleStepDelegate(SingleStep);
                varView.Invoke(d, new object[] { funcChecked });
            }
            else
            {
                try
                {
                    if (!funcChecked)
                    {
                        Function func = FindFunctionWithinLine();
                        if (func != null)
                        {
                            if (func.IsMine == false)
                            {
                                // Cannot step into library functions, step over rather than into it.
                                StepOver(true);
                                return;
                            }
                        }
                    }

                    String stepStr = "P0000\n";
                    Send(stepStr);
                    GUI.RunButtons(false);
                    // make this a backgroud task so that we can abort, if required, with the 'pause' button
                    _Running = new BackgroundWorker();
                    _Running.WorkerSupportsCancellation = true;
                    _Running.DoWork += new DoWorkEventHandler((state, args) =>
                    {
                        do
                        {
                            comString = ReadLine();
                            if (comString.Length == 0)
                            {
                                MessageBox.Show("timeout in single step");
                                break;
                            }
                            if (_Running.CancellationPending)
                            {
                                // force device to think it's reached its target
                                Send("Y");
                                break;
                            }
                            char firstChar = comString[0];
                            if (firstChar == 'P')
                            {
                                // moved to where we need; this is our 'step'
                                //continuing = false;
                                newProgramCounter();
                                GetVariables();
                                break;
                            }
                            else if (firstChar == 'S')
                            {
                                string reply = newProgramCounter();
                                Send(reply);

                            }
                        }
                        while (true);

                        GUI.RunButtons(true);
                    });
                    _Running.RunWorkerAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Sorry there has been a problem. (A)");
                }

            }

        }

            /// <summary>
            /// need to set a temporary breakpoint, avoiding any function calls
            /// </summary>
        public void StepOver(bool funcChecked = false)
        {
            if (!funcChecked)
            {
                Function func = FindFunctionWithinLine();
                if (func == null)
                {
                    // asking to step over but there's no function here, so single step instead
                    //if (func.Owner == Function.FunctionOwner.None)
                    //if (func.IsMine )
                    //{
                        SingleStep(true);
                        return;
                    //}
                }
            }

            if (nextBreakpoint != null)
            {

                currentBreakpoint = nextBreakpoint;
                GoToBreakpoint(nextBreakpoint);
            }
            else
            {
                MessageBox.Show("Error, no suitable step found");
            }
        }

        public void FindBreakpoint()
        {
            bool bpFound = false;
            foreach (Breakpoint bp in Breakpoints)
            {
                if (bp.Manual)
                {
                    bpFound = true;
                    GoToBreakpoint(bp);
                    break;
                }
            }
            if (!bpFound)
            {
                MessageBox.Show("No breakpoints set!");
                return;
            }
        }
        public BackgroundWorker _Running = null;
        public void GoToBreakpoint(Breakpoint bp)
        {
            try
            {
                String sendStr = "P" + bp.ProgramCounter.ToString("X4") + "\n";
                Send(sendStr);

                // now wait for the bp to be hit.....or a pause command
                _Running = new BackgroundWorker();
                _Running.WorkerSupportsCancellation = true;
                bool pauseReqd = false;

                GUI.RunButtons(false);
                _Running.DoWork += new DoWorkEventHandler((state, args) =>
                {
                    do
                    {
                        comString = ReadLine(); // waiting for "?"...DoWeNeedToStop?
                        if (comString.Length > 0)
                        {
                            if (comString[0] == '?')
                            {
                                //check for pause button just pressed
                                if (_Running.CancellationPending)
                                {
                                    Send("X");        // instruction to force targetPC to be equal to current PC
                                    pauseReqd = true;
                                    break;
                                }
                                else
                                {
                                    Send("N");
                                }
                            }
                            else // reached predefined breakpoint
                                break;
                        }
                    } while (true);
                    Thread.Sleep(100); //allow time for target to catch up
                    GetVariables();

                    //UpdateVariableWindow();
                    UpdateCodeWindows(bp.ProgramCounter);
                    if (!pauseReqd && bp.Manual)
                        MarkBreakpointHit(bp);
                    GUI.RunButtons(true);
                });
                _Running.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Sorry there has been a problem (B)");
            }
        }



         public void GetVariables()
        {

            //// first see if we need to also get local variables
            Function func = null;
            if (currentBreakpoint != null)
            {
                UInt16 progCounter = currentBreakpoint.ProgramCounter;
                
                foreach (Function f in Functions)
                {
                    if (f.IsMine)
                    {
                        if (f.HighPC > progCounter && f.LowPC <= progCounter)
                        {
                            // we are currently 'in' this function
                            func = f;
                            break;
                        }
                    }

                }
            }

            String sendStr = "PFFFF\n";  // todo: can get rid of this command to save time & code space
            Send(sendStr);
            //System.Threading.Thread.Sleep(100);

            // get current frame pointer, needed for local variables
            comString = ReadLine();
            if (comString.Length > 4 && comString[0]=='F' && currentBreakpoint != null)
            {
                ushort fpointer;
                if (ushort.TryParse(comString.Substring(1, 4), System.Globalization.NumberStyles.HexNumber, null, out fpointer))
                {
                    currentBreakpoint.FramePointer = fpointer;
                }
            }
            foreach (Variable var in Variables)
            {
                if (func != null)
                {
                    if (var.Function != func)
                    {
                        // We are not in the same function where this variable is used
                        continue;
                    }
                }
                if (var.GetValue(func) == false)
                {
                    break;
                }
            }
            UpdateVariableWindow();

        }
#endif

        /// <summary>
        /// A list of variables which have been 'expanded' to show contents (of arrays etc)
        /// </summary>
        private List<ListViewItem> expandedItems = new List<ListViewItem>();

        /// <summary>
        /// Attempt to expand a compound variable by clicking on its row
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Variable_Click(object sender, EventArgs e)
        {
#if __GDB__
            if (varView.SelectedItems.Count == 0)
                return;
            try
            {
                ListViewItem clicked = varView.SelectedItems[0];
                string itemName = clicked.Name;
                Variable var = Variables.Find(x => x.Name == itemName);
                if (var == null)
                    return;
                if (var.isArray || var.isPointer || var.isStruct)
                {
                    // can be expanded
                    if (expandedItems.Contains(clicked) == false)
                    {
                        // This row is not expanded.
                        // Expand item to show contents of this compound variable
                        int size = var.Type.Size;
                        int index = clicked.Index;
                        //ushort addr = var.Address;

                        // note that this row is expanded, so we can contract it again later
                        expandedItems.Add(clicked);
                        // show it can be unexpanded later
                        clicked.Text = "-";
                        if (var.isPointer)
                        {
                            // todo
                        }
                        else if (var.isArray)
                        {
                            // get the individual array elements
                            // e.g. {elem1,elem2,elem3,...}
                            char[] delimiters = new char[] { '{', '}', ',' ,' '};
                            string[] arrayElements = var.currentValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                            int i = 0;
                            //int bracket = itemName.IndexOf('[');
                            //string varName = itemName.Substring(0, bracket);
                            foreach (string elem in arrayElements) 
                            {
                                Variable arrayElement = new Variable(this);
                                arrayElement.Type = var.Type;
                                arrayElement.currentValue = elem;
                                arrayElement.Name = itemName + '[' + i + ']';
                                // temporarily add the expanded item to the list of variables, so it will be updated during single-stepping etc.
                                Variables.Add(arrayElement);

                                  // create a new row and display it
                                string[] items = { " ","  " + arrayElement.Name, arrayElement.currentValue };
                                ListViewItem arrayItem = new ListViewItem(items);
                                arrayItem.Name = arrayElement.Name;
                                arrayItem.BackColor = System.Drawing.Color.Azure;
                                varView.Items.Insert(++index, arrayItem);

                                // tag the new row with the row that was expanded, so we can easily unexpand later
                                clicked.Tag = itemName;      // this should be unique
                                arrayItem.Tag = clicked.Tag;
                                ++i;
                            }
                        }
                        else if (var.isStruct)
                        {
                            // get the individual struct elements
                            // e.g. {elem1=1,elem2=3,elem3=4.5,...}
                            char[] delimiters = new char[] { '{', '}', ',', ' ','=' };
                            string[] structMembers = var.currentValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                            int i = 0;
                            for (int elem=0; elem < structMembers.Length; elem+=2)
                            {
                                Variable structMember = new Variable(this);
                                structMember.Type = var.Type;
                                structMember.currentValue = structMembers[elem+1];
                                structMember.Name = itemName + '.'+ structMembers[elem]; 
                                // temporarily add the expanded item to the list of variables, so it will be updated during single-stepping etc.
                                Variables.Add(structMember);

                                // create a new row and display it
                                string[] items = {" ", "  " + structMember.Name, structMember.currentValue };
                                ListViewItem structItem = new ListViewItem(items);
                                structItem.Name = structMember.Name;
                                structItem.BackColor = System.Drawing.Color.Azure;
                                varView.Items.Insert(++index, structItem);

                                // tag the new row with the row that was expanded, so we can easily unexpand later
                                clicked.Tag = itemName;      // this should be unique
                                structItem.Tag = clicked.Tag;
                                ++i;
                            }
                        }

                        UpdateVariableWindow();
                    }
                    else
                    {
                        // This row has already been expanded.
                        // Unexpand the added items, and remove the extra temporary variables
                        clicked.Text = "+";
                        foreach (ListViewItem item in varView.Items)
                        {
                            if (item.Tag != null && item != clicked)
                            {
                                // this was an expanded row. Was it expanded under the row that was clicked?
                                if (item.Tag.ToString() == clicked.Tag.ToString())
                                {
                                    // yes, remove it
                                    string varName = item.Name;
                                    Variable arrayVar = Variables.Find(x => x.Name == varName);
                                    Variables.Remove(arrayVar);
                                    varView.Items.Remove(item);
                                }
                            }
                        }
                        expandedItems.Remove(clicked);
                        clicked.Tag = null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Sorry there has been a problem (C)");
            }
#else
            if (varView.SelectedItems.Count == 0)
                return;
            try
            {
                ListViewItem clicked = varView.SelectedItems[0];
                string itemName = clicked.Name;
                Variable var = Variables.Find(x => x.Name == itemName);
                if (var == null)
                    return;
                if (var.Type.BaseType == null)
                    // if type is already a base type it can't be expanded
                    return;

                if (expandedItems.Contains(clicked) == false)
                {
                    // This row is not expanded.
                    // Expand item to show contents of this compound variable
                    int size = var.Type.Size;
                    int index = clicked.Index;
                    ushort addr = var.Address;

                    // note that this row is expanded, so we can contract it again later
                    expandedItems.Add(clicked);

                    if (var.Type.Name == "pointer")
                    {
                        // get the value pointed to
                        //Variable pointerElement = new Variable(this);
                        Variable indirect = new Variable(this);

                        // get the indirected value of this pointer
                        int indAddr = -1;
                        //int.TryParse(pointerElement.currentValue, out indAddr);
                        int.TryParse(var.currentValue, out indAddr);
                        if (indAddr < 0)
                            return;

                        // successful address found; create a new variable to show its content
                        indirect.Address = (ushort)indAddr;
                        indirect.Type = var.Type.BaseType;
                        indirect.Name = "*" + var.Name;
                        // temporarily add the expanded item to the list of variables, so it will be updated during single-stepping etc.
                        Variables.Add(indirect);
                        // ask the device for the value of the indirected variable
                        indirect.GetValue(null);

                        // create a new row and display it
                        string[] items = { "  " + indirect.Name, var.Type.BaseType.Name, "0x" + indAddr.ToString("X4"), indirect.currentValue };
                        ListViewItem indirectItem = new ListViewItem(items);
                        indirectItem.Name = indirect.Name;
                        indirectItem.BackColor = System.Drawing.Color.Azure;
                        varView.Items.Insert(++index, indirectItem);

                        // tag the new row with the row that was expanded, so we can easily unexpand later
                        clicked.Tag = var.Address;      // this should be unique
                        indirectItem.Tag = clicked.Tag;
                    }
                    else
                    {
                        // get the individual array elements from memory
                        for (int i = 0; i < size; i++)
                        {
                            Variable arrayElement = new Variable(this);

                            arrayElement.Address = addr;
                            arrayElement.Type = var.Type.BaseType;
                            arrayElement.Name = itemName + '[' + i + ']';
                            // temporarily add the expanded item to the list of variables, so it will be updated during single-stepping etc.
                            Variables.Add(arrayElement);

                            arrayElement.GetValue(null);

                            // create a new row and display it
                            string[] items = { "  " + arrayElement.Name, var.Type.BaseType.Name, addr.ToString("X4"), arrayElement.currentValue };
                            ListViewItem arrayItem = new ListViewItem(items);
                            arrayItem.Name = arrayElement.Name;
                            arrayItem.BackColor = System.Drawing.Color.Azure;
                            varView.Items.Insert(++index, arrayItem);

                            // tag the new row with the row that was expanded, so we can easily unexpand later
                            clicked.Tag = var.Address;      // this should be unique
                            arrayItem.Tag = clicked.Tag;

                            // get ready for next member of array
                            addr += (ushort)var.Type.BaseType.Size;
                        }

                    }
                    UpdateVariableWindow();
                }
                else
                {
                    // This row has already been expanded.
                    // Unexpand the added items, and remove the extra temporary variables

                    foreach (ListViewItem item in varView.Items)
                    {
                        if (item.Tag != null && item != clicked)
                        {
                            // this was an expanded row. Was it expanded under the row that was clicked?
                            if (item.Tag.ToString() == clicked.Tag.ToString())
                            {
                                // yes, remove it
                                string varName = item.Name;
                                Variable arrayVar = Variables.Find(x => x.Name == varName);
                                Variables.Remove(arrayVar);
                                varView.Items.Remove(item);
                            }
                        }
                    }
                    expandedItems.Remove(clicked);
                    clicked.Tag = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Sorry there has been a problem (C)");
            }
#endif
        }

        /// <summary>
        /// use the list of breakpoint-able lines to determine if we are at the next place to 'single-step' to
        /// </summary>
        /// <param name="pc">pc at current breakpoint</param>
        public bool AreWeThereYet(ushort pc)
        {
            foreach (Breakpoint bp in Breakpoints)
            {
                if (bp.ProgramCounter == pc)
                {
                    currentBreakpoint = bp;
                    return true;
                }
            }
            return false;
        }


        private void UpdateLocals()
        {
            bool varsNeedDeleting = false;
            int varCount = LocalVariables.Count;
            if (varCount == 0)
            {
                varsNeedDeleting = true;
            }
            else
            {
                for (int v = 0; v < varCount; v++)
                {
                    if (PrevLocalVariables.Count < 1 + v )
                    {
                        break;
                    }
                    Variable var = LocalVariables[v];
                    if (var.Name != PrevLocalVariables[v].Name)
                    {
                        // at least one name has changed so this is a new function
                        varsNeedDeleting = true;
                        break;
                    }
                }
            }
            if (varsNeedDeleting)
            {
                foreach (Variable var in PrevLocalVariables)
                {
                    var.isMine = false;
                    UpdateVariableInWindow(var);
                }
            }
            PrevLocalVariables.Clear();
            foreach (Variable var in LocalVariables)
            {
                var.isMine = true;
                UpdateVariableInWindow(var);
                PrevLocalVariables.Add(var);
            }
        }

        private void UpdateArgs()
        {
            bool varsNeedDeleting = false;
            int varCount = ArgVariables.Count;
            if (varCount == 0)
            {
                varsNeedDeleting = true;
            }
            else
            {
                for (int v = 0; v < varCount; v++)
                {
                    if (PrevArgVariables.Count < 1 + v)
                    {
                        break;
                    }
                    Variable var = ArgVariables[v];
                    if (var.Name != PrevArgVariables[v].Name)
                    {
                        // at least one name has changed so this is a new function
                        varsNeedDeleting = true;
                        break;
                    }
                }
            }
            if (varsNeedDeleting)
            {
                foreach (Variable var in PrevArgVariables)
                {
                    var.isMine = false;
                    UpdateVariableInWindow(var);
                }
            }
            PrevArgVariables.Clear();
            foreach (Variable var in ArgVariables)
            {
                var.isMine = true;
                UpdateVariableInWindow(var);
                PrevArgVariables.Add(var);
            }
        }
#if __GDB__
        delegate void varViewDelegate(Variable var);
        void UpdateVariableInWindow(Variable var)
        {
            if (varView.InvokeRequired)
            {
                varViewDelegate d = new varViewDelegate(UpdateVariableInWindow);
                varView.Invoke(d, new object[] { var });
            }
            else
            {
                ListViewItem lvi;

                ListView.ListViewItemCollection vars = varView.Items;
                ListViewItem[] lvis = vars.Find(var.Name, false);


                if (lvis == null || lvis.Length == 0)
                {
                    if (var.isGlobal == false)
                    {

                        lvi = var.CreateVarViewItem();
                        if (var.currentValue.Contains("<optimized"))
                        {
                            var.currentValue = "Unavailable";
                        }
                        lvi.SubItems[1].Text = "  " + lvi.SubItems[1].Text;
                        lvi.BackColor = System.Drawing.Color.Beige;
                        varView.Items.Insert(varView.Items.Count, lvi);
                        
                    }
                    else
                        return;
                }
                else
                {
                    lvi = lvis[0];
                }
                if (var.isGlobal== false && var.isMine == false)
                {
                    // needs removing from list
                    varView.Items.Remove(lvi);
                    return;
                }
                if (var.currentValue.Contains('\\'))
                {
                    if (var.Type.Name == "char")
                    {
                        // convert from octal
                        // may be an array "\000\000\000....." or just a single value
                        char[] delimiters = new char[] { '\\', '"' };
                        string[] octals = var.currentValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                        string newString = "{";
                        foreach (string octal in octals)
                        {
                            if (octal.Length == 3)
                            {
                                int val = octal[0] - '0';
                                val = (val << 3) + (octal[1] - '0');
                                val = (val << 3) + (octal[2] - '0');
                                newString += val;
                                if (val >= 32 && val < 127)
                                    newString += '(' + (char)val + ')';
                                newString += ',';
                            }
                            
                        }
                        newString += '}';
                        var.currentValue = newString;

                    }
                }
                lvi.SubItems[2].Text = var.currentValue;

                if (var.currentValue != var.lastValue)
                {
                    lvi.ForeColor = System.Drawing.Color.Red;
                    // if this is a pointer, and it has changed, 
                    //   we need to also find a new value for the value pointed to, if it's currently displayed
                    if (var.isPointer)
                    {
                        // get the indirected value of this pointer
                        ushort indAddr = 0;
                        if (ushort.TryParse(var.currentValue, out indAddr))
                        {
                            // if it's currently displayed it will be in the list
                            var indirect = Variables.Find(x => x.Name == "*" + var.Name);
                            if (indirect != null)
                            {
                                indirect.Address = indAddr;
                                //  indirect.GetValue(null);
                            }
                        }
                    }
                }
                else
                {
                    lvi.ForeColor = System.Drawing.Color.Black;
                }
                var.lastValue = var.currentValue;

            }


        }
        delegate void varViewDelegate2();
        void UpdateVariableWindow()
        {
            if (varView.InvokeRequired)
            {
                varViewDelegate2 d = new varViewDelegate2(UpdateVariableWindow);
                varView.Invoke(d, new object[] { });
            }
            else
            {

                foreach (Variable var in Variables)
                {
                    ListView.ListViewItemCollection vars = varView.Items;
                    ListViewItem[] lvis = vars.Find(var.Name, false);
                    if (lvis == null || lvis.Length == 0)
                        continue;
                    ListViewItem lvi = lvis[0];
                    if (var.currentValue != var.lastValue)
                    {
                        lvi.ForeColor = System.Drawing.Color.Red;

                    }
                    else
                    {
                        lvi.ForeColor = System.Drawing.Color.Black;
                    }
                    var.lastValue = var.currentValue;

                }
            }
        }

#else
        delegate void varViewDelegate();
        void UpdateVariableWindow()
        {
            if (varView.InvokeRequired)
            {
                varViewDelegate d = new varViewDelegate(UpdateVariableWindow);
                varView.Invoke(d, new object[] { });
            }
            else
            {

                foreach (Variable var in Variables)
                {
                    ListView.ListViewItemCollection vars = varView.Items;
                    ListViewItem[] lvis = vars.Find(var.Name,false);
                    if (lvis == null || lvis.Length == 0)
                        continue;
                    ListViewItem lvi = lvis[0];

                    if (var.Address != 0)
                    {
                        lvi.SubItems[2].Text = "0x" + var.Address.ToString("X4");
                        if (var.Type.BaseType == null)
                        {
                            lvi.SubItems[3].Text = var.currentValue;
                        }
                        else if (var.Type.Name == "array")
                        {
                            lvi.SubItems[3].Text = "....";
                        }
                        else if (var.Type.Name == "pointer")
                        {
                            // show address pointed to (in hex)
                            int addr = -1;
                            int.TryParse(var.currentValue, out addr);
                            if (addr >= 0)
                            {
                                lvi.SubItems[3].Text = "0x" + addr.ToString("X4");
                            }

                        }
                        if (var.currentValue != var.lastValue)
                        {
                            lvi.ForeColor = System.Drawing.Color.Red;
                            // if this is a pointer, and it has changed, 
                            //   we need to also find a new value for the value pointed to, if it's currently displayed
                            if (var.Type.Name == "pointer")
                            {
                                // get the indirected value of this pointer
                                ushort indAddr = 0;
                                if (ushort.TryParse(var.currentValue, out indAddr))
                                {
                                    // if it's currently displayed it will be in the list
                                    var indirect = Variables.Find(x => x.Name == "*" + var.Name);
                                    if (indirect != null)
                                    {
                                        indirect.Address = indAddr;
                                        indirect.GetValue(null);
                                    }
                                }
                            }
                        }
                        else
                        {
                            lvi.ForeColor = System.Drawing.Color.Black;
                        }
                        var.lastValue = var.currentValue;
                    }
                    else
                    {
                        //lvi.Text = "  " + lvi.Text;
                        lvi.BackColor = System.Drawing.Color.Beige;
                    }
                }
            }
        }
#endif
        void UpdateCodeWindows(ushort pc)
        {
            UpdateDisassembly(pc);
            //UpdateSource();
        }

        delegate void updateDissDelegate(ushort pc);
        void UpdateDisassembly(ushort pc)
         {
            if (disassembly.InvokeRequired)
            {
                updateDissDelegate d = new updateDissDelegate(UpdateDisassembly);
                disassembly.Invoke(d, new object[] { pc });
            }
            else
            {
                int linecount = 0;
                nextBreakpoint = null;
                if (disassembly != null )
                {
                    // find a line that starts with [whitespace][pc][:]
                    ListView.ListViewItemCollection disItems = disassembly.Items;
                    string pcStr = pc.ToString("x") + ':';
                    linecount = 0;
                    bool currentLineFound = false;
                    int nextSourceLine = 0;
                    foreach (ListViewItem disItem in disItems)
                    {
                        ++linecount;
                        string line = disItem.SubItems[1].Text;
                        if (line.Contains(pcStr) && !currentLineFound)
                        {
                            int index = disItem.Index;
                            disassembly.Items[index].Selected = true;
                            disassembly.Select();
                            disassembly.EnsureVisible(index);
                            currentLineFound = true;
                        }
                        else if (currentLineFound)
                        {
                            // searching for next source line that we'll need for 'step over' instruction
                            //  i.e. avoiding any function calls here
                            //char[] delimiters = new char[] { ' ', ':' };
                            int starIndex = line.IndexOf(" *** ");
                            if (starIndex > 0)
                            {
                                // this is our added ref to the source code.
                                string sourceLine = line.Substring(starIndex + 5);
                                // Try to find corresponding source line in source window
                                nextSourceLine = FindSourceFromDisassembly(sourceLine);
                                if (nextSourceLine > 0)
                                {
                                    // this will be the next bp for 'step over'
                                    foreach (Breakpoint bp in Breakpoints)
                                    {
                                        if (bp.SourceLine == nextSourceLine)
                                        {
                                            nextBreakpoint = bp;
                                            break;
                                        }
                                    }
                                    // done searching
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }


#if __GDB__
        delegate void updateSourceDelegate(int linenum);
        void UpdateSource(int linenum)
        {
            if (source == null)
                return;
            if (linenum < 0)
                return;

            if (source.InvokeRequired)
            {
                updateSourceDelegate d = new updateSourceDelegate(UpdateSource);
                source.Invoke(d, new object[] { linenum});
            }
            else
            {
                if (linenum > source.Items.Count)
                {
                    // not in this file; not dealing with this situation yet
                    // keep selected line as before
                    if (currentLine > 0)
                    {
                        source.Items[currentLine].Selected = true;
                        source.Select();
                        source.EnsureVisible(currentLine);
                    }
                    return;
                }

                if (currentLine > 0 && currentLine < source.Items.Count)
                {
                    source.Items[currentLine].Selected = false;

                }

                currentLine = linenum;

                if (currentLine < source.Items.Count)
                {
                    source.Items[currentLine].Selected = true;
                    source.Select();
                    source.EnsureVisible(linenum);
                }
                // find the line that contains the current breakpoint
                // ListView.ListViewItemCollection sourceItems = source.Items;
                //int linecount = 0;
                //bool lineFound = false;
                //if (currentBreakpoint != null && currentBreakpoint.SourceLine < source.Items.Count)
                //{
                //    source.Items[currentBreakpoint.SourceLine].Selected = false;
                //    currentBreakpoint.SourceLine = linenum;
                //}
                //else
                //{
                //    currentBreakpoint = new Breakpoint("file", linenum);
                //}
                //if (currentBreakpoint.SourceLine < source.Items.Count)
                //{
                //    source.Items[currentBreakpoint.SourceLine].Selected = true;
                //    source.Select();
                //    source.EnsureVisible(linenum);
                //}
            }

        }
#else
        delegate void updateSourceDelegate();
        void UpdateSource()
        {
            if (source.InvokeRequired)
            {
                updateSourceDelegate d = new updateSourceDelegate(UpdateSource);
                source.Invoke(d, new object[] { });
            }
            else
            {
                if (source == null)
                    return;
                // find the line that contains the current breakpoint
                ListView.ListViewItemCollection sourceItems = source.Items;
                int linecount = 0;
                bool lineFound = false;
                foreach (ListViewItem sourceItem in sourceItems)
                {
                    if (sourceItem.BackColor == breakpointHitColour)
                    {
                        sourceItem.BackColor = breakpointColour;
                    }

                    ++linecount;
                    if (currentBreakpoint != null && currentBreakpoint.SourceLine == linecount)
                    {
                        int index = sourceItem.Index;
                        source.Items[index].Selected = true;
                        source.Select();
                        source.EnsureVisible(index);
                        lineFound = true;
                        break;
                    }
                }
                if (!lineFound)
                {
                    source.Items[0].Selected = true;
                    source.EnsureVisible(0);
                }
            }

        }
#endif


        private void Source_Click(object sender, EventArgs e)
        {

            if (source.SelectedItems.Count == 0)
                return;
            ListViewItem lvi = source.SelectedItems[0];
            //ListView.ListViewItemCollection sourceItems = source.Items;

            lvi.Selected = false;

            // clear any previous breakpoints
            //foreach (Breakpoint bp in Breakpoints)
            //{
            //    if (bp.Manual)
            //    {
            //        bp.Manual = false;
            //        ListViewItem bpItem = sourceItems[bp.SourceLine - 1];
            //         bpItem.BackColor = sourceLineColour;

            //    }
            //}

            // set a new breakpoint (only 1 bp allowed for now....)

            if (lvi.BackColor == sourceLineColour) // i.e. breakpoint possible on this line
            {
#if __GDB__
                for (int b=0; b < Breakpoints.Count; b++)
                {
                    Breakpoint bp = Breakpoints[b];
                    if (bp.SourceLine == lvi.Index + 1)
                    {
                        // already a bp here, remove it
                        lvi.ImageIndex = -1;
                        //Breakpoints.Remove(bp);
                        gdb.SetState(GDB.State.breakpoint);
                        currentBreakpoint = new Breakpoint(ShortFilename, lvi.Index + 1);
                        NewCommand(string.Format("clear {0}:{1}", ShortFilename, lvi.Index + 1));
                        // wait for resposne before removing bp from our own list
                        return;
                    }
                }
                if (Breakpoints.Count < MaxBreakpoints)
                {
                    lvi.ImageIndex = 0;
                    //Breakpoints.Add(new Breakpoint(this.ShortFilename, lvi.Index + 1));
                    gdb.SetState(GDB.State.breakpoint);
                    currentBreakpoint = new Breakpoint(ShortFilename, lvi.Index + 1);
                    NewCommand(string.Format("break {0}:{1}",ShortFilename,lvi.Index + 1));
                    // wait for resposne before adding bp to our own list
                }
                else
                {
                    MessageBox.Show(string.Format("Only {0} breakpoints allowed", MaxBreakpoints));
                }


#else
                // now need to find the correct (single-step) breakpoint and make it manual 
                foreach (Breakpoint bp in Breakpoints)
                {
                    if (bp.SourceLine == lvi.Index + 1)
                    {

                            bp.Manual = true;
                            lvi.Selected = false;
                            lvi.BackColor = breakpointColour;

                    }
                }
#endif
            }
            //else if (lvi.BackColor == breakpointColour || lvi.BackColor == breakpointHitColour) // i.e. breakpoint possible on this line
            //{
            //    // now need to find the correct (single-step) breakpoint and make it not manual 
            //    foreach (Breakpoint bp in Breakpoints)
            //    {
            //        if (bp.SourceLine == lvi.Index + 1)
            //        {
  
            //                // undoing a previous breakpoint
            //                bp.Manual = false;
            //                lvi.Selected = false;
            //                lvi.BackColor = sourceLineColour;
            //         }
            //    }
            //}


        }
        delegate void bpDelegate(Breakpoint bp);
        private void MarkBreakpointHit(Breakpoint bp)
        {
            if (source.InvokeRequired)
            {
                bpDelegate d = new bpDelegate(MarkBreakpointHit);
                source.Invoke(d, new object[] { bp });
            }
            else
            {
                if (bp.SourceLine > 1)
                {
                    int index = bp.SourceLine - 1;
                    ListView.ListViewItemCollection sourceItems = source.Items;
                    ListViewItem item = sourceItems[index];
                    item.BackColor = breakpointHitColour;
                    source.EnsureVisible(index);

                    if (source.SelectedItems.Count == 1)
                    {
                        item = source.SelectedItems[0];
                        item.Selected = false;
                    }
                    currentBreakpoint = bp;
                }
            }
        }


        public string newProgramCounter()
        {
            ushort pc;
            if (comString.Length < 5)
                return null;
            if (ushort.TryParse(comString.Substring(1,4), System.Globalization.NumberStyles.HexNumber, null, out pc))
            {
                byte firstChar = (byte)comString[0];
                if (firstChar == 'S')
                //if (firstChar == (char)Chars.STEPPING_CHAR)
                    {
                    // single-stepping
                    if (AreWeThereYet(pc))
                    {
                       // UpdateCodeWindows(pc);
                        //return Chars.YES_CHAR.ToString();
                        return "Y";
                    }
                    else
                        //return Chars.NO_CHAR.ToString();
                        return "N";

                }
                else
                {
                    UpdateCodeWindows(pc);
                }
            }
            return null;
        }

        //public void FunctionList()
        //{
        //    if (Functions.Count == 0)
        //        return;
        //    string funcList = "";
        //    foreach (Function func in Functions)
        //    {
        //        funcList += func.Name;
        //        funcList += "\t";
        //        funcList += func.fileRef;
        //        if (func.IsMine)
        //            funcList += " *";
        //        funcList += "\n";
        //    }
            
        //    MessageBox.Show(funcList,"Function List");
            

        //}
        
    }

}
