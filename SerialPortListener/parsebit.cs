         foreach (string line in File.ReadLines(file))
            {
                if (inLocationSection & line.Length > 10)
                {
                    var = ParseLocations(line, var);
                    continue;
                }
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
