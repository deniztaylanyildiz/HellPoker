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

        /// <summary>Every circle takes this much (percent) off what a won demon's hand takes off his bar: the game's is
        /// <see cref="Circles.WinStepPercent"/>; the simulation tries others (HELLPOKER_CIRCLE_WIN).</summary>
        public static int CircleWinStepPercent = Circles.WinStepPercent;

        /// <summary>A lost hand heals a demon's bar by this much of its cost: the game's is <see cref="BossTable.BarHealPercent"/>; the
        /// simulation tries others (HELLPOKER_BAR_HEAL).</summary>
        public static int BarHealPercent = BossTable.BarHealPercent;

        /// <summary>A new run's map generator in place of <see cref="ChapterMap.CurrentVersion"/> (HELLPOKER_MAP; 0: the game's).</summary>
        public static int MapVersion;

        /// <summary>A demon's loss percent in place of the chapter's (1-3; 4: Lucifer) (HELLPOKER_BOSS_LOSS).</summary>
        public static readonly Dictionary<int, int> BossLossPercent = new Dictionary<int, int>();

        public static void Reset()
        {
            StartingCoins.Clear();
            SentenceTotals.Clear();
            TributeOverStart.Clear();
            CircleWinStepPercent = Circles.WinStepPercent;
            BarHealPercent = BossTable.BarHealPercent;
            BossLossPercent.Clear();
            MapVersion = 0;
        }
    }
}
