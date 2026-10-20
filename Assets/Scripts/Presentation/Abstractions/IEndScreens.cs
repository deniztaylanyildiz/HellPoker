using System;
using System.Collections.Generic;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>How a demo run ended (the table's engine still reports it; Demo 3.4.1: no screen shows it any more).</summary>
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

    /// <summary>The records' lines: this one alone splits them into two columns (the game's: the runs left, the circles and locks right).</summary>
    public static class RecordsLayout
    {
        public const string ColumnBreak = "\f";
    }

    public interface IRecordsView
    {
        event Action BackPressed;

        bool IsVisible { get; }

        /// <summary>The game's records, line by line (the chapters' book).</summary>
        void ShowLines(IReadOnlyList<string> lines);
        void Hide();
    }
}
