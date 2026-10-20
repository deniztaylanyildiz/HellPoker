using System;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// Demo 3.4: SAVE on its own small layer over the chapter's screens (the map's panels swallow clicks; the button stays pressable over
    /// them). On the map it stands beside MENU at the side panel's foot; at a table beside HANDS on the bottom row, dim, its hover saying
    /// that a table cannot be saved. A save flashes "SAVED" over it.
    /// </summary>
    public sealed class SaveButtonView : MonoBehaviour, ISaveButtonView
    {
        public const int SortingOrder = 46;

        /// <summary>The button's places on the 640×360 screen: the map's (beside its MENU) and the table's (beside HANDS).</summary>
        public const int MapX = 68, MapY = PixelScreen.Height - 30, MapWidth = 52;
        public const int TableX = 144, TableY = PixelScreen.Height - 22, TableWidth = 56;
        private const int Height = 18;
        private const float SavedSeconds = 1.2f;

        private Canvas _canvas;
        private Button _button;
        private ButtonFeel _feel;
        private GameObject _tip;
        private Text _saved;
        private Text _label;
        private float _savedLeft;
        private bool _enabled;

        public event Action Pressed;

        /// <summary>Where the button stands and whether it is lit (for tests and screenshots).</summary>
        public SaveButtonPlace Place { get; private set; }
        public bool Enabled => _enabled;
        public bool TipShown => _tip.activeSelf;
        public bool SavedShown => _saved.gameObject.activeSelf;

        public static SaveButtonView Create(Transform parent)
        {
            Canvas canvas = UiFactory.CreateScreen("SaveButtonCanvas", parent, SortingOrder, out RectTransform screen, letterbox: false);
            var view = canvas.gameObject.AddComponent<SaveButtonView>();
            view._canvas = canvas;
            view.Build(screen);
            view.Show(SaveButtonPlace.Hidden, false);
            return view;
        }

        private void Build(RectTransform screen)
        {
            _button = UiFactory.CreateButton("SaveButton", screen, "", 8, out Text label, ButtonSkin.Ash);
            label.Localized(() => UiText.SaveButton);
            _label = label;
            _button.onClick.AddListener(() =>
            {
                if (_enabled) Pressed?.Invoke();
                else ShowTip();   // dim: says why
            });
            _feel = _button.GetComponent<ButtonFeel>();
            var hover = _button.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => { if (!_enabled) ShowTip(); });
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => _tip.SetActive(false));
            hover.triggers.Add(enter);
            hover.triggers.Add(exit);

            Image tip = UiFactory.CreateImage("SaveTip", screen, Palette.Black);
            tip.raycastTarget = false;
            HoverTip.Make(tip.gameObject);
            UiFactory.AddBorder(tip.gameObject, Palette.Gold, 1f);
            Text text = UiFactory.CreateText("Text", tip.transform, "", 8, Palette.Bone, TextAnchor.MiddleCenter).Localized(() => UiText.SaveAtTableHint);
            text.rectTransform.PlaceTL(4, 2, 136, 10);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _tip = tip.gameObject;
            _tip.SetActive(false);

            // The flash takes the button's own label for a moment (the text font: SAVED fits the button; above it, it ran over the
            // map's RELICS).
            _saved = UiFactory.CreateText("SavedFlash", _button.transform, "", 8, Palette.GoldLight, TextAnchor.MiddleCenter).WithOutline()
                .Localized(() => UiText.SavedFlash);
            _saved.horizontalOverflow = HorizontalWrapMode.Overflow;
            _saved.raycastTarget = false;
            _saved.gameObject.SetActive(false);
        }

        public void Show(SaveButtonPlace place, bool enabled)
        {
            Place = place;
            _enabled = enabled;
            bool shown = place != SaveButtonPlace.Hidden;
            _canvas.enabled = shown;
            GetComponent<GraphicRaycaster>().enabled = shown;
            _button.gameObject.SetActive(shown);
            if (!shown)
            {
                _tip.SetActive(false);
                _saved.gameObject.SetActive(false);
                _label.enabled = true;
                _savedLeft = 0f;
                return;
            }
            bool map = place == SaveButtonPlace.Map;
            int x = map ? MapX : TableX, y = map ? MapY : TableY, width = map ? MapWidth : TableWidth;
            ((RectTransform)_button.transform).PlaceTL(x, y, width, Height);
            ((RectTransform)_tip.transform).PlaceTL(x, y - 16, 144, 14);
            _saved.rectTransform.PlaceTL(-8, 4, width + 16, 10);
            if (_feel != null) _feel.Locked = !enabled;
            if (enabled) _tip.SetActive(false);
        }

        private void ShowTip()
        {
            _tip.transform.SetAsLastSibling();
            _tip.SetActive(true);
        }

        /// <summary>Points at the button as the pointer would (tests): a dim one's hover box.</summary>
        public void PointAt()
        {
            if (!_enabled) ShowTip();
        }

        public void Saved()
        {
            _saved.gameObject.SetActive(true);
            _label.enabled = false;
            _savedLeft = SavedSeconds;
        }

        private void Update()
        {
            if (_savedLeft <= 0f) return;
            _savedLeft -= Time.unscaledDeltaTime;
            if (_savedLeft > 0f) return;
            _saved.gameObject.SetActive(false);
            _label.enabled = true;
        }
    }
}
