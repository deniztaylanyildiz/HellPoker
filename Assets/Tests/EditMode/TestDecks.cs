using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Randomness;

namespace HellPoker.Core.Tests
{
    internal static class TestDecks
    {
        private sealed class NoShuffle : IShuffler
        {
            public void Shuffle<T>(IList<T> items) { }
        }

        /// <summary>A deck that deals exactly the given cards, first card first. Reset restores the same order.</summary>
        public static Deck Stacked(string cardsInDrawOrder)
        {
            // Deck draws from the end of its list.
            return new Deck(new NoShuffle(), TestCards.Cards(cardsInDrawOrder).Reverse());
        }
    }
}
