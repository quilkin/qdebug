using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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


    class Variable
    {

    //     <3><1d9a>: Abbrev Number: 39 (DW_TAG_variable)
    //      <1d9b>   DW_AT_name        : (indirect string, offset: 0x7ef): changeTarget
    //      <1d9f> DW_AT_decl_file   : 3
    //      <1da0>   DW_AT_decl_line   : 131
    //      <1da1>   DW_AT_type        : <0x6da>
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
        private Arduino arduino;
        private String comString = String.Empty;
        public Variable(Arduino ard)
        {
            arduino = ard;
            currentValue = string.Empty;
        }


        //public void SetDetails(string type, UInt16 addr, int size, bool signed)
        //{
        //    Address = addr;

        //}
        public void GetValue()
        {
             UInt16 requestedAddr = 0;
            UInt16 data = 0;
            UInt16 datahi = 0;
            UInt32 bigdata = 0;
            String sendStr;

            requestedAddr = Address;
            sendStr = "A" + requestedAddr.ToString("X4") + "\n";
            arduino.Send(sendStr);
            comString = arduino.ReadLine();

            if (comString.Length < 5)
                return ;
            if (Type == null)
                return;
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
                            return;
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
            //return currentValue;
        }

    }
}


