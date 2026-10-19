namespace HellPoker.Presentation.Abstractions
{
    /// <summary>
    /// Demo 3.1: how the player's sinner feels at a chapter's table, as the presenter sees it. The view turns it into an animation of the
    /// class's portrait (the presenter knows no animations): calm breathing, a small joy, a big one, a wince, the tension of a sealed hand.
    /// </summary>
    public enum SinnerMood
    {
        /// <summary>Between hands and while playing: the idle loop.</summary>
        Calm,

        /// <summary>A hand won (high card to trips): a short smile, then calm.</summary>
        Pleased,

        /// <summary>A strong hand won (a straight and up): a burst of joy, then calm.</summary>
        Elated,

        /// <summary>A hand lost: a wince, then calm.</summary>
        Hurt,

        /// <summary>A sealed hand or the soul's line near: a tense loop in place of the idle one, until calm again.</summary>
        Tense
    }
}
