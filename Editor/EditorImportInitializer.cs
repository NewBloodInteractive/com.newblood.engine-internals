using System;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditor;

namespace NewBlood
{
#if UNITY_2020_1_OR_NEWER
    [InitializeOnLoad]
    static class EditorImportInitializer
    {
        static EditorImportInitializer()
        {
            foreach (FieldInfo field in TypeCache.GetFieldsWithAttribute<EditorImportAttribute>())
            {
                var attribute = field.GetCustomAttribute<EditorImportAttribute>();

                if (!field.IsStatic || field.IsInitOnly || string.IsNullOrEmpty(attribute.Name))
                    continue;

                if (!EditorSymbols.TryGetSymbol(attribute.Name, out IntPtr address))
                    continue;

                if (field.FieldType == typeof(IntPtr))
                    field.SetValue(null, address);
                else if (typeof(Delegate).IsAssignableFrom(field.FieldType))
                    field.SetValue(null, Marshal.GetDelegateForFunctionPointer(address, field.FieldType));
            }
        }
    }
#endif
}
