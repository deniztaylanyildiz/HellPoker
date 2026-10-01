using System;
using System.Linq;
using HellPoker.Core.Betting;
using HellPoker.Core.Dealers;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Presentation;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    public class DealerTests
    {
        private static readonly GameRules Table = new GameRules(1000, 2000, forcedRaiseYears: 250);

        [Test]
        public void Roster_HasThreeDistinctDealers()
        {
            var ids = DealerRoster.All.Select(d => d.Id).ToArray();

            CollectionAssert.AreEqual(new[] { DealerRoster.MammonId, DealerRoster.BelialId, DealerRoster.LilithId }, ids);
        }

        [Test]
        public void ApplyTo_KeepsTableNumbers_AndSetsHouseRules()
        {
            GameRules rules = DealerRoster.Lilith.ApplyTo(Table);

            Assert.AreEqual(1000, rules.StartingYears);
            Assert.AreEqual(1500, rules.SoulThreshold, "Lilith takes the soul earliest.");
            Assert.AreEqual(2500, rules.DamnationYears, "Soul line + the soul's worth.");
            Assert.AreEqual(250, rules.ForcedRaiseYears);
            Assert.AreSame(Table.Stakes, rules.Stakes);
            Assert.AreEqual(4, rules.MaxDiscards);
            Assert.AreEqual(2, rules.HouseCardsShown);
        }

        [Test]
        public void Mammon_PlaysByTheBook_AndHonestly()
        {
            Dealer mammon = DealerRoster.Mammon;

            Assert.AreEqual(3, mammon.MaxDiscards);
            Assert.AreEqual(2, mammon.HouseCardsShown);
            Assert.AreEqual(100, mammon.Payouts.LossPercent);
            Assert.AreEqual(50, mammon.Payouts.FoldPercentBeforeDraw);
            Assert.AreEqual(100, mammon.Payouts.FoldPercentAfterDraw);
            Assert.AreEqual(10, mammon.Payouts.GetMultiplier(HandCategory.FourOfAKind));
            Assert.AreEqual(20, mammon.Payouts.GetMultiplier(HandCategory.RoyalFlush));
            AssertTemper(mammon, 70, 5);
        }

        [Test]
        public void Belial_PaysMore_ShowsOneCard_AndBluffs()
        {
            Dealer belial = DealerRoster.Belial;

            Assert.AreEqual(1, belial.HouseCardsShown);
            Assert.AreEqual(100, belial.Payouts.LossPercent, "Symmetric losses: 150% was too harsh.");
            Assert.AreEqual(8, belial.Payouts.GetMultiplier(HandCategory.Flush));
            Assert.AreEqual(15, belial.Payouts.GetMultiplier(HandCategory.FourOfAKind));
            Assert.AreEqual(25, belial.Payouts.GetMultiplier(HandCategory.StraightFlush));
            Assert.AreEqual(30, belial.Payouts.GetMultiplier(HandCategory.RoyalFlush));
            AssertTemper(belial, 60, 30);
        }

        [Test]
        public void Lilith_AllowsFourDiscards_ButFoldingAlwaysCostsTheWholeStake()
        {
            Dealer lilith = DealerRoster.Lilith;

            Assert.AreEqual(4, lilith.MaxDiscards);
            Assert.AreEqual(2, lilith.HouseCardsShown);
            Assert.AreEqual(125, lilith.Payouts.LossPercent, "The hardest table: losses cost a quarter more.");
            Assert.AreEqual(100, lilith.Payouts.FoldPercentBeforeDraw);
            Assert.AreEqual(100, lilith.Payouts.FoldPercentAfterDraw);
            AssertTemper(lilith, 90, 10);
        }

        [Test]
        public void SoulLines_ComeEarlierAtHarderTables()
        {
            Assert.AreEqual(2000, DealerRoster.Mammon.SoulThreshold);
            Assert.AreEqual(1750, DealerRoster.Belial.SoulThreshold);
            Assert.AreEqual(1500, DealerRoster.Lilith.SoulThreshold);
            Assert.IsTrue(DealerRoster.Lilith.TakesSoulAt(1500));
            Assert.IsFalse(DealerRoster.Lilith.TakesSoulAt(1499));
        }

        [Test]
        public void DealerCard_CarriesTheSoulLine()
        {
            Assert.AreEqual(1750, DealerCards.Describe(DealerRoster.Belial).SoulThreshold);
        }

        private static void AssertTemper(Dealer dealer, int strong, int bluff)
        {
            Assert.AreEqual(HandCategory.TwoPair, dealer.Betting.StrongFrom);
            Assert.AreEqual(strong, dealer.Betting.StrongPercent);
            Assert.AreEqual(bluff, dealer.Betting.BluffPercent);
        }

        [Test]
        public void Factory_BuildsGameUnderTheDealersRules()
        {
            HellPokerGame game = HellPokerGameFactory.Create(Table, DealerRoster.Lilith, seed: 1);
            game.PlaceBet();
            while (game.Phase == GamePhase.PlayerReveal)
                game.Bet(BetAction.Pass);

            Assert.AreEqual(4, game.Rules.MaxDiscards);
            Assert.IsTrue(game.CanDraw(new[] { 0, 1, 2, 3 }, out _), "Lilith lets the player throw back four cards.");
        }

        [Test]
        public void FoldBeforeTheDraw_UnderLilith_AddsTheWholeStake()
        {
            HellPokerGame game = HellPokerGameFactory.Create(Table, DealerRoster.Lilith, seed: 1);
            game.PlaceBet();

            game.Bet(BetAction.Fold);

            Assert.AreEqual(1100, game.Years);
        }

        [Test]
        public void FoldBeforeTheDraw_UnderMammon_AddsHalf()
        {
            HellPokerGame game = HellPokerGameFactory.Create(Table, DealerRoster.Mammon, seed: 1);
            game.PlaceBet();

            game.Bet(BetAction.Fold);

            Assert.AreEqual(1050, game.Years);
        }

        [Test]
        public void Dealer_RejectsInvalidRules()
        {
            Assert.Throws<ArgumentException>(() => new Dealer("", 3, 2, PayoutTable.CreateDefault()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Dealer("x", 6, 2, PayoutTable.CreateDefault()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Dealer("x", 3, 5, PayoutTable.CreateDefault()));
            Assert.Throws<ArgumentNullException>(() => new Dealer("x", 3, 2, null));
        }

        [Test]
        public void Dealer_WithoutTemper_NeverReRaises()
        {
            Assert.AreEqual(0, new Dealer("x", 3, 2, PayoutTable.CreateDefault()).Betting.StrongPercent);
            Assert.AreEqual(0, new Dealer("x", 3, 2, PayoutTable.CreateDefault(), HouseBettingStyle.Silent).Betting.BluffPercent);
        }

        [Test]
        public void DealerCard_TraitsDescribeTheActualRules()
        {
            var card = DealerCards.Describe(DealerRoster.Belial);

            Assert.AreEqual("BELIAL", card.Name);
            Assert.IsNotEmpty(card.Title);
            Assert.That(card.Traits, Has.Some.Contains("3 cards"));
            Assert.That(card.Traits, Has.Some.Contains("only 1"));
            Assert.That(card.Traits, Has.Some.Contains("Royal ×30"));
            Assert.That(card.Traits, Has.Some.Contains("half before the draw, all after"));
            Assert.That(card.Traits, Has.Some.Contains("60%"));
            Assert.That(card.Traits, Has.Some.Contains("bluffs 30%"));
        }

        [Test]
        public void DealerCard_ForLilith_MentionsFourCards_AndFullFold()
        {
            var card = DealerCards.Describe(DealerRoster.Lilith);

            Assert.That(card.Traits, Has.Some.Contains("4 cards"));
            Assert.That(card.Traits, Has.Some.Contains("Fold: always all of the stake"));
            Assert.That(card.Traits, Has.Some.Contains("House's hand × 1.25"));
        }

        [Test]
        public void DealerCard_ForMammon_HasNoLossSurcharge()
        {
            var card = DealerCards.Describe(DealerRoster.Mammon);

            Assert.That(card.Traits, Has.Some.EndsWith("on the House's hand"));
        }
    }
}
