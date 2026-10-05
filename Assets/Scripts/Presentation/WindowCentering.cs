using System;
using System.Collections;
using System.Runtime.InteropServices;
using HellPoker.Presentation.Settings;
using UnityEngine;

namespace HellPoker.Presentation
{
    /// <summary>
    /// Centres the game's window in its display's work area once a resize has happened. Screen.SetResolution only takes effect at the
    /// end of the frame, and the window's real size includes its title bar and borders — measured here (Windows: the visible frame,
    /// DWM's extended frame bounds) — so the move waits two frames and centres the whole frame, never letting it out of the work area
    /// (the taskbar stays clear). Measured on a 125% display at ×2 / ×3: without the frame the window sat 39 px low.
    /// </summary>
    public sealed class WindowCentering : MonoBehaviour
    {
        private static WindowCentering _runner;
        private Coroutine _pending;

        /// <summary>After the next resize to a <paramref name="width"/>×<paramref name="height"/> client area, centre the window.</summary>
        public static void Schedule(int width, int height)
        {
            if (_runner == null)
            {
                var host = new GameObject("WindowCentering") { hideFlags = HideFlags.HideAndDontSave };
                DontDestroyOnLoad(host);
                _runner = host.AddComponent<WindowCentering>();
            }
            if (_runner._pending != null) _runner.StopCoroutine(_runner._pending);
            _runner._pending = _runner.StartCoroutine(_runner.CenterSoon(width, height));
        }

        private IEnumerator CenterSoon(int width, int height)
        {
            yield return null;   // the resize lands at the end of a frame
            yield return null;
            _pending = null;
            try
            {
                DisplayInfo display = Screen.mainWindowDisplayInfo;
                RectInt area = display.workArea.width > 0 ? display.workArea : new RectInt(0, 0, display.width, display.height);
                (int frameWidth, int frameHeight) = OuterSize(width, height);
                (int x, int y) = WindowScales.Centered(area.x, area.y, area.width, area.height, frameWidth, frameHeight);
                Screen.MoveMainWindowTo(display, new Vector2Int(x, y));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Hell Poker: the window could not be centred ({exception.Message}).");
            }
        }

        /// <summary>The window's whole size: the client area plus its title bar and borders (the client area alone elsewhere).</summary>
        private static (int width, int height) OuterSize(int width, int height)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            IntPtr window = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            if (window != IntPtr.Zero && GetClientRect(window, out Rect client) &&
                DwmGetWindowAttribute(window, ExtendedFrameBounds, out Rect frame, Marshal.SizeOf(typeof(Rect))) == 0)
            {
                int chromeWidth = Math.Max(0, (frame.Right - frame.Left) - (client.Right - client.Left));
                int chromeHeight = Math.Max(0, (frame.Bottom - frame.Top) - (client.Bottom - client.Top));
                return (width + chromeWidth, height + chromeHeight);
            }
#endif
            return (width, height);
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private const int ExtendedFrameBounds = 9;

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left, Top, Right, Bottom;
        }

        [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr window, out Rect rect);
        [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out Rect rect, int size);
#endif
    }
}
