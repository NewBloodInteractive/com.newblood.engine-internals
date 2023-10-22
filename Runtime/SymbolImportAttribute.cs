using System;
using System.Runtime.InteropServices;

namespace NewBlood
{
    /// <summary>Indicates that the attributed method is exposed by the engine's debugging symbols.</summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class SymbolImportAttribute : Attribute
    {
        /// <summary>The name of the symbol to import.</summary>
        public string Name { get; }

        /// <summary>The calling convention to use.</summary>
        public CallingConvention CallingConvention { get; set; }

        /// <summary>Initializes a new <see cref="SymbolImportAttribute"/> instance.</summary>
        public SymbolImportAttribute(string name)
        {
            Name = name;
        }
    }
}
