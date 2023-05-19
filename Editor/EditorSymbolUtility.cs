using Dia2Lib;
using System;
using System.Diagnostics;

namespace NewBlood
{
    public static class EditorSymbolUtility
    {
        private static readonly IDiaDataSource _source;

        private static readonly IDiaSession _session;

        private static readonly IDiaSymbol _globalScope;

        static EditorSymbolUtility()
        {
            _source = Dia2.CreateDataSource();

            using (var process = Process.GetCurrentProcess())
            using (var module  = UnityModuleHelpers.GetUnityModule(process))
            {
                _source.loadDataForExe(module.FileName, null, null);
                _source.openSession(out _session);
                _session.loadAddress = (ulong)module.BaseAddress;
            }

            _globalScope = _session.globalScope;
        }

        public static bool TryResolveSymbol(string name, out IntPtr address)
        {
            IDiaEnumSymbols enumerator;
            _globalScope.findChildren(SymTagEnum.SymTagPublicSymbol, name, 0, out enumerator);

            if (enumerator.count <= 0)
            {
                address = IntPtr.Zero;
                return false;
            }

            var symbol = enumerator.Item(0);
            address    = (IntPtr)symbol.virtualAddress;
            return true;
        }
    }
}
