using System;
using System.IO;
using System.Linq;
using HellPoker.Core.Cheats;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Events;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using HellPoker.Core.Sinners;
using HellPoker.Presentation;
using HellPoker.Presentation.Settings;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// The playtest's run log: what a run's diary holds, that it is written when the run ends, is left, or the game closes on
    /// it — and that writing it can never stop the game. The file sink keeps the newest 50. And the window's centring.
    /// </summary>
    public class RunLogTests
    {
        private const string Flush = "2C 9C JC 4C KC";
        private const string HouseTwos = "2D 2H 5S 7H 9D";
        private const string Nothing = "2C 5D 7H 9S JC";
        private const string HouseFullHouse = "KS KH KD 4C 4H";
        private const string Blanks = "3S 6D 10S 8H 3H QC 8C 7C 6H 4S 10D 3D";

        private sealed class FirstChoice : IRandomSource
        {
            public int Next(int maxExclusive) => 0;
        }

        private sealed class OnlyCheat : ICheatPolicy
        {
            private readonly ICheat _cheat;
            public OnlyCheat(ICheat cheat) => _cheat = cheat;
            public System.Collections.Generic.IReadOnlyList<ICheat> Cheats => new[] { _cheat };
            public CheatPick Choose(CheatContext context, IRandomSource random) => new CheatPick(_cheat, null);
            public ICheat Find(string id) => id == _cheat.Id ? _cheat : null;
        }

        private FakeTableView _view;
        private TablePresenter _presenter;
        private HellPokerGame _game;
        private MemoryRunLogSink _logs;

        [TearDown]
        public void TearDown() => _presenter?.Dispose();

        private TablePresenter Table(string player, string house, ICheat cheat = null, EventSession events = null)
        {
            _view = new FakeTableView();
            _logs = new MemoryRunLogSink { Version = "0.1.5" };
            return _presenter = new TablePresenter((d, sinner) =>
            {
                var random = new FirstChoice();
                return _game = new HellPokerGame(d.ApplyTo(new GameRules(1000, 5000, luciferGateYears: 0)),
                    TestDecks.Stacked($"{player} {house} {Blanks}"), HandEvaluator.CreateDefault(), new CardExchanger(new MaxDiscardPolicy()),
                    new HouseDrawStrategy(), d.Payouts, null, new CheatSession(cheat == null ? null : new OnlyCheat(cheat), 1, random, sinner), random, sinner);
            }, _view, null, null, null, events, null, _logs);
        }

        /// <summary>Deals and plays the hand out: passes (raises where passing is forbidden — the last 250 years), stands pat, calls.</summary>
        private void PlayAHand()
        {
            _view.PressAction();   // deal
            for (int guard = 0; guard < 20 && _game.Phase != GamePhase.RoundOver && !_game.IsGameOver; guard++)
            {
                if (_game.Phase == GamePhase.Drawing) _view.PressAction();   // stand pat
                else if (_game.Phase == GamePhase.HouseReRaise) _view.PressBet(BetAction.Call);
                else _view.PressBet(_game.CanBet(BetAction.Pass, out _) ? BetAction.Pass : BetAction.Raise);
            }
        }

        private string OnlyLog => _logs.Files.Values.Single();

        [Test]
        public void TheDiary_TellsTheRun_LineByLine()
        {
            var log = new RunLog("0.1.5", "Turkish", "belial", "king", 1250, new DateTime(2026, 10, 5, 21, 7, 3));
            log.Hand(1, "belial", 1250, 1300, "HighCard HouseWins", "OnePair", 100, sealedHand: false, soul: false);
            log.Cheat(1, "played", CheatIds.ForkedTongue, CheatIds.FalseFace);
            log.Note("table: belial -> mammon at 1300 years");
            log.Hand(2, "mammon", 1300, 1300, null, null, 100, sealedHand: false, soul: false, freeFold: true);
            log.End("DAMNED", 3000, 2);

            string text = log.ToText();

            Assert.AreEqual("run-20261005-210703.txt", log.FileName);
            StringAssert.Contains("version:  0.1.5", text);
            StringAssert.Contains("language: Turkish", text);
            StringAssert.Contains("demon:    belial", text);
            StringAssert.Contains("class:    king", text);
            StringAssert.Contains("start:    1250 years", text);
            StringAssert.Contains("#1 belial 1250 -> 1300  HighCard HouseWins vs OnePair  stake 100", text);
            StringAssert.Contains("cheat played: forked_tongue (announced as false_face)", text);
            StringAssert.Contains("-- table: belial -> mammon", text);
            StringAssert.Contains("#2 mammon 1300 -> 1300  free fold", text);
            StringAssert.Contains("== DAMNED at 3000 years after 2 hands", text);
            Assert.IsTrue(log.IsEnded);
        }

        [Test]
        public void ARunToTheEnd_IsWritten_WithItsHands_Cheats_AndResult()
        {
            Table(Flush, HouseTwos, new CollateralCheat()).StartNewRun(DealerRoster.Mammon, SinnerRoster.King);
            _game.TakeOver(40, 0);   // one flush ends it
            PlayAHand();

            Assert.AreEqual(GamePhase.Absolved, _game.Phase);
            string text = OnlyLog;
            StringAssert.Contains("version:  0.1.5", text);
            StringAssert.Contains("demon:    mammon", text);
            StringAssert.Contains("class:    king", text);
            StringAssert.Contains("#1 mammon 40 -> 0  Flush PlayerWins vs", text);
            StringAssert.Contains("cheat announced: collateral", text);
            StringAssert.Contains("cheat played: collateral", text);
            StringAssert.Contains("== ABSOLVED at 0 years after 1 hands", text);
        }

        [Test]
        public void TablesEventsAndRelics_AreNoted_AndALeftRunIsWritten()
        {
            Table(Nothing, HouseFullHouse, events: new EventSession(new IHellEvent[] { new RelicEvent(EventIds.CursedChest) }, new FirstChoice(), 100, 1))
                .StartNewRun(DealerRoster.Mammon);   // the chest is offered at once
            _view.PressEventOption(0);
            _presenter.SwitchTable(DealerRoster.Belial);
            PlayAHand();
            _presenter.AbandonRun();

            string text = OnlyLog;
            StringAssert.Contains("start:    1000 years", text);
            StringAssert.Contains("-- event cursed_chest: accept", text);
            StringAssert.Contains("-- relic taken: bone_die", text);
            StringAssert.Contains("-- table: mammon -> belial at 1000 years", text);
            StringAssert.Contains("#1 belial 1000 ->", text);
            StringAssert.Contains("== ABANDONED", text);
        }

        [Test]
        public void TheClassPower_IsNoted_AndAResumedRunSaysSo()
        {
            Table(Nothing, HouseFullHouse)
                .Resume(DealerRoster.Mammon, new RunSnapshot("mammon", 1000, 3, new RunStats(1000, "mammon"), classId: "peasant", classCharge: 5));
            _view.PressAction();     // deal
            _presenter.UsePower();   // the honest heart: walk away
            _presenter.CloseLog();

            string text = OnlyLog;
            StringAssert.Contains("resumed:  from a save after hand 3, at 1000 years", text);
            StringAssert.Contains("-- power: free fold (hand 4)", text);
            StringAssert.Contains("#4 mammon 1000 -> 1000  free fold", text);
            StringAssert.Contains("== UNFINISHED: the game was closed at", text);
        }

        [Test]
        public void AHalfRun_IsWritten_WhenTheGameCloses()
        {
            Table(Nothing, HouseFullHouse).StartNewRun(DealerRoster.Mammon);
            PlayAHand();
            _view.PressAction();   // the next hand
            _view.PressAction();   // its deal

            _presenter.CloseLog();

            StringAssert.Contains("== UNFINISHED: the game was closed mid-hand", OnlyLog);
            StringAssert.Contains("#1 mammon 1000 ->", OnlyLog);
        }

        [Test]
        public void AFailingDisk_NeverStopsTheGame()
        {
            Table(Flush, HouseTwos).StartNewRun(DealerRoster.Mammon);
            _logs.Broken = true;
            _game.TakeOver(40, 0);

            Assert.DoesNotThrow(PlayAHand);
            Assert.AreEqual(GamePhase.Absolved, _game.Phase, "The run ended all the same.");
            Assert.DoesNotThrow(_presenter.CloseLog);
        }

        [Test]
        public void TheFileSink_KeepsTheNewest50_AndSwallowsItsFailures()
        {
            string folder = Path.Combine(Path.GetTempPath(), "hellpoker-runs-" + Guid.NewGuid().ToString("N"));
            try
            {
                var sink = new FileRunLogSink(folder, "0.1.5");
                var start = new DateTime(2026, 10, 5, 12, 0, 0);
                for (int i = 0; i < FileRunLogSink.Kept + 5; i++)
                {
                    var log = new RunLog("0.1.5", "English", "mammon", "peasant", 1000, start.AddMinutes(i));
                    log.End("DAMNED", 3000, i);
                    sink.Write(log);
                }

                string[] files = Directory.GetFiles(folder, "run-*.txt").Select(Path.GetFileName).OrderBy(f => f, StringComparer.Ordinal).ToArray();
                Assert.AreEqual(FileRunLogSink.Kept, files.Length);
                Assert.AreEqual("run-20261005-120500.txt", files.First(), "The five oldest went.");
                StringAssert.Contains("== DAMNED", File.ReadAllText(Path.Combine(folder, files.Last())));

                // A folder that cannot be made (a file is in the way): a warning, never an exception.
                string blocked = Path.Combine(folder, "blocked");
                File.WriteAllText(blocked, "a file, not a folder");
                Assert.DoesNotThrow(() => new FileRunLogSink(Path.Combine(blocked, "runs"), "0.1.5").Write(new RunLog("0.1.5", "English", "mammon", "peasant", 1000, start)));
            }
            finally
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
            }
        }

        [TestCase(0, 0, 1920, 1040, 1440, 810, 240, 115)]
        [TestCase(0, 0, 1920, 1040, 960, 540, 480, 250)]
        [TestCase(1920, 0, 2560, 1400, 1920, 1080, 2240, 160)]   // a second display to the right
        [TestCase(0, 0, 1280, 680, 1440, 810, 0, 0)]             // too big for the area: its top-left corner, never off it
        public void TheWindow_IsCentred_InTheWorkArea_NeverOutside(int x, int y, int w, int h, int width, int height, int ex, int ey)
        {
            Assert.AreEqual((ex, ey), WindowScales.Centered(x, y, w, h, width, height));
        }
    }
}
