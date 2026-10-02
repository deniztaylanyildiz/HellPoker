using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HellPoker.Editor
{
    /// <summary>
    /// A playable Windows x64 build in Builds/Windows (menu: Hell Poker ▸ Build Windows, or batchmode:
    /// -executeMethod HellPoker.Editor.HellPokerBuild.Windows). Batchmode exits with code 1 when the build fails.
    /// </summary>
    public static class HellPokerBuild
    {
        public const string WindowsFolder = "Builds/Windows";
        public const string WindowsExe = WindowsFolder + "/HellPoker.exe";

        [MenuItem("Hell Poker/Build Windows")]
        public static void Windows()
        {
            if (!File.Exists(HellPokerSceneBuilder.ScenePath))
                HellPokerSceneBuilder.Build();

            var options = new BuildPlayerOptions
            {
                scenes = new[] { HellPokerSceneBuilder.ScenePath },
                locationPathName = WindowsExe,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Hell Poker built: {Path.GetFullPath(WindowsExe)} ({summary.totalSize / (1024 * 1024)} MB, {summary.totalTime.TotalSeconds:0} s).");
                return;
            }

            Debug.LogError($"Hell Poker build {summary.result}: {summary.totalErrors} error(s).");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
