using System;

namespace HellPoker.Presentation.Abstractions
{
    public interface IMainMenuView
    {
        event Action NewGamePressed;
        event Action ContinuePressed;
        event Action ChangeTablePressed;
        event Action QuitPressed;
        event Action SettingsPressed;
        event Action RecordsPressed;

        bool IsVisible { get; }

        /// <summary>Closes a panel open over the menu (the rules); false when there was none.</summary>
        bool CloseOverlay();

        /// <summary>Shows the menu; Continue and Change Table only when a run is in progress.</summary>
        void Show(bool canContinue);

        void Hide();
    }
}
