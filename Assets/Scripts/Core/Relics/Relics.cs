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

        public RelicEffects(int antePercent = 100, int winPercent = 100, int houseCardsDelta = 0, int reRaiseExtraUnits = 0,
            int maliceExtraPerHand = 0, int redrawsPerTable = 0, int soulLossPercent = -1)
        {
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
                ReRaiseExtraUnits + other.ReRaiseExtraUnits, MaliceExtraPerHand + other.MaliceExtraPerHand, RedrawsPerTable + other.RedrawsPerTable, soul);
        }
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

        /// <summary>A rough estimate of what carrying it is worth over a run, in years (for the balance simulation's player).</summary>
        int ExpectedYears { get; }
    }

    public static class RelicIds
    {
        public const string BoneDie = "bone_die";
        public const string RustyCrown = "rusty_crown";
        public const string FerrymansCoin = "ferrymans_coin";
        public const string ThornedRosary = "thorned_rosary";
    }

    /// <summary>The Bone Die: once a table, before a draw, a card of yours is thrown back and redealt — but the House re-raises two units.
    /// (Once a hand it was a free extra discard: +12 / +11 / +22 points; only while the cards turned still +16 at Lilith's.)</summary>
    public sealed class BoneDie : IRelic
    {
        public string Id => RelicIds.BoneDie;
        public RelicEffects Effects { get; } = new RelicEffects(reRaiseExtraUnits: 1, redrawsPerTable: 1);
        public int ExpectedYears => 25;
    }

    /// <summary>The Rusty Crown: a win forgives a twentieth more — but the demon's malice grows one more every hand.
    /// (110% put the relics over the balance line: Lilith +3.4 with the Warlock.)</summary>
    public sealed class RustyCrown : IRelic
    {
        public string Id => RelicIds.RustyCrown;
        public RelicEffects Effects { get; } = new RelicEffects(winPercent: 105, maliceExtraPerHand: 1);
        public int ExpectedYears => 0;
    }

    /// <summary>The Ferryman's Coin: the ante is four fifths — but the House shows one card fewer. (Three quarters: see the crown.)</summary>
    public sealed class FerrymansCoin : IRelic
    {
        public string Id => RelicIds.FerrymansCoin;
        public RelicEffects Effects { get; } = new RelicEffects(antePercent: 80, houseCardsDelta: -1);
        public int ExpectedYears => 20;
    }

    /// <summary>The Thorned Rosary: the soul burns slower (losses ×1.25 instead of ×1.5) — but a win forgives a tenth less.</summary>
    public sealed class ThornedRosary : IRelic
    {
        public string Id => RelicIds.ThornedRosary;
        public RelicEffects Effects { get; } = new RelicEffects(winPercent: 90, soulLossPercent: 125);
        public int ExpectedYears => -40;
    }

    /// <summary>Every relic there is, and how many a run may carry.</summary>
    public static class RelicRoster
    {
        public const int MaxCarried = 2;

        public static IReadOnlyList<IRelic> All { get; } = new IRelic[] { new BoneDie(), new RustyCrown(), new FerrymansCoin(), new ThornedRosary() };

        public static IRelic Find(string id) => All.FirstOrDefault(r => r.Id == id);

        /// <summary>The combined effects of the relics carried.</summary>
        public static RelicEffects Combined(IEnumerable<string> ids)
        {
            RelicEffects all = RelicEffects.None;
            foreach (string id in ids ?? Enumerable.Empty<string>())
            {
                IRelic relic = Find(id);
                if (relic != null) all = all.With(relic.Effects);
            }
            return all;
        }
    }
}
