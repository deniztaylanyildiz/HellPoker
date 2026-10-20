using System;
using System.Collections.Generic;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>A small icon in the demon's block (his rule, a rule he carries, his rage) and its one-sentence hover.</summary>
    public sealed class SeatIcon
    {
        /// <summary>A cheat icon's id (Ui/cheat_icons), or <see cref="Rage"/>.</summary>
        public string IconId { get; }
        public string Tip { get; }

        public const string Rage = "rage";

        public SeatIcon(string iconId, string tip)
        {
            IconId = iconId ?? "";
            Tip = tip ?? "";
        }
    }

    /// <summary>
    /// Demo 3.5.1: the gauges under the demon's name. A floor's imp (a warden): PURSE — a coin, the number, a gold bar of what is left of
    /// the purse he sat down with — and ANTE with a dot for every hand before it grows (or the free hands left). A demon's table (Lucifer's):
    /// ANTE under his health bar (fixed: no dots) and his rule, a rule he carries and his rage as small icons. Every part has its hover.
    /// </summary>
    public sealed class SeatGauges
    {
        public static readonly SeatGauges Hidden = new SeatGauges(false, -1, 0, "", "", 0, "", null);

        public bool Shown { get; }

        /// <summary>The imp's purse now and when he sat down; <see cref="PurseShown"/> false (-1) at a demon's table.</summary>
        public int Purse { get; }
        public int PurseStart { get; }
        public bool PurseShown => Purse >= 0;
        public string PurseTip { get; }

        /// <summary>"ANTE 4", "ANTE 3%", "NO ANTE: 2".</summary>
        public string AnteLabel { get; }

        /// <summary>The hands before the ante grows, one dot each (0: none shown).</summary>
        public int AnteDots { get; }
        public string AnteTip { get; }

        public IReadOnlyList<SeatIcon> Icons { get; }

        public SeatGauges(bool shown, int purse, int purseStart, string purseTip, string anteLabel, int anteDots, string anteTip,
            IReadOnlyList<SeatIcon> icons)
        {
            Shown = shown;
            Purse = purse;
            PurseStart = Math.Max(1, purseStart);
            PurseTip = purseTip ?? "";
            AnteLabel = anteLabel ?? "";
            AnteDots = Math.Max(0, anteDots);
            AnteTip = anteTip ?? "";
            Icons = icons ?? Array.Empty<SeatIcon>();
        }

        /// <summary>What it says, as one string (the presenter shows it again only when it changed).</summary>
        public string Key => $"{Shown}|{Purse}/{PurseStart}|{AnteLabel}|{AnteDots}|{AnteTip}|{PurseTip}|" +
                             string.Join(";", System.Linq.Enumerable.Select(Icons, i => i.IconId + ":" + i.Tip));
    }
}
