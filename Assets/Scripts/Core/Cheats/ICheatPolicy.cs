using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Randomness;

namespace HellPoker.Core.Cheats
{
    /// <summary>What the demon will do this hand, and what they say they will do (the same, unless they lie).</summary>
    public sealed class CheatPick
    {
        public ICheat Cheat { get; }

        /// <summary>The announced intent. Belial's is sometimes another cheat of his.</summary>
        public ICheat Shown { get; }

        public bool IsLie => Shown.Id != Cheat.Id;

        public CheatPick(ICheat cheat, ICheat shown = null)
        {
            Cheat = cheat ?? throw new ArgumentNullException(nameof(cheat));
            Shown = shown ?? cheat;
        }
    }

    /// <summary>The situation a demon picks a cheat in.</summary>
    public readonly struct CheatContext
    {
        public int Years { get; }

        /// <summary>At or below this sentence the big cheats come out.</summary>
        public int MajorCheatYears { get; }

        /// <summary>The chance (percent) of a big cheat once they may come out.</summary>
        public int MajorCheatPercent { get; }

        /// <summary>True once a big cheat has been played at this table (Lucifer plays The Fall once per attempt).</summary>
        public bool MajorUsed { get; }

        public CheatContext(int years, int majorCheatYears, int majorCheatPercent, bool majorUsed)
        {
            Years = years;
            MajorCheatYears = majorCheatYears;
            MajorCheatPercent = majorCheatPercent;
            MajorUsed = majorUsed;
        }
    }

    /// <summary>A demon's repertoire of cheats and how they choose one when their malice is full.</summary>
    public interface ICheatPolicy
    {
        IReadOnlyList<ICheat> Cheats { get; }

        CheatPick Choose(CheatContext context, IRandomSource random);

        /// <summary>A cheat of this demon by id (for saved games); null when unknown.</summary>
        ICheat Find(string id);
    }

    /// <summary>
    /// The usual way: a minor cheat — or, at or below the big-cheat sentence, a major one half the time. The intent shown is
    /// the truth, except for a liar (<see cref="LiePercent"/>), who sometimes announces another of their cheats.
    /// A demon with its own big-cheat line (<see cref="OwnMajorYears"/>) uses that instead; one whose big cheat comes once
    /// per table (<see cref="MajorOncePerTable"/>, Lucifer's Fall) does not repeat it.
    /// </summary>
    public sealed class DemonCheatPolicy : ICheatPolicy
    {
        private readonly ICheat[] _minor;
        private readonly ICheat[] _major;

        public IReadOnlyList<ICheat> Cheats { get; }
        public int LiePercent { get; }
        public int? OwnMajorYears { get; }
        public bool MajorOncePerTable { get; }

        public DemonCheatPolicy(IEnumerable<ICheat> minor, IEnumerable<ICheat> major, int liePercent = 0, int? ownMajorYears = null,
            bool majorOncePerTable = false)
        {
            _minor = (minor ?? Enumerable.Empty<ICheat>()).ToArray();
            _major = (major ?? Enumerable.Empty<ICheat>()).ToArray();
            if (_minor.Length == 0) throw new ArgumentException("A demon needs at least one minor cheat.", nameof(minor));
            if (liePercent < 0 || liePercent > 100) throw new ArgumentOutOfRangeException(nameof(liePercent));
            Cheats = _minor.Concat(_major).ToArray();
            LiePercent = liePercent;
            OwnMajorYears = ownMajorYears;
            MajorOncePerTable = majorOncePerTable;
        }

        public CheatPick Choose(CheatContext context, IRandomSource random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));

            int majorYears = OwnMajorYears ?? context.MajorCheatYears;
            bool majorPossible = _major.Length > 0 && context.Years <= majorYears && !(MajorOncePerTable && context.MajorUsed);
            bool major = majorPossible && random.Next(100) < context.MajorCheatPercent;
            ICheat[] tier = major ? _major : _minor;
            ICheat cheat = tier[random.Next(tier.Length)];

            ICheat shown = cheat;
            if (LiePercent > 0 && Cheats.Count > 1 && random.Next(100) < LiePercent)
            {
                ICheat[] others = Cheats.Where(c => c.Id != cheat.Id).ToArray();
                shown = others[random.Next(others.Length)];
            }
            return new CheatPick(cheat, shown);
        }

        public ICheat Find(string id) => Cheats.FirstOrDefault(c => c.Id == id);
    }
}
