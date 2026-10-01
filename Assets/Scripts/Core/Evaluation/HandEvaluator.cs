using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Evaluation.Rules;

namespace HellPoker.Core.Evaluation
{
    /// <summary>Runs rules from the strongest category down; the first match wins.</summary>
    public sealed class HandEvaluator : IHandEvaluator
    {
        private readonly IHandRule[] _rules;

        public HandEvaluator(IEnumerable<IHandRule> rules)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));

            _rules = rules.OrderByDescending(rule => rule.Category).ToArray();
            if (_rules.Length == 0)
                throw new ArgumentException("At least one hand rule is required.", nameof(rules));
        }

        public static HandEvaluator CreateDefault()
        {
            return new HandEvaluator(StandardHandRules.Create());
        }

        public HandEvaluation Evaluate(Hand hand)
        {
            var analysis = new HandAnalysis(hand);

            foreach (IHandRule rule in _rules)
            {
                if (rule.TryMatch(analysis, out HandEvaluation evaluation))
                    return evaluation;
            }

            throw new InvalidOperationException($"No rule matched hand {hand}. Include a fallback rule such as high card.");
        }
    }
}
