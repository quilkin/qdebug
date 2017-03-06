using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace ArdDebug
{
    class Arduino
    {

        private ListView source, disassembly, varView;
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

        /// <summary>
        /// ascii chars used for interaction strings
        /// (can't use under 128 because normal Serial.print will use them)
        /// </summary>
        //public enum Chars : byte { PROGCOUNT_CHAR = 248, TARGET_CHAR, STEPPING_CHAR, ADDRESS_CHAR, DATA_CHAR, NO_CHAR, YES_CHAR };

        //public class InteractionString
        //{
        //    public int byteCount { get; set; }
        //    public int messageLength { get; set; }
        //    public ushort RecvdNumber { get; set; }

        //    public string Content { get;  set; }
        //    public InteractionString()
        //    {
        //        byteCount = 0;
        //        messageLength = 0;
        //        Content = string.Empty;
        //        RecvdNumber = 0;
        //    }
        //}
        //public InteractionString comString;

        #region FILES

        public bool OpenFiles(ListView source, ListView disassembly, ListView variables)
        {
            this.source = source;
            this.disassembly = disassembly;
            this.varView = variables;
            if (openSourceFile())
            {
                if (OpenDisassembly())
                {

                    return true;
                }

            }
            MessageBox.Show("Problem opening files");
            return false;
        }
        private bool openSourceFile()
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

            startInfo.Arguments = "-S -l -C " + elfPath;

            // disassembly file
            if (doObjDump(startInfo, ".lss") == false)
                return false;
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
                lvi.Text = (count++).ToString();
                lvi.SubItems.Add(line.Replace("\t", "    "));
                disassembly.Items.Add(lvi);

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
                            if (++bpCount > 1)// miss out lines before call to qdebug????
                            {
                                bp.SetDetails(progCounter, line);
                                Breakpoints.Add(bp);

                                // find the line in the source view and mark as appropriate
                                ListView.ListViewItemCollection sourceItems = source.Items;
                                ListViewItem sourceItem = sourceItems[bp.SourceLine - 1];
                                if (sourceItem != null)
                                {
                                    sourceItem.Checked = true;
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
                        // see if there are any variables declared here
                        // looking for something like "  int locali = iii*3;"
                        // or                        "unsigned int ms = 0;"
                        // but not if there's a function definition here
                        // and not an assignment e.g. " ms = 3;"
                        int equals = line.IndexOf('=');
                        int bracket = line.IndexOf('(');
                        if (bracket < 0 && equals > 0)
                        {
                            // should be a variable declaration. Might be local (deal with that later.....)
                            char[] delimiters = new char[] { ' ' };
                            string[] parts = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length < 4)
                                continue;           // not a declaration
                            if (parts[1] == "=")
                                continue;  // not a declaration
                            string varName = null; 
                            if (parts[2]=="=")
                                varName = parts[1];
                            else if (parts[3]=="=")
                                varName = parts[2];
                            if (varName != null)
                                MyVariables.Add(varName);
                        }
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
        public String NewData(ArdDebug.Serial.SerialPortManager spmanager,String str)
        {
            comString += str;
            if (comString.Contains("\n"))
            {
                 // two messages may be concatenated
                string[] split = null;
                if (comString.IndexOf('\n') < comString.Length - 1)
                {
                    split = comString.Split('\n');
                    if (split.Length > 1)
                    {
                        comString = split[0];
                    }
                }
                char firstChar = comString[0];
                if (comString.Length > 4)
                {
                    //if (firstChar == (byte)Arduino.Chars.PROGCOUNT_CHAR) 
                    if (firstChar == 'P')
                    {
                        newProgramCounter();
                    }
                    else if (firstChar == 'S')
                    {
                        string reply = newProgramCounter();
                        spmanager.Send(reply);
                    }
                    else if (firstChar == 'D')
                    {
                        // incoming data for a variable

                    }
                    else if (firstChar == 'T')
                    {
                        // no reply to send, just info

                    }
                }
                if (split != null && split.Length > 1)
                    comString = split[1];
                else
                    comString = string.Empty;
 

            }
            return comString;
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
