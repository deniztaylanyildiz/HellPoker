using System;
using System.Collections.Generic;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>A row of casino chips, one per allowed opening stake. The chosen chip turns gold and rises.</summary>
    public sealed class StakeSelectorView : MonoBehaviour, IStakeSelectorView
    {
        private const float ChipSize = 92f;
        private const float SelectedLift = 10f;

        private readonly List<(int stake, Image image, Button button, Text label)> _chips = new List<(int, Image, Button, Text)>();
        private AnimationSequencer _sequencer;

        public event Action<int> StakeChosen;

        public static StakeSelectorView Create(Transform parent, Vector2 position, IReadOnlyList<int> stakes, AnimationSequencer sequencer)
        {
            const float spacing = 14f;
            const float labelWidth = 120f;
            float width = labelWidth + (ChipSize + spacing) * stakes.Count;

            RectTransform root = UiFactory.CreateRect("StakeSelector", parent).Place(new Vector2(0.5f, 0.5f), position, new Vector2(width, ChipSize));
            var view = root.gameObject.AddComponent<StakeSelectorView>();
            view._sequencer = sequencer;

            UiFactory.CreateText("Label", root, UiText.StakeLabel, 30, Palette.Gold, TextAnchor.MiddleLeft, FontStyle.Bold).WithShadow()
                .rectTransform.Place(new Vector2(0f, 0.5f), Vector2.zero, new Vector2(labelWidth, ChipSize), new Vector2(0f, 0.5f));

            for (int i = 0; i < stakes.Count; i++)
            {
                int stake = stakes[i];
                Sprite sprite = UiArt.Sprite(UiArt.Chip);
                Image image = UiFactory.CreateImage($"Chip{stake}", root, sprite != null ? Color.white : Palette.Button);
                image.sprite = sprite;
                image.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(labelWidth + i * (ChipSize + spacing), 0f), Vector2.one * ChipSize,
                    new Vector2(0f, 0.5f));

                var chip = image.gameObject.AddComponent<Button>();
                chip.targetGraphic = image;
                ColorBlock colors = chip.colors;
                colors.disabledColor = new Color(0.35f, 0.3f, 0.3f, 0.75f);
                chip.colors = colors;
                UiFactory.MakeClickOnly(chip);
                chip.onClick.AddListener(() => view.StakeChosen?.Invoke(stake));

                Text label = UiFactory.CreateText("Label", image.transform, stake.ToString(), 28, Palette.Bone, style: FontStyle.Bold).WithShadow();
                label.rectTransform.Stretch();
                view._chips.Add((stake, image, chip, label));
            }

            return view;
        }

        public void SetSelected(int stake)
        {
            _sequencer.Do(() =>
            {
                Sprite normal = UiArt.Sprite(UiArt.Chip);
                Sprite selected = UiArt.Sprite(UiArt.ChipSelected);
                foreach (var chip in _chips)
                {
                    bool isSelected = chip.stake == stake;
                    if (normal != null)
                        chip.image.sprite = isSelected ? selected : normal;
                    else
                        chip.image.color = isSelected ? Palette.Ember : Palette.Button;
                    chip.label.color = isSelected ? Palette.Ink : Palette.Bone;
                    Vector2 position = chip.image.rectTransform.anchoredPosition;
                    chip.image.rectTransform.anchoredPosition = new Vector2(position.x, isSelected ? SelectedLift : 0f);
                }
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
