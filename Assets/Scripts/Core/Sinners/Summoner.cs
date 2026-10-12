namespace HellPoker.Core.Sinners
{
    /// <summary>
    /// The Summoner (Phase 2 only, unlocked by the first freedom): he calls up imps for a price — the Eye (a closed card of the
    /// demon's), the Hand (one of his cards for an open one of the demon's), the Whisper (the next cheat, truthfully). A summoned imp
    /// may run wild. His mechanic is a Phase 2 kit (<see cref="Chapters.SummonPowers"/>); the demo never offers him.
    /// </summary>
    public sealed class Summoner : SinnerClass
    {
        public const string ClassId = "summoner";

        public override string Id => ClassId;
    }
}