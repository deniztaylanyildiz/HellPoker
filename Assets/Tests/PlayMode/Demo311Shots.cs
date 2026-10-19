using System.Collections;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Chapters;
using HellPoker.Core.Game;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HellPoker.PlayMode.Tests
{
    /// <summary>
    /// Demo 3.1.1's screens (Screenshots/, or HELLPOKER_SHOTS; HELLPOKER_LANG=tr): every hand's own motion caught mid-way (two pair, trips'
    /// sparks, the straight's wave, the flush's rain and tone), a big loss's red edge (after the House's quads, and with a pair), How to
    /// Play's classes and relics pages. Not a real test (Explicit).
    /// </summary>
    [Explicit]
    public class Demo311Shots
    {
        private const string HouseNothing = "2D 4H 6S 8C 10D";
        private const string Draws = " 3S 5H 7D 8H JD";

        [TearDown]
        public void Restore() => HellPokerBootstrap.ForceSimulator = null;

        private static ChapterSave Boss() => new ChapterSave
        {
            ClassId = Core.Sinners.King.ClassId, Seed = 7, Chapter = 1, Coins = 40, Years = 1600, Match = "boss", BossBarStart = 1600, BossBar = 1600,
            NodeDone = false, Relics = Core.Relics.ChapterRelics.ClassChain(Core.Sinners.King.ClassId).ToList()
        };

        [UnityTest, Timeout(900000)]
        public IEnumerator CaptureDemoThreeOneOne()
        {
            var shots = new (string player, string house, string name, float at, bool raise)[]
            {
                ("AS AD 7C 7H JS", HouseNothing, "330_two_pair", 0.35f, false),
                ("QS QD QC 4C 3H", HouseNothing, "331_trips_sparks", 0.3f, false),
                ("9S 8D 7C 6H 5S", HouseNothing, "332_straight_wave", 0.3f, false),
                ("2H 6H 9H JH KH", HouseNothing, "333_flush_rain", 0.4f, false),
                ("2H 6H 9H JH KH", HouseNothing, "334_flush_tone", 0.75f, false),
                ("3D 5C 7H JC KD", "9C 9D 9H 9S 2C", "335_house_quads", 0.2f, false),
                ("3D 5C 7H JC KD", "9C 9D 9H 9S 2C", "336_big_loss_red_edge", 0.85f, false),
                ("3D 5C 7H JC KD", "AS AD 9C 6D 2C", "337_big_loss_pair", 0.15f, true)
            };
            foreach (var (player, house, name, at, raise) in shots)
            {
                yield return ChapterJourneyTests.LoadWithSave(Boss());
                yield return ChapterJourneyTests.WaitForChapterTable();
                yield return PlayUntilTheFanfare(player, house, raise);
                yield return new WaitForSeconds(at);
                yield return HellPokerScreenshots.Shot(name);
                if (name.StartsWith("336"))
                {
                    // The flicker switches on and off: two more frames of it.
                    yield return new WaitForSeconds(0.07f);
                    yield return HellPokerScreenshots.Shot(name + "_b");
                    yield return new WaitForSeconds(0.07f);
                    yield return HellPokerScreenshots.Shot(name + "_c");
                }
                ChapterJourneyTests.ChapterTableView.SkipAnimations();
                yield return new WaitForSeconds(0.3f);
            }

            // How to Play: the classes' page and the relics' page, their new pictures beside the words.
            yield return ChapterJourneyTests.LoadWithSave(Boss());
            ChapterJourneyTests.Menu.GoBack();
            yield return new WaitForSeconds(0.6f);
            HellPokerScreenshots.Press("HowToPlayButton");
            yield return new WaitForSeconds(0.4f);
            var menu = Object.FindFirstObjectByType<MainMenuView>();
            menu.HowTo.Show(6);
            yield return null;
            yield return HellPokerScreenshots.Shot("338_howto_classes");
            menu.HowTo.Show(7);
            yield return null;
            yield return HellPokerScreenshots.Shot("339_howto_relics");
        }

        private static IEnumerator PlayUntilTheFanfare(string player, string house, bool raise)
        {
            var table = (TablePresenter)ChapterJourneyTests.Chapters.Table;
            TableView view = ChapterJourneyTests.ChapterTableView;
            var head = CardCodes.TryParseDeck(string.Join(",", (player + " " + house + Draws).Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries))).ToList();
            ((HellPokerGame)table.Game).StackTheDeal(head.Concat(Deck.CreateStandardCards().Where(c => !head.Contains(c))).ToList());
            int guard = 0;
            while (guard++ < 30)
            {
                GamePhase phase = table.Game.Phase;
                bool deciding = phase == GamePhase.PlayerReveal || phase == GamePhase.DrawReveal || phase == GamePhase.HouseReveal;
                if (raise && deciding) table.Bet(BetAction.Raise);
                else if (raise && phase == GamePhase.HouseReRaise) table.Bet(BetAction.Call);
                if (!raise || table.Game.Phase == phase && phase != GamePhase.HouseReRaise) table.PerformAction();
                float started = Time.time;
                yield return null;
                while (view.IsBusy && view.Fanfare.Playing == HandTier.None && Time.time - started < 15f) yield return null;
                if (view.Fanfare.Playing != HandTier.None || table.Game.Phase == GamePhase.RoundOver || table.Game.IsGameOver) yield break;
            }
        }
    }
}
