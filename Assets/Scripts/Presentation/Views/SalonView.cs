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
    /// What is asked for (<see cref="DealerId"/>, <see cref="Mode"/>) and what is on screen (<see cref="ShownDealerId"/>,
    /// <see cref="ShownMode"/>) are kept apart; whenever no wipe is running they are brought back in line, so an interrupted
    /// wipe can never leave an old hall behind.
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

        /// <summary>The hall asked for.</summary>
        public string DealerId => _dealerId;

        /// <summary>The mood asked for.</summary>
        public SalonMode Mode => _mode;

        /// <summary>The hall and mood actually on screen (lag the request only while a wipe runs).</summary>
        public string ShownDealerId { get; private set; }
        public SalonMode ShownMode { get; private set; }

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

        /// <summary>Moves to another demon's hall in its current mood; <paramref name="wipe"/> plays the transition.</summary>
        public void SetSalon(string dealerId, bool wipe = false) => SetSalon(dealerId, _mode, wipe);

        /// <summary>Moves to a hall and a mood together, so the wipe reveals the right picture.</summary>
        public void SetSalon(string dealerId, SalonMode mode, bool wipe = false)
        {
            bool changed = dealerId != _dealerId || mode != _mode;
            _dealerId = dealerId;
            _mode = mode;
            if (!changed && IsShowingRequest) return;

            if (wipe && dealerId != ShownDealerId && isActiveAndEnabled)
            {
                if (_wipe != null) StopCoroutine(_wipe);
                _wipe = StartCoroutine(Wipe());
            }
            else
            {
                StopWipe();
                Show();
            }
        }

        public void SetMode(SalonMode mode)
        {
            if (mode == _mode && IsShowingRequest) return;
            _mode = mode;
            if (_wipe == null)
                Show();   // a running wipe reveals the new mood itself
        }

        private bool IsShowingRequest => ShownDealerId == _dealerId && ShownMode == _mode;

        private void Show()
        {
            ShownDealerId = _dealerId;
            ShownMode = _mode;
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

        private void StopWipe()
        {
            if (_wipe != null)
            {
                StopCoroutine(_wipe);
                _wipe = null;
            }
            SetBlinds(0f);
        }

        private void Update()
        {
            // Safety net: nothing should ever leave the screen behind the request for longer than a frame.
            if (_wipe == null && !IsShowingRequest)
                Show();
        }

        private void OnDisable()
        {
            // A wipe killed with the object would otherwise never reveal the new hall.
            _wipe = null;
            if (_blinds == null || _animator == null) return;   // the scene is closing
            SetBlinds(0f);
            if (!IsShowingRequest)
                Show();
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
