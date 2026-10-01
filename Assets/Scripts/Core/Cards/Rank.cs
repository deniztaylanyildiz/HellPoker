namespace HellPoker.Core.Cards
{
    /// <summary>Numeric values double as comparison strength; Ace is high (14).</summary>
    public enum Rank
    {
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        Ace = 14
    }

    public static class RankExtensions
    {
        public static string ToShortString(this Rank rank)
        {
            switch (rank)
            {
                case Rank.Ten: return "10";
                case Rank.Jack: return "J";
                case Rank.Queen: return "Q";
                case Rank.King: return "K";
                case Rank.Ace: return "A";
                default: return ((int)rank).ToString();
            }
        }
    }
}
