namespace HellPoker.Core.Cards
{
    /// <summary>A deck that can carry jokers (the Jester's run): how many it holds from the next reset on.</summary>
    public interface IJokerDeck
    {
        int Jokers { get; }

        /// <summary>The deck holds this many jokers from its next <see cref="IDeck.Reset"/> on; 0 for the plain 52.</summary>
        void SetJokers(int count);
    }
}