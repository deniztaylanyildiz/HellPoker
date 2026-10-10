using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Dealers;

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
        public const string LyingWitness = "lying_witness";
        public const string Spectacle = "spectacle";
        public const string FalseCoin = "false_coin";
        public const string NightBargain = "night_bargain";
        public const string Desire = "desire";
        public const string Insomnia = "insomnia";
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
        public bool CanAppear(ChapterRun run) => run.Rules.BossId == DealerRoster.MammonId && run.Years > 1;

        public void Accept(ChapterRun run)
        {
            run.StrikeYears(YearsNow);
            run.OweAtGate(YearsBack);
        }
    }

    /// <summary>The Gambler's Ghost: half the purse on one hand, nothing to raise; a loss costs that half and no more. Never offered
    /// to a purse of less than two coins.</summary>
    public sealed class GamblerGhost : IFloorEvent
    {
        public string Id => FloorEventIds.GamblerGhost;
        public bool CanAppear(ChapterRun run) => run.Purse.Coins / 2 > 0;
        public void Accept(ChapterRun run) => run.OpenGamble(run.Purse.Coins / 2);
    }

    /// <summary>The Lying Witness (Belial's chapter): coins for a look at one of the next imp's cards — a look that lies one time in four.</summary>
    public sealed class LyingWitness : IFloorEvent
    {
        public const int Price = 15;
        public const int LiePercent = 25;

        public string Id => FloorEventIds.LyingWitness;
        public bool CanAppear(ChapterRun run) => run.Rules.BossId == DealerRoster.BelialId && run.Purse.Coins > Price;

        public void Accept(ChapterRun run)
        {
            if (!run.Purse.TrySpend(Price)) return;
            run.AddTableMarks(new TableMarks(openCard: true, openCardLiePercent: LiePercent));
        }
    }

    /// <summary>The Spectacle (Belial's chapter): at the next table the imp shows no card at all — but every hand won pays half again
    /// (never past the floor's ×3).</summary>
    public sealed class Spectacle : IFloorEvent
    {
        public const int WinPercent = 150;

        public string Id => FloorEventIds.Spectacle;
        public bool CanAppear(ChapterRun run) => run.Rules.BossId == DealerRoster.BelialId;
        public void Accept(ChapterRun run) => run.AddTableMarks(new TableMarks(hidesAll: true, winPercent: WinPercent));
    }

    /// <summary>The False Coin (Belial's chapter): coins now; Belial's bar starts longer.</summary>
    public sealed class FalseCoin : IFloorEvent
    {
        public const int Coins = 40;
        public const int Years = 150;

        public string Id => FloorEventIds.FalseCoin;
        public bool CanAppear(ChapterRun run) => run.Rules.BossId == DealerRoster.BelialId;

        public void Accept(ChapterRun run)
        {
            run.Purse.Add(Coins);
            run.OweAtGate(Years);
        }
    }

    /// <summary>The Night Bargain (Lilith's chapter): coins now; Lilith's bar starts longer.</summary>
    public sealed class NightBargain : IFloorEvent
    {
        public const int Coins = 50;
        public const int Years = 200;

        public string Id => FloorEventIds.NightBargain;
        public bool CanAppear(ChapterRun run) => run.Rules.BossId == DealerRoster.LilithId;

        public void Accept(ChapterRun run)
        {
            run.Purse.Add(Coins);
            run.OweAtGate(Years);
        }
    }

    /// <summary>Desire (Lilith's chapter): one of the relics carried counts twice for the rest of the chapter — its gift and its curse.
    /// Never offered to a run without a relic. The relic is <see cref="RelicId"/> (the first carried when none is named).</summary>
    public sealed class Desire : IFloorEvent
    {
        public string Id => FloorEventIds.Desire;

        /// <summary>The relic the player chose (set before <see cref="Accept"/>).</summary>
        public string RelicId { get; set; }

        public bool CanAppear(ChapterRun run) => run.Rules.BossId == DealerRoster.LilithId && run.Effects.Relics.Count > 0;

        public void Accept(ChapterRun run)
        {
            string id = RelicId != null && run.Effects.Relics.Contains(RelicId) ? RelicId : run.Effects.Relics.FirstOrDefault();
            if (id != null) run.Effects.Amplify(id);
        }
    }

    /// <summary>Insomnia (Lilith's chapter): the next table's first hands come without an ante — and without a raise.</summary>
    public sealed class Insomnia : IFloorEvent
    {
        public const int Hands = 3;

        public string Id => FloorEventIds.Insomnia;
        public bool CanAppear(ChapterRun run) => run.Rules.BossId == DealerRoster.LilithId;
        public void Accept(ChapterRun run) => run.AddTableMarks(new TableMarks(freeHands: Hands));
    }

    public static class FloorEventDeck
    {
        /// <summary>Every offer (each says where it may appear).</summary>
        public static IReadOnlyList<IFloorEvent> Standard => new IFloorEvent[]
        {
            new PurgatoryUsurer(), new MammonsLedger(), new GamblerGhost(), new LyingWitness(), new Spectacle(), new FalseCoin(),
            new NightBargain(), new Desire(), new Insomnia()
        };

        /// <summary>The offers of a chapter: the Usurer and the Gambler everywhere, and the chapter's own three.</summary>
        public static IReadOnlyList<IFloorEvent> For(ChapterRules rules)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            return Standard.ToArray();
        }

        /// <summary>An offer by id (a saved run's pending offer); null when unknown.</summary>
        public static IFloorEvent Find(string id) => Standard.FirstOrDefault(e => e.Id == id);
    }
}
