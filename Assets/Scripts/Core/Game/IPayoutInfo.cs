using HellPoker.Core.Evaluation;

namespace HellPoker.Core.Game
{
    /// <summary>Read-only view of the payout schedule, for displaying it to the player.</summary>
    public interface IPayoutInfo
    {
        bool IsAbsolution(HandCategory category);

        int GetMultiplier(HandCategory category);

        /// <summary>A loss adds stake × the house's multiplier × this percent.</summary>
        int LossPercent { get; }

        /// <summary>Share of the stake added on a fold before the draw, in percent.</summary>
        int FoldPercentBeforeDraw { get; }

        /// <summary>Share of the stake added on a fold after the draw, in percent.</summary>
        int FoldPercentAfterDraw { get; }
    }
}
