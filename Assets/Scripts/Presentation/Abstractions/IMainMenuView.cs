using System;

namespace HellPoker.Presentation.Abstractions
{
    public interface IMainMenuView : ILanguageButton
    {
        event Action NewGamePressed;
        event Action ContinuePressed;
        event Action ChangeTablePressed;
        event Action QuitPressed;
        event Action SettingsPressed;
        event Action RecordsPressed;

        /// <summary>The Phase 2 button: a new chapter run.</summary>
        event Action ChaptersPressed;

        /// <summary>The Phase 2 CONTINUE button: back to the chapter run that waits.</summary>
        event Action ChaptersContinuePressed;

        /// <summary>The Phase 2 buttons: shown when <paramref name="available"/>; CONTINUE only while a chapter run waits.</summary>
        void SetChapters(bool available, bool inProgress);


        /// <summary>The player accepted the warning open over the menu (New Game or Quit over a run in progress).</summary>
        event Action Confirmed;

        /// <summary>True while the rules (How to Play) are open over the menu.</summary>
        bool IsShowingRules { get; }

        /// <summary>True while a warning (New Game, Quit) is open.</summary>
        bool IsConfirming { get; }

        /// <summary>Opens a warning: the demon's word on it, what it costs, and the button that goes ahead
        /// (<paramref name="confirmLabel"/>: ABANDON, QUIT). BACK (or Esc) closes it.</summary>
        void AskToConfirm(string taunt, string warning, string confirmLabel);

        bool IsVisible { get; }

        /// <summary>Closes a panel open over the menu (the New Game warning, the rules); false when there was none.</summary>
        bool CloseOverlay();

        /// <summary>Shows the menu; Continue and Change Table only when a run is in progress.</summary>
        void Show(bool canContinue);

        void Hide();
    }
}
