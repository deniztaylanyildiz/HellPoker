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

        /// <summary>The King: against a cheat announced, the crown guards the whole hand — no cheat may touch the player's cards this hand.</summary>
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

        /// <summary>Jokers in the deck at the start of a run (the Jester's); 0 for a class that plays the plain 52.</summary>
        public virtual int StartingJokers => 0;

        /// <summary>Above this many jokers a lost hand takes one out of the deck again (the Jester's); never below the start.</summary>
        public virtual int JokerLossLine => int.MaxValue;

        /// <summary>At this many jokers (reached by a won hand) the jokers go, the count starts again, and the run earns the Jester's
        /// Rattle (once); 0: never.</summary>
        public virtual int JokerJackpot => 0;

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

        /// <summary>The King's crown guards this hand: a cheat that would touch the player's cards is refused. Lasts until the hand ends.</summary>
        public bool HandProtected { get; private set; }

        /// <summary>How many cheats the crown refused (the whole run).</summary>
        public int CrownBlocks { get; private set; }

        /// <summary>A hand ended: the crown's guard goes with it.</summary>
        public void EndHand() => HandProtected = false;

        /// <summary>Switches the power on (a full gauge, a power that waits for a move: the Peasant's free fold).</summary>
        public bool Arm()
        {
            if (!IsCharged || Class.Ability != SinnerAbility.FreeFold) return false;
            PowerArmed = true;
            return true;
        }

        /// <summary>Switches the power off again; the gauge stays as it is.</summary>
        public void Disarm() => PowerArmed = false;

        /// <summary>
        /// The jokers in the run's deck (the Jester's; 0 for every other class). A won hand adds one from the next hand on; above
        /// <see cref="SinnerClass.JokerLossLine"/> a lost hand takes one away; never fewer than the class starts with. A fold or a
        /// tie changes nothing.
        /// </summary>
        public int Jokers { get; private set; }

        /// <summary>The last settled hand brought the jokers to <see cref="SinnerClass.JokerJackpot"/>: they went, the count is the start again.</summary>
        public bool HitJokerJackpot { get; private set; }

        /// <summary>How many times the run's deck reached the jackpot.</summary>
        public int JokerJackpots { get; private set; }

        /// <summary>How many times the power was used, and how many cheats were warded off (the whole run).</summary>
        public int PowersUsed { get; private set; }
        public int WardsUsed { get; private set; }

        /// <param name="charge">The gauge (a saved run's); clamped to 0..full.</param>
        /// <param name="wardRaised">A ward raised and still waiting (a saved run's).</param>
        /// <param name="jokers">The deck's jokers (a saved run's); fewer than the class starts with (or none given) is the start.</param>
        public Sinner(SinnerClass sinnerClass, int charge = 0, ChargeRules rules = null, bool wardRaised = false, int jokers = 0)
        {
            Class = sinnerClass ?? throw new ArgumentNullException(nameof(sinnerClass));
            Jokers = Class.StartingJokers > 0 ? Math.Max(Class.StartingJokers, jokers) : 0;
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

            HitJokerJackpot = false;
            if (Class.StartingJokers <= 0 || folded) return;
            if (outcome == Game.ShowdownOutcome.PlayerWins)
            {
                Jokers++;
                if (Class.JokerJackpot > 0 && Jokers >= Class.JokerJackpot)
                {
                    // Twenty jokers: the deck is cleared of them, the count starts again (the table hands out the Rattle).
                    Jokers = Class.StartingJokers;
                    HitJokerJackpot = true;
                    JokerJackpots++;
                }
            }
            else if (outcome == Game.ShowdownOutcome.HouseWins && Jokers > Class.JokerLossLine) Jokers = Math.Max(Class.StartingJokers, Jokers - 1);
        }

        /// <summary>A joker put into or taken out of the Jester's deck (a floor's black market); never below the start. False when nothing changed.</summary>
        public bool ChangeJokers(int delta)
        {
            if (Class.StartingJokers <= 0) return false;
            int jokers = Math.Max(Class.StartingJokers, Jokers + delta);
            if (jokers == Jokers) return false;
            Jokers = jokers;
            return true;
        }

        /// <summary>Spends the full gauge on <paramref name="ability"/>; false when it is not full (or the class has another power).</summary>
        public bool TryUse(SinnerAbility ability)
        {
            if (ability == SinnerAbility.None || Class.Ability != ability || !IsCharged) return false;
            Charge = 0;
            PowerArmed = false;
            PowersUsed++;
            if (ability == SinnerAbility.Ward) WardRaised = true;
            if (ability == SinnerAbility.Protect) HandProtected = true;
            return true;
        }

        /// <summary>The ward refuses the next minor cheat that would really strike (one that would come to nothing keeps it up).</summary>
        public bool Allows(ICheat cheat, CheatTable table)
        {
            // The crown: every cheat that would touch the player's cards this hand is refused (one on the demon's side gets through).
            if (HandProtected && cheat != null && CheatRules.TouchesPlayerCards(cheat.Id) && cheat.CanApply(table))
            {
                CrownBlocks++;
                return false;
            }
            if (!WardRaised || cheat == null || cheat.Tier != CheatTier.Minor || !cheat.CanApply(table)) return true;
            WardRaised = false;
            WardsUsed++;
            return false;
        }
    }
}
