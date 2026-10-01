using HellPoker.Core.Cards;

namespace HellPoker.Core.Evaluation.Rules
{
    /// <summary>Straight flush; the ace-high one is reported as <see cref="HandCategory.RoyalFlush"/> by a separate instance.</summary>
    public sealed class StraightFlushRule : IHandRule
    {
        private readonly bool _royal;

        private StraightFlushRule(bool royal)
        {
            _royal = royal;
        }

        public static StraightFlushRule Royal() => new StraightFlushRule(true);
        public static StraightFlushRule Regular() => new StraightFlushRule(false);

        public HandCategory Category => _royal ? HandCategory.RoyalFlush : HandCategory.StraightFlush;

        public bool TryMatch(HandAnalysis analysis, out HandEvaluation evaluation)
        {
            bool isRoyal = analysis.StraightHighRank == Rank.Ace;
            if (!analysis.IsFlush || !analysis.IsStraight || isRoyal != _royal)
            {
                evaluation = null;
                return false;
            }

            evaluation = new HandEvaluation(analysis.Hand, Category, new[] { analysis.StraightHighRank });
            return true;
        }
    }
}
