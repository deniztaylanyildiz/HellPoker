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
    /// Phase 2's class mechanics at a chapter's table (the Peasant's satchel, the Oracle's foresight, the King's treasury, the Summoner's
    /// imps, the Thief's steal), their passives and the demon's rule's words. Nothing here runs at a demo table: <see cref="_kit"/> is null there.
    /// </summary>
    public sealed partial class TablePresenter
    {
        /// <summary>The chapter table's hooks (the class's acts, the demon's rule); null at a demo table.</summary>
        private ChapterHooks _kit;

        /// <summary>The satchel cards picked to go into this draw.</summary>
        private readonly List<Card> _bagTakes = new List<Card>();

        /// <summary>A pick waits for the card the player gives (the King's seizure, the imp's Hand, the Thief's steal).</summary>
        private bool _seizing;

        /// <summary>The taxes the King had lost when the hand was dealt (the line about half of them gone).</summary>
        private int _taxesLostAtDeal;

        /// <summary>The demon said he is cornered (once a table).</summary>
        private bool _corneredSaid;

        /// <summary>The hand whose passive the log has written (once a hand).</summary>
        private int _passiveLoggedRound = -1;

        /// <summary>The chapter table's hooks, for tests and screenshots.</summary>
        public ChapterHooks Kit => _kit;

        private HellPokerGame KitGame => _game as HellPokerGame;

        private void ClearKit()
        {
            _kit = null;
            _bagTakes.Clear();
            _seizing = false;
            _view.SetKit(KitDisplay.Hidden);
            _view.SetCarriedRule(null, null);
            _view.SetEnraged(false);
            _gaugesShown = null;
            _view.SetSeatGauges(SeatGauges.Hidden);
        }

        private void SeatKit(IHellPokerGame game)
        {
            ClearKit();
            _kit = (game as HellPokerGame)?.Hooks as ChapterHooks;
            _corneredSaid = false;
            _soulWarned = false;
            _drawSoundRound = _resultSoundRound = _barRoundShown = _passiveLoggedRound = -1;
            _taxesLostAtDeal = _kit?.Kit.TaxesLost ?? 0;
            // Demo 3.4, Ironman: the other demon's rule this one carries, beside his portrait.
            string carried = _kit?.Carried?.Id;
            _view.SetCarriedRule(carried, carried == null ? null
                : string.Format(UiText.CarriedRuleFormat, UiText.CarriedRuleName(carried), UiText.CarriedRuleText(carried)));
        }

        private void KitHandDealt()
        {
            _bagTakes.Clear();
            _seizing = false;
            if (_kit == null) return;
            _taxesLostAtDeal = _kit.Kit.TaxesLost;
            if (_kit.Prophecy != null) _view.PlaySfx(SfxIds.ProphecySee);   // a new hand: the Oracle looks at the deck's top again
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
            LogNote($"satchel: {string.Join(" ", _bagTakes)} into the draw (hand {_game.RoundNumber}{(_bagTakes.Count >= 2 ? "; the demon sees the second" : "")})");
            _view.PlaySfx(SfxIds.SatchelTake);
            KitGame.Draw(discards, fill);
            _bagTakes.Clear();
            return true;
        }

        // ------------------------------------------------------------------ K: the Thief's steal; for the others, what the class does

        /// <summary>
        /// K (or the badge) at a chapter's table (Demo 2.3: the classes' powers are passive): the Thief's STEAL; every other class only
        /// hears what it does.
        /// </summary>
        private void UseKitPower()
        {
            if (_kit?.Theft != null)
            {
                PressSteal(_kit.Theft);
                return;
            }
            _view.SetMessage(UiText.ClassPassiveInfo(_game.Sinner.Id, _kit?.Kit), Tone.Neutral);
        }

        /// <summary>Why an act cannot be done now, in a few words; the refusal goes into the log.</summary>
        private string KitRefusalText(KitRefusal why, int price = 0)
        {
            LogNote($"refused: {why} (hand {_game.RoundNumber + (_game.Phase == GamePhase.Betting && !(KitGame?.HandInPlay ?? false) ? 1 : 0)}, {_game.Phase})");
            if (why == KitRefusal.NotNow && _kit?.Theft != null) return UiText.StealNotNow;
            if (why == KitRefusal.NotNow) return UiText.KitNotBeforeDraw;
            return UiText.KitRefused(why, _kit?.Kit.Taxes ?? 0, price);
        }

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
                if (_bagTakes.Count > 0)
                    _view.SetMessage(string.Format(_bagTakes.Count >= 2 ? UiText.SatchelTakeSeenFormat : UiText.SatchelTakeFormat, _bagTakes.Count), Tone.Warning);
                return;
            }
            if (_kit.Prophecy != null) _view.SetMessage(ProphecyHint, Tone.Neutral);   // the cards only show: nothing moves them
        }

        /// <summary>The Oracle's hover text with his own numbers (FORESIGHT's cards now and at most).</summary>
        private string ProphecyHint => UiText.KitProphecyHint(_kit?.Kit);

        public void PressKitButton(int index)
        {
            if (_kit?.Theft != null)
            {
                if (Playing && _pendingEvent == null && !Hurry()) PressSteal(_kit.Theft);
                return;
            }
            if (_kit?.Summon != null)
            {
                PressSummon(index);
                return;
            }
            if (!Playing || _kit?.Treasury == null || _pendingEvent != null || Hurry()) return;
            HellPokerGame game = KitGame;
            TreasuryPowers treasury = _kit.Treasury;
            TaxAct act = index == 0 ? TaxAct.Reveal : index == 1 ? TaxAct.FreeRaise : TaxAct.Seize;
            if (act == TaxAct.Seize && _seizing)
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
                _view.SetMessage(why == KitRefusal.NoTaxes ? UiText.SummonNoCoins : why == KitRefusal.Locked ? UiText.ImpLockedText(act)
                    : KitRefusalText(why), Tone.Warning);
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
                    _view.SetMessage(WithWild(summon, WithLoyal(price, UiText.EyeOpened)), Tone.Good);
                    return;
                case SummonAct.Whisper:
                    summon.Whisper(game);
                    LogNote($"imps: the WHISPER: {(summon.WhisperedCheat?.Id ?? "nothing")} (hand {_game.RoundNumber}, {PriceNote(summon, price)})");
                    _view.PlaySfx(SfxIds.ProphecySee);
                    Refresh();
                    _view.SetMessage(WithWild(summon, WithLoyal(price, summon.WhisperedCheat == null ? UiText.WhisperNothing
                        : string.Format(UiText.WhisperFormat, UiText.CheatName(summon.WhisperedCheat.Id)))), Tone.Good);
                    return;
                default:
                    _seizing = true;
                    Refresh();
                    _view.SetMessage(UiText.SwapPrompt, Tone.Warning);
                    return;
            }
        }

        /// <summary>An imp the loyal imp freed says so.</summary>
        private static string WithLoyal(int price, string message) => price == 0 ? message + "\n" + UiText.LoyalImpLine : message;

        private string PriceNote(SummonPowers summon, int price) =>
            price == 0 ? "free: passive LOYAL IMP" : summon.AtDemonsTable ? $"+{price} on the bar" : $"{price} coins, purse {_seat?.Run.Purse.Coins}";

        /// <summary>An imp that ran wild says so after what it did.</summary>
        private string WithWild(SummonPowers summon, string message)
        {
            if (!summon.WildFrom.HasValue || _wildTold == summon.WildFrom) return message;
            _wildTold = summon.WildFrom;
            LogNote($"imps: one ran wild{(summon.WildOnDemon ? " on the demon (the Blood Oath)" : "")}, {summon.WildFrom.Value} → {summon.WildTo.Value}");
            _view.PlaySfx(SfxIds.Cheat);
            return message + "\n" + string.Format(summon.WildOnDemon ? UiText.WildDemonFormat : UiText.WildFormat, summon.WildFrom.Value, summon.WildTo.Value);
        }

        private Card? _wildTold;

        /// <summary>The Thief's STEAL (K or the panel's button): the pick of the card he gives opens (again: put away).</summary>
        private void PressSteal(TheftPowers theft)
        {
            if (_seizing)
            {
                _seizing = false;
                Refresh();
                return;
            }
            KitRefusal why = theft.WhyNoSteal(KitGame);
            if (why != KitRefusal.None)
            {
                _view.SetMessage(KitRefusalText(why), Tone.Warning);
                return;
            }
            _seizing = true;
            Refresh();
            _view.SetMessage(UiText.StealPrompt, Tone.Warning);
        }

        /// <summary>The steal itself, with the card picked: taken (blind), or caught and fined.</summary>
        private void StealWith(int index)
        {
            TheftPowers theft = _kit.Theft;
            Card given = _game.PlayerHand[index];
            bool afterDraw = KitGame.IsAfterDraw;
            if (!theft.CanGive(KitGame, index) || !theft.Steal(KitGame, index))
            {
                _view.SetMessage(UiText.StealPrompt, Tone.Warning);   // not that card: the pick stays open
                return;
            }
            _seizing = false;
            _discards.Remove(index);
            string when = afterDraw ? "after the draw" : "before the draw";
            if (theft.CaughtThisHand)
            {
                LogNote($"theft: CAUGHT giving {given} (hand {_game.RoundNumber}, {when}, fine {theft.Fine}{(theft.AtDemonsTable ? " on the bar" : " coins")})");
                _view.PlaySfx(SfxIds.Cheat);
                Refresh();
                _view.SetMessage(theft.AtDemonsTable ? string.Format(UiText.CaughtBarFormat, TheftPowers.FineBarPercent)
                    : string.Format(UiText.CaughtCoinsFormat, theft.Fine), Tone.Bad);
                return;
            }
            LogNote($"theft: {theft.Taken} stolen for {given} (hand {_game.RoundNumber}, {when})");
            _view.PlaySfx(SfxIds.Seize);
            Refresh();
            _view.SetMessage(string.Format(UiText.StolenFormat, theft.Taken.Value, given), Tone.Good);
        }

        private int[] SeizablePlayerCards()
        {
            if (_kit?.Theft != null && KitGame != null)
                return Enumerable.Range(0, Hand.Size).Where(i => _kit.Theft.CanGive(KitGame, i)).ToArray();
            if (_kit?.Summon != null && KitGame != null)
                return Enumerable.Range(0, Hand.Size).Where(i => _kit.Summon.CanGive(KitGame, i)).ToArray();
            if (_kit?.Treasury == null || KitGame == null) return null;
            return Enumerable.Range(0, Hand.Size).Where(i => _kit.Treasury.CanGive(KitGame, i)).ToArray();
        }

        /// <summary>A kit's pick stays open while its moment lasts: before the draw (a seizure, the imp's Hand), until the showdown (the
        /// Thief's steal, Demo 2.3).</summary>
        private bool KitPickMomentGone()
        {
            HellPokerGame game = KitGame;
            if (game == null || !game.HandInPlay) return true;
            return _kit?.Theft != null ? !TheftPowers.InTheHand(game) : game.IsAfterDraw;
        }

        private void SeizeWith(int index)
        {
            if (_kit?.Theft != null)
            {
                StealWith(index);
                return;
            }
            if (_kit?.Summon != null)
            {
                SwapWith(index);
                return;
            }
            HellPokerGame game = KitGame;
            Card given = _game.PlayerHand[index];
            int price = _kit.Treasury.PriceOf(TaxAct.Seize);
            Card? taken = _kit.Treasury.Seize(game, index);
            if (taken == null)
            {
                _view.SetMessage(UiText.SeizePrompt, Tone.Warning);   // not that card: the pick stays open
                return;
            }
            _seizing = false;
            _discards.Remove(index);
            LogNote($"treasury: SEIZURE {taken} for {given} (hand {_game.RoundNumber}, {price} taxes; passive ROYAL DISCOUNT, full price {_kit.Kit.SeizeFullPrice})");
            _view.PlaySfx(SfxIds.Seize);
            Refresh();
            _view.SetMessage(string.Format(UiText.SeizedFormat, taken.Value, given) + "\n" + string.Format(UiText.RoyalDiscountLineFormat, price,
                _kit.Kit.SeizeFullPrice), Tone.Good);
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
            _view.SetMessage(WithWild(summon, WithLoyal(price, string.Format(UiText.SwappedFormat, taken.Value, given))), Tone.Good);
        }

        // ------------------------------------------------------------------ the passives, in the log

        /// <summary>A passive that worked this hand goes into the run's log, once a hand: "passive: LOYAL IMP, EYE free".</summary>
        private void LogPassive()
        {
            ClassPowers powers = _kit?.Powers;
            if (powers == null || !powers.PassiveThisHand || _passiveLoggedRound == _game.RoundNumber || _relabelling) return;
            _passiveLoggedRound = _game.RoundNumber;
            switch (powers)
            {
                case SatchelPowers satchel when satchel.SilentCard.HasValue:
                    LogNote($"passive: SILENT FIELD, {satchel.SilentCard.Value} from the satchel unseen (hand {_game.RoundNumber})");
                    break;
                case TreasuryPowers _:
                    LogNote($"passive: ROYAL DISCOUNT, a seizure for {_kit.Kit.PriceOf(TaxAct.Seize)} of {_kit.Kit.SeizeFullPrice} taxes (hand {_game.RoundNumber})");
                    break;
                case SummonPowers summon when summon.FreedThisHand.HasValue:
                    LogNote($"passive: LOYAL IMP, {summon.FreedThisHand.Value.ToString().ToUpperInvariant()} free (hand {_game.RoundNumber})");
                    break;
            }
        }

        // ------------------------------------------------------------------ what the panel and the lines show

        /// <summary>The line under the message while a kit pick is under way; null otherwise.</summary>
        private string KitHint()
        {
            if (_kit == null) return null;
            if (_seizing) return _kit.Theft != null ? UiText.StealPrompt : _kit.Summon != null ? UiText.SwapPrompt : UiText.SeizePrompt;
            if (_kit.Rule is BelialRoleSwap swap && _kit.Prophecy != null && _game.Phase == GamePhase.Betting && swap.ComesIn(KitGame))
                return UiText.RoleSwapComing;
            return null;
        }

        private void ShowKit()
        {
            LogPassive();
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
                            UiText.KitSatchelHint(kit), waiting: true));
                        return;
                    }
                    bool drawing = _game.Phase == GamePhase.Drawing;
                    _view.SetKit(new KitDisplay(string.Format(UiText.KitSatchelTitleFormat, kit.Satchel.Count, kit.SatchelCapacity),
                        kit.Satchel.Select(c => new KitCard(c, _bagTakes.Contains(c), drawing)).ToArray(), null, UiText.KitSatchelHint(kit)));
                    return;
                }
                case ProphecyPowers prophecy:
                {
                    // Shut between hands (the deal would take the very cards shown) and after the draw; it opens once the deal has played
                    // out (this is queued after the cards' animation). The Fate Weaver: the card the demon knows, under the title.
                    string known = prophecy.ShowsKnownCard && prophecy.KnownCard.HasValue && game.HandInPlay
                        ? string.Format(UiText.DemonKnowsFormat, prophecy.KnownCard.Value) : null;
                    if (!ProphecyPowers.IsOpen(game))
                    {
                        bool afterDraw = game.IsAfterDraw && game.HandInPlay;
                        _view.SetKit(new KitDisplay(UiText.KitProphecyTitle, null, null, ProphecyHint,
                            note: known ?? (afterDraw ? UiText.KitProphecyNextDeal : UiText.KitProphecyAfterDeal)));
                        return;
                    }
                    IReadOnlyList<Card> seen = prophecy.TopCards(game);
                    _view.SetKit(new KitDisplay(known == null ? UiText.KitProphecyTitle : UiText.KitProphecyTitle + "  " + known,
                        seen.Select(c => new KitCard(c)).ToArray(), null, ProphecyHint));
                    return;
                }
                case TheftPowers theft:
                    _view.SetKit(new KitDisplay(UiText.KitThiefTitle, null,
                        new[] { new KitButton(UiText.StealButton, theft.WhyNoSteal(game) != KitRefusal.None && !_seizing) },
                        UiText.KitThiefHint(kit), waiting: _seizing));
                    return;
                case SummonPowers summon:
                {
                    string Price(SummonAct act) => !summon.IsOpen(act) ? UiText.KitLockedShort : summon.PriceOf(act) == 0 ? UiText.KitFree
                        : summon.AtDemonsTable ? string.Format(UiText.KitBarPriceFormat, summon.BarPercentOf(act)) : summon.PriceOf(act).ToString();
                    KitButton Button(SummonAct act, string format) =>
                        new KitButton(string.Format(format, Price(act)), summon.WhyNot(game, act) != KitRefusal.None);
                    // The loyal imp's free one shows in the title while the imps' moment lasts (dealt, the draw still to come).
                    bool impsMoment = game.HandInPlay && !game.IsAfterDraw;
                    _view.SetKit(new KitDisplay(summon.FreeLeft > 0 && impsMoment ? UiText.KitSummonLoyalTitle : UiText.KitSummonTitle, null, new[]
                    {
                        Button(SummonAct.Eye, UiText.KitEyeButtonFormat), Button(SummonAct.Hand, UiText.KitHandButtonFormat),
                        Button(SummonAct.Whisper, UiText.KitWhisperButtonFormat)
                    }, UiText.KitSummonHint(kit), waiting: _seizing));
                    return;
                }
                case TreasuryPowers treasury:
                {
                    KitButton Button(TaxAct act, string format) =>
                        new KitButton(string.Format(format, treasury.PriceOf(act)), treasury.WhyNot(game, act) != KitRefusal.None);
                    _view.SetKit(new KitDisplay(string.Format(UiText.KitTreasuryTitleFormat, kit.Taxes, kit.TaxCapacity), null, new[]
                    {
                        Button(TaxAct.Reveal, UiText.KitRevealButtonFormat), Button(TaxAct.FreeRaise, UiText.KitFreeRaiseButtonFormat),
                        Button(TaxAct.Seize, UiText.KitSeizeButtonFormat)
                    }, UiText.KitTreasuryHint(kit), waiting: _seizing));
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

        /// <summary>The result, with the demon's rule's line (interest, temptation, the mirror), the King's taxes lost, the Jester's shield.</summary>
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
                    // What his mirror really did this hand (the line follows the act, not the class).
                    if (mirror.EyeSaw.HasValue) lines.Add(UiText.MirrorEye);
                    if (mirror.UsedBag) lines.Add(UiText.MirrorSatchel);
                    if (mirror.DrewTop) lines.Add(UiText.MirrorTopCard);
                    break;
            }
            // Demo 3.4: the demon enraged at half his bar.
            if (_kit.Rule?.EnragedThisHand == true) lines.Add(string.Format(UiText.RageLineFormat, UiText.NameInSentence(_dealerText)));
            if (_kit.Treasury != null && _kit.Kit.TaxesLost > _taxesLostAtDeal)
                lines.Add(_kit.Kit.Has(UpgradeIds.CrownGuard) ? UiText.TaxesQuartered : UiText.TaxesHalved);
            if (_kit.Upgrades?.LaughedThisHand == true) lines.Add(UiText.DoubleLaughLine);
            if (_kit.Upgrades?.ShieldedThisHand == true)
            {
                lines.Add(string.Format(UiText.JesterShieldFormat, _kit.Upgrades.ShieldsLeft));
                if (_shieldLoggedRound != _game.RoundNumber)
                {
                    _shieldLoggedRound = _game.RoundNumber;
                    LogNote($"jester's shield: the two jokers' loss spared, a push ({_kit.Upgrades.ShieldsLeft} left at this table)");
                }
            }
            return lines.Count == 0 ? message : message + "\n" + string.Join("\n", lines);
        }

        private int _shieldLoggedRound = -1;

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
            if (_kit.Rule?.EnragedThisHand == true && !_relabelling)
            {
                // Demo 3.4: the rage — a growl, the word flaring up; his look burns from now on (SetEnraged in the refresh).
                _view.PlaySfx(SfxIds.Rage);
                _view.PlayMoment(TableMoment.Rage, UiText.RageFlash);
                LogNote($"{_dealer?.Id} enraged at bar {_game.Years} of {_kit.Rule.BarStart}");
            }
            switch (_kit.Rule)
            {
                case LilithTemptation t when t.Tempted.HasValue: _view.PlaySfx(SfxIds.RuleTemptation); break;
                case LuciferMirror m when m.StruckThisHand && (m.EyeSaw.HasValue || m.UsedBag || m.DrewTop): _view.PlaySfx(SfxIds.RuleMirror); break;
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

        /// <summary>The gauges as last shown (sent again only when they change).</summary>
        private string _gaugesShown;

        /// <summary>
        /// Demo 3.5.1: the gauges under the demon's name — a floor's imp: his purse (and its bar) and the ante with a dot for every hand
        /// before it grows (or the free hands); a demon's table: the fixed ante under his bar, his rule, a carried rule and his rage as icons.
        /// </summary>
        private void ShowSeatGauges()
        {
            SeatGauges gauges = SeatGaugesNow();
            if (gauges.Key == _gaugesShown) return;
            _gaugesShown = gauges.Key;
            _view.SetSeatGauges(gauges);
        }

        private SeatGauges SeatGaugesNow()
        {
            if (_seat == null) return SeatGauges.Hidden;
            if (CoinMode)
            {
                FloorTable floor = _seat.Floor;
                if (floor.HousePurse == null) return SeatGauges.Hidden;   // the ghost's one hand keeps its line
                string purseTip = floor.IsWarden ? UiText.GaugeWardenPurseTip : UiText.GaugePurseTip;
                if (floor.FreeHandsLeft > 0)
                    return new SeatGauges(true, floor.HousePurse.Coins, floor.HouseStart, purseTip, string.Format(UiText.GaugeNoAnteFormat, floor.FreeHandsLeft),
                        0, string.Format(UiText.GaugeNoAnteTipFormat, floor.FreeHandsLeft), null);
                int next = floor.HandsToNextAnte;
                int grown = floor.AnteNow + _seat.Run.Rules.AnteStep;
                string anteTip = next <= 1 ? string.Format(UiText.GaugeAnteNextTipFormat, next, grown) : string.Format(UiText.GaugeAnteTipFormat, next, grown);
                return new SeatGauges(true, floor.HousePurse.Coins, floor.HouseStart, purseTip, string.Format(UiText.GaugeAnteFormat, floor.AnteNow),
                    next, anteTip, null);
            }
            if (!BarMode) return SeatGauges.Hidden;
            var icons = new List<SeatIcon>();
            DemonRule rule = _kit?.Rule;
            if (rule != null)
            {
                string line = UiText.DemonRuleLine(rule);
                if (!string.IsNullOrEmpty(line)) icons.Add(new SeatIcon(RuleIcon(rule.Id), line));
            }
            string carried = _kit?.Carried?.Id;
            if (carried != null)
                icons.Add(new SeatIcon(RuleIcon(carried), string.Format(UiText.CarriedRuleFormat, UiText.CarriedRuleName(carried), UiText.CarriedRuleText(carried))));
            if (rule != null && rule.Enraged)
                icons.Add(new SeatIcon(SeatIcon.Rage, string.Format(UiText.RageHintFormat, DifficultyRules.RageStakePercent)));
            return new SeatGauges(true, -1, 0, null, string.Format(UiText.GaugeAnteBarFormat, Shown(_game.UpcomingAnte)), 0, UiText.GaugeAnteFixedTip, icons);
        }

        /// <summary>A demon's rule's icon: the cheat icon of his own kind.</summary>
        private static string RuleIcon(string ruleId)
        {
            switch (ruleId)
            {
                case "interest": return "tithe";
                case "role_swap": return "serpent_swap";
                case "temptation": return "thorn";
                default: return "gaze";
            }
        }

        /// <summary>The beat between a hand's own change of the bar and Mammon's interest on it.</summary>
        private const float InterestPause = 0.5f;

        private BossBarDisplay BarOf(int value)
        {
            bool fall = _seat.CastDown != null;
            int played = Math.Max(1, _game.RoundNumber + (_game.Phase == GamePhase.Betting ? 1 : 0));
            string label = UiText.Upper(string.Format(UiText.BarLabelFormat, UiText.GenitiveOf(_dealerText)));
            string hint = UiText.BossBarExplain + "\n" + string.Format(_game.IsSoulAtStake ? UiText.BossBarSoulHintFormat : UiText.BossBarHintFormat, label, played,
                fall ? UiText.BarMarkFall : UiText.BarMarkSoul) + "\n" + UiText.BarLeft(value, _seat.BarStart);
            DemonRule rule = _kit?.Rule;
            if (rule != null && rule.Enraged) hint += "\n" + string.Format(UiText.RageHintFormat, DifficultyRules.RageStakePercent);
            var bar = new BossBarDisplay(value, _seat.BarStart, _seat.Limit, fall ? BarLine.Fall : BarLine.Soul, hint, LastBlow);
            // Demo 3.4: the rage's mark on the bar (where it waits, and once it came).
            return rule != null && rule.CanRage ? bar.WithRage(rule.BarStart * DifficultyRules.RageBarPercent / 100, rule.Enraged) : bar;
        }

        /// <summary>Between hands at a demon's table: the next win ends him (the bar is a unit or less: 2.1.1's fixed stakes).</summary>
        private bool LastBlow => BarMode && KitGame != null && KitGame.NextWinEndsIt && !_game.IsSoulAtStake;

        /// <summary>At the showdown: the thrown card Lilith kept is marked in her hand.</summary>
        private bool IsTempted(int index) =>
            _kit?.Rule is LilithTemptation t && t.Tempted.HasValue && !IsHandInPlay && _game.HouseHand != null && _game.HouseHand[index].Equals(t.Tempted.Value);
    }
}
