using System;
using System.Collections.Generic;

namespace HellPoker.Core.Chapters
{
    /// <summary>
    /// A stranger's offer at an event node: take it or pass. Most offers bend the purse against the sentence the demon will hold
    /// at the gate. A new offer: a class and a line in <see cref="FloorEventDeck"/>.
    /// </summary>
    public interface IFloorEvent
    {
        /// <summary>Stable id (the save, the presentation's words).</summary>
        string Id { get; }

        bool CanAppear(ChapterRun run);

        /// <summary>The player takes the offer.</summary>
        void Accept(ChapterRun run);
    }

    public static class FloorEventIds
    {
        public const string Usurer = "purgatory_usurer";
        public const string MammonsLedger = "mammons_ledger";
        public const string GamblerGhost = "gambler_ghost";
    }

    /// <summary>The Usurer of Purgatory: coins now; the demon's table starts with years more.</summary>
    public sealed class PurgatoryUsurer : IFloorEvent
    {
        public const int Coins = 30;
        public const int Years = 100;

        public string Id => FloorEventIds.Usurer;
        public bool CanAppear(ChapterRun run) => true;

        public void Accept(ChapterRun run)
        {
            run.Purse.Add(Coins);
            run.OweAtGate(Years);
        }
    }

    /// <summary>Mammon's Ledger (his chapter only): years struck off now, written back with interest at his table.</summary>
    public sealed class MammonsLedger : IFloorEvent
    {
        public const int YearsNow = 200;
        public const int YearsBack = 300;

        public string Id => FloorEventIds.MammonsLedger;
        public bool CanAppear(ChapterRun run) => run.Rules.BossId == Dealers.DealerRoster.MammonId && run.Years > 1;

        public void Accept(ChapterRun run)
        {
            run.StrikeYears(YearsNow);
            run.OweAtGate(YearsBack);
        }
    }

    /// <summary>The Gambler's Ghost: half the purse on one hand, nothing to raise. Never offered to a purse in debt.</summary>
    public sealed class GamblerGhost : IFloorEvent
    {
        public string Id => FloorEventIds.GamblerGhost;
        public bool CanAppear(ChapterRun run) => run.Purse.Coins / 2 > 0;
        public void Accept(ChapterRun run) => run.OpenGamble(run.Purse.Coins / 2);
    }

    public static class FloorEventDeck
    {
        public static IReadOnlyList<IFloorEvent> Standard => new IFloorEvent[] { new PurgatoryUsurer(), new MammonsLedger(), new GamblerGhost() };
    }
}
