namespace HellPoker.Presentation.Abstractions
{
    /// <summary>What the table needs to know of the player's settings to guide a newcomer.</summary>
    public interface IGuideSettings
    {
        /// <summary>Show the current hand's name and hint which cards to keep.</summary>
        bool HandGuide { get; }

        bool HasSeenTip(string tip);
        void MarkTipSeen(string tip);
    }
}
