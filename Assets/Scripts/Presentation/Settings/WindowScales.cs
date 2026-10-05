using System;
using System.Collections.Generic;

namespace HellPoker.Presentation.Settings
{
    /// <summary>
    /// The window sizes a display offers: whole multiples of 480×270 that fit on it (with room for the title bar and the
    /// taskbar), from ×2 up; and the automatic size (the largest multiple that fits comfortably — the old behaviour).
    /// Pure arithmetic on the display's size, so tests can hand it any display.
    /// </summary>
    public static class WindowScales
    {
        public const int Width = 480;
        public const int Height = 270;

        /// <summary>The smallest size offered (×1 is a postage stamp).</summary>
        public const int Smallest = 2;

        /// <summary>A window may take this share of the display's height (the title bar and the taskbar take the rest).</summary>
        public const float FitShare = 0.92f;

        /// <summary>The automatic window: this share of the display, either way.</summary>
        public const float AutoShare = 0.85f;

        /// <summary>The largest multiple that fits on the display (0 when not even ×1 does).</summary>
        public static int Largest(int displayWidth, int displayHeight)
        {
            if (displayWidth <= 0 || displayHeight <= 0) return 0;
            return Math.Max(0, Math.Min(displayWidth / Width, (int)(displayHeight * FitShare) / Height));
        }

        /// <summary>What the window size row cycles through: automatic (<see cref="GameSettings.AutoWindowScale"/>), then ×2 up to the
        /// largest that fits. A multiple that does not fit is never in the list.</summary>
        public static IReadOnlyList<int> Choices(int displayWidth, int displayHeight)
        {
            var choices = new List<int> { GameSettings.AutoWindowScale };
            for (int scale = Smallest; scale <= Largest(displayWidth, displayHeight); scale++)
                choices.Add(scale);
            return choices;
        }

        /// <summary>The automatic multiple: the largest within <see cref="AutoShare"/> of the display, at least ×1.</summary>
        public static int Auto(int displayWidth, int displayHeight) =>
            Math.Max(1, Math.Min((int)(displayWidth * AutoShare) / Width, (int)(displayHeight * AutoShare) / Height));

        /// <summary>The multiple a window really opens at: the saved one if this display fits it, otherwise automatic.</summary>
        public static int Resolve(int saved, int displayWidth, int displayHeight) =>
            saved >= Smallest && saved <= Largest(displayWidth, displayHeight) ? saved : Auto(displayWidth, displayHeight);
    }
}
