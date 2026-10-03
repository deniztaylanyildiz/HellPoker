using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Cheats;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// Cheats that may turn on their demon (Belial's slipping tongue, Lucifer's burning card and The Fall) and every other cheat,
    /// which never helps the player — checked over hundreds of shuffled hands, where each one also has to bite at least once.
    /// </summary>
    public class CheatBackfireTests
    {
        private sealed class FirstChoice : IRandomSource
        {
            public int Next(int maxExclusive) => 0;
        }

        private sealed class OnlyCheat : ICheatPolicy
        {
            private readonly ICheat _cheat;
            public OnlyCheat(ICheat cheat) { _cheat = cheat; }
            public IReadOnlyList<ICheat> Cheats => new[] { _cheat };
            public CheatPick Choose(CheatContext context, IRandomSource random) => new CheatPick(_cheat);
            public ICheat Find(string id) => id == _cheat.Id ? _cheat : null;
        }

        private static readonly IHandEvaluator Evaluator = HandEvaluator.CreateDefault();

        private static HellPokerGame Stacked(string cards, ICheat cheat, int backfirePercent = 0)
        {
            var random = new FirstChoice();
            var rules = new GameRules(1000, 5000, luciferGateYears: 0);
            return new HellPokerGame(rules, TestDecks.Stacked(cards), Evaluator, new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)),
                new HouseDrawStrategy(rules.MaxDiscards), PayoutTable.CreateDefault(), null,
                new CheatSession(new OnlyCheat(cheat), 1, random, null, backfirePercent), random);
        }

        private static CheatResult Played(HellPokerGame game) => game.CheatsThisHand.Single();

        private static void StandPatToTheEnd(HellPokerGame game)
        {
            if (game.Phase == GamePhase.PlayerReveal) game.CheckToDraw();
            game.Draw(new int[0]);
            while (game.Phase == GamePhase.DrawReveal || game.Phase == GamePhase.HouseReveal)
                game.Bet(BetAction.Pass);
        }

        // ------------------------------------------------------------------ Belial's forked tongue

        [Test]
        public void ForkedTongue_Aimed_BreaksTheFourOfASuit_AndNeverHelps()
        {
            // Four clubs and a 9♥. Aimed, the tongue turns a club away (the 2♣ into the 2♠) — never the 9♥ into a club.
            var game = Stacked("9H 2C 5C JC KC  2D 2H 5S 7H 4D  3S 6D 10S 9C 2S QD QC 8H 7C 3H", new ForkedTongueCheat());
            game.PlaceBet();

            StandPatToTheEnd(game);

            CheatResult result = Played(game);
            Assert.AreEqual(new Card(Rank.Two, Suit.Clubs), result.Lost);
            Assert.AreEqual(new Card(Rank.Two, Suit.Spades), result.Gained);
            Assert.IsFalse(result.Backfired);
            Assert.AreEqual(HandCategory.HighCard, game.LastRound.Showdown.Player.Category);
        }

        [Test]
        public void ForkedTongue_Slipping_MayHandThePlayerAFlush_ItIsABackfire()
        {
            // The same hand, but the tongue slips (the dice land under Belial's 20%): the 9♥ turns into the 9♣. A flush.
            var game = Stacked("9H 2C 5C JC KC  2D 2H 5S 7H 4D  3S 6D 10S 9C 2S QD QC 8H 7C 3H", new ForkedTongueCheat(),
                DealerRoster.BelialBackfirePercent);
            game.PlaceBet();

            StandPatToTheEnd(game);

            CheatResult result = Played(game);
            Assert.AreEqual(new Card(Rank.Nine, Suit.Clubs), result.Gained);
            Assert.IsTrue(result.Backfired, "The liar's tongue slipped — in the player's favour.");
            Assert.AreEqual(HandCategory.Flush, game.LastRound.Showdown.Player.Category);
        }

        [Test]
        public void Belial_SlipsOneTimeInFive_AndTheOthersDoNot()
        {
            Assert.AreEqual(20, DealerRoster.Belial.BackfirePercent);
            Assert.AreEqual(0, DealerRoster.Mammon.BackfirePercent);
            Assert.AreEqual(0, DealerRoster.Lilith.BackfirePercent);
            Assert.AreEqual(0, DealerRoster.Lucifer.BackfirePercent);
            Assert.AreEqual(35, DealerRoster.Belial.WithBackfirePercent(35).BackfirePercent, "Tunable on the dealer.");
        }

        // ------------------------------------------------------------------ Lucifer's chance cheats

        [Test]
        public void BurningCard_TheNewCardIsChance_ItMayBackfire()
        {
            // The K♥ burns (no combination); the next card of the deck is the 2♦ — a pair of twos for the player.
            var game = Stacked("2C 5D KH 9S JC  3S 4S 6H 7H 8S  2D 3H 6D 10S QD QC 8H 7C", new BurningCardCheat());
            game.PlaceBet();
            game.CheckToDraw();

            CheatResult result = Played(game);
            Assert.AreEqual(new Card(Rank.King, Suit.Hearts), result.Lost);
            Assert.AreEqual(new Card(Rank.Two, Suit.Diamonds), result.Gained);
            Assert.IsTrue(result.Backfired);
        }

        [Test]
        public void TheFall_MayBackfire_WhenTheNewCardIsBetter()
        {
            // 150 years at Lucifer's table: a pair of queens beats the jacks, so The Fall comes — the A♥ is dealt again as the
            // Q♣, three queens; the House's J♠ falls to the 2♠. Still a win, and a better one.
            Dealer lucifer = DealerRoster.Lucifer;
            var random = new FirstChoice();
            var game = new HellPokerGame(lucifer.ApplyTo(GameRules.Default),
                TestDecks.Stacked("AH QH QD 5C 2D  JS JH 9D 4C 3S  8H 7S 6H QC 2S 4D 5H 6C"), Evaluator,
                new CardExchanger(new MaxDiscardPolicy(3)), new HouseDrawStrategy(3), lucifer.Payouts, null,
                new CheatSession(lucifer.Cheats, lucifer.MaliceMax, random), random);
            game.TakeOver(150, 5);
            game.PlaceBet();
            Assert.AreEqual(CheatIds.TheFall, game.PendingCheat.Id);

            StandPatToTheEnd(game);

            CheatResult result = Played(game);
            Assert.AreEqual(CheatIds.TheFall, result.CheatId);
            Assert.AreEqual(new Card(Rank.Queen, Suit.Clubs), result.Gained);
            Assert.IsTrue(result.Backfired);
            Assert.AreEqual(HandCategory.ThreeOfAKind, game.LastRound.Showdown.Player.Category);
            Assert.AreEqual(ShowdownOutcome.PlayerWins, game.LastRound.Showdown.Outcome);
        }

        // ------------------------------------------------------------------ the save and the records remember them

        [Test]
        public void TheSave_KeepsTheRunsBackfires_AndAnOlderV3SaveHasNone()
        {
            var stats = new RunStats(1000, "belial");
            stats.NoteBackfires(2);
            string text = new RunSnapshot("belial", 800, 9, stats).Encode();

            Assert.IsTrue(RunSnapshot.TryDecode(text, out RunSnapshot back));
            Assert.AreEqual(2, back.Stats.Backfires);

            string older = string.Join("\n", text.Split('\n').Where(line => !line.StartsWith("backfires=")));
            Assert.IsTrue(RunSnapshot.TryDecode(older, out RunSnapshot old), "A v=3 save from before backfires still reads.");
            Assert.AreEqual(0, old.Stats.Backfires);
        }

        [Test]
        public void TheRecords_CountEveryBackfireSeen()
        {
            var book = new RecordBook();
            book.NoteBackfires(1);
            book.NoteBackfires(2);

            RecordBook back = RecordBook.Decode(book.Encode());

            Assert.AreEqual(3, back.BackfiresSeen);
            Assert.AreEqual(0, RecordBook.Decode("v=1\nruns=1\nabsolved=0\ndamned=0\nfastest=").BackfiresSeen, "An older book has seen none.");
        }

        // ------------------------------------------------------------------ every other cheat: never a gift, but it bites

        private static IEnumerable<ICheat> NeverHelping()
        {
            yield return new CollateralCheat();
            yield return new TitheCheat();
            yield return new BuyoutCheat();
            yield return new FalseFaceCheat();
            yield return new ForkedTongueCheat();   // aimed: the tongue never slips here (0%)
            yield return new SerpentSwapCheat();
            yield return new NightVeilCheat();
            yield return new ThornCheat();
            yield return new MoonlessCheat();
            yield return new GazeCheat();
            yield return new RewriteCheat();
        }

        [TestCaseSource(nameof(NeverHelping))]
        public void ThisCheat_NeverHelpsThePlayer_AndBitesAtLeastOnce(ICheat cheat)
        {
            int played = 0;
            for (int seed = 1; seed <= 300; seed++)
            {
                var cheatRandom = new SystemRandomSource(seed * 31 + 7);
                var rules = new GameRules(1000, 5000, luciferGateYears: 0);
                var game = new HellPokerGame(rules, new Deck(new FisherYatesShuffler(new SystemRandomSource(seed))), Evaluator,
                    new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)), new HouseDrawStrategy(rules.MaxDiscards), PayoutTable.CreateDefault(),
                    null, new CheatSession(new OnlyCheat(cheat), 1, cheatRandom, null, backfirePercent: 0), cheatRandom);

                game.PlaceBet();
                if (game.Phase == GamePhase.PlayerReveal) game.CheckToDraw();
                Hand before = game.PlayerHand;
                // A sensible draw (what the House would throw), minus anything chained.
                int[] toss = new HouseDrawStrategy(rules.MaxDiscards).ChooseDiscards(game.PlayerHand)
                    .Where(i => !game.IsPlayerCardChained(i)).ToArray();
                game.Draw(toss);
                while (game.Phase == GamePhase.DrawReveal || game.Phase == GamePhase.HouseReveal)
                    game.Bet(BetAction.Pass);

                foreach (CheatResult result in game.CheatsThisHand)
                {
                    Assert.IsFalse(result.Backfired, $"Seed {seed}: {cheat.Id} helped the player ({before} → {game.PlayerHand}).");
                    if (result.Outcome == CheatOutcome.Played) played++;
                }
            }

            Assert.Greater(played, 0, $"{cheat.Id} never struck in 300 hands: it does nothing.");
        }
    }
}
