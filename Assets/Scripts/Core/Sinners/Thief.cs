namespace HellPoker.Core.Sinners
{
    /// <summary>
    /// The Thief (Phase 2 only; Demo 2.2): a glance at one of the demon's face-down cards from the deal, and a quick hand — one of his
    /// cards for one of the demon's face-down ones, blind, once a hand before the draw. Caught, the swap fails and he pays a fine. His
    /// mechanic is a Phase 2 kit (<see cref="Chapters.TheftPowers"/>); the demo never offers him.
    /// </summary>
    public sealed class Thief : SinnerClass
    {
        public const string ClassId = "thief";

        public override string Id => ClassId;
    }
}
