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
    /// Forked Tongue (minor, after the draw): one of the player's cards changes suit. It goes for a flush first; without one,
    /// any card. The new suit's card comes out of the deck, so no card is ever in two places.
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

            var (i, into) = targets[table.Random.Next(targets.Length)];
            Card lost = table.PlayerHand[i];
            table.TurnPlayerCardInto(i, into);
            return new CheatResult(Id, CheatOutcome.Played, new[] { i }, lost: lost, gained: into);
        }

        /// <summary>
        /// Every (position, same rank in another suit still in the deck) the tongue could strike. With a flush every card is
        /// part of it, so any of them breaks it; without one, any card will do.
        /// </summary>
        private static System.Collections.Generic.IEnumerable<(int, Card)> Targets(CheatTable table)
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

        public bool CanApply(CheatTable table) =>
            table.PlayerTargets().Any() && table.HouseTargets(i => i >= table.HouseCardsRevealed).Any();

        public CheatResult Apply(CheatTable table)
        {
            if (!CanApply(table)) return CheatResult.Fizzled(Id);

            // The player's most valuable card: the highest of the most repeated rank (a pair's card before a lone ace).
            Hand hand = table.PlayerHand;
            int mine = table.PlayerTargets()
                .OrderByDescending(i => hand.Count(c => c.Rank == hand[i].Rank))
                .ThenByDescending(i => hand[i].Rank)
                .First();
            int theirs = table.Pick(table.HouseTargets(i => i >= table.HouseCardsRevealed));

            Card taken = hand[mine];
            Card given = table.HouseHand[theirs];
            table.PlayerHand = hand.With(mine, given);
            table.HouseHand = table.HouseHand.With(theirs, taken);
            table.Marks.HiddenFromPlayer.Add(given);
            return new CheatResult(Id, CheatOutcome.Played, new[] { mine }, new[] { theirs }, lost: taken);
        }
    }
}
