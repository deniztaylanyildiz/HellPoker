using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.Build;
#endif

namespace HellPoker.Editor
{
    /// <summary>
    /// Hell Poker ▸ İkonu Ayarla: the pixel demon (Assets/Art/Icon, made by Tools/make_icon.py) becomes the default icon and fills every
    /// size of the PC, Mac &amp; Linux Standalone override — each size gets the PNG of that size, or the nearest larger one.
    /// </summary>
    public static class HellPokerIconSetter
    {
        private const string FilePrefix = "hellpoker-icon-";
        private const int DefaultIconSize = 1024;

        /// <summary>The Standalone override's slots, for when the editor reports none (batchmode answers an empty list).</summary>
        private static readonly int[] StandaloneSizes = { 1024, 512, 256, 128, 48, 32, 16 };

        [MenuItem("Hell Poker/İkonu Ayarla")]
        public static void SetIcons()
        {
            Dictionary<int, Texture2D> icons = LoadIcons();
            if (!icons.TryGetValue(DefaultIconSize, out Texture2D defaultIcon))
            {
                Debug.LogError($"Hell Poker: {HellPokerIconImporter.IconFolder}{FilePrefix}{DefaultIconSize}.png is missing — run `py Tools/make_icon.py` first.");
                return;
            }

            Debug.Log($"Hell Poker: Unity {ProjectUnityVersion()}; default icon = {defaultIcon.name}");
            int[] sizes = StandaloneIconSizes();
            Debug.Log($"Hell Poker: standalone wants {sizes.Length} sizes: {string.Join(", ", sizes)}");
            var textures = new Texture2D[sizes.Length];
            for (int i = 0; i < sizes.Length; i++)
            {
                textures[i] = Pick(icons, sizes[i]);
                Debug.Log($"Hell Poker: standalone {sizes[i]}x{sizes[i]} <- {textures[i].name}.png");
            }
            ApplyIcons(defaultIcon, textures);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Every hellpoker-icon-&lt;size&gt;.png in the icon folder, by size.</summary>
        private static Dictionary<int, Texture2D> LoadIcons()
        {
            var icons = new Dictionary<int, Texture2D>();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { HellPokerIconImporter.IconFolder.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                if (name.StartsWith(FilePrefix) && int.TryParse(name.Substring(FilePrefix.Length), out int size))
                    icons[size] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            return icons;
        }

        /// <summary>The PNG of exactly this size, otherwise the nearest larger one, otherwise the largest there is.</summary>
        private static Texture2D Pick(Dictionary<int, Texture2D> icons, int size)
        {
            int best = icons.Keys.Where(s => s >= size).DefaultIfEmpty(icons.Keys.Max()).Min();
            return icons[best];
        }

        /// <summary>The editor version from ProjectSettings/ProjectVersion.txt (for the log; the API is chosen at compile time).</summary>
        private static string ProjectUnityVersion()
        {
            const string file = "ProjectSettings/ProjectVersion.txt";
            if (!File.Exists(file)) return Application.unityVersion;
            string line = File.ReadAllLines(file).FirstOrDefault(l => l.StartsWith("m_EditorVersion:"));
            return line != null ? line.Substring("m_EditorVersion:".Length).Trim() : Application.unityVersion;
        }

#if UNITY_2021_2_OR_NEWER
        private static int[] StandaloneIconSizes()
        {
            int[] sizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Any);
            return sizes.Length > 0 ? sizes : StandaloneSizes;
        }

        private static void ApplyIcons(Texture2D defaultIcon, Texture2D[] standalone)
        {
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { defaultIcon }, IconKind.Any);
            PlayerSettings.SetIcons(NamedBuildTarget.Standalone, standalone, IconKind.Any);
        }
#else
        private static int[] StandaloneIconSizes() => PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Standalone);

        private static void ApplyIcons(Texture2D defaultIcon, Texture2D[] standalone)
        {
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { defaultIcon });
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, standalone);
        }
#endif
    }
}
