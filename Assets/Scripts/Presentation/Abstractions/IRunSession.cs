using System;
using HellPoker.Core.Dealers;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>What walking away from the current run would cost (the New Game warning says it).</summary>
    public enum AbandonRisk
    {
        /// <summary>No run to walk away from.</summary>
        None,

        /// <summary>Between hands: the run is forgotten.</summary>
        Run,

        /// <summary>A hand is on the table: it counts as folded, the years go on the run before it ends.</summary>
        Hand,

        /// <summary>The soul is on the table: walking away counts as damnation.</summary>
        Soul
    }

    /// <summary>The current run as the menu sees it.</summary>
    public interface IRunSession
    {
        /// <summary>True when a run has started and is not over yet.</summary>
        bool CanContinue { get; }

        /// <summary>The id of the demon the player sits with; null before the first run.</summary>
        string CurrentDealerId { get; }

        /// <summary>Raised when the player asks to leave the table and is allowed to (between hands, soul not at stake).</summary>
        event Action LeaveRequested;

        /// <summary>Raised when the player moves on from a finished run (absolved or damned): time for the end screen.</summary>
        event Action<RunSummary> RunEnded;

        /// <summary>Records across all runs.</summary>
        Core.Game.RecordBook Records { get; }

        /// <summary>What walking away from the run in progress would cost; <see cref="AbandonRisk.None"/> without one.</summary>
        AbandonRisk AbandonRisk { get; }

        /// <summary>The player walks away from the run in progress (a new game over it): a hand on the table is forfeited,
        /// a staked soul counts as damned. Nothing to continue afterwards.</summary>
        void AbandonRun();

        /// <summary>Starts a fresh sentence at the table of <paramref name="dealer"/>, as a <paramref name="sinner"/> (the Peasant if none).</summary>
        void StartNewRun(Dealer dealer, Core.Sinners.SinnerClass sinner = null);

        /// <summary>The player wants to change tables: raises <see cref="LeaveRequested"/>, or the dealer refuses (soul bound).</summary>
        void RequestLeave();

        /// <summary>True when sitting with <paramref name="dealer"/> would put the player's soul straight on the table.</summary>
        bool WouldStakeSoul(Dealer dealer);

        /// <summary>Moves the player, sentence and all, to <paramref name="dealer"/>'s table.</summary>
        void SwitchTable(Dealer dealer);
    }
}
