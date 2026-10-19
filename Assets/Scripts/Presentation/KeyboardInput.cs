using HellPoker.Core.Game;
using HellPoker.Presentation.Abstractions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HellPoker.Presentation
{
    /// <summary>
    /// Keyboard shortcuts: Esc one screen up, Alt+Enter full screen; at the table Space/Enter main action, pass or call,
    /// R raise, D check to draw, S shuffle (between hands), K the class's power (the King: protect a card), C call, F fold, 1-5 pick cards, H hand ranks, the arrows the joker picker;
    /// Q / E or the left / right arrows switch the settings' tabs, up / down and Enter pick in its LANGUAGE tab (Demo 3.3: no L key any more);
    /// on the title menu the arrows move its cursor and Enter / Space press the button under it. While the table animates any of them hurries the animation instead.
    /// Nothing is read while a screen change plays.
    /// </summary>
    public sealed class KeyboardInput : MonoBehaviour
    {
        private ITableCommands _table;
        private IMenuCommands _menu;
        private ISettingsCommands _settings;

        /// <summary>Phase 2: the chapter's map and panels, and the chapter's own table (null: none).</summary>
        private IChapterCommands _chapters;

        /// <summary>The bug report box: while it is open the keys are its text's (Esc closes it).</summary>
        private IFeedbackCommands _feedback;

        public void Bind(ITableCommands table, IMenuCommands menu, ISettingsCommands settings, IChapterCommands chapters = null,
            IFeedbackCommands feedback = null)
        {
            _feedback = feedback;
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

            if (_feedback != null && _feedback.IsFeedbackOpen)
            {
                if (keyboard.escapeKey.wasPressedThisFrame) _feedback.CloseFeedback();
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
                else if (_settings.IsLanguageTab && keyboard.upArrowKey.wasPressedThisFrame) _settings.StepLanguage(-1);
                else if (_settings.IsLanguageTab && keyboard.downArrowKey.wasPressedThisFrame) _settings.StepLanguage(1);
                else if (_settings.IsLanguageTab && (enter || keyboard.spaceKey.wasPressedThisFrame)) _settings.ConfirmLanguage();
                return;
            }

            // How to Play's pages: the arrows or Q / E turn them.
            if (_menu.IsShowingRules)
            {
                if (keyboard.qKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) _menu.TurnRulesPage(-1);
                else if (keyboard.eKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) _menu.TurnRulesPage(1);
                return;
            }

            // F9 on the title menu: the Boss Simulator (the old demo, a developer's mode) — or back to the game.
            if (keyboard.f9Key.wasPressedThisFrame && _menu.IsAtMenuRoot)
            {
                _menu.ToggleSimulator();
                return;
            }

            // Demo 3.3: the title menu's cursor — the arrows move it, Enter / Space press the button under it.
            if (_menu.IsAtMenuRoot)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame) _menu.MoveMenuSelection(-1, 0);
                else if (keyboard.rightArrowKey.wasPressedThisFrame) _menu.MoveMenuSelection(1, 0);
                else if (keyboard.upArrowKey.wasPressedThisFrame) _menu.MoveMenuSelection(0, -1);
                else if (keyboard.downArrowKey.wasPressedThisFrame) _menu.MoveMenuSelection(0, 1);
                else if (keyboard.spaceKey.wasPressedThisFrame || enter) _menu.PressMenuSelection();
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
