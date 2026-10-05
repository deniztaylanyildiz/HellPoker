using System;
using System.Collections.Generic;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// The run's cursed relics beside the dealer's portrait: an icon each on a black tile (uses left, for one that is used),
    /// stacked down the box's right edge under the intent sign. Hover: the name, the gift and the curse. Click: use it (the Bone Die).
    /// </summary>
    public sealed class RelicBarView : MonoBehaviour
    {
        private const int Tile = 20;
        private const int Slots = 2;

        private AnimationSequencer _sequencer;
        private Action<string> _pressed;
        private readonly List<(GameObject tile, Image icon, Text uses)> _tiles = new List<(GameObject, Image, Text)>();
        private GameObject _tooltip;
        private Text _tooltipText;
        private IReadOnlyList<RelicBadge> _relics = new RelicBadge[0];

        /// <summary>The relics on show (for tests and screenshots).</summary>
        public IReadOnlyList<RelicBadge> Relics => _relics;

        public static RelicBarView Create(Transform screen, int x, int y, AnimationSequencer sequencer, Action<string> pressed)
        {
            RectTransform root = UiFactory.CreateRect("Relics", screen).PlaceTL(x, y, Tile, Slots * (Tile + 2));
            var view = root.gameObject.AddComponent<RelicBarView>();
            view._sequencer = sequencer;
            view._pressed = pressed;
            view.Build(root);
            view.Apply(new RelicBadge[0]);
            return view;
        }

        private void Build(RectTransform root)
        {
            Image tip = UiFactory.CreateImage("RelicTip", root, Palette.Black);
            tip.raycastTarget = false;
            tip.rectTransform.PlaceTL(Tile + 6, 0, 196, 38);
            UiFactory.AddBorder(tip.gameObject, Palette.Lilac, 1f);
            _tooltipText = UiFactory.CreateText("Text", tip.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft);
            _tooltipText.rectTransform.PlaceTL(4, 3, 188, 34);
            _tooltip = tip.gameObject;
            _tooltip.SetActive(false);

            for (int i = 0; i < Slots; i++)
            {
                int slot = i;
                Image tile = UiFactory.CreateImage("Relic" + i, root, Palette.Black);
                tile.rectTransform.PlaceTL(0, i * (Tile + 2), Tile, Tile);
                UiFactory.AddBorder(tile.gameObject, Palette.Lilac, 1f);
                tile.raycastTarget = true;
                var button = tile.gameObject.AddComponent<Button>();
                button.targetGraphic = tile;
                button.transition = Selectable.Transition.None;
                UiFactory.MakeClickOnly(button);
                button.onClick.AddListener(() =>
                {
                    if (slot < _relics.Count) _pressed?.Invoke(_relics[slot].Id);
                });
                Image icon = UiFactory.CreateImage("Icon", tile.transform, Color.white);
                icon.raycastTarget = false;
                icon.rectTransform.PlaceTL(2, 2, 16, 16);
                Text uses = UiFactory.CreateText("Uses", tile.transform, "", 8, Palette.GoldLight, TextAnchor.LowerRight, FontStyle.Bold).WithOutline();
                uses.rectTransform.PlaceTL(10, 10, 10, 9);
                uses.horizontalOverflow = HorizontalWrapMode.Overflow;

                var hover = tile.gameObject.AddComponent<EventTrigger>();
                AddTrigger(hover, EventTriggerType.PointerEnter, () => ShowTip(slot));
                AddTrigger(hover, EventTriggerType.PointerExit, () => _tooltip.SetActive(false));
                _tiles.Add((tile.gameObject, icon, uses));
            }
        }

        private void ShowTip(int slot)
        {
            if (slot >= _relics.Count) return;
            _tooltipText.text = _relics[slot].Name + "\n" + _relics[slot].Description;
            // As tall as the words (the Bone Die's gift wraps to more lines than the others), whole pixels.
            int textHeight = Mathf.Max(34, Mathf.CeilToInt(_tooltipText.preferredHeight));
            _tooltipText.rectTransform.PlaceTL(4, 3, 188, textHeight);
            ((RectTransform)_tooltip.transform).PlaceTL(Tile + 6, slot * (Tile + 2), 196, textHeight + 4);
            _tooltip.SetActive(true);
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        public void Set(IReadOnlyList<RelicBadge> relics) => _sequencer.Do(() => Apply(relics ?? new RelicBadge[0]));

        private void Apply(IReadOnlyList<RelicBadge> relics)
        {
            _relics = relics;
            for (int i = 0; i < _tiles.Count; i++)
            {
                bool shown = i < relics.Count;
                _tiles[i].tile.SetActive(shown);
                if (!shown) continue;
                _tiles[i].icon.sprite = UiArt.RelicIcon(relics[i].Id);
                _tiles[i].icon.enabled = _tiles[i].icon.sprite != null;
                _tiles[i].uses.text = relics[i].Uses >= 0 ? relics[i].Uses.ToString() : "";
            }
            if (relics.Count == 0) _tooltip.SetActive(false);
        }
    }
}
