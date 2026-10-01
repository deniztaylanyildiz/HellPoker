using System.Linq;
using HellPoker.Core.Cards;

namespace HellPoker.Core.Evaluation.Rules
{
    /// <summary>Wild Bill Hickok's last hand: the black aces and black eights. The fifth card is free and acts as kicker.</summary>
    public sealed class DeadMansHandRule : IHandRule
    {
        private static readonly Card[] RequiredCards =
        {
            new Card(Rank.Ace, Suit.Spades),
            new Card(Rank.Ace, Suit.Clubs),
            new Card(Rank.Eight, Suit.Spades),
            new Card(Rank.Eight, Suit.Clubs)
        };

        public HandCategory Category => HandCategory.DeadMansHand;

        public bool TryMatch(HandAnalysis analysis, out HandEvaluation evaluation)
        {
            Hand hand = analysis.Hand;
            if (!RequiredCards.All(hand.Contains))
            {
                evaluation = null;
                return false;
            }

            Card kicker = hand.First(card => !RequiredCards.Contains(card));
            evaluation = new HandEvaluation(hand, Category, new[] { kicker.Rank });
            return true;
        }
    }
}
