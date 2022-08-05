﻿using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NewBlood
{
    /// <summary>Provides APIs for managing native Unity objects.</summary>
    public static unsafe class NativeObjectUtility
    {
        static readonly int s_OffsetOfInstanceIDInCPlusPlusObject = (int)typeof(Object)
            .GetMethod("GetOffsetOfInstanceIDInCPlusPlusObject", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, null);

        static readonly Func<Object, IntPtr> s_GetCachedPtr = (Func<Object, IntPtr>)typeof(Object)
            .GetMethod("GetCachedPtr", BindingFlags.NonPublic | BindingFlags.Instance)
            .CreateDelegate(typeof(Func<Object, IntPtr>));

        /// <summary>Gets the instance ID of the provided native object.</summary>
        public static int GetInstanceID(NativeObject* native)
        {
            return *(int*)IntPtr.Add((IntPtr)native, s_OffsetOfInstanceIDInCPlusPlusObject);
        }

    #if UNITY_2020_2_OR_NEWER
        /// <summary>Gets the managed wrapper associated with the provided native object.</summary>
        public static Object GetManagedObject(NativeObject* native)
        {
            return Resources.InstanceIDToObject(GetInstanceID(native));
        }
    #elif UNITY_EDITOR
        /// <summary>Gets the managed wrapper associated with the provided native object.</summary>
        public static Object GetManagedObject(NativeObject* native)
        {
            return UnityEditor.EditorUtility.InstanceIDToObject(GetInstanceID(native));
        }
    #endif

        /// <summary>Returns the underlying native object from a managed wrapper.</summary>
        public static NativeObject* GetNativeObject(Object managed)
        {
            return (NativeObject*)s_GetCachedPtr(managed);
        }
    }
}
