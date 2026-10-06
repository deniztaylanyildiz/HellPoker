using System.Linq;
using System.Reflection;
using HellPoker.Core.Cards;
using HellPoker.Core.Cheats;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Core.Sinners;
using HellPoker.Presentation;
using HellPoker.Presentation.Settings;
using HellPoker.Presentation.Ui;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// A hand the jokers have already decided ends at once, at the stake on the table — no betting round left open. After the draw
    /// the counts are final; before it, every count still reachable (throwing jokers back, drawing the deck's) must agree. A hand
    /// the cards may still decide is never ended early. Payouts, the joker rules and the count are as at any showdown.
    /// </summary>
    public class JokerSettleTests
    {
        private const string Nothing = "2C 5D 7H 9S JC";
        private const string Blanks = "3S 6D 10S 8H 3H QC 8C 7C 6H 4S 10D 3D";

        private static Sinner Jester(int jokers) => new Sinner(SinnerRoster.Jester, jokers: jokers);

        private static HellPokerGame Game(string player, string house, Sinner sinner, string blanks = Blanks)
        {
            var rules = new GameRules(1000, 5000, luciferGateYears: 0);
            var game = new HellPokerGame(rules, TestDecks.Stacked($"{player} {house} {blanks}"), HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)), new HouseDrawStrategy(rules.MaxDiscards), PayoutTable.CreateDefault(),
                null, null, null, sinner);
            game.TakeOver(1000, 0);
            return game;
        }

        private static void ToTheDraw(HellPokerGame game)
        {
            game.PlaceBet();
            if (game.Phase == GamePhase.PlayerReveal) game.CheckToDraw();
        }

        [Test]
        public void TwoJokersInAll_BothThePlayers_NotSettledBeforeTheDraw_KeptBoth_LostAtOnceAfterIt()
        {
            Sinner sinner = Jester(2);
            HellPokerGame game = Game("JK1 JK2 AS AH 9D", Nothing, sinner);
            game.PlaceBet();
            Assert.IsNull(game.JokerOutcomeIsSettled(), "He may still throw one back.");
            Assert.AreEqual(GamePhase.PlayerReveal, game.Phase);
            Assert.IsTrue(game.CanBet(BetAction.Raise, out _), "Betting is open.");
            game.CheckToDraw();
            Assert.AreEqual(GamePhase.Drawing, game.Phase);

            game.Draw(new int[0]);   // both kept
            Assert.AreEqual(GamePhase.RoundOver, game.Phase, "No betting round after the draw.");
            Assert.IsTrue(game.LastRound.SettledByJokers);
            Assert.AreEqual(ShowdownOutcome.HouseWins, game.LastRound.Showdown.Outcome);
            Assert.AreEqual(game.Ante, game.LastRound.Stake, "The stake on the table: the ante, no raise.");
            Assert.Greater(game.LastRound.YearsChange, 0);
            Assert.IsFalse(game.LastRound.Folded, "A showdown lost, not a fold.");
            Assert.AreEqual(5, game.PlayerCardsRevealed);
            Assert.AreEqual(5, game.HouseCardsRevealed);
            Assert.AreEqual(2, sinner.Jokers, "A loss at ten or below: the count stays.");
        }

        [Test]
        public void TwoJokersInAll_OneThrownBack_TheHandGoesOn()
        {
            HellPokerGame game = Game("JK1 JK2 AS AH 9D", Nothing, Jester(2));
            ToTheDraw(game);
            game.Draw(new[] { 1 });
            Assert.IsNull(game.LastRound, "Still being played.");
            Assert.AreNotEqual(GamePhase.RoundOver, game.Phase);
        }

        [Test]
        public void TheDemonKeepsTwoJokers_AfterTheDraw_HeLosesAtOnce()
        {
            // The House throws its extra joker back and draws the third: two again.
            Sinner sinner = Jester(3);
            HellPokerGame game = Game(Nothing, "JK1 JK2 AS AH AD", sinner, "JK3 " + Blanks.Replace("3S ", ""));
            ToTheDraw(game);
            Assert.IsNull(game.JokerOutcomeIsSettled(), "Before the draw the House may end with one.");
            game.Draw(new int[0]);
            Assert.IsTrue(game.LastRound.SettledByJokers);
            Assert.AreEqual(ShowdownOutcome.PlayerWins, game.LastRound.Showdown.Outcome);
            Assert.AreEqual(4, sinner.Jokers, "A win: one more.");
        }

        [Test]
        public void ThreeJokersInAll_TwoThePlayers_OneTheDemons_LostAfterTheDraw()
        {
            HellPokerGame game = Game("JK1 JK2 AS AH AD", "JK3 KS KH 2C 4D", Jester(3));
            ToTheDraw(game);
            game.Draw(new int[0]);
            Assert.IsTrue(game.LastRound.SettledByJokers);
            Assert.AreEqual(ShowdownOutcome.HouseWins, game.LastRound.Showdown.Outcome);
        }

        [Test]
        public void TwoEach_APush_NoYears_TheCountStays()
        {
            Sinner sinner = Jester(5);
            HellPokerGame game = Game("JK1 JK2 AS AH AD", "JK3 JK4 2C 4D 5H", sinner, "JK5 " + Blanks.Replace("3S ", ""));
            ToTheDraw(game);
            game.Draw(new int[0]);
            Assert.IsTrue(game.LastRound.SettledByJokers);
            Assert.AreEqual(ShowdownOutcome.Push, game.LastRound.Showdown.Outcome);
            Assert.AreEqual(0, game.LastRound.YearsChange);
            Assert.AreEqual(5, sinner.Jokers);
        }

        [Test]
        public void TwoChainedJokers_NoJokerLeftInTheDeck_LostBeforeTheDraw()
        {
            HellPokerGame game = Game("JK1 JK2 AS AH 9D", Nothing, Jester(2));
            game.PlaceBet();
            // Chained (a test's hand: no cheat chains a joker): they cannot be thrown back, and no joker is left to come.
            var session = (CheatSession)typeof(HellPokerGame).GetField("_cheats", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(game);
            session.Marks.Chained.Add(Card.Joker(1));
            session.Marks.Chained.Add(Card.Joker(2));
            Assert.AreEqual(ShowdownOutcome.HouseWins, game.JokerOutcomeIsSettled());
            game.Bet(BetAction.Pass);
            Assert.AreEqual(GamePhase.RoundOver, game.Phase, "It ends at the next step: no draw.");
            Assert.IsTrue(game.LastRound.SettledByJokers);
            Assert.IsNull(game.LastRound.PlayerExchange, "No draw was played.");
        }

        [Test]
        public void JokersLeftInTheDeck_NothingSettled_TheBettingIsNormal()
        {
            // Three jokers: two in his hand, one still in the deck — the House could draw it, he could throw his back.
            HellPokerGame game = Game("JK1 JK2 AS AH 9D", Nothing, Jester(3), Blanks + " JK3");
            game.PlaceBet();
            Assert.IsNull(game.JokerOutcomeIsSettled());
            game.Bet(BetAction.Raise);
            Assert.IsNull(game.LastRound);
            Assert.AreEqual(GamePhase.PlayerReveal, game.Phase);
        }

        [Test]
        public void NoJokers_NothingEverSettlesEarly()
        {
            var rules = new GameRules(1000, 5000, luciferGateYears: 0);
            var game = new HellPokerGame(rules, TestDecks.Stacked($"KS KH 2C 5D 9C {Nothing} {Blanks}"), HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)), new HouseDrawStrategy(rules.MaxDiscards), PayoutTable.CreateDefault(),
                null, null, null, new Sinner(SinnerRoster.Peasant));
            game.PlaceBet();
            Assert.IsNull(game.JokerOutcomeIsSettled());
            game.CheckToDraw();
            game.Draw(new int[0]);
            Assert.AreEqual(GamePhase.DrawReveal, game.Phase);
        }

        // ------------------------------------------------------------------ the table

        [Test]
        public void TheTable_SaysTheCounts_InBothLanguages_AndTheLogTellsIt()
        {
            var view = new FakeTableView();
            HellPokerGame game = null;
            var presenter = new TablePresenter((d, sinner) =>
            {
                var rules = d.ApplyTo(new GameRules(1000, 5000, luciferGateYears: 0));
                return game = new HellPokerGame(rules, TestDecks.Stacked($"JK1 JK2 AS AH 9D {Nothing} {Blanks}"), HandEvaluator.CreateDefault(),
                    new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)), new HouseDrawStrategy(rules.MaxDiscards), d.Payouts, null, null, null, sinner);
            }, view, runLog: new MemoryRunLogSink());
            try
            {
                presenter.Resume(DealerRoster.Mammon,
                    new RunSnapshot("mammon", 1000, 3, new RunStats(1000, "mammon"), classId: HellPoker.Core.Sinners.Jester.ClassId, classJokers: 2));
                view.PressAction();        // deal
                view.PressCheckToDraw();
                view.PressAction();        // stand pat: both jokers kept
                Assert.AreEqual(GamePhase.RoundOver, game.Phase);
                StringAssert.StartsWith(string.Format(UiText.SettledLossFormat, 2, 0), view.Message);
                StringAssert.Contains("SETTLED BY JOKERS: player 2 vs house 0: HouseWins", presenter.Log.ToText());
            }
            finally
            {
                presenter.Dispose();
            }

            Lang.Set(Language.Turkish);
            try
            {
                Assert.AreEqual("Destedeki jokerler hesaplandı: sende 2, şeytanda 0. El kesin kaybedildi, bahis yok.", string.Format(UiText.SettledLossFormat, 2, 0));
                Assert.AreEqual("Destedeki jokerler hesaplandı: sende 0, şeytanda 2. El kesin kazanıldı, bahis yok.", string.Format(UiText.SettledWinFormat, 0, 2));
                Assert.AreEqual("Destedeki jokerler hesaplandı: sende 2, şeytanda 2. El berabere, yıl değişmez.", string.Format(UiText.SettledPushFormat, 2, 2));
            }
            finally
            {
                Lang.Set(Language.English);
            }
        }
    }
}
