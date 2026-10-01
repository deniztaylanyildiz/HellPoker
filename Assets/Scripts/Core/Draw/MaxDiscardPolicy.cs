using System;
using System.Collections.Generic;
using HellPoker.Core.Cards;

namespace HellPoker.Core.Draw
{
    public sealed class MaxDiscardPolicy : IDiscardPolicy
    {
        /// <summary>Classic five-card draw limit.</summary>
        public const int ClassicLimit = 3;

        private readonly int _maxDiscards;

        public MaxDiscardPolicy(int maxDiscards = ClassicLimit)
        {
            if (maxDiscards < 0 || maxDiscards > Hand.Size)
                throw new ArgumentOutOfRangeException(nameof(maxDiscards), maxDiscards, $"Must be between 0 and {Hand.Size}.");
            _maxDiscards = maxDiscards;
        }

        public bool Allows(Hand hand, IReadOnlyCollection<int> discardIndices, out string reason)
        {
            if (discardIndices.Count > _maxDiscards)
            {
                reason = $"You may discard at most {_maxDiscards} cards.";
                return false;
            }

            reason = null;
            return true;
        }
    }
}
