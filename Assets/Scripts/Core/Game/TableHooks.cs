using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Draw;

namespace HellPoker.Core.Game
{
    /// <summary>
    /// Phase 2's hooks into a hand (<see cref="HellPokerGame.UseHooks"/>): a sinner class's mechanics and a demon's own rule. Every
    /// method does nothing by default, and a demo table has no hooks at all — its hands play exactly as they always did.
    /// The game calls them at fixed moments: the deal, the moment the draw begins, the House's draw, the draw done, the House's
    /// re-raise, the settled hand. <see cref="HookChain"/> lets several speak at one table.
    /// </summary>
    public abstract class TableHooks
    {
        /// <summary>True when the class's demo power (the free fold, the ward, the crown) gives way to these mechanics.</summary>
        public virtual bool ReplacesClassPower => false;

        /// <summary>Cards that are out of the deck while they lie elsewhere (the Peasant's satchel): a shuffle never deals them.</summary>
        public virtual IEnumerable<Card> CardsOutOfDeck => Enumerable.Empty<Card>();

        /// <summary>The demon's gauge fills this much more at the deal (a cornered demon).</summary>
        public virtual int ExtraMalicePerHand(HellPokerGame game) => 0;

        /// <summary>A hand was dealt (the cards are on the table, before the deal's cheat).</summary>
        public virtual void HandDealt(HellPokerGame game) { }

        /// <summary>The draw begins (every card of the player's has turned): a demon's swap, a seizure.</summary>
        public virtual void DrawBegins(HellPokerGame game) { }

        /// <summary>The player may take <paramref name="card"/> in the draw from outside the deck (the satchel).</summary>
        public virtual bool CanTakeFromOutside(HellPokerGame game, Card card) => false;

        /// <summary>The player took these cards in the draw from outside the deck.</summary>
        public virtual void TookFromOutside(HellPokerGame game, IReadOnlyCollection<Card> cards) { }

        /// <summary>What the House throws back, after its own sense of it (a demon who knows more).</summary>
        public virtual IReadOnlyList<int> AdjustHouseDiscards(HellPokerGame game, IReadOnlyList<int> discards) => discards;

        /// <summary>Cards the House takes in its draw from outside the deck, by position (a demon's own satchel); null for none.</summary>
        public virtual IReadOnlyDictionary<int, Card> HouseTakesFromOutside(HellPokerGame game, IReadOnlyList<int> discards) => null;

        /// <summary>Both hands have drawn (the House's hand may still change here: a demon's temptation).</summary>
        public virtual void HandDrawn(HellPokerGame game, ExchangeResult player, ExchangeResult house) { }

        /// <summary>Whether the House re-raises after a raise past the draw (<paramref name="wants"/>: its own temper's answer).</summary>
        public virtual bool HouseReRaises(HellPokerGame game, bool wants) => wants;

        /// <summary>True for a card of the player's no cheat (and no demon's rule) may touch (the King's palace).</summary>
        public virtual bool IsPlayerCardGuarded(Card card) => false;

        /// <summary>Years put on after the hand is judged, before it is settled (a demon's interest).</summary>
        public virtual int YearsAfterHand(HellPokerGame game, ShowdownOutcome? outcome, bool folded, bool houseFolded) => 0;

        /// <summary>The hand is settled (<see cref="HellPokerGame.LastRound"/> is set).</summary>
        public virtual void HandSettled(HellPokerGame game, RoundResult round) { }
    }

    /// <summary>Several hooks at one table, asked in order (the demon's rule first, then the class's).</summary>
    public class HookChain : TableHooks
    {
        private readonly List<TableHooks> _links = new List<TableHooks>();

        public HookChain(IEnumerable<TableHooks> links)
        {
            if (links != null) _links.AddRange(links.Where(l => l != null));
        }

        public IReadOnlyList<TableHooks> Links => _links;

        public override bool ReplacesClassPower => _links.Any(l => l.ReplacesClassPower);
        public override IEnumerable<Card> CardsOutOfDeck => _links.SelectMany(l => l.CardsOutOfDeck).Distinct().ToArray();
        public override int ExtraMalicePerHand(HellPokerGame game) => _links.Sum(l => l.ExtraMalicePerHand(game));

        public override void HandDealt(HellPokerGame game)
        {
            foreach (TableHooks l in _links) l.HandDealt(game);
        }

        public override void DrawBegins(HellPokerGame game)
        {
            foreach (TableHooks l in _links) l.DrawBegins(game);
        }

        public override bool CanTakeFromOutside(HellPokerGame game, Card card) => _links.Any(l => l.CanTakeFromOutside(game, card));

        public override void TookFromOutside(HellPokerGame game, IReadOnlyCollection<Card> cards)
        {
            foreach (TableHooks l in _links) l.TookFromOutside(game, cards);
        }

        public override IReadOnlyList<int> AdjustHouseDiscards(HellPokerGame game, IReadOnlyList<int> discards)
        {
            foreach (TableHooks l in _links) discards = l.AdjustHouseDiscards(game, discards) ?? discards;
            return discards;
        }

        public override IReadOnlyDictionary<int, Card> HouseTakesFromOutside(HellPokerGame game, IReadOnlyList<int> discards)
        {
            foreach (TableHooks l in _links)
            {
                IReadOnlyDictionary<int, Card> taken = l.HouseTakesFromOutside(game, discards);
                if (taken != null && taken.Count > 0) return taken;
            }
            return null;
        }

        public override void HandDrawn(HellPokerGame game, ExchangeResult player, ExchangeResult house)
        {
            foreach (TableHooks l in _links) l.HandDrawn(game, player, house);
        }

        public override bool HouseReRaises(HellPokerGame game, bool wants)
        {
            foreach (TableHooks l in _links) wants = l.HouseReRaises(game, wants);
            return wants;
        }

        public override bool IsPlayerCardGuarded(Card card) => _links.Any(l => l.IsPlayerCardGuarded(card));

        public override int YearsAfterHand(HellPokerGame game, ShowdownOutcome? outcome, bool folded, bool houseFolded) =>
            _links.Sum(l => l.YearsAfterHand(game, outcome, folded, houseFolded));

        public override void HandSettled(HellPokerGame game, RoundResult round)
        {
            foreach (TableHooks l in _links) l.HandSettled(game, round);
        }

        /// <summary>The first link of a kind (a class's powers, a demon's rule); null when there is none.</summary>
        public T Find<T>() where T : TableHooks => _links.OfType<T>().FirstOrDefault();
    }
}
