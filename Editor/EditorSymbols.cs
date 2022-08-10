﻿using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEditor;

namespace NewBlood
{
    /// <summary>Provides APIs for managing editor symbols.</summary>
    public static unsafe class EditorSymbols
    {
    #if UNITY_EDITOR_WIN
        static EditorSymbols()
        {
            using (var process = Process.GetCurrentProcess())
            using (var module  = process.MainModule)
            {
                fixed (char* pUserSearchPath = Path.GetDirectoryName(module.FileName))
                {
                    DbgHelp.SymSetOptions(DbgHelp.SYMOPT_IGNORE_CVREC);
                    
                    if (DbgHelp.SymInitializeW(new IntPtr(-1), (ushort*)pUserSearchPath, fInvadeProcess: 1) != 0)
                    {
                        AssemblyReloadEvents.beforeAssemblyReload += () =>
                        {
                            DbgHelp.SymCleanup(new IntPtr(-1));
                        };
                    }
                }
            }
        }
    #endif

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
            return (T)Marshal.GetDelegateForFunctionPointer(GetSymbol(name), typeof(T));
        }
    #endif
    }
}
