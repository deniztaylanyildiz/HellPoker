using HellPoker.Presentation.Ui;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// A small frames-per-second readout in the top left corner, for development builds (and the editor): F3 shows and hides
    /// it. Shows the average over the last half second and the slowest frame in it. Also measures on request
    /// (<see cref="Measure"/>) for the FPS tour.
    /// </summary>
    public sealed class FpsCounter : MonoBehaviour
    {
        private const int SortingOrder = 500;
        private const float Window = 0.5f;

        private Canvas _canvas;
        private Text _text;
        private int _frames;
        private float _elapsed;
        private float _slowest;

        /// <summary>The last window's average and minimum (frames per second).</summary>
        public float Average { get; private set; }
        public float Minimum { get; private set; }

        public bool IsShown => _canvas.enabled;

        public static FpsCounter Create(Transform parent)
        {
            Canvas canvas = UiFactory.CreateScreen("FpsCanvas", parent, SortingOrder, out RectTransform screen, letterbox: false);
            var counter = canvas.gameObject.AddComponent<FpsCounter>();
            counter._canvas = canvas;
            Image strip = UiFactory.CreateImage("FpsStrip", screen, Palette.Black);
            strip.raycastTarget = false;
            strip.rectTransform.PlaceTL(2, 2, 76, 11);
            counter._text = UiFactory.CreateText("Fps", strip.transform, "", 8, Palette.GreenLight, TextAnchor.MiddleLeft);
            counter._text.rectTransform.PlaceTL(3, 1, 72, 9);
            canvas.enabled = false;
            return counter;
        }

        public void Toggle() => _canvas.enabled = !_canvas.enabled;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f3Key.wasPressedThisFrame)
                Toggle();

            float delta = Time.unscaledDeltaTime;
            _frames++;
            _elapsed += delta;
            _slowest = Mathf.Max(_slowest, delta);
            if (_elapsed < Window) return;

            Average = _frames / _elapsed;
            Minimum = _slowest > 0f ? 1f / _slowest : 0f;
            _frames = 0;
            _elapsed = 0f;
            _slowest = 0f;
            if (_canvas.enabled)
                _text.text = string.Format(UiText.FpsFormat, Mathf.RoundToInt(Average), Mathf.RoundToInt(Minimum));
        }

        /// <summary>The frame rate over a stretch of real time: average, slowest frame, frame count.</summary>
        public sealed class Measurement
        {
            /// <summary>A frame slower than this (25 ms, under 40 FPS) is a hitch the eye can catch.</summary>
            public const float HitchSeconds = 0.025f;

            public float Seconds;
            public int Frames;
            public float Slowest;
            public int Hitches;

            public void Add(float delta)
            {
                Frames++;
                Seconds += delta;
                Slowest = Mathf.Max(Slowest, delta);
                if (delta > HitchSeconds) Hitches++;
            }

            public float Average => Seconds > 0f ? Frames / Seconds : 0f;
            public float Minimum => Slowest > 0f ? 1f / Slowest : 0f;
        }

        /// <summary>Starts a measurement fed by every frame until read.</summary>
        public static Measurement Measure() => new Measurement();
    }
}
