using HellPoker.Core.Evaluation;

namespace HellPoker.Core.Betting
{
    /// <summary>How the house answers a raise after the draw: with a re-raise, or by letting it stand.</summary>
    public interface IHouseBettingStrategy
    {
        /// <param name="houseHand">The house's final hand, evaluated.</param>
        bool WantsToReRaise(HandEvaluation houseHand);
    }
}
