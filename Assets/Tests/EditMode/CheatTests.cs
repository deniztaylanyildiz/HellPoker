using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Betting;
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
    /// The demons' cheats in the rules: the malice gauge, the choice (minor / major, Belial's lies, Lucifer's Fall), every
    /// cheat's effect on the cards, the Dead Man's Hand's immunity, the guard hook, and how cheats live with the seal, the
    /// soul and the save.
    /// </summary>
    public class CheatTests
    {
        private const string Nothing = "2C 5D 7H 9S JC";
        private const string HouseFullHouse = "KS KH KD 4C 4H";
        private const string Flush = "2C 9C JC 4C KC";
        private const string HouseTwos = "2D 2H 5S 7H 9D";
        private const string Blanks = "3S 6D 10S 2S 9H QD QC 8H 7C 3H";

        /// <summary>Always the first option: the first target, the first cheat, a major one and a lie whenever possible.</summary>
        private sealed class FirstChoice : IRandomSource
        {
            public int Next(int maxExclusive) => 0;
        }

        /// <summary>Always this cheat, announced as itself (or as <paramref name="shown"/>).</summary>
        private sealed class OnlyCheat : ICheatPolicy
        {
            private readonly ICheat _cheat;
            private readonly ICheat _shown;
            public OnlyCheat(ICheat cheat, ICheat shown = null) { _cheat = cheat; _shown = shown; }
            public IReadOnlyList<ICheat> Cheats => new[] { _cheat };
            public CheatPick Choose(CheatContext context, IRandomSource random) => new CheatPick(_cheat, _shown);
            public ICheat Find(string id) => id == _cheat.Id ? _cheat : null;
        }

        private sealed class BlockEverything : ICheatGuard
        {
            public int Asked { get; private set; }
            public bool Allows(ICheat cheat, CheatTable table) { Asked++; return false; }
        }

        private static GameRules Rules(int tableCapPercent = 30, int startingYears = 1000) =>
            new GameRules(startingYears, stakes: new StakeScale(tableCapPercent: tableCapPercent), luciferGateYears: 0);

        /// <summary>A game where the demon plays <paramref name="cheat"/> every hand (gauge of 1).</summary>
        private static HellPokerGame Game(string player, string house, ICheat cheat, string rest = Blanks, GameRules rules = null,
            IHouseBettingStrategy betting = null, ICheatGuard guard = null, int maliceMax = 1, ICheatPolicy policy = null,
            IPayoutTable payouts = null)
        {
            var random = new FirstChoice();
            rules = rules ?? Rules();
            return new HellPokerGame(rules, TestDecks.Stacked($"{player} {house} {rest}"), HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)), new HouseDrawStrategy(rules.MaxDiscards), payouts ?? PayoutTable.CreateDefault(),
                betting, new CheatSession(policy ?? new OnlyCheat(cheat), maliceMax, random, guard), random);
        }

        private static void ToTheDraw(HellPokerGame game)
        {
            game.PlaceBet();
            if (game.Phase == GamePhase.PlayerReveal) game.CheckToDraw();
        }

        private static void PassToTheEnd(HellPokerGame game)
        {
            while (game.Phase == GamePhase.DrawReveal || game.Phase == GamePhase.HouseReveal)
                game.Bet(BetAction.Pass);
        }

        private static void PlayPassively(HellPokerGame game, params int[] discards)
        {
            ToTheDraw(game);
            game.Draw(discards);
            PassToTheEnd(game);
        }

        private static CheatResult Played(HellPokerGame game) => game.CheatsThisHand.Single();

        // ================================================================== the gauge and the choice

        [Test]
        public void Malice_Grows_EveryHand_AndAFullGaugeAnnouncesACheat()
        {
            var game = Game(Nothing, HouseFullHouse, new GazeCheat(), maliceMax: 3, rules: new GameRules(5000, 6000, luciferGateYears: 0));
            for (int hand = 1; hand <= 2; hand++)
            {
                game.PlaceBet();
                Assert.AreEqual(hand, game.Malice);
                Assert.IsNull(game.PendingCheat);
                game.Bet(BetAction.Fold);
                game.NextRound();
            }

            game.PlaceBet();

            Assert.AreEqual(3, game.MaliceMax);
            Assert.AreEqual(0, game.Malice, "Gaze strikes at the deal: the gauge is spent.");
            Assert.AreEqual(CheatIds.Gaze, Played(game).CheatId);
        }

        [Test]
        public void TheIntent_IsShown_UntilTheCheatStrikes()
        {
            var game = Game(Nothing, HouseFullHouse, new CollateralCheat());

            game.PlaceBet();
            Assert.AreEqual(CheatIds.Collateral, game.PendingCheat.Id, "Announced at the deal.");
            Assert.AreEqual(1, game.Malice);

            game.CheckToDraw();
            Assert.IsNull(game.PendingCheat, "Played out before the draw.");
            Assert.AreEqual(0, game.Malice);
        }

        [Test]
        public void AWin_FeedsTheDemonsMalice()
        {
            var game = Game(Flush, HouseTwos, new GazeCheat(), maliceMax: 5, rules: new GameRules(5000, 6000, luciferGateYears: 0));

            PlayPassively(game);

            Assert.AreEqual(ShowdownOutcome.PlayerWins, game.LastRound.Showdown.Outcome);
            Assert.AreEqual(2, game.Malice, "One for the hand, one for the win.");
        }

        [Test]
        public void ALowSentence_FeedsItFaster_ButNotAtTheFinalTable()
        {
            var low = Game(Nothing, HouseFullHouse, new GazeCheat(), maliceMax: 5, rules: Rules(startingYears: 500));
            low.PlaceBet();
            Assert.AreEqual(2, low.Malice, "At 500 years or less: one more every hand.");

            var final = new HellPokerGame(DealerRoster.Lucifer.ApplyTo(GameRules.Default), TestDecks.Stacked($"{Nothing} {HouseFullHouse} {Blanks}"),
                HandEvaluator.CreateDefault(), new CardExchanger(new MaxDiscardPolicy()), new HouseDrawStrategy(), PayoutTable.CreateDefault(),
                null, new CheatSession(new OnlyCheat(new GazeCheat()), 5, new FirstChoice()), new FirstChoice());
            final.TakeOver(100, 3);
            final.PlaceBet();
            Assert.AreEqual(1, final.Malice, "Not at Lucifer's table.");
        }

        [Test]
        public void MajorCheats_ComeOnlyAtOrBelow400_HalfTheTime()
        {
            var policy = DealerRoster.MammonCheats;   // FirstChoice: a major one whenever it may come

            Assert.AreEqual(CheatTier.Minor, policy.Choose(new CheatContext(401, 400, 50, false), new FirstChoice()).Cheat.Tier);
            Assert.AreEqual(CheatIds.Buyout, policy.Choose(new CheatContext(400, 400, 50, false), new FirstChoice()).Cheat.Id);
            Assert.AreEqual(CheatTier.Minor, policy.Choose(new CheatContext(400, 400, 0, false), new FirstChoice()).Cheat.Tier, "0%: never.");
        }

        [Test]
        public void Belial_SometimesAnnouncesAnotherCheat_AndTheLieComesOut()
        {
            ICheatPolicy belial = DealerRoster.BelialCheats;
            CheatPick pick = belial.Choose(new CheatContext(1000, 400, 50, false), new FirstChoice());
            Assert.IsTrue(pick.IsLie, "FirstChoice always takes the lie (25%).");
            Assert.AreNotEqual(pick.Cheat.Id, pick.Shown.Id);
            Assert.IsFalse(DealerRoster.MammonCheats.Choose(new CheatContext(1000, 400, 50, false), new FirstChoice()).IsLie, "Mammon never lies.");
            Assert.IsFalse(DealerRoster.LuciferCheats.Choose(new CheatContext(100, 400, 50, false), new FirstChoice()).IsLie);

            // At the table: the intent says one thing, the cheat that strikes is another.
            var game = Game(Nothing, HouseFullHouse, null, policy: new OnlyCheat(new CollateralCheat(), new ThornCheat()));
            game.PlaceBet();
            Assert.AreEqual(CheatIds.Thorn, game.PendingCheat.Id);
            game.CheckToDraw();
            CheatResult result = Played(game);
            Assert.AreEqual(CheatIds.Collateral, result.CheatId);
            Assert.AreEqual(CheatIds.Thorn, result.ShownId);
            Assert.IsTrue(result.WasLie);
        }

        [Test]
        public void TheGuard_IsAsked_AndABlockedCheatChangesNothing()
        {
            var guard = new BlockEverything();
            var game = Game(Nothing, HouseFullHouse, new CollateralCheat(), guard: guard);

            ToTheDraw(game);

            Assert.AreEqual(1, guard.Asked);
            Assert.AreEqual(CheatOutcome.Blocked, Played(game).Outcome);
            Assert.IsFalse(Enumerable.Range(0, 5).Any(game.IsPlayerCardChained));
            Assert.AreEqual(0, game.Malice, "A blocked cheat is spent.");
        }

        [Test]
        public void ACheatWithNothingToWorkOn_Fizzles_AndTheGaugeStaysFull()
        {
            var game = Game(Nothing, HouseFullHouse, new MoonlessCheat());

            PlayPassively(game);   // stands pat: nothing drawn, nothing to darken

            Assert.AreEqual(CheatOutcome.Fizzled, Played(game).Outcome);
            Assert.AreEqual(1, game.Malice);
        }

        [Test]
        public void AFoldBeforeTheCheatsMoment_LeavesTheGaugeFull_AndTheNextHandPicksAgain()
        {
            var game = Game(Nothing, HouseFullHouse, new CollateralCheat());
            game.PlaceBet();

            game.Bet(BetAction.Fold);

            Assert.IsEmpty(game.CheatsThisHand);
            Assert.AreEqual(1, game.Malice);
            game.NextRound();
            Assert.IsNull(game.PendingCheat, "Between hands nothing is announced.");
            game.PlaceBet();
            Assert.AreEqual(CheatIds.Collateral, game.PendingCheat.Id);
        }

        // ================================================================== the Dead Man's Hand is beyond them

        [Test]
        public void TheDeadMansCards_AreNeverTouched_TheCheatMovesToTheFifth()
        {
            var game = Game("AS AC 8S 8C 3H", HouseFullHouse, new BurningCardCheat());

            ToTheDraw(game);

            Assert.AreEqual(4, Played(game).PlayerCards.Single(), "Only the 3♥ could burn.");
            Assert.AreEqual(HandCategory.DeadMansHand, HandEvaluator.CreateDefault().Evaluate(game.PlayerHand).Category);
        }

        [Test]
        public void WithNothingButTheDeadMansCardsWorthTaking_ABuyoutComesToNothing()
        {
            // The only card that may be bought is the 3♥ — worth less than anything the House would give.
            var game = Game("AS AC 8S 8C 3H", "KD QD JD 10D 9H", new BuyoutCheat());

            ToTheDraw(game);

            Assert.AreEqual(CheatOutcome.Fizzled, Played(game).Outcome);
            Assert.AreEqual("A♠ A♣ 8♠ 8♣ 3♥", game.PlayerHand.ToString());
        }

        [Test]
        public void TheTithe_NeverTouchesTheDeadMansHand()
        {
            var game = Game("AS AC 8S 8C 3H", HouseTwos, new TitheCheat());

            PlayPassively(game);

            Assert.AreEqual(GamePhase.Absolved, game.Phase);
            Assert.AreEqual(CheatOutcome.Fizzled, Played(game).Outcome);
        }

        // ================================================================== Mammon

        [Test]
        public void Collateral_ChainsTheHighestCard_ItCannotBeThrownBack()
        {
            var game = Game(Nothing, HouseFullHouse, new CollateralCheat());

            ToTheDraw(game);

            Assert.AreEqual(4, Played(game).PlayerCards.Single(), "J♣ is the highest.");
            Assert.IsTrue(game.IsPlayerCardChained(4));
            Assert.IsFalse(game.CanDraw(new[] { 4 }, out string reason));
            StringAssert.Contains("collateral", reason);
            Assert.IsTrue(game.CanDraw(new[] { 0, 1 }, out _));
            CollectionAssert.DoesNotContain(game.SuggestedDiscards(), 4);
            Assert.Throws<InvalidOperationException>(() => game.Draw(new[] { 4 }));
        }

        [Test]
        public void Tithe_TakesAUnitOffAWin()
        {
            var game = Game(Flush, HouseTwos, new TitheCheat());

            PlayPassively(game);

            Assert.AreEqual(1000 - (100 + 100 * 4) + 100, game.Years, "A flush forgives 500, the tithe takes 100 of it.");
            Assert.AreEqual(100, game.TitheYearsThisHand);
            Assert.AreEqual(100, Played(game).Years);
        }

        [Test]
        public void Tithe_OnALoss_ComesToNothing()
        {
            var game = Game(Nothing, HouseFullHouse, new TitheCheat(), rules: new GameRules(1000, 5000, luciferGateYears: 0));

            PlayPassively(game);

            Assert.AreEqual(CheatOutcome.Fizzled, Played(game).Outcome);
            Assert.AreEqual(1, game.Malice);
        }

        [Test]
        public void Buyout_TradesThePlayersHighestForTheHousesLowest()
        {
            var game = Game("2C 5D KH 9S JC", "3D 4D 6H 7H 8D", new BuyoutCheat());

            ToTheDraw(game);

            CheatResult result = Played(game);
            Assert.AreEqual(new Card(Rank.King, Suit.Hearts), result.Lost);
            Assert.AreEqual(new Card(Rank.Three, Suit.Diamonds), result.Gained);
            Assert.AreEqual(new Card(Rank.Three, Suit.Diamonds), game.PlayerHand[2]);
            Assert.AreEqual(new Card(Rank.King, Suit.Hearts), game.HouseHand[0]);
        }

        // ================================================================== Belial

        [Test]
        public void FalseFace_ShowsAWeakerHouseCard_UntilTheShowdown()
        {
            var game = Game(Nothing, HouseFullHouse, new FalseFaceCheat(), rules: new GameRules(1000, 5000, luciferGateYears: 0));
            ToTheDraw(game);
            game.Draw(new int[0]);
            game.Bet(BetAction.Pass);   // the House shows two cards

            Assert.AreEqual(GamePhase.HouseReveal, game.Phase);
            Assert.IsTrue(game.IsHouseCardFalse(0));
            Card face = game.HouseCardFace(0);
            Assert.AreNotEqual(game.HouseHand[0], face);
            Assert.Less(face.Rank, game.HouseHand[0].Rank, "A weaker face, to lure a raise.");
            Assert.IsFalse(game.PlayerHand.Contains(face) || game.HouseHand.Contains(face), "Nobody's card.");

            game.Bet(BetAction.Pass);

            Assert.IsFalse(game.IsHouseCardFalse(0), "The showdown turns the truth.");
            Assert.AreEqual(game.HouseHand[0], game.HouseCardFace(0));
        }

        [Test]
        public void ForkedTongue_ChangesASuit_AndBreaksTheFlush()
        {
            var game = Game(Flush, HouseTwos, new ForkedTongueCheat());
            ToTheDraw(game);

            game.Draw(new int[0]);

            CheatResult result = Played(game);
            Assert.AreEqual(result.Lost.Value.Rank, result.Gained.Value.Rank);
            Assert.AreNotEqual(result.Lost.Value.Suit, result.Gained.Value.Suit);
            Assert.AreNotEqual(HandCategory.Flush, HandEvaluator.CreateDefault().Evaluate(game.PlayerHand).Category);
        }

        [Test]
        public void SerpentSwap_TakesAPairCard_AndWhatComesBackStaysDark()
        {
            var game = Game("QS QH 2C 5D 9C", "3D 4D 6H 7H 8D", new SerpentSwapCheat());
            ToTheDraw(game);

            game.Draw(new int[0]);

            CheatResult result = Played(game);
            Assert.AreEqual(Rank.Queen, result.Lost.Value.Rank, "A card of the pair.");
            Assert.IsTrue(game.HouseHand.Contains(result.Lost.Value));
            int slot = result.PlayerCards.Single();
            Assert.IsTrue(game.IsPlayerCardHidden(slot));
            Assert.IsNull(result.Gained, "The player is not told what came back.");
        }

        // ================================================================== Lilith

        [Test]
        public void NightVeil_HidesACard_TheGuideCannotSeeIt()
        {
            var game = Game("KS KH 2C 5D 9C", "3D 4D 6H 7H 8D", new NightVeilCheat());

            ToTheDraw(game);

            Assert.IsTrue(game.IsPlayerCardHidden(0));
            Assert.AreEqual(HandCategory.HighCard, game.PlayerHandNow, "The kings' pair is not visible any more.");
            Assert.IsEmpty(game.SuggestedDiscards(), "No hint that would give the hidden card away.");
            Assert.IsTrue(game.CanDraw(new[] { 0 }, out _), "It may be thrown back blind.");

            game.Draw(new[] { 0 });
            Assert.IsFalse(game.IsPlayerCardHidden(0), "The veil went with the card.");
        }

        [Test]
        public void Thorn_ThrowingTheCardBack_CostsAUnitAtOnce()
        {
            var game = Game(Nothing, HouseFullHouse, new ThornCheat(), rules: new GameRules(1000, 5000, luciferGateYears: 0));
            ToTheDraw(game);
            Assert.IsTrue(game.IsPlayerCardThorned(0));

            game.Draw(new[] { 0, 1 });

            Assert.AreEqual(1100, game.Years, "Added the moment the card was thrown back.");
            Assert.AreEqual(100, game.ThornYearsThisHand);
        }

        [Test]
        public void Thorn_KeepingTheCard_CostsNothing()
        {
            var game = Game(Nothing, HouseFullHouse, new ThornCheat());
            ToTheDraw(game);

            game.Draw(new[] { 1 });

            Assert.AreEqual(1000, game.Years);
        }

        [Test]
        public void Moonless_TheDrawnCardsStayDark()
        {
            var game = Game(Nothing, HouseFullHouse, new MoonlessCheat());
            ToTheDraw(game);

            game.Draw(new[] { 0, 2 });

            Assert.IsTrue(game.IsPlayerCardHidden(0));
            Assert.IsTrue(game.IsPlayerCardHidden(2));
            Assert.IsFalse(game.IsPlayerCardHidden(1));
        }

        // ================================================================== Lucifer

        [Test]
        public void Gaze_TheHouseReRaises_WhenThePlayerWouldLose_EvenIfItNeverWould()
        {
            var game = Game(Nothing, HouseFullHouse, new GazeCheat(), rules: Rules(50), betting: new HellPokerGameTests.FixedHouseBetting(false));
            ToTheDraw(game);
            game.Draw(new int[0]);

            game.Bet(BetAction.Raise);

            Assert.AreEqual(GamePhase.HouseReRaise, game.Phase);
        }

        [Test]
        public void Gaze_TheHouseNeverReRaises_WhenThePlayerWouldWin()
        {
            var game = Game(Flush, HouseTwos, new GazeCheat(), rules: Rules(50), betting: new HellPokerGameTests.FixedHouseBetting(true));
            ToTheDraw(game);
            game.Draw(new int[0]);

            game.Bet(BetAction.Raise);

            Assert.AreEqual(GamePhase.HouseReveal, game.Phase);
        }

        [Test]
        public void Rewrite_DropsTheHand_OneCategoryDown()
        {
            var game = Game("KS KH 4D 4C 9C", Nothing, new RewriteCheat());
            ToTheDraw(game);

            game.Draw(new int[0]);

            Assert.AreEqual(HandCategory.OnePair, HandEvaluator.CreateDefault().Evaluate(game.PlayerHand).Category, "Two pair became one pair.");
            CheatResult result = Played(game);
            Assert.IsFalse(game.PlayerHand.Contains(result.Lost.Value));
            Assert.IsTrue(game.PlayerHand.Contains(result.Gained.Value));
        }

        [Test]
        public void BurningCard_TheHighestCardBecomesAnother()
        {
            var game = Game("2C 5D KH 9S JC", HouseFullHouse, new BurningCardCheat());

            ToTheDraw(game);

            CheatResult result = Played(game);
            Assert.AreEqual(new Card(Rank.King, Suit.Hearts), result.Lost);
            Assert.AreEqual(result.Gained.Value, game.PlayerHand[2]);
            Assert.AreNotEqual(Rank.King, game.PlayerHand[2].Rank);
        }

        /// <summary>Lucifer's own table and policy at <paramref name="years"/>, with FirstChoice dice.</summary>
        private static HellPokerGame LuciferTable(int years, string player, string house, string rest)
        {
            Dealer lucifer = DealerRoster.Lucifer;
            GameRules rules = lucifer.ApplyTo(GameRules.Default);
            var random = new FirstChoice();
            var game = new HellPokerGame(rules, TestDecks.Stacked($"{player} {house} {rest}"), HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy(3)), new HouseDrawStrategy(3), lucifer.Payouts, null,
                new CheatSession(lucifer.Cheats, lucifer.MaliceMax, random), random);
            game.TakeOver(years, 5);
            return game;
        }

        [Test]
        public void TheFall_IsAnnounced_AndTurnsAWinIntoALoss()
        {
            // 150 years: The Fall may come. The player's flush wins — then the K♣ and the House's 10♠ are dealt again.
            HellPokerGame game = LuciferTable(150, Flush, HouseTwos, "3S 6C 10S 3H 4S 5H 6H");
            game.PlaceBet();
            Assert.AreEqual(CheatIds.TheFall, game.PendingCheat.Id, "THE FALL AWAITS — the player knows.");

            game.CheckToDraw();
            game.Draw(new int[0]);
            PassToTheEnd(game);

            CheatResult result = Played(game);
            Assert.AreEqual(CheatIds.TheFall, result.CheatId);
            Assert.AreEqual(new Card(Rank.King, Suit.Clubs), result.Lost);
            Assert.AreEqual(ShowdownOutcome.HouseWins, game.LastRound.Showdown.Outcome, "Judged anew: the twos win.");
            Assert.IsTrue(game.MajorCheatUsed);
        }

        [Test]
        public void TheFall_ComesOncePerAttempt()
        {
            HellPokerGame game = LuciferTable(150, Flush, HouseTwos, "3S 6C 10S 3H 4S 5H 6H");
            game.PlaceBet();
            game.CheckToDraw();
            game.Draw(new int[0]);
            PassToTheEnd(game);
            game.NextRound();
            game.TakeOver(120, 6);
            Assert.IsTrue(game.MajorCheatUsed, "Spent at this table (this attempt).");

            game.PlaceBet();

            Assert.AreEqual(CheatIds.Gaze, Played(game).CheatId, "A minor one (the Gaze strikes at the deal).");
        }

        [Test]
        public void TheFall_NeverAbove150()
        {
            HellPokerGame game = LuciferTable(151, Flush, HouseTwos, "3S 6C 10S 3H 4S 5H 6H");

            game.PlaceBet();

            Assert.AreEqual(CheatIds.Gaze, Played(game).CheatId, "Above 150 only the minor cheats.");
        }

        [Test]
        public void TheFall_OnALoss_IsNotSpent()
        {
            HellPokerGame game = LuciferTable(150, Nothing, HouseFullHouse, "3S 6C 10S 3H 4S 5H 6H");
            game.PlaceBet();
            game.CheckToDraw();
            game.Draw(new int[0]);
            PassToTheEnd(game);

            Assert.AreEqual(CheatOutcome.Fizzled, Played(game).Outcome);
            Assert.IsFalse(game.MajorCheatUsed);
        }

        [Test]
        public void Lucifer_CheatsEveryHand()
        {
            HellPokerGame game = HellPokerGameFactory.Create(GameRules.Default, DealerRoster.Lucifer, seed: 7);
            game.TakeOver(200, 3);
            for (int hand = 0; hand < 5; hand++)
            {
                game.PlaceBet();
                Assert.IsTrue(game.PendingCheat != null || game.CheatsThisHand.Count > 0, $"Hand {hand}: no cheat.");
                game.Bet(BetAction.Fold);
                if (game.IsGameOver) break;
                game.NextRound();
            }
        }

        // ================================================================== living with the other rules

        [Test]
        public void ASealedHand_StillTakesItsCheat()
        {
            var game = Game(Nothing, HouseFullHouse, new CollateralCheat());
            game.PlaceBet();
            game.Bet(BetAction.Raise);
            game.Bet(BetAction.Raise);   // the table is full: sealed, straight to the draw

            Assert.IsTrue(game.IsCommitted);
            Assert.AreEqual(GamePhase.Drawing, game.Phase);
            Assert.IsTrue(game.IsPlayerCardChained(4));
        }

        [Test]
        public void ThornOnTheSoul_BurnsYearsOfTheSoul()
        {
            var game = Game(Nothing, HouseFullHouse, new ThornCheat(), rules: new GameRules(1000, 2000, luciferGateYears: 0));
            game.TakeOver(2100, 3);
            ToTheDraw(game);
            Assert.IsTrue(game.IsSoulHand);

            game.Draw(new[] { 0 });

            Assert.AreEqual(2100 + game.Unit, game.Years);
        }

        [Test]
        public void Restore_BringsBackTheGauge()
        {
            var game = Game(Nothing, HouseFullHouse, new GazeCheat(), maliceMax: 4);

            game.RestoreMalice(3, true);

            Assert.AreEqual(3, game.Malice);
            Assert.IsTrue(game.MajorCheatUsed);
            game.PlaceBet();
            Assert.AreEqual(CheatIds.Gaze, Played(game).CheatId, "3 + 1: full.");
        }

        [Test]
        public void TheHandInProgress_KnowsItsCheat()
        {
            var game = Game(Nothing, HouseFullHouse, new CollateralCheat());
            game.PlaceBet();
            Assert.AreEqual(CheatIds.Collateral, game.CurrentHand.CheatId);
            Assert.IsFalse(game.CurrentHand.CheatResolved);

            game.CheckToDraw();

            Assert.IsTrue(game.CurrentHand.CheatResolved);
        }

        [Test]
        public void Snapshot_v3_KeepsTheGaugeAndTheHandsCheat()
        {
            var snapshot = new RunSnapshot("belial", 800, 9, new RunStats(1000, "belial"),
                new HandInProgress(200, 100, false, false, false, CheatIds.SerpentSwap, CheatIds.FalseFace, true), malice: 2, majorCheatUsed: true);

            string text = snapshot.Encode();
            StringAssert.StartsWith("v=3", text);
            Assert.IsTrue(RunSnapshot.TryDecode(text, out RunSnapshot back));

            Assert.AreEqual(2, back.Malice);
            Assert.IsTrue(back.MajorCheatUsed);
            Assert.AreEqual(CheatIds.SerpentSwap, back.Hand.CheatId);
            Assert.AreEqual(CheatIds.FalseFace, back.Hand.ShownCheatId);
            Assert.IsTrue(back.Hand.CheatResolved);
        }

        [Test]
        public void Snapshot_v2_StillReads_WithAnEmptyGauge()
        {
            string text = "v=2\ndealer=mammon\nyears=900\nrounds=3\nhands=3\nlowest=900\nhighest=1000\nbest=\ndealers=mammon\nsoul=0\n" +
                          "lucifer=0\norigin=\nattempts=0";

            Assert.IsTrue(RunSnapshot.TryDecode(text, out RunSnapshot snapshot));
            Assert.AreEqual(0, snapshot.Malice);
            Assert.IsFalse(snapshot.MajorCheatUsed);
        }

        [Test]
        public void EveryDemon_HasTheirCheats()
        {
            CollectionAssert.AreEquivalent(new[] { CheatIds.Collateral, CheatIds.Tithe, CheatIds.Buyout },
                DealerRoster.Mammon.Cheats.Cheats.Select(c => c.Id));
            CollectionAssert.AreEquivalent(new[] { CheatIds.FalseFace, CheatIds.ForkedTongue, CheatIds.SerpentSwap },
                DealerRoster.Belial.Cheats.Cheats.Select(c => c.Id));
            CollectionAssert.AreEquivalent(new[] { CheatIds.NightVeil, CheatIds.Thorn, CheatIds.Moonless },
                DealerRoster.Lilith.Cheats.Cheats.Select(c => c.Id));
            CollectionAssert.AreEquivalent(new[] { CheatIds.Gaze, CheatIds.Rewrite, CheatIds.BurningCard, CheatIds.TheFall },
                DealerRoster.Lucifer.Cheats.Cheats.Select(c => c.Id));
            Assert.AreEqual(4, DealerRoster.Mammon.MaliceMax);
            Assert.AreEqual(2, DealerRoster.Belial.MaliceMax);
            Assert.AreEqual(4, DealerRoster.Lilith.MaliceMax);
            Assert.AreEqual(1, DealerRoster.Lucifer.MaliceMax);
        }
    }
}
