using System;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;

namespace ArdDebug
{
    public enum Tags : byte {
        none,                   compile_unit,
        base_type,              array_type,                 const_type,
        volatile_type,          structure_type,             pointer_type,
        subrange_type,          subroutine_type,            variable,
        typedef,                unspecified_parameters,     subprogram,
        member,                 formal_parameter,           inheritance,
        enumerator,             union_type,                 enumeration_type,
        reference_type,         inlined_subroutine,         GNU_call_site,
        GNU_call_site_parameter,lexical_block
    };
    public enum Attributes : byte
    {
        name,                   decl_file,                  decl_line,
        low_pc,                 high_pc,                    type,
        location,               call_file,                  call_line,
        sibling,                artificial,                 abstract_origin,
        frame_base,             object_pointer,             MIPS_linkage_name,
        specification,          stmt_list,                  GNU_call_site_value,
        GNU_all_call_sites,     GNU_tail_call,
        byte_size,
        encoding,
        external,
        producer,
        language,
        containing_type,
        data_member_location,
        accessibility,
        upper_bound,
        declaration,
        const_value,
        vtable_elem_location,
        virtuality,
        inline,
        entry_pc,
        ranges

    }
    class DebugItem
    {
        public class Tag
        {
            public Tags code;
            public String def;
            public Tag (Tags t, string s)
            {
                code = t;
                def = s;
            }
        }
        public class Attribute
        {
            public Attributes code;
            public String def;
            public Attribute(Attributes a, string s)
            {
                code = a;
                def = s;
            }
        }
        public static readonly List<Attribute> AttributeList = new List<Attribute>
        {
            {new Attribute(Attributes.name,"DW_AT_name")},
            {new Attribute(Attributes.decl_file,"DW_AT_decl_file")},
            {new Attribute(Attributes.decl_line,"DW_AT_decl_line")},
            {new Attribute(Attributes.low_pc,"DW_AT_low_pc")},
            {new Attribute(Attributes.high_pc,"DW_AT_high_pc")},
            {new Attribute(Attributes.type,"DW_AT_type")},
            {new Attribute(Attributes.location,"DW_AT_location")},
            {new Attribute(Attributes.call_file,"DW_AT_call_file")},
            {new Attribute(Attributes.call_line,"DW_AT_call_line")},
            {new Attribute(Attributes.sibling,"DW_AT_sibling")},
            {new Attribute(Attributes.artificial,"DW_AT_artificial")},
            {new Attribute(Attributes.abstract_origin,"DW_AT_abstract_origin")},
            {new Attribute(Attributes.frame_base,"DW_AT_frame_base")},
            {new Attribute(Attributes.object_pointer,"DW_AT_object_pointer")},
            {new Attribute(Attributes.MIPS_linkage_name,"DW_AT_MIPS_linkage_name")},
            {new Attribute(Attributes.specification,"DW_AT_specification")},
            {new Attribute(Attributes.stmt_list,"DW_AT_stmt_list")},
            {new Attribute(Attributes.GNU_call_site_value,"DW_AT_GNU_call_site_value")},
            {new Attribute(Attributes.GNU_all_call_sites,"DW_AT_GNU_all_call_sites")},
            {new Attribute(Attributes.GNU_tail_call,"DW_AT_GNU_tail_call")},
            {new Attribute(Attributes.byte_size,"DW_AT_byte_size")},
            {new Attribute(Attributes.encoding,"DW_AT_encoding")},
            {new Attribute(Attributes.external,"DW_AT_external")},
            {new Attribute(Attributes.producer,"DW_AT_producer")},
            {new Attribute(Attributes.language,"DW_AT_language")},
            {new Attribute(Attributes.containing_type,"DW_AT_containing_type")},
            {new Attribute(Attributes.data_member_location,"DW_AT_data_member_location")},
            {new Attribute(Attributes.accessibility,"DW_AT_accessibility")},
            {new Attribute(Attributes.upper_bound,"DW_AT_upper_bound")},
            {new Attribute(Attributes.declaration,"DW_AT_declaration")},
            {new Attribute(Attributes.const_value,"DW_AT_const_value")},
            {new Attribute(Attributes.vtable_elem_location,"DW_AT_vtable_elem_location")},
            {new Attribute(Attributes.virtuality,"DW_AT_virtuality")},
            {new Attribute(Attributes.inline,"DW_AT_inline")},
            {new Attribute(Attributes.entry_pc,"DW_AT_entry_pc")},
            {new Attribute(Attributes.ranges,"DW_AT_ranges")},
        };

        public static readonly List<Tag> TagList = new List<Tag>
        {
            {new Tag(Tags.array_type, "DW_TAG_array_type")},
            {new Tag(Tags.base_type, "DW_TAG_base_type")},
            {new Tag(Tags.variable, "DW_TAG_variable")},
            {new Tag(Tags.typedef, "DW_TAG_typedef")},
            {new Tag(Tags.subprogram, "DW_TAG_subprogram")},
            {new Tag(Tags.structure_type, "DW_TAG_structure_type")},
            {new Tag(Tags.member, "DW_TAG_member")},
            {new Tag(Tags.compile_unit,"DW_TAG_compile_unit")},
            {new Tag(Tags.const_type,"DW_TAG_const_type")},
            {new Tag(Tags.volatile_type,"DW_TAG_volatile_type")},
            {new Tag(Tags.pointer_type,"DW_TAG_pointer_type")},
            {new Tag(Tags.subrange_type,"DW_TAG_subrange_type")},
            {new Tag(Tags.subroutine_type,"DW_TAG_subroutine_type")},
            {new Tag(Tags.typedef,"DW_TAG_typedef")},
            {new Tag(Tags.unspecified_parameters,"DW_TAG_unspecified_parameters")},
            {new Tag(Tags.formal_parameter,"DW_TAG_formal_parameter")},
            {new Tag(Tags.inheritance,"DW_TAG_inheritance")},
            {new Tag(Tags.enumerator,"DW_TAG_enumerator")},
            {new Tag(Tags.union_type,"DW_TAG_union_type")},
            {new Tag(Tags.enumeration_type,"DW_TAG_enumeration_type")},
            {new Tag(Tags.reference_type,"DW_TAG_reference_type")},
            {new Tag(Tags.inlined_subroutine,"DW_TAG_inlined_subroutine")},
            {new Tag(Tags.GNU_call_site,"DW_TAG_GNU_call_site")},
            {new Tag(Tags.GNU_call_site_parameter,"DW_TAG_GNU_call_site_parameter")},
            {new Tag(Tags.lexical_block,"DW_TAG_lexical_block")},

        };

        
    //<1><14>: Abbrev Number: 2 (DW_TAG_base_type)
    //   <15>   DW_AT_name        : (indirect string, offset: 0xf): uint8_t
    //   <19> DW_AT_byte_size   : 1
    //   <1a>   DW_AT_encoding    : 8	(unsigned char)
        public int Level { get; set; }      // <1>
        public ushort Ref { get; set; }     // <1a>

        // will have either a tag or an attribute, not both.
        public Tags tag { get; set; }       // e.g. DW_TAG_formal_parameter
        public Attributes attr { get; set; }  // e.g. DW_AT_decl_file

        public String content { get; set; } // only for attributes, e.g. "3 byte block: 92 20 2 	(DW_OP_bregx: 32 (r32) 2)"

    }

    partial class Arduino
    {
        List<string> ParseErrors = new List<string>();
        List<DebugItem> DebugItems = new List<DebugItem>();

        private void LookForInitialisedVariables(string line)
        {
            // see which (simple) variables are initialised here
            // Need to know so we can scheck for no-allowed use of i/o pin 7
            // looking for something like "const int buttonPin = 6; "

            string line2 = line;
            if (line2.Length < 3)
                return;
            if (line2.IndexOf('(') >= 0)
                return;  // function call or definition, not a variable

            char[] delimiters = new char[] { ' ',';' };
            string[] parts = line2.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 1)
                return;
            if (parts[0].StartsWith("#"))
                return;
            if (parts[0].StartsWith("//"))
                return;


            if (ReservedTypeWords.Contains(parts[0]) || TypedefWords.Contains(parts[0]))
            {
                // should be a variable declaration. 
                // find first word that is NOT in the reserved lists
                 for (int p = 1; p < parts.Length; p++)
                {
                    string part = parts[p];
                    if (ReservedTypeWords.Contains(part) || TypedefWords.Contains(part))
                    {
                        continue;
                    }
                    Variable var = new Variable(this);
                    var.Name = part;
                    if (p+2 < parts.Length)
                    {
                        int val;
                        string valString = parts[p + 2];
                        if (int.TryParse(valString, out val))
                        {
                            var.currentValue = valString;
                            // do't need to save type in this case
                            MyVariables.Add(var);
                        }
                    }
                    break;
                }

            }
        }

        private bool parseSourceFile()
        {

            source.Items.Clear();
            disassembly.Items.Clear();
            varView.Items.Clear();
            Variables.Clear();
            MyVariables.Clear();
            Functions.Clear();
            int count = 1;
            int SerialLine = 0;
            bool qdebugConstrFound = false;
            foreach (var line in System.IO.File.ReadLines(FullFilename))
            {

                // tabs ('\t') seem ignored in listview so replace with spaces
                string untabbed = line.Replace("\t", "    ");
                string trimmed = line.Trim();

                LookForInitialisedVariables(line);

                ListViewItem lvi = new ListViewItem();
                lvi.Text = ""; // for breakpoint markers

                lvi.SubItems.Add((count++).ToString());
                lvi.SubItems.Add(untabbed);
                source.Items.Add(lvi);

                if (line.Contains("QDebug"))
                    qdebugConstrFound = true;

                if (line.Contains("pinMode"))
                {
                    // check to see if pin 7 is being used. Can't allow this.
                    char[] delimiters = new char[] { ' ', '(' , ')', ','};
                    string[] parts = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 2)
                    {
                        string pinDefString = parts[1];
                        Variable var = MyVariables.Find(x => x.Name == pinDefString);
                        if (var != null)
                        {
                            int val;
                            if (int.TryParse(var.currentValue, out val))
                            {
                                if (val == 7)
                                {
                                    MessageBox.Show("Use of Pin 7 has been found.  Sorry, pin 7 cannot be used with Qdebug. Please change pin being used, or ignore this warning (and send sketch to our support) if Qdebug has mistaken the pin. ", "Warning!");
                                }
                            }
                        }
                    }
                }
                int index = line.IndexOf("Serial.");
                int comment = line.IndexOf("//");
                if (index >= 0)
                {
                    if (comment < 0 || comment > index)
                    {
                        lvi.BackColor = System.Drawing.Color.Yellow;
                        SerialLine = lvi.Index;
                    }
                }
            }
            if (SerialLine > 0)
            {
                source.EnsureVisible(SerialLine);
                MessageBox.Show("Sorry, cannot have 'Serial' commands, these are used by the debugger.\n Please comment out or use 'SoftwareSerial', then reload file");
                return false;
            }

            if (qdebugConstrFound == false && ShortFilename.EndsWith(".ino"))
            {
                MessageBox.Show("You must create a 'QDebug' object as the first line of 'Setup()'");
                return false;
            }

            return true;

        }

        private bool ParseBaseTypes(string file)
        {
            VariableType varType = null;

            bool insideDef = false;
            foreach (string line in File.ReadLines(file))
            {
                if (line.Contains(".debug_line"))
                {
                    // end of the bit we are interested in
                    break;
                }
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
                            ParseErrors.Add("error parsing variable types.." + line);
                        }
                    }
                    else if (line.Contains("DW_AT_name"))
                    {
                        int index = line.LastIndexOf(':');
                        varType.Name = line.Substring(index + 1).Trim();
                        if (varType.Name == "__unknown__")
                        {
                            // bug in linker??
                            varType.Name = "unsigned long";
                            if (varType.Size == 8)
                            {
                                varType.Name += " long";
                            }
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
                if (line.Contains("DW_TAG_base_type") && insideDef == false)
                {
                    //  <1><6c8>: Abbrev Number: 6 (DW_TAG_base_type)
                    int index1 = line.LastIndexOf('<');
                    int index2 = line.LastIndexOf('>');
                    if (index1 < 0 || index2 < 0)
                        continue;
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
            return true;
        }

        bool ParseTypeDefs(string file)
        {
            // find typedefs (and volatiles) next

            //  for now, we will effectively promote a typedef type to it's base type

            //< 1 >< 6e0 >: Abbrev Number: 7(DW_TAG_typedef)
            //       < 6e1 > DW_AT_name        : (indirect string, offset: 0x37a): uint8_t
            //       < 6e5 > DW_AT_decl_file   : 14
            //       < 6e6 > DW_AT_decl_line   : 126
            //       < 6e7 > DW_AT_type        : < 0x6eb >

            //    < 1 >< 6eb >: Abbrev Number: 2(DW_TAG_base_type)
            //           < 6ec > DW_AT_byte_size   : 1
            //           < 6ed > DW_AT_encoding    : 8(unsigned char)
            //           < 6ee > DW_AT_name        : (indirect string, offset: 0x480): unsigned char
            bool insideDef = false;
            bool volatileVar = false;
            VariableType varType = null;

            foreach (string line in File.ReadLines(file))
            {
                if (line.Contains(".debug_line"))
                {
                    // end of the bit we are interested in
                    break;
                }
                if (varType != null)
                {
                    // found something in the previous line
                    insideDef = true;
                    if (line.Contains("DW_AT_name"))
                    {
                        int index = line.LastIndexOf(':');
                        varType.Name = line.Substring(index + 1).Trim();
                    }
                    else if (line.Contains("DW_AT_type") /* && varType.BaseType == null */)
                    {
                        int index1 = line.LastIndexOf('<');
                        int index2 = line.LastIndexOf('>');
                        UInt16 reference = 0;
                        string refStr = line.Substring(index1 + 3, index2 - index1 - 3);
                        if (ushort.TryParse(refStr, System.Globalization.NumberStyles.HexNumber, null, out reference))
                        {
                            VariableType baseType = VariableTypes.Find(x => x.Reference == reference);
                            if (baseType != null)
                            {
                                //varType.BaseType = baseType;
                                varType.Encoding = baseType.Encoding;
                                varType.Size = baseType.Size;
                                if (volatileVar)
                                    varType.Name = baseType.Name;
                            }
                            else
                            {
                            }
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
                if ((line.Contains("DW_TAG_typedef") || line.Contains("DW_TAG_volatile_type")) && insideDef == false)
                {
                    volatileVar = line.Contains("DW_TAG_volatile_type");

                    //    < 1 >< 6e0 >: Abbrev Number: 7(DW_TAG_typedef)
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
            return true;

        }

        private bool ParseArrayTypes(string file)
        {
            // find Array types, structs and pointers next
            bool insideDef = false;
            bool pointerVar = false;
            bool structVar = false;
            VariableType varType = null;

            foreach (string line in File.ReadLines(file))
            {
                if (line.Contains(".debug_line"))
                {
                    // end of the bit we are interested in
                    break;
                }
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
                    if (line.Contains("DW_AT_byte_size"))
                    {
                        int index = line.LastIndexOf(':');
                        int size = 0;
                        if (int.TryParse(line.Substring(index + 1), out size))
                        {
                            varType.Size = size;
                        }
                    }
                    else if (line.Contains("DW_AT_type") && varType.BaseType == null)
                    {
                        //< 1f63 > DW_AT_type        : < 0x6c7 >        .... this is base type of an array or pointer
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
                if ((line.Contains("DW_TAG_array_type") || line.Contains("DW_TAG_pointer_type") || line.Contains("DW_TAG_structure_type")) && insideDef == false)
                {
                    // < 1 >< 1f62 >: Abbrev Number: 29(DW_TAG_array_type)
                    //< 1f63 > DW_AT_type        : < 0x6c7 >        .... points to base type
                    //< 1f67 > DW_AT_sibling     : < 0x1f72 >
                    //< 2 >< 2037 >: Abbrev Number: 30(DW_TAG_subrange_type)
                    // < 2038 > DW_AT_type        : < 0xae0 >
                    //  < 203c > DW_AT_upper_bound : 9

                    pointerVar = line.Contains("DW_TAG_pointer_type");
                    structVar = line.Contains("DW_TAG_structure_type");
                    int index1 = line.LastIndexOf('<');
                    int index2 = line.LastIndexOf('>');
                    UInt16 reference = 0;
                    string refStr = line.Substring(index1 + 1, index2 - index1 - 1);
                    if (ushort.TryParse(refStr, System.Globalization.NumberStyles.HexNumber, null, out reference))
                    {
                        varType = new VariableType(reference);
                        varType.Name = "array";
                        if (pointerVar)
                            varType.Name = "pointer";
                        if (structVar)
                            varType.Name = "struct";
                    }

                    else
                    {
                        MessageBox.Show("error parsing variable types.." + line);
                    }
                }
            }
            return true;

        }
        private bool ParseDebugInfo(string file)
        {
            Variable var = null;
            Function func = null;
            bool inLocationSection = false;

            // firstly, convert the file into a list of debug items
            foreach (string line in File.ReadLines(file))
            {
                if (line.Contains(".debug_line"))
                {
                    // end of the bit we are interested in
                    break;
                }
                char[] delimiters = new char[] { ' ', '<', '>', ':' };
                string[] parts = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                    continue;
                DebugItem item = new DebugItem();
                UInt16 number = 0;
                if (UInt16.TryParse(parts[0], out number))
                {
                    //if (number < 10)
                    if (line.Contains("Abbrev Number"))
                    {
                        // e.g. < 1 >< 22 >: Abbrev Number: 4 (DW_TAG_array_type)
                        item.Level = number;
                        if (UInt16.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, null, out number))
                        {
                            item.Ref = number;
                            if (parts.Length > 6)
                            {
                                string tagstr = parts[6];
                                DebugItem.Tag t = DebugItem.TagList.Find(x => x.def == tagstr);
                                if (t == null)
                                {
                                    string err = "missing tag definition: " + tagstr;
                                    if (ParseErrors.Contains(err) == false)
                                    {
                                        ParseErrors.Add(err);
                                    }
                                    continue;
                                }
                                item.tag = t.code;
                            }
                            else
                            {
                                item.tag = Tags.none;
                            }
                            DebugItems.Add(item);
                        }
                        else
                        {
                            ParseErrors.Add("Unkown level indicator: " + line);
                            continue;
                        }
                    }
                    else
                    {
                        // an attribute line, e.g.     <5e32>   DW_AT_high_pc     : 0x1b82
                        if (UInt16.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, null, out number))
                        {
                            item.Ref = number;
                            string attrstr = parts[1];
                            DebugItem.Attribute a = DebugItem.AttributeList.Find(x => x.def == attrstr);
                            if (a == null)
                            {
                                string err = "missing attribute definition: " + attrstr;
                                if (ParseErrors.Contains(err) == false)
                                {
                                    ParseErrors.Add(err);
                                }
                                continue;
                            }
                            item.attr = a.code;
                            // rejoin the parts for the rest of the content
                            string content = "";
                            for (int p = 2; p < parts.Length; p++)
                            {
                                content += parts[p] + " ";
                            }
                            item.content = content;
                            DebugItems.Add(item);
                        }
                        else
                        {
                            ParseErrors.Add("Unkown reference indicator: " + line);
                            continue;
                        }
                    }
                }
            }
            SourceFileRef = 0;

            // info from http://www.dwarfstd.org/doc/Debugging%20using%20DWARF-2012.pdf

            // first of all need to find a list of variable types used. Info is in this format:
            //  <1><6c8>: Abbrev Number: 6 (DW_TAG_base_type)
            //  <6c9>   DW_AT_byte_size   : 2
            //  <6ca>   DW_AT_encoding    : 5	(signed)
            //  <6cb>   DW_AT_name        : int
            VariableTypes.Clear();

            // first find the entry in the File Name Table so we can se which vars are in our source file
            int linecount = 0;
            foreach (string line in File.ReadLines(file))
            {

                if (line.Contains(ShortFilename))
                {
                    if (line.Contains(ShortFilename + ".elf") == false && line.Contains(ShortFilename + ".cpp") == false) // i.e. not the header line etc
                    {
                        if (line.Contains("DW_AT_name") == false)
                        {
                            // 8 3   0   0   qdebugtest.ino
                            char[] delimiters = new char[] { ' ', '\t' };
                            string[] parts = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                            int fileRef = 0;
                            if (int.TryParse(parts[0], out fileRef)) {
                                SourceFileRef = fileRef;
                            }
                            break;
                        }
                    }
                }
                ++linecount;
            }
            if (SourceFileRef== 0)
            {
                MessageBox.Show("error parsing debug file, no source file abbr found");
                return false;
            }
            if (ParseBaseTypes(file) == false)
                return false; 
            if (ParseTypeDefs(file) == false)
                return false;
            if (ParseArrayTypes(file) == false)
                return false;

            
            // now find the variables themselves. Remove old ones first.
            // Also find our own functions, so we can get local variables inside them
            //  and libray functions, so we can step over them
            varView.Items.Clear();
            varView.Sorting = SortOrder.None;
            
            foreach (string line in File.ReadLines(file))
            {
                if (inLocationSection & line.Length > 10)
                {
                    var = ParseLocations(line, var);
                    continue;
                }
                //if (line.Contains(".debug_line"))
                //{
                //    // end of the bit we are interested in
                //    break;
                //}
                //// looking for info like this, to find location of our variables ('numf' is a global in this example)
                //< 1 >< 1fa7 >: Abbrev Number: 78(DW_TAG_variable)
                //  < 1fa8 > DW_AT_name        : (indirect string, offset: 0x208): numf
                //  < 1fac > DW_AT_decl_file   : 7
                //  < 1fad > DW_AT_decl_line   : 9
                //  < 1fae > DW_AT_type        : < 0x1fb8 >
                //  < 1fb2 > DW_AT_location    : 5 byte block: 3 0 1 80 0(DW_OP_addr: 800100)
                if (var != null || func != null)
                {
                    // found something in the previous line
                    if (line.Contains("DW_AT_name"))
                    {
                        //  could be    "< 1fa8 > DW_AT_name        : (indirect string, offset: 0x208): num1"
                        //  or          "< 1fa8 > DW_AT_name        : num2"
                        int index = line.LastIndexOf(':');
                        String name = line.Substring(index + 1).Trim();
                        if (func != null && func.Name != null && var != null)
                        {
                            // this is a local var in a func, must modify its name
                            var.Name = func.Name + "." + name;
                        }
                        else if (func != null)
                        {
                            func.Name = name;
                        }
                        // We're only interested in vars that occur in our own files, not library files
                        else
                        {
                            var.Name = name;
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
                            VariableType vType = VariableTypes.Find(x => x.Reference == typeRef);
                            if (var != null)
                                var.Type = vType;
                            else if (func != null)
                                func.Type = vType;
                        }
                        else
                        {
                            MessageBox.Show("error parsing variables..(Type)..." + line);
                        }
                    }
                    else if (line.Contains("DW_AT_decl_file"))
                    {
                        int index = line.LastIndexOf(':');
                        UInt16 fileRef = 0;
                        string refStr = line.Substring(index + 1);
                        if (ushort.TryParse(refStr, out fileRef))
                        {
                            if (var != null)
                            {
                                if (fileRef == SourceFileRef)
                                {
                                    // this variable is part of our source file
                                }
                                else
                                {
                                    var = null;  // ignore and get ready for next one
                                }
                            }
                            else if (func != null)
                            {
                                if (fileRef == SourceFileRef)
                                {
                                    // this variable is part of our source file
                                    func.fileRef = fileRef;
                                }
                                else
                                {
                                    //func= null;  // ignore and get ready for next one
                                }

                            }

                        }
                        else
                        {
                            MessageBox.Show("error parsing variables...(file).." + line);
                        }
                    }
                    //else if (line.Contains("DW_AT_artificial") && func != null)
                    //{
                    //    func = null;  // ignore and get ready for next one
                    //    var = null;
                    //}
                    else if (line.Contains("DW_AT_abstract_origin") && var != null)
                    {
                        var = null;  // ignore and get ready for next one
                        continue;
                    }
                    else if (line.Contains("Abbrev Number: 0"))
                    {
                        // end of section...
                        var = null;
                        if (func != null && func.Name != null && func.Name.Length > 0 && func.Name.StartsWith("_")==false)
                        {
                            Functions.Add(func);
                        }
                        func = null;
                        continue;
                    }
                    else if (line.Contains("DW_AT_location") && var != null)
                    {
                        if (line.Contains("DW_OP_stack_value"))
                            continue;
                        int index1, index2;
                        string refStr;
                        // local or register var?
                        if (func == null || func.Name == null)
                        {
                            index1 = line.LastIndexOf(':');
                            index2 = line.LastIndexOf(')');
                            if (index1 < 0 || index2 < 0)
                                continue;
                            UInt32 loc = 0;
                            refStr = line.Substring(index1 + 1, index2 - index1 - 1);
                            if (uint.TryParse(refStr, System.Globalization.NumberStyles.HexNumber, null, out loc))
                            {
                                var.Address = (ushort)(loc & 0xFFFF);
                                Variables.Add(var);
                                ListViewItem lvi = var.CreateVarViewItem();
                                if (lvi != null)
                                    varView.Items.Add(lvi);
                                var = null;  // get ready for next one
                            }
                            else
                            {
                                MessageBox.Show("error parsing variables..(location)..." + line);
                            }
                            continue;
                        }


                        if (line.Contains("DW_OP_reg") || line.Contains("DW_OP_breg") || line.Contains("DW_OP_fbreg"))
                        {
                            // e.g.   <2474>   DW_AT_location    : 6 byte block: 64 93 1 65 93 1 	(DW_OP_reg20 (r20); DW_OP_piece: 1; DW_OP_reg21 (r21); DW_OP_piece: 1)
                            int bracket = line.IndexOf('(');
                            if (bracket < 0)
                            {
                                MessageBox.Show("error parsing variables...(location).." + line);
                            }
                            LocationItem item = new LocationItem();
                            // any address valid
                            item.StartAddr = 0x00000000;
                            item.EndAddr = 0xFFFFFFFF;
                            ParseRegisterString(var, item, line.Substring(bracket));
                            var.Function = func;
                            Variables.Add(var);
                            // associate with function so we can update locals only when stepping through function
                            func.LocalVars.Add(var);
                            varView.Items.Add(var.CreateVarViewItem());
                            var = null;
                            continue;
                        }


                        if (line.Contains("location list"))
                        {
                       
                            // local or register var
                            if (func == null || func.Name == null)
                            {
                                // not in a  functon so not interested
                                var = null;  // get ready for next one
                                continue;
                            }
                            else if (line.Contains("location list"))
                            {
                                // This is in a function. First need to mofify the param name with the function name
                                // var.Name = func.Name + "." + var.Name;
                                // start to find location of local variable or formal parameter
                                //   < 14f7 > DW_AT_location    : 0x656(location list)
                                index1 = line.LastIndexOf(':');
                                index2 = line.LastIndexOf('(');
                                if (index1 < 0 || index2 < 0)
                                    continue;
                                UInt16 location = 0;
                                refStr = line.Substring(index1 + 4, index2 - index1 - 4);
                                if (UInt16.TryParse(refStr, System.Globalization.NumberStyles.HexNumber, null, out location))
                                {
                                    var.Location = location;
                                    var.Function = func;
                                    Variables.Add(var);
                                    // associate with function so we can update locals only when stepping through function
                                    func.LocalVars.Add(var);
                                    ListViewItem item = var.CreateVarViewItem();
                                    if (item != null)
                                        varView.Items.Add(var.CreateVarViewItem());
                                    var = null;
                                    continue;
                                }
                                else
                                {
                                    MessageBox.Show("error parsing variables..." + var.Name + " has no location");
                                    var = null;
                                    continue;
                                }

                            }
                        }
                        
                    }
                    else if (line.Contains("DW_AT_low_pc") && func != null)
                    {
                        int colon = line.LastIndexOf(": 0x");
                        UInt16 loc = 0;
                        string refStr = line.Substring(colon+4);
                        if (UInt16.TryParse(refStr, System.Globalization.NumberStyles.HexNumber, null, out loc))
                            func.LowPC = loc;
                    }
                    else if (line.Contains("DW_AT_high_pc") && func != null)
                    {
                        int colon = line.LastIndexOf(": 0x");
                        UInt16 loc = 0;
                        string refStr = line.Substring(colon + 4);
                        if (UInt16.TryParse(refStr, System.Globalization.NumberStyles.HexNumber, null, out loc))
                            func.HighPC = loc;
                    }
                    else if (line.Contains("DW_TAG_subprogram") || line.Contains("DW_TAG_GNU_call_site") || line.Contains("DW_TAG_inlined_subroutine"))
                    {
                        if (func != null) { 
                            // done with this func, onto the next.
                            if (func.Name != null && func.Name.Length > 0)
                            {
                                Functions.Add(func);
                            }
                            func = null;
                        }
                        if (var != null)
                        {
                            var = null;
                        }
                    }
                    else if (line.Contains("DW_AT_specification") )
                    {
                        // used for structs - not dealing with this yet
                        func = null;
                        continue;
                    }
                }
                if (line.Contains("DW_TAG_variable"))
                {
                    var = new Variable(this);
                }
                else if (line.Contains("DW_TAG_formal_parameter"))
                {
                    if (func == null)
                        continue;
                    if (func.Name == null)
                        continue;
                    //if (func.IsMine == false)
                    //    continue;
                    var = new Variable(this);
                }
                else if (line.Contains("DW_TAG_subprogram"))
                {
                    func = new Function(this);
                }
                else if (line.Contains(".debug_loc"))
                {
                    inLocationSection = true;
                    var = null;
                }
            }

            return true;
        }

        void AddToVarView(Variable var)
        {
            ListViewItem lvi = new ListViewItem();
            lvi.Name = var.Name.ToString();
            lvi.Text = var.Name.ToString();
            if (var.Type == null)
            {
                MessageBox.Show("error parsing variables..." + var.Name + " has no type");
                var = null;
                return;
            }
            string typeName = var.Type.Name;
            if (var.Type.BaseType != null)
            {
                // array type etc
                if (var.Type.Name == "array")
                {
                    typeName = var.Type.BaseType.Name + " []";

                }
                else if (var.Type.Name == "pointer")
                {
                    typeName = var.Type.BaseType.Name + " *";
                }
            }
            lvi.SubItems.Add(typeName);
            lvi.SubItems.Add(var.Address.ToString("X"));
            lvi.SubItems.Add(var.currentValue);
            varView.Items.Add(lvi);
        }
        
        private void ParseRegisterString(Variable var, LocationItem item, string toParse)
        {
            char[] delimiters = new char[] { ' ', ':', ')', '(' , ';'};
            string[] parts = toParse.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            if (parts[0].Contains("DW_OP_fbreg"))
            {
                // e.g  (DW_OP_fbreg: -5)
                // an offset from frame pointer
                item.LocationStr = parts[1];
            }
            else if (parts[0].Contains("DW_OP_bregx"))
            {
                // e.g.  (DW_OP_bregx: 32 (r32) 5)
                // offset from X-regsiter (which is R26/27 - why does it list R32 (which doesn't exist??)
                item.LocationStr = parts[2] + ',' + parts[3];
            }
            else if (parts[0].Contains("DW_OP_breg"))
            {
                // e.g.  (DW_OP_breg28 (r28): 5)
                // offset from a register
                item.LocationStr = parts[1] + ',' + parts[2];
            }
            else if (parts[0].Contains("DW_OP_reg"))
            {
                // e.g. (DW_OP_reg22 (r22); DW_OP_piece: 1; DW_OP_reg23(r23); DW_OP_piece: 1)
                // directly in register
                item.LocationStr = parts[1];
            }
            else if (parts[0].Contains("DW_OP_lit"))
            {
                // not sure what this means yet...
                item.LocationStr = parts[0];
            }
            else if (parts[0].Contains("DW_OP_GNU"))
            {
                // not sure what this means yet...
                item.LocationStr = parts[0];
            }
            else
            {
                MessageBox.Show("Unknown location in section: " + toParse);
                return ;

            }
            if (var != null)
            {
                int reg = 0;
                int offset = 0;
                parts = item.LocationStr.Split(',');
                if (parts[0].StartsWith("r"))
                {
                    // register location, r18-31: these will have been saved on the stack too
                    parts[0] = parts[0].Substring(1);
                    if (int.TryParse(parts[0], out reg))
                    {
                        // see 'analog_int.s' to find/calculate this offset
                        item.FrameOffset = reg - 17;

                        if (parts.Length > 1)
                        {
                            // this is an offset from a  reg, not the reg itself....
                            if (int.TryParse(parts[1], out offset))
                            {
                                item.RegisterOffset = offset;
                            }

                        }
                        else
                        {
                            item.RegisterOffset = 9999; // special value indicating that register value is direct
                        }

                    }
                }
                else
                {
                    // not a regsiter, just a direct offset number
                    if (int.TryParse(item.LocationStr, out offset))
                    {
                        item.FrameOffset = offset;
                        item.RegisterOffset = 8888; // special value indicating that this is straightforward offset
                    }

                }
                var.Locations.Add(item);
            }
        }
        private Variable ParseLocations(string line, Variable var)
        {
            // this is an example list of LocationItems (for a single variable) in the debug file:
            // 000006a6 000004ae 000004ca (DW_OP_reg22 (r22); DW_OP_piece: 1; DW_OP_reg23(r23); DW_OP_piece: 1)
            // 000006b6 000004ca 000004fe (DW_OP_breg28 (r28): 5)
            // 000006c2 000004fe 00000508 (DW_OP_bregx: 32 (r32) 5)
            // 000006cf 00000508 0000050e (DW_OP_fbreg: -5)
            // 000006db<End of list>

            if (line.Contains("End of list"))
            {
                var = null;
                return var;
            }
            if (line.Contains("Offset"))
            {
                // just the column header
                var = null;
                return var;
            }
            UInt32 num;
            char[] delimiters = new char[] { ' '};
            string[] parts = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            if (var == null)
            {
                // this is the first time in this sub-list. Need to find the variable it refers to.
                if (UInt32.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, null, out num))
                {
                    var = Variables.Find(x => x.Location == num);
                    if (var == null)
                    {
                        // not one of our variables
                        return null;
                    }
                }
                else
                {
                    MessageBox.Show("Error in location section: " + line);
                }
            }
            LocationItem item = new LocationItem();
            if (UInt32.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, null, out num))
            {
                item.StartAddr = num;
            }
            if (UInt32.TryParse(parts[2], System.Globalization.NumberStyles.HexNumber, null, out num))
            {
                item.EndAddr = num;
            }
            int bracket = line.IndexOf('(');
            if (bracket < 0)
            {
                MessageBox.Show("error parsing variables...(location).." + line);
            }
            ParseRegisterString(var, item, line.Substring(bracket));


            //if (parts[3].Contains("DW_OP_fbreg"))
            //{
            //    // e.g  000006cf 00000508 0000050e (DW_OP_fbreg: -5)
            //    // an offset from frame pointer
            //    item.LocationStr = parts[4];
            //}
            //else if (parts[3].Contains("DW_OP_bregx"))
            //{
            //    // e.g. 000006c2 000004fe 00000508 (DW_OP_bregx: 32 (r32) 5)
            //    // offset from X-regsiter (which is R26/27 - why does it list R32 (which doesn't exist??)
            //    item.LocationStr = parts[5] + ',' + parts[6];
            //}
            //else if (parts[3].Contains("DW_OP_breg"))
            //{
            //    // e.g. 000006b6 000004ca 000004fe (DW_OP_breg28 (r28): 5)
            //    // offset from a register
            //    item.LocationStr = parts[4] +',' + parts[5];
            //}
            //else if (parts[3].Contains("DW_OP_reg"))
            //{
            //    // e.g. 000006a6 000004ae 000004ca (DW_OP_reg22 (r22); DW_OP_piece: 1; DW_OP_reg23(r23); DW_OP_piece: 1)
            //    // directly in register
            //    item.LocationStr = parts[4];
            //}
            //else if (parts[3].Contains("DW_OP_lit"))
            //{
            //    // not sure what this means yet...
            //    item.LocationStr = parts[3];
            //}
            //else if (parts[3].Contains("DW_OP_GNU"))
            //{
            //    // not sure what this means yet...
            //    item.LocationStr = parts[3];
            //}
            //else
            //{
            //    MessageBox.Show("Unknown location in section: " + line);
            //    return null;

            //}
            //if (var != null)
            //{
            //    int reg = 0;
            //    int offset = 0;
            //    parts = item.LocationStr.Split(',');
            //    if (parts[0].StartsWith("r"))
            //    {
            //        // register location, r18-31: these will have been saved on the stack too
            //        parts[0] = parts[0].Substring(1);
            //        if (int.TryParse(parts[0], out reg))
            //        {
            //            // see 'analog_int.s' to find/calculate this offset
            //            item.FrameOffset = reg - 17;

            //            if (parts.Length > 1)
            //            {
            //                // this is an offset from a  reg, not the reg itself....
            //                if (int.TryParse(parts[1], out offset))
            //                {
            //                    item.RegisterOffset = offset;
            //                }

            //            }
            //            else
            //            {
            //                item.RegisterOffset = 9999; // special value indicating that register value is direct
            //            }

            //        }
            //    }
            //    else
            //    {
            //        // not a regsiter, just a direct offset number
            //        if (int.TryParse(item.LocationStr, out offset))
            //        {
            //            item.FrameOffset = offset;
            //         }

            //    }
            //    var.Locations.Add(item);
            //}
            return var;

        }

        /// <summary>
        /// Using a line in the disassembly (e.g. C:\Users\chris\Documents\Arduino\qdebugtest/qdebugtest.ino:48)
        /// find the source line number at the end
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
                                        sourceItem.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
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