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
    /// row of cards (the Peasant's satchel or the cards he may keep, the Oracle's view of the deck's top) or the class's buttons (the King's
    /// treasury, the Summoner's imps, the Thief's steal). Hover: what the class can do. A card or a button that can be clicked now blinks; a
    /// pick under way pulses the frame. Demo 2.3: the cards are 24×36 — the rank and the suit read like the table's.
    /// </summary>
    public sealed class KitView : MonoBehaviour
    {
        public const int Width = 104;

        /// <summary>The panel's height with a row of cards, and with buttons (or a note) only.</summary>
        public const int CardsHeight = 52, ButtonsHeight = 38;

        /// <summary>The cards (Demo 2.3: 24×36, up from 18×22): four fit side by side; a fifth closes them up.</summary>
        public const int CardWidth = 24, CardHeight = 36;
        private const int CardStep = 25, TightStep = 20, MaxCards = 5, CardsTop = 13;

        private AnimationSequencer _sequencer;
        private Action<int> _cardPressed, _buttonPressed;

        private RectTransform _root;
        private Image _frame;
        private Text _title;
        private Text _note;
        private GameObject _tooltip;
        private Text _tooltipText;
        private readonly List<(Image border, Image face, Text rank, Image suit)> _cards = new List<(Image, Image, Text, Image)>();

        /// <summary>Each card's rim (in its suit's colour) and its big centre suit.</summary>
        private readonly List<(Image rim, Image big)> _cardParts = new List<(Image, Image)>();
        private bool _subscribed;
        private readonly List<(Button button, Image image, Text label)> _buttons = new List<(Button, Image, Text)>();
        private KitDisplay _shown = KitDisplay.Hidden;

        /// <summary>What the panel shows (for tests and screenshots).</summary>
        public KitDisplay Shown => _shown;

        /// <summary>The panel's height now (for the layout test).</summary>
        public int Height { get; private set; } = ButtonsHeight;

        public static KitView Create(Transform screen, int x, int y, AnimationSequencer sequencer, Action<int> cardPressed, Action<int> buttonPressed)
        {
            RectTransform root = UiFactory.CreateRect("ClassKit", screen).PlaceTL(x, y, Width, ButtonsHeight);
            var view = root.gameObject.AddComponent<KitView>();
            view._root = root;
            view._sequencer = sequencer;
            view._cardPressed = cardPressed;
            view._buttonPressed = buttonPressed;
            view.Build(root);
            view.Apply(KitDisplay.Hidden);
            return view;
        }

        private void Build(RectTransform root)
        {
            _frame = UiFactory.CreateImage("Frame", root, Palette.Night);
            _frame.rectTransform.PlaceTL(0, 0, Width, ButtonsHeight);
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
                border.rectTransform.PlaceTL(4 + i * CardStep, CardsTop, CardWidth, CardHeight);
                border.raycastTarget = true;
                var button = border.gameObject.AddComponent<Button>();
                button.targetGraphic = border;
                button.transition = Selectable.Transition.None;
                UiFactory.MakeClickOnly(button);
                button.onClick.AddListener(() => _cardPressed?.Invoke(index));
                // A rim in the suit's colour inside the frame: the suit reads at a glance.
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
                // The 5×5 suit beside the rank (placed after the rank's width), the big 11×11 one under them, in the middle.
                Image suit = UiFactory.CreateImage("Suit", face.transform, Color.white);
                suit.rectTransform.PlaceTL(CardWidth - 4 - 6, 2, 5, 5);
                suit.raycastTarget = false;
                Image big = UiFactory.CreateImage("BigSuit", face.transform, Color.white);
                big.rectTransform.PlaceTL((CardWidth - 4 - 11) / 2, 13, 11, 11);
                big.raycastTarget = false;
                _cards.Add((border, face, rank, suit));
                _cardParts.Add((rim, big));
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
            bool cards = kit.Cards.Count > 0 && kit.Buttons.Count == 0;
            Height = cards ? CardsHeight : ButtonsHeight;
            _frame.rectTransform.PlaceTL(0, 0, Width, Height);
            _root.sizeDelta = new Vector2(Width, Height);
            _title.text = kit.Title;
            _note.text = kit.Note ?? "";
            int step = kit.Cards.Count > 4 ? TightStep : CardStep;
            for (int i = 0; i < _cards.Count; i++)
            {
                bool on = cards && i < kit.Cards.Count;
                _cards[i].border.gameObject.SetActive(on);
                if (!on) continue;
                _cards[i].border.rectTransform.PlaceTL(4 + i * step, CardsTop, CardWidth, CardHeight);
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
            }
        }
    }
}
