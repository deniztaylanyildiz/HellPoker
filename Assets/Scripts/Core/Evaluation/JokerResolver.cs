using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;

namespace HellPoker.Core.Evaluation
{
    /// <summary>
    /// What jokers become. A joker turns into any card its owner does not already hold (no five of a kind, no card twice in one
    /// hand; the opponent's cards do not matter). <see cref="Resolve"/> finds the best hand the jokers can make, category by
    /// category from the top — no search, so it stays cheap however many jokers there are. A hand without jokers comes back as it is.
    /// </summary>
    public static class JokerResolver
    {
        private static readonly Card[] DeadMansCards =
        {
            new Card(Rank.Ace, Suit.Spades), new Card(Rank.Ace, Suit.Clubs), new Card(Rank.Eight, Suit.Spades), new Card(Rank.Eight, Suit.Clubs)
        };

        private static readonly Suit[] Suits = { Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs };

        /// <summary>Ranks from the Ace down.</summary>
        private static readonly Rank[] RanksDown = Enum.GetValues(typeof(Rank)).Cast<Rank>().OrderByDescending(r => r).ToArray();

        public static int CountJokers(IEnumerable<Card> cards) => cards.Count(card => card.IsJoker);

        /// <summary>The hand with every joker turned into the card that makes the best hand (positions kept).</summary>
        public static Hand Resolve(Hand hand)
        {
            if (hand == null) throw new ArgumentNullException(nameof(hand));
            int[] jokers = Enumerable.Range(0, Hand.Size).Where(i => hand[i].IsJoker).ToArray();
            if (jokers.Length == 0) return hand;
            List<Card> held = hand.Where(card => !card.IsJoker).ToList();
            IReadOnlyList<Card> into = BestSubstitutes(held, jokers.Length);
            return hand.Replace(jokers, into);
        }

        /// <summary>The cards a single joker may become in this hand: every card the owner does not hold, in deck order.</summary>
        public static IReadOnlyList<Card> Choices(Hand hand)
        {
            if (hand == null) throw new ArgumentNullException(nameof(hand));
            return Deck.CreateStandardCards().Where(card => !hand.Contains(card)).ToArray();
        }

        /// <summary>The hand with its only joker turned into <paramref name="card"/>; null when that is not allowed (a card it holds).</summary>
        public static Hand Name(Hand hand, Card card)
        {
            if (hand == null) throw new ArgumentNullException(nameof(hand));
            if (card.IsJoker || hand.Contains(card)) return null;
            int joker = Enumerable.Range(0, Hand.Size).FirstOrDefault(i => hand[i].IsJoker);
            return hand[joker].IsJoker ? hand.With(joker, card) : null;
        }

        /// <summary>The best <paramref name="wild"/> cards to add to <paramref name="held"/> (5 in all), strongest category first.</summary>
        private static IReadOnlyList<Card> BestSubstitutes(List<Card> held, int wild)
        {
            return DeadMansHand(held, wild)
                   ?? StraightFlush(held)
                   ?? FourOfAKind(held, wild)
                   ?? FullHouse(held)
                   ?? Flush(held, wild)
                   ?? Straight(held)
                   ?? ThreeOfAKind(held, wild)
                   ?? Pair(held)
                   ?? throw new InvalidOperationException($"No joker resolution for {string.Join(" ", held)} + {wild} jokers.");
        }

        private static int CountOf(List<Card> held, Rank rank) => held.Count(card => card.Rank == rank);

        /// <summary>A card of <paramref name="rank"/> not in <paramref name="taken"/>; null when all four are taken.</summary>
        private static Card? FreeCard(Rank rank, ICollection<Card> taken)
        {
            foreach (Suit suit in Suits)
            {
                var card = new Card(rank, suit);
                if (!taken.Contains(card)) return card;
            }
            return null;
        }

        /// <summary>The highest free card whose rank is not one of <paramref name="avoidRanks"/>.</summary>
        private static Card HighestFree(ICollection<Card> taken, ICollection<Rank> avoidRanks)
        {
            foreach (Rank rank in RanksDown)
            {
                if (avoidRanks.Contains(rank)) continue;
                Card? card = FreeCard(rank, taken);
                if (card.HasValue) return card.Value;
            }
            throw new InvalidOperationException("No free card left.");
        }

        private static IReadOnlyList<Card> DeadMansHand(List<Card> held, int wild)
        {
            List<Card> outside = held.Where(card => Array.IndexOf(DeadMansCards, card) < 0).ToList();
            if (outside.Count > 1) return null;
            var subs = DeadMansCards.Where(card => !held.Contains(card)).ToList();
            if (subs.Count < wild)
                subs.Add(HighestFree(held.Concat(DeadMansCards).ToList(), Array.Empty<Rank>()));   // the free fifth card: a kicker
            return subs;
        }

        /// <summary>The five ranks of the straight topped by <paramref name="high"/> (the wheel: A 2 3 4 5).</summary>
        private static Rank[] Window(Rank high) =>
            high == Rank.Five
                ? new[] { Rank.Ace, Rank.Two, Rank.Three, Rank.Four, Rank.Five }
                : Enumerable.Range((int)high - 4, 5).Select(r => (Rank)r).ToArray();

        private static IEnumerable<Rank> StraightTops() => RanksDown.Where(r => r >= Rank.Five);

        private static IReadOnlyList<Card> StraightFlush(List<Card> held)
        {
            if (held.Count == 0 || held.Select(card => card.Suit).Distinct().Count() != 1) return null;
            if (held.Select(card => card.Rank).Distinct().Count() != held.Count) return null;
            Suit suit = held[0].Suit;
            foreach (Rank high in StraightTops())
            {
                Rank[] window = Window(high);
                if (held.All(card => window.Contains(card.Rank)))
                    return window.Where(r => held.All(card => card.Rank != r)).Select(r => new Card(r, suit)).ToList();
            }
            return null;
        }

        private static IReadOnlyList<Card> FourOfAKind(List<Card> held, int wild)
        {
            foreach (Rank rank in RanksDown)
            {
                int count = CountOf(held, rank);
                if (held.Count - count > 1 || count + wild < 4) continue;
                var taken = new List<Card>(held);
                var subs = new List<Card>();
                for (int i = count; i < 4; i++)
                {
                    Card card = FreeCard(rank, taken).Value;
                    subs.Add(card);
                    taken.Add(card);
                }
                if (subs.Count < wild)
                    subs.Add(HighestFree(taken, new[] { rank }));   // the kicker
                return subs;
            }
            return null;
        }

        private static IReadOnlyList<Card> FullHouse(List<Card> held)
        {
            foreach (Rank trips in RanksDown)
            foreach (Rank pair in RanksDown)
            {
                if (pair == trips) continue;
                int t = CountOf(held, trips), p = CountOf(held, pair);
                if (t > 3 || p > 2 || t + p != held.Count) continue;
                var taken = new List<Card>(held);
                var subs = new List<Card>();
                for (int i = t; i < 3; i++) { Card card = FreeCard(trips, taken).Value; subs.Add(card); taken.Add(card); }
                for (int i = p; i < 2; i++) { Card card = FreeCard(pair, taken).Value; subs.Add(card); taken.Add(card); }
                return subs;
            }
            return null;
        }

        private static IReadOnlyList<Card> Flush(List<Card> held, int wild)
        {
            if (held.Count == 0 || held.Select(card => card.Suit).Distinct().Count() != 1) return null;
            Suit suit = held[0].Suit;
            return RanksDown.Where(r => held.All(card => card.Rank != r)).Take(wild).Select(r => new Card(r, suit)).ToList();
        }

        private static IReadOnlyList<Card> Straight(List<Card> held)
        {
            if (held.Select(card => card.Rank).Distinct().Count() != held.Count) return null;
            foreach (Rank high in StraightTops())
            {
                Rank[] window = Window(high);
                if (!held.All(card => window.Contains(card.Rank))) continue;
                // Any suit not making a flush: a flush would have been found above, so the held cards are of two suits or more.
                return window.Where(r => held.All(card => card.Rank != r)).Select(r => new Card(r, Suit.Spades)).ToList();
            }
            return null;
        }

        private static IReadOnlyList<Card> ThreeOfAKind(List<Card> held, int wild)
        {
            foreach (Rank rank in RanksDown)
            {
                int count = CountOf(held, rank);
                if (count + wild < 3) continue;
                var taken = new List<Card>(held);
                var subs = new List<Card>();
                for (int i = count; i < 3; i++) { Card card = FreeCard(rank, taken).Value; subs.Add(card); taken.Add(card); }
                var heldRanks = held.Select(card => card.Rank).Append(rank).ToList();
                while (subs.Count < wild)
                {
                    Card kicker = HighestFree(taken, heldRanks);
                    subs.Add(kicker);
                    taken.Add(kicker);
                    heldRanks.Add(kicker.Rank);
                }
                return subs;
            }
            return null;
        }

        private static IReadOnlyList<Card> Pair(List<Card> held)
        {
            if (held.Count == 0) return null;
            Rank highest = held.Max(card => card.Rank);
            Card? card = FreeCard(highest, held);
            return card.HasValue ? new[] { card.Value } : null;
        }
    }

    /// <summary>
    /// Judges a hand with jokers as the best hand they can make (<see cref="JokerResolver.Resolve"/>); without jokers it is the
    /// plain evaluator. The evaluation's hand is the resolved one (the jokers' cards in their places).
    /// </summary>
    public sealed class WildJokerEvaluator : IHandEvaluator
    {
        private readonly IHandEvaluator _inner;

        public WildJokerEvaluator(IHandEvaluator inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public HandEvaluation Evaluate(Hand hand) => _inner.Evaluate(JokerResolver.Resolve(hand));
    }
}
