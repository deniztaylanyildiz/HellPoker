using System;

namespace HellPoker.Presentation.Ui
{
    /// <summary>
    /// The deck's colours right now (Settings ▸ GAME ▸ FOUR-COLOUR DECK, "settings.fourcolor.v2"): the classic two (Demo 2.3's default)
    /// or four colours (spades black, hearts red, clubs green, diamonds blue). Every card on screen (the table's, the joker picker's, the
    /// class panel's) reads it and listens to <see cref="Changed"/> to repaint.
    /// </summary>
    public static class SuitColors
    {
        public static bool FourColor { get; private set; }

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
