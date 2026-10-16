using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Presentation;
using HellPoker.Presentation.Ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// Demo 2.2: the pictures of How to Play, taken from the game itself. Each scene is set up, drawn at 1920×1080 (×4), brought back to
    /// its own 480×270 pixels (one pixel of every 4×4 block — nothing is smoothed) and cropped to <see cref="Presentation.Views.HowToPlayView.ImageWidth"/>
    /// × ImageHeight; saved to Assets/Resources/Art/HowTo/&lt;page&gt;_&lt;en|tr&gt;.png (the full frames to Screenshots/howto). Not a real
    /// test (Explicit): run it again when the screens change.
    /// </summary>
    [Explicit]
    public class HowToShots
    {
        private const int Scale = 4;
        private static readonly Vector2Int Size = new Vector2Int(480 * Scale, 270 * Scale);

        [TearDown]
        public void BackToTheUsualMenu() => HellPokerBootstrap.ForceSimulator = null;

        /// <summary>The crop of each page's scene (x, y from the top left of the 480×270 screen).</summary>
        private static readonly Dictionary<int, RectInt> Crops = new Dictionary<int, RectInt>
        {
            [0] = new RectInt(0, 0, 200, 112),      // the chapter's card beside the map: the purse, the demon's bar, the sentence, the tribute
            [1] = new RectInt(150, 4, 200, 112),    // the map's first floors
            [2] = new RectInt(0, 0, 200, 112),      // a floor's table: the imp, his purse and ante
            [3] = new RectInt(135, 104, 200, 112),   // a hand at the draw: two cards to throw
            [4] = new RectInt(0, 20, 200, 112),     // the demon's bar under him, glowing: the last blow
            [5] = new RectInt(0, 0, 200, 112),      // the cheat announced over the demon, his gauge
            [6] = new RectInt(44, 28, 200, 112),    // the class cards, the Oracle's in the middle
            [7] = new RectInt(100, 50, 200, 112),   // the relics: worn and in the bag
            [8] = new RectInt(140, 62, 200, 112),   // a market's seller
        };

        private static string LangCode => HellPoker.Presentation.Ui.Lang.Current == Language.Turkish ? "tr" : "en";

        [UnityTest, Timeout(900000)]
        public IEnumerator CaptureHowToPictures()
        {
            foreach (string language in new[] { "English", "Turkish" })
                yield return CaptureAll(language);
        }

        /// <summary>Three pages of How to Play as the player sees them, in both languages (Screenshots/, or HELLPOKER_SHOTS).</summary>
        [UnityTest, Timeout(300000)]
        public IEnumerator CaptureHowToPages()
        {
            foreach (string language in new[] { "English", "Turkish" })
            {
                yield return Load(language);
                HellPokerScreenshots.Press("HowToPlayButton");
                yield return null;
                var menu = Object.FindFirstObjectByType<Presentation.Views.MainMenuView>();
                foreach (int page in new[] { 0, 3, 6, 9 })
                {
                    menu.HowTo.Show(page);
                    yield return null;
                    yield return HellPokerScreenshots.Shot($"howto_{(language == "Turkish" ? "tr" : "en")}_{page + 1:00}");
                }
            }
        }

        private static IEnumerator Load(string language, ChapterSave save = null)
        {
            HellPokerBootstrap.BatchStore.Clear();
            HellPokerBootstrap.BatchStore.SetString("settings.language", language);
            if (save != null) new Presentation.Settings.ChapterArchive(HellPokerBootstrap.BatchStore).SaveRun(save);
            HellPokerBootstrap.ForceSimulator = false;
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);
            if (save != null)
            {
                HellPokerScreenshots.Press("ContinueButton");
                yield return new WaitForSeconds(0.6f);
            }
        }

        private static ChapterSave Boss(Core.Sinners.SinnerClass cls, int bar, int barStart)
        {
            ChapterSave save = ChapterPresenter.PlacedSave(cls, 11, 1, "");
            save.NodeDone = false;
            save.Match = "boss";
            save.BossBarStart = barStart;
            save.BossBar = bar;
            return save;
        }

        private static ChapterSave AtNode(Core.Sinners.SinnerClass cls, int seed, NodeKind kind, string pending)
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

        private static IEnumerator Settle(float seconds = 0.5f)
        {
            yield return ChapterJourneyTests.WaitForChapterTable();
            yield return new WaitForSeconds(seconds);
        }

        private static IEnumerator CaptureAll(string language)
        {
            string[] five = { Core.Relics.RelicIds.FerrymansCoin, Core.Relics.ChapterRelicIds.IronVow, Core.Relics.ChapterRelicIds.BlackBook,
                Core.Relics.ChapterRelicIds.LeadSeal, Core.Relics.ChapterRelicIds.AshPurse };

            // 1-2: a map, a step taken (its chapter card for the goal, its floors for the map).
            ChapterSave map = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 31, 1, "");
            map.NodeDone = true;
            yield return Load(language, map);
            yield return new WaitForSeconds(0.4f);
            yield return Picture(0);
            yield return Picture(1);

            // 3: a floor's table, dealt.
            yield return Load(language);
            HellPokerScreenshots.Press("NewGameButton");
            yield return new WaitForSeconds(0.6f);
            HellPokerScreenshots.ChooseSinner(0);
            yield return new WaitForSeconds(0.6f);
            HellPokerScreenshots.Press("PanelOption0");   // DESCEND
            yield return null;
            MapNode start = ChapterJourneyTests.Chapters.Run.Choices.First(n => n.Kind == NodeKind.Table);
            HellPokerScreenshots.Find<Button>($"Node0_{start.Lane}").onClick.Invoke();
            yield return Settle();
            ChapterJourneyTests.Chapters.Table.PerformAction();
            yield return Settle(0.8f);
            yield return Picture(2);

            // 4: the same hand at the draw, two cards picked to throw.
            var table = (TablePresenter)ChapterJourneyTests.Chapters.Table;
            if (table.Game.Phase == Core.Game.GamePhase.PlayerReveal) table.CheckToDraw();
            yield return Settle(0.8f);
            if (table.Game.Phase == Core.Game.GamePhase.Drawing)
            {
                table.ToggleDiscard(3);
                table.ToggleDiscard(4);
            }
            yield return Settle(0.4f);
            yield return Picture(3);

            // 5: Mammon's table, his bar nearly gone: the last blow.
            yield return Load(language, Boss(Core.Sinners.SinnerRoster.King, 40, 1600));
            yield return Settle(0.8f);
            yield return Picture(4);

            // 6: Mammon's table, his gauge full: the cheat announced over him.
            ChapterSave cheat = Boss(Core.Sinners.SinnerRoster.Peasant, 1300, 1300);
            cheat.Malice = 9;
            yield return Load(language, cheat);
            yield return Settle();
            ChapterJourneyTests.Chapters.Table.PerformAction();
            yield return Settle(0.8f);
            yield return Picture(5);

            // 7: the class cards.
            yield return Load(language);
            HellPokerScreenshots.Press("NewGameButton");
            yield return new WaitForSeconds(0.8f);
            yield return Picture(6);

            // 8: the camp's bag: five worn, three carried.
            ChapterSave camp = AtNode(Core.Sinners.SinnerRoster.Peasant, 11, NodeKind.PurgatoryFire, "fire");
            camp.Relics = five.ToList();
            camp.Bag = new List<string> { Core.Relics.ChapterRelicIds.RustedChain, Core.Relics.ChapterRelicIds.LiarsTongue, Core.Relics.RelicIds.RustyCrown };
            yield return Load(language, camp);
            yield return new WaitForSeconds(0.3f);
            HellPokerScreenshots.Press("PanelOption4");
            yield return new WaitForSeconds(0.3f);
            yield return Picture(7);

            // 9: a market.
            ChapterSave market = AtNode(Core.Sinners.SinnerRoster.Find(Core.Sinners.Jester.ClassId), 11, NodeKind.BlackMarket, "market");
            market.Vendor = (int)MarketVendor.BoneMerchant;
            yield return Load(language, market);
            yield return new WaitForSeconds(0.4f);
            yield return Picture(8);
        }

        /// <summary>The screen at its own pixels, cropped for <paramref name="page"/>; the full frame beside the screenshots.</summary>
        private static IEnumerator Picture(int page)
        {
            Texture2D full = null;
            yield return Grab(t => full = t);
            var native = new Texture2D(480, 270, TextureFormat.RGB24, false);
            for (int y = 0; y < 270; y++)
                for (int x = 0; x < 480; x++)
                    native.SetPixel(x, y, full.GetPixel(x * Scale + Scale / 2, y * Scale + Scale / 2));
            native.Apply();
            Object.Destroy(full);

            string name = UiText.HowToImage(page) + "_" + LangCode;
            string shots = Path.Combine(Application.dataPath, "..", "Screenshots", "howto");
            Directory.CreateDirectory(shots);
            File.WriteAllBytes(Path.Combine(shots, "full_" + name + ".png"), native.EncodeToPNG());

            RectInt crop = Crops[page];
            var cut = new Texture2D(crop.width, crop.height, TextureFormat.RGB24, false);
            // Textures count rows from the bottom; the crop from the top.
            cut.SetPixels(native.GetPixels(crop.x, 270 - crop.y - crop.height, crop.width, crop.height));
            cut.Apply();
            string folder = Path.Combine(Application.dataPath, "Resources", "Art", "HowTo");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), cut.EncodeToPNG());
            Object.Destroy(native);
            Object.Destroy(cut);
        }

        private static IEnumerator Grab(Action<Texture2D> done)
        {
            var target = new RenderTexture(Size.x, Size.y, 24);
            var cameraObject = new GameObject("HowToCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.targetTexture = target;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas).ToList();
            var modes = canvases.Select(c => (canvas: c, mode: c.renderMode)).ToList();
            foreach (Canvas canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 10f;
            }
            yield return null;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(Size.x, Size.y, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Size.x, Size.y), 0, 0);
            texture.Apply();
            RenderTexture.active = null;
            foreach (var (canvas, mode) in modes)
            {
                canvas.renderMode = mode;
                canvas.worldCamera = null;
            }
            Object.Destroy(cameraObject);
            target.Release();
            done(texture);
        }
    }
}
