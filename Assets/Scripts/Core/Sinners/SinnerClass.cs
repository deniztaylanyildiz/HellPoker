using System;
using HellPoker.Core.Cheats;

namespace HellPoker.Core.Sinners
{
    /// <summary>What a sinner class can do, at the table and in the save.</summary>
    public enum SinnerAbility
    {
        /// <summary>No ability.</summary>
        None,

        /// <summary>The Peasant: the run's first fold costs nothing.</summary>
        FreeFold,

        /// <summary>The Warlock: a minor cheat about to strike is warded off (refused by the guard).</summary>
        Ward,

        /// <summary>The King: before the draw, one card is put under protection for the hand — no cheat may touch it.</summary>
        Protect
    }

    /// <summary>
    /// A sinner class: who the player was in life, and what that buys them in Hell. A class is a small rule package — the
    /// starting sentence, a change to the payouts, an ability with charges (per run or per table) and, through the run's
    /// <see cref="Sinner"/>, a say over the demons' cheats (<see cref="ICheatGuard"/>).
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

        public virtual SinnerAbility Ability => SinnerAbility.None;

        /// <summary>Charges of the ability for the whole run (never refilled).</summary>
        public virtual int ChargesPerRun => 0;

        /// <summary>Charges of the ability at each table (refilled at every new table — Lucifer's included).</summary>
        public virtual int ChargesPerTable => 0;

        /// <summary>True when the class sees through a demon's lie the moment it is told.</summary>
        public virtual bool SeesLies => false;

        /// <summary>How many House cards turn before the last decision for this class at a table with these rules (the table's own
        /// number unless the class sees more).</summary>
        public virtual int HouseCardsShownAt(Game.GameRules rules) => rules.HouseCardsShown;

        /// <summary>The charges a run starts with (and, for a per-table ability, every new table).</summary>
        public int FullCharges => ChargesPerTable > 0 ? ChargesPerTable : ChargesPerRun;

        /// <summary>
        /// Asked before a cheat strikes (through the run's <see cref="Sinner"/>). Return false to refuse it; spend a charge with
        /// <paramref name="sinner"/> when the ability is used.
        /// </summary>
        public virtual bool Allows(ICheat cheat, CheatTable table, Sinner sinner) => true;

        public override string ToString() => Id;
    }

    /// <summary>
    /// The run's sinner: the class and what is left of its ability. It is the guard every cheat asks (the Warlock's ward),
    /// and it carries the charges from table to table — refilled at a new table for a per-table ability, kept for a per-run one.
    /// </summary>
    public sealed class Sinner : ICheatGuard
    {
        public SinnerClass Class { get; }

        /// <summary>What is left of the ability (at this table, or this run).</summary>
        public int Charges { get; private set; }

        /// <summary>How many cheats this sinner has warded off (the whole run).</summary>
        public int WardsUsed { get; private set; }

        public Sinner(SinnerClass sinnerClass, int? charges = null)
        {
            Class = sinnerClass ?? throw new ArgumentNullException(nameof(sinnerClass));
            int full = sinnerClass.FullCharges;
            Charges = Math.Max(0, Math.Min(full, charges ?? full));
        }

        public string Id => Class.Id;

        public SinnerAbility Ability => Class.Ability;

        /// <summary>A new table: a per-table ability is full again; a per-run one stays as it is.</summary>
        public void SitDown()
        {
            if (Class.ChargesPerTable > 0)
                Charges = Class.ChargesPerTable;
        }

        /// <summary>Spends one charge of <paramref name="ability"/>; false when none is left (or the class has another ability).</summary>
        public bool TrySpend(SinnerAbility ability)
        {
            if (ability == SinnerAbility.None || Class.Ability != ability || Charges <= 0) return false;
            Charges--;
            if (ability == SinnerAbility.Ward) WardsUsed++;
            return true;
        }

        public bool CanUse(SinnerAbility ability) => ability != SinnerAbility.None && Class.Ability == ability && Charges > 0;

        public bool Allows(ICheat cheat, CheatTable table) => Class.Allows(cheat, table, this);
    }
}
