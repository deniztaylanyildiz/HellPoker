using HellPoker.Core.Game;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>Player intents any input source (mouse, keyboard, gamepad) can trigger.</summary>
    public interface ITableCommands
    {
        /// <summary>
        /// The default move: deal, draw, next hand or restart depending on the phase;
        /// during a bet decision it passes (when passing is allowed).
        /// </summary>
        void PerformAction();

        void Bet(BetAction action);

        void ToggleDiscard(int index);

        /// <summary>Moves the stake one option up (+1) or down (-1).</summary>
        void StepStake(int direction);
    }
}
