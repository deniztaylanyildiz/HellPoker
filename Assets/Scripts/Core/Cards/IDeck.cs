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

        /// <summary>The cards still in the deck, the next one to be drawn last.</summary>
        IReadOnlyList<Card> Remaining { get; }

        /// <summary>Takes one particular card out of the deck (a cheat turning a card into it); false when it is not there.</summary>
        bool Take(Card card);

        /// <summary>The deck becomes exactly these cards, in this order (the first one drawn first) — a saved or carried deck.</summary>
        void Restore(IEnumerable<Card> cardsInDrawOrder);
    }
}
