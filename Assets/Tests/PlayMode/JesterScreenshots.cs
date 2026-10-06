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
    /// Screenshots of the Jester (60–66), on stacked decks: the four class cards, two jokers in hand (the warning), a joker in hand
    /// (WITH JOKER), the showdown's picker, the named card with its cap and the badge's count, the badge's hover box, the demon's
    /// two jokers. HELLPOKER_LANG=tr for Turkish; HELLPOKER_SHOTS for the folder (as <see cref="HellPokerScreenshots"/>).
    /// </summary>
    [Explicit, Category("Screenshots")]
    public class JesterScreenshots
    {
        private const string Rest = "3S 6D 10S 8H 3H QC 8C 7C 6H 4S";
        private const string TwoJokers = "JK1 JK2 AS AH 9D  2C 5D 7H 9S JC  " + Rest;
        private const string OneJoker = "AS AH 5C 9D JK1  KS KH KD 2C 4D  " + Rest;
        private const string NoPicker = "AS AH 5C 9D JK1  JK2 JK3 KS KH KD  JK4 6D 10S 8H 3H QC 8C 7C 6H 4S";
        private const string DemonsJokers = "2C 5D 7H 9S JC  JK1 JK2 AS AH AD  JK3 6D 10S 8H 3H QC 8C 7C 6H 4S";

        private static TablePresenter Presenter =>
            (TablePresenter)typeof(HellPokerBootstrap).GetField("_tablePresenter", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(UnityEngine.Object.FindFirstObjectByType<HellPokerBootstrap>());

        [UnityTest, Timeout(300000)]
        public IEnumerator CaptureTheJester()
        {
            HellPokerBootstrap.BatchStore.Clear();
            if (Environment.GetEnvironmentVariable("HELLPOKER_LANG") == "tr")
                HellPokerBootstrap.BatchStore.SetString("settings.language", "Turkish");
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);

            Find<Button>("NewGameButton").onClick.Invoke();
            yield return new WaitForSeconds(0.6f);
            Find<Button>("ChooseDealer0").onClick.Invoke();
            yield return new WaitForSeconds(1f);
            yield return HellPokerScreenshots.Shot("60_sinners_four");
            Find<Button>("ChooseSinner3").onClick.Invoke();
            yield return HellPokerScreenshots.WaitForTable();

            // Two jokers in the opening cards: the warning stays under the message.
            Deal(TwoJokers);
            Press("ActionButton");
            yield return HellPokerScreenshots.WaitForTable();
            yield return new WaitForSeconds(0.5f);
            yield return HellPokerScreenshots.Shot("62_two_jokers_warning");
            Press("FoldButton");
            yield return HellPokerScreenshots.WaitForTable();
            Press("ActionButton");   // next hand
            yield return HellPokerScreenshots.WaitForTable();

            // One joker: WITH JOKER at the draw, then the picker at the showdown, then the named card.
            Deal(OneJoker);
            Press("ActionButton");
            yield return HellPokerScreenshots.WaitForTable();
            Press("CheckToDrawButton");
            yield return HellPokerScreenshots.WaitForTable();
            yield return new WaitForSeconds(0.4f);
            yield return HellPokerScreenshots.Shot("61_joker_in_hand");
            Press("ActionButton");   // stand
            yield return HellPokerScreenshots.WaitForTable();
            for (int guard = 0; guard < 6 && Presenter.Game.Phase != GamePhase.NamingJoker; guard++)
            {
                Press("PassButton");
                yield return HellPokerScreenshots.WaitForTable();
            }
            yield return new WaitForSeconds(0.6f);
            yield return HellPokerScreenshots.Shot("63_joker_picker");
            Press("JokerConfirm");
            yield return HellPokerScreenshots.WaitForTable();
            yield return new WaitForSeconds(0.8f);
            yield return HellPokerScreenshots.Shot("64_joker_named");
            var badge = UnityEngine.Object.FindFirstObjectByType<SinnerBadgeView>();
            var tooltip = (GameObject)typeof(SinnerBadgeView).GetField("_tooltip", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(badge);
            tooltip.SetActive(true);
            yield return HellPokerScreenshots.Shot("65_jester_badge");
            tooltip.SetActive(false);
            Press("ActionButton");
            yield return HellPokerScreenshots.WaitForTable();

            // The demon's two jokers: the fool's laugh.
            Deal(DemonsJokers);
            Press("ActionButton");
            yield return HellPokerScreenshots.WaitForTable();
            Press("CheckToDrawButton");
            yield return HellPokerScreenshots.WaitForTable();
            Press("ActionButton");
            yield return HellPokerScreenshots.WaitForTable();
            for (int guard = 0; guard < 6 && Presenter.Game.Phase != GamePhase.RoundOver; guard++)
            {
                Press("PassButton");
                yield return new WaitForSeconds(1.6f);
            }
            yield return HellPokerScreenshots.Shot("66_demon_two_jokers");
            yield return HellPokerScreenshots.WaitForTable();
            Press("ActionButton");
            yield return HellPokerScreenshots.WaitForTable();

            // The player's one joker against the demon's two: no picker — it becomes its best card at once, the demon laughs no more.
            Deal(NoPicker);
            Press("ActionButton");
            yield return HellPokerScreenshots.WaitForTable();
            Press("CheckToDrawButton");
            yield return HellPokerScreenshots.WaitForTable();
            Press("ActionButton");
            yield return HellPokerScreenshots.WaitForTable();
            for (int guard = 0; guard < 6 && Presenter.Game.Phase != GamePhase.RoundOver; guard++)
            {
                Press("PassButton");
                yield return HellPokerScreenshots.WaitForTable();
            }
            yield return new WaitForSeconds(0.8f);
            yield return HellPokerScreenshots.Shot("67_no_picker");
        }

        /// <summary>The next table deals exactly these cards (with the run's other jokers at the bottom); a fresh seat at Mammon's.</summary>
        private static void Deal(string stack)
        {
            Func<Dealer, Sinner, IHellPokerGame> stacked = (dealer, sinner) =>
            {
                List<Card> cards = Cards(stack).ToList();
                int have = cards.Count(c => c.IsJoker);
                for (int serial = have + 1; serial <= sinner.Jokers; serial++) cards.Add(Card.Joker(serial));
                GameRules rules = dealer.ApplyTo(new GameRules());
                return new HellPokerGame(rules, new Deck(new NoShuffle(), Enumerable.Reverse(cards)), HandEvaluator.CreateDefault(),
                    new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)), new HouseDrawStrategy(rules.MaxDiscards), dealer.Payouts,
                    null, null, null, sinner);
            };
            typeof(TablePresenter).GetField("_createGame", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(Presenter, stacked);
            Presenter.SwitchTable(DealerRoster.Mammon);
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

        private static void Press(string name)
        {
            Button button = Find<Button>(name);
            Assert.IsTrue(button.gameObject.activeInHierarchy, $"{name} cannot be pressed.");
            button.onClick.Invoke();
        }

        private static T Find<T>(string name) where T : Component
        {
            T found = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(c => c.name == name).OrderByDescending(c => c.gameObject.activeInHierarchy).FirstOrDefault();
            Assert.IsNotNull(found, $"'{name}' not found in scene.");
            return found;
        }
    }
}
