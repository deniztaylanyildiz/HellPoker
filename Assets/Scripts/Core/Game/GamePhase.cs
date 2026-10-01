namespace HellPoker.Core.Game
{
    public enum GamePhase
    {
        /// <summary>Waiting for the player to choose the opening stake.</summary>
        Betting,

        /// <summary>The player's cards are turned one by one; after each one the player raises, passes or folds.</summary>
        PlayerReveal,

        /// <summary>All five cards visible; waiting for the player to choose discards.</summary>
        Drawing,

        /// <summary>The house's cards are turned one by one; the player raises, passes or folds in between.</summary>
        HouseReveal,

        /// <summary>Hand settled; waiting to start the next round.</summary>
        RoundOver,

        /// <summary>Sentence reduced to zero. The player is free.</summary>
        Absolved,

        /// <summary>Sentence reached the damnation limit. Game over.</summary>
        Damned
    }
}
