using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Core.Game;
using HellPoker.Core.Sinners;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Settings;
using HellPoker.Presentation.Ui;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Demo 3.4: SAVE on the chapter's screens (never at a table) and Ironman's saves (only at the camps; a dead run never comes back).</summary>
    public class SaveButtonTests
    {
        private sealed class FakeSaveButton : ISaveButtonView
        {
            public event Action Pressed;
            public SaveButtonPlace Place { get; private set; }
            public bool Enabled { get; private set; }
            public int SavedShown { get; private set; }

            public void Show(SaveButtonPlace place, bool enabled)
            {
                Place = place;
                Enabled = enabled;
            }

            public void Saved() => SavedShown++;
            public void Press() => Pressed?.Invoke();
        }

        private MemoryStore _store;
        private ChapterArchive _archive;
        private FakeChapterMapView _map;
        private FakeChapterPanelView _panel;
        private FakeTableView _tableView;
        private TablePresenter _table;
        private FakeSaveButton _save;
        private ChapterPresenter _chapters;
        private AudioTests.FakeAudio _audio;

        [SetUp]
        public void SetUp()
        {
            _store = new MemoryStore();
            _archive = new ChapterArchive(_store);
            Build();
        }

        /// <summary>A presenter over the same store (as a new launch of the game would be).</summary>
        private void Build()
        {
            _chapters?.Dispose();
            _audio = new AudioTests.FakeAudio();
            _map = new FakeChapterMapView();
            _panel = new FakeChapterPanelView();
            _tableView = new FakeTableView();
            _table = new TablePresenter((d, s) => HellPokerGameFactory.Create(GameRules.Default, d, 1, sinner: s), _tableView);
            _save = new FakeSaveButton();
            _chapters = new ChapterPresenter(_map, _panel, () => (_table, _tableView), null, _audio, GameRules.Default, () => 4242, _archive, null, null, _save);
        }

        [TearDown]
        public void TearDown() => _chapters.Dispose();

        private int SavedCount => _archive.LoadRun()?.SaveCount ?? -1;

        [Test]
        public void SAVE_OnTheMapAndItsPanels_SavesAtOnce_AtATableItIsDimAndSavesNothing()
        {
            _chapters.Start(SinnerRoster.Peasant, Difficulty.Medium);
            Assert.AreEqual(SaveButtonPlace.Map, _save.Place, "over the chapter's opening panel");
            Assert.IsTrue(_save.Enabled);
            _panel.Press(0);   // DESCEND
            int before = SavedCount;
            _save.Press();
            Assert.AreEqual(before + 1, SavedCount, "saved by hand into the one slot");
            Assert.AreEqual(1, _save.SavedShown, "SAVED flashes");
            CollectionAssert.Contains(_audio.Sfx, SfxIds.Saved);

            _map.Press(_chapters.Run.Choices.First(n => n.Kind == NodeKind.Table));
            Assert.IsTrue(_chapters.IsAtTable);
            Assert.AreEqual(SaveButtonPlace.Table, _save.Place, "beside HANDS at the table");
            Assert.IsFalse(_save.Enabled, "dim: a table cannot be saved");
            int atTable = SavedCount;
            _save.Press();
            Assert.AreEqual(atTable, SavedCount, "nothing written at the table");
            Assert.AreEqual(1, _save.SavedShown);
        }

        [Test]
        public void Ironman_SavesOnceAsTheRunBegins_ThenOnlyAtTheCamps_TheButtonOnlyThere()
        {
            _chapters.Start(SinnerRoster.Peasant, Difficulty.Ironman);
            Assert.AreEqual(1, SavedCount, "once as the run begins (a crash goes back to the start)");
            Assert.AreEqual(SaveButtonPlace.Hidden, _save.Place, "no SAVE away from a camp");
            Assert.IsFalse(_chapters.IronmanUnsaved);
            _panel.Press(0);
            _map.Press(_chapters.Run.Choices.First(n => n.Kind == NodeKind.Table));
            Assert.AreEqual(1, SavedCount, "a step to a table writes nothing");
            Assert.IsTrue(_chapters.IronmanUnsaved, "progress since the last save");
            Assert.AreEqual(SaveButtonPlace.Hidden, _save.Place, "nor at the table");
        }

        [Test]
        public void Ironman_AtACamp_TheButtonSaves_AndTheRecordsKeepTheCount()
        {
            ChapterSave camp = AtCamp(SinnerRoster.Peasant, Difficulty.Ironman);
            _archive.SaveRun(camp);
            Build();
            _chapters.Continue();
            Assert.AreEqual(NodeKind.PurgatoryFire, _chapters.Run.CurrentKind);
            Assert.AreEqual(SaveButtonPlace.Map, _save.Place, "at a camp SAVE is there");
            Assert.IsTrue(_save.Enabled);
            int before = SavedCount;
            _save.Press();
            Assert.AreEqual(before + 1, SavedCount);
            Assert.IsTrue(_chapters.Records.MayLoad(camp.RunId, SavedCount));
            Assert.IsFalse(_chapters.Records.MayLoad(camp.RunId, before), "an older copy would not load");
        }

        [Test]
        public void Ironman_ADeadRunsSave_AndAnOlderCopy_NeverContinue()
        {
            ChapterSave save = AtCamp(SinnerRoster.King, Difficulty.Ironman);
            ChapterRecords book = _archive.LoadRecords();
            book.RunDied(save.RunId);
            _archive.SaveRecords(book);
            _archive.SaveRun(save);
            Build();
            Assert.IsFalse(_chapters.CanContinue, "the dead run's save is thrown away");
            Assert.IsFalse(_archive.HasRun);

            ChapterSave copy = AtCamp(SinnerRoster.King, Difficulty.Ironman);
            copy.SaveCount = 2;
            book = _archive.LoadRecords();
            book.RunSaved(copy.RunId, 5);
            _archive.SaveRecords(book);
            _archive.SaveRun(copy);
            Build();
            Assert.IsFalse(_chapters.CanContinue, "an older copy of a live run's save");

            ChapterSave easy = AtCamp(SinnerRoster.King, Difficulty.Easy);
            _archive.SaveRun(easy);
            Build();
            Assert.IsTrue(_chapters.CanContinue, "the other levels have no such rule");
        }

        [Test]
        public void AnOldSave_ContinuesAsEasy()
        {
            string old = string.Join("\n", ChapterPresenter.PlacedSave(SinnerRoster.Peasant, 3, 1, "").Encode().Split('\n')
                .Select(l => l.StartsWith("p2=") ? "p2=1" : l.StartsWith("difficulty=") ? "circle=4" : l));
            _store.SetString(ChapterArchive.RunKey, old);
            Build();
            Assert.IsTrue(_chapters.CanContinue);
            _chapters.Continue();
            Assert.AreEqual(Difficulty.Easy, _chapters.Journey.Difficulty);
        }

        /// <summary>A save standing at the first chapter's nearest camp, its panel waiting.</summary>
        private static ChapterSave AtCamp(SinnerClass cls, Difficulty difficulty)
        {
            ChapterJourney journey = ChapterJourney.Begin(cls, 11, difficulty);
            ChapterMap map = journey.Run.Map;
            var paths = map.Floors[0].Select(n => new List<MapNode> { n }).ToList();
            List<MapNode> found = null;
            while (paths.Count > 0 && found == null)
            {
                found = paths.FirstOrDefault(p => p.Last().Kind == NodeKind.PurgatoryFire);
                if (found != null) break;
                paths = paths.SelectMany(p => map.NextFrom(p.Last()).Select(n => new List<MapNode>(p) { n })).ToList();
            }
            ChapterSave save = journey.Capture();
            save.Trail = found.Select(n => n.Lane).ToList();
            save.NodeDone = false;
            save.Pending = "fire";
            save.Coins = 300;
            return save;
        }
    }
}
