using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;

namespace HellPoker.Presentation.Abstractions
{
    public interface IPayoutView
    {
        /// <summary>Shows this payout schedule (each dealer keeps their own).</summary>
        void SetTable(IPayoutInfo payouts);

        /// <summary>Highlights the row of the winning category, or clears it with null.</summary>
        void Highlight(HandCategory? category);
    }
}
