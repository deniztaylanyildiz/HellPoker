using HellPoker.Core.Game;
using HellPoker.Presentation.Abstractions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HellPoker.Presentation
{
    /// <summary>
    /// Keyboard shortcuts: Esc one screen up, Alt+Enter full screen; at the table Space/Enter main action, pass or call,
    /// R raise, D check to draw, S shuffle (between hands), K the class's power (the King: protect a card), C call, F fold, 1-5 pick cards, H hand ranks, the arrows the joker picker; L the language, on the title menu only;
    /// Q / E or the arrows switch the settings' tabs. While the table animates any of them hurries the animation instead.
    /// Nothing is read while a screen change plays.
    /// </summary>
    public sealed class KeyboardInput : MonoBehaviour
    {
        private ITableCommands _table;
        private IMenuCommands _menu;
        private ISettingsCommands _settings;

        /// <summary>Phase 2: the chapter's map and panels, and the chapter's own table (null: none).</summary>
        private IChapterCommands _chapters;

        public void Bind(ITableCommands table, IMenuCommands menu, ISettingsCommands settings, IChapterCommands chapters = null)
        {
            _table = table;
            _menu = menu;
            _settings = settings;
            _chapters = chapters;
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

            // The chapter's map and its panels: the arrows pick, Enter / Space go, Esc closes what it can (else the menu).
            if (_chapters != null && _chapters.IsMapOpen && !_menu.IsMenuOpen)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    if (!_chapters.Back()) _menu.GoBack();
                }
                else if (keyboard.spaceKey.wasPressedThisFrame || enter) _chapters.Confirm();
                else if (keyboard.leftArrowKey.wasPressedThisFrame) _chapters.Step(-1, 0);
                else if (keyboard.rightArrowKey.wasPressedThisFrame) _chapters.Step(1, 0);
                else if (keyboard.upArrowKey.wasPressedThisFrame) _chapters.Step(0, -1);
                else if (keyboard.downArrowKey.wasPressedThisFrame) _chapters.Step(0, 1);
                return;
            }
            // At one of the chapter's tables every table key goes to that table.
            ITableCommands table = _chapters != null && _chapters.IsAtTable && _chapters.Table != null ? _chapters.Table : _table;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                // A panel over the table closes first; only then does Esc leave the table.
                if (_menu.IsMenuOpen || !table.CloseOverlay())
                    _menu.GoBack();
                return;
            }

            // The settings' tabs: Q / E or the arrows walk them round.
            if (_settings != null && _settings.IsSettingsOpen)
            {
                if (keyboard.qKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) _settings.PreviousTab();
                else if (keyboard.eKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) _settings.NextTab();
                return;
            }

            // The language changes on the title menu only — never at the table, under a warning or on a sub-screen.
            if (keyboard.lKey.wasPressedThisFrame && _menu.IsAtMenuRoot)
            {
                _settings?.CycleLanguage();
                return;
            }

            if (_menu.IsMenuOpen) return;

            if (keyboard.hKey.wasPressedThisFrame)
            {
                table.ToggleHandRanks();
                return;
            }

            // One action per frame at most: Space and Enter together still count once.
            if (keyboard.spaceKey.wasPressedThisFrame || enter)
                table.PerformAction();
            else if (keyboard.rKey.wasPressedThisFrame) table.Bet(BetAction.Raise);
            else if (keyboard.dKey.wasPressedThisFrame) table.CheckToDraw();
            else if (keyboard.kKey.wasPressedThisFrame) table.UsePower();
            else if (keyboard.sKey.wasPressedThisFrame) table.ShuffleDeck();
            else if (keyboard.cKey.wasPressedThisFrame) table.Bet(BetAction.Call);
            else if (keyboard.fKey.wasPressedThisFrame) table.Bet(BetAction.Fold);
            else if (keyboard.digit1Key.wasPressedThisFrame) table.ToggleDiscard(0);
            else if (keyboard.digit2Key.wasPressedThisFrame) table.ToggleDiscard(1);
            else if (keyboard.digit3Key.wasPressedThisFrame) table.ToggleDiscard(2);
            else if (keyboard.digit4Key.wasPressedThisFrame) table.ToggleDiscard(3);
            else if (keyboard.digit5Key.wasPressedThisFrame) table.ToggleDiscard(4);
            // The joker picker at the showdown: left / right the rank, up / down the suit (nothing happens without a picker).
            else if (keyboard.leftArrowKey.wasPressedThisFrame) table.StepJoker(-1, 0);
            else if (keyboard.rightArrowKey.wasPressedThisFrame) table.StepJoker(1, 0);
            else if (keyboard.upArrowKey.wasPressedThisFrame) table.StepJoker(0, 1);
            else if (keyboard.downArrowKey.wasPressedThisFrame) table.StepJoker(0, -1);
        }
    }
}
