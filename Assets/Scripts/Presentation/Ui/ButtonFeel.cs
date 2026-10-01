using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HellPoker.Presentation.Ui
{
    /// <summary>
    /// How a pixel button answers the mouse: the label lights up on hover and sinks one pixel while pressed.
    /// A <see cref="Locked"/> button looks dull but still takes clicks, so the presenter can say why it is locked
    /// ("TABLE FULL", "SOUL BOUND") instead of the click vanishing.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ButtonFeel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private static readonly Color LockedTint = new Color(0.55f, 0.55f, 0.55f);

        private Image _background;
        private Color _backgroundColor = Color.white;
        private Text _label;
        private Vector2 _labelMin;
        private Vector2 _labelMax;
        private Color _labelColor = Palette.Bone;
        private bool _hover;
        private bool _pressed;
        private bool _locked;

        public bool Locked
        {
            get => _locked;
            set
            {
                if (_locked == value) return;
                _locked = value;
                Apply();
            }
        }

        /// <summary>The label's colour when the button is neither hovered nor locked.</summary>
        public Color LabelColor
        {
            get => _labelColor;
            set
            {
                _labelColor = value;
                Apply();
            }
        }

        public static ButtonFeel Attach(Button button, Text label)
        {
            var feel = button.gameObject.AddComponent<ButtonFeel>();
            feel._background = button.targetGraphic as Image;
            if (feel._background != null) feel._backgroundColor = feel._background.color;
            feel._label = label;
            feel._labelMin = label.rectTransform.offsetMin;
            feel._labelMax = label.rectTransform.offsetMax;
            feel._labelColor = label.color;
            feel.Apply();
            return feel;
        }

        public void OnPointerEnter(PointerEventData eventData) => Set(ref _hover, true);
        public void OnPointerExit(PointerEventData eventData)
        {
            _hover = false;
            Set(ref _pressed, false);
        }

        public void OnPointerDown(PointerEventData eventData) => Set(ref _pressed, true);
        public void OnPointerUp(PointerEventData eventData) => Set(ref _pressed, false);

        private void OnDisable()
        {
            _hover = false;
            _pressed = false;
            Apply();
        }

        private void Set(ref bool field, bool value)
        {
            field = value;
            Apply();
        }

        private void Apply()
        {
            if (_label == null) return;

            Vector2 sink = _pressed ? new Vector2(0f, -1f) : Vector2.zero;
            _label.rectTransform.offsetMin = _labelMin + sink;
            _label.rectTransform.offsetMax = _labelMax + sink;
            _label.color = _locked ? Palette.BoneDark : _hover ? Palette.GoldLight : _labelColor;
            if (_background != null)
                _background.color = _locked ? _backgroundColor * LockedTint : _backgroundColor;
        }
    }
}
