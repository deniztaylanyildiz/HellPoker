using System.Collections.Generic;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// Demo 3.5.1: the gauges under the demon's name, in his column. The first row (a floor's imp, a warden): a coin, "PURSE 14" and a gold
    /// bar of what is left of the purse he sat down with (a part lost flashes ember before it goes). The second row: a chip, "ANTE 4" and a
    /// dot for every hand before it grows (a dot dims as a hand passes; when the ante grows the word flares) — at a demon's table the fixed
    /// ante, with his rule, a rule he carries and his rage as small icons on the right. Each part opens its hover box to the left.
    /// Whole pixels, the palette's colours, no alpha.
    /// </summary>
    public sealed class SeatGaugesView : MonoBehaviour
    {
        public const int Width = TableLayout.Column, RowA = 0, RowB = 16, Height = 32;
        public const int BarX = 64, BarWidth = 36, BarHeight = 5, Icon = 16, IconGap = 1;
        private const int Dot = 3, DotStep = 5, MostDots = 6;
        private const float Flash = 0.4f;

        private RectTransform _purseRow;
        private Image _coin;
        private Text _purseText;
        private Image _barFill;
        private Image _barLost;
        private RectTransform _anteRow;
        private Image _chip;
        private Text _anteText;
        private readonly List<Image> _dots = new List<Image>();
        private readonly List<(Image box, Image icon)> _icons = new List<(Image, Image)>();
        private readonly List<string> _iconTips = new List<string>();
        private GameObject _tip;
        private Text _tipText;

        private SeatGauges _shown = SeatGauges.Hidden;
        private float _lostUntil = -1f, _flareUntil = -1f, _dimUntil = -1f;
        private int _dimDot = -1;

        /// <summary>For tests: what is shown, and the parts' places.</summary>
        public SeatGauges Shown => _shown;
        public Text PurseText => _purseText;
        public Text AnteText => _anteText;
        public int DotsShown { get; private set; }
        public int IconsShown { get; private set; }
        public int BarFillWidth { get; private set; }
        public RectTransform PurseRow => _purseRow;
        public RectTransform AnteRow => _anteRow;
        public bool Flaring => Time.unscaledTime < _flareUntil;

        /// <summary>The purse bar's middle on the screen (the coins of a won hand fly there).</summary>
        public Vector2Int PurseBarPoint
        {
            get
            {
                var rect = (RectTransform)transform;
                return new Vector2Int(Mathf.RoundToInt(rect.anchoredPosition.x) + BarX + BarWidth / 2, -Mathf.RoundToInt(rect.anchoredPosition.y) + RowA + 3);
            }
        }

        public static SeatGaugesView Create(Transform parent, int x, int y)
        {
            RectTransform root = UiFactory.CreateRect("SeatGauges", parent).PlaceTL(x, y, Width, Height);
            var view = root.gameObject.AddComponent<SeatGaugesView>();
            view.Build(root);
            view.Show(SeatGauges.Hidden);
            return view;
        }

        private static Image Pixel(Transform parent, string name, int x, int y, int w, int h, Color color)
        {
            Image image = UiFactory.CreateImage(name, parent, color);
            image.raycastTarget = false;
            image.rectTransform.PlaceTL(x, y, w, h);
            return image;
        }

        private void Build(RectTransform root)
        {
            Sprite[] small = UiArt.Strip("Ui/seat_gauges", 8);
            _purseRow = UiFactory.CreateRect("PurseRow", root).PlaceTL(0, RowA, Width, 9);
            Hover(Pixel(_purseRow, "Hit", 0, 0, Width, 9, Palette.Black), () => _shown.PurseTip);
            _purseRow.Find("Hit").GetComponent<Image>().color = Color.clear;
            _coin = Pixel(_purseRow, "Coin", 4, 0, 8, 8, Palette.Gold);
            SetSprite(_coin, small, 0);
            _purseText = UiFactory.CreateText("PurseText", _purseRow, "", 8, Palette.GoldLight, TextAnchor.UpperLeft).WithOutline();
            _purseText.rectTransform.PlaceTL(14, 0, BarX - 16, 9);
            _purseText.horizontalOverflow = HorizontalWrapMode.Overflow;
            Pixel(_purseRow, "BarEdge", BarX - 1, 1, BarWidth + 2, BarHeight + 2, Palette.Black);
            Pixel(_purseRow, "BarBack", BarX, 2, BarWidth, BarHeight, Palette.Night);
            _barLost = Pixel(_purseRow, "BarLost", BarX, 2, 0, BarHeight, Palette.Ember);
            _barFill = Pixel(_purseRow, "BarFill", BarX, 2, 0, BarHeight, Palette.Gold);

            _anteRow = UiFactory.CreateRect("AnteRow", root).PlaceTL(0, RowB, Width, Icon);
            Image anteHit = Pixel(_anteRow, "Hit", 0, 4, 60, 9, Color.clear);
            Hover(anteHit, () => _shown.AnteTip);
            _chip = Pixel(_anteRow, "Chip", 4, 4, 8, 8, Palette.Red);
            SetSprite(_chip, small, 1);
            _anteText = UiFactory.CreateText("AnteText", _anteRow, "", 8, Palette.Bone, TextAnchor.UpperLeft).WithOutline();
            _anteText.rectTransform.PlaceTL(14, 4, 60, 9);
            _anteText.horizontalOverflow = HorizontalWrapMode.Overflow;
            for (int i = 0; i < MostDots; i++)
                _dots.Add(Pixel(_anteRow, "Dot" + i, 0, 7, Dot, Dot, Palette.GoldLight));
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                Image box = Pixel(_anteRow, "RuleIcon" + i, Width - 4 - (i + 1) * Icon - i * IconGap, 0, Icon, Icon, Palette.Black);
                Image icon = Pixel(box.transform, "Icon", 0, 0, Icon, Icon, Color.white);
                Hover(box, () => index < _iconTips.Count ? _iconTips[index] : "");
                _icons.Add((box, icon));
            }

            Image tip = UiFactory.CreateImage("GaugeTip", root, Palette.Black);
            tip.raycastTarget = false;
            HoverTip.Make(tip.gameObject);
            UiFactory.AddBorder(tip.gameObject, Palette.Gold, 1f);
            _tipText = UiFactory.CreateText("Text", tip.transform, "", 8, Palette.Bone, TextAnchor.UpperLeft);
            _tipText.rectTransform.PlaceTL(4, 3, 180, 60);
            tip.rectTransform.PlaceTL(-190, 0, 188, 30);
            _tip = tip.gameObject;
            _tip.SetActive(false);
        }

        private static void SetSprite(Image image, Sprite[] sprites, int index)
        {
            if (sprites == null || index >= sprites.Length) return;
            image.sprite = sprites[index];
            image.color = Color.white;
        }

        private void Hover(Image target, System.Func<string> text)
        {
            target.raycastTarget = true;
            var trigger = target.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => OpenTip(text(), (RectTransform)target.transform));
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => _tip.SetActive(false));
            trigger.triggers.Add(enter);
            trigger.triggers.Add(exit);
        }

        private void OpenTip(string text, RectTransform over)
        {
            if (string.IsNullOrEmpty(text)) return;
            _tipText.text = text;
            int h = Mathf.Clamp(Mathf.CeilToInt(_tipText.preferredHeight) + 7, 14, 66);
            Vector3 local = transform.InverseTransformPoint(over.position);
            ((RectTransform)_tip.transform).PlaceTL(-190, Mathf.RoundToInt(-local.y), 188, h);
            _tip.SetActive(true);
        }

        public void Show(SeatGauges gauges)
        {
            gauges = gauges ?? SeatGauges.Hidden;
            SeatGauges before = _shown;
            _shown = gauges;
            gameObject.SetActive(gauges.Shown);
            if (!gauges.Shown)
            {
                _tip.SetActive(false);
                return;
            }

            _purseRow.gameObject.SetActive(gauges.PurseShown);
            if (gauges.PurseShown)
            {
                _purseText.text = UiText.GaugePurse + " " + gauges.Purse;
                int fill = Mathf.Clamp(Mathf.RoundToInt(BarWidth * (float)gauges.Purse / gauges.PurseStart), gauges.Purse > 0 ? 1 : 0, BarWidth);
                int was = BarFillWidth;
                BarFillWidth = fill;
                _barFill.rectTransform.PlaceTL(BarX, 2, fill, BarHeight);
                if (before.Shown && before.PurseShown && fill < was)
                {
                    _barLost.rectTransform.PlaceTL(BarX + fill, 2, was - fill, BarHeight);
                    _barLost.gameObject.SetActive(true);
                    _lostUntil = Time.unscaledTime + Flash;
                }
                else _barLost.gameObject.SetActive(false);
            }

            _anteText.text = gauges.AnteLabel;
            int dots = Mathf.Min(MostDots, gauges.AnteDots);
            bool sameTable = before.Shown && before.PurseShown == gauges.PurseShown;
            if (sameTable && gauges.AnteLabel != before.AnteLabel && before.AnteLabel.Length > 0) _flareUntil = Time.unscaledTime + Flash;   // the ante grew
            else if (sameTable && dots < DotsShown) { _dimDot = dots; _dimUntil = Time.unscaledTime + Flash; }   // a hand passed
            DotsShown = dots;
            int dotsX = 14 + Mathf.CeilToInt(_anteText.preferredWidth) + 3;
            for (int i = 0; i < _dots.Count; i++)
            {
                _dots[i].rectTransform.PlaceTL(dotsX + i * DotStep, 7, Dot, Dot);
                _dots[i].color = Palette.GoldLight;
                _dots[i].gameObject.SetActive(i < dots || (i == _dimDot && Time.unscaledTime < _dimUntil));
            }

            _iconTips.Clear();
            IconsShown = Mathf.Min(_icons.Count, gauges.Icons.Count);
            for (int i = 0; i < _icons.Count; i++)
            {
                bool on = i < IconsShown;
                _icons[i].box.gameObject.SetActive(on);
                if (!on) continue;
                SeatIcon icon = gauges.Icons[i];
                _iconTips.Add(icon.Tip);
                Sprite sprite = icon.IconId == SeatIcon.Rage ? UiArt.Sprite("Ui/seat_rage") : UiArt.CheatIcon(icon.IconId);
                _icons[i].icon.sprite = sprite;
                _icons[i].icon.color = sprite != null ? Color.white : (icon.IconId == SeatIcon.Rage ? Palette.Hell : Palette.Ember);
            }
            Tick();
        }

        private void Update() => Tick();

        private void Tick()
        {
            float now = Time.unscaledTime;
            if (_barLost != null && _barLost.gameObject.activeSelf && now >= _lostUntil) _barLost.gameObject.SetActive(false);
            if (_anteText != null) _anteText.color = now < _flareUntil ? Palette.Ember : Palette.Bone;
            if (_dimDot >= 0 && _dimDot < _dots.Count)
            {
                if (now < _dimUntil) _dots[_dimDot].color = Palette.BoneDark;
                else
                {
                    _dots[_dimDot].gameObject.SetActive(_dimDot < DotsShown);
                    _dots[_dimDot].color = Palette.GoldLight;
                    _dimDot = -1;
                }
            }
        }
    }
}
