using System;
using System.Collections.Generic;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// A Phase 2 run's relics (Demo 2.1): five worn places over five of the bag, a tile each (the relic's icon in its rarity's frame, an
    /// ember mark on a forged one). At a camp a click picks a relic (ember) and the next click puts it there — the two change places —;
    /// LEAVE IT BEHIND asks once more. The worn relics' sum is written under the places, live; hovering a place tells its gift and curse.
    /// Elsewhere (the map's RELICS) it is a look only. Over the map and its panels; clicks behind it are swallowed.
    /// </summary>
    public sealed class RelicBagView : MonoBehaviour, IRelicBagView
    {
        public const int SortingOrder = 50;

        private const int X = 96, Y = 46, Width = 288, Height = 178;
        private const int Tile = 22, Step = 28, Left = 10;
        private const int WornY = 34, BagY = 78;

        private Canvas _canvas;
        private Text _totals;
        private Text _message;
        private GameObject _tip;
        private Text _tipText;
        private Button _drop;
        private Text _dropLabel;
        private readonly List<(Image tile, Image icon, Outline frame, Image forged)> _places = new List<(Image, Image, Outline, Image)>();
        private RelicBagState _state;

        public event Action<int> PlacePressed;
        public event Action DonePressed;
        public event Action DropPressed;

        public bool IsOpen => _canvas.enabled;

        /// <summary>The state on show (for tests and screenshots).</summary>
        public RelicBagState State => _state;

        public static RelicBagView Create(Transform parent)
        {
            Canvas canvas = UiFactory.CreateScreen("RelicBagCanvas", parent, SortingOrder, out RectTransform screen, letterbox: false);
            var view = canvas.gameObject.AddComponent<RelicBagView>();
            view._canvas = canvas;
            view.Build(screen);
            view.Hide();
            return view;
        }

        private void Build(RectTransform screen)
        {
            Image shade = UiFactory.CreateImage("BagShade", screen, Color.clear);
            shade.rectTransform.Stretch();
            shade.raycastTarget = true;

            Image panel = UiFactory.CreatePanel("BagPanel", screen, hot: true);
            panel.raycastTarget = true;
            panel.rectTransform.PlaceTL(X, Y, Width, Height);

            UiFactory.CreateText("BagTitle", panel.transform, "", 8, Palette.GoldLight, TextAnchor.UpperLeft, FontStyle.Bold).WithOutline()
                .Localized(() => UiText.BagTitle).rectTransform.PlaceTL(Left, 8, Width - 20, 10);
            UiFactory.CreateText("BagWornLabel", panel.transform, "", 8, Palette.BoneMid, TextAnchor.UpperLeft).Localized(() => UiText.BagWornLabel)
                .rectTransform.PlaceTL(Left, WornY - 11, 140, 9);
            UiFactory.CreateText("BagBagLabel", panel.transform, "", 8, Palette.BoneMid, TextAnchor.UpperLeft).Localized(() => UiText.BagBagLabel)
                .rectTransform.PlaceTL(Left, BagY - 11, 140, 9);

            for (int i = 0; i < RelicBagState.Slots; i++)
            {
                int place = i;
                int x = Left + (i % RelicBagState.Worn) * Step, y = i < RelicBagState.Worn ? WornY : BagY;
                Image tile = UiFactory.CreateImage("BagPlace" + i, panel.transform, Palette.Black);
                tile.rectTransform.PlaceTL(x, y, Tile, Tile);
                tile.raycastTarget = true;
                Outline frame = UiFactory.AddBorder(tile.gameObject, Palette.BoneDark, 1f);
                var button = tile.gameObject.AddComponent<Button>();
                button.targetGraphic = tile;
                button.transition = Selectable.Transition.None;
                UiFactory.MakeClickOnly(button);
                button.onClick.AddListener(() => PlacePressed?.Invoke(place));
                Image icon = UiFactory.CreateImage("Icon", tile.transform, Color.white);
                icon.raycastTarget = false;
                icon.rectTransform.PlaceTL(3, 3, 16, 16);
                Image forged = UiFactory.CreateImage("Forged", tile.transform, Palette.Ember);
                forged.raycastTarget = false;
                forged.rectTransform.PlaceTL(Tile - 5, 1, 4, 4);
                var hover = tile.gameObject.AddComponent<EventTrigger>();
                AddTrigger(hover, EventTriggerType.PointerEnter, () => ShowTip(place));
                AddTrigger(hover, EventTriggerType.PointerExit, () => ShowTip(_state?.Cursor ?? -1));
                _places.Add((tile, icon, frame, forged));
            }

            // The hover's words, right of the places.
            Image tip = UiFactory.CreateImage("BagTip", panel.transform, Palette.Black);
            tip.raycastTarget = false;
            tip.rectTransform.PlaceTL(Left + RelicBagState.Worn * Step + 4, WornY - 2, Width - (Left + RelicBagState.Worn * Step + 4) - 8, BagY + Tile - WornY + 4);
            UiFactory.AddBorder(tip.gameObject, Palette.Lilac, 1f);
            _tipText = UiFactory.CreateText("BagTipText", tip.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft);
            _tipText.rectTransform.PlaceTL(4, 3, Width - (Left + RelicBagState.Worn * Step + 4) - 16, BagY + Tile - WornY - 2);
            _tip = tip.gameObject;

            _totals = UiFactory.CreateText("BagTotals", panel.transform, "", 8, Palette.GoldLight, TextAnchor.UpperLeft);
            _totals.rectTransform.PlaceTL(Left, BagY + Tile + 8, Width - 2 * Left, 20);
            _message = UiFactory.CreateText("BagMessage", panel.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft);
            _message.rectTransform.PlaceTL(Left, BagY + Tile + 30, Width - 2 * Left, 18);

            _drop = UiFactory.CreateButton("BagDropButton", panel.transform, "", 8, out _dropLabel, ButtonSkin.Ash);
            ((RectTransform)_drop.transform).PlaceTL(Left, Height - 26, 132, 18);
            _drop.onClick.AddListener(() => DropPressed?.Invoke());
            Button done = UiFactory.CreateButton("BagDoneButton", panel.transform, "", 8, out Text doneLabel);
            doneLabel.Localized(() => UiText.BagDone);
            ((RectTransform)done.transform).PlaceTL(Width - Left - 100, Height - 26, 100, 18);
            done.onClick.AddListener(() => DonePressed?.Invoke());
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        private void ShowTip(int place)
        {
            RelicBadge relic = _state != null && place >= 0 && place < _state.Places.Count ? _state.Places[place] : null;
            _tipText.text = relic == null ? (_state?.Editable ?? false ? UiText.BagHint : "") : relic.Name + (relic.Forged ? " " + UiText.BagForgedMark : "") + "\n" + relic.Description;
        }

        public void Show(RelicBagState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _canvas.enabled = true;
            GetComponent<GraphicRaycaster>().enabled = true;
            for (int i = 0; i < _places.Count; i++)
            {
                RelicBadge relic = state.Places[i];
                var (tile, icon, frame, forged) = _places[i];
                icon.sprite = relic == null ? null : UiArt.RelicIcon(relic.Id);
                icon.enabled = icon.sprite != null;
                forged.enabled = relic != null && relic.Forged;
                bool picked = i == state.Picked, cursor = i == state.Cursor;
                tile.color = picked ? Palette.Ember : Palette.Black;
                frame.effectColor = cursor ? Palette.GoldLight : relic == null ? Palette.BoneDark : RelicBarView.FrameOf(relic.Rarity);
            }
            _totals.text = state.Totals;
            _message.text = state.Message;
            _drop.gameObject.SetActive(state.Editable);
            _dropLabel.text = state.ConfirmingDrop ? UiText.BagDropSure : UiText.BagDrop;
            ShowTip(state.Cursor);
        }

        public void Hide()
        {
            _canvas.enabled = false;
            GetComponent<GraphicRaycaster>().enabled = false;
        }
    }
}
