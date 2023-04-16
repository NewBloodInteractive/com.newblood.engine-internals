using System.Runtime.InteropServices;
using UnityEngine;

namespace NewBlood
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct NativeObject
    {
        public static NativeObject* GetPointer(Object managed)
        {
            return NativeObjectUtility.GetNativeObject(managed);
        }

        public int GetInstanceID()
        {
            fixed (NativeObject* pThis = &this)
            {
                return NativeObjectUtility.GetInstanceID(pThis);
            }
        }

        public Object GetObject()
        {
            fixed (NativeObject* pThis = &this)
            {
                return NativeObjectUtility.GetManagedObject(pThis);
            }
        }
    }
}
