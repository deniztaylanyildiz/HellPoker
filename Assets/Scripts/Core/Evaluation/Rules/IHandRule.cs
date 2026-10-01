namespace HellPoker.Core.Evaluation.Rules
{
    /// <summary>Recognises one hand category. Add new special hands by adding rules, not by editing the evaluator.</summary>
    public interface IHandRule
    {
        HandCategory Category { get; }

        bool TryMatch(HandAnalysis analysis, out HandEvaluation evaluation);
    }
}
