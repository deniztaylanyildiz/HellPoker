using System.Collections;
using System.Linq;
using HellPoker.Presentation;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>Loads the real game scene and plays a hand at the first imp's table by pressing the actual UI buttons, waiting for animations
    /// (Demo 3.4.1: the game's own table — the Boss Simulator's is gone).</summary>
    public class HellPokerSceneTests
    {
        private const string SceneName = "HellPoker";
        private const float AnimationTimeout = 10f;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            HellPokerBootstrap.BatchStore.Clear();   // every test starts with no save, no records, default settings
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Scene_BuildsTheMenu_AndNewGameLeadsToATable()
        {
            Assert.IsNotNull(Object.FindFirstObjectByType<HellPokerBootstrap>(), "Bootstrap missing from scene.");
            Assert.IsNotNull(Object.FindFirstObjectByType<EventSystem>(), "No EventSystem, UI would not receive clicks.");
            Assert.IsTrue(Object.FindFirstObjectByType<MainMenuView>().IsVisible, "The game should open on the main menu.");
            Assert.IsFalse(IsActive("ContinueButton"), "Nothing to continue yet.");

            Press("NewGameButton");
            Assert.IsFalse(Object.FindFirstObjectByType<MainMenuView>().IsVisible);
            Assert.IsTrue(Object.FindFirstObjectByType<SinnerSelectView>().IsVisible, "New Game asks who the player was first.");

            yield return StartRun();
            Assert.IsTrue(ChapterJourneyTests.Chapters.IsAtTable, "the first imp's table");
            Assert.AreEqual("DEAL", ActionLabel());
            Assert.IsFalse(IsActive("PassButton"));
        }

        [UnityTest]
        public IEnumerator SettingsLanguageTab_BringsThePlayerBackToATurkishTable_MidHand()
        {
            yield return StartRun();
            Press("ActionButton");
            yield return WaitForTable();
            Assert.AreEqual("PASS", Find<Button>("PassButton").GetComponentInChildren<Text>().text);
            Assert.IsNull(Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name == "LanguageButton" || b.name == "MenuLanguageButton"), "No language button at the table or on the menu.");

            Press("MenuButton");
            yield return new WaitForSeconds(0.4f);
            yield return ChooseLanguage(1);   // Settings ▸ LANGUAGE ▸ Türkçe, then back to the menu
            Assert.AreEqual("YENİ OYUN", Find<Button>("NewGameButton").GetComponentInChildren<Text>().text);
            Press("ContinueButton");
            yield return WaitForTable();

            Assert.AreEqual("MENÜ", Find<Button>("MenuButton").GetComponentInChildren<Text>().text);
            Assert.AreEqual("PAS", Find<Button>("PassButton").GetComponentInChildren<Text>().text);
            Assert.IsTrue(IsActive("PassButton"), "Still the same decision: the hand goes on.");
            Assert.AreEqual("Turkish", HellPokerBootstrap.BatchStore.GetString("settings.language", null), "Saved.");

            Press("MenuButton");
            yield return new WaitForSeconds(0.4f);
            yield return ChooseLanguage(0);
            Assert.AreEqual("NEW GAME", Find<Button>("NewGameButton").GetComponentInChildren<Text>().text);
        }

        /// <summary>From the title menu: Settings, its LANGUAGE tab, the language at <paramref name="index"/>, BACK.</summary>
        private IEnumerator ChooseLanguage(int index)
        {
            Press("SettingsButton");
            yield return new WaitForSeconds(0.4f);
            Press("SettingsTabLanguage");
            yield return null;
            Press("SettingsLanguage" + index);
            yield return null;
            Press("SettingsBackButton");
            yield return new WaitForSeconds(0.4f);
        }

        [UnityTest]
        public IEnumerator TheSettings_DisplayTab_WindowSize_IsSaved()
        {
            Press("SettingsButton");
            yield return new WaitForSeconds(0.4f);
            Assert.IsFalse(IsActive("WindowScaleButton"), "The settings open on the game tab.");

            Press("SettingsTabDisplay");
            Assert.IsTrue(IsActive("WindowScaleButton"));
            Assert.IsFalse(IsActive("SpeedButton"), "One tab at a time.");

            Press("FullscreenButton");   // a window: the size row comes alive (the editor's Game view keeps its size)
            Assert.AreEqual(1, HellPokerBootstrap.BatchStore.GetInt("settings.display.mode", -1));
            Press("WindowScaleButton");

            Assert.AreEqual(2, HellPokerBootstrap.BatchStore.GetInt("settings.display.scale", -1), "Saved.");
            StringAssert.StartsWith("×2", Find<Button>("WindowScaleButton").GetComponentInChildren<Text>().text);

            Press("SettingsBackButton");
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(Object.FindFirstObjectByType<MainMenuView>().IsVisible);
        }

        [UnityTest]
        public IEnumerator PlayingAHand_ThroughTheButtons()
        {
            yield return StartRun();
            Press("ActionButton");
            yield return WaitForTable();

            // Player's cards turn one by one, each followed by a decision.
            Assert.IsTrue(IsActive("PassButton"), "Bet decision should follow the first card.");
            Assert.IsFalse(IsActive("ActionButton"));
            Press("RaiseButton");
            yield return WaitForTable();
            yield return PassWhileDeciding();

            Assert.AreEqual("STAND PAT", ActionLabel());
            Press(Find<Transform>("PlayerHand").GetComponentsInChildren<Button>().First());
            yield return WaitForTable();
            Assert.AreEqual("DRAW 1", ActionLabel());

            Press("ActionButton");
            yield return WaitForTable();

            // One decision on the new hand, one after the house shows its cards, then the showdown.
            Assert.IsTrue(IsActive("PassButton"), "Bet decision should follow the draw.");
            yield return PassWhileDeciding();

            Assert.IsNotNull(ActionLabel(), "the showdown: one button to go on");
            Assert.IsFalse(IsActive("PassButton"));
        }

        [UnityTest]
        public IEnumerator Folding_EndsTheHand()
        {
            yield return StartRun();
            Press("ActionButton");
            yield return WaitForTable();

            Press("MenuButton");
            Assert.IsTrue(IsActive("ContinueButton"), "A run in progress can be continued.");
            Press("ContinueButton");
            yield return WaitForTable();

            Press("FoldButton");
            yield return WaitForTable();

            Assert.IsFalse(IsActive("FoldButton"));
            Assert.IsNotNull(ActionLabel(), "the hand is over: one button to go on");
        }

        [UnityTest]
        public IEnumerator ScreenTransition_CoversOnlyWhilePlaying()
        {
            var transition = Object.FindFirstObjectByType<ScreenTransitionView>();
            Press("NewGameButton");
            Assert.IsTrue(transition.IsPlaying);
            Assert.IsTrue(transition.GetComponentsInChildren<Image>().Any(i => i.enabled), "The curtain is down.");

            yield return new WaitForSeconds(0.6f);

            Assert.IsFalse(transition.IsPlaying);
            Assert.IsFalse(transition.GetComponentsInChildren<Image>().Any(i => i.enabled), "Nothing of the transition is left over the screen.");
        }

        [UnityTest]
        public IEnumerator APress_DuringTheDeal_SkipsToTheEnd()
        {
            yield return StartRun();
            yield return WaitForTable();
            var table = ChapterJourneyTests.ChapterTableView;
            var presenter = (TablePresenter)ChapterJourneyTests.Chapters.Table;

            Press("ActionButton");   // deal: cards start flying
            yield return null;
            Assert.IsTrue(table.IsBusy, "The deal animates.");

            presenter.PerformAction();   // Space while the cards fly

            Assert.IsFalse(table.IsBusy, "Everything landed at once.");
            Assert.AreEqual(HellPoker.Core.Game.GamePhase.PlayerReveal, presenter.Game.Phase, "The press was spent on the skip, not on a pass.");
            Assert.IsTrue(IsActive("PassButton"), "The decision is on screen.");
            Assert.AreEqual(3, FaceUp("PlayerHand"));
        }

        private static int FaceUp(string hand) => Find<Transform>(hand).GetComponentsInChildren<CardView>().Count(c => c.IsFaceUp);

        /// <summary>NEW GAME, the Peasant (Medium), the chapter's opening words, the first imp's table among the starts.</summary>
        private static IEnumerator StartRun()
        {
            Press("NewGameButton");
            ChooseSinner(0);
            yield return new WaitForSeconds(0.6f);
            Press("PanelOption0");   // DESCEND
            yield return null;
            HellPoker.Core.Chapters.MapNode start = ChapterJourneyTests.Chapters.Run.Choices.First(n => n.Kind == HellPoker.Core.Chapters.NodeKind.Table);
            Press($"Node0_{start.Lane}");
            yield return new WaitForSeconds(0.6f);
            yield return WaitForTable();
        }

        private static IEnumerator PassWhileDeciding()
        {
            for (int guard = 0; guard < 10 && IsActive("PassButton"); guard++)
            {
                Press("PassButton");
                yield return WaitForTable();
            }
        }

        private static IEnumerator WaitForTable()
        {
            var table = ChapterJourneyTests.ChapterTableView;
            float started = Time.time;
            yield return null;
            while (table != null && table.IsBusy)
            {
                Assert.Less(Time.time - started, AnimationTimeout, "Table animations never finished.");
                yield return null;
            }
        }

        private static void Press(string name) => Press(Find<Button>(name));

        private static void Press(Button button)
        {
            Assert.IsTrue(button.gameObject.activeInHierarchy, $"{button.name} is hidden.");
            Assert.IsTrue(button.interactable, $"{button.name} is not interactable.");
            button.onClick.Invoke();
            // New Game over a run asks first: the tests mean it.
            if (button.name == "NewGameButton") ConfirmNewGame();
        }

        private static void ChooseSinner(int index)
        {
            Button choose = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name == "ChooseSinner" + index && b.gameObject.activeInHierarchy);
            if (choose != null) choose.onClick.Invoke();
        }

        private static void ConfirmNewGame()
        {
            Button confirm = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name == "MenuConfirmButton" && b.gameObject.activeInHierarchy);
            if (confirm != null) confirm.onClick.Invoke();
        }

        private static bool IsActive(string name) => Find<Button>(name).gameObject.activeInHierarchy;

        private static string ActionLabel()
        {
            Button action = Find<Button>("ActionButton");
            return action.gameObject.activeInHierarchy ? action.GetComponentInChildren<Text>().text : null;
        }

        private static T Find<T>(string name) where T : Component
        {
            T found = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c.name == name);
            Assert.IsNotNull(found, $"'{name}' not found in scene.");
            return found;
        }
    }
}
