using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SurfPool.EditorTools
{
    /// <summary>
    /// Builds the downloadable Mac app to Builds/Mac/Surf Pool.app (Apple Silicon; Intel is deprecated in Unity 6.6 and needs the llvm-lipo step, which fails here).
    /// The product name is "Surf Pool" only for the build, so the app gets a nice name and its own save folder;
    /// the Editor keeps "surf-pool-sim" (that name is where Ido's save and settings live).
    /// Zip it for a release with: ditto -c -k --keepParent "Builds/Mac/Surf Pool.app" SurfPool-mac.zip
    /// </summary>
    public static class ReleaseBuild
    {
        public const string MacApp = "Builds/Mac/Surf Pool.app";

        [MenuItem("Surf Pool/Build Mac Release")]
        public static void BuildMacMenu() => Debug.Log(BuildMac());

        public static string BuildMac()
        {
            string oldName = PlayerSettings.productName;
            var oldArch = UnityEditor.OSXStandalone.UserBuildSettings.architecture;
            try
            {
                PlayerSettings.productName = "Surf Pool";
                UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.ARM64;
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/_Game/Scenes/Pool.unity" },
                    locationPathName = MacApp,
                    target = BuildTarget.StandaloneOSX,
                    options = BuildOptions.None,
                };
                BuildReport report = BuildPipeline.BuildPlayer(options);
                var s = report.summary;
                return $"{s.result}: {s.totalErrors} errors, {s.totalSize / (1024 * 1024)} MB, {s.totalTime.TotalSeconds:0}s -> {MacApp}";
            }
            finally
            {
                PlayerSettings.productName = oldName;
                UnityEditor.OSXStandalone.UserBuildSettings.architecture = oldArch;
                AssetDatabase.SaveAssets(); // the build saved "Surf Pool" into ProjectSettings.asset; write the old name back
            }
        }
    }
}
