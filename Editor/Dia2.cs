using System;
using System.Runtime.InteropServices;
using Dia2Lib;

namespace NewBlood
{
    static unsafe class Dia2
    {
        [DllImport("msdia140", PreserveSig = false)]
        static extern void DllGetClassObject([In] Guid rclsid, [In] Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

        public static IDiaDataSource CreateDataSource()
        {
            object ppv;
            DllGetClassObject(typeof(DiaSourceClass).GUID, typeof(IClassFactory).GUID, out ppv);
            var factory = (IClassFactory)ppv;
            factory.CreateInstance(null, typeof(IDiaDataSource).GUID, out ppv);
            return (IDiaDataSource)ppv;
        }
    }
}
