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

        /// <summary>K (or the class badge): use the class's power when its gauge is full (the King then picks a card, or stops picking).</summary>
        void UsePower();

        /// <summary>S (or SHUFFLE): between hands, pay to have the deck shuffled back to 52.</summary>
        void ShuffleDeck();

        /// <summary>The joker picker at the showdown: step the card's rank and / or suit (the arrow keys).</summary>
        void StepJoker(int rankStep, int suitStep);

        /// <summary>Opens or closes the hand ranking panel (H).</summary>
        void ToggleHandRanks();

        /// <summary>Closes whatever is open over the table (Esc); false when nothing was.</summary>
        bool CloseOverlay();
    }
}
