namespace HellPoker.Core.Cheats
{
    /// <summary>Small everyday tricks, or the big ones a demon saves for the end of a sentence.</summary>
    public enum CheatTier
    {
        Minor,
        Major
    }

    /// <summary>The moment of the hand a cheat strikes.</summary>
    public enum CheatTiming
    {
        /// <summary>Right after the deal (before the first decision).</summary>
        AfterDeal,

        /// <summary>When the player's five cards are out, before the player chooses discards.</summary>
        BeforeDraw,

        /// <summary>Right after the exchange of cards.</summary>
        AfterDraw,

        /// <summary>When the House turns its cards before the last decision.</summary>
        HouseReveal,

        /// <summary>When the hands meet, before the hand is settled.</summary>
        BeforeShowdown
    }

    /// <summary>
    /// A demon's trick. It is announced as an intent at the start of the hand (the demon's malice gauge is full) and strikes
    /// at its <see cref="Timing"/> — always in plain sight: every change it makes is reported in its <see cref="CheatResult"/>.
    /// Cards of the Dead Man's Hand (A♠ A♣ 8♠ 8♣) are never touched: a cheat aimed at one moves to another card or misses.
    /// Every cheat can be refused by an <see cref="ICheatGuard"/> before it strikes.
    /// </summary>
    public interface ICheat
    {
        /// <summary>Stable id, also the key of its name, icon and lines in the presentation (see <see cref="CheatIds"/>).</summary>
        string Id { get; }

        CheatTier Tier { get; }

        CheatTiming Timing { get; }

        /// <summary>True when the cheat has something to work on at the table as it stands.</summary>
        bool CanApply(CheatTable table);

        /// <summary>Strikes: changes the table and says exactly what changed.</summary>
        CheatResult Apply(CheatTable table);
    }

    /// <summary>The ids of every cheat (keys for the presentation and the save).</summary>
    public static class CheatIds
    {
        // Mammon
        public const string Collateral = "collateral";
        public const string Tithe = "tithe";
        public const string Buyout = "buyout";

        // Belial
        public const string FalseFace = "false_face";
        public const string ForkedTongue = "forked_tongue";
        public const string SerpentSwap = "serpent_swap";

        // Lilith
        public const string NightVeil = "night_veil";
        public const string Thorn = "thorn";
        public const string Moonless = "moonless";

        // Lucifer
        public const string Gaze = "gaze";
        public const string Rewrite = "rewrite";
        public const string BurningCard = "burning_card";
        public const string TheFall = "the_fall";
    }
}
