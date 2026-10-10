using System;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Randomness;

namespace HellPoker.Core.Betting
{
    /// <summary>Folds a hand below <see cref="FoldsBelow"/> <see cref="Percent"/>% of the time (the floors' imps: below a pair, 40%).</summary>
    public sealed class WeakHandFoldStrategy : IHouseFoldStrategy
    {
        private readonly IRandomSource _random;

        public HandCategory FoldsBelow { get; }
        public int Percent { get; }

        public WeakHandFoldStrategy(HandCategory foldsBelow, int percent, IRandomSource random)
        {
            if (percent < 0 || percent > 100) throw new ArgumentOutOfRangeException(nameof(percent));
            FoldsBelow = foldsBelow;
            Percent = percent;
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public bool WantsToFold(HandEvaluation houseHand)
        {
            if (houseHand == null) throw new ArgumentNullException(nameof(houseHand));
            return houseHand.Category < FoldsBelow && Percent > 0 && _random.Next(100) < Percent;
        }
    }
}
