using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Randomness;

namespace HellPoker.Core.Cards
{
    public sealed class Deck : IDeck
    {
        private readonly IShuffler _shuffler;
        private readonly Card[] _fullSet;
        private readonly List<Card> _cards;

        /// <summary>Standard 52-card deck, shuffled on creation.</summary>
        public Deck(IShuffler shuffler) : this(shuffler, CreateStandardCards())
        {
        }

        /// <summary>Custom card set, e.g. for tests or special game modes. Shuffled on creation.</summary>
        public Deck(IShuffler shuffler, IEnumerable<Card> cards)
        {
            _shuffler = shuffler ?? throw new ArgumentNullException(nameof(shuffler));
            if (cards == null) throw new ArgumentNullException(nameof(cards));

            _fullSet = cards.ToArray();
            _cards = new List<Card>(_fullSet.Length);
            Reset();
        }

        public int Count => _cards.Count;

        public Card Draw()
        {
            if (_cards.Count == 0)
                throw new InvalidOperationException("The deck is empty.");

            int last = _cards.Count - 1;
            Card card = _cards[last];
            _cards.RemoveAt(last);
            return card;
        }

        public IReadOnlyList<Card> Draw(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (count > _cards.Count)
                throw new InvalidOperationException($"Cannot draw {count} cards, only {_cards.Count} left.");

            var drawn = new Card[count];
            for (int i = 0; i < count; i++)
                drawn[i] = Draw();
            return drawn;
        }

        public void Reset()
        {
            _cards.Clear();
            _cards.AddRange(_fullSet);
            _shuffler.Shuffle(_cards);
        }

        public static IEnumerable<Card> CreateStandardCards()
        {
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                yield return new Card(rank, suit);
        }
    }
}
