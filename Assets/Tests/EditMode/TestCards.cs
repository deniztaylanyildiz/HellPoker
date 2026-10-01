using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;

namespace HellPoker.Core.Tests
{
    /// <summary>Parses compact notation like "AS 10H 8C" (rank + suit letter C/D/H/S).</summary>
    internal static class TestCards
    {
        public static Hand Hand(string notation)
        {
            return new Hand(Cards(notation));
        }

        public static IEnumerable<Card> Cards(string notation)
        {
            return notation.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(Card).ToArray();
        }

        public static Card Card(string token)
        {
            string rankPart = token.Substring(0, token.Length - 1);
            char suitPart = char.ToUpperInvariant(token[token.Length - 1]);

            Rank rank;
            switch (rankPart.ToUpperInvariant())
            {
                case "A": rank = Rank.Ace; break;
                case "K": rank = Rank.King; break;
                case "Q": rank = Rank.Queen; break;
                case "J": rank = Rank.Jack; break;
                default: rank = (Rank)int.Parse(rankPart); break;
            }

            Suit suit;
            switch (suitPart)
            {
                case 'C': suit = Suit.Clubs; break;
                case 'D': suit = Suit.Diamonds; break;
                case 'H': suit = Suit.Hearts; break;
                case 'S': suit = Suit.Spades; break;
                default: throw new ArgumentException($"Unknown suit in '{token}'.");
            }

            return new Card(rank, suit);
        }
    }
}
