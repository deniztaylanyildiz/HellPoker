using System.Collections;
using System.Linq;
using HellPoker.Presentation;
using HellPoker.Presentation.Ui;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// The whole journey through the real scene: new game → a few hands → change tables → menu → continue → "close and
    /// reopen" the game (reload the scene, the save is read back) → continue → play on. Any error log fails the test.
    /// </summary>
    public class RunJourneyTests
    {
        private const string TheEnd = "THE END";   // TheEnd (internal)

        private static TablePresenter Presenter =>
            (TablePresenter)typeof(HellPokerBootstrap).GetField("_tablePresenter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(Object.FindFirstObjectByType<HellPokerBootstrap>());

        [UnityTest]
        public IEnumerator NewGame_Hands_ChangeTable_Continue_Reopen_Continue()
        {
            HellPokerBootstrap.BatchStore.Clear();
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return null;

            Press("NewGameButton");
            Press("ChooseDealer0");
            yield return WaitForTable();
            for (int hand = 0; hand < 2; hand++)
                yield return PlayHand();
            if (ActionLabel() == TheEnd) Assert.Ignore("The run ended by chance in two hands.");

            // Change tables between hands: Mammon → Belial (safe at this sentence).
            Press("ActionButton");   // next hand
            yield return WaitForTable();
            Press("LeaveButton");
            yield return null;
            Press("ChooseDealer1");
            yield return WaitForTable();
            Assert.AreEqual("belial", Presenter.CurrentDealerId);

            Press("MenuButton");
            Press("ContinueButton");
            yield return WaitForTable();
            int years = Presenter.Game.Years;
            int rounds = Presenter.Game.RoundNumber;

            // "Close" the game and open it again: the scene is rebuilt and reads the save back.
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return null;
            Assert.IsTrue(Object.FindFirstObjectByType<MainMenuView>().IsVisible, "The game opens on the menu.");
            Assert.IsTrue(IsActive("ContinueButton"), "The saved run can be continued.");

            Press("ContinueButton");
            yield return WaitForTable();
            Assert.AreEqual("belial", Presenter.CurrentDealerId);
            Assert.AreEqual(years, Presenter.Game.Years);
            Assert.AreEqual(rounds, Presenter.Game.RoundNumber);
            Assert.AreEqual("BELIAL", Object.FindFirstObjectByType<DealerView>().transform.Find("Name").GetComponent<Text>().text);
            Assert.AreEqual("belial", Object.FindFirstObjectByType<TableView>().GetComponentInChildren<SalonView>(true).ShownDealerId);

            yield return PlayHand();
            StringAssert.IsMatch("NEXT HAND|THE END", ActionLabel());
        }

        [UnityTest]
        public IEnumerator ClosingMidHand_AndReopening_ForfeitsTheHand()
        {
            HellPokerBootstrap.BatchStore.Clear();
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return null;

            Press("NewGameButton");
            Press("ChooseDealer0");   // Mammon, 1000 years
            yield return WaitForTable();
            Press("ActionButton");    // deal: 100 on the table
            yield return WaitForTable();
            Press("RaiseButton");     // 200
            yield return WaitForTable();

            // "Close" the game in the middle of the hand and open it again.
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return null;
            Assert.IsTrue(IsActive("ContinueButton"));

            Press("ContinueButton");
            yield return WaitForTable();

            Assert.AreEqual(1100, Presenter.Game.Years, "The hand counts as a fold before the draw: half of 200.");
            Assert.AreEqual(1, Presenter.Game.RoundNumber);
            Assert.AreEqual("DEAL", ActionLabel(), "Back between hands — the old cards are gone.");
            StringAssert.Contains("+100", Find<Text>("Message").text);

            // Reopening once more does not charge the hand again.
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return null;
            Press("ContinueButton");
            yield return WaitForTable();
            Assert.AreEqual(1100, Presenter.Game.Years);
        }

        /// <summary>Deals and plays one hand to its end: passes (or raises when passing is locked), stands pat, calls.</summary>
        private static IEnumerator PlayHand()
        {
            if (ActionLabel() == "NEXT HAND")
            {
                Press("ActionButton");
                yield return WaitForTable();
            }
            Press("ActionButton");   // deal
            yield return WaitForTable();

            for (int guard = 0; guard < 20; guard++)
            {
                string label = ActionLabel();
                if (label == "NEXT HAND" || label == TheEnd) yield break;

                if (IsActive("PassButton") && !IsLocked("PassButton")) Press("PassButton");
                else if (IsActive("RaiseButton") && !IsLocked("RaiseButton")) Press("RaiseButton");
                else if (IsActive("CallButton")) Press("CallButton");
                else if (label == "STAND PAT") Press("ActionButton");
                else if (IsActive("FoldButton")) Press("FoldButton");
                yield return WaitForTable();
            }
            Assert.Fail("The hand never ended.");
        }

        private static IEnumerator WaitForTable()
        {
            var table = Object.FindFirstObjectByType<TableView>();
            float started = Time.time;
            yield return null;
            while (table.IsBusy)
            {
                Assert.Less(Time.time - started, 10f, "Table animations never finished.");
                yield return null;
            }
        }

        private static string ActionLabel()
        {
            Button action = Find<Button>("ActionButton");
            return action.gameObject.activeInHierarchy ? action.GetComponentInChildren<Text>().text : null;
        }

        private static void Press(string name)
        {
            Button button = Find<Button>(name);
            Assert.IsTrue(button.gameObject.activeInHierarchy && button.interactable, $"{name} cannot be pressed.");
            button.onClick.Invoke();
        }

        private static bool IsActive(string name) => Find<Button>(name).gameObject.activeInHierarchy;
        private static bool IsLocked(string name) => Find<ButtonFeel>(name).Locked;

        private static T Find<T>(string name) where T : Component
        {
            T found = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(c => c.name == name).OrderByDescending(c => c.gameObject.activeInHierarchy).FirstOrDefault();
            Assert.IsNotNull(found, $"'{name}' not found in scene.");
            return found;
        }
    }
}
