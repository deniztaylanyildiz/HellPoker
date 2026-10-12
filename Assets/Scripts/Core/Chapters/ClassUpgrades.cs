using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Sinners;

namespace HellPoker.Core.Chapters
{
    public static class UpgradeIds
    {
        // The Peasant's satchel.
        public const string WideSatchel = "wide_satchel";
        public const string QuietSatchel = "quiet_satchel";
        public const string Seed = "seed";
        public const string Abundance = "abundance";

        // The Warlock's prophecy.
        public const string DeepProphecy = "deep_prophecy";
        public const string BlindMirror = "blind_mirror";
        public const string Thrice = "thrice";
        public const string EyeOfTruth = "eye_of_truth";

        // The King's treasury.
        public const string OverflowingTreasury = "overflowing_treasury";
        public const string CheapSeizure = "cheap_seizure";
        public const string CrownGuard = "crown_guard";
        public const string RoyalDecree = "royal_decree";

        // The Jester's jokers.
        public const string ControlledChaos = "controlled_chaos";
        public const string DoubleLaugh = "double_laugh";
        public const string JokerPurse = "joker_purse";
        public const string Buffoon = "buffoon";
    }

    /// <summary>
    /// A class's four Phase 2 upgrades (a beaten demon's loot, the purgatory fire's whetted soul): each makes the class's own mechanic
    /// stronger for the rest of the run, once a run. What each does lives where the mechanic does (<see cref="ClassKit"/>, the powers,
    /// <see cref="UpgradeHooks"/>); the words in UiText. A new one: an id, a line here, its effect, its words and icon.
    /// </summary>
    public static class ClassUpgrades
    {
        private static readonly Dictionary<string, string[]> ByClass = new Dictionary<string, string[]>
        {
            [Peasant.ClassId] = new[] { UpgradeIds.WideSatchel, UpgradeIds.QuietSatchel, UpgradeIds.Seed, UpgradeIds.Abundance },
            [Warlock.ClassId] = new[] { UpgradeIds.DeepProphecy, UpgradeIds.BlindMirror, UpgradeIds.Thrice, UpgradeIds.EyeOfTruth },
            [King.ClassId] = new[] { UpgradeIds.OverflowingTreasury, UpgradeIds.CheapSeizure, UpgradeIds.CrownGuard, UpgradeIds.RoyalDecree },
            [Jester.ClassId] = new[] { UpgradeIds.ControlledChaos, UpgradeIds.DoubleLaugh, UpgradeIds.JokerPurse, UpgradeIds.Buffoon },
        };

        /// <summary>Every upgrade, class by class (the icon strip's order).</summary>
        public static IReadOnlyList<string> All { get; } = ByClass.Values.SelectMany(u => u).ToArray();

        /// <summary>A class's four upgrades (none for a class without any).</summary>
        public static IReadOnlyList<string> For(string classId) =>
            classId != null && ByClass.TryGetValue(classId, out string[] ids) ? ids : Array.Empty<string>();

        /// <summary>The class an upgrade belongs to; null when unknown.</summary>
        public static string ClassOf(string upgradeId) => ByClass.FirstOrDefault(p => p.Value.Contains(upgradeId)).Key;

        public static bool IsKnown(string upgradeId) => upgradeId != null && All.Contains(upgradeId);
    }
}
