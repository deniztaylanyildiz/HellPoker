using System;
using System.Collections.Generic;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>A row of chip buttons, one per allowed opening stake.</summary>
    public sealed class StakeSelectorView : MonoBehaviour, IStakeSelectorView
    {
        private readonly List<(int stake, Image image, Button button)> _chips = new List<(int, Image, Button)>();
        private AnimationSequencer _sequencer;

        public event Action<int> StakeChosen;

        public static StakeSelectorView Create(Transform parent, Vector2 position, IReadOnlyList<int> stakes, AnimationSequencer sequencer)
        {
            var chipSize = new Vector2(104f, 64f);
            const float spacing = 12f;
            const float labelWidth = 120f;
            float width = labelWidth + (chipSize.x + spacing) * stakes.Count;

            RectTransform root = UiFactory.CreateRect("StakeSelector", parent).Place(new Vector2(0.5f, 0.5f), position, new Vector2(width, chipSize.y));
            var view = root.gameObject.AddComponent<StakeSelectorView>();
            view._sequencer = sequencer;

            UiFactory.CreateText("Label", root, UiText.StakeLabel, 26, Palette.MutedText, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Place(new Vector2(0f, 0.5f), Vector2.zero, new Vector2(labelWidth, chipSize.y), new Vector2(0f, 0.5f));

            for (int i = 0; i < stakes.Count; i++)
            {
                int stake = stakes[i];
                Button chip = UiFactory.CreateButton($"Chip{stake}", root, stake.ToString(), 28, out _);
                ((RectTransform)chip.transform).Place(new Vector2(0f, 0.5f), new Vector2(labelWidth + i * (chipSize.x + spacing), 0f), chipSize, new Vector2(0f, 0.5f));
                chip.onClick.AddListener(() => view.StakeChosen?.Invoke(stake));
                view._chips.Add((stake, (Image)chip.targetGraphic, chip));
            }

            return view;
        }

        public void SetSelected(int stake)
        {
            _sequencer.Do(() =>
            {
                foreach (var chip in _chips)
                    chip.image.color = chip.stake == stake ? Palette.Ember : Palette.Button;
            });
        }

        public void SetAvailable(ICollection<int> stakes)
        {
            var available = new HashSet<int>(stakes);
            _sequencer.Do(() =>
            {
                foreach (var chip in _chips)
                    chip.button.interactable = available.Contains(chip.stake);
            });
        }

        public void SetVisible(bool visible)
        {
            _sequencer.Do(() => gameObject.SetActive(visible));
        }
    }
}
