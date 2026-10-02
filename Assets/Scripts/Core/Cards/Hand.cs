using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HellPoker.Core.Cards
{
    /// <summary>Immutable five-card hand. Exchanging cards produces a new hand.</summary>
    public sealed class Hand : IReadOnlyList<Card>
    {
        public const int Size = 5;

        private readonly Card[] _cards;

        public Hand(IEnumerable<Card> cards)
        {
            if (cards == null) throw new ArgumentNullException(nameof(cards));

            _cards = cards.ToArray();
            if (_cards.Length != Size)
                throw new ArgumentException($"A hand must contain exactly {Size} cards, got {_cards.Length}.", nameof(cards));
            if (_cards.Distinct().Count() != Size)
                throw new ArgumentException("A hand cannot contain duplicate cards.", nameof(cards));
        }

        public Card this[int index] => _cards[index];

        public int Count => Size;

        public bool Contains(Card card)
        {
            return Array.IndexOf(_cards, card) >= 0;
        }

        /// <summary>Returns a new hand where each card at <paramref name="indices"/>[i] is replaced by <paramref name="replacements"/>[i].</summary>
        public Hand Replace(IReadOnlyList<int> indices, IReadOnlyList<Card> replacements)
        {
            if (indices == null) throw new ArgumentNullException(nameof(indices));
            if (replacements == null) throw new ArgumentNullException(nameof(replacements));
            if (indices.Count != replacements.Count)
                throw new ArgumentException("Each replaced index needs exactly one replacement card.");

            var next = (Card[])_cards.Clone();
            for (int i = 0; i < indices.Count; i++)
            {
                int index = indices[i];
                if (index < 0 || index >= Size)
                    throw new ArgumentOutOfRangeException(nameof(indices), index, "Card index out of range.");
                next[index] = replacements[i];
            }

            return new Hand(next);
        }

        /// <summary>A new hand with the card at <paramref name="index"/> replaced.</summary>
        public Hand With(int index, Card card) => Replace(new[] { index }, new[] { card });

        public int IndexOf(Card card) => Array.IndexOf(_cards, card);

        public IEnumerator<Card> GetEnumerator()
        {
            return ((IEnumerable<Card>)_cards).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString()
        {
            return string.Join(" ", _cards);
        }
    }
}
