using HellPoker.Core.Cards;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>What one card position on the table should show.</summary>
    public readonly struct CardSlot
    {
        public enum SlotKind
        {
            Empty,
            Back,
            Face
        }

        public SlotKind Kind { get; }

        /// <summary>Only meaningful when <see cref="Kind"/> is Face.</summary>
        public Card Card { get; }

        private CardSlot(SlotKind kind, Card card)
        {
            Kind = kind;
            Card = card;
        }

        public static CardSlot Empty => new CardSlot(SlotKind.Empty, default);
        public static CardSlot Back => new CardSlot(SlotKind.Back, default);
        public static CardSlot Face(Card card) => new CardSlot(SlotKind.Face, card);

        public bool SameAs(CardSlot other)
        {
            return Kind == other.Kind && (Kind != SlotKind.Face || Card == other.Card);
        }

        public override string ToString()
        {
            return Kind == SlotKind.Face ? Card.ToString() : Kind.ToString();
        }
    }
}
