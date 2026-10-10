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
            public readonly List<int> Gate = new List<int>(), TableLengths = new List<int>(), WardenLengths = new List<int>();
            public readonly Dictionary<string, int> Offered = new Dictionary<string, int>(), Accepted = new Dictionary<string, int>();
            public readonly Dictionary<FireChoice, int> Fire = new Dictionary<FireChoice, int>();
            public readonly Dictionary<NodeKind, int> Path = new Dictionary<NodeKind, int>();

            public void Add(Tally o)
            {
                Runs += o.Runs; Broke += o.Broke; BrokeAtImp += o.BrokeAtImp; BrokeAtWarden += o.BrokeAtWarden; BrokeFloors += o.BrokeFloors;
                PaidFull += o.PaidFull; CoinsAtGate += o.CoinsAtGate; YearsAtGate += o.YearsAtGate; Reached += o.Reached;
                Buyers += o.Buyers; BuyersShort += o.BuyersShort; BuyersPaidFull += o.BuyersPaidFull;
                ImpHands += o.ImpHands; ImpWins += o.ImpWins; ImpLosses += o.ImpLosses; ImpFolds += o.ImpFolds; ImpPushes += o.ImpPushes;
                ImpHouseFolds += o.ImpHouseFolds;
                WardenHands += o.WardenHands; WardenWins += o.WardenWins; WardenToll += o.WardenToll;
                TablesPlayed += o.TablesPlayed; TablesWon += o.TablesWon; WardensPlayed += o.WardensPlayed; WardensWon += o.WardensWon;
                MarketVisits += o.MarketVisits; MarketShopped += o.MarketShopped; RelicsBought += o.RelicsBought; EyesBought += o.EyesBought;
                EventsSeen += o.EventsSeen; Treasures += o.Treasures;
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
                purse.imp, purse.warden, step, EnvInt("HELLPOKER_CAP", designer.CapAntes), EnvInt("HELLPOKER_MULT", designer.MultiplierCap))
                .WithStartPercent(EnvInt("HELLPOKER_START", 100));
            SinnerClass[] classes = (Environment.GetEnvironmentVariable("HELLPOKER_CLASSES") ?? "peasant,warlock,king,jester")
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
            var run = ChapterRun.Begin(rules, sinner, effects, seed);
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
                        run.FinishTable(table);
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
                            return;
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
                        bool take = offer.Id != FloorEventIds.MammonsLedger && behind;
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
                        FireChoice choice = run.CanTend(FireChoice.SilenceCurse) ? FireChoice.SilenceCurse : FireChoice.BreakFirstCheat;
                        run.TendFire(choice, choice == FireChoice.SilenceCurse ? effects.Relics[0] : null);
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
        }

        /// <summary>The coins the player keeps after a purchase (an empty purse loses the run at the next table).</summary>
        private static readonly int Reserve = EnvInt("HELLPOKER_RESERVE", 30);

        /// <summary>A warden first (once), then a table, a market it can buy at, an event, any market; ties at random.</summary>
        private static MapNode ChooseNext(ChapterRun run, IRandomSource random, bool wardenSeen)
        {
            int cheapest = run.Rules.Price(BlackMarket.RelicPrices[0]);
            bool canShop = run.Purse.Coins - cheapest >= Reserve && run.Effects.CarriedOffered < RelicRoster.MaxCarried;
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
                BalanceSimulation.PlayHand(table.Game, new HouseDrawStrategy(table.Game.Rules.MaxDiscards), payouts, deal: false);
                FloorHand hand = table.Settle();
                if (tally == null) continue;
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
