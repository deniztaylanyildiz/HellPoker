using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;

namespace HellPoker.Core.Draw
{
    public sealed class CardExchanger : ICardExchanger
    {
        private readonly IDiscardPolicy _policy;

        public CardExchanger(IDiscardPolicy policy)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public bool CanExchange(Hand hand, IReadOnlyCollection<int> discardIndices, IDeck deck, out string reason)
        {
            if (hand == null) throw new ArgumentNullException(nameof(hand));
            if (discardIndices == null) throw new ArgumentNullException(nameof(discardIndices));
            if (deck == null) throw new ArgumentNullException(nameof(deck));

            if (discardIndices.Any(index => index < 0 || index >= Hand.Size))
            {
                reason = $"Card positions must be between 0 and {Hand.Size - 1}.";
                return false;
            }

            if (discardIndices.Distinct().Count() != discardIndices.Count)
            {
                reason = "The same card cannot be discarded twice.";
                return false;
            }

            if (discardIndices.Count > deck.Count)
            {
                reason = "Not enough cards left in the deck.";
                return false;
            }

            return _policy.Allows(hand, discardIndices, out reason);
        }

        public ExchangeResult Exchange(Hand hand, IReadOnlyCollection<int> discardIndices, IDeck deck)
        {
            if (!CanExchange(hand, discardIndices, deck, out string reason))
                throw new InvalidOperationException(reason);

            int[] indices = discardIndices.OrderBy(index => index).ToArray();
            Card[] discarded = indices.Select(index => hand[index]).ToArray();
            IReadOnlyList<Card> drawn = deck.Draw(indices.Length);

            return new ExchangeResult(hand.Replace(indices, drawn), indices, discarded, drawn);
        }
    }
}
