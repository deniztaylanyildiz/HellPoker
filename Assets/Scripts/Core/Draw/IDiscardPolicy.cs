using System.Collections.Generic;
using HellPoker.Core.Cards;

namespace HellPoker.Core.Draw
{
    /// <summary>Game rule deciding which discards are legal (e.g. "at most 3 cards").</summary>
    public interface IDiscardPolicy
    {
        bool Allows(Hand hand, IReadOnlyCollection<int> discardIndices, out string reason);
    }
}
