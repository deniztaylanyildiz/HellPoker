using System;

namespace HellPoker.Presentation.Abstractions
{
    public interface IMainMenuView
    {
        event Action NewGamePressed;
        event Action ContinuePressed;
        event Action QuitPressed;
        event Action SettingsPressed;
        event Action RecordsPressed;

        /// <summary>The player accepted the warning open over the menu (New Game or Quit over a run in progress).</summary>
        event Action Confirmed;

        /// <summary>True while the rules (How to Play) are open over the menu.</summary>
        bool IsShowingRules { get; }

        /// <summary>The arrows / Q-E while How to Play is open: <paramref name="step"/> pages on (negative: back).</summary>
        void TurnRulesPage(int step);

        /// <summary>True while a warning (New Game, Quit) is open.</summary>
        bool IsConfirming { get; }

        /// <summary>Opens a warning: the demon's word on it, what it costs, and the button that goes ahead
        /// (<paramref name="confirmLabel"/>: ABANDON, QUIT, YES). BACK (or <paramref name="cancelLabel"/>, or Esc) closes it.</summary>
        void AskToConfirm(string taunt, string warning, string confirmLabel, string cancelLabel = null);

        bool IsVisible { get; }

        /// <summary>Closes a panel open over the menu (the New Game warning, the rules); false when there was none.</summary>
        bool CloseOverlay();

        /// <summary>Shows the menu; CONTINUE only when a run waits.</summary>
        void Show(bool canContinue);

        /// <summary>Demo 3.3: the card beside CONTINUE — the run that waits, in short; null: no card.</summary>
        void SetContinueCard(ContinueCard card);

        /// <summary>Demo 3.3: the arrows on the title menu move its cursor from button to button (<paramref name="dx"/> across the columns,
        /// <paramref name="dy"/> down the rows).</summary>
        void MoveSelection(int dx, int dy);

        /// <summary>Demo 3.3: Enter / Space on the title menu press the button under the cursor.</summary>
        void PressSelection();

        void Hide();
    }
}
