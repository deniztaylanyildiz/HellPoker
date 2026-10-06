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

        public static IReadOnlyList<SinnerClass> All { get; } = new[] { Peasant, Warlock, King, Jester };

        /// <summary>The class with this id; null for an unknown one.</summary>
        public static SinnerClass Find(string id) => All.FirstOrDefault(c => c.Id == id);
    }
}
