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

        private static IEnumerator Load(ChapterSave save)
        {
            HellPokerBootstrap.BatchStore.Clear();
            if (System.Environment.GetEnvironmentVariable("HELLPOKER_LANG") == "tr")
                HellPokerBootstrap.BatchStore.SetString("settings.language", "Turkish");
            new Presentation.Settings.ChapterArchive(HellPokerBootstrap.BatchStore).SaveRun(save);
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);
            HellPokerScreenshots.Press("ContinueButton");
            yield return new WaitForSeconds(1f);
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

            yield return Load(ChapterPresenter.PlacedSave(SinnerRoster.King, 11, 1, "loot"));
            yield return HellPokerScreenshots.Shot($"{stage}_loot_mammon_{Tag}");
            yield return Load(ChapterPresenter.PlacedSave(SinnerRoster.King, 11, 2, "loot"));
            yield return HellPokerScreenshots.Shot($"{stage}_loot_belial_{Tag}");
            yield return Load(ChapterPresenter.PlacedSave(SinnerRoster.King, 11, 3, "lucifer"));
            yield return HellPokerScreenshots.Shot($"{stage}_lilith_end_lucifer_calls_{Tag}");

            ChapterSave map = Full(ChapterPresenter.PlacedSave(SinnerRoster.King, 31, 1, ""));
            map.NodeDone = true;
            yield return Load(map);
            HellPokerScreenshots.Press("MapRelicsButton");
            yield return new WaitForSeconds(0.4f);
            yield return HellPokerScreenshots.Shot($"{stage}_bag_{Tag}");
        }
    }
}
