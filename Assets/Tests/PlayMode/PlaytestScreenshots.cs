using System;
using System.Collections;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Chapters;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Ui;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// 2.1.1's screens (140–155): the two scenes of the playtest put right (the House's caption by the deck counter, a three-line result),
    /// the prophecy before and after the deal, the small cards in four colours and in two, the result band (a floor's and a demon's, won
    /// and lost), the last blow, the Devil's Compass picking. Not a real test (Explicit). HELLPOKER_LANG=tr for Turkish; HELLPOKER_SHOTS for
    /// the folder.
    /// </summary>
    [Explicit]
    public class PlaytestScreenshots
    {
        [TearDown]
        public void BackToTheUsualMenu()
        {
            HellPokerBootstrap.ForceSimulator = null;
            SuitColors.Set(true);
        }

        private static TableView View => ChapterJourneyTests.ChapterTableView;
        private static TablePresenter Table => (TablePresenter)ChapterJourneyTests.Chapters.Table;

        private static IEnumerator Settle(float seconds = 0.4f)
        {
            yield return ChapterJourneyTests.WaitForChapterTable();
            yield return new WaitForSeconds(seconds);
        }

        private static ChapterSave Boss(Core.Sinners.SinnerClass cls, int bar, int barStart)
        {
            ChapterSave save = ChapterPresenter.PlacedSave(cls, 11, 1, "");
            save.NodeDone = false;
            save.Match = "boss";
            save.BossBarStart = barStart;
            save.BossBar = bar;
            return save;
        }

        /// <summary>A hand played to its result, passing; then the band of <paramref name="outcome"/> as the table would say it.</summary>
        private static IEnumerator ToTheResult()
        {
            Table.PerformAction();   // the deal
            yield return Settle(0.2f);
            int guard = 0;
            while (Table.Game.Phase != Core.Game.GamePhase.RoundOver && !Table.Game.IsGameOver && guard++ < 20)
            {
                Table.PerformAction();
                yield return Settle(0.1f);
            }
            yield return Settle(0.6f);
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator CapturePlaytestFixes()
        {
            HellPokerBootstrap.ForceSimulator = false;

            // 140-141: a floor's table — the House's caption and the deck counter apart; a three-line result under the band.
            yield return ChapterJourneyTests.OpenChapters();
            HellPokerScreenshots.Press("PanelOption0");   // DESCEND
            yield return null;
            MapNode start = ChapterJourneyTests.Chapters.Run.Choices.First(n => n.Kind == NodeKind.Table);
            HellPokerScreenshots.Find<Button>($"Node0_{start.Lane}").onClick.Invoke();
            yield return Settle();
            yield return ToTheResult();
            yield return HellPokerScreenshots.Shot("140_house_caption_and_deck");
            View.SetMessage(string.Format(UiText.CoinWinFormat, UiText.CategoryName(Core.Evaluation.HandCategory.OnePair),
                UiText.CategoryName(Core.Evaluation.HandCategory.OnePair), 8) + "\n" + string.Format(UiText.CoinTollFormat, 2) + "\n" +
                string.Format(UiText.InterestFormat, 2), Tone.Good);
            yield return Settle(0.2f);
            yield return HellPokerScreenshots.Shot("141_result_three_lines");
            View.SetResultBand(new ResultBand(HandOutcome.Win, UiText.BandTitle(HandOutcome.Win), string.Format(UiText.BandCoinsFormat, "+12")));
            yield return Settle(0.2f);
            yield return HellPokerScreenshots.Shot("142_band_win_floor");
            View.SetResultBand(new ResultBand(HandOutcome.Loss, UiText.BandTitle(HandOutcome.Loss), string.Format(UiText.BandCoinsFormat, "−8")));
            View.PlayMoment(TableMoment.LossGlow);
            View.FloatAmount("−8", Tone.Doom, overBar: false);
            yield return new WaitForSeconds(0.05f);
            yield return HellPokerScreenshots.Shot("143_band_loss_floor");

            // 144-145: the Warlock's prophecy at Mammon's table, before the deal and after it.
            yield return ChapterJourneyTests.LoadWithSave(Boss(Core.Sinners.SinnerRoster.Warlock, 1300, 1300));
            yield return Settle();
            yield return HellPokerScreenshots.Shot("144_prophecy_before_deal");
            Table.PerformAction();
            yield return Settle(0.6f);
            yield return HellPokerScreenshots.Shot("145_prophecy_after_deal");

            // 146-147: the Peasant's satchel (small cards) in four colours and in two.
            ChapterSave satchel = Boss(Core.Sinners.SinnerRoster.Peasant, 1300, 1300);
            satchel.Satchel = new[] { new Card(Rank.Ten, Suit.Clubs), new Card(Rank.Queen, Suit.Diamonds), new Card(Rank.Ace, Suit.Spades) }.ToList();
            yield return ChapterJourneyTests.LoadWithSave(satchel);
            yield return Settle();
            yield return HellPokerScreenshots.Shot("146_small_cards_four_colours");
            SuitColors.Set(false);
            yield return Settle(0.2f);
            yield return HellPokerScreenshots.Shot("147_small_cards_two_colours");
            SuitColors.Set(true);
            Table.PerformAction();
            yield return Settle(0.6f);
            yield return HellPokerScreenshots.Shot("148_table_cards_four_colours");

            // 149-151: a demon's table — the band, won and lost; the last blow.
            yield return ChapterJourneyTests.LoadWithSave(Boss(Core.Sinners.SinnerRoster.King, 1600, 1600));
            yield return Settle();
            yield return ToTheResult();
            View.SetResultBand(new ResultBand(HandOutcome.Win, UiText.BandTitle(HandOutcome.Win), UiText.BarSentenceLines(-9, -150)));
            View.PlayMoment(TableMoment.WinGlow);
            View.FloatAmount(UiText.SignedPercent(-9), Tone.Triumph, overBar: true);
            yield return new WaitForSeconds(0.05f);
            yield return HellPokerScreenshots.Shot("149_band_win_boss");
            View.SetResultBand(new ResultBand(HandOutcome.Loss, UiText.BandTitle(HandOutcome.Loss), UiText.BarSentenceLines(6, 240)));
            yield return Settle(0.2f);
            yield return HellPokerScreenshots.Shot("150_band_loss_boss");
            yield return ChapterJourneyTests.LoadWithSave(Boss(Core.Sinners.SinnerRoster.King, 40, 1600));
            yield return Settle(0.6f);
            yield return HellPokerScreenshots.Shot("151_last_blow");

            // 152: the Devil's Compass picking a node across the floor.
            ChapterSave compass = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.Peasant, 11, 1, "");
            compass.NodeDone = true;
            compass.Relics = new[] { Core.Relics.ChapterRelicIds.DevilsCompass }.ToList();
            ChapterJourney journey = ChapterJourney.Begin(Core.Sinners.SinnerRoster.Peasant, 11);
            MapNode first = journey.Run.Map.Floors[0].First();
            MapNode second = journey.Run.Map.NextFrom(first).First();
            compass.Trail = new[] { first.Lane, second.Lane }.ToList();
            yield return ChapterJourneyTests.LoadWithSave(compass);
            yield return new WaitForSeconds(0.4f);
            ChapterJourneyTests.Chapters.UseCompass();
            yield return new WaitForSeconds(0.3f);
            yield return HellPokerScreenshots.Shot("152_compass_pick");
            Assert.IsTrue(ChapterJourneyTests.Chapters.IsCompassOut, "the compass is out");
        }
    }
}
