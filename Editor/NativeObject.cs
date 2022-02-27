using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NewBlood
{
    /// <summary>Provides APIs for managing native Unity objects.</summary>
    public static unsafe class NativeObject
    {
        static readonly int s_OffsetOfInstanceIDInCPlusPlusObject = (int)typeof(Object)
            .GetMethod("GetOffsetOfInstanceIDInCPlusPlusObject", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, null);

        /// <summary>Gets the instance ID of the provided native object.</summary>
        public static int GetInstanceID(IntPtr address)
        {
            return *(int*)IntPtr.Add(address, s_OffsetOfInstanceIDInCPlusPlusObject);
        }

        /// <summary>Gets the managed wrapper object associated with the provided native object.</summary>
        public static Object GetManagedObject(IntPtr address)
        {
            return Resources.InstanceIDToObject(GetInstanceID(address));
        }
    }
}
