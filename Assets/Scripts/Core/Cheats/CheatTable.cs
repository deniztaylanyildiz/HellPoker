using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Draw;
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

        /// <summary>The King's protection: these cards of the player's are beyond every cheat this hand.</summary>
        public HashSet<Card> Protected { get; } = new HashSet<Card>();

        public void Clear()
        {
            Chained.Clear();
            Thorned.Clear();
            HiddenFromPlayer.Clear();
            FakeHouseIndex = -1;
            Tithe = false;
            Gaze = false;
            Protected.Clear();
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

        /// <summary>How many of the player's cards (from the left) the player has already seen face up.</summary>
        public int PlayerCardsSeen { get; }

        /// <summary>How often (percent) a cheat that may slip does slip and leave it to chance (Belial's tongue).</summary>
        public int BackfirePercent { get; }

        /// <summary>The House's own sense of which cards to throw back; null when there is none to ask.</summary>
        public IDrawStrategy Advice { get; }

        public CheatTable(Hand playerHand, Hand houseHand, IDeck deck, IHandEvaluator evaluator, IRandomSource random, CheatMarks marks,
            int unit, int houseCardsRevealed = 0, IReadOnlyList<int> drawnIndices = null, ShowdownResult showdown = null,
            int playerCardsSeen = Hand.Size, int backfirePercent = 0, IDrawStrategy advice = null)
        {
            if (playerCardsSeen < 0 || playerCardsSeen > Hand.Size) throw new ArgumentOutOfRangeException(nameof(playerCardsSeen));
            if (backfirePercent < 0 || backfirePercent > 100) throw new ArgumentOutOfRangeException(nameof(backfirePercent));
            PlayerCardsSeen = playerCardsSeen;
            BackfirePercent = backfirePercent;
            Advice = advice;
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

        /// <summary>True for a player's card no cheat may touch: a Dead Man's Hand card, or one under the King's protection.</summary>
        public bool IsUntouchable(int index) => IsImmune(PlayerHand[index]) || Marks.Protected.Contains(PlayerHand[index]);

        /// <summary>Positions in the player's hand a cheat may touch (not untouchable, and passing the extra test).</summary>
        public IEnumerable<int> PlayerTargets(Func<int, bool> also = null) =>
            Enumerable.Range(0, Hand.Size).Where(i => !IsUntouchable(i) && (also == null || also(i)));

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

        /// <summary>The cards a sensible player would throw back (the House's own logic); empty for a made hand or no advice.</summary>
        public IReadOnlyCollection<int> AdvisedDiscards() =>
            Advice == null ? Array.Empty<int>() : Advice.ChooseDiscards(PlayerHand);

        /// <summary>True when <paramref name="after"/> is a stronger hand for the player than the one they hold now.</summary>
        public bool Improves(Hand after) => Evaluator.Evaluate(after).CompareTo(Evaluator.Evaluate(PlayerHand)) > 0;

        /// <summary>
        /// The player's cards that make up their best combination: every card of a straight, flush or better; the cards of a
        /// pair, two pair, trips, full house or quads; nothing for a plain high card.
        /// </summary>
        public IEnumerable<int> CombinationCards()
        {
            HandCategory category = Evaluator.Evaluate(PlayerHand).Category;
            bool wholeHand = category == HandCategory.Straight || category == HandCategory.Flush || category == HandCategory.StraightFlush
                             || category == HandCategory.RoyalFlush;
            Hand hand = PlayerHand;
            return Enumerable.Range(0, Hand.Size).Where(i => wholeHand || hand.Count(c => c.Rank == hand[i].Rank) >= 2);
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
