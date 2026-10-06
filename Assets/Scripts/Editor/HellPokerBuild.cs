using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HellPoker.Editor
{
    /// <summary>
    /// A playable Windows x64 build in Builds/Windows, packed for testers as Builds/HellPoker-&lt;version&gt;-win64.zip
    /// (menu: Hell Poker ▸ Build Windows, or batchmode: -executeMethod HellPoker.Editor.HellPokerBuild.Windows). The zip holds
    /// one folder, HellPoker-&lt;version&gt;, with the game (exe, _Data, dlls, MonoBleedingEdge — never Unity's
    /// *_DoNotShip debug folder) and the Turkish OKUBENI.txt / GERI_BILDIRIM.txt from Docs/Release ({VERSION} filled in).
    /// Batchmode exits with code 1 when the build fails.
    /// </summary>
    public static class HellPokerBuild
    {
        public const string WindowsFolder = "Builds/Windows";
        public const string WindowsExe = WindowsFolder + "/HellPoker.exe";
        public const string ReleaseTexts = "Docs/Release";

        /// <summary>A development build (F3 frame rate, -fpstour) kept apart from the one handed out.</summary>
        public const string DevelopmentFolder = "Builds/WindowsDev";

        /// <summary>Builds/HellPoker-0.1.6-win64.zip — or, for a demo, Builds/HellPoker-Demo-1.0-win64.zip.</summary>
        public static string ZipPath => $"Builds/HellPoker-{HellPoker.Core.Game.ReleaseVersion.FileName(PlayerSettings.bundleVersion)}-win64.zip";

        [MenuItem("Hell Poker/Build Windows")]
        public static void Windows()
        {
            if (!Build(WindowsFolder, BuildOptions.None)) return;
            try
            {
                Pack(WindowsFolder, ZipPath, PlayerSettings.bundleVersion);   // the notes say "Demo 1.0", the folder HellPoker-Demo-1.0
                Debug.Log($"Hell Poker packed: {Path.GetFullPath(ZipPath)} ({new FileInfo(ZipPath).Length / (1024 * 1024)} MB).");
            }
            catch (IOException exception)
            {
                Debug.LogError($"Hell Poker: the zip could not be written: {exception.Message}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        /// <summary>The build folder and the tester notes into one zip, under a folder named for the version.</summary>
        public static void Pack(string buildFolder, string zipPath, string version)
        {
            string root = $"HellPoker-{HellPoker.Core.Game.ReleaseVersion.FileName(version)}/";
            if (File.Exists(zipPath)) File.Delete(zipPath);
            using (ZipArchive zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                string full = Path.GetFullPath(buildFolder);
                foreach (string file in Directory.GetFiles(full, "*", SearchOption.AllDirectories))
                {
                    string relative = file.Substring(full.Length + 1).Replace('\\', '/');
                    if (relative.Split('/')[0].EndsWith("_DoNotShip")) continue;   // Burst's debug info stays home
                    zip.CreateEntryFromFile(file, root + relative, System.IO.Compression.CompressionLevel.Optimal);
                }
                foreach (string note in new[] { "OKUBENI.txt", "GERI_BILDIRIM.txt" })
                {
                    string text = File.ReadAllText(Path.Combine(ReleaseTexts, note)).Replace("{VERSION}",
                        HellPoker.Core.Game.ReleaseVersion.IsDemo(version) ? HellPoker.Core.Game.ReleaseVersion.Display(version) : version)
                        .Replace("{FOLDER}", HellPoker.Core.Game.ReleaseVersion.FileName(version));   // the folder the zip unpacks to
                    ZipArchiveEntry entry = zip.CreateEntry(root + note);
                    using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(true)))   // with a BOM, for Notepad
                        writer.Write(text.Replace("\r\n", "\n").Replace("\n", "\r\n"));
                }
            }
        }

        [MenuItem("Hell Poker/Build Windows (Development)")]
        public static void WindowsDevelopment() => Build(DevelopmentFolder, BuildOptions.Development);

        public const string StudioLogo = HellPokerArtImporter.SplashFolder + "caveman_logo.png";
        public const float StudioLogoSeconds = 2f;

        /// <summary>
        /// The splash screen: on, without Unity's logo, our studio logo (Caveman) alone on black for about two seconds,
        /// standing still (no zoom: pixel art). Applied before every build, so the settings cannot drift.
        /// </summary>
        [MenuItem("Hell Poker/Apply Splash Screen")]
        public static void ApplySplashScreen()
        {
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>(StudioLogo);
            if (logo == null)
            {
                Debug.LogError($"Hell Poker: no studio logo at {StudioLogo} (py Tools/ArtGen/generate_art.py splash).");
                return;
            }
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.backgroundColor = Color.black;
            PlayerSettings.SplashScreen.background = null;
            PlayerSettings.SplashScreen.overlayOpacity = 0f;
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Static;
            PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(StudioLogoSeconds, logo) };
            AssetDatabase.SaveAssets();
        }

        /// <returns>True when the build succeeded (batchmode exits with 1 when it did not).</returns>
        private static bool Build(string folder, BuildOptions buildOptions)
        {
            if (!File.Exists(HellPokerSceneBuilder.ScenePath))
                HellPokerSceneBuilder.Build();
            ApplySplashScreen();
            // A clean folder: nothing left over from an older build ends up in the zip.
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);

            string exe = folder + "/HellPoker.exe";
            var options = new BuildPlayerOptions
            {
                scenes = new[] { HellPokerSceneBuilder.ScenePath },
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = buildOptions
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Hell Poker built: {Path.GetFullPath(exe)} ({summary.totalSize / (1024 * 1024)} MB, {summary.totalTime.TotalSeconds:0} s).");
                return true;
            }

            Debug.LogError($"Hell Poker build {summary.result}: {summary.totalErrors} error(s).");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return false;
        }
    }
}
