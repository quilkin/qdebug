using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArdDebug
{
    class Function
    {
        //public enum FunctionOwner : byte { None, Mine, Other };
        private Arduino arduino;
        //public FunctionOwner Owner { get; set; }
        public string Name { get; set; }
        public bool IsMine { get { return (fileRef == arduino.SourceFileRef);  } }

        /// <summary>
        /// Name of return type (int, float etc)
        /// </summary>
        public VariableType Type { get; set; }
        /// <summary>
        /// source file reference (from .elf) where this is defined
        /// </summary>
        public int fileRef       { get; set; }

        /// <summary>
        /// The file it was declared in
        /// </summary>
        public string File { get; set; }
        public List<Variable> LocalVars { get; set; }

        public UInt16 LowPC { get; set; }
        public UInt16 HighPC { get; set; }
        
        public Function(Arduino ard)
        {
            arduino = ard;
            LocalVars = new List<Variable>();
        }
    }
}

