using System;
using System.Collections.Generic;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// "Choose your sin": after the demon, before the first hand — a row of class cards (portrait, name, who they were, the
    /// ability and its price, the starting sentence) and a CHOOSE button on each, over the chosen demon's hall.
    /// Built on <see cref="Show"/>, so it adapts to however many classes the roster holds.
    /// </summary>
    public sealed class SinnerSelectView : MonoBehaviour, ISinnerSelectView
    {
        private const int SortingOrder = 112;
        /// <summary>The widest a card gets (three classes); with more, the cards narrow so the row always fits the 480 px screen.</summary>
        private const int MaxCardWidth = 148;
        private const int CardSpacing = 6;
        private const int ScreenMargin = 4;
        private const int CardsY = 34;
        private const int CardHeight = 230;

        /// <summary>Where the ability text starts, its margin, and the top of CHOOSE: the ability and the price live between.</summary>
        public const int AbilityTop = 152;
        public const int TextX = 5;
        public const int ChooseTop = CardHeight - 24;

        /// <summary>The card width for <paramref name="count"/> classes (whole pixels, even).</summary>
        public static int CardWidthFor(int count) =>
            Math.Min(MaxCardWidth, (PixelScreen.Width - 2 * ScreenMargin - (Math.Max(1, count) - 1) * CardSpacing) / Math.Max(1, count) / 2 * 2);

        private int CardWidth { get; set; } = MaxCardWidth;

        private Canvas _canvas;
        private SalonView _salon;
        private RectTransform _cards;

        /// <summary>Demo 2.3.1: the hovered card's whole text (the card keeps its short words).</summary>
        private GameObject _tip;
        private Text _tipText;
        private const int TipWidth = 196;

        /// <summary>The hover box is open (for tests and screenshots), and its words.</summary>
        public bool TipShown => _tip != null && _tip.activeSelf;
        public string TipText => _tipText != null ? _tipText.text : "";

        public event Action<int> SinnerChosen;
        public event Action BackPressed;
        public event Action<int, int> CircleStepped;

        private Text _subtitle;

        /// <summary>Below this card width the portrait is drawn once (48 px), not twice: five cards need the room for their words.</summary>
        private const int SmallPortraitBelow = 100;

        /// <summary>How far up a card's words move when its portrait is drawn once.</summary>
        private int Shift => CardWidth < SmallPortraitBelow ? 48 : 0;

        /// <summary>Where the ability text starts on the cards on show.</summary>
        public int AbilityTopNow => AbilityTop - Shift;

        /// <summary>The top of the circle row (Phase 2): the ability and the price must end above it.</summary>
        public const int CircleTop = ChooseTop - 13;

        public bool IsVisible => _canvas.enabled;

        public static SinnerSelectView Create(Transform parent, SalonLibrary salons)
        {
            Canvas canvas = UiFactory.CreateScreen("SinnerSelectCanvas", parent, SortingOrder, out RectTransform screen);
            var view = canvas.gameObject.AddComponent<SinnerSelectView>();
            view._canvas = canvas;
            view.Build(screen, salons);
            view.Hide();
            return view;
        }

        private void Build(RectTransform screen, SalonLibrary salons)
        {
            _salon = SalonView.Create(screen, salons);

            UiFactory.CreateText("Title", screen, "", 16, Palette.GoldLight, style: FontStyle.Bold).WithOutline().Localized(() => UiText.ChooseSinnerTitle)
                .rectTransform.PlaceTL(0, 4, PixelScreen.Width, 16);
            _subtitle = UiFactory.CreateText("Subtitle", screen, UiText.ChooseSinnerSubtitle, 8, Palette.BoneMid).WithOutline();
            _subtitle.rectTransform.PlaceTL(0, 21, PixelScreen.Width, 9);

            _cards = UiFactory.CreateRect("Cards", screen).Stretch();

            Button back = UiFactory.CreateButton("SinnerBackButton", screen, "", 8, out Text backLabel, ButtonSkin.Ash);
            backLabel.Localized(() => UiText.Back);
            ((RectTransform)back.transform).PlaceTL(8, 4, 56, 16);
            back.onClick.AddListener(() => BackPressed?.Invoke());

            Image tip = UiFactory.CreateImage("SinnerCardTip", screen, Palette.Black);
            tip.raycastTarget = false;
            HoverTip.Make(tip.gameObject);
            UiFactory.AddBorder(tip.gameObject, Palette.Gold, 1f);
            _tipText = UiFactory.CreateText("Text", tip.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft);
            _tipText.raycastTarget = false;
            _tip = tip.gameObject;
            _tip.SetActive(false);
        }

        /// <summary>The hover box beside the card at <paramref name="x"/> (right of it, or left when there is no room), as tall as its words.</summary>
        public void ShowTip(string text, int x)
        {
            if (string.IsNullOrEmpty(text)) return;
            _tipText.text = text;
            _tipText.rectTransform.PlaceTL(4, 3, TipWidth - 8, 200);
            int height = Mathf.CeilToInt(_tipText.preferredHeight) + 6;
            _tipText.rectTransform.PlaceTL(4, 3, TipWidth - 8, height - 6);
            int left = x + CardWidth + 2 + TipWidth <= PixelScreen.Width ? x + CardWidth + 2 : Math.Max(0, x - TipWidth - 2);
            ((RectTransform)_tip.transform).PlaceTL(left, CardsY + 40, TipWidth, height);
            _tip.transform.SetAsLastSibling();
            _tip.SetActive(true);
        }

        public void HideTip()
        {
            if (_tip != null) _tip.SetActive(false);
        }

        public void Show(IReadOnlyList<SinnerCard> sinners, string dealerId, string note = null)
        {
            HideTip();
            _subtitle.text = note ?? UiText.ChooseSinnerSubtitle;
            _subtitle.color = note == null ? Palette.BoneMid : Palette.Ember;
            foreach (Transform child in _cards)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            if (!string.IsNullOrEmpty(dealerId))
                _salon.SetSalon(dealerId, SalonMode.Normal, false);

            CardWidth = CardWidthFor(sinners.Count);
            int total = sinners.Count * CardWidth + (sinners.Count - 1) * CardSpacing;
            int left = (PixelScreen.Width - total) / 2;
            for (int i = 0; i < sinners.Count; i++)
                BuildCard(sinners[i], i, left + i * (CardWidth + CardSpacing));

            _canvas.enabled = true;
            GetComponent<GraphicRaycaster>().enabled = true;
        }

        public void Hide()
        {
            HideTip();
            _canvas.enabled = false;
            GetComponent<GraphicRaycaster>().enabled = false;
        }

        private void BuildCard(SinnerCard sinner, int index, int x)
        {
            Image box = UiFactory.CreatePanel($"Sinner_{sinner.Id}", _cards);
            box.rectTransform.PlaceTL(x, CardsY, CardWidth, CardHeight);
            Transform card = box.transform;
            if (!string.IsNullOrEmpty(sinner.Hover))
            {
                // Demo 2.3.1: the whole text while the card is hovered.
                box.raycastTarget = true;
                var hover = box.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                var enter = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ => ShowTip(sinner.Hover, x));
                var exit = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
                exit.callback.AddListener(_ => HideTip());
                hover.triggers.Add(enter);
                hover.triggers.Add(exit);
            }

            // The portrait at twice its size (48 → 96): whole pixels.
            // The portrait at twice its size (48 → 96) — once (48) when five cards share the row: whole pixels either way.
            int size = Shift > 0 ? 48 : 96;
            Image frame = UiFactory.CreateImage("Frame", card, Palette.Black);
            frame.rectTransform.PlaceTL((CardWidth - size - 4) / 2, 6, size + 4, size + 4);
            UiFactory.AddBorder(frame.gameObject, sinner.Locked ? Palette.BoneDark : Palette.Gold, 1f);
            Sprite portrait = UiArt.SinnerPortrait(sinner.Id);
            Image picture = UiFactory.CreateImage("Portrait", frame.transform, portrait == null ? Palette.Plum : sinner.Locked ? Palette.Dusk : Color.white);
            picture.sprite = portrait;
            picture.raycastTarget = false;
            picture.rectTransform.PlaceTL(2, 2, size, size);

            int shift = Shift;
            UiFactory.CreateText("Name", card, sinner.Name, 8, sinner.Locked ? Palette.BoneDark : Palette.GoldLight, style: FontStyle.Bold).WithOutline()
                .rectTransform.PlaceTL(0, 110 - shift, CardWidth, 8);
            Text title = UiFactory.CreateText("Title", card, sinner.Title, 8, Palette.BoneMid, TextAnchor.UpperCenter);
            title.rectTransform.PlaceTL(4, 120 - shift, CardWidth - 8, 18);
            // Demo 2.2: six narrow cards — a title that wraps to a third line pushes the start line down, not under it.
            int titleHeight = Math.Max(18, Mathf.CeilToInt(title.preferredHeight));
            title.rectTransform.PlaceTL(4, 120 - shift, CardWidth - 8, titleHeight);
            int startTop = 120 - shift + titleHeight + 2;
            Text start = UiFactory.CreateText("Start", card, sinner.Locked ? UiText.LockedLabel : sinner.Start, 8, Palette.Ember, TextAnchor.UpperCenter);
            start.rectTransform.PlaceTL(2, startTop, CardWidth - 4, 9);
            int startHeight = Math.Max(9, Mathf.CeilToInt(start.preferredHeight));
            start.rectTransform.PlaceTL(2, startTop, CardWidth - 4, startHeight);
            // The ability, then the price right under its real height (not a fixed one: a long line wraps), both above CHOOSE (and the
            // circle row). A locked class says what opens it in place of its price. A start line that wraps pushes them down.
            int top = Math.Max(AbilityTop - shift, startTop + startHeight + 3);
            Text ability = UiFactory.CreateText("Ability", card, sinner.Ability, 8, sinner.Locked ? Palette.BoneDark : Palette.Bone, TextAnchor.UpperLeft);
            ability.rectTransform.PlaceTL(TextX, top, CardWidth - 2 * TextX, 9);
            int abilityHeight = Mathf.CeilToInt(ability.preferredHeight);
            ability.rectTransform.PlaceTL(TextX, top, CardWidth - 2 * TextX, abilityHeight);
            Text detail = UiFactory.CreateText("Detail", card, sinner.Locked ? sinner.LockNote : sinner.Detail, 8,
                sinner.Locked ? Palette.Ember : Palette.LilacLight, TextAnchor.UpperLeft);
            detail.rectTransform.PlaceTL(TextX, top + abilityHeight + 2, CardWidth - 2 * TextX, 9);
            detail.rectTransform.PlaceTL(TextX, top + abilityHeight + 2, CardWidth - 2 * TextX, Mathf.CeilToInt(detail.preferredHeight));

            if (sinner.ShowsCircles && !sinner.Locked) BuildCircleRow(card, sinner, index);

            Button choose = UiFactory.CreateButton($"ChooseSinner{index}", card, sinner.Locked ? UiText.LockedLabel : UiText.ChooseSinner, 8, out _,
                sinner.Locked ? ButtonSkin.Ash : ButtonSkin.Ember);
            ((RectTransform)choose.transform).PlaceTL((CardWidth - Math.Min(88, CardWidth - 8)) / 2, ChooseTop, Math.Min(88, CardWidth - 8), 18);
            choose.onClick.AddListener(() => SinnerChosen?.Invoke(index));
        }

        /// <summary>Phase 2's circle row: ‹ CIRCLE n › (the arrows only where there is a circle to go to).</summary>
        private void BuildCircleRow(Transform card, SinnerCard sinner, int index)
        {
            string label = sinner.Circle <= 0 ? UiText.CircleNone : string.Format(UiText.CircleLabelFormat, sinner.Circle);
            Text text = UiFactory.CreateText("Circle", card, label, 8, sinner.Circle > 0 ? Palette.Ember : Palette.BoneMid, TextAnchor.MiddleCenter);
            text.rectTransform.PlaceTL(14, CircleTop, CardWidth - 28, 11);
            if (sinner.Circle > 0) AddArrow(card, $"CircleDown{index}", "<", 3, () => CircleStepped?.Invoke(index, -1));
            if (sinner.Circle < sinner.CirclesOpen) AddArrow(card, $"CircleUp{index}", ">", CardWidth - 15, () => CircleStepped?.Invoke(index, 1));
        }

        private static void AddArrow(Transform card, string name, string glyph, int x, Action pressed)
        {
            Button arrow = UiFactory.CreateButton(name, card, glyph, 8, out _, ButtonSkin.Ash);
            ((RectTransform)arrow.transform).PlaceTL(x, CircleTop, 12, 11);
            arrow.onClick.AddListener(() => pressed());
        }
    }
}
