using System;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;

namespace HellPoker.Core.Chapters
{
    /// <summary>
    /// The class upgrades that act in a hand outside the class's own powers: the Jester's (he has no panel) — Controlled Chaos, the
    /// Double Laugh, the Buffoon. (The Warlock's Eye of Truth opens a card at the deal: <see cref="ChapterHooks.Attach"/>.) Its dice are
    /// asked only by an upgrade the run has, so a run without them plays as before.
    /// </summary>
    public sealed class UpgradeHooks : TableHooks
    {
        /// <summary>The Buffoon: with one joker in the Jester's hand the demon re-raises only this often (percent) of what he would.</summary>
        public const int BuffoonReRaisePercent = 50;

        private readonly ClassKit _kit;
        private readonly IRandomSource _random;
        private int _jokersAtDeal;
        private bool _laughed;

        public UpgradeHooks(ClassKit kit, IRandomSource random)
        {
            _kit = kit ?? throw new ArgumentNullException(nameof(kit));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>The Double Laugh threw a joker back this hand.</summary>
        public bool LaughedThisHand => _laughed;

        public override void HandDealt(HellPokerGame game)
        {
            _jokersAtDeal = _kit.Sinner.Jokers;
            _laughed = false;
        }

        /// <summary>The Double Laugh: a hand drawn to two jokers or more throws one back for the deck's next plain card (the joker goes under).</summary>
        public override void HandDrawn(HellPokerGame game, ExchangeResult player, ExchangeResult house)
        {
            if (!_kit.Has(UpgradeIds.DoubleLaugh) || JokerResolver.CountJokers(game.PlayerHand) < 2) return;
            var order = game.Deck.Remaining.Reverse().ToList();   // the top first
            int next = order.FindIndex(c => !c.IsJoker);
            if (next < 0) return;
            Card fresh = order[next];
            order.RemoveAt(next);
            int at = Enumerable.Range(0, Hand.Size).Last(i => game.PlayerHand[i].IsJoker);
            order.Add(game.PlayerHand[at]);
            game.Deck.Restore(order);
            game.SetHands(game.PlayerHand.With(at, fresh), game.HouseHand);
            _laughed = true;
        }

        /// <summary>The Buffoon: one joker in the Jester's hand makes the demon wary of raising back.</summary>
        public override bool HouseReRaises(HellPokerGame game, bool wants)
        {
            if (!wants || !_kit.Has(UpgradeIds.Buffoon) || JokerResolver.CountJokers(game.PlayerHand) != 1) return wants;
            return _random.Next(100) < BuffoonReRaisePercent;
        }

        /// <summary>Controlled Chaos: every lost hand takes a joker out of the deck (not only above the line), never below the start.</summary>
        public override void HandSettled(HellPokerGame game, RoundResult round)
        {
            if (!_kit.Has(UpgradeIds.ControlledChaos) || round == null || round.Folded) return;
            if (round.Showdown?.Outcome != ShowdownOutcome.HouseWins) return;
            if (_kit.Sinner.Jokers == _jokersAtDeal) _kit.Sinner.ChangeJokers(-1);   // the class's own rule did not take one already
        }
    }
}
