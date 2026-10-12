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
        private const int CardWidth = 16, CardHeight = 22, CardStep = 19, MaxCards = 5;

        private AnimationSequencer _sequencer;
        private Action<int> _cardPressed, _buttonPressed;
        private Image _frame;
        private Text _title;
        private GameObject _tooltip;
        private Text _tooltipText;
        private readonly List<(Image border, Image face, Text rank, Image suit)> _cards = new List<(Image, Image, Text, Image)>();
        private readonly List<(Button button, Image image, Text label)> _buttons = new List<(Button, Image, Text)>();
        private KitDisplay _shown = KitDisplay.Hidden;

        /// <summary>What the panel shows (for tests and screenshots).</summary>
        public KitDisplay Shown => _shown;

        public static KitView Create(Transform screen, int x, int y, AnimationSequencer sequencer, Action<int> cardPressed, Action<int> buttonPressed)
        {
            RectTransform root = UiFactory.CreateRect("ClassKit", screen).PlaceTL(x, y, Width, Height);
            var view = root.gameObject.AddComponent<KitView>();
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
                Image face = UiFactory.CreateImage("Face", border.transform, Palette.Bone);
                face.rectTransform.PlaceTL(1, 1, CardWidth - 2, CardHeight - 2);
                face.raycastTarget = false;
                Text rank = UiFactory.CreateText("Rank", face.transform, "", 8, Palette.BlackSuit, TextAnchor.UpperLeft);
                rank.rectTransform.PlaceTL(1, 0, CardWidth - 3, 9);
                rank.horizontalOverflow = HorizontalWrapMode.Overflow;
                rank.raycastTarget = false;
                Image suit = UiFactory.CreateImage("Suit", face.transform, Color.white);
                suit.rectTransform.PlaceTL(4, 11, 5, 5);
                suit.raycastTarget = false;
                _cards.Add((border, face, rank, suit));
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
            _title.text = kit.Title;
            for (int i = 0; i < _cards.Count; i++)
            {
                bool on = i < kit.Cards.Count && kit.Buttons.Count == 0;
                _cards[i].border.gameObject.SetActive(on);
                if (!on) continue;
                Card card = kit.Cards[i].Card;
                bool red = card.Suit == Suit.Hearts || card.Suit == Suit.Diamonds;
                _cards[i].rank.text = card.IsJoker ? "JK" : RankText(card.Rank);
                _cards[i].rank.color = red ? Palette.RedSuit : Palette.BlackSuit;
                _cards[i].suit.sprite = card.IsJoker ? null : UiArt.Suit(card.Suit, small: true);
                _cards[i].suit.enabled = _cards[i].suit.sprite != null;
                _cards[i].border.color = kit.Cards[i].Picked ? Palette.GoldLight : Palette.Black;
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
