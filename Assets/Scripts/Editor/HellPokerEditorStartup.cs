using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HellPoker.Editor
{
    /// <summary>
    /// Opens the game scene when the editor starts on an empty untitled scene (once per session).
    /// Batchmode runs (tests, builds) reset the editor's "last opened scene", and pressing Play on the untitled
    /// scene only shows the default blue camera background.
    /// </summary>
    /// <remarks>
    /// Deliberately does not set <c>EditorSceneManager.playModeStartScene</c>: that redirects the Test Runner's own
    /// PlayMode scene too and makes PlayMode tests hang.
    /// </remarks>
    [InitializeOnLoad]
    public static class HellPokerEditorStartup
    {
        private const string OpenedOnStartupKey = "HellPoker.OpenedSceneOnStartup";

        static HellPokerEditorStartup()
        {
            // An earlier version forced the Play start scene; clear it so the Test Runner works again.
            if (EditorSceneManager.playModeStartScene != null
                && AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) == HellPokerSceneBuilder.ScenePath)
                EditorSceneManager.playModeStartScene = null;

            if (Application.isBatchMode) return;
            EditorApplication.delayCall += OpenGameSceneIfUntitled;
        }

        private static void OpenGameSceneIfUntitled()
        {
            if (SessionState.GetBool(OpenedOnStartupKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool(OpenedOnStartupKey, true);

            Scene active = SceneManager.GetActiveScene();
            bool untitledAndUntouched = string.IsNullOrEmpty(active.path) && !active.isDirty;
            if (untitledAndUntouched && System.IO.File.Exists(HellPokerSceneBuilder.ScenePath))
                EditorSceneManager.OpenScene(HellPokerSceneBuilder.ScenePath);
        }
    }
}
