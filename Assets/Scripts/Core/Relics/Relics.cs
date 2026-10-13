using System;
using System.Collections.Generic;
using System.Linq;

namespace HellPoker.Core.Relics
{
    /// <summary>
    /// What a cursed relic does to every hand while the player carries it — one gift and one curse — through the same kind of
    /// hooks as an event's next-hand modifier: the ante, a win, the House's open cards and re-raises, the demon's malice,
    /// the soul's burn, a redraw.
    /// </summary>
    public sealed class RelicEffects
    {
        // (Before None: the static fields are set in order.)
        private static readonly IReadOnlyDictionary<string, int> NoExtras = new Dictionary<string, int>();

        public static readonly RelicEffects None = new RelicEffects();

        /// <summary>The ante, in percent (the Ferryman's Coin: 80).</summary>
        public int AntePercent { get; }

        /// <summary>What a win forgives, in percent (the Rusty Crown: 105, the Thorned Rosary: 90).</summary>
        public int WinPercent { get; }

        /// <summary>House cards turned before the last decision, more or fewer (the Ferryman's Coin: −1; never below 0).</summary>
        public int HouseCardsDelta { get; }

        /// <summary>Units added to the House's re-raise (the Bone Die: +1, so two).</summary>
        public int ReRaiseExtraUnits { get; }

        /// <summary>Malice gained every hand on top of the usual (the Rusty Crown: +1).</summary>
        public int MaliceExtraPerHand { get; }

        /// <summary>Cards the player may redraw at each table, before a draw (the Bone Die: 1; full again at a new table).</summary>
        public int RedrawsPerTable { get; }

        /// <summary>A soul hand's loss surcharge in percent instead of the rule's (the Thorned Rosary: 125); -1: the rule's.</summary>
        public int SoulLossPercent { get; }

        /// <summary>A won hand forgives this share of the ante more, in percent (the Jester's Rattle: 50).</summary>
        public int WinAntePercent { get; }

        /// <summary>A lost hand adds this share of the ante more, in percent (the Jester's Rattle: 50).</summary>
        public int LossAntePercent { get; }

        // ---- Phase 2 only (the chapters read them; a demo table never does): percents of the chapter's own numbers.

        /// <summary>A floor's treasure, in percent.</summary>
        public int TreasurePercent { get; }

        /// <summary>The black market's prices, in percent.</summary>
        public int MarketPercent { get; }

        /// <summary>The gate's tribute, in percent.</summary>
        public int TributePercent { get; }

        /// <summary>A table imp's purse, in percent.</summary>
        public int ImpPursePercent { get; }

        /// <summary>A demon's bar as his table begins, in percent.</summary>
        public int BossBarPercent { get; }

        /// <summary>Phase 2's other marks (<see cref="RelicExtras"/>): counts and percent points that add up from relic to relic.</summary>
        public IReadOnlyDictionary<string, int> Extras { get; }

        /// <summary>A mark of <see cref="Extras"/>; 0 when no relic carries it.</summary>
        public int Extra(string key) => key != null && Extras.TryGetValue(key, out int n) ? n : 0;

        /// <summary>The unknown nodes of the map show what they hold (the Ferryman's Lantern).</summary>
        public bool RevealsUnknown => Extra(RelicExtras.RevealUnknown) > 0;

        public RelicEffects(int antePercent = 100, int winPercent = 100, int houseCardsDelta = 0, int reRaiseExtraUnits = 0,
            int maliceExtraPerHand = 0, int redrawsPerTable = 0, int soulLossPercent = -1, int winAntePercent = 0, int lossAntePercent = 0,
            int treasurePercent = 100, int marketPercent = 100, int tributePercent = 100, int impPursePercent = 100, int bossBarPercent = 100,
            IReadOnlyDictionary<string, int> extras = null)
        {
            Extras = extras ?? NoExtras;
            if (treasurePercent < 0 || marketPercent <= 0 || tributePercent < 0 || impPursePercent <= 0 || bossBarPercent <= 0)
                throw new ArgumentOutOfRangeException(nameof(treasurePercent));
            TreasurePercent = treasurePercent;
            MarketPercent = marketPercent;
            TributePercent = tributePercent;
            ImpPursePercent = impPursePercent;
            BossBarPercent = bossBarPercent;
            if (winAntePercent < 0 || lossAntePercent < 0) throw new ArgumentOutOfRangeException(nameof(winAntePercent));
            WinAntePercent = winAntePercent;
            LossAntePercent = lossAntePercent;
            if (antePercent <= 0 || winPercent < 0 || reRaiseExtraUnits < 0 || maliceExtraPerHand < 0 || redrawsPerTable < 0)
                throw new ArgumentOutOfRangeException(nameof(antePercent));
            AntePercent = antePercent;
            WinPercent = winPercent;
            HouseCardsDelta = houseCardsDelta;
            ReRaiseExtraUnits = reRaiseExtraUnits;
            MaliceExtraPerHand = maliceExtraPerHand;
            RedrawsPerTable = redrawsPerTable;
            SoulLossPercent = soulLossPercent;
        }

        /// <summary>Two relics at once: percents multiply, counts add, the lower soul burn wins.</summary>
        public RelicEffects With(RelicEffects other)
        {
            if (other == null) return this;
            int soul = SoulLossPercent < 0 ? other.SoulLossPercent : other.SoulLossPercent < 0 ? SoulLossPercent : Math.Min(SoulLossPercent, other.SoulLossPercent);
            return new RelicEffects(AntePercent * other.AntePercent / 100, WinPercent * other.WinPercent / 100, HouseCardsDelta + other.HouseCardsDelta,
                ReRaiseExtraUnits + other.ReRaiseExtraUnits, MaliceExtraPerHand + other.MaliceExtraPerHand, RedrawsPerTable + other.RedrawsPerTable, soul,
                WinAntePercent + other.WinAntePercent, LossAntePercent + other.LossAntePercent, TreasurePercent * other.TreasurePercent / 100,
                MarketPercent * other.MarketPercent / 100, TributePercent * other.TributePercent / 100, ImpPursePercent * other.ImpPursePercent / 100,
                BossBarPercent * other.BossBarPercent / 100, AddExtras(Extras, other.Extras));
        }

        private static IReadOnlyDictionary<string, int> AddExtras(IReadOnlyDictionary<string, int> a, IReadOnlyDictionary<string, int> b)
        {
            if (b.Count == 0) return a;
            if (a.Count == 0) return b;
            var sum = new Dictionary<string, int>();
            foreach (var p in a) sum[p.Key] = p.Value;
            foreach (var p in b) sum[p.Key] = (sum.TryGetValue(p.Key, out int n) ? n : 0) + p.Value;
            return sum;
        }

        /// <summary>
        /// This much (percent) of the effects' own pull away from nothing (Demo 2.1's forge adds half a gift: <c>Boon.Partial(50)</c>): every
        /// percent's distance from 100 and every count scaled, rounded away from zero (a single redraw stays one).
        /// </summary>
        public RelicEffects Partial(int percent)
        {
            int P(int value) => 100 + Away((value - 100) * percent);
            int N(int value) => Away(value * percent);
            int soul = SoulLossPercent < 0 ? -1 : Math.Max(0, Game.GameRules.Default.SoulLossPercent + Away((SoulLossPercent - Game.GameRules.Default.SoulLossPercent) * percent));
            var extras = Extras.Count == 0 ? null : Extras.ToDictionary(p => p.Key, p => N(p.Value));
            return new RelicEffects(Math.Max(1, P(AntePercent)), Math.Max(0, P(WinPercent)), N(HouseCardsDelta), Math.Max(0, N(ReRaiseExtraUnits)),
                Math.Max(0, N(MaliceExtraPerHand)), Math.Max(0, N(RedrawsPerTable)), soul, Math.Max(0, N(WinAntePercent)), Math.Max(0, N(LossAntePercent)),
                Math.Max(0, P(TreasurePercent)), Math.Max(1, P(MarketPercent)), Math.Max(0, P(TributePercent)), Math.Max(1, P(ImpPursePercent)),
                Math.Max(1, P(BossBarPercent)), extras);
        }

        /// <summary>n / 100, rounded away from zero.</summary>
        private static int Away(int hundredths) => hundredths >= 0 ? (hundredths + 99) / 100 : -((-hundredths + 99) / 100);

        /// <summary>Phase 2's marks as a relic's own (key, amount) pairs.</summary>
        public static IReadOnlyDictionary<string, int> Marks(params (string key, int amount)[] marks) =>
            marks.ToDictionary(m => m.key, m => m.amount);
    }

    /// <summary>The keys of <see cref="RelicEffects.Extras"/>: Phase 2's marks of the relics (Demo 2.1's), each read where its rule is.</summary>
    public static class RelicExtras
    {
        public const string RevealUnknown = "reveal.unknown";

        /// <summary>A swindler's trap reads plainly (the Liar's Tongue).</summary>
        public const string TrapHints = "trap.hints";

        /// <summary>Points on a helper's good turn, in percent (the Lost Soul's Letter +20, the Liar's Tongue −15).</summary>
        public const string HelpGoodPercent = "help.good";

        /// <summary>A camp's rest is this much (percent) more (the Rusted Chain); a camp may not forge while it is worn.</summary>
        public const string RestBonusPercent = "rest.bonus", NoForge = "forge.none";

        /// <summary>The class relics (Demo 2.1): the satchel's room and risk, the prophecy's cards and the bottom forbidden, the treasury's
        /// cap and the seizure's price, the imps' wildness (percent points) and their price (coins on a floor; a demon's table: percent).</summary>
        public const string SatchelRoom = "satchel.room", SatchelRisk = "satchel.risk", ProphecyCards = "prophecy.cards", ProphecyNoBottom = "prophecy.nobottom",
            TaxCap = "tax.cap", SeizePrice = "seize.price", ImpWild = "imp.wild", ImpPrice = "imp.price", ImpBarPrice = "imp.bar";

        /// <summary>The floors (Demo 2.1's Ash Purse): coins more for a won floor hand; the first hand's ante more.</summary>
        public const string FloorWinCoins = "floor.win", FloorFirstAnte = "floor.first.ante";

        /// <summary>A demon's table: lost hands spared as pushes (the Lead Seal: the first at each), a win's percent points (the Lead Seal −10),
        /// the bar never heals (the Blood Pact), the sentence's share of a loss in points (the Blood Pact +25), Lucifer's Feather (once a run).</summary>
        public const string BossSpare = "boss.spare", BossWinPoints = "boss.win", BarNeverHeals = "bar.noheal", SentenceLossPoints = "sentence.loss",
            FeatherSpare = "feather";

        /// <summary>Years on the sentence for every offer taken (the Lost Soul's Letter).</summary>
        public const string EventYears = "event.years";

        /// <summary>A won hand with a joker in it, a hand lost to two jokers: percent points more (the Jester's Hat).</summary>
        public const string JokerWinPoints = "joker.win", JokerBustPoints = "joker.bust";

        /// <summary>Mammon's Gold Tooth: points on a demon's win for every 50 coins brought to the gate, before the tribute (at most 15).</summary>
        public const string ToothPoints = "tooth";
    }

    /// <summary>
    /// A cursed relic, won from an event: it stays with the run (at most <see cref="RelicRoster.MaxCarried"/>), at every table,
    /// and changes every hand — a gift and a curse. A new relic: a class and one line in <see cref="RelicRoster.All"/>.
    /// </summary>
    public interface IRelic
    {
        /// <summary>Stable id: the save ("relics") and the presentation's words and icon.</summary>
        string Id { get; }

        RelicEffects Effects { get; }

        /// <summary>The gift alone, the curse silenced (a floor's purgatory fire, until the chapter ends).</summary>
        RelicEffects Boon { get; }

        /// <summary>A rough estimate of what carrying it is worth over a run, in years (for the balance simulation's player).</summary>
        int ExpectedYears { get; }

        /// <summary>
        /// Earned, never offered (the Jester's Rattle, for twenty jokers): no event deals it, and it does not count towards
        /// <see cref="RelicRoster.MaxCarried"/>.
        /// </summary>
        bool IsReward { get; }
    }

    public static class RelicIds
    {
        public const string BoneDie = "bone_die";
        public const string RustyCrown = "rusty_crown";
        public const string FerrymansCoin = "ferrymans_coin";
        public const string ThornedRosary = "thorned_rosary";
        public const string JestersRattle = "jesters_rattle";
    }

    /// <summary>The Bone Die: once a table, before a draw, a card of yours is thrown back and redealt — but the House re-raises two units.
    /// (Once a hand it was a free extra discard: +12 / +11 / +22 points; only while the cards turned still +16 at Lilith's.)</summary>
    public sealed class BoneDie : IRelic
    {
        public string Id => RelicIds.BoneDie;
        public RelicEffects Effects { get; } = new RelicEffects(reRaiseExtraUnits: 1, redrawsPerTable: 1);
        public RelicEffects Boon { get; } = new RelicEffects(redrawsPerTable: 1);
        public int ExpectedYears => 25;
        public bool IsReward => false;
    }

    /// <summary>The Rusty Crown: a win forgives a twentieth more — but the demon's malice grows one more every hand.
    /// (110% put the relics over the balance line: Lilith +3.4 with the Warlock.)</summary>
    public sealed class RustyCrown : IRelic
    {
        public string Id => RelicIds.RustyCrown;
        public RelicEffects Effects { get; } = new RelicEffects(winPercent: 105, maliceExtraPerHand: 1);
        public RelicEffects Boon { get; } = new RelicEffects(winPercent: 105);
        public int ExpectedYears => 0;
        public bool IsReward => false;
    }

    /// <summary>The Ferryman's Coin: the ante is four fifths — but the House shows one card fewer. (Three quarters: see the crown.)</summary>
    public sealed class FerrymansCoin : IRelic
    {
        public string Id => RelicIds.FerrymansCoin;
        public RelicEffects Effects { get; } = new RelicEffects(antePercent: 80, houseCardsDelta: -1);
        public RelicEffects Boon { get; } = new RelicEffects(antePercent: 80);
        public int ExpectedYears => 20;
        public bool IsReward => false;
    }

    /// <summary>The Thorned Rosary: the soul burns slower (losses ×1.25 instead of ×1.5) — but a win forgives a tenth less.</summary>
    public sealed class ThornedRosary : IRelic
    {
        public string Id => RelicIds.ThornedRosary;
        public RelicEffects Effects { get; } = new RelicEffects(winPercent: 90, soulLossPercent: 125);
        public RelicEffects Boon { get; } = new RelicEffects(soulLossPercent: 125);
        public int ExpectedYears => -40;
        public bool IsReward => false;
    }

    /// <summary>
    /// The Jester's Rattle: earned when the Jester's deck reaches twenty jokers (once a run; the jokers go and the count starts again).
    /// A won hand forgives half an ante more — and a lost one adds half an ante more: the swings, doubled.
    /// </summary>
    public sealed class JestersRattle : IRelic
    {
        public string Id => RelicIds.JestersRattle;
        public RelicEffects Effects { get; } = new RelicEffects(winAntePercent: 50, lossAntePercent: 50);
        public RelicEffects Boon { get; } = new RelicEffects(winAntePercent: 50);
        public int ExpectedYears => 0;
        public bool IsReward => true;
    }

    /// <summary>The demo's relics, and how many a demo run may carry (a Phase 2 run carries <see cref="ChapterRelics.Slots"/>).</summary>
    public static class RelicRoster
    {
        public const int MaxCarried = 2;

        /// <summary>Every relic, in the icon strip's order (the rewards last).</summary>
        public static IReadOnlyList<IRelic> All { get; } = new IRelic[] { new BoneDie(), new RustyCrown(), new FerrymansCoin(), new ThornedRosary(), new JestersRattle() };

        /// <summary>The relics an event may offer (not the rewards).</summary>
        public static IEnumerable<IRelic> Offered => All.Where(r => !r.IsReward);

        public static bool IsReward(string id) => Find(id)?.IsReward ?? false;

        /// <summary>A relic by id: the demo's five, then Phase 2's own (<see cref="ChapterRelics"/>); null when unknown.</summary>
        public static IRelic Find(string id) => All.FirstOrDefault(r => r.Id == id) ?? (IRelic)ChapterRelics.Find(id);

        /// <summary>The icon strip's order: the demo's relics, then Phase 2's.</summary>
        public static IReadOnlyList<string> IconOrder { get; } = All.Select(r => r.Id).Concat(ChapterRelics.New.Select(r => r.Id)).ToArray();

        /// <summary>The combined effects of the relics carried; the one named <paramref name="silenced"/> brings its gift alone, the one
        /// named <paramref name="amplified"/> counts twice (Phase 2's Desire: its gift and its curse doubled).</summary>
        /// <param name="forged">Relics forged at a camp (Demo 2.1): their gift half again.</param>
        /// <param name="cleansed">Relics whose curse the alchemist burned away (Demo 2.1): their gift alone, for good.</param>
        public static RelicEffects Combined(IEnumerable<string> ids, string silenced = null, string amplified = null, ICollection<string> forged = null,
            ICollection<string> cleansed = null)
        {
            RelicEffects all = RelicEffects.None;
            foreach (string id in ids ?? Enumerable.Empty<string>())
            {
                IRelic relic = Find(id);
                if (relic == null) continue;
                RelicEffects effects = id == silenced || (cleansed != null && cleansed.Contains(id)) ? relic.Boon : relic.Effects;
                all = all.With(effects);
                if (id == amplified) all = all.With(effects);
                if (forged != null && forged.Contains(id)) all = all.With(relic.Boon.Partial(ForgePercent));
            }
            return all;
        }

        /// <summary>A forged relic's gift counts this much (percent) more.</summary>
        public const int ForgePercent = 50;
    }
}
