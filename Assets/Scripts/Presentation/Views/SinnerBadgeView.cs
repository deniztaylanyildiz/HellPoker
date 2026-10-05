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
    /// The player's class on the table: a small badge in the portrait box's corner — the class icon and its power's charge
    /// gauge as a row of pips, like the demon's malice — on a black strip so it reads over any portrait. A full gauge glows
    /// (the lit pips pulse); when the power can be used right now a short outlined hint sits on top ("READY: K"). Hovering tells
    /// the class, the power and the charge; clicking uses the power (as K does).
    /// </summary>
    public sealed class SinnerBadgeView : MonoBehaviour
    {
        public const int Width = 44;
        private const int Height = 18;
        private const int PipSize = 4;
        private const int PipStep = 5;
        private const float PulseSeconds = 0.5f;
        private const float PowerBeat = 1f;

        private AnimationSequencer _sequencer;
        private Image _strip;
        private Image _icon;
        private readonly List<Image> _pips = new List<Image>();
        private Text _hint;
        private GameObject _tooltip;
        private Text _tooltipText;
        private float _pulse;

        /// <summary>The badge on show (for tests and screenshots).</summary>
        public SinnerBadge Badge { get; private set; } = SinnerBadge.Hidden;

        public static SinnerBadgeView Create(Transform screen, int x, int y, AnimationSequencer sequencer, Action pressed)
        {
            RectTransform root = UiFactory.CreateRect("SinnerBadge", screen).PlaceTL(x, y, Width, Height);
            var view = root.gameObject.AddComponent<SinnerBadgeView>();
            view._sequencer = sequencer;
            view.Build(root, pressed);
            view.Apply(SinnerBadge.Hidden);
            return view;
        }

        private void Build(RectTransform root, Action pressed)
        {
            _strip = UiFactory.CreateImage("Strip", root, Palette.Black);
            _strip.rectTransform.Stretch();
            UiFactory.AddBorder(_strip.gameObject, Palette.Gold, 1f);
            _strip.raycastTarget = true;
            var button = _strip.gameObject.AddComponent<Button>();
            button.targetGraphic = _strip;
            button.transition = Selectable.Transition.None;
            UiFactory.MakeClickOnly(button);
            button.onClick.AddListener(() => pressed?.Invoke());

            _icon = UiFactory.CreateImage("Icon", _strip.transform, Color.white);
            _icon.raycastTarget = false;
            _icon.rectTransform.PlaceTL(1, 1, UiArt.SinnerIconSize, UiArt.SinnerIconSize);

            // The charge: five pips, whole pixels, beside the icon.
            for (int i = 0; i < 5; i++)
            {
                Image pip = UiFactory.CreateImage("Pip" + i, _strip.transform, Palette.Plum);
                pip.raycastTarget = false;
                pip.rectTransform.PlaceTL(UiArt.SinnerIconSize + 3 + i * PipStep, (Height - PipSize) / 2, PipSize, PipSize);
                _pips.Add(pip);
            }

            // "READY: K" over the badge when the power can be used now.
            _hint = UiFactory.CreateText("SinnerReady", root, "", 8, Palette.GoldLight, TextAnchor.LowerRight, FontStyle.Bold).WithOutline();
            _hint.rectTransform.PlaceTL(-60, -10, Width + 60, 8);
            _hint.horizontalOverflow = HorizontalWrapMode.Overflow;
            _hint.raycastTarget = false;

            // What the class can do, beside the portrait, while the badge is hovered.
            Image tip = UiFactory.CreateImage("SinnerTip", root, Palette.Black);
            tip.raycastTarget = false;
            tip.rectTransform.PlaceTL(Width + 6, -2, 188, 46);
            UiFactory.AddBorder(tip.gameObject, Palette.Gold, 1f);
            _tooltipText = UiFactory.CreateText("Text", tip.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft);
            _tooltipText.rectTransform.PlaceTL(4, 3, 180, 42);
            _tooltip = tip.gameObject;
            _tooltip.SetActive(false);

            var hover = _strip.gameObject.AddComponent<EventTrigger>();
            AddTrigger(hover, EventTriggerType.PointerEnter, () => _tooltip.SetActive(Badge.Visible));
            AddTrigger(hover, EventTriggerType.PointerExit, () => _tooltip.SetActive(false));
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        public void Set(SinnerBadge badge) => _sequencer.Do(() => Apply(badge ?? SinnerBadge.Hidden));

        private void Apply(SinnerBadge badge)
        {
            Badge = badge;
            _strip.gameObject.SetActive(badge.Visible);
            _hint.gameObject.SetActive(badge.Visible && (badge.Usable || badge.PowerOn));
            if (!badge.Visible)
            {
                _tooltip.SetActive(false);
                return;
            }
            _icon.sprite = UiArt.SinnerIcon(badge.ClassId);
            _icon.enabled = _icon.sprite != null;
            for (int i = 0; i < _pips.Count; i++)
            {
                _pips[i].enabled = i < badge.Full;
                _pips[i].color = i < badge.Charge ? Palette.GoldLight : Palette.Plum;
            }
            _hint.text = badge.PowerOn ? UiText.PowerOnLabel : badge.Usable ? UiText.PowerReadyHint : "";
            if (!badge.PowerOn) _strip.color = Palette.Black;
            _tooltipText.text = badge.Name + "\n" + badge.Description + "\n" +
                                (badge.WardRaised ? UiText.WardUpHint : UiText.SinnerCharge(badge.Charge, badge.Full));
            // As tall as the words, whole pixels.
            int textHeight = Mathf.Max(34, Mathf.CeilToInt(_tooltipText.preferredHeight));
            _tooltipText.rectTransform.PlaceTL(4, 3, 180, textHeight);
            ((RectTransform)_tooltip.transform).PlaceTL(Width + 6, -2, 188, textHeight + 4);
        }

        /// <summary>
        /// A full gauge glows: the lit pips switch between gold and ember. A power switched on (or a ward up) pulses the whole badge
        /// too, slowly — a one-second beat — so it cannot be missed (whole colours, no blending).
        /// </summary>
        private void Update()
        {
            if (!Badge.Visible) return;
            if (Badge.PowerOn)
                _strip.color = Mathf.Repeat(Time.unscaledTime, PowerBeat) < PowerBeat / 2f ? Palette.Plum : Palette.Black;
            if (!Badge.IsCharged) return;
            _pulse += Time.unscaledDeltaTime;
            bool bright = (int)(_pulse / PulseSeconds) % 2 == 0;
            for (int i = 0; i < _pips.Count && i < Badge.Charge; i++)
                _pips[i].color = bright ? Palette.GoldLight : Palette.Ember;
        }
    }
}
