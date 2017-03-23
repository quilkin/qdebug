using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Text.RegularExpressions;

namespace ArdDebug
{
    class Breakpoint
    {

        public ushort ProgramCounter   { get; set; }
        public ushort FramePointer      { get; set; }
        public UInt32 OpCode           { get; set; }
        public string Assembler        { get; set; }
        /// <summary>
        /// The source file that this breakpoint lies in
        /// </summary>
        public string File             { get; set; }
        /// <summary>
        /// The function name that this breakpoint lies in
        /// </summary>
        public string Function         { get; set; }
        public int SourceLine          { get; set; }
        /// <summary>
        /// was this point set by a user? If not all others are for single-stepping
        /// </summary>
        public bool Manual            { get; set; }

        Breakpoint(ushort pc, UInt32 op, string ass, string f, string func)
        {
            ProgramCounter = pc;
            OpCode = op;
            Assembler = ass;
            File = f;
            Function = func;
            Manual = false;
        }
        public Breakpoint(string f, int source)
        {
            File = f;
            SourceLine = source;
            ProgramCounter = 0;
            OpCode = 0;
            Assembler = "";
             Function = "";
            Manual = false;
        }
        public void SetDetails(ushort pc, UInt32 op, string ass, string func)
        {
            ProgramCounter = pc;
            OpCode = op;
            Assembler = ass;
            Function = func;
        }
        public void SetDetails(ushort pc, string line)
        {
            // we have a line that looks like this:
            ////      520:	8f 92       	push	r8
            // or like
            ////      790:	1e 82       	std	Y+6, r1	; 0x06
            // or like
            ////      40c:	0e 94 f2 01 	call	0x3e4	; 0x3e4 <millis>
            ProgramCounter = pc;
            char[] delimiters = new char[] { '\t', ' ' };
            string[] parts = line.Split(delimiters,StringSplitOptions.RemoveEmptyEntries);

            bool opcodeDone = false;
            OpCode = 0;
            Assembler = string.Empty;
            foreach (string part in parts)
            {
                if (part.EndsWith(":"))
                {
                    continue; // we have pc already
                }
                if (part == ";")
                {
                    //ignore comments
                    break;
                }
                if (!opcodeDone)
                {
                    ushort codebyte;
                    if (ushort.TryParse(part, System.Globalization.NumberStyles.HexNumber, null, out codebyte))
                    {
                        OpCode = (OpCode << 8) + codebyte;
                    }
                    else
                    {
                        opcodeDone = true;
                    }
                }
                if (opcodeDone)
                {
                    Assembler += (part + " ");
                }

            }

        }

    }
}
