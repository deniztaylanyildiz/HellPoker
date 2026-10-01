using System;
using System.Linq;
using HellPoker.Core.Dealers;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Presentation;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    public class DealerTests
    {
        private static readonly GameRules Table = new GameRules(1000, 2000, 10, 200, forcedRaiseYears: 250);

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
            Assert.AreEqual(2000, rules.DamnationYears);
            Assert.AreEqual(10, rules.MinStake);
            Assert.AreEqual(200, rules.MaxStake);
            Assert.AreEqual(250, rules.ForcedRaiseYears);
            Assert.AreEqual(4, rules.MaxDiscards);
            Assert.AreEqual(3, rules.HouseRevealDecisions);
        }

        [Test]
        public void Mammon_PlaysByTheBook()
        {
            Dealer mammon = DealerRoster.Mammon;

            Assert.AreEqual(3, mammon.MaxDiscards);
            Assert.AreEqual(3, mammon.HouseRevealDecisions);
            Assert.AreEqual(100, mammon.Payouts.GetYearsAdded(HandCategory.OnePair, 100));
            Assert.AreEqual(50, mammon.Payouts.GetFoldPenalty(100));
            Assert.AreEqual(5, mammon.Payouts.GetMultiplier(HandCategory.Flush));
        }

        [Test]
        public void Belial_PaysMore_ChargesMore_AndHidesHisHand()
        {
            Dealer belial = DealerRoster.Belial;

            Assert.AreEqual(1, belial.HouseRevealDecisions);
            Assert.AreEqual(150, belial.Payouts.GetYearsAdded(HandCategory.OnePair, 100));
            Assert.AreEqual(38, belial.Payouts.GetYearsAdded(HandCategory.OnePair, 25), "37.5 rounds up.");
            Assert.Greater(belial.Payouts.GetMultiplier(HandCategory.Flush), DealerRoster.Mammon.Payouts.GetMultiplier(HandCategory.Flush));
        }

        [Test]
        public void Lilith_AllowsFourDiscards_ButFoldingCostsTheWholeStake()
        {
            Dealer lilith = DealerRoster.Lilith;

            Assert.AreEqual(4, lilith.MaxDiscards);
            Assert.AreEqual(100, lilith.Payouts.GetFoldPenalty(100));
        }

        [Test]
        public void Factory_BuildsGameUnderTheDealersRules()
        {
            HellPokerGame game = HellPokerGameFactory.Create(Table, DealerRoster.Lilith, seed: 1);
            game.PlaceBet(50);
            while (game.Phase == GamePhase.PlayerReveal)
                game.Bet(BetAction.Pass);

            Assert.AreEqual(4, game.Rules.MaxDiscards);
            Assert.IsTrue(game.CanDraw(new[] { 0, 1, 2, 3 }, out _), "Lilith lets the player throw back four cards.");
        }

        [Test]
        public void FoldUnderLilith_AddsTheWholeStake()
        {
            HellPokerGame game = HellPokerGameFactory.Create(Table, DealerRoster.Lilith, seed: 1);
            game.PlaceBet(50);

            game.Bet(BetAction.Fold);

            Assert.AreEqual(1050, game.Years);
        }

        [Test]
        public void Dealer_RejectsInvalidRules()
        {
            Assert.Throws<ArgumentException>(() => new Dealer("", 3, 3, PayoutTable.CreateDefault()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Dealer("x", 6, 3, PayoutTable.CreateDefault()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Dealer("x", 3, 5, PayoutTable.CreateDefault()));
            Assert.Throws<ArgumentNullException>(() => new Dealer("x", 3, 3, null));
        }

        [Test]
        public void PayoutTable_DefaultsMatchTheClassicLedger()
        {
            PayoutTable payouts = PayoutTable.CreateDefault();

            Assert.AreEqual(25, payouts.GetYearsAdded(HandCategory.Flush, 25));
            Assert.AreEqual(13, payouts.GetFoldPenalty(25), "Half of 25 rounds up.");
        }

        [Test]
        public void DealerCard_TraitsDescribeTheActualRules()
        {
            var card = DealerCards.Describe(DealerRoster.Belial);

            Assert.AreEqual("BELIAL", card.Name);
            Assert.IsNotEmpty(card.Title);
            Assert.That(card.Traits, Has.Some.Contains("3 cards"));
            Assert.That(card.Traits, Has.Some.Contains("only 1"));
            Assert.That(card.Traits, Has.Some.Contains("1.5 × the stake"));
        }

        [Test]
        public void DealerCard_ForLilith_MentionsFourCards_AndFullFold()
        {
            var card = DealerCards.Describe(DealerRoster.Lilith);

            Assert.That(card.Traits, Has.Some.Contains("4 cards"));
            Assert.That(card.Traits, Has.Some.Contains("Fold: + the stake"));
        }
    }
}
