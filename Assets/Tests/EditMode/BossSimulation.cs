using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HellPoker.Core.Chapters;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Game;
using HellPoker.Core.Sinners;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// Not a real test: plays Phase 2's demon tables (the health bar) thousands of times for every class × boss, with the balance
    /// simulation's player, and reports the hands a table lasts and how often it damns (Lucifer: casts down) against the designer's
    /// targets. Explicit, so normal runs skip it. Run with:
    ///   -testPlatform EditMode -testFilter HellPoker.Core.Tests.BossSimulation
    /// Knobs: HELLPOKER_BOSS_WIN / HELLPOKER_BOSS_LOSS (the payouts' percents, 100), HELLPOKER_BOSS_EXTRA (years on every bar: the
    /// tribute's, 0), HELLPOKER_CLASSES, HELLPOKER_BOSSES, HELLPOKER_SEED.
    /// </summary>
    [Explicit, Category("Simulation")]
    public class BossSimulation
    {
        private const int Runs = 2000;
        private const int HandsSafety = 2000;

        private sealed class Tally
        {
            public int Runs, Beaten, Damned, CastDown, SoulReached, Unfinished, FreedByDeadMan;
            public readonly List<int> Hands = new List<int>();
        }

        [Test, Timeout(3600000)]
        public void BossTables()
        {
            // -1: each chapter's own percents (ChapterRules.BossWinPercent / BossLossPercent; Lucifer 100 / 100).
            int win = EnvInt("HELLPOKER_BOSS_WIN", -1), loss = EnvInt("HELLPOKER_BOSS_LOSS", -1), extra = EnvInt("HELLPOKER_BOSS_EXTRA", 0);
            int seedShift = EnvInt("HELLPOKER_SEED", 0);
            SinnerClass[] classes = (Environment.GetEnvironmentVariable("HELLPOKER_CLASSES") ?? "peasant,warlock,king,jester")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(id => SinnerRoster.Find(id.Trim())).Where(c => c != null).ToArray();
            string[] bosses = (Environment.GetEnvironmentVariable("HELLPOKER_BOSSES") ?? string.Join(",", BossShares.Bosses))
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(b => b.Trim()).ToArray();

            var report = new StringBuilder();
            string payouts = win < 0 && loss < 0
                ? $"each boss's own (wins / losses: Mammon {ChapterRules.For(1).BossWinPercent}/{ChapterRules.For(1).BossLossPercent}, " +
                  $"Belial {ChapterRules.BelialWinPercent}/{ChapterRules.BelialLossPercent}, Lilith {ChapterRules.LilithWinPercent}/{ChapterRules.LilithLossPercent}, " +
                  $"Lucifer {ChapterRules.LuciferWinPercent}/{ChapterRules.LuciferLossPercent})"
                : $"wins {win}%, losses {loss}% of the demon's own";
            report.AppendLine($"Phase 2 demon tables: {Runs} runs per class × boss. Payouts: {payouts}; " +
                              $"+{extra} years on every bar (the tribute's). Soul line: Mammon 200%, Belial 175%, Lilith 150% of the bar's start " +
                              $"(soul worth {GameRules.Default.SoulWorthYears}); Lucifer: stakes on the bar, cast down past " +
                              $"{EnvInt("HELLPOKER_LUCIFER_GATE", ChapterRules.LuciferCastDownPercent)}% of the start.");
            report.AppendLine("Targets: Mammon 15-25 hands / damned 10-20%; Belial 15-25 / 15-25%; Lilith 20-30 / 20-30%; Lucifer 15-25 / cast down 30-50%.");
            report.AppendLine("Player: the balance simulation's (no relics; the class's power and charge as at any table).");
            report.AppendLine();
            report.AppendLine("class     boss      bar   hands avg (p10/p50/p90)   beaten   damned / cast down   soul reached   Dead Man's Hand");

            var byBoss = bosses.ToDictionary(b => b, b => new Tally());
            foreach (SinnerClass sinnerClass in classes)
            foreach (string boss in bosses)
            {
                var tally = new Tally();
                int bar = BossShares.For(sinnerClass.Id, boss) + extra;
                for (int run = 0; run < Runs; run++)
                    Play(sinnerClass, boss, bar, win, loss, run * 7919 + 17 + seedShift, tally, byBoss[boss]);
                report.AppendLine(Row(sinnerClass.Id, boss, bar, tally));
            }
            report.AppendLine();
            foreach (string boss in bosses)
                report.AppendLine(Row("all", boss, 0, byBoss[boss]));

            TestContext.WriteLine(report.ToString());
            Assert.Pass(report.ToString());
        }

        private static void Play(SinnerClass sinnerClass, string bossId, int bar, int win, int loss, int seed, Tally tally, Tally all)
        {
            var sinner = new Sinner(sinnerClass);
            bool lucifer = bossId == DealerRoster.LuciferId;
            // Lucifer: his stakes measured against the bar (Phase 2's), or — HELLPOKER_LUCIFER_FIXED=1 — the demo's 50 / 150.
            Dealer boss = lucifer ? (EnvInt("HELLPOKER_LUCIFER_FIXED", 0) == 1 ? DealerRoster.Lucifer : BossTable.Lucifer) : DealerRoster.Find(bossId);
            ChapterRules chapter = lucifer ? null : ChapterRules.For(Array.IndexOf(BossShares.Bosses, bossId) + 1);
            int soulLine = chapter?.SoulLinePercent ?? 101;
            if (win < 0) win = chapter?.BossWinPercent ?? ChapterRules.LuciferWinPercent;
            if (loss < 0) loss = chapter?.BossLossPercent ?? ChapterRules.LuciferLossPercent;
            HellPokerGame game = BossTable.Create(GameRules.Default, boss, bar, soulLine, win, loss, seed, sinner, sinner);
            int gate = BossTable.LuciferGate(bar, EnvInt("HELLPOKER_LUCIFER_GATE", ChapterRules.LuciferCastDownPercent));
            int hands = 0;
            bool soul = false, castDown = false;
            while (!BossTable.IsOver(game.Phase))
            {
                if (hands >= HandsSafety) break;
                BalanceSimulation.PlayHand(game, new HouseDrawStrategy(game.Rules.MaxDiscards), boss.Payouts);
                hands++;
                soul |= game.IsSoulAtStake || game.Phase == GamePhase.Damned;
                if (lucifer && game.Phase == GamePhase.RoundOver && game.Years > gate)
                {
                    castDown = true;
                    break;
                }
                if (game.Phase == GamePhase.RoundOver) game.NextRound();
            }
            foreach (Tally t in new[] { tally, all })
            {
                t.Runs++;
                t.Hands.Add(hands);
                if (soul) t.SoulReached++;
                if (castDown) t.CastDown++;
                else if (game.Phase == GamePhase.Damned) t.Damned++;
                else if (game.Phase == GamePhase.Absolved)
                {
                    t.Beaten++;
                    RoundResult last = game.LastRound;
                    if (last?.Showdown != null && last.Showdown.Player.Category == Evaluation.HandCategory.DeadMansHand) t.FreedByDeadMan++;
                }
                else t.Unfinished++;
            }
        }

        private static string Row(string id, string boss, int bar, Tally t)
        {
            var sorted = t.Hands.OrderBy(h => h).ToList();
            int P(int q) => sorted.Count == 0 ? 0 : sorted[Math.Min(sorted.Count - 1, sorted.Count * q / 100)];
            string hands = $"{(sorted.Count == 0 ? 0 : sorted.Average()):0.0} ({P(10)}/{P(50)}/{P(90)})";
            string lost = Pct(t.Damned + t.CastDown, t.Runs) + (t.Unfinished > 0 ? $" (+{t.Unfinished} unfinished)" : "");
            return $"{id,-9} {boss,-8} {(bar > 0 ? bar.ToString() : ""),5}   {hands,-24} {Pct(t.Beaten, t.Runs),7}   {lost,-19}  {Pct(t.SoulReached, t.Runs),12}   " +
                   $"{Pct(t.FreedByDeadMan, t.Runs)}";
        }

        private static string Pct(int count, int of) => of == 0 ? "-" : (100.0 * count / of).ToString("0.0") + "%";

        private static int EnvInt(string name, int fallback) =>
            int.TryParse(Environment.GetEnvironmentVariable(name), out int value) ? value : fallback;
    }
}
