using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// Demo 3.0.2: a hover box shows only while something is pointed at. Demo 3.0.1 drew the relics' empty bag box (black, a lilac
    /// frame) over the map and the chapter's panels: the bag hides by switching its canvas off, and a box's own canvas drew on.
    /// </summary>
    public class HoverTipTests
    {

        [UnityTest, Timeout(600000)]
        public IEnumerator NothingPointedAt_NoHoverBoxShows_OnAnyChapterScreen()
        {
            // The map; the chapter's opening panel.
            ChapterSave map = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 31, 1, "");
            map.NodeDone = true;
            map.Relics = new List<string> { Core.Relics.RelicIds.FerrymansCoin, Core.Relics.ChapterRelicIds.IronVow };
            yield return ChapterJourneyTests.LoadWithSave(map);
            yield return NoBoxShows("the map");

            ChapterSave opening = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 11, 1, "chapter");
            opening.Coins = 120;
            yield return ChapterJourneyTests.LoadWithSave(opening);
            yield return NoBoxShows("the chapter's panel");

            // A floor's table, dealt: on from the opening panel.
            HellPokerScreenshots.Press("PanelOption0");
            yield return new WaitForSeconds(0.3f);
            yield return NoBoxShows("the map after the panel");
            MapNode first = ChapterJourneyTests.Chapters.Run.Choices.First(n => n.Kind == NodeKind.Table);
            HellPokerScreenshots.Find<UnityEngine.UI.Button>($"Node{first.Floor}_{first.Lane}").onClick.Invoke();
            yield return ChapterJourneyTests.WaitForChapterTable();
            ChapterJourneyTests.Chapters.Table.PerformAction();
            yield return ChapterJourneyTests.WaitForChapterTable();
            yield return NoBoxShows("a floor's table");

            yield return ChapterJourneyTests.LoadWithSave(AtNode(NodeKind.BlackMarket, "market"));
            yield return NoBoxShows("a market");
            yield return ChapterJourneyTests.LoadWithSave(AtNode(NodeKind.PurgatoryFire, "fire"));
            yield return NoBoxShows("a camp");
            yield return ChapterJourneyTests.LoadWithSave(AtNode(NodeKind.Event, "event:" + ChoiceEventIds.ChainedSoul));
            yield return NoBoxShows("an offer");
            yield return ChapterJourneyTests.LoadWithSave(ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 11, 1, "loot"));
            yield return NoBoxShows("the spoils");

            // A demon's table with relics, the bar and the class panel, dealt.
            ChapterSave boss = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 11, 1, "");
            boss.NodeDone = false;
            boss.Match = "boss";
            boss.BossBarStart = boss.BossBar = 1600;
            boss.Relics = Core.Relics.ChapterRelics.ClassChain(Core.Sinners.King.ClassId).ToList();
            yield return ChapterJourneyTests.LoadWithSave(boss);
            ChapterJourneyTests.Chapters.Table.PerformAction();
            yield return ChapterJourneyTests.WaitForChapterTable();
            yield return NoBoxShows("a demon's table");

            // Lucifer's call, then his table.
            yield return ChapterJourneyTests.LoadWithSave(ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 11, 3, "lucifer"));
            yield return NoBoxShows("Lucifer's call");
            HellPokerScreenshots.Press("PanelOption0");
            yield return ChapterJourneyTests.WaitForChapterTable();
            ChapterJourneyTests.Chapters.Table.PerformAction();
            yield return ChapterJourneyTests.WaitForChapterTable();
            yield return NoBoxShows("Lucifer's table");
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator TheBagsBox_ShowsInsideTheOpenBag_AndClosesWithIt()
        {
            ChapterSave map = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 31, 1, "");
            map.NodeDone = true;
            map.Relics = new List<string> { Core.Relics.RelicIds.FerrymansCoin, Core.Relics.ChapterRelicIds.IronVow };
            yield return ChapterJourneyTests.LoadWithSave(map);
            var bag = Object.FindFirstObjectByType<RelicBagView>();
            HoverTip box = bag.GetComponentsInChildren<HoverTip>(true).Single();
            Assert.IsFalse(bag.IsOpen);
            Assert.IsFalse(box.Visible, "the bag closed: no box");

            HellPokerScreenshots.Press("MapRelicsButton");
            yield return null;
            Assert.IsTrue(bag.IsOpen, "RELICS opens the bag");
            yield return null;
            // The keyboard's cursor is a pointer too: on a relic the box tells it; on an empty place, a look only, nothing to tell.
            RelicBagState state = bag.State;
            bool onRelic = state.Cursor >= 0 && state.Cursor < state.Places.Count && state.Places[state.Cursor] != null;
            Assert.AreEqual(onRelic || state.Editable, box.Visible, "the box only with something to tell");
            bag.PointAt(-1);
            yield return null;
            Assert.IsFalse(box.Visible, "a look only, nothing pointed at: nothing to tell");

            bag.PointAt(0);
            yield return null;
            yield return null;
            Assert.IsTrue(box.Visible, "a relic pointed at: its box");
            Canvas.ForceUpdateCanvases();
            var panel = (RectTransform)box.transform.parent;
            Assert.AreEqual("BagPanel", panel.name);
            Rect inside = Box(panel), rect = Box((RectTransform)box.transform);
            Assert.IsTrue(rect.xMin >= inside.xMin - 0.5f && rect.xMax <= inside.xMax + 0.5f && rect.yMin >= inside.yMin - 0.5f && rect.yMax <= inside.yMax + 0.5f,
                $"the box inside the bag ({rect} in {inside})");
            Assert.AreEqual(panel.lossyScale.x, box.transform.lossyScale.x, 0.0001f, "the bag's own scale");
            Assert.AreEqual(panel.lossyScale.y, box.transform.lossyScale.y, 0.0001f, "the bag's own scale");
            StringAssert.Contains(UiText.RelicName(Core.Relics.RelicIds.FerrymansCoin), box.GetComponentInChildren<UnityEngine.UI.Text>().text);

            bag.PointAt(-1);
            Assert.IsFalse(bag.TipShown, "pointing away: nothing to tell, the box closes");
            bag.PointAt(0);
            HellPokerScreenshots.Press("BagDoneButton");
            yield return null;
            Assert.IsFalse(bag.IsOpen);
            Assert.IsFalse(bag.TipShown, "the box closes with the bag");
            Assert.IsFalse(box.Visible);
            yield return NoBoxShows("the map after the bag");

            HellPokerScreenshots.Press("MapRelicsButton");
            yield return null;
            yield return null;
            state = bag.State;
            onRelic = state.Cursor >= 0 && state.Cursor < state.Places.Count && state.Places[state.Cursor] != null;
            Assert.AreEqual(onRelic, box.Visible, "opened again: the box only for what the cursor is on");
        }

        /// <summary>The fix on screen (Screenshots/, or HELLPOKER_SHOTS): the map and the chapter's panel without the empty box, the bag
        /// with a relic pointed at.</summary>
        [UnityTest, Explicit, Timeout(300000)]
        public IEnumerator CaptureTheScreens()
        {
            ChapterSave opening = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 11, 1, "chapter");
            opening.Coins = 120;
            yield return ChapterJourneyTests.LoadWithSave(opening);
            yield return HellPokerScreenshots.Shot("302_chapter_panel");

            ChapterSave map = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 31, 1, "");
            map.NodeDone = true;
            map.Relics = new List<string> { Core.Relics.RelicIds.FerrymansCoin, Core.Relics.ChapterRelicIds.IronVow };
            yield return ChapterJourneyTests.LoadWithSave(map);
            yield return HellPokerScreenshots.Shot("302_map");
            HellPokerScreenshots.Press("MapRelicsButton");
            yield return null;
            Object.FindFirstObjectByType<RelicBagView>().PointAt(1);
            yield return null;
            yield return null;
            yield return HellPokerScreenshots.Shot("302_bag_relic_hover");
        }

        /// <summary>No hover box is on the screen (each frame of a few; the boxes keep themselves in their LateUpdate).</summary>
        private static IEnumerator NoBoxShows(string where)
        {
            for (int frame = 0; frame < 3; frame++)
            {
                yield return null;
                HoverTip[] boxes = Object.FindObjectsByType<HoverTip>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Assert.Greater(boxes.Length, 0, where + ": the screens and their boxes are built");
                string[] shown = boxes.Where(b => b.Visible).Select(b => b.name + " under " + b.transform.parent.name).ToArray();
                Assert.IsEmpty(shown, where + ": a hover box shows with nothing pointed at:\n" + string.Join("\n", shown));
            }
        }

        private static ChapterSave AtNode(NodeKind kind, string pending)
        {
            ChapterJourney journey = ChapterJourney.Begin(Core.Sinners.SinnerRoster.Peasant, 11);
            ChapterMap map = journey.Run.Map;
            var paths = map.Floors[0].Select(n => new List<MapNode> { n }).ToList();
            List<MapNode> found = null;
            while (paths.Count > 0 && found == null)
            {
                found = paths.FirstOrDefault(p => p.Last().Kind == kind);
                if (found != null) break;
                paths = paths.SelectMany(p => map.NextFrom(p.Last()).Select(n => new List<MapNode>(p) { n })).ToList();
            }
            ChapterSave save = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.Peasant, 11, 1, pending);
            save.Trail = found?.Select(n => n.Lane).ToList() ?? new List<int>();
            save.NodeDone = false;
            save.Coins = 300;
            return save;
        }

        private static Rect Box(RectTransform t)
        {
            var corners = new Vector3[4];
            t.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
    }
}
