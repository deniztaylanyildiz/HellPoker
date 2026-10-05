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

        /// <summary>Charges of the ability at each demon's table (full at a table never sat at and at every summons to Lucifer;
        /// spent ones stay spent when the player comes back — see <see cref="Game.TableCharges"/>).</summary>
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
    /// and it carries the charges from table to table — kept per demon for a per-table ability (<see cref="Game.TableCharges"/>:
    /// changing seats does not refill it), kept for the run for a per-run one.
    /// </summary>
    public sealed class Sinner : ICheatGuard
    {
        public SinnerClass Class { get; }

        private readonly Game.TableCharges _tables;
        private int _runCharges;

        /// <summary>What is left of the ability (at this table, or this run).</summary>
        public int Charges => IsPerTable ? _tables.Left : _runCharges;

        /// <summary>How many cheats this sinner has warded off (the whole run).</summary>
        public int WardsUsed { get; private set; }

        /// <param name="charges">What was left at the table the run was saved at (an older save's only number).</param>
        /// <param name="tables">The per-demon counts of a per-table ability, as <see cref="Game.TableCharges.Encode"/> wrote them.</param>
        public Sinner(SinnerClass sinnerClass, int? charges = null, string tables = null)
        {
            Class = sinnerClass ?? throw new ArgumentNullException(nameof(sinnerClass));
            int full = sinnerClass.FullCharges;
            _tables = new Game.TableCharges(() => Class.ChargesPerTable);
            if (IsPerTable)
                _tables.Restore(tables, charges.HasValue ? Math.Max(0, Math.Min(full, charges.Value)) : -1);
            else
                _runCharges = Math.Max(0, Math.Min(full, charges ?? full));
        }

        private bool IsPerTable => Class.ChargesPerTable > 0;

        public string Id => Class.Id;

        public SinnerAbility Ability => Class.Ability;

        /// <summary>The per-demon counts for the save (empty for a per-run ability).</summary>
        public string TableChargesCode => IsPerTable ? _tables.Encode() : "";

        /// <summary>
        /// The player sits at <paramref name="dealerId"/>'s table: a per-table ability has what was left there (full at a table
        /// never sat at; <paramref name="fresh"/> — Lucifer's summons — full again). A per-run one stays as it is.
        /// </summary>
        public void SitAt(string dealerId, bool fresh = false) => _tables.SitAt(dealerId, fresh);

        /// <summary>Spends one charge of <paramref name="ability"/>; false when none is left (or the class has another ability).</summary>
        public bool TrySpend(SinnerAbility ability)
        {
            if (ability == SinnerAbility.None || Class.Ability != ability || Charges <= 0) return false;
            if (IsPerTable) _tables.TrySpend();
            else _runCharges--;
            if (ability == SinnerAbility.Ward) WardsUsed++;
            return true;
        }

        public bool CanUse(SinnerAbility ability) => ability != SinnerAbility.None && Class.Ability == ability && Charges > 0;

        public bool Allows(ICheat cheat, CheatTable table) => Class.Allows(cheat, table, this);
    }
}
