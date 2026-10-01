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
