namespace HellPoker.Core.Cheats
{
    /// <summary>
    /// Asked before every cheat strikes. Today nothing stops a demon; later the player's abilities hook in here
    /// (see through a lie, ward a card, block a cheat outright).
    /// </summary>
    public interface ICheatGuard
    {
        /// <returns>False to block the cheat: it is spent without effect.</returns>
        bool Allows(ICheat cheat, CheatTable table);
    }

    /// <summary>The guard of a player with no abilities: every cheat goes through.</summary>
    public sealed class AllowEveryCheat : ICheatGuard
    {
        public static readonly AllowEveryCheat Instance = new AllowEveryCheat();

        public bool Allows(ICheat cheat, CheatTable table) => true;
    }
}
