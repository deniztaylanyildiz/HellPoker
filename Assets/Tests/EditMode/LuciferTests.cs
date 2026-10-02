using System;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// The final table: Lucifer's house rules and fixed stakes, the gate that summons the player below 250 years and casts
    /// them down above it, the last year an ordinary table keeps, and the save of a run that met him.
    /// </summary>
    public class LuciferTests
    {
        private const string Nothing = "2C 5D 7H 9S JC";
        private const string HouseFullHouse = "KS KH KD 4C 4H";
        private const string Flush = "2C 9C JC 4C KC";
        private const string HouseTwos = "2D 2H 5S 7H 9D";
        private const string Blanks = "3S 8D QD 6C 10S";
        private const string Royal = "10H JH QH KH AH";

        private static HellPokerGame Table(Dealer dealer, int years, string player = Nothing, string house = HouseFullHouse,
            GameRules rules = null)
        {
            var game = new HellPokerGame(
                dealer.ApplyTo(rules ?? GameRules.Default),
                TestDecks.Stacked($"{player} {house} {Blanks}"),
                HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy(dealer.MaxDiscards)),
                new HouseDrawStrategy(dealer.MaxDiscards),
                dealer.Payouts);
            game.TakeOver(years, 7);
            return game;
        }

        private static void PlayPassively(HellPokerGame game)
        {
            game.PlaceBet();
            if (game.Phase == GamePhase.PlayerReveal) game.CheckToDraw();
            game.Draw(new int[0]);
            while (game.Phase == GamePhase.DrawReveal || game.Phase == GamePhase.HouseReveal)
                game.Bet(BetAction.Pass);
        }

        // ------------------------------------------------------------------ his table

        [Test]
        public void Lucifer_IsInTheRoster_ButNotAmongTheChoices()
        {
            Assert.AreEqual("lucifer", DealerRoster.Lucifer.Id);
            Assert.IsTrue(DealerRoster.Lucifer.IsFinalTable);
            foreach (Dealer dealer in DealerRoster.All)
                Assert.IsFalse(dealer.IsFinalTable, dealer.Id);
            Assert.AreEqual("lucifer", DealerRoster.Find("lucifer").Id);
            Assert.AreEqual("belial", DealerRoster.Find("belial").Id);
            Assert.IsNull(DealerRoster.Find("nobody"));
        }

        [Test]
        public void HouseRules_AreHisOwn()
        {
            Dealer lucifer = DealerRoster.Lucifer;

            Assert.AreEqual(3, lucifer.MaxDiscards);
            Assert.AreEqual(0, lucifer.HouseCardsShown);
            Assert.AreEqual(125, lucifer.Payouts.LossPercent);
            Assert.AreEqual(100, lucifer.Payouts.FoldPercentBeforeDraw);
            Assert.AreEqual(100, lucifer.Payouts.FoldPercentAfterDraw);
            Assert.AreEqual(8, lucifer.Payouts.GetMultiplier(HandCategory.FullHouse), "Standard multipliers.");
            Assert.AreEqual(HandCategory.TwoPair, lucifer.Betting.StrongFrom);
            Assert.AreEqual(80, lucifer.Betting.StrongPercent);
            Assert.AreEqual(25, lucifer.Betting.BluffPercent);
        }

        [TestCase(250)]
        [TestCase(200)]
        [TestCase(1000)]
        public void Stakes_AreFixed_FiftyAndAtMostOneHundredFifty(int years)
        {
            HellPokerGame game = Table(DealerRoster.Lucifer, years);

            game.PlaceBet();

            Assert.AreEqual(50, game.Unit);
            Assert.AreEqual(50, game.Ante);
            Assert.AreEqual(150, game.TableCap);
        }

        [Test]
        public void Stakes_AreAllIn_WhenLessIsLeft()
        {
            HellPokerGame game = Table(DealerRoster.Lucifer, 30);

            game.PlaceBet();

            Assert.AreEqual(30, game.Ante);
            Assert.AreEqual(30, game.TableCap);
            Assert.IsTrue(game.IsCommitted, "All in: the pact is sealed at once.");
        }

        [Test]
        public void Stakes_CapAtWhatIsLeft()
        {
            HellPokerGame game = Table(DealerRoster.Lucifer, 120);

            game.PlaceBet();

            Assert.AreEqual(50, game.Ante);
            Assert.AreEqual(120, game.TableCap);
        }

        [Test]
        public void HeShowsNoCards_OneDecisionAfterTheDraw_ThenTheShowdown()
        {
            HellPokerGame game = Table(DealerRoster.Lucifer, 200);
            game.PlaceBet();
            game.CheckToDraw();
            game.Draw(new int[0]);
            Assert.AreEqual(GamePhase.DrawReveal, game.Phase);
            Assert.AreEqual(0, game.HouseCardsRevealed);

            game.Bet(BetAction.Pass);

            Assert.AreEqual(GamePhase.RoundOver, game.Phase, "No House card turns before a last decision: straight to the showdown.");
        }

        [Test]
        public void TheFinalStretchRule_DoesNotApplyAtHisTable()
        {
            HellPokerGame game = Table(DealerRoster.Lucifer, 200);
            game.PlaceBet();

            Assert.IsFalse(game.IsRaiseForced);
            Assert.IsTrue(game.CanBet(BetAction.Pass, out _));
            Assert.IsTrue(game.CanCheckToDraw(out _));
        }

        [Test]
        public void NobodyLeavesHisTable()
        {
            HellPokerGame game = Table(DealerRoster.Lucifer, 200);

            Assert.IsFalse(game.CanLeaveTable(out string reason));
            Assert.IsNotNull(reason);
        }

        [Test]
        public void ALossAtHisTable_CostsAQuarterMore()
        {
            HellPokerGame game = Table(DealerRoster.Lucifer, 200);

            PlayPassively(game);

            Assert.AreEqual(200 + (50 + 50 * 7) * 125 / 100, game.Years, "Full house ×8 on the 50 ante, × 1.25.");
        }

        [Test]
        public void WinningAtHisTable_EndsTheSentence()
        {
            HellPokerGame game = Table(DealerRoster.Lucifer, 150, Flush, HouseTwos);

            PlayPassively(game);

            Assert.AreEqual(GamePhase.Absolved, game.Phase, "50 + 50 × 4 = 250 forgiven.");
        }

        // ------------------------------------------------------------------ ordinary tables keep the last year

        [Test]
        public void AnOrdinaryTable_NeverEndsTheSentence()
        {
            // 300 years at Mammon: unit 25 — a royal flush forgives 25 + 25 × 19 = 500, more than is left.
            HellPokerGame game = Table(DealerRoster.Mammon, 300, Royal, HouseTwos);

            PlayPassively(game);

            Assert.AreEqual(1, game.Years, "The last year is left for Lucifer.");
            Assert.AreEqual(GamePhase.RoundOver, game.Phase);
        }

        [Test]
        public void TheDeadMansHand_SetsThePlayerFree_AnywhereAndAtOnce()
        {
            HellPokerGame game = Table(DealerRoster.Mammon, 900, "AS AC 8S 8C 3H", HouseTwos);

            PlayPassively(game);

            Assert.AreEqual(GamePhase.Absolved, game.Phase, "Wild Bill's escape: Lucifer never sees this one.");
        }

        [Test]
        public void WithoutLucifer_EveryTableCanSetThePlayerFree()
        {
            HellPokerGame game = Table(DealerRoster.Mammon, 300, Royal, HouseTwos, new GameRules(luciferGateYears: 0));

            PlayPassively(game);

            Assert.AreEqual(GamePhase.Absolved, game.Phase);
        }

        // ------------------------------------------------------------------ the gate

        [Test]
        public void TheGate_SummonsAtOrBelow250_BetweenHandsOnly()
        {
            var gate = new LuciferGate(GameRules.Default);

            Assert.AreEqual(GateCall.Summoned, gate.Check(250, GamePhase.Betting));
            Assert.AreEqual(GateCall.Summoned, gate.Check(1, GamePhase.Betting));
            Assert.AreEqual(GateCall.Stay, gate.Check(251, GamePhase.Betting));
            Assert.AreEqual(GateCall.Stay, gate.Check(200, GamePhase.RoundOver), "Never mid-hand or on the result.");
            Assert.AreEqual(GateCall.Stay, gate.Check(200, GamePhase.PlayerReveal));
            Assert.AreEqual(GateCall.Stay, gate.Check(0, GamePhase.Betting), "Nothing left to summon.");
        }

        [Test]
        public void Summoned_RemembersWhereFrom_AndCountsTheAttempt()
        {
            var gate = new LuciferGate(GameRules.Default);

            gate.Summon("belial");

            Assert.IsTrue(gate.IsAtLucifer);
            Assert.AreEqual("belial", gate.OriginDealerId);
            Assert.AreEqual(1, gate.Attempts);
            Assert.AreEqual(0, gate.CastDowns);
            Assert.AreEqual(GateCall.Stay, gate.Check(250, GamePhase.Betting), "At his table, 250 is still his.");
            Assert.AreEqual(GateCall.Stay, gate.Check(10, GamePhase.Betting));
            Assert.AreEqual(GateCall.CastDown, gate.Check(251, GamePhase.Betting));
        }

        [TestCase(260, 500)]
        [TestCase(499, 500)]
        [TestCase(500, 500)]
        [TestCase(735, 735)]
        public void CastDown_LeavesAtLeast500(int years, int landing)
        {
            var gate = new LuciferGate(GameRules.Default);
            gate.Summon("mammon");

            Assert.AreEqual(landing, gate.CastDown(years));
            Assert.IsFalse(gate.IsAtLucifer);
            Assert.AreEqual(1, gate.CastDowns);
        }

        [Test]
        public void ReachingTheGateAgain_SummonsAgain()
        {
            var gate = new LuciferGate(GameRules.Default);
            gate.Summon("lilith");
            gate.CastDown(300);

            Assert.AreEqual(GateCall.Summoned, gate.Check(240, GamePhase.Betting));
            gate.Summon("lilith");

            Assert.AreEqual(2, gate.Attempts);
            Assert.AreEqual(1, gate.CastDowns);
        }

        [Test]
        public void WithoutAGate_NothingMoves()
        {
            var gate = new LuciferGate(new GameRules(luciferGateYears: 0));

            Assert.AreEqual(GateCall.Stay, gate.Check(10, GamePhase.Betting));
        }

        [Test]
        public void TheGate_RefusesNonsense()
        {
            var gate = new LuciferGate(GameRules.Default);
            Assert.Throws<InvalidOperationException>(() => gate.CastDown(300));
            gate.Summon("mammon");
            Assert.Throws<InvalidOperationException>(() => gate.Summon("mammon"));
            Assert.Throws<ArgumentException>(() => new LuciferGate(250, 500, true, null, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameRules(luciferGateYears: 250, luciferCastDownYears: 200));
        }

        // ------------------------------------------------------------------ records

        [Test]
        public void Records_CountHisVictims_AndHisFalls()
        {
            var book = new RecordBook();
            book.RunEnded(true, "mammon", 30, luciferAttempts: 3, beatLucifer: true);
            book.RunEnded(true, "belial", 20, luciferAttempts: 1, beatLucifer: true);
            book.RunEnded(false, "lilith", 50, luciferAttempts: 2);
            book.RunEnded(true, "mammon", 9, wildBill: true);
            book.RunEnded(false, "mammon", 40);

            RecordBook back = RecordBook.Decode(book.Encode());

            Assert.AreEqual(3, back.LuciferReached);
            Assert.AreEqual(2, back.LuciferDefeated);
            Assert.AreEqual(1, back.FewestLuciferAttempts);
            Assert.AreEqual(1, back.WildBillEscapes);
            Assert.AreEqual(3, back.Absolutions);
            Assert.AreEqual(2, back.Damnations);
        }

        [Test]
        public void Records_FromBeforeLucifer_StillRead()
        {
            RecordBook old = RecordBook.Decode("v=1\nruns=4\nabsolved=1\ndamned=2\nfastest=33\nfree.mammon=1");

            Assert.AreEqual(4, old.RunsStarted);
            Assert.AreEqual(33, old.FastestAbsolution);
            Assert.AreEqual(0, old.LuciferReached);
            Assert.IsNull(old.FewestLuciferAttempts);
        }

        // ------------------------------------------------------------------ the save

        [Test]
        public void Snapshot_AtLucifer_SurvivesTheRoundTrip()
        {
            var snapshot = new RunSnapshot("lucifer", 180, 33, new RunStats(1000, "belial"), null, true, "belial", 2);

            string text = snapshot.Encode();
            Assert.IsTrue(text.StartsWith("v=3"));
            Assert.IsTrue(RunSnapshot.TryDecode(text, out RunSnapshot back));

            Assert.AreEqual("lucifer", back.DealerId);
            Assert.IsTrue(back.AtLucifer);
            Assert.AreEqual("belial", back.OriginDealerId);
            Assert.AreEqual(2, back.LuciferAttempts);
        }

        [Test]
        public void Snapshot_AfterAFall_KeepsTheAttempts()
        {
            var snapshot = new RunSnapshot("mammon", 500, 40, new RunStats(1000, "mammon"), null, false, "mammon", 1);

            Assert.IsTrue(RunSnapshot.TryDecode(snapshot.Encode(), out RunSnapshot back));

            Assert.IsFalse(back.AtLucifer);
            Assert.AreEqual("mammon", back.OriginDealerId);
            Assert.AreEqual(1, back.LuciferAttempts);
        }

        [Test]
        public void AVersion1Save_StillReads_AsARunThatNeverMetHim()
        {
            string text = "v=1\ndealer=mammon\nyears=900\nrounds=3\nhands=3\nlowest=900\nhighest=1000\nbest=\ndealers=mammon\nsoul=0";

            Assert.IsTrue(RunSnapshot.TryDecode(text, out RunSnapshot snapshot));

            Assert.AreEqual(900, snapshot.Years);
            Assert.IsFalse(snapshot.AtLucifer);
            Assert.IsNull(snapshot.OriginDealerId);
            Assert.AreEqual(0, snapshot.LuciferAttempts);
        }

        [TestCase("v=2\ndealer=lucifer\nyears=100\nrounds=3\nhands=3\nlowest=100\nhighest=1000\nbest=\ndealers=mammon\nsoul=0\nlucifer=1\norigin=\nattempts=1")]
        [TestCase("v=2\ndealer=lucifer\nyears=100\nrounds=3\nhands=3\nlowest=100\nhighest=1000\nbest=\ndealers=mammon\nsoul=0\nlucifer=1\norigin=mammon\nattempts=0")]
        [TestCase("v=2\ndealer=mammon\nyears=100\nrounds=3\nhands=3\nlowest=100\nhighest=1000\nbest=\ndealers=mammon\nsoul=0\nlucifer=maybe\norigin=\nattempts=0")]
        [TestCase("v=2\ndealer=mammon\nyears=100\nrounds=3\nhands=3\nlowest=100\nhighest=1000\nbest=\ndealers=mammon\nsoul=0")]
        [TestCase("v=3\ndealer=mammon\nyears=100\nrounds=3\nhands=3\nlowest=100\nhighest=1000\nbest=\ndealers=mammon\nsoul=0\nlucifer=0\norigin=\nattempts=0")]
        public void BrokenLuciferLines_MakeTheSaveUnreadable(string text)
        {
            Assert.IsFalse(RunSnapshot.TryDecode(text, out _));
        }
    }
}
