using System;
using HellPoker.Core.Cheats;

namespace HellPoker.Core.Sinners
{
    /// <summary>What a sinner class can do when its charge is full (its power), at the table and in the save.</summary>
    public enum SinnerAbility
    {
        /// <summary>No power.</summary>
        None,

        /// <summary>The Peasant: an honest heart — walk away from this hand for nothing (the fold costs no years).</summary>
        FreeFold,

        /// <summary>The Warlock: the minor cheat announced is warded off when it comes (refused by the guard).</summary>
        Ward,

        /// <summary>The King: before the draw, one card is put under protection for the hand — no cheat may touch it.</summary>
        Protect
    }

    /// <summary>
    /// How a sinner's power charges: a won hand +1, a lost one +2, a fold +1 (a fold is a loss, but pays less, so folding
    /// cannot be farmed), a tie nothing — up to <see cref="Full"/>; a full gauge waits until the player uses the power.
    /// (The designer's numbers. A hand the Peasant walks away from with his power charges nothing.)
    /// </summary>
    public sealed class ChargeRules
    {
        public static readonly ChargeRules Default = new ChargeRules();

        /// <summary>The gauge is full (the power is ready) at this.</summary>
        public int Full { get; }
        public int PerWin { get; }
        public int PerLoss { get; }
        public int PerFold { get; }
        public int PerTie { get; }

        public ChargeRules(int full = 5, int perWin = 1, int perLoss = 2, int perFold = 1, int perTie = 0)
        {
            if (full <= 0) throw new ArgumentOutOfRangeException(nameof(full));
            if (perWin < 0 || perLoss < 0 || perFold < 0 || perTie < 0) throw new ArgumentOutOfRangeException(nameof(perWin));
            Full = full;
            PerWin = perWin;
            PerLoss = perLoss;
            PerFold = perFold;
            PerTie = perTie;
        }
    }

    /// <summary>
    /// A sinner class: who the player was in life, and what that buys them in Hell. A class is a small rule package — the
    /// starting sentence, passive traits (a change to the payouts, seeing through lies) and one power that the run's charge
    /// gauge pays for (<see cref="Sinner"/>).
    /// A new class is a new subclass and one line in <see cref="SinnerRoster"/>; nothing else needs to change.
    /// </summary>
    public abstract class SinnerClass
    {
        /// <summary>Stable id: the save, the records and the presentation's words and art.</summary>
        public abstract string Id { get; }

        /// <summary>The sentence a run starts with.</summary>
        public virtual int StartingYears => 1000;

        /// <summary>A won hand forgives this share of the ante more, in percent (100: the ante multiplier +1).</summary>
        public virtual int WinAntePercent => 0;

        /// <summary>The power the charge gauge pays for.</summary>
        public virtual SinnerAbility Ability => SinnerAbility.None;

        /// <summary>True when the class sees through a demon's lie the moment it is told.</summary>
        public virtual bool SeesLies => false;

        /// <summary>How many House cards turn before the last decision for this class at a table with these rules (the table's own
        /// number unless the class sees more).</summary>
        public virtual int HouseCardsShownAt(Game.GameRules rules) => rules.HouseCardsShown;

        public override string ToString() => Id;
    }

    /// <summary>
    /// The run's sinner: the class and its power's charge gauge. The gauge belongs to the run — a new table, Lucifer's summons
    /// and the fall all keep it. It fills with every settled hand (<see cref="ChargeRules"/>); full, the power may be used —
    /// only when the player chooses (nothing is ever spent on its own) — and the gauge empties. It is also the guard every
    /// cheat asks: a ward the Warlock raised refuses the next minor cheat that would really strike.
    /// </summary>
    public sealed class Sinner : ICheatGuard
    {
        public SinnerClass Class { get; }

        public ChargeRules Rules { get; }

        /// <summary>The gauge, 0 to <see cref="ChargeRules.Full"/>.</summary>
        public int Charge { get; private set; }

        /// <summary>True when the gauge is full and the class has a power to use.</summary>
        public bool IsCharged => Class.Ability != SinnerAbility.None && Charge >= Rules.Full;

        /// <summary>The Warlock's ward is up: the next minor cheat that would strike is refused.</summary>
        public bool WardRaised { get; private set; }

        /// <summary>
        /// The power is switched on and waiting (the Peasant's free fold until he folds, the King's protection until he picks a
        /// card) — the gauge is spent only when it is used. Off again with <see cref="Disarm"/>, or when it is used.
        /// </summary>
        public bool PowerArmed { get; private set; }

        /// <summary>Switches the power on (a full gauge, a power that waits for a move: the free fold, the protection).</summary>
        public bool Arm()
        {
            if (!IsCharged || (Class.Ability != SinnerAbility.FreeFold && Class.Ability != SinnerAbility.Protect)) return false;
            PowerArmed = true;
            return true;
        }

        /// <summary>Switches the power off again; the gauge stays as it is.</summary>
        public void Disarm() => PowerArmed = false;

        /// <summary>How many times the power was used, and how many cheats were warded off (the whole run).</summary>
        public int PowersUsed { get; private set; }
        public int WardsUsed { get; private set; }

        /// <param name="charge">The gauge (a saved run's); clamped to 0..full.</param>
        /// <param name="wardRaised">A ward raised and still waiting (a saved run's).</param>
        public Sinner(SinnerClass sinnerClass, int charge = 0, ChargeRules rules = null, bool wardRaised = false)
        {
            Class = sinnerClass ?? throw new ArgumentNullException(nameof(sinnerClass));
            Rules = rules ?? ChargeRules.Default;
            Charge = Math.Max(0, Math.Min(Rules.Full, charge));
            WardRaised = wardRaised && Class.Ability == SinnerAbility.Ward;
        }

        public string Id => Class.Id;

        public SinnerAbility Ability => Class.Ability;

        /// <summary>A hand was settled: the gauge fills (a win +1, a loss +2, a fold +1, a tie nothing), up to full.</summary>
        public void HandSettled(bool folded, Game.ShowdownOutcome? outcome)
        {
            int gain = folded ? Rules.PerFold
                : outcome == Game.ShowdownOutcome.PlayerWins ? Rules.PerWin
                : outcome == Game.ShowdownOutcome.HouseWins ? Rules.PerLoss
                : Rules.PerTie;
            Charge = Math.Min(Rules.Full, Charge + gain);
        }

        /// <summary>Spends the full gauge on <paramref name="ability"/>; false when it is not full (or the class has another power).</summary>
        public bool TryUse(SinnerAbility ability)
        {
            if (ability == SinnerAbility.None || Class.Ability != ability || !IsCharged) return false;
            Charge = 0;
            PowerArmed = false;
            PowersUsed++;
            if (ability == SinnerAbility.Ward) WardRaised = true;
            return true;
        }

        /// <summary>The ward refuses the next minor cheat that would really strike (one that would come to nothing keeps it up).</summary>
        public bool Allows(ICheat cheat, CheatTable table)
        {
            if (!WardRaised || cheat == null || cheat.Tier != CheatTier.Minor || !cheat.CanApply(table)) return true;
            WardRaised = false;
            WardsUsed++;
            return false;
        }
    }
}
