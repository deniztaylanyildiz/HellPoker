using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Ui
{
    /// <summary>
    /// Keeps a canvas pixel-perfect: the game is laid out on a 480×270 screen that is scaled by a whole number
    /// (×3 at 1440×810, ×4 at 1920×1080...), centred, with black bars filling whatever is left. With <see cref="FillScreen"/>
    /// (the settings' "fill screen", at full screen) the scale is the largest that fits, whole or not: the shape is kept, the
    /// bars shrink to the sliver the aspect ratio leaves, and the pixels may be slightly uneven. Clicks stay right either way:
    /// the UI's hit areas are its rectangles, scaled with it.
    /// </summary>
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
    public sealed class PixelScreen : MonoBehaviour
    {
        public const int Width = 480;
        public const int Height = 270;

        private Canvas _canvas;
        private CanvasScaler _scaler;
        private Vector2Int _lastSize;
        private bool _lastFill;

        /// <summary>Fill the screen (a fractional scale) instead of a whole-number one. Every pixel screen follows it.</summary>
        public static bool FillScreen { get; set; }

        /// <summary>The whole-number scale (the fractional one when filling, rounded down).</summary>
        public int Scale { get; private set; } = 1;

        /// <summary>The largest scale at which 480×270 fits, whole or not (filling the screen).</summary>
        public static float FillScaleFor(int screenWidth, int screenHeight)
        {
            if (screenWidth <= 0 || screenHeight <= 0) return 1f;
            return Mathf.Min(screenWidth / (float)Width, screenHeight / (float)Height);
        }

        /// <summary>The largest whole-number scale at which 480×270 fits the given screen (at least 1).</summary>
        public static int ScaleFor(int screenWidth, int screenHeight)
        {
            return Mathf.Max(1, Mathf.Min(screenWidth / Width, screenHeight / Height));
        }

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _scaler = GetComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            _scaler.referencePixelsPerUnit = 100f;
            _canvas.pixelPerfect = true;
            Apply();
        }

        private void LateUpdate()
        {
            Apply();
        }

        private void Apply()
        {
            Vector2Int size = OutputSize();
            if (size == _lastSize && FillScreen == _lastFill) return;

            _lastSize = size;
            _lastFill = FillScreen;
            Scale = ScaleFor(size.x, size.y);
            _scaler.scaleFactor = FillScreen ? FillScaleFor(size.x, size.y) : Scale;
        }

        /// <summary>The screen, or the target of the camera the canvas renders through (e.g. screenshots).</summary>
        private Vector2Int OutputSize()
        {
            Camera camera = _canvas.renderMode == RenderMode.ScreenSpaceCamera ? _canvas.worldCamera : null;
            return camera != null ? new Vector2Int(camera.pixelWidth, camera.pixelHeight) : new Vector2Int(Screen.width, Screen.height);
        }
    }
}
