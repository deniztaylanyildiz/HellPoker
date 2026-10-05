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
            Screen.SetResolution(PixelScreen.Width * scale, PixelScreen.Height * scale, FullScreenMode.Windowed);
        }
    }
}
