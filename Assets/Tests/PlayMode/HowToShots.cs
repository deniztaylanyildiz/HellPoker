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
    /// Demo 2.2: the pictures of How to Play, taken from the game itself. Each scene is set up, drawn at 2560×1440 (×4), brought back to
    /// its own 640×360 pixels (one pixel of every 4×4 block — nothing is smoothed) and cropped to <see cref="Presentation.Views.HowToPlayView.ImageWidth"/>
    /// × ImageHeight; saved to Assets/Resources/Art/HowTo/&lt;page&gt;_&lt;en|tr&gt;.png (the full frames to Screenshots/howto). Not a real
    /// test (Explicit): run it again when the screens change.
    /// </summary>
    [Explicit]
    public class HowToShots
    {
        private const int Scale = 4;
        private static readonly Vector2Int Size = new Vector2Int(Presentation.Ui.PixelScreen.Width * Scale, Presentation.Ui.PixelScreen.Height * Scale);


        /// <summary>The crop of each page's scene (x, y from the top left of the 640×360 screen).</summary>
        private static readonly Dictionary<int, RectInt> Crops = new Dictionary<int, RectInt>
        {
            [0] = new RectInt(0, 0, 256, 144),      // the chapter's card beside the map: the purse, the demon's bar, the sentence, the tribute
            [1] = new RectInt(150, 12, 256, 144),    // the map's first floors
            [2] = new RectInt(384, 0, 256, 144),    // a floor's table: the imp, his purse and ante (Demo 3.2: the demon top right)
            [3] = new RectInt(222, 130, 256, 144),   // a hand at the draw: two cards to throw, DRAW
            [4] = new RectInt(384, 16, 256, 144),   // the demon's bar under him, glowing: the last blow
            [5] = new RectInt(384, 0, 256, 144),    // the cheat announced over the demon, his gauge
            [6] = new RectInt(0, 160, 256, 144),    // Demo 3.2: the player's panel bottom left — the class's portrait, its level, its passive, its relics
            [7] = new RectInt(0, 206, 256, 144),    // the relics on the panel, one hovered (its box opens to the right): its gift and its curse
            [8] = new RectInt(188, 100, 256, 144),  // a market's seller and his wares
            [9] = new RectInt(192, 212, 256, 144),  // Demo 3.4: the difficulty row under the class cards, its words over it
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

        /// <summary>Demo 2.2's screens (160–166), in both languages: the Oracle's card, BEND FATE's arrows and a card brought up, a map
        /// (the markets spaced), the camp, a demon's table at the last blow (the sentence, the bar).</summary>
        [UnityTest, Timeout(600000)]
        public IEnumerator CaptureDemoTwoTwo()
        {
            foreach (string language in new[] { "English", "Turkish" })
            {
                string l = language == "Turkish" ? "tr" : "en";
                yield return Load(language);
                HellPokerScreenshots.Press("NewGameButton");
                yield return new WaitForSeconds(0.8f);
                yield return HellPokerScreenshots.Shot($"160_oracle_card_{l}");

                yield return Load(language, Boss(Core.Sinners.SinnerRoster.Warlock, 1300, 1300));
                yield return Settle();
                ChapterJourneyTests.Chapters.Table.PerformAction();
                yield return Settle(0.8f);
                yield return HellPokerScreenshots.Shot($"161_oracle_foresight_{l}");   // Demo 2.3: BEND FATE is gone (162 with it)

                ChapterSave map = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.King, 31, 2, "");
                map.NodeDone = true;
                yield return Load(language, map);
                yield return new WaitForSeconds(0.4f);
                yield return HellPokerScreenshots.Shot($"163_map_markets_spaced_{l}");

                yield return Load(language, AtNode(Core.Sinners.SinnerRoster.Peasant, 11, NodeKind.PurgatoryFire, "fire"));
                yield return new WaitForSeconds(0.4f);
                yield return HellPokerScreenshots.Shot($"164_camp_{l}");

                yield return Load(language, Boss(Core.Sinners.SinnerRoster.King, 40, 1600));
                yield return Settle(0.8f);
                yield return HellPokerScreenshots.Shot($"165_boss_sentence_bar_last_blow_{l}");
            }
        }

        /// <summary>Demo 2.3's screens (170–178), in both languages: the class cards (power and PASSIVE), every class's badge hover and panel at a
        /// demon's table, the Oracle's four big FORESIGHT cards at the highest level.</summary>
        [UnityTest, Timeout(900000)]
        public IEnumerator CaptureDemoTwoThree()
        {
            foreach (string language in new[] { "English", "Turkish" })
            {
                string l = language == "Turkish" ? "tr" : "en";
                yield return Load(language);
                HellPokerScreenshots.Press("NewGameButton");
                yield return new WaitForSeconds(0.8f);
                yield return HellPokerScreenshots.Shot($"170_class_cards_{l}");

                var classes = Core.Sinners.SinnerRoster.PhaseTwo;
                for (int i = 0; i < classes.Count; i++)
                {
                    yield return Load(language, Boss(classes[i], 1600, 1600));
                    yield return Settle();
                    ChapterJourneyTests.Chapters.Table.PerformAction();   // the deal: the panel at work
                    yield return Settle(0.8f);
                    ShowBadgeHover();
                    yield return null;
                    yield return HellPokerScreenshots.Shot($"{171 + i}_{classes[i].Id}_badge_panel_{l}");
                }

                ChapterSave seer = Boss(Core.Sinners.SinnerRoster.Warlock, 1600, 1600);
                seer.Relics = Core.Relics.ChapterRelics.ClassChain(Core.Sinners.Warlock.ClassId).ToList();
                seer.Upgrades.Add(UpgradeIds.DeepProphecy);
                yield return Load(language, seer);
                yield return Settle();
                ChapterJourneyTests.Chapters.Table.PerformAction();
                yield return Settle(0.8f);
                yield return HellPokerScreenshots.Shot($"177_oracle_four_cards_{l}");
            }
        }

        /// <summary>The class badge's hover box, opened as the pointer would.</summary>
        private static void ShowBadgeHover()
        {
            var badge = Object.FindObjectsByType<Presentation.Views.SinnerBadgeView>(FindObjectsSortMode.None)
                .FirstOrDefault(b => b.isActiveAndEnabled && b.Badge.Visible);
            Transform tip = badge == null ? null : badge.transform.Find("SinnerTip");
            if (tip != null) tip.gameObject.SetActive(true);
        }

        private static IEnumerator Load(string language, ChapterSave save = null)
        {
            HellPokerBootstrap.BatchStore.Clear();
            HellPokerBootstrap.BatchStore.SetString("settings.language", language);
            if (save != null) new Presentation.Settings.ChapterArchive(HellPokerBootstrap.BatchStore).SaveRun(save);
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

            // 7-8 (Demo 3.1.1): the player's panel at a demon's table — the King, his class's relics and the Bone Die; then one hovered.
            ChapterSave panel = Boss(Core.Sinners.SinnerRoster.King, 1600, 1600);
            panel.Relics = Core.Relics.ChapterRelics.ClassChain(Core.Sinners.King.ClassId).Concat(new[] { Core.Relics.RelicIds.BoneDie }).ToList();
            yield return Load(language, panel);
            yield return Settle();
            ChapterJourneyTests.Chapters.Table.PerformAction();
            yield return Settle(0.8f);
            yield return Picture(6);
            ChapterJourneyTests.ChapterTableView.PlayerPanel.Relics.PointAt(2);
            yield return null;
            yield return null;
            yield return Picture(7);

            // 9: a market.
            ChapterSave market = AtNode(Core.Sinners.SinnerRoster.Find(Core.Sinners.Jester.ClassId), 11, NodeKind.BlackMarket, "market");
            market.Vendor = (int)MarketVendor.BoneMerchant;
            yield return Load(language, market);
            yield return new WaitForSeconds(0.4f);
            yield return Picture(8);

            // 10 (Demo 3.4): the class choice's difficulty row (Medium), its words open.
            yield return Load(language);
            HellPokerScreenshots.Press("NewGameButton");
            yield return new WaitForSeconds(0.8f);
            Object.FindFirstObjectByType<Presentation.Views.SinnerSelectView>().PointAtDifficulty();
            yield return null;
            yield return Picture(9);
        }

        /// <summary>The screen at its own pixels, cropped for <paramref name="page"/>; the full frame beside the screenshots.</summary>
        private static IEnumerator Picture(int page)
        {
            Texture2D full = null;
            yield return Grab(t => full = t);
            int nativeW = Presentation.Ui.PixelScreen.Width, nativeH = Presentation.Ui.PixelScreen.Height;
            var native = new Texture2D(nativeW, nativeH, TextureFormat.RGB24, false);
            for (int y = 0; y < nativeH; y++)
                for (int x = 0; x < nativeW; x++)
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
            cut.SetPixels(native.GetPixels(crop.x, nativeH - crop.y - crop.height, crop.width, crop.height));
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
