using System;
using System.Collections.Generic;
using System.Linq;

namespace HellPoker.Core.Cards
{
    /// <summary>
    /// Cards as short text for the save ("AS", "10H", "3C"): rank, then the suit's letter (C D H S). Jokers have no code — a deck
    /// with jokers (the Jester's) is never saved.
    /// </summary>
    public static class CardCodes
    {
        public static string Format(Card card)
        {
            if (card.IsJoker) throw new ArgumentException("A joker has no code.", nameof(card));
            return card.Rank.ToShortString() + "CDHS"[(int)card.Suit];
        }

        public static string FormatAll(IEnumerable<Card> cards) => string.Join(",", cards.Select(Format));

        public static bool TryParse(string code, out Card card)
        {
            card = default;
            if (string.IsNullOrEmpty(code) || code.Length < 2) return false;
            int suit = "CDHS".IndexOf(code[code.Length - 1]);
            if (suit < 0) return false;
            string rank = code.Substring(0, code.Length - 1);
            Rank r;
            switch (rank)
            {
                case "A": r = Rank.Ace; break;
                case "K": r = Rank.King; break;
                case "Q": r = Rank.Queen; break;
                case "J": r = Rank.Jack; break;
                default:
                    if (!int.TryParse(rank, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int n)
                        || n < 2 || n > 10) return false;
                    r = (Rank)n;
                    break;
            }
            card = new Card(r, (Suit)suit);
            return true;
        }

        /// <summary>
        /// A saved deck: the cards in order, or null when the text is missing or wrong in any way — a bad code, a card twice, more
        /// than 52 (the caller then shuffles a fresh deck). An empty text is an empty deck.
        /// </summary>
        public static IReadOnlyList<Card> TryParseDeck(string text)
        {
            if (text == null) return null;
            if (text.Length == 0) return new Card[0];
            var cards = new List<Card>();
            var seen = new HashSet<Card>();
            foreach (string code in text.Split(','))
            {
                if (!TryParse(code.Trim(), out Card card) || !seen.Add(card)) return null;
                cards.Add(card);
            }
            return cards.Count <= 52 ? cards : null;
        }
    }
}