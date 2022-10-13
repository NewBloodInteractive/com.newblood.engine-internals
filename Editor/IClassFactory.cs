using System;
using System.Runtime.InteropServices;

namespace NewBlood
{
    [ComImport]
    [Guid("00000001-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IClassFactory
    {
        void CreateInstance([MarshalAs(UnmanagedType.IUnknown)] object outer, [In] Guid interfaceId, [MarshalAs(UnmanagedType.IUnknown)] out object instance);
        void LockServer(bool @lock);
    }
}
