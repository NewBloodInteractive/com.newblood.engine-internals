using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NewBlood
{
    public static class UnityModuleHelpers
    {
        private static readonly IntPtr s_BaseAddress = GetBaseAddressCore();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr GetBaseAddress()
        {
            return s_BaseAddress;
        }

        internal static ProcessModule GetUnityModule(Process process)
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

        private static IntPtr GetBaseAddressCore()
        {
            using (var process = Process.GetCurrentProcess())
            using (var module  = GetUnityModule(process))
            {
                return module.BaseAddress;
            }
        }
    }
}
