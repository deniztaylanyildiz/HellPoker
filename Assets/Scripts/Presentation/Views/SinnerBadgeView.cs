using System;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// The player's class on the table: a small badge in the portrait box's corner — the class icon and what is left of its
    /// ability — on a black strip so it reads over any portrait. Hovering it tells the class and the ability; clicking it is
    /// the King's way to protect a card (as K is).
    /// </summary>
    public sealed class SinnerBadgeView : MonoBehaviour
    {
        private const int Width = 30;
        private const int Height = 18;

        private AnimationSequencer _sequencer;
        private Image _strip;
        private Image _icon;
        private Text _charges;
        private GameObject _tooltip;
        private Text _tooltipText;

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
            _charges = UiFactory.CreateText("Charges", _strip.transform, "", 8, Palette.GoldLight, TextAnchor.MiddleCenter, FontStyle.Bold);
            _charges.rectTransform.PlaceTL(18, 5, 11, 8);
            _charges.horizontalOverflow = HorizontalWrapMode.Overflow;

            // What the class can do, beside the portrait, while the badge is hovered.
            Image tip = UiFactory.CreateImage("SinnerTip", root, Palette.Black);
            tip.raycastTarget = false;
            tip.rectTransform.PlaceTL(Width + 6, -2, 188, 38);
            UiFactory.AddBorder(tip.gameObject, Palette.Gold, 1f);
            _tooltipText = UiFactory.CreateText("Text", tip.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft);
            _tooltipText.rectTransform.PlaceTL(4, 3, 180, 34);
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
            if (!badge.Visible)
            {
                _tooltip.SetActive(false);
                return;
            }
            _icon.sprite = UiArt.SinnerIcon(badge.ClassId);
            _icon.enabled = _icon.sprite != null;
            bool hasAbility = badge.MaxCharges > 0;
            _charges.text = hasAbility ? badge.Charges.ToString() : "";
            _charges.color = badge.Charges > 0 ? Palette.GoldLight : Palette.BoneDark;
            _tooltipText.text = badge.Name + "\n" + badge.Description +
                                (hasAbility ? "\n" + string.Format(UiText.SinnerChargesFormat, badge.Charges, badge.MaxCharges) : "");
        }
    }
}
