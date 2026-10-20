using System.Collections;
using System.IO;
using HellPoker.Core.Chapters;
using HellPoker.Core.Sinners;
using HellPoker.Presentation;
using HellPoker.Presentation.Ui;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>Demo 3.5.1: Lilith's gate with coins left — the Last Bribe's line in the panel (both languages, fitting), its coins flying and
    /// the bar shrinking, then her table with the bar it foretold.</summary>
    public class LastBribePlayTests
    {
        [TearDown]
        public void English() => Lang.Set(Language.English);

        internal static ChapterSave AtLilithsGate(int coins)
        {
            ChapterSave save = ChapterPresenter.PlacedSave(SinnerRoster.King, 11, 3, "gate");
            save.Coins = coins;
            return save;
        }

        internal static IEnumerator Load(ChapterSave save, bool turkish)
        {
            HellPokerBootstrap.BatchStore.Clear();
            HellPokerBootstrap.BatchStore.SetString("settings.language", turkish ? "Turkish" : "English");
            new Presentation.Settings.ChapterArchive(HellPokerBootstrap.BatchStore).SaveRun(save);
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);
            HellPokerScreenshots.Press("ContinueButton");
            yield return null;
            yield return null;
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator LilithsGate_SaysTheBribe_ItFits_TheBarShrinks_InBothLanguages()
        {
            foreach (bool turkish in new[] { false, true })
            {
                string lang = turkish ? " (tr)" : " (en)";
                yield return Load(AtLilithsGate(2000), turkish);
                ChapterPresenter chapters = ChapterJourneyTests.Chapters;
                ChapterRun run = chapters.Run;
                Assert.AreEqual(ChapterRules.BribeChapter, run.Rules.Number, lang);
                var panel = Object.FindFirstObjectByType<ChapterPanelView>();
                Assert.IsNotNull(chapters.Panel, "the gate" + lang);
                int left = 2000 - run.TributePaid(2000);
                StringAssert.Contains(string.Format(UiText.GateBribeFormat, left, ChapterRun.BribePercentFor(left)), chapters.Panel.Text, lang);
                Assert.IsNotNull(chapters.Panel.BarBefore, "the bar before the bribe" + lang);
                Assert.IsTrue(panel.BribePlaying, "the coins fly" + lang);
                float started = Time.time;
                while (panel.BribePlaying && Time.time - started < 3f) yield return null;
                Assert.IsFalse(panel.BribePlaying, "the moment ends" + lang);
                Canvas.ForceUpdateCanvases();
                RectTransform box = panel.Box;
                Vector2 at = box.anchoredPosition;
                Assert.That(at.x >= 0 && -at.y >= 0 && at.x + box.sizeDelta.x <= PixelScreen.Width && -at.y + box.sizeDelta.y <= PixelScreen.Height,
                    "the panel on the screen" + lang);
                var body = box.Find("Text").GetComponent<UnityEngine.UI.Text>();
                Assert.LessOrEqual(body.preferredWidth, body.rectTransform.rect.width + 1, "the words fit" + lang);

                int foretold = run.BarAfterGate(2000);
                HellPokerScreenshots.Press("PanelOption0");   // PAY: her table
                yield return new WaitForSeconds(0.3f);
                Assert.IsTrue(chapters.IsAtTable, "her table" + lang);
                Assert.AreEqual(foretold, run.BossBarStart, "her bar as the gate foretold it" + lang);
                Assert.AreEqual(0, run.Purse.Coins, "every coin went to her" + lang);
            }
        }

        /// <summary>Demo 3.5.1's screens (Screenshots/demo351, 1920×1080, both languages): an imp's table, Belial's, Lilith's gate with the bribe.
        /// Not a real test (Explicit).</summary>
        [UnityTest, Explicit, Timeout(300000)]
        public IEnumerator CaptureDemo351()
        {
            string folder = Path.Combine(Application.dataPath, "..", "Screenshots", "demo351");
            System.Environment.SetEnvironmentVariable("HELLPOKER_SHOTS", folder);
            foreach (bool turkish in new[] { false, true })
            {
                string l = turkish ? "tr" : "en";
                yield return Load(TableLayoutTests.AtNode(SinnerRoster.Peasant, NodeKind.Table, "imp"), turkish);
                yield return ChapterJourneyTests.WaitForChapterTable();
                ChapterJourneyTests.Chapters.Table.PerformAction();
                yield return new WaitForSeconds(1.2f);
                yield return HellPokerScreenshots.Shot($"imp_table_{l}");

                yield return Load(new ChapterSave
                {
                    ClassId = Core.Sinners.King.ClassId, Seed = 7, Chapter = 2, Coins = 40, Years = 1600, Match = "boss", BossBarStart = 1600,
                    BossBar = 1600, NodeDone = false
                }, turkish);
                yield return ChapterJourneyTests.WaitForChapterTable();
                ChapterJourneyTests.Chapters.Table.PerformAction();
                yield return new WaitForSeconds(1.2f);
                yield return HellPokerScreenshots.Shot($"belial_table_{l}");

                yield return Load(AtLilithsGate(2000), turkish);
                yield return new WaitForSeconds(1.0f);
                yield return HellPokerScreenshots.Shot($"lilith_gate_bribe_{l}");
            }
        }
    }
}
