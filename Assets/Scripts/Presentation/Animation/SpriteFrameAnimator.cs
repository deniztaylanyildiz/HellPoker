using System;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Animation
{
    /// <summary>
    /// Plays a <see cref="SpriteClip"/> on a UI Image: swaps whole frames at the clip's rate, loops or plays once and reports
    /// the end. With no clip the Image shows its fallback colour instead of a sprite.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class SpriteFrameAnimator : MonoBehaviour
    {
        private Image _image;
        private float _time;
        private Action _onComplete;
        private bool _completed;
        private int _shown = -1;

        public SpriteClip Clip { get; private set; }

        /// <summary>Colour shown when there is no clip at all (missing art).</summary>
        public Color FallbackColor { get; set; } = Color.black;

        public bool IsPlayingOnce => Clip != null && !Clip.Loop && !_completed;

        public void Play(SpriteClip clip, Action onComplete = null)
        {
            EnsureImage();
            Clip = clip;
            _time = 0f;
            _onComplete = onComplete;
            _completed = false;
            _shown = -1;

            if (clip == null)
            {
                _image.sprite = null;
                _image.color = FallbackColor;
                _onComplete = null;
                onComplete?.Invoke();
                return;
            }

            _image.color = Color.white;
            Show(0);
        }

        private void Update()
        {
            if (Clip == null) return;

            _time += Time.unscaledDeltaTime;
            int frame = Clip.FrameAt(_time, out bool finished);
            Show(frame);

            if (finished && !_completed)
            {
                _completed = true;
                Action done = _onComplete;
                _onComplete = null;
                done?.Invoke();
            }
        }

        private void Show(int frame)
        {
            if (frame == _shown) return;
            _shown = frame;
            _image.sprite = Clip.Frames[frame];
        }

        private void EnsureImage()
        {
            if (_image == null)
                _image = GetComponent<Image>();
        }
    }
}
