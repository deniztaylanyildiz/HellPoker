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

        /// <summary>Why K cannot be used now, in a few words (2.1.1: once a hand, at the class's moment). The refusal goes into the log.</summary>
        private string PowerRefusalText(KitRefusal why)
        {
            HellPokerGame game = KitGame;
            LogNote($"K refused: {why} (hand {_game.RoundNumber + (_game.Phase == GamePhase.Betting && !game.HandInPlay ? 1 : 0)}, {_game.Phase})");
            switch (why)
            {
                case KitRefusal.AlreadyUsed: return UiText.KitUsedThisHand;
                case KitRefusal.NoTaxes when _kit.Prophecy != null: return UiText.SummonNoCoins;   // the floors' price of sending down
                case KitRefusal.NoTaxes when _kit.Treasury != null:
                    return UiText.KitRefused(why, 0, 0, _kit.Kit.Taxes, _kit.Treasury.PowerSeizePrice);
                case KitRefusal.NotNow:
                    if (_kit.Satchel != null) return UiText.KitHarvestInDraw;
                    return game.IsAfterDraw && game.HandInPlay ? UiText.KitNotAfterDraw
                        : _kit.Prophecy != null || !game.HandInPlay ? UiText.KitAfterTheDeal : UiText.KitNotBeforeDraw;
                default: return UiText.KitRefused(why, _sinner.Charge, _sinner.Rules.Full);
            }
        }

        private void UseKitPower()
        {
            HellPokerGame game = KitGame;
            switch (_kit.Powers)
            {
                case SatchelPowers satchel:
                {
                    KitRefusal why = satchel.WhyNoHarvest(game, _discards);
                    if (why != KitRefusal.None)
                    {
                        _view.SetMessage(PowerRefusalText(why), Tone.Warning);
                        return;
                    }
                    TableState before = CaptureState();
                    _view.PlaySfx(SfxIds.Deal);
                    Hand playerBefore = _game.PlayerHand, houseBefore = _game.HouseHand;
                    if (satchel.Harvest(game, _discards.OrderBy(i => i).ToArray()) == null)
                    {
                        // The draw could not be made with these cards (Demo 2.2: it was announced as done all the same).
                        _view.SetMessage(PowerRefusalText(KitRefusal.NotNow), Tone.Warning);
                        return;
                    }
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
                        _view.SetMessage(PowerRefusalText(why), Tone.Warning);
                        return;
                    }
                    if (!prophecy.StartRearranging(game)) return;
                    LogNote($"power: the Oracle bends fate (hand {_game.RoundNumber})");
                    Refresh();
                    _view.SetMessage(BendPrompt(prophecy), Tone.Good);
                    return;
                }
                case SummonPowers summon:
                {
                    KitRefusal why = summon.WhyNoLegion(game);
                    if (why != KitRefusal.None)
                    {
                        _view.SetMessage(PowerRefusalText(why), Tone.Warning);
                        return;
                    }
                    if (!summon.CallLegion(game)) return;
                    LogNote($"power: the LEGION (hand {_game.RoundNumber})");
                    Refresh();
                    _view.SetMessage(UiText.LegionCalled, Tone.Good);
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
                        _view.SetMessage(PowerRefusalText(why), Tone.Warning);
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
                    _view.SetMessage(ProphecyHint, Tone.Neutral);
                    return;
                }
                // A click on the card itself while bending: down when it may go down, else up (the Seer's Spectacles).
                Bend(index, prophecy.CanSendToBottom(game, index) ? false : true);
            }
        }

        /// <summary>A small card's ▲ / ▼: the Oracle's BEND FATE (the hand's K power is spent on the first move).</summary>
        public void PressKitArrow(int index, bool toTop)
        {
            if (!Playing || _kit?.Prophecy == null || _pendingEvent != null || Hurry()) return;
            Bend(index, toTop);
        }

        private void Bend(int index, bool toTop)
        {
            HellPokerGame game = KitGame;
            ProphecyPowers prophecy = _kit.Prophecy;
            IReadOnlyList<Card> top = prophecy.TopCards(game);
            if (index < 0 || index >= top.Count) return;
            if (prophecy.RearrangesLeft <= 0)
            {
                KitRefusal why = prophecy.WhyNoRearrange(game);
                if (why != KitRefusal.None)
                {
                    _view.SetMessage(PowerRefusalText(why), Tone.Warning);
                    return;
                }
            }
            bool possible = toTop ? index > 0 : _kit.Kit.CanSendToBottom;
            if (!possible)
            {
                _view.SetMessage(toTop ? UiText.BendAlreadyOnTop : UiText.KitNoBottom, Tone.Warning);
                return;
            }
            bool started = prophecy.RearrangesLeft <= 0;
            Card card = top[index];
            if (!prophecy.Bend(game, index, toTop)) return;
            if (started) LogNote($"power: the Oracle bends fate (hand {_game.RoundNumber})");
            LogNote($"bend fate: {card} to the {(toTop ? "top" : "bottom")} (hand {_game.RoundNumber})");
            _view.PlaySfx(SfxIds.ProphecyBottom);
            Refresh();
            _view.SetMessage(string.Format(toTop ? UiText.BentUpFormat : UiText.RearrangedFormat, card)
                + (prophecy.RearrangesLeft > 0 ? "\n" + BendPrompt(prophecy) : ""), Tone.Good);
        }

        /// <summary>The Oracle's hover text with his own numbers (the Fate Weaver moves two).</summary>
        private string ProphecyHint => string.Format(UiText.KitProphecyHintFormat, _kit?.Kit.RearrangesPerCharge ?? ClassKit.RearrangesPerUse);

        private static string BendPrompt(ProphecyPowers prophecy) => string.Format(UiText.RearrangePromptFormat, prophecy.RearrangesLeft);

        public void PressKitButton(int index)
        {
            if (_kit?.Summon != null)
            {
                PressSummon(index);
                return;
            }
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

        /// <summary>An imp's button: the Eye and the Whisper at once, the Hand opens the pick of the card it trades.</summary>
        private void PressSummon(int index)
        {
            if (!Playing || _pendingEvent != null || Hurry()) return;
            HellPokerGame game = KitGame;
            SummonPowers summon = _kit.Summon;
            SummonAct act = index == 0 ? SummonAct.Eye : index == 1 ? SummonAct.Hand : SummonAct.Whisper;
            if (act == SummonAct.Hand && _seizing)
            {
                _seizing = false;   // the button again: put away
                Refresh();
                return;
            }
            KitRefusal why = summon.WhyNot(game, act);
            if (why != KitRefusal.None)
            {
                _view.SetMessage(why == KitRefusal.NoTaxes ? UiText.SummonNoCoins : KitRefusalText(why), Tone.Warning);
                return;
            }
            int price = summon.PriceOf(act);
            switch (act)
            {
                case SummonAct.Eye:
                    summon.Eye(game);
                    LogNote($"imps: the EYE (hand {_game.RoundNumber}, {PriceNote(summon, price)})");
                    _view.PlaySfx(SfxIds.ProphecySee);
                    _view.PlaySfx(SfxIds.Flip);
                    Refresh();
                    _view.SetMessage(WithWild(summon, UiText.EyeOpened), Tone.Good);
                    return;
                case SummonAct.Whisper:
                    summon.Whisper(game);
                    LogNote($"imps: the WHISPER: {(summon.WhisperedCheat?.Id ?? "nothing")} (hand {_game.RoundNumber}, {PriceNote(summon, price)})");
                    _view.PlaySfx(SfxIds.ProphecySee);
                    Refresh();
                    _view.SetMessage(WithWild(summon, summon.WhisperedCheat == null ? UiText.WhisperNothing
                        : string.Format(UiText.WhisperFormat, UiText.CheatName(summon.WhisperedCheat.Id))), Tone.Good);
                    return;
                default:
                    _seizing = true;
                    Refresh();
                    _view.SetMessage(UiText.SwapPrompt, Tone.Warning);
                    return;
            }
        }

        private string PriceNote(SummonPowers summon, int price) =>
            price == 0 ? "free" : summon.AtDemonsTable ? $"+{price} on the bar" : $"{price} coins, purse {_seat?.Run.Purse.Coins}";

        /// <summary>An imp that ran wild says so after what it did.</summary>
        private string WithWild(SummonPowers summon, string message)
        {
            if (!summon.WildFrom.HasValue || _wildTold == summon.WildFrom) return message;
            _wildTold = summon.WildFrom;
            LogNote($"imps: one ran wild, {summon.WildFrom.Value} → {summon.WildTo.Value}");
            _view.PlaySfx(SfxIds.Cheat);
            return message + "\n" + string.Format(UiText.WildFormat, summon.WildFrom.Value, summon.WildTo.Value);
        }

        private Card? _wildTold;

        private int[] SeizablePlayerCards()
        {
            if (_kit?.Summon != null && KitGame != null)
                return Enumerable.Range(0, Hand.Size).Where(i => _kit.Summon.CanGive(KitGame, i)).ToArray();
            if (_kit?.Treasury == null || KitGame == null) return null;
            return Enumerable.Range(0, Hand.Size).Where(i => _kit.Treasury.CanGive(KitGame, i)).ToArray();
        }

        private void SeizeWith(int index)
        {
            if (_kit?.Summon != null)
            {
                SwapWith(index);
                return;
            }
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

        /// <summary>The imp's hand: the picked card for the demon's open card that serves the hand best.</summary>
        private void SwapWith(int index)
        {
            SummonPowers summon = _kit.Summon;
            Card given = _game.PlayerHand[index];
            int price = summon.PriceOf(SummonAct.Hand);
            Card? taken = summon.Swap(KitGame, index);
            if (taken == null)
            {
                _view.SetMessage(UiText.SwapPrompt, Tone.Warning);   // not that card: the pick stays open
                return;
            }
            _seizing = false;
            _discards.Remove(index);
            LogNote($"imps: the HAND, {taken} for {given} (hand {_game.RoundNumber}, {PriceNote(summon, price)})");
            _view.PlaySfx(SfxIds.Seize);
            Refresh();
            _view.SetMessage(WithWild(summon, string.Format(UiText.SwappedFormat, taken.Value, given)), Tone.Good);
        }

        // ------------------------------------------------------------------ what the panel and the lines show

        /// <summary>The line under the message while a kit pick is under way; null otherwise.</summary>
        private string KitHint()
        {
            if (_kit == null) return null;
            if (_seizing) return _kit.Summon != null ? UiText.SwapPrompt : UiText.SeizePrompt;
            if (_kit.Prophecy != null && _kit.Prophecy.RearrangesLeft > 0)
                return BendPrompt(_kit.Prophecy);
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
                    // 2.1.1: shut between hands (the deal would take the very cards shown) and after the draw; it opens once the deal
                    // has played out (this is queued after the cards' animation).
                    if (!ProphecyPowers.IsOpen(game))
                    {
                        bool afterDraw = game.IsAfterDraw && game.HandInPlay;
                        _view.SetKit(new KitDisplay(UiText.KitProphecyShutTitle, null, null, ProphecyHint,
                            note: afterDraw ? UiText.KitProphecyNextDeal : UiText.KitProphecyAfterDeal));
                        return;
                    }
                    bool rearranging = prophecy.RearrangesLeft > 0;
                    // The arrows show while fate may be bent: the power ready this hand, or a bend under way.
                    bool arrows = rearranging || prophecy.WhyNoRearrange(game) == KitRefusal.None;
                    string title = rearranging ? string.Format(UiText.KitRearrangeTitleFormat, prophecy.RearrangesLeft) : UiText.KitProphecyTitle;
                    IReadOnlyList<Card> seen = prophecy.TopCards(game);
                    _view.SetKit(new KitDisplay(title, seen.Select((c, i) => new KitCard(c, clickable: rearranging, arrows: arrows,
                            canTop: i > 0, canBottom: kit.CanSendToBottom)).ToArray(), null,
                        ProphecyHint, waiting: rearranging));
                    return;
                }
                case SummonPowers summon:
                {
                    string Price(SummonAct act) => summon.LegionFreeLeft > 0 ? UiText.KitFree : summon.AtDemonsTable
                        ? string.Format(UiText.KitBarPriceFormat, summon.BarPercentOf(act)) : summon.PriceOf(act).ToString();
                    KitButton Button(SummonAct act, string format) =>
                        new KitButton(string.Format(format, Price(act)), summon.WhyNot(game, act) != KitRefusal.None);
                    _view.SetKit(new KitDisplay(summon.Legion ? UiText.KitSummonLegionTitle : UiText.KitSummonTitle, null, new[]
                    {
                        Button(SummonAct.Eye, UiText.KitEyeButtonFormat), Button(SummonAct.Hand, UiText.KitHandButtonFormat),
                        Button(SummonAct.Whisper, UiText.KitWhisperButtonFormat)
                    }, UiText.KitSummonHint, waiting: _seizing));
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
                    }, string.Format(UiText.KitTreasuryHintFormat, kit.TaxCapacity, treasury.PriceOf(TaxAct.Reveal), treasury.PriceOf(TaxAct.FreeRaise),
                        treasury.PriceOf(TaxAct.Seize)), waiting: _seizing));
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
                case LuciferMirror mirror when mirror.StruckThisHand:
                    // What his mirror really did this hand (Demo 2.2: the line followed the class, not the act — the Summoner's
                    // Eye and the fifth circle's second copy were told as "he drew the top card").
                    if (mirror.EyeSaw.HasValue) lines.Add(UiText.MirrorEye);
                    if (mirror.UsedBag) lines.Add(UiText.MirrorSatchel);
                    if (mirror.DrewTop) lines.Add(UiText.MirrorTopCard);
                    if (mirror.SentDown) lines.Add(UiText.MirrorSentDown);
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
                case LuciferMirror m when m.StruckThisHand && (m.EyeSaw.HasValue || m.UsedBag || m.DrewTop || m.SentDown): _view.PlaySfx(SfxIds.RuleMirror); break;
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
                fall ? UiText.BarMarkFall : UiText.BarMarkSoul) + "\n" + UiText.BarLeft(value, _seat.BarStart);
            return new BossBarDisplay(value, _seat.BarStart, _seat.Limit, fall ? BarLine.Fall : BarLine.Soul, hint, LastBlow);
        }

        /// <summary>Between hands at a demon's table: the next win ends him (the bar is a unit or less: 2.1.1's fixed stakes).</summary>
        private bool LastBlow => BarMode && KitGame != null && KitGame.NextWinEndsIt && !_game.IsSoulAtStake;

        /// <summary>At the showdown: the thrown card Lilith kept is marked in her hand.</summary>
        private bool IsTempted(int index) =>
            _kit?.Rule is LilithTemptation t && t.Tempted.HasValue && !IsHandInPlay && _game.HouseHand != null && _game.HouseHand[index].Equals(t.Tempted.Value);
    }
}
