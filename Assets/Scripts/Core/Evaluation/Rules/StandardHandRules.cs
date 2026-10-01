using System.Collections.Generic;

namespace HellPoker.Core.Evaluation.Rules
{
    public static class StandardHandRules
    {
        /// <summary>Hell Poker's rule set: standard five-card poker plus the Dead Man's Hand on top.</summary>
        public static IReadOnlyList<IHandRule> Create()
        {
            return new IHandRule[]
            {
                new DeadMansHandRule(),
                StraightFlushRule.Royal(),
                StraightFlushRule.Regular(),
                new RankPatternRule(HandCategory.FourOfAKind, 4, 1),
                new RankPatternRule(HandCategory.FullHouse, 3, 2),
                new FlushRule(),
                new StraightRule(),
                new RankPatternRule(HandCategory.ThreeOfAKind, 3, 1, 1),
                new RankPatternRule(HandCategory.TwoPair, 2, 2, 1),
                new RankPatternRule(HandCategory.OnePair, 2, 1, 1, 1),
                new RankPatternRule(HandCategory.HighCard, 1, 1, 1, 1, 1)
            };
        }
    }
}
