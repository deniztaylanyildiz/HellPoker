using System;

namespace HellPoker.Core.Cards
{
    /// <summary>Immutable playing card value.</summary>
    public readonly struct Card : IEquatable<Card>
    {
        public Rank Rank { get; }
        public Suit Suit { get; }

        public Card(Rank rank, Suit suit)
        {
            if (!Enum.IsDefined(typeof(Rank), rank))
                throw new ArgumentOutOfRangeException(nameof(rank), rank, "Unknown rank.");
            if (!Enum.IsDefined(typeof(Suit), suit))
                throw new ArgumentOutOfRangeException(nameof(suit), suit, "Unknown suit.");

            Rank = rank;
            Suit = suit;
        }

        public bool Equals(Card other)
        {
            return Rank == other.Rank && Suit == other.Suit;
        }

        public override bool Equals(object obj)
        {
            return obj is Card other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ((int)Rank << 2) | (int)Suit;
        }

        public static bool operator ==(Card left, Card right) => left.Equals(right);
        public static bool operator !=(Card left, Card right) => !left.Equals(right);

        public override string ToString()
        {
            return Rank.ToShortString() + Suit.ToSymbol();
        }
    }
}
