using System;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Randomness;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    public class DeckTests
    {
        private static Deck CreateDeck(int seed = 42)
        {
            return new Deck(new FisherYatesShuffler(new SystemRandomSource(seed)));
        }

        [Test]
        public void NewDeck_Has52UniqueCards()
        {
            var deck = CreateDeck();
            var cards = deck.Draw(52);

            Assert.AreEqual(52, cards.Distinct().Count());
            Assert.AreEqual(0, deck.Count);
        }

        [Test]
        public void Draw_FromEmptyDeck_Throws()
        {
            var deck = CreateDeck();
            deck.Draw(52);

            Assert.Throws<InvalidOperationException>(() => deck.Draw());
        }

        [Test]
        public void Reset_RestoresAllCards()
        {
            var deck = CreateDeck();
            deck.Draw(20);
            deck.Reset();

            Assert.AreEqual(52, deck.Count);
        }

        [Test]
        public void SameSeed_ProducesSameOrder()
        {
            CollectionAssert.AreEqual(CreateDeck(7).Draw(52), CreateDeck(7).Draw(52));
        }

        [Test]
        public void DealHand_TakesFiveCards()
        {
            var deck = CreateDeck();
            Hand hand = deck.DealHand();

            Assert.AreEqual(5, hand.Count);
            Assert.AreEqual(47, deck.Count);
        }

        [Test]
        public void Hand_RejectsDuplicatesAndWrongSize()
        {
            Assert.Throws<ArgumentException>(() => TestCards.Hand("AS AS KD 4C 2H"));
            Assert.Throws<ArgumentException>(() => TestCards.Hand("AS KD 4C 2H"));
        }
    }
}
