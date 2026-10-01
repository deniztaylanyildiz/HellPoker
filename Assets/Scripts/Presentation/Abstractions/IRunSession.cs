using HellPoker.Core.Dealers;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>The current run as the menu sees it.</summary>
    public interface IRunSession
    {
        /// <summary>True when a run has started and is not over yet.</summary>
        bool CanContinue { get; }

        /// <summary>Starts a fresh sentence at the table of <paramref name="dealer"/>.</summary>
        void StartNewRun(Dealer dealer);
    }
}
