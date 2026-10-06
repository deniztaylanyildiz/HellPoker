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

        /// <summary>A hand dealt and folded: the deck is no longer whole (42 left).</summary>
        private static HellPokerGame Used(HellPokerGame game)
        {
            game.PlaceBet();
            FoldAndNext(game);
            return game;
        }

        [Test]
        public void Shuffle_CostsTenYears_AllFiftyTwoBack_OnceBeforeEachHand()
        {
            HellPokerGame game = Game();
            Assert.AreEqual(ShuffleRefusal.DeckFull, game.WhyNoShuffle(), "A whole deck: nothing to shuffle back.");
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
        public void Shuffle_NotWhenTheDemonShufflesAnyway()
        {
            HellPokerGame game = Game();
            while (game.DeckCount >= game.CardsForAHand) Used(game);
            Assert.AreEqual(ShuffleRefusal.ShuffleComing, game.WhyNoShuffle());
            Assert.AreEqual(UiText.ShuffleComing, UiText.ShuffleRefused(ShuffleRefusal.ShuffleComing, 300));
        }

        [Test]
        public void Shuffle_NotBelow300Years_NotWithTheSoulOnTheTable_NorIfItWouldPutItThere()
        {
            HellPokerGame game = Used(Game());
            game.TakeOver(299, 3);
            Assert.AreEqual(ShuffleRefusal.TooFewYears, game.WhyNoShuffle());
            game.TakeOver(300, 3);
            Assert.AreEqual(ShuffleRefusal.None, game.WhyNoShuffle());
            game.TakeOver(2100, 3);
            Assert.AreEqual(ShuffleRefusal.SoulOnTable, game.WhyNoShuffle());

            HellPokerGame belial = Used(Game(DealerRoster.Belial));
            belial.TakeOver(1743, 3);
            Assert.AreEqual(ShuffleRefusal.WouldStakeSoul, belial.WhyNoShuffle(), "1743 + 10 reaches Belial's line at 1750.");
            Assert.AreEqual("The shuffle's 10 years would take you to 1750: your soul would go on the table.",
                UiText.ShuffleRefused(ShuffleRefusal.WouldStakeSoul, 300, belial.Rules.ShuffleYears, belial.Rules.SoulThreshold));
            belial.TakeOver(1739, 3);
            Assert.AreEqual(ShuffleRefusal.None, belial.WhyNoShuffle());
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
                Assert.AreEqual(string.Format(UiText.DeckCountFormat, 52), view.DeckLabel);
                Assert.IsNull(view.ShuffleLabel, "A whole deck: no SHUFFLE.");
                view.PressShuffle();
                Assert.AreEqual(UiText.ShuffleDeckFull, view.Message);

                view.PressAction();   // deal
                Assert.AreEqual(string.Format(UiText.DeckCountFormat, 42), view.DeckLabel);
                Assert.IsNull(view.ShuffleLabel, "Only between hands.");
                view.PressBet(BetAction.Fold);
                view.PressAction();   // next hand
                Assert.AreEqual(string.Format(UiText.ShuffleButtonFormat, 10), view.ShuffleLabel);
                Assert.IsFalse(view.ShuffleLocked);
                int years = game.Years;
                view.PressShuffle();
                Assert.AreEqual(years + 10, game.Years);
                Assert.AreEqual(string.Format(UiText.ShuffledFormat, 10), view.Message);
                Assert.IsNull(view.ShuffleLabel, "Once before each hand (and the deck is whole again).");
                Assert.AreEqual(string.Format(UiText.DeckCountFormat, 52), view.DeckLabel);

                view.PressAction();   // deal
                for (int guard = 0; guard < 8 && !game.DeckShuffledThisHand; guard++)
                {
                    if (game.Phase == GamePhase.Betting && game.DeckCount < game.CardsForAHand)
                    {
                        Assert.IsNull(view.ShuffleLabel, "The demon shuffles anyway: no SHUFFLE.");
                        view.PressShuffle();
                        Assert.AreEqual(UiText.ShuffleComing, view.Message);
                    }
                    if (game.Phase != GamePhase.Betting)
                    {
                        view.PressBet(BetAction.Fold);
                        view.PressAction();   // next hand
                    }
                    view.PressAction();       // deal
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
        public void TheTable_NearTheSoulLine_ShowsShuffleDimmed_AndSaysWhy()
        {
            var view = new FakeTableView();
            HellPokerGame game = null;
            var presenter = new TablePresenter((d, sinner) => game = HellPokerGameFactory.Create(new GameRules(luciferGateYears: 0), d, 21, sinner: sinner), view);
            try
            {
                presenter.StartNewRun(DealerRoster.Belial, SinnerRoster.Peasant);
                view.PressAction();
                view.PressBet(BetAction.Fold);
                view.PressAction();
                game.TakeOver(1745, game.RoundNumber);
                presenter.SwitchTable(DealerRoster.Belial);
                Assert.AreEqual(string.Format(UiText.ShuffleButtonFormat, 10), view.ShuffleLabel, "Shown...");
                Assert.IsTrue(view.ShuffleLocked, "...but dimmed.");
                view.PressShuffle();
                Assert.AreEqual(string.Format(UiText.ShuffleWouldStakeSoulFormat, 10, 1750), view.Message);
                Assert.AreEqual(1745, game.Years);
            }
            finally
            {
                presenter.Dispose();
            }
        }

        [Test]
        public void TheCounter_ShowsNoNumberUnderTen_SHUFFLING_TheNumberComesBackWithAWholeDeck()
        {
            var view = new FakeTableView();
            HellPokerGame game = null;
            var presenter = new TablePresenter((d, sinner) => game = HellPokerGameFactory.Create(new GameRules(luciferGateYears: 0), d, 21, sinner: sinner), view);
            try
            {
                presenter.StartNewRun(DealerRoster.Mammon, SinnerRoster.Peasant);
                var cards = Deck.CreateStandardCards().ToList();
                game.RestoreDeck(cards.Take(10).ToList());
                presenter.SwitchTable(DealerRoster.Mammon);   // the run's deck (10 cards) comes along
                Assert.AreEqual(string.Format(UiText.DeckCountFormat, 10), view.DeckLabel);
                game.RestoreDeck(cards.Take(9).ToList());
                presenter.SwitchTable(DealerRoster.Mammon);
                Assert.AreEqual(UiText.DeckShufflingLabel, view.DeckLabel, "Nine: no number.");
                Assert.AreEqual(UiText.DeckShufflingHint, view.DeckHint);
                view.PressAction();   // the demon shuffles as the hand begins
                Assert.IsTrue(game.DeckShuffledThisHand);
                Assert.AreEqual(string.Format(UiText.DeckCountFormat, 42), view.DeckLabel, "A whole deck: the number is back.");
            }
            finally
            {
                presenter.Dispose();
            }
        }
        // ------------------------------------------------------------------ the deck belongs to the run

        [Test]
        public void ChangingTables_TheDeckGoesOn_NoFreeShuffle()
        {
            var view = new FakeTableView();
            HellPokerGame game = null;
            var presenter = new TablePresenter((d, sinner) => game = HellPokerGameFactory.Create(new GameRules(luciferGateYears: 0), d, 21, sinner: sinner), view);
            try
            {
                presenter.StartNewRun(DealerRoster.Mammon, SinnerRoster.Peasant);
                view.PressAction();
                view.PressBet(BetAction.Fold);
                view.PressAction();
                List<Card> left = game.DeckCards.ToList();
                Assert.AreEqual(42, left.Count);

                presenter.SwitchTable(DealerRoster.Belial);
                Assert.AreEqual(42, game.DeckCount, "Another demon, the same deck.");
                CollectionAssert.AreEqual(left, game.DeckCards);
                presenter.SwitchTable(DealerRoster.Mammon);
                CollectionAssert.AreEqual(left, game.DeckCards, "And back: still the same deck.");
                view.PressAction();
                CollectionAssert.AreEqual(left.Take(5), game.PlayerHand, "Dealt from where it was.");
            }
            finally
            {
                presenter.Dispose();
            }
        }

        [Test]
        public void TheSave_KeepsTheDeck_BetweenHandsAndMidHand_AGarbledOneIsAFreshShuffle()
        {
            var store = new MemoryStore();
            var archive = new RunArchive(store);
            var view = new FakeTableView();
            HellPokerGame game = null;
            var presenter = new TablePresenter((d, sinner) => game = HellPokerGameFactory.Create(new GameRules(luciferGateYears: 0), d, 21, sinner: sinner),
                view, archive: archive);
            List<Card> left;
            List<Card> inHand;
            try
            {
                presenter.StartNewRun(DealerRoster.Mammon, SinnerRoster.Peasant);
                view.PressAction();
                view.PressBet(BetAction.Fold);
                view.PressAction();
                RunSnapshot between = archive.LoadRun();
                CollectionAssert.AreEqual(game.DeckCards, between.DeckCards);
                StringAssert.Contains("deck=" + CardCodes.FormatAll(game.DeckCards.Take(3)), between.Encode());

                view.PressAction();   // a hand on the table: the save keeps its cards out of the deck
                inHand = game.PlayerHand.Concat(game.HouseHand).ToList();
                left = game.DeckCards.ToList();
                CollectionAssert.AreEqual(left, archive.LoadRun().DeckCards);
            }
            finally
            {
                presenter.Dispose();
            }

            var again = new FakeTableView();
            var resumed = new TablePresenter((d, sinner) => game = HellPokerGameFactory.Create(new GameRules(luciferGateYears: 0), d, 99, sinner: sinner),
                again, archive: archive);
            try
            {
                resumed.Resume(DealerRoster.Mammon, archive.LoadRun());   // the hand left mid-way is forfeit
                CollectionAssert.AreEqual(left, game.DeckCards, "The deck goes on; the left hand's cards do not come back.");
                Assert.IsFalse(game.DeckCards.Any(inHand.Contains));
            }
            finally
            {
                resumed.Dispose();
            }

            string text = new RunSnapshot("mammon", 900, 4, new RunStats(1000, "mammon"), deckCards: TestCards.Cards("AS 10H 3C").ToList()).Encode();
            StringAssert.Contains("deck=AS,10H,3C", text);
            foreach (string bad in new[] { "deck=AS,AS,3C", "deck=AS,XX", "deck=AS,1H" })
            {
                Assert.IsTrue(RunSnapshot.TryDecode(text.Replace("deck=AS,10H,3C", bad), out RunSnapshot read), bad + ": the run is still read");
                Assert.IsNull(read.DeckCards, bad + ": a fresh shuffle");
            }
            Assert.IsTrue(RunSnapshot.TryDecode(text.Replace("deck=AS,10H,3C\n", "").Replace("\ndeck=AS,10H,3C", ""), out RunSnapshot none));
            Assert.IsNull(none.DeckCards, "An older save: a fresh shuffle.");
        }
        [Test]
        public void TheJestersTable_ShowsNoCounter_AndNoShuffle()
        {
            var view = new FakeTableView();
            var presenter = new TablePresenter((d, sinner) => HellPokerGameFactory.Create(new GameRules(luciferGateYears: 0), d, 21, sinner: sinner), view);
            try
            {
                presenter.StartNewRun(DealerRoster.Mammon, SinnerRoster.Jester);
                Assert.IsNull(view.DeckLabel);
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
