using HellPoker.Core.Cards;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>The showdown's joker picker as the screen shows it: the card the player's joker would become now.</summary>
    public sealed class JokerPick
    {
        public Card Card { get; }

        public JokerPick(Card card)
        {
            Card = card;
        }
    }
}