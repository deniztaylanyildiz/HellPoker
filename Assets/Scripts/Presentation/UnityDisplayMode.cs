using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Settings;
using HellPoker.Presentation.Ui;
using UnityEngine;

namespace HellPoker.Presentation
{
    /// <summary>
    /// Full screen covers the display (borderless); a window opens at a whole multiple of 480×270 (<see cref="WindowScales"/>).
    /// At full screen <see cref="PixelScreen"/> keeps the pixels at a whole-number scale, or fills the display keeping the shape.
    /// Vertical sync on, or off with the frame rate held at 60.
    /// </summary>
    public sealed class UnityDisplayMode : IDisplayMode
    {
        /// <summary>The editor's Game view decides its own size: the window list is offered as for a 1080p display.</summary>
        public int DisplayWidth => Application.isEditor ? 1920 : Display.main.systemWidth;
        public int DisplayHeight => Application.isEditor ? 1080 : Display.main.systemHeight;

        public void Apply(bool fullscreen, int windowScale, bool fill, bool vSync)
        {
            QualitySettings.vSyncCount = vSync ? 1 : 0;
            Application.targetFrameRate = vSync ? -1 : 60;
            PixelScreen.FillScreen = fullscreen && fill;

            if (Application.isEditor) return;   // the editor's Game view decides its own size
            if (fullscreen)
            {
                Screen.SetResolution(DisplayWidth, DisplayHeight, FullScreenMode.FullScreenWindow);
                return;
            }
            int scale = Mathf.Max(1, windowScale);
            int width = PixelScreen.Width * scale, height = PixelScreen.Height * scale;
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
            Center(width, height);
        }

        /// <summary>The window goes to the middle of its display's work area, never outside it (where the platform allows moving it).</summary>
        private static void Center(int width, int height)
        {
            try
            {
                DisplayInfo display = Screen.mainWindowDisplayInfo;
                RectInt area = display.workArea.width > 0 ? display.workArea : new RectInt(0, 0, display.width, display.height);
                (int x, int y) = WindowScales.Centered(area.x, area.y, area.width, area.height, width, height);
                Screen.MoveMainWindowTo(display, new Vector2Int(x, y));
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"Hell Poker: the window could not be centred ({exception.Message}).");
            }
        }
    }
}
