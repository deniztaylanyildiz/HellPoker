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

    /// <summary>
    /// The player's preferences: animation speed, full screen, the hand guide (current hand name and draw hints), which
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

        /// <summary>The language of a first launch, before the player ever chose one (from the system, or English).</summary>
        private readonly Language _firstLanguage;

        private readonly ISettingsStore _store;
        private readonly HashSet<string> _tipsSeen = new HashSet<string>();

        public AnimationSpeed Speed { get; private set; } = AnimationSpeed.Normal;
        public bool Fullscreen { get; private set; } = true;
        public bool HandGuide { get; private set; } = true;
        public IReadOnlyCollection<string> TipsSeen => _tipsSeen;
        public Language Language { get; private set; } = Language.English;

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

        public void ToggleFullscreen() => Set(() => Fullscreen = !Fullscreen);

        public void SetFullscreen(bool fullscreen)
        {
            if (Fullscreen != fullscreen) Set(() => Fullscreen = fullscreen);
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

        private void Load()
        {
            int speed = _store.GetInt(SpeedKey, (int)AnimationSpeed.Normal);
            Speed = Enum.IsDefined(typeof(AnimationSpeed), speed) ? (AnimationSpeed)speed : AnimationSpeed.Normal;
            Fullscreen = _store.GetInt(FullscreenKey, 1) != 0;
            HandGuide = _store.GetInt(HandGuideKey, 1) != 0;
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
            _store.SetInt(FullscreenKey, Fullscreen ? 1 : 0);
            _store.SetInt(HandGuideKey, HandGuide ? 1 : 0);
            _store.SetString(LanguageKey, Language.ToString());
            _store.SetString(TipsKey, string.Join(",", _tipsSeen.OrderBy(t => t)));
            _store.Save();
        }
    }
}
