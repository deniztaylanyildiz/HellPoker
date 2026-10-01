using HellPoker.Core.Draw;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    public class HouseDrawStrategyTests
    {
        private readonly HouseDrawStrategy _strategy = new HouseDrawStrategy();

        [TestCase("AS KS QS JS 10S", new int[0], TestName = "Royal flush: stand")]
        [TestCase("2C 9C JC 4C KC", new int[0], TestName = "Flush: stand")]
        [TestCase("10S 9H 8D 7C 6H", new int[0], TestName = "Straight: stand")]
        [TestCase("KS KH KD 4C 4H", new int[0], TestName = "Full house: stand")]
        [TestCase("7S 7H 7D 7C 2H", new int[0], TestName = "Quads: stand")]
        [TestCase("QS 4C QH 2H QD", new[] { 1, 3 }, TestName = "Trips: drop the two kickers")]
        [TestCase("JS JH 4D 4C 2H", new[] { 4 }, TestName = "Two pair: drop the kicker")]
        [TestCase("AS AC 8S 8C 2H", new[] { 4 }, TestName = "Dead Man's Hand: keeps the four")]
        [TestCase("3S 10H 10D KC 2H", new[] { 0, 3, 4 }, TestName = "Pair: drop the three others")]
        [TestCase("2H 9H JH 4H KC", new[] { 4 }, TestName = "Four-flush: drop the odd suit")]
        [TestCase("9S 8H 7D 6C KH", new[] { 4 }, TestName = "Open-ended straight draw")]
        [TestCase("AS 2H 3D 4C 9H", new[] { 4 }, TestName = "Wheel draw")]
        [TestCase("AS 9H 6D 4C 2H", new[] { 2, 3, 4 }, TestName = "Nothing: keep the two highest")]
        public void ChooseDiscards(string hand, int[] expected)
        {
            CollectionAssert.AreEqual(expected, _strategy.ChooseDiscards(TestCards.Hand(hand)));
        }

        [Test]
        public void ChooseDiscards_RespectsLimit_DroppingLowestFirst()
        {
            var strategy = new HouseDrawStrategy(maxDiscards: 2);

            CollectionAssert.AreEqual(new[] { 3, 4 }, strategy.ChooseDiscards(TestCards.Hand("AS 9H 6D 4C 2H")));
        }
    }
}
