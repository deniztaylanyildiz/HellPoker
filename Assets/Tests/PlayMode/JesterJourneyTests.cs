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
using HellPoker.Core.Sinners;
using HellPoker.Presentation;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// The Jester through the real scene: chosen on the class screen (the fourth card), a hand dealt with a joker, the picker at the
    /// showdown (its arrow and NAME IT pressed), the named card shown with the fool's cap; the badge counts one joker more.
    /// </summary>
    public class JesterJourneyTests
    {
        private const float SceneTimeout = 25f;

        // Player five (a pair of aces and a joker), House five (nothing), then the draws; the run's second joker at the bottom.
        private const string Deal = "AS AH 5C 9D JK1  2C 5D 7H 9S JC  3S 6D 10S 8H 3H QC 8C 7C JK2";

        private static TablePresenter Presenter =>
            (TablePresenter)typeof(HellPokerBootstrap).GetField("_tablePresenter", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(UnityEngine.Object.FindFirstObjectByType<HellPokerBootstrap>());

        [UnityTest, Timeout(120000)]
        public IEnumerator TheJester_AJokerAtTheShowdown_ThePickerNamesIt()
        {
            HellPokerBootstrap.BatchStore.Clear();
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return null;

            Find<Button>("NewGameButton").onClick.Invoke();
            yield return new WaitForSeconds(0.4f);
            Find<Button>("ChooseDealer0").onClick.Invoke();   // Mammon
            yield return new WaitForSeconds(0.4f);
            Find<Button>("ChooseSinner3").onClick.Invoke();   // the Jester, the fourth card
            yield return WaitForTable();
            Assert.AreEqual(Jester.ClassId, Presenter.Sinner.Id);
            Assert.AreEqual(2, Presenter.Sinner.Jokers);

            Func<Dealer, Sinner, IHellPokerGame> stacked = (dealer, sinner) =>
            {
                GameRules rules = dealer.ApplyTo(new GameRules());
                return new HellPokerGame(rules, new Deck(new NoShuffle(), Cards(Deal).Reverse()), HandEvaluator.CreateDefault(),
                    new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)), new HouseDrawStrategy(rules.MaxDiscards), dealer.Payouts,
                    null, null, null, sinner);
            };
            typeof(TablePresenter).GetField("_createGame", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(Presenter, stacked);
            Presenter.SwitchTable(DealerRoster.Mammon);
            yield return WaitForTable();

            Press("ActionButton");   // deal
            yield return WaitForTable();
            for (int guard = 0; guard < 12 && Presenter.Game.Phase != GamePhase.NamingJoker; guard++)
            {
                if (IsActive("CheckToDrawButton")) Press("CheckToDrawButton");
                else if (Presenter.Game.Phase == GamePhase.Drawing) Press("ActionButton");
                else if (IsActive("PassButton")) Press("PassButton");
                yield return WaitForTable();
            }
            Assert.AreEqual(GamePhase.NamingJoker, Presenter.Game.Phase);
            JokerPickerView picker = UnityEngine.Object.FindFirstObjectByType<JokerPickerView>(FindObjectsInactive.Include);
            Assert.IsTrue(picker.IsOpen);
            Assert.AreEqual(Rank.Ace, picker.Pick.Card.Rank, "The picker opens on the best card.");
            var rect = (RectTransform)picker.transform;
            Assert.LessOrEqual(-rect.anchoredPosition.y + rect.rect.height, 270, "The picker fits the screen.");

            Press("JokerRankForward");
            yield return WaitForTable();
            Assert.AreEqual(Rank.Two, picker.Pick.Card.Rank);
            Press("JokerRankBack");
            yield return WaitForTable();
            Press("JokerConfirm");
            yield return WaitForTable();

            Assert.AreEqual(GamePhase.RoundOver, Presenter.Game.Phase);
            Assert.IsFalse(picker.IsOpen);
            Assert.AreEqual(HandCategory.ThreeOfAKind, Presenter.Game.LastRound.Showdown.Player.Category);
            CardView joker = Find<Transform>("PlayerHand").GetComponentsInChildren<CardView>()[4];
            Assert.IsTrue(joker.HasMark, "The named card wears the fool's cap.");
            Assert.AreEqual(Rank.Ace, joker.FaceShown?.Rank);
            Assert.AreEqual(3, Presenter.Sinner.Jokers, "A won hand: one joker more.");
        }

        private sealed class NoShuffle : IShuffler
        {
            public void Shuffle<T>(IList<T> items) { }
        }

        private static IEnumerable<Card> Cards(string notation)
        {
            foreach (string token in notation.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (token.StartsWith("JK"))
                {
                    yield return Card.Joker(int.Parse(token.Substring(2)));
                    continue;
                }
                string rank = token.Substring(0, token.Length - 1);
                Rank r = rank == "A" ? Rank.Ace : rank == "K" ? Rank.King : rank == "Q" ? Rank.Queen : rank == "J" ? Rank.Jack : (Rank)int.Parse(rank);
                Suit s = token[token.Length - 1] == 'C' ? Suit.Clubs : token[token.Length - 1] == 'D' ? Suit.Diamonds
                    : token[token.Length - 1] == 'H' ? Suit.Hearts : Suit.Spades;
                yield return new Card(r, s);
            }
        }

        private static TableView Table => UnityEngine.Object.FindFirstObjectByType<TableView>();

        private static IEnumerator WaitForTable()
        {
            float started = Time.time;
            yield return null;
            while (Table != null && Table.IsBusy)
            {
                Assert.Less(Time.time - started, SceneTimeout, "Table animations never finished.");
                yield return null;
            }
        }

        private static void Press(string name)
        {
            Button button = Find<Button>(name);
            Assert.IsTrue(button.gameObject.activeInHierarchy && button.interactable, $"{name} cannot be pressed.");
            button.onClick.Invoke();
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
