namespace HellPoker.Presentation.Abstractions
{
    /// <summary>Menu intents an input source can trigger.</summary>
    public interface IMenuCommands
    {
        /// <summary>True on any screen but the table (the table is not taking input).</summary>
        bool IsMenuOpen { get; }

        /// <summary>True on the title menu itself, with nothing open over it (no warning, no rules): the arrows and the selection work
        /// only here.</summary>
        bool IsAtMenuRoot { get; }

        /// <summary>True while How to Play is open over the menu (the arrows and Q / E turn its pages).</summary>
        bool IsShowingRules { get; }

        /// <summary>The arrows / Q-E on How to Play: a page on (1) or back (-1).</summary>
        void TurnRulesPage(int step);

        /// <summary>True while a screen change plays; all input waits.</summary>
        bool IsTransitioning { get; }

        /// <summary>
        /// Esc: one screen up. A warning closes, a sub-screen (rules, settings, records, the class choice) returns to the menu, the
        /// chapter's screens to the menu, and the menu back into a run in progress.
        /// </summary>
        void GoBack();

        /// <summary>Demo 3.3: the arrows on the title menu: the cursor to the next button that way (nothing off the menu's root).</summary>
        void MoveMenuSelection(int dx, int dy);

        /// <summary>Demo 3.3: Enter / Space on the title menu: the button under the cursor (nothing off the menu's root).</summary>
        void PressMenuSelection();
    }

    /// <summary>Setting shortcuts an input source can trigger.</summary>
    public interface ISettingsCommands
    {
        /// <summary>Alt+Enter.</summary>
        void ToggleFullscreen();

        /// <summary>True while the settings screen is up (Q / E and the arrows switch its tabs).</summary>
        bool IsSettingsOpen { get; }

        /// <summary>Demo 3.3: the LANGUAGE tab is on show (up / down move through the list, Enter / Space choose).</summary>
        bool IsLanguageTab { get; }

        /// <summary>Up / down in the LANGUAGE tab: the next language in the list (round and round).</summary>
        void StepLanguage(int step);

        /// <summary>Enter / Space in the LANGUAGE tab: the language the keys point at, saved — the screens rewrite at once.</summary>
        void ConfirmLanguage();

        /// <summary>E or the right arrow: the next tab (round and round).</summary>
        void NextTab();

        /// <summary>Q or the left arrow: the previous tab.</summary>
        void PreviousTab();
    }
}
