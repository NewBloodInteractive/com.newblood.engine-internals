using System;

namespace NewBlood
{
#if UNITY_2019_2_OR_NEWER
    /// <summary>Attribute used to indicate that a field should be initialized to the address of an editor symbol.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class EditorImportAttribute : Attribute
    {
        /// <summary>The name of the symbol to import.</summary>
        public string Name { get; }

        /// <summary>Initializes a new <see cref="EditorImportAttribute"/> instance.</summary>
        public EditorImportAttribute(string name)
        {
            Name = name;
        }
    }
#endif
}
