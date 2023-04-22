using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NewBlood
{
    /// <summary>Provides APIs for managing native Unity objects.</summary>
    public static unsafe class NativeObjectUtility
    {
        private static readonly int s_OffsetOfInstanceIDInCPlusPlusObject = (int)typeof(Object)
            .GetMethod("GetOffsetOfInstanceIDInCPlusPlusObject", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, null);

        private static readonly Func<Object, IntPtr> s_GetCachedPtr = (Func<Object, IntPtr>)typeof(Object)
            .GetMethod("GetCachedPtr", BindingFlags.NonPublic | BindingFlags.Instance)
            .CreateDelegate(typeof(Func<Object, IntPtr>));

        /// <summary>Gets the instance ID of the provided native object.</summary>
        public static int GetInstanceID(NativeObject* native)
        {
            if (native == null)
                ThrowArgumentNullException(nameof(native));

            return *(int*)((byte*)native + s_OffsetOfInstanceIDInCPlusPlusObject);
        }

        /// <summary>Gets the managed wrapper associated with the provided native object.</summary>
        public static Object GetManagedObject(NativeObject* native)
        {
        #if UNITY_2020_2_OR_NEWER
            return Resources.InstanceIDToObject(GetInstanceID(native));
        #elif UNITY_EDITOR
            return UnityEditor.EditorUtility.InstanceIDToObject(GetInstanceID(native));
        #else
            // TODO: Is it possible to implement this with a SymbolImport?
            throw new NotSupportedException();
        #endif
        }

        /// <summary>Returns the underlying native object from a managed wrapper.</summary>
        public static NativeObject* GetNativeObject(Object managed)
        {
            // We only care about true null here, a destroyed object is okay.
            if (ReferenceEquals(managed, null))
                ThrowArgumentNullException(nameof(managed));

            return (NativeObject*)s_GetCachedPtr(managed);
        }

        private static void ThrowArgumentNullException(string paramName)
        {
            throw new ArgumentNullException(paramName);
        }
    }
}
