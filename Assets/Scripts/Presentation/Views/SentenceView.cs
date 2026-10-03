using System.Collections.Generic;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// The remaining sentence in big gold pixel digits that count up and down, with a bar filling toward damnation.
    /// In the final stretch the counter throbs like a heartbeat (a one-pixel jump) and the label flashes red.
    /// </summary>
    public sealed class SentenceView : MonoBehaviour, ISentenceView
    {
        public const int Width = 104;
        public const int Height = 54;
        private const float CountDuration = 0.8f;
        private const int MaxDigits = 5;
        private const int DigitHeight = 16;

        private readonly List<Image> _digits = new List<Image>();
        private Sprite[] _digitSprites;
        private Text _fallbackNumber;
        private RectTransform _digitRow;
        private Text _label;
        private Text _damnation;
        private RectTransform _barFill;
        private int _barWidth;

        private int _damnationYears = 1;
        private float _shown = -1f;
        private float _from;
        private int _target;
        private float _elapsed = CountDuration;
        private int _displayed = -1;

        private AnimationSequencer _sequencer;
        private bool _pulsing;
        private float _lossFlash;
        private const float LossFlashSeconds = 1.2f;

        public static SentenceView Create(Transform parent, int x, int y, AnimationSequencer sequencer)
        {
            RectTransform root = UiFactory.CreateRect("Sentence", parent).PlaceTL(x, y, Width, Height);
            var view = root.gameObject.AddComponent<SentenceView>();
            view._sequencer = sequencer;
            view.Build(root);
            return view;
        }

        /// <summary>Makes the counter throb like a heartbeat (final stretch).</summary>
        public void SetPulsing(bool pulsing)
        {
            _pulsing = pulsing;
            if (!pulsing)
            {
                _label.color = Palette.BoneMid;
                _digitRow.anchoredPosition = new Vector2(_digitRow.anchoredPosition.x, -6f);
            }
        }

        private void Build(RectTransform root)
        {
            UiFactory.CreatePanel("Panel", root).rectTransform.Stretch();

            _digitSprites = UiArt.Strip(UiArt.Digits, UiArt.DigitWidth);
            _digitRow = UiFactory.CreateRect("Digits", root).PlaceTL(0, 6, Width, DigitHeight);
            for (int i = 0; i < MaxDigits; i++)
            {
                Image digit = UiFactory.CreateImage("Digit" + i, _digitRow, Color.white);
                digit.raycastTarget = false;
                digit.enabled = false;
                _digits.Add(digit);
            }
            _fallbackNumber = UiFactory.CreateText("Years", _digitRow, "", 16, Palette.GoldLight, style: FontStyle.Bold).WithOutline();
            _fallbackNumber.rectTransform.Stretch();
            _fallbackNumber.enabled = _digitSprites == null;

            _label = UiFactory.CreateText("Label", root, UiText.YearsLabel, 8, Palette.BoneMid);
            _label.rectTransform.PlaceTL(0, 24, Width, 9);

            _barWidth = Width - 16;
            Image bar = UiFactory.CreateImage("Bar", root, Palette.Black);
            bar.rectTransform.PlaceTL(8, 35, _barWidth, 4);
            Image fill = UiFactory.CreateImage("Fill", bar.transform, Palette.Hell);
            _barFill = fill.rectTransform;
            _barFill.anchorMin = new Vector2(0f, 0f);
            _barFill.anchorMax = new Vector2(0f, 1f);
            _barFill.pivot = new Vector2(0f, 0.5f);
            _barFill.anchoredPosition = Vector2.zero;

            _damnation = UiFactory.CreateText("Damnation", root, "", 8, Palette.BoneDark);
            _damnation.rectTransform.PlaceTL(0, 41, Width, 9);
        }

        public void SetSoulLine(int years) => SetLimit(years, string.Format(UiText.SoulLineFormat, years));

        public void SetLimit(int years, string text)
        {
            _sequencer.Do(() =>
            {
                _damnationYears = Mathf.Max(1, years);
                _damnation.text = text ?? "";
                Apply(_shown < 0f ? _target : _shown);
            });
        }

        public void SetLabel(string text)
        {
            _sequencer.Do(() => _label.text = text ?? "");
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetYears(int years, bool animate)
        {
            _sequencer.Do(() =>
            {
                _target = years;
                _from = _shown < 0f ? years : _shown;
                _elapsed = animate ? 0f : CountDuration;
                if (!animate)
                    Apply(years);
            });
        }

        private void Update()
        {
            if (_pulsing)
            {
                // A heartbeat: two quick thumps, then a rest.
                float beat = Mathf.Repeat(Time.unscaledTime, 1.2f);
                bool thump = beat < 0.1f || (beat > 0.22f && beat < 0.32f);
                _digitRow.anchoredPosition = new Vector2(_digitRow.anchoredPosition.x, thump ? -5f : -6f);
                _label.color = Mathf.Repeat(Time.unscaledTime, 0.8f) < 0.4f ? Palette.Hell : Palette.BoneMid;
            }

            if (_lossFlash > 0f)
            {
                _lossFlash -= Time.unscaledDeltaTime * AnimationClock.Speed;
                TintDigits(_lossFlash > 0f && Mathf.Repeat(_lossFlash, 0.2f) < 0.1f ? Palette.Hell : Color.white);
            }

            if (_elapsed >= CountDuration) return;

            _elapsed += Time.unscaledDeltaTime * AnimationClock.Speed;
            float t = Mathf.Clamp01(_elapsed / CountDuration);
            Apply(Mathf.Lerp(_from, _target, 1f - Mathf.Pow(1f - t, 3f)));
        }

        /// <summary>The counter flashes red for a moment (a heavy loss).</summary>
        public void FlashLoss()
        {
            _lossFlash = LossFlashSeconds;
        }

        /// <summary>Jumps the count to where it is heading (skip).</summary>
        public void Snap()
        {
            _lossFlash = 0f;
            TintDigits(Color.white);
            if (_elapsed >= CountDuration) return;
            _elapsed = CountDuration;
            Apply(_target);
        }

        private void TintDigits(Color color)
        {
            foreach (Image digit in _digits)
                digit.color = color;
            _fallbackNumber.color = color == Color.white ? Palette.GoldLight : color;
        }

        private void Apply(float years)
        {
            _shown = years;
            int value = Mathf.Max(0, Mathf.RoundToInt(years));
            _barFill.sizeDelta = new Vector2(Mathf.Round(_barWidth * Mathf.Clamp01(years / _damnationYears)), 0f);
            if (value == _displayed) return;
            _displayed = value;

            string text = value.ToString();
            _fallbackNumber.text = text;
            if (_digitSprites == null) return;

            // Centre the digits on whole pixels.
            int count = Mathf.Min(text.Length, MaxDigits);
            int left = (Width - count * UiArt.DigitWidth) / 2;
            for (int i = 0; i < _digits.Count; i++)
            {
                Image digit = _digits[i];
                bool used = i < count;
                digit.enabled = used;
                if (!used) continue;

                digit.sprite = _digitSprites[text[text.Length - count + i] - '0'];
                digit.rectTransform.PlaceTL(left + i * UiArt.DigitWidth, 0, UiArt.DigitWidth, DigitHeight);
            }
        }
    }
}
