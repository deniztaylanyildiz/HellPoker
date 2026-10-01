using System;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>The settings screen: one button per setting, each showing its current value.</summary>
    public interface ISettingsView
    {
        event Action SpeedPressed;
        event Action FullscreenPressed;
        event Action HandGuidePressed;
        event Action ResetTipsPressed;
        event Action BackPressed;

        bool IsVisible { get; }

        /// <param name="speed">The speed's name ("NORMAL").</param>
        /// <param name="tipsLeft">True when there are seen tips that a reset would bring back.</param>
        void Render(string speed, bool fullscreen, bool handGuide, bool tipsLeft);

        void Show();
        void Hide();
    }

    /// <summary>The window: full screen or a window, always at a whole-number pixel scale.</summary>
    public interface IDisplayMode
    {
        void SetFullscreen(bool fullscreen);
    }

    /// <summary>A short curtain over a screen change; input waits while it plays.</summary>
    public interface IScreenTransition
    {
        bool IsPlaying { get; }

        /// <summary>Covers the screen and opens on whatever is showing now.</summary>
        void Play();
    }
}
