using System;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Randomness;

namespace HellPoker.Core.Betting
{
    /// <summary>Re-raises by hand strength and temperament (<see cref="HouseBettingStyle"/>), rolling the dice on an <see cref="IRandomSource"/>.</summary>
    public sealed class HandStrengthBettingStrategy : IHouseBettingStrategy
    {
        private readonly HouseBettingStyle _style;
        private readonly IRandomSource _random;

        public HandStrengthBettingStrategy(HouseBettingStyle style, IRandomSource random)
        {
            _style = style ?? throw new ArgumentNullException(nameof(style));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public bool WantsToReRaise(HandEvaluation houseHand)
        {
            if (houseHand == null) throw new ArgumentNullException(nameof(houseHand));

            int percent = houseHand.Category >= _style.StrongFrom ? _style.StrongPercent : _style.BluffPercent;
            return percent > 0 && _random.Next(100) < percent;
        }
    }
}
