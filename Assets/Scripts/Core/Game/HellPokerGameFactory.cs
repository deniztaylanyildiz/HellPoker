using System;
using HellPoker.Core.Cards;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Randomness;

namespace HellPoker.Core.Game
{
    /// <summary>Wires the standard Hell Poker implementations together.</summary>
    public static class HellPokerGameFactory
    {
        /// <param name="rules">Defaults to <see cref="GameRules.Default"/>.</param>
        /// <param name="payouts">Defaults to <see cref="PayoutTable.CreateDefault"/>.</param>
        /// <param name="seed">Fixed seed for reproducible shuffles; null for a random game.</param>
        public static HellPokerGame Create(GameRules rules = null, IPayoutTable payouts = null, int? seed = null)
        {
            rules = rules ?? GameRules.Default;
            IRandomSource random = seed.HasValue ? new SystemRandomSource(seed.Value) : new SystemRandomSource();

            return new HellPokerGame(
                rules,
                new Deck(new FisherYatesShuffler(random)),
                HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)),
                new HouseDrawStrategy(rules.MaxDiscards),
                payouts ?? PayoutTable.CreateDefault());
        }

        /// <summary>A run at <paramref name="table"/>'s stakes, dealt by <paramref name="dealer"/> under their house rules.</summary>
        public static HellPokerGame Create(GameRules table, Dealer dealer, int? seed = null)
        {
            if (dealer == null) throw new ArgumentNullException(nameof(dealer));

            return Create(dealer.ApplyTo(table ?? GameRules.Default), dealer.Payouts, seed);
        }
    }
}
