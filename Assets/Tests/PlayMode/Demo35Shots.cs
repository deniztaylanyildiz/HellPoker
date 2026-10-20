using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;
using HellPoker.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// Demo 3.5's screens (HELLPOKER_SHOTS, HELLPOKER_SHOT_SIZE, HELLPOKER_LANG=tr): the black market's six vendors, a treasure, a warden's
    /// relic, the spoils of Mammon and Belial (Lilith's chapter ends in Lucifer's call: her panel is shot instead), the bag. The same walk
    /// before and after the new scenes (Screenshots/demo35/before, .../after). Not a real test (Explicit).
    /// </summary>
    [Explicit]
    public class Demo35Shots
    {
        private static string Tag => (System.Environment.GetEnvironmentVariable("HELLPOKER_SHOT_SIZE") ?? "1920x1080") + "_" +
                                     (System.Environment.GetEnvironmentVariable("HELLPOKER_LANG") == "tr" ? "tr" : "en");

        /// <summary>Five relics carried and three in the bag: a full panel, a full bag.</summary>
        private static readonly string[] Carried =
            { RelicIds.FerrymansCoin, ChapterRelicIds.IronVow, ChapterRelicIds.BlackBook, ChapterRelicIds.LeadSeal, ChapterRelicIds.AshPurse };
        private static readonly string[] InTheBag = { RelicIds.BoneDie, RelicIds.RustyCrown, RelicIds.ThornedRosary };

        private static IEnumerator Load(ChapterSave save, float settle = 1f)
        {
            HellPokerBootstrap.BatchStore.Clear();
            if (System.Environment.GetEnvironmentVariable("HELLPOKER_LANG") == "tr")
                HellPokerBootstrap.BatchStore.SetString("settings.language", "Turkish");
            new Presentation.Settings.ChapterArchive(HellPokerBootstrap.BatchStore).SaveRun(save);
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);
            HellPokerScreenshots.Press("ContinueButton");
            if (settle > 0f) yield return new WaitForSeconds(settle);
        }

        /// <summary>A save standing on the first chapter's nearest node of <paramref name="kind"/>, its panel waiting.</summary>
        internal static ChapterSave AtNode(SinnerClass cls, int seed, NodeKind kind, string pending)
        {
            ChapterJourney journey = ChapterJourney.Begin(cls, seed);
            ChapterMap map = journey.Run.Map;
            var paths = map.Floors[0].Select(n => new List<MapNode> { n }).ToList();
            List<MapNode> found = null;
            while (paths.Count > 0 && found == null)
            {
                found = paths.FirstOrDefault(p => p.Last().Kind == kind);
                if (found != null) break;
                paths = paths.SelectMany(p => map.NextFrom(p.Last()).Select(n => new List<MapNode>(p) { n })).ToList();
            }
            ChapterSave save = ChapterPresenter.PlacedSave(cls, seed, 1, pending);
            save.Trail = found?.Select(n => n.Lane).ToList() ?? new List<int>();
            save.NodeDone = false;
            save.Coins = 300;
            return save;
        }

        private static ChapterSave Full(ChapterSave save)
        {
            save.Relics = Carried.ToList();
            save.Bag = InTheBag.ToList();
            return save;
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator CaptureTheShopsTheSpoilsAndTheBag()
        {
            string stage = System.Environment.GetEnvironmentVariable("HELLPOKER_STAGE") ?? "before";
            foreach (MarketVendor vendor in System.Enum.GetValues(typeof(MarketVendor)))
            {
                ChapterSave market = AtNode(SinnerRoster.Find(Jester.ClassId), 11, NodeKind.BlackMarket, "market");
                market.Vendor = (int)vendor;
                yield return Load(Full(market));
                yield return HellPokerScreenshots.Shot($"{stage}_market_{vendor.ToString().ToLowerInvariant()}_{Tag}");
            }

            yield return Load(Full(AtNode(SinnerRoster.King, 11, NodeKind.Treasure, "treasure")));
            yield return HellPokerScreenshots.Shot($"{stage}_treasure_{Tag}");

            ChapterSave warden = ChapterPresenter.PlacedSave(SinnerRoster.King, 11, 1, "warden");
            warden.WardenRelic = ChapterRelicIds.IronVow;
            yield return Load(warden);
            yield return HellPokerScreenshots.Shot($"{stage}_warden_relic_{Tag}");

            // The spoils (the scene since Demo 3.5: the deal hurried to its end for the still).
            for (int chapter = 1; chapter <= 2; chapter++)
            {
                yield return Load(BossLootPlayTests.Spoils(chapter));
                Object.FindFirstObjectByType<Presentation.Views.BossLootView>()?.Skip();
                yield return null;
                yield return HellPokerScreenshots.Shot($"{stage}_loot_{(chapter == 1 ? "mammon" : "belial")}_{Tag}");
            }
            yield return Load(ChapterPresenter.PlacedSave(SinnerRoster.King, 11, 3, "lucifer"));
            yield return HellPokerScreenshots.Shot($"{stage}_lilith_end_lucifer_calls_{Tag}");

            ChapterSave map = Full(ChapterPresenter.PlacedSave(SinnerRoster.King, 31, 1, ""));
            map.NodeDone = true;
            yield return Load(map);
            HellPokerScreenshots.Press("MapRelicsButton");
            yield return new WaitForSeconds(0.4f);
            yield return HellPokerScreenshots.Shot($"{stage}_bag_{Tag}");
        }

        /// <summary>The spoils' scene frame by frame (after Demo 3.5): the cards coming in and turning over, the choice playing out.</summary>
        [UnityTest, Timeout(300000)]
        public IEnumerator CaptureTheSpoilsAnimation()
        {
            yield return Load(BossLootPlayTests.Spoils(1, SinnerRoster.Find(Thief.ClassId)), settle: 0f);   // the deal from its first frame
            var loot = Object.FindFirstObjectByType<Presentation.Views.BossLootView>();
            loot.ManualClock = true;   // a frame grab takes longer than a frame: the scene's clock is moved by hand
            float now = 0f;
            int frame = 0;
            foreach (float at in new[] { 0.12f, 0.3f, 0.45f, 0.62f, 0.85f, 1.1f, 1.4f, 1.8f })
            {
                loot.Advance(at - now);
                now = at;
                yield return null;
                yield return HellPokerScreenshots.Shot($"after_loot_anim_{frame++:00}_deal_{Tag}");
            }
            loot.Skip();
            HellPokerScreenshots.Press("LootCard0");
            now = 0f;
            foreach (float at in new[] { 0.12f, 0.3f, 0.5f, 0.7f })
            {
                loot.Advance(at - now);
                now = at;
                yield return null;
                yield return HellPokerScreenshots.Shot($"after_loot_anim_{frame++:00}_choice_{Tag}");
            }
            loot.ManualClock = false;
        }
    }
}
