namespace HellPoker.Core.Evaluation
{
    /// <summary>Ordered weakest to strongest; the numeric order is used for comparison.</summary>
    public enum HandCategory
    {
        HighCard,
        OnePair,
        TwoPair,
        ThreeOfAKind,
        Straight,
        Flush,
        FullHouse,
        FourOfAKind,
        StraightFlush,
        RoyalFlush,

        /// <summary>A♠ A♣ 8♠ 8♣ + any fifth card. Hell's own hand — beats everything.</summary>
        DeadMansHand
    }
}
