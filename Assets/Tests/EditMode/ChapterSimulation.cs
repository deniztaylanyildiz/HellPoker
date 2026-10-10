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
    /// Knobs: HELLPOKER_CHAPTER (1), HELLPOKER_CLASSES, HELLPOKER_TABLE_BONUSES ("3,5,8"; the first knob of the tuning),
    /// HELLPOKER_ANTE, HELLPOKER_TRIBUTE, HELLPOKER_IMP ("re-raise%,fold%"), HELLPOKER_SEED.
    /// </summary>
    [Explicit, Category("Simulation")]
    public class ChapterSimulation
    {
        private const int Runs = 2000;

        /// <summary>The designer's numbers to compare against.</summary>
        private const string Targets = "Targets: tribute paid in full 40-60%, coins at the gate 55-75 on average, relic buyers 20-40 coins short, " +
                                       "hands won against an imp ~55%.";

        private sealed class Tally
        {
            public int Runs, PaidFull, CoinsAtGate, YearsAtGate, Debt;
            public int Buyers, BuyersShort, BuyersPaidFull, BuyersCoins;
            public int ImpHands, ImpWins, ImpLosses, ImpFolds, ImpPushes, ImpHouseFolds, ImpCoins;
            public int WardenHands, WardenWins, WardenCoins, WardenToll;
            public int TablesPlayed, TablesWon, WardensPlayed, WardensWon;
            public int MarketVisits, MarketShopped, RelicsBought, EyesBought;
            public int EventsSeen, Treasures;
            public readonly List<int> Gate = new List<int>();
            public readonly Dictionary<string, int> Offered = new Dictionary<string, int>(), Accepted = new Dictionary<string, int>();
            public readonly Dictionary<FireChoice, int> Fire = new Dictionary<FireChoice, int>();
            public readonly Dictionary<NodeKind, int> Path = new Dictionary<NodeKind, int>();

            public void Add(Tally o)
            {
                Runs += o.Runs; PaidFull += o.PaidFull; CoinsAtGate += o.CoinsAtGate; YearsAtGate += o.YearsAtGate; Debt += o.Debt;
                Buyers += o.Buyers; BuyersShort += o.BuyersShort; BuyersPaidFull += o.BuyersPaidFull; BuyersCoins += o.BuyersCoins;
                ImpHands += o.ImpHands; ImpWins += o.ImpWins; ImpLosses += o.ImpLosses; ImpFolds += o.ImpFolds; ImpPushes += o.ImpPushes;
                ImpHouseFolds += o.ImpHouseFolds; ImpCoins += o.ImpCoins;
                WardenHands += o.WardenHands; WardenWins += o.WardenWins; WardenCoins += o.WardenCoins; WardenToll += o.WardenToll;
                TablesPlayed += o.TablesPlayed; TablesWon += o.TablesWon; WardensPlayed += o.WardensPlayed; WardensWon += o.WardensWon;
                MarketVisits += o.MarketVisits; MarketShopped += o.MarketShopped; RelicsBought += o.RelicsBought; EyesBought += o.EyesBought;
                EventsSeen += o.EventsSeen; Treasures += o.Treasures;
                Gate.AddRange(o.Gate);
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
            int[] bonuses = EnvInts("HELLPOKER_TABLE_BONUSES") ?? new[] { 3, 5, 8 };
            int[] imp = EnvInts("HELLPOKER_IMP");
            ChapterRules designer = ChapterRules.For(chapter);
            int[] antes = EnvInts("HELLPOKER_ANTES") ?? new[] { designer.Ante };
            ChapterRules Rules(int tableBonus, int ante) => designer.With(tableBonus, ante, EnvInt("HELLPOKER_TRIBUTE", designer.Tribute),
                imp?[0], imp != null && imp.Length > 1 ? imp[1] : (int?)null);
            SinnerClass[] classes = (Environment.GetEnvironmentVariable("HELLPOKER_CLASSES") ?? "peasant,warlock,king,jester")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(id => SinnerRoster.Find(id.Trim())).Where(c => c != null).ToArray();
            int main = bonuses.Contains(designer.TableBonus) ? designer.TableBonus : bonuses[0];
            int mainAnte = antes.Contains(designer.Ante) ? designer.Ante : antes[0];

            var report = new StringBuilder();
            ChapterRules shown = Rules(main, mainAnte);
            report.AppendLine($"Chapter {chapter} ({shown.BossId}): {Runs} runs per class. Ante {shown.Ante}, tribute {shown.Tribute} " +
                              $"({shown.YearsPerMissingCoin} years a missing coin), start +{shown.StartingCoins}, treasure +{shown.TreasureCoins}, " +
                              $"table {shown.TableHands} hands ({shown.TableWinsNeeded} to win, +{main}), warden {shown.WardenHands} ({shown.WardenWinsNeeded}, " +
                              $"+{shown.WardenBonus} and a relic, toll {shown.WardenTollPercent}% up to {shown.WardenTollMax}; {shown.MaxWardens} wardens, one reachable from every start), cap {shown.CapAntes} antes, ×{shown.MultiplierCap} at most, " +
                              $"imps re-raise {shown.ImpReRaisePercent}% / give up {shown.ImpFoldPercent}%.");
            report.AppendLine(Targets);
            report.AppendLine("Player: the balance simulation's at the tables; on the map it keeps to a way to a warden until it met one, a warden first (once), then a table, a market it can buy at, " +
                              "an event, a market; buys the strongest relic it can afford, the imp's eye only with the tribute still covered; takes " +
                              "the usurer and the gambler only behind the tribute's pace, never Mammon's ledger; a warden's relic with full slots: the coins. " +
                              "The fire's boons are for the demon's table (not simulated): a silenced curse with a relic carried, else the broken cheat.");
            report.AppendLine();

            var byCombo = new Dictionary<(int ante, int bonus), Tally>();
            Tally mainTotal = null;
            var mainByClass = new List<(SinnerClass cls, Tally tally)>();
            foreach (int ante in antes)
            foreach (int bonus in bonuses)
            {
                if (ante != mainAnte && bonus != main) continue;   // the knobs one at a time
                ChapterRules rules = Rules(bonus, ante);
                bool isMain = bonus == main && ante == mainAnte;
                var total = new Tally();
                foreach (SinnerClass sinnerClass in classes)
                {
                    var tally = new Tally();
                    for (int run = 0; run < Runs; run++)
                        PlayChapter(rules, sinnerClass, run * 7919 + 13 + seedShift, tally);
                    total.Add(tally);
                    if (isMain) mainByClass.Add((sinnerClass, tally));
                }
                byCombo[(ante, bonus)] = total;
                if (isMain) mainTotal = total;
            }

            report.AppendLine($"Ante {mainAnte}, table bonus +{main} (the designer's), per class:");
            report.AppendLine("class     paid in full  coins at gate (p10/p50/p90)  years at gate  in debt   relic buyers: share  short  paid full" +
                              "   imp hands won (lost/fold/push; gave up)   coins/imp hand   warden won  tables won");
            foreach (var (cls, t) in mainByClass.Concat(new[] { ((SinnerClass)null, mainTotal) }))
                report.AppendLine(Row(cls?.Id ?? "all", t));

            report.AppendLine();
            report.AppendLine("Knobs compared, one at a time (all classes):");
            report.AppendLine("ante  bonus  paid in full  coins at gate  years at gate  relic buyers short  in debt  wardens per run  imp hands won");
            foreach (var p in byCombo.OrderBy(p => p.Key.ante).ThenBy(p => p.Key.bonus))
            {
                Tally t = p.Value;
                report.AppendLine($"{p.Key.ante,4}  +{p.Key.bonus,-4} {Pct(t.PaidFull, t.Runs),12}  {Avg(t.CoinsAtGate, t.Runs),13}  {Avg(t.YearsAtGate, t.Runs),13}  " +
                                  $"{Avg(t.BuyersShort, t.Buyers),18}  {Pct(t.Debt, t.Runs),7}  {Avg(t.WardensPlayed, t.Runs),15}  {Pct(t.ImpWins, t.ImpHands),13}");
            }

            Tally all = mainTotal;
            report.AppendLine();
            report.AppendLine($"Purgatory fire (+{main}): " + string.Join("  ", Enum.GetValues(typeof(FireChoice)).Cast<FireChoice>()
                .Select(c => $"{c} {Pct(all.Fire.TryGetValue(c, out int n) ? n : 0, all.Runs)}")));
            report.AppendLine($"Black market (+{main}): {Avg(all.MarketVisits, all.Runs)} visits per run, " +
                              $"bought something on {Pct(all.MarketShopped, all.MarketVisits)} of visits; relics {all.RelicsBought}, imp's eyes {all.EyesBought}; " +
                              $"runs with a relic bought {Pct(all.Buyers, all.Runs)}.");
            report.AppendLine($"Path per run (+{main}): " + string.Join("  ", all.Path.OrderBy(p => p.Key).Select(p => $"{p.Key} {Avg(p.Value, all.Runs)}")));
            report.AppendLine($"Events (offered / taken): " + string.Join("  ", all.Offered.OrderBy(p => p.Key)
                .Select(p => $"{p.Key} {p.Value}/{(all.Accepted.TryGetValue(p.Key, out int a) ? a : 0)}")));
            report.AppendLine($"Wardens: {Avg(all.WardensPlayed, all.Runs)} per run (target 0.8-1.0); hands {all.WardenHands}: won {Pct(all.WardenWins, all.WardenHands)}, " +
                              $"coins per hand {Avg(all.WardenCoins, all.WardenHands)}, toll per warden {Avg(all.WardenToll, all.WardensPlayed)} " +
                              $"(target: below the +{shown.WardenBonus} of a match won); matches won {Pct(all.WardensWon, all.WardensPlayed)}.");
            Tally jester = mainByClass.FirstOrDefault(c => c.cls.Id == Jester.ClassId).tally;
            if (jester != null) report.AppendLine($"Jester in debt at the gate: {Pct(jester.Debt, jester.Runs)} (accepted as the risky class; watched).");

            TestContext.WriteLine(report.ToString());
            Assert.Pass(report.ToString());
        }

        private static string Row(string id, Tally t)
        {
            t.Gate.Sort();
            int P(int q) => t.Gate.Count == 0 ? 0 : t.Gate[Math.Min(t.Gate.Count - 1, t.Gate.Count * q / 100)];
            string imp = $"{Pct(t.ImpWins, t.ImpHands)} ({Pct(t.ImpLosses, t.ImpHands)}/{Pct(t.ImpFolds, t.ImpHands)}/{Pct(t.ImpPushes, t.ImpHands)}; " +
                         $"{Pct(t.ImpHouseFolds, t.ImpHands)})";
            return $"{id,-9} {Pct(t.PaidFull, t.Runs),12}  {Avg(t.CoinsAtGate, t.Runs),6} ({P(10)}/{P(50)}/{P(90)}){"",-8}  {Avg(t.YearsAtGate, t.Runs),13}  " +
                   $"{Pct(t.Debt, t.Runs),7}   {Pct(t.Buyers, t.Runs),19}  {Avg(t.BuyersShort, t.Buyers),5}  {Pct(t.BuyersPaidFull, t.Buyers),9}   " +
                   $"{imp,-40} {Avg(t.ImpCoins, t.ImpHands),14}   {Pct(t.WardensWon, t.WardensPlayed),10}  {Pct(t.TablesWon, t.TablesPlayed),10}";
        }

        // ------------------------------------------------------------------ one chapter

        private static void PlayChapter(ChapterRules rules, SinnerClass sinnerClass, int seed, Tally tally)
        {
            var sinner = new Sinner(sinnerClass);
            var effects = new RunEffects();
            var run = new ChapterRun(rules, sinner, effects, sinnerClass.StartingYears, 0, seed);
            var random = new SystemRandomSource(RandomSeeds.Derive(seed, 99));
            var payouts = new FloorPayoutTable(rules.Boss.Payouts, rules.MultiplierCap);
            bool boughtRelic = false;
            bool wardenSeen = false;

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
                            if (table.MatchWon) tally.WardensWon++;
                        }
                        else
                        {
                            tally.TablesPlayed++;
                            if (table.MatchWon) tally.TablesWon++;
                        }
                        break;

                    case NodeKind.Treasure:
                        run.TakeTreasure();
                        tally.Treasures++;
                        break;

                    case NodeKind.BlackMarket:
                        tally.MarketVisits++;
                        BlackMarket market = run.OpenMarket();
                        RelicOffer? best = market.Relics.Where(o => market.CanBuyRelic(o.RelicId)).OrderByDescending(o => o.Price).Select(o => (RelicOffer?)o).FirstOrDefault();
                        if (best.HasValue && market.BuyRelic(best.Value.RelicId))
                        {
                            boughtRelic = true;
                            tally.RelicsBought++;
                        }
                        if (market.CanBuyImpsEye && run.Purse.Coins - market.EyePrice >= rules.Tribute && market.BuyImpsEye())
                            tally.EyesBought++;
                        if (market.Purchases > 0) tally.MarketShopped++;
                        break;

                    case NodeKind.Event:
                        IFloorEvent offer = run.DrawEvent();
                        if (offer == null) break;
                        tally.EventsSeen++;
                        Bump(tally.Offered, offer.Id);
                        bool behind = run.Purse.Coins < rules.Tribute * (next.Floor + 1) / rules.Floors;
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
            tally.Runs++;
            tally.CoinsAtGate += coins;
            tally.Gate.Add(coins);
            tally.YearsAtGate += toll.YearsForMissing + toll.YearsOwed;
            if (toll.PaidInFull) tally.PaidFull++;
            if (coins < 0) tally.Debt++;
            if (boughtRelic)
            {
                tally.Buyers++;
                tally.BuyersCoins += coins;
                tally.BuyersShort += toll.Missing;
                if (toll.PaidInFull) tally.BuyersPaidFull++;
            }
        }

        /// <summary>A warden first (once), then a table, a market it can buy at, an event, any market; ties at random.</summary>
        private static MapNode ChooseNext(ChapterRun run, IRandomSource random, bool wardenSeen)
        {
            int cheapest = run.Rules.Price(BlackMarket.RelicPrices[0]);
            bool canShop = run.Purse.Coins >= cheapest && run.Effects.CarriedOffered < RelicRoster.MaxCarried;
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

        private static void PlayMatch(FloorTable table, FloorPayoutTable payouts, Tally tally)
        {
            while (!table.IsOver)
            {
                table.Deal();
                BalanceSimulation.PlayHand(table.Game, new HouseDrawStrategy(table.Game.Rules.MaxDiscards), payouts, deal: false);
                FloorHand hand = table.Settle();
                if (tally == null) continue;
                if (table.IsWarden)
                {
                    tally.WardenHands++;
                    if (hand.Won) tally.WardenWins++;
                    tally.WardenCoins += hand.Coins;
                    tally.WardenToll += hand.Toll;
                    continue;
                }
                tally.ImpHands++;
                tally.ImpCoins += hand.Coins;
                if (hand.Won) tally.ImpWins++;
                else if (hand.Lost) tally.ImpLosses++;
                else if (hand.Folded) tally.ImpFolds++;
                else tally.ImpPushes++;
                if (hand.HouseFolded) tally.ImpHouseFolds++;
            }
        }

        private static string Pct(int count, int of) => of == 0 ? "-" : (100.0 * count / of).ToString("0.0") + "%";

        private static string Avg(int sum, int of) => of == 0 ? "-" : (sum / (double)of).ToString("0.0");

        private static int EnvInt(string name, int fallback) =>
            int.TryParse(Environment.GetEnvironmentVariable(name), out int value) ? value : fallback;

        private static int[] EnvInts(string name)
        {
            string text = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(text) ? null : text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
        }
    }
}
