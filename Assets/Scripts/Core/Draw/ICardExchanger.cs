using System.Collections.Generic;
using HellPoker.Core.Cards;

namespace HellPoker.Core.Draw
{
    /// <summary>Performs the draw phase: discard the chosen cards and replace them from the deck.</summary>
    public interface ICardExchanger
    {
        bool CanExchange(Hand hand, IReadOnlyCollection<int> discardIndices, IDeck deck, out string reason);

        ExchangeResult Exchange(Hand hand, IReadOnlyCollection<int> discardIndices, IDeck deck);
    }
}
