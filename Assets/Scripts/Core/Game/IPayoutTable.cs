using HellPoker.Core.Evaluation;

namespace HellPoker.Core.Game
{
    /// <summary>Turns the end of a hand into years forgiven or added.</summary>
    public interface IPayoutTable
    {
        int GetYearsForgiven(HandCategory playerCategory, int stake, int currentYears);

        int GetYearsAdded(HandCategory houseCategory, int stake);

        int GetFoldPenalty(int stake);
    }
}
