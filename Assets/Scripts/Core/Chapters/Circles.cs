using System;

namespace HellPoker.Core.Chapters
{
    /// <summary>
    /// The circles of Hell: Phase 2's difficulty, 0 (the designer's numbers) to <see cref="Deepest"/>, each adding to the ones above it.
    /// A class opens its next circle by walking free at the deepest it has open (<see cref="ChapterRecords.CirclesOpen"/>).
    /// 1: the imps' purses +25%. 2: the tribute +20 coins. 3: the demons' bars +15%. 4: the black market ×1.25, the fire offers two
    /// boons. (3: the bars also give 15% less for a win.) 5: the demons' malice gauges one shorter, Lucifer copies two mechanics.
    /// And every circle: a won hand takes <see cref="WinStepPercent"/>% less off a demon's bar per circle.
    /// </summary>
    public static class Circles
    {
        public const int Deepest = 5;

        public const int ImpPursePercent = 125, TributeExtra = 20, BarPercent = 115, MarketPercent = 125, FireOffers = 2;

        /// <summary>Every circle takes this much (percent) off what a won hand takes off a demon's bar (the circle's own rules on top).
        /// Demo 2.0.1: alone the listed rules barely moved freedom (circle 5: 21%); with it circle 5 is near 10%.</summary>
        public const int WinStepPercent = 5;

        public static int Clamp(int circle) => Math.Max(0, Math.Min(Deepest, circle));

        /// <summary>A table imp's purse at <paramref name="circle"/>, in percent.</summary>
        public static int ImpPurseAt(int circle) => circle >= 1 ? ImpPursePercent : 100;

        /// <summary>Coins on the gate's tribute.</summary>
        public static int TributeAt(int circle) => circle >= 2 ? TributeExtra : 0;

        /// <summary>A demon's bar as his table begins, in percent (Lucifer's too).</summary>
        public static int BarAt(int circle) => circle >= 3 ? BarPercent : 100;

        /// <summary>A demon's table's win percent at <paramref name="circle"/>: the bars are thicker from the third circle — they start
        /// longer and every win takes that much less off them (a longer bar alone changes nothing: the stakes are measured against it) —
        /// and every circle takes <see cref="WinStepPercent"/>% more off the win.</summary>
        public static int WinPercentAt(int circle, int winPercent) =>
            Math.Max(1, winPercent * 100 / BarAt(circle) * (100 - ChapterTuning.CircleWinStepPercent * Clamp(circle)) / 100);

        /// <summary>The black market's prices, in percent.</summary>
        public static int MarketAt(int circle) => circle >= 4 ? MarketPercent : 100;

        /// <summary>How many of the fire's boons are offered (all four above the fourth circle).</summary>
        public static int FireOffersAt(int circle) => circle >= 4 ? FireOffers : 4;

        /// <summary>The demons' malice gauges are one shorter (never below one): every cheat comes a hand sooner, the whole table long.</summary>
        public static int MaliceShortAt(int circle) => circle >= 5 ? 1 : 0;

        /// <summary>Lucifer copies a second mechanic.</summary>
        public static bool LuciferMirrorsTwoAt(int circle) => circle >= 5;
    }
}
