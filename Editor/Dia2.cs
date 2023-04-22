﻿using System;
using System.Runtime.InteropServices;
using Dia2Lib;

namespace NewBlood
{
    internal static unsafe class Dia2
    {
    #if UNITY_EDITOR_WIN
        [DllImport("msdia140", PreserveSig = false)]
        private static extern void DllGetClassObject([In] Guid rclsid, [In] Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
    #endif

        public static IDiaDataSource CreateDataSource()
        {
        #if UNITY_EDITOR_WIN
            object ppv;
            DllGetClassObject(typeof(DiaSourceClass).GUID, typeof(IClassFactory).GUID, out ppv);
            var factory = (IClassFactory)ppv;
            factory.CreateInstance(null, typeof(IDiaDataSource).GUID, out ppv);
            return (IDiaDataSource)ppv;
        #else
            throw new NotSupportedException();
        #endif
        }
    }
}
