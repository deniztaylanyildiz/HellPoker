using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>Demo 3.5: the black market's scene in the real game — six vendors in both languages: the words fit, nothing lies over
    /// anything, every place on the screen and the grid, the keys reach everything, a purchase plays out and ends.</summary>
    public class MarketScenePlayTests
    {
        [TearDown]
        public void English() => Lang.Set(Language.English);

        private static IEnumerator Load(ChapterSave save, bool turkish)
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

        internal static ChapterSave Market(MarketVendor vendor, SinnerClass cls = null, int coins = 300)
        {
            ChapterSave save = Demo35Shots.AtNode(cls ?? SinnerRoster.Find(Jester.ClassId), 11, NodeKind.BlackMarket, "market");
            save.Vendor = (int)vendor;
            save.Coins = coins;
            save.Relics = new List<string> { RelicIds.RustyCrown, RelicIds.FerrymansCoin, ChapterRelicIds.AshPurse };
            return save;
        }

        /// <summary>A place's rect on the 640×360 screen (top left): every place lies straight on the scene's full-screen root.</summary>
        private static RectInt OnScreen(RectTransform rect) =>
            new RectInt(Mathf.RoundToInt(rect.anchoredPosition.x), Mathf.RoundToInt(-rect.anchoredPosition.y), Mathf.RoundToInt(rect.sizeDelta.x),
                Mathf.RoundToInt(rect.sizeDelta.y));

        [UnityTest, Timeout(600000)]
        public IEnumerator EveryVendor_InBothLanguages_FitsTheGrid_NothingOverlaps_TheKeysReachEverything()
        {
            foreach (bool turkish in new[] { false, true })
            foreach (MarketVendor vendor in System.Enum.GetValues(typeof(MarketVendor)))
            {
                string what = vendor + (turkish ? " (tr)" : " (en)");
                yield return Load(Market(vendor, vendor == MarketVendor.Tailor ? SinnerRoster.King : null), turkish);
                var view = Object.FindFirstObjectByType<MarketView>();
                Assert.IsNotNull(view, what);
                Assert.IsTrue(view.IsOpen, what + ": the scene");
                Assert.IsTrue(ChapterJourneyTests.Chapters.IsMarketOpen, what);
                Canvas.ForceUpdateCanvases();
                MarketScene scene = view.Scene;

                // The words fit: the title, the purse, the relics, the name, the bubble, every tile's name and price.
                Assert.LessOrEqual(view.Title.preferredWidth, view.Title.rectTransform.rect.width + 1, what + ": the title");
                Assert.LessOrEqual(view.Relics.preferredWidth, view.Relics.rectTransform.rect.width, what + ": the relics");
                Assert.LessOrEqual(view.Owner.preferredWidth, view.Owner.rectTransform.rect.width, what + ": the vendor's name");
                Assert.LessOrEqual(view.Line.preferredHeight, view.Line.rectTransform.rect.height, what + ": the bubble's words");
                Assert.IsNotNull(view.Portrait.sprite, what + ": the vendor's portrait");
                Assert.AreEqual("market_" + scene.VendorId, view.Hall.ShownDealerId, what + ": his stall");
                foreach (int tile in view.TilePlaces)
                {
                    Assert.LessOrEqual(view.TileName(tile).preferredWidth, MarketView.TileWidth - 28, $"{what}: the tile '{view.TileName(tile).text}'");
                    Assert.LessOrEqual(view.TilePrice(tile).preferredWidth, MarketView.TileWidth - 28, $"{what}: the price '{view.TilePrice(tile).text}'");
                }

                // Every place on the screen and the 8 px grid, none over another, none over the bubble or the portrait.
                var rects = new List<(string name, RectInt rect)>();
                for (int i = 0; i < scene.Slots.Count; i++)
                {
                    RectInt r = OnScreen(view.PlaceOf(i));
                    Assert.That(r.xMin >= 0 && r.yMin >= 0 && r.xMax <= PixelScreen.Width && r.yMax <= PixelScreen.Height, $"{what}: place {i} on the screen ({r})");
                    if (scene.Slots[i].Kind != MarketSlotKind.Leave) Assert.AreEqual(0, r.xMin % 8, $"{what}: place {i} on the grid ({r})");
                    rects.Add(("place " + i, r));
                }
                rects.Add(("the bubble", new RectInt(MarketView.BubbleX, MarketView.BubbleY, MarketView.BubbleWidth, PixelScreen.Height - 24 - MarketView.BubbleY)));
                rects.Add(("the portrait", new RectInt(60, 36, MarketView.PortraitSize + 8, MarketView.PortraitSize + 8)));
                for (int a = 0; a < rects.Count; a++)
                    for (int b = a + 1; b < rects.Count; b++)
                        Assert.IsFalse(rects[a].rect.Overlaps(rects[b].rect), $"{what}: {rects[a].name} {rects[a].rect} over {rects[b].name} {rects[b].rect}");

                // The keys: every place reached from where they start.
                var reached = new HashSet<int> { scene.Selected };
                var frontier = new Queue<int>(reached);
                while (frontier.Count > 0)
                {
                    int from = frontier.Dequeue();
                    foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                        if (reached.Add(scene.Move(from, dx, dy))) frontier.Enqueue(scene.Move(from, dx, dy));
                }
                Assert.AreEqual(scene.Slots.Count, reached.Count, what + ": the keys reach every place");
                Assert.IsFalse(Object.FindObjectsByType<HoverTip>(FindObjectsSortMode.None).Any(tip => tip.Visible), what + ": no hover box while nothing is pointed at");
            }
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator APurchase_PlaysOut_AndEnds_TheCardStampedSold()
        {
            yield return Load(Market(MarketVendor.Smuggler), false);
            var view = Object.FindFirstObjectByType<MarketView>();
            int index = Enumerable.Range(0, view.Scene.Slots.Count).First(i => view.Scene.Slots[i].Kind == MarketSlotKind.Stall && view.Scene.Slots[i].Enabled);
            int coins = view.Scene.Coins;
            HellPokerScreenshots.Press("PanelOption" + index);
            yield return null;
            Assert.IsTrue(view.IsAnimating, "the coins fly");
            float started = Time.time;
            while (view.IsAnimating && Time.time - started < 4f) yield return null;
            Assert.IsFalse(view.IsAnimating, "the moment ends on its own");
            Assert.Less(view.Scene.Coins, coins);
            Assert.AreEqual(view.Scene.Coins.ToString(), view.Purse.text);
            Assert.IsTrue(view.Cards[index].StampShown, "SOLD");
            Assert.AreEqual(OnScreen(view.PlaceOf(index)).position, OnScreen(view.PlaceOf(index)).position / 8 * 8, "back in its place");

            // Esc at the stall: the way out.
            ChapterJourneyTests.Chapters.Back();
            Assert.IsFalse(view.IsOpen, "left");
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator ThePick_ShowsTheCarriedRelics_AndBackReturns()
        {
            yield return Load(Market(MarketVendor.Pawnshop), true);
            var view = Object.FindFirstObjectByType<MarketView>();
            int sell = Enumerable.Range(0, view.Scene.Slots.Count).First(i => view.Scene.Slots[i].Kind == MarketSlotKind.Service && view.Scene.Slots[i].Service == MarketService.Sell);
            HellPokerScreenshots.Press("PanelOption" + sell);
            yield return null;
            Assert.IsTrue(view.Scene.Picking);
            Assert.AreEqual(3, view.Scene.Slots.Count(s => s.Kind == MarketSlotKind.Pick));
            Canvas.ForceUpdateCanvases();
            for (int i = 0; i < view.Scene.Slots.Count; i++)
                if (view.Scene.Slots[i].Kind != MarketSlotKind.Leave) Assert.AreEqual(0, OnScreen(view.PlaceOf(i)).xMin % 8, $"place {i} on the grid");
            HellPokerScreenshots.Press("PanelOption" + (view.Scene.Slots.Count - 1));
            yield return null;
            Assert.IsFalse(view.Scene.Picking, "BACK: the stall again");
        }
    }
}
