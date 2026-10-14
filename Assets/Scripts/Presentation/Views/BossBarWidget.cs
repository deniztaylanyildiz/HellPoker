using System;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// A demon's health bar, drawn in whole pixels and never with a number: a red fill on a dark track, a faint tick where the bar began,
    /// and right of its end the line it must not reach (the soul's, lilac; Lucifer's fall, ember). The fill darkens as it nears that
    /// line. A shrinking bar flashes the part it lost before it goes; a growing one fills the new part in dark red first. Used at the
    /// demon's table (under the portrait), on the map and at the gate (small).
    /// </summary>
    public sealed class BossBarWidget : MonoBehaviour
    {
        private const float FlashSeconds = 0.6f, GrowSeconds = 0.7f;

        private int _width, _height;
        private Image _fill, _change, _startTick, _line;
        private GameObject _tooltip;
        private Text _tooltipText;
        private BossBarDisplay _shown = BossBarDisplay.Hidden;
        private int _shownWidth = -1;
        private float _changeLeft;
        private bool _shrinking;

        /// <summary>The bar as last shown (for tests and screenshots).</summary>
        public BossBarDisplay Shown => _shown;

        /// <summary>The fill's width in pixels as last shown.</summary>
        public int FillWidth => Math.Max(0, _shownWidth);

        /// <param name="width">The bar's length, its frame included (the line's mark sits 2 pixels past it).</param>
        public static BossBarWidget Create(Transform parent, string name, int x, int y, int width, int height, bool hover = false)
        {
            Image frame = UiFactory.CreateImage(name, parent, Palette.Black);
            frame.rectTransform.PlaceTL(x, y, width, height);
            frame.raycastTarget = hover;
            var widget = frame.gameObject.AddComponent<BossBarWidget>();
            widget.Build(frame, width, height, hover);
            widget.Apply(BossBarDisplay.Hidden, animate: false);
            return widget;
        }

        private void Build(Image frame, int width, int height, bool hover)
        {
            _width = width - 2;
            _height = height - 2;
            Image track = UiFactory.CreateImage("Track", frame.transform, Palette.Black);
            track.raycastTarget = false;
            track.rectTransform.PlaceTL(1, 1, _width, _height);
            _fill = UiFactory.CreateImage("Fill", frame.transform, Palette.Red);
            _fill.raycastTarget = false;
            _change = UiFactory.CreateImage("Change", frame.transform, Palette.Spark);
            _change.raycastTarget = false;
            _change.enabled = false;
            _startTick = UiFactory.CreateImage("Start", frame.transform, Palette.BoneDark);
            _startTick.raycastTarget = false;
            _line = UiFactory.CreateImage("Line", frame.transform, Palette.Lilac);
            _line.raycastTarget = false;
            _line.rectTransform.PlaceTL(width + 1, -1, 2, height + 2);

            if (!hover) return;
            Image tip = UiFactory.CreateImage("BarTip", frame.transform, Palette.Black);
            tip.raycastTarget = false;
            UiFactory.AddBorder(tip.gameObject, Palette.Lilac, 1f);
            _tooltipText = UiFactory.CreateText("Text", tip.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft);
            _tooltipText.raycastTarget = false;
            _tooltip = tip.gameObject;
            _tooltip.SetActive(false);
            var trigger = frame.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => ShowTip());
            trigger.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => _tooltip.SetActive(false));
            trigger.triggers.Add(exit);
        }

        private void ShowTip()
        {
            if (_tooltip == null || string.IsNullOrEmpty(_shown.Hint)) return;
            const int width = 150;
            _tooltipText.text = _shown.Hint;
            _tooltipText.rectTransform.PlaceTL(4, 3, width - 8, 60);
            int height = Mathf.CeilToInt(_tooltipText.preferredHeight) + 6;
            _tooltipText.rectTransform.PlaceTL(4, 3, width - 8, height - 6);
            ((RectTransform)_tooltip.transform).PlaceTL(0, _height + 4, width, height);
            _tooltip.SetActive(true);
        }

        /// <summary>The bar now; <paramref name="animate"/>: a change from the last one is drawn (a flash out, a dark fill in).</summary>
        public void Apply(BossBarDisplay bar, bool animate)
        {
            bar = bar ?? BossBarDisplay.Hidden;
            gameObject.SetActive(!bar.IsHidden);
            if (bar.IsHidden)
            {
                _shown = bar;
                _shownWidth = -1;
                return;
            }
            int width = Mathf.RoundToInt(bar.Fill * _width);
            if (bar.Value > 0 && width == 0) width = 1;   // a bar not yet empty never looks empty
            int before = _shownWidth;
            _shown = bar;
            _shownWidth = width;

            _fill.rectTransform.PlaceTL(1, 1, width, _height);
            _fill.enabled = width > 0;
            _fill.color = ColorFor(bar.Fill);
            int tick = Mathf.Clamp(Mathf.RoundToInt(bar.StartMark * _width), 0, _width - 1);
            _startTick.rectTransform.PlaceTL(1 + tick, 1, 1, _height);
            _startTick.enabled = bar.StartMark < 0.995f;
            _line.color = bar.Line == BarLine.Fall ? Palette.Ember : Palette.Lilac;

            if (!animate || before < 0 || before == width)
            {
                _change.enabled = false;
                _changeLeft = 0f;
                return;
            }
            _shrinking = width < before;
            int from = Math.Min(before, width), to = Math.Max(before, width);
            _change.rectTransform.PlaceTL(1 + from, 1, to - from, _height);
            _change.enabled = true;
            _changeLeft = _shrinking ? FlashSeconds : GrowSeconds;
            _change.color = _shrinking ? Palette.Spark : Palette.BloodDark;
        }

        /// <summary>Darker as the bar nears its line.</summary>
        private static Color ColorFor(float fill) => fill < 0.6f ? Palette.Red : fill < 0.92f ? Palette.Crimson : Palette.BloodDark;

        private void Update()
        {
            // The last blow: the fill burns gold and ember, slowly (whole colours).
            if (_shown.LastBlow && _changeLeft <= 0f && _fill.enabled)
                _fill.color = Mathf.Repeat(Time.unscaledTime, 0.6f) < 0.3f ? Palette.GoldLight : Palette.Ember;
            else if (_changeLeft <= 0f && _fill.enabled)
                _fill.color = ColorFor(_shown.Fill);
            if (_changeLeft <= 0f) return;
            _changeLeft -= Time.unscaledDeltaTime;
            if (_changeLeft <= 0f)
            {
                _change.enabled = false;
                return;
            }
            // The lost part blinks white and gold before it goes; the gained part fills in dark red, then joins the fill.
            if (_shrinking) _change.color = Mathf.Repeat(_changeLeft, 0.2f) < 0.1f ? Palette.White : Palette.Spark;
        }
    }
}
