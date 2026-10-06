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
    /// An earned relic (the Jester's Rattle) sits apart, left of the first tile, in a gold frame; a relic that has just joined the run
    /// flashes for a moment.
    /// </summary>
    public sealed class RelicBarView : MonoBehaviour
    {
        private const int Tile = 20;
        /// <summary>Two offered relics down the edge, the earned one apart (slot 2).</summary>
        private const int Slots = 3;
        private const int RewardSlot = 2;
        private const float NewFlashSeconds = 1.6f;
        private readonly RelicBadge[] _bySlot = new RelicBadge[Slots];
        private readonly float[] _flash = new float[Slots];

        private AnimationSequencer _sequencer;
        private Action<string> _pressed;
        private readonly List<(GameObject tile, Image icon, Text uses)> _tiles = new List<(GameObject, Image, Text)>();
        private GameObject _tooltip;
        private Text _tooltipText;
        private IReadOnlyList<RelicBadge> _relics = new RelicBadge[0];

        /// <summary>True while the tile in <paramref name="slot"/> flashes (a relic just joined); for tests and screenshots.</summary>
        public bool IsFlashing(int slot) => slot >= 0 && slot < Slots && _flash[slot] > 0f;

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
                if (i == RewardSlot)
                    tile.rectTransform.PlaceTL(-(Tile + 4), 0, Tile, Tile);   // apart: left of the first tile
                else
                    tile.rectTransform.PlaceTL(0, i * (Tile + 2), Tile, Tile);
                UiFactory.AddBorder(tile.gameObject, i == RewardSlot ? Palette.GoldLight : Palette.Lilac, 1f);
                tile.raycastTarget = true;
                var button = tile.gameObject.AddComponent<Button>();
                button.targetGraphic = tile;
                button.transition = Selectable.Transition.None;
                UiFactory.MakeClickOnly(button);
                button.onClick.AddListener(() =>
                {
                    if (_bySlot[slot] != null) _pressed?.Invoke(_bySlot[slot].Id);
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
            RelicBadge relic = _bySlot[slot];
            if (relic == null) return;
            _tooltipText.text = relic.Name + "\n" + relic.Description;
            // As tall as the words (the Bone Die's gift wraps to more lines than the others), whole pixels.
            int textHeight = Mathf.Max(34, Mathf.CeilToInt(_tooltipText.preferredHeight));
            _tooltipText.rectTransform.PlaceTL(4, 3, 188, textHeight);
            ((RectTransform)_tooltip.transform).PlaceTL(Tile + 6, slot == RewardSlot ? 0 : slot * (Tile + 2), 196, textHeight + 4);
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
            // The offered relics fill the edge in order; the earned one has its own slot. A relic new to the bar flashes.
            var before = new HashSet<string>();
            foreach (RelicBadge old in _relics) before.Add(old.Id);
            _relics = relics;
            Array.Clear(_bySlot, 0, Slots);
            int next = 0;
            foreach (RelicBadge relic in relics)
            {
                int slot = relic.IsReward ? RewardSlot : next < RewardSlot ? next++ : -1;
                if (slot < 0) continue;
                _bySlot[slot] = relic;
                if (!before.Contains(relic.Id) && (before.Count > 0 || relic.IsReward)) _flash[slot] = NewFlashSeconds;   // not the bar's first filling (a loaded run)
            }
            for (int i = 0; i < _tiles.Count; i++)
            {
                RelicBadge relic = _bySlot[i];
                _tiles[i].tile.SetActive(relic != null);
                if (relic == null) continue;
                _tiles[i].icon.sprite = UiArt.RelicIcon(relic.Id);
                _tiles[i].icon.enabled = _tiles[i].icon.sprite != null;
                _tiles[i].uses.text = relic.Uses >= 0 ? relic.Uses.ToString() : "";
            }
            if (relics.Count == 0) _tooltip.SetActive(false);
            for (int i = 0; i < _tiles.Count; i++) _tiles[i].tile.GetComponent<Image>().color = Palette.Black;
        }

        /// <summary>A relic being used (the Bone Die picking its card) pulses on a one-second beat until it is used or put away.</summary>
        private void Update()
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                if (_flash[i] > 0f)
                {
                    _flash[i] -= Time.unscaledDeltaTime;
                    _tiles[i].tile.GetComponent<Image>().color = _flash[i] > 0f && Mathf.Repeat(_flash[i], 0.3f) < 0.15f ? Palette.Gold : Palette.Black;
                    continue;
                }
                if (_bySlot[i] != null && _bySlot[i].Selecting)
                    _tiles[i].tile.GetComponent<Image>().color = Mathf.Repeat(Time.unscaledTime, 1f) < 0.5f ? Palette.Plum : Palette.Black;
            }
        }
    }
}
