using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HellPoker.Core.Cards;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
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
    /// The final table through the real scene and its buttons, on stacked decks: win at 260 years and be summoned →
    /// lose at Lucifer's table and be cast down → back at Mammon's → summoned again (he remembers) → beat him → the
    /// Morning Star falls. Every scene (darkness, fall) plays out. Any error log fails the test.
    /// </summary>
    public class LuciferJourneyTests
    {
        private const float SceneTimeout = 25f;

        // Player five, House five, then the draws.
        private const string Win = "2C 9C JC 4C KC  2D 2H 5S 7H 9D  3S 6C JD QC 10S 2S";        // a flush beats a pair of twos
        private const string LoseTwoPair = "2C 5D 7H 9S JC  KS KH 4D 4C 9H  3S 6C JD QC 10S 2S"; // two pair beats nothing

        private static TablePresenter Presenter =>
            (TablePresenter)typeof(HellPokerBootstrap).GetField("_tablePresenter", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(UnityEngine.Object.FindFirstObjectByType<HellPokerBootstrap>());

        [UnityTest, Timeout(180000)]
        public IEnumerator Summoned_CastDown_SummonedAgain_AndTheMorningStarFalls()
        {
            HellPokerBootstrap.BatchStore.Clear();
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return null;

            Press("NewGameButton");
            Press("ChooseDealer0");   // Mammon
            yield return WaitForTable();

            // From here on every table deals the next stacked deck.
            var decks = new Queue<string>(new[] { Win, LoseTwoPair, Win, Win });
            StackTheDecks(decks);
            Presenter.Game.TakeOver(260, 3);
            Presenter.SwitchTable(DealerRoster.Mammon);
            yield return WaitForTable();

            // 260 → a flush forgives 125 → 135; the summons comes with NEXT HAND.
            yield return PlayHand();
            Assert.AreEqual("mammon", Presenter.CurrentDealerId);
            Press("ActionButton");
            yield return WaitForTable();

            Assert.AreEqual("lucifer", Presenter.CurrentDealerId);
            Assert.AreEqual("THE MORNING STAR", Find<DealerView>("Dealer").transform.Find("Name").GetComponent<Text>().text);
            Assert.AreEqual("", Find<DealerView>("Dealer").transform.Find("Title").GetComponent<Text>().text,
                "'Waits below 250 years' is for the locked card only.");
            Assert.AreEqual("lucifer", Table.GetComponentInChildren<SalonView>(true).ShownDealerId);
            Assert.AreEqual("NO ESCAPE", Find<Button>("LeaveButton").GetComponentInChildren<Text>().text);
            Assert.AreEqual(Vector2.zero, ScreenOffset(), "The scene left nothing behind.");
            Assert.IsFalse(Find<Image>("Shroud").enabled, "The darkness lifted.");

            // At his table: two pair costs (50 + 50) × 1.25 = 125 → 260, above the gate: cast down to 500 at Mammon's.
            yield return PlayHand();
            Press("ActionButton");
            yield return WaitForTable();

            Assert.AreEqual("mammon", Presenter.CurrentDealerId);
            Assert.AreEqual(500, Presenter.Game.Years);
            Assert.AreEqual("mammon", Table.GetComponentInChildren<SalonView>(true).ShownDealerId);
            Assert.AreEqual(Vector2.zero, ScreenOffset(), "Landed.");

            // 500 → a flush forgives 250 → 250: summoned again.
            yield return PlayHand();
            Press("ActionButton");
            yield return WaitForTable();

            Assert.AreEqual("lucifer", Presenter.CurrentDealerId);
            Assert.AreEqual(2, Presenter.Gate.Attempts);
            StringAssert.Contains("ATTEMPT 2", Find<Transform>("Sentence").Find("Label").GetComponent<Text>().text);

            // 250 → a flush forgives 250: free.
            yield return PlayHand();
            Assert.AreEqual("THE END", ActionLabel());
            Assert.AreEqual(GamePhase.Absolved, Presenter.Game.Phase);

            Press("ActionButton");
            yield return null;
            Assert.IsTrue(UnityEngine.Object.FindFirstObjectByType<EndScreenView>().IsVisible);
            Assert.AreEqual("THE MORNING STAR FALLS", Find<Transform>("EndCanvas").GetComponentsInChildren<Text>().First(t => t.name == "Title").text);
        }

        /// <summary>Swaps the bootstrap's game factory for one that deals the queued decks, in order (the last one repeats).</summary>
        private static void StackTheDecks(Queue<string> decks)
        {
            string last = decks.Peek();
            Func<Dealer, HellPoker.Core.Sinners.Sinner, IHellPokerGame> stacked = (dealer, _) =>
            {
                if (decks.Count > 0) last = decks.Dequeue();
                // Each table its own stacked deck: the run's deck is not carried (a fresh deal every hand).
                GameRules rules = dealer.ApplyTo(new GameRules(continuousDeck: false));
                return new HellPokerGame(rules, new Deck(new NoShuffle(), Cards(last).Reverse()), HandEvaluator.CreateDefault(),
                    new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)), new HouseDrawStrategy(rules.MaxDiscards), dealer.Payouts);
            };
            typeof(TablePresenter).GetField("_createGame", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(Presenter, stacked);
        }

        private sealed class NoShuffle : IShuffler
        {
            public void Shuffle<T>(IList<T> items) { }
        }

        private static IEnumerable<Card> Cards(string notation)
        {
            foreach (string token in notation.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string rank = token.Substring(0, token.Length - 1);
                Rank r = rank == "A" ? Rank.Ace : rank == "K" ? Rank.King : rank == "Q" ? Rank.Queen : rank == "J" ? Rank.Jack : (Rank)int.Parse(rank);
                Suit s = token[token.Length - 1] == 'C' ? Suit.Clubs : token[token.Length - 1] == 'D' ? Suit.Diamonds
                    : token[token.Length - 1] == 'H' ? Suit.Hearts : Suit.Spades;
                yield return new Card(r, s);
            }
        }

        /// <summary>Deals, checks to the draw, stands pat and passes to the end of the hand — all through the buttons.</summary>
        private static IEnumerator PlayHand()
        {
            Press("ActionButton");   // deal
            yield return WaitForTable();
            for (int guard = 0; guard < 12; guard++)
            {
                string label = ActionLabel();
                if (label == "NEXT HAND" || label == "THE END") yield break;

                if (IsActive("CheckToDrawButton") && !Find<ButtonFeel>("CheckToDrawButton").Locked) Press("CheckToDrawButton");
                else if (label == "STAND PAT") Press("ActionButton");
                else if (IsActive("PassButton")) Press("PassButton");
                else if (IsActive("CallButton")) Press("CallButton");
                yield return WaitForTable();
            }
            Assert.Fail("The hand never ended.");
        }

        private static TableView Table => UnityEngine.Object.FindFirstObjectByType<TableView>();

        /// <summary>Where the table's 480×270 screen sits (moved only while a fall or a shake plays).</summary>
        private static Vector2 ScreenOffset() => ((RectTransform)Table.GetComponentInChildren<SalonView>(true).transform.parent).anchoredPosition;

        private static IEnumerator WaitForTable()
        {
            TableView table = Table;
            float started = Time.time;
            yield return null;
            while (table.IsBusy)
            {
                Assert.Less(Time.time - started, SceneTimeout, "Table animations never finished.");
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

        private static T Find<T>(string name) where T : Component
        {
            T found = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(c => c.name == name).OrderByDescending(c => c.gameObject.activeInHierarchy).FirstOrDefault();
            Assert.IsNotNull(found, $"'{name}' not found in scene.");
            return found;
        }
    }
}
