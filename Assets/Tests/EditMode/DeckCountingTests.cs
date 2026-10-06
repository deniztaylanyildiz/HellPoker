using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Cheats;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Events;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;
using HellPoker.Presentation;
using HellPoker.Presentation.Settings;
using HellPoker.Presentation.Ui;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// The deck goes on from hand to hand (cards can be counted): dealt, thrown and drawn cards do not come back until the deck is
    /// too thin for another hand, when the demon shuffles all 52; a new table (Lucifer, a fall, a resumed run) brings a fresh deck.
    /// Between hands the player may SHUFFLE for 10 years — once before each hand, not below 300 years, not with the soul on the
    /// table. The Jester's deck is shuffled every hand (nothing to count, no SHUFFLE). A cheat or the Bone Die on an empty deck
    /// does nothing, and throws nothing.
    /// </summary>
    public class DeckCountingTests
    {
        private static HellPokerGame Game(Dealer dealer = null, Sinner sinner = null, GameRules rules = null, int seed = 11) =>
            HellPokerGameFactory.Create(rules ?? new GameRules(luciferGateYears: 0), dealer ?? DealerRoster.Mammon, seed, sinner: sinner ?? new Sinner(SinnerRoster.Peasant));

        private static void FoldAndNext(HellPokerGame game)
        {
            game.Bet(BetAction.Fold);
            game.NextRound();
        }

        [Test]
        public void TheDeck_GoesOnFromHandToHand_TheDealtCardsDoNotComeBack()
        {
            HellPokerGame game = Game();
            Assert.AreEqual(52, game.DeckCount, "A new table: a fresh deck.");
            game.PlaceBet();
            Assert.AreEqual(42, game.DeckCount);
            Assert.IsFalse(game.DeckShuffledThisHand);
            var first = game.PlayerHand.Concat(game.HouseHand).ToList();
            FoldAndNext(game);
            Assert.AreEqual(42, game.DeckCount, "Between hands nothing comes back.");
            game.PlaceBet();
            Assert.AreEqual(32, game.DeckCount);
            Assert.IsFalse(game.PlayerHand.Concat(game.HouseHand).Any(first.Contains), "Last hand's cards are gone from the deck.");
        }

        [Test]
        public void ADeckTooThinForAHand_IsShuffled_Back52_AsTheHandBegins()
        {
            HellPokerGame game = Game();
            Assert.AreEqual(2 * 5 + 2 * 3 + CheatTable.MostCardsACheatDeals, game.CardsForAHand, "Mammon: two hands, two draws of three, a cheat's two.");
            int hands = 0;
            while (true)
            {
                game.PlaceBet();
                hands++;
                if (game.DeckShuffledThisHand) break;
                Assert.GreaterOrEqual(game.DeckCount + 10, game.CardsForAHand - 0, "Never dealt from a deck that could run dry.");
                FoldAndNext(game);
                Assert.Less(hands, 10);
            }
            Assert.AreEqual(42, game.DeckCount);
            Assert.AreEqual(1, game.AutoShuffles);

            Assert.AreEqual(2 * 5 + 2 * 4 + CheatTable.MostCardsACheatDeals, Game(DealerRoster.Lilith).CardsForAHand, "Lilith's draw of four.");
            HellPokerGame die = Game();
            var effects = new RunEffects();
            effects.AddRelic(RelicIds.BoneDie);
            die.UseEffects(effects);
            Assert.AreEqual(19, die.CardsForAHand, "The Bone Die's redraw is kept spare too.");
        }

        [Test]
        public void ANewTable_LucifersOrAnother_DealsFromAFreshDeck()
        {
            foreach (Dealer dealer in DealerRoster.All.Append(DealerRoster.Lucifer))
                Assert.AreEqual(52, Game(dealer).DeckCount, dealer.Id);
        }

        [Test]
        public void WithoutTheRule_EveryHandIsAFreshShuffle()
        {
            HellPokerGame game = Game(rules: new GameRules(luciferGateYears: 0, continuousDeck: false));
            for (int i = 0; i < 3; i++)
            {
                game.PlaceBet();
                Assert.AreEqual(42, game.DeckCount);
                FoldAndNext(game);
            }
            Assert.AreEqual(ShuffleRefusal.NoDeckToCount, game.WhyNoShuffle());
        }

        [Test]
        public void Shuffle_CostsTenYears_AllFiftyTwoBack_OnceBeforeEachHand()
        {
            HellPokerGame game = Game();
            game.PlaceBet();
            Assert.AreEqual(ShuffleRefusal.NotBetweenHands, game.WhyNoShuffle());
            Assert.IsFalse(game.Shuffle());
            FoldAndNext(game);
            int years = game.Years;
            Assert.AreEqual(ShuffleRefusal.None, game.WhyNoShuffle());
            Assert.IsTrue(game.Shuffle());
            Assert.AreEqual(years + 10, game.Years);
            Assert.AreEqual(52, game.DeckCount);
            Assert.AreEqual(ShuffleRefusal.AlreadyShuffled, game.WhyNoShuffle());
            Assert.IsFalse(game.Shuffle());
            Assert.AreEqual(years + 10, game.Years, "Refused: nothing paid.");
            game.PlaceBet();
            FoldAndNext(game);
            Assert.AreEqual(ShuffleRefusal.None, game.WhyNoShuffle(), "Once more before the next hand.");
        }

        [Test]
        public void Shuffle_NotBelow300Years_NotWithTheSoulOnTheTable()
        {
            HellPokerGame game = Game();
            game.TakeOver(299, 3);
            Assert.AreEqual(ShuffleRefusal.TooFewYears, game.WhyNoShuffle());
            game.TakeOver(300, 3);
            Assert.AreEqual(ShuffleRefusal.None, game.WhyNoShuffle());
            game.TakeOver(2100, 3);
            Assert.AreEqual(ShuffleRefusal.SoulAtStake, game.WhyNoShuffle());
            game.TakeOver(1995, 3);
            Assert.AreEqual(ShuffleRefusal.SoulAtStake, game.WhyNoShuffle(), "Ten years would put the soul on the table.");
        }

        [Test]
        public void TheJestersDeck_IsShuffledEveryHand_NothingToCount()
        {
            HellPokerGame game = Game(sinner: new Sinner(SinnerRoster.Jester));
            Assert.AreEqual(ShuffleRefusal.NoDeckToCount, game.WhyNoShuffle());
            for (int i = 0; i < 4; i++)
            {
                game.PlaceBet();
                Assert.AreEqual(44, game.DeckCount, "54 cards, a fresh shuffle every hand.");
                FoldAndNext(game);
            }
        }

        // ------------------------------------------------------------------ an empty deck

        private sealed class FirstChoice : IRandomSource
        {
            public int Next(int maxExclusive) => 0;
        }

        private sealed class NoShuffle : IShuffler
        {
            public void Shuffle<T>(IList<T> items) { }
        }

        [Test]
        public void OnAnEmptyDeck_NoCheatThrows_AndNothingIsDealt()
        {
            IHandEvaluator evaluator = new WildJokerEvaluator(HandEvaluator.CreateDefault());
            foreach (ICheat cheat in DealerRoster.All.Append(DealerRoster.Lucifer).SelectMany(d => d.Cheats.Cheats))
            {
                Hand player = TestCards.Hand("KS KH 2C 5D 9C");
                Hand house = TestCards.Hand("2D 2H 5S 7H 9D");
                var deck = new Deck(new NoShuffle(), new Card[0]);
                var table = new CheatTable(player, house, deck, evaluator, new FirstChoice(), new CheatMarks(), 50, 2, new[] { 4 },
                    ShowdownResult.Resolve(evaluator.Evaluate(player), evaluator.Evaluate(house)), 2, 100, new HouseDrawStrategy());
                Assert.DoesNotThrow(() =>
                {
                    if (cheat.CanApply(table)) cheat.Apply(table);
                }, cheat.Id);
                Assert.IsFalse(table.RedealPlayerCard(0, out _));
                Assert.AreEqual(0, deck.Count);
            }
        }

        [Test]
        public void OnAnEmptyDeck_TheBoneDieDoesNothing_AndTheDrawStandsPat()
        {
            var rules = new GameRules(1000, 5000, luciferGateYears: 0);
            var game = new HellPokerGame(rules, TestDecks.Stacked("KS KH 2C 5D 9C 2D 2H 5S 7H 9D"), HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)), new HouseDrawStrategy(rules.MaxDiscards), PayoutTable.CreateDefault());
            var effects = new RunEffects();
            effects.AddRelic(RelicIds.BoneDie);
            game.UseEffects(effects);
            game.PlaceBet();
            Assert.AreEqual(0, game.DeckCount);
            Assert.IsFalse(game.CanRedraw(0));
            Assert.IsNull(game.Redraw(0));
            game.CheckToDraw();
            Assert.IsFalse(game.CanDraw(new[] { 2 }, out _), "No card to draw.");
            Assert.DoesNotThrow(() => game.Draw(new int[0]));
        }

        // ------------------------------------------------------------------ the table

        [Test]
        public void TheTable_CountsTheDeck_OffersShuffle_AndLogsBothShuffles()
        {
            var view = new FakeTableView();
            var logs = new MemoryRunLogSink();
            HellPokerGame game = null;
            var presenter = new TablePresenter((d, sinner) => game = HellPokerGameFactory.Create(new GameRules(luciferGateYears: 0), d, 21, sinner: sinner),
                view, runLog: logs);
            try
            {
                presenter.StartNewRun(DealerRoster.Mammon, SinnerRoster.Peasant);
                Assert.AreEqual(52, view.DeckCount);
                Assert.AreEqual(string.Format(UiText.ShuffleButtonFormat, 10), view.ShuffleLabel);
                Assert.IsFalse(view.ShuffleLocked);

                view.PressShuffle();
                Assert.AreEqual(1010, game.Years);
                Assert.AreEqual(string.Format(UiText.ShuffledFormat, 10), view.Message);
                Assert.IsTrue(view.ShuffleLocked);
                view.PressShuffle();
                Assert.AreEqual(UiText.ShuffleAlreadyDone, view.Message);
                Assert.AreEqual(1010, game.Years);

                view.PressAction();   // deal
                Assert.AreEqual(42, view.DeckCount);
                Assert.IsNull(view.ShuffleLabel, "Only between hands.");
                for (int guard = 0; guard < 8 && !game.DeckShuffledThisHand; guard++)
                {
                    view.PressBet(BetAction.Fold);
                    view.PressAction();   // next hand
                    view.PressAction();   // deal
                }
                Assert.IsTrue(game.DeckShuffledThisHand);
                StringAssert.Contains(UiText.DeckRanOut, view.Message);
                string log = presenter.Log.ToText();
                StringAssert.Contains("SHUFFLE (the player)", log);
                StringAssert.Contains("deck ran out", log);
            }
            finally
            {
                presenter.Dispose();
            }
        }

        [Test]
        public void TheJestersTable_ShowsNoCounter_AndNoShuffle()
        {
            var view = new FakeTableView();
            var presenter = new TablePresenter((d, sinner) => HellPokerGameFactory.Create(new GameRules(luciferGateYears: 0), d, 21, sinner: sinner), view);
            try
            {
                presenter.StartNewRun(DealerRoster.Mammon, SinnerRoster.Jester);
                Assert.AreEqual(-1, view.DeckCount);
                Assert.IsNull(view.ShuffleLabel);
                presenter.ShuffleDeck();
                Assert.AreEqual(UiText.ShuffleJesterDeck, view.Message);
            }
            finally
            {
                presenter.Dispose();
            }
        }
    }
}
