namespace HellPoker.Core.Evaluation.Rules
{
    public sealed class StraightRule : IHandRule
    {
        public HandCategory Category => HandCategory.Straight;

        public bool TryMatch(HandAnalysis analysis, out HandEvaluation evaluation)
        {
            if (!analysis.IsStraight || analysis.IsFlush)
            {
                evaluation = null;
                return false;
            }

            evaluation = new HandEvaluation(analysis.Hand, Category, new[] { analysis.StraightHighRank });
            return true;
        }
    }
}
