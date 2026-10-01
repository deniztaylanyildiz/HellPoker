using HellPoker.Core.Evaluation;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    public class HandEvaluatorTests
    {
        private HandEvaluator _evaluator;

        [SetUp]
        public void SetUp()
        {
            _evaluator = HandEvaluator.CreateDefault();
        }

        private HandEvaluation Eval(string hand) => _evaluator.Evaluate(TestCards.Hand(hand));

        [TestCase("AS AC 8S 8C 9D", HandCategory.DeadMansHand)]
        [TestCase("AS AC 8S 8C AH", HandCategory.DeadMansHand)]
        [TestCase("AH AD 8H 8D 9C", HandCategory.TwoPair)]
        [TestCase("AS KS QS JS 10S", HandCategory.RoyalFlush)]
        [TestCase("9H 8H 7H 6H 5H", HandCategory.StraightFlush)]
        [TestCase("AD 2D 3D 4D 5D", HandCategory.StraightFlush)]
        [TestCase("7S 7H 7D 7C 2H", HandCategory.FourOfAKind)]
        [TestCase("KS KH KD 4C 4H", HandCategory.FullHouse)]
        [TestCase("2C 9C JC 4C KC", HandCategory.Flush)]
        [TestCase("10S 9H 8D 7C 6H", HandCategory.Straight)]
        [TestCase("AS 2H 3D 4C 5H", HandCategory.Straight)]
        [TestCase("AS KH QD JC 10H", HandCategory.Straight)]
        [TestCase("QS QH QD 4C 2H", HandCategory.ThreeOfAKind)]
        [TestCase("JS JH 4D 4C 2H", HandCategory.TwoPair)]
        [TestCase("10S 10H 4D 3C 2H", HandCategory.OnePair)]
        [TestCase("KS QH 4D 3C 2H", HandCategory.HighCard)]
        [TestCase("QS KS AS 2S 3H", HandCategory.HighCard)]
        public void Evaluate_RecognisesCategory(string hand, HandCategory expected)
        {
            Assert.AreEqual(expected, Eval(hand).Category);
        }

        [Test]
        public void DeadMansHand_BeatsRoyalFlush()
        {
            Assert.That(Eval("AS AC 8S 8C 2D") > Eval("AH KH QH JH 10H"));
        }

        [Test]
        public void RoyalFlush_BeatsStraightFlush()
        {
            Assert.That(Eval("AH KH QH JH 10H") > Eval("KS QS JS 10S 9S"));
        }

        [Test]
        public void Wheel_IsLowestStraight()
        {
            Assert.That(Eval("AS 2H 3D 4C 5H") < Eval("2S 3H 4D 5C 6H"));
        }

        [Test]
        public void FullHouse_ComparesTripsBeforePair()
        {
            Assert.That(Eval("3S 3H 3D 2C 2H") > Eval("2S 2D 2C AC AH"));
        }

        [Test]
        public void TwoPair_ComparesHighPairThenLowPairThenKicker()
        {
            Assert.That(Eval("KS KH 3D 3C 2H") > Eval("QS QH JD JC AH"));
            Assert.That(Eval("KS KH 4D 4C 2H") > Eval("KC KD 3S 3H AH"));
            Assert.That(Eval("KS KH 4D 4C 5H") > Eval("KC KD 4S 4H 2D"));
        }

        [Test]
        public void OnePair_ComparesKickers()
        {
            Assert.That(Eval("9S 9H AD 4C 2H") > Eval("9C 9D KS QC JH"));
        }

        [Test]
        public void HighCard_ComparesAllCardsInOrder()
        {
            Assert.That(Eval("AS JH 8D 5C 3H") > Eval("AC JD 8S 5H 2D"));
        }

        [Test]
        public void IdenticalRanks_DifferentSuits_Tie()
        {
            Assert.AreEqual(0, Eval("AS KH 8D 5C 3H").CompareTo(Eval("AC KD 8S 5H 3D")));
        }
    }
}
