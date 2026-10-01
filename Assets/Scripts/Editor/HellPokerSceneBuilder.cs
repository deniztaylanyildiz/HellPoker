using System.Linq;
using HellPoker.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HellPoker.Editor
{
    /// <summary>
    /// Creates the main game scene (camera + bootstrap) and puts it first in the build settings.
    /// Menu: Hell Poker ▸ Build Main Scene. Batch: -executeMethod HellPoker.Editor.HellPokerSceneBuilder.Build
    /// </summary>
    public static class HellPokerSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/HellPoker.unity";

        [MenuItem("Hell Poker/Build Main Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.015f, 0.015f);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            new GameObject("HellPoker").AddComponent<HellPokerBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            Debug.Log($"Hell Poker scene built at {ScenePath}.");
        }
    }
}
