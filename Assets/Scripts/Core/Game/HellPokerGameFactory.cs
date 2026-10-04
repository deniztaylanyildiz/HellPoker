using System;
using HellPoker.Core.Betting;
using HellPoker.Core.Cards;
using HellPoker.Core.Cheats;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Randomness;
using HellPoker.Core.Sinners;

namespace HellPoker.Core.Game
{
    /// <summary>Wires the standard Hell Poker implementations together.</summary>
    public static class HellPokerGameFactory
    {
        /// <summary>The dice streams derived from a game's master seed (<see cref="RandomSeeds.Derive"/>).</summary>
        public const int DeckStream = 0, HouseStream = 1, CheatStream = 2;

        /// <summary>The run's events roll on their own stream (a run-level one, not a table's), so nothing else shifts.</summary>
        public const int EventStream = 3;

        /// <param name="rules">Defaults to <see cref="GameRules.Default"/>.</param>
        /// <param name="payouts">Defaults to <see cref="PayoutTable.CreateDefault"/>.</param>
        /// <param name="seed">Fixed seed for reproducible shuffles; null for a random game.</param>
        /// <param name="betting">The house's temper; null for a house that never re-raises.</param>
        /// <param name="cheats">The demon's cheats; null for an honest table.</param>
        /// <param name="maliceMax">The demon's malice gauge (see <see cref="CheatSession"/>).</param>
        /// <param name="guard">Asked before each cheat strikes; null lets every cheat through (the player's abilities, later).</param>
        public static HellPokerGame Create(GameRules rules = null, IPayoutTable payouts = null, int? seed = null, HouseBettingStyle betting = null,
            ICheatPolicy cheats = null, int maliceMax = 0, ICheatGuard guard = null, int backfirePercent = 0, Sinner sinner = null)
        {
            rules = rules ?? GameRules.Default;
            // One master seed; separate streams derived from it for the deck, the house's temper and the demon's cheats,
            // so their dice never change the order of the cards (and never start from the same clock tick).
            int master = seed ?? RandomSeeds.Fresh();
            IRandomSource deckRandom = new SystemRandomSource(RandomSeeds.Derive(master, DeckStream));
            IRandomSource houseRandom = new SystemRandomSource(RandomSeeds.Derive(master, HouseStream));
            IRandomSource cheatRandom = new SystemRandomSource(RandomSeeds.Derive(master, CheatStream));

            return new HellPokerGame(
                rules,
                new Deck(new FisherYatesShuffler(deckRandom)),
                HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)),
                new HouseDrawStrategy(rules.MaxDiscards),
                payouts ?? PayoutTable.CreateDefault(),
                betting == null ? null : new HandStrengthBettingStrategy(betting, houseRandom),
                new CheatSession(cheats, maliceMax, cheatRandom, guard ?? sinner, backfirePercent),
                cheatRandom, sinner);
        }

        /// <summary>A run at <paramref name="table"/>'s stakes, dealt by <paramref name="dealer"/> under their house rules and cheats.</summary>
        /// <param name="sinner">The run's sinner class (its guard asks every cheat first); null for a classless game.</param>
        public static HellPokerGame Create(GameRules table, Dealer dealer, int? seed = null, ICheatGuard guard = null, Sinner sinner = null)
        {
            if (dealer == null) throw new ArgumentNullException(nameof(dealer));

            return Create(dealer.ApplyTo(table ?? GameRules.Default), dealer.Payouts, seed, dealer.Betting, dealer.Cheats, dealer.MaliceMax, guard,
                dealer.BackfirePercent, sinner);
        }
    }
}
