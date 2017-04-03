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
        none,                   decl_file,                  decl_line,
        low_pc,                 high_pc,                    type,
        location,               call_file,                  call_line,
        sibling,                artificial,                 abstract_origin,
        frame_base,             object_pointer,             MIPS_linkage_name,
        specification,          stmt_list,                  GNU_call_site_value,
        GNU_all_call_sites,     GNU_tail_call,              name,
        byte_size,              encoding,                   external,
        producer,               language,                   containing_type,
        data_member_location,   accessibility,              upper_bound,
        declaration,            const_value,                vtable_elem_location,
        virtuality,             inline,                     entry_pc,
        ranges,                 comp_dir,                   prototyped
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
            {new Attribute(Attributes.comp_dir,"DW_AT_comp_dir")},
            {new Attribute(Attributes.prototyped,"DW_AT_prototyped")},
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
        public DebugItem()
        {

        }
    }

    partial class Arduino
    {
        List<string> ParseErrors = new List<string>();
        List<DebugItem> DebugItems = new List<DebugItem>();
        List<string> LocationStrings = new List<string>();

        private void SaveError(DebugItem item, string err)
        {
            string errstr = "";
            if (item != null)
                errstr = "Ref <" + item.Ref.ToString("X4") + '>';
            errstr += err;
            if (ParseErrors.Contains(errstr) == false)
                ParseErrors.Add(errstr);
        }
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

            //if (qdebugConstrFound == false && ShortFilename.EndsWith(".ino"))
            //{
            //    MessageBox.Show("You must create a 'QDebug' object as the first line of 'Setup()'");
            //    return false;
            //}

            return true;

        }

        // info from http://www.dwarfstd.org/doc/Debugging%20using%20DWARF-2012.pdf
        private bool ParseBaseTypes()
        {
            //  need to find a list of variable types used. Info is in this format:
            //  <1><6c8>: Abbrev Number: 6 (DW_TAG_base_type)
            //     <6c9>   DW_AT_byte_size   : 2
            //     <6ca>   DW_AT_encoding    : 5	(signed)
            //     <6cb>   DW_AT_name        : int

            VariableTypes.Clear();
            VariableType varType = null;

            foreach (DebugItem item in DebugItems)
            {
                if (item.Level > 0)
                {
                    // end of this item definition, if any, so save it
                    if (varType != null)
                    {
                        VariableTypes.Add(varType);
                    }
                    varType = null;
                    // start of a new one?
                    if (item.tag == Tags.base_type)
                    {
                        if (item.Level > 1)
                        {
                            SaveError(item, "Level 2+ definiton for base type?");
                            continue;
                        }
                        varType = new VariableType(item.Ref);
                        continue;
                    }
                }
                if (varType != null)
                {
                    // found something in the previous item

                    if (item.attr == Attributes.byte_size)
                    {
                        int size = 0;
                        if (int.TryParse(item.content, out size))
                        {
                            varType.Size = size;
                        }
                        else
                        {
                            SaveError(item, item.content + " byte size is not a number");
                        }
                    }
                    else if (item.attr == Attributes.encoding)
                    {
                        int enc = 0;
                        int index = item.content.LastIndexOf('\t');
                        string encStr = item.content.Substring(0, index);
                        if (int.TryParse(encStr, out enc))
                        {
                            varType.Encoding = enc;
                            varType.EncodingString = item.content.Substring(index+1);
                        }
                        else
                        {
                            SaveError(item, "undefined type encoding.." + item.content);
                        }
                    }
                    else if (item.attr == Attributes.name)
                    {
                        int index = item.content.LastIndexOf(')');
                        varType.Name = item.content.Substring(index + 1).Trim();
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
            }
            return true;
        }

        bool ParseTypeDefs()
        {
            // find typedefs (and volatiles) next
            //  for now, we will effectively promote a typedef type to it's base type

            //< 1 >< 6e0 >: Abbrev Number: 7(DW_TAG_typedef)
            //       < 6e1 > DW_AT_name        : (indirect string, offset: 0x37a): uint8_t
            //       < 6e5 > DW_AT_decl_file   : 14
            //       < 6e6 > DW_AT_decl_line   : 126
            //       < 6e7 > DW_AT_type        : < 0x6eb >

            VariableType varType = null;

            foreach (DebugItem item in DebugItems)
            {

                if (item.Level > 0)
                {
                    // end of this item definition, if any, so save it
                    if (varType != null)
                    {
                        VariableTypes.Add(varType);
                    }
                    varType = null;
                    // start of a new one?
                    if (item.tag == Tags.volatile_type || item.tag == Tags.typedef)
                    {
                        if (item.Level > 1)
                        {
                            SaveError(item, "Level 2+ definiton for typedefs?");
                            continue;
                        }
                        varType = new VariableType(item.Ref);
                        continue;
                    }
                }
                if (varType != null)
                {
                    // found something in the previous item
                    if (item.attr == Attributes.name)
                    {
                        int index = item.content.LastIndexOf(')');
                        varType.Name = item.content.Substring(index + 1).Trim();
                    }
                    else if (item.attr == Attributes.type)
                    {
                        UInt16 reference = 0;
                        if (ushort.TryParse(item.content.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out reference))
                        {
                            VariableType baseType = VariableTypes.Find(x => x.Reference == reference);
                            if (baseType != null)
                            {
                                // find type of existing (pre-parsed) base type
                                varType.Encoding = baseType.Encoding;
                                varType.Size = baseType.Size;
                                if (item.tag == Tags.volatile_type)
                                    varType.Name = baseType.Name;
                            }
                        }
                        else
                        {
                            SaveError(item, "undefined type ..." + item.content);
                        }
                    }
                }
            }
            return true;
        }

        private bool ParseArrayTypes()
        {
            // find Array types and pointers next
            VariableType varType = null;
            int prevLevel = 1;

            foreach (DebugItem item in DebugItems)
            {

                if (item.Level > 0)
                {
                    if (item.Level == prevLevel)
                    {
                        // end of this item definition, if any, so save it
                        if (varType != null)
                        {
                            VariableTypes.Add(varType);
                        }
                        varType = null;
                        if (item.tag == Tags.array_type || item.tag == Tags.pointer_type)
                        {
                            if (item.Level > 2)
                            {
                                SaveError(item, "Level 3+ definiton for array defs?");
                                continue;
                            }
                            varType = new VariableType(item.Ref);
                            varType.Name = (item.tag == Tags.array_type )? "array": "pointer";
                            continue;
                        }

                        if (item.tag == Tags.none)
                        {
                            // Abbrev Number: 0   ?
                            --prevLevel;
                        }
                    }
                    else
                    {
                        prevLevel = item.Level;
                        // internal definition, continue with it
                    }
                }
                if (varType != null)
                {
                    // found something in the previous item
                    //< 2 >< 2037 >: Abbrev Number: 30(DW_TAG_subrange_type)
                    // < 2038 > DW_AT_type        : < 0xae0 >
                    //  < 203c > DW_AT_upper_bound : 9              ... this provides the size of the array
                    //if (line.Contains(" DW_AT_upper_bound"))
                    if (item.attr == Attributes.upper_bound)
                    {
                        UInt16 size = 0;
                        if (ushort.TryParse(item.content, out size))
                        {
                            varType.Size = size + 1;
                        }
                        else
                        {
                            SaveError(item, "Array size error: " + item.content);
                        }
                    }
                     else if (item.attr == Attributes.type)
                    {
                        if (varType.BaseType == null)
                        {
                            UInt16 reference = 0;
                            if (ushort.TryParse(item.content.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out reference))
                            {
                                VariableType baseType = VariableTypes.Find(x => x.Reference == reference);
                                if (baseType != null)
                                {
                                    // find type of existing (pre-parsed) base type
                                    varType.Encoding = baseType.Encoding;
                                    varType.Size = baseType.Size;
                                    varType.BaseType = baseType;
                                }
                            }
                            else
                            {
                                SaveError(item, "undefined type ..." + item.content);
                            }
                        }
                        // else this is  a size_type ? (not needed????)
                    }
                }

            }
            return true;

        }
        /// <summary>
        /// find the entry in the File Name Table so we can se which vars are in our source file
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private int CheckFileReference(string line)
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
                        if (int.TryParse(parts[0], out fileRef))
                        {
                            return fileRef;
                        }
                    }
                }
            }
            return 0;
        }

        bool ParseGlobalFunctions()
        {
            // ignore funcs inside classes for now
            // include parsing of local vars in functions
            Function func = null;
            Variable var = null;
            int currentLevel = 1;
            foreach (DebugItem item in DebugItems)
            {
                // global functions will be marked as level 1
                // parameters and local vars will be marked as level 2 (or 3+ for included lexical blocks)
                // a function definition isn't complete until there is another item with level 1
                // ... so until then, any vars are members of that function
                if (item.Level == 1)
                {
                    // end of this func definition, if any, so save it (if it's been fully defined)
                    if (func != null && func.Name != null)
                    {
                        Functions.Add(func);
                    }
                    func = null;
                    if (item.tag == Tags.subprogram)
                    {
                        // a new defintion starts here
                        func = new Function(this);
                        continue;
                    }

                }
                if (item.Level >= 2)
                {
                    // end of a variable or param definition, if any, so save it
                    if (var != null && var.Name != null)
                    {
                        Variables.Add(var);
                        ListViewItem lvi = var.CreateVarViewItem();
                        if (lvi != null)
                            varView.Items.Add(lvi);
                        var = null;  // get ready for next one
                    }
                    var = null;
                    if (item.tag == Tags.variable || item.tag == Tags.formal_parameter)
                    {
                        // a new defintion starts here
                        var = new Variable(this);
                        continue;
                    }
                   
                }
                if (item.Level >= 1)
                {
                    currentLevel = item.Level;
                    if (item.tag == Tags.none)
                    {
                        // Abbrev Number: 0   ?
                        --currentLevel;
                        continue;
                    }
                }
                

                if (currentLevel >= 2 && var != null)
                {
                    // found something in the previous line, and we're dealing with a variable not the function itself
                    if (func == null)
                    {
                        // we've discarded thsi function (not being delat with yet)
                        // so discard its variables as well
                        var = null;
                        continue;
                    }
                    if (item.attr == Attributes.artificial)
                    {
                        // not interested (at this stage anyway...)
                        var = null;
                        continue;
                    }
                    if (item.attr == Attributes.name)
                    {
                        // this is a local var in a func, must modify its name
                        var.Name = func.Name + "." + VarNameFromItemContent(item);
                    }
                    else if (item.attr == Attributes.type)
                    {
                        var.Type = VarTypeFromItemContent(item);
                    }
                    else if (item.attr == Attributes.decl_file)
                    {
                        var = VarFileReference(item, var);
                    }
                    else if (item.attr == Attributes.abstract_origin)
                    {
                        // related to inline functions, not dealing with these yet....
                        var = null;
                    }
                    else if (item.attr == Attributes.location)
                    {
                        // local vars usually have this type of entry:
                        //     <10db>   DW_AT_location    : 0x271 (location list)
                        // also might be this
                        //       <425a>   DW_AT_location    : 2 byte block: 8c 1 	(DW_OP_breg28 (r28): 1)
                        if (item.content.Contains("location list"))
                        {
                            UInt16 loc = 0;
                            if (UInt16.TryParse(item.content.Substring(2, 4), System.Globalization.NumberStyles.HexNumber, null, out loc))
                            {
                                var.Location = loc;
                                // actual address to be found later, from the location list at the end of the file
                            }
                            else
                            {
                                SaveError(item, "error parsing variables..(location)..." + item.content);
                            }
                        }
                        else if (item.content.Contains("DW_OP_reg") || item.content.Contains("DW_OP_breg") || item.content.Contains("DW_OP_fbreg"))
                        {
                            // e.g.   <2474>   DW_AT_location    : 6 byte block: 64 93 1 65 93 1 	(DW_OP_reg20 (r20); DW_OP_piece: 1; DW_OP_reg21 (r21); DW_OP_piece: 1)
                            int bracket = item.content.IndexOf('(');
                            //if (bracket < 0)
                            //{
                            //    MessageBox.Show("error parsing variables...(location).." + line);
                            //}
                            LocationItem locItem = new LocationItem();
                            // any address valid
                            locItem.StartAddr = 0x00000000;
                            locItem.EndAddr = 0xFFFFFFFF;
                            string err = ParseRegisterString(var, locItem, item.content.Substring(bracket));
                            if (err.Length > 0)
                                SaveError(item, err);
                            var.Function = func;
                            Variables.Add(var);
                            // associate with function so we can update locals only when stepping through function
                            func.LocalVars.Add(var);
                            if (var.Type == null)
                            {
                                SaveError(item, "Var has no type: " + item.content);
                            }
                            else
                            {
                                varView.Items.Add(var.CreateVarViewItem());
                            }
                            var = null;
                            continue;
                        }
                        else
                        {
                            SaveError(item, "error parsing variables..(location)..." + item.content);
                        }
                    }

                }
                if (currentLevel == 1 && func != null)
                {
                    // dealing with the function itself, rather than vars within it
                    if (item.attr == Attributes.name)
                    {
                        func.Name = VarNameFromItemContent(item);
                    }
                    else if (item.attr == Attributes.artificial)
                    {
                        // not dealing with this (yet?), ignore all included stuff
                        func = null;
                    }
                    else if (item.attr == Attributes.low_pc)
                    {
                        //   < f96 > DW_AT_low_pc      : 0x1b4
                        UInt16 loc = 0;
                        if (UInt16.TryParse(item.content.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out loc))
                            func.LowPC = loc;
                    }
                    else if (item.attr == Attributes.high_pc)
                    {
                        UInt16 loc = 0;
                        if (UInt16.TryParse(item.content.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out loc))
                            func.HighPC = loc;
                    }
                    else if (item.attr == Attributes.decl_file)
                    {
                        UInt16 fileRef = 0;
                        if (ushort.TryParse(item.content, out fileRef))
                        { 
                             func.fileRef = fileRef;
                        }
                        else
                        {
                            SaveError(item, "invalid file ref for function: " + item.content);
                        }
                    }
                    else if (item.attr == Attributes.specification)
                    {
                        // used for structs/classes - not dealing with this yet
                        func = null;
                        continue;
                    }
                    else if (item.attr == Attributes.inline)
                    {
                        // not dealing with this yet
                        func = null;
                        continue;
                    }

                }

            }
            return true;
        }


        string VarNameFromItemContent(DebugItem item)
        {
            //  could be    "< 1fa8 > DW_AT_name        : (indirect string, offset: 0x208): num1"
            //  or          "< 1fa8 > DW_AT_name        : num2"
            // (not yet sure what the difference is......)
            char[] delimiters = new char[] { ' ', ':', ')', '(' };
            string[] parts = item.content.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            string name;
            if (parts.Length == 1)
            {
                name = parts[0];
            }
            else
            {
                name = parts[parts.Length - 1];
            }
            return name;
        }
        VariableType VarTypeFromItemContent(DebugItem item)
        {
            // e.g.    < 1e63 > DW_AT_type        : < 0xb14 >
            UInt16 typeRef = 0;
            if (ushort.TryParse(item.content.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out typeRef))
            {
                VariableType vType = VariableTypes.Find(x => x.Reference == typeRef);
                if (vType != null)
                {
                    return vType;
                }
                else
                {
                    SaveError(item, "cannot find type for var");
                }
            }
            else
            {
                SaveError(item, "invalid type ref for var: " + item.content);
            }
            return null;
        }

        Variable VarFileReference(DebugItem item, Variable var)
        {
            UInt16 fileRef = 0;
            if (ushort.TryParse(item.content, out fileRef))
            {
                if (fileRef == SourceFileRef)
                {
                    // this variable is part of our source file
                    var.isMine = true;
                    return var;
                }
                else
                {
                    return null;  // ignore for now???
                }
            }
            else
            {
                SaveError(item, "invalid file ref for var: " + item.content);
                return null;
            }
        }
        bool ParseGlobalVariables()
        {
            Variable var = null;
            foreach (DebugItem item in DebugItems)
            {
                //// looking for info like this, to find location of our variables ('numf' is a global in this example)
                //< 1 >< 1fa7 >: Abbrev Number: 78(DW_TAG_variable)
                //  < 1fa8 > DW_AT_name        : (indirect string, offset: 0x208): numf
                //  < 1fac > DW_AT_decl_file   : 7
                //  < 1fad > DW_AT_decl_line   : 9
                //  < 1fae > DW_AT_type        : < 0x1fb8 >
                //  < 1fb2 > DW_AT_location    : 5 byte block: 3 0 1 80 0(DW_OP_addr: 800100)

                // global var defintions should always be level 1
                if (item.Level == 1)
                {
                    // end of this item definition, if any, so save it
                    if (var != null)
                    {
                        var.isGlobal = true;
                        Variables.Add(var);
                        ListViewItem lvi = var.CreateVarViewItem();
                        if (lvi != null)
                            varView.Items.Add(var.CreateVarViewItem());
                    }
                    var = null;
                    // start of a new one?
                    if (item.tag == Tags.variable)
                    {
                        //if (item.Level > 1)
                        //{
                        //    SaveError(item, "Level 2+ definiton for global var?");
                        //    continue;
                        //}
                        var = new Variable(this);
                        continue;
                    }
                }
                if (var != null)
                {
                    // found something in the previous line
                    if (item.attr == Attributes.name)
                    {
                        var.Name = VarNameFromItemContent(item);
                    }
                    else if (item.attr == Attributes.type)
                    {
                        var.Type = VarTypeFromItemContent(item);
                    }
                    else if (item.attr == Attributes.decl_file)
                    {
                        var = VarFileReference(item, var);
                    }
                    else if (item.attr == Attributes.location)
                    {
                        //   < 1f2f > DW_AT_location    : 5 byte block: 3 1f 1 80 0(DW_OP_addr: 80011f)
                        // don't yet know the significance of the '5 byte block' bit ......
                        int addrIndex = item.content.LastIndexOf("addr");
                        UInt32 addr = 0;
                        if (UInt32.TryParse(item.content.Substring(addrIndex + 5, 6), System.Globalization.NumberStyles.HexNumber, null, out addr))
                        {
                            // don't want the '80' bit for this processsor
                            var.Address = (UInt16)(addr & 0xFFFF);
                        }
                        else
                        {
                            SaveError(item, "invalid address for var: " + item.content);
                        }
                    }
                }

            }
            return true;
        }
        private bool ParseDebugInfo(string file)
        {

            bool inLocationSection = false;
            bool startedItems = false;
            bool doneItems = false;
            bool doneFileRef = false;

            DebugItems.Clear();
            ParseErrors.Clear();

            varView.Items.Clear();
            Variables.Clear();
            MyVariables.Clear();
            Functions.Clear();
            LocationStrings.Clear();

            // firstly, convert the file into a list of debug items
            foreach (string line in File.ReadLines(file))
            {
                if (line.StartsWith(" <"))
                {
                    startedItems = true;
                }
                if (startedItems == false)
                    continue;

                if (inLocationSection & line.Length > 10)
                {
                    // ParseLocations(line);
                    LocationStrings.Add(line);
                    continue;
                }
                if (line.Contains(".debug_line"))
                {
                    // end of main debug definitions
                    doneItems = true;
                }
                else if (line.Contains(".debug_loc"))
                {
                    inLocationSection = true;
                }
                if (doneItems  && ! doneFileRef)
                {
                    SourceFileRef = CheckFileReference(line);
                    if (SourceFileRef > 0)
                    {
                        doneFileRef = true;
                        
                    }
                    continue;
                }

                char[] delimiters = new char[] { ' ', '<', '>', ':' };
                string[] parts = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                    continue;
                DebugItem item = new DebugItem();
                UInt16 number = 0;
                if (UInt16.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, null, out number))
                {
                    //if (number < 10)
                    if (line.Contains("Abbrev Number"))
                    {
                        // e.g. < 1 >< 22 >: Abbrev Number: 4 (DW_TAG_array_type)
                        item.Level = number;
                        if (UInt16.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, null, out number))
                        {
                            item.Ref = number;
                            if (parts.Length > 5)
                            {
                                string tagstr = parts[5].Replace("(", "");
                                tagstr = tagstr.Replace(")", "");
                                DebugItem.Tag t = DebugItem.TagList.Find(x => x.def == tagstr);
                                if (t == null)
                                {
                                    SaveError(item, "missing tag definition: " + tagstr);
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
                            SaveError(item, "Unkown level indicator: " + line);
                            continue;
                        }
                    }
                    else
                    {
                        // an attribute line, e.g.     <5e32>   DW_AT_high_pc     : 0x1b82
                        if (UInt16.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, null, out number))
                        {
                            item.Ref = number;
                            item.Level = -1;
                            string attrstr = parts[1];
                            DebugItem.Attribute a = DebugItem.AttributeList.Find(x => x.def == attrstr);
                            if (a == null)
                            {
                                SaveError(item, "missing attribute definition: " + attrstr);
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
                            SaveError(item, "Unkown reference indicator: " + line);
                            continue;
                        }
                    }
                }
            }
            if (SourceFileRef == 0)
            {
                SaveError(null, "error parsing debug file, no source file abbr found");
            }

            // get all the different variable types. Base types first so that array types know about their content
            if (ParseBaseTypes() == false)
                return false; 
            if (ParseTypeDefs() == false)
                return false;
            if (ParseArrayTypes() == false)
                return false;

            // Now find the variables themselves.
            // Remove old ones first.
            varView.Items.Clear();
            varView.Sorting = SortOrder.None;
            if (ParseGlobalVariables() == false)
                return false;
            if (ParseGlobalFunctions() == false)
                return false;
            foreach (string locStr in LocationStrings)
            {
                ParseLocations(locStr);
            }
            if (ParseErrors.Count > 0)
            {
                string errStr = "";
                foreach (string err in ParseErrors)
                {
                    errStr += (err + '\n');
                }
                MessageBox.Show(errStr, "File import failed");
                return false;
            }
            return true;
        }

        //void AddToVarView(Variable var)
        //{
        //    ListViewItem lvi = new ListViewItem();
        //    lvi.Name = var.Name.ToString();
        //    lvi.Text = var.Name.ToString();
        //    if (var.Type == null)
        //    {
        //        MessageBox.Show("error parsing variables..." + var.Name + " has no type");
        //        var = null;
        //        return;
        //    }
        //    string typeName = var.Type.Name;
        //    if (var.Type.BaseType != null)
        //    {
        //        // array type etc
        //        if (var.Type.Name == "array")
        //        {
        //            typeName = var.Type.BaseType.Name + " []";

        //        }
        //        else if (var.Type.Name == "pointer")
        //        {
        //            typeName = var.Type.BaseType.Name + " *";
        //        }
        //    }
        //    lvi.SubItems.Add(typeName);
        //    lvi.SubItems.Add(var.Address.ToString("X"));
        //    lvi.SubItems.Add(var.currentValue);
        //    varView.Items.Add(lvi);
        //}
        
        private string ParseRegisterString(Variable var, LocationItem item, string toParse)
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
                return ("Unknown location in section: " + toParse);
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
            return "";
        }
        private bool ParseLocations(string line)
        {
            // this is an example list of LocationItems (for a single variable) in the debug file:
            // 000006a6 000004ae 000004ca (DW_OP_reg22 (r22); DW_OP_piece: 1; DW_OP_reg23(r23); DW_OP_piece: 1)
            // 000006b6 000004ca 000004fe (DW_OP_breg28 (r28): 5)
            // 000006c2 000004fe 00000508 (DW_OP_bregx: 32 (r32) 5)
            // 000006cf 00000508 0000050e (DW_OP_fbreg: -5)
            // 000006db<End of list>
            Variable var = null;
            if (line.Contains("End of list"))
            {
        //        var = null;
                return false;
            }
            if (line.Contains("Offset"))
            {
                // just the column header
          //      var = null;
                return false;
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
                        return false;
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
            return true;

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