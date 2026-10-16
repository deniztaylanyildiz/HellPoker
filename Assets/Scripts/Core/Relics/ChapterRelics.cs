using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Randomness;

namespace HellPoker.Core.Relics
{
    /// <summary>How rare a Phase 2 relic is: the market and the wardens draw by it, the boss's own two are his loot.</summary>
    public enum RelicRarity
    {
        Common,
        Rare,

        /// <summary>A strong gift and a heavy curse.</summary>
        Legendary,

        /// <summary>A beaten demon's own (Mammon's, Belial's): only his loot gives it.</summary>
        Boss,

        /// <summary>Earned, never drawn (the Jester's Rattle).</summary>
        Reward
    }

    /// <summary>How often each rarity is drawn: a market's table, a warden's prize (percent; they need not add up to 100).</summary>
    public readonly struct RelicWeights
    {
        public int Common { get; }
        public int Rare { get; }
        public int Legendary { get; }

        public RelicWeights(int common, int rare, int legendary)
        {
            if (common < 0 || rare < 0 || legendary < 0) throw new ArgumentOutOfRangeException(nameof(common));
            Common = common;
            Rare = rare;
            Legendary = legendary;
        }

        public int Of(RelicRarity rarity) => rarity == RelicRarity.Common ? Common : rarity == RelicRarity.Rare ? Rare : rarity == RelicRarity.Legendary ? Legendary : 0;

        /// <summary>The black market: rare 25%, legendary 8%.</summary>
        public static readonly RelicWeights Market = new RelicWeights(67, 25, 8);

        /// <summary>A warden's prize: rare 40%, legendary 15%.</summary>
        public static readonly RelicWeights Warden = new RelicWeights(45, 40, 15);
    }

    /// <summary>A Phase 2 relic: a gift and a curse like the demo's, with its rarity (and, for a boss's own, the boss).</summary>
    public sealed class ChapterRelic : IRelic
    {
        public ChapterRelic(string id, RelicRarity rarity, RelicEffects effects, RelicEffects boon, int expectedYears = 0, string bossId = null,
            string classId = null)
        {
            ClassId = classId;
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Rarity = rarity;
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
            Boon = boon ?? throw new ArgumentNullException(nameof(boon));
            ExpectedYears = expectedYears;
            BossId = bossId;
        }

        public string Id { get; }
        public RelicRarity Rarity { get; }
        public RelicEffects Effects { get; }
        public RelicEffects Boon { get; }
        public int ExpectedYears { get; }
        public bool IsReward => false;

        /// <summary>The demon whose loot it is; null for the others.</summary>
        public string BossId { get; }

        /// <summary>The class whose relic it is (its gift is that class's): drawn only for that class, three times as often (Demo 2.1).</summary>
        public string ClassId { get; }
    }

    public static class ChapterRelicIds
    {
        public const string BloodyDie = "bloody_die";
        public const string LeadPurse = "lead_purse";
        public const string AshLedger = "ash_ledger";
        public const string CandleStub = "candle_stub";
        public const string RatTooth = "rat_tooth";

        public const string IronVow = "iron_vow";
        public const string PilgrimsLantern = "pilgrims_lantern";
        public const string RavenFeather = "raven_feather";
        public const string BloodRing = "blood_ring";
        public const string OldMap = "old_map";
        public const string SilverBell = "silver_bell";
        public const string SaltCircle = "salt_circle";

        public const string BlackBook = "black_book";
        public const string HangmansRope = "hangmans_rope";
        public const string LilithsTear = "liliths_tear";
        public const string AbyssCoin = "abyss_coin";

        public const string MammonsSeal = "mammons_seal";
        public const string MammonsScales = "mammons_scales";
        public const string BelialsMask = "belials_mask";
        public const string BelialsTongue = "belials_tongue";

        // Demo 2.1: fifteen more, for the bag, the camps, the offers, the markets, the map and the classes.
        public const string AshPurse = "ash_purse", FerrymansLantern = "ferrymans_lantern", RustedChain = "rusted_chain", LeadSeal = "lead_seal";
        public const string LiarsTongue = "liars_tongue", BloodPact = "blood_pact", LostSoulsLetter = "lost_souls_letter", TwinDice = "twin_dice",
            JestersHat = "jesters_hat", SeersSpectacles = "seers_spectacles", PeasantsScythe = "peasants_scythe", KingsSignet = "kings_signet",
            SummonersLeash = "summoners_leash";
        public const string LucifersFeather = "lucifers_feather", MammonsGoldTooth = "mammons_gold_tooth";

        // 2.1.1
        public const string DevilsCompass = "devils_compass";

        // Demo 2.3: every class's three relics (common, rare, legendary), each a step of its main power; the rare ones are Demo 2.1's.
        public const string RopeBelt = "rope_belt", HarvestMoon = "harvest_moon";
        public const string CrackedOrb = "cracked_orb", StarChart = "star_chart";
        public const string TaxLedger = "tax_ledger", GoldenScepter = "golden_scepter";
        public const string ChalkCircle = "chalk_circle", BlackGrimoire = "black_grimoire";
        public const string WornGloves = "worn_gloves", Lockpick = "lockpick", ShadowCloak = "shadow_cloak";
        public const string BellCollar = "bell_collar", MockingMask = "mocking_mask";
    }

    /// <summary>
    /// Phase 2's relics: the demo's four offered ones (common, but the Bone Die rare) and twenty of the chapters' own — eight common, eight
    /// rare, four legendary and cursed, and two of Mammon's and two of Belial's own for their loot (Lilith's fall leads straight to
    /// Lucifer). A run carries <see cref="Slots"/>. (Tuned by the whole-run simulation: the Blood Ring's curse 40% of an ante → the malice +1 and its
    /// gift 112% → 110%, the Bloody Die's curse 25% → 20%, the Hangman's Rope 60% → 75%, Lilith's Tear 85% → 90% — each moved freedom by 10 points or more.) A new one: an id, a line in <see cref="New"/>, words in UiText.Relics, an icon.
    /// </summary>
    public static class ChapterRelics
    {
        /// <summary>A Phase 2 run carries this many relics (the demo: <see cref="RelicRoster.MaxCarried"/>).</summary>
        public const int Slots = 5;

        /// <summary>A Phase 2 run's bag: relics held but not worn (Demo 2.1; worn and bagged are changed at a camp).</summary>
        public const int BagSlots = 5;

        /// <summary>The market's first-chapter price by rarity.</summary>
        /// (Demo 2.1: 45 / 55 / 65 → 50 / 60 / 70 → 55 / 65 / 75 — five worn relics and fifteen new ones lifted freedom 6.4 points over Demo 2.0.1.)
        public const int CommonPrice = 55, RarePrice = 65, LegendaryPrice = 75;

        private static ChapterRelic R(string id, RelicRarity rarity, RelicEffects effects, RelicEffects boon, int expected = 0, string boss = null,
            string cls = null) => new ChapterRelic(id, rarity, effects, boon, expected, boss, cls);

        /// <summary>A relic of Phase 2's marks alone (<see cref="RelicExtras"/>): <paramref name="gift"/> its boon, <paramref name="curse"/> the rest.</summary>
        private static ChapterRelic M(string id, RelicRarity rarity, (string, int)[] gift, (string, int)[] curse, int expected, string cls = null,
            RelicEffects giftEffects = null, RelicEffects curseEffects = null)
        {
            RelicEffects boon = new RelicEffects(extras: gift.Length == 0 ? null : RelicEffects.Marks(gift)).With(giftEffects);
            RelicEffects all = boon.With(new RelicEffects(extras: curse.Length == 0 ? null : RelicEffects.Marks(curse))).With(curseEffects);
            return new ChapterRelic(id, rarity, all, boon, expected, null, cls);
        }

        private static (string, int)[] Mk(params (string, int)[] marks) => marks;

        /// <summary>The chapters' own relics, in the icon strip's order (after the demo's).</summary>
        public static IReadOnlyList<ChapterRelic> New { get; } = new[]
        {
            R(ChapterRelicIds.BloodyDie, RelicRarity.Common, new RelicEffects(winAntePercent: 25, lossAntePercent: 20), new RelicEffects(winAntePercent: 25)),
            R(ChapterRelicIds.LeadPurse, RelicRarity.Common, new RelicEffects(treasurePercent: 150, marketPercent: 120), new RelicEffects(treasurePercent: 150)),
            R(ChapterRelicIds.AshLedger, RelicRarity.Common, new RelicEffects(tributePercent: 90, winPercent: 95), new RelicEffects(tributePercent: 90)),
            R(ChapterRelicIds.CandleStub, RelicRarity.Common, new RelicEffects(houseCardsDelta: 1, antePercent: 115), new RelicEffects(houseCardsDelta: 1), 10),
            R(ChapterRelicIds.RatTooth, RelicRarity.Common, new RelicEffects(marketPercent: 85, lossAntePercent: 15), new RelicEffects(marketPercent: 85)),

            R(ChapterRelicIds.IronVow, RelicRarity.Rare, new RelicEffects(soulLossPercent: 110, antePercent: 115), new RelicEffects(soulLossPercent: 110), 10),
            R(ChapterRelicIds.PilgrimsLantern, RelicRarity.Rare, new RelicEffects(houseCardsDelta: 1, reRaiseExtraUnits: 1), new RelicEffects(houseCardsDelta: 1), 15),
            R(ChapterRelicIds.RavenFeather, RelicRarity.Rare, new RelicEffects(bossBarPercent: 92, maliceExtraPerHand: 1), new RelicEffects(bossBarPercent: 92), 20),
            R(ChapterRelicIds.BloodRing, RelicRarity.Rare, new RelicEffects(winPercent: 110, maliceExtraPerHand: 1), new RelicEffects(winPercent: 110), 10),
            R(ChapterRelicIds.OldMap, RelicRarity.Rare, new RelicEffects(marketPercent: 75, tributePercent: 110), new RelicEffects(marketPercent: 75)),
            R(ChapterRelicIds.SilverBell, RelicRarity.Rare, new RelicEffects(redrawsPerTable: 1, antePercent: 110), new RelicEffects(redrawsPerTable: 1), 20),
            R(ChapterRelicIds.SaltCircle, RelicRarity.Rare, new RelicEffects(treasurePercent: 200, impPursePercent: 115), new RelicEffects(treasurePercent: 200)),

            R(ChapterRelicIds.BlackBook, RelicRarity.Legendary, new RelicEffects(winPercent: 125, maliceExtraPerHand: 2), new RelicEffects(winPercent: 125), 20),
            R(ChapterRelicIds.HangmansRope, RelicRarity.Legendary, new RelicEffects(antePercent: 75, soulLossPercent: 175), new RelicEffects(antePercent: 75), 10),
            R(ChapterRelicIds.LilithsTear, RelicRarity.Legendary, new RelicEffects(soulLossPercent: 100, winPercent: 90), new RelicEffects(soulLossPercent: 100), 10),
            R(ChapterRelicIds.AbyssCoin, RelicRarity.Legendary, new RelicEffects(winPercent: 140, lossAntePercent: 100), new RelicEffects(winPercent: 140), 15),

            R(ChapterRelicIds.MammonsSeal, RelicRarity.Boss, new RelicEffects(tributePercent: 75, maliceExtraPerHand: 1), new RelicEffects(tributePercent: 75),
                15, Dealers.DealerRoster.MammonId),
            R(ChapterRelicIds.MammonsScales, RelicRarity.Boss, new RelicEffects(winAntePercent: 50, antePercent: 120), new RelicEffects(winAntePercent: 50),
                15, Dealers.DealerRoster.MammonId),
            R(ChapterRelicIds.BelialsMask, RelicRarity.Boss, new RelicEffects(houseCardsDelta: 1, winPercent: 95), new RelicEffects(houseCardsDelta: 1),
                15, Dealers.DealerRoster.BelialId),
            R(ChapterRelicIds.BelialsTongue, RelicRarity.Boss, new RelicEffects(redrawsPerTable: 1, bossBarPercent: 105), new RelicEffects(redrawsPerTable: 1),
                15, Dealers.DealerRoster.BelialId),

            // Demo 2.1 — common
            M(ChapterRelicIds.AshPurse, RelicRarity.Common, Mk((RelicExtras.FloorWinCoins, 2)), Mk((RelicExtras.FloorFirstAnte, 1)), 5),
            M(ChapterRelicIds.FerrymansLantern, RelicRarity.Common, Mk((RelicExtras.RevealUnknown, 1)), Mk(), 0, curseEffects: new RelicEffects(marketPercent: 110)),
            M(ChapterRelicIds.RustedChain, RelicRarity.Common, Mk((RelicExtras.RestBonusPercent, 50)), Mk((RelicExtras.NoForge, 1)), 10),
            M(ChapterRelicIds.LeadSeal, RelicRarity.Common, Mk((RelicExtras.BossSpare, 1)), Mk((RelicExtras.BossWinPoints, -10)), 5),
            // rare
            M(ChapterRelicIds.LiarsTongue, RelicRarity.Rare, Mk((RelicExtras.TrapHints, 1)), Mk((RelicExtras.HelpGoodPercent, -15)), 5),
            // (The bar never healing at all: +35 points of freedom in the simulation — half its healing instead.)
            M(ChapterRelicIds.BloodPact, RelicRarity.Rare, Mk((RelicExtras.BarNeverHeals, 25)), Mk((RelicExtras.SentenceLossPoints, 25)), 20),
            M(ChapterRelicIds.LostSoulsLetter, RelicRarity.Rare, Mk((RelicExtras.HelpGoodPercent, 20)), Mk((RelicExtras.EventYears, 20)), 0),
            R(ChapterRelicIds.TwinDice, RelicRarity.Rare, new RelicEffects(redrawsPerTable: 1, maliceExtraPerHand: 1), new RelicEffects(redrawsPerTable: 1), 15),
            // (Demo 2.3: the five class relics are the rare steps of their classes' chains — see the end of the list.)
            C(ChapterRelicIds.JestersHat, RelicRarity.Rare, Sinners.Jester.ClassId, Mk((RelicExtras.JokerBustPoints, 25))),
            C(ChapterRelicIds.SeersSpectacles, RelicRarity.Rare, Sinners.Warlock.ClassId, Mk((RelicExtras.ProphecyBully, 10))),
            C(ChapterRelicIds.PeasantsScythe, RelicRarity.Rare, Sinners.Peasant.ClassId, Mk((RelicExtras.SatchelRisk, 5))),
            C(ChapterRelicIds.KingsSignet, RelicRarity.Rare, Sinners.King.ClassId, Mk((RelicExtras.SeizePrice, 1))),
            C(ChapterRelicIds.SummonersLeash, RelicRarity.Rare, Sinners.Summoner.ClassId, Mk((RelicExtras.ImpPrice, 2), (RelicExtras.ImpBarPrice, 1))),
            // legendary
            M(ChapterRelicIds.LucifersFeather, RelicRarity.Legendary, Mk((RelicExtras.FeatherSpare, 1)), Mk(), 15),
            M(ChapterRelicIds.MammonsGoldTooth, RelicRarity.Legendary, Mk((RelicExtras.ToothPoints, 3)), Mk(), 20, curseEffects: new RelicEffects(tributePercent: 115)),
            // 2.1.1 — rare: two steps a chapter across a floor of the map, forty years each.
            M(ChapterRelicIds.DevilsCompass, RelicRarity.Rare, Mk((RelicExtras.CompassSteps, CompassSteps)), Mk((RelicExtras.CompassYears, CompassYears)), 5),
            // Demo 2.3 — the classes' chains: a common and a legendary step for every class (the rare is above), the Thief's three.
            C(ChapterRelicIds.RopeBelt, RelicRarity.Common, Sinners.Peasant.ClassId, Mk(), new RelicEffects(antePercent: 105)),
            C(ChapterRelicIds.HarvestMoon, RelicRarity.Legendary, Sinners.Peasant.ClassId, Mk(), new RelicEffects(maliceExtraPerHand: 1)),
            C(ChapterRelicIds.CrackedOrb, RelicRarity.Common, Sinners.Warlock.ClassId, Mk(), new RelicEffects(antePercent: 105)),
            C(ChapterRelicIds.StarChart, RelicRarity.Legendary, Sinners.Warlock.ClassId, Mk(), new RelicEffects(maliceExtraPerHand: 1)),
            C(ChapterRelicIds.TaxLedger, RelicRarity.Common, Sinners.King.ClassId, Mk(), new RelicEffects(marketPercent: 110)),
            C(ChapterRelicIds.GoldenScepter, RelicRarity.Legendary, Sinners.King.ClassId, Mk(), new RelicEffects(maliceExtraPerHand: 1)),
            C(ChapterRelicIds.ChalkCircle, RelicRarity.Common, Sinners.Summoner.ClassId, Mk((RelicExtras.ImpPrice, 1))),
            C(ChapterRelicIds.BlackGrimoire, RelicRarity.Legendary, Sinners.Summoner.ClassId, Mk(), new RelicEffects(maliceExtraPerHand: 1)),
            C(ChapterRelicIds.WornGloves, RelicRarity.Common, Sinners.Thief.ClassId, Mk(), new RelicEffects(antePercent: 105)),
            C(ChapterRelicIds.Lockpick, RelicRarity.Rare, Sinners.Thief.ClassId, Mk(), new RelicEffects(lossAntePercent: 10)),
            C(ChapterRelicIds.ShadowCloak, RelicRarity.Legendary, Sinners.Thief.ClassId, Mk(), new RelicEffects(maliceExtraPerHand: 1)),
            C(ChapterRelicIds.BellCollar, RelicRarity.Common, Sinners.Jester.ClassId, Mk(), new RelicEffects(antePercent: 105)),
            C(ChapterRelicIds.MockingMask, RelicRarity.Legendary, Sinners.Jester.ClassId, Mk(), new RelicEffects(maliceExtraPerHand: 1)),
        };

        /// <summary>A class relic (Demo 2.3): its gift a step of the class's main power (<see cref="RelicExtras.ClassStep"/>), its curse a
        /// light one (marks, or the hand's own effects). The simulation's player wears it before anything (its worth).</summary>
        private static ChapterRelic C(string id, RelicRarity rarity, string cls, (string, int)[] curse, RelicEffects curseEffects = null) =>
            M(id, rarity, Mk((RelicExtras.ClassStep, 1)), curse, ClassRelicWorth + (int)rarity * 5, cls, curseEffects: curseEffects);

        /// <summary>What a class relic is worth to the simulation's player (more than any other: the class's power grows with it).</summary>
        public const int ClassRelicWorth = 40;

        /// <summary>A class's three relics in the order they come (common, rare, legendary); empty for an unknown class.</summary>
        public static IReadOnlyList<string> ClassChain(string classId) =>
            New.Where(r => r.ClassId != null && r.ClassId == classId).OrderBy(r => r.Rarity).Select(r => r.Id).ToArray();

        /// <summary>A class relic: one of a class's chain.</summary>
        public static bool IsClassRelic(string id) => Find(id)?.ClassId != null;

        /// <summary>
        /// The class relic that comes next (Demo 2.3: they come in order — the common, then the rare, then the legendary — so the power grows
        /// step by step): the first of the chain the run does not hold. Null when it holds all three, or the next one is locked.
        /// </summary>
        public static string NextClassRelic(string classId, Func<string, bool> held, Func<string, bool> available = null)
        {
            string next = ClassChain(classId).FirstOrDefault(id => held == null || !held(id));
            return next != null && (available == null || available(next)) ? next : null;
        }

        /// <summary>How often (percent) a market's stall shows the class's next relic, and a warden leaves it as his prize (a tuning knob).</summary>
        public static int ClassRelicChance = 35;

        /// <summary>The Devil's Compass: steps a chapter, and the years every step puts on the sentence.</summary>
        public const int CompassSteps = 2, CompassYears = 40;

        public static ChapterRelic Find(string id) => New.FirstOrDefault(r => r.Id == id);

        /// <summary>Every relic a Phase 2 run may hold, by rarity: the demo's four offered ones and the chapters' twenty (the Rattle apart).</summary>
        public static IReadOnlyList<string> All { get; } = RelicRoster.Offered.Select(r => r.Id).Concat(New.Select(r => r.Id)).ToArray();

        /// <summary>A relic's rarity: the demo's are common (the Bone Die rare), the Rattle a reward.</summary>
        public static RelicRarity RarityOf(string id)
        {
            ChapterRelic relic = Find(id);
            if (relic != null) return relic.Rarity;
            if (id == RelicIds.JestersRattle) return RelicRarity.Reward;
            return id == RelicIds.BoneDie ? RelicRarity.Rare : RelicRarity.Common;
        }

        public static IEnumerable<string> OfRarity(RelicRarity rarity) => All.Where(id => RarityOf(id) == rarity);

        /// <summary>A beaten demon's own two relics (none for a demon without loot).</summary>
        public static IReadOnlyList<string> BossLoot(string bossId) => New.Where(r => r.BossId != null && r.BossId == bossId).Select(r => r.Id).ToArray();

        /// <summary>The market's first-chapter price of a relic, by its rarity.</summary>
        public static int PriceOf(string id)
        {
            switch (RarityOf(id))
            {
                case RelicRarity.Legendary: return LegendaryPrice;
                case RelicRarity.Rare: case RelicRarity.Boss: return RarePrice;
                default: return CommonPrice;
            }
        }

        /// <summary>
        /// <paramref name="count"/> different relics drawn by <paramref name="weights"/>: a rarity first (among those with a relic left), then a
        /// relic of it. Never one in <paramref name="exclude"/> (those carried) or one <paramref name="available"/> refuses (locked).
        /// </summary>
        /// <param name="classId">The run's class. Demo 2.3: no class relic is drawn here — they come in order, by their own ways
        /// (<see cref="NextClassRelic"/>: a market's stall, a warden's prize, a demon's loot).</param>
        public static IReadOnlyList<string> Draw(IRandomSource random, RelicWeights weights, int count, ICollection<string> exclude = null,
            Func<string, bool> available = null, string classId = null)
        {
            Func<string, bool> classOk = id => !IsClassRelic(id);
            Func<string, bool> unlocked = available;
            available = id => classOk(id) && (unlocked == null || unlocked(id));
            if (random == null) throw new ArgumentNullException(nameof(random));
            var picked = new List<string>();
            var tiers = new[] { RelicRarity.Common, RelicRarity.Rare, RelicRarity.Legendary };
            while (picked.Count < count)
            {
                var left = tiers.ToDictionary(t => t, t => OfRarity(t)
                    .Where(id => !picked.Contains(id) && (exclude == null || !exclude.Contains(id)) && (available == null || available(id))).ToList());
                var open = tiers.Where(t => left[t].Count > 0 && weights.Of(t) > 0).ToList();
                if (open.Count == 0) open = tiers.Where(t => left[t].Count > 0).ToList();   // nothing weighted is left: whatever is
                if (open.Count == 0) break;
                int total = open.Sum(t => Math.Max(1, weights.Of(t)));
                int roll = random.Next(total);
                RelicRarity tier = open.Last();
                foreach (RelicRarity t in open)
                {
                    roll -= Math.Max(1, weights.Of(t));
                    if (roll < 0)
                    {
                        tier = t;
                        break;
                    }
                }
                var pool = left[tier];
                picked.Add(pool[random.Next(pool.Count)]);
            }
            return picked;
        }
    }
}
