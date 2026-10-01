using System.Collections.Generic;
using HellPoker.Core.Cards;

namespace HellPoker.Core.Draw
{
    public sealed class ExchangeResult
    {
        public Hand Hand { get; }
        public IReadOnlyList<Card> Discarded { get; }
        public IReadOnlyList<Card> Drawn { get; }

        /// <summary>Hand positions that received new cards, aligned with <see cref="Drawn"/>.</summary>
        public IReadOnlyList<int> ReplacedIndices { get; }

        public ExchangeResult(Hand hand, IReadOnlyList<int> replacedIndices, IReadOnlyList<Card> discarded, IReadOnlyList<Card> drawn)
        {
            Hand = hand;
            ReplacedIndices = replacedIndices;
            Discarded = discarded;
            Drawn = drawn;
        }
    }
}
