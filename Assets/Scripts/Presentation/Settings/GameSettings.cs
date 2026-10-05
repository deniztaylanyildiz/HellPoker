using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;

namespace HellPoker.Presentation.Settings
{
    public enum AnimationSpeed
    {
        Normal,
        Fast,
        VeryFast
    }

    /// <summary>How the game sits on the display (saved as its number: "settings.display.mode").</summary>
    public enum WindowMode
    {
        /// <summary>Covering the display, borderless.</summary>
        Fullscreen = 0,

        /// <summary>A window at a whole multiple of 480×270.</summary>
        Windowed = 1
    }

    /// <summary>
    /// The player's preferences: animation speed, the display (mode, window scale, fill, vertical sync), the volumes, the hand guide (current hand name and draw hints), which
    /// first-game tips were already shown, and the language (applied to <see cref="Lang"/> on load and on every change). Loads from and saves to an <see cref="ISettingsStore"/>; a missing or garbled
    /// value falls back to its default.
    /// </summary>
    public sealed class GameSettings : IGuideSettings
    {
        private const string SpeedKey = "settings.speed";
        private const string FullscreenKey = "settings.fullscreen";
        private const string HandGuideKey = "settings.handGuide";
        private const string TipsKey = "settings.tipsSeen";
        private const string LanguageKey = "settings.language";
        private const string MusicKey = "settings.music";
        private const string SfxKey = "settings.sfx";
        private const string MasterKey = "settings.master";
        private const string WindowModeKey = "settings.display.mode";
        private const string WindowScaleKey = "settings.display.scale";
        private const string FillKey = "settings.display.fill";
        private const string VSyncKey = "settings.vsync";

        /// <summary>A window's scale: 0 is automatic (the largest multiple that fits comfortably), otherwise ×this.</summary>
        public const int AutoWindowScale = 0;

        /// <summary>No display is this big; a larger saved scale is garbage.</summary>
        private const int MaxSavedWindowScale = 32;

        /// <summary>Volumes go from 0 (silent) to this.</summary>
        public const int MaxVolume = 10;
        public const int DefaultVolume = 7;

        /// <summary>The language of a first launch, before the player ever chose one (from the system, or English).</summary>
        private readonly Language _firstLanguage;

        private readonly ISettingsStore _store;
        private readonly HashSet<string> _tipsSeen = new HashSet<string>();

        public AnimationSpeed Speed { get; private set; } = AnimationSpeed.Normal;
        public WindowMode WindowMode { get; private set; } = WindowMode.Fullscreen;
        public bool Fullscreen => WindowMode == WindowMode.Fullscreen;

        /// <summary>The window's scale (×2, ×3...), or <see cref="AutoWindowScale"/>. Kept while full screen, used in a window.</summary>
        public int WindowScale { get; private set; } = AutoWindowScale;

        /// <summary>Full screen fills the display keeping the shape (pixels may be slightly uneven) instead of a whole-number
        /// scale with black bars.</summary>
        public bool FillScreen { get; private set; }

        /// <summary>Vertical sync; without it the frame rate is held at 60.</summary>
        public bool VSync { get; private set; } = true;

        /// <summary>The master volume: music and effects are each a share of it.</summary>
        public int MasterVolume { get; private set; } = MaxVolume;
        public bool HandGuide { get; private set; } = true;
        public IReadOnlyCollection<string> TipsSeen => _tipsSeen;
        public Language Language { get; private set; } = Language.English;
        public int MusicVolume { get; private set; } = DefaultVolume;
        public int SfxVolume { get; private set; } = DefaultVolume;

        /// <summary>The next music volume, round and round (10 → 0).</summary>
        public void CycleMusic() => Set(() => MusicVolume = (MusicVolume + 1) % (MaxVolume + 1));

        public void CycleSfx() => Set(() => SfxVolume = (SfxVolume + 1) % (MaxVolume + 1));

        public void CycleMaster() => Set(() => MasterVolume = (MasterVolume + 1) % (MaxVolume + 1));

        /// <summary>
        /// The next window scale among <paramref name="choices"/> (what the display fits: automatic first, then ×2, ×3...). A saved
        /// scale the display no longer fits counts as automatic.
        /// </summary>
        public void CycleWindowScale(IReadOnlyList<int> choices)
        {
            if (choices == null || choices.Count == 0) return;
            int at = -1;
            for (int i = 0; i < choices.Count; i++)
                if (choices[i] == WindowScale) at = i;
            int next = choices[(at + 1) % choices.Count];
            if (next != WindowScale) Set(() => WindowScale = next);
        }

        public void ToggleFill() => Set(() => FillScreen = !FillScreen);

        public void ToggleVSync() => Set(() => VSync = !VSync);

        /// <summary>Raised after any setting changed (and was saved).</summary>
        public event Action Changed;

        /// <param name="firstLanguage">The language when none was saved yet (the system's, on a first launch); English if omitted.</param>
        public GameSettings(ISettingsStore store, Language firstLanguage = Language.English)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _firstLanguage = Enum.IsDefined(typeof(Language), firstLanguage) ? firstLanguage : Language.English;
            Load();
            Lang.Set(Language);
        }

        /// <summary>The first launch's language from the system's: Turkish for a Turkish system, English for every other.</summary>
        public static Language LanguageForSystem(bool systemIsTurkish) => systemIsTurkish ? Language.Turkish : Language.English;

        public void CycleLanguage() => SetLanguage(Lang.Next(Language));

        public void SetLanguage(Language language)
        {
            if (!Enum.IsDefined(typeof(Language), language)) language = Language.English;
            if (language == Language) return;
            Set(() => Language = language);
            Lang.Set(Language);
        }

        /// <summary>How much faster than normal the animations run.</summary>
        public float SpeedMultiplier => MultiplierOf(Speed);

        public static float MultiplierOf(AnimationSpeed speed)
        {
            switch (speed)
            {
                case AnimationSpeed.Fast: return 2f;
                case AnimationSpeed.VeryFast: return 4f;
                default: return 1f;
            }
        }

        public void CycleSpeed() => Set(() => Speed = (AnimationSpeed)(((int)Speed + 1) % 3));

        public void ToggleFullscreen() => SetFullscreen(!Fullscreen);

        public void SetFullscreen(bool fullscreen)
        {
            if (Fullscreen != fullscreen) Set(() => WindowMode = fullscreen ? WindowMode.Fullscreen : WindowMode.Windowed);
        }

        public void ToggleHandGuide() => Set(() => HandGuide = !HandGuide);

        public bool HasSeenTip(string tip) => _tipsSeen.Contains(tip);

        public void MarkTipSeen(string tip)
        {
            if (string.IsNullOrEmpty(tip) || _tipsSeen.Contains(tip)) return;
            Set(() => _tipsSeen.Add(tip));
        }

        public void ResetTips()
        {
            if (_tipsSeen.Count > 0) Set(_tipsSeen.Clear);
        }

        private void Set(Action change)
        {
            change();
            Save();
            Changed?.Invoke();
        }

        /// <summary>A saved volume, or the default when it is out of range.</summary>
        private static int Volume(int saved, int fallback = DefaultVolume) => saved >= 0 && saved <= MaxVolume ? saved : fallback;

        private void Load()
        {
            int speed = _store.GetInt(SpeedKey, (int)AnimationSpeed.Normal);
            Speed = Enum.IsDefined(typeof(AnimationSpeed), speed) ? (AnimationSpeed)speed : AnimationSpeed.Normal;
            // The display mode; an older save only knew "settings.fullscreen" (1 / 0), and that carries over.
            int mode = _store.GetInt(WindowModeKey, -1);
            WindowMode = mode == -1 ? (_store.GetInt(FullscreenKey, 1) != 0 ? WindowMode.Fullscreen : WindowMode.Windowed)
                : Enum.IsDefined(typeof(WindowMode), mode) ? (WindowMode)mode : WindowMode.Fullscreen;
            int scale = _store.GetInt(WindowScaleKey, AutoWindowScale);
            WindowScale = scale >= 2 && scale <= MaxSavedWindowScale ? scale : AutoWindowScale;
            FillScreen = _store.GetInt(FillKey, 0) == 1;
            VSync = _store.GetInt(VSyncKey, 1) != 0;
            MasterVolume = Volume(_store.GetInt(MasterKey, MaxVolume), MaxVolume);
            HandGuide = _store.GetInt(HandGuideKey, 1) != 0;
            MusicVolume = Volume(_store.GetInt(MusicKey, DefaultVolume));
            SfxVolume = Volume(_store.GetInt(SfxKey, DefaultVolume));
            // Saved by name; never saved yet: the first launch's language; anything unreadable: English.
            string language = _store.GetString(LanguageKey, null);
            Language = language == null ? _firstLanguage
                : Enum.TryParse(language, out Language parsed) && Enum.IsDefined(typeof(Language), parsed) && !int.TryParse(language, out _)
                    ? parsed : Language.English;

            _tipsSeen.Clear();
            foreach (string tip in (_store.GetString(TipsKey, "") ?? "").Split(','))
            {
                string trimmed = tip.Trim();
                if (trimmed.Length > 0) _tipsSeen.Add(trimmed);
            }
        }

        private void Save()
        {
            _store.SetInt(SpeedKey, (int)Speed);
            _store.SetInt(FullscreenKey, Fullscreen ? 1 : 0);   // still written: an older build reads only this
            _store.SetInt(WindowModeKey, (int)WindowMode);
            _store.SetInt(WindowScaleKey, WindowScale);
            _store.SetInt(FillKey, FillScreen ? 1 : 0);
            _store.SetInt(VSyncKey, VSync ? 1 : 0);
            _store.SetInt(MasterKey, MasterVolume);
            _store.SetInt(HandGuideKey, HandGuide ? 1 : 0);
            _store.SetString(LanguageKey, Language.ToString());
            _store.SetInt(MusicKey, MusicVolume);
            _store.SetInt(SfxKey, SfxVolume);
            _store.SetString(TipsKey, string.Join(",", _tipsSeen.OrderBy(t => t)));
            _store.Save();
        }
    }
}
