using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NewBlood
{
    public static class EngineImportHelpers
    {
        public static IntPtr BaseAddress { get; } = GetBaseAddress();

    #if UNITY_EDITOR
        public static string MainModuleFileName { get; } = GetMainModuleFileName();
    #endif

        public static Delegate GetDelegate(IntPtr ptr, Type t)
        {
            if (ptr == IntPtr.Zero)
                return null;

            return Marshal.GetDelegateForFunctionPointer(ptr, t);
        }

        private static ProcessModule GetMainModule(Process process)
        {
        #if UNITY_EDITOR && !UNITY_2023_1_OR_NEWER
            return process.MainModule;
        #else
            foreach (ProcessModule module in process.Modules)
            {
            #if UNITY_EDITOR && UNITY_2023_1_OR_NEWER
                if (module.ModuleName.Equals("Unity.dll", StringComparison.OrdinalIgnoreCase))
                    return module;
            #endif

            #if !UNITY_EDITOR
                if (module.ModuleName.Equals("UnityPlayer.dll", StringComparison.OrdinalIgnoreCase))
                    return module;
            #endif
            }

            return process.MainModule;
        #endif
        }

    #if UNITY_EDITOR
        private static string GetMainModuleFileName()
        {
            using (var process = Process.GetCurrentProcess())
            {
                return GetMainModule(process).FileName;
            }
        }
    #endif

        private static IntPtr GetBaseAddress()
        {
            using (var process = Process.GetCurrentProcess())
            {
                return GetMainModule(process).BaseAddress;
            }
        }
    }
}
