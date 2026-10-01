using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HellPoker.Presentation;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// Not a real test: walks through the screens and saves 1920×1080 screenshots, to review the look without opening the editor.
    /// Explicit, so normal runs skip it. Run with:
    ///   -testPlatform PlayMode -testFilter HellPoker.PlayMode.Tests.HellPokerScreenshots
    /// Output: the folder in the HELLPOKER_SHOTS environment variable, or Screenshots/ in the project root.
    /// </summary>
    [Explicit, Category("Screenshots")]
    public class HellPokerScreenshots
    {
        private static readonly Vector2Int Size = new Vector2Int(1920, 1080);

        [UnityTest]
        public IEnumerator CaptureScreens()
        {
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);
            yield return Shot("01_menu");

            Press("HowToPlayButton");
            yield return Shot("02_rules");
            Press("BackButton");

            Press("NewGameButton");
            yield return new WaitForSeconds(1f);
            yield return Shot("03_dealers");

            Press("ChooseDealer0");
            yield return WaitForTable();
            yield return new WaitForSeconds(2.5f);
            yield return Shot("04_table_mammon");

            // Make the house re-raise every time, to see the Call / Fold answer (screenshot tool only).
            var bootstrap = Object.FindFirstObjectByType<HellPokerBootstrap>();
            var presenter = (TablePresenter)typeof(HellPokerBootstrap).GetField("_tablePresenter", Flags).GetValue(bootstrap);
            presenter.Game.GetType().GetField("_houseBetting", Flags).SetValue(presenter.Game, new AlwaysReRaise());

            Press("ActionButton");
            yield return WaitForTable();
            Press("RaiseButton");
            yield return WaitForTable();
            yield return Shot("05_decision");

            for (int guard = 0; guard < 10 && IsActive("PassButton"); guard++)
            {
                Press("PassButton");
                yield return WaitForTable();
            }

            Press(Find<Transform>("PlayerHand").GetComponentsInChildren<Button>()[1]);
            Press(Find<Transform>("PlayerHand").GetComponentsInChildren<Button>()[3]);
            yield return WaitForTable();
            yield return Shot("06_draw");

            Press("ActionButton");
            yield return WaitForTable();
            yield return Shot("07_after_draw");
            Press("RaiseButton");
            yield return WaitForTable();
            yield return new WaitForSeconds(1.5f);
            yield return Shot("07b_house_reraise");
            Press("CallButton");
            yield return WaitForTable();
            yield return Shot("07c_house_reveal");
            for (int guard = 0; guard < 10 && IsActive("PassButton"); guard++)
            {
                Press("PassButton");
                yield return WaitForTable();
            }

            yield return new WaitForSeconds(2.5f);
            yield return Shot("08_result");

            foreach (int dealer in new[] { 1, 2 })
            {
                Press("MenuButton");
                Press("NewGameButton");
                Press("ChooseDealer" + dealer);
                yield return WaitForTable();
                yield return new WaitForSeconds(2.5f);
                yield return Shot("09_table_dealer" + dealer);
            }

            // Final stretch: cut the sentence to 200 years behind the game's back (screenshot tool only).
            object ledger = presenter.Game.GetType().GetField("_ledger", Flags).GetValue(presenter.Game);
            ledger.GetType().GetMethod("Reset").Invoke(ledger, new object[] { 200 });
            typeof(TablePresenter).GetMethod("Refresh", Flags).Invoke(presenter, null);
            Press("ActionButton");
            yield return WaitForTable();
            yield return new WaitForSeconds(2.5f);
            yield return Shot("10_final_stretch");
        }

        private const System.Reflection.BindingFlags Flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        private sealed class AlwaysReRaise : HellPoker.Core.Betting.IHouseBettingStrategy
        {
            public bool WantsToReRaise(HellPoker.Core.Evaluation.HandEvaluation houseHand) => true;
        }

        private static IEnumerator Shot(string name)
        {
            // Overlay canvases do not render into cameras, so switch them to a camera that draws into a texture.
            var target = new RenderTexture(Size.x, Size.y, 24);
            var cameraObject = new GameObject("ScreenshotCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.targetTexture = target;

            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas).ToList();
            var modes = new List<(Canvas canvas, RenderMode mode)>();
            foreach (Canvas canvas in canvases)
            {
                modes.Add((canvas, canvas.renderMode));
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 10f;
            }

            yield return null;
            Canvas.ForceUpdateCanvases();
            camera.Render();

            RenderTexture.active = target;
            var texture = new Texture2D(Size.x, Size.y, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Size.x, Size.y), 0, 0);
            texture.Apply();
            RenderTexture.active = null;

            string folder = Environment.GetEnvironmentVariable("HELLPOKER_SHOTS");
            if (string.IsNullOrEmpty(folder))
                folder = Path.Combine(Application.dataPath, "..", "Screenshots");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), texture.EncodeToPNG());

            foreach (var (canvas, mode) in modes)
            {
                canvas.renderMode = mode;
                canvas.worldCamera = null;
            }

            Object.Destroy(texture);
            Object.Destroy(cameraObject);
            target.Release();
        }

        private static IEnumerator WaitForTable()
        {
            var table = Object.FindFirstObjectByType<TableView>();
            float started = Time.time;
            yield return null;
            while (table.IsBusy && Time.time - started < 10f)
                yield return null;
        }

        private static void Press(string name) => Press(Find<Button>(name));

        private static void Press(Button button)
        {
            Assert.IsTrue(button.gameObject.activeInHierarchy && button.interactable, $"{button.name} cannot be pressed.");
            button.onClick.Invoke();
        }

        private static bool IsActive(string name) => Find<Button>(name).gameObject.activeInHierarchy;

        private static T Find<T>(string name) where T : Component
        {
            T found = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c.name == name);
            Assert.IsNotNull(found, $"'{name}' not found in scene.");
            return found;
        }
    }
}
