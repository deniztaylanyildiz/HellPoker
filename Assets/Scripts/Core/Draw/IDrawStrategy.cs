using System.Collections.Generic;
using HellPoker.Core.Cards;

namespace HellPoker.Core.Draw
{
    /// <summary>Decides which cards a computer-controlled player discards.</summary>
    public interface IDrawStrategy
    {
        IReadOnlyList<int> ChooseDiscards(Hand hand);
    }
}
