using System;
using System.Collections.Generic;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;

namespace HellPoker.Core.Cheats
{
    /// <summary>
    /// A demon's cheating at one table: the malice gauge, the cheat chosen when it fills (announced as the hand's intent),
    /// striking at the cheat's moment, and the marks and results it leaves on the hand.
    /// - Malice: +<see cref="GameRules.MalicePerHand"/> every hand, +<see cref="GameRules.MalicePerWin"/> when the player wins,
    ///   +<see cref="GameRules.MaliceLowSentenceBonus"/> more at or below <see cref="GameRules.MaliceLowSentenceYears"/>
    ///   (not at the final table). Full: a cheat is chosen at the deal.
    /// - A cheat that strikes (or is blocked) empties the gauge. One that finds nothing to work on fizzles and the gauge stays
    ///   full; one whose moment never comes (the player folded first) also leaves it full — the next hand picks again.
    /// </summary>
    public sealed class CheatSession
    {
        private readonly ICheatPolicy _policy;
        private readonly IRandomSource _random;
        private readonly ICheatGuard _guard;
        private readonly List<CheatResult> _results = new List<CheatResult>();
        private CheatPick _pick;
        private bool _resolved;

        public int MaliceMax { get; }
        public int Malice { get; private set; }

        /// <summary>True once a major cheat has struck at this table (Lucifer's Fall comes once per attempt).</summary>
        public bool MajorUsed { get; private set; }

        public CheatMarks Marks { get; } = new CheatMarks();

        /// <summary>What happened with this hand's cheat (at most one entry; empty when nothing struck yet).</summary>
        public IReadOnlyList<CheatResult> Results => _results;

        public bool IsActive => _policy != null && MaliceMax > 0;

        public ICheatPolicy Policy => _policy;

        /// <summary>The announced intent, until it is played out (Belial's may be a lie); null when nothing is coming.</summary>
        public ICheat Intent => _pick != null && !_resolved ? _pick.Shown : null;

        /// <summary>The cheat really coming this hand (for the save); null when none.</summary>
        public ICheat Planned => _pick?.Cheat;

        /// <summary>True once this hand's cheat struck, was blocked or fizzled.</summary>
        public bool IsResolved => _resolved;

        /// <param name="policy">The demon's cheats; null for a demon who never cheats.</param>
        /// <param name="guard">Asked before each cheat strikes; null lets every cheat through.</param>
        public CheatSession(ICheatPolicy policy, int maliceMax, IRandomSource random, ICheatGuard guard = null)
        {
            if (maliceMax < 0) throw new ArgumentOutOfRangeException(nameof(maliceMax));
            if (policy != null && maliceMax > 0 && random == null) throw new ArgumentNullException(nameof(random));
            _policy = policy;
            MaliceMax = policy == null ? 0 : maliceMax;
            _random = random;
            _guard = guard ?? AllowEveryCheat.Instance;
        }

        /// <summary>The start of a hand: the gauge grows, and when it is full the demon picks this hand's cheat.</summary>
        public void BeginHand(GameRules rules, int years)
        {
            ClearHand();
            if (!IsActive) return;

            int gain = rules.MalicePerHand;
            if (!rules.IsFinalTable && years <= rules.MaliceLowSentenceYears)
                gain += rules.MaliceLowSentenceBonus;
            Malice = Math.Min(MaliceMax, Malice + gain);

            if (Malice >= MaliceMax)
                _pick = _policy.Choose(new CheatContext(years, rules.MajorCheatYears, rules.MajorCheatPercent, MajorUsed), _random);
        }

        /// <summary>The player won a hand: the demon's malice grows.</summary>
        public void PlayerWon(GameRules rules)
        {
            if (IsActive) Malice = Math.Min(MaliceMax, Malice + rules.MalicePerWin);
        }

        /// <summary>
        /// The moment <paramref name="timing"/> has come: the hand's cheat strikes if it is due now.
        /// </summary>
        /// <param name="table">Builds the table as it stands (only called when a cheat is due).</param>
        /// <returns>The table after the cheat (to take its hands back), or null when nothing was due.</returns>
        public CheatTable Strike(CheatTiming timing, Func<CheatTable> table)
        {
            if (_pick == null || _resolved || _pick.Cheat.Timing != timing) return null;

            CheatTable state = table();
            ICheat cheat = _pick.Cheat;
            CheatResult result;
            if (!_guard.Allows(cheat, state))
                result = new CheatResult(cheat.Id, CheatOutcome.Blocked);
            else if (!cheat.CanApply(state))
                result = CheatResult.Fizzled(cheat.Id);
            else
                result = cheat.Apply(state);

            _resolved = true;
            _results.Add(result.AnnouncedAs(_pick.Shown.Id));
            if (result.Outcome != CheatOutcome.Fizzled)
            {
                Malice = 0;
                if (cheat.Tier == CheatTier.Major && result.Outcome == CheatOutcome.Played)
                    MajorUsed = true;
            }
            return state;
        }

        /// <summary>Between hands: everything of the last hand is gone (the gauge stays).</summary>
        public void ClearHand()
        {
            Marks.Clear();
            _results.Clear();
            _pick = null;
            _resolved = false;
        }

        /// <summary>A saved run comes back: the gauge as it was, and whether the big cheat has been spent at this table.</summary>
        public void Restore(int malice, bool majorUsed)
        {
            if (malice < 0) throw new ArgumentOutOfRangeException(nameof(malice));
            Malice = Math.Min(MaliceMax, malice);
            MajorUsed = majorUsed;
        }
    }
}
