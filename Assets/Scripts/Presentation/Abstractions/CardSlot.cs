using HellPoker.Core.Cards;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>What one card position on the table should show — and any mark a cheat left on it.</summary>
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

        /// <summary>A cheat's mark on the card (chain, thorn, veil, false face).</summary>
        public CardMark Mark { get; }

        private CardSlot(SlotKind kind, Card card, CardMark mark = CardMark.None)
        {
            Kind = kind;
            Card = card;
            Mark = mark;
        }

        public static CardSlot Empty => new CardSlot(SlotKind.Empty, default);
        public static CardSlot Back => new CardSlot(SlotKind.Back, default);
        public static CardSlot Face(Card card) => new CardSlot(SlotKind.Face, card);

        public CardSlot WithMark(CardMark mark) => new CardSlot(Kind, Card, mark);

        /// <summary>Same card shown the same way (the mark aside).</summary>
        public bool SameAs(CardSlot other)
        {
            return Kind == other.Kind && (Kind != SlotKind.Face || Card == other.Card);
        }

        public override string ToString()
        {
            string shown = Kind == SlotKind.Face ? Card.ToString() : Kind.ToString();
            return Mark == CardMark.None ? shown : shown + "+" + Mark;
        }
    }
}
