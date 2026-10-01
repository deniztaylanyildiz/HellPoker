using System;
using System.Linq;
using HellPoker.Core.Cards;

namespace HellPoker.Core.Evaluation.Rules
{
    /// <summary>
    /// Matches hands by the exact shape of their rank groups, e.g. full house = {3, 2}, two pair = {2, 2, 1}.
    /// Covers every category that is defined purely by repeated ranks.
    /// </summary>
    public sealed class RankPatternRule : IHandRule
    {
        private readonly int[] _groupSizes;
        private readonly bool _excludeFlushAndStraight;

        public RankPatternRule(HandCategory category, params int[] groupSizes)
        {
            if (groupSizes == null || groupSizes.Length == 0)
                throw new ArgumentException("A pattern needs at least one group.", nameof(groupSizes));

            Category = category;
            _groupSizes = groupSizes;
            // Only five distinct ranks can also form a flush or straight; those hands belong to stronger rules.
            _excludeFlushAndStraight = groupSizes.Length == Hand.Size;
        }

        public HandCategory Category { get; }

        public bool TryMatch(HandAnalysis analysis, out HandEvaluation evaluation)
        {
            bool matches = analysis.GroupSizes.SequenceEqual(_groupSizes)
                           && !(_excludeFlushAndStraight && (analysis.IsFlush || analysis.IsStraight));

            evaluation = matches ? new HandEvaluation(analysis.Hand, Category, analysis.GroupRanks) : null;
            return matches;
        }
    }
}
