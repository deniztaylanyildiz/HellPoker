using System;
using HellPoker.Core.Evaluation;

namespace HellPoker.Core.Betting
{
    /// <summary>
    /// A demon's temperament at the table: with a hand of <see cref="StrongFrom"/> or better it re-raises
    /// <see cref="StrongPercent"/>% of the time; with anything weaker it bluffs <see cref="BluffPercent"/>% of the time.
    /// </summary>
    public sealed class HouseBettingStyle
    {
        public HandCategory StrongFrom { get; }
        public int StrongPercent { get; }
        public int BluffPercent { get; }

        public HouseBettingStyle(HandCategory strongFrom, int strongPercent, int bluffPercent)
        {
            if (strongPercent < 0 || strongPercent > 100) throw new ArgumentOutOfRangeException(nameof(strongPercent));
            if (bluffPercent < 0 || bluffPercent > 100) throw new ArgumentOutOfRangeException(nameof(bluffPercent));

            StrongFrom = strongFrom;
            StrongPercent = strongPercent;
            BluffPercent = bluffPercent;
        }

        /// <summary>A house that never re-raises.</summary>
        public static HouseBettingStyle Silent => new HouseBettingStyle(HandCategory.HighCard, 0, 0);
    }
}
