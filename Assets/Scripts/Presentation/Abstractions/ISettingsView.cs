using System;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>The settings screen's tabs, in order (Q / E and the arrow keys walk them round).</summary>
    public enum SettingsTab
    {
        Game,
        Display,
        Sound
    }

    /// <summary>Everything the settings screen shows, as words (the presenter picks them, the view lays them out).</summary>
    public sealed class SettingsScreen
    {
        public SettingsTab Tab { get; set; }

        // The game tab.
        public string Speed { get; set; }
        public bool HandGuide { get; set; }

        /// <summary>True when there are seen tips that a reset would bring back.</summary>
        public bool TipsLeft { get; set; }

        /// <summary>The language's own name ("TÜRKÇE").</summary>
        public string Language { get; set; }

        // The display tab.
        public bool Fullscreen { get; set; }
        public string WindowMode { get; set; }

        /// <summary>"AUTO", "×3  1440×810"...</summary>
        public string WindowScale { get; set; }

        /// <summary>The window size means nothing at full screen: the row looks dull (a press does nothing).</summary>
        public bool WindowScaleLocked { get; set; }

        public string PixelScale { get; set; }

        /// <summary>The pixel scale means nothing in a window (it is a whole multiple already): the row looks dull.</summary>
        public bool PixelScaleLocked { get; set; }

        public string VSync { get; set; }

        // The sound tab: the volumes as shown ("7 / 10", "OFF").
        public string Master { get; set; }
        public string Music { get; set; }
        public string Sfx { get; set; }
    }

    /// <summary>The settings screen: three tabs, one button per setting, each showing its current value.</summary>
    public interface ISettingsView : ILanguageButton
    {
        event Action<SettingsTab> TabPressed;

        event Action SpeedPressed;
        event Action HandGuidePressed;
        event Action ResetTipsPressed;

        /// <summary>The display mode row (full screen / window).</summary>
        event Action FullscreenPressed;
        event Action WindowScalePressed;
        event Action PixelScalePressed;
        event Action VSyncPressed;

        event Action MasterPressed;
        event Action MusicPressed;
        event Action SfxPressed;
        event Action BackPressed;

        bool IsVisible { get; }

        void Render(SettingsScreen screen);

        void Show();
        void Hide();
    }

    /// <summary>A screen with a language button (the table, the title menu, the settings).</summary>
    public interface ILanguageButton
    {
        /// <summary>The language button was pressed: the next language, please.</summary>
        event Action LanguagePressed;
    }

    /// <summary>The window and the display it sits on.</summary>
    public interface IDisplayMode
    {
        /// <summary>The display's size in pixels (what a window has to fit in).</summary>
        int DisplayWidth { get; }
        int DisplayHeight { get; }

        /// <param name="fullscreen">Full screen (borderless) or a window.</param>
        /// <param name="windowScale">The window's whole multiple of 480×270 (already resolved: never automatic).</param>
        /// <param name="fill">At full screen: fill the display keeping the shape instead of a whole-number scale with bars.</param>
        /// <param name="vSync">Vertical sync; off holds the frame rate at 60.</param>
        void Apply(bool fullscreen, int windowScale, bool fill, bool vSync);
    }

    /// <summary>A short curtain over a screen change; input waits while it plays.</summary>
    public interface IScreenTransition
    {
        bool IsPlaying { get; }

        /// <summary>Covers the screen and opens on whatever is showing now.</summary>
        void Play();
    }
}
