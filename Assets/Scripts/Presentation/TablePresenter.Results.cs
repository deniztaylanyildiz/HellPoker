using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;

namespace HellPoker.Presentation
{
    /// <summary>
    /// A chapter's table says how a hand ended plainly (2.1.1): the result band across the middle (YOU WIN / YOU LOSE / PUSH / FOLDED and
    /// the amount), a blink of the screen's edge, the amount floating up from the purse (or the demon's bar), and the last five hands as pips
    /// beside the counter. The old result line stays in the message for the details. Nothing of it at a demo table.
    /// </summary>
    public sealed partial class TablePresenter
    {
        /// <summary>How many hands the pips remember.</summary>
        public const int RecentHandsShown = 5;

        /// <summary>A hand the pips remember: how it ended, the player's hand, what it did (coins, or the demon's bar in percent).</summary>
        private sealed class RecentHand
        {
            public HandOutcome Outcome;
            public HandCategory? Category;
            public int Coins;
            public int BarPercent;
            public bool InCoins;
        }

        private readonly List<RecentHand> _recent = new List<RecentHand>();
        private int _resultRound = -1;
        private bool _bandUp;

        /// <summary>A new chapter table: no band, no pips; the sinner calm (or tense, the soul's line near).</summary>
        private void ClearResults()
        {
            _recent.Clear();
            _resultRound = -1;
            _bandUp = false;
            _view.SetResultBand(null);
            _view.SetRecentHands(Array.Empty<HandPip>());
            _view.SetSinnerMood(RestingMood());
        }

        /// <summary>The band goes as soon as the next hand begins (NEXT HAND); a skip never takes it down. The sinner settles again.</summary>
        private void TakeBandDown()
        {
            if (!_bandUp) return;
            _bandUp = false;
            _view.SetResultBand(null);
            _view.SetSinnerMood(RestingMood());
        }

        /// <summary>Demo 3.1: between hands the sinner breathes calmly — or tensely with the demon's bar near the soul's line.</summary>
        private SinnerMood RestingMood() =>
            BarMode && _seat != null && _seat.Limit > 0 && _game != null && (long)_game.Years * 100 / _seat.Limit >= SoulWarningPercent ? SinnerMood.Tense : SinnerMood.Calm;

        /// <summary>Demo 3.1: how the hand left the sinner — a smile for a win, joy for a straight and up, a wince for a loss; a fold or a push
        /// moves nothing.</summary>
        internal static SinnerMood? MoodAfter(HandOutcome outcome, HandCategory? category)
        {
            switch (outcome)
            {
                case HandOutcome.Win: return category.HasValue && category.Value >= HandCategory.Straight ? SinnerMood.Elated : SinnerMood.Pleased;
                case HandOutcome.Loss: return SinnerMood.Hurt;
                default: return null;
            }
        }

        private HandOutcome OutcomeOf(RoundResult round)
        {
            if (round.Folded) return HandOutcome.Fold;
            if (round.HouseFolded) return HandOutcome.Win;
            if (round.Showdown == null) return HandOutcome.Push;
            if (KitGame?.SparedThisHand == true) return HandOutcome.Push;   // a relic spared the loss
            switch (round.Showdown.Outcome)
            {
                case ShowdownOutcome.PlayerWins: return HandOutcome.Win;
                case ShowdownOutcome.HouseWins: return HandOutcome.Loss;
                default: return HandOutcome.Push;
            }
        }

        /// <summary>
        /// The settled hand at a chapter's table: the band (queued before the hand's moments, so a good hand's flare shows over it first),
        /// and — once a hand — the pip, the edge's blink and the floating amount.
        /// </summary>
        private void ShowHandResult(RoundResult round)
        {
            if (_seat == null || round == null) return;
            HandOutcome outcome = OutcomeOf(round);
            int coins = _seat.LastHand?.Coins ?? -round.YearsChange;
            int bar = Math.Sign(round.YearsChange) * Shown(Math.Abs(round.YearsChange));
            int sentence = round.YearsChange + (KitGame?.UnhealedYearsThisHand ?? 0);
            string amount = CoinMode ? CoinAmount(coins)
                : bar == 0 && sentence == 0 ? UiText.BandBarStays : UiText.BarSentenceLines(bar, sentence);
            _view.SetResultBand(new ResultBand(outcome, UiText.BandTitle(outcome), amount));
            _bandUp = true;

            if (_relabelling || _resultRound == _game.RoundNumber)
            {
                _view.SetRecentHands(RecentPips());   // a language change: the hovers in the new words
                return;
            }
            _resultRound = _game.RoundNumber;
            ShowdownResult s = round.Showdown;
            _recent.Add(new RecentHand
            {
                Outcome = outcome,
                Category = round.Folded || s == null || s.PlayerBust ? (HandCategory?)null : s.Player.Category,
                Coins = coins,
                BarPercent = bar,
                InCoins = CoinMode
            });
            while (_recent.Count > RecentHandsShown) _recent.RemoveAt(0);
            _view.SetRecentHands(RecentPips());
            SinnerMood? mood = MoodAfter(outcome, _recent[_recent.Count - 1].Category);
            if (mood.HasValue) _view.SetSinnerMood(mood.Value);

            if (outcome == HandOutcome.Win) _view.PlayMoment(TableMoment.WinGlow);
            else if (outcome == HandOutcome.Loss) _view.PlayMoment(TableMoment.LossGlow);
            if (CoinMode && coins != 0)
                _view.FloatAmount(UiText.SignedYears(coins), coins > 0 ? Tone.Triumph : Tone.Doom, overBar: false);
            else if (!CoinMode && bar != 0)
                _view.FloatAmount(UiText.SignedPercent(bar), bar < 0 ? Tone.Triumph : Tone.Doom, overBar: true);
        }

        /// <summary>
        /// Demo 3.1: the hand's ending at a chapter's table, by its strength (<see cref="HandFanfare.TierOf"/>): the player's hand as judged (a
        /// joker counts as the card it became; a sealed hand that turned itself over is judged the same), the House's when it won with trips or
        /// more; a fold and a push play nothing. The sound goes with the tier. The Dead Man's Hand at a demon's table keeps its own scene.
        /// </summary>
        private void PlayHandEnding(RoundResult round)
        {
            HandOutcome outcome = OutcomeOf(round);
            ShowdownResult s = round.Showdown;
            HandCategory? player = round.Folded || round.HouseFolded || s == null || s.PlayerBust ? (HandCategory?)null : s.Player.Category;
            HandCategory? house = round.Folded || round.HouseFolded || s == null || s.HouseBust ? (HandCategory?)null : s.House.Category;
            HandTier tier = HandFanfare.TierOf(outcome, player, house);

            if (CoinMode)
                PlayCoinSound(round);   // a floor counts coins: they ring (more of them, a fuller sound)
            else if (outcome == HandOutcome.Win)
                _view.PlaySfx(tier == HandTier.Plain ? SfxIds.WinSmall : SfxIds.WinBig);
            else if (outcome == HandOutcome.Loss || (round.Folded && !round.FreeFold))
                _view.PlaySfx(SfxIds.Loss);
            if (CoinMode && tier >= HandTier.TwoPair && tier <= HandTier.Quads) _view.PlaySfx(SfxIds.WinBig);
            if (tier == HandTier.Blaze) _view.PlaySfx(SfxIds.WinBlaze);

            if (player == HandCategory.DeadMansHand && !CoinMode)
            {
                _view.PlayMoment(TableMoment.DeadMansHand, playerCards: DeadMansCards(_game.PlayerHand));
                return;
            }
            // Demo 3.1.1: a big loss (four antes and up: the purse's coins, the bar's years) — with the House's strong hand or its pair.
            int lost = CoinMode ? -(_seat?.LastHand?.Coins ?? -round.YearsChange) : round.YearsChange;
            bool bigLoss = HandFanfare.IsBigLoss(outcome, lost, _game.Ante);
            if (tier == HandTier.None && !bigLoss) return;
            IReadOnlyList<IReadOnlyList<int>> groups = tier == HandTier.HouseStrong ? HandFanfare.GroupsOf(s.House.Hand, s.House.Category)
                : player.HasValue && tier != HandTier.None ? HandFanfare.GroupsOf(s.Player.Hand, player.Value) : Array.Empty<IReadOnlyList<int>>();
            string title = tier >= HandTier.TwoPair && tier != HandTier.HouseStrong && player.HasValue
                ? string.Format(UiText.GoodHandFormat, UiText.CategoryNameUpper(player.Value)) : null;
            Suit? flushSuit = tier == HandTier.Flush ? s.Player.Hand[0].Suit : (Suit?)null;
            _view.PlayHand(new HandFanfare(tier, title, groups, coinsToBar: !CoinMode, bigLoss: bigLoss, flushSuit: flushSuit));
        }

        private static string CoinAmount(int coins) => coins == 0 ? UiText.BandCoinsStay : string.Format(UiText.BandCoinsFormat, UiText.SignedYears(coins));

        /// <summary>The remembered hands as pips: the colour, and the hover (the hand's name and what it did).</summary>
        private IReadOnlyList<HandPip> RecentPips() => _recent.Select(h => new HandPip(h.Outcome, string.Format(UiText.RecentHandFormat,
            h.Outcome == HandOutcome.Fold ? UiText.BandTitle(HandOutcome.Fold) : h.Category.HasValue ? UiText.CategoryName(h.Category.Value) : UiText.BandTitle(h.Outcome),
            h.InCoins ? CoinAmount(h.Coins) : h.BarPercent == 0 ? UiText.BandBarStays : string.Format(UiText.BarChangeFormat, UiText.SignedPercent(h.BarPercent)))))
            .ToArray();
    }
}
