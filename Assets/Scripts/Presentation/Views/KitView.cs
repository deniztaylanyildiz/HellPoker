using System;
using System.Collections.Generic;
using HellPoker.Core.Cards;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// Phase 2's class panel, under the dealer's dialog box (where the demo's LEAVE TABLE sits; a chapter's table has none): a title, a
    /// row of small cards (the Peasant's satchel or the cards he may keep, the Warlock's view of the deck's top) or the King's three
    /// treasury buttons. Hover: what the class can do. A card or a button that can be clicked now blinks; a pick under way pulses the frame.
    /// </summary>
    public sealed class KitView : MonoBehaviour
    {
        public const int Width = 104, Height = 38;
        private const int CardWidth = 18, CardHeight = 22, CardStep = 20, MaxCards = 5;

        /// <summary>With the Oracle's arrows a card and its ▲ / ▼ column take this much room (four cards still fit the panel).</summary>
        private const int ArrowCardStep = 25, ArrowWidth = 5, ArrowHeight = 10;

        private AnimationSequencer _sequencer;
        private Action<int> _cardPressed, _buttonPressed;
        private Action<int, bool> _arrowPressed;

        /// <summary>Each card's ▲ and ▼: the button and the pixels of its triangle.</summary>
        private readonly List<(Button up, Image[] upInk, Button down, Image[] downInk)> _arrows = new List<(Button, Image[], Button, Image[])>();
        private Image _frame;
        private Text _title;
        private Text _note;
        private GameObject _tooltip;
        private Text _tooltipText;
        private readonly List<(Image border, Image face, Text rank, Image suit)> _cards = new List<(Image, Image, Text, Image)>();

        /// <summary>Each small card's rim (in its suit's colour) and its big centre suit.</summary>
        private readonly List<(Image rim, Image big)> _cardParts = new List<(Image, Image)>();
        private bool _subscribed;
        private readonly List<(Button button, Image image, Text label)> _buttons = new List<(Button, Image, Text)>();
        private KitDisplay _shown = KitDisplay.Hidden;

        /// <summary>What the panel shows (for tests and screenshots).</summary>
        public KitDisplay Shown => _shown;

        public static KitView Create(Transform screen, int x, int y, AnimationSequencer sequencer, Action<int> cardPressed, Action<int> buttonPressed,
            Action<int, bool> arrowPressed = null)
        {
            RectTransform root = UiFactory.CreateRect("ClassKit", screen).PlaceTL(x, y, Width, Height);
            var view = root.gameObject.AddComponent<KitView>();
            view._sequencer = sequencer;
            view._cardPressed = cardPressed;
            view._buttonPressed = buttonPressed;
            view._arrowPressed = arrowPressed;
            view.Build(root);
            view.Apply(KitDisplay.Hidden);
            return view;
        }

        private void Build(RectTransform root)
        {
            _frame = UiFactory.CreateImage("Frame", root, Palette.Night);
            _frame.rectTransform.PlaceTL(0, 0, Width, Height);
            _frame.raycastTarget = true;
            UiFactory.AddBorder(_frame.gameObject, Palette.Gold, 1f);
            var hover = _frame.gameObject.AddComponent<EventTrigger>();
            AddTrigger(hover, EventTriggerType.PointerEnter, ShowTip);
            AddTrigger(hover, EventTriggerType.PointerExit, () => _tooltip.SetActive(false));

            _title = UiFactory.CreateText("Title", _frame.transform, "", 8, Palette.GoldLight, TextAnchor.UpperLeft);
            _title.rectTransform.PlaceTL(4, 2, Width - 8, 9);
            _title.horizontalOverflow = HorizontalWrapMode.Overflow;
            _title.raycastTarget = false;

            _note = UiFactory.CreateText("Note", _frame.transform, "", 8, Palette.BoneMid, TextAnchor.UpperLeft);
            _note.rectTransform.PlaceTL(4, 15, Width - 8, 20);
            _note.raycastTarget = false;

            for (int i = 0; i < MaxCards; i++)
            {
                int index = i;
                Image border = UiFactory.CreateImage("Card" + i, _frame.transform, Palette.Black);
                border.rectTransform.PlaceTL(4 + i * CardStep, 13, CardWidth, CardHeight);
                border.raycastTarget = true;
                var button = border.gameObject.AddComponent<Button>();
                button.targetGraphic = border;
                button.transition = Selectable.Transition.None;
                UiFactory.MakeClickOnly(button);
                button.onClick.AddListener(() => _cardPressed?.Invoke(index));
                // A rim in the suit's colour inside the frame: the suit reads at a glance even on so small a card.
                Image rim = UiFactory.CreateImage("Rim", border.transform, Palette.BlackSuit);
                rim.rectTransform.PlaceTL(1, 1, CardWidth - 2, CardHeight - 2);
                rim.raycastTarget = false;
                Image face = UiFactory.CreateImage("Face", border.transform, Palette.Bone);
                face.rectTransform.PlaceTL(2, 2, CardWidth - 4, CardHeight - 4);
                face.raycastTarget = false;
                Text rank = UiFactory.CreateText("Rank", face.transform, "", 8, Palette.BlackSuit, TextAnchor.UpperLeft);
                rank.rectTransform.PlaceTL(1, 0, CardWidth - 5, 9);
                rank.horizontalOverflow = HorizontalWrapMode.Overflow;
                rank.raycastTarget = false;
                // The 5×5 suit beside the rank (placed after the rank's width), the big one under them.
                Image suit = UiFactory.CreateImage("Suit", face.transform, Color.white);
                suit.rectTransform.PlaceTL(CardWidth - 4 - 6, 2, 5, 5);
                suit.raycastTarget = false;
                Image big = UiFactory.CreateImage("BigSuit", face.transform, Color.white);
                big.rectTransform.PlaceTL((CardWidth - 4 - 11) / 2, CardHeight - 4 - 11, 11, 11);
                big.raycastTarget = false;
                _cards.Add((border, face, rank, suit));
                _cardParts.Add((rim, big));
                _arrows.Add((Arrow(index, true, out Image[] upInk), upInk, Arrow(index, false, out Image[] downInk), downInk));
            }

            (int x, int y, int w)[] places = { (4, 13, 47), (53, 13, 47), (4, 25, 96) };
            for (int i = 0; i < places.Length; i++)
            {
                int index = i;
                Button button = UiFactory.CreateButton("KitButton" + i, _frame.transform, "", 8, out Text label);
                ((RectTransform)button.transform).PlaceTL(places[i].x, places[i].y, places[i].w, 11);
                label.font = UiArt.Body;
                label.fontStyle = FontStyle.Normal;
                button.onClick.AddListener(() => _buttonPressed?.Invoke(index));
                _buttons.Add((button, button.GetComponent<Image>(), label));
            }

            Image tip = UiFactory.CreateImage("KitTip", root, Palette.Black);
            tip.raycastTarget = false;
            UiFactory.AddBorder(tip.gameObject, Palette.Lilac, 1f);
            _tooltipText = UiFactory.CreateText("Text", tip.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft);
            _tooltipText.raycastTarget = false;
            _tooltip = tip.gameObject;
            _tooltip.SetActive(false);
        }

        /// <summary>A card's ▲ or ▼ (the Oracle's BEND FATE): a small dark button with a 5-pixel triangle drawn in rows (no glyph: the fonts
        /// have none).</summary>
        private Button Arrow(int index, bool up, out Image[] ink)
        {
            Image back = UiFactory.CreateImage(up ? "Up" + index : "Down" + index, _frame.transform, Palette.Black);
            back.rectTransform.PlaceTL(0, 0, ArrowWidth, ArrowHeight);
            back.raycastTarget = true;
            var button = back.gameObject.AddComponent<Button>();
            button.targetGraphic = back;
            button.transition = Selectable.Transition.None;
            UiFactory.MakeClickOnly(button);
            button.onClick.AddListener(() => _arrowPressed?.Invoke(index, up));
            ink = new Image[3];
            for (int row = 0; row < 3; row++)
            {
                int width = up ? 1 + row * 2 : 5 - row * 2;
                Image line = UiFactory.CreateImage("Ink" + row, back.transform, Palette.GoldLight);
                line.rectTransform.PlaceTL((ArrowWidth - width) / 2, 3 + row + (up ? 0 : 1), width, 1);
                line.raycastTarget = false;
                ink[row] = line;
            }
            back.gameObject.SetActive(false);
            return button;
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        private void ShowTip()
        {
            if (string.IsNullOrEmpty(_shown.Hint)) return;
            const int width = 220;
            _tooltipText.text = _shown.Hint;
            _tooltipText.rectTransform.PlaceTL(4, 3, width - 8, 200);
            int height = Mathf.CeilToInt(_tooltipText.preferredHeight) + 6;
            _tooltipText.rectTransform.PlaceTL(4, 3, width - 8, height - 6);
            // Above the panel, left-aligned with it (the screen's left edge): it never covers the cards it describes.
            ((RectTransform)_tooltip.transform).PlaceTL(0, -height - 2, width, height);
            _tooltip.transform.SetAsLastSibling();
            _tooltip.SetActive(true);
        }

        public void Set(KitDisplay kit) => _sequencer.Do(() => Apply(kit ?? KitDisplay.Hidden));

        private void Apply(KitDisplay kit)
        {
            _shown = kit;
            _frame.gameObject.SetActive(!kit.IsHidden);
            if (kit.IsHidden)
            {
                _tooltip.SetActive(false);
                return;
            }
            _title.text = kit.Title;
            _note.text = kit.Note ?? "";
            bool arrows = false;
            foreach (KitCard c in kit.Cards) arrows |= c.Arrows;
            int step = arrows ? ArrowCardStep : CardStep;
            for (int i = 0; i < _cards.Count; i++)
            {
                bool on = i < kit.Cards.Count && kit.Buttons.Count == 0;
                _cards[i].border.gameObject.SetActive(on);
                bool showArrows = on && kit.Cards[i].Arrows && 4 + i * step + CardWidth + 1 + ArrowWidth <= Width;
                _arrows[i].up.gameObject.SetActive(showArrows);
                _arrows[i].down.gameObject.SetActive(showArrows);
                if (!on) continue;
                _cards[i].border.rectTransform.PlaceTL(4 + i * step, 13, CardWidth, CardHeight);
                if (showArrows)
                {
                    int ax = 4 + i * step + CardWidth + 1;
                    ((RectTransform)_arrows[i].up.transform).PlaceTL(ax, 13, ArrowWidth, ArrowHeight);
                    ((RectTransform)_arrows[i].down.transform).PlaceTL(ax, 13 + CardHeight - ArrowHeight, ArrowWidth, ArrowHeight);
                    Paint(_arrows[i].upInk, kit.Cards[i].CanTop ? Palette.GoldLight : Palette.MutedText);
                    Paint(_arrows[i].downInk, kit.Cards[i].CanBottom ? Palette.GoldLight : Palette.MutedText);
                }
                Card card = kit.Cards[i].Card;
                Color ink = card.IsJoker ? Palette.Crimson : Palette.SuitInk(card.Suit, SuitColors.FourColor);
                _cards[i].rank.text = card.IsJoker ? "JK" : RankText(card.Rank);
                _cards[i].rank.color = ink;
                _cardParts[i].rim.color = ink;
                _cards[i].suit.sprite = card.IsJoker ? null : UiArt.Suit(card.Suit, small: true);
                _cards[i].suit.enabled = _cards[i].suit.sprite != null;
                // Beside the rank: one pixel after its width ("10" is wider than "7").
                int rankWidth = Mathf.CeilToInt(_cards[i].rank.preferredWidth);
                _cards[i].suit.rectTransform.PlaceTL(Mathf.Min(CardWidth - 4 - 5, 1 + rankWidth + 1), 2, 5, 5);
                _cardParts[i].big.sprite = card.IsJoker ? null : UiArt.Suit(card.Suit, small: false);
                _cardParts[i].big.enabled = _cardParts[i].big.sprite != null;
                _cards[i].border.color = kit.Cards[i].Picked ? Palette.GoldLight : Palette.Black;
            }
            if (!_subscribed)
            {
                _subscribed = true;
                SuitColors.Changed += Repaint;
            }
            for (int i = 0; i < _buttons.Count; i++)
            {
                bool on = i < kit.Buttons.Count;
                _buttons[i].button.gameObject.SetActive(on);
                if (!on) continue;
                _buttons[i].label.text = kit.Buttons[i].Label;
                _buttons[i].image.color = kit.Buttons[i].Locked ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
                _buttons[i].label.color = kit.Buttons[i].Locked ? Palette.MutedText : Palette.Bone;
            }
            if (_tooltip.activeSelf) ShowTip();
        }

        private static void Paint(Image[] ink, Color colour)
        {
            foreach (Image line in ink) line.color = colour;
        }

        /// <summary>Whether card <paramref name="index"/>'s arrows show (for tests and screenshots).</summary>
        public bool ArrowsShown(int index) => index >= 0 && index < _arrows.Count && _arrows[index].up.gameObject.activeSelf;

        /// <summary>The deck's colours changed: the cards on show take the new ink.</summary>
        private void Repaint()
        {
            if (this != null && _shown != null && !_shown.IsHidden) Apply(_shown);
        }

        private void OnDestroy() => SuitColors.Changed -= Repaint;

        private static string RankText(Rank rank)
        {
            switch (rank)
            {
                case Rank.Ace: return "A";
                case Rank.King: return "K";
                case Rank.Queen: return "Q";
                case Rank.Jack: return "J";
                default: return ((int)rank).ToString();
            }
        }

        /// <summary>A card that can be clicked now blinks its frame gold; a pick under way pulses the panel's frame.</summary>
        private void Update()
        {
            if (_shown.IsHidden) return;
            bool beat = Mathf.Repeat(Time.unscaledTime, 1f) < 0.5f;
            _frame.color = _shown.Waiting && beat ? Palette.Plum : Palette.Night;
            for (int i = 0; i < _cards.Count && i < _shown.Cards.Count; i++)
            {
                KitCard card = _shown.Cards[i];
                if (card.Picked) continue;
                _cards[i].border.color = card.Clickable && beat ? Palette.Gold : Palette.Black;
                if (!card.Arrows) continue;
                // A usable arrow blinks between gold and its brighter tone.
                if (card.CanTop) Paint(_arrows[i].upInk, beat ? Palette.Gold : Palette.GoldLight);
                if (card.CanBottom) Paint(_arrows[i].downInk, beat ? Palette.Gold : Palette.GoldLight);
            }
        }
    }
}
