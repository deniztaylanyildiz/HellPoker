using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// The soul bar that replaces the year counter while the soul is on the table: a phial with a flickering pale flame
    /// and a bar of what is left of the soul. The part on the table blinks; wins fill the bar, losses burn it down.
    /// It shows shares only — there is no number anywhere. Appearing is a moment: the box flashes for a second.
    /// </summary>
    public sealed class SoulView : MonoBehaviour
    {
        public const int Width = 104;
        public const int Height = 54;
        private const int BarWidth = 72;
        private const int BarHeight = 8;
        private const float FillSeconds = 0.8f;
        private const float EntranceSeconds = 1.2f;

        private AnimationSequencer _sequencer;
        private Image _box;
        private Image _fill;
        private Image _risk;
        private Text _label;
        private Text _onTable;
        private Sprite _calmBox;
        private Sprite _hotBox;

        private float _shown = 1f;
        private float _from = 1f;
        private float _target = 1f;
        private float _atRisk;
        private float _elapsed = FillSeconds;
        private float _entrance;

        public bool IsShowing => gameObject.activeSelf;

        public static SoulView Create(Transform parent, int x, int y, AnimationSequencer sequencer)
        {
            RectTransform root = UiFactory.CreateRect("Soul", parent).PlaceTL(x, y, Width, Height);
            var view = root.gameObject.AddComponent<SoulView>();
            view._sequencer = sequencer;
            view.Build(root);
            root.gameObject.SetActive(false);
            return view;
        }

        private void Build(RectTransform root)
        {
            _box = UiFactory.CreatePanel("Panel", root);
            _box.rectTransform.Stretch();
            _calmBox = _box.sprite;
            _hotBox = UiArt.Sprite(UiArt.PanelHot);

            Image lamp = UiFactory.CreateImage("Lamp", root, Palette.LilacLight);
            lamp.raycastTarget = false;
            lamp.rectTransform.PlaceTL(6, 15, UiArt.SoulLampWidth, 24);
            var flicker = lamp.gameObject.AddComponent<SpriteFrameAnimator>();
            flicker.FallbackColor = Palette.LilacLight;
            Sprite[] frames = UiArt.Strip(UiArt.SoulLamp, UiArt.SoulLampWidth);
            flicker.Play(frames != null ? new SpriteClip(frames, 6f, loop: true) : null);

            _label = UiFactory.CreateText("Label", root, "", 8, Palette.LilacLight, TextAnchor.MiddleLeft, FontStyle.Bold).WithOutline().Localized(() => UiText.SoulLabel);
            _label.rectTransform.PlaceTL(26, 10, 76, 8);
            _label.horizontalOverflow = HorizontalWrapMode.Overflow;

            Image frame = UiFactory.CreateImage("BarFrame", root, Palette.Black);
            frame.rectTransform.PlaceTL(25, 23, BarWidth + 2, BarHeight + 2);
            UiFactory.AddBorder(frame.gameObject, Palette.BoneDark, 1f);

            _fill = UiFactory.CreateImage("Fill", frame.transform, Palette.LilacLight);
            _risk = UiFactory.CreateImage("AtRisk", frame.transform, Palette.Hell);
            foreach (Image bar in new[] { _fill, _risk })
            {
                RectTransform rect = bar.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                bar.raycastTarget = false;
            }

            _onTable = UiFactory.CreateText("OnTable", root, "", 8, Palette.Hell, TextAnchor.MiddleLeft);
            _onTable.rectTransform.PlaceTL(26, 37, 76, 9);
        }

        public void SetGauge(SoulGauge gauge)
        {
            _sequencer.Do(() =>
            {
                bool appearing = gauge.Visible && !gameObject.activeSelf;
                gameObject.SetActive(gauge.Visible);
                if (!gauge.Visible) return;

                if (appearing)
                {
                    _entrance = EntranceSeconds;
                    _shown = gauge.Remaining;
                    _from = gauge.Remaining;
                    _elapsed = FillSeconds;
                }
                else if (!Mathf.Approximately(gauge.Remaining, _target))
                {
                    _from = _shown;
                    _elapsed = 0f;
                }

                _target = gauge.Remaining;
                _atRisk = Mathf.Min(gauge.AtRisk, gauge.Remaining);
                _onTable.text = _atRisk > 0f ? UiText.SoulOnTable : "";
                Draw();
            });
        }

        /// <summary>The bar jumps to its new level (skip); the entrance flash stops.</summary>
        public void Snap()
        {
            _elapsed = FillSeconds;
            _shown = _target;
            _entrance = 0f;
            if (gameObject.activeInHierarchy) Draw();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime * AnimationClock.Speed;
            if (_elapsed < FillSeconds)
            {
                _elapsed += dt;
                float t = Mathf.Clamp01(_elapsed / FillSeconds);
                _shown = Mathf.Lerp(_from, _target, 1f - Mathf.Pow(1f - t, 3f));
            }

            if (_entrance > 0f)
                _entrance -= dt;

            Draw();
        }

        private void Draw()
        {
            int fill = Mathf.RoundToInt(BarWidth * Mathf.Clamp01(_shown));
            int risk = Mathf.Min(fill, Mathf.CeilToInt(BarWidth * _atRisk));
            bool falling = _elapsed < FillSeconds && _target < _from;
            bool rising = _elapsed < FillSeconds && _target > _from;
            bool blink = Mathf.Repeat(Time.unscaledTime, 0.5f) < 0.25f;

            _fill.rectTransform.anchoredPosition = new Vector2(1f, 0f);
            _fill.rectTransform.sizeDelta = new Vector2(fill - risk, BarHeight);
            _fill.color = falling ? Palette.Hell : rising ? Palette.Bone : Palette.LilacLight;

            _risk.rectTransform.anchoredPosition = new Vector2(1f + fill - risk, 0f);
            _risk.rectTransform.sizeDelta = new Vector2(risk, BarHeight);
            _risk.enabled = risk > 0;
            _risk.color = blink ? Palette.Hell : Palette.Bone;

            // The entrance: the box flashes hot and the label flickers.
            bool flash = _entrance > 0f && Mathf.Repeat(_entrance, 0.3f) < 0.15f;
            if (_hotBox != null)
                _box.sprite = flash ? _hotBox : _calmBox;
            _label.color = flash ? Palette.Bone : Palette.LilacLight;
        }
    }
}
