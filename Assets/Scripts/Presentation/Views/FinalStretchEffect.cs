using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// The end-of-sentence look: the table heats up, a pulsing hellfire vignette closes in, and a warning banner throbs.
    /// Fades in and out smoothly when toggled.
    /// </summary>
    public sealed class FinalStretchEffect : MonoBehaviour
    {
        private const float FadeSeconds = 1.5f;

        private Image _background;
        private Image _felt;
        private Image _vignette;
        private Text _banner;
        private bool _active;
        private float _blend;

        public static FinalStretchEffect Create(Transform parent, Image background, Image felt, Vector2 bannerPosition)
        {
            // Sits right above the background, behind the table, so the fire frames the game without covering it.
            Image vignette = UiFactory.CreateImage("HellfireVignette", parent, Color.clear);
            vignette.rectTransform.Stretch();
            vignette.sprite = UiFactory.CreateVignetteSprite();
            vignette.raycastTarget = false;
            vignette.transform.SetSiblingIndex(background.transform.GetSiblingIndex() + 1);

            Text banner = UiFactory.CreateText("FinalStretchBanner", parent, "", 24, Palette.Ember, style: FontStyle.Bold).WithShadow(2f);
            banner.rectTransform.Place(new Vector2(0.5f, 1f), bannerPosition, new Vector2(1000f, 36f));
            banner.horizontalOverflow = HorizontalWrapMode.Overflow;

            var effect = vignette.gameObject.AddComponent<FinalStretchEffect>();
            effect._background = background;
            effect._felt = felt;
            effect._vignette = vignette;
            effect._banner = banner;
            effect.Apply(0f);
            return effect;
        }

        public void SetActive(bool active, string banner)
        {
            _active = active;
            if (active) _banner.text = banner;
        }

        private void Update()
        {
            _blend = Mathf.MoveTowards(_blend, _active ? 1f : 0f, Time.deltaTime / FadeSeconds);
            Apply(Mathf.SmoothStep(0f, 1f, _blend));
        }

        private void Apply(float blend)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 2.6f);

            // The backdrop and table are art, so the heat is a tint over them rather than a new colour.
            _background.color = Color.Lerp(Color.white, Palette.HellTint, blend * (0.8f + 0.2f * pulse));
            _felt.color = Color.Lerp(Color.white, Palette.HellFeltTint, blend);
            _vignette.color = new Color(1f, 0.22f + 0.14f * pulse, 0.03f, blend * (0.25f + 0.25f * pulse));

            _banner.gameObject.SetActive(blend > 0.01f);
            Color bannerColor = Color.Lerp(Palette.Ember, Palette.Gold, pulse);
            bannerColor.a = blend;
            _banner.color = bannerColor;
        }
    }
}
