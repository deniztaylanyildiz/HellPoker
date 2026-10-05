using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HellPoker.Presentation
{
    /// <summary>
    /// The game's window comes to the front and takes the focus when the game opens (Windows players only; the editor and
    /// other platforms leave it to the system). A window that opened behind another one would otherwise sit there unnoticed.
    /// </summary>
    public static class WindowFocus
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr window, int command);
        [DllImport("user32.dll")] private static extern bool BringWindowToTop(IntPtr window);
        [DllImport("user32.dll")] private static extern IntPtr GetActiveWindow();

        private const int Show = 5;   // SW_SHOW: shows it as it is (SW_RESTORE could undo a borderless full screen)
#endif

        /// <summary>Brings the game's window to the front, once; any failure is only a warning.</summary>
        public static void BringToFront()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            try
            {
                IntPtr window = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                if (window == IntPtr.Zero) window = GetActiveWindow();
                if (window == IntPtr.Zero) return;
                ShowWindow(window, Show);
                BringWindowToTop(window);
                SetForegroundWindow(window);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Hell Poker: the window could not be brought to the front ({exception.Message}).");
            }
#endif
        }
    }
}
