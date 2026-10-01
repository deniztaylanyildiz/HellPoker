using UnityEditor;
using UnityEditor.SceneManagement;

namespace HellPoker.Editor
{
    /// <summary>Menu: Hell Poker ▸ Play — opens the game scene (saving changes first) and enters Play mode.</summary>
    public static class HellPokerMenu
    {
        [MenuItem("Hell Poker/Play %#p")]
        public static void Play()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (!System.IO.File.Exists(HellPokerSceneBuilder.ScenePath))
                HellPokerSceneBuilder.Build();

            EditorSceneManager.OpenScene(HellPokerSceneBuilder.ScenePath);
            EditorApplication.isPlaying = true;
        }
    }
}
