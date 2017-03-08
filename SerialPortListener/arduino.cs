using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;

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

        public Breakpoint currentBreakpoint = null;
        public String comString = String.Empty;
        //public UInt16 requestedAddr = 0;
        //public static ManualResetEvent waitingForRX = new ManualResetEvent(false);
        //public static object expectedReply = new Object();
        Serial.SerialPortManager spmanager;

        /// <summary>
        /// ascii chars used for interaction strings
        /// (can't use under 128 because normal Serial.print will use them)
        /// </summary>
        //public enum Chars : byte { PROGCOUNT_CHAR = 248, TARGET_CHAR, STEPPING_CHAR, ADDRESS_CHAR, DATA_CHAR, NO_CHAR, YES_CHAR };

        // varaible types built in to gcc or typedefs defined by Arduino
        const string ReservedTypeWords = "signed char unsigned int float double long volatile";
        const string TypedefWords = "word boolean bool byte uint8_t uint16_t uint32_t uint64_t";


        #region FILES

        public bool OpenFiles(ListView source, ListView disassembly, ListView variables, TextBox comms)
        {
            this.source = source;
            this.disassembly = disassembly;
            this.varView = variables;
            this.comms = comms;
            if (parseSourceFile())
            {

                if (OpenDisassembly())
                {
                    // get ready to accept breakpoints
                    source.Click += Source_Click;
                    //source.SelectedIndexChanged += Source_SelectedIndexChanged;
                    //source.ItemChecked += Source_ItemChecked;
                    return true;
                }

            }
            MessageBox.Show("Problem opening files");
            return false;
        }


        private bool parseSourceFile()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Arduino files|*.ino";
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
            string[] arduinoPaths = Directory.GetDirectories(Path.GetTempPath(), "arduino_build_*");
            string elfPath = null;
            bool found = false;
            foreach (string path in arduinoPaths)
            {
                elfPath = path + "\\" + ShortFilename + ".elf";
                if (File.Exists(elfPath))
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                MessageBox.Show("No compiled files found. You may need to recompile your project");
                return false;
            }
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

            foreach (string line in File.ReadLines(file))
            {
                // first of all need to find a list of variable types used. Info is in this format:
                //  <1><6c8>: Abbrev Number: 6 (DW_TAG_base_type)
                //  <6c9>   DW_AT_byte_size   : 2
                //  <6ca>   DW_AT_encoding    : 5	(signed)
                //  <6cb>   DW_AT_name        : int
                if (varType != null)
                {
                    // found something in the previous line

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
                        string encStr = line.Substring(index+2,1); // but not 100% sure if these are all < 16 i..e one digit
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

                }
                if (line.Contains("DW_TAG_base_type"))
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
                else if (line.Contains("Abbrev Number"))
                {
                    // done with previous definition
                    if (varType != null)
                        VariableTypes.Add(varType);
                    varType = null;
                }
            }


            // now find the variables themselves. Remove old ones first
            varView.Items.Clear();
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
                        if (line.Contains("location list") || line.Contains("DW_OP_reg") || line.Contains("DW_OP_stack_value"))
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
                            lvi.Text = var.Name.ToString();
                            lvi.SubItems.Add(var.Type.Name);
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

        


        private bool ParseDisassembly(string file)
        {
            int count = 1;
            Breakpoint bp = null;
            int bpCount = 0;

            
            foreach (string line in File.ReadLines(file))
            {
                ListViewItem lvi = new ListViewItem();

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
                                    sourceItem.SubItems[0].BackColor = System.Drawing.Color.AliceBlue;
                                }
                            }
                            bp = null;
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
                    ////    float f;
                    ////    for (int index = 0; index < 5; index++)
                    ////         790:	1e 82           std Y+6, r1; 0x06  
                    // In this case, line 31 would be a breakpoint, pointing to address 0x0790
                    // We need to parse these lines (from filename line, as far as the line with the address ':')
                    int colon = line.LastIndexOf(':');
                    string strSourceLine = line.Substring(colon + 1);
                    int sourceLine = 0;
                    if (int.TryParse(strSourceLine, out sourceLine))
                    {
                        bp = new Breakpoint(ShortFilename, sourceLine);
                    }
                }

                if (line[1]==':' && line[2]=='\\')
                {
                    // name of file, don't need in disassembly
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
                lvi.SubItems.Add(line.Replace("\t", "    "));
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
            int maxTextLength = 1000; // maximum text length in text box
            if (comms.TextLength > maxTextLength)
                comms.Text = comms.Text.Remove(0, maxTextLength / 2);

            comms.ForeColor = (sending ? System.Drawing.Color.Red : System.Drawing.Color.Black);

            comms.AppendText(str + " ");
        }

        private string ReadLine(int timeout)
        {
            string str = spmanager.ReadLine(timeout);
            UpdateCommsBox(str,false);
            return str;
        }
        private string ReadLine()
        {
            string str = spmanager.ReadLine();
            UpdateCommsBox(str,false);
            return str;
        }
        private void Send(string str)
        {
            UpdateCommsBox(str,true);
            spmanager.Send(str);
        }
        public void Startup(Serial.SerialPortManager _spmanager)
        {
            this.spmanager = _spmanager;
            spmanager.StartListening();  // this will reset the Arduino
            comString = null;
            currentBreakpoint = null;
            Send("startup");
            // might take  a while for a reset etc
            comString = ReadLine(5000);
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

        public void GoToBreakpoint()
        {
            bool bpFound = false;
            foreach (Breakpoint bp in Breakpoints)
            {
                if (bp.Manual)
                {
                    bpFound = true;
                    String sendStr = "P" + bp.ProgramCounter.ToString("X4") + "\n";
                    Send(sendStr);
                    comString = ReadLine();     // should be 'Txxxx' - echo adrees that was sent.
                    // now wait for the bp to be hit.....
                    comString = ReadLine(System.IO.Ports.SerialPort.InfiniteTimeout);  /// ***need to change to successive short timeouts so we can escape
                    GetVariables();
                    UpdateVariableWindow();
                    MarkBreakpointHit(bp);
                }
            }
            if (!bpFound)
            {
                MessageBox.Show("No breakpoints set!");
                return;
            }


        }
        //public void newVariableData()
        //{
        //    ushort data;
        //    if (comString.Length < 5)
        //        return;
        //    if (ushort.TryParse(comString.Substring(1, 4), System.Globalization.NumberStyles.HexNumber, null, out data))
        //    {
        //        Variable var = Variables.Find(x => x.Address == requestedAddr);
        //        if (var.Type.Name == "char")
        //        {
        //            // our data has two bytes; we just want the 'first' one
        //            char ch = (char)(data & 0xFF);
        //            var.currentValue = "'" + ch + "'";
        //        }
        //        else
        //        {
        //            var.currentValue = data.ToString();
        //        }
        //    }
        //}
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
                                bigdata += (UInt32)(datahi<<16) + data;
                                var.currentValue = bigdata.ToString();
                                if (var.Type.Name == "float")
                                {
                                    byte[] fbytes = BitConverter.GetBytes(bigdata);
                                    double f = BitConverter.ToSingle(fbytes, 0);
                                  

                                    //byte* p1 = (byte*)&bigdata;
                                    //byte* p2 = (byte*)&f;
                                    //// Copy the bits from the integer variable to a float variable
                                    //for (byte i = 0; i < 4; i++)
                                    //    *p2++ = *p1++;
                                    ////float f = (float)bigdata;
                                    var.currentValue = f.ToString();
                                }
                            }

                        }
                    }
                }
            }
            UpdateVariableWindow();

        }
        /// <summary>
        /// use the list of breakpoint-able lines to determine the next place to 'single-step' to
        /// </summary>
        /// <param name="pc">pc at current breakpoint</param>
        /// <returns>next pc to pause execution</returns>
        public ushort FindNextPC(ushort pc)
        {
            // placeholder only for now
            return pc;

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

        void UpdateVariableWindow()
        {
            // a bit inefficient but will do for now
            varView.Items.Clear();
            foreach (Variable var in Variables)
            {
                ListViewItem lvi = new ListViewItem();
                lvi.Text = var.Name.ToString();
                lvi.SubItems.Add(var.Type.Name);
                lvi.SubItems.Add("0x" + var.Address.ToString("X4"));
                lvi.SubItems.Add(var.currentValue);
                varView.Items.Add(lvi);
            }


        }

        void UpdateCodeWindows(ushort pc) {
            int linecount = 0;
            if (disassembly != null && disassembly.Visible)
            {
                // find a line that starts with [whitespace][pc][:]
                ListView.ListViewItemCollection disItems = disassembly.Items;
                string pcStr = pc.ToString("x") + ':';
                linecount = 0;
                foreach (ListViewItem disItem in disItems)
                {
                    ++linecount;
                    string line = disItem.SubItems[1].Text;
                    if (line.Contains(pcStr))
                    {
                        int index = disItem.Index;
                        disassembly.Items[index].Selected = true;
                        disassembly.Select();
                        disassembly.EnsureVisible(index);
                        break;
                    }
                }
            }
            if (source == null)
                return;
            // find the line that contains the current breakpoint
            ListView.ListViewItemCollection sourceItems = source.Items;
            linecount = 0;
            bool lineFound = false;
            foreach (ListViewItem sourceItem in sourceItems)
            {
                ++linecount;
                //string line = sourceItem.SubItems[2].Text;
                if (currentBreakpoint != null &&  currentBreakpoint.SourceLine == linecount)
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



        private void Source_Click(object sender, EventArgs e)
        {

            if (source.SelectedItems.Count == 0)
                return;
            ListView.ListViewItemCollection sourceItems = source.Items;
            ListViewItem lvi = source.SelectedItems[0];

            // set a new breakpoint (only 1 bp allowed for now....)


            if (lvi.SubItems[0].BackColor == System.Drawing.Color.AliceBlue) // i.e. breakpoint possible on this line
            {

                // now need to find the correct (single-step) breakpoint and make it manual
                foreach (Breakpoint bp in Breakpoints)
                {
                    if (bp.SourceLine == lvi.Index + 1)
                    {
                        if (lvi.BackColor == System.Drawing.Color.Red || lvi.BackColor == System.Drawing.Color.Orange)
                        {
                            //bp already set; unset it
                            bp.Manual = false;
                            lvi.BackColor = System.Drawing.Color.White;
                            lvi.SubItems[0].BackColor = System.Drawing.Color.AliceBlue;
                        }
                        else
                        {
                            bp.Manual = true;
                            lvi.Selected = false;
                            lvi.BackColor = System.Drawing.Color.Red;

                        }
                    }

                    else
                    {

                        bp.Manual = false;
                        ListViewItem item = sourceItems[bp.SourceLine - 1];
                        item.BackColor = System.Drawing.Color.White;
                        item.SubItems[0].BackColor = System.Drawing.Color.AliceBlue;
                    }
                }
            }

        }

        private void MarkBreakpointHit(Breakpoint bp)
        {
            ListView.ListViewItemCollection sourceItems = source.Items;
            ListViewItem item = sourceItems[bp.SourceLine - 1];
            item.BackColor = System.Drawing.Color.Orange;
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

                    //nextpc = FindNextPC(pc);
                    //string newString = nextpc.ToString("X");
                    //return newString;
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
