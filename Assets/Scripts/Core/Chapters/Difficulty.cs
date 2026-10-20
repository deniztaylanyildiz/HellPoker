using System;

namespace HellPoker.Core.Chapters
{
    /// <summary>
    /// Demo 3.4: how hard a Phase 2 run is, chosen beside the class (it replaced the circles of Hell). Every level carries all the rules of
    /// the one before it: <see cref="Easy"/> is the designer's game; <see cref="Medium"/> (the default) speeds the floors' ante, ties the
    /// tribute to the purse, enrages the demons at half their bar and makes the map less safe; <see cref="Hard"/> tops the purse up less
    /// between chapters and makes a demon's loss hurt more; <see cref="Ironman"/> fills the demons' malice faster, stacks a second demon's rule
    /// at half strength and saves only at the camps. The numbers are <see cref="DifficultyRules"/>'.
    /// </summary>
    public enum Difficulty
    {
        Easy = 0,
        Medium = 1,
        Hard = 2,
        Ironman = 3
    }

    /// <summary>
    /// Every number of the difficulty levels in one place (the chapter simulation tries others; <see cref="Reset"/> puts the game's back).
    /// Arrays are by chapter (Mammon, Belial, Lilith) or by demon (Mammon, Belial, Lilith, Lucifer). Nothing here is a new mechanic: each
    /// rule turns a number the game already has (the ante's pace, the tribute, the stakes, the camps, the market, the purse, the losses, the
    /// malice gauge, the demons' rules).
    /// </summary>
    public static class DifficultyRules
    {
        public const Difficulty Default = Difficulty.Medium;
        public const Difficulty Hardest = Difficulty.Ironman;

        /// <summary>The stable ids (the save, the records, the logs): never translated.</summary>
        public static string IdOf(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Medium: return "medium";
                case Difficulty.Hard: return "hard";
                case Difficulty.Ironman: return "ironman";
                default: return "easy";
            }
        }

        /// <summary>A difficulty by its id; Easy for anything else (an old save without one plays on as Easy).</summary>
        public static Difficulty Parse(string id)
        {
            switch (id)
            {
                case "medium": return Difficulty.Medium;
                case "hard": return Difficulty.Hard;
                case "ironman": return Difficulty.Ironman;
                default: return Difficulty.Easy;
            }
        }

        public static Difficulty Clamp(int level) => (Difficulty)Math.Max(0, Math.Min((int)Hardest, level));

        private static int At(int[] values, int index) => values[Math.Max(0, Math.Min(values.Length - 1, index))];

        // ------------------------------------------------------------------ Medium (1-4)

        /// <summary>1. The floors' ante grows by one every this many hands (Mammon, Belial, Lilith); Easy: every three.</summary>
        public static int[] AnteStepHands = { 3, 2, 1 };

        /// <summary>2. The tribute is at least this share of the purse brought to the gate (Mammon, Belial, Lilith).</summary>
        public static int[] TributePursePercent = { 40, 50, 60 };

        /// <summary>3. Under this share of his bar's start a demon is enraged (once, for good): the stakes ×<see cref="RageStakePercent"/>
        /// and his rule comes more often (an interval halved, a chance ×<see cref="RageRulePercent"/>, Mammon's interest ×it).</summary>
        public static int RageBarPercent = 50;
        public static int RageStakePercent = 150;
        public static int RageRulePercent = 150;

        /// <summary>4. The map: at most this many camps in the middle (the one before the gate apart), the black market's prices ×this
        /// percent, an unknown node a warden this often (taken off the events' share) — and in Lilith's chapter every path from every start
        /// meets a warden.</summary>
        public static int[] MiddleCampsAtMost = { 2, 1, 1 };
        public static int[] MarketPercent = { 100, 120, 140 };
        public static int[] UnknownWardenPercent = { 5, 10, 15 };
        public static bool LilithWardenOnEveryPath = true;

        // ------------------------------------------------------------------ Hard (5-6)

        /// <summary>5. A chapter's purse is topped up only to this share of the class's starting purse (Mammon: none to top up, Belial,
        /// Lilith); a richer purse is left alone.</summary>
        public static int[] PurseTopUpPercent = { 100, 75, 50 };

        /// <summary>6. A loss at a demon's table puts this much (percent) on his bar and the sentence (Mammon, Belial, Lilith, Lucifer).</summary>
        public static int[] BossLossPercent = { 100, 125, 150, 150 };

        /// <summary>Ironman's own numbers of 5 and 6 (the simulation: 7 and 8 alone barely moved freedom): the purse topped up to less, the
        /// losses heavier still.</summary>
        public static int[] IronmanPurseTopUpPercent = { 100, 50, 25 };
        public static int[] IronmanBossLossPercent = { 125, 150, 175, 175 };

        // ------------------------------------------------------------------ Ironman (7-9)

        /// <summary>7. A demon's malice gauge holds at most this many (Mammon, Belial, Lilith, Lucifer): it fills in that many hands. Only the
        /// cheats' pace — never his play. (A gauge already that short stays as it is.) The asked 5 / 4 / 3 / 3 changed only Lilith's (the
        /// gauges are 4 / 2 / 4 / 1); the simulation's 3 / 2 / 2 / 1 shortens Mammon's and Lilith's.</summary>
        public static int[] MaliceHands = { 3, 2, 2, 1 };

        /// <summary>8. A demon carries the rule of the one before him at this strength (percent): Belial Mammon's interest, Lilith Belial's
        /// role swap. Lucifer's mirror stays his own.</summary>
        public static int StackedRulePercent = 50;

        // ------------------------------------------------------------------ which rules a level has

        public static bool FasterAnte(Difficulty d) => d >= Difficulty.Medium;
        public static bool TributeFromPurse(Difficulty d) => d >= Difficulty.Medium;
        public static bool Rage(Difficulty d) => d >= Difficulty.Medium;
        public static bool SaferMapGone(Difficulty d) => d >= Difficulty.Medium;
        public static bool ShortTopUp(Difficulty d) => d >= Difficulty.Hard;
        public static bool HeavierLoss(Difficulty d) => d >= Difficulty.Hard;
        public static bool FasterMalice(Difficulty d) => d >= Difficulty.Ironman;
        public static bool StackedRules(Difficulty d) => d >= Difficulty.Ironman;

        /// <summary>9. Ironman's saves: only at the camps (on the way in and out, and by hand there), once as the run begins; a death deletes
        /// the save.</summary>
        public static bool SavesOnlyAtCamps(Difficulty d) => d >= Difficulty.Ironman;

        /// <summary>The floors' ante pace in <paramref name="chapter"/> (1-3); null: the chapter's own.</summary>
        public static int? AnteStepHandsFor(Difficulty d, int chapter) => FasterAnte(d) ? At(AnteStepHands, chapter - 1) : (int?)null;

        public static int TributePurseFor(Difficulty d, int chapter) => TributeFromPurse(d) ? At(TributePursePercent, chapter - 1) : 0;

        public static int MarketFor(Difficulty d, int chapter) => SaferMapGone(d) ? At(MarketPercent, chapter - 1) : 100;

        /// <summary>The middle camps a map may hold; int.MaxValue: as many as the starts need (Easy).</summary>
        public static int MiddleCampsFor(Difficulty d, int chapter) => SaferMapGone(d) ? At(MiddleCampsAtMost, chapter - 1) : int.MaxValue;

        /// <summary>The warden's weight among an unknown node's contents (of a hundred); Easy: the map's own (5).</summary>
        public static int UnknownWardenFor(Difficulty d, int chapter) => SaferMapGone(d) ? At(UnknownWardenPercent, chapter - 1) : 5;

        public static bool WardenOnEveryPath(Difficulty d, int chapter) => SaferMapGone(d) && LilithWardenOnEveryPath && chapter >= 3;

        public static int PurseTopUpFor(Difficulty d, int chapter) =>
            !ShortTopUp(d) ? 100 : At(d >= Difficulty.Ironman ? IronmanPurseTopUpPercent : PurseTopUpPercent, chapter - 1);

        /// <summary>The loss percent at the demon <paramref name="boss"/> (0 Mammon ... 3 Lucifer).</summary>
        public static int BossLossFor(Difficulty d, int boss) =>
            !HeavierLoss(d) ? 100 : At(d >= Difficulty.Ironman ? IronmanBossLossPercent : BossLossPercent, boss);

        /// <summary>How much shorter the demon's gauge is (never below one; 0 for none).</summary>
        public static int MaliceShortFor(Difficulty d, int boss, int maliceMax) =>
            FasterMalice(d) ? Math.Max(0, maliceMax - At(MaliceHands, boss)) : 0;

        /// <summary>The demon index of a boss's id (0 Mammon ... 3 Lucifer); -1 for none.</summary>
        public static int BossIndex(string bossId)
        {
            switch (bossId)
            {
                case Dealers.DealerRoster.MammonId: return 0;
                case Dealers.DealerRoster.BelialId: return 1;
                case Dealers.DealerRoster.LilithId: return 2;
                case Dealers.DealerRoster.LuciferId: return 3;
                default: return -1;
            }
        }

        /// <summary>The game's numbers back (after a simulation tried others).</summary>
        public static void Reset()
        {
            AnteStepHands = new[] { 3, 2, 1 };
            TributePursePercent = new[] { 40, 50, 60 };
            RageBarPercent = 50;
            RageStakePercent = 150;
            RageRulePercent = 150;
            MiddleCampsAtMost = new[] { 2, 1, 1 };
            MarketPercent = new[] { 100, 120, 140 };
            UnknownWardenPercent = new[] { 5, 10, 15 };
            LilithWardenOnEveryPath = true;
            PurseTopUpPercent = new[] { 100, 75, 50 };
            BossLossPercent = new[] { 100, 125, 150, 150 };
            IronmanPurseTopUpPercent = new[] { 100, 50, 25 };
            IronmanBossLossPercent = new[] { 125, 150, 175, 175 };
            MaliceHands = new[] { 3, 2, 2, 1 };
            StackedRulePercent = 50;
        }
    }
}
