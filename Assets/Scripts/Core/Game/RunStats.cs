using System;
using System.Collections.Generic;
using HellPoker.Core.Evaluation;

namespace HellPoker.Core.Game
{
    /// <summary>What happened in one run, for the end screen: hands, the lowest and highest sentence, the best hand,
    /// every demon sat with, and whether the soul ever went on the table.</summary>
    public sealed class RunStats
    {
        private readonly List<string> _dealers = new List<string>();

        public int HandsPlayed { get; private set; }
        public int LowestYears { get; private set; }
        public int HighestYears { get; private set; }

        /// <summary>The best hand the player held at a showdown; null before the first one.</summary>
        public HandCategory? BestHand { get; private set; }

        public IReadOnlyList<string> Dealers => _dealers;
        public bool SoulStaked { get; private set; }

        public RunStats(int startingYears, string dealerId)
        {
            if (startingYears < 0) throw new ArgumentOutOfRangeException(nameof(startingYears));
            LowestYears = startingYears;
            HighestYears = startingYears;
            SatWith(dealerId);
        }

        /// <summary>Rebuilds saved stats.</summary>
        public RunStats(int handsPlayed, int lowestYears, int highestYears, HandCategory? bestHand, IEnumerable<string> dealers, bool soulStaked)
        {
            if (handsPlayed < 0) throw new ArgumentOutOfRangeException(nameof(handsPlayed));
            if (lowestYears < 0 || highestYears < lowestYears) throw new ArgumentOutOfRangeException(nameof(lowestYears));
            HandsPlayed = handsPlayed;
            LowestYears = lowestYears;
            HighestYears = highestYears;
            BestHand = bestHand;
            SoulStaked = soulStaked;
            foreach (string dealer in dealers ?? Array.Empty<string>())
                SatWith(dealer);
        }

        public void SatWith(string dealerId)
        {
            if (string.IsNullOrEmpty(dealerId)) throw new ArgumentException("A dealer id is needed.", nameof(dealerId));
            if (!_dealers.Contains(dealerId)) _dealers.Add(dealerId);
        }

        /// <summary>Notes the sentence (and the soul) as it stands, without counting a hand — e.g. on sitting at a new table.</summary>
        public void Note(int years, bool soulAtStake)
        {
            LowestYears = Math.Min(LowestYears, years);
            HighestYears = Math.Max(HighestYears, years);
            SoulStaked |= soulAtStake;
        }

        /// <summary>A hand has been settled.</summary>
        /// <param name="playerHand">The player's hand at the showdown; null when they folded.</param>
        public void RecordHand(int yearsAfter, HandCategory? playerHand, bool soulAtStake)
        {
            HandsPlayed++;
            Note(yearsAfter, soulAtStake);
            if (playerHand.HasValue && (!BestHand.HasValue || playerHand.Value > BestHand.Value))
                BestHand = playerHand;
        }
    }
}
