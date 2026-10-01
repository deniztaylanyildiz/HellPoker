using HellPoker.Core.Evaluation;

namespace HellPoker.Core.Game
{
    /// <summary>Read-only view of the payout schedule, for displaying it to the player.</summary>
    public interface IPayoutInfo
    {
        bool IsAbsolution(HandCategory category);

        int GetMultiplier(HandCategory category);

        /// <summary>Share of the stake added to the sentence on a loss, in percent.</summary>
        int LossPercent { get; }

        /// <summary>Share of the stake added to the sentence on a fold, in percent.</summary>
        int FoldPercent { get; }
    }
}
