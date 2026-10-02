using HellPoker.Core.Game;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>Player intents any input source (mouse, keyboard, gamepad) can trigger.</summary>
    public interface ITableCommands
    {
        /// <summary>
        /// The default move: deal, draw, next hand or restart depending on the phase;
        /// during a bet decision it passes (when passing is allowed); against a house re-raise it calls.
        /// </summary>
        void PerformAction();

        void Bet(BetAction action);

        /// <summary>Passes every card still to come before the draw in one go (D).</summary>
        void CheckToDraw();

        void ToggleDiscard(int index);

        /// <summary>Opens or closes the hand ranking panel (H).</summary>
        void ToggleHandRanks();

        /// <summary>Closes whatever is open over the table (Esc); false when nothing was.</summary>
        bool CloseOverlay();
    }
}
