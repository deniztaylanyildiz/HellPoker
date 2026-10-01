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
    /// <summary>A captioned row of five card slots.</summary>
    public sealed class HandView : MonoBehaviour, IHandView
    {
        private readonly List<CardView> _cards = new List<CardView>();
        private AnimationSequencer _sequencer;
        private Text _caption;

        public event Action<int> CardClicked;

        public static HandView Create(Transform parent, string name, Vector2 position, Vector2 cardSize, float spacing, bool captionAbove,
            AnimationSequencer sequencer)
        {
            float width = cardSize.x * Hand.Size + spacing * (Hand.Size - 1);
            RectTransform root = UiFactory.CreateRect(name, parent).Place(new Vector2(0.5f, 0.5f), position, new Vector2(width, cardSize.y));

            var view = root.gameObject.AddComponent<HandView>();
            view._sequencer = sequencer;
            view.Build(root, cardSize, spacing, captionAbove);
            return view;
        }

        private void Build(RectTransform root, Vector2 cardSize, float spacing, bool captionAbove)
        {
            var row = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = spacing;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            for (int i = 0; i < Hand.Size; i++)
            {
                int index = i;
                CardView card = CardView.Create(root, cardSize);
                card.Clicked += () => CardClicked?.Invoke(index);
                _cards.Add(card);
            }

            // Caption lives outside the layout row so the group does not try to arrange it.
            float captionY = captionAbove ? cardSize.y / 2f + 42f : -cardSize.y / 2f - 42f;
            _caption = UiFactory.CreateText("Caption", root.parent, "", 30, Palette.MutedText, style: FontStyle.Bold);
            _caption.rectTransform.Place(new Vector2(0.5f, 0.5f), root.anchoredPosition + new Vector2(0f, captionY), new Vector2(root.sizeDelta.x + 200f, 44f));
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
                if (!slots[i].SameAs(_cards[i].Planned))
                    _sequencer.Play(_cards[i].AnimateTo(slots[i]));
            }
        }

        public void SetSelection(ICollection<int> selectedIndices)
        {
            int[] selected = selectedIndices?.ToArray() ?? new int[0];
            _sequencer.Do(() =>
            {
                for (int i = 0; i < _cards.Count; i++)
                    _cards[i].SetSelected(selected.Contains(i));
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
