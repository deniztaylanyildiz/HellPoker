namespace HellPoker.Presentation.Abstractions
{
    /// <summary>How the dealer feels about what just happened. The view decides how that looks.</summary>
    public enum DealerMood
    {
        /// <summary>Plain talk: greetings, a push.</summary>
        Neutral,

        /// <summary>The player lost or fled: the dealer laughs.</summary>
        Gloating,

        /// <summary>The player won: the dealer fumes.</summary>
        Annoyed,

        /// <summary>The house just raised back: a sly look.</summary>
        Scheming,

        /// <summary>The gates are near: the dealer turns intense.</summary>
        Menacing
    }

    /// <summary>How a new demon takes the table.</summary>
    public enum SeatChange
    {
        /// <summary>At once (a new run, a chosen table, a resumed save).</summary>
        Instant,

        /// <summary>The player is summoned to Lucifer: the hall slowly goes dark, then his eyes open.</summary>
        Summoned,

        /// <summary>Lucifer casts the player down: the screen falls into the old demon's hall.</summary>
        CastDown
    }

    /// <summary>The demon at the table: portrait, name and what they say.</summary>
    public interface IDealerView
    {
        /// <param name="change">Instant changes happen at once; the others play their scene after what is already queued.</param>
        void SetDealer(DealerCard dealer, SeatChange change = SeatChange.Instant);

        /// <summary>The dealer speaks a line in a mood.</summary>
        void Say(string line, DealerMood mood);

        /// <summary>The same demon under the words of another language: name and title only, nothing replayed.</summary>
        void Relabel(DealerCard dealer);

        /// <summary>The line already on screen, in other words: shown whole at once — no typing, no look, nothing replayed.
        /// Nothing happens when no line is showing.</summary>
        void SetLine(string line);
    }
}
