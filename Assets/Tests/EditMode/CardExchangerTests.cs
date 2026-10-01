using System;
using HellPoker.Core.Cards;
using HellPoker.Core.Draw;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    public class CardExchangerTests
    {
        [Test]
        public void Exchange_ReplacesOnlySelectedCards()
        {
            var exchanger = new CardExchanger(new MaxDiscardPolicy());
            Hand hand = TestCards.Hand("AS AC 2D 8C 3H");
            IDeck deck = TestDecks.Stacked("8S 9D KD");

            ExchangeResult result = exchanger.Exchange(hand, new[] { 4, 2 }, deck);

            Assert.AreEqual(TestCards.Hand("AS AC 8S 8C 9D").ToString(), result.Hand.ToString());
            CollectionAssert.AreEqual(TestCards.Cards("2D 3H"), result.Discarded);
            CollectionAssert.AreEqual(new[] { 2, 4 }, result.ReplacedIndices);
            Assert.AreEqual(1, deck.Count);
        }

        [Test]
        public void Exchange_NoDiscards_KeepsHand()
        {
            var exchanger = new CardExchanger(new MaxDiscardPolicy());
            Hand hand = TestCards.Hand("AS AC 2D 8C 3H");

            ExchangeResult result = exchanger.Exchange(hand, new int[0], TestDecks.Stacked("QD KD"));

            Assert.AreEqual(hand.ToString(), result.Hand.ToString());
            Assert.IsEmpty(result.Drawn);
        }

        [Test]
        public void Exchange_OverPolicyLimit_IsRejected()
        {
            var exchanger = new CardExchanger(new MaxDiscardPolicy(3));
            Hand hand = TestCards.Hand("AS AC 2D 8C 3H");
            IDeck deck = TestDecks.Stacked("KD QD JD 10D");

            Assert.IsFalse(exchanger.CanExchange(hand, new[] { 0, 1, 2, 3 }, deck, out string reason));
            Assert.IsNotNull(reason);
            Assert.Throws<InvalidOperationException>(() => exchanger.Exchange(hand, new[] { 0, 1, 2, 3 }, deck));
            Assert.AreEqual(4, deck.Count);
        }

        [TestCase(new[] { 5 })]
        [TestCase(new[] { -1 })]
        [TestCase(new[] { 1, 1 })]
        public void Exchange_InvalidIndices_AreRejected(int[] indices)
        {
            var exchanger = new CardExchanger(new MaxDiscardPolicy(5));
            Hand hand = TestCards.Hand("AS AC 2D 8C 3H");

            Assert.IsFalse(exchanger.CanExchange(hand, indices, TestDecks.Stacked("KD QD"), out _));
        }
    }
}
