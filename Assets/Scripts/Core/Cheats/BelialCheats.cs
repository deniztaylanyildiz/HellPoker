using System;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Evaluation;

namespace HellPoker.Core.Cheats
{
    /// <summary>
    /// False Face (minor, as the House shows its cards): one card it turns shows a false face — a weaker one, to lure a raise —
    /// until the showdown turns the truth. The false card carries the faintest silver sheen.
    /// </summary>
    public sealed class FalseFaceCheat : ICheat
    {
        public string Id => CheatIds.FalseFace;
        public CheatTier Tier => CheatTier.Minor;
        public CheatTiming Timing => CheatTiming.HouseReveal;

        public bool CanApply(CheatTable table) =>
            table.HouseCardsRevealed > 0 && table.Deck.Count > 0 && table.HouseTargets(i => i < table.HouseCardsRevealed).Any();

        public CheatResult Apply(CheatTable table)
        {
            if (!CanApply(table)) return CheatResult.Fizzled(Id);
            int index = table.Pick(table.HouseTargets(i => i < table.HouseCardsRevealed));
            Card real = table.HouseHand[index];

            // A face from the deck (so it is in nobody's hand), weaker than the truth when one can be found.
            Card[] weaker = table.Deck.Remaining.Where(c => c.Rank < real.Rank).ToArray();
            Card[] pool = weaker.Length > 0 ? weaker : table.Deck.Remaining.ToArray();
            Card face = pool[table.Random.Next(pool.Length)];

            table.Marks.FakeHouseIndex = index;
            table.Marks.FakeHouseFace = face;
            return new CheatResult(Id, CheatOutcome.Played, houseCards: new[] { index });
        }
    }

    /// <summary>
    /// Forked Tongue (minor, after the draw): one of the player's cards changes suit; the new suit's card comes out of the deck,
    /// so no card is ever in two places.
    /// Most of the time it is aimed: it breaks a flush (or four of a suit) first, else any change that weakens the hand, else
    /// a change that does no good. But a liar's tongue may slip (<see cref="CheatTable.BackfirePercent"/>): then the suit
    /// changes at random — and that may hand the player a flush.
    /// </summary>
    public sealed class ForkedTongueCheat : ICheat
    {
        public string Id => CheatIds.ForkedTongue;
        public CheatTier Tier => CheatTier.Minor;
        public CheatTiming Timing => CheatTiming.AfterDraw;

        public bool CanApply(CheatTable table) => Targets(table).Any();

        public CheatResult Apply(CheatTable table)
        {
            (int index, Card card)[] targets = Targets(table).ToArray();
            if (targets.Length == 0) return CheatResult.Fizzled(Id);

            bool slipped = table.Random.Next(100) < table.BackfirePercent;
            (int, Card)[] pool = slipped ? targets : Aimed(table, targets);
            if (pool.Length == 0) return CheatResult.Fizzled(Id);
            var (i, into) = pool[table.Random.Next(pool.Length)];
            Card lost = table.PlayerHand[i];
            table.TurnPlayerCardInto(i, into);
            return new CheatResult(Id, CheatOutcome.Played, new[] { i }, lost: lost, gained: into);
        }

        /// <summary>The aimed strikes: break the flush (or the four of a suit), else weaken, else at least do no good.</summary>
        private static (int, Card)[] Aimed(CheatTable table, (int index, Card card)[] targets)
        {
            Hand hand = table.PlayerHand;
            Suit? flushSuit = hand.GroupBy(c => c.Suit).Where(g => g.Count() >= 4).Select(g => (Suit?)g.Key).FirstOrDefault();
            if (flushSuit.HasValue)
            {
                (int, Card)[] breakers = targets.Where(t => hand[t.index].Suit == flushSuit.Value && t.card.Suit != flushSuit.Value).ToArray();
                if (breakers.Length > 0) return breakers;
            }

            HandEvaluation now = table.Evaluator.Evaluate(hand);
            (int, Card)[] weaker = targets.Where(t => table.Evaluator.Evaluate(hand.With(t.index, t.card)).CompareTo(now) < 0).ToArray();
            if (weaker.Length > 0) return weaker;

            // Never a gift when aimed (in the unlikely hand where every change helps, the tongue holds still).
            return targets.Where(t => !table.Improves(hand.With(t.index, t.card))).ToArray();
        }

        /// <summary>Every (position, same rank in another suit still in the deck) the tongue could strike.</summary>
        private static System.Collections.Generic.IEnumerable<(int index, Card card)> Targets(CheatTable table)
        {
            foreach (int i in table.PlayerTargets())
            {
                Card card = table.PlayerHand[i];
                foreach (Suit suit in Enum.GetValues(typeof(Suit)))
                {
                    if (suit == card.Suit) continue;
                    var other = new Card(card.Rank, suit);
                    if (CheatRules.IsImmune(other) || !table.Deck.Remaining.Contains(other)) continue;
                    yield return (i, other);
                }
            }
        }
    }

    /// <summary>
    /// Serpent Swap (major, after the draw): one of the player's best cards slips across to the House, and a face-down House
    /// card slips back — face down to the player too, until the showdown.
    /// </summary>
    public sealed class SerpentSwapCheat : ICheat
    {
        public string Id => CheatIds.SerpentSwap;
        public CheatTier Tier => CheatTier.Major;
        public CheatTiming Timing => CheatTiming.AfterDraw;

        public bool CanApply(CheatTable table) => Mine(table) >= 0 && Theirs(table, Mine(table)).Any();

        public CheatResult Apply(CheatTable table)
        {
            int mine = Mine(table);
            int[] options = mine < 0 ? new int[0] : Theirs(table, mine).ToArray();
            if (options.Length == 0) return CheatResult.Fizzled(Id);

            Hand hand = table.PlayerHand;
            int theirs = options[table.Random.Next(options.Length)];
            Card taken = hand[mine];
            Card given = table.HouseHand[theirs];
            table.PlayerHand = hand.With(mine, given);
            table.HouseHand = table.HouseHand.With(theirs, taken);
            table.Marks.HiddenFromPlayer.Add(given);
            return new CheatResult(Id, CheatOutcome.Played, new[] { mine }, new[] { theirs }, lost: taken);
        }

        /// <summary>The player's most valuable card: the highest of the most repeated rank (a pair's card before a lone ace).</summary>
        private static int Mine(CheatTable table)
        {
            Hand hand = table.PlayerHand;
            return table.PlayerTargets()
                .OrderByDescending(i => hand.Count(c => c.Rank == hand[i].Rank))
                .ThenByDescending(i => hand[i].Rank)
                .DefaultIfEmpty(-1)
                .First();
        }

        /// <summary>The House's face-down cards that may slip across without leaving the player any better off.</summary>
        private static System.Collections.Generic.IEnumerable<int> Theirs(CheatTable table, int mine) =>
            table.HouseTargets(i => i >= table.HouseCardsRevealed && !table.Improves(table.PlayerHand.With(mine, table.HouseHand[i])));
    }
}
