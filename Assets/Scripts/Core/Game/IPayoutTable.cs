using HellPoker.Core.Evaluation;

namespace HellPoker.Core.Game
{
    /// <summary>Turns the end of a hand into years forgiven or added.</summary>
    public interface IPayoutTable
    {
        int GetYearsForgiven(HandCategory playerCategory, int stake, int currentYears);

        int GetYearsAdded(HandCategory houseCategory, int stake);

        /// <param name="afterDraw">True once the cards have been exchanged; folding late usually costs more.</param>
        int GetFoldPenalty(int stake, bool afterDraw);

        /// <summary>The least a win can forgive with this stake (the weakest winning hand).</summary>
        int GetLeastYearsForgiven(int stake, int currentYears);

        /// <summary>The least a loss can add with this stake (the weakest hand the house can win with).</summary>
        int GetLeastYearsAdded(int stake);
    }
}
