using System;

namespace HellPoker.Presentation.Abstractions
{
    public interface IMainMenuView
    {
        event Action NewGamePressed;
        event Action ContinuePressed;
        event Action ChangeTablePressed;
        event Action QuitPressed;

        bool IsVisible { get; }

        /// <summary>Shows the menu; Continue and Change Table only when a run is in progress.</summary>
        void Show(bool canContinue);

        void Hide();
    }
}
