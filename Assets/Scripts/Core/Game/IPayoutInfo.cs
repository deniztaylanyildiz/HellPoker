using HellPoker.Core.Evaluation;

namespace HellPoker.Core.Game
{
    /// <summary>Read-only view of the payout schedule, for displaying it to the player.</summary>
    public interface IPayoutInfo
    {
        bool IsAbsolution(HandCategory category);

        int GetMultiplier(HandCategory category);
    }
}
