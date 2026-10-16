using System;
using System.Collections.Generic;
using HellPoker.Core.Dealers;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Core.Sinners;

namespace HellPoker.Core.Chapters
{
    /// <summary>
    /// Phase 2's sentence, split among the demons: every boss holds a share of it, and at their table that share is the demon's
    /// health bar. One table for every class × boss (the shares are 20 / 30 / 34 / 16 percent of the class's whole sentence).
    /// </summary>
    public static class BossShares
    {
        /// <summary>The bosses in the order they are met.</summary>
        public static readonly string[] Bosses = { DealerRoster.MammonId, DealerRoster.BelialId, DealerRoster.LilithId, DealerRoster.LuciferId };

        private static readonly Dictionary<string, int[]> Shares = new Dictionary<string, int[]>
        {
            //                                 Mammon Belial Lilith Lucifer
            { Jester.ClassId,  new[] { 1000, 1500, 1700,  800 } },   // 5000
            { Peasant.ClassId, new[] { 1300, 1950, 2200, 1050 } },   // 6500
            { Warlock.ClassId, new[] { 1300, 1950, 2200, 1050 } },   // 6500
            { King.ClassId,    new[] { 1600, 2400, 2700, 1300 } },   // 8000
            { Summoner.ClassId, new[] { 1160, 1740, 1972, 928 } },
            { Thief.ClassId,   new[] { 1300, 1950, 2200, 1050 } },   // 6500 (Demo 2.2: the Peasant's, tuned by the simulation)   // 5800 (6500 and 140 coins asked: 19.6% free; 200 coins: 26.8%)
        };

        /// <summary>The years a sinner of <paramref name="classId"/> owes <paramref name="bossId"/> (any other class: the Peasant's).</summary>
        public static int For(string classId, string bossId)
        {
            int boss = Array.IndexOf(Bosses, bossId);
            if (boss < 0) throw new ArgumentException($"'{bossId}' holds no share of a sentence.", nameof(bossId));
            return Of(classId)[boss];
        }

        /// <summary>The class's whole sentence (the four shares).</summary>
        public static int Total(string classId)
        {
            int total = 0;
            foreach (int share in Of(classId)) total += share;
            return total;
        }

        private static int[] Of(string classId)
        {
            int[] shares = classId != null && Shares.TryGetValue(classId, out int[] own) ? own : Shares[Peasant.ClassId];
            if (classId == null || !ChapterTuning.SentenceTotals.TryGetValue(classId, out int total)) return shares;
            int designed = 0;
            foreach (int share in shares) designed += share;
            var scaled = new int[shares.Length];
            for (int i = 0; i < shares.Length; i++) scaled[i] = (int)((long)shares[i] * total / designed);
            return scaled;
        }
    }

    /// <summary>
    /// A boss's payouts scaled for the bar (the tuning's knob): a won hand takes <see cref="WinScalePercent"/> of what the demon's table
    /// would pay off the bar, a lost one (or a fold) puts <see cref="LossScalePercent"/> of its cost on (rounded up). 100 / 100 is the
    /// demon's own table. The Dead Man's Hand still empties the bar.
    /// </summary>
    public sealed class ScaledPayoutTable : IPayoutTable, IPayoutInfo
    {
        private readonly PayoutTable _source;

        public int WinScalePercent { get; }
        public int LossScalePercent { get; }

        public ScaledPayoutTable(PayoutTable source, int winScalePercent = 100, int lossScalePercent = 100)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            if (winScalePercent <= 0 || lossScalePercent <= 0) throw new ArgumentOutOfRangeException(nameof(winScalePercent));
            WinScalePercent = winScalePercent;
            LossScalePercent = lossScalePercent;
        }

        public bool IsAbsolution(HandCategory category) => _source.IsAbsolution(category);
        public int GetMultiplier(HandCategory category) => _source.GetMultiplier(category);
        public int LossPercent => _source.LossPercent;
        public int FoldPercentBeforeDraw => _source.FoldPercentBeforeDraw;
        public int FoldPercentAfterDraw => _source.FoldPercentAfterDraw;

        public int GetYearsForgiven(HandCategory playerCategory, int stake, int ante, int currentYears) =>
            _source.IsAbsolution(playerCategory) ? currentYears
                : Math.Min(currentYears, Win(_source.GetYearsForgiven(playerCategory, stake, ante, int.MaxValue)));

        public int GetYearsAdded(HandCategory houseCategory, int stake, int ante, int surchargePercent = 100) =>
            Loss(_source.GetYearsAdded(houseCategory, stake, ante, surchargePercent));

        public int GetFoldPenalty(int stake, bool afterDraw, int surchargePercent = 100) => Loss(_source.GetFoldPenalty(stake, afterDraw, surchargePercent));

        public int GetLeastYearsForgiven(int stake, int ante, int currentYears) =>
            Math.Min(currentYears, Win(_source.GetLeastYearsForgiven(stake, ante, int.MaxValue)));

        public int GetLeastYearsAdded(int stake, int ante, int surchargePercent = 100) => Loss(_source.GetLeastYearsAdded(stake, ante, surchargePercent));

        private int Win(int years) => (int)((long)years * WinScalePercent / 100);
        private int Loss(int years) => (int)(((long)years * LossScalePercent + 99) / 100);
    }

    /// <summary>
    /// The numbers of a boss's table in Phase 2: the player's share of the sentence is the demon's bar. Played until the bar is empty
    /// (the demon is beaten: the game's <see cref="GamePhase.Absolved"/>) or the soul burns (<see cref="GamePhase.Damned"/>); no hand
    /// limit, no final stretch (the bar is not a sentence of years left), no Lucifer's gate. The soul line is the bar's start times the
    /// chapter's <see cref="ChapterRules.SoulLinePercent"/>; the soul's worth and its rules are the demo's.
    /// </summary>
    public static class BossTable
    {
        /// <summary>A lost hand at a demon's table heals his bar by this much of its cost (percent, rounded up); the whole cost goes on the
        /// run's sentence (<see cref="RunSentence"/>). The simulation tries others (<see cref="ChapterTuning.BarHealPercent"/>).</summary>
        public const int BarHealPercent = 50;

        /// <summary>
        /// Lucifer at the end of Phase 2: the demo's Morning Star (his temper, his cheats and The Fall, his payouts) but with his stakes
        /// measured against the bar like every other demon's (his fixed 50 / 150 would never empty a bar of a thousand years).
        /// </summary>
        public static Dealer Lucifer
        {
            get
            {
                Dealer l = DealerRoster.Lucifer;
                return new Dealer(l.Id, l.MaxDiscards, l.HouseCardsShown, l.Payouts, l.Betting, l.SoulThreshold, null, isFinalTable: true,
                    l.MaliceMax, l.Cheats, l.BackfirePercent);
            }
        }

        /// <summary>Lucifer's gate: past <see cref="ChapterRules.LuciferCastDownPercent"/> of its start the player is cast down — the run
        /// is over.</summary>
        public static int LuciferGate(int start, int castDownPercent = ChapterRules.LuciferCastDownPercent) =>
            (int)((long)start * castDownPercent / 100);

        /// <summary>The player is cast down from Lucifer's table: the bar climbed past the gate.</summary>
        public static bool CastDown(int bar, int start, int castDownPercent = ChapterRules.LuciferCastDownPercent) =>
            bar > LuciferGate(start, castDownPercent);

        /// <summary>The table's rules for a bar of <paramref name="bar"/> years (the template gives the rest: cheats, deck, events).</summary>
        public static GameRules Rules(GameRules template, Dealer boss, int bar, int soulLinePercent)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            if (boss == null) throw new ArgumentNullException(nameof(boss));
            if (bar <= 0) throw new ArgumentOutOfRangeException(nameof(bar));
            bool final = boss.IsFinalTable;
            int soulLine = final ? Math.Max(boss.SoulThreshold, bar + 1) : Math.Max(bar + 1, (int)((long)bar * soulLinePercent / 100));
            var t = template;
            return new GameRules(bar, soulLine, boss.MaxDiscards, forcedRaiseYears: 0, boss.HouseCardsShown, boss.Stakes ?? t.Stakes,
                t.OpeningCardsShown, t.RaiseUnitsBeforeDraw, t.RaiseUnitsAfterDraw, t.HouseReRaiseUnits, t.SoulWorthYears, t.SoulLossPercent,
                luciferGateYears: 0, t.LuciferCastDownYears, final, t.MalicePerHand, t.MalicePerWin, t.MaliceLowSentenceYears,
                t.MaliceLowSentenceBonus, t.MajorCheatYears, t.MajorCheatPercent, t.GrudgeHands, t.GrudgeMalicePerHand,
                t.EventChancePercent, t.EventCooldownHands, t.ContinuousDeck, t.ShuffleYears, t.ShuffleMinYears);
        }

        /// <summary>The demon's table for a bar: the boss's own temper, cheats and payouts (scaled by the chapter's percents).</summary>
        /// <param name="maliceShort">The demon's gauge is this much smaller (the fifth circle: one), never below one.</param>
        /// <remarks>A loss heals the bar by <see cref="ChapterTuning.BarHealPercent"/> of it (the game's <see cref="HellPokerGame.BarHealPercent"/>).</remarks>
        public static HellPokerGame Create(GameRules template, Dealer boss, int bar, int soulLinePercent, int winPercent, int lossPercent,
            int seed, Cheats.ICheatGuard guard, Sinner sinner, int maliceShort = 0)
        {
            HellPokerGame game = HellPokerGameFactory.Create(Rules(template, boss, bar, soulLinePercent), new ScaledPayoutTable(boss.Payouts, winPercent, lossPercent),
                seed, boss.Betting, boss.Cheats, Math.Max(1, boss.MaliceMax - Math.Max(0, maliceShort)), guard, boss.BackfirePercent, sinner);
            game.BarHealPercent = ChapterTuning.BarHealPercent;
            // 2.1.1: the bets are a share of the bar as the table began, not of what is left (the last tenth no longer drags on).
            if (ChapterTuning.FixedBossStakes) game.FixedStakeYears = bar;
            return game;
        }

        /// <summary>A demon's win percent with the worn relics (the Lead Seal's points off, Mammon's Gold Tooth's points on for every 50 coins).</summary>
        public static int WinWithRelics(int winPercent, Relics.RelicEffects relics, int coins)
        {
            int points = relics.Extra(Relics.RelicExtras.BossWinPoints);
            int tooth = relics.Extra(Relics.RelicExtras.ToothPoints);
            if (tooth > 0) points += Math.Min(ToothMost, Math.Max(0, coins) / 50 * tooth);
            return Math.Max(1, winPercent * (100 + points) / 100);
        }

        /// <summary>Mammon's Gold Tooth: at most this many points.</summary>
        public const int ToothMost = 15;

        /// <summary>
        /// A demon's table takes the worn relics' marks (Demo 2.1): the Blood Pact (the bar never heals; a loss on the sentence a quarter more),
        /// the Lead Seal (the first lost hand spared) and Lucifer's Feather (one lost hand spared, once a run: <see cref="Events.RunEffects.FeatherSpent"/>).
        /// </summary>
        public static void Arm(HellPokerGame game, Events.RunEffects effects)
        {
            if (game == null || effects == null) return;
            Relics.RelicEffects relics = effects.CombinedRelics;
            int cut = relics.Extra(Relics.RelicExtras.BarNeverHeals);   // the Blood Pact: the bar heals by less (points off its percent)
            if (cut > 0) game.BarHealPercent = Math.Max(0, game.BarHealPercent - cut);
            game.SentenceLossPercent = 100 + relics.Extra(Relics.RelicExtras.SentenceLossPoints);
            int seal = relics.Extra(Relics.RelicExtras.BossSpare);
            bool feather = relics.Extra(Relics.RelicExtras.FeatherSpare) > 0 && !effects.FeatherSpent;
            game.SparesLeft = seal + (feather ? 1 : 0);
            if (!feather) return;
            int sealLeft = seal;
            game.LossSpared += () =>
            {
                if (sealLeft > 0) sealLeft--;
                else effects.FeatherSpent = true;
            };
        }

        /// <summary>The demon is beaten (the bar is empty) or the soul burned: the table is over.</summary>
        public static bool IsOver(GamePhase phase) => phase == GamePhase.Absolved || phase == GamePhase.Damned;
    }
}
