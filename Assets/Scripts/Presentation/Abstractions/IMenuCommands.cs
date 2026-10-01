namespace HellPoker.Presentation.Abstractions
{
    /// <summary>Menu intents an input source can trigger.</summary>
    public interface IMenuCommands
    {
        bool IsMenuOpen { get; }

        /// <summary>Opens the menu from the table, or returns to a run in progress.</summary>
        void ToggleMenu();
    }
}
