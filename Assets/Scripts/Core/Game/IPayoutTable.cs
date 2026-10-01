using HellPoker.Core.Evaluation;

namespace HellPoker.Core.Game
{
    /// <summary>
    /// Turns the end of a hand into years forgiven or added.
    /// The hand multiplier applies to the ante only; raises and re-raises pay one to one.
    /// </summary>
    public interface IPayoutTable
    {
        /// <summary>stake + ante × (multiplier − 1), at most the sentence; the absolution hand forgives everything.</summary>
        int GetYearsForgiven(HandCategory playerCategory, int stake, int ante, int currentYears);

        /// <summary>
        /// (stake + ante × (house multiplier − 1)) × the loss percent × <paramref name="surchargePercent"/> (e.g. 150 when the soul
        /// is on the table), rounded up once.
        /// </summary>
        int GetYearsAdded(HandCategory houseCategory, int stake, int ante, int surchargePercent = 100);

        /// <summary>The fold percent of the stake × <paramref name="surchargePercent"/> (e.g. 150 with the soul on the table), rounded up once.</summary>
        /// <param name="afterDraw">True once the cards have been exchanged; folding late usually costs more.</param>
        int GetFoldPenalty(int stake, bool afterDraw, int surchargePercent = 100);

        /// <summary>The least a win can forgive (the weakest winning hand).</summary>
        int GetLeastYearsForgiven(int stake, int ante, int currentYears);

        /// <summary>The least a loss can add (the weakest hand the house can win with).</summary>
        int GetLeastYearsAdded(int stake, int ante, int surchargePercent = 100);
    }
}
