using System;
using HellPoker.Core.Dealers;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>The current run as the menu sees it.</summary>
    public interface IRunSession
    {
        /// <summary>True when a run has started and is not over yet.</summary>
        bool CanContinue { get; }

        /// <summary>The id of the demon the player sits with; null before the first run.</summary>
        string CurrentDealerId { get; }

        /// <summary>Raised when the player asks to leave the table and is allowed to (between hands, soul not at stake).</summary>
        event Action LeaveRequested;

        /// <summary>Starts a fresh sentence at the table of <paramref name="dealer"/>.</summary>
        void StartNewRun(Dealer dealer);

        /// <summary>The player wants to change tables: raises <see cref="LeaveRequested"/>, or the dealer refuses (soul bound).</summary>
        void RequestLeave();

        /// <summary>True when sitting with <paramref name="dealer"/> would put the player's soul straight on the table.</summary>
        bool WouldStakeSoul(Dealer dealer);

        /// <summary>Moves the player, sentence and all, to <paramref name="dealer"/>'s table.</summary>
        void SwitchTable(Dealer dealer);
    }
}
