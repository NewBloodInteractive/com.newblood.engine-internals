using System;
using System.Runtime.InteropServices;

namespace NewBlood
{
    static unsafe class DbgHelp
    {
        public const int SYMOPT_IGNORE_CVREC = 0x00000080;

        [DllImport("DbgHelp", ExactSpelling = true, SetLastError = true)]
        public static extern int SymInitializeW(IntPtr hProcess, ushort* UserSearchPath, int fInvadeProcess);

        [DllImport("DbgHelp", ExactSpelling = true, SetLastError = true)]
        public static extern int SymCleanup(IntPtr hProcess);

        [DllImport("DbgHelp", ExactSpelling = true, SetLastError = true)]
        public static extern int SymFromNameW(IntPtr hProcess, ushort* Name, SYMBOL_INFOW* Symbol);

        [DllImport("DbgHelp", ExactSpelling = true, SetLastError = true)]
        public static extern uint SymSetOptions(uint SymOptions);
    }
}
