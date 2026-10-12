using System.Collections.Generic;

namespace HellPoker.Core.Chapters
{
    /// <summary>
    /// The simulations' knobs over the chapters' numbers (a class's starting purse, its whole sentence, a chapter's tribute): empty in
    /// the game, where the designer's numbers stand. Only the tests reach it (internal); <see cref="Reset"/> after every use.
    /// </summary>
    internal static class ChapterTuning
    {
        /// <summary>A class's starting purse in place of <see cref="ChapterRules.StartingCoinsFor"/>'s.</summary>
        public static readonly Dictionary<string, int> StartingCoins = new Dictionary<string, int>();

        /// <summary>A class's whole sentence (its four shares scaled to it) in place of <see cref="BossShares"/>'.</summary>
        public static readonly Dictionary<string, int> SentenceTotals = new Dictionary<string, int>();

        /// <summary>A chapter's tribute over the class's start in place of its rules'.</summary>
        public static readonly Dictionary<int, int> TributeOverStart = new Dictionary<int, int>();

        public static void Reset()
        {
            StartingCoins.Clear();
            SentenceTotals.Clear();
            TributeOverStart.Clear();
        }
    }
}
