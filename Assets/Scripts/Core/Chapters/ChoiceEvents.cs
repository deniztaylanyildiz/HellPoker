using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;

namespace HellPoker.Core.Chapters
{
    /// <summary>What kind of stranger: one who needs help, a swindler (a trap, with a hint in the words), or Hell's own weather.</summary>
    public enum EventFamily
    {
        Help,
        Scam,
        Hell
    }

    /// <summary>What a choice of an offer did, for its result panel: the purse's and the sentence's change, what was given, and a word of
    /// how it went (<see cref="Result"/>: the outcome's key, its words the presentation's).</summary>
    public sealed class EventOutcome
    {
        public string EventId { get; set; }
        public string Choice { get; set; }
        public string Result { get; set; }

        /// <summary>The purse's change.</summary>
        public int Coins { get; set; }

        /// <summary>The sentence's change: positive on it, negative off it.</summary>
        public int Years { get; set; }

        public string Relic { get; set; }
        public string Upgrade { get; set; }

        /// <summary>A second choice follows (the sin swap's offers).</summary>
        public bool Waits { get; set; }

        /// <summary>A side table follows (the pickpocket's chase).</summary>
        public bool OpensTable { get; set; }

        /// <summary>A number for the words (the seer's bar, the coins at stake).</summary>
        public int Value { get; set; }
    }

    /// <summary>
    /// Demo 2.1's offers: two or three choices, a roll of the dice behind some of them, and a result shown on its own panel. A swindler's
    /// words carry a small hint; the Warlock's prophecy (and the Liar's Tongue) reads it plainly, the Summoner's eye once for coins. Coins
    /// and years grow with the chapter (<see cref="ChapterRun.Scaled"/>); years go on (or off) the sentence at once and on the demon's bar at
    /// the gate. Each comes once a run, in any chapter.
    /// </summary>
    public interface IChoiceEvent : IFloorEvent
    {
        EventFamily Family { get; }

        /// <summary>It happens to the player (no passing): <see cref="Appear"/> as it is drawn.</summary>
        bool IsEncounter { get; }

        /// <summary>An encounter's own blow as it appears (the pickpocket's hand).</summary>
        void Appear(ChapterRun run);

        /// <summary>The choices, in order (an offer's last is "pass").</summary>
        IReadOnlyList<string> Choices(ChapterRun run);

        bool CanChoose(ChapterRun run, string choice);

        /// <summary>The coins a choice asks for (shown on its button); 0 for none.</summary>
        int Cost(ChapterRun run, string choice);

        EventOutcome Choose(ChapterRun run, string choice);

        /// <summary>What a choice is worth in coins on average (years at <see cref="ChapterRules.YearsPerMissingCoin"/> a coin), as a player
        /// who <paramref name="seesTrap"/> — or one who believes the swindler — reckons it (the simulation's player).</summary>
        double Worth(ChapterRun run, string choice, bool seesTrap);
    }

    public static class EventChoices
    {
        public const string Pass = "pass";

        /// <summary>The sin swap: the relic given ("give:id"), then the one taken ("take:id").</summary>
        public const string GivePrefix = "give:", TakePrefix = "take:";
    }

    /// <summary>The common ground of Demo 2.1's offers.</summary>
    public abstract class ChoiceEvent : IChoiceEvent
    {
        public abstract string Id { get; }
        public abstract EventFamily Family { get; }
        public virtual bool IsEncounter => false;
        public virtual bool CanAppear(ChapterRun run) => true;
        public virtual void Appear(ChapterRun run) { }
        public abstract IReadOnlyList<string> Choices(ChapterRun run);
        public virtual int Cost(ChapterRun run, string choice) => 0;

        public virtual bool CanChoose(ChapterRun run, string choice)
        {
            if (!Choices(run).Contains(choice)) return false;
            int cost = Cost(run, choice);
            return cost == 0 || run.Purse.Coins > cost;   // never the last coin
        }

        public EventOutcome Choose(ChapterRun run, string choice)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            if (!CanChoose(run, choice)) throw new InvalidOperationException($"{Id}: '{choice}' cannot be chosen.");
            int coins = run.Purse.Coins;
            int cost = Cost(run, choice);
            if (cost > 0) run.Purse.Add(-cost);
            var outcome = new EventOutcome { EventId = Id, Choice = choice, Result = choice };
            if (choice != EventChoices.Pass)
            {
                Resolve(run, choice, outcome);
                // The Lost Soul's Letter: every offer taken is years on the sentence.
                int letter = run.Effects.CombinedRelics.Extra(RelicExtras.EventYears);
                if (letter > 0 && !choice.StartsWith(EventChoices.TakePrefix, StringComparison.Ordinal)) YearsOn(run, letter, outcome);
            }
            else if (Family == EventFamily.Scam && run.Kit.Has(UpgradeIds.EyeOfTruth))
            {
                // The Prophecy Seal: the Oracle walks away from a swindler, and is paid for seeing it.
                run.Purse.Add(ClassKit.SealCoins);
                run.Kit.SealCoinsEarned += ClassKit.SealCoins;
                outcome.Result = "sealed";
            }
            outcome.Coins = run.Purse.Coins - coins;
            return outcome;
        }

        protected abstract void Resolve(ChapterRun run, string choice, EventOutcome outcome);

        public abstract double Worth(ChapterRun run, string choice, bool seesTrap);

        /// <summary>The player takes the first choice (the old offers' way: for tests and an offer accepted blind).</summary>
        public void Accept(ChapterRun run)
        {
            string first = Choices(run).FirstOrDefault(c => CanChoose(run, c));
            if (first != null) Choose(run, first);
        }

        protected static string[] Of(params string[] choices) => choices;

        /// <summary>Years on the sentence now, on the demon's bar at the gate.</summary>
        protected static void YearsOn(ChapterRun run, int years, EventOutcome outcome)
        {
            run.OweAtGate(years);
            outcome.Years += years;
        }

        /// <summary>Years off the sentence and this demon's share now (never the last year).</summary>
        protected static void YearsOff(ChapterRun run, int years, EventOutcome outcome)
        {
            int before = run.Years;
            run.StrikeYears(years);
            outcome.Years -= before - run.Years;
        }

        protected static double Coins(ChapterRun run, int years) => years / (double)Math.Max(1, run.Rules.YearsPerMissingCoin);

        /// <summary>A relic's worth on average, in coins (about its black market price), and an upgrade's.</summary>
        protected const double CommonWorth = 25, RareWorth = 35, LegendaryWorth = 45, UpgradeWorth = 30;
    }

    public static class ChoiceEventIds
    {
        public const string ChainedSoul = "chained_soul", FallenAngel = "fallen_angel", HungryGambler = "hungry_gambler", DebtorImp = "debtor_imp",
            LostSoul = "lost_soul", Accountant = "mammons_accountant";

        public const string ThreeCups = "three_cups", FakePardon = "fake_pardon", SoulInsurance = "soul_insurance", Pickpocket = "pickpocket";

        public const string LavaFerryman = "lava_ferryman", HellLottery = "hell_lottery", Confession = "confession_cell", FireRain = "fire_rain",
            SinSwap = "sin_swap", BlindSeer = "blind_seer";
    }

    // ================================================================== help

    /// <summary>The Chained Soul: "Break my chain." Coins: grateful six times in ten (a common relic or years off), else an imp in disguise.</summary>
    public sealed class ChainedSoul : ChoiceEvent
    {
        public const int Price = 20, Years = 100, GratefulPercent = 60;
        public override string Id => ChoiceEventIds.ChainedSoul;
        public override EventFamily Family => EventFamily.Help;
        public override IReadOnlyList<string> Choices(ChapterRun run) => Of("pay", EventChoices.Pass);
        public override int Cost(ChapterRun run, string choice) => choice == "pay" ? run.Scaled(Price) : 0;

        protected override void Resolve(ChapterRun run, string choice, EventOutcome outcome)
        {
            if (run.NodeDice.Next(100) >= run.HelpPercent(GratefulPercent))
            {
                outcome.Result = "imp";
                return;
            }
            string relic = run.NodeDice.Next(2) == 0 ? run.DrawRelic(RelicRarity.Common) : null;
            if (relic != null && run.GiveRelic(relic))
            {
                outcome.Result = "relic";
                outcome.Relic = relic;
                return;
            }
            outcome.Result = "years";
            YearsOff(run, run.Scaled(Years), outcome);
        }

        public override double Worth(ChapterRun run, string choice, bool seesTrap) =>
            choice != "pay" ? 0 : run.HelpPercent(GratefulPercent) / 100.0 * (CommonWorth + Coins(run, run.Scaled(Years))) / 2 - Cost(run, choice);
    }

    /// <summary>The Fallen Angel, a wing broken: years on the sentence for a rare relic or a class upgrade (the player's pick).</summary>
    public sealed class FallenAngel : ChoiceEvent
    {
        public const int Years = 100;   // 150 at first (75: taken every time, no choice left): a help that worked as a trap (freed −13 when taken)
        public override string Id => ChoiceEventIds.FallenAngel;
        public override EventFamily Family => EventFamily.Help;
        public override IReadOnlyList<string> Choices(ChapterRun run) => Of("relic", "upgrade", EventChoices.Pass);

        public override bool CanChoose(ChapterRun run, string choice) =>
            base.CanChoose(run, choice) && (choice != "upgrade" || run.Kit.UpgradesLeft.Any()) && (choice != "relic" || run.CanDrawRelic(RelicRarity.Rare));

        protected override void Resolve(ChapterRun run, string choice, EventOutcome outcome)
        {
            YearsOn(run, run.Scaled(Years), outcome);
            if (choice == "relic")
            {
                outcome.Relic = run.DrawRelic(RelicRarity.Rare);
                run.GiveRelic(outcome.Relic);
            }
            else outcome.Upgrade = run.GiveUpgrade();
        }

        public override double Worth(ChapterRun run, string choice, bool seesTrap) =>
            choice == "relic" ? RareWorth - Coins(run, run.Scaled(Years)) : choice == "upgrade" ? UpgradeWorth - Coins(run, run.Scaled(Years)) : 0;
    }

    /// <summary>The Hungry Gambler: coins for a tip — at the next floor table the imp's first hand plays two cards face up.</summary>
    public sealed class HungryGambler : ChoiceEvent
    {
        public const int Price = 15, OpenCards = 2;
        public override string Id => ChoiceEventIds.HungryGambler;
        public override EventFamily Family => EventFamily.Help;
        public override IReadOnlyList<string> Choices(ChapterRun run) => Of("pay", EventChoices.Pass);
        public override int Cost(ChapterRun run, string choice) => choice == "pay" ? run.Scaled(Price) : 0;
        protected override void Resolve(ChapterRun run, string choice, EventOutcome outcome) => run.AddTableMarks(new TableMarks(firstHandOpen: OpenCards));
        public override double Worth(ChapterRun run, string choice, bool seesTrap) => choice == "pay" ? 8 - Cost(run, choice) : 0;
    }

    /// <summary>The Debtor Imp: lend him coins; seven times in ten he pays back double at the gate, else he is gone.</summary>
    public sealed class DebtorImp : ChoiceEvent
    {
        public const int Loan = 30, Back = 60, PaysPercent = 70;
        public override string Id => ChoiceEventIds.DebtorImp;
        public override EventFamily Family => EventFamily.Help;
        public override IReadOnlyList<string> Choices(ChapterRun run) => Of("lend", EventChoices.Pass);
        public override int Cost(ChapterRun run, string choice) => choice == "lend" ? run.Scaled(Loan) : 0;

        protected override void Resolve(ChapterRun run, string choice, EventOutcome outcome)
        {
            run.LoanDue += run.Scaled(Back) * run.HelpPercent(100) / 100;
            run.LoanPays = run.NodeDice.Next(100) < PaysPercent;
            outcome.Value = run.LoanDue;
        }

        public override double Worth(ChapterRun run, string choice, bool seesTrap) =>
            choice == "lend" ? PaysPercent / 100.0 * run.Scaled(Back) - Cost(run, choice) : 0;
    }

    /// <summary>The Lost Soul: show it the way — the unknown nodes of the next two floors show what they hold; but the next table's ante
    /// is one more.</summary>
    public sealed class LostSoul : ChoiceEvent
    {
        public const int Floors = 2;
        public override string Id => ChoiceEventIds.LostSoul;
        public override EventFamily Family => EventFamily.Help;
        public override bool CanAppear(ChapterRun run) => run.Current != null && run.Current.Floor + 1 < run.Map.FloorCount - 1;
        public override IReadOnlyList<string> Choices(ChapterRun run) => Of("guide", EventChoices.Pass);

        protected override void Resolve(ChapterRun run, string choice, EventOutcome outcome)
        {
            int from = (run.Current?.Floor ?? -1) + 1;
            int shown = 0;
            for (int f = from; f < from + Floors && f < run.Map.FloorCount; f++)
                foreach (MapNode node in run.Map.Floors[f].Where(n => n.Kind == NodeKind.Unknown))
                {
                    run.Reveal(node);
                    shown++;
                }
            outcome.Value = shown;
            run.AddTableMarks(new TableMarks(anteDelta: 1));
        }

        public override double Worth(ChapterRun run, string choice, bool seesTrap) => choice == "guide" ? -2 : 0;
    }

    /// <summary>Mammon's Accountant: put his ledger right (the next three tables' antes one less) or report him (coins now, and the next
    /// warden's purse half again).</summary>
    public sealed class MammonsAccountant : ChoiceEvent
    {
        public const int Tables = 3, Reward = 40, WardenPercent = 150;
        public override string Id => ChoiceEventIds.Accountant;
        public override EventFamily Family => EventFamily.Help;
        public override IReadOnlyList<string> Choices(ChapterRun run) => Of("fix", "report", EventChoices.Pass);

        protected override void Resolve(ChapterRun run, string choice, EventOutcome outcome)
        {
            if (choice == "fix") run.AnteCutTables += Tables;
            else
            {
                run.Purse.Add(run.Scaled(Reward) * run.HelpPercent(100) / 100);
                run.NextWardenPursePercent = WardenPercent;
            }
        }

        public override double Worth(ChapterRun run, string choice, bool seesTrap) =>
            choice == "fix" ? 9 : choice == "report" ? run.Scaled(Reward) - 15 : 0;
    }

    // ================================================================== swindlers

    /// <summary>Three Cups: a stake on finding the pea, paid double. It looks like one in three; it is one in five (the Warlock always
    /// finds it).</summary>
    public sealed class ThreeCups : ChoiceEvent
    {
        public static readonly int[] Stakes = { 10, 20, 40 };
        public const int TruePercent = 20, ShownPercent = 33;
        public override string Id => ChoiceEventIds.ThreeCups;
        public override EventFamily Family => EventFamily.Scam;
        public override IReadOnlyList<string> Choices(ChapterRun run) => Of("bet0", "bet1", "bet2", EventChoices.Pass);

        public override int Cost(ChapterRun run, string choice) =>
            choice != null && choice.StartsWith("bet", StringComparison.Ordinal) ? run.Scaled(Stakes[choice[3] - '0']) : 0;

        protected override void Resolve(ChapterRun run, string choice, EventOutcome outcome)
        {
            int stake = Cost(run, choice);
            outcome.Value = stake;
            bool found = run.Sinner.Id == Warlock.ClassId || run.NodeDice.Next(100) < TruePercent;
            outcome.Result = found ? "found" : "lost";
            if (found) run.Purse.Add(stake * 2);
        }

        public override double Worth(ChapterRun run, string choice, bool seesTrap)
        {
            if (choice == EventChoices.Pass) return 0;
            int stake = Cost(run, choice);
            double chance = run.Sinner.Id == Warlock.ClassId ? 1.0 : (seesTrap ? TruePercent : ShownPercent) / 100.0;
            return chance * stake * 2 - stake;
        }
    }

    /// <summary>The Forged Pardon: coins for "five hundred years off". Half of them are forged (nothing); the other half are real but
    /// strike two hundred.</summary>
    public sealed class FakePardon : ChoiceEvent
    {
        public const int Price = 50, Promised = 500, Real = 200, RealPercent = 50;
        public override string Id => ChoiceEventIds.FakePardon;
        public override EventFamily Family => EventFamily.Scam;
        public override IReadOnlyList<string> Choices(ChapterRun run) => Of("buy", EventChoices.Pass);
        public override int Cost(ChapterRun run, string choice) => choice == "buy" ? run.Scaled(Price) : 0;

        protected override void Resolve(ChapterRun run, string choice, EventOutcome outcome)
        {
            if (run.NodeDice.Next(100) >= RealPercent)
            {
                outcome.Result = "forged";
                return;
            }
            outcome.Result = "real";
            YearsOff(run, run.Scaled(Real), outcome);
        }

        public override double Worth(ChapterRun run, string choice, bool seesTrap) =>
            choice != "buy" ? 0 : (seesTrap ? RealPercent / 100.0 * Coins(run, run.Scaled(Real)) : Coins(run, run.Scaled(Promised))) - Cost(run, choice);
    }

    /// <summary>The Soul Insurance: coins for a policy that "pays if you lose at the demon's table". The insurer is gone by the gate —
    /// always (the hint: the signature is written in ash).</summary>
    public sealed class SoulInsurance : ChoiceEvent
    {
        public const int Price = 25;
        public override string Id => ChoiceEventIds.SoulInsurance;
        public override EventFamily Family => EventFamily.Scam;
        public override IReadOnlyList<string> Choices(ChapterRun run) => Of("buy", EventChoices.Pass);
        public override int Cost(ChapterRun run, string choice) => choice == "buy" ? run.Scaled(Price) : 0;
        protected override void Resolve(ChapterRun run, string choice, EventOutcome outcome) => run.Insured = true;
        public override double Worth(ChapterRun run, string choice, bool seesTrap) => choice != "buy" ? 0 : (seesTrap ? 0 : 35) - Cost(run, choice);
    }

    /// <summary>The Pickpocket (an encounter): a tenth of the purse gone as he appears. Run after him — three hands against him; come out
    /// ahead and he gives it back, and more — or let him go.</summary>
    public sealed class Pickpocket : ChoiceEvent
    {
        public const int Percent = 10;
        public override string Id => ChoiceEventIds.Pickpocket;
        public override EventFamily Family => EventFamily.Scam;
        public override bool IsEncounter => true;
        public override bool CanAppear(ChapterRun run) => run.Purse.Coins >= 10;

        public override void Appear(ChapterRun run)
        {
            int stolen = Math.Max(1, Math.Min(run.Purse.Coins * Percent / 100, run.Purse.Coins - 1));
            run.Purse.Add(-stolen);
            run.EventMemo = stolen;
        }

        public override IReadOnlyList<string> Choices(ChapterRun run) => Of("chase", "let");

        protected override void Resolve(ChapterRun run, string choice, EventOutcome outcome)
        {
            outcome.Value = run.EventMemo;
            if (choice != "chase")
            {
                run.EventMemo = 0;
                return;
            }
            run.OpenChase(run.EventMemo);
            outcome.OpensTable = true;
        }

        public override double Worth(ChapterRun run, string choice, bool seesTrap) =>
            choice == "chase" ? 0.45 * (run.EventMemo + run.Scaled(ChapterRun.ChaseBonus)) - 4 : 0;
    }

    // ================================================================== Hell's own

    /// <summary>The Lava Ferryman: coins to be carried over a floor — or swim it (years on the sentence).</summary>
    public sealed class LavaFerryman : ChoiceEvent
    {
        public const int Price = 15, SwimYears = 100;
        public override string Id => ChoiceEventIds.LavaFerryman;
        public override EventFamily Family => EventFamily.Hell;
        public override bool CanAppear(ChapterRun run) => run.Current != null && run.Current.Floor + 2 <= run.Map.FloorCount - 1;
        public override IReadOnlyList<string> Choices(ChapterRun run) => Of("ferry", "swim", EventChoices.Pass);
        public override int Cost(ChapterRun run, string choice) => choice == "ferry" ? run.Scaled(Price) : 0;

        protected override void Resolve(ChapterRun run, string choice, EventOutcome outcome)
        {
            if (choice == "swim") YearsOn(run, run.Scaled(SwimYears), outcome);
            run.SkipNext = true;
        }

        public override double Worth(ChapterRun run, string choice, bool seesTrap) =>
            choice == "ferry" ? 3 - Cost(run, choice) : choice == "swim" ? 3 - Coins(run, run.Scaled(SwimYears)) : 0;
    }

    /// <summary>Hell's Lottery: a ticket for coins — seven in ten nothing, a quarter coins, one in twenty a legendary relic.</summary>
    public sealed class HellLottery : ChoiceEvent
    {
        public const int Price = 10, Prize = 40, PrizePercent = 25, RelicPercent = 5;
        public override string Id => ChoiceEventIds.HellLottery;
        public override EventFamily Family => EventFamily.Hell;
        public override IReadOnlyList<string> Choices(ChapterRun run) => Of("ticket", EventChoices.Pass);
        public override int Cost(ChapterRun run, string choice) => choice == "ticket" ? run.Scaled(Price) : 0;

        protected override void Resolve(ChapterRun run, string choice, EventOutcome outcome)
        {
            int roll = run.NodeDice.Next(100);
            if (roll < RelicPercent)
            {
                string relic = run.DrawRelic(RelicRarity.Legendary);
                if (relic != null && run.GiveRelic(relic))
                {
                    outcome.Result = "relic";
                    outcome.Relic = relic;
                    return;
                }
                roll = RelicPercent;   // none left: the coins
            }
            if (roll < RelicPercent + PrizePercent)
            {
                outcome.Result = "coins";
                run.Purse.Add(run.Scaled(Prize));
                return;
            }
            outcome.Result = "blank";
        }

        public override double Worth(ChapterRun run, string choice, bool seesTrap) =>
            choice == "ticket" ? PrizePercent / 100.0 * run.Scaled(Prize) + RelicPercent / 100.0 * LegendaryWorth - Cost(run, choice) : 0;
    }

    /// <summary>The Confession Cell: confess — years off the sentence, but this chapter's demon sits down with his malice full.</summary>
    public sealed class ConfessionCell : ChoiceEvent
    {
        public const int Years = 150;
        public override string Id => ChoiceEventIds.Confession;
        public override EventFamily Family => EventFamily.Hell;
        public override bool CanAppear(ChapterRun run) => !run.BossMaliceFull;
        public override IReadOnlyList<string> Choices(ChapterRun run) => Of("confess", EventChoices.Pass);

        protected override void Resolve(ChapterRun run, string choice, EventOutcome outcome)
        {
            YearsOff(run, run.Scaled(Years), outcome);
            run.BossMaliceFull = true;
        }

        public override double Worth(ChapterRun run, string choice, bool seesTrap) => choice == "confess" ? Coins(run, run.Scaled(Years)) - 15 : 0;
    }

    /// <summary>The Rain of Fire (an encounter): lose coins, or take years on the sentence.</summary>
    public sealed class FireRain : ChoiceEvent
    {
        public const int Price = 10, Years = 50;
        public override string Id => ChoiceEventIds.FireRain;
        public override EventFamily Family => EventFamily.Hell;
        public override bool IsEncounter => true;
        public override IReadOnlyList<string> Choices(ChapterRun run) => Of("coins", "years");
        public override int Cost(ChapterRun run, string choice) => choice == "coins" ? run.Scaled(Price) : 0;

        protected override void Resolve(ChapterRun run, string choice, EventOutcome outcome)
        {
            if (choice == "years") YearsOn(run, run.Scaled(Years), outcome);
        }

        public override double Worth(ChapterRun run, string choice, bool seesTrap) =>
            choice == "coins" ? -Cost(run, choice) : -Coins(run, run.Scaled(Years));
    }

    /// <summary>The Sin Swap: give a relic, take one of two drawn for it (or pass).</summary>
    public sealed class SinSwap : ChoiceEvent
    {
        public const int Offered = 2;
        public override string Id => ChoiceEventIds.SinSwap;
        public override EventFamily Family => EventFamily.Hell;

        public override bool CanAppear(ChapterRun run) => Givable(run).Any() && run.CanDrawRelic(null, Offered);

        private static IEnumerable<string> Givable(ChapterRun run) => run.Effects.AllHeld.Where(r => !RelicRoster.IsReward(r));

        public override IReadOnlyList<string> Choices(ChapterRun run) =>
            run.EventOffers.Count > 0
                ? run.EventOffers.Select(id => EventChoices.TakePrefix + id).ToArray()
                : Givable(run).Select(id => EventChoices.GivePrefix + id).Concat(new[] { EventChoices.Pass }).ToArray();

        protected override void Resolve(ChapterRun run, string choice, EventOutcome outcome)
        {
            if (choice.StartsWith(EventChoices.GivePrefix, StringComparison.Ordinal))
            {
                string given = choice.Substring(EventChoices.GivePrefix.Length);
                var offers = ChapterRelics.Draw(run.NodeDice, RelicWeights.Market, Offered, run.Effects.AllHeld.Concat(new[] { given }).ToList(), run.RelicAvailable, run.Sinner.Id);
                run.EventOffers.Clear();
                outcome.Relic = given;
                // Nothing to take for it (the pool ran dry): the relic stays — a relic leaves only for what the player chose.
                if (offers.Count == 0)
                {
                    outcome.Result = "none";
                    return;
                }
                run.Effects.Drop(given);
                run.EventOffers.AddRange(offers);
                outcome.Result = "give";
                outcome.Waits = true;
                return;
            }
            string taken = choice.Substring(EventChoices.TakePrefix.Length);
            run.EventOffers.Clear();
            run.GiveRelic(taken);
            outcome.Result = "take";
            outcome.Relic = taken;
        }

        public override double Worth(ChapterRun run, string choice, bool seesTrap)
        {
            if (choice.StartsWith(EventChoices.GivePrefix, StringComparison.Ordinal))
                return 5 - (RelicRoster.Find(choice.Substring(EventChoices.GivePrefix.Length))?.ExpectedYears ?? 0) / 5.0;
            if (choice.StartsWith(EventChoices.TakePrefix, StringComparison.Ordinal))
                return (RelicRoster.Find(choice.Substring(EventChoices.TakePrefix.Length))?.ExpectedYears ?? 0) / 5.0;
            return 0;
        }
    }

    /// <summary>The Blind Seer: a few coins for the chapter's demon — his rule, and roughly how long his bar will be.</summary>
    public sealed class BlindSeer : ChoiceEvent
    {
        public const int Price = 5;
        public override string Id => ChoiceEventIds.BlindSeer;
        public override EventFamily Family => EventFamily.Hell;
        public override IReadOnlyList<string> Choices(ChapterRun run) => Of("ask", EventChoices.Pass);
        public override int Cost(ChapterRun run, string choice) => choice == "ask" ? run.Scaled(Price) : 0;

        protected override void Resolve(ChapterRun run, string choice, EventOutcome outcome)
        {
            int share = Math.Max(1, BossShares.For(run.Sinner.Id, run.Rules.BossId));
            outcome.Value = (int)((long)run.BarAfterGate(run.Purse.Coins) * 100 / share);
            outcome.Result = outcome.Value >= LongPercent ? "long" : outcome.Value <= ShortPercent ? "short" : "medium";
        }

        /// <summary>The bar against the class's share: this long or more "long", this short or less "short".</summary>
        public const int LongPercent = 115, ShortPercent = 95;

        public override double Worth(ChapterRun run, string choice, bool seesTrap) => choice == "ask" ? -Cost(run, choice) : 0;
    }

    /// <summary>Demo 2.1's sixteen offers, in every chapter, each once a run.</summary>
    public static class ChoiceEventDeck
    {
        public static IReadOnlyList<IChoiceEvent> All => new IChoiceEvent[]
        {
            new ChainedSoul(), new FallenAngel(), new HungryGambler(), new DebtorImp(), new LostSoul(), new MammonsAccountant(),
            new ThreeCups(), new FakePardon(), new SoulInsurance(), new Pickpocket(),
            new LavaFerryman(), new HellLottery(), new ConfessionCell(), new FireRain(), new SinSwap(), new BlindSeer()
        };

        /// <summary>The player sees a swindler's trap plainly: the Warlock's prophecy, the Liar's Tongue.</summary>
        public static bool SeesTraps(ChapterRun run) =>
            run.Sinner.Id == Warlock.ClassId || run.Effects.CombinedRelics.Extra(RelicExtras.TrapHints) > 0;
    }
}
