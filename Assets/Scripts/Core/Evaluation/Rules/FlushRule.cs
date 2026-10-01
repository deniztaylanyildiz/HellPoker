namespace HellPoker.Core.Evaluation.Rules
{
    public sealed class FlushRule : IHandRule
    {
        public HandCategory Category => HandCategory.Flush;

        public bool TryMatch(HandAnalysis analysis, out HandEvaluation evaluation)
        {
            if (!analysis.IsFlush || analysis.IsStraight)
            {
                evaluation = null;
                return false;
            }

            evaluation = new HandEvaluation(analysis.Hand, Category, analysis.GroupRanks);
            return true;
        }
    }
}
