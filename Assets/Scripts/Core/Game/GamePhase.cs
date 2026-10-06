namespace HellPoker.Core.Game
{
    public enum GamePhase
    {
        /// <summary>Waiting for the player to deal; the ante is set by the sentence.</summary>
        Betting,

        /// <summary>The player's cards turn (the first ones together); from the third card on, each is followed by raise / pass / fold.</summary>
        PlayerReveal,

        /// <summary>All five cards visible; waiting for the player to choose discards.</summary>
        Drawing,

        /// <summary>The new cards are in; one decision before the house shows its hand.</summary>
        DrawReveal,

        /// <summary>The house answered a raise with a re-raise; the player calls or folds.</summary>
        HouseReRaise,

        /// <summary>Some of the house's cards are turned; one last decision, then the showdown.</summary>
        HouseReveal,

        /// <summary>Hand settled; waiting to start the next round.</summary>
        RoundOver,

        /// <summary>Sentence reduced to zero. The player is free.</summary>
        Absolved,

        /// <summary>Sentence reached the damnation limit. Game over.</summary>
        Damned,

        /// <summary>The showdown waits for the player to name their joker (the Jester's deck): every card turned, then settled.</summary>
        NamingJoker
    }
}
