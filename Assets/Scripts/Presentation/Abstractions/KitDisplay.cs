using System.Collections.Generic;
using HellPoker.Core.Cards;

namespace HellPoker.Presentation.Abstractions
{
    /// <summary>A small card of the class panel (a satchel card, a thrown card to keep, a card of the deck's top).</summary>
    public sealed class KitCard
    {
        public Card Card { get; }

        /// <summary>Picked (a satchel card going into the draw): a gold frame.</summary>
        public bool Picked { get; }

        /// <summary>A click does something now (the frame blinks).</summary>
        public bool Clickable { get; }

        public KitCard(Card card, bool picked = false, bool clickable = false)
        {
            Card = card;
            Picked = picked;
            Clickable = clickable;
        }
    }

    /// <summary>A button of the class panel (the King's acts).</summary>
    public sealed class KitButton
    {
        public string Label { get; }

        /// <summary>Dimmed: it cannot be done now (a click still says why).</summary>
        public bool Locked { get; }

        public KitButton(string label, bool locked)
        {
            Label = label;
            Locked = locked;
        }
    }

    /// <summary>
    /// The class panel of a Phase 2 table, under the dialog box: a title, a row of small cards or of buttons, and a hover text.
    /// <see cref="Hidden"/> at every demo table (and for the Jester).
    /// </summary>
    public sealed class KitDisplay
    {
        public static readonly KitDisplay Hidden = new KitDisplay(null, null, null, null);

        public string Title { get; }
        public IReadOnlyList<KitCard> Cards { get; }
        public IReadOnlyList<KitButton> Buttons { get; }
        public string Hint { get; }

        /// <summary>The panel waits for a click (a pick under way): it pulses.</summary>
        public bool Waiting { get; }

        /// <summary>A line in place of the cards (the prophecy shut: "Shows after the deal."); null for none.</summary>
        public string Note { get; }

        public bool IsHidden => Title == null;

        public KitDisplay(string title, IReadOnlyList<KitCard> cards, IReadOnlyList<KitButton> buttons, string hint, bool waiting = false,
            string note = null)
        {
            Note = note;
            Title = title;
            Cards = cards ?? new KitCard[0];
            Buttons = buttons ?? new KitButton[0];
            Hint = hint;
            Waiting = waiting;
        }
    }
}
