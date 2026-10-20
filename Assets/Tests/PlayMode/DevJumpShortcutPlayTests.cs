// GEÇİCİ TEST KISAYOLU'nun testleri. Kısayolu yansımayla bulur: DevJumpShortcut.cs silinince derleme bozulmaz, bu testler kendiliğinden
// atlanır (Ignore); istersen bu dosyayı da sil.

using System;
using System.Collections;
using System.Reflection;
using HellPoker.Core.Chapters;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// Demo 3.4.1's temporary F9: a player's build has none of it; a developer's goes to each of the four demons' tables — the game's own
    /// table — through the chapters' flow, writing nothing over the player's save, records or unlocks; the table over, the menu.
    /// </summary>
    public class DevJumpShortcutPlayTests
    {
        private static Type Shortcut => Type.GetType("HellPoker.Presentation.DevJumpShortcut, HellPoker.Presentation");
        private static string[] Targets => (string[])Shortcut.GetField("Targets").GetValue(null);
        private static void Toggle(Component s) => Shortcut.GetMethod("Toggle").Invoke(s, null);
        private static void Jump(Component s, int target) => Shortcut.GetMethod("Jump").Invoke(s, new object[] { target });
        private static bool IsOpen(Component s) => (bool)Shortcut.GetProperty("IsOpen").GetValue(s);
        private static bool IsAllowed(Component s) => (bool)Shortcut.GetProperty("IsAllowed").GetValue(s);
        private static bool Traceless => (bool)typeof(ChapterPresenter).GetProperty("IsTracelessRun").GetValue(ChapterJourneyTests.Chapters);
        private static Component TheGames => Object.FindFirstObjectByType<HellPokerBootstrap>().GetComponent(Shortcut);

        private static Component Install(bool allowed) =>
            (Component)Shortcut.GetMethod("Install", new[] { typeof(GameObject), typeof(ChapterPresenter), typeof(IMenuCommands), typeof(bool) })
                .Invoke(null, new object[] { Object.FindFirstObjectByType<HellPokerBootstrap>().gameObject, ChapterJourneyTests.Chapters, ChapterJourneyTests.Menu, allowed });

        [SetUp]
        public void RequireIt()
        {
            if (Shortcut == null) Assert.Ignore("The temporary F9 is gone (DevJumpShortcut.cs deleted).");
        }

        [TearDown]
        public void Keys() => MenuGridTests.ForgetTheKeyboard();

        [UnityTest, Timeout(120000)]
        public IEnumerator F9_NothingInAPlayersBuild_TheListInADevelopers()
        {
            HellPokerBootstrap.BatchStore.Clear();
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);
            Component developers = TheGames;
            Assert.IsNotNull(developers, "installed by the bootstrap");
            Assert.IsTrue(IsAllowed(developers), "the editor is a developer's");
            Object.Destroy(developers);
            Object.Destroy(GameObject.Find("DevJumpCanvas"));   // its list goes with it: a player's build never had one
            yield return null;

            Component players = Install(allowed: false);
            yield return MenuGridTests.Tap(Key.F9);
            Assert.IsFalse(IsOpen(players), "a player's build: F9 does nothing");
            Assert.IsNull(GameObject.Find("DevJumpCanvas"), "no list was ever built");
            Jump(players, 1);
            Assert.IsFalse(ChapterJourneyTests.Chapters.HasRun, "nor does a jump");
            Object.Destroy(players);
            yield return null;

            Component shortcut = Install(allowed: true);
            yield return MenuGridTests.Tap(Key.F9);
            Assert.IsTrue(IsOpen(shortcut), "a developer's build: the list");
            for (int i = 0; i < Targets.Length; i++)
                Assert.IsTrue(HellPokerScreenshots.IsActive("DevJump" + i), $"target {i}");
            yield return MenuGridTests.Tap(Key.Escape);
            Assert.IsFalse(IsOpen(shortcut), "Esc closes it");
            Assert.IsTrue(ChapterJourneyTests.Menu.IsAtMenuRoot, "the menu as it was (Esc did not go into a run)");
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator F9_GoesToEachDemonsTable_TheGamesOwn_LeavingNoTrace()
        {
            ChapterJourneyTests.PlayersOwnRunAndRecords(out string save, out string book);
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);
            Component shortcut = TheGames;
            Assert.IsNotNull(shortcut);
            ChapterPresenter chapters = ChapterJourneyTests.Chapters;
            string recordsBefore = chapters.Records.Encode();
            string Saved() => HellPokerBootstrap.BatchStore.GetString(Presentation.Settings.ChapterArchive.RunKey, null);
            string Book() => HellPokerBootstrap.BatchStore.GetString(Presentation.Settings.ChapterArchive.RecordsKey, null);

            for (int target = 0; target < Targets.Length; target++)
            {
                string demon = Targets[target];
                Assert.IsTrue(ChapterJourneyTests.Menu.IsAtMenuRoot, demon + ": from the title menu");
                Toggle(shortcut);
                Assert.IsTrue(IsOpen(shortcut), demon + ": the list");
                Jump(shortcut, target);
                Assert.IsFalse(IsOpen(shortcut));
                yield return new WaitForSeconds(0.8f);
                yield return ChapterJourneyTests.WaitForChapterTable();

                Assert.IsTrue(chapters.IsAtTable, demon + ": at his table");
                Assert.IsTrue(Traceless, demon + ": a run that leaves no trace");
                if (target == 3) Assert.AreEqual(JourneyStage.Lucifer, chapters.Journey.Stage, "Lucifer's table");
                else Assert.AreEqual(demon, chapters.Run.Rules.BossId, demon + "'s chapter");
                Assert.AreEqual(Difficulty.Medium, chapters.Journey.Difficulty, demon + ": Medium");
                var tables = Object.FindObjectsByType<TableView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Assert.AreEqual(1, tables.Length, demon + ": the game's one table");
                Assert.AreSame(ChapterJourneyTests.ChapterTableView, tables[0]);
                Assert.IsTrue(ChapterJourneyTests.ChapterTableView.GetComponent<Canvas>().enabled, demon + ": on screen");
                Assert.AreEqual(demon, ((TablePresenter)chapters.Table).CurrentDealerId, demon + ": he deals");
                chapters.Table.PerformAction();   // a hand dealt: nothing of it is kept
                yield return ChapterJourneyTests.WaitForChapterTable();

                Assert.AreEqual(save, Saved(), demon + ": the player's save untouched");
                Assert.AreEqual(book, Book(), demon + ": the records untouched");

                // His table is over (won or lost): the run goes, the menu comes.
                typeof(ChapterPresenter).GetMethod(target == 3 ? "ResolveLucifer" : "ResolveBoss", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(chapters, null);
                yield return new WaitForSeconds(0.6f);
                Assert.IsTrue(ChapterJourneyTests.Menu.IsAtMenuRoot, demon + ": the menu after his table");
                Assert.IsFalse(Traceless);
                Assert.IsFalse(chapters.HasRun, demon + ": the jump's run is gone");
                Assert.AreEqual(recordsBefore, chapters.Records.Encode(), demon + ": the records (and the unlocks) as they were");
                Assert.AreEqual(save, Saved());
                Assert.AreEqual(book, Book());
            }

            HellPokerScreenshots.Press("ContinueButton");
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(Core.Sinners.King.ClassId, chapters.Journey.Sinner.Id, "CONTINUE: the player's own run");
            Assert.AreEqual(2, chapters.Journey.Chapter);
            Assert.AreEqual(Difficulty.Hard, chapters.Journey.Difficulty);
        }
    }
}
