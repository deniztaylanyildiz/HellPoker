using HellPoker.Presentation.Ui;
using UnityEngine;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// Demo 3.2: where the table's blocks stand, worked out from the screen's size (<see cref="PixelScreen.Width"/> /
    /// <see cref="PixelScreen.Height"/>) and the blocks' own sizes. Masa birliği: every poker table of the game looks the same — a floor's, a
    /// warden's, a demon's, Lucifer's and the Boss Simulator's: the demon top right (his portrait, name, bar, malice and words), the counter
    /// top left, the player's panel bottom left with the class's kit (or the simulator's LEAVE TABLE) just over it, the middle column (the
    /// cards, the message, the controls, the band) between. The payouts are in the hands' panel (H), never on the table.
    /// </summary>
    public sealed class TableLayout
    {
        /// <summary>The side columns' width (a 96 px portrait in its box) and their distance from the screen's edge.</summary>
        public const int Column = DealerView.PortraitSize + 8;
        public const int Edge = 4;

        /// <summary>The middle column.</summary>
        public const int MiddleWidth = 320;
        public static int Middle => (PixelScreen.Width - MiddleWidth) / 2;

        /// <summary>The bottom row (MENU, HANDS, the shortcut line).</summary>
        public static int BottomY => PixelScreen.Height - 22;

        public static int LeftX => Edge;
        public static int RightX => PixelScreen.Width - Edge - Column;

        /// <summary>The table's one layout.</summary>
        public static TableLayout Table => new TableLayout();

        /// <summary>The demon's column: the portrait box's top left (the name, the title, the dialog box below it).</summary>
        public Vector2Int Dealer { get; }

        /// <summary>The counter (the purse, the sentence, the soul's lamp) and its last hands' pips.</summary>
        public Vector2Int Counter { get; }

        /// <summary>The player's panel.</summary>
        public Vector2Int Panel { get; }

        /// <summary>The class's kit: its left edge and its bottom (just over the player's panel; it grows upward).</summary>
        public int KitX { get; }
        public int KitY { get; }
        public bool KitFromBottom => true;

        /// <summary>The Boss Simulator's LEAVE TABLE / SOUL BOUND, where a chapter's table has its kit: just over the player's panel.</summary>
        public Vector2Int Leave { get; }
        public const int LeaveWidth = Column, LeaveHeight = 18;

        /// <summary>The demon's bar under his name, and the percent floating up beside it (in the gap before the middle column).</summary>
        public Vector2Int Bar { get; }
        public RectInt BarFloat { get; }

        /// <summary>The middle of the demon's portrait (the tithe's coins fly there).</summary>
        public Vector2Int DealerPoint { get; }

        /// <summary>Hover boxes open towards the middle: the demon's to the left of his column, the panel's to the right.</summary>
        public bool DealerTipsLeft => true;
        public bool PanelTipsRight => true;

        /// <summary>The bar's place in the demon's column: under the name (the line under it), inset from the box.</summary>
        public const int BarInset = 4, BarTop = 118, BarWidth = 92, BarHeight = 7;

        /// <summary>The gap between the kit and the panel under it.</summary>
        private const int KitGap = 3;

        private TableLayout()
        {
            Dealer = new Vector2Int(RightX, Edge);
            Counter = new Vector2Int(LeftX, Edge);
            Panel = new Vector2Int(LeftX, BottomY - Edge - PlayerPanelView.Height);
            KitX = LeftX;
            KitY = Panel.y - KitGap;
            Leave = new Vector2Int(LeftX, Panel.y - KitGap - LeaveHeight);
            Bar = new Vector2Int(Dealer.x + BarInset, Dealer.y + BarTop);
            int floatX = Middle + MiddleWidth + 2;
            BarFloat = new RectInt(floatX, Dealer.y + BarTop - 2, RightX - floatX - 2, 8);
            DealerPoint = new Vector2Int(Dealer.x + Column / 2, Dealer.y + Column / 2);
        }
    }
}
