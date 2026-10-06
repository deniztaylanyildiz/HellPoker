using System;

namespace HellPoker.Core.Cards
{
    /// <summary>
    /// Immutable playing card value — or a joker (the Jester's deck). A joker has no rank or suit of its own (it becomes a card only
    /// at the showdown, see <see cref="Evaluation.JokerResolver"/>); jokers are told apart by a serial number, so a deck may hold
    /// many. The 52 ordinary cards behave, compare and hash exactly as they always did.
    /// </summary>
    public readonly struct Card : IEquatable<Card>
    {
        /// <summary>Not a suit: a joker's (it never counts towards a flush).</summary>
        private const Suit NoSuit = (Suit)(-1);

        /// <summary>For a joker: (Rank)0, below every real rank; it never counts towards a straight or a group.</summary>
        public Rank Rank { get; }
        public Suit Suit { get; }

        /// <summary>0 for an ordinary card; 1, 2, 3... for the jokers.</summary>
        private readonly int _joker;

        public Card(Rank rank, Suit suit)
        {
            if (!Enum.IsDefined(typeof(Rank), rank))
                throw new ArgumentOutOfRangeException(nameof(rank), rank, "Unknown rank.");
            if (!Enum.IsDefined(typeof(Suit), suit))
                throw new ArgumentOutOfRangeException(nameof(suit), suit, "Unknown suit.");

            Rank = rank;
            Suit = suit;
            _joker = 0;
        }

        private Card(int jokerSerial)
        {
            Rank = 0;
            Suit = NoSuit;
            _joker = jokerSerial;
        }

        /// <summary>The joker number <paramref name="serial"/> (1 and up).</summary>
        public static Card Joker(int serial)
        {
            if (serial <= 0) throw new ArgumentOutOfRangeException(nameof(serial), serial, "Jokers are numbered from 1.");
            return new Card(serial);
        }

        public bool IsJoker => _joker > 0;

        /// <summary>The joker's number; 0 for an ordinary card.</summary>
        public int JokerSerial => _joker;

        public bool Equals(Card other)
        {
            return Rank == other.Rank && Suit == other.Suit && _joker == other._joker;
        }

        public override bool Equals(object obj)
        {
            return obj is Card other && Equals(other);
        }

        public override int GetHashCode()
        {
            return IsJoker ? 1000 + _joker : ((int)Rank << 2) | (int)Suit;
        }

        public static bool operator ==(Card left, Card right) => left.Equals(right);
        public static bool operator !=(Card left, Card right) => !left.Equals(right);

        public override string ToString()
        {
            return IsJoker ? "Jk" + _joker : Rank.ToShortString() + Suit.ToSymbol();
        }
    }
}
