using System.Collections.Generic;

namespace HellPoker.Core.Cards
{
    public interface IDeck
    {
        int Count { get; }

        Card Draw();

        IReadOnlyList<Card> Draw(int count);

        /// <summary>Returns every card to the deck and shuffles it.</summary>
        void Reset();
    }
}
