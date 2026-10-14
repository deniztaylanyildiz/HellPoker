using System;

namespace HellPoker.Presentation.Ui
{
    /// <summary>
    /// The deck's colours right now (Settings ▸ GAME ▸ FOUR-COLOUR DECK, "settings.fourcolor"): four colours (spades black, hearts red,
    /// clubs green, diamonds blue — the default) or the classic two. Every card on screen (the table's, the joker picker's, the class
    /// panel's small ones) reads it and listens to <see cref="Changed"/> to repaint.
    /// </summary>
    public static class SuitColors
    {
        public static bool FourColor { get; private set; } = true;

        /// <summary>Raised after the setting changed (not when it is set to what it was).</summary>
        public static event Action Changed;

        public static void Set(bool fourColor)
        {
            if (fourColor == FourColor) return;
            FourColor = fourColor;
            Changed?.Invoke();
        }
    }
}
