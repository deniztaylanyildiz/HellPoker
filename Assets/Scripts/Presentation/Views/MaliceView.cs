using System.Collections;
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
    /// The demon's malice, worn on the portrait box: a row of pips along the bottom in the demon's own style (Mammon's coins,
    /// Belial's scales, Lilith's thorns, Lucifer's one ember), a new pip blinking as it fills; and, when the gauge is full,
    /// the announced cheat across the top — its icon and name. Hovering the sign tells what the cheat does. When Belial's
    /// sign was a lie it flickers "LIAR", shudders apart and shows the truth. Everything waits its turn in the table's
    /// sequencer and gives way to a skip.
    /// </summary>
    public sealed class MaliceView : MonoBehaviour
    {
        private const int Pip = 8;
        private const int PipGap = 2;
        private const int MaxPips = 6;
        private const float BlinkSeconds = 0.8f;

        private AnimationSequencer _sequencer;
        private readonly List<Image> _pips = new List<Image>();
        private Image _pipStrip;
        private GameObject _banner;
        private RectTransform _bannerRect;
        private Image _icon;
        private Text _name;
        private Text _liar;
        private GameObject _tooltip;
        private Text _tooltipText;
        private MaliceGauge _gauge = MaliceGauge.Hidden;
        private int _blinkPip = -1;
        private float _blinkLeft;
        private int _size;

        /// <summary>The intent on show (for tests and screenshots); null when none.</summary>
        public CheatCard Intent { get; private set; }

        public static MaliceView Create(Transform screen, int x, int y, int boxSize, AnimationSequencer sequencer)
        {
            RectTransform root = UiFactory.CreateRect("Malice", screen).PlaceTL(x, y, boxSize, boxSize);
            var view = root.gameObject.AddComponent<MaliceView>();
            view._sequencer = sequencer;
            view._size = boxSize;
            view.Build(root);
            return view;
        }

        private void Build(RectTransform root)
        {
            // Pips along the bottom of the box, on a black strip so they read over any portrait.
            _pipStrip = UiFactory.CreateImage("PipStrip", root, Palette.Black);
            _pipStrip.raycastTarget = false;
            for (int i = 0; i < MaxPips; i++)
            {
                Image pip = UiFactory.CreateImage("Pip" + i, _pipStrip.transform, Color.white);
                pip.raycastTarget = false;
                pip.rectTransform.PlaceTL(2 + i * (Pip + PipGap), 2, Pip, Pip);
                _pips.Add(pip);
            }

            // The announced cheat across the top: a black, hellfire-edged sign.
            Image banner = UiFactory.CreateImage("Intent", root, Palette.Black);
            banner.rectTransform.PlaceTL(2, 2, _size - 4, 18);
            UiFactory.AddBorder(banner.gameObject, Palette.Hell, 1f);
            banner.raycastTarget = true;
            _banner = banner.gameObject;
            _bannerRect = banner.rectTransform;
            _icon = UiFactory.CreateImage("Icon", banner.transform, Color.white);
            _icon.raycastTarget = false;
            _icon.rectTransform.PlaceTL(1, 1, UiArt.CheatIconSize, UiArt.CheatIconSize);
            _name = UiFactory.CreateText("Name", banner.transform, "", 8, Palette.Ember, TextAnchor.MiddleLeft);
            _name.rectTransform.PlaceTL(19, 5, _size - 26, 8);
            _name.horizontalOverflow = HorizontalWrapMode.Overflow;
            _liar = UiFactory.CreateText("Liar", banner.transform, UiText.LiarFlash, 8, Palette.Hell, TextAnchor.MiddleCenter, FontStyle.Bold).WithOutline();
            _liar.rectTransform.PlaceTL(0, 5, _size - 4, 8);
            _liar.enabled = false;

            // What the cheat does, beside the portrait, while the sign is hovered.
            Image tip = UiFactory.CreateImage("IntentTip", root, Palette.Black);
            tip.raycastTarget = false;
            tip.rectTransform.PlaceTL(_size + 2, 2, 188, 30);
            UiFactory.AddBorder(tip.gameObject, Palette.Hell, 1f);
            _tooltipText = UiFactory.CreateText("Text", tip.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft);
            _tooltipText.rectTransform.PlaceTL(4, 3, 180, 26);
            _tooltip = tip.gameObject;
            _tooltip.SetActive(false);

            var hover = banner.gameObject.AddComponent<EventTrigger>();
            AddTrigger(hover, EventTriggerType.PointerEnter, () => _tooltip.SetActive(Intent != null));
            AddTrigger(hover, EventTriggerType.PointerExit, () => _tooltip.SetActive(false));

            ApplyGauge(MaliceGauge.Hidden);
            ApplyIntent(null);
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, System.Action action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        public void SetGauge(MaliceGauge gauge) => _sequencer.Do(() => ApplyGauge(gauge));

        public void SetIntent(CheatCard intent) => _sequencer.Do(() => ApplyIntent(intent));

        /// <summary>The sign was a lie: "LIAR" flickers over it, it shudders apart, and the truth takes its place.</summary>
        public void RevealLie(CheatCard truth) => _sequencer.Play(Shatter(truth));

        /// <summary>Whatever is still blinking or shuddering ends now (skip).</summary>
        public void Finish()
        {
            _blinkLeft = 0f;
            _liar.enabled = false;
            _bannerRect.anchoredPosition = new Vector2(2, -2);
            if (_blinkPip >= 0 && _blinkPip < _pips.Count) _pips[_blinkPip].enabled = _gauge.Visible && _blinkPip < _gauge.Max;
        }

        private void ApplyGauge(MaliceGauge gauge)
        {
            int before = _gauge.DealerId == gauge.DealerId ? _gauge.Value : gauge.Value;
            _gauge = gauge;
            int count = Mathf.Min(gauge.Max, MaxPips);
            _pipStrip.enabled = gauge.Visible;
            _pipStrip.rectTransform.PlaceTL(4, _size - Pip - 8, 4 + count * (Pip + PipGap) - PipGap, Pip + 4);
            for (int i = 0; i < _pips.Count; i++)
            {
                bool used = gauge.Visible && i < count;
                _pips[i].enabled = used;
                if (!used) continue;
                Sprite sprite = UiArt.MalicePip(gauge.DealerId, full: i < gauge.Value);
                _pips[i].sprite = sprite;
                _pips[i].color = sprite != null ? Color.white : i < gauge.Value ? Palette.Hell : Palette.Dusk;
            }

            // The pip that just filled blinks a moment.
            if (gauge.Visible && gauge.Value > before && gauge.Value <= count)
            {
                _blinkPip = gauge.Value - 1;
                _blinkLeft = BlinkSeconds;
            }
        }

        private void ApplyIntent(CheatCard intent)
        {
            Intent = intent;
            _banner.SetActive(intent != null);
            if (intent == null)
            {
                _tooltip.SetActive(false);
                return;
            }
            _icon.sprite = UiArt.CheatIcon(intent.Id);
            _icon.enabled = _icon.sprite != null;
            _name.text = intent.Name;
            _tooltipText.text = intent.Description;
            _banner.transform.SetAsLastSibling();
        }

        private IEnumerator Shatter(CheatCard truth)
        {
            for (int i = 0; i < 4; i++)
            {
                _liar.enabled = i % 2 == 0;
                _name.enabled = !_liar.enabled;
                yield return Tween.Wait(0.09f);
            }
            _liar.enabled = false;
            _name.enabled = true;
            foreach (int dx in new[] { 2, -2, 1, -1, 0 })
            {
                _bannerRect.anchoredPosition = new Vector2(2 + dx, -2 + (dx != 0 ? 1 : 0));
                yield return Tween.Wait(0.05f);
            }
            ApplyIntent(truth);
        }

        private void Update()
        {
            if (_blinkLeft <= 0f) return;
            _blinkLeft -= Time.unscaledDeltaTime * AnimationClock.Speed;
            if (_blinkPip >= 0 && _blinkPip < _pips.Count)
                _pips[_blinkPip].enabled = _blinkLeft <= 0f || Mathf.Repeat(_blinkLeft, 0.2f) < 0.12f;
        }
    }
}
