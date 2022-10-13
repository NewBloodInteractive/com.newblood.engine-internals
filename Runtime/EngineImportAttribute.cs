using System;
using System.Runtime.InteropServices;

namespace NewBlood
{
    /// <summary>Attribute used to indicate that a field should be initialized to the address of a symbol.</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class EngineImportAttribute : Attribute
    {
        /// <summary>The name of the symbol to import.</summary>
        public string Name { get; }

        /// <summary>The calling convention to use.</summary>
        public CallingConvention CallingConvention { get; set; }

        /// <summary>Initializes a new <see cref="EngineImportAttribute"/> instance.</summary>
        public EngineImportAttribute(string name)
        {
            Name = name;
        }
    }
}
