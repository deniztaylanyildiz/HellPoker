using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;

namespace HellPoker.Presentation
{
    /// <summary>
    /// Full screen covers the display (borderless); a window opens at the largest whole multiple of 480×270 that fits
    /// comfortably on the display. Either way <see cref="PixelScreen"/> keeps the pixels at a whole-number scale.
    /// </summary>
    public sealed class UnityDisplayMode : IDisplayMode
    {
        private const float WindowShare = 0.85f;

        public void SetFullscreen(bool fullscreen)
        {
            if (Application.isEditor) return;   // the editor's Game view decides its own size

            int displayWidth = Display.main.systemWidth;
            int displayHeight = Display.main.systemHeight;
            if (fullscreen)
            {
                Screen.SetResolution(displayWidth, displayHeight, FullScreenMode.FullScreenWindow);
                return;
            }

            int scale = Mathf.Max(1, Mathf.Min(
                Mathf.FloorToInt(displayWidth * WindowShare / PixelScreen.Width),
                Mathf.FloorToInt(displayHeight * WindowShare / PixelScreen.Height)));
            Screen.SetResolution(PixelScreen.Width * scale, PixelScreen.Height * scale, FullScreenMode.Windowed);
        }
    }
}
