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

        [UnityTest, Timeout(600000)]
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
            foreach (string id in new[] { "belial", "lilith", "mammon" })
            {
                Press(Find<Transform>("Dealer_" + id).GetComponentInChildren<Button>());   // the portrait selects
                yield return new WaitForSeconds(0.6f);
                if (id != "mammon")
                    yield return Shot("03_dealers_" + id);
            }

            Press("ChooseDealer0");
            yield return WaitForTable();
            yield return new WaitForSeconds(2.5f);
            yield return Shot("04_table_mammon");

            // Screenshot tool only: a 650-year sentence (unit 50, cap 195) leaves room under the cap for a house re-raise,
            // and the house re-raises every time it can, to show the Call / Fold answer.
            var bootstrap = Object.FindFirstObjectByType<HellPokerBootstrap>();
            var presenter = (TablePresenter)typeof(HellPokerBootstrap).GetField("_tablePresenter", Flags).GetValue(bootstrap);
            presenter.Game.GetType().GetField("_houseBetting", Flags).SetValue(presenter.Game, new AlwaysReRaise());
            SetSentence(presenter, 650);

            Press("ActionButton");
            yield return WaitForTable();
            yield return new WaitForSeconds(0.5f);
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
            yield return new WaitForSeconds(0.4f);
            yield return Shot("07b_house_reraise");
            Press("CallButton");
            yield return WaitForTable();
            yield return Shot("07c_house_reveal");
            for (int guard = 0; guard < 10 && IsActive("PassButton"); guard++)
            {
                Press("PassButton");
                yield return WaitForTable();
            }
            yield return new WaitForSeconds(0.3f);
            yield return Shot("08_result_first");

            // Keep playing plain hands until both a win and a loss have been on screen.
            bool won = false, lost = false;
            for (int hand = 0; hand < 30 && !(won && lost); hand++)
            {
                if (presenter.Game.IsGameOver || presenter.Game.Years < 300)
                {
                    SetSentence(presenter, 1000);
                    yield return WaitForTable();
                }
                Press("ActionButton");   // next hand / play again
                yield return WaitForTable();
                if (presenter.Game.Phase == HellPoker.Core.Game.GamePhase.Betting)
                {
                    Press("ActionButton");   // deal
                    yield return WaitForTable();
                }
                yield return PlayToShowdown();

                var outcome = presenter.Game.LastRound?.Showdown?.Outcome;
                if (outcome == HellPoker.Core.Game.ShowdownOutcome.PlayerWins && !won)
                {
                    won = true;
                    yield return new WaitForSeconds(0.5f);
                    yield return Shot("08_result_win");
                }
                else if (outcome == HellPoker.Core.Game.ShowdownOutcome.HouseWins && !lost)
                {
                    lost = true;
                    yield return new WaitForSeconds(0.5f);
                    yield return Shot("08_result_loss");
                }
            }

            // Every hall: betting, a decision, and the final stretch (sentence cut to 200 behind the game's back).
            string[] halls = { "mammon", "belial", "lilith" };
            for (int dealer = 0; dealer < halls.Length; dealer++)
            {
                Press("MenuButton");
                Press("NewGameButton");
                Press("ChooseDealer" + dealer);
                yield return WaitForTable();
                yield return new WaitForSeconds(1.5f);
                yield return Shot($"09_{halls[dealer]}_table");

                Press("ActionButton");
                yield return WaitForTable();
                yield return new WaitForSeconds(0.5f);
                yield return Shot($"09_{halls[dealer]}_decision");

                Press("FoldButton");
                yield return WaitForTable();
                SetSentence(presenter, 200);
                yield return WaitForTable();
                Press("ActionButton");   // next hand
                yield return WaitForTable();
                Press("ActionButton");   // deal
                yield return WaitForTable();
                yield return new WaitForSeconds(1.5f);
                yield return Shot($"10_{halls[dealer]}_final_stretch");
            }

            yield return CaptureSoul(presenter);
        }

        /// <summary>The soul: going on the table, the locked exit, a soul hand with a re-raise, a loss, a rescue, and the trap seat.</summary>
        private static IEnumerator CaptureSoul(TablePresenter presenter)
        {
            Press("MenuButton");
            Press("NewGameButton");
            Press("ChooseDealer0");
            yield return WaitForTable();
            presenter.Game.GetType().GetField("_houseBetting", Flags).SetValue(presenter.Game, new AlwaysReRaise());

            SetSentence(presenter, 2050);
            yield return WaitForTable();
            yield return new WaitForSeconds(0.3f);
            yield return Shot("11_soul_entry");
            yield return new WaitForSeconds(1.5f);

            Press("LeaveButton");
            yield return WaitForTable();
            yield return new WaitForSeconds(1.5f);
            yield return Shot("12_leave_locked");

            Press("ActionButton");   // deal
            yield return WaitForTable();
            yield return new WaitForSeconds(0.4f);
            yield return Shot("13_soul_decision");
            Press("RaiseButton");
            yield return WaitForTable();
            for (int guard = 0; guard < 10 && IsActive("PassButton"); guard++)
            {
                Press("PassButton");
                yield return WaitForTable();
            }
            Press("ActionButton");   // stand pat
            yield return WaitForTable();
            if (IsActive("RaiseButton") && Find<Button>("RaiseButton").interactable)
            {
                Press("RaiseButton");
                yield return WaitForTable();
                yield return new WaitForSeconds(0.4f);
                if (IsActive("CallButton"))
                    yield return Shot("14_soul_reraise");
            }
            yield return PlayToShowdown();
            yield return new WaitForSeconds(0.6f);
            yield return Shot("15_soul_result");

            // Play on just past the line until a loss and a rescue have both been on screen.
            bool lost = false, rescued = false;
            for (int hand = 0; hand < 40 && !(lost && rescued); hand++)
            {
                if (presenter.Game.IsGameOver || !presenter.Game.IsSoulAtStake)
                {
                    SetSentence(presenter, 2050);
                    yield return WaitForTable();
                }
                Press("ActionButton");   // next hand / play again
                yield return WaitForTable();
                if (presenter.Game.Phase == HellPoker.Core.Game.GamePhase.Betting)
                {
                    Press("ActionButton");   // deal
                    yield return WaitForTable();
                }
                yield return PlayToShowdown();

                var outcome = presenter.Game.LastRound?.Showdown?.Outcome;
                if (outcome == HellPoker.Core.Game.ShowdownOutcome.HouseWins && !lost && !presenter.Game.IsGameOver)
                {
                    lost = true;
                    yield return new WaitForSeconds(0.4f);
                    yield return Shot("16_soul_loss");
                }
                else if (outcome == HellPoker.Core.Game.ShowdownOutcome.PlayerWins && !rescued && !presenter.Game.IsSoulAtStake)
                {
                    rescued = true;
                    yield return new WaitForSeconds(1.2f);
                    yield return Shot("17_soul_rescued");
                }
            }

            // The trap: 1600 years is safe with Mammon but past Lilith's line.
            if (presenter.Game.IsGameOver || presenter.Game.Phase == HellPoker.Core.Game.GamePhase.RoundOver)
            {
                SetSentence(presenter, 1600);
                yield return WaitForTable();
                Press("ActionButton");
                yield return WaitForTable();
            }
            SetSentence(presenter, 1600);
            yield return WaitForTable();
            yield return new WaitForSeconds(0.5f);
            Press("LeaveButton");
            yield return new WaitForSeconds(1f);
            yield return Shot("18_change_table");

            Press("ChooseDealer2");
            yield return new WaitForSeconds(0.3f);
            yield return Shot("19_soul_warning");

            Press("ConfirmSeatButton");
            yield return WaitForTable();
            yield return new WaitForSeconds(2f);
            yield return Shot("20_lilith_soul");
        }

        private const System.Reflection.BindingFlags Flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        /// <summary>Screenshot tool only: rewrites the sentence between hands and refreshes the table.</summary>
        private static void SetSentence(TablePresenter presenter, int years)
        {
            object ledger = presenter.Game.GetType().GetField("_ledger", Flags).GetValue(presenter.Game);
            ledger.GetType().GetMethod("Reset").Invoke(ledger, new object[] { years });
            if (presenter.Game.IsGameOver)
                presenter.Game.GetType().GetProperty("Phase").SetValue(presenter.Game, HellPoker.Core.Game.GamePhase.RoundOver);
            typeof(TablePresenter).GetMethod("Refresh", Flags).Invoke(presenter, null);
        }

        /// <summary>Passes every decision, stands pat, calls any re-raise, until the hand is settled.</summary>
        private static IEnumerator PlayToShowdown()
        {
            for (int guard = 0; guard < 20; guard++)
            {
                if (IsActive("PassButton") && Find<Button>("PassButton").interactable) Press("PassButton");
                else if (IsActive("RaiseButton") && Find<Button>("RaiseButton").interactable && !IsActive("ActionButton")) Press("RaiseButton");
                else if (IsActive("CallButton")) Press("CallButton");
                else if (ActionLabelIs("STAND PAT")) Press("ActionButton");
                else break;
                yield return WaitForTable();
            }
        }

        private static bool ActionLabelIs(string label)
        {
            Button action = Find<Button>("ActionButton");
            return action.gameObject.activeInHierarchy && action.GetComponentInChildren<Text>().text == label;
        }

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
