using HellPoker.Core.Cards;

namespace HellPoker.Core.Evaluation
{
    public interface IHandEvaluator
    {
        HandEvaluation Evaluate(Hand hand);
    }
}
