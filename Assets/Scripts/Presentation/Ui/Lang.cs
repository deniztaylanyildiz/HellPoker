using System;

namespace HellPoker.Presentation.Ui
{
    /// <summary>The languages the game speaks. Saved by name ("settings.language"); English is the default.</summary>
    public enum Language
    {
        English,
        Turkish
    }

    /// <summary>
    /// The language the game speaks right now. Every player-facing string in <see cref="UiText"/> picks its words through
    /// <see cref="Pick{T}"/>; views and presenters listen to <see cref="Changed"/> to rewrite what is on screen.
    /// Never touches <c>CultureInfo.CurrentCulture</c> (Turkish casing would turn "i" into "İ" and break number formats):
    /// the Turkish strings are written in their final case.
    /// </summary>
    public static class Lang
    {
        public static Language Current { get; private set; } = Language.English;

        /// <summary>Raised after the language changed (not when it is set to the one already in use).</summary>
        public static event Action Changed;

        public static void Set(Language language)
        {
            if (!Enum.IsDefined(typeof(Language), language)) language = Language.English;
            if (language == Current) return;
            Current = language;
            Changed?.Invoke();
        }

        public static bool IsTurkish => Current == Language.Turkish;

        /// <summary>The English or the Turkish one, by the current language.</summary>
        public static T Pick<T>(T english, T turkish) => Current == Language.Turkish ? turkish : english;

        /// <summary>The language after <paramref name="language"/>, round and round.</summary>
        public static Language Next(Language language)
        {
            int at = Array.IndexOf((Language[])All, language);
            return All[(at + 1) % All.Count];
        }

        /// <summary>Demo 3.3: every language the game speaks, in order (the settings' LANGUAGE tab lists them; a new one is a new enum value
        /// and its name below).</summary>
        public static System.Collections.Generic.IReadOnlyList<Language> All { get; } = (Language[])Enum.GetValues(typeof(Language));

        /// <summary>A language's own name, in its own words ("English", "Türkçe") — the same whatever language is on.</summary>
        public static string NativeName(Language language)
        {
            switch (language)
            {
                case Language.Turkish: return "Türkçe";
                default: return "English";
            }
        }
    }
}
