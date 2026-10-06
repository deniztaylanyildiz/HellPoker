using System;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// The joker picker at the showdown, in the bet controls' row: the card the joker would become (a real card, so it reads at a
    /// glance), small arrows to step its rank and suit, and NAME IT. What the hand makes with it is said by the presenter on the
    /// line under the message. The keyboard does the same (arrows, Enter).
    /// </summary>
    public sealed class JokerPickerView : MonoBehaviour
    {
        public const int Width = 256;
        public const int Height = 56;

        private AnimationSequencer _sequencer;
        private CardView _card;

        /// <summary>The card on show (for tests and screenshots); null while the picker is closed.</summary>
        public JokerPick Pick { get; private set; }

        public bool IsOpen => gameObject.activeSelf;

        public static JokerPickerView Create(Transform screen, int x, int y, AnimationSequencer sequencer, Action<int, int> step, Action confirm)
        {
            Image panel = UiFactory.CreatePanel("JokerPicker", screen);
            panel.raycastTarget = true;
            panel.rectTransform.PlaceTL(x, y, Width, Height);
            var view = panel.gameObject.AddComponent<JokerPickerView>();
            view._sequencer = sequencer;
            view.Build(panel.transform, step, confirm);
            panel.gameObject.SetActive(false);
            return view;
        }

        private void Build(Transform root, Action<int, int> step, Action confirm)
        {
            Selector(root, "Rank", 6, () => UiText.JokerRankLabel, () => step?.Invoke(-1, 0), () => step?.Invoke(1, 0));
            Selector(root, "Suit", 30, () => UiText.JokerSuitLabel, () => step?.Invoke(0, -1), () => step?.Invoke(0, 1));

            _card = CardView.Create(root);
            ((RectTransform)_card.transform).PlaceTL(112, 4, CardView.Size.x, CardView.Size.y);
            _card.SetInteractable(false);

            Button ok = UiFactory.CreateButton("JokerConfirm", root, "", 8, out Text okLabel, ButtonSkin.Ember);
            okLabel.Localized(() => UiText.JokerConfirm);
            ((RectTransform)ok.transform).PlaceTL(156, 18, 92, 20);
            ok.onClick.AddListener(() => confirm?.Invoke());
        }

        /// <summary>A row: "&lt;", the label, "&gt;".</summary>
        private static void Selector(Transform root, string name, int y, Func<string> label, Action back, Action forward)
        {
            Button left = UiFactory.CreateButton("Joker" + name + "Back", root, "<", 8, out _, ButtonSkin.Ash);
            ((RectTransform)left.transform).PlaceTL(6, y, 18, 18);
            left.onClick.AddListener(() => back());
            UiFactory.CreateText(name + "Label", root, "", 8, Palette.GoldLight, TextAnchor.MiddleCenter, FontStyle.Bold).Localized(label)
                .rectTransform.PlaceTL(26, y + 5, 58, 8);
            Button right = UiFactory.CreateButton("Joker" + name + "Forward", root, ">", 8, out _, ButtonSkin.Ash);
            ((RectTransform)right.transform).PlaceTL(86, y, 18, 18);
            right.onClick.AddListener(() => forward());
        }

        /// <summary>Opens the picker on <paramref name="pick"/> (or moves it there); null closes it. In the table's queue.</summary>
        public void Show(JokerPick pick) => _sequencer.Do(() => Apply(pick));

        private void Apply(JokerPick pick)
        {
            bool open = pick != null;
            bool wasOpen = gameObject.activeSelf;
            gameObject.SetActive(open);
            if (!open)
            {
                Pick = null;
                return;
            }
            bool changed = Pick == null || Pick.Card != pick.Card;
            Pick = pick;
            if (!changed && wasOpen) return;
            var slot = CardSlot.Face(pick.Card);
            if (wasOpen && isActiveAndEnabled)
                StartCoroutine(_card.AnimateTo(slot));
            else
                SnapCard(slot);
        }

        /// <summary>The card shows at once (the picker just opened): the flip plays out within a frame.</summary>
        private void SnapCard(CardSlot slot)
        {
            System.Collections.IEnumerator flip = _card.AnimateTo(slot);
            if (isActiveAndEnabled) StartCoroutine(flip);
        }
    }
}