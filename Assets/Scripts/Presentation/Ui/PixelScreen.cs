using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Ui
{
    /// <summary>
    /// Keeps a canvas pixel-perfect: the game is laid out on a 480×270 screen that is scaled by a whole number
    /// (×3 at 1440×810, ×4 at 1920×1080...), centred, with black bars filling whatever is left.
    /// </summary>
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
    public sealed class PixelScreen : MonoBehaviour
    {
        public const int Width = 480;
        public const int Height = 270;

        private Canvas _canvas;
        private CanvasScaler _scaler;
        private Vector2Int _lastSize;

        public int Scale { get; private set; } = 1;

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
            if (size == _lastSize) return;

            _lastSize = size;
            Scale = ScaleFor(size.x, size.y);
            _scaler.scaleFactor = Scale;
        }

        /// <summary>The screen, or the target of the camera the canvas renders through (e.g. screenshots).</summary>
        private Vector2Int OutputSize()
        {
            Camera camera = _canvas.renderMode == RenderMode.ScreenSpaceCamera ? _canvas.worldCamera : null;
            return camera != null ? new Vector2Int(camera.pixelWidth, camera.pixelHeight) : new Vector2Int(Screen.width, Screen.height);
        }
    }
}
