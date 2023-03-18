#if UNITY_EDITOR_WIN
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace NewBlood
{
    internal sealed class BuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.StandaloneWindows &&
                report.summary.platform != BuildTarget.StandaloneWindows64)
            {
                // Non-Windows not supported yet.
                AssemblyPostProcessor.BuildVariantExecutable = null;
                return;
            }

            var target = BuildPipeline.GetPlaybackEngineDirectory(report.summary.platformGroup, report.summary.platform, report.summary.options);
        #if UNITY_2021_2_OR_NEWER
            var backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.FromBuildTargetGroup(report.summary.platformGroup));
        #else
            var backend = PlayerSettings.GetScriptingBackend(report.summary.platformGroup);
        #endif
            var variant = GetVariationName(report.summary.platform, report.summary.options, backend);
            AssemblyPostProcessor.BuildVariantExecutable = Path.Combine(target, "Variations", variant, "UnityPlayer.dll");
        }

        private static string GetVariationName(BuildTarget target, BuildOptions options, ScriptingImplementation backend)
        {
            string name;

            if (target == BuildTarget.StandaloneWindows)
                name = "win32_";
            else if (target == BuildTarget.StandaloneWindows64)
                name = "win64_";
            else
                throw new NotSupportedException();

        #if UNITY_2021_1_OR_NEWER
            name += "player_";
        #endif

            if (options.HasFlag(BuildOptions.Development))
                name += "development_";
            else
                name += "nondevelopment_";

            if (backend == ScriptingImplementation.IL2CPP)
                name += "il2cpp";
            else
                name += "mono";

            return name;
        }
    }
}
#endif
