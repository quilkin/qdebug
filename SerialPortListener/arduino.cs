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
    class Arduino
    {

        private ListView source, disassembly, varView;
        private TextBox comms;
        public String ShortFilename { private set; get; }
        public String FullFilename { private set; get; }

        private List<Breakpoint> Breakpoints = new List<Breakpoint>();
        private List<Variable> Variables = new List<Variable>();
        private List<VariableType> VariableTypes = new List<VariableType>();
        /// <summary>
        /// variable names found by parsing the assembler file, included in our own source files
        /// Just used for comaprison when parsing full list of variables which contain eveything
        /// </summary>
        private List<String> MyVariables = new List<String>();

        /// <summary>
        /// where program counter is currently sitting
        /// </summary>
        private Breakpoint currentBreakpoint = null;
        /// <summary>
        /// next place to stop if we skip over a function call
        /// </summary>
        private Breakpoint nextBreakpoint = null;

        public String comString = String.Empty;

        Serial.SerialPortManager spmanager;

        public bool pauseReqd { set; get; }
        /// <summary>
        /// ascii chars used for interaction strings
        /// (can't use under 128 because normal Serial.print will use them)
        /// </summary>
        //public enum Chars : byte { PROGCOUNT_CHAR = 248, TARGET_CHAR, STEPPING_CHAR, ADDRESS_CHAR, DATA_CHAR, NO_CHAR, YES_CHAR };

        // varaible types built in to gcc or typedefs defined by Arduino
        const string ReservedTypeWords = "signed char unsigned int float double long volatile";
        const string TypedefWords = "word boolean bool byte uint8_t uint16_t uint32_t uint64_t";

        System.Drawing.Color sourceLineColour = System.Drawing.Color.AliceBlue;
        System.Drawing.Color breakpointColour = System.Drawing.Color.Red;
        System.Drawing.Color breakpointHitColour = System.Drawing.Color.Orange;



        #region FILES

        public bool OpenFiles(ListView source, ListView disassembly, ListView variables, TextBox comms)
        {
            this.source = source;
            this.disassembly = disassembly;
            this.varView = variables;
            this.comms = comms;
            Breakpoints.Clear();
            if (parseSourceFile())
            {

                if (OpenDisassembly())
                {

                    source.Click += Source_Click;
                    varView.Click += Variable_Click;
                    return true;
                }

            }
            MessageBox.Show("Problem opening files");
            return false;
        }


        private bool parseSourceFile()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Arduino files|*.ino;*.c;*.cpp|All files (*.*)|*.*";
            ofd.Title = "Load Arduino Sketch";
            var dialogResult = ofd.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                ShortFilename = ofd.SafeFileName;
                FullFilename = ofd.FileName;
                source.Items.Clear();
                disassembly.Items.Clear();
                varView.Items.Clear();
                Variables.Clear();
                MyVariables.Clear();
                int count = 1;
                foreach (var line in System.IO.File.ReadLines(ofd.FileName))
                {
                    // tabs ('\t') seem ignored in listview so replace with spaces
                    string untabbed = line.Replace("\t", "    ");
                    string trimmed = line.Trim();

                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = ""; // for breakpoint markers

                    lvi.SubItems.Add((count++).ToString());
                    lvi.SubItems.Add(untabbed);
                    source.Items.Add(lvi);

                    // see which variables declared here
                    // Need to know so we can separate our own vars from all the library ones, when we parse the debug file

                    // looking for something like "  int locali = iii*3;"
                    // or                        "unsigned int ms = 0;"
                    // or just                  "float numf;"
                    // or                       "unsigned long ms;"
                    // or                       "char array[10];"
                    // but not if there's a function definition here
                    // and not an assignment e.g. " ms = 3;"
                    // or                         " array [x] = 3;"

                    string line2 = line;
                    if (line2.Length < 3)
                        continue;
                    if (line2.IndexOf('(') >= 0)
                        continue;  // function definition....

                    int equals = line2.IndexOf('=');
                    if (equals > 0)
                    {
                        // get rid of any assignments made with the var name, not interested at this stage
                        line2 = line2.Substring(0, equals);
                    }
                    char[] delimiters = new char[] { ' ', '[' };
                    string[] parts = line2.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 1)
                        continue;
                    if (parts[0].StartsWith("#"))
                        continue;
                    if (parts[0].StartsWith("//"))
                        continue;
                    if (ReservedTypeWords.Contains(parts[0]) || TypedefWords.Contains(parts[0]))
                    {
                        // should be a variable declaration. Might be local (deal with that later.....)
                        // find first word that is NOT in the reserved lists
                        string varName = null;
                        for (int p = 1; p < parts.Length; p++)
                        {
                            string part = parts[p];
                            if (part.EndsWith(";"))
                            {
                                part = part.Substring(0, part.Length - 1);
                            }
                            if (ReservedTypeWords.Contains(part) || TypedefWords.Contains(part))
                            {
                                continue;
                            }
                            varName = part;
                            break;
                        }
                        if (varName != null)
                        {
                            MyVariables.Add(varName);
                        }
                    }
                }
                return true;
            }
            return false;
        }

        private bool doObjDump(ProcessStartInfo startInfo, string fileExt)
        {

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    using (StreamWriter writer = File.CreateText(ShortFilename + fileExt))
                    using (StreamReader reader = exeProcess.StandardOutput)
                    {
                        writer.AutoFlush = true;
                        for (;;)
                        {
                            string textLine = reader.ReadLine();
                            if (textLine == null)
                                break;
                            writer.WriteLine(textLine);
                        }

                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
            return true;
        }

        private bool OpenDisassembly()
        {
            // find the .elf file corresponding to this sketch

            string elfPath = null;
            if (ShortFilename.EndsWith(".ino"))
            {
                string[] arduinoPaths = Directory.GetDirectories(Path.GetTempPath(), "arduino_build_*");


                foreach (string path in arduinoPaths)
                {
                    elfPath = path + "\\" + ShortFilename + ".elf";
                    if (File.Exists(elfPath))
                    {
                        break;
                    }
                }
            }
            else
            { // non-Arduino (Atmel Studio) project
                // debug files should be in ..\debug relative to source folder
                string path = FullFilename;
                int index = path.IndexOf("\\src\\");
                if (index > 0)
                {
                    //C: \Users\chris\Documents\Atmel Studio\7.0\MEGA_LED_EXAMPLE1\MEGA_LED_EXAMPLE1\src\mega_led_example.c

                    elfPath = path.Substring(0, index);
                    int lastSlash = elfPath.LastIndexOf("\\");
                    string nameRoot = elfPath.Substring(lastSlash + 1);

                    elfPath = elfPath + "\\Debug\\" + nameRoot + ".elf"; ;

                }
            }
            if (elfPath == null)
            {
                MessageBox.Show("No compiled files found. You may need to recompile your project");
                return false;
            }
            // in case path includes spces, argument needs quotes
            elfPath = "\"" + elfPath + "\"";

            // Use ProcessStartInfo class
            // objdump - d progcount2.ino.elf > progcount2.ino.lss
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "avr-objdump.exe";
            startInfo.RedirectStandardOutput = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            startInfo.Arguments = "-S -l -C -t " + elfPath;

            // disassembly file
            if (doObjDump(startInfo, ".lss") == false)
                return false;
            //if (ParseSourceInfo(ShortFilename) == false)
            //    return false;
            if (ParseDisassembly(ShortFilename + ".lss") == false)
                return false;

            // debug info file
            startInfo.Arguments = "-Wi " + elfPath;
            if (doObjDump(startInfo, ".dbg") == false)
                return false;
            if (ParseDebugInfo(ShortFilename + ".dbg") == false)
                return false;

            //// line number table
            //startInfo.Arguments = "-W " + elfPath;
            //if (doObjDump(startInfo, ".lin") == false)
            //    return false;

            return true;

        }

        #endregion

        #region PARSE
        private bool ParseDebugInfo(string file)
        {
            Variable var = null;
            VariableType varType = null;

            // info from http://www.dwarfstd.org/doc/Debugging%20using%20DWARF-2012.pdf

            // first of all need to find a list of variable types used. Info is in this format:
            //  <1><6c8>: Abbrev Number: 6 (DW_TAG_base_type)
            //  <6c9>   DW_AT_byte_size   : 2
            //  <6ca>   DW_AT_encoding    : 5	(signed)
            //  <6cb>   DW_AT_name        : int
            VariableTypes.Clear();
            bool insideDef = false;
            foreach (string line in File.ReadLines(file))
            {

                if (varType != null)
                {
                    // found something in the previous line
                    insideDef = true;
                    if (line.Contains("DW_AT_byte_size"))
                    {
                        int index = line.LastIndexOf(':');
                        int size = 0;
                        if (int.TryParse(line.Substring(index + 1), out size))
                        {
                            varType.Size = size;
                        }
                    }
                    else if (line.Contains("DW_AT_encoding"))
                    {
                        int index = line.LastIndexOf(':');
                        int enc = 0;
                        string encStr = line.Substring(index + 2, 1); // but not 100% sure if these are all < 16 i..e one digit
                        if (int.TryParse(encStr, out enc))
                        {
                            varType.Encoding = enc;
                            varType.EncodingString = line.Substring(index + 4);

                        }
                        else
                        {
                            MessageBox.Show("error parsing variable types.." + line);
                        }
                    }
                    else if (line.Contains("DW_AT_name"))
                    {
                        int index = line.LastIndexOf(':');
                        varType.Name = line.Substring(index + 1).Trim();
                        //VariableTypes.Add(varType);
                        //varType = null;
                    }
                    else if (line.Contains("DW_AT_type") && varType.BaseType == null)
                    {
                        //< 1f63 > DW_AT_type        : < 0x6c7 >        .... this is base type of an array
                        int index1 = line.LastIndexOf('<');
                        int index2 = line.LastIndexOf('>');
                        UInt16 reference = 0;
                        string refStr = line.Substring(index1 + 3, index2 - index1 - 3);
                        if (ushort.TryParse(refStr, System.Globalization.NumberStyles.HexNumber, null, out reference))
                        {
                            VariableType baseType = VariableTypes.Find(x => x.Reference == reference);
                            varType.BaseType = baseType;
                        }
                    }
                    else if (line.Contains(" DW_AT_upper_bound"))
                    {
                        //< 2 >< 2037 >: Abbrev Number: 30(DW_TAG_subrange_type)
                        // < 2038 > DW_AT_type        : < 0xae0 >
                        //  < 203c > DW_AT_upper_bound : 9              ... this provides the size of the array
                        int index = line.LastIndexOf(':');
                        string sizeStr = line.Substring(index + 1).Trim();
                        ushort size = 0;
                        if (ushort.TryParse(sizeStr, out size))
                        { 
                            varType.Size = size + 1;
                        }
                    }

                }
                if (insideDef && line.Contains("Abbrev Number") && line.Contains("DW_TAG_subrange_type") ==false)
                {
                    // done with previous definition
                    if (varType != null)
                    {
                        VariableTypes.Add(varType);
                    }
                    varType = null;
                    insideDef = false;
                }
                if (line.Contains("DW_TAG_base_type") && insideDef == false)
                {
                    //  <1><6c8>: Abbrev Number: 6 (DW_TAG_base_type)
                    int index1 = line.LastIndexOf('<');
                    int index2 = line.LastIndexOf('>');
                    UInt16 reference = 0;
                    string refStr = line.Substring(index1 + 1, index2 - index1 - 1);
                    if (ushort.TryParse(refStr, System.Globalization.NumberStyles.HexNumber, null, out reference))
                    {
                        varType = new VariableType(reference);
                    }
                    else
                    {
                        MessageBox.Show("error parsing variable types.." + line);
                    }
                }
                if (line.Contains("DW_TAG_array_type") && insideDef == false)
                {
                    // < 1 >< 1f62 >: Abbrev Number: 29(DW_TAG_array_type)
                    //< 1f63 > DW_AT_type        : < 0x6c7 >        .... points to base type
                    //< 1f67 > DW_AT_sibling     : < 0x1f72 >
                         //< 2 >< 2037 >: Abbrev Number: 30(DW_TAG_subrange_type)
                         // < 2038 > DW_AT_type        : < 0xae0 >
                         //  < 203c > DW_AT_upper_bound : 9
                    int index1 = line.LastIndexOf('<');
                    int index2 = line.LastIndexOf('>');
                    UInt16 reference = 0;
                    string refStr = line.Substring(index1 + 1, index2 - index1 - 1);
                    if (ushort.TryParse(refStr, System.Globalization.NumberStyles.HexNumber, null, out reference))
                    {
                        varType = new VariableType(reference);
                        varType.Name = "array";
                    }
                    else
                    {
                        MessageBox.Show("error parsing variable types.." + line);
                    }
                }
                

            }


            // now find the variables themselves. Remove old ones first
            varView.Items.Clear();
            varView.Sorting = SortOrder.None;
            foreach (string line in File.ReadLines(file))
            {
                //// looking for info like this, to find location of our variables ('numf' is a global in this example)
                //< 1 >< 1fa7 >: Abbrev Number: 78(DW_TAG_variable)
                //  < 1fa8 > DW_AT_name        : (indirect string, offset: 0x208): numf
                //  < 1fac > DW_AT_decl_file   : 7
                //  < 1fad > DW_AT_decl_line   : 9
                //  < 1fae > DW_AT_type        : < 0x1fb8 >
                //  < 1fb2 > DW_AT_location    : 5 byte block: 3 0 1 80 0(DW_OP_addr: 800100)
                if (var != null)
                {
                    // found something in the previous line
                    if (line.Contains("DW_AT_name"))
                    {
                        //  could be    "< 1fa8 > DW_AT_name        : (indirect string, offset: 0x208): num1"
                        //  or          "< 1fa8 > DW_AT_name        : num2"
                        int index = line.LastIndexOf(':');
                        String name = line.Substring(index + 1).Trim();
                        // We're only interested in vars that occur in our own files, not library files
                        // Also, only global vars for now.
                        if (MyVariables.Contains(name))
                        {
                            var.Name = name;
                        }
                        else
                        {
                            var = null;  // ignore and get ready for next one
                        }

                    }
                    else if (line.Contains("DW_AT_type"))
                    {
                        int index1 = line.LastIndexOf('<');
                        int index2 = line.LastIndexOf('>');
                        UInt16 typeRef = 0;
                        string refStr = line.Substring(index1 + 3, index2 - index1 - 3);
                        if (ushort.TryParse(refStr, System.Globalization.NumberStyles.HexNumber, null, out typeRef))
                        {
                            var.Type = VariableTypes.Find(x => x.Reference == typeRef);
                        }
                        else
                        {
                            MessageBox.Show("error parsing variables..." + line);
                        }
                    }
                    else if (line.Contains("DW_AT_location"))
                    {
                        if (line.Contains("location list") || line.Contains("DW_OP_reg") || line.Contains("DW_OP_breg") || line.Contains("DW_OP_stack_value"))
                        {
                            // local or register var, not dealing with these yet
                            var = null;  // get ready for next one
                            continue;

                        }
                        int index1 = line.LastIndexOf(':');
                        int index2 = line.LastIndexOf(')');
                        UInt32 loc = 0;
                        string refStr = line.Substring(index1 + 1, index2 - index1 - 1);
                        if (uint.TryParse(refStr, System.Globalization.NumberStyles.HexNumber, null, out loc))
                        {
                            var.Address = (ushort)(loc & 0xFFFF);
                            Variables.Add(var);
                            ListViewItem lvi = new ListViewItem();
                            lvi.Name = var.Name.ToString();
                            lvi.Text = var.Name.ToString();
                            if (var.Type == null)
                            {
                                MessageBox.Show("error parsing variables..." + var.Name + " has no type");
                                var = null;
                                continue;
                            }
                            string typeName = var.Type.Name;
                            if (var.Type.BaseType != null)
                            {
                                // array type etc
                                if (var.Type.Name == "array")
                                {
                                    typeName += "[]";
                                    lvi.Text += "[0]";
                                }
                            }
                            lvi.SubItems.Add(typeName);
                            lvi.SubItems.Add(var.Address.ToString("X"));
                            lvi.SubItems.Add(var.currentValue);
                            varView.Items.Add(lvi);
                            var = null;  // get ready for next one
                        }
                        else
                        {
                            MessageBox.Show("error parsing variables..." + line);
                        }
                    }
                }
                if (line.Contains("DW_TAG_variable"))
                {
                    var = new Variable();
                }
            }

            return true;
        }



        /// <summary>
        /// Using a line in teh dsisassembly 
        /// (e.g. C:\Users\chris\Documents\Arduino\qdebugtest/qdebugtest.ino:48)
        /// find the source lien number at the end
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private int FindSourceFromDisassembly(string line)
        {
            int colon = line.LastIndexOf(':');
            int bracket = line.LastIndexOf("(");
            string strSourceLine;
            if (colon > bracket && bracket > 0)
                return 0;
            if (bracket > 0)
            {
                strSourceLine = line.Substring(colon + 1, bracket - colon - 1);
            }
            else
            {
                strSourceLine = line.Substring(colon + 1);
            }
            int sourceLine = 0;
            if (int.TryParse(strSourceLine, out sourceLine))
            {
                return sourceLine;
            }
            return 0;
        }


        private bool ParseDisassembly(string file)
        {
            int count = 1;
            Breakpoint bp = null;
            int bpCount = 0;
            string sourceLinePending = string.Empty;

            foreach (string line in File.ReadLines(file))
            {
                ListViewItem lvi = new ListViewItem();
                
                bool lineIsAddress = false;

                if (line.Length < 3)
                    continue;

                if (bp != null)
                {
                    // in the middle of breakpoint parse. Find the line containing the debug info (see below)
                    int colon = line.LastIndexOf(':');
                    if (colon >= 0 && colon < 12) // to avoid colons in comments etc
                    {
                        string strPC = line.Substring(0, colon);
                        ushort progCounter;

                        if (ushort.TryParse(strPC, System.Globalization.NumberStyles.HexNumber, null, out progCounter))
                        {
                            //if (++bpCount > 1)// miss out lines before call to qdebug????
                            ++bpCount;
                            {
                                bp.SetDetails(progCounter, line);
                                Breakpoints.Add(bp);

                                // find the line in the source view and mark as appropriate
                                ListView.ListViewItemCollection sourceItems = source.Items;
                                ListViewItem sourceItem = sourceItems[bp.SourceLine - 1];
                                if (sourceItem != null)
                                {
                                    sourceItem.BackColor = System.Drawing.Color.AliceBlue;
                                }
                            }
                            bp = null;
                            lineIsAddress = true;

                        }
                        else
                        {
                            // might happen .....
                            MessageBox.Show("confusing debug line? " + line);
                        };
                    }
                    else
                    {
                    }
                }

                if (line.Contains(ShortFilename))
                {
                    // there should be a debuggable line shortly after, e.g.
                    ////    C: \Users\chris\Documents\Arduino\sketch_feb26a / sketch_feb26a.ino:31
                    ////    (or C: \Users\chris\Documents\Atmel Studio\7.0\MEGA_LED_EXAMPLE1\MEGA_LED_EXAMPLE1\Debug /../ src / mega_led_example.c:82(discriminator 1))
                    ////    float f;
                    ////    for (int index = 0; index < 5; index++)
                    ////         790:	1e 82           std Y+6, r1; 0x06  
                    // In this case, line 31 would be a breakpoint, pointing to address 0x0790
                    // We need to parse these lines (from filename line, as far as the line with the address ':')
                    int sourceLine = FindSourceFromDisassembly(line);
                     
                    if (sourceLine>0)
                    {
                        bp = new Breakpoint(ShortFilename, sourceLine);
                    }
                    // Tag this to next 'addressed' line for later use....
                    sourceLinePending = line;
                    continue;
                }

                if (line[1] == ':' && line[2] == '\\')
                {
                    // name of file and source line number. Tag this to next 'addressed' line for later use....
                    continue;
                }
                if (line.IndexOf("//") == 0)
                {
                    // comment, don't need in disassembly
                    continue;
                }
                if (line.IndexOf("\t//") == 0)
                {
                    // comment, don't need in disassembly
                    continue;
                }
                lvi.Text = (count++).ToString();
                string dissLine = line.Replace("\t", "    ");
                if (sourceLinePending.Length > 0 && lineIsAddress)
                {
                    dissLine += " *** ";
                    dissLine += sourceLinePending;
                    sourceLinePending = string.Empty;

                }
                lvi.SubItems.Add(dissLine);

                disassembly.Items.Add(lvi);


            }
            if (disassembly.Items.Count < 3)
            {
                MessageBox.Show("error with disassembly listing");
                return false;
            }
            return true;
        }
        #endregion

        #region INTERACTION
        private void UpdateCommsBox(string str, bool sending)
        {
            if (str == null)
                return;
            if (_Running != null && _Running.IsBusy)
                return;
            int maxTextLength = 1000; // maximum text length in text box
            if (comms.TextLength > maxTextLength)
                comms.Text = comms.Text.Remove(0, maxTextLength / 2);

            comms.ForeColor = (sending ? System.Drawing.Color.Red : System.Drawing.Color.Black);

            comms.AppendText(str + " ");
        }

        private string ReadLine(int timeout)
        {
            string str = spmanager.ReadLine(timeout);
            if (str.Length > 3)
                UpdateCommsBox(str, false);
            return str;
        }
        private string ReadLine()
        {
            string str = spmanager.ReadLine();

            if (str.Length > 3)
                UpdateCommsBox(str, false);
            return str;
        }
        private void Send(string str)
        {
            if (str.Length > 3)
                UpdateCommsBox(str, true);
            spmanager.Send(str);
        }
        public void Startup(Serial.SerialPortManager _spmanager)
        {
            this.spmanager = _spmanager;
            spmanager.StartListening();  // this will reset the Arduino
            comString = null;
            currentBreakpoint = null;
            nextBreakpoint = null;
            Send("startup\n");
            // might take  a while for a reset etc
            comString = ReadLine(10000);
            GetVariables();
        }
        public void SingleStep()
        {
            String stepStr = "P0000\n";
            bool continuing = true;
            Send(stepStr);
            while (continuing) {
                comString = ReadLine();
                if (comString.Length == 0)
                {
                    MessageBox.Show("timeout in single step");
                    break;
                }
                char firstChar = comString[0];
                if (firstChar == 'P')
                {
                    // moved to where we need; this is our 'step'
                    continuing = false;
                    newProgramCounter();
                    GetVariables();
                }
                else if (firstChar == 'S')
                {
                    string reply = newProgramCounter();
                    Send(reply);

                }
            }

        }

        /// <summary>
        /// need to set a temporary breakpoint, avoiding any function calls
        /// </summary>
        public void StepOver()
        {
            if (nextBreakpoint != null)
            {
                //// set up a 'temporary' breakpoint
                //Breakpoint bp = new Breakpoint("", 0);
                //bp.ProgramCounter = nextBreakpoint;
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
            String sendStr = "P" + bp.ProgramCounter.ToString("X4") + "\n";
            Send(sendStr);
            comString = ReadLine();     // should be 'Txxxx' - echo adrees that was sent.
                                        // now wait for the bp to be hit.....or a pause command
            _Running = new BackgroundWorker();
            _Running.WorkerSupportsCancellation = true;

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
                GetVariables();
                UpdateVariableWindow();
                UpdateCodeWindows(bp.ProgramCounter);
                MarkBreakpointHit(bp);

            });
            _Running.RunWorkerAsync();
        }


        public void GetVariable(Variable var)
        {

        }

        public void GetVariables()
        {
            UInt16 requestedAddr = 0;
            UInt16 data = 0;
            UInt16 datahi = 0;
            UInt32 bigdata = 0;

            String sendStr = "PFFFF\n";  // todo: can get rid of this command to save time & code space
            Send(sendStr);
            System.Threading.Thread.Sleep(100);
            foreach (Variable var in Variables)
            {

                requestedAddr = var.Address;
                sendStr = "A" + requestedAddr.ToString("X4") + "\n";
                Send(sendStr);
                comString = ReadLine();

                if (comString.Length < 5)
                    continue;
                if (ushort.TryParse(comString.Substring(1, 4), System.Globalization.NumberStyles.HexNumber, null, out data))
                {
                    if (var.Type.Size == 1)
                    {
                        // our data has two bytes; we just want the 'lowest' one

                        if (var.Type.Name == "char")
                        {
                            char bite = (char)(data & 0xFF);
                            var.currentValue = "'" + bite + "'";
                        }
                        else
                        {
                            byte bite = (byte)(data & 0xFF);
                            var.currentValue = bite.ToString();
                        }
                    }
                    else
                    {
                        var.currentValue = data.ToString();
                        if (var.Type.Size == 4)
                        {
                            // need to get the next two bytes
                            requestedAddr += 2;
                            sendStr = "A" + requestedAddr.ToString("X4") + "\n";
                            Send(sendStr);
                            comString = ReadLine();
                            if (comString.Length < 5)
                                continue;
                            if (ushort.TryParse(comString.Substring(1, 4), System.Globalization.NumberStyles.HexNumber, null, out datahi))
                            {
                                bigdata = (UInt32)(datahi << 16) + data;
                                var.currentValue = bigdata.ToString();
                                if (var.Type.Name == "float")
                                {
                                    byte[] fbytes = BitConverter.GetBytes(bigdata);
                                    double f = BitConverter.ToSingle(fbytes, 0);

                                    var.currentValue = f.ToString();
                                }
                            }

                        }
                    }
                }
            }
            UpdateVariableWindow();

        }

        public void Variable_Click(object sender, EventArgs e)
        {
            if (varView.SelectedItems.Count == 0)
                return;
            ListViewItem clicked = varView.SelectedItems[0];
            string itemName = clicked.Name;
            Variable var = Variables.Find(x => x.Name == itemName);
            if (var == null)
                return;
            if (var.Type.BaseType == null)
                return;
            //varView.Sorting = SortOrder.None;
            if ((string)clicked.Tag != "expanded")
            {
                // expand item to show contents of this compound variable
                int size = var.Type.Size;
                int index = clicked.Index;
                ushort addr = var.Address;
                for (int i = 0; i < size; i++)
                {
                    string[] items = { "  " + itemName + '[' + i + ']', var.Type.BaseType.Name, addr.ToString("X4"), i.ToString() };
                    ListViewItem arrayItem = new ListViewItem(items);
                    arrayItem.BackColor = System.Drawing.Color.Azure;
                    arrayItem.Tag = "arrayItem";
                    varView.Items.Insert(++index,arrayItem);
                    addr += (ushort)var.Type.BaseType.Size;
                }
                clicked.Tag = "expanded";
            }
            else
            {
                // unexpand the added items
                ListView.ListViewItemCollection items = varView.Items;
                foreach (ListViewItem item  in items)
                {
                    if ((string) item.Tag == "arrayItem")
                    {
                        varView.Items.Remove(item);

                    }
                }
                clicked.Tag = "";
            //    varView.Sorting = SortOrder.Ascending;

            }

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
                // a bit inefficient but will do for now
                //varView.Items.Clear();
                foreach (Variable var in Variables)
                {
                    ListView.ListViewItemCollection vars = varView.Items;
                    ListViewItem[] lvis = vars.Find(var.Name,false);
                    if (lvis == null)
                        continue;
                    ListViewItem lvi = lvis[0];
                   // lvi.Text = var.Name.ToString();

                   // lvi.SubItems.Add(var.Type.Name);
                    lvi.SubItems[2].Text = "0x" + var.Address.ToString("X4");
                    lvi.SubItems[3].Text = var.currentValue;
                    //varView.Items.Add(lvi);
                    
                }
            }
        }
        void UpdateCodeWindows(ushort pc)
        {
            UpdateDisassembly(pc);
            UpdateSource();
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
                if (disassembly != null && disassembly.Visible)
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



        private void Source_Click(object sender, EventArgs e)
        {

            if (source.SelectedItems.Count == 0)
                return;
            ListViewItem lvi = source.SelectedItems[0];
            ListView.ListViewItemCollection sourceItems = source.Items;

            // clear any previous breakpoints
            foreach (Breakpoint bp in Breakpoints)
            {
                ListViewItem bpItem = sourceItems[bp.SourceLine - 1];
               // if (bp.SourceLine == sourceItems.Find + 1)
                {
                    bp.Manual = false;
                    bpItem.BackColor = sourceLineColour;
                }
            }
           
            // set a new breakpoint (only 1 bp allowed for now....)

            if (lvi.BackColor == sourceLineColour) // i.e. breakpoint possible on this line
            {
                // now need to find the correct (single-step) breakpoint and make it manual
                foreach (Breakpoint bp in Breakpoints)
                {
                    if (bp.SourceLine == lvi.Index + 1)
                    {
                        //if (lvi.BackColor == System.Drawing.Color.Red || lvi.BackColor == System.Drawing.Color.Orange)
                        //{
                        //    //bp already set; unset it
                        //    bp.Manual = false;
                        //    lvi.BackColor = System.Drawing.Color.White;
                        //    lvi.SubItems[0].BackColor = System.Drawing.Color.AliceBlue;
                        //}
                        //else
                        //{
                        bp.Manual = true;
                        lvi.Selected = false;
                        lvi.BackColor = breakpointColour;

                        //}
                    }
                }
            }

            //else
            //{
            //    // now need to find the correct (single-step) breakpoint and undo it
            //    foreach (Breakpoint bp in Breakpoints)
            //    {
            //        if (bp.SourceLine == lvi.Index + 1)
            //        {
            //            bp.Manual = false;
            //            lvi.BackColor = System.Drawing.Color.AliceBlue;
            //        }
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
                        UpdateCodeWindows(pc);
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

        #endregion

        
    }

}
