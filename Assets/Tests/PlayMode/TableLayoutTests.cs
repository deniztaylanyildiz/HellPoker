using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Core.Evaluation;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Settings;
using HellPoker.Presentation.Ui;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// 2.1.1: nothing on a chapter's table is written over anything else. Every visible text's glyphs (the deck counter, both captions, the
    /// message's lines, the pot, the ante, the outlook, the power's line, the result band) and every button are walked; no two texts
    /// overlap and no text runs under a button that is not its own — in both languages, at a floor's table and a demon's, before the
    /// deal, in a hand, at the result, with the longest words (a three-line message, a four-line one folded, the longest hand names).
    /// </summary>
    public class TableLayoutTests
    {
        [TearDown]
        public void BackToTheUsualMenu() => HellPokerBootstrap.ForceSimulator = null;

        [UnityTest, Timeout(240000)]
        public IEnumerator NothingOverlaps_AtTheTables_InEnglish() => Walk(false);

        [UnityTest, Timeout(240000)]
        public IEnumerator NothingOverlaps_AtTheTables_InTurkish() => Walk(true);

        private static IEnumerator Load(bool turkish, ChapterSave save)
        {
            HellPokerBootstrap.BatchStore.Clear();
            HellPokerBootstrap.BatchStore.SetString("settings.language", turkish ? "Turkish" : "English");
            if (save != null) new ChapterArchive(HellPokerBootstrap.BatchStore).SaveRun(save);
            HellPokerBootstrap.ForceSimulator = false;
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.5f);
            if (save != null)
            {
                HellPokerScreenshots.Press("ContinueButton");
                yield return new WaitForSeconds(0.6f);
            }
        }

        private static IEnumerator Settle()
        {
            yield return ChapterJourneyTests.WaitForChapterTable();
            TableView table = ChapterJourneyTests.ChapterTableView;
            if (table != null && table.IsBusy) table.SkipAnimations();
            yield return null;
            yield return null;
        }

        /// <summary>The longest name a hand has in the language on show.</summary>
        private static string LongestHand() =>
            Enum.GetValues(typeof(HandCategory)).Cast<HandCategory>().Select(UiText.CategoryName).OrderByDescending(n => n.Length).First();

        private IEnumerator Walk(bool turkish)
        {
            // A demon's table: the King at Mammon's, before the deal (the ante and DEAL), in a hand, at a result.
            yield return Load(turkish, new ChapterSave
            {
                ClassId = Core.Sinners.King.ClassId, Seed = 7, Chapter = 1, Coins = 40, Years = 1600, Match = "boss", BossBarStart = 1600,
                BossBar = 1600, NodeDone = false
            });
            yield return Settle();
            TableView view = ChapterJourneyTests.ChapterTableView;
            Assert.IsNotNull(view, "the chapter's table");
            Assert.IsTrue(ChapterJourneyTests.Chapters.IsAtTable);
            AssertNothingOverlaps(view, "boss, before the deal");
            view.SetAnte(99);
            yield return Settle();
            AssertNothingOverlaps(view, "boss, before the deal, the widest ante");
            Assert.AreEqual("", view.DeckCountText, "Phase 3: no deck counter at a chapter's table");

            yield return Hand(view, coins: false);

            // A floor's table: an imp's, for coins.
            yield return Load(turkish, null);
            HellPokerScreenshots.Press("NewGameButton");
            yield return new WaitForSeconds(0.6f);
            HellPokerScreenshots.ChooseSinner(0);
            yield return new WaitForSeconds(0.6f);
            HellPokerScreenshots.Press("PanelOption0");   // DESCEND
            yield return null;
            MapNode start = ChapterJourneyTests.Chapters.Run.Choices.First(n => n.Kind == NodeKind.Table);
            HellPokerScreenshots.Find<Button>($"Node0_{start.Lane}").onClick.Invoke();
            yield return Settle();
            view = ChapterJourneyTests.ChapterTableView;
            AssertNothingOverlaps(view, "floor, before the deal");
            yield return Hand(view, coins: true);
        }

        /// <summary>A hand dealt and played to its end, the table checked at every step and with the longest words forced on it.</summary>
        private IEnumerator Hand(TableView view, bool coins)
        {
            string where = coins ? "floor" : "boss";
            ITableCommands table = ChapterJourneyTests.Chapters.Table;
            table.PerformAction();   // the deal
            yield return Settle();
            AssertNothingOverlaps(view, where + ", the first decision");

            // The longest words: three lines of message, the pot, the outlook, both captions as long as they get.
            string longest = LongestHand();
            view.SetMessage(Long(1) + "\n" + Long(2) + "\n" + Long(3), Tone.Neutral);
            view.SetPot(99);
            view.SetStakeInfo(string.Format(coins ? UiText.CoinStakeInfoFormat : UiText.BarStakeInfoFormat, 999, 999));
            view.House.SetCaption(string.Format(UiText.WinnerFormat, string.Format(UiText.HouseDrewFormat, longest, 3)), Tone.Bad);
            view.Player.SetCaption(string.Format(UiText.WinnerFormat, longest), Tone.Triumph);
            yield return Settle();
            AssertNothingOverlaps(view, where + ", a three-line message and the longest captions");
            Assert.AreEqual(3, view.MessageShown.Split('\n').Length);

            // Played to its end (passing).
            int guard = 0;
            while (table is TablePresenter presenter && presenter.Game.Phase != Core.Game.GamePhase.RoundOver && !presenter.Game.IsGameOver && guard++ < 20)
            {
                table.PerformAction();
                yield return Settle();
                AssertNothingOverlaps(view, where + ", phase " + presenter.Game.Phase);
            }
            AssertNothingOverlaps(view, where + ", the result");
            Assert.IsNotNull(view.Band, "the result band");

            // The result with four lines of message (folded into two under the band) and the longest band.
            view.SetResultBand(new ResultBand(HandOutcome.Loss, UiText.BandTitle(HandOutcome.Loss),
                coins ? string.Format(UiText.BandCoinsFormat, "−999") : UiText.BarSentenceLines(99, 9999)));
            view.SetMessage(Long(1) + "\n" + Long(2) + "\n" + Long(3) + "\n" + Long(4), Tone.Bad);
            view.House.SetCaption(string.Format(UiText.WinnerFormat, string.Format(UiText.HouseDrewFormat, longest, 3)), Tone.Bad);
            yield return Settle();
            AssertNothingOverlaps(view, where + ", the band and a four-line message");
            Assert.LessOrEqual(view.MessageShown.Split('\n').Length, 2, "under the band the message keeps to two lines");
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator EveryClassesPanel_AtItsLargest_KeepsOffTheHandAndTheWords()
        {
            foreach (bool turkish in new[] { false, true })
            foreach (Core.Sinners.SinnerClass cls in Core.Sinners.SinnerRoster.PhaseTwo)
            {
                // The power at its highest level (the class's three relics and its step upgrade): four cards for FORESIGHT and the satchel.
                string step = ClassKit.StepUpgradeOf(cls.Id);
                var save = new ChapterSave
                {
                    ClassId = cls.Id, Seed = 7, Chapter = 1, Coins = 40, Years = 1600, Match = "boss", BossBarStart = 1600, BossBar = 1600, NodeDone = false,
                    Relics = Core.Relics.ChapterRelics.ClassChain(cls.Id).ToList(), Taxes = 20,
                    Satchel = Core.Cards.CardCodes.TryParseDeck("AS,KH,QD,JC").ToList()
                };
                if (step != null) save.Upgrades.Add(step);
                yield return Load(turkish, save);
                yield return Settle();
                TableView view = ChapterJourneyTests.ChapterTableView;
                ChapterJourneyTests.Chapters.Table.PerformAction();   // the deal: FORESIGHT opens
                yield return Settle();
                string where = cls.Id + (turkish ? " (tr)" : " (en)");
                AssertNothingOverlaps(view, where + ", the class panel in a hand");
                AssertPanelKeepsClear(view, where);
            }
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TheGamesClassCards_TheirWordsFitAboveTheCircleAndChoose_InBothLanguages()
        {
            foreach (bool turkish in new[] { false, true })
            {
                yield return Load(turkish, null);
                HellPokerScreenshots.Press("NewGameButton");
                yield return new WaitForSeconds(0.6f);
                var classes = Core.Sinners.SinnerRoster.PhaseTwo;
                for (int index = 0; index < classes.Count; index++)
                {
                    string id = classes[index].Id, what = $"{id} ({(turkish ? "tr" : "en")})";
                    Transform card = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                        .Single(t => t.name == "Sinner_" + id);
                    var ability = (RectTransform)card.Find("Ability");
                    var detail = (RectTransform)card.Find("Detail");
                    var choose = (RectTransform)card.Find("ChooseSinner" + index);
                    var circle = (RectTransform)card.Find("Circle");
                    float Top(RectTransform r) => -r.anchoredPosition.y;
                    float Bottom(RectTransform r) => Top(r) + r.rect.height;
                    Assert.LessOrEqual(ability.GetComponent<Text>().preferredHeight, ability.rect.height + 0.5f, $"{what}: the power fits its box");
                    Assert.LessOrEqual(detail.GetComponent<Text>().preferredHeight, detail.rect.height + 0.5f, $"{what}: the price fits its box");
                    Assert.GreaterOrEqual(Top(detail), Bottom(ability), $"{what}: the price under the power");
                    Assert.LessOrEqual(Bottom(detail), circle != null ? Top(circle) : Top(choose), $"{what}: the words end above the circle's row");
                    Assert.LessOrEqual(Bottom(detail), Top(choose), $"{what}: and above CHOOSE");

                    // Demo 2.3.1: the hover box holds the whole text, on screen.
                    var select = UnityEngine.Object.FindFirstObjectByType<SinnerSelectView>();
                    select.ShowTip(UiText.ChapterSinnerFull(id) + "\n" + UiText.ChapterSinnerDetailFull(id), (int)((RectTransform)card).anchoredPosition.x);
                    yield return null;
                    Assert.IsTrue(select.TipShown, what + ": the hover box opens");
                    var tip = (RectTransform)select.transform.GetComponentsInChildren<Transform>(true).First(t => t.name == "SinnerCardTip");
                    Text tipText = tip.GetComponentInChildren<Text>();
                    Assert.LessOrEqual(tipText.preferredHeight, tipText.rectTransform.rect.height + 0.5f, what + ": the whole text fits its box");
                    Assert.GreaterOrEqual(tip.anchoredPosition.x, 0f, what + ": the box on screen (left)");
                    Assert.LessOrEqual(tip.anchoredPosition.x + tip.rect.width, PixelScreen.Width, what + ": on screen (right)");
                    Assert.LessOrEqual(-tip.anchoredPosition.y + tip.rect.height, PixelScreen.Height, what + ": on screen (bottom)");
                    if (UiText.ChapterPassiveLine(id) != null) StringAssert.Contains(turkish ? "PASİF:" : "PASSIVE:", select.TipText, what);
                    select.HideTip();
                }
            }
        }

        // ------------------------------------------------------------------ Phase 3: the hover boxes, the payouts in the hands' panel

        [UnityTest, Timeout(300000)]
        public IEnumerator EveryHoverBox_IsOverEverythingAndOnScreen_InBothLanguages()
        {
            foreach (bool turkish in new[] { false, true })
            {
                string lang = turkish ? " (tr)" : " (en)";
                // A demon's table with relics, the class panel, the bar, the malice: the King at Mammon's, in a hand.
                yield return Load(turkish, new ChapterSave
                {
                    ClassId = Core.Sinners.King.ClassId, Seed = 7, Chapter = 1, Coins = 40, Years = 1600, Match = "boss", BossBarStart = 1600,
                    BossBar = 1600, NodeDone = false, Relics = Core.Relics.ChapterRelics.ClassChain(Core.Sinners.King.ClassId).ToList(), Taxes = 20
                });
                yield return Settle();
                TableView view = ChapterJourneyTests.ChapterTableView;
                ChapterJourneyTests.Chapters.Table.PerformAction();
                yield return Settle();
                int opened = 0;
                yield return HoverAll(view.transform, "boss" + lang, n => opened += n);

                // A floor's table after a hand (the last hands' pips under the coins), the map before it, the class cards before that.
                yield return Load(turkish, null);
                HellPokerScreenshots.Press("NewGameButton");
                yield return new WaitForSeconds(0.6f);
                yield return HoverAll(UnityEngine.Object.FindFirstObjectByType<SinnerSelectView>().transform, "class cards" + lang, n => opened += n);
                HellPokerScreenshots.ChooseSinner(0);
                yield return new WaitForSeconds(0.6f);
                HellPokerScreenshots.Press("PanelOption0");   // DESCEND
                yield return null;
                yield return HoverAll(UnityEngine.Object.FindFirstObjectByType<ChapterMapView>().transform, "map" + lang, n => opened += n);
                MapNode start = ChapterJourneyTests.Chapters.Run.Choices.First(n => n.Kind == NodeKind.Table);
                HellPokerScreenshots.Find<Button>($"Node0_{start.Lane}").onClick.Invoke();
                yield return Settle();
                view = ChapterJourneyTests.ChapterTableView;
                ITableCommands table = ChapterJourneyTests.Chapters.Table;
                int guard = 0;
                table.PerformAction();
                yield return Settle();
                while (table is TablePresenter presenter && presenter.Game.Phase != Core.Game.GamePhase.RoundOver && guard++ < 20)
                {
                    table.PerformAction();
                    yield return Settle();
                }
                yield return HoverAll(view.transform, "floor" + lang, n => opened += n);
                Assert.Greater(opened, 8, "the hovers were really opened" + lang);
            }
        }

        /// <summary>Phase 3: the 640×360 grid — a whole-number scale at every common size, and nothing of a chapter's table outside the screen.</summary>
        [UnityTest, Timeout(120000)]
        public IEnumerator TheTable_IsLaidOutOnThe640By360Grid()
        {
            Assert.AreEqual(640, PixelScreen.Width);
            Assert.AreEqual(360, PixelScreen.Height);
            foreach (var (w, h, scale) in new[] { (1920, 1080, 3), (2560, 1440, 4), (3840, 2160, 6), (1366, 768, 2), (1280, 720, 2) })
            {
                Assert.AreEqual(scale, PixelScreen.ScaleFor(w, h), $"{w}×{h}");
                if (w % PixelScreen.Width == 0 && h % PixelScreen.Height == 0)
                    Assert.AreEqual((float)scale, PixelScreen.FillScaleFor(w, h), 0.0001f, $"{w}×{h}: the screen filled exactly, no bars");
            }

            yield return Load(false, new ChapterSave
            {
                ClassId = Core.Sinners.King.ClassId, Seed = 7, Chapter = 1, Coins = 40, Years = 1600, Match = "boss", BossBarStart = 1600,
                BossBar = 1600, NodeDone = false, Relics = Core.Relics.ChapterRelics.ClassChain(Core.Sinners.King.ClassId).ToList(), Taxes = 20
            });
            yield return Settle();
            TableView view = ChapterJourneyTests.ChapterTableView;
            ChapterJourneyTests.Chapters.Table.PerformAction();
            yield return Settle();
            Canvas.ForceUpdateCanvases();
            Rect area = Box(HoverTip.ScreenOf(view.Kit.transform));
            var outside = new List<string>();
            foreach (Graphic g in view.GetComponentsInChildren<Graphic>(false).Where(g => g.isActiveAndEnabled && g.color.a > 0.01f))
            {
                if (g is Text t && string.IsNullOrWhiteSpace(t.text)) continue;
                if (g.name.StartsWith("ResultEdge") || g.name == "Letterbox" || Under(g.transform, "Salon") || Under(g.transform, "Backdrop")) continue;
                Rect box = g is Text text ? Glyphs(text) : Box(g.rectTransform);
                if (box.width <= 0f) continue;
                if (box.xMin < area.xMin - 0.5f || box.xMax > area.xMax + 0.5f || box.yMin < area.yMin - 0.5f || box.yMax > area.yMax + 0.5f)
                    outside.Add(g.name);
            }
            Assert.IsEmpty(outside, "off the 640×360 screen:\n" + string.Join("\n", outside));
        }

        /// <summary>Points at every hover of <paramref name="root"/> in turn: each box that opens draws over its whole screen and stays inside it.</summary>
        private static IEnumerator HoverAll(Transform root, string where, Action<int> opened)
        {
            var problems = new List<string>();
            int count = 0;
            var triggers = root.GetComponentsInChildren<UnityEngine.EventSystems.EventTrigger>(false).Where(t => t.isActiveAndEnabled).ToList();
            foreach (var trigger in triggers)
            {
                var data = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                UnityEngine.EventSystems.ExecuteEvents.Execute(trigger.gameObject, data, UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
                yield return null;   // the box keeps itself on screen in its LateUpdate
                yield return null;
                Canvas.ForceUpdateCanvases();
                foreach (HoverTip tip in UnityEngine.Object.FindObjectsByType<HoverTip>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    count++;
                    Canvas rootCanvas = tip.GetComponent<Canvas>().rootCanvas;
                    int highest = rootCanvas.GetComponentsInChildren<Canvas>(false).Where(c => c.GetComponent<HoverTip>() == null && c.overrideSorting)
                        .Select(c => c.sortingOrder).DefaultIfEmpty(rootCanvas.sortingOrder).Max();
                    if (tip.SortingOrder <= Math.Max(highest, rootCanvas.sortingOrder))
                        problems.Add($"{tip.name} (from {trigger.name}) under its screen ({tip.SortingOrder} ≤ {highest})");
                    RectTransform screen = HoverTip.ScreenOf(tip.transform);
                    Rect area = Box(screen), box = Box((RectTransform)tip.transform);
                    if (box.xMin < area.xMin - 0.5f || box.xMax > area.xMax + 0.5f || box.yMin < area.yMin - 0.5f || box.yMax > area.yMax + 0.5f)
                        problems.Add($"{tip.name} (from {trigger.name}) off the screen");
                    Text text = tip.GetComponentInChildren<Text>();
                    if (text != null && text.preferredHeight > ((RectTransform)tip.transform).rect.height + 1f)
                        problems.Add($"{tip.name} (from {trigger.name}): its words run out of the box");
                }
                UnityEngine.EventSystems.ExecuteEvents.Execute(trigger.gameObject, data, UnityEngine.EventSystems.ExecuteEvents.pointerExitHandler);
            }
            opened(count);
            Assert.IsEmpty(problems, where + ":\n" + string.Join("\n", problems));
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator TheHandsPanel_HoldsTheTablesPayouts_AndFits_InBothLanguages()
        {
            foreach (bool turkish in new[] { false, true })
            {
                yield return Load(turkish, new ChapterSave
                {
                    ClassId = Core.Sinners.King.ClassId, Seed = 7, Chapter = 1, Coins = 40, Years = 1600, Match = "boss", BossBarStart = 1600,
                    BossBar = 1600, NodeDone = false
                });
                yield return Settle();
                TableView view = ChapterJourneyTests.ChapterTableView;
                string what = turkish ? "tr" : "en";
                Assert.IsFalse(view.PayoutTableShown, what + ": no payouts panel at a chapter's table");
                Assert.IsFalse(view.transform.GetComponentsInChildren<Transform>(false).Any(t => t.name == "Payouts"), what + ": nothing of it on show");
                Text hands = view.transform.GetComponentsInChildren<Button>(false).Single(b => b.name == "HandsButton").GetComponentInChildren<Text>();
                Assert.AreEqual(UiText.HandsPayoutsButton, hands.text);
                Assert.LessOrEqual(hands.preferredWidth, hands.rectTransform.rect.width + 0.5f, what + ": HANDS / PAYOUTS fits its button");

                ChapterJourneyTests.Chapters.Table.ToggleHandRanks();
                yield return null;
                Canvas.ForceUpdateCanvases();
                HandRanksPanel panel = view.HandRanks;
                Assert.IsTrue(panel.IsOpen, what + ": H opens it");
                var categories = Enum.GetValues(typeof(HandCategory)).Cast<HandCategory>().Reverse().ToArray();
                var mammon = Core.Dealers.DealerRoster.Mammon.Payouts;
                for (int i = 0; i < categories.Length; i++)
                    Assert.AreEqual(mammon.IsAbsolution(categories[i]) ? UiText.Absolution : "×" + mammon.GetMultiplier(categories[i]), panel.Values[i],
                        what + ": " + categories[i] + " as Mammon's table pays it");
                StringAssert.Contains(UiText.PayoutNoteAnte, panel.NotesShown);
                var notes = (RectTransform)panel.transform.Find("Notes");
                Assert.LessOrEqual(notes.GetComponent<Text>().preferredHeight, notes.rect.height + 0.5f, what + ": the notes fit");
                foreach (Text line in panel.GetComponentsInChildren<Text>(false).Where(t => !string.IsNullOrEmpty(t.text)))
                    Assert.LessOrEqual(line.preferredWidth, line.rectTransform.rect.width + 1f, $"{what}: '{line.text}' fits its line");
                Rect area = Box(HoverTip.ScreenOf(panel.transform)), box = Box((RectTransform)panel.transform);
                Assert.GreaterOrEqual(box.yMin, area.yMin - 0.5f, what + ": the panel on screen");
                ChapterJourneyTests.Chapters.Table.ToggleHandRanks();
                yield return null;
                Assert.IsFalse(panel.IsOpen, what + ": H closes it");
            }
        }

        // ------------------------------------------------------------------ Demo 3.1: the player's panel

        [UnityTest, Timeout(480000)]
        public IEnumerator ThePlayersPanel_StandsUnderTheCounter_ClearOfEverything_AndTheDemonsPortraitIsClean_InBothLanguages()
        {
            foreach (bool turkish in new[] { false, true })
            foreach (Core.Sinners.SinnerClass cls in Core.Sinners.SinnerRoster.PhaseTwo)
            {
                // The class's three relics, the Bone Die and the step upgrade: a full panel.
                var relics = Core.Relics.ChapterRelics.ClassChain(cls.Id).ToList();
                relics.Add(Core.Relics.RelicIds.BoneDie);
                string step = ClassKit.StepUpgradeOf(cls.Id);
                var save = new ChapterSave
                {
                    ClassId = cls.Id, Seed = 7, Chapter = 1, Coins = 40, Years = 1600, Match = "boss", BossBarStart = 1600, BossBar = 1600, NodeDone = false,
                    Relics = relics, Taxes = 20
                };
                if (step != null) save.Upgrades.Add(step);
                yield return Load(turkish, save);
                yield return Settle();
                TableView view = ChapterJourneyTests.ChapterTableView;
                string where = cls.Id + (turkish ? " (tr)" : " (en)");
                PlayerPanelView panel = view.PlayerPanel;

                // Where it stands: in the right column under the counter, the badge and the relics gone from the demon's portrait.
                Assert.IsTrue(panel.gameObject.activeInHierarchy, where + ": the panel at a chapter's table");
                Assert.IsFalse(view.PortraitBadge.gameObject.activeInHierarchy, where + ": no class badge on the demon's portrait");
                Assert.IsFalse(view.PortraitRelics.gameObject.activeInHierarchy, where + ": no relics on the demon's portrait");
                Assert.AreEqual(cls.Id, panel.Badge.ClassId, where);
                Assert.AreEqual(relics.Count, panel.Relics.Relics.Count, where + ": every worn relic on the panel");
                Assert.AreEqual(8, panel.Sinner.Portrait.Clip?.Frames.Length, where + ": the class's idle loop plays (Sinners/<id>/idle)");
                Assert.AreEqual(new Vector2(SinnerView.Size, SinnerView.Size), ((RectTransform)panel.Sinner.Portrait.transform).sizeDelta, where + ": at 64, its own pixels");

                ChapterJourneyTests.Chapters.Table.PerformAction();   // the deal: the bet row, the class's panel at work
                yield return Settle();
                Canvas.ForceUpdateCanvases();
                Rect box = Box((RectTransform)panel.transform), screen = Box(HoverTip.ScreenOf(panel.transform));
                Rect counter = Box((RectTransform)view.transform.GetComponentsInChildren<Transform>(false).First(t => t.name == "Sentence"));
                Assert.IsTrue(box.xMax <= screen.xMax + 0.5f && box.yMin >= screen.yMin - 0.5f, where + ": on the 640×360 screen");
                Assert.LessOrEqual(box.yMax, counter.yMin + 0.5f, where + ": under the counter");
                AssertNothingOverlaps(view, where + ", the panel in a hand");

                // Nothing else on it: no text and no button outside the panel touches it (the bet row, the counter, the hints).
                var problems = new List<string>();
                foreach (Text text in view.GetComponentsInChildren<Text>(false).Where(t => t.isActiveAndEnabled && !string.IsNullOrWhiteSpace(t.text)
                             && t.color.a > 0.01f && !Under(t.transform, "PlayerPanel") && t.name != "ResultFloat" && t.name != "HandFlare"))
                    if (Overlaps(box, Glyphs(text))) problems.Add($"the words '{Short(text)}' ({text.name})");
                foreach (Button button in view.GetComponentsInChildren<Button>(false).Where(b => b.isActiveAndEnabled && !Under(b.transform, "PlayerPanel")))
                    if (Overlaps(box, Box((RectTransform)button.transform))) problems.Add("the button " + button.name);
                Assert.IsEmpty(problems, where + ": over the player's panel\n" + string.Join("\n", problems));

                // Its words fit: the class's name on its line, the passive's name in its row.
                Assert.LessOrEqual(panel.ClassName.preferredWidth, PlayerPanelView.Width - 4, where + ": the class's name fits");
                Text passive = panel.PassiveName;
                Assert.IsNotEmpty(passive.text, where + ": the passive's row");
                Assert.LessOrEqual(passive.preferredHeight, passive.rectTransform.rect.height + 0.5f, $"{where}: '{passive.text}' fits its row");

                // The relics answer as they did on the portrait: the hover opens on screen, left of the tiles; the Bone Die picks a card.
                panel.Relics.PointAt(0);
                yield return null;
                yield return null;
                HoverTip tip = panel.Relics.GetComponentsInChildren<HoverTip>(false).SingleOrDefault();
                Assert.IsNotNull(tip, where + ": a relic's hover box");
                Assert.IsTrue(tip.Visible, where + ": drawn");
                Rect tipBox = Box((RectTransform)tip.transform);
                Rect tile = Box(panel.Relics.TileOf(0));
                Assert.IsTrue(tipBox.xMin >= screen.xMin - 0.5f && tipBox.xMax <= tile.xMin + 0.5f, where + ": the relic's box on screen, left of its tile");
                int die = relics.IndexOf(Core.Relics.RelicIds.BoneDie);
                panel.Relics.Press(die);
                yield return Settle();
                Assert.IsTrue(panel.Relics.Relics.First(r => r.Id == Core.Relics.RelicIds.BoneDie).Selecting, where + ": the Bone Die picks a card from the panel");
                panel.Relics.Press(die);   // put away again
                yield return Settle();

                // The portrait and the passive's row do what K does.
                panel.PressPortrait();
                yield return Settle();
                if (cls.Id != Core.Sinners.Thief.ClassId)
                    StringAssert.StartsWith(turkish ? "Güçlerin" : "Your powers", view.MessageShown, where + ": the portrait tells the passive");
                panel.PressPassive();
                yield return Settle();
                if (cls.Id != Core.Sinners.Thief.ClassId)
                    StringAssert.StartsWith(turkish ? "Güçlerin" : "Your powers", view.MessageShown, where + ": so does the passive's row");
            }
        }

        /// <summary>The class panel's frame touches no card of the player's hand, no text and no button outside it.</summary>
        internal static void AssertPanelKeepsClear(TableView view, string where)
        {
            Canvas.ForceUpdateCanvases();
            KitView kit = view.GetComponentInChildren<KitView>(false);
            if (kit == null || kit.Shown.IsHidden) return;
            Rect panel = Box((RectTransform)kit.transform.Find("Frame"));
            var problems = new List<string>();
            Transform hand = view.transform.GetComponentsInChildren<Transform>(false).FirstOrDefault(t => t.name == "PlayerHand");
            if (hand != null)
                foreach (Image card in hand.GetComponentsInChildren<Image>(false).Where(i => i.isActiveAndEnabled && i.color.a > 0.01f))
                    if (Overlaps(panel, Box(card.rectTransform))) problems.Add("a card of the hand: " + card.name);
            foreach (Text text in view.GetComponentsInChildren<Text>(false).Where(t => t.isActiveAndEnabled && !string.IsNullOrWhiteSpace(t.text)
                         && t.color.a > 0.01f && !Under(t.transform, "ClassKit") && t.name != "ResultFloat" && t.name != "HandFlare"))
                if (Overlaps(panel, Glyphs(text))) problems.Add($"the words '{Short(text)}' ({text.name})");
            foreach (Button button in view.GetComponentsInChildren<Button>(false).Where(b => b.isActiveAndEnabled && !Under(b.transform, "ClassKit")))
                if (Overlaps(panel, Box((RectTransform)button.transform))) problems.Add("the button " + button.name);
            Assert.AreEqual(kit.Shown.Cards.Count > 0 ? KitView.CardsHeight : KitView.ButtonsHeight, kit.Height, where + ": the panel's height");
            Assert.IsEmpty(problems, where + ": the class panel over\n" + string.Join("\n", problems));
        }

        /// <summary>A long line of the language on show (a hand's result as the floors word it).</summary>
        private static string Long(int n) => string.Format(UiText.CoinLossFormat, LongestHand(), LongestHand(), 100 + n);

        // ------------------------------------------------------------------ the check

        internal static void AssertNothingOverlaps(TableView view, string where)
        {
            Canvas.ForceUpdateCanvases();
            var texts = view.GetComponentsInChildren<Text>(false)
                .Where(t => t.isActiveAndEnabled && !string.IsNullOrWhiteSpace(t.text) && t.color.a > 0.01f && t.transform.lossyScale.x > 0.01f)
                .Where(t => t.name != "ResultFloat" && t.name != "HandFlare")   // moving for a moment over everything, by design
                .Select(t => (text: t, box: Glyphs(t))).Where(b => b.box.width > 0f && b.box.height > 0f).ToList();
            var buttons = view.GetComponentsInChildren<Button>(false).Where(b => b.isActiveAndEnabled)
                .Select(b => (button: b, box: Box((RectTransform)b.transform))).ToList();
            var problems = new List<string>();
            for (int i = 0; i < texts.Count; i++)
            {
                for (int j = i + 1; j < texts.Count; j++)
                {
                    if (!Overlaps(texts[i].box, texts[j].box)) continue;
                    // A button's label and anything inside the same small widget are one thing.
                    if (SameWidget(texts[i].text, texts[j].text)) continue;
                    problems.Add($"'{Short(texts[i].text)}' ({texts[i].text.name}) over '{Short(texts[j].text)}' ({texts[j].text.name})");
                }
                foreach (var (button, box) in buttons)
                {
                    if (texts[i].text.transform.IsChildOf(button.transform) || !Overlaps(texts[i].box, box)) continue;
                    if (button.transform.IsChildOf(texts[i].text.transform)) continue;
                    if (OwnWidget(texts[i].text, button)) continue;
                    problems.Add($"'{Short(texts[i].text)}' ({texts[i].text.name}) under the button {button.name}");
                }
            }
            Assert.IsEmpty(problems, where + ":\n" + string.Join("\n", problems));
        }

        private static string Short(Text t) => t.text.Length > 30 ? t.text.Substring(0, 30) + "…" : t.text;

        /// <summary>Two texts of one widget (a card's corners, a badge's lines, a panel's title over its buttons) are its own business.</summary>
        private static bool SameWidget(Text a, Text b)
        {
            Transform pa = a.transform.parent, pb = b.transform.parent;
            if (pa == pb && pa != null && pa.GetComponent<TableView>() == null && pa.name != "Screen") return true;
            foreach (string widget in new[] { "Card", "ClassKit", "SinnerBadge", "Sentence", "Payouts", "PayoutTable", "Dealer", "RelicBar", "Malice", "BossBar", "PlayerPanel" })
                if (Under(a.transform, widget) && Under(b.transform, widget)) return true;
            return false;
        }

        /// <summary>A text inside the same widget as the button (the class panel's title over its own buttons).</summary>
        private static bool OwnWidget(Text text, Button button)
        {
            foreach (string widget in new[] { "ClassKit", "RelicBar", "SinnerBadge", "Card", "PlayerPanel" })
                if (Under(text.transform, widget) && Under(button.transform, widget)) return true;
            return false;
        }

        private static bool Under(Transform t, string name)
        {
            for (Transform p = t; p != null; p = p.parent)
                if (p.name.StartsWith(name, StringComparison.Ordinal)) return true;
            return false;
        }

        private static bool Overlaps(Rect a, Rect b) =>
            a.xMin < b.xMax - 0.5f && b.xMin < a.xMax - 0.5f && a.yMin < b.yMax - 0.5f && b.yMin < a.yMax - 0.5f;

        private static Rect Box(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners.Min(c => c.x), corners.Min(c => c.y), corners.Max(c => c.x), corners.Max(c => c.y));
        }

        /// <summary>Where a text's glyphs really are on screen (not its box): the generator's quads, in world space.</summary>
        private static Rect Glyphs(Text text)
        {
            TextGenerator generator = text.cachedTextGenerator;
            IList<UIVertex> verts = generator.verts;
            if (verts == null || verts.Count == 0) return Rect.zero;
            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
            for (int i = 0; i + 3 < verts.Count; i += 4)
            {
                float w = Mathf.Abs(verts[i + 1].position.x - verts[i].position.x), h = Mathf.Abs(verts[i + 2].position.y - verts[i].position.y);
                if (w < 0.01f || h < 0.01f) continue;   // a space
                for (int k = 0; k < 4; k++)
                {
                    Vector3 p = text.transform.TransformPoint(verts[i + k].position / text.pixelsPerUnit);
                    xMin = Mathf.Min(xMin, p.x); xMax = Mathf.Max(xMax, p.x);
                    yMin = Mathf.Min(yMin, p.y); yMax = Mathf.Max(yMax, p.y);
                }
            }
            return xMin > xMax ? Rect.zero : Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}
