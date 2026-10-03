using HellPoker.Core.Game;
using HellPoker.Presentation.Abstractions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HellPoker.Presentation
{
    /// <summary>
    /// Keyboard shortcuts: Esc one screen up, Alt+Enter full screen; at the table Space/Enter main action, pass or call,
    /// R raise, D check to draw, C call, F fold, 1-5 pick cards, H hand ranks; L the language, on every screen. While the table animates any of them hurries the animation instead.
    /// Nothing is read while a screen change plays.
    /// </summary>
    public sealed class KeyboardInput : MonoBehaviour
    {
        private ITableCommands _table;
        private IMenuCommands _menu;
        private ISettingsCommands _settings;

        public void Bind(ITableCommands table, IMenuCommands menu, ISettingsCommands settings)
        {
            _table = table;
            _menu = menu;
            _settings = settings;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || _table == null || _menu == null) return;

            bool enter = keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
            bool alt = keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed;
            if (enter && alt)
            {
                _settings?.ToggleFullscreen();
                return;
            }

            if (_menu.IsTransitioning) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                // A panel over the table closes first; only then does Esc leave the table.
                if (_menu.IsMenuOpen || !_table.CloseOverlay())
                    _menu.GoBack();
                return;
            }

            if (keyboard.lKey.wasPressedThisFrame)
            {
                _settings?.CycleLanguage();
                return;
            }

            if (_menu.IsMenuOpen) return;

            if (keyboard.hKey.wasPressedThisFrame)
            {
                _table.ToggleHandRanks();
                return;
            }

            // One action per frame at most: Space and Enter together still count once.
            if (keyboard.spaceKey.wasPressedThisFrame || enter)
                _table.PerformAction();
            else if (keyboard.rKey.wasPressedThisFrame) _table.Bet(BetAction.Raise);
            else if (keyboard.dKey.wasPressedThisFrame) _table.CheckToDraw();
            else if (keyboard.cKey.wasPressedThisFrame) _table.Bet(BetAction.Call);
            else if (keyboard.fKey.wasPressedThisFrame) _table.Bet(BetAction.Fold);
            else if (keyboard.digit1Key.wasPressedThisFrame) _table.ToggleDiscard(0);
            else if (keyboard.digit2Key.wasPressedThisFrame) _table.ToggleDiscard(1);
            else if (keyboard.digit3Key.wasPressedThisFrame) _table.ToggleDiscard(2);
            else if (keyboard.digit4Key.wasPressedThisFrame) _table.ToggleDiscard(3);
            else if (keyboard.digit5Key.wasPressedThisFrame) _table.ToggleDiscard(4);
        }
    }
}
