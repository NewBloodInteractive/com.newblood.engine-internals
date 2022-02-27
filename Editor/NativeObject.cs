using System;
using System.Reflection;
using UnityEditor;
using Object = UnityEngine.Object;

namespace NewBlood
{
    /// <summary>Provides APIs for managing native Unity objects.</summary>
    public static unsafe class NativeObject
    {
        static readonly int s_OffsetOfInstanceIDInCPlusPlusObject = (int)typeof(Object)
            .GetMethod("GetOffsetOfInstanceIDInCPlusPlusObject", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, null);

        static readonly Func<Object, IntPtr> s_GetCachedPtr = (Func<Object, IntPtr>)typeof(Object)
            .GetMethod("GetCachedPtr", BindingFlags.NonPublic | BindingFlags.Instance)
            .CreateDelegate(typeof(Func<Object, IntPtr>));

        /// <summary>Gets the instance ID of the provided native object.</summary>
        public static int GetInstanceID(IntPtr native)
        {
            return *(int*)IntPtr.Add(native, s_OffsetOfInstanceIDInCPlusPlusObject);
        }

        /// <summary>Gets the managed wrapper associated with the provided native object.</summary>
        public static Object GetManagedObject(IntPtr native)
        {
            return EditorUtility.InstanceIDToObject(GetInstanceID(native));
        }

        /// <summary>Returns the underlying native object from a managed wrapper.</summary>
        public static IntPtr GetNativeObject(Object managed)
        {
            return s_GetCachedPtr(managed);
        }
    }
}
