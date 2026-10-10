using HellPoker.Core.Evaluation;

namespace HellPoker.Core.Betting
{
    /// <summary>
    /// Whether the House gives up its hand when the player raises after the draw (a floor's imp; a demon never does).
    /// The player then wins the stake already on the table, one to one; the raise comes back.
    /// </summary>
    public interface IHouseFoldStrategy
    {
        bool WantsToFold(HandEvaluation houseHand);
    }
}
