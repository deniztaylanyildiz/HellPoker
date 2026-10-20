using System.Collections;
using System.Collections.Generic;
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
    /// Demo 3.1's screens (Screenshots/, or HELLPOKER_SHOTS; HELLPOKER_LANG=tr for Turkish): the player's panel at a floor's table and a
    /// demon's, each hand ending's tier caught in the middle of its animation, the settings' SCREEN SHAKE row. Not a real test (Explicit).
    /// </summary>
    [Explicit]
    public class Demo31Shots
    {
        private const string HouseNothing = "2D 4H 6S 8C 10D";
        private const string HouseQuads = "9C 9D 9H 9S 2C";
        private const string Draws = " 3S 5H 7D 8H JD";


        private static ChapterSave Boss(Core.Sinners.SinnerClass cls) => new ChapterSave
        {
            ClassId = cls.Id, Seed = 7, Chapter = 1, Coins = 40, Years = 1600, Match = "boss", BossBarStart = 1600, BossBar = 1600, NodeDone = false,
            Relics = Core.Relics.ChapterRelics.ClassChain(cls.Id).Concat(new[] { Core.Relics.RelicIds.BoneDie }).ToList()
        };

        [UnityTest, Timeout(900000)]
        public IEnumerator CaptureDemoThreeOne()
        {
            // The panel: every class at a demon's table, dealt.
            foreach (Core.Sinners.SinnerClass cls in Core.Sinners.SinnerRoster.PhaseTwo)
            {
                yield return ChapterJourneyTests.LoadWithSave(Boss(cls));
                yield return ChapterJourneyTests.WaitForChapterTable();
                ChapterJourneyTests.Chapters.Table.PerformAction();
                yield return ChapterJourneyTests.WaitForChapterTable();
                yield return new WaitForSeconds(0.6f);
                yield return HellPokerScreenshots.Shot($"310_panel_{cls.Id}");
            }

            // The panel at a floor's table.
            ChapterSave floor = ChapterPresenter.PlacedSave(Core.Sinners.SinnerRoster.Peasant, 11, 1, "chapter");
            floor.Relics = new List<string> { Core.Relics.RelicIds.FerrymansCoin, Core.Relics.ChapterRelicIds.IronVow, Core.Relics.ChapterRelicIds.BlackBook,
                Core.Relics.ChapterRelicIds.LeadSeal, Core.Relics.ChapterRelicIds.AshPurse };
            yield return ChapterJourneyTests.LoadWithSave(floor);
            HellPokerScreenshots.Press("PanelOption0");
            yield return new WaitForSeconds(0.3f);
            MapNode first = ChapterJourneyTests.Chapters.Run.Choices.First(n => n.Kind == NodeKind.Table);
            HellPokerScreenshots.Find<UnityEngine.UI.Button>($"Node{first.Floor}_{first.Lane}").onClick.Invoke();
            yield return ChapterJourneyTests.WaitForChapterTable();
            ChapterJourneyTests.Chapters.Table.PerformAction();
            yield return ChapterJourneyTests.WaitForChapterTable();
            yield return new WaitForSeconds(0.6f);
            yield return HellPokerScreenshots.Shot("311_panel_floor_five_relics");

            // Every tier, in the middle of its animation.
            var tiers = new (string player, string house, string name, float at)[]
            {
                ("AS AD 7C 3C JS", HouseNothing, "320_plain", 0.3f), ("AS AD 7C 7H JS", HouseNothing, "321_good", 0.45f),
                ("9S 8D 7C 6H 5S", HouseNothing, "322_strong", 0.75f), ("KS KD KC 5S 5C", HouseNothing, "323_full", 0.7f),
                ("QS QD QC QH JS", HouseNothing, "324_quads_flash", 0.02f), ("QS QD QC QH JS", HouseNothing, "325_quads_rain", 1.2f),
                ("AS KS QS JS 10S", HouseNothing, "326_blaze", 1.4f), ("3D 5C 7H JC KD", HouseQuads, "327_house_strong", 0.15f)
            };
            foreach (var (player, house, name, at) in tiers)
            {
                yield return ChapterJourneyTests.LoadWithSave(Boss(Core.Sinners.SinnerRoster.King));
                yield return ChapterJourneyTests.WaitForChapterTable();
                yield return PlayUntilTheFanfare(player, house);
                yield return new WaitForSeconds(at);
                yield return HellPokerScreenshots.Shot(name);
                ChapterJourneyTests.ChapterTableView.SkipAnimations();
                yield return new WaitForSeconds(0.3f);
                if (name == "323_full") yield return HellPokerScreenshots.Shot("328_after_the_fanfare");
            }

            // The settings' display tab: SCREEN SHAKE.
            yield return ChapterJourneyTests.LoadWithSave(Boss(Core.Sinners.SinnerRoster.King));
            ChapterJourneyTests.Menu.GoBack();
            yield return new WaitForSeconds(0.6f);
            HellPokerScreenshots.Press("SettingsButton");
            yield return new WaitForSeconds(0.6f);
            HellPokerScreenshots.Press("SettingsTabDisplay");
            yield return null;
            yield return HellPokerScreenshots.Shot("329_settings_screen_shake");
        }

        private static IEnumerator PlayUntilTheFanfare(string player, string house)
        {
            var table = (TablePresenter)ChapterJourneyTests.Chapters.Table;
            TableView view = ChapterJourneyTests.ChapterTableView;
            var head = CardCodes.TryParseDeck(string.Join(",", (player + " " + house + Draws).Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries))).ToList();
            ((HellPokerGame)table.Game).StackTheDeal(head.Concat(Deck.CreateStandardCards().Where(c => !head.Contains(c))).ToList());
            int guard = 0;
            while (guard++ < 30)
            {
                table.PerformAction();
                float started = Time.time;
                yield return null;
                while (view.IsBusy && view.Fanfare.Playing == HandTier.None && Time.time - started < 15f) yield return null;
                if (view.Fanfare.Playing != HandTier.None || table.Game.Phase == GamePhase.RoundOver || table.Game.IsGameOver) yield break;
            }
        }
    }
}
