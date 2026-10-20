// GEÇİCİ TEST KISAYOLU'nun testi. Kısayolu yansımayla bulur: DevJumpShortcut.cs silinince derleme bozulmaz, bu testler kendiliğinden
// atlanır (Ignore); istersen bu dosyayı da sil.

using System;
using HellPoker.Core.Dealers;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Demo 3.4.1's temporary F9: only a developer's build has it; it goes to the four demons in order.</summary>
    public class DevJumpShortcutTests
    {
        private static Type Shortcut => Type.GetType("HellPoker.Presentation.DevJumpShortcut, HellPoker.Presentation");

        private static bool Allowed(bool debugBuild, bool editor, string devValue) =>
            (bool)Shortcut.GetMethod("Allowed").Invoke(null, new object[] { debugBuild, editor, devValue });

        [SetUp]
        public void RequireIt()
        {
            if (Shortcut == null) Assert.Ignore("The temporary F9 is gone (DevJumpShortcut.cs deleted).");
        }

        [Test]
        public void APlayersBuild_HasNoF9_ADevelopersHasIt()
        {
            Assert.IsFalse(Allowed(false, false, null), "a player's build: nothing");
            Assert.IsFalse(Allowed(false, false, "0"));
            Assert.IsTrue(Allowed(true, false, null), "a development build");
            Assert.IsTrue(Allowed(false, true, null), "the editor");
            Assert.IsTrue(Allowed(false, false, "1"), "HELLPOKER_DEV=1");
        }

        /// <summary>Every class × every difficulty: a jump to a demon's table (the four in turn) plays that class at that level, at his table,
        /// and leaves the player's save and records as they were; his table over, the run is gone.</summary>
        [Test]
        public void EveryClass_AtEveryDifficulty_JumpsToATable_LeavingNoTrace()
        {
            var store = new Presentation.Settings.MemoryStore();
            var archive = new Presentation.Settings.ChapterArchive(store);
            Chapters.ChapterSave mine = Presentation.ChapterPresenter.PlacedSave(Sinners.SinnerRoster.King, 31, 2, "");
            archive.SaveRun(mine);
            var book = new Chapters.ChapterRecords();
            book.RunEnded(Chapters.JourneyEnd.Freed, 4, Sinners.King.ClassId, 100, Chapters.Difficulty.Medium, 200);
            archive.SaveRecords(book);
            string save = store.GetString(Presentation.Settings.ChapterArchive.RunKey, null);
            string records = store.GetString(Presentation.Settings.ChapterArchive.RecordsKey, null);

            var tableView = new FakeTableView();
            var table = new Presentation.TablePresenter((d, s) => Game.HellPokerGameFactory.Create(Game.GameRules.Default, d, 1, sinner: s), tableView);
            var chapters = new Presentation.ChapterPresenter(new FakeChapterMapView(), new FakeChapterPanelView(), () => (table, tableView), null,
                new AudioTests.FakeAudio(), Game.GameRules.Default, () => 4242, archive);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            System.Reflection.MethodInfo jump = typeof(Presentation.ChapterPresenter).GetMethod("JumpTraceless", flags);
            string[] targets = (string[])Shortcut.GetField("Targets").GetValue(null);
            try
            {
                int n = 0;
                foreach (Sinners.SinnerClass cls in Sinners.SinnerRoster.PhaseTwo)
                foreach (Chapters.Difficulty level in new[] { Chapters.Difficulty.Easy, Chapters.Difficulty.Medium, Chapters.Difficulty.Hard, Chapters.Difficulty.Ironman })
                {
                    int target = n++ % targets.Length;
                    string what = $"{cls.Id} {level} → {targets[target]}";
                    jump.Invoke(chapters, new object[] { targets[target], cls, level });
                    Assert.IsTrue(chapters.IsAtTable, what + ": at his table");
                    Assert.AreEqual(cls.Id, chapters.Journey.Sinner.Id, what);
                    Assert.AreEqual(level, chapters.Journey.Difficulty, what);
                    Assert.AreEqual(targets[target], table.CurrentDealerId, what + ": he deals");
                    table.PerformAction();   // a hand dealt
                    Assert.AreEqual(save, store.GetString(Presentation.Settings.ChapterArchive.RunKey, null), what + ": the save untouched");
                    Assert.AreEqual(records, store.GetString(Presentation.Settings.ChapterArchive.RecordsKey, null), what + ": the records untouched");
                    typeof(Presentation.ChapterPresenter).GetMethod(target == 3 ? "ResolveLucifer" : "ResolveBoss", flags).Invoke(chapters, null);
                    Assert.IsFalse(chapters.HasRun, what + ": gone after his table");
                    Assert.AreEqual(Chapters.ChapterRecords.Decode(records).Encode(), chapters.Records.Encode(), what + ": the records in memory too");
                }
            }
            finally
            {
                chapters.Dispose();
            }
        }

        [Test]
        public void TheFourTargets_AreTheChaptersDemonsAndLucifer()
        {
            CollectionAssert.AreEqual(new[] { DealerRoster.MammonId, DealerRoster.BelialId, DealerRoster.LilithId, DealerRoster.LuciferId },
                (string[])Shortcut.GetField("Targets").GetValue(null));
        }
    }
}
