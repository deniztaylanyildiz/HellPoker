using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// The end-of-sentence look, in pixels: the demon's hall turns to its burning variant, rows of pixel flames burn along
    /// the top and bottom edges of the screen, and a warning banner blinks. No blending — every colour stays in the palette.
    /// </summary>
    public sealed class FinalStretchEffect : MonoBehaviour
    {
        private const float FlameFps = 8f;
        private const int FlameHeight = 20;

        private SalonView _salon;
        private Image _flamesBottom;
        private Image _flamesTop;
        private Sprite[] _flameFrames;
        private Text _banner;
        private bool _active;

        /// <param name="salon">The demon's hall, switched to its burning look.</param>
        /// <param name="bannerY">Top of the banner line, in screen pixels from the top.</param>
        public static FinalStretchEffect Create(Transform screen, SalonView salon, int bannerX, int bannerY, int bannerWidth)
        {
            var effect = salon.gameObject.AddComponent<FinalStretchEffect>();
            effect._salon = salon;
            effect._flameFrames = UiArt.Strip(UiArt.Flames, UiArt.FlameFrameWidth);

            // Flames sit just above the backdrop, behind everything else.
            Image background = salon.GetComponent<Image>();
            effect._flamesBottom = CreateFlames(screen, "FlamesBottom", background, flipped: false);
            effect._flamesTop = CreateFlames(screen, "FlamesTop", background, flipped: true);

            effect._banner = UiFactory.CreateText("FinalStretchBanner", screen, "", 8, Palette.Hell, style: FontStyle.Bold).WithShadow();
            effect._banner.rectTransform.PlaceTL(bannerX, bannerY, bannerWidth, 8);
            effect._banner.horizontalOverflow = HorizontalWrapMode.Overflow;

            effect.Apply();
            return effect;
        }

        private static Image CreateFlames(Transform screen, string name, Image background, bool flipped)
        {
            Image flames = UiFactory.CreateImage(name, screen, Color.white);
            flames.raycastTarget = false;
            flames.type = Image.Type.Tiled;
            flames.pixelsPerUnitMultiplier = 1f;
            RectTransform rect = flames.rectTransform;
            rect.anchorMin = new Vector2(0f, flipped ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, flipped ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(0f, FlameHeight);
            rect.anchoredPosition = Vector2.zero;
            if (flipped)
                rect.localScale = new Vector3(1f, -1f, 1f);
            flames.transform.SetSiblingIndex(background.transform.GetSiblingIndex() + 1);
            return flames;
        }

        public bool IsActive => _active;

        public void SetActive(bool active, string banner)
        {
            _active = active;
            if (active) _banner.text = banner;
            Apply();
        }

        private void Apply()
        {
            if (_salon.Mode != SalonMode.Soul)
                _salon.SetMode(_active ? SalonMode.Hell : SalonMode.Normal);
            bool flames = _active && _flameFrames != null;
            _flamesBottom.enabled = flames;
            _flamesTop.enabled = flames;
            _banner.enabled = _active;
        }

        private void Update()
        {
            if (!_active) return;

            if (_flameFrames != null)
            {
                Sprite frame = _flameFrames[Mathf.FloorToInt(Time.unscaledTime * FlameFps) % _flameFrames.Length];
                _flamesBottom.sprite = frame;
                _flamesTop.sprite = frame;
            }

            // Hard blink between two palette colours.
            _banner.color = Mathf.Repeat(Time.unscaledTime, 0.6f) < 0.3f ? Palette.Hell : Palette.Amber;
        }
    }
}
