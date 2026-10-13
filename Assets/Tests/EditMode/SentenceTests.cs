using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Chapters;
using HellPoker.Core.Dealers;
using HellPoker.Core.Game;
using HellPoker.Core.Sinners;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Demo 2.1's sentence: the run's years, apart from the demons' bars — a win takes off both, a loss heals the bar by half
    /// and goes on the sentence whole, years owed go on both; the score, the save, the records.</summary>
    public class SentenceTests
    {
        private const string Royal = "AS KS QS JS 10S";
        private const string HouseNothing = "2D 4H 6S 8C 10D";
        private const string HouseQuads = "9C 9D 9H 9S 2C";
        private const string PlayerNothing = "3D 5C 7H JC KD";

        private static IReadOnlyList<Card> DeckStarting(string first)
        {
            var head = TestCards.Cards(first).ToList();
            return head.Concat(Deck.CreateStandardCards().Where(c => !head.Contains(c))).ToList();
        }

        /// <summary>Mammon's table for a bar of <paramref name="bar"/>, as a chapter opens it (no hooks: no interest).</summary>
        private static HellPokerGame Mammon(int bar)
        {
            ChapterRules rules = ChapterRules.For(1);
            var sinner = new Sinner(SinnerRoster.Peasant);
            return BossTable.Create(GameRules.Default, rules.Boss, bar, rules.SoulLinePercent, rules.BossWinPercent, rules.BossLossPercent, 5, sinner, sinner);
        }

        /// <summary>One hand on a stacked deck: the player checks to the draw, keeps everything, passes and calls.</summary>
        private static RoundResult PlayOne(HellPokerGame game, string cards)
        {
            if (game.Phase == GamePhase.RoundOver) game.NextRound();
            game.RestoreDeck(DeckStarting(cards));
            game.PlaceBet();
            if (game.Phase == GamePhase.PlayerReveal && game.CanCheckToDraw(out _)) game.CheckToDraw();
            while (game.Phase != GamePhase.RoundOver && !game.IsGameOver)
            {
                if (game.Phase == GamePhase.Drawing) game.Draw(Array.Empty<int>());
                else game.Bet(game.Phase == GamePhase.HouseReRaise ? BetAction.Call : BetAction.Pass);
            }
            return game.LastRound;
        }

        [Test]
        public void AWin_TakesItsYearsOffTheBarAndTheSentence()
        {
            HellPokerGame game = Mammon(1300);
            var sentence = new RunSentence(6500);
            sentence.Follow(game);
            RoundResult round = PlayOne(game, Royal + " " + HouseNothing);
            Assert.Less(round.YearsChange, 0, "a royal wins");
            Assert.AreEqual(6500 + round.YearsChange, sentence.Years, "the same years off the sentence");
            Assert.AreEqual(0, game.UnhealedYearsThisHand);
        }

        [Test]
        public void ALoss_HealsTheBarByHalf_AndGoesOnTheSentenceWhole()
        {
            HellPokerGame game = Mammon(1300);
            Assert.AreEqual(BossTable.BarHealPercent, game.BarHealPercent);
            var sentence = new RunSentence(6500);
            sentence.Follow(game);
            RoundResult round = PlayOne(game, PlayerNothing + " " + HouseQuads);
            int loss = sentence.Years - 6500;
            Assert.Greater(loss, 0, "quads win");
            Assert.AreEqual((loss + 1) / 2, round.YearsChange, "the bar takes half of it, rounded up");
            Assert.AreEqual(loss - round.YearsChange, game.UnhealedYearsThisHand, "the rest is the sentence's alone");

            sentence.Unfollow();
            PlayOne(game, PlayerNothing + " " + HouseQuads);
            Assert.AreEqual(6500 + loss, sentence.Years, "unfollowed: the table no longer moves it");
        }

        [Test]
        public void TheDemosTables_PutAWholeLossOnTheYears()
        {
            HellPokerGame game = HellPokerGameFactory.Create(GameRules.Default, DealerRoster.Mammon, 5);
            Assert.AreEqual(100, game.BarHealPercent);
            int before = game.Years;
            RoundResult round = PlayOne(game, PlayerNothing + " " + HouseQuads);
            Assert.Greater(round.YearsChange, 0);
            Assert.AreEqual(0, game.UnhealedYearsThisHand);
            Assert.AreEqual(before + round.YearsChange, game.Years);
        }

        [Test]
        public void TheBossesLosses_AreTwiceTheBarsOldPercents()
        {
            Assert.AreEqual(250, ChapterRules.For(1).BossLossPercent);
            Assert.AreEqual(220, ChapterRules.For(2).BossLossPercent);
            Assert.AreEqual(110, ChapterRules.For(3).BossLossPercent);
            Assert.AreEqual(90, ChapterRules.LuciferLossPercent);
            Assert.AreEqual(50, BossTable.BarHealPercent);
        }

        [Test]
        public void ARunBeginsWithTheClassesWholeSentence_TheCirclesBarsOnIt()
        {
            Assert.AreEqual(6500, ChapterJourney.Begin(SinnerRoster.Peasant, 1).Sentence.Years);
            Assert.AreEqual(8000, ChapterJourney.Begin(SinnerRoster.Find(King.ClassId), 1).Sentence.Years);
            Assert.AreEqual(5000, ChapterJourney.Begin(SinnerRoster.Find(Jester.ClassId), 1).Sentence.Years);
            Assert.AreEqual(5800, ChapterJourney.Begin(SinnerRoster.Find(Summoner.ClassId), 1).Sentence.Years);
            Assert.AreEqual(6500 * Circles.BarAt(3) / 100, ChapterJourney.Begin(SinnerRoster.Peasant, 1, circle: 3).Sentence.Years, "the third circle's thicker bars");
        }

        [Test]
        public void YearsOwedAndStruck_MoveTheSentenceAtOnce_TheTributesAtTheGate()
        {
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Peasant, 3);
            ChapterRun run = journey.Run;
            run.OweAtGate(100);
            Assert.AreEqual(6600, journey.Sentence.Years);
            Assert.AreEqual(1300, run.Years, "the bar takes it at the gate");
            run.StrikeYears(200);
            Assert.AreEqual(6400, journey.Sentence.Years);

            run.Purse.Add(-run.Purse.Coins);   // nothing to pay with
            GateToll toll = run.PayTribute();
            Assert.Greater(toll.YearsForMissing, 0);
            Assert.AreEqual(6400 + toll.YearsForMissing, journey.Sentence.Years, "the missing coins' years on the sentence too");
            Assert.AreEqual(1300 - 200 + 100 + toll.YearsForMissing, run.Years, "and on the bar, with what was owed");
        }

        [Test]
        public void TheDemonsTable_MovesTheSentence_ItsEndLeavesItAlone()
        {
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Peasant, 4);
            journey.Run.PayTribute();
            int before = journey.Sentence.Years;
            HellPokerGame game = journey.Run.OpenBossTable(GameRules.Default);
            RoundResult round = PlayOne(game, Royal + " " + HouseNothing);
            Assert.AreEqual(before + round.YearsChange + game.UnhealedYearsThisHand, journey.Sentence.Years);
            int after = journey.Sentence.Years;
            journey.Run.LeaveBossTable(0);
            Assert.AreEqual(after, journey.Sentence.Years, "the demon beaten: what is left of the sentence goes on");
        }

        [Test]
        public void TheSentenceIsSaved_AnOldSaveReckonsIt()
        {
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Find(Warlock.ClassId), 31);
            journey.Run.OweAtGate(150);
            ChapterSave save = journey.Capture();
            Assert.AreEqual(6650, save.Sentence);
            Assert.IsTrue(ChapterSave.TryDecode(save.Encode(), out ChapterSave read));
            Assert.AreEqual(6650, ChapterJourney.Restore(read).Sentence.Years);

            // A Demo 2.0.1 save has no sentence: this demon's share and what is owed, and the shares still ahead.
            string old = string.Join("\n", save.Encode().Split('\n').Where(l => !l.StartsWith("sentence=", StringComparison.Ordinal)));
            Assert.IsTrue(ChapterSave.TryDecode(old, out ChapterSave older));
            Assert.AreEqual(-1, older.Sentence);
            Assert.AreEqual(1300 + 150 + 1950 + 2200 + 1050, ChapterJourney.Restore(older).Sentence.Years);
        }

        [Test]
        public void TheRecords_KeepTheFewestYearsLeft_PerClassAndCircle()
        {
            var records = new ChapterRecords();
            records.RunEnded(JourneyEnd.Freed, 4, Peasant.ClassId, 100, 0, 900);
            records.RunEnded(JourneyEnd.Freed, 4, Peasant.ClassId, 100, 0, 420);
            records.RunEnded(JourneyEnd.Freed, 4, Peasant.ClassId, 100, 0, 700);
            records.RunEnded(JourneyEnd.Freed, 4, Peasant.ClassId, 100, 1, 0);
            records.RunEnded(JourneyEnd.CastDown, 4, King.ClassId, 100, 0, 10);
            ChapterRecords back = ChapterRecords.Decode(records.Encode());
            Assert.AreEqual(420, back.LowestSentence(Peasant.ClassId, 0));
            Assert.AreEqual(0, back.LowestSentence(Peasant.ClassId, 1), "spotless");
            Assert.IsNull(back.LowestSentence(King.ClassId, 0), "only a freedom is a score");
        }
    }
}
