namespace HellPoker.Presentation.Abstractions
{
    /// <summary>Menu intents an input source can trigger.</summary>
    public interface IMenuCommands
    {
        /// <summary>True on any screen but the table (the table is not taking input).</summary>
        bool IsMenuOpen { get; }

        /// <summary>True on the title menu itself, with nothing open over it (no warning, no rules): the only place the
        /// language changes (L).</summary>
        bool IsAtMenuRoot { get; }

        /// <summary>True while a screen change plays; all input waits.</summary>
        bool IsTransitioning { get; }

        /// <summary>
        /// Esc: one screen up. A warning closes, a sub-screen (rules, settings) returns to the menu, the dealer choice to where it
        /// came from, the table to the menu, and the menu back into a run in progress.
        /// </summary>
        void GoBack();
    }

    /// <summary>Setting shortcuts an input source can trigger.</summary>
    public interface ISettingsCommands
    {
        /// <summary>Alt+Enter.</summary>
        void ToggleFullscreen();

        /// <summary>L on the title menu, or a language button (the menu's corner, the settings row): the next language, saved.</summary>
        void CycleLanguage();

        /// <summary>True while the settings screen is up (Q / E and the arrows switch its tabs).</summary>
        bool IsSettingsOpen { get; }

        /// <summary>E or the right arrow: the next tab (round and round).</summary>
        void NextTab();

        /// <summary>Q or the left arrow: the previous tab.</summary>
        void PreviousTab();
    }
}
