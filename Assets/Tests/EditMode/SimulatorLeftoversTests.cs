using HellPoker.Core.Chapters;
using HellPoker.Core.Sinners;
using HellPoker.Presentation;
using HellPoker.Presentation.Settings;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Demo 3.4.1: what the Boss Simulator left in a player's store goes at launch; the game's save, records and settings stay.</summary>
    public class SimulatorLeftoversTests
    {
        private static MemoryStore StoreWithEverything(out string gameSave, out string gameRecords)
        {
            var store = new MemoryStore();
            store.SetString(RunArchive.RunKey, "v=4\ndealer=mammon\nyears=640\nrounds=12");
            store.SetString(RunArchive.RecordsKey, "runs=7\nabsolved=2");
            var archive = new ChapterArchive(store);
            ChapterSave save = ChapterPresenter.PlacedSave(SinnerRoster.King, 11, 2, "");
            save.Difficulty = Difficulty.Hard;
            archive.SaveRun(save);
            var records = new ChapterRecords();
            records.RunEnded(JourneyEnd.Freed, 4, King.ClassId, 120, Difficulty.Medium, 300);
            archive.SaveRecords(records);
            store.SetString("settings.language", "Turkish");
            gameSave = store.GetString(ChapterArchive.RunKey, null);
            gameRecords = store.GetString(ChapterArchive.RecordsKey, null);
            return store;
        }

        [Test]
        public void TheSimulatorsRunAndRecords_Go_TheGamesOwnStay()
        {
            MemoryStore store = StoreWithEverything(out string gameSave, out string gameRecords);

            Assert.IsTrue(SimulatorLeftovers.Forget(store));

            Assert.IsNull(store.GetString(RunArchive.RunKey, null), "the simulator's run is gone");
            Assert.IsNull(store.GetString(RunArchive.RecordsKey, null), "and its records");
            Assert.AreEqual(gameSave, store.GetString(ChapterArchive.RunKey, null), "the game's save untouched");
            Assert.AreEqual(gameRecords, store.GetString(ChapterArchive.RecordsKey, null), "the game's records untouched");
            Assert.AreEqual("Turkish", store.GetString("settings.language", null), "the settings too");

            var archive = new ChapterArchive(store);
            ChapterSave loaded = archive.LoadRun();
            Assert.IsNotNull(loaded, "the game's run still loads");
            Assert.AreEqual(Difficulty.Hard, loaded.Difficulty);
            Assert.AreEqual(1, archive.LoadRecords().FreedAt(King.ClassId, Difficulty.Medium), "the game's records still read");
        }

        [TestCase("garbage \u0000 not a save")]
        [TestCase("")]
        [TestCase("v=99\ndealer=nobody")]
        public void AGarbledSimulatorSave_NeverCrashes_ItJustGoes(string text)
        {
            var store = new MemoryStore();
            store.SetString(RunArchive.RunKey, text);
            store.SetString(RunArchive.RecordsKey, text);

            Assert.DoesNotThrow(() => SimulatorLeftovers.Forget(store));

            Assert.IsNull(store.GetString(RunArchive.RunKey, null));
            Assert.IsNull(store.GetString(RunArchive.RecordsKey, null));
        }

        [Test]
        public void NothingLeft_NothingWritten()
        {
            var store = new MemoryStore();
            store.SetString("settings.language", "English");
            int saves = store.Saves;

            Assert.IsFalse(SimulatorLeftovers.Forget(store));
            Assert.AreEqual(saves, store.Saves, "no save for nothing");
            Assert.IsFalse(SimulatorLeftovers.Forget(null));
        }
    }
}
