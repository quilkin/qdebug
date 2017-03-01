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
        public string OpCode           { get; set; }
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

        Breakpoint(ushort pc, string op, string ass, string f, string func)
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
            OpCode = "";
            Assembler = "";
             Function = "";
            Manual = false;
        }
        public void SetDetails(ushort pc, string op, string ass, string func)
        {
            ProgramCounter = pc;
            OpCode = op;
            Assembler = ass;
            Function = func;
        }
        public void SetDetails(ushort pc, string line)
        {
            // we have a line that looks like this:
            ////      790:	1e 82       	std	Y+6, r1	; 0x06
            ProgramCounter = pc;
            char[] delimiters = new char[] { '\t', ' ' };
            string[] parts = line.Split(delimiters,StringSplitOptions.RemoveEmptyEntries);

            OpCode = parts[1] + parts[2];
            Assembler = parts[3 ] + " " +parts[4];

        }

    }
}
