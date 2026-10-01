using System.Collections;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// The full-screen backdrop: the current demon's hall in its current mood, gently animated. Changing hall can play a
    /// pixel "blinds" wipe (dark bars close in 8 px steps, the hall changes, the bars open again).
    /// </summary>
    public sealed class SalonView : MonoBehaviour
    {
        private const float WipeSeconds = 0.14f;
        private const int WipeStep = 8;

        private SalonLibrary _library;
        private SpriteFrameAnimator _animator;
        private Image _blinds;
        private string _dealerId;
        private SalonMode _mode;
        private Coroutine _wipe;

        public string DealerId => _dealerId;
        public SalonMode Mode => _mode;

        public static SalonView Create(Transform screen, SalonLibrary library)
        {
            Image image = UiFactory.CreateImage("Salon", screen, Palette.Night);
            image.rectTransform.Stretch();
            image.raycastTarget = true;

            var view = image.gameObject.AddComponent<SalonView>();
            view._library = library;
            view._animator = image.gameObject.AddComponent<SpriteFrameAnimator>();
            view._animator.FallbackColor = Palette.Night;

            // A dark curtain dropping from the top edge, sized in whole 8 px rows.
            view._blinds = UiFactory.CreateImage("Blinds", image.transform, Palette.Black);
            view._blinds.raycastTarget = false;
            RectTransform blinds = view._blinds.rectTransform;
            blinds.anchorMin = new Vector2(0f, 1f);
            blinds.anchorMax = new Vector2(1f, 1f);
            blinds.pivot = new Vector2(0.5f, 1f);
            blinds.anchoredPosition = Vector2.zero;
            view.SetBlinds(0f);

            view.Show();
            return view;
        }

        /// <summary>Moves to another demon's hall; <paramref name="wipe"/> plays the transition.</summary>
        public void SetSalon(string dealerId, bool wipe = false)
        {
            if (dealerId == _dealerId) return;
            _dealerId = dealerId;
            if (wipe && isActiveAndEnabled)
            {
                if (_wipe != null) StopCoroutine(_wipe);
                _wipe = StartCoroutine(Wipe());
            }
            else
            {
                Show();
            }
        }

        public void SetMode(SalonMode mode)
        {
            if (mode == _mode) return;
            _mode = mode;
            Show();
        }

        private void Show()
        {
            _animator.Play(_library.Get(_dealerId, _mode));
        }

        private IEnumerator Wipe()
        {
            yield return Tween.Run(WipeSeconds, SetBlinds);
            Show();
            yield return Tween.Run(WipeSeconds, t => SetBlinds(1f - t));
            SetBlinds(0f);
            _wipe = null;
        }

        /// <summary>Lowers the curtain to a share of the screen, in whole 8 px rows.</summary>
        private void SetBlinds(float share)
        {
            float rows = Mathf.Round(Mathf.Clamp01(share) * PixelScreen.Height / WipeStep) * WipeStep;
            _blinds.rectTransform.sizeDelta = new Vector2(0f, Mathf.Min(rows, PixelScreen.Height));
            _blinds.enabled = rows > 0f;
        }
    }
}
