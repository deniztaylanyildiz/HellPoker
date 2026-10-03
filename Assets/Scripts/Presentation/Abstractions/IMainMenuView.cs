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


        /// <summary>The player accepted the New Game warning (walk away from the run in progress).</summary>
        event Action NewGameConfirmed;

        /// <summary>True while the New Game warning is open.</summary>
        bool IsConfirming { get; }

        /// <summary>Opens the New Game warning: the demon's word on it, and what walking away costs. BACK (or Esc) closes it.</summary>
        void AskToConfirmNewGame(string taunt, string warning);

        bool IsVisible { get; }

        /// <summary>Closes a panel open over the menu (the New Game warning, the rules); false when there was none.</summary>
        bool CloseOverlay();

        /// <summary>Shows the menu; Continue and Change Table only when a run is in progress.</summary>
        void Show(bool canContinue);

        void Hide();
    }
}
