using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using UnityEngine;

namespace HellPoker.Presentation.Views
{
    /// <summary>
    /// Demo 3.2: where the table's blocks stand — one place for both layouts, worked out from the screen's size
    /// (<see cref="PixelScreen.Width"/> / <see cref="PixelScreen.Height"/>) and the blocks' own sizes. The middle column (the cards, the message,
    /// the controls, the band) is the same in both; the side columns swap: at a chapter's table the demon goes top right (his portrait, name,
    /// bar, malice and words), the counter top left and the player's panel bottom left with the class's kit just over it.
    /// </summary>
    public sealed class TableLayout
    {
        /// <summary>The side columns' width (a 96 px portrait in its box) and their distance from the screen's edge.</summary>
        public const int Column = DealerView.PortraitSize + 8;
        public const int Edge = 4;

        /// <summary>The middle column (the same in both layouts).</summary>
        public const int MiddleWidth = 320;
        public static int Middle => (PixelScreen.Width - MiddleWidth) / 2;

        /// <summary>The bottom row (MENU, HANDS, the shortcut line).</summary>
        public static int BottomY => PixelScreen.Height - 22;

        public static int LeftX => Edge;
        public static int RightX => PixelScreen.Width - Edge - Column;

        public TableLayoutKind Kind { get; }

        /// <summary>The demon's column: the portrait box's top left (the name, the title, the dialog box below it).</summary>
        public Vector2Int Dealer { get; }

        /// <summary>The counter (the purse, the sentence, the soul's lamp) and its last hands' pips.</summary>
        public Vector2Int Counter { get; }

        /// <summary>The player's panel (a chapter's table; the simulator has the payouts there instead).</summary>
        public Vector2Int Panel { get; }

        /// <summary>The class's kit: its left edge, and its top (the simulator: under the dialog box) or its bottom (a chapter's: just over
        /// the player's panel).</summary>
        public int KitX { get; }
        public int KitY { get; }
        public bool KitFromBottom { get; }

        /// <summary>The demon's bar under his name, and the percent floating up beside it (in the gap before the middle column).</summary>
        public Vector2Int Bar { get; }
        public RectInt BarFloat { get; }

        /// <summary>The middle of the demon's portrait (the tithe's coins fly there).</summary>
        public Vector2Int DealerPoint { get; }

        /// <summary>Hover boxes open towards the middle: the demon's to the left of his column at a chapter's table, the panel's to the right.</summary>
        public bool DealerTipsLeft { get; }
        public bool PanelTipsRight { get; }

        private TableLayout(TableLayoutKind kind, Vector2Int dealer, Vector2Int counter, Vector2Int panel, int kitX, int kitY, bool kitFromBottom,
            RectInt barFloat, bool dealerTipsLeft, bool panelTipsRight)
        {
            Kind = kind;
            Dealer = dealer;
            Counter = counter;
            Panel = panel;
            KitX = kitX;
            KitY = kitY;
            KitFromBottom = kitFromBottom;
            Bar = new Vector2Int(dealer.x + BarInset, dealer.y + BarTop);
            BarFloat = barFloat;
            DealerPoint = new Vector2Int(dealer.x + Column / 2, dealer.y + Column / 2);
            DealerTipsLeft = dealerTipsLeft;
            PanelTipsRight = panelTipsRight;
        }

        /// <summary>The bar's place in the demon's column: under the name (the line under it), inset from the box.</summary>
        public const int BarInset = 4, BarTop = 118, BarWidth = 92, BarHeight = 7;

        /// <summary>The gap between the kit and the panel under it.</summary>
        private const int KitGap = 3;

        public static TableLayout For(TableLayoutKind kind) => kind == TableLayoutKind.Chapter ? Chapter() : Simulator();

        private static TableLayout Simulator()
        {
            var dealer = new Vector2Int(LeftX, Edge);
            int floatX = LeftX + Column + Edge;
            return new TableLayout(TableLayoutKind.Simulator, dealer, new Vector2Int(RightX, Edge), new Vector2Int(RightX, 62),
                LeftX, Edge + DealerView.Height + KitGap, false,
                new RectInt(floatX, dealer.y + BarTop - 2, Middle - floatX - 2, 8), false, false);
        }

        private static TableLayout Chapter()
        {
            var dealer = new Vector2Int(RightX, Edge);
            var panel = new Vector2Int(LeftX, BottomY - Edge - PlayerPanelView.Height);
            int floatX = Middle + MiddleWidth + 2;
            return new TableLayout(TableLayoutKind.Chapter, dealer, new Vector2Int(LeftX, Edge), panel,
                LeftX, panel.y - KitGap, true,
                new RectInt(floatX, dealer.y + BarTop - 2, RightX - floatX - 2, 8), true, true);
        }
    }
}
