#if UNITY_EDITOR && UNITY_2019_3_OR_NEWER
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;

namespace NewBlood
{
    sealed class LinkerProcessor : IUnityLinkerProcessor
    {
        int IOrderedCallback.callbackOrder => 0;

        const string LinkerFileGuid = "bcbe54df6774f9148b1eb4362fcdfed4";

        string IUnityLinkerProcessor.GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            return Path.GetFullPath(AssetDatabase.GUIDToAssetPath(LinkerFileGuid));
        }

    #if !UNITY_2021_2_OR_NEWER
        void IUnityLinkerProcessor.OnBeforeRun(BuildReport report, UnityLinkerBuildPipelineData data)
        {
        }

        void IUnityLinkerProcessor.OnAfterRun(BuildReport report, UnityLinkerBuildPipelineData data)
        {
        }
    #endif
    }
}
#endif
