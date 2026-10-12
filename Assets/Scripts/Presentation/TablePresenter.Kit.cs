using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Chapters;
using HellPoker.Core.Game;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;

namespace HellPoker.Presentation
{
    /// <summary>
    /// Phase 2's class mechanics at a chapter's table (the Peasant's satchel, the Warlock's prophecy, the King's treasury) and the
    /// demon's rule's words. Nothing here runs at a demo table: <see cref="_kit"/> is null there.
    /// </summary>
    public sealed partial class TablePresenter
    {
        /// <summary>The chapter table's hooks (the class's acts, the demon's rule); null at a demo table.</summary>
        private ChapterHooks _kit;

        /// <summary>The satchel cards picked to go into this draw.</summary>
        private readonly List<Card> _bagTakes = new List<Card>();

        /// <summary>A seizure waits for the card the King gives (paid with the full gauge when <see cref="_seizeCharged"/>).</summary>
        private bool _seizing, _seizeCharged;

        /// <summary>The taxes the King had lost when the hand was dealt (the line about half of them gone).</summary>
        private int _taxesLostAtDeal;

        /// <summary>The demon said he is cornered (once a table).</summary>
        private bool _corneredSaid;

        /// <summary>The chapter table's hooks, for tests and screenshots.</summary>
        public ChapterHooks Kit => _kit;

        private HellPokerGame KitGame => _game as HellPokerGame;

        private void ClearKit()
        {
            _kit = null;
            _bagTakes.Clear();
            _seizing = false;
            _view.SetKit(KitDisplay.Hidden);
        }

        private void SeatKit(IHellPokerGame game)
        {
            ClearKit();
            _kit = (game as HellPokerGame)?.Hooks as ChapterHooks;
            _corneredSaid = false;
            _soulWarned = false;
            _drawSoundRound = _resultSoundRound = _barRoundShown = -1;
            _taxesLostAtDeal = _kit?.Kit.TaxesLost ?? 0;
        }

        private void KitHandDealt()
        {
            _bagTakes.Clear();
            _seizing = false;
            if (_kit == null) return;
            _taxesLostAtDeal = _kit.Kit.TaxesLost;
            if (_kit.Prophecy != null) _view.PlaySfx(SfxIds.ProphecySee);   // a new hand: the Warlock looks at the deck's top again
            if (_kit.Rule != null && !_corneredSaid && _kit.Rule.IsCornered(KitGame))
            {
                _corneredSaid = true;
                Say(_ => UiText.CorneredLine, DealerMood.Menacing);
            }
        }

        // ------------------------------------------------------------------ the draw (the satchel's cards go in)

        /// <summary>DRAW at a chapter's table: the satchel's picked cards fill thrown places. False when the draw cannot be made (said why).</summary>
        private bool KitDraw()
        {
            int[] discards = _discards.OrderBy(i => i).ToArray();
            if (_kit?.Satchel == null || _bagTakes.Count == 0)
            {
                _game.Draw(discards);
                return true;
            }
            if (_bagTakes.Count > discards.Length)
            {
                _view.SetMessage(UiText.SatchelTooMany, Tone.Warning);
                return false;
            }
            var fill = new Dictionary<int, Card>();
            for (int k = 0; k < _bagTakes.Count; k++) fill[discards[k]] = _bagTakes[k];
            if (!KitGame.CanDraw(discards, fill, out _))
            {
                _bagTakes.Clear();
                Refresh();
                return false;
            }
            LogNote($"satchel: {string.Join(" ", _bagTakes)} into the draw (hand {_game.RoundNumber}; the demon sees it)");
            _view.PlaySfx(SfxIds.SatchelTake);
            KitGame.Draw(discards, fill);
            _bagTakes.Clear();
            return true;
        }

        // ------------------------------------------------------------------ K: the class's charged power

        private void UseKitPower()
        {
            HellPokerGame game = KitGame;
            int charge = _sinner.Charge, full = _sinner.Rules.Full;
            switch (_kit.Powers)
            {
                case SatchelPowers satchel:
                {
                    KitRefusal why = satchel.WhyNoHarvest(game, _discards);
                    if (why != KitRefusal.None)
                    {
                        _view.SetMessage(UiText.KitRefused(why, charge, full), Tone.Warning);
                        return;
                    }
                    TableState before = CaptureState();
                    _view.PlaySfx(SfxIds.Deal);
                    Hand playerBefore = _game.PlayerHand, houseBefore = _game.HouseHand;
                    satchel.Harvest(game, _discards.OrderBy(i => i).ToArray());
                    LogNote($"power: HARVEST (hand {_game.RoundNumber})");
                    _view.PlaySfx(SfxIds.SatchelTake);
                    _discards.Clear();
                    _bagTakes.Clear();
                    LogJokers("drawn", NewCards(playerBefore, _game.PlayerHand), NewCards(houseBefore, _game.HouseHand));
                    PlayOutSealedHand(before);
                    PlayCheatStrikes(before);
                    Refresh();
                    _view.SetMessage(UiText.HarvestDone, Tone.Good);
                    return;
                }
                case ProphecyPowers prophecy:
                {
                    KitRefusal why = prophecy.WhyNoRearrange(game);
                    if (why != KitRefusal.None)
                    {
                        _view.SetMessage(UiText.KitRefused(why, charge, full), Tone.Warning);
                        return;
                    }
                    prophecy.StartRearranging(game);
                    LogNote($"power: the Warlock rearranges the deck (hand {_game.RoundNumber})");
                    Refresh();
                    _view.SetMessage(string.Format(UiText.RearrangePromptFormat, prophecy.RearrangesLeft), Tone.Good);
                    return;
                }
                case TreasuryPowers treasury:
                {
                    if (_seizing && _seizeCharged)
                    {
                        _seizing = false;   // K again: put away
                        Refresh();
                        return;
                    }
                    KitRefusal why = treasury.WhyNot(game, TaxAct.Seize, charged: true);
                    if (why != KitRefusal.None)
                    {
                        _view.SetMessage(why == KitRefusal.NoTaxes ? UiText.KitRefused(KitRefusal.NotCharged, charge, full) : KitRefusalText(why), Tone.Warning);
                        return;
                    }
                    _seizing = true;
                    _seizeCharged = true;
                    Refresh();
                    _view.SetMessage(UiText.SeizePrompt, Tone.Warning);
                    return;
                }
            }
        }

        private string KitRefusalText(KitRefusal why, int price = 0) =>
            why == KitRefusal.NotNow && _kit?.Treasury != null ? UiText.KitNotBeforeDraw
                : UiText.KitRefused(why, _sinner.Charge, _sinner.Rules.Full, _kit?.Kit.Taxes ?? 0, price);

        // ------------------------------------------------------------------ the panel's cards and buttons

        public void PressKitCard(int index)
        {
            if (!Playing || _kit == null || _pendingEvent != null || Hurry()) return;
            HellPokerGame game = KitGame;
            if (_kit.Satchel != null)
            {
                SatchelPowers satchel = _kit.Satchel;
                IReadOnlyList<Card> offered = satchel.Offered;
                if (offered.Count > 0 && game.IsAfterDraw)
                {
                    if (index < 0 || index >= offered.Count) return;
                    Card card = offered[index];
                    if (satchel.Keep(game, card))
                    {
                        LogNote($"satchel: keeps {card} (hand {_game.RoundNumber})");
                        _view.PlaySfx(SfxIds.SatchelPut);
                        Refresh();
                        _view.SetMessage(string.Format(UiText.SatchelKeptFormat, card), Tone.Good);
                    }
                    return;
                }
                IReadOnlyList<Card> bag = _kit.Kit.Satchel;
                if (index < 0 || index >= bag.Count) return;
                if (_game.Phase != GamePhase.Drawing)
                {
                    _view.SetMessage(UiText.SatchelOnlyInDraw, Tone.Warning);
                    return;
                }
                if (!_bagTakes.Remove(bag[index])) _bagTakes.Add(bag[index]);
                Refresh();
                if (_bagTakes.Count > 0) _view.SetMessage(string.Format(UiText.SatchelTakeFormat, _bagTakes.Count), Tone.Warning);
                return;
            }
            if (_kit.Prophecy != null)
            {
                ProphecyPowers prophecy = _kit.Prophecy;
                if (prophecy.RearrangesLeft <= 0)
                {
                    _view.SetMessage(UiText.KitProphecyHint, Tone.Neutral);
                    return;
                }
                IReadOnlyList<Card> top = prophecy.TopCards(game);
                if (index < 0 || index >= top.Count) return;
                Card card = top[index];
                if (!prophecy.SendToBottom(game, index)) return;
                LogNote($"prophecy: {card} to the bottom (hand {_game.RoundNumber})");
                _view.PlaySfx(SfxIds.ProphecyBottom);
                Refresh();
                _view.SetMessage(string.Format(UiText.RearrangedFormat, card), Tone.Good);
            }
        }

        public void PressKitButton(int index)
        {
            if (!Playing || _kit?.Treasury == null || _pendingEvent != null || Hurry()) return;
            HellPokerGame game = KitGame;
            TreasuryPowers treasury = _kit.Treasury;
            TaxAct act = index == 0 ? TaxAct.Reveal : index == 1 ? TaxAct.FreeRaise : TaxAct.Seize;
            if (act == TaxAct.Seize && _seizing && !_seizeCharged)
            {
                _seizing = false;   // the button again: put away
                Refresh();
                return;
            }
            KitRefusal why = treasury.WhyNot(game, act);
            if (why != KitRefusal.None)
            {
                _view.SetMessage(KitRefusalText(why, treasury.PriceOf(act)), Tone.Warning);
                return;
            }
            switch (act)
            {
                case TaxAct.Reveal:
                    treasury.Reveal(game);
                    LogNote($"treasury: a card of the demon's turns (hand {_game.RoundNumber}, taxes {_kit.Kit.Taxes})");
                    _view.PlaySfx(SfxIds.TaxSpend);
                    _view.PlaySfx(SfxIds.Flip);
                    Refresh();
                    _view.SetMessage(UiText.RevealedMessage, Tone.Good);
                    return;
                case TaxAct.FreeRaise:
                    treasury.FreeRaise(game);
                    LogNote($"treasury: a free raise (hand {_game.RoundNumber}, taxes {_kit.Kit.Taxes})");
                    _view.PlaySfx(SfxIds.TaxSpend);
                    Refresh();
                    _view.SetMessage(UiText.FreeRaiseMessage, Tone.Good);
                    return;
                default:
                    _seizing = true;
                    _seizeCharged = false;
                    Refresh();
                    _view.SetMessage(UiText.SeizePrompt, Tone.Warning);
                    return;
            }
        }

        private int[] SeizablePlayerCards()
        {
            if (_kit?.Treasury == null || KitGame == null) return null;
            return Enumerable.Range(0, Hand.Size).Where(i => _kit.Treasury.CanGive(KitGame, i)).ToArray();
        }

        private void SeizeWith(int index)
        {
            HellPokerGame game = KitGame;
            Card given = _game.PlayerHand[index];
            Card? taken = _kit.Treasury.Seize(game, index, _seizeCharged);
            if (taken == null)
            {
                _view.SetMessage(UiText.SeizePrompt, Tone.Warning);   // not that card: the pick stays open
                return;
            }
            _seizing = false;
            _discards.Remove(index);
            LogNote($"treasury: SEIZURE {taken} for {given}{(_seizeCharged ? " (the charged power)" : "")} (hand {_game.RoundNumber})");
            _view.PlaySfx(SfxIds.Seize);
            Refresh();
            _view.SetMessage(string.Format(UiText.SeizedFormat, taken.Value, given), Tone.Good);
        }

        // ------------------------------------------------------------------ what the panel and the lines show

        /// <summary>The line under the message while a kit pick is under way; null otherwise.</summary>
        private string KitHint()
        {
            if (_kit == null) return null;
            if (_seizing) return UiText.SeizePrompt;
            if (_kit.Prophecy != null && _kit.Prophecy.RearrangesLeft > 0)
                return string.Format(UiText.RearrangePromptFormat, _kit.Prophecy.RearrangesLeft);
            if (_kit.Rule is BelialRoleSwap && _kit.Prophecy != null && _game.Phase == GamePhase.Betting && BelialRoleSwap.ComingIn(KitGame))
                return UiText.RoleSwapComing;
            return null;
        }

        private void ShowKit()
        {
            if (_kit == null || _kit.Powers == null)
            {
                _view.SetKit(KitDisplay.Hidden);
                return;
            }
            HellPokerGame game = KitGame;
            ClassKit kit = _kit.Kit;
            switch (_kit.Powers)
            {
                case SatchelPowers satchel:
                {
                    IReadOnlyList<Card> offered = satchel.Offered;
                    if (offered.Count > 0 && game.IsAfterDraw)
                    {
                        _view.SetKit(new KitDisplay(UiText.KitKeepTitle, offered.Select(c => new KitCard(c, clickable: true)).ToArray(), null,
                            UiText.KitSatchelHint, waiting: true));
                        return;
                    }
                    bool drawing = _game.Phase == GamePhase.Drawing;
                    _view.SetKit(new KitDisplay(string.Format(UiText.KitSatchelTitleFormat, kit.Satchel.Count, kit.SatchelCapacity),
                        kit.Satchel.Select(c => new KitCard(c, _bagTakes.Contains(c), drawing)).ToArray(), null, UiText.KitSatchelHint));
                    return;
                }
                case ProphecyPowers prophecy:
                {
                    bool coming = game.Phase == GamePhase.Betting && game.DeckCount < game.CardsForAHand;
                    if (coming)
                    {
                        _view.SetKit(new KitDisplay(UiText.KitShuffleComing, null, null, UiText.KitProphecyHint));
                        return;
                    }
                    bool rearranging = prophecy.RearrangesLeft > 0;
                    string title = rearranging ? string.Format(UiText.KitRearrangeTitleFormat, prophecy.RearrangesLeft) : UiText.KitProphecyTitle;
                    _view.SetKit(new KitDisplay(title, prophecy.TopCards(game).Select(c => new KitCard(c, clickable: rearranging)).ToArray(), null,
                        UiText.KitProphecyHint, waiting: rearranging));
                    return;
                }
                case TreasuryPowers treasury:
                {
                    KitButton Button(TaxAct act, string format) =>
                        new KitButton(string.Format(format, treasury.PriceOf(act)), treasury.WhyNot(game, act) != KitRefusal.None
                            && !(act == TaxAct.Seize && treasury.WhyNot(game, act, charged: true) == KitRefusal.None));
                    _view.SetKit(new KitDisplay(string.Format(UiText.KitTreasuryTitleFormat, kit.Taxes, kit.TaxCapacity), null, new[]
                    {
                        Button(TaxAct.Reveal, UiText.KitRevealButtonFormat), Button(TaxAct.FreeRaise, UiText.KitFreeRaiseButtonFormat),
                        Button(TaxAct.Seize, UiText.KitSeizeButtonFormat)
                    }, UiText.KitTreasuryHint, waiting: _seizing));
                    return;
                }
                default:
                    _view.SetKit(KitDisplay.Hidden);
                    return;
            }
        }

        /// <summary>The draw's prompt, with what the demon's rule did as the draw began (Belial's swap, Lucifer's seizure).</summary>
        private string WithDrawLines(string prompt)
        {
            switch (_kit?.Rule)
            {
                case BelialRoleSwap swap when swap.Given.HasValue:
                    return prompt + "\n" + string.Format(UiText.RoleSwapFormat, swap.Taken.Value, swap.Given.Value);
                case LuciferMirror mirror when mirror.Seized.HasValue:
                    return prompt + "\n" + string.Format(UiText.MirrorSeizedFormat, mirror.Seized.Value, mirror.GivenBack.Value);
                default:
                    return prompt;
            }
        }

        /// <summary>The result, with the demon's rule's line (interest, temptation, the mirror) and the King's taxes lost.</summary>
        private string WithKitLines(string message, RoundResult round)
        {
            if (_kit == null || round == null) return message;
            var lines = new List<string>();
            switch (_kit.Rule)
            {
                case MammonInterest interest when interest.StruckThisHand && KitGame.HookYearsThisHand > 0:
                    lines.Add(string.Format(UiText.InterestFormat, MammonInterest.InterestPercent));
                    break;
                case LilithTemptation temptation when temptation.Tempted.HasValue:
                    lines.Add(string.Format(UiText.TemptedFormat, temptation.Tempted.Value));
                    break;
                case LuciferMirror mirror when mirror.StruckThisHand && !mirror.Seized.HasValue:
                    lines.Add(mirror.Mirrors == KitKind.Satchel ? UiText.MirrorSatchel : mirror.SentDown ? UiText.MirrorSentDown : UiText.MirrorTopCard);
                    break;
            }
            if (_kit.Treasury != null && _kit.Kit.TaxesLost > _taxesLostAtDeal)
                lines.Add(_kit.Kit.Has(UpgradeIds.CrownGuard) ? UiText.TaxesQuartered : UiText.TaxesHalved);
            if (_kit.Upgrades?.LaughedThisHand == true) lines.Add(UiText.DoubleLaughLine);
            return lines.Count == 0 ? message : message + "\n" + string.Join("\n", lines);
        }

        // ------------------------------------------------------------------ the sounds of a chapter's table

        /// <summary>A coin win or loss at least this many antes rings fuller.</summary>
        private const int BigCoinAntes = 4;

        /// <summary>The bar this close to its line (percent) sounds the soul's warning (again once it has fallen back under the rearm line).</summary>
        private const int SoulWarningPercent = 85, SoulWarningRearmPercent = 70;

        private bool _soulWarned;
        private int _drawSoundRound = -1, _resultSoundRound = -1;

        /// <summary>A floor hand's coins: won or lost, a few or many.</summary>
        private void PlayCoinSound(RoundResult round)
        {
            int coins = _seat?.LastHand?.Coins ?? -round.YearsChange;
            if (coins == 0) return;
            bool big = Math.Abs(coins) >= BigCoinAntes * Math.Max(1, _game.Ante);
            _view.PlaySfx(coins > 0 ? (big ? SfxIds.CoinWinBig : SfxIds.CoinWin) : (big ? SfxIds.CoinLossBig : SfxIds.CoinLoss));
        }

        /// <summary>As the draw begins (once a hand): a demon's rule that struck before it (Belial's swap, Lucifer's seizure).</summary>
        private void KitDrawSounds()
        {
            if (_kit == null || _relabelling || _drawSoundRound == _game.RoundNumber) return;
            _drawSoundRound = _game.RoundNumber;
            if (_kit.Rule is BelialRoleSwap swap && swap.Given.HasValue) _view.PlaySfx(SfxIds.RuleSwap);
            if (_kit.Rule is LuciferMirror mirror && mirror.Seized.HasValue) _view.PlaySfx(SfxIds.RuleMirror);
        }

        /// <summary>A settled hand's sounds of the chapter: the demon's rule (interest, temptation, the mirror), the King's tax, the bar.</summary>
        private void KitResultSounds(RoundResult round)
        {
            if (_kit == null || _resultSoundRound == _game.RoundNumber) return;
            _resultSoundRound = _game.RoundNumber;
            switch (_kit.Rule)
            {
                case LilithTemptation t when t.Tempted.HasValue: _view.PlaySfx(SfxIds.RuleTemptation); break;
                case LuciferMirror m when m.StruckThisHand && !m.Seized.HasValue: _view.PlaySfx(SfxIds.RuleMirror); break;
            }
            if (_kit.Treasury != null && round.Showdown?.Outcome == ShowdownOutcome.PlayerWins && !round.Folded) _view.PlaySfx(SfxIds.TaxGain);
            if (!BarMode || round.YearsChange == 0) return;
            int interest = KitGame?.HookYearsThisHand ?? 0;
            if (round.YearsChange - interest < 0) _view.PlaySfx(SfxIds.BarHit);
            else if (round.YearsChange - interest > 0) _view.PlaySfx(SfxIds.BarGrow);
            if (interest > 0) _view.PlaySfx(SfxIds.RuleInterest);
        }

        /// <summary>The bar nears its line: the soul's warning, once until it falls back.</summary>
        private void SoulWarningSound()
        {
            if (!BarMode || _seat.Limit <= 0 || _relabelling) return;
            long percent = (long)_game.Years * 100 / _seat.Limit;
            if (percent < SoulWarningRearmPercent) _soulWarned = false;
            if (_soulWarned || percent < SoulWarningPercent || _game.IsGameOver) return;
            _soulWarned = true;
            _view.PlaySfx(SfxIds.SoulWarning);
        }

        // ------------------------------------------------------------------ the demon's health bar

        /// <summary>The hand whose result the bar has shown (Mammon's interest is drawn as its own step, once).</summary>
        private int _barRoundShown = -1;

        /// <summary>
        /// The demon's health bar under the portrait (a chapter demon's table only): what is left of the player's share, toward the soul
        /// line (Lucifer's: the fall line). A settled hand's interest is drawn as a second step after the hand's own change.
        /// </summary>
        private void ShowBossBar()
        {
            if (!BarMode || _seat.Limit <= 0)
            {
                _view.SetBossBar(BossBarDisplay.Hidden);
                return;
            }
            bool settled = _game.LastRound != null && (_game.Phase == GamePhase.RoundOver || _game.IsGameOver);
            int interest = KitGame?.HookYearsThisHand ?? 0;
            if (settled && interest > 0 && _barRoundShown != _game.RoundNumber && !_relabelling)
            {
                _view.SetBossBar(BarOf(_game.Years - interest));
                _view.Pause(InterestPause);
            }
            if (settled) _barRoundShown = _game.RoundNumber;
            _view.SetBossBar(BarOf(_game.Years));
            SoulWarningSound();
        }

        /// <summary>The beat between a hand's own change of the bar and Mammon's interest on it.</summary>
        private const float InterestPause = 0.5f;

        private BossBarDisplay BarOf(int value)
        {
            bool fall = _seat.CastDown != null;
            int played = Math.Max(1, _game.RoundNumber + (_game.Phase == GamePhase.Betting ? 1 : 0));
            string label = UiText.Upper(string.Format(UiText.BarLabelFormat, UiText.GenitiveOf(_dealerText)));
            string hint = string.Format(_game.IsSoulAtStake ? UiText.BossBarSoulHintFormat : UiText.BossBarHintFormat, label, played,
                fall ? UiText.BarMarkFall : UiText.BarMarkSoul);
            return new BossBarDisplay(value, _seat.BarStart, _seat.Limit, fall ? BarLine.Fall : BarLine.Soul, hint);
        }

        /// <summary>At the showdown: the thrown card Lilith kept is marked in her hand.</summary>
        private bool IsTempted(int index) =>
            _kit?.Rule is LilithTemptation t && t.Tempted.HasValue && !IsHandInPlay && _game.HouseHand != null && _game.HouseHand[index].Equals(t.Tempted.Value);
    }
}
