using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Chapters;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Core.Sinners;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Settings;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Demo 3.1: a chapter table's hand ends by its strength — the tiers, the cards that hit, the hands played to each tier.</summary>
    public class HandFanfareTests
    {
        // ------------------------------------------------------------------ the tiers

        [TestCase(HandCategory.HighCard, HandTier.Plain)]
        [TestCase(HandCategory.OnePair, HandTier.Plain)]
        [TestCase(HandCategory.TwoPair, HandTier.TwoPair)]
        [TestCase(HandCategory.ThreeOfAKind, HandTier.Trips)]
        [TestCase(HandCategory.Straight, HandTier.Straight)]
        [TestCase(HandCategory.Flush, HandTier.Flush)]
        [TestCase(HandCategory.FullHouse, HandTier.Full)]
        [TestCase(HandCategory.FourOfAKind, HandTier.Quads)]
        [TestCase(HandCategory.StraightFlush, HandTier.Blaze)]
        [TestCase(HandCategory.RoyalFlush, HandTier.Blaze)]
        [TestCase(HandCategory.DeadMansHand, HandTier.Blaze)]
        public void AWin_IsAsLoudAsItsHand(HandCategory category, HandTier tier)
        {
            Assert.AreEqual(tier, HandFanfare.TierOf(HandOutcome.Win, category, HandCategory.HighCard));
        }

        [Test]
        public void ALoss_PlaysOnlyToTheHousesTripsOrBetter_AndAFoldOrAPushPlaysNothing()
        {
            Assert.AreEqual(HandTier.HouseStrong, HandFanfare.TierOf(HandOutcome.Loss, HandCategory.OnePair, HandCategory.ThreeOfAKind));
            Assert.AreEqual(HandTier.HouseStrong, HandFanfare.TierOf(HandOutcome.Loss, HandCategory.OnePair, HandCategory.RoyalFlush));
            Assert.AreEqual(HandTier.None, HandFanfare.TierOf(HandOutcome.Loss, HandCategory.HighCard, HandCategory.TwoPair));
            Assert.AreEqual(HandTier.None, HandFanfare.TierOf(HandOutcome.Loss, null, null), "a bust House hand");
            Assert.AreEqual(HandTier.None, HandFanfare.TierOf(HandOutcome.Fold, null, HandCategory.FourOfAKind));
            Assert.AreEqual(HandTier.None, HandFanfare.TierOf(HandOutcome.Push, HandCategory.FullHouse, HandCategory.FullHouse));
            Assert.AreEqual(HandTier.Plain, HandFanfare.TierOf(HandOutcome.Win, null, null), "the imp folded: no hand to show");
        }

        [Test]
        public void EveryTier_HoldsLongerThanTheOneBefore()
        {
            HandTier[] wins = { HandTier.Plain, HandTier.TwoPair, HandTier.Trips, HandTier.Straight, HandTier.Flush, HandTier.Full, HandTier.Quads, HandTier.Blaze };
            for (int i = 1; i < wins.Length; i++)
                Assert.Greater(HandFanfare.SecondsOf(wins[i]), HandFanfare.SecondsOf(wins[i - 1]), wins[i].ToString());
            Assert.AreEqual(0f, HandFanfare.SecondsOf(HandTier.None));
        }

        // ------------------------------------------------------------------ the cards that hit

        private static readonly IHandEvaluator Judge = new WildJokerEvaluator(HandEvaluator.CreateDefault());

        private static IReadOnlyList<IReadOnlyList<int>> Groups(string cards, out HandCategory category)
        {
            HandEvaluation judged = Judge.Evaluate(new Hand(TestCards.Cards(cards)));
            category = judged.Category;
            return HandFanfare.GroupsOf(judged.Hand, judged.Category);
        }

        [Test]
        public void AFullHouse_HitsItsTripsThenItsPair()
        {
            var groups = Groups("4H KS 4S KD KC", out HandCategory category);
            Assert.AreEqual(HandCategory.FullHouse, category);
            Assert.AreEqual(2, groups.Count);
            CollectionAssert.AreEquivalent(new[] { 1, 3, 4 }, groups[0], "the kings first");
            CollectionAssert.AreEquivalent(new[] { 0, 2 }, groups[1]);
        }

        [Test]
        public void TwoPair_HitHighFirst_AndAStraight_CardByCardFromTheLeft()
        {
            var two = Groups("7C AS 2S 7H AD", out _);
            CollectionAssert.AreEquivalent(new[] { 1, 4 }, two[0], "the aces");
            CollectionAssert.AreEquivalent(new[] { 0, 3 }, two[1]);
            var straight = Groups("9S 5D 7C 6H 8S", out HandCategory category);
            Assert.AreEqual(HandCategory.Straight, category);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, straight.Select(g => g.Single()));
            var high = Groups("3D 5C KH 7S 9C", out _);
            CollectionAssert.AreEqual(new[] { 2 }, high.Single(), "the high card alone");
        }

        [Test]
        public void AJoker_CountsAsTheCardItBecame()
        {
            // Three kings and a joker: the joker is the fourth king — four of a kind, and it hits with them.
            var cards = TestCards.Cards("KS KD KC 4H").ToList();
            cards.Insert(2, Card.Joker(1));
            HandEvaluation judged = Judge.Evaluate(new Hand(cards));
            Assert.AreEqual(HandCategory.FourOfAKind, judged.Category);
            Assert.AreEqual(HandTier.Quads, HandFanfare.TierOf(HandOutcome.Win, judged.Category, null));
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, HandFanfare.GroupsOf(judged.Hand, judged.Category).Single(), "the joker's place too");
        }

        // ------------------------------------------------------------------ played at a demon's table (stacked decks)

        private const string HouseNothing = "2D 4H 6S 8C 10D";
        private const string HouseQuads = "9C 9D 9H 9S 2C";
        // No card twice: the stacked deck is refused (a fresh one dealt) if any card repeats.
        private const string Draws = " 3S 5H 7D 8H JD";

        private FakeChapterMapView _map;
        private FakeChapterPanelView _panel;
        private FakeTableView _tableView;
        private TablePresenter _table;
        private ChapterPresenter _chapters;

        [SetUp]
        public void SetUp()
        {
            _map = new FakeChapterMapView();
            _panel = new FakeChapterPanelView();
            _tableView = new FakeTableView();
            _table = new TablePresenter((d, s) => HellPokerGameFactory.Create(GameRules.Default, d, 1, sinner: s), _tableView);
            _chapters = new ChapterPresenter(_map, _panel, () => (_table, _tableView), null, new AudioTests.FakeAudio(), GameRules.Default, () => 4242);
        }

        [TearDown]
        public void TearDown() => _chapters.Dispose();

        /// <summary>A hand at Mammon's table from a stacked deck (the player's five, the House's five, the draws), played to its end by passing.</summary>
        private void PlayAtTheDemon(string player, string house, bool fold = false, bool raise = false)
        {
            _chapters.Dispose();
            var store = new MemoryStore();
            var archive = new ChapterArchive(store);
            archive.SaveRun(new ChapterSave
            {
                ClassId = Sinners.Peasant.ClassId, Seed = 7, Chapter = 1, Coins = 40, Years = 1300, Match = "boss", BossBarStart = 1300,
                BossBar = 1300, NodeDone = false
            });
            _chapters = new ChapterPresenter(_map, _panel, () => (_table, _tableView), null, new AudioTests.FakeAudio(), GameRules.Default, () => 4242, archive);
            _chapters.Continue();
            Assert.IsTrue(_chapters.IsAtTable);
            var game = (HellPokerGame)_table.Game;
            var head = TestCards.Cards(player + " " + house + Draws).ToList();
            game.StackTheDeal(head.Concat(Deck.CreateStandardCards().Where(c => !head.Contains(c))).ToList());
            _tableView.Fanfares.Clear();
            _tableView.SinnerMoods.Clear();
            _table.PerformAction();   // the deal
            if (fold)
            {
                _table.Bet(BetAction.Fold);
                return;
            }
            int guard = 0;
            while (_table.Game.Phase != GamePhase.RoundOver && !_table.Game.IsGameOver && guard++ < 30)
            {
                GamePhase before = _table.Game.Phase;
                bool deciding = before == GamePhase.PlayerReveal || before == GamePhase.DrawReveal || before == GamePhase.HouseReveal;
                // Raising every decision it may (a big stake); a refused raise (the table full) passes instead.
                if (raise && deciding) _table.Bet(BetAction.Raise);
                else if (raise && before == GamePhase.HouseReRaise) _table.Bet(BetAction.Call);
                if (!raise || _table.Game.Phase == before && _table.Game.Phase != GamePhase.HouseReRaise && (deciding || before == GamePhase.Drawing)) _table.PerformAction();
            }
        }

        [TestCase("AS AD 7C 3C JS", HandTier.Plain)]
        [TestCase("AS AD 7C 7H JS", HandTier.TwoPair)]
        [TestCase("QS QD QC 4C 3H", HandTier.Trips)]
        [TestCase("9S 8D 7C 6H 5S", HandTier.Straight)]
        [TestCase("2H 6H 9H JH KH", HandTier.Flush)]
        [TestCase("KS KD KC 5S 5C", HandTier.Full)]
        [TestCase("QS QD QC QH JS", HandTier.Quads)]
        [TestCase("AS KS QS JS 10S", HandTier.Blaze)]
        public void EachTier_IsPlayedForItsHand(string player, HandTier tier)
        {
            PlayAtTheDemon(player, HouseNothing);
            Assert.AreEqual(HandOutcome.Win, _tableView.Band?.Outcome, "the hand was won");
            HandFanfare fanfare = _tableView.Fanfares.Single();
            Assert.AreEqual(tier, fanfare.Tier);
            Assert.IsTrue(fanfare.CoinsToBar, "at a demon's table the coins fly at his bar");
            Assert.AreEqual(tier == HandTier.Plain, fanfare.Title == null, "the big name from two pair up");
            Assert.IsTrue(fanfare.Groups.SelectMany(g => g).All(i => i >= 0 && i < Hand.Size));
            Assert.Contains(tier >= HandTier.Straight ? SinnerMood.Elated : SinnerMood.Pleased, _tableView.SinnerMoods);
            Assert.IsFalse(fanfare.BigLoss);
            Assert.AreEqual(tier == HandTier.Flush ? Suit.Hearts : (Suit?)null, fanfare.FlushSuit, "a flush rains its own suit");
        }

        [Test]
        public void TwoPairTripsStraightAndFlush_EachHaveTheirOwnMotion_AndItsCards()
        {
            PlayAtTheDemon("AS AD 7C 7H JS", HouseNothing);
            HandFanfare two = _tableView.Fanfares.Single();
            Assert.AreEqual(2, two.Groups.Count, "two pair: each pair on its own");
            CollectionAssert.AreEquivalent(new[] { 0, 1 }, two.Groups[0], "the aces first");
            PlayAtTheDemon("QS QD QC 4C 3H", HouseNothing);
            HandFanfare trips = _tableView.Fanfares.Single();
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, trips.Groups.Single(), "trips: the three together");
            PlayAtTheDemon("9S 8D 7C 6H 5S", HouseNothing);
            HandFanfare straight = _tableView.Fanfares.Single();
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, straight.Groups.Select(g => g.Single()), "a straight: a wave from the left");
            PlayAtTheDemon("2H 6H 9H JH KH", HouseNothing);
            HandFanfare flush = _tableView.Fanfares.Single();
            Assert.AreEqual(Suit.Hearts, flush.FlushSuit, "a flush: its suit rains");
            CollectionAssert.AllItemsAreUnique(new[] { two.Tier, trips.Tier, straight.Tier, flush.Tier }, "four motions, not one tier");
        }

        [Test]
        public void ABigLoss_PlaysWithTheHousesPairToo()
        {
            PlayAtTheDemon("3D 5C 7H JC KD", "AS AD 9C 6D 2C", raise: true);
            Assert.AreEqual(HandOutcome.Loss, _tableView.Band?.Outcome);
            HandFanfare fanfare = _tableView.Fanfares.Single();
            Assert.AreEqual(HandTier.None, fanfare.Tier, "a pair is no strong hand of the House's");
            Assert.IsTrue(fanfare.BigLoss, "but four antes and more lost: the big loss");
            Assert.IsTrue(fanfare.Plays);
            Assert.Contains(SinnerMood.Hurt, _tableView.SinnerMoods);
        }

        [Test]
        public void ASmallLossToAPair_PlaysNothing()
        {
            PlayAtTheDemon("3D 5C 7H JC KD", "AS AD 9C 6D 2C");
            Assert.AreEqual(HandOutcome.Loss, _tableView.Band?.Outcome);
            Assert.IsEmpty(_tableView.Fanfares, "an ante lost to a pair: no animation");
        }

        [Test]
        public void BigLoss_IsFourAntesAndUp()
        {
            Assert.IsTrue(HandFanfare.IsBigLoss(HandOutcome.Loss, 20, 5));
            Assert.IsFalse(HandFanfare.IsBigLoss(HandOutcome.Loss, 19, 5));
            Assert.IsFalse(HandFanfare.IsBigLoss(HandOutcome.Fold, 40, 5), "a fold is no loss at the showdown");
            Assert.IsFalse(HandFanfare.IsBigLoss(HandOutcome.Loss, 40, 0));
        }

        [Test]
        public void ALossToTheHousesQuads_HitsOnItsSide_AndTheSinnerWinces()
        {
            PlayAtTheDemon("3D 5C 7H JC KD", HouseQuads);
            HandFanfare fanfare = _tableView.Fanfares.Single();
            Assert.AreEqual(HandTier.HouseStrong, fanfare.Tier);
            Assert.IsTrue(fanfare.OnHouseSide);
            Assert.IsTrue(fanfare.BigLoss, "the House's quads take ten antes: the House's cards hit, then the edge flickers");
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, fanfare.Groups.Single(), "the House's four nines");
            Assert.Contains(SinnerMood.Hurt, _tableView.SinnerMoods);
        }

        [Test]
        public void AFold_PlaysNothing()
        {
            PlayAtTheDemon("3D 5C 7H JC KD", HouseQuads, fold: true);
            Assert.IsEmpty(_tableView.Fanfares);
            Assert.IsFalse(_tableView.SinnerMoods.Contains(SinnerMood.Hurt), "a fold moves nothing");
        }

        [Test]
        public void TheSimulatorsTable_PlaysNoTiers()
        {
            _table.StartNewRun(Dealers.DealerRoster.Mammon, SinnerRoster.Peasant);
            var game = (HellPokerGame)_table.Game;
            var head = TestCards.Cards("QS QD QC QH JS " + HouseNothing + Draws).ToList();
            game.StackTheDeal(head.Concat(Deck.CreateStandardCards().Where(c => !head.Contains(c))).ToList());
            _table.PerformAction();
            int guard = 0;
            while (_table.Game.Phase != GamePhase.RoundOver && !_table.Game.IsGameOver && guard++ < 30) _table.PerformAction();
            Assert.IsEmpty(_tableView.Fanfares, "the Boss Simulator's table is as it was");
            Assert.IsEmpty(_tableView.SinnerMoods);
        }

        // ------------------------------------------------------------------ the setting

        [Test]
        public void ScreenShake_IsOnByDefault_SavedAndApplied()
        {
            try
            {
                var store = new MemoryStore();
                var settings = new GameSettings(store);
                Assert.IsTrue(settings.ScreenShake);
                Assert.IsTrue(Presentation.Animation.ScreenShake.Enabled);
                settings.ToggleScreenShake();
                Assert.IsFalse(Presentation.Animation.ScreenShake.Enabled, "off: no shake, no flash");
                Assert.IsFalse(new GameSettings(store).ScreenShake, "saved");
            }
            finally
            {
                Presentation.Animation.ScreenShake.Set(true);
            }
        }
    }
}
