using System.Linq;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Settings;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>The run saved between hands, the records, and the end of a run.</summary>
    public class SaveTests
    {
        // ------------------------------------------------------------------ the save format (Core)

        private static RunSnapshot Sample() =>
            new RunSnapshot("lilith", 1630, 14, new RunStats(14, 720, 1630, HandCategory.FullHouse, new[] { "mammon", "lilith" }, true));

        [Test]
        public void Snapshot_SurvivesTheRoundTrip()
        {
            Assert.IsTrue(RunSnapshot.TryDecode(Sample().Encode(), out RunSnapshot back));

            Assert.AreEqual("lilith", back.DealerId);
            Assert.AreEqual(1630, back.Years);
            Assert.AreEqual(14, back.RoundsPlayed);
            Assert.AreEqual(14, back.Stats.HandsPlayed);
            Assert.AreEqual(720, back.Stats.LowestYears);
            Assert.AreEqual(1630, back.Stats.HighestYears);
            Assert.AreEqual(HandCategory.FullHouse, back.Stats.BestHand);
            CollectionAssert.AreEqual(new[] { "mammon", "lilith" }, back.Stats.Dealers);
            Assert.IsTrue(back.Stats.SoulStaked);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("garbage that is not a save")]
        [TestCase("v=2\ndealer=mammon\nyears=900\nrounds=3\nhands=3\nlowest=900\nhighest=1000\nbest=\ndealers=mammon\nsoul=0")]
        [TestCase("v=1\ndealer=mammon\nyears=-5\nrounds=3\nhands=3\nlowest=0\nhighest=1000\nbest=\ndealers=mammon\nsoul=0")]
        [TestCase("v=1\ndealer=mammon\nyears=900\nrounds=3\nhands=3\nlowest=900\nhighest=1000\nbest=77\ndealers=mammon\nsoul=0")]
        [TestCase("v=1\ndealer=mammon\nyears=nine hundred\nrounds=3")]
        [TestCase("v=1\nyears=900\nrounds=3\nhands=3\nlowest=900\nhighest=1000\nbest=\ndealers=\nsoul=0")]
        public void BadOrOldSaves_AreIgnored(string text)
        {
            Assert.IsFalse(RunSnapshot.TryDecode(text, out RunSnapshot snapshot));
            Assert.IsNull(snapshot);
        }

        [Test]
        public void Records_SurviveTheRoundTrip_AndGarbageStartsEmpty()
        {
            var book = new RecordBook();
            book.RunStarted();
            book.RunStarted();
            book.RunEnded(true, "belial", 31);
            book.RunEnded(false, "lilith", 50);

            RecordBook back = RecordBook.Decode(book.Encode());

            Assert.AreEqual(2, back.RunsStarted);
            Assert.AreEqual(1, back.Absolutions);
            Assert.AreEqual(1, back.Damnations);
            Assert.AreEqual(31, back.FastestAbsolution);
            Assert.AreEqual(1, back.AbsolutionsAt("belial"));
            Assert.AreEqual(0, back.AbsolutionsAt("mammon"));

            Assert.AreEqual(0, RecordBook.Decode("v=1\nruns=lots").RunsStarted);
            Assert.AreEqual(0, RecordBook.Decode("v=9\nruns=4\nabsolved=1\ndamned=0\nfastest=").RunsStarted);
        }

        [Test]
        public void Records_KeepTheFastestAbsolution()
        {
            var book = new RecordBook();
            book.RunEnded(true, "mammon", 40);
            book.RunEnded(true, "mammon", 25);
            book.RunEnded(true, "belial", 60);

            Assert.AreEqual(25, book.FastestAbsolution);
            Assert.AreEqual(2, book.AbsolutionsAt("mammon"));
        }

        [Test]
        public void Stats_TrackTheRun()
        {
            var stats = new RunStats(1000, "mammon");
            stats.RecordHand(1100, null, false);
            stats.RecordHand(600, HandCategory.TwoPair, false);
            stats.RecordHand(700, HandCategory.OnePair, false);
            stats.SatWith("belial");
            stats.SatWith("mammon");
            stats.Note(1800, true);

            Assert.AreEqual(3, stats.HandsPlayed);
            Assert.AreEqual(600, stats.LowestYears);
            Assert.AreEqual(1800, stats.HighestYears);
            Assert.AreEqual(HandCategory.TwoPair, stats.BestHand);
            CollectionAssert.AreEqual(new[] { "mammon", "belial" }, stats.Dealers);
            Assert.IsTrue(stats.SoulStaked);
        }

        // ------------------------------------------------------------------ saving at the table

        private const string Blanks = "3S 6C JD QC 10S 2S 4H 5C 6D 7S";

        private MemoryStore _store;
        private RunArchive _archive;
        private FakeTableView _view;
        private HellPokerGame _game;
        private TablePresenter _presenter;

        private TablePresenter CreatePresenter(string player = "2C 9C JC 4C KC", string house = "2D 2H 5S 7H 9D", int startingYears = 1000)
        {
            _view = new FakeTableView();
            return new TablePresenter(d => _game = new HellPokerGame(
                d.ApplyTo(new GameRules(startingYears, 5000)),
                TestDecks.Stacked($"{player} {house} {Blanks}"),
                HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy()),
                new HouseDrawStrategy(),
                d.Payouts), _view, null, _archive);
        }

        [SetUp]
        public void SetUp()
        {
            _store = new MemoryStore();
            _archive = new RunArchive(_store);
        }

        [TearDown]
        public void TearDown() => _presenter?.Dispose();

        private void PlayAHand()
        {
            _view.PressAction();
            for (int guard = 0; guard < 10 && _game.Phase != GamePhase.Drawing; guard++) _view.PressBet(BetAction.Pass);
            _view.PressAction();
            for (int guard = 0; guard < 10 && _game.Phase != GamePhase.RoundOver && !_game.IsGameOver; guard++) _view.PressBet(BetAction.Pass);
        }

        [Test]
        public void ANewRun_IsSaved_AndCounted()
        {
            _presenter = CreatePresenter();
            _presenter.StartNewRun(DealerRoster.Belial);

            RunSnapshot saved = _archive.LoadRun();
            Assert.AreEqual("belial", saved.DealerId);
            Assert.AreEqual(1000, saved.Years);
            Assert.AreEqual(1, _archive.LoadRecords().RunsStarted);
        }

        [Test]
        public void EveryFinishedHand_IsSaved()
        {
            _presenter = CreatePresenter();
            _presenter.StartNewRun(DealerRoster.Mammon);

            PlayAHand();   // a flush beats a pair: 500 forgiven

            RunSnapshot saved = _archive.LoadRun();
            Assert.AreEqual(500, saved.Years);
            Assert.AreEqual(1, saved.RoundsPlayed);
            Assert.AreEqual(1, saved.Stats.HandsPlayed);
            Assert.AreEqual(HandCategory.Flush, saved.Stats.BestHand);
        }

        [Test]
        public void ChangingTables_IsSaved()
        {
            _presenter = CreatePresenter();
            _presenter.StartNewRun(DealerRoster.Mammon);
            PlayAHand();
            _view.PressAction();   // next hand

            _presenter.SwitchTable(DealerRoster.Belial);

            RunSnapshot saved = _archive.LoadRun();
            Assert.AreEqual("belial", saved.DealerId);
            CollectionAssert.AreEqual(new[] { "mammon", "belial" }, saved.Stats.Dealers);
        }

        [Test]
        public void ASavedRun_ResumesWhereItWasLeft()
        {
            _archive.SaveRun(Sample());
            _presenter = CreatePresenter();

            _presenter.Resume(DealerRoster.Lilith, _archive.LoadRun());

            Assert.AreEqual(1630, _game.Years);
            Assert.AreEqual(14, _game.RoundNumber);
            Assert.AreEqual(GamePhase.Betting, _game.Phase);
            Assert.IsTrue(_presenter.CanContinue);
            Assert.IsTrue(_game.IsSoulAtStake, "Past Lilith's line: the soul is back on her table.");
            Assert.IsTrue(_view.Soul.Visible);
            Assert.AreEqual(14, _presenter.Stats.HandsPlayed);
        }

        [Test]
        public void ASaveOfAFinishedRun_ShowsTheEnd_WithoutCrashing()
        {
            _archive.SaveRun(new RunSnapshot("mammon", 0, 9, new RunStats(9, 0, 1000, HandCategory.Flush, new[] { "mammon" }, false)));
            _presenter = CreatePresenter();

            _presenter.Resume(DealerRoster.Mammon, _archive.LoadRun());

            Assert.AreEqual(GamePhase.Absolved, _game.Phase);
            Assert.AreEqual("THE END", _view.ActionLabel);
            Assert.IsNull(_archive.LoadRun(), "A finished run is not kept.");
            Assert.IsFalse(_presenter.CanContinue);
        }

        [Test]
        public void AnUnreadableSave_IsDropped()
        {
            _store.SetString(RunArchive.RunKey, "v=1\nthis is not right");

            Assert.IsNull(_archive.LoadRun());
            Assert.IsNull(_store.GetString(RunArchive.RunKey, null), "The bad save is deleted.");
        }

        [Test]
        public void TheEndOfARun_UpdatesTheRecords_ClearsTheSave_AndShowsTheEnd()
        {
            // A Dead Man's Hand on the first hand.
            _presenter = CreatePresenter(player: "AS AC 8S 8C 3H", house: "KS KH 4D 4C 9H");
            RunSummary summary = null;
            _presenter.RunEnded += s => summary = s;
            _presenter.StartNewRun(DealerRoster.Mammon);

            PlayAHand();
            Assert.AreEqual(GamePhase.Absolved, _game.Phase);
            Assert.AreEqual("THE END", _view.ActionLabel);
            Assert.IsNull(_archive.LoadRun(), "A finished run is not saved.");
            RecordBook records = _archive.LoadRecords();
            Assert.AreEqual(1, records.Absolutions);
            Assert.AreEqual(1, records.FastestAbsolution);
            Assert.AreEqual(1, records.AbsolutionsAt("mammon"));

            _view.PressAction();

            Assert.IsNotNull(summary);
            Assert.IsTrue(summary.Absolved);
            Assert.AreEqual(1, summary.HandsPlayed);
            Assert.AreEqual(HandCategory.DeadMansHand, summary.BestHand);
            CollectionAssert.AreEqual(new[] { "MAMMON" }, summary.DealerNames);
        }

        [Test]
        public void WithoutAnEndScreen_TheEnd_StartsAFreshRun()
        {
            _presenter = CreatePresenter(player: "AS AC 8S 8C 3H", house: "KS KH 4D 4C 9H");
            _presenter.StartNewRun(DealerRoster.Mammon);
            PlayAHand();

            _view.PressAction();

            Assert.AreEqual(GamePhase.Betting, _game.Phase);
            Assert.AreEqual(1000, _game.Years);
            Assert.AreEqual(2, _archive.LoadRecords().RunsStarted);
        }
    }
}
