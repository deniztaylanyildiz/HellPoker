using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Chapters;
using HellPoker.Core.Dealers;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Events;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Phase 2's chapters: the map, the floors' coin matches, the black market, the fire, the offers and the gate.</summary>
    public class ChapterTests
    {
        private const string Royal = "AS KS QS JS 10S";
        private const string HouseNothing = "2D 4H 6S 8C 10D";
        private const string HouseQuads = "9C 9D 9H 9S 2C";
        private const string PlayerNothing = "3D 5C 7H JC KD";

        /// <summary>The whole deck in draw order: <paramref name="first"/> first (the player's five, the House's five, the draws), the rest after.</summary>
        private static IReadOnlyList<Card> DeckStarting(string first)
        {
            var head = TestCards.Cards(first).ToList();
            return head.Concat(Deck.CreateStandardCards().Where(c => !head.Contains(c))).ToList();
        }

        private static Sinner NewPeasant() => new Sinner(SinnerRoster.Find(Sinners.Peasant.ClassId));

        private static ChapterRules Chapter1(int impFoldPercent = 40) =>
            new ChapterRules(1, DealerRoster.MammonId, bossHands: 8, ante: 5, tribute: 70, pricePercent: 100, impReRaisePercent: 10,
                impFoldPercent: impFoldPercent);

        /// <summary>Plays one hand: through the cards turning, no cards thrown, then <paramref name="afterDraw"/> at the first decision past the draw.</summary>
        private static FloorHand PlayOne(FloorTable table, BetAction afterDraw = BetAction.Pass)
        {
            table.Deal();
            HellPokerGame game = table.Game;
            if (game.Phase == GamePhase.PlayerReveal) game.CheckToDraw();
            game.Draw(Array.Empty<int>());
            bool first = true;
            while (game.Phase != GamePhase.RoundOver)
            {
                if (game.Phase == GamePhase.HouseReRaise) game.Bet(BetAction.Call);
                else
                {
                    game.Bet(first && game.CanBet(afterDraw, out _) ? afterDraw : BetAction.Pass);
                    first = false;
                }
            }
            return table.Settle();
        }

        // ------------------------------------------------------------------ the map

        [Test]
        public void TheMapKeepsItsShapeForEverySeed()
        {
            ChapterRules rules = ChapterRules.For(1);
            for (int seed = 0; seed < 300; seed++)
            {
                ChapterMap map = ChapterMap.Generate(rules, new SystemRandomSource(seed));
                Assert.AreEqual(8, map.FloorCount);
                Assert.That(map.Floors.All(f => f.Count == 6));
                Assert.That(map.Floors[0].All(n => n.Kind == NodeKind.Table), "the first floor is all tables");
                Assert.That(map.Floors[4].All(n => n.Kind == NodeKind.Treasure), "the fifth floor is the treasure");
                Assert.That(map.Floors[7].All(n => n.Kind == NodeKind.PurgatoryFire), "the last floor is the fire");
                Assert.That(map.Floors.Take(2).SelectMany(f => f).All(n => n.Kind != NodeKind.BlackMarket), "no market before the third floor");
                Assert.That(map.Floors.Take(3).SelectMany(f => f).All(n => n.Kind != NodeKind.Warden), "no warden before the fourth floor");
                Assert.LessOrEqual(map.Count(NodeKind.Warden), 3, "three wardens at most in the first chapter");
                for (int start = 0; start < 6; start++)
                    Assert.IsTrue(ReachesAWarden(map, start), $"seed {seed}: a warden can be reached from lane {start}");

                for (int f = 0; f < 7; f++)
                for (int l = 0; l < 6; l++)
                {
                    MapNode node = map[f, l];
                    Assert.Contains(l, node.Next.ToList(), "every node leads straight on");
                    Assert.That(node.Next.All(n => Math.Abs(n - l) <= 1));
                    if (l < 5 && node.Next.Contains(l + 1)) Assert.IsFalse(map[f, l + 1].Next.Contains(l), "paths never cross");
                }
                Assert.That(map.Floors[7].All(n => n.Next.Count == 0));
            }
        }

        private static bool ReachesAWarden(ChapterMap map, int start)
        {
            var here = new List<MapNode> { map[0, start] };
            while (here.Count > 0)
            {
                if (here.Any(n => n.Kind == NodeKind.Warden)) return true;
                here = here.SelectMany(map.NextFrom).Distinct().ToList();
            }
            return false;
        }

        [Test]
        public void LaterChaptersKeepTwoWardens()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                ChapterMap map = ChapterMap.Generate(ChapterRules.For(2), new SystemRandomSource(seed));
                Assert.AreEqual(2, map.Count(NodeKind.Warden));
                for (int start = 0; start < 6; start++) Assert.IsTrue(ReachesAWarden(map, start));
            }
        }

        [Test]
        public void TheSameSeedGivesTheSameMap()
        {
            ChapterRules rules = ChapterRules.For(1);
            string Draw(int seed) => string.Join(" ", ChapterMap.Generate(rules, new SystemRandomSource(seed)).Floors
                .SelectMany(f => f).Select(n => n + "->" + string.Join(",", n.Next)));
            Assert.AreEqual(Draw(42), Draw(42));
            Assert.AreNotEqual(Draw(42), Draw(43));
        }

        // ------------------------------------------------------------------ the floors' payouts

        [Test]
        public void AFloorPaysAtMostThreeTimesTheAnteAndNoHandWipesAnythingOut()
        {
            var payouts = new FloorPayoutTable(DealerRoster.Belial.Payouts, 3);
            Assert.AreEqual(1, payouts.GetMultiplier(HandCategory.HighCard));
            Assert.AreEqual(2, payouts.GetMultiplier(HandCategory.OnePair), "Belial's pair pays ×2");
            Assert.AreEqual(3, payouts.GetMultiplier(HandCategory.TwoPair), "Belial's ×3 stays");
            Assert.AreEqual(3, payouts.GetMultiplier(HandCategory.RoyalFlush));
            Assert.AreEqual(3, payouts.GetMultiplier(HandCategory.DeadMansHand));
            Assert.IsFalse(payouts.IsAbsolution(HandCategory.DeadMansHand));
            Assert.AreEqual(10 + 5 * 2, payouts.GetYearsForgiven(HandCategory.DeadMansHand, 10, 5, int.MaxValue));
            Assert.AreEqual(10 + 5 * 2, payouts.GetYearsAdded(HandCategory.RoyalFlush, 10, 5), "a loss is the pot, no surcharge");
            Assert.AreEqual(15, payouts.GetFoldPenalty(15, afterDraw: false), "a fold leaves the stake, nothing more");
            Assert.AreEqual(15, payouts.GetFoldPenalty(15, afterDraw: true));
        }

        // ------------------------------------------------------------------ a floor's match

        [Test]
        public void AWonFloorHandPaysCoinsAndNeverTouchesTheSentence()
        {
            var purse = new CoinPurse(0);
            FloorTable table = FloorTable.Imp(Chapter1(), NewPeasant(), new RunEffects(), purse, 1,
                DeckStarting(Royal + " " + HouseNothing + " AH KC QD"));
            FloorHand hand = PlayOne(table);
            Assert.IsTrue(hand.Won);
            Assert.AreEqual(5 + 5 * 2, hand.Coins, "a royal flush pays the capped ×3 on the ante");
            Assert.AreEqual(15, purse.Coins);
            Assert.AreEqual(1, table.Wins);
        }

        [Test]
        public void ALostHandCanPutThePurseInDebt()
        {
            var purse = new CoinPurse(0);
            FloorTable table = FloorTable.Imp(Chapter1(), NewPeasant(), new RunEffects(), purse, 1,
                DeckStarting(PlayerNothing + " " + HouseQuads + " 2H 3H 4D"));
            FloorHand hand = PlayOne(table);
            Assert.IsTrue(hand.Lost);
            Assert.AreEqual(-(5 + 5 * 2), hand.Coins);
            Assert.IsTrue(purse.InDebt);
            Assert.AreEqual(-15, purse.Coins);
        }

        [Test]
        public void AnImpMayGiveUpAWeakHandFacingARaiseAfterTheDraw()
        {
            var purse = new CoinPurse(0);
            var sinner = NewPeasant();
            FloorTable table = FloorTable.Imp(Chapter1(impFoldPercent: 100), sinner, new RunEffects(), purse, 1,
                DeckStarting(Royal + " " + HouseNothing + " AH KC QD"));
            FloorHand hand = PlayOne(table, afterDraw: BetAction.Raise);
            Assert.IsTrue(hand.HouseFolded);
            Assert.IsTrue(hand.Won);
            Assert.AreEqual(5, hand.Coins, "the stake on the table, one to one; the raise comes back");
            Assert.IsFalse(table.Game.LastRound.Folded);
            Assert.IsNull(table.Game.LastRound.Showdown);
            Assert.AreEqual(1, sinner.Charge, "a house fold charges as a win");
        }

        [Test]
        public void AnImpNeverGivesUpAPair()
        {
            var purse = new CoinPurse(0);
            FloorTable table = FloorTable.Imp(Chapter1(impFoldPercent: 100), NewPeasant(), new RunEffects(), purse, 1,
                DeckStarting(Royal + " 2D 2H 6S 8C 10D AH KC QD"));
            FloorHand hand = PlayOne(table, afterDraw: BetAction.Raise);
            Assert.IsFalse(hand.HouseFolded);
            Assert.IsTrue(hand.Won);
        }

        [Test]
        public void AFreeAnteIsNotLost()
        {
            var purse = new CoinPurse(0);
            FloorTable table = FloorTable.Imp(Chapter1(), NewPeasant(), new RunEffects(), purse, 1,
                DeckStarting(PlayerNothing + " " + HouseQuads + " 2H 3H 4D"), freeFirstAnte: true);
            FloorHand hand = PlayOne(table);
            Assert.AreEqual(-(5 + 5 * 2) + 5, hand.Coins);
        }

        [Test]
        public void TheWardenTakesHisTollFromAWonHand()
        {
            var purse = new CoinPurse(100);
            FloorTable table = FloorTable.Warden(Chapter1(), NewPeasant(), new RunEffects(), purse, 1,
                DeckStarting(Royal + " " + HouseNothing + " AH KC QD"));
            FloorHand hand = PlayOne(table);
            Assert.AreEqual(15, hand.Coins);
            Assert.AreEqual(5, hand.Toll, "a twentieth of the purse after the win (5.75), rounded down");
            Assert.AreEqual(110, purse.Coins);
            Assert.AreEqual(5, table.HandsToPlay);
        }

        [Test]
        public void TheWardensTollIsFiveCoinsAtMost()
        {
            var purse = new CoinPurse(400);
            FloorTable table = FloorTable.Warden(Chapter1(), NewPeasant(), new RunEffects(), purse, 1,
                DeckStarting(Royal + " " + HouseNothing + " AH KC QD"));
            Assert.AreEqual(5, PlayOne(table).Toll);
        }

        [Test]
        public void TheImpsEyeShowsAnImpsCardFromTheDeal()
        {
            FloorTable table = FloorTable.Imp(Chapter1(), NewPeasant(), new RunEffects(), new CoinPurse(), 1, impsEye: true);
            table.Deal();
            Assert.AreEqual(1, table.Game.HouseCardsRevealed);
            FloorTable plain = FloorTable.Imp(Chapter1(), NewPeasant(), new RunEffects(), new CoinPurse(), 1);
            plain.Deal();
            Assert.AreEqual(0, plain.Game.HouseCardsRevealed);
        }

        [Test]
        public void AWardenPlaysOnlyMinorCheats()
        {
            ChapterRules rules = Chapter1();
            for (int seed = 0; seed < 40; seed++)
            {
                FloorTable table = FloorTable.Warden(rules, NewPeasant(), new RunEffects(), new CoinPurse(), seed);
                while (!table.IsOver)
                {
                    table.Deal();
                    HellPokerGame game = table.Game;
                    while (game.Phase != GamePhase.RoundOver)
                    {
                        if (game.Phase == GamePhase.Drawing) game.Draw(Array.Empty<int>());
                        else game.Bet(game.Phase == GamePhase.HouseReRaise ? BetAction.Call : BetAction.Pass);
                    }
                    Assert.That(game.CheatsThisHand.All(r => DealerRoster.MammonCheats.Find(r.CheatId).Tier == Cheats.CheatTier.Minor));
                    table.Settle();
                }
            }
        }

        [Test]
        public void ABetFloorHandChargesTheRunsPower()
        {
            var sinner = NewPeasant();
            FloorTable table = FloorTable.Imp(Chapter1(), sinner, new RunEffects(), new CoinPurse(), 1,
                DeckStarting(PlayerNothing + " " + HouseQuads + " 2H 3H 4D"));
            PlayOne(table);
            Assert.AreEqual(2, sinner.Charge, "a loss +2, as at a demon's table");
        }

        [Test]
        public void TheGamblersHandIsOneHandForHisStake()
        {
            var purse = new CoinPurse(40);
            FloorTable table = FloorTable.Gamble(Chapter1(), 20, NewPeasant(), new RunEffects(), purse, 1,
                DeckStarting(Royal + " " + HouseNothing + " AH KC QD"));
            FloorHand hand = PlayOne(table, afterDraw: BetAction.Raise);
            Assert.IsTrue(table.IsOver);
            Assert.AreEqual(20 * 3, hand.Coins, "nothing to raise; the stake is the ante, ×3");
        }

        // ------------------------------------------------------------------ the chapter

        private static ChapterRun Run(int coins = 0, ChapterRules rules = null, int years = 1000) =>
            new ChapterRun(rules ?? ChapterRules.For(1), NewPeasant(), new RunEffects(), years, coins, 7);

        private static MapNode FirstOf(ChapterRun run, NodeKind kind) => run.Map.Floors.SelectMany(f => f).FirstOrDefault(n => n.Kind == kind);

        /// <summary>Puts the run at <paramref name="node"/> (walking there lane by lane, as far as the map allows).</summary>
        private static void Stand(ChapterRun run, MapNode node)
        {
            typeof(ChapterRun).GetProperty(nameof(ChapterRun.Current)).SetValue(run, node);
        }

        [Test]
        public void AChapterStartsWithItsCoinsOnTopOfWhatIsLeft()
        {
            Assert.AreEqual(20, Run().Purse.Coins);
            Assert.AreEqual(33, Run(13).Purse.Coins);
        }

        [Test]
        public void ThePlayerStartsOnAnyFirstFloorTableAndFollowsThePaths()
        {
            ChapterRun run = Run();
            Assert.AreEqual(6, run.Choices.Count());
            MapNode start = run.Map[0, 2];
            run.MoveTo(start);
            CollectionAssert.AreEquivalent(start.Next, run.Choices.Select(n => n.Lane));
            MapNode far = run.Map.Floors[1].First(n => !start.Next.Contains(n.Lane));
            Assert.Throws<InvalidOperationException>(() => run.MoveTo(far));
        }

        [TestCase(70, 0, 0)]
        [TestCase(90, 0, 20)]
        [TestCase(50, 100, 0)]
        [TestCase(-10, 400, 0)]
        public void AtTheGateEveryMissingCoinIsFiveYears(int coins, int years, int left)
        {
            ChapterRun run = Run();
            run.Purse.Add(coins - run.Purse.Coins);
            GateToll toll = run.PayTribute();
            Assert.AreEqual(years, toll.YearsForMissing);
            Assert.AreEqual(1000 + years, run.Years);
            Assert.AreEqual(left, run.Purse.Coins, "what is left goes on to the next chapter; a debt is paid in years");
            Assert.AreEqual(years == 0, toll.PaidInFull);
        }

        [Test]
        public void TheUsurersYearsAreWrittenOnAtTheGate()
        {
            ChapterRun run = Run(50);
            new PurgatoryUsurer().Accept(run);
            Assert.AreEqual(100, run.Purse.Coins);
            Assert.AreEqual(1000, run.Years, "the floors never touch the sentence");
            GateToll toll = run.PayTribute();
            Assert.AreEqual(100, toll.YearsOwed);
            Assert.AreEqual(1100, run.Years);
            Assert.AreEqual(30, run.Purse.Coins);
        }

        [Test]
        public void MammonsLedgerStrikesYearsNowAndWritesMoreBack()
        {
            ChapterRun run = Run(50);
            new MammonsLedger().Accept(run);
            Assert.AreEqual(800, run.Years);
            run.PayTribute();
            Assert.AreEqual(1100, run.Years);
            Assert.IsFalse(new MammonsLedger().CanAppear(Run(rules: ChapterRules.For(2))), "his chapter only");
        }

        [Test]
        public void TheGamblerNeverComesToAPurseInDebt()
        {
            ChapterRun run = Run();
            run.Purse.Add(-30);
            Assert.IsFalse(new GamblerGhost().CanAppear(run));
        }

        [Test]
        public void AMatchWonPaysItsBonusAndAWardensRelic()
        {
            ChapterRun run = Run();
            int before = run.Purse.Coins;
            FloorTable won = WonMatch(run, warden: false);
            run.FinishTable(won);
            Assert.AreEqual(before + won.Coins + 5, run.Purse.Coins);

            before = run.Purse.Coins;
            FloorTable warden = WonMatch(run, warden: true);
            run.FinishTable(warden);
            Assert.AreEqual(before + warden.Coins - warden.TollTaken + 15, run.Purse.Coins);
            Assert.AreEqual(1, run.Effects.Relics.Count);
        }

        [Test]
        public void AWardensRelicWaitsWhenTheRunCarriesAllItMay()
        {
            ChapterRun run = Run();
            run.Effects.AddRelic(RelicIds.BoneDie);
            run.Effects.AddRelic(RelicIds.RustyCrown);
            run.FinishTable(WonMatch(run, warden: true));
            Assert.IsNotNull(run.WardenRelicWaiting);
            int coins = run.Purse.Coins;
            run.TakeWardenCoins();
            Assert.AreEqual(coins + 10, run.Purse.Coins);
            Assert.IsNull(run.WardenRelicWaiting);

            run.FinishTable(WonMatch(run, warden: true));
            string offered = run.WardenRelicWaiting;
            coins = run.Purse.Coins;
            Assert.IsTrue(run.SwapForWardenRelic(RelicIds.RustyCrown));
            CollectionAssert.AreEquivalent(new[] { RelicIds.BoneDie, offered }, run.Effects.Relics);
            Assert.AreEqual(coins, run.Purse.Coins, "a swap brings no coins");
        }

        /// <summary>A finished match with every hand won (a royal flush against nothing, stacked each hand).</summary>
        private static FloorTable WonMatch(ChapterRun run, bool warden)
        {
            FloorTable table = warden
                ? FloorTable.Warden(run.Rules, run.Sinner, run.Effects, run.Purse, 3)
                : FloorTable.Imp(run.Rules, run.Sinner, run.Effects, run.Purse, 3);
            while (!table.IsOver)
            {
                table.Game.RestoreDeck(DeckStarting(Royal + " " + HouseNothing + " AH KC QD"));
                PlayOne(table);
            }
            Assert.IsTrue(table.MatchWon);
            return table;
        }

        // ------------------------------------------------------------------ the black market

        [Test]
        public void TheMarketPricesTheStrongerGiftHigher()
        {
            ChapterRun run = Run(100);
            Stand(run, FirstOf(run, NodeKind.BlackMarket) ?? new MapNode(2, 0, NodeKind.BlackMarket, new[] { 0 }));
            BlackMarket market = run.OpenMarket();
            Assert.AreEqual(3, market.Relics.Count);
            CollectionAssert.AreEqual(new[] { 45, 50, 60 }, market.Relics.Select(o => o.Price));
            var order = market.Relics.Select(o => Array.IndexOf(BlackMarket.StrengthOrder, o.RelicId)).ToList();
            CollectionAssert.IsOrdered(order);
        }

        [Test]
        public void TheMarketGrowsDearerWithTheChapters()
        {
            Assert.AreEqual(54, ChapterRules.For(2).Price(45));
            Assert.AreEqual(72, ChapterRules.For(2).Price(60));
            Assert.AreEqual(84, ChapterRules.For(3).Price(60));
            Assert.AreEqual(35, ChapterRules.For(3).Price(25));
        }

        [Test]
        public void NothingIsBoughtOnDebtOrPastTheRelicLimit()
        {
            ChapterRun run = Run();
            Stand(run, new MapNode(2, 0, NodeKind.BlackMarket, new[] { 0 }));
            BlackMarket market = run.OpenMarket();
            string cheapest = market.Relics[0].RelicId;
            Assert.IsFalse(market.BuyRelic(cheapest), "20 coins do not buy a 45-coin relic");

            run.Purse.Add(200);
            Assert.IsTrue(market.BuyRelic(cheapest));
            Assert.AreEqual(220 - 45, run.Purse.Coins);
            Assert.IsTrue(market.BuyRelic(market.Relics[0].RelicId));
            Assert.IsFalse(market.CanBuyRelic(market.Relics[0].RelicId), "two relics at most");
            Assert.IsTrue(market.DropRelic(cheapest));
            Assert.AreEqual(1, run.Effects.Relics.Count);
            int coins = run.Purse.Coins;
            Assert.IsTrue(market.BuyImpsEye());
            Assert.AreEqual(coins - 20, run.Purse.Coins);
            Assert.IsTrue(run.ImpsEyeNext);
            Assert.IsFalse(market.BuyImpsEye(), "one eye waits at a time");
            Assert.IsFalse(market.ChangeJokers(1), "jokers are the Jester's");
        }

        [Test]
        public void TheJestersJokersAreSoldInAndOut()
        {
            var jester = new Sinner(SinnerRoster.Find(Jester.ClassId));
            var run = new ChapterRun(ChapterRules.For(1), jester, new RunEffects(), 750, 100, 7);
            Stand(run, new MapNode(2, 0, NodeKind.BlackMarket, new[] { 0 }));
            BlackMarket market = run.OpenMarket();
            Assert.IsFalse(market.ChangeJokers(-1), "never below the start");
            Assert.IsTrue(market.ChangeJokers(1));
            Assert.AreEqual(3, jester.Jokers);
            Assert.IsTrue(market.ChangeJokers(-1));
            Assert.AreEqual(2, jester.Jokers);
            Assert.AreEqual(120 - 40, run.Purse.Coins);
        }

        // ------------------------------------------------------------------ the fire

        [Test]
        public void TheFireBreaksACheatSilencesACurseOrShufflesForAFreeAnteAtTheDemonsTable()
        {
            ChapterRun run = Run();
            Stand(run, new MapNode(7, 0, NodeKind.PurgatoryFire, Array.Empty<int>()));
            Assert.AreSame(run.Sinner, run.BossGuard());
            Assert.IsTrue(run.TendFire(FireChoice.BreakFirstCheat));
            Assert.IsInstanceOf<FirstCheatBreaker>(run.BossGuard());

            run.Effects.AddRelic(RelicIds.RustyCrown);
            Assert.IsTrue(run.TendFire(FireChoice.SilenceCurse, RelicIds.RustyCrown));
            Assert.AreEqual(0, run.Effects.CombinedRelics.MaliceExtraPerHand, "the curse is silent");
            Assert.AreEqual(105, run.Effects.CombinedRelics.WinPercent, "the gift stays");
            run.EndChapter();
            Assert.AreEqual(1, run.Effects.CombinedRelics.MaliceExtraPerHand, "until the chapter ends");

            Assert.IsTrue(run.TendFire(FireChoice.ShuffleAndFreeAnte));
            Assert.IsTrue(run.FreeBossAnte);
            Assert.IsEmpty(run.DeckCards);
        }

        private sealed class FakeCheat : Cheats.ICheat
        {
            public string Id { get; set; } = "fake";
            public Cheats.CheatTier Tier { get; set; } = Cheats.CheatTier.Minor;
            public Cheats.CheatTiming Timing => Cheats.CheatTiming.AfterDeal;
            public bool Applies { get; set; } = true;
            public bool CanApply(Cheats.CheatTable table) => Applies;
            public Cheats.CheatResult Apply(Cheats.CheatTable table) => throw new NotSupportedException();
        }

        [Test]
        public void TheBreakerRefusesTheFirstMinorCheatThatWouldStrikeOnly()
        {
            var breaker = new FirstCheatBreaker(null);
            Assert.IsTrue(breaker.Allows(new FakeCheat { Tier = Cheats.CheatTier.Major }, null), "a major cheat is not its to break");
            Assert.IsTrue(breaker.Allows(new FakeCheat { Applies = false }, null), "one that would come to nothing does not use it up");
            Assert.IsFalse(breaker.Allows(new FakeCheat(), null));
            Assert.IsTrue(breaker.Used);
            Assert.IsTrue(breaker.Allows(new FakeCheat(), null), "only the first");
        }

        [Test]
        public void EveryRelicsGiftIsPartOfItsEffects()
        {
            foreach (IRelic relic in RelicRoster.All)
            {
                RelicEffects silenced = RelicRoster.Combined(new[] { relic.Id }, relic.Id);
                Assert.AreEqual(relic.Boon.WinPercent == 100 ? 100 : relic.Effects.WinPercent, silenced.WinPercent, relic.Id);
                Assert.AreEqual(relic.Effects.RedrawsPerTable, silenced.RedrawsPerTable, relic.Id);
                Assert.That(silenced.MaliceExtraPerHand == 0 && silenced.ReRaiseExtraUnits == 0 && silenced.HouseCardsDelta == 0 &&
                            silenced.LossAntePercent == 0, relic.Id + ": no curse");
            }
        }
    }
}
