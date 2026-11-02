using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SurfPool.EditorTools
{
    /// <summary>
    /// Builds the downloadable apps: Builds/Mac/Surf Pool.app (Apple Silicon; Intel is deprecated in Unity 6.6 and needs the llvm-lipo step, which fails here)
    /// and Builds/Windows/Surf Pool.exe (64-bit, Mono, cross-built on the Mac with the Windows Build Support module).
    /// The product name is "Surf Pool" only for the build, so the app gets a nice name and its own save folder;
    /// the Editor keeps "surf-pool-sim" (that name is where Ido's save and settings live).
    /// Zip them for a release with:
    ///   ditto -c -k --sequesterRsrc --keepParent "Builds/Mac/Surf Pool.app" SurfPool-mac.zip
    ///   rsync -a --exclude "*_BackUpThisFolder_ButDontShipItWithYourGame" Builds/Windows/ "stage/Surf Pool/" && cd stage && zip -qr -X ../SurfPool-windows.zip "Surf Pool"
    /// (a new module like Windows Build Support only shows up after an Editor restart: "Build target not supported" until then)
    /// </summary>
    public static class ReleaseBuild
    {
        public const string MacApp = "Builds/Mac/Surf Pool.app";
        public const string WindowsExe = "Builds/Windows/Surf Pool.exe";

        [MenuItem("Surf Pool/Build Mac Release")]
        public static void BuildMacMenu() => Debug.Log(BuildMac());

        [MenuItem("Surf Pool/Build Windows Release")]
        public static void BuildWindowsMenu() => Debug.Log(BuildWindows());

        public static string BuildMac()
        {
            var oldArch = UnityEditor.OSXStandalone.UserBuildSettings.architecture;
            try
            {
                UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.ARM64;
                return Build(MacApp, BuildTarget.StandaloneOSX);
            }
            finally
            {
                UnityEditor.OSXStandalone.UserBuildSettings.architecture = oldArch;
            }
        }

        public static string BuildWindows()
        {
            // the Windows folder is zipped whole, so clear out files from an older build
            string dir = System.IO.Path.GetDirectoryName(WindowsExe);
            if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
            return Build(WindowsExe, BuildTarget.StandaloneWindows64);
        }

        static string Build(string path, BuildTarget target)
        {
            string oldName = PlayerSettings.productName;
            try
            {
                PlayerSettings.productName = "Surf Pool";
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/_Game/Scenes/Pool.unity" },
                    locationPathName = path,
                    target = target,
                    options = BuildOptions.None,
                };
                BuildReport report = BuildPipeline.BuildPlayer(options);
                var s = report.summary;
                return $"{s.result}: {s.totalErrors} errors, {s.totalSize / (1024 * 1024)} MB, {s.totalTime.TotalSeconds:0}s -> {path}";
            }
            finally
            {
                PlayerSettings.productName = oldName;
                AssetDatabase.SaveAssets(); // the build saved "Surf Pool" into ProjectSettings.asset; write the old name back
            }
        }
    }
}
