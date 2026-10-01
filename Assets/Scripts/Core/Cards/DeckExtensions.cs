namespace HellPoker.Core.Cards
{
    public static class DeckExtensions
    {
        public static Hand DealHand(this IDeck deck)
        {
            return new Hand(deck.Draw(Hand.Size));
        }
    }
}
