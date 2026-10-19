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
    /// Demo 3.1: the player's own panel at a chapter's table, in the right column under the counter where the payouts stood (Phase 3 put
    /// them in the hands' panel). Top to bottom: the class's animated 64×64 portrait in its box (the upgrades as gold pips in its corner,
    /// "READY: K" over its foot), the class's name and its level pips, the passive's row (a small icon and its short name; it flashes when
    /// the passive works — the Jester's row counts his jokers), and the worn relics, three a row (the earned one last). Clicking the portrait
    /// or the passive's row does what K does; hovering them tells the class in full. The relics' tiles hover, click (the Bone Die), wear
    /// their rarity's frame and flash when new exactly as they did on the portrait's edge (it is the same <see cref="RelicBarView"/>).
    /// </summary>
    public sealed class PlayerPanelView : MonoBehaviour
    {
        public const int Width = 104;
        public const int Height = 174;

        private const int BoxSize = SinnerView.Size + 8;
        private const int BoxX = (Width - BoxSize) / 2, BoxY = 4;
        private const int NameY = BoxY + BoxSize + 3;
        private const int PipsY = NameY + 11;
        private const int PassiveY = PipsY + 7, PassiveHeight = 22;
        private const int RelicsY = PassiveY + PassiveHeight + 4;
        private const int PipSize = 4, PipStep = 6;
        private const float FlashSeconds = 1.2f;
        private const float PowerBeat = 1f;

        private AnimationSequencer _sequencer;
        private SinnerView _sinner;
        private Image _box;
        private Text _name;
        private readonly List<Image> _pips = new List<Image>();
        private readonly List<Image> _upgradePips = new List<Image>();
        private Image _passive;
        private Image _passiveIcon;
        private Text _passiveName;
        private Text _ready;
        private GameObject _tooltip;
        private Text _tooltipText;
        private float _flash;

        /// <summary>The badge on show (for tests and screenshots).</summary>
        public SinnerBadge Badge { get; private set; } = SinnerBadge.Hidden;

        public SinnerView Sinner => _sinner;
        public RelicBarView Relics { get; private set; }

        /// <summary>The passive's row as written (for tests).</summary>
        public Text PassiveName => _passiveName;

        public Text ClassName => _name;

        /// <summary>The upgrade pips lit (for tests).</summary>
        public int UpgradePipsShown { get; private set; }

        /// <summary>The passive's row is flashing (it just worked; for tests).</summary>
        public bool IsFlashing => _flash > 0f;

        public static PlayerPanelView Create(Transform screen, int x, int y, AnimationSequencer sequencer, SinnerAnimationLibrary sinners,
            Action pressed, Action<string> relicPressed)
        {
            Image panel = UiFactory.CreatePanel("PlayerPanel", screen);
            panel.rectTransform.PlaceTL(x, y, Width, Height);
            var view = panel.gameObject.AddComponent<PlayerPanelView>();
            view._sequencer = sequencer;
            view.Build(panel.rectTransform, sinners, pressed, relicPressed);
            view.Apply(SinnerBadge.Hidden);
            return view;
        }

        private void Build(RectTransform root, SinnerAnimationLibrary sinners, Action pressed, Action<string> relicPressed)
        {
            // The portrait in its box: a click is K.
            _box = UiFactory.CreateImage("PortraitBox", root, Palette.Black);
            _box.rectTransform.PlaceTL(BoxX, BoxY, BoxSize, BoxSize);
            UiFactory.AddBorder(_box.gameObject, Palette.Gold, 1f);
            _box.raycastTarget = true;
            Clickable(_box, pressed);
            _sinner = SinnerView.Create(_box.transform, 4, 4, _sequencer, sinners);

            // The upgrades: a gold pip each on a black square, in the box's top left corner.
            for (int i = 0; i < 4; i++)
            {
                Image back = UiFactory.CreateImage("Upgrade" + i, _box.transform, Palette.Black);
                back.raycastTarget = false;
                back.rectTransform.PlaceTL(1 + i * 6, 1, 5, 5);
                Image pip = UiFactory.CreateImage("Pip", back.transform, Palette.GoldLight);
                pip.raycastTarget = false;
                pip.rectTransform.PlaceTL(1, 1, 3, 3);
                _upgradePips.Add(back);
            }

            // "READY: K" / "POWER ON" over the portrait's foot.
            _ready = UiFactory.CreateText("PanelReady", _box.transform, "", 8, Palette.GoldLight, style: FontStyle.Bold).WithOutline();
            _ready.rectTransform.PlaceTL(-8, BoxSize - 14, BoxSize + 16, 8);
            _ready.horizontalOverflow = HorizontalWrapMode.Overflow;
            _ready.raycastTarget = false;

            _name = UiFactory.CreateText("PanelClass", root, "", 8, Palette.GoldLight, style: FontStyle.Bold).WithOutline();
            _name.rectTransform.PlaceTL(0, NameY, Width, 8);
            _name.horizontalOverflow = HorizontalWrapMode.Overflow;
            _name.raycastTarget = false;

            // The main power's level: pips under the name, centred.
            for (int i = 0; i < 5; i++)
            {
                Image pip = UiFactory.CreateImage("Level" + i, root, Palette.Plum);
                pip.raycastTarget = false;
                _pips.Add(pip);
            }

            // The passive's row: an icon and the short name, two lines at most. A click is K too.
            _passive = UiFactory.CreateImage("PassiveRow", root, Palette.Black);
            _passive.rectTransform.PlaceTL(4, PassiveY, Width - 8, PassiveHeight);
            UiFactory.AddBorder(_passive.gameObject, Palette.BoneDark, 1f);
            _passive.raycastTarget = true;
            Clickable(_passive, pressed);
            _passiveIcon = UiFactory.CreateImage("Icon", _passive.transform, Color.white);
            _passiveIcon.raycastTarget = false;
            _passiveIcon.rectTransform.PlaceTL(3, (PassiveHeight - UiArt.PassiveIconSize) / 2, UiArt.PassiveIconSize, UiArt.PassiveIconSize);
            _passiveName = UiFactory.CreateText("Name", _passive.transform, "", 8, Palette.Bone, TextAnchor.MiddleLeft);
            _passiveName.rectTransform.PlaceTL(18, 2, Width - 8 - 20, PassiveHeight - 4);
            _passiveName.lineSpacing = 1f;
            _passiveName.raycastTarget = false;

            Relics = RelicBarView.Create(root, (Width - (RelicBarView.GridColumns * RelicBarView.GridStep - 2)) / 2, RelicsY, _sequencer, relicPressed, grid: true);

            // What the class does, left of the panel, while the portrait or the passive is hovered.
            Image tip = UiFactory.CreateImage("PanelTip", root, Palette.Black);
            tip.raycastTarget = false;
            HoverTip.Make(tip.gameObject);
            UiFactory.AddBorder(tip.gameObject, Palette.Gold, 1f);
            _tooltipText = UiFactory.CreateText("Text", tip.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft);
            _tooltip = tip.gameObject;
            _tooltip.SetActive(false);
            foreach (Image hover in new[] { _box, _passive })
            {
                var trigger = hover.gameObject.AddComponent<EventTrigger>();
                AddTrigger(trigger, EventTriggerType.PointerEnter, () => ShowTip(hover == _box ? BoxY : PassiveY));
                AddTrigger(trigger, EventTriggerType.PointerExit, () => _tooltip.SetActive(false));
            }
        }

        private static void Clickable(Image image, Action pressed)
        {
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            UiFactory.MakeClickOnly(button);
            button.onClick.AddListener(() => pressed?.Invoke());
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        /// <summary>Points at the portrait as the pointer would (tests): the class in full.</summary>
        public void PointAtPortrait() => ShowTip(BoxY);

        /// <summary>Clicks the portrait / the passive's row as the pointer would (tests).</summary>
        public void PressPortrait() => _box.GetComponent<Button>().onClick.Invoke();

        public void PressPassive() => _passive.GetComponent<Button>().onClick.Invoke();

        private void ShowTip(int y)
        {
            if (!Badge.Visible) return;
            const int width = 188;
            _tooltipText.text = Badge.Name + "\n" + Badge.Description;
            _tooltipText.rectTransform.PlaceTL(4, 3, width - 8, 300);
            int textHeight = Mathf.Max(18, Mathf.CeilToInt(_tooltipText.preferredHeight));
            _tooltipText.rectTransform.PlaceTL(4, 3, width - 8, textHeight);
            ((RectTransform)_tooltip.transform).PlaceTL(-(width + 4), y, width, textHeight + 6);
            _tooltip.SetActive(true);
        }

        public void Set(SinnerBadge badge) => _sequencer.Do(() => Apply(badge ?? SinnerBadge.Hidden));

        public void SetMood(SinnerMood mood) => _sinner.SetMood(mood);

        public void SetRelics(IReadOnlyList<RelicBadge> relics) => Relics.Set(relics);

        private void Apply(SinnerBadge badge)
        {
            // A passive worked, or the Jester's count changed (not the first showing): the row flashes.
            if (badge.Passive && Badge.Passive && badge.PassiveFlashes > Badge.PassiveFlashes && Badge.ClassId == badge.ClassId)
                _flash = FlashSeconds;
            if (badge.ShowsJokers && Badge.ShowsJokers && badge.Jokers != Badge.Jokers && Badge.ClassId == badge.ClassId)
                _flash = FlashSeconds;
            Badge = badge;
            _box.gameObject.SetActive(badge.Visible);
            _passive.gameObject.SetActive(badge.Visible);
            _name.text = badge.Visible ? UiText.Upper(badge.Name) : "";
            if (!badge.Visible)
            {
                _tooltip.SetActive(false);
                foreach (Image pip in _pips) pip.enabled = false;
                UpgradePipsShown = 0;
                return;
            }
            _sinner.SetClass(badge.ClassId);

            UpgradePipsShown = Math.Min(_upgradePips.Count, badge.Upgrades);
            for (int i = 0; i < _upgradePips.Count; i++) _upgradePips[i].gameObject.SetActive(i < badge.Upgrades);

            int shown = Math.Min(_pips.Count, badge.Full);
            int left = (Width - (shown * PipStep - (PipStep - PipSize))) / 2;
            for (int i = 0; i < _pips.Count; i++)
            {
                _pips[i].enabled = i < shown;
                _pips[i].rectTransform.PlaceTL(left + i * PipStep, PipsY, PipSize, PipSize);
                _pips[i].color = i < badge.Charge ? Palette.GoldLight : Palette.Plum;
            }

            _passiveIcon.sprite = UiArt.PassiveIcon(badge.ClassId) ?? UiArt.SinnerIcon(badge.ClassId);
            _passiveIcon.enabled = _passiveIcon.sprite != null;
            _passiveName.text = badge.ShowsJokers ? string.Format(UiText.PanelJokersFormat, badge.Jokers)
                : UiText.ChapterPassiveName(badge.ClassId) ?? badge.Name;
            _passiveName.color = Palette.Bone;

            _ready.gameObject.SetActive(!badge.Passive || badge.Usable || badge.PowerOn);
            _ready.text = badge.PowerOn ? UiText.PowerOnLabel : badge.Usable ? UiText.PowerReadyHint : "";
            if (!badge.PowerOn) _box.color = Palette.Black;
        }

        /// <summary>The passive's flash (whole colours, no blending) and a power switched on pulsing the portrait's box on a one-second beat.</summary>
        private void Update()
        {
            if (!Badge.Visible) return;
            if (_flash > 0f)
            {
                _flash -= Time.unscaledDeltaTime;
                bool on = _flash > 0f && Mathf.Repeat(_flash, 0.3f) < 0.15f;
                _passive.color = on ? Palette.Gold : Palette.Black;
                _passiveName.color = on ? Palette.White : Palette.Bone;
            }
            if (Badge.PowerOn)
                _box.color = Mathf.Repeat(Time.unscaledTime, PowerBeat) < PowerBeat / 2f ? Palette.Plum : Palette.Black;
        }

        /// <summary>Whatever flashes ends now (skip; the portrait's own smile plays on — it holds nothing up).</summary>
        public void Finish()
        {
            _flash = 0f;
            _passive.color = Palette.Black;
            _passiveName.color = Palette.Bone;
        }
    }
}
