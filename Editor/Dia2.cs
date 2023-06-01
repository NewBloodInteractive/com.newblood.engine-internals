using System;
using System.Runtime.InteropServices;
using Dia2Lib;

namespace NewBlood
{
    internal static unsafe class Dia2
    {
    #if UNITY_EDITOR_WIN
        [DllImport("msdia140", PreserveSig = false)]
        private static extern void DllGetClassObject(Guid* rclsid, Guid* riid, void** ppv);
    #endif

        public static IDiaDataSource CreateDataSource()
        {
        #if UNITY_EDITOR_WIN
            IntPtr ppv;
            var clsid = typeof(DiaSourceClass).GUID;
            var iid = typeof(IClassFactory).GUID;
            DllGetClassObject(&clsid, &iid, (void**)&ppv);
            var factory = (IClassFactory)Marshal.GetObjectForIUnknown(ppv);
            iid = typeof(IDiaDataSource).GUID;
            factory.CreateInstance(null, &iid, (void**)&ppv);
            return (IDiaDataSource)Marshal.GetObjectForIUnknown(ppv);
        #else
            throw new NotSupportedException();
        #endif
        }
    }
}
