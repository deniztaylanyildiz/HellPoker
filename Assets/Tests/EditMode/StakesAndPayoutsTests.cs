using System;
using System.Collections.Generic;
using HellPoker.Core.Betting;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    public class StakeScaleTests
    {
        [TestCase(2000, 200)]
        [TestCase(1000, 100)]
        [TestCase(1999, 100)]
        [TestCase(999, 50)]
        [TestCase(650, 50)]
        [TestCase(500, 50)]
        [TestCase(499, 25)]
        [TestCase(340, 25)]
        [TestCase(250, 25)]
        [TestCase(249, 20)]
        [TestCase(180, 10)]
        [TestCase(60, 10)]
        public void Unit_IsATenth_RoundedToAReadableStep(int years, int unit)
        {
            Assert.AreEqual(unit, StakeScale.Default.UnitFor(years));
        }

        [TestCase(1000, 100)]
        [TestCase(40, 10)]
        [TestCase(7, 7)]
        public void Ante_IsOneUnit_OrAllIn(int years, int ante)
        {
            Assert.AreEqual(ante, StakeScale.Default.AnteFor(years));
        }

        [TestCase(1000, 300)]
        [TestCase(650, 195)]
        [TestCase(251, 75)]
        [TestCase(15, 10)]
        [TestCase(7, 7)]
        public void Cap_IsThirtyPercentOfTheSentence_ButNeverBelowTheAnte(int years, int cap)
        {
            Assert.AreEqual(cap, StakeScale.Default.CapFor(years));
        }

        [Test]
        public void Scale_IsConfigurable()
        {
            var scale = new StakeScale(divisor: 5, minimumUnit: 5, tableCapPercent: 30, tiers: new[] { new StakeScale.Tier(0, 5) });

            Assert.AreEqual(200, scale.UnitFor(1000));
            Assert.AreEqual(300, scale.CapFor(1000));
        }

        [Test]
        public void InvalidScale_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StakeScale(divisor: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StakeScale(tableCapPercent: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StakeScale(tableCapPercent: 101));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StakeScale.Tier(0, 0));
        }
    }

    public class PayoutTableTests
    {
        [Test]
        public void StandardMultipliers()
        {
            PayoutTable payouts = PayoutTable.CreateDefault();

            Assert.AreEqual(1, payouts.GetMultiplier(HandCategory.HighCard));
            Assert.AreEqual(1, payouts.GetMultiplier(HandCategory.OnePair));
            Assert.AreEqual(8, payouts.GetMultiplier(HandCategory.FullHouse));
            Assert.AreEqual(10, payouts.GetMultiplier(HandCategory.FourOfAKind));
            Assert.AreEqual(15, payouts.GetMultiplier(HandCategory.StraightFlush));
            Assert.AreEqual(20, payouts.GetMultiplier(HandCategory.RoyalFlush));
        }

        [Test]
        public void Win_MultipliesTheAnte_RaisesPayOneToOne()
        {
            PayoutTable payouts = PayoutTable.CreateDefault();

            Assert.AreEqual(1000, payouts.GetYearsForgiven(HandCategory.FullHouse, stake: 300, ante: 100, currentYears: 2000),
                "300 + 100 × 7 — not 300 × 8 = 2400.");
            Assert.AreEqual(300, payouts.GetYearsForgiven(HandCategory.OnePair, 300, 100, 2000), "A ×1 hand returns the table.");
            Assert.AreEqual(100 + 100 * 4, payouts.GetYearsForgiven(HandCategory.Flush, 100, 100, 2000));
        }

        [Test]
        public void Win_NeverForgivesMoreThanTheSentence()
        {
            Assert.AreEqual(400, PayoutTable.CreateDefault().GetYearsForgiven(HandCategory.FullHouse, 300, 100, 400));
        }

        [Test]
        public void Loss_IsTheSameFormula_OnTheHousesHand()
        {
            PayoutTable payouts = PayoutTable.CreateDefault();

            Assert.AreEqual(300, payouts.GetYearsAdded(HandCategory.OnePair, 300, 100));
            Assert.AreEqual(1000, payouts.GetYearsAdded(HandCategory.FullHouse, 300, 100));
        }

        [Test]
        public void Loss_AppliesLossPercent_RoundingUp()
        {
            var payouts = new PayoutTable(PayoutTable.DefaultMultipliers, HandCategory.DeadMansHand, lossPercent: 125);

            Assert.AreEqual(1250, payouts.GetYearsAdded(HandCategory.FullHouse, 300, 100));
            Assert.AreEqual(32, payouts.GetYearsAdded(HandCategory.HighCard, 25, 25), "31.25 rounds up.");
            Assert.AreEqual(63, payouts.GetYearsAdded(HandCategory.TwoPair, 25, 25), "(25 + 25) × 1.25 = 62.5 rounds up.");
        }

        [Test]
        public void TheAnteMustBePartOfTheStake()
        {
            PayoutTable payouts = PayoutTable.CreateDefault();

            Assert.Throws<ArgumentOutOfRangeException>(() => payouts.GetYearsAdded(HandCategory.Flush, 100, 200));
            Assert.Throws<ArgumentOutOfRangeException>(() => payouts.GetYearsForgiven(HandCategory.Flush, 100, -1, 1000));
        }

        [Test]
        public void HouseWinningWithDeadMansHand_CountsAsTheTopMultiplier()
        {
            Assert.AreEqual(50 + 50 * 19, PayoutTable.CreateDefault().GetYearsAdded(HandCategory.DeadMansHand, 50, 50));
        }

        [Test]
        public void PlayerWinningWithDeadMansHand_ForgivesEverything()
        {
            Assert.AreEqual(1234, PayoutTable.CreateDefault().GetYearsForgiven(HandCategory.DeadMansHand, 10, 10, 1234));
        }

        [Test]
        public void Fold_CostsHalfBeforeTheDraw_AllAfter_ByDefault()
        {
            PayoutTable payouts = PayoutTable.CreateDefault();

            Assert.AreEqual(13, payouts.GetFoldPenalty(25, afterDraw: false), "Half of 25 rounds up.");
            Assert.AreEqual(25, payouts.GetFoldPenalty(25, afterDraw: true));
        }

        [Test]
        public void LeastOutcomes_UseTheWeakestMultiplier()
        {
            PayoutTable payouts = PayoutTable.CreateDefault();

            Assert.AreEqual(200, payouts.GetLeastYearsForgiven(200, 100, 1000));
            Assert.AreEqual(150, payouts.GetLeastYearsForgiven(200, 100, 150), "Never more than the sentence.");
            Assert.AreEqual(200, payouts.GetLeastYearsAdded(200, 100));
            Assert.AreEqual(250, new PayoutTable(PayoutTable.DefaultMultipliers, HandCategory.DeadMansHand, lossPercent: 125)
                .GetLeastYearsAdded(200, 100));
        }

        [Test]
        public void InvalidSettings_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PayoutTable(PayoutTable.DefaultMultipliers, HandCategory.DeadMansHand, lossPercent: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PayoutTable(new Dictionary<HandCategory, int> { { HandCategory.HighCard, 0 } }, HandCategory.DeadMansHand));
        }
    }

    public class HouseBettingTests
    {
        /// <summary>Returns the queued numbers in order.</summary>
        private sealed class QueuedRandom : IRandomSource
        {
            private readonly Queue<int> _values;
            public QueuedRandom(params int[] values) => _values = new Queue<int>(values);
            public int Next(int maxExclusive) => _values.Dequeue();
        }

        private static HandEvaluation Evaluate(string hand) => HandEvaluator.CreateDefault().Evaluate(TestCards.Hand(hand));

        private static readonly HouseBettingStyle Honest = new HouseBettingStyle(HandCategory.TwoPair, strongPercent: 70, bluffPercent: 5);

        [Test]
        public void StrongHand_ReRaises_WhenTheRollIsUnderTheStrongPercent()
        {
            var strategy = new HandStrengthBettingStrategy(Honest, new QueuedRandom(69, 70));

            Assert.IsTrue(strategy.WantsToReRaise(Evaluate("KS KH 4D 4C 2H")));
            Assert.IsFalse(strategy.WantsToReRaise(Evaluate("KS KH 4D 4C 2H")));
        }

        [Test]
        public void WeakHand_Bluffs_WhenTheRollIsUnderTheBluffPercent()
        {
            var strategy = new HandStrengthBettingStrategy(Honest, new QueuedRandom(4, 5));

            Assert.IsTrue(strategy.WantsToReRaise(Evaluate("KS KH 9D 4C 2H")));
            Assert.IsFalse(strategy.WantsToReRaise(Evaluate("KS KH 9D 4C 2H")));
        }

        [Test]
        public void SilentStyle_NeverReRaises_AndDoesNotRoll()
        {
            var strategy = new HandStrengthBettingStrategy(HouseBettingStyle.Silent, new QueuedRandom());

            Assert.IsFalse(strategy.WantsToReRaise(Evaluate("AS AH AD AC 2H")));
        }

        [Test]
        public void InvalidStyle_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HouseBettingStyle(HandCategory.TwoPair, 101, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HouseBettingStyle(HandCategory.TwoPair, 50, -1));
        }
    }
}
