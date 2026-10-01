using HellPoker.Core.Game;
using HellPoker.Presentation.Abstractions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HellPoker.Presentation
{
    /// <summary>
    /// Keyboard shortcuts: Esc menu; at the table Space/Enter main action, pass or call, R raise, C call, F fold, 1-5 pick cards.
    /// </summary>
    public sealed class KeyboardInput : MonoBehaviour
    {
        private ITableCommands _table;
        private IMenuCommands _menu;

        public void Bind(ITableCommands table, IMenuCommands menu)
        {
            _table = table;
            _menu = menu;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || _table == null || _menu == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                _menu.ToggleMenu();
                return;
            }

            if (_menu.IsMenuOpen) return;

            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                _table.PerformAction();

            if (keyboard.rKey.wasPressedThisFrame) _table.Bet(BetAction.Raise);
            if (keyboard.cKey.wasPressedThisFrame) _table.Bet(BetAction.Call);
            if (keyboard.fKey.wasPressedThisFrame) _table.Bet(BetAction.Fold);

            if (keyboard.digit1Key.wasPressedThisFrame) _table.ToggleDiscard(0);
            if (keyboard.digit2Key.wasPressedThisFrame) _table.ToggleDiscard(1);
            if (keyboard.digit3Key.wasPressedThisFrame) _table.ToggleDiscard(2);
            if (keyboard.digit4Key.wasPressedThisFrame) _table.ToggleDiscard(3);
            if (keyboard.digit5Key.wasPressedThisFrame) _table.ToggleDiscard(4);
        }
    }
}
