using System;
using System.Collections.Generic;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>How a run ended, for the end screen.</summary>
    public sealed class RunSummary
    {
        public bool Absolved { get; }
        public int HandsPlayed { get; }
        public int LowestYears { get; }
        public int HighestYears { get; }
        public HandCategory? BestHand { get; }
        public IReadOnlyList<string> DealerNames { get; }
        public bool SoulStaked { get; }

        /// <summary>Set free at Lucifer's table: the Morning Star falls.</summary>
        public bool BeatLucifer { get; }

        /// <summary>Set free by the Dead Man's Hand at an ordinary table: Wild Bill's escape.</summary>
        public bool WildBill { get; }

        /// <summary>How many times the run was summoned to Lucifer (0: never met him).</summary>
        public int LuciferAttempts { get; }

        public RunSummary(bool absolved, int handsPlayed, int lowestYears, int highestYears, HandCategory? bestHand,
            IReadOnlyList<string> dealerNames, bool soulStaked, bool beatLucifer = false, bool wildBill = false, int luciferAttempts = 0)
        {
            BeatLucifer = beatLucifer;
            WildBill = wildBill;
            LuciferAttempts = luciferAttempts;
            Absolved = absolved;
            HandsPlayed = handsPlayed;
            LowestYears = lowestYears;
            HighestYears = highestYears;
            BestHand = bestHand;
            DealerNames = dealerNames ?? Array.Empty<string>();
            SoulStaked = soulStaked;
        }
    }

    /// <summary>The screen after a run: walked free or damned, with the run's story.</summary>
    public interface IEndScreenView
    {
        event Action NewGamePressed;
        event Action MenuPressed;

        bool IsVisible { get; }

        void Show(RunSummary summary);
        void Hide();
    }

    /// <summary>Records across all runs.</summary>
    public interface IRecordsView
    {
        event Action BackPressed;

        bool IsVisible { get; }

        /// <param name="dealers">Every demon, for the absolutions per table.</param>
        /// <param name="phase2">Phase 2's records in a line (its own book); null: none.</param>
        void Show(RecordBook records, IReadOnlyList<DealerCard> dealers, string phase2 = null);
        void Hide();
    }
}
