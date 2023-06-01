using System;
using System.Runtime.InteropServices;

namespace NewBlood
{
    [ComImport]
    [Guid("00000001-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe interface IClassFactory
    {
        void CreateInstance(void* outer, Guid* interfaceId, void** instance);
        void LockServer(byte @lock);
    }
}
