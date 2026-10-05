using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>A captioned row of five pixel cards, placed on whole pixels.</summary>
    public sealed class HandView : MonoBehaviour, IHandView
    {
        private readonly List<CardView> _cards = new List<CardView>();
        private AnimationSequencer _sequencer;
        private Text _caption;

        public event Action<int> CardClicked;

        /// <param name="x">Left edge of the row, in screen pixels from the left.</param>
        /// <param name="y">Top edge of the row, in screen pixels from the top.</param>
        /// <param name="captionY">Top of the caption line, in screen pixels from the top.</param>
        public static HandView Create(Transform parent, string name, int x, int y, int spacing, int captionY, AnimationSequencer sequencer)
        {
            int width = CardView.Size.x * Hand.Size + spacing * (Hand.Size - 1);
            RectTransform root = UiFactory.CreateRect(name, parent).PlaceTL(x, y, width, CardView.Size.y);

            var view = root.gameObject.AddComponent<HandView>();
            view._sequencer = sequencer;
            view.Build(root, spacing, x, captionY, width);
            return view;
        }

        private void Build(RectTransform root, int spacing, int x, int captionY, int width)
        {
            for (int i = 0; i < Hand.Size; i++)
            {
                int index = i;
                CardView card = CardView.Create(root);
                ((RectTransform)card.transform).PlaceTL(i * (CardView.Size.x + spacing), 0, CardView.Size.x, CardView.Size.y);
                card.Clicked += () => CardClicked?.Invoke(index);
                _cards.Add(card);
            }

            _caption = UiFactory.CreateText("Caption", root.parent, "", 8, Palette.MutedText, style: FontStyle.Bold).WithOutline();
            _caption.rectTransform.PlaceTL(x - 40, captionY, width + 80, 8);
            _caption.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        public void SetCaption(string text, Tone tone)
        {
            _sequencer.Do(() =>
            {
                _caption.text = text;
                _caption.color = Palette.For(tone);
            });
        }

        /// <summary>Clears in one sweep, deals new cards one by one, then turns the cards that must be face up.</summary>
        public void Show(IReadOnlyList<CardSlot> slots)
        {
            if (slots.Count != _cards.Count)
                throw new ArgumentException($"Expected {_cards.Count} slots.", nameof(slots));

            var clears = new List<IEnumerator>();
            for (int i = 0; i < _cards.Count; i++)
            {
                if (slots[i].Kind == CardSlot.SlotKind.Empty && _cards[i].Planned.Kind != CardSlot.SlotKind.Empty)
                    clears.Add(_cards[i].AnimateTo(CardSlot.Empty));
            }
            if (clears.Count > 0)
                _sequencer.Play(Together(clears));

            for (int i = 0; i < _cards.Count; i++)
            {
                if (slots[i].Kind != CardSlot.SlotKind.Empty && _cards[i].Planned.Kind == CardSlot.SlotKind.Empty)
                    _sequencer.Play(_cards[i].AnimateTo(CardSlot.Back));
            }

            for (int i = 0; i < _cards.Count; i++)
            {
                if (!slots[i].SameAs(_cards[i].Planned) || slots[i].Mark != _cards[i].Planned.Mark)
                    _sequencer.Play(_cards[i].AnimateTo(slots[i]));
            }
        }

        /// <summary>A card of the row (for the cheats' effects: where it is, a shake).</summary>
        public CardView Card(int index) => index >= 0 && index < _cards.Count ? _cards[index] : null;

        public void SetSelection(ICollection<int> selectedIndices)
        {
            int[] selected = selectedIndices?.ToArray() ?? new int[0];
            _sequencer.Do(() =>
            {
                for (int i = 0; i < _cards.Count; i++)
                    _cards[i].SetSelected(selected.Contains(i));
            });
        }

        public void SetHints(ICollection<int> keepIndices)
        {
            int[] keep = keepIndices?.ToArray() ?? new int[0];
            _sequencer.Do(() =>
            {
                for (int i = 0; i < _cards.Count; i++)
                    _cards[i].SetHint(keep.Contains(i));
            });
        }

        /// <summary>Lights one card up brightly (the Dead Man's Hand scene).</summary>
        public void SetGlint(int index, bool on)
        {
            if (index >= 0 && index < _cards.Count)
                _cards[index].SetHint(on, bright: true);
        }

        public void SetPicking(ICollection<int> pickable)
        {
            int[] take = pickable?.ToArray() ?? new int[0];
            _sequencer.Do(() =>
            {
                for (int i = 0; i < _cards.Count; i++)
                    _cards[i].SetPick(take.Length == 0 ? CardView.PickState.None
                        : take.Contains(i) ? CardView.PickState.Pickable : CardView.PickState.Dimmed);
            });
        }

        public void SetInteractable(bool interactable)
        {
            _sequencer.Do(() =>
            {
                foreach (CardView card in _cards)
                    card.SetInteractable(interactable);
            });
        }

        /// <summary>Runs several animations in parallel within one sequencer step.</summary>
        private static IEnumerator Together(List<IEnumerator> animations)
        {
            animations = animations.Select(Tween.Flatten).ToList();
            bool running = true;
            while (running)
            {
                running = false;
                foreach (IEnumerator animation in animations)
                    running |= animation.MoveNext();
                if (running) yield return null;
            }
        }
    }
}
