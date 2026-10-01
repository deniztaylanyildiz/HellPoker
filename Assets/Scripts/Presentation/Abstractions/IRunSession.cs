namespace HellPoker.Presentation.Abstractions
{
    /// <summary>The current run as the menu sees it.</summary>
    public interface IRunSession
    {
        /// <summary>True when a run has started and is not over yet.</summary>
        bool CanContinue { get; }

        void StartNewRun();
    }
}
