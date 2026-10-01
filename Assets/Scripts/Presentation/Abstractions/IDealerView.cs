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

    /// <summary>The demon at the table: portrait, name and what they say.</summary>
    public interface IDealerView
    {
        void SetDealer(DealerCard dealer);

        /// <summary>The dealer speaks a line in a mood.</summary>
        void Say(string line, DealerMood mood);
    }
}
