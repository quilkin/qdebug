using System;
using System.Windows.Forms;
using System.IO;


namespace ArdDebug
{
    partial class Arduino
    {

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
                int SerialLine = 0;
                bool qdebugHeaderFound = false;
                bool qdebugConstrFound = false;
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

                    //if (line.Contains("QDebug"))
                    //    qdebugConstrFound = true;

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
                    int index = line.IndexOf("Serial.");
                    int comment = line.IndexOf("//");
                    if (index >= 0)
                    {
                        if ( comment < 0 || comment > index)
                        {
                            lvi.BackColor = System.Drawing.Color.Yellow;
                            SerialLine = lvi.Index;
                        }
                    }
                    string line2 = line;
                    if (line2.Length < 3)
                        continue;
                    if (line2.IndexOf('(') >= 0)
                        continue;  // function call or definition, not a variable

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
                    {
                        if (line.Contains("qdebug.h"))
                            qdebugHeaderFound = true;
                        continue;
                    }
                    if (parts[0].StartsWith("//"))
                        continue;

                    if (line.Contains("QDebug"))
                        qdebugConstrFound = true;

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
                if (SerialLine > 0)
                {
                    source.EnsureVisible(SerialLine);
                    MessageBox.Show("Sorry, cannot have 'Serial' commands, these are used by the debugger.\n Please comment out or use 'SoftwareSerial', then reload file");
                    return false;
                }
                if ( qdebugHeaderFound == false && ShortFilename.EndsWith(".ino"))
                {
                    MessageBox.Show("You must #include \"qdebug.h\" at the top of your file");
                    return false;
                }
                if (qdebugConstrFound == false && ShortFilename.EndsWith(".ino"))
                {
                    MessageBox.Show("You must create a 'QDebug' object as the first line of 'Setup()'");
                    return false;
                }

                return true;
            }
            return false;
        }

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

            // find Base types first
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



                }
                if (insideDef && line.Contains("Abbrev Number") && line.Contains("DW_TAG_subrange_type") == false)
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



            }
            // find Array types next
            insideDef = false;
            foreach (string line in File.ReadLines(file))
            {

                if (varType != null)
                {
                    // found something in the previous line
                    insideDef = true;

                    if (line.Contains(" DW_AT_upper_bound"))
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
                }

                if (insideDef && line.Contains("Abbrev Number") && line.Contains("DW_TAG_subrange_type") == false)
                {
                    // done with previous definition
                    if (varType != null)
                    {
                        VariableTypes.Add(varType);
                    }
                    varType = null;
                    insideDef = false;
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
                                    //lvi.Text += "[0]";
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
                    var = new Variable(this);
                }
            }

            return true;
        }



        /// <summary>
        /// Using a line in the disassembly (e.g. C:\Users\chris\Documents\Arduino\qdebugtest/qdebugtest.ino:48)
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
                                if (bp.SourceLine < sourceItems.Count)
                                {
                                    ListViewItem sourceItem = sourceItems[bp.SourceLine - 1];
                                    if (sourceItem != null)
                                    {
                                        sourceItem.BackColor = System.Drawing.Color.AliceBlue;
                                    }
                                }
                            }
                            bp = null;
                            lineIsAddress = true;

                        }
                        else
                        {
                            // might happen ..... lables and public: etc
                            // MessageBox.Show("confusing debug line? " + line);
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

                    if (sourceLine > 0)
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



    }
}