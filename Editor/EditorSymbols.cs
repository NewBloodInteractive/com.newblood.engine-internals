using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEditor;

namespace NewBlood
{
    /// <summary>Provides APIs for managing editor symbols.</summary>
    [InitializeOnLoad]
    public static unsafe class EditorSymbols
    {
        /// <summary>Ensures that the <see cref="EditorSymbols"/> class has been initialized.</summary>
        public static void EnsureInitialized()
        {
        }

        /// <summary>Gets the address of the symbol with the provided name.</summary>
        public static IntPtr GetSymbol(string name)
        {
            IntPtr address;

            if (!TryGetSymbol(name, out address))
                throw new EntryPointNotFoundException();

            return address;
        }

        /// <summary>Gets the address of the symbol with the provided name.</summary>
        public static bool TryGetSymbol(string name, out IntPtr address)
        {
        #if UNITY_EDITOR_WIN
            var symbol = new SYMBOL_INFOW { SizeOfStruct = (uint)sizeof(SYMBOL_INFOW) };

            fixed (char* pName = name)
            {
                if (DbgHelp.SymFromNameW(new IntPtr(-1), (ushort*)pName, &symbol) == 0)
                {
                    address = IntPtr.Zero;
                    return false;
                }
            }

            address = (IntPtr)symbol.Address;
            return true;
        #else
            throw new PlatformNotSupportedException();
        #endif
        }

        /// <summary>Gets a delegate for the symbol with the provided name.</summary>
        public static Delegate GetDelegateForSymbol(string name, Type t)
        {
            return Marshal.GetDelegateForFunctionPointer(GetSymbol(name), t);
        }

    #if CSHARP_7_3_OR_NEWER
        /// <summary>Gets a delegate for the symbol with the provided name.</summary>
        public static T GetDelegateForSymbol<T>(string name)
            where T : Delegate
        {
            return Marshal.GetDelegateForFunctionPointer<T>(GetSymbol(name));
        }
    #endif

    #if UNITY_EDITOR_WIN
        static EditorSymbols()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

            using (var process = Process.GetCurrentProcess())
            using (var module  = process.MainModule)
            {
                fixed (char* pUserSearchPath = Path.GetDirectoryName(module.FileName))
                {
                    DbgHelp.SymSetOptions(DbgHelp.SYMOPT_IGNORE_CVREC);
                    ThrowOnError(DbgHelp.SymInitializeW(new IntPtr(-1), (ushort*)pUserSearchPath, fInvadeProcess: 1));
                }
            }

        #if UNITY_2020_1_OR_NEWER
            foreach (FieldInfo field in TypeCache.GetFieldsWithAttribute<EditorImportAttribute>())
            {
                var attribute = field.GetCustomAttribute<EditorImportAttribute>();

                if (!field.IsStatic || string.IsNullOrEmpty(attribute.Name))
                    continue;

                if (!TryGetSymbol(attribute.Name, out IntPtr address))
                    continue;

                if (field.FieldType == typeof(IntPtr))
                    field.SetValue(null, address);
                else if (typeof(Delegate).IsAssignableFrom(field.FieldType))
                    field.SetValue(null, Marshal.GetDelegateForFunctionPointer(address, field.FieldType));
            }
        #endif
        }

        static void OnBeforeAssemblyReload()
        {
            ThrowOnError(DbgHelp.SymCleanup(new IntPtr(-1)));
        }
    #endif

        static void ThrowOnError(int result)
        {
            if (result == 0)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        }
    }
}
