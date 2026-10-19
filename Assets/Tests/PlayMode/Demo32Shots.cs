using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// Demo 3.2's screens (Screenshots/, or HELLPOKER_SHOTS; HELLPOKER_SHOT_SIZE, HELLPOKER_LANG=tr): the chapter tables in the new layout —
    /// a floor's, a warden's, a demon's, Lucifer's — and every class's panel at a demon's table. Not a real test (Explicit).
    /// </summary>
    [Explicit]
    public class Demo32Shots
    {
        [TearDown]
        public void Restore() => HellPokerBootstrap.ForceSimulator = null;

        private static string Size => System.Environment.GetEnvironmentVariable("HELLPOKER_SHOT_SIZE") ?? "1920x1080";

        private static ChapterSave AtNode(Core.Sinners.SinnerClass cls, NodeKind kind, string match)
        {
            ChapterJourney journey = ChapterJourney.Begin(cls, 11);
            ChapterMap map = journey.Run.Map;
            var paths = map.Floors[0].Select(n => new List<MapNode> { n }).ToList();
            List<MapNode> found = null;
            while (paths.Count > 0 && found == null)
            {
                found = paths.FirstOrDefault(p => p.Last().Kind == kind);
                if (found != null) break;
                paths = paths.SelectMany(p => map.NextFrom(p.Last()).Select(n => new List<MapNode>(p) { n })).ToList();
            }
            ChapterSave save = ChapterPresenter.PlacedSave(cls, 11, 1, "");
            save.Trail = found?.Select(n => n.Lane).ToList() ?? new List<int>();
            save.NodeDone = false;
            save.Coins = 200;
            save.Match = match;
            save.MatchHouse = 50;
            save.Relics = Core.Relics.ChapterRelics.ClassChain(cls.Id).ToList();
            return save;
        }

        private static IEnumerator Dealt(string name)
        {
            yield return ChapterJourneyTests.WaitForChapterTable();
            ChapterJourneyTests.Chapters.Table.PerformAction();
            yield return ChapterJourneyTests.WaitForChapterTable();
            yield return new WaitForSeconds(0.6f);
            yield return HellPokerScreenshots.Shot($"{name}_{Size}");
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator CaptureDemoThreeTwo()
        {
            yield return ChapterJourneyTests.LoadWithSave(AtNode(Core.Sinners.SinnerRoster.Peasant, NodeKind.Table, "imp"));
            yield return Dealt("340_floor");
            yield return ChapterJourneyTests.LoadWithSave(AtNode(Core.Sinners.SinnerRoster.Peasant, NodeKind.Warden, "warden"));
            yield return Dealt("341_warden");

            foreach (Core.Sinners.SinnerClass cls in Core.Sinners.SinnerRoster.PhaseTwo)
            {
                yield return ChapterJourneyTests.LoadWithSave(new ChapterSave
                {
                    ClassId = cls.Id, Seed = 7, Chapter = 1, Coins = 40, Years = 1600, Match = "boss", BossBarStart = 1600, BossBar = 1600, NodeDone = false,
                    Relics = Core.Relics.ChapterRelics.ClassChain(cls.Id).Concat(new[] { Core.Relics.RelicIds.BoneDie }).ToList(), Taxes = 20
                });
                yield return Dealt($"342_boss_{cls.Id}");
            }

            yield return ChapterJourneyTests.LoadWithSave(ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 11, 3, "lucifer"));
            HellPokerScreenshots.Press("PanelOption0");
            yield return new WaitForSeconds(2.5f);
            yield return Dealt("343_lucifer");
        }
    }
}
