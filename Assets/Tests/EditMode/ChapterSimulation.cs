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

            /// <summary>The Devil's Compass's steps (2.1.1).</summary>
            public int CompassSteps;

            /// <summary>Floor hands lost to the player's two jokers, won by the house's.</summary>
            public int PlayerBustHands, HouseBustHands;
            public readonly List<int> Gate = new List<int>(), TableLengths = new List<int>(), WardenLengths = new List<int>();
            public readonly Dictionary<string, int> Offered = new Dictionary<string, int>(), Accepted = new Dictionary<string, int>();
            public readonly Dictionary<FireChoice, int> Fire = new Dictionary<FireChoice, int>();
            public readonly Dictionary<NodeKind, int> Path = new Dictionary<NodeKind, int>();

            /// <summary>At the camps: the relics worn and in the bag (summed), and the camps.</summary>
            public int Worn, Bagged, Camps;

            /// <summary>The black market's vendors: visits and things bought.</summary>
            public readonly Dictionary<string, int> VendorVisits = new Dictionary<string, int>(), VendorBuys = new Dictionary<string, int>();

            /// <summary>What the unknown nodes stepped on held.</summary>
            public readonly Dictionary<NodeKind, int> Unknowns = new Dictionary<NodeKind, int>();

            /// <summary>Demo 2.1's offers: the choices made ("id/choice"), and the coins and years each offer moved in all.</summary>
            public readonly Dictionary<string, int> Choices = new Dictionary<string, int>(), EventCoins = new Dictionary<string, int>(),
                EventYears = new Dictionary<string, int>();

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
                EventsSeen += o.EventsSeen; Treasures += o.Treasures; CompassSteps += o.CompassSteps;
                PlayerBustHands += o.PlayerBustHands; HouseBustHands += o.HouseBustHands;
                Gate.AddRange(o.Gate);
                TableLengths.AddRange(o.TableLengths);
                WardenLengths.AddRange(o.WardenLengths);
                foreach (var p in o.Offered) Bump(Offered, p.Key, p.Value);
                foreach (var p in o.Accepted) Bump(Accepted, p.Key, p.Value);
                foreach (var p in o.Fire) Bump(Fire, p.Key, p.Value);
                foreach (var p in o.Path) Bump(Path, p.Key, p.Value);
                foreach (var p in o.Unknowns) Bump(Unknowns, p.Key, p.Value);
                Worn += o.Worn; Bagged += o.Bagged; Camps += o.Camps;
                foreach (var p in o.Choices) Bump(Choices, p.Key, p.Value);
                foreach (var p in o.VendorVisits) Bump(VendorVisits, p.Key, p.Value);
                foreach (var p in o.VendorBuys) Bump(VendorBuys, p.Key, p.Value);
                foreach (var p in o.EventCoins) Bump(EventCoins, p.Key, p.Value);
                foreach (var p in o.EventYears) Bump(EventYears, p.Key, p.Value);
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
                // A side table of an offer (the pickpocket's chase, the gambler's hand) may have emptied the purse: the run is over.
                if (run.PurseEmptied)
                {
                    tally.Broke++;
                    tally.BrokeAtImp++;
                    tally.BrokeFloors += (run.Current?.Floor ?? 0) + 1;
                    return false;
                }
                // The Jester's twenty jokers after the first: a relic while there is room, else the coins.
                while (run.Sinner.JackpotRewardsWaiting > 0)
                {
                    if (!run.RelicsFull) run.TakeJackpotRelic();
                    else run.TakeJackpotCoins();
                    if (run.WardenRelicWaiting != null) run.TakeWardenCoins();
                }
                UseCompass(run, wardenSeen, tally);
                MapNode next = ChooseNext(run, random, wardenSeen);
                run.MoveTo(next);
                Bump(tally.Path, next.Kind);
                if (next.Kind == NodeKind.Unknown) Bump(tally.Unknowns, next.Hidden);
                switch (ChapterRun.KindOf(next))
                {
                    case NodeKind.Table:
                    case NodeKind.Warden:
                        wardenSeen |= ChapterRun.KindOf(next) == NodeKind.Warden;
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
                        // Two relics freely (Demo 2.0's market sold no more); past them only with the gate's tribute still in the purse.
                        int held = effects.CarriedOffered + effects.Bag.Count;
                        int keep = held < 2 ? Reserve : Math.Max(Reserve, run.Tribute);
                        RelicOffer? best = market.Relics.Where(o => market.CanBuyRelic(o.RelicId) && run.Purse.Coins - o.Price >= keep)
                            .OrderByDescending(o => o.Price).Select(o => (RelicOffer?)o).FirstOrDefault();
                        if (best.HasValue && market.BuyRelic(best.Value.RelicId))
                        {
                            Bump(tally.RelicPicks, best.Value.RelicId);
                            boughtRelic = true;
                            tally.RelicsBought++;
                        }
                        if (market.CanBuyImpsEye && run.Purse.Coins - market.EyePrice >= run.Tribute && market.BuyImpsEye())
                            tally.EyesBought++;
                        ShopVendor(run, market);
                        if (market.Purchases > 0) tally.MarketShopped++;
                        Bump(tally.VendorVisits, market.Vendor.ToString());
                        Bump(tally.VendorBuys, market.Vendor.ToString(), market.Purchases);
                        RunVendors.Add((market.Vendor.ToString(), market.Purchases > 0));
                        break;

                    case NodeKind.Event:
                        IFloorEvent offer = run.DrawEvent();
                        if (offer == null) break;
                        tally.EventsSeen++;
                        Bump(tally.Offered, offer.Id);
                        if (offer is IChoiceEvent choiceEvent)
                        {
                            PlayChoiceEvent(run, choiceEvent, payouts, tally);
                            break;
                        }
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
                        ArrangeRelics(effects);
                        FireChoice choice = CampChoice(run);
                        string target = choice == FireChoice.SilenceCurse ? effects.Relics.OrderBy(r => RelicRoster.Find(r)?.ExpectedYears ?? 0).First()
                            : choice == FireChoice.Forge ? run.Forgeable.OrderByDescending(r => RelicRoster.Find(r)?.ExpectedYears ?? 0).First() : null;
                        run.TendFire(choice, target);
                        if (choice == FireChoice.WhetSoul && run.FireUpgrade != null) Bump(tally.UpgradePicks, run.FireUpgrade);
                        Bump(tally.Fire, choice);
                        tally.Worn += effects.CarriedOffered;
                        tally.Bagged += effects.Bag.Count;
                        tally.Camps++;
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

        /// <summary>
        /// Demo 2.1's offers as the simulation's player takes them: the choice worth the most (passing is worth nothing), the trap seen only
        /// by the Warlock — and by the Summoner, whose imp looks a swindler over for its coins. A swap takes the better of the two; a chase is
        /// played out.
        /// </summary>
        private static void PlayChoiceEvent(ChapterRun run, IChoiceEvent offer, FloorPayoutTable payouts, Tally tally)
        {
            bool sees = ChoiceEventDeck.SeesTraps(run);
            if (!sees && offer.Family == EventFamily.Scam && run.Sinner.Id == Summoner.ClassId && run.Purse.Coins > Reserve &&
                run.Purse.TrySpend(ChapterPresenterEyePrice)) sees = true;
            int coins = run.Purse.Coins, years = run.Sentence?.Years ?? 0;
            bool taken = false;
            for (int step = 0; step < 3; step++)
            {
                var choices = offer.Choices(run).Where(c => offer.CanChoose(run, c)).ToList();
                if (choices.Count == 0) break;
                string best = choices.OrderByDescending(c => offer.Worth(run, c, sees) - (offer.Cost(run, c) > 0 && run.Purse.Coins - offer.Cost(run, c) < Reserve ? 1000 : 0))
                    .ThenBy(c => c == EventChoices.Pass ? 0 : 1).First();
                EventOutcome outcome = offer.Choose(run, best);
                Bump(tally.Choices, offer.Id + "/" + best.Split(':')[0]);
                taken |= best != EventChoices.Pass;
                if (outcome.OpensTable && run.PendingChase != null)
                {
                    FloorTable chase = run.PendingChase;
                    PlayMatch(chase, payouts, null);
                    run.FinishTable(chase);
                }
                if (run.WardenRelicWaiting != null) run.TakeWardenCoins();
                if (!outcome.Waits) break;
            }
            Bump(tally.EventCoins, offer.Id, run.Purse.Coins - coins);
            Bump(tally.EventYears, offer.Id, (run.Sentence?.Years ?? 0) - years);
            RunSeen.Add(offer.Id);
            if (taken)
            {
                Bump(tally.Accepted, offer.Id);
                RunTaken.Add(offer.Id);
            }
        }

        /// <summary>
        /// A vendor's own wares as the simulation's player takes them, never below the gate's tribute: the pawnshop pawns the soul when the
        /// purse is behind the tribute; the alchemist burns the worst curse; the gambler's box is bought and opened; the tailor's best
        /// upgrade is bought. No haggling, no new stall (both are even bets).
        /// </summary>
        private static void ShopVendor(ChapterRun run, BlackMarket market)
        {
            int keep = Math.Max(Reserve, run.Tribute);
            switch (market.Vendor)
            {
                case MarketVendor.Pawnshop:
                    if (run.Purse.Coins < run.Tribute) market.Pawn();
                    break;
                case MarketVendor.Alchemist:
                    string worst = run.Effects.AllHeld.Where(market.CanCleanse).OrderBy(id => RelicRoster.Find(id)?.ExpectedYears ?? 0).FirstOrDefault();
                    if (worst != null && run.Purse.Coins - market.CleanseCost >= keep) market.Cleanse(worst);
                    break;
                case MarketVendor.GamblersStall:
                    if (market.CanBuyBox && run.Purse.Coins - market.BoxCost >= keep && market.BuyBox())
                    {
                        market.OpenBox();
                        if (run.WardenRelicWaiting != null) run.TakeWardenCoins();
                    }
                    break;
                case MarketVendor.Tailor:
                    string upgrade = market.Upgrades.FirstOrDefault();
                    if (upgrade != null && run.Purse.Coins - market.UpgradeCost >= keep) market.BuyUpgrade(upgrade);
                    break;
            }
        }

        /// <summary>The vendors met in the whole run being played, and whether anything was bought of them.</summary>
        private static readonly List<(string vendor, bool bought)> RunVendors = new List<(string, bool)>();

        /// <summary>Demo 2.1's offers seen in the whole run being played.</summary>
        private static readonly List<string> RunSeen = new List<string>();

        /// <summary>Demo 2.1's offers taken (not passed) in the whole run being played, and whether it walked free.</summary>
        private static readonly List<string> RunTaken = new List<string>();
        private static bool RunFreed;

        /// <summary>The Summoner's eye on a swindler (the presentation's price).</summary>
        private const int ChapterPresenterEyePrice = 5;

        /// <summary>A camp's action as the simulation's player takes it: a rest while the sentence is high (at least nine tenths of its
        /// start; HELLPOKER_REST_AT), else a forge for a good worn relic, else (before Lilith) a whetted soul, else a rite (a curse silenced
        /// when a relic is worn, else the first cheat broken), else a rest.</summary>
        private static FireChoice CampChoice(ChapterRun run)
        {
            int start = RunSentence.StartFor(run.Sinner.Id, run.Rules.Circle);
            int restAt = EnvInt("HELLPOKER_REST_AT", 90);
            if (run.Sentence != null && run.Sentence.Years * 100L >= start * (long)restAt && run.CanTend(FireChoice.RestByTheFire)) return FireChoice.RestByTheFire;
            if (run.CanTend(FireChoice.Forge) && run.Forgeable.Any(r => (RelicRoster.Find(r)?.ExpectedYears ?? 0) > 0)) return FireChoice.Forge;
            if (run.Rules.Number < 3 && EnvInt("HELLPOKER_WHET", 1) != 0 && run.CanTend(FireChoice.WhetSoul)) return FireChoice.WhetSoul;
            if (run.CanTend(FireChoice.SilenceCurse)) return FireChoice.SilenceCurse;
            if (run.CanTend(FireChoice.BreakFirstCheat)) return FireChoice.BreakFirstCheat;
            foreach (FireChoice c in run.FireOffers)
                if (run.CanTend(c) && c != FireChoice.SilenceCurse && c != FireChoice.Forge) return c;
            return FireChoice.RestByTheFire;
        }

        /// <summary>At a camp the player wears the five best relics held (by what they are worth on average).</summary>
        private static void ArrangeRelics(RunEffects effects)
        {
            int Worth(string id) => RelicRoster.Find(id)?.ExpectedYears ?? 0;
            for (int guard = 0; guard < 10; guard++)
            {
                string best = effects.Bag.OrderByDescending(Worth).FirstOrDefault();
                if (best == null) return;
                if (!effects.RelicSlotsFull)
                {
                    effects.Equip(best);
                    continue;
                }
                string worst = effects.Relics.Where(r => !RelicRoster.IsReward(r)).OrderBy(Worth).FirstOrDefault();
                if (worst == null || Worth(worst) >= Worth(best)) return;
                effects.Swap(worst, best);
            }
        }

        // ------------------------------------------------------------------ a whole run: the three chapters, then Lucifer

        private sealed class RunTally
        {
            public int Runs, ReachedLucifer, Freed, CastDown, Jackpots;

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

            /// <summary>Demo 2.1's offers: the runs that took one, and those of them freed.</summary>
            public readonly Dictionary<string, int> EventRuns = new Dictionary<string, int>(), EventFreed = new Dictionary<string, int>();

            /// <summary>... and the runs that saw one and passed it (the fair comparison: both got that far).</summary>
            public readonly Dictionary<string, int> EventPassedRuns = new Dictionary<string, int>(), EventPassedFreed = new Dictionary<string, int>();

            /// <summary>The runs that began the second chapter, and those of them freed; by a relic or an upgrade held as it began.</summary>
            public int ReachedCh2, FreedFromCh2;
            public readonly Dictionary<string, int> HeldAtCh2 = new Dictionary<string, int>(), FreedHeldAtCh2 = new Dictionary<string, int>();

            /// <summary>The class mechanics used, over all runs.</summary>
            public long Kept, Taken, Harvests, Rearranges, Reveals, FreeRaises, Seizures, TaxesEarned, TaxesLost, Summons, WildImps, PowerUses;

            public void AddKit(ClassKit kit)
            {
                Kept += kit.Kept; Taken += kit.TakenFromSatchel; Harvests += kit.Harvests; Rearranges += kit.Rearranges; Reveals += kit.Reveals;
                FreeRaises += kit.FreeRaises; Seizures += kit.Seizures; TaxesEarned += kit.TaxesEarned; TaxesLost += kit.TaxesLost;
                Summons += kit.Summons; WildImps += kit.WildImps; PowerUses += kit.PowerUses;
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
                ChapterTuning.MapVersion = EnvInt("HELLPOKER_MAP", 0);   // 1: Demo 2.0's map
                int[] losses = EnvInts("HELLPOKER_BOSS_LOSS");   // "250,220,110,90": Mammon, Belial, Lilith, Lucifer
                for (int i = 0; losses != null && i < losses.Length && i < 4; i++) ChapterTuning.BossLossPercent[i + 1] = losses[i];
                if (EnvInt("HELLPOKER_SATCHEL_RISK", 0) > 0) ClassKit.SatchelReRaisePercent = EnvInt("HELLPOKER_SATCHEL_RISK", 0);
                // 2.1.1: the demons' fixed stakes (0: measured against the bar left) and the K powers once a hand (0: the full gauge).
                ChapterTuning.FixedBossStakes = EnvInt("HELLPOKER_FIXED_STAKES", 1) != 0;
                ChapterTuning.PowersEveryHand = EnvInt("HELLPOKER_POWER_HAND", 1) != 0;
                WholeRunReport();
            }
            finally
            {
                ChapterTuning.Reset();
                ClassKit.SatchelReRaisePercent = satchelRisk;
            }
        }

        /// <summary>
        /// Demo 2.2: the Oracle's numbers tried side by side in one run of Unity. HELLPOKER_ORACLE_GRID = "foresight,bar%,coins,knows%;…"
        /// (the cards seen, BEND FATE's price at a demon's and on the floors, how often the demon knows a card); freed per setting.
        /// </summary>
        [Test, Timeout(3600000)]
        public void OracleTuning()
        {
            string grid = Environment.GetEnvironmentVariable("HELLPOKER_ORACLE_GRID") ?? "2,1,2,100;3,1,2,100;2,0,0,100;2,1,2,50";
            int runs = EnvInt("HELLPOKER_RUNS", 500);
            int cards = ClassKit.ForesightCards, bar = ProphecyPowers.RearrangeBarPercent, coins = ProphecyPowers.RearrangeCoins;
            var report = new StringBuilder($"The Oracle, {runs} whole runs a setting (circle 0)\nforesight bar% coins knows%   reached Lucifer   freed\n");
            try
            {
                foreach (string setting in grid.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int[] n = setting.Split(',').Select(s => int.Parse(s.Trim())).ToArray();
                    ChapterTuning.Reset();
                    ClassKit.ForesightCards = n[0];
                    ProphecyPowers.RearrangeBarPercent = n[1];
                    ProphecyPowers.RearrangeCoins = n[2];
                    ChapterTuning.DemonKnowsPercent = n.Length > 3 ? n[3] : 100;
                    var tally = new RunTally();
                    var all = new RunTally();
                    for (int run = 0; run < runs; run++) PlayWholeRun(SinnerRoster.Warlock, run * 7919 + 29, tally, all);
                    report.AppendLine($"{n[0],9} {n[1],4} {n[2],5} {(n.Length > 3 ? n[3] : 100),6}   {Pct(tally.ReachedLucifer, tally.Runs),15}  {Pct(tally.Freed, tally.Runs),6}");
                }
            }
            finally
            {
                ChapterTuning.Reset();
                ClassKit.ForesightCards = cards;
                ProphecyPowers.RearrangeBarPercent = bar;
                ProphecyPowers.RearrangeCoins = coins;
            }
            TestContext.Out.WriteLine(report.ToString());
        }

        /// <summary>Demo 2.2: the Peasant's harvest limited per table. HELLPOKER_HARVEST_GRID = "0;1;2" (0: every hand); freed per setting.</summary>
        [Test, Timeout(3600000)]
        public void PeasantTuning()
        {
            // "harvests a table,re-raise risk %,satchel size;..."
            string grid = Environment.GetEnvironmentVariable("HELLPOKER_HARVEST_GRID") ?? "0,30,3;1,30,3;2,30,3";
            int runs = EnvInt("HELLPOKER_RUNS", 500);
            int keep = SatchelPowers.HarvestsPerTable, risk = ClassKit.SatchelReRaisePercent, size = ClassKit.SatchelSize;
            var report = new StringBuilder($"The Peasant, {runs} whole runs a setting (circle 0)\nharvests,risk,size   reached Lucifer   freed   harvests / taken a run\n");
            try
            {
                foreach (string setting in grid.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    ChapterTuning.Reset();
                    int[] n = setting.Split(',').Select(s => int.Parse(s.Trim())).ToArray();
                    SatchelPowers.HarvestsPerTable = n[0];
                    ClassKit.SatchelReRaisePercent = n.Length > 1 ? n[1] : risk;
                    ClassKit.SatchelSize = n.Length > 2 ? n[2] : size;
                    var tally = new RunTally();
                    var all = new RunTally();
                    for (int run = 0; run < runs; run++) PlayWholeRun(SinnerRoster.Peasant, run * 7919 + 29, tally, all);
                    report.AppendLine($"{setting.Trim(),18}   {Pct(tally.ReachedLucifer, tally.Runs),15}  {Pct(tally.Freed, tally.Runs),6}   " +
                                      $"{Avg((int)tally.Harvests, tally.Runs)} / {Avg((int)tally.Taken, tally.Runs)}");
                }
            }
            finally
            {
                ChapterTuning.Reset();
                SatchelPowers.HarvestsPerTable = keep;
                ClassKit.SatchelReRaisePercent = risk;
                ClassKit.SatchelSize = size;
            }
            TestContext.Out.WriteLine(report.ToString());
        }

        private void WholeRunReport()
        {
            int runs = EnvInt("HELLPOKER_RUNS", 1000);
            int seedShift = EnvInt("HELLPOKER_SEED", 0);
            SinnerClass[] classes = (Environment.GetEnvironmentVariable("HELLPOKER_CLASSES") ?? "peasant,warlock,king,jester,summoner,thief")
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
            report.AppendLine("the way down, a run on average per chapter (the nodes stepped on; ? = unknown, what it held after the arrow)");
            for (int ch = 0; ch < 3; ch++)
            {
                Tally c = all.Chapters[ch];
                report.AppendLine($"  ch{ch + 1}: " + string.Join("  ", c.Path.OrderBy(p => p.Key).Select(p => $"{p.Key} {Avg(p.Value, c.Runs)}")) +
                                  "   ? -> " + string.Join(" ", c.Unknowns.OrderBy(p => p.Key).Select(p => $"{p.Key} {Avg(p.Value, c.Runs)}")));
            }
            report.AppendLine("the camps, all chapters: " + string.Join("  ", Enumerable.Range(0, 3).SelectMany(ch => all.Chapters[ch].Fire)
                .GroupBy(p => p.Key).Select(g => (g.Key, n: g.Sum(p => p.Value))).OrderBy(p => p.Key)
                .Select(p => $"{p.Key} {Pct(p.n, all.Chapters.Sum(c => c.Camps))}")) +
                $";  at a camp worn {Avg(all.Chapters.Sum(c => c.Worn), all.Chapters.Sum(c => c.Camps))}, bag {Avg(all.Chapters.Sum(c => c.Bagged), all.Chapters.Sum(c => c.Camps))}");
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
            foreach (var (cls, t) in perClass.Where(p => p.Item1.StartingJokers > 0))
                report.AppendLine($"{cls.Id}: twenty jokers {Avg(t.Jackpots, t.Runs)} times a run (Phase 2: the count falls to {Jester.PhaseTwoJackpotFallsTo}), " +
                                  $"freed {Pct(t.Freed, t.Runs)}");
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
                                  $"(wild {Avg((int)t.WildImps, t.Runs)});  K used {Avg((int)t.PowerUses, t.Runs)} hands;  " +
                                  $"compass steps {Avg(t.Chapters.Sum(c => c.CompassSteps), t.Runs)}");

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

            report.AppendLine();
            report.AppendLine("Demo 2.1's offers (all classes): offered, the choices, coins / years an offer moved on average, freed when taken vs when " +
                              "seen and passed (both got that far; a gap past 5 points is marked !; an encounter cannot be passed)");
            var everyChapter = new Tally();
            foreach (Tally c in all.Chapters) everyChapter.Add(c);
            foreach (IChoiceEvent e in ChoiceEventDeck.All)
            {
                int offered = everyChapter.Offered.TryGetValue(e.Id, out int o) ? o : 0;
                string choices = string.Join(" ", everyChapter.Choices.Where(p => p.Key.StartsWith(e.Id + "/", StringComparison.Ordinal))
                    .OrderByDescending(p => p.Value).Select(p => $"{p.Key.Substring(e.Id.Length + 1)} {p.Value}"));
                int runsWith = all.EventRuns.TryGetValue(e.Id, out int r) ? r : 0, freedWith = all.EventFreed.TryGetValue(e.Id, out int f) ? f : 0;
                int passed = all.EventPassedRuns.TryGetValue(e.Id, out int pr) ? pr : 0, freedPassed = all.EventPassedFreed.TryGetValue(e.Id, out int pf) ? pf : 0;
                // Within each class (a class that plays it one way and a weaker class the other way would fake a gap), weighted.
                double gapSum = 0, weightSum = 0;
                foreach (var (_, ct) in perClass)
                {
                    int cw = ct.EventRuns.TryGetValue(e.Id, out int a) ? a : 0, cp = ct.EventPassedRuns.TryGetValue(e.Id, out int b) ? b : 0;
                    if (cw < 10 || cp < 10) continue;
                    double g = 100.0 * (ct.EventFreed.TryGetValue(e.Id, out int fa) ? fa : 0) / cw - 100.0 * (ct.EventPassedFreed.TryGetValue(e.Id, out int fb) ? fb : 0) / cp;
                    double weight = Math.Min(cw, cp);
                    gapSum += g * weight;
                    weightSum += weight;
                }
                double gap = weightSum == 0 ? 0 : gapSum / weightSum;
                string mark = weightSum >= 30 && Math.Abs(gap) > 5 ? " !" : "";
                report.AppendLine($"  {e.Id,-19} offered {offered,5}  [{choices}]  coins {Avg(everyChapter.EventCoins.TryGetValue(e.Id, out int ec) ? ec : 0, offered),6}" +
                                  $"  years {Avg(everyChapter.EventYears.TryGetValue(e.Id, out int ey) ? ey : 0, offered),7}  freed taken {Pct(freedWith, runsWith),6}" +
                                  $" / passed {Pct(freedPassed, passed),6} ({(gap >= 0 ? "+" : "")}{gap:0.0}){mark}");
            }

            report.AppendLine();
            report.AppendLine("the black market's vendors (all classes): visits, things bought, freed when something was bought vs met and left (within classes; ! past 5)");
            foreach (string v in Enum.GetNames(typeof(MarketVendor)))
            {
                int visits = everyChapter.VendorVisits.TryGetValue(v, out int vv) ? vv : 0, buys = everyChapter.VendorBuys.TryGetValue(v, out int vb) ? vb : 0;
                double gapSum = 0, weightSum = 0;
                foreach (var (_, ct) in perClass)
                {
                    string key = "vendor:" + v;
                    int cw = ct.EventRuns.TryGetValue(key, out int a) ? a : 0, cp = ct.EventPassedRuns.TryGetValue(key, out int b) ? b : 0;
                    if (cw < 10 || cp < 10) continue;
                    double g = 100.0 * (ct.EventFreed.TryGetValue(key, out int fa) ? fa : 0) / cw - 100.0 * (ct.EventPassedFreed.TryGetValue(key, out int fb) ? fb : 0) / cp;
                    gapSum += g * Math.Min(cw, cp);
                    weightSum += Math.Min(cw, cp);
                }
                double gap = weightSum == 0 ? 0 : gapSum / weightSum;
                report.AppendLine($"  {v,-14} visits {visits,5}  bought {buys,5}  gap {(gap >= 0 ? "+" : "")}{gap:0.0}{(weightSum >= 30 && Math.Abs(gap) > 5 ? " !" : "")}");
            }

            TestContext.WriteLine(report.ToString());
            Assert.Pass(report.ToString());
        }

        private static string Median(List<int> values) => values.Count == 0 ? "-" : values.OrderBy(v => v).ElementAt(values.Count / 2).ToString();

        private static void PlayWholeRun(SinnerClass cls, int seed, RunTally tally, RunTally all)
        {
            foreach (RunTally t in new[] { tally, all }) t.Runs++;
            RunTaken.Clear();
            RunSeen.Clear();
            RunVendors.Clear();
            RunFreed = false;
            ChapterJourney journey = ChapterJourney.Begin(cls, seed, EnvInt("HELLPOKER_CIRCLE", 0));   // a knob: the circle of Hell
            if (EnvInt("HELLPOKER_SLOTS", 0) > 0) journey.Effects.MaxCarried = EnvInt("HELLPOKER_SLOTS", 0);   // a knob: the relic slots
            PlayWholeRun(journey, seed, tally, all);
            foreach (RunTally t in new[] { tally, all })
            {
                t.AddKit(journey.Kit);
                t.Jackpots += journey.Sinner.JokerJackpots;
                foreach (string id in RunTaken.Distinct())
                {
                    Bump(t.EventRuns, id);
                    if (RunFreed) Bump(t.EventFreed, id);
                }
                foreach (string id in RunSeen.Distinct().Except(RunTaken))
                {
                    Bump(t.EventPassedRuns, id);
                    if (RunFreed) Bump(t.EventPassedFreed, id);
                }
                // The vendors go in the same books, "vendor:" before their names: bought of, or met and left.
                foreach (string v in RunVendors.Where(x => x.bought).Select(x => x.vendor).Distinct())
                {
                    Bump(t.EventRuns, "vendor:" + v);
                    if (RunFreed) Bump(t.EventFreed, "vendor:" + v);
                }
                foreach (string v in RunVendors.Select(x => x.vendor).Distinct().Except(RunVendors.Where(x => x.bought).Select(x => x.vendor)))
                {
                    Bump(t.EventPassedRuns, "vendor:" + v);
                    if (RunFreed) Bump(t.EventPassedFreed, "vendor:" + v);
                }
            }
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
                else if (relic != null && !journey.Run.RelicsFull && journey.Run.TakeLoot(relic))
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
                    RunFreed = true;
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

        /// <summary>The player could buy a relic at a market now (and keep what the next table and the gate need).</summary>
        private static bool CanShop(ChapterRun run)
        {
            int cheapest = run.PriceOf(BlackMarket.RelicPrices[0]);
            return run.Purse.Coins - cheapest >= (run.Effects.CarriedOffered + run.Effects.Bag.Count < 2 ? Reserve : Math.Max(Reserve, run.Tribute)) && !run.RelicsFull;
        }

        /// <summary>
        /// The Devil's Compass (2.1.1): a step across the floor when no way from here leads straight to what the player wants next — a warden
        /// (until one is met) or a market it could buy at — and another node of the floor does.
        /// </summary>
        private static void UseCompass(ChapterRun run, bool wardenSeen, Tally tally)
        {
            if (run.CompassStepsLeft <= 0 || run.Current == null) return;
            bool canShop = CanShop(run);
            bool Wants(MapNode n) => (!wardenSeen && n.Kind == NodeKind.Warden) || (canShop && n.Kind == NodeKind.BlackMarket);
            if (run.Choices.Any(Wants)) return;
            MapNode step = run.CompassTargets.FirstOrDefault(t => run.Map.NextFrom(t).Any(Wants));
            if (step != null && run.StepWithCompass(step)) tally.CompassSteps++;
        }

        /// <summary>A warden first (once), then a table, a market it can buy at, an event, any market; ties at random.</summary>
        private static MapNode ChooseNext(ChapterRun run, IRandomSource random, bool wardenSeen)
        {
            bool canShop = CanShop(run);
            int Score(MapNode node)
            {
                switch (node.Kind)
                {
                    case NodeKind.Warden: return wardenSeen ? 2 : 5;
                    case NodeKind.Table: return 4;
                    case NodeKind.BlackMarket: return canShop ? 3 : 1;
                    case NodeKind.Event: return 2;
                    case NodeKind.Unknown: return 2;
                    case NodeKind.Treasure: return 5;   // free coins
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
