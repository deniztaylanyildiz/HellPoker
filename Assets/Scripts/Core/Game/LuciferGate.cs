using System;

namespace HellPoker.Core.Game
{
    /// <summary>What the gate asks of the player between hands.</summary>
    public enum GateCall
    {
        /// <summary>Play on at this table.</summary>
        Stay,

        /// <summary>The sentence is down to the gate: the player is summoned to Lucifer's table.</summary>
        Summoned,

        /// <summary>The sentence climbed back above the gate at Lucifer's table: the player is cast down to where they came from.</summary>
        CastDown
    }

    /// <summary>
    /// A run's dealings with Lucifer. Between hands (and only then), a sentence at or below the gate summons the player to his
    /// table — wherever they sit — and the demon they came from is remembered. At his table the player cannot leave; a
    /// sentence back above the gate casts them down to that demon with at least <see cref="CastDownYears"/>, and reaching the
    /// gate again summons them again. Every summons is an attempt. Only at his table does the sentence end (Absolved).
    /// </summary>
    public sealed class LuciferGate
    {
        /// <summary>At or below this, between hands, Lucifer summons the player. 0: there is no Lucifer in this run.</summary>
        public int GateYears { get; }

        /// <summary>Being cast down leaves the sentence at least this.</summary>
        public int CastDownYears { get; }

        /// <summary>True while the player sits at Lucifer's table.</summary>
        public bool IsAtLucifer { get; private set; }

        /// <summary>The demon the player was summoned from (and is cast down to); null before the first summons.</summary>
        public string OriginDealerId { get; private set; }

        /// <summary>How many times the player has been summoned (the current sitting included).</summary>
        public int Attempts { get; private set; }

        /// <summary>How many times the player has been cast down.</summary>
        public int CastDowns => IsAtLucifer ? Attempts - 1 : Attempts;

        public bool ReachedLucifer => Attempts > 0;

        public LuciferGate(GameRules rules) : this(rules?.LuciferGateYears ?? 0, rules?.LuciferCastDownYears ?? 0)
        {
        }

        public LuciferGate(int gateYears, int castDownYears)
        {
            if (gateYears < 0) throw new ArgumentOutOfRangeException(nameof(gateYears));
            if (gateYears > 0 && castDownYears <= gateYears) throw new ArgumentOutOfRangeException(nameof(castDownYears));
            GateYears = gateYears;
            CastDownYears = castDownYears;
        }

        /// <summary>Rebuilds the state of a saved run.</summary>
        public LuciferGate(int gateYears, int castDownYears, bool isAtLucifer, string originDealerId, int attempts)
            : this(gateYears, castDownYears)
        {
            if (attempts < 0) throw new ArgumentOutOfRangeException(nameof(attempts));
            if (isAtLucifer && (attempts == 0 || string.IsNullOrEmpty(originDealerId)))
                throw new ArgumentException("A player at Lucifer's table was summoned from somewhere.", nameof(originDealerId));
            IsAtLucifer = isAtLucifer;
            OriginDealerId = originDealerId;
            Attempts = attempts;
        }

        /// <summary>What happens next with this sentence. Only between hands (<see cref="GamePhase.Betting"/>) does the gate act.</summary>
        public GateCall Check(int years, GamePhase phase)
        {
            if (GateYears == 0 || phase != GamePhase.Betting) return GateCall.Stay;
            if (IsAtLucifer) return years > GateYears ? GateCall.CastDown : GateCall.Stay;
            return years > 0 && years <= GateYears ? GateCall.Summoned : GateCall.Stay;
        }

        /// <summary>The player goes to Lucifer's table from <paramref name="fromDealerId"/>'s.</summary>
        public void Summon(string fromDealerId)
        {
            if (string.IsNullOrEmpty(fromDealerId)) throw new ArgumentException("Summoned from where?", nameof(fromDealerId));
            if (IsAtLucifer) throw new InvalidOperationException("Already at Lucifer's table.");
            OriginDealerId = fromDealerId;
            IsAtLucifer = true;
            Attempts++;
        }

        /// <summary>The player is thrown back to <see cref="OriginDealerId"/>'s table.</summary>
        /// <returns>The sentence they land with: at least <see cref="CastDownYears"/>.</returns>
        public int CastDown(int years)
        {
            if (!IsAtLucifer) throw new InvalidOperationException("Not at Lucifer's table.");
            IsAtLucifer = false;
            return Math.Max(years, CastDownYears);
        }
    }
}
