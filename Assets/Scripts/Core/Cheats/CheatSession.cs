using System;
using System.Collections.Generic;
using HellPoker.Core.Cards;
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

        /// <summary>How often (percent) the demon's slippery cheats slip (see <see cref="Dealers.Dealer.BackfirePercent"/>).</summary>
        public int BackfirePercent { get; }

        /// <summary>The announced intent, until it is played out (Belial's may be a lie); null when nothing is coming.</summary>
        public ICheat Intent => _pick != null && !_resolved ? _pick.Shown : null;

        /// <summary>The cheat really coming this hand (for the save); null when none.</summary>
        public ICheat Planned => _pick?.Cheat;

        /// <summary>True once this hand's cheat struck, was blocked or fizzled.</summary>
        public bool IsResolved => _resolved;

        /// <param name="policy">The demon's cheats; null for a demon who never cheats.</param>
        /// <param name="guard">Asked before each cheat strikes; null lets every cheat through.</param>
        /// <param name="backfirePercent">How often a cheat that may slip does (Belial's tongue).</param>
        public CheatSession(ICheatPolicy policy, int maliceMax, IRandomSource random, ICheatGuard guard = null, int backfirePercent = 0)
        {
            if (maliceMax < 0) throw new ArgumentOutOfRangeException(nameof(maliceMax));
            if (backfirePercent < 0 || backfirePercent > 100) throw new ArgumentOutOfRangeException(nameof(backfirePercent));
            BackfirePercent = backfirePercent;
            if (policy != null && maliceMax > 0 && random == null) throw new ArgumentNullException(nameof(random));
            _policy = policy;
            MaliceMax = policy == null ? 0 : maliceMax;
            _random = random;
            _guard = guard ?? AllowEveryCheat.Instance;
        }

        /// <summary>The start of a hand: the gauge grows, and when it is full the demon picks this hand's cheat.</summary>
        /// <param name="extraGain">More malice this hand (a relic's curse: the Rusty Crown).</param>
        public void BeginHand(GameRules rules, int years, int extraGain = 0)
        {
            ClearHand();
            if (!IsActive) return;

            int gain = rules.MalicePerHand + Math.Max(0, extraGain);
            if (!rules.IsFinalTable && years <= rules.MaliceLowSentenceYears)
                gain += rules.MaliceLowSentenceBonus;
            if (Grudge > 0)
            {
                gain += rules.GrudgeMalicePerHand;
                Grudge--;
            }
            Malice = Math.Min(MaliceMax, Malice + gain);

            if (Malice >= MaliceMax)
                _pick = _policy.Choose(new CheatContext(years, rules.MajorCheatYears, rules.MajorCheatPercent, MajorUsed), _random);
        }

        /// <summary>Hands left in which the demon's malice grows faster: the player walked out on a cheat.</summary>
        public int Grudge { get; private set; }

        /// <summary>
        /// The player walked out on a hand the demon meant to cheat: the gauge fills at once (the cheat comes at the next
        /// deal) and the grudge (<see cref="GameRules.GrudgeHands"/>) makes the next ones come sooner.
        /// </summary>
        public void PlayerFled(GameRules rules)
        {
            if (!IsActive) return;
            Malice = MaliceMax;
            Grudge = Math.Max(Grudge, rules.GrudgeHands);
        }

        /// <summary>
        /// The gauge as it goes along to another demon's table: the same share of the gauge, rounded up (a full gauge stays
        /// full, a demon with a small gauge cannot be used to drain a big one). 0 when it was empty or either demon has none.
        /// </summary>
        public static int Carry(int malice, int fromMax, int toMax)
        {
            if (malice <= 0 || fromMax <= 0 || toMax <= 0) return 0;
            return Math.Min(toMax, (Math.Min(malice, fromMax) * toMax + fromMax - 1) / fromMax);
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
            {
                Hand before = state.PlayerHand;
                result = cheat.Apply(state);
                // A cheat that left the player stronger turned on its demon: everyone sees it.
                if (result.Outcome == CheatOutcome.Played && state.Evaluator.Evaluate(state.PlayerHand).CompareTo(state.Evaluator.Evaluate(before)) > 0)
                    result = result.AsBackfire();
            }

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

        /// <summary>
        /// A saved run comes back — or the player sits down at another table, the demons' malice going along: the gauge as it
        /// was (no fuller than this demon's), the grudge, and whether the big cheat has been spent at this table.
        /// </summary>
        public void Restore(int malice, bool majorUsed, int grudge = 0)
        {
            if (malice < 0) throw new ArgumentOutOfRangeException(nameof(malice));
            if (grudge < 0) throw new ArgumentOutOfRangeException(nameof(grudge));
            Malice = Math.Min(MaliceMax, malice);
            MajorUsed = majorUsed;
            Grudge = IsActive ? grudge : 0;
        }
    }
}
