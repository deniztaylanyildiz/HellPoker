using System.Collections;
using System.Linq;
using HellPoker.Presentation;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>The hall behind the dealer choice and the table must always match the demon in front of it.</summary>
    public class SalonRegressionTests
    {
        private const float Settle = 0.6f;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            HellPokerBootstrap.BatchStore.Clear();   // every test starts with no save, no records, default settings
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return null;
        }

        private static MainMenuPresenter Menu => Field<MainMenuPresenter>("_menuPresenter");
        private static TablePresenter Table => Field<TablePresenter>("_tablePresenter");

        private static T Field<T>(string name)
        {
            var bootstrap = Object.FindFirstObjectByType<HellPokerBootstrap>();
            return (T)typeof(HellPokerBootstrap).GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(bootstrap);
        }

        private static SalonView SalonOf<TView>() where TView : Component =>
            Object.FindFirstObjectByType<TView>().GetComponentInChildren<SalonView>(true);

        private static void AssertShows<TView>(string dealerId, SalonMode mode) where TView : Component
        {
            SalonView salon = SalonOf<TView>();
            Assert.AreEqual(dealerId, salon.ShownDealerId, $"{typeof(TView).Name} shows the wrong hall.");
            Assert.AreEqual(mode, salon.ShownMode, $"{typeof(TView).Name} shows the wrong mood.");
        }

        [UnityTest]
        public IEnumerator NewGame_AfterBrowsing_HighlightsAndShowsTheFirstDemon()
        {
            Press("NewGameButton");
            yield return new WaitForSeconds(Settle);
            Press(Find<Transform>("Dealer_lilith").GetComponentInChildren<Button>());
            yield return new WaitForSeconds(Settle);
            AssertShows<DealerSelectView>("lilith", SalonMode.Normal);

            Menu.GoBack();   // Esc
            Press("NewGameButton");
            yield return new WaitForSeconds(Settle);

            AssertShows<DealerSelectView>("mammon", SalonMode.Normal);
            Assert.AreEqual(0, Object.FindFirstObjectByType<DealerSelectView>().SelectedIndex);
        }

        [UnityTest]
        public IEnumerator ChangeTable_BrowseAndBack_ThenContinue_KeepsTheTablesHall()
        {
            Press("NewGameButton");
            Press("ChooseDealer0");
            yield return WaitForTable();
            Press("ActionButton");   // play a hand so Continue is offered
            yield return WaitForTable();
            Press("FoldButton");
            yield return WaitForTable();

            Press("MenuButton");
            Press("ChangeTableButton");
            yield return new WaitForSeconds(Settle);
            AssertShows<DealerSelectView>("mammon", SalonMode.Normal);
            Press(Find<Transform>("Dealer_lilith").GetComponentInChildren<Button>());
            yield return new WaitForSeconds(Settle);
            Press("DealerBackButton");
            yield return WaitForTable();
            Press("MenuButton");
            Press("ContinueButton");
            yield return WaitForTable();

            AssertShows<TableView>("mammon", SalonMode.Normal);
        }

        [UnityTest]
        public IEnumerator NewGame_AfterTheFinalStretchOrTheSoul_StartsInANormalHall()
        {
            // 140: summoned to Lucifer, in his last moments (his hall burning); 2100: the soul at Mammon's.
            foreach (int years in new[] { 140, 2100 })
            {
                Press(Menu.IsMenuOpen ? "NewGameButton" : "MenuButton");
                if (!Object.FindFirstObjectByType<DealerSelectView>().IsVisible) Press("NewGameButton");
                Press("ChooseDealer0");
                yield return WaitForTable();
                Table.Game.TakeOver(years, 3);
                Table.SwitchTable(HellPoker.Core.Dealers.DealerRoster.Mammon);
                yield return WaitForTable();
                AssertShows<TableView>(years > 1000 ? "mammon" : "lucifer", years > 1000 ? SalonMode.Soul : SalonMode.Hell);

                Press("MenuButton");
                Press("NewGameButton");
                Press("ChooseDealer1");
                yield return WaitForTable();

                AssertShows<TableView>("belial", SalonMode.Normal);
            }
        }

        [UnityTest]
        public IEnumerator SwitchingTables_ShowsTheNewHallAtOnce()
        {
            Press("NewGameButton");
            Press("ChooseDealer0");
            yield return WaitForTable();
            Press("ActionButton");
            yield return WaitForTable();
            Press("FoldButton");
            yield return WaitForTable();
            Press("ActionButton");   // next hand: between hands again
            yield return WaitForTable();

            Press("LeaveButton");
            yield return null;
            Press("ChooseDealer2");

            AssertShows<TableView>("lilith", SalonMode.Normal);
        }

        [UnityTest]
        public IEnumerator BrowsingFast_AndLeavingMidWipe_NeverLeavesAnOldHall()
        {
            Press("NewGameButton");
            yield return null;
            Press(Find<Transform>("Dealer_lilith").GetComponentInChildren<Button>());
            Press(Find<Transform>("Dealer_belial").GetComponentInChildren<Button>());
            yield return null;   // mid-wipe
            Press("DealerBackButton");
            Press("NewGameButton");

            AssertShows<DealerSelectView>("mammon", SalonMode.Normal);   // at once, no wipe on opening
            yield return new WaitForSeconds(Settle);
            AssertShows<DealerSelectView>("mammon", SalonMode.Normal);   // and no late wipe overwrites it
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
            // New Game over a run asks first: the tests mean it.
            if (button.name == "NewGameButton") ConfirmNewGame();
        }

        private static void ConfirmNewGame()
        {
            Button confirm = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(b => b.name == "ConfirmNewGameButton" && b.gameObject.activeInHierarchy);
            if (confirm != null) confirm.onClick.Invoke();
        }

        /// <summary>The live object of that name (old dealer cards linger until the end of the frame they were replaced in).</summary>
        private static T Find<T>(string name) where T : Component
        {
            T found = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .LastOrDefault(c => c.name == name && c.gameObject.activeInHierarchy);
            Assert.IsNotNull(found, $"'{name}' not found in scene.");
            return found;
        }
    }
}
