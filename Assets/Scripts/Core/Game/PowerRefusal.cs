namespace HellPoker.Core.Game
{
    /// <summary>Why the sinner's power cannot be used right now (the presentation puts it into words).</summary>
    public enum PowerRefusal
    {
        /// <summary>It can be used.</summary>
        None,

        /// <summary>No class, or a class without a power.</summary>
        NoPower,

        /// <summary>The gauge is not full yet.</summary>
        NotCharged,

        /// <summary>No hand is being played (between hands, or after it was settled).</summary>
        NoHand,

        /// <summary>The Peasant: folding is not possible now (a sealed hand).</summary>
        CannotFold,

        /// <summary>The Warlock: no cheat has been announced this hand.</summary>
        NoCheatAnnounced,

        /// <summary>The Warlock: the cheat coming is a major one (The Fall among them) — beyond a ward.</summary>
        MajorCheat,

        /// <summary>The Warlock: a ward is already up.</summary>
        WardAlreadyRaised,

        /// <summary>The King: only before the draw.</summary>
        NotBeforeDraw,

        /// <summary>The King: no card he can see is left to protect.</summary>
        NoCardToProtect
    }
}
