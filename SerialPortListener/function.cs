using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArdDebug
{
    class Function
    {
        public string Name { get; set; }
        public bool isMine { get; set; }

        /// <summary>
        /// Name of return type (int, float etc)
        /// </summary>
        public VariableType Type { get; set; }
        /// <summary>
        /// source file reference (from .elf) where this is defined
        /// </summary>
        public int fileRef       { get; set; }
    }
}

