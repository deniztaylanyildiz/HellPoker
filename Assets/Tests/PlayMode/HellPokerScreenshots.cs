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
    /// The PlayMode tests' helpers: a screenshot (<see cref="Shot"/>: 1920×1080 or HELLPOKER_SHOT_SIZE, into HELLPOKER_SHOTS or Screenshots/),
    /// pressing a button as the player would, finding one, waiting for the table. (Demo 3.4.1: its own screenshot walk was the Boss
    /// Simulator's and went with it; the game's screens are shot by the Demo*Shots, PhaseThreeShots, HowToShots and ChapterScreenshots.)
    /// </summary>
    public static class HellPokerScreenshots
    {
        /// <summary>1920×1080, or HELLPOKER_SHOT_SIZE ("2560x1440": ×5, an odd scale).</summary>
        private static Vector2Int Size
        {
            get
            {
                string[] parts = (System.Environment.GetEnvironmentVariable("HELLPOKER_SHOT_SIZE") ?? "").Split('x');
                return parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h) ? new Vector2Int(w, h) : new Vector2Int(1920, 1080);
            }
        }

        internal const System.Reflection.BindingFlags Flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        /// <summary>Screenshot tool only: rewrites the sentence between hands and refreshes the table.</summary>
        internal static void SetSentence(TablePresenter presenter, int years)
        {
            object ledger = presenter.Game.GetType().GetField("_ledger", Flags).GetValue(presenter.Game);
            ledger.GetType().GetMethod("Reset").Invoke(ledger, new object[] { years });
            if (presenter.Game.IsGameOver)
                presenter.Game.GetType().GetProperty("Phase").SetValue(presenter.Game, HellPoker.Core.Game.GamePhase.RoundOver);
            typeof(TablePresenter).GetMethod("Refresh", Flags).Invoke(presenter, null);
        }

        /// <summary>Passes every decision, stands pat, calls any re-raise, until the hand is settled.</summary>
        internal static IEnumerator PlayToShowdown()
        {
            for (int guard = 0; guard < 20; guard++)
            {
                if (IsActive("PassButton") && !IsLocked("PassButton")) Press("PassButton");
                else if (IsActive("RaiseButton") && !IsLocked("RaiseButton") && !IsActive("ActionButton")) Press("RaiseButton");
                else if (IsActive("CallButton")) Press("CallButton");
                else if (ActionLabelIs(HellPoker.Presentation.Ui.UiText.Stand)) Press("ActionButton");
                else break;
                yield return WaitForTable();
            }
        }

        internal static bool ActionLabelIs(string label)
        {
            Button action = Find<Button>("ActionButton");
            return action.gameObject.activeInHierarchy && action.GetComponentInChildren<Text>().text == label;
        }

        internal static IEnumerator Shot(string name)
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

        internal static IEnumerator WaitForTable()
        {
            var table = ChapterJourneyTests.ChapterTableView;
            float started = Time.time;
            yield return null;
            while (table != null && table.IsBusy && Time.time - started < 10f)
                yield return null;
        }

        internal static void Press(string name) => Press(Find<Button>(name));

        internal static void Press(Button button)
        {
            Assert.IsTrue(button.gameObject.activeInHierarchy && button.interactable, $"{button.name} cannot be pressed.");
            button.onClick.Invoke();
            // New Game over a run asks first: the tests mean it.
            if (button.name == "NewGameButton") ConfirmNewGame();
        }

        internal static void ChooseSinner(int index)
        {
            Button choose = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name == "ChooseSinner" + index && b.gameObject.activeInHierarchy);
            if (choose != null) choose.onClick.Invoke();
        }

        internal static void ConfirmNewGame()
        {
            Button confirm = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name == "MenuConfirmButton" && b.gameObject.activeInHierarchy);
            if (confirm != null) confirm.onClick.Invoke();
        }

        internal static bool IsActive(string name) => Find<Button>(name).gameObject.activeInHierarchy;

        /// <summary>A locked button still takes clicks (it answers with the reason), so check the look, not interactable.</summary>
        internal static bool IsLocked(string name) => Find<HellPoker.Presentation.Ui.ButtonFeel>(name).Locked;

        internal static T Find<T>(string name) where T : Component
        {
            // Prefer a live object: replaced dealer cards linger (inactive) until the end of the frame.
            T found = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(c => c.name == name).OrderByDescending(c => c.gameObject.activeInHierarchy).FirstOrDefault();
            Assert.IsNotNull(found, $"'{name}' not found in scene.");
            return found;
        }
    }
}
