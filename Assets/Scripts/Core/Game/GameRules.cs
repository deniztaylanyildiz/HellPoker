using System;
using HellPoker.Core.Cards;
using HellPoker.Core.Draw;

namespace HellPoker.Core.Game
{
    /// <summary>Tunable numbers for a Hell Poker run.</summary>
    public sealed class GameRules
    {
        public int StartingYears { get; }

        /// <summary>Reaching this sentence means eternal damnation (game over).</summary>
        public int DamnationYears { get; }

        /// <summary>
        /// Allowed range for the opening stake (ante). Each raise adds the ante again.
        /// The total stake is also capped by the player's remaining sentence.
        /// </summary>
        public int MinStake { get; }
        public int MaxStake { get; }

        /// <summary>How many cards a player may exchange in the draw.</summary>
        public int MaxDiscards { get; }

        /// <summary>At or below this many years left, passing is forbidden: every bet decision must raise (or fold).</summary>
        public int ForcedRaiseYears { get; }

        /// <summary>
        /// How many of the house's cards come with a bet decision after they are turned (0-4).
        /// The remaining cards are turned straight into the showdown, so the last card always stays a surprise.
        /// </summary>
        public int HouseRevealDecisions { get; }

        public GameRules(int startingYears = 1000, int damnationYears = 2000, int minStake = 10, int maxStake = 200,
            int maxDiscards = MaxDiscardPolicy.ClassicLimit, int forcedRaiseYears = 250, int houseRevealDecisions = 3)
        {
            if (startingYears <= 0) throw new ArgumentOutOfRangeException(nameof(startingYears));
            if (damnationYears <= startingYears) throw new ArgumentOutOfRangeException(nameof(damnationYears), "Must be above the starting sentence.");
            if (minStake <= 0 || maxStake < minStake) throw new ArgumentOutOfRangeException(nameof(maxStake), "Invalid stake range.");
            if (maxDiscards < 0 || maxDiscards > Hand.Size) throw new ArgumentOutOfRangeException(nameof(maxDiscards));
            if (forcedRaiseYears < 0) throw new ArgumentOutOfRangeException(nameof(forcedRaiseYears));
            if (houseRevealDecisions < 0 || houseRevealDecisions >= Hand.Size)
                throw new ArgumentOutOfRangeException(nameof(houseRevealDecisions), $"Must be between 0 and {Hand.Size - 1}.");

            StartingYears = startingYears;
            DamnationYears = damnationYears;
            MinStake = minStake;
            MaxStake = maxStake;
            MaxDiscards = maxDiscards;
            ForcedRaiseYears = forcedRaiseYears;
            HouseRevealDecisions = houseRevealDecisions;
        }

        public static GameRules Default => new GameRules();
    }
}
