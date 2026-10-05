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
    /// <summary>Loads the real game scene and plays a hand by pressing the actual UI buttons, waiting for animations.</summary>
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
        public IEnumerator Scene_BuildsTable()
        {
            Assert.IsNotNull(Object.FindFirstObjectByType<HellPokerBootstrap>(), "Bootstrap missing from scene.");
            Assert.IsNotNull(Object.FindFirstObjectByType<EventSystem>(), "No EventSystem, UI would not receive clicks.");
            Assert.IsNotNull(Object.FindFirstObjectByType<TableView>(), "Table was not built.");
            Assert.IsTrue(Object.FindFirstObjectByType<MainMenuView>().IsVisible, "The game should open on the main menu.");
            Assert.IsFalse(IsActive("ContinueButton"), "Nothing to continue yet.");

            Press("NewGameButton");
            Assert.IsFalse(Object.FindFirstObjectByType<MainMenuView>().IsVisible);
            Assert.IsTrue(Object.FindFirstObjectByType<DealerSelectView>().IsVisible, "New Game should ask for a dealer first.");

            Press("ChooseDealer1");
            Assert.IsFalse(Object.FindFirstObjectByType<DealerSelectView>().IsVisible);
            Assert.AreEqual("DEAL", ActionLabel());
            Assert.IsFalse(IsActive("PassButton"));
            Assert.AreEqual("BELIAL", Object.FindFirstObjectByType<DealerView>().transform.Find("Name").GetComponent<Text>().text);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheMenusLanguageButton_BringsThePlayerBackToATurkishTable_MidHand()
        {
            StartRun();
            Press("ActionButton");
            yield return WaitForTable();
            Assert.AreEqual("PASS", Find<Button>("PassButton").GetComponentInChildren<Text>().text);
            Assert.IsNull(Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name == "LanguageButton"), "No language button at the table.");

            Press("MenuButton");
            yield return new WaitForSeconds(0.4f);
            Press("MenuLanguageButton");
            Assert.AreEqual("YENİ OYUN", Find<Button>("NewGameButton").GetComponentInChildren<Text>().text);
            Press("ContinueButton");
            yield return WaitForTable();

            Assert.AreEqual("MENÜ", Find<Button>("MenuButton").GetComponentInChildren<Text>().text);
            Assert.AreEqual("PAS", Find<Button>("PassButton").GetComponentInChildren<Text>().text);
            Assert.IsTrue(IsActive("PassButton"), "Still the same decision: the hand goes on.");
            Assert.AreEqual("Turkish", HellPokerBootstrap.BatchStore.GetString("settings.language", null), "Saved.");

            Press("MenuButton");
            yield return new WaitForSeconds(0.4f);
            Press("MenuLanguageButton");
            Assert.AreEqual("NEW GAME", Find<Button>("NewGameButton").GetComponentInChildren<Text>().text);
        }

        [UnityTest]
        public IEnumerator TheSinnerCards_TheirWordsFitAboveChoose_InBothLanguages()
        {
            Press("NewGameButton");
            yield return new WaitForSeconds(0.4f);
            Find<Button>("ChooseDealer0").onClick.Invoke();   // the class cards (not the tests' usual Peasant)
            yield return new WaitForSeconds(0.4f);
            try
            {
                foreach (HellPoker.Presentation.Ui.Language language in new[] { HellPoker.Presentation.Ui.Language.English, HellPoker.Presentation.Ui.Language.Turkish })
                {
                    HellPoker.Presentation.Ui.Lang.Set(language);   // the open screen is described again in it
                    yield return null;
                    for (int index = 0; index < HellPoker.Core.Sinners.SinnerRoster.All.Count; index++)
                    {
                        string id = HellPoker.Core.Sinners.SinnerRoster.All[index].Id;
                        Transform card = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                            .Single(t => t.name == "Sinner_" + id);
                        var ability = (RectTransform)card.Find("Ability");
                        var detail = (RectTransform)card.Find("Detail");
                        var choose = (RectTransform)card.Find("ChooseSinner" + index);
                        float Top(RectTransform r) => -r.anchoredPosition.y;
                        float Bottom(RectTransform r) => Top(r) + r.rect.height;

                        string what = $"{id} ({language})";
                        Assert.LessOrEqual(ability.GetComponent<Text>().preferredHeight, ability.rect.height + 0.5f, $"{what}: the ability fits its box.");
                        Assert.LessOrEqual(detail.GetComponent<Text>().preferredHeight, detail.rect.height + 0.5f, $"{what}: the price fits its box.");
                        Assert.GreaterOrEqual(Top(detail), Bottom(ability), $"{what}: the price starts under the ability.");
                        Assert.LessOrEqual(Bottom(detail), Top(choose), $"{what}: both end above CHOOSE.");
                    }
                }
            }
            finally
            {
                HellPoker.Presentation.Ui.Lang.Set(HellPoker.Presentation.Ui.Language.English);
            }
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
            StartRun();
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

            StringAssert.IsMatch("NEXT HAND|THE END", ActionLabel());
            if (ActionLabel() == "NEXT HAND")
            {
                Press("ActionButton");
                yield return WaitForTable();
                Assert.AreEqual("DEAL", ActionLabel());
            }
        }

        [UnityTest]
        public IEnumerator Folding_EndsTheHand()
        {
            StartRun();
            Press("ActionButton");
            yield return WaitForTable();

            Press("MenuButton");
            Assert.IsTrue(IsActive("ContinueButton"), "A run in progress can be continued.");
            Press("ContinueButton");
            yield return WaitForTable();

            Press("FoldButton");
            yield return WaitForTable();

            Assert.IsFalse(IsActive("FoldButton"));
            StringAssert.IsMatch("NEXT HAND|THE END", ActionLabel());
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
            StartRun();
            yield return WaitForTable();
            var table = Object.FindFirstObjectByType<TableView>();
            var presenter = (TablePresenter)typeof(HellPokerBootstrap)
                .GetField("_tablePresenter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(Object.FindFirstObjectByType<HellPokerBootstrap>());

            Press("ActionButton");   // deal: cards start flying
            yield return null;
            Assert.IsTrue(table.IsBusy, "The deal animates.");

            presenter.PerformAction();   // Space while the cards fly

            Assert.IsFalse(table.IsBusy, "Everything landed at once.");
            Assert.AreEqual(HellPoker.Core.Game.GamePhase.PlayerReveal, presenter.Game.Phase, "The press was spent on the skip, not on a pass.");
            Assert.IsTrue(IsActive("PassButton"), "The decision is on screen.");
            Assert.AreEqual(3, Find<Transform>("PlayerHand").GetComponentsInChildren<CardView>().Count(c => c.IsFaceUp));
        }

        [UnityTest]
        public IEnumerator ASealedHand_PlaysOutOnItsOwn_WithoutBetButtons()
        {
            StartRun();   // Mammon, 1000 years: ante 100, the table full at 300
            Press("ActionButton");
            yield return WaitForTable();
            Assert.IsTrue(IsActive("CheckToDrawButton"), "CHECK TO DRAW sits next to PASS before the draw.");
            Press("RaiseButton");
            yield return WaitForTable();

            Press("RaiseButton");   // 300: the pact is sealed, the fifth card turns by itself
            yield return PlayOutWithoutBetButtons();

            Assert.AreEqual("STAND PAT", ActionLabel(), "The draw is still the player's.");
            Assert.AreEqual(5, FaceUp("PlayerHand"));
            Assert.IsFalse(IsActive("PassButton"));

            Press("ActionButton");   // stand pat: the House's cards turn one by one into the showdown
            float started = Time.time;
            yield return PlayOutWithoutBetButtons();

            Assert.Greater(Time.time - started, 2f, "Each of the House's cards waits its beat.");
            StringAssert.IsMatch("NEXT HAND|THE END", ActionLabel());
            Assert.AreEqual(5, FaceUp("HouseHand"));
        }

        [UnityTest]
        public IEnumerator ASealedReveal_CanBeHurried()
        {
            StartRun();
            Press("ActionButton");
            yield return WaitForTable();
            Press("RaiseButton");
            yield return WaitForTable();
            Press("RaiseButton");
            yield return WaitForTable();
            var table = Object.FindFirstObjectByType<TableView>();
            TablePresenter presenter = Presenter();

            Press("ActionButton");   // the House starts turning its cards
            yield return null;
            Assert.IsTrue(table.IsBusy);

            presenter.PerformAction();   // Space

            Assert.IsFalse(table.IsBusy, "A press brings the whole reveal to its end.");
            Assert.AreEqual(5, FaceUp("HouseHand"));
            StringAssert.IsMatch("NEXT HAND|THE END", ActionLabel());
        }

        private static TablePresenter Presenter() => (TablePresenter)typeof(HellPokerBootstrap)
            .GetField("_tablePresenter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(Object.FindFirstObjectByType<HellPokerBootstrap>());

        private static int FaceUp(string hand) => Find<Transform>(hand).GetComponentsInChildren<CardView>().Count(c => c.IsFaceUp);

        /// <summary>Waits out the table's animations, checking at every frame that no bet button ever shows.</summary>
        private static IEnumerator PlayOutWithoutBetButtons()
        {
            var table = Object.FindFirstObjectByType<TableView>();
            float started = Time.time;
            do
            {
                yield return null;
                Assert.IsFalse(IsActive("PassButton"), "PASS showed while the sealed hand played out.");
                Assert.IsFalse(IsActive("FoldButton"), "FOLD showed while the sealed hand played out.");
                Assert.Less(Time.time - started, AnimationTimeout, "Table animations never finished.");
            } while (table.IsBusy);
        }

        private static void StartRun(int dealer = 0)
        {
            Press("NewGameButton");
            Press("ChooseDealer" + dealer);
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
            var table = Object.FindFirstObjectByType<TableView>();
            float started = Time.time;
            yield return null;
            while (table.IsBusy)
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
            // A new run asks who the player was: the tests play the Peasant unless they say otherwise.
            if (button.name.StartsWith("ChooseDealer")) ChooseSinner(0);
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
