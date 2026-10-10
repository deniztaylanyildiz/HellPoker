using System;
using HellPoker.Core.Cheats;

namespace HellPoker.Core.Chapters
{
    /// <summary>
    /// The purgatory fire's "break the demon's first cheat": at the demon's table the first minor cheat that would really strike is
    /// refused (one that would come to nothing does not use it up); every cheat after it goes to the run's own guard (the sinner).
    /// </summary>
    public sealed class FirstCheatBreaker : ICheatGuard
    {
        private readonly ICheatGuard _next;

        /// <summary>True once the first cheat was broken.</summary>
        public bool Used { get; private set; }

        public FirstCheatBreaker(ICheatGuard next)
        {
            _next = next ?? AllowEveryCheat.Instance;
        }

        public bool Allows(ICheat cheat, CheatTable table)
        {
            if (cheat == null) throw new ArgumentNullException(nameof(cheat));
            if (!Used && cheat.Tier == CheatTier.Minor && cheat.CanApply(table))
            {
                Used = true;
                return false;
            }
            return _next.Allows(cheat, table);
        }
    }
}
