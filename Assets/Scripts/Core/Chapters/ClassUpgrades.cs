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

        // The Oracle's sight (Demo 2.2: the ids are the old Warlock's, kept for the saves).
        /// <summary>The Third Eye: FORESIGHT a level up (one card more).</summary>
        public const string DeepProphecy = "deep_prophecy";
        /// <summary>The Blind Mirror: half the hands the demon learns none of the Oracle's cards.</summary>
        public const string BlindMirror = "blind_mirror";
        /// <summary>The Fate Weaver (Demo 2.3; BEND FATE is gone): the Oracle sees which of his cards the demon knows.</summary>
        public const string Thrice = "thrice";
        /// <summary>The Prophecy Seal: a swindler's offer walked away from pays <see cref="ClassKit.SealCoins"/> coins.</summary>
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

        // The Thief's quick hand (Demo 2.2).
        /// <summary>Light Fingers: the quick hand a level up (caught less often: <see cref="ClassKit.Level"/>).</summary>
        public const string LightFingers = "light_fingers";
        /// <summary>The Night Eye: two of the demon's face-down cards glanced at.</summary>
        public const string NightEye = "night_eye";
        /// <summary>The Master Thief: the best of the demon's open cards is taken when there is one (else a face-down one, blind).</summary>
        public const string MasterThief = "master_thief";
        /// <summary>The Loot Sack: a steal on the floors brings coins too.</summary>
        public const string LootSack = "loot_sack";

        // The Summoner's imps (Demo 2.3: he had none).
        /// <summary>Two Loyal Imps: the first two imps of a hand come for nothing.</summary>
        public const string LoyalPair = "loyal_pair";
        /// <summary>Tame Imps: an imp never runs wild.</summary>
        public const string TameImps = "tame_imps";
        /// <summary>The Keen Eye: the EYE turns two of the demon's cards.</summary>
        public const string KeenEye = "keen_eye";
        /// <summary>The Blood Oath: an imp that runs wild lowers one of the demon's cards, not the Summoner's.</summary>
        public const string BloodOath = "blood_oath";
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
            [Thief.ClassId] = new[] { UpgradeIds.LightFingers, UpgradeIds.NightEye, UpgradeIds.MasterThief, UpgradeIds.LootSack },
            [Summoner.ClassId] = new[] { UpgradeIds.LoyalPair, UpgradeIds.TameImps, UpgradeIds.KeenEye, UpgradeIds.BloodOath },
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
