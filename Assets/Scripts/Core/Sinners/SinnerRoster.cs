using System.Collections.Generic;
using System.Linq;

namespace HellPoker.Core.Sinners
{
    /// <summary>Every sinner class the player may choose, in the order of the choice screen. The Peasant is the default.</summary>
    public static class SinnerRoster
    {
        public static readonly SinnerClass Peasant = new Peasant();
        public static readonly SinnerClass Warlock = new Warlock();
        public static readonly SinnerClass King = new King();
        public static readonly SinnerClass Jester = new Jester();

        public static readonly SinnerClass Summoner = new Summoner();
        public static readonly SinnerClass Thief = new Thief();

        /// <summary>The demo's classes (the demo's table engine and its tests; the game plays <see cref="PhaseTwo"/>).</summary>
        public static IReadOnlyList<SinnerClass> All { get; } = new[] { Peasant, Warlock, King, Jester };

        /// <summary>Phase 2's classes: the demo's, the Summoner and the Thief (Demo 2.2: all open from the start). The icon strip's order.</summary>
        public static IReadOnlyList<SinnerClass> PhaseTwo { get; } = new[] { Peasant, Warlock, King, Jester, Summoner, Thief };

        /// <summary>The class with this id (Phase 2's included); null for an unknown one.</summary>
        public static SinnerClass Find(string id) => PhaseTwo.FirstOrDefault(c => c.Id == id);
    }
}
