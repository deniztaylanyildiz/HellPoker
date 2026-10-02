using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;

namespace HellPoker.Core.Cheats
{
    /// <summary>
    /// What the cheats have done to the current hand that the table must keep showing (and the rules must respect):
    /// chained and thorned cards, cards hidden from the player, a false face on a House card, the tithe, the gaze.
    /// Marks follow the cards, not the positions — a veiled card thrown back takes its veil with it.
    /// </summary>
    public sealed class CheatMarks
    {
        /// <summary>Collateral: these cards may not be thrown back this hand.</summary>
        public HashSet<Card> Chained { get; } = new HashSet<Card>();

        /// <summary>Thorns: throwing one of these back costs a betting unit, at once.</summary>
        public HashSet<Card> Thorned { get; } = new HashSet<Card>();

        /// <summary>Cards in the player's hand the player cannot see until the showdown.</summary>
        public HashSet<Card> HiddenFromPlayer { get; } = new HashSet<Card>();

        /// <summary>False Face: the House card at <see cref="FakeHouseIndex"/> shows as <see cref="FakeHouseFace"/> until the showdown.</summary>
        public int FakeHouseIndex { get; set; } = -1;

        public Card FakeHouseFace { get; set; }

        /// <summary>Tithe: a win this hand forgives one unit less.</summary>
        public bool Tithe { get; set; }

        /// <summary>Gaze: the House re-raises knowing the player's hand.</summary>
        public bool Gaze { get; set; }

        public void Clear()
        {
            Chained.Clear();
            Thorned.Clear();
            HiddenFromPlayer.Clear();
            FakeHouseIndex = -1;
            Tithe = false;
            Gaze = false;
        }
    }

    /// <summary>
    /// The hand as a cheat sees it and may change it: both hands, the deck, the marks, and the means to judge cards.
    /// The game hands one over at the cheat's moment and takes the hands back afterwards.
    /// </summary>
    public sealed class CheatTable
    {
        public Hand PlayerHand { get; set; }
        public Hand HouseHand { get; set; }
        public IDeck Deck { get; }
        public IHandEvaluator Evaluator { get; }
        public IRandomSource Random { get; }
        public CheatMarks Marks { get; }

        /// <summary>The betting unit of the hand (the size of a thorn or a tithe).</summary>
        public int Unit { get; }

        /// <summary>How many House cards are face up.</summary>
        public int HouseCardsRevealed { get; }

        /// <summary>Positions in the player's hand that received new cards in the draw.</summary>
        public IReadOnlyList<int> DrawnIndices { get; }

        /// <summary>At the showdown: how it stands. The Fall may change it.</summary>
        public ShowdownResult Showdown { get; set; }

        public CheatTable(Hand playerHand, Hand houseHand, IDeck deck, IHandEvaluator evaluator, IRandomSource random, CheatMarks marks,
            int unit, int houseCardsRevealed = 0, IReadOnlyList<int> drawnIndices = null, ShowdownResult showdown = null)
        {
            PlayerHand = playerHand ?? throw new ArgumentNullException(nameof(playerHand));
            HouseHand = houseHand ?? throw new ArgumentNullException(nameof(houseHand));
            Deck = deck ?? throw new ArgumentNullException(nameof(deck));
            Evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            Random = random ?? throw new ArgumentNullException(nameof(random));
            Marks = marks ?? throw new ArgumentNullException(nameof(marks));
            Unit = unit;
            HouseCardsRevealed = houseCardsRevealed;
            DrawnIndices = drawnIndices ?? Array.Empty<int>();
            Showdown = showdown;
        }

        // ------------------------------------------------------------------ helpers every cheat leans on

        /// <summary>The Dead Man's Hand cards are beyond any cheat.</summary>
        public static bool IsImmune(Card card) => CheatRules.IsImmune(card);

        /// <summary>Positions in the player's hand a cheat may touch (not immune, and passing the extra test).</summary>
        public IEnumerable<int> PlayerTargets(Func<int, bool> also = null) =>
            Enumerable.Range(0, Hand.Size).Where(i => !IsImmune(PlayerHand[i]) && (also == null || also(i)));

        public IEnumerable<int> HouseTargets(Func<int, bool> also = null) =>
            Enumerable.Range(0, Hand.Size).Where(i => !IsImmune(HouseHand[i]) && (also == null || also(i)));

        /// <summary>The position of the highest card among the targets (ties: the first); -1 when there is none.</summary>
        public static int Highest(Hand hand, IEnumerable<int> targets)
        {
            int best = -1;
            foreach (int i in targets)
            {
                if (best < 0 || hand[i].Rank > hand[best].Rank) best = i;
            }
            return best;
        }

        public static int Lowest(Hand hand, IEnumerable<int> targets)
        {
            int best = -1;
            foreach (int i in targets)
            {
                if (best < 0 || hand[i].Rank < hand[best].Rank) best = i;
            }
            return best;
        }

        public int Pick(IEnumerable<int> targets)
        {
            int[] all = targets.ToArray();
            return all.Length == 0 ? -1 : all[Random.Next(all.Length)];
        }

        /// <summary>The player's card at <paramref name="index"/> becomes a particular card taken out of the deck.</summary>
        /// <returns>False when that card is not in the deck.</returns>
        public bool TurnPlayerCardInto(int index, Card card)
        {
            if (!Deck.Take(card)) return false;
            PlayerHand = PlayerHand.With(index, card);
            return true;
        }

        /// <summary>The player's card at <paramref name="index"/> is replaced by the next card of the deck; false when it is empty.</summary>
        public bool RedealPlayerCard(int index, out Card card)
        {
            card = default;
            if (Deck.Count == 0) return false;
            card = Deck.Draw();
            PlayerHand = PlayerHand.With(index, card);
            return true;
        }

        public bool RedealHouseCard(int index, out Card card)
        {
            card = default;
            if (Deck.Count == 0) return false;
            card = Deck.Draw();
            HouseHand = HouseHand.With(index, card);
            return true;
        }
    }

    /// <summary>The rules no cheat may break.</summary>
    public static class CheatRules
    {
        private static readonly Card[] DeadMansCards =
        {
            new Card(Rank.Ace, Suit.Spades), new Card(Rank.Ace, Suit.Clubs), new Card(Rank.Eight, Suit.Spades), new Card(Rank.Eight, Suit.Clubs)
        };

        /// <summary>A♠ A♣ 8♠ 8♣ are beyond any cheat, wherever they lie.</summary>
        public static bool IsImmune(Card card) => Array.IndexOf(DeadMansCards, card) >= 0;
    }
}
