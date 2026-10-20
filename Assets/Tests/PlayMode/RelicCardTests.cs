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
    /// <summary>Demo 3.5: the relic card — every relic, upgrade and the coins fit it in both languages, on the 8 px grid; its states.</summary>
    public class RelicCardTests
    {
        [TearDown]
        public void English() => Lang.Set(Language.English);

        private static IEnumerator Scene()
        {
            HellPokerBootstrap.BatchStore.Clear();
            yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.3f);
        }

        private static float Top(RectTransform r) => -r.anchoredPosition.y;
        private static float Bottom(RectTransform r) => Top(r) + r.rect.height;

        private static void AssertFits(RelicCardView card, string what, List<string> problems)
        {
            Canvas.ForceUpdateCanvases();
            void Fits(Text text, string part)
            {
                if (string.IsNullOrEmpty(text.text)) return;
                if (text.preferredHeight > text.rectTransform.rect.height + 0.5f) problems.Add($"{what}: the {part} is cut ('{text.text}')");
            }
            Fits(card.NameText, "name");
            if (card.IsSmall)
            {
                if (card.NameText.preferredWidth > card.NameText.rectTransform.rect.width + 0.5f) problems.Add($"{what}: the name runs out");
                return;
            }
            Fits(card.RarityText, "rarity");
            Fits(card.GiftText, "gift");
            Fits(card.FooterText, "foot");
            if (card.CurseText.text.Length > 0)
            {
                float curseBottom = Top(card.CurseText.rectTransform) + card.CurseText.preferredHeight;
                if (curseBottom > Top(card.FooterText.rectTransform)) problems.Add($"{what}: the curse runs into the foot ({curseBottom} > {Top(card.FooterText.rectTransform)})");
            }
            float giftBottom = Top(card.GiftText.rectTransform) + card.GiftText.preferredHeight;
            if (giftBottom > Top(card.FooterText.rectTransform)) problems.Add($"{what}: the gift runs into the foot");
            if (card.RarityText.text.Length > 0 && Top(card.GiftText.rectTransform) < Bottom(card.RarityText.rectTransform))
                problems.Add($"{what}: the gift over the rarity");
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator EveryCard_FitsItsWords_InBothLanguages_OnThe8pxGrid()
        {
            yield return Scene();
            Canvas canvas = UiFactory.CreateScreen("CardTest", Object.FindFirstObjectByType<HellPokerBootstrap>().transform, 500, out RectTransform screen, letterbox: false);
            RelicCardView big = RelicCardView.Create(screen, "Big", small: false);
            RelicCardView small = RelicCardView.Create(screen, "Small", small: true);
            big.PlaceAt(16, 16);
            small.PlaceAt(160, 16);
            Assert.AreEqual(0, RelicCardView.Width % 8, "the card's width on the grid");
            Assert.AreEqual(0, RelicCardView.Height % 8);
            Assert.AreEqual(0, RelicCardView.SmallWidth % 8);
            Assert.AreEqual(0, RelicCardView.SmallHeight % 8);

            var problems = new List<string>();
            foreach (Language language in new[] { Language.English, Language.Turkish })
            {
                Lang.Set(language);
                foreach (string id in RelicRoster.IconOrder)
                {
                    big.Show(RelicCards.Relic(id, string.Format(UiText.CardPriceFormat, 75)));
                    AssertFits(big, $"{id} ({language})", problems);
                    Assert.IsNotNull(big.Picture.sprite, id + ": its big picture");
                    Assert.AreEqual(new Vector2(64, 64), big.Picture.rectTransform.sizeDelta, id + ": shown at 2×");
                    small.Show(RelicCards.Small(id));
                    AssertFits(small, $"{id} small ({language})", problems);
                }
                foreach (SinnerClass cls in SinnerRoster.PhaseTwo)
                    foreach (string upgrade in ClassUpgrades.For(cls.Id))
                    {
                        big.Show(RelicCards.Upgrade(upgrade, cls.Id, UiText.CardTaken));
                        AssertFits(big, $"{upgrade} ({language})", problems);
                    }
                big.Show(RelicCards.Coins(30));
                AssertFits(big, $"coins ({language})", problems);
                yield return null;
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));

            // The states: dim with its reason, the stamp, the lift on the pointer.
            big.Show(RelicCards.Relic(ChapterRelicIds.BlackBook, UiText.CardNoCoins, RelicCardState.Unavailable));
            Assert.AreEqual(UiText.CardNoCoins, big.FooterText.text);
            Assert.IsFalse(big.StampShown);
            big.Show(RelicCards.Relic(ChapterRelicIds.BlackBook, "", RelicCardState.Taken));
            Assert.IsTrue(big.StampShown, "SOLD");
            float before = ((RectTransform)big.transform).anchoredPosition.y;
            big.SetHovered(true);
            Assert.AreEqual(before + 2, ((RectTransform)big.transform).anchoredPosition.y, "hovered: 2 px up");
            big.SetHovered(false);
            Assert.AreEqual(before, ((RectTransform)big.transform).anchoredPosition.y);
            Object.Destroy(canvas.gameObject);
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator TheWardensPanel_TheCardAndTheSmallCards_FitTheBox_TouchNothing_InBothLanguages()
        {
            string[] relics = ChapterRelics.All.Where(id => !ChapterRelics.IsClassRelic(id) && !RelicRoster.IsReward(id))
                .OrderByDescending(id => UiText.RelicName(id).Length).Take(11).ToArray();
            foreach (bool turkish in new[] { false, true })
            {
                ChapterSave save = ChapterPresenter.PlacedSave(SinnerRoster.King, 11, 1, "warden");
                save.Relics = relics.Take(5).ToList();
                save.Bag = relics.Skip(5).Take(5).ToList();
                save.WardenRelic = relics[10];
                HellPokerBootstrap.BatchStore.Clear();
                HellPokerBootstrap.BatchStore.SetString("settings.language", turkish ? "Turkish" : "English");
                new Presentation.Settings.ChapterArchive(HellPokerBootstrap.BatchStore).SaveRun(save);
                yield return SceneManager.LoadSceneAsync("HellPoker", LoadSceneMode.Single);
                yield return new WaitForSeconds(0.5f);
                HellPokerScreenshots.Press("ContinueButton");
                yield return new WaitForSeconds(0.8f);
                var panel = Object.FindFirstObjectByType<ChapterPanelView>();
                string lang = turkish ? " (tr)" : " (en)";
                Assert.AreEqual(1, panel.ShowcaseCards.Count, "the warden's relic" + lang);
                AssertInsideAndApart(panel, "warden" + lang);

                HellPokerScreenshots.Press("PanelOption1");   // TAKE IT
                yield return null;
                Assert.AreEqual(10, panel.OptionCards.Count(c => c != null && c.IsSmall), "ten small cards" + lang);
                AssertInsideAndApart(panel, "swap" + lang);
            }
        }

        /// <summary>Every card and button inside the panel's box, none over another, every card on the 8 px grid of the box.</summary>
        private static void AssertInsideAndApart(ChapterPanelView panel, string what)
        {
            Canvas.ForceUpdateCanvases();
            var boxes = new List<(string name, Rect rect)>();
            foreach (RelicCardView card in panel.ShowcaseCards.Concat(panel.OptionCards.Where(c => c != null)))
            {
                var r = (RectTransform)card.transform;
                Assert.AreEqual(0, Mathf.RoundToInt(r.anchoredPosition.x) % 8, $"{what}: {card.name} on the grid (x)");
                boxes.Add((card.name, new Rect(r.anchoredPosition.x, -r.anchoredPosition.y, r.sizeDelta.x, r.sizeDelta.y)));
            }
            foreach (Button button in panel.GetComponentsInChildren<Button>().Where(b => b.name.StartsWith("PanelOption") && b.GetComponentInParent<RelicCardView>() == null))
            {
                var r = (RectTransform)button.transform;
                boxes.Add((button.name, new Rect(r.anchoredPosition.x, -r.anchoredPosition.y, r.sizeDelta.x, r.sizeDelta.y)));
            }
            Rect box = new Rect(0, 0, panel.Box.sizeDelta.x, panel.Box.sizeDelta.y);
            foreach (var (name, rect) in boxes)
                Assert.IsTrue(rect.xMin >= 0 && rect.yMin >= 0 && rect.xMax <= box.width && rect.yMax <= box.height, $"{what}: {name} inside the panel ({rect} in {box})");
            for (int i = 0; i < boxes.Count; i++)
                for (int j = i + 1; j < boxes.Count; j++)
                    Assert.IsFalse(boxes[i].rect.Overlaps(boxes[j].rect), $"{what}: {boxes[i].name} over {boxes[j].name}");
            Assert.LessOrEqual(panel.Box.sizeDelta.x, PixelScreen.Width, what + ": on the screen");
        }
    }
}
