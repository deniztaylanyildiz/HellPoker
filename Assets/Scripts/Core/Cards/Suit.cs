namespace HellPoker.Core.Cards
{
    public enum Suit
    {
        Clubs,
        Diamonds,
        Hearts,
        Spades
    }

    public static class SuitExtensions
    {
        public static bool IsBlack(this Suit suit)
        {
            return suit == Suit.Clubs || suit == Suit.Spades;
        }

        public static char ToSymbol(this Suit suit)
        {
            switch (suit)
            {
                case Suit.Clubs: return '♣';
                case Suit.Diamonds: return '♦';
                case Suit.Hearts: return '♥';
                default: return '♠';
            }
        }
    }
}
