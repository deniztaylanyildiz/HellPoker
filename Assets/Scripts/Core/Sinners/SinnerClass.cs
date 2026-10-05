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
    /// How a sinner's power charges: every hand won or lost adds a pip; a fold (a loss, but a cheap one) and a tie add nothing,
    /// so folding cannot be farmed. Up to <see cref="Full"/>; a full gauge waits until the player uses the power.
    /// (Planned as win +1 / loss +2 / fold +1: the Peasant's free fold at Mammon's long runs then rose +3.7 points over the
    /// balance line; loss +1 / fold 0 keeps every class within ±3 — see CLAUDE.md.)
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

        public ChargeRules(int full = 5, int perWin = 1, int perLoss = 1, int perFold = 0, int perTie = 0)
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

        /// <summary>A hand was settled: the gauge fills (a win or a loss a pip; a fold or a tie nothing), up to full.</summary>
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
