using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HellPoker.Core.Chapters;
using HellPoker.Core.Draw;
using HellPoker.Core.Events;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// Not a real test: plays Phase 2's first chapter (the floors, up to Mammon's gate) thousands of times for every class, with
    /// the balance simulation's player at the tables and a simple player on the map, and reports the coin economy against the
    /// designer's targets. Explicit, so normal runs skip it. Run with:
    ///   -testPlatform EditMode -testFilter HellPoker.Core.Tests.ChapterSimulation
    /// The report is in the test's output (results.xml).
    /// Knobs (each a list: the first value is the main one, the others are compared one knob at a time):
    /// HELLPOKER_CHAPTER (1), HELLPOKER_CLASSES, HELLPOKER_PURSES ("30/60,40/80": the imp's / the warden's coins),
    /// HELLPOKER_ANTE_STEPS ("3,2": hands between the ante's steps), HELLPOKER_ANTE, HELLPOKER_TRIBUTE (over the class's start),
    /// HELLPOKER_IMP ("re-raise%,fold%"), HELLPOKER_SEED.
    /// </summary>
    [Explicit, Category("Simulation")]
    public class ChapterSimulation
    {
        private const int Runs = 2000;

        /// <summary>The designer's numbers to compare against.</summary>
        private const string Targets = "Targets: purse emptied (run over) 10-20%, tribute paid in full 40-60%, 6-10 hands a match.";

        private sealed class Tally
        {
            public int Runs, Broke, BrokeAtImp, BrokeAtWarden, BrokeFloors, PaidFull, CoinsAtGate, YearsAtGate, Reached;
            public int Buyers, BuyersShort, BuyersPaidFull;
            public int ImpHands, ImpWins, ImpLosses, ImpFolds, ImpPushes, ImpHouseFolds;
            public int WardenHands, WardenWins, WardenToll;
            public int TablesPlayed, TablesWon, WardensPlayed, WardensWon;
            public int MarketVisits, MarketShopped, RelicsBought, EyesBought;
            public int EventsSeen, Treasures;

            /// <summary>Floor hands lost to the player's two jokers, won by the house's.</summary>
            public int PlayerBustHands, HouseBustHands;
            public readonly List<int> Gate = new List<int>(), TableLengths = new List<int>(), WardenLengths = new List<int>();
            public readonly Dictionary<string, int> Offered = new Dictionary<string, int>(), Accepted = new Dictionary<string, int>();
            public readonly Dictionary<FireChoice, int> Fire = new Dictionary<FireChoice, int>();
            public readonly Dictionary<NodeKind, int> Path = new Dictionary<NodeKind, int>();

            /// <summary>Relics taken (the market, a warden, the spoils) and class upgrades taken (the spoils, the fire), by id.</summary>
            public readonly Dictionary<string, int> RelicPicks = new Dictionary<string, int>(), UpgradePicks = new Dictionary<string, int>();

            public void Add(Tally o)
            {
                foreach (var p in o.RelicPicks) Bump(RelicPicks, p.Key, p.Value);
                foreach (var p in o.UpgradePicks) Bump(UpgradePicks, p.Key, p.Value);
                Runs += o.Runs; Broke += o.Broke; BrokeAtImp += o.BrokeAtImp; BrokeAtWarden += o.BrokeAtWarden; BrokeFloors += o.BrokeFloors;
                PaidFull += o.PaidFull; CoinsAtGate += o.CoinsAtGate; YearsAtGate += o.YearsAtGate; Reached += o.Reached;
                Buyers += o.Buyers; BuyersShort += o.BuyersShort; BuyersPaidFull += o.BuyersPaidFull;
                ImpHands += o.ImpHands; ImpWins += o.ImpWins; ImpLosses += o.ImpLosses; ImpFolds += o.ImpFolds; ImpPushes += o.ImpPushes;
                ImpHouseFolds += o.ImpHouseFolds;
                WardenHands += o.WardenHands; WardenWins += o.WardenWins; WardenToll += o.WardenToll;
                TablesPlayed += o.TablesPlayed; TablesWon += o.TablesWon; WardensPlayed += o.WardensPlayed; WardensWon += o.WardensWon;
                MarketVisits += o.MarketVisits; MarketShopped += o.MarketShopped; RelicsBought += o.RelicsBought; EyesBought += o.EyesBought;
                EventsSeen += o.EventsSeen; Treasures += o.Treasures;
                PlayerBustHands += o.PlayerBustHands; HouseBustHands += o.HouseBustHands;
                Gate.AddRange(o.Gate);
                TableLengths.AddRange(o.TableLengths);
                WardenLengths.AddRange(o.WardenLengths);
                foreach (var p in o.Offered) Bump(Offered, p.Key, p.Value);
                foreach (var p in o.Accepted) Bump(Accepted, p.Key, p.Value);
                foreach (var p in o.Fire) Bump(Fire, p.Key, p.Value);
                foreach (var p in o.Path) Bump(Path, p.Key, p.Value);
            }
        }

        private static void Bump<T>(Dictionary<T, int> counts, T key, int by = 1) => counts[key] = counts.TryGetValue(key, out int n) ? n + by : by;

        [Test, Timeout(3600000)]
        public void ChapterOne()
        {
            int chapter = EnvInt("HELLPOKER_CHAPTER", 1);
            int seedShift = EnvInt("HELLPOKER_SEED", 0);
            int[] imp = EnvInts("HELLPOKER_IMP");
            ChapterRules designer = ChapterRules.For(chapter);
            (int imp, int warden)[] purses = EnvPairs("HELLPOKER_PURSES") ?? new[] { (designer.ImpCoins, designer.WardenCoins) };
            int[] steps = EnvInts("HELLPOKER_ANTE_STEPS") ?? new[] { designer.AnteStepHands };
            ChapterRules Rules((int imp, int warden) purse, int step) => designer.With(EnvInt("HELLPOKER_ANTE", designer.Ante),
                EnvInt("HELLPOKER_TRIBUTE", designer.TributeOverStart), imp?[0], imp != null && imp.Length > 1 ? imp[1] : (int?)null,
                purse.imp, purse.warden, step, EnvInt("HELLPOKER_CAP", designer.CapAntes), EnvInt("HELLPOKER_MULT", designer.MultiplierCap),
                firstImpCoins: EnvInt("HELLPOKER_FIRST_IMP", purse.imp / 2))
                .WithStartPercent(EnvInt("HELLPOKER_START", 100));
            SinnerClass[] classes = (Environment.GetEnvironmentVariable("HELLPOKER_CLASSES") ?? "peasant,warlock,king,jester,summoner")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(id => SinnerRoster.Find(id.Trim())).Where(c => c != null).ToArray();

            var report = new StringBuilder();
            ChapterRules shown = Rules(purses[0], steps[0]);
            report.AppendLine($"Chapter {chapter} ({shown.BossId}): {Runs} runs per class. Ante {shown.Ante}, +{shown.AnteStep} every {shown.AnteStepHands} hands, " +
                              $"cap {shown.CapAntes} antes, ×{shown.MultiplierCap} at most; imp {shown.ImpCoins} coins, warden {shown.WardenCoins} " +
                              $"(toll {shown.WardenTollPercent}% up to {shown.WardenTollMax}, a relic; {shown.MaxWardens} wardens, one reachable from every start); " +
                              $"tribute = start + {shown.TributeOverStart} ({shown.YearsPerMissingCoin} years a missing coin), treasure +{shown.TreasureCoins}; " +
                              $"imps re-raise {shown.ImpReRaisePercent}% / give up {shown.ImpFoldPercent}%. A match lasts until a purse is empty; " +
                              $"the player's empty purse ends the run.");
            report.AppendLine("Starting purse / tribute: " + string.Join(", ", classes.Select(c =>
                $"{c.Id} {shown.StartingCoinsOf(c.Id)} / {shown.TributeFor(c.Id)}")));
            report.AppendLine(Targets);
            report.AppendLine("Player: the balance simulation's at the tables; on the map it keeps to a way to a warden until it met one, a warden first (once), then a table, a market it can buy at, " +
                              "an event, a market; buys the strongest relic it can afford (the market never takes the last coin), the imp's eye only with the tribute still covered; takes " +
                              "the usurer and the gambler only behind the tribute's pace, never Mammon's ledger; a warden's relic with full slots: the coins. " +
                              "The fire's boons are for the demon's table (not simulated): a silenced curse with a relic carried, else the broken cheat.");
            report.AppendLine();

            var combos = new List<((int imp, int warden) purse, int step)>();
            foreach (var purse in purses) combos.Add((purse, steps[0]));
            foreach (int step in steps.Skip(1)) combos.Add((purses[0], step));

            var byCombo = new List<(string name, Tally tally)>();
            Tally mainTotal = null;
            var mainByClass = new List<(SinnerClass cls, Tally tally)>();
            foreach (var combo in combos)
            {
                ChapterRules rules = Rules(combo.purse, combo.step);
                bool isMain = combo.purse == purses[0] && combo.step == steps[0];
                var total = new Tally();
                var perClass = new List<(SinnerClass, Tally)>();
                foreach (SinnerClass sinnerClass in classes)
                {
                    var tally = new Tally();
                    for (int run = 0; run < Runs; run++)
                        PlayChapter(rules, sinnerClass, run * 7919 + 13 + seedShift, tally);
                    total.Add(tally);
                    perClass.Add((sinnerClass, tally));
                    if (isMain) mainByClass.Add((sinnerClass, tally));
                }
                string name = $"imp {combo.purse.imp,3} / warden {combo.purse.warden,3}, ante +1 every {combo.step}";
                byCombo.Add((name + "  [" + string.Join(" ", perClass.Select(p => $"{p.Item1.Id} {Pct(p.Item2.Broke, p.Item2.Runs)}")) + " broke]", total));
                if (isMain) mainTotal = total;
            }

            report.AppendLine("Per class (the main numbers):");
            report.AppendLine("class     purse emptied (imp/warden; avg floor)  paid in full  coins at gate (p10/p50/p90)  years at gate   hands a table (p10/p50/p90)  " +
                              "hands a warden   imp hands won (lost/fold/push; gave up)   tables won  wardens won  relic buyers (short, paid full)");
            foreach (var (cls, t) in mainByClass.Concat(new[] { ((SinnerClass)null, mainTotal) }))
                report.AppendLine(Row(cls?.Id ?? "all", t));

            if (byCombo.Count > 1)
            {
                report.AppendLine();
                report.AppendLine("Knobs compared, one at a time (all classes):");
                report.AppendLine("purses and ante step                            purse emptied  paid in full  coins at gate  hands a table  hands a warden");
                foreach (var (name, t) in byCombo)
                    report.AppendLine($"{name}\n{"",48}{Pct(t.Broke, t.Runs),13}  {Pct(t.PaidFull, t.Runs),12}  {Avg(t.CoinsAtGate, t.Reached),13}  " +
                                      $"{AvgOf(t.TableLengths),13}  {AvgOf(t.WardenLengths),14}");
            }

            Tally all = mainTotal;
            report.AppendLine();
            report.AppendLine("Purgatory fire: " + string.Join("  ", Enum.GetValues(typeof(FireChoice)).Cast<FireChoice>()
                .Select(c => $"{c} {Pct(all.Fire.TryGetValue(c, out int n) ? n : 0, all.Reached)}")) + " (of the runs reaching it)");
            report.AppendLine($"Black market: {Avg(all.MarketVisits, all.Runs)} visits per run, " +
                              $"bought something on {Pct(all.MarketShopped, all.MarketVisits)} of visits; relics {all.RelicsBought}, imp's eyes {all.EyesBought}; " +
                              $"runs with a relic bought {Pct(all.Buyers, all.Runs)}.");
            report.AppendLine("Path per run: " + string.Join("  ", all.Path.OrderBy(p => p.Key).Select(p => $"{p.Key} {Avg(p.Value, all.Runs)}")));
            report.AppendLine("Events (offered / taken): " + string.Join("  ", all.Offered.OrderBy(p => p.Key)
                .Select(p => $"{p.Key} {p.Value}/{(all.Accepted.TryGetValue(p.Key, out int a) ? a : 0)}")));
            report.AppendLine($"Wardens: {Avg(all.WardensPlayed, all.Runs)} per run; hands {all.WardenHands}: won {Pct(all.WardenWins, all.WardenHands)}, " +
                              $"toll per warden {Avg(all.WardenToll, all.WardensPlayed)}; matches won {Pct(all.WardensWon, all.WardensPlayed)}.");

            TestContext.WriteLine(report.ToString());
            Assert.Pass(report.ToString());
        }

        private static string Row(string id, Tally t)
        {
            string broke = $"{Pct(t.Broke, t.Runs)} ({t.BrokeAtImp}/{t.BrokeAtWarden}; {Avg(t.BrokeFloors, t.Broke)})";
            string imp = $"{Pct(t.ImpWins, t.ImpHands)} ({Pct(t.ImpLosses, t.ImpHands)}/{Pct(t.ImpFolds, t.ImpHands)}/{Pct(t.ImpPushes, t.ImpHands)}; " +
                         $"{Pct(t.ImpHouseFolds, t.ImpHands)})";
            string buyers = $"{Pct(t.Buyers, t.Runs)} ({Avg(t.BuyersShort, t.Buyers)}, {Pct(t.BuyersPaidFull, t.Buyers)})";
            return $"{id,-9} {broke,-38} {Pct(t.PaidFull, t.Runs),12}  {Avg(t.CoinsAtGate, t.Reached),6} ({Percentiles(t.Gate)}){"",-6}  " +
                   $"{Avg(t.YearsAtGate, t.Reached),13}   {AvgOf(t.TableLengths),5} ({Percentiles(t.TableLengths)}){"",-10}  {AvgOf(t.WardenLengths),14}   " +
                   $"{imp,-40} {Pct(t.TablesWon, t.TablesPlayed),10}  {Pct(t.WardensWon, t.WardensPlayed),11}  {buyers}";
        }

        private static string Percentiles(List<int> values)
        {
            if (values.Count == 0) return "-";
            var sorted = values.OrderBy(v => v).ToList();
            int P(int q) => sorted[Math.Min(sorted.Count - 1, sorted.Count * q / 100)];
            return $"{P(10)}/{P(50)}/{P(90)}";
        }

        // ------------------------------------------------------------------ one chapter

        private static void PlayChapter(ChapterRules rules, SinnerClass sinnerClass, int seed, Tally tally)
        {
            var sinner = new Sinner(sinnerClass);
            var effects = new RunEffects();
            PlayFloorsAndGate(ChapterRun.Begin(rules, sinner, effects, seed), seed, tally);
        }

        /// <summary>A chapter's floors and its gate; false when the purse went empty on the way.</summary>
        private static bool PlayFloorsAndGate(ChapterRun run, int seed, Tally tally)
        {
            ChapterRules rules = run.Rules;
            RunEffects effects = run.Effects;
            var random = new SystemRandomSource(RandomSeeds.Derive(seed, 99));
            var payouts = new FloorPayoutTable(rules.Boss.Payouts, rules.MultiplierCap);
            int start = run.Purse.Coins;
            bool boughtRelic = false;
            bool wardenSeen = false;
            tally.Runs++;

            while (!run.AtGate)
            {
                MapNode next = ChooseNext(run, random, wardenSeen);
                run.MoveTo(next);
                Bump(tally.Path, next.Kind);
                switch (next.Kind)
                {
                    case NodeKind.Table:
                    case NodeKind.Warden:
                        wardenSeen |= next.Kind == NodeKind.Warden;
                        FloorTable table = run.OpenTable();
                        PlayMatch(table, payouts, tally);
                        int carried = effects.Relics.Count;
                        run.FinishTable(table);
                        if (effects.Relics.Count > carried) Bump(tally.RelicPicks, effects.Relics.Last());
                        if (run.WardenRelicWaiting != null) run.TakeWardenCoins();   // the designer's rule for the simulation: always the coins
                        if (table.IsWarden)
                        {
                            tally.WardensPlayed++;
                            tally.WardenLengths.Add(table.HandsPlayed);
                            if (table.MatchWon) tally.WardensWon++;
                        }
                        else
                        {
                            tally.TablesPlayed++;
                            tally.TableLengths.Add(table.HandsPlayed);
                            if (table.MatchWon) tally.TablesWon++;
                        }
                        if (run.PurseEmptied)
                        {
                            tally.Broke++;
                            if (table.IsWarden) tally.BrokeAtWarden++;
                            else tally.BrokeAtImp++;
                            tally.BrokeFloors += next.Floor + 1;
                            return false;
                        }
                        break;

                    case NodeKind.Treasure:
                        run.TakeTreasure();
                        tally.Treasures++;
                        break;

                    case NodeKind.BlackMarket:
                        tally.MarketVisits++;
                        BlackMarket market = run.OpenMarket();
                        RelicOffer? best = market.Relics.Where(o => market.CanBuyRelic(o.RelicId) && run.Purse.Coins - o.Price >= Reserve)
                            .OrderByDescending(o => o.Price).Select(o => (RelicOffer?)o).FirstOrDefault();
                        if (best.HasValue && market.BuyRelic(best.Value.RelicId))
                        {
                            Bump(tally.RelicPicks, best.Value.RelicId);
                            boughtRelic = true;
                            tally.RelicsBought++;
                        }
                        if (market.CanBuyImpsEye && run.Purse.Coins - market.EyePrice >= run.Tribute && market.BuyImpsEye())
                            tally.EyesBought++;
                        if (market.Purchases > 0) tally.MarketShopped++;
                        break;

                    case NodeKind.Event:
                        IFloorEvent offer = run.DrawEvent();
                        if (offer == null) break;
                        tally.EventsSeen++;
                        Bump(tally.Offered, offer.Id);
                        bool behind = run.Purse.Coins < start + rules.TributeOverStart * (next.Floor + 1) / rules.Floors;
                        // Coins for years only behind the pace; the tables' marks (the witness, the show, insomnia) and desire always.
                        bool forCoins = offer.Id == FloorEventIds.Usurer || offer.Id == FloorEventIds.GamblerGhost || offer.Id == FloorEventIds.FalseCoin
                                        || offer.Id == FloorEventIds.NightBargain;
                        bool take = offer.Id != FloorEventIds.MammonsLedger && (!forCoins || behind);
                        if (!take) break;
                        Bump(tally.Accepted, offer.Id);
                        offer.Accept(run);
                        if (run.PendingGamble != null)
                        {
                            FloorTable gamble = run.PendingGamble;
                            PlayMatch(gamble, payouts, null);
                            run.FinishTable(gamble);
                        }
                        break;

                    case NodeKind.PurgatoryFire:
                        // A curse to silence first; else, before Lilith, a class upgrade for a longer bar (HELLPOKER_WHET=0: never); else a rest.
                        FireChoice choice = run.CanTend(FireChoice.SilenceCurse) ? FireChoice.SilenceCurse
                            : run.Rules.Number < 3 && EnvInt("HELLPOKER_WHET", 1) != 0 && run.CanTend(FireChoice.WhetSoul) ? FireChoice.WhetSoul
                            : FireChoice.RestByTheFire;
                        run.TendFire(choice, choice == FireChoice.SilenceCurse ? effects.Relics[0] : null);
                        if (choice == FireChoice.WhetSoul && run.FireUpgrade != null) Bump(tally.UpgradePicks, run.FireUpgrade);
                        Bump(tally.Fire, choice);
                        break;
                }
            }

            int coins = run.Purse.Coins;
            GateToll toll = run.PayTribute();
            tally.Reached++;
            tally.CoinsAtGate += coins;
            tally.Gate.Add(coins);
            tally.YearsAtGate += toll.YearsForMissing + toll.YearsOwed;
            if (toll.PaidInFull) tally.PaidFull++;
            if (boughtRelic)
            {
                tally.Buyers++;
                tally.BuyersShort += toll.Missing;
                if (toll.PaidInFull) tally.BuyersPaidFull++;
            }
            return true;
        }

        // ------------------------------------------------------------------ a whole run: the three chapters, then Lucifer

        private sealed class RunTally
        {
            public int Runs, ReachedLucifer, Freed, CastDown;

            /// <summary>The years left of the sentence of every run that walked free (its score).</summary>
            public readonly List<int> FreedSentences = new List<int>();
            public readonly Tally[] Chapters = { new Tally(), new Tally(), new Tally() };
            public readonly int[] BossSat = new int[4], BossDamned = new int[4];
            public readonly List<int>[] BossHands = { new List<int>(), new List<int>(), new List<int>(), new List<int>() };

            /// <summary>How often each demon's rule struck, and the hands each played cornered (Mammon, Belial, Lilith, Lucifer).</summary>
            public readonly int[] RuleTriggers = new int[4], Cornered = new int[4];

            /// <summary>Boss hands lost to the player's two jokers, won by the demon's, and all boss hands (Mammon ... Lucifer).</summary>
            public readonly int[] BossPlayerBusts = new int[4], BossHouseBusts = new int[4];

            /// <summary>The jokers in the deck as each chapter (and Lucifer's table) ended, for the runs that got there.</summary>
            public readonly List<int>[] Jokers = { new List<int>(), new List<int>(), new List<int>(), new List<int>() };

            /// <summary>The runs that began the second chapter, and those of them freed; by a relic or an upgrade held as it began.</summary>
            public int ReachedCh2, FreedFromCh2;
            public readonly Dictionary<string, int> HeldAtCh2 = new Dictionary<string, int>(), FreedHeldAtCh2 = new Dictionary<string, int>();

            /// <summary>The class mechanics used, over all runs.</summary>
            public long Kept, Taken, Harvests, Rearranges, Reveals, FreeRaises, Seizures, TaxesEarned, TaxesLost, Summons, WildImps;

            public void AddKit(ClassKit kit)
            {
                Kept += kit.Kept; Taken += kit.TakenFromSatchel; Harvests += kit.Harvests; Rearranges += kit.Rearranges; Reveals += kit.Reveals;
                FreeRaises += kit.FreeRaises; Seizures += kit.Seizures; TaxesEarned += kit.TaxesEarned; TaxesLost += kit.TaxesLost;
                Summons += kit.Summons; WildImps += kit.WildImps;
            }
        }

        /// <summary>
        /// Not a real test either: whole Phase 2 runs (Mammon, Belial, Lilith, Lucifer) for every class, with the designer's numbers, and
        /// per chapter: the purses emptied, the coins at the gate, the tributes paid in full; per demon: the hands and the souls lost;
        /// per class: Lucifer reached and freedom. HELLPOKER_RUNS (1000) runs a class. Run with
        ///   -testPlatform EditMode -testFilter HellPoker.Core.Tests.ChapterSimulation.WholeRun
        /// </summary>
        [Test, Timeout(3600000)]
        public void WholeRun()
        {
            // Knobs (none: the designer's numbers): HELLPOKER_JESTER_PURSE, HELLPOKER_JESTER_TOTAL (the Jester's whole sentence),
            // HELLPOKER_TRIBUTE2 / HELLPOKER_TRIBUTE3 (the tribute over the start), HELLPOKER_SATCHEL_RISK.
            ChapterTuning.Reset();
            int satchelRisk = ClassKit.SatchelReRaisePercent;
            try
            {
                if (EnvInt("HELLPOKER_JESTER_PURSE", 0) > 0) ChapterTuning.StartingCoins[Jester.ClassId] = EnvInt("HELLPOKER_JESTER_PURSE", 0);
                if (EnvInt("HELLPOKER_JESTER_TOTAL", 0) > 0) ChapterTuning.SentenceTotals[Jester.ClassId] = EnvInt("HELLPOKER_JESTER_TOTAL", 0);
                if (EnvInt("HELLPOKER_SUMMONER_PURSE", 0) > 0) ChapterTuning.StartingCoins[Summoner.ClassId] = EnvInt("HELLPOKER_SUMMONER_PURSE", 0);
                if (EnvInt("HELLPOKER_SUMMONER_TOTAL", 0) > 0) ChapterTuning.SentenceTotals[Summoner.ClassId] = EnvInt("HELLPOKER_SUMMONER_TOTAL", 0);
                if (EnvInt("HELLPOKER_TRIBUTE2", 0) > 0) ChapterTuning.TributeOverStart[2] = EnvInt("HELLPOKER_TRIBUTE2", 0);
                if (EnvInt("HELLPOKER_TRIBUTE3", 0) > 0) ChapterTuning.TributeOverStart[3] = EnvInt("HELLPOKER_TRIBUTE3", 0);
                ChapterTuning.CircleWinStepPercent = EnvInt("HELLPOKER_CIRCLE_WIN", Circles.WinStepPercent);
                ChapterTuning.BarHealPercent = EnvInt("HELLPOKER_BAR_HEAL", BossTable.BarHealPercent);
                int[] losses = EnvInts("HELLPOKER_BOSS_LOSS");   // "250,220,110,90": Mammon, Belial, Lilith, Lucifer
                for (int i = 0; losses != null && i < losses.Length && i < 4; i++) ChapterTuning.BossLossPercent[i + 1] = losses[i];
                if (EnvInt("HELLPOKER_SATCHEL_RISK", 0) > 0) ClassKit.SatchelReRaisePercent = EnvInt("HELLPOKER_SATCHEL_RISK", 0);
                WholeRunReport();
            }
            finally
            {
                ChapterTuning.Reset();
                ClassKit.SatchelReRaisePercent = satchelRisk;
            }
        }

        private void WholeRunReport()
        {
            int runs = EnvInt("HELLPOKER_RUNS", 1000);
            int seedShift = EnvInt("HELLPOKER_SEED", 0);
            SinnerClass[] classes = (Environment.GetEnvironmentVariable("HELLPOKER_CLASSES") ?? "peasant,warlock,king,jester,summoner")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(id => SinnerRoster.Find(id.Trim())).Where(c => c != null).ToArray();
            var report = new StringBuilder();
            report.AppendLine($"Phase 2 whole runs: {runs} per class, circle {EnvInt("HELLPOKER_CIRCLE", 0)}. Player: the chapter simulation's on the floors, the balance " +
                              "simulation's at the tables; a demon's spoils: a relic while a slot is free, else the coins.");
            report.AppendLine("Targets: purse emptied 10-25% a chapter; tribute paid in full 40-60%; Mammon 15-25 hands / 10-20% damned, Belial 15-25 / 15-25%, " +
                              "Lilith 20-30 / 20-30%, Lucifer 15-25 / cast down 30-50%.");
            report.AppendLine();
            report.AppendLine("class     ch  reached  purse emptied  coins at gate (p10/p50/p90)  paid full  boss hands (p50)  boss damned");
            var all = new RunTally();
            var perClass = new List<(SinnerClass, RunTally)>();
            foreach (SinnerClass cls in classes)
            {
                var tally = new RunTally();
                for (int run = 0; run < runs; run++) PlayWholeRun(cls, run * 7919 + 29 + seedShift, tally, all);
                perClass.Add((cls, tally));
                for (int ch = 0; ch < 3; ch++)
                {
                    Tally t = tally.Chapters[ch];
                    report.AppendLine($"{cls.Id,-9} {ch + 1,2}  {t.Runs,7}  {Pct(t.Broke, t.Runs),13}  {Avg(t.CoinsAtGate, t.Reached),6} ({Percentiles(t.Gate)})" +
                                      $"{"",-6}  {Pct(t.PaidFull, t.Reached),9}  {AvgOf(tally.BossHands[ch]),5} ({Median(tally.BossHands[ch])})" +
                                      $"{"",-6}  {Pct(tally.BossDamned[ch], tally.BossSat[ch]),10}");
                }
                report.AppendLine($"{cls.Id,-9} lucifer: sat {tally.BossSat[3]}, hands {AvgOf(tally.BossHands[3])} ({Median(tally.BossHands[3])}), " +
                                  $"cast down {Pct(tally.CastDown, tally.BossSat[3])}, soul lost {Pct(tally.BossDamned[3], tally.BossSat[3])}");
            }
            report.AppendLine();
            report.AppendLine($"class     reached Lucifer   freed   sentence left when freed: avg (p10/p50/p90), spotless   (bar heal {ChapterTuning.BarHealPercent}%)");
            foreach (var (cls, t) in perClass.Concat(new[] { ((SinnerClass)null, all) }))
                report.AppendLine($"{cls?.Id ?? "all",-9} {Pct(t.ReachedLucifer, t.Runs),15}  {Pct(t.Freed, t.Runs),6}   {AvgOf(t.FreedSentences),6} " +
                                  $"({Percentiles(t.FreedSentences)}), {Pct(t.FreedSentences.Count(y => y == 0), t.FreedSentences.Count)}");
            report.AppendLine();
            report.AppendLine("demons' rules, a table on average (struck / hands cornered): interest, role swap, temptation, mirror");
            foreach (var (cls, t) in perClass.Concat(new[] { ((SinnerClass)null, all) }))
                report.AppendLine($"{cls?.Id ?? "all",-9} " + string.Join("   ", Enumerable.Range(0, 4).Select(b =>
                    $"{Avg(t.RuleTriggers[b], t.BossSat[b])} / {Avg(t.Cornered[b], t.BossSat[b])}")));
            report.AppendLine();
            report.AppendLine("where runs end (per 100 runs): purse emptied in ch1/ch2/ch3 (avg floor), soul lost at Mammon/Belial/Lilith, cast down, soul at Lucifer");
            foreach (var (cls, t) in perClass)
            {
                string Broke(int ch) => $"{Per100(t.Chapters[ch].Broke, t.Runs)} ({Avg(t.Chapters[ch].BrokeFloors, t.Chapters[ch].Broke)})";
                report.AppendLine($"{cls.Id,-9} purse {Broke(0)} / {Broke(1)} / {Broke(2)};  soul {Per100(t.BossDamned[0], t.Runs)} / " +
                                  $"{Per100(t.BossDamned[1], t.Runs)} / {Per100(t.BossDamned[2], t.Runs)};  cast down {Per100(t.CastDown, t.Runs)}, " +
                                  $"soul at Lucifer {Per100(t.BossDamned[3], t.Runs)};  freed {Per100(t.Freed, t.Runs)}");
            }
            report.AppendLine();
            report.AppendLine("jokers (the Jester's deck) at the end of ch1/ch2/ch3/Lucifer (avg), and hands lost to the player's two jokers / won by the house's " +
                              "two jokers, floors (all chapters) and each boss");
            foreach (var (cls, t) in perClass.Where(p => p.Item1.StartingJokers > 0))
            {
                int floorHands = t.Chapters.Sum(c => c.ImpHands + c.WardenHands), floorPlayer = t.Chapters.Sum(c => c.PlayerBustHands),
                    floorHouse = t.Chapters.Sum(c => c.HouseBustHands);
                int BossHandsAt(int b) => t.BossHands[b].Sum();
                report.AppendLine($"{cls.Id,-9} jokers {AvgOf(t.Jokers[0])} / {AvgOf(t.Jokers[1])} / {AvgOf(t.Jokers[2])} / {AvgOf(t.Jokers[3])};  " +
                                  $"floors {Pct(floorPlayer, floorHands)} / {Pct(floorHouse, floorHands)};  bosses " +
                                  string.Join("  ", Enumerable.Range(0, 4).Select(b => $"{Pct(t.BossPlayerBusts[b], BossHandsAt(b))} / {Pct(t.BossHouseBusts[b], BossHandsAt(b))}")));
            }
            report.AppendLine();
            report.AppendLine("class powers, a run on average: satchel kept / taken / harvests; rearranges; taxes earned / lost, reveals, free raises, seizures");
            foreach (var (cls, t) in perClass.Concat(new[] { ((SinnerClass)null, all) }))
                report.AppendLine($"{cls?.Id ?? "all",-9} {Avg((int)t.Kept, t.Runs)} / {Avg((int)t.Taken, t.Runs)} / {Avg((int)t.Harvests, t.Runs)};  " +
                                  $"{Avg((int)t.Rearranges, t.Runs)};  {Avg((int)t.TaxesEarned, t.Runs)} / {Avg((int)t.TaxesLost, t.Runs)}, " +
                                  $"{Avg((int)t.Reveals, t.Runs)}, {Avg((int)t.FreeRaises, t.Runs)}, {Avg((int)t.Seizures, t.Runs)};  imps {Avg((int)t.Summons, t.Runs)} " +
                                  $"(wild {Avg((int)t.WildImps, t.Runs)})");

            report.AppendLine();
            report.AppendLine("relics and upgrades: taken (market + wardens + spoils; the fire's upgrades), and freedom of the runs that began chapter 2 " +
                              "holding it against those that did not (a gap past 10 points is marked !)");
            foreach (var (cls, t) in perClass.Concat(new[] { ((SinnerClass)null, all) }))
            {
                var picks = new Dictionary<string, int>();
                foreach (Tally c in t.Chapters)
                {
                    foreach (var p in c.RelicPicks) Bump(picks, p.Key, p.Value);
                    foreach (var p in c.UpgradePicks) Bump(picks, p.Key, p.Value);
                }
                report.AppendLine($"{cls?.Id ?? "all"}: chapter 2 begun by {t.ReachedCh2} runs, {Pct(t.FreedFromCh2, t.ReachedCh2)} freed");
                foreach (string id in picks.Keys.Union(t.HeldAtCh2.Keys).OrderByDescending(k => picks.TryGetValue(k, out int n) ? n : 0))
                {
                    int held = t.HeldAtCh2.TryGetValue(id, out int h) ? h : 0, heldFreed = t.FreedHeldAtCh2.TryGetValue(id, out int hf) ? hf : 0;
                    int without = t.ReachedCh2 - held, withoutFreed = t.FreedFromCh2 - heldFreed;
                    double gap = held == 0 || without == 0 ? 0 : 100.0 * heldFreed / held - 100.0 * withoutFreed / without;
                    string mark = held >= 30 && Math.Abs(gap) > 10 ? " !" : "";
                    report.AppendLine($"  {id,-22} taken {(picks.TryGetValue(id, out int n) ? n : 0),5}   held at ch2 {held,5}: freed {Pct(heldFreed, held),6}" +
                                      $" vs {Pct(withoutFreed, without),6} ({(gap >= 0 ? "+" : "")}{gap:0.0}){mark}");
                }
            }

            TestContext.WriteLine(report.ToString());
            Assert.Pass(report.ToString());
        }

        private static string Median(List<int> values) => values.Count == 0 ? "-" : values.OrderBy(v => v).ElementAt(values.Count / 2).ToString();

        private static void PlayWholeRun(SinnerClass cls, int seed, RunTally tally, RunTally all)
        {
            foreach (RunTally t in new[] { tally, all }) t.Runs++;
            ChapterJourney journey = ChapterJourney.Begin(cls, seed, EnvInt("HELLPOKER_CIRCLE", 0));   // a knob: the circle of Hell
            if (EnvInt("HELLPOKER_SLOTS", 0) > 0) journey.Effects.MaxCarried = EnvInt("HELLPOKER_SLOTS", 0);   // a knob: the relic slots
            PlayWholeRun(journey, seed, tally, all);
            foreach (RunTally t in new[] { tally, all }) t.AddKit(journey.Kit);
        }

        private static void PlayWholeRun(ChapterJourney journey, int seed, RunTally tally, RunTally all)
        {
            var heldAtCh2 = new List<string>();
            for (int ch = 1; ; ch++)
            {
                var chapterTally = new Tally();
                bool alive = PlayFloorsAndGate(journey.Run, seed + ch * 101, chapterTally);
                tally.Chapters[ch - 1].Add(chapterTally);
                all.Chapters[ch - 1].Add(chapterTally);
                if (!alive) return;

                HellPokerGame boss = journey.Run.OpenBossTable(GameRules.Default);
                int hands = PlayDemon(boss, null, journey.Run.Rules.Boss.Payouts, out int playerBusts, out int houseBusts);
                foreach (RunTally t in new[] { tally, all })
                {
                    t.BossSat[ch - 1]++;
                    t.BossHands[ch - 1].Add(hands);
                    if (boss.Phase == GamePhase.Damned) t.BossDamned[ch - 1]++;
                    t.RuleTriggers[ch - 1] += journey.Run.Hooks.Rule?.Triggers ?? 0;
                    t.BossPlayerBusts[ch - 1] += playerBusts;
                    t.BossHouseBusts[ch - 1] += houseBusts;
                    t.Jokers[ch - 1].Add(journey.Sinner.Jokers);
                    t.Cornered[ch - 1] += journey.Run.Hooks.Rule?.CorneredHands ?? 0;
                }
                if (boss.Phase != GamePhase.Absolved) return;
                journey.Run.LeaveBossTable(0);
                if (ch == ChapterRules.Chapters) break;
                // The spoils: the class upgrade (a lasting gift, no curse); else a relic while a slot is free; else the coins.
                string relic = journey.Run.LootOffers.OrderByDescending(r => RelicRoster.Find(r)?.ExpectedYears ?? 0).FirstOrDefault();
                string upgrade = journey.Run.LootUpgrade;
                if (upgrade != null && EnvInt("HELLPOKER_LOOT_UPGRADE", 1) != 0 && journey.Run.TakeLootUpgrade())
                    foreach (RunTally t in new[] { tally, all }) Bump(t.Chapters[ch - 1].UpgradePicks, upgrade);
                else if (relic != null && !journey.Run.Effects.RelicSlotsFull && journey.Run.TakeLoot(relic))
                    foreach (RunTally t in new[] { tally, all }) Bump(t.Chapters[ch - 1].RelicPicks, relic);
                else journey.Run.TakeLootCoins();
                // The rule tops every chapter's purse up to the class's start; HELLPOKER_REFILL=0 carries only what is left (the old rule).
                journey.NextChapter(EnvInt("HELLPOKER_REFILL", 1) == 0 ? 0 : -1);
                if (ch == 1) heldAtCh2.AddRange(journey.Effects.Relics.Concat(journey.Kit.Upgrades));
                if (ch == 1)
                    foreach (RunTally t in new[] { tally, all })
                    {
                        t.ReachedCh2++;
                        foreach (string id in journey.Effects.Relics.Concat(journey.Kit.Upgrades)) Bump(t.HeldAtCh2, id);
                    }
            }

            foreach (RunTally t in new[] { tally, all }) t.ReachedLucifer++;
            HellPokerGame lucifer = journey.OpenLuciferTable(GameRules.Default);
            int luciferHands = PlayDemon(lucifer, journey, BossTable.Lucifer.Payouts, out int lPlayerBusts, out int lHouseBusts);
            bool castDown = lucifer.Phase == GamePhase.RoundOver && journey.IsCastDown(lucifer.Years);
            foreach (RunTally t in new[] { tally, all })
            {
                t.BossSat[3]++;
                t.BossHands[3].Add(luciferHands);
                t.RuleTriggers[3] += journey.LuciferHooks.Rule?.Triggers ?? 0;
                t.BossPlayerBusts[3] += lPlayerBusts;
                t.BossHouseBusts[3] += lHouseBusts;
                t.Jokers[3].Add(journey.Sinner.Jokers);
                t.Cornered[3] += journey.LuciferHooks.Rule?.CorneredHands ?? 0;
                if (castDown) t.CastDown++;
                else if (lucifer.Phase == GamePhase.Damned) t.BossDamned[3]++;
                else if (lucifer.Phase == GamePhase.Absolved)
                {
                    t.Freed++;
                    t.FreedSentences.Add(journey.Sentence.Years);
                    t.FreedFromCh2++;
                    foreach (string id in heldAtCh2) Bump(t.FreedHeldAtCh2, id);
                }
            }
        }

        /// <summary>A demon's table played out by the balance simulation's player: until the bar is empty, the soul burns, or (Lucifer)
        /// the player is cast down. Returns the hands.</summary>
        private static int PlayDemon(HellPokerGame game, ChapterJourney lucifer, IPayoutInfo payouts, out int playerBusts, out int houseBusts)
        {
            int hands = 0;
            playerBusts = houseBusts = 0;
            while (!BossTable.IsOver(game.Phase) && hands < 2000)
            {
                BalanceSimulation.PlayHand(game, new HouseDrawStrategy(game.Rules.MaxDiscards), payouts, kit: game.Hooks as ChapterHooks);
                hands++;
                ShowdownResult showdown = game.LastRound?.Showdown;
                if (showdown != null && showdown.PlayerBust) playerBusts++;
                if (showdown != null && showdown.HouseBust) houseBusts++;
                if (lucifer != null && game.Phase == GamePhase.RoundOver && lucifer.IsCastDown(game.Years)) break;
                if (game.Phase == GamePhase.RoundOver) game.NextRound();
            }
            return hands;
        }

        /// <summary>The coins the player keeps after a purchase (an empty purse loses the run at the next table).</summary>
        private static readonly int Reserve = EnvInt("HELLPOKER_RESERVE", 30);

        /// <summary>A warden first (once), then a table, a market it can buy at, an event, any market; ties at random.</summary>
        private static MapNode ChooseNext(ChapterRun run, IRandomSource random, bool wardenSeen)
        {
            int cheapest = run.PriceOf(BlackMarket.RelicPrices[0]);
            bool canShop = run.Purse.Coins - cheapest >= Reserve && run.Effects.CarriedOffered < Math.Min(run.Effects.MaxCarried, BlackMarket.SellsUpTo);
            int Score(MapNode node)
            {
                switch (node.Kind)
                {
                    case NodeKind.Warden: return wardenSeen ? 2 : 5;
                    case NodeKind.Table: return 4;
                    case NodeKind.BlackMarket: return canShop ? 3 : 1;
                    case NodeKind.Event: return 2;
                    default: return 0;
                }
            }
            var options = run.Choices.ToList();
            // Before the first warden: only the ways that still lead to one (the map promises one from every start).
            if (!wardenSeen)
            {
                var toWarden = options.Where(n => LeadsToWarden(run.Map, n)).ToList();
                if (toWarden.Count > 0) options = toWarden;
            }
            int best = options.Max(Score);
            var top = options.Where(n => Score(n) == best).ToList();
            return top[random.Next(top.Count)];
        }

        private static bool LeadsToWarden(ChapterMap map, MapNode from)
        {
            var here = new List<MapNode> { from };
            while (here.Count > 0)
            {
                if (here.Any(n => n.Kind == NodeKind.Warden)) return true;
                here = here.SelectMany(map.NextFrom).Distinct().ToList();
            }
            return false;
        }

        /// <summary>A match never runs past this many hands in the simulation (the growing ante ends one long before).</summary>
        private const int HandsSafety = 500;

        private static void PlayMatch(FloorTable table, FloorPayoutTable payouts, Tally tally)
        {
            while (!table.IsOver)
            {
                if (table.HandsPlayed >= HandsSafety) Assert.Fail($"A match ran past {HandsSafety} hands.");
                table.Deal();
                BalanceSimulation.PlayHand(table.Game, new HouseDrawStrategy(table.Game.Rules.MaxDiscards), payouts, deal: false,
                    kit: table.Game.Hooks as ChapterHooks);
                FloorHand hand = table.Settle();
                if (tally == null) continue;
                ShowdownResult showdown = table.Game.LastRound?.Showdown;
                if (showdown != null && showdown.PlayerBust) tally.PlayerBustHands++;
                if (showdown != null && showdown.HouseBust) tally.HouseBustHands++;
                if (table.IsWarden)
                {
                    tally.WardenHands++;
                    if (hand.Won) tally.WardenWins++;
                    tally.WardenToll += hand.Toll;
                    continue;
                }
                tally.ImpHands++;
                if (hand.Won) tally.ImpWins++;
                else if (hand.Lost) tally.ImpLosses++;
                else if (hand.Folded) tally.ImpFolds++;
                else tally.ImpPushes++;
                if (hand.HouseFolded) tally.ImpHouseFolds++;
            }
        }

        private static string Pct(int count, int of) => of == 0 ? "-" : (100.0 * count / of).ToString("0.0") + "%";

        private static string Per100(int count, int of) => of == 0 ? "-" : (100.0 * count / of).ToString("0.0");

        private static string Avg(int sum, int of) => of == 0 ? "-" : (sum / (double)of).ToString("0.0");

        private static string AvgOf(List<int> values) => values.Count == 0 ? "-" : values.Average().ToString("0.0");

        private static int EnvInt(string name, int fallback) =>
            int.TryParse(Environment.GetEnvironmentVariable(name), out int value) ? value : fallback;

        private static int[] EnvInts(string name)
        {
            string text = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(text) ? null : text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
        }

        /// <summary>"30/60,40/80" → (30, 60), (40, 80).</summary>
        private static (int, int)[] EnvPairs(string name)
        {
            string text = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(text)) return null;
            return text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(p =>
            {
                string[] parts = p.Split('/');
                return (int.Parse(parts[0]), int.Parse(parts[1]));
            }).ToArray();
        }
    }
}
