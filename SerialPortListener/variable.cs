using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ArdDebug
{
    // info from http://www.dwarfstd.org/doc/Debugging%20using%20DWARF-2012.pdf

    class VariableType
    {
    //  <1><6c8>: Abbrev Number: 6 (DW_TAG_base_type)
    //  <6c9>   DW_AT_byte_size   : 2
    //  <6ca>   DW_AT_encoding    : 5	(signed)
    //  <6cb>   DW_AT_name        : int

        /// <summary>
        /// Name of type (int, float etc)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// ref used by variable, to find its type
        /// </summary>
        public UInt16 Reference { get; set; }
        /// <summary>
        /// The size (in bytes) of storage space
        /// </summary>
        public int Size { get; set; }
        public int Encoding { get; set; }
        public string EncodingString { get; set; }

        public VariableType BaseType { get; set; }

    public VariableType(UInt16 reference)
        {
            Reference = reference;
            BaseType = null;
        }
    }

    /// <summary>
    /// Used for local variables in functions, tells us where to find them as teh program counter changes
    /// </summary>
    class LocationItem
    {
        // this is an example list of LocationItems (for a single variable) in the debug file:
        // 000006a6 000004ae 000004ca (DW_OP_reg22 (r22); DW_OP_piece: 1; DW_OP_reg23(r23); DW_OP_piece: 1)
        // 000006b6 000004ca 000004fe (DW_OP_breg28 (r28): 5)
        // 000006c2 000004fe 00000508 (DW_OP_bregx: 32 (r32) 5)
        // 000006cf 00000508 0000050e (DW_OP_fbreg: -5)
        // 000006db<End of list>
        public UInt32 StartAddr { get; set; }
        public UInt32 EndAddr  { get; set; }
        public String LocationStr { get; set; }
        public int FrameOffset { get; set; }
    }
    class Variable
    {
        public string Name { get; set; }
        /// <summary>
        /// Name of type (int, float etc)
        /// </summary>
        public VariableType Type { get; set; }
        /// <summary>
        /// Where this var is stored in memory
        /// </summary>
        public UInt16 Address { get; set; }
        public string currentValue { get; set; }
        public string lastValue { get; set; }

        /// <summary>
        /// Used for location of local variables
        /// Just gives a pointer to the location section of the debug file
        /// (used only temporarily while file is being parsed)
        /// </summary>
        public UInt16 Location { get; set; }
        /// <summary>
        /// A list of the actual locations of a local variable, found from the location section
        /// </summary>
        public List<LocationItem> Locations;
        /// <summary>
        /// The function that this var appears in (if any) : only for local and parameter variables
        /// </summary>
        public Function Function { get; set; }

        private Arduino arduino;
        private String comString = String.Empty;
        public Variable(Arduino ard)
        {
            arduino = ard;
            currentValue = string.Empty;
            Locations = new List<LocationItem>();
        }

        public bool GetValue(Function func)
        {
            UInt16 requestedAddr = 0;
            UInt16 data = 0;
            UInt16 datahi = 0;
            UInt32 bigdata = 0;
            String sendStr;

            // first find location if it's a local variable. This will depend on current program counter
            if (Function == func && arduino.currentBreakpoint != null)
            {
                foreach (LocationItem item in Locations)
                {
                    if (item.StartAddr <= arduino.currentBreakpoint.ProgramCounter)
                    {
                        if (item.EndAddr > arduino.currentBreakpoint.ProgramCounter)
                        {
                            // we are in the right area.
                            int offset = item.FrameOffset;
                            if (offset > 0)
                            {
                                Address = (ushort)(arduino.currentBreakpoint.FramePointer + offset);
                            }
                            else
                            { //???? not sure yet!
                                Address = (ushort)(arduino.currentBreakpoint.FramePointer - offset);

                            }
  
                        }
                    }

                }
            }

            requestedAddr = Address;
            sendStr = "A" + requestedAddr.ToString("X4") + "\n";
            arduino.Send(sendStr);
            comString = arduino.ReadLine();

            if (comString.Length < 5)
                return false;
            if (Type == null)
                return false;
            lastValue = currentValue;
            if (ushort.TryParse(comString.Substring(1, 4), System.Globalization.NumberStyles.HexNumber, null, out data))
            {
                if (Type.Size == 1)
                {
                    // our data has two bytes; we just want the 'lowest' one

                    if (Type.Name == "char")
                    {
                        char bite = (char)(data & 0xFF);
                        currentValue = "'" + bite + "'";
                    }
                    else
                    {
                        byte bite = (byte)(data & 0xFF);
                        currentValue = bite.ToString();
                    }
                }
                else
                {
                    currentValue = data.ToString();
                    if (Type.Size == 4)
                    {
                        // need to get the next two bytes
                        requestedAddr += 2;
                        sendStr = "A" + requestedAddr.ToString("X4") + "\n";
                        arduino.Send(sendStr);
                        comString = arduino.ReadLine();
                        if (comString.Length < 5)
                            return false;
                        if (ushort.TryParse(comString.Substring(1, 4), System.Globalization.NumberStyles.HexNumber, null, out datahi))
                        {
                            bigdata = (UInt32)(datahi << 16) + data;
                            currentValue = bigdata.ToString();
                            if (Type.Name == "float")
                            {
                                byte[] fbytes = BitConverter.GetBytes(bigdata);
                                double f = BitConverter.ToSingle(fbytes, 0);

                                currentValue = f.ToString();
                            }
                        }

                    }
                }
            }
            return true;
        }
        public ListViewItem CreateVarViewItem()
        {
 
            ListViewItem lvi = new ListViewItem();
            lvi.Name = Name.ToString();
            lvi.Text = Name.ToString();
            if (Type == null)
            {
                MessageBox.Show("error parsing variables..." + Name + " has no type");
                // var = null;
                return null;
            }
            string typeName = Type.Name;
            if (Type.BaseType != null)
            {
                // array type etc
                if (Type.Name == "array")
                {
                    typeName = Type.BaseType.Name + " []";

                }
                else if (Type.Name == "pointer")
                {
                    typeName = Type.BaseType.Name + " *";
                }
            }
            lvi.SubItems.Add(typeName);
            lvi.SubItems.Add(Address.ToString("X"));
            //if (Address != 0)
            lvi.SubItems.Add(currentValue);
 
            return lvi;
        }
        
    }
}


