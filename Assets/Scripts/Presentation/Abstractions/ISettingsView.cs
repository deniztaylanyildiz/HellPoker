using System;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>The settings screen: one button per setting, each showing its current value.</summary>
    public interface ISettingsView : ILanguageButton
    {
        event Action SpeedPressed;
        event Action FullscreenPressed;
        event Action HandGuidePressed;
        event Action ResetTipsPressed;
        event Action BackPressed;

        bool IsVisible { get; }

        /// <param name="speed">The speed's name ("NORMAL").</param>
        /// <param name="tipsLeft">True when there are seen tips that a reset would bring back.</param>
        /// <param name="language">The language's own name ("TÜRKÇE").</param>
        void Render(string speed, bool fullscreen, bool handGuide, bool tipsLeft, string language);

        void Show();
        void Hide();
    }

    /// <summary>A screen with a language button (the table, the title menu, the settings).</summary>
    public interface ILanguageButton
    {
        /// <summary>The language button was pressed: the next language, please.</summary>
        event Action LanguagePressed;
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
