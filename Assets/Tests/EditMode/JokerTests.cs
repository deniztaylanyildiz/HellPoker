using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HellPoker.Core.Cards;
using HellPoker.Core.Cheats;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Events;
using HellPoker.Core.Game;
using HellPoker.Core.Relics;
using HellPoker.Core.Randomness;
using HellPoker.Core.Sinners;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// The Jester's deck: jokers in it (2 at the start, +1 a won hand, -1 a lost hand above 10, never below 2; folds and ties change
    /// nothing), for him only. A single joker at the showdown becomes any card its owner does not hold (the best by default, the
    /// player's own pick otherwise); two or more lose the hand (both sides: a push). No cheat touches a joker. The other classes
    /// play the plain 52, exactly as before.
    /// </summary>
    public class JokerTests
    {
        private const string Nothing = "2C 5D 7H 9S JC";
        private const string Blanks = "3S 6D 10S 8H 3H QC 8C 7C 6H 4S 10D 3D";

        private static readonly IHandEvaluator Plain = HandEvaluator.CreateDefault();
        private static readonly IHandEvaluator Wild = new WildJokerEvaluator(HandEvaluator.CreateDefault());

        private static Sinner Jester(int jokers = 2) => new Sinner(SinnerRoster.Jester, jokers: jokers);

        /// <summary>A Jester's game dealt from exactly these cards (the stack must hold as many jokers as the run has).</summary>
        private static HellPokerGame Game(string player, string house, Sinner sinner, string blanks = Blanks)
        {
            var rules = new GameRules(1000, 5000, luciferGateYears: 0);
            return new HellPokerGame(rules, TestDecks.Stacked($"{player} {house} {blanks}"), HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)), new HouseDrawStrategy(rules.MaxDiscards), PayoutTable.CreateDefault(),
                null, null, null, sinner);
        }

        private static void ToShowdown(HellPokerGame game, params int[] discards)
        {
            game.PlaceBet();
            if (game.Phase == GamePhase.PlayerReveal) game.CheckToDraw();
            game.Draw(discards);
            while (game.Phase == GamePhase.DrawReveal || game.Phase == GamePhase.HouseReveal)
                game.Bet(BetAction.Pass);
        }

        private static IDeck DeckOf(HellPokerGame game) =>
            (IDeck)typeof(HellPokerGame).GetField("_deck", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(game);

        private static List<Card> AllCards(HellPokerGame game) =>
            DeckOf(game).Remaining.Concat(game.PlayerHand).Concat(game.HouseHand).ToList();

        // ------------------------------------------------------------------ the deck

        [Test]
        public void TheJestersRun_Deals54Cards_TwoOfThemJokers_EveryOtherClassThePlain52()
        {
            HellPokerGame jester = HellPokerGameFactory.Create(GameRules.Default, DealerRoster.Mammon, 7, sinner: Jester());
            jester.PlaceBet();
            List<Card> cards = AllCards(jester);
            Assert.AreEqual(54, cards.Count);
            Assert.AreEqual(2, cards.Count(c => c.IsJoker));
            Assert.AreEqual(54, cards.Distinct().Count());

            foreach (SinnerClass other in new[] { SinnerRoster.Peasant, SinnerRoster.Warlock, SinnerRoster.King })
            {
                HellPokerGame game = HellPokerGameFactory.Create(GameRules.Default, DealerRoster.Mammon, 7, sinner: new Sinner(other));
                game.PlaceBet();
                Assert.AreEqual(52, AllCards(game).Count, other.Id);
                Assert.IsFalse(AllCards(game).Any(c => c.IsJoker), other.Id);
            }
        }

        [Test]
        public void ANewTable_DealsTheRunsJokers_52PlusTheCount()
        {
            Sinner sinner = Jester(7);
            foreach (Dealer dealer in DealerRoster.All.Append(DealerRoster.Lucifer))
            {
                HellPokerGame game = HellPokerGameFactory.Create(GameRules.Default, dealer, 3, sinner: sinner);
                game.TakeOver(200, 0);
                game.PlaceBet();
                Assert.AreEqual(59, AllCards(game).Count, dealer.Id);
                Assert.AreEqual(7, AllCards(game).Count(c => c.IsJoker), dealer.Id);
            }
        }

        [Test]
        public void TheOrdinaryCards_StayAsTheyWere_EqualityHashAndText()
        {
            var ace = new Card(Rank.Ace, Suit.Spades);
            Assert.AreEqual(((int)Rank.Ace << 2) | (int)Suit.Spades, ace.GetHashCode());
            Assert.AreEqual("A♠", ace.ToString());
            Assert.IsFalse(ace.IsJoker);
            Assert.AreNotEqual(Card.Joker(1), Card.Joker(2));
            Assert.AreEqual(Card.Joker(1), Card.Joker(1));
            Assert.AreNotEqual(Card.Joker(1), default(Card));
            Assert.AreEqual(52, Deck.CreateStandardCards().Count());
        }

        // ------------------------------------------------------------------ the count

        [Test]
        public void TheCount_AWinAddsOne_ALossAboveTenTakesOne_FoldsAndTiesNothing_NeverBelowTwo()
        {
            Sinner sinner = Jester();
            Assert.AreEqual(2, sinner.Jokers);
            sinner.HandSettled(false, ShowdownOutcome.PlayerWins);
            Assert.AreEqual(3, sinner.Jokers);
            sinner.HandSettled(false, ShowdownOutcome.HouseWins);
            Assert.AreEqual(3, sinner.Jokers, "At ten or below a loss changes nothing.");
            sinner.HandSettled(true, null);
            sinner.HandSettled(false, ShowdownOutcome.Push);
            Assert.AreEqual(3, sinner.Jokers, "A fold and a tie change nothing.");

            Sinner ten = Jester(10);
            ten.HandSettled(false, ShowdownOutcome.HouseWins);
            Assert.AreEqual(10, ten.Jokers);
            Sinner eleven = Jester(11);
            eleven.HandSettled(false, ShowdownOutcome.HouseWins);
            Assert.AreEqual(10, eleven.Jokers);

            Assert.AreEqual(2, Jester(0).Jokers, "Never below the start.");
            Assert.AreEqual(2, Jester(1).Jokers);
            Assert.AreEqual(0, new Sinner(SinnerRoster.Peasant, jokers: 5).Jokers, "Only the Jester has jokers.");
        }

        [Test]
        public void TheCount_AfterAPlayedHand_AWinShowsInTheNextDeck()
        {
            Sinner sinner = Jester(3);
            HellPokerGame game = Game("AS AH AD KC KD", "JK1 JK2 2C 4D 5H", sinner, "JK3 " + Blanks.Replace("3S ", ""));
            // (the House throws one joker back and draws the third: two jokers again — it loses whatever it holds)
            ToShowdown(game);
            Assert.AreEqual(ShowdownOutcome.PlayerWins, game.LastRound.Showdown.Outcome);
            Assert.AreEqual(4, sinner.Jokers);
            game.NextRound();
            game.PlaceBet();
            Assert.AreEqual(4, AllCards(game).Count(c => c.IsJoker), "The next hand is dealt with four.");
        }

        // ------------------------------------------------------------------ what a joker becomes

        private static HandEvaluation Judge(string hand) => Wild.Evaluate(TestCards.Hand(hand));

        [Test]
        public void OneJoker_MakesTheBestHandInEveryCategory()
        {
            Assert.AreEqual(HandCategory.ThreeOfAKind, Judge("AS AH 5C 9D JK1").Category, "A pair and a joker: three of a kind.");
            HandEvaluation flush = Judge("2H 5H 9H KH JK1");
            Assert.AreEqual(HandCategory.Flush, flush.Category);
            Assert.AreEqual(Rank.Ace, flush.TieBreakers[0], "The joker is the flush's ace.");
            HandEvaluation straight = Judge("5C 6D 7H 8S JK1");
            Assert.AreEqual(HandCategory.Straight, straight.Category);
            Assert.AreEqual(Rank.Nine, straight.TieBreakers[0], "The top of the straight.");
            HandEvaluation quads = Judge("9C 9D 9H 9S JK1");
            Assert.AreEqual(HandCategory.FourOfAKind, quads.Category, "Four of a kind and a joker: no five of a kind.");
            Assert.AreEqual(Rank.Ace, quads.TieBreakers[1], "The joker is the kicker.");
            Assert.AreEqual(HandCategory.FullHouse, Judge("KS KH 4C 4D JK1").Category, "Two pair and a joker: a full house.");
            Assert.AreEqual(HandCategory.StraightFlush, Judge("9H 10H JH QH JK1").Category);
            Assert.AreEqual(HandCategory.RoyalFlush, Judge("10S JS QS KS JK1").Category);
            Assert.AreEqual(HandCategory.OnePair, Judge("2C 5D 9H KS JK1").Category, "Nothing else: a pair with the highest card.");
            Assert.AreEqual(Rank.King, Judge("2C 5D 9H KS JK1").TieBreakers[0]);
            Assert.AreEqual(HandCategory.DeadMansHand, Judge("AS AC 8S 5D JK1").Category, "Any card it is not holding — Wild Bill's too.");
        }

        [Test]
        public void AJoker_NeverBecomesACardItsOwnerHolds_ButMayCopyTheOpponents()
        {
            Hand hand = TestCards.Hand("AS AH 5C 9D JK1");
            Assert.IsNull(JokerResolver.Name(hand, new Card(Rank.Ace, Suit.Spades)));
            Assert.IsNotNull(JokerResolver.Name(hand, new Card(Rank.Ace, Suit.Diamonds)));
            Assert.AreEqual(48, JokerResolver.Choices(hand).Count);
            Assert.IsFalse(JokerResolver.Choices(hand).Any(hand.Contains));
            Hand resolved = JokerResolver.Resolve(hand);
            Assert.AreEqual(5, resolved.Distinct().Count());
            Assert.IsFalse(resolved.Any(c => c.IsJoker));
        }

        [Test]
        public void TheResolver_MatchesTryingEveryCard_OneJokerAndTwo()
        {
            var random = new System.Random(4242);
            List<Card> all = Deck.CreateStandardCards().ToList();
            for (int n = 0; n < 300; n++)
            {
                List<Card> cards = all.OrderBy(_ => random.Next()).Take(4).ToList();
                cards.Insert(random.Next(5), Card.Joker(1));
                var hand = new Hand(cards);
                HandEvaluation best = JokerResolver.Choices(hand).Select(c => Plain.Evaluate(JokerResolver.Name(hand, c))).Aggregate((a, b) => b.CompareTo(a) > 0 ? b : a);
                Assert.AreEqual(0, Wild.Evaluate(hand).CompareTo(best), hand.ToString());
            }
            for (int n = 0; n < 40; n++)
            {
                List<Card> cards = all.OrderBy(_ => random.Next()).Take(3).ToList();
                cards.Insert(random.Next(4), Card.Joker(1));
                cards.Insert(random.Next(5), Card.Joker(2));
                var hand = new Hand(cards);
                int first = cards.IndexOf(Card.Joker(1)), second = cards.IndexOf(Card.Joker(2));
                HandEvaluation best = null;
                foreach (Card a in all.Where(c => !cards.Contains(c)))
                foreach (Card b in all.Where(c => !cards.Contains(c) && c != a))
                {
                    HandEvaluation e = Plain.Evaluate(hand.Replace(new[] { first, second }, new[] { a, b }));
                    if (best == null || e.CompareTo(best) > 0) best = e;
                }
                Assert.AreEqual(0, Wild.Evaluate(hand).CompareTo(best), hand.ToString());
            }
        }

        [Test]
        public void WhatTheCardsShow_CountsAVisibleJokerAsItsBestCard()
        {
            Assert.AreEqual(HandCategory.OnePair, VisibleHandReader.Read(TestCards.Cards("KS JK1").ToList(), Wild));
            Assert.AreEqual(HandCategory.ThreeOfAKind, VisibleHandReader.Read(TestCards.Cards("KS KH JK1").ToList(), Wild));
            Assert.AreEqual(HandCategory.FourOfAKind, VisibleHandReader.Read(TestCards.Cards("KS KH KD KC JK1").ToList(), Wild));
        }

        // ------------------------------------------------------------------ two jokers lose

        [Test]
        public void TwoJokersInThePlayersHand_TheHandIsLost_WhateverItMakes()
        {
            HellPokerGame game = Game("JK1 JK2 AS AH AD", Nothing, Jester());
            ToShowdown(game);
            Assert.AreEqual(GamePhase.RoundOver, game.Phase, "Two jokers: nothing to name.");
            Assert.IsTrue(game.LastRound.Showdown.PlayerBust);
            Assert.AreEqual(ShowdownOutcome.HouseWins, game.LastRound.Showdown.Outcome);
            Assert.Greater(game.LastRound.YearsChange, 0);
        }

        [Test]
        public void TwoJokersInTheDemonsHand_ThePlayerWins()
        {
            HellPokerGame game = Game(Nothing, "JK1 JK2 AS AH AD", Jester(3), "JK3 " + Blanks.Replace("3S ", ""));
            ToShowdown(game);
            Assert.IsTrue(game.LastRound.Showdown.HouseBust);
            Assert.IsFalse(game.LastRound.Showdown.PlayerBust);
            Assert.AreEqual(ShowdownOutcome.PlayerWins, game.LastRound.Showdown.Outcome);
            Assert.Less(game.LastRound.YearsChange, 0);
        }

        [Test]
        public void TwoJokersOnBothSides_APush()
        {
            ShowdownResult both = ShowdownResult.Judge(Plain.Evaluate(TestCards.Hand(Nothing)), Plain.Evaluate(TestCards.Hand("AS AH AD KC KD")), true, true);
            Assert.AreEqual(ShowdownOutcome.Push, both.Outcome);

            HellPokerGame game = Game("JK1 JK2 AS AH AD", "JK3 JK4 2C 4D 5H", Jester(5), "JK5 " + Blanks.Replace("3S ", ""));
            ToShowdown(game);
            Assert.IsTrue(game.LastRound.Showdown.PlayerBust && game.LastRound.Showdown.HouseBust);
            Assert.AreEqual(ShowdownOutcome.Push, game.LastRound.Showdown.Outcome);
            Assert.AreEqual(0, game.LastRound.YearsChange);
        }

        [Test]
        public void ThrowingTheExtraJokerBackAtTheDraw_TheRuleFalls()
        {
            HellPokerGame game = Game("JK1 JK2 AS AH 9D", Nothing, Jester());
            ToShowdown(game, 1);
            Assert.AreEqual(GamePhase.NamingJoker, game.Phase, "One joker left: it waits for its name.");
            game.NameJoker(game.BestJokerCard);
            Assert.IsFalse(game.LastRound.Showdown.PlayerBust);
            Assert.AreEqual(HandCategory.ThreeOfAKind, game.LastRound.Showdown.Player.Category);
        }

        [Test]
        public void AFoldedHandWithTwoJokers_IsJustAFold()
        {
            HellPokerGame game = Game("JK1 JK2 AS AH AD", Nothing, Jester());
            game.PlaceBet();
            game.Bet(BetAction.Fold);
            Assert.IsTrue(game.LastRound.Folded);
            Assert.IsNull(game.LastRound.Showdown);
            Assert.AreEqual(2, game.Sinner.Jokers, "A fold changes nothing.");
        }

        // ------------------------------------------------------------------ the player names the joker

        [Test]
        public void ThePicker_StartsOnTheBestCard_AWorseCardPickedCountsAsItIs()
        {
            HellPokerGame game = Game("AS AH 5C 9D JK1", Nothing, Jester(), "JK2 " + Blanks.Replace("3S ", ""));
            ToShowdown(game);
            Assert.AreEqual(GamePhase.NamingJoker, game.Phase);
            Assert.AreEqual(5, game.HouseCardsRevealed, "Every card turns before the joker is named.");
            Assert.AreEqual(Rank.Ace, game.BestJokerCard.Rank);
            Assert.AreEqual(HandCategory.ThreeOfAKind, game.EvaluateJokerAs(game.BestJokerCard).Category);
            Assert.IsFalse(game.CanNameJoker(new Card(Rank.Ace, Suit.Spades)), "Not a card it holds.");
            Assert.IsNotNull(game.CurrentHand, "The naming is still the hand: a closed game forfeits it.");

            var two = new Card(Rank.Two, Suit.Hearts);
            game.NameJoker(two);
            Assert.AreEqual(HandCategory.OnePair, game.LastRound.Showdown.Player.Category, "The player's pick, worse or not.");
            Assert.AreEqual(two, game.LastRound.Showdown.Player.Hand[4]);
        }

        // ------------------------------------------------------------------ the House with jokers

        [Test]
        public void TheHouse_KeepsOneJoker_ThrowsTheExtraBack_AndCountsItAsItsBestCard()
        {
            var strategy = new HouseDrawStrategy();
            CollectionAssert.DoesNotContain(strategy.ChooseDiscards(TestCards.Hand("JK1 2C 5D 9H KS")), 0);
            IReadOnlyList<int> two = strategy.ChooseDiscards(TestCards.Hand("JK1 JK2 AS AH 5D"));
            CollectionAssert.Contains(two, 1);
            CollectionAssert.DoesNotContain(two, 0);
            CollectionAssert.AreEqual(new[] { 1 }, strategy.ChooseDiscards(TestCards.Hand("JK1 JK2 AS AH AD")), "Trips and a joker: only the extra goes.");

            HellPokerGame game = Game(Nothing, "JK1 KS KH 5D 9C", Jester(), Blanks + " JK2");
            ToShowdown(game);
            Assert.AreEqual(GamePhase.RoundOver, game.Phase, "The House names its own joker.");
            Assert.GreaterOrEqual(game.LastRound.Showdown.House.Category, HandCategory.ThreeOfAKind);
            Assert.IsFalse(game.LastRound.Showdown.House.Hand.Any(c => c.IsJoker));
        }

        // ------------------------------------------------------------------ no cheat touches a joker

        private sealed class FirstChoice : IRandomSource
        {
            public int Next(int maxExclusive) => 0;
        }

        [Test]
        public void NoCheat_EverTargetsOrDealsAJoker_AtAnyDemonsTable()
        {
            var cheats = DealerRoster.All.Append(DealerRoster.Lucifer).SelectMany(d => d.Cheats.Cheats).GroupBy(c => c.Id).Select(g => g.First());
            foreach (ICheat cheat in cheats)
            {
                Hand player = TestCards.Hand("JK1 JK2 JK3 JK4 KD");
                Hand house = TestCards.Hand("JK5 2C 3D 4H 9S");
                // Jokers on top of the deck: a cheat dealing "the next card" must pass them over.
                Deck deck = TestDecks.Stacked("JK6 JK7 5S 6S 7D 8D QH QD 10C 10H JD JH");
                var marks = new CheatMarks();
                ShowdownResult showdown = ShowdownResult.Resolve(Wild.Evaluate(player), Wild.Evaluate(house));
                var table = new CheatTable(player, house, deck, Wild, new FirstChoice(), marks, 50, 2, new[] { 4 }, showdown, 2, 100,
                    new HouseDrawStrategy());
                if (cheat.CanApply(table)) cheat.Apply(table);

                for (int i = 0; i < 4; i++)
                    Assert.AreEqual(Card.Joker(i + 1), table.PlayerHand[i], cheat.Id);
                Assert.AreEqual(Card.Joker(5), table.HouseHand[0], cheat.Id);
                Assert.AreEqual(5, table.PlayerHand.Count(c => c.IsJoker) + table.HouseHand.Count(c => c.IsJoker), cheat.Id + ": no joker dealt");
                Assert.IsFalse(marks.Chained.Concat(marks.Thorned).Concat(marks.HiddenFromPlayer).Any(c => c.IsJoker), cheat.Id);
                Assert.IsFalse(marks.FakeHouseIndex >= 0 && (marks.FakeHouseFace.IsJoker || house[marks.FakeHouseIndex].IsJoker), cheat.Id);
            }
        }

        // ------------------------------------------------------------------ twenty jokers: the Jester's Rattle

        /// <summary>A won hand (a full house against nothing) for a Jester at <paramref name="jokers"/>, all of them at the deck's bottom.</summary>
        private static HellPokerGame WinningGame(Sinner sinner, RunEffects effects)
        {
            string jokers = string.Join(" ", Enumerable.Range(1, sinner.Jokers).Select(n => "JK" + n));
            HellPokerGame game = Game("AS AH AD KC KD", Nothing, sinner, Blanks + " " + jokers);
            game.UseEffects(effects);
            return game;
        }

        [Test]
        public void TwentyJokers_TheDeckIsCleared_BackToTwo_AndTheRattleJoinsTheRun_OnceARun()
        {
            Sinner sinner = Jester(19);
            sinner.HandSettled(false, ShowdownOutcome.PlayerWins);
            Assert.AreEqual(2, sinner.Jokers);
            Assert.IsTrue(sinner.HitJokerJackpot);
            Assert.AreEqual(1, sinner.JokerJackpots);
            sinner.HandSettled(false, ShowdownOutcome.PlayerWins);
            Assert.IsFalse(sinner.HitJokerJackpot);

            var effects = new RunEffects();
            effects.AddRelic(RelicIds.BoneDie);
            effects.AddRelic(RelicIds.RustyCrown);
            HellPokerGame game = WinningGame(Jester(19), effects);
            ToShowdown(game);
            Assert.AreEqual(ShowdownOutcome.PlayerWins, game.LastRound.Showdown.Outcome);
            Assert.IsTrue(game.LastRound.JokerJackpot);
            Assert.IsTrue(game.LastRound.RattleGiven, "Beyond the two relics already carried.");
            CollectionAssert.AreEqual(new[] { RelicIds.BoneDie, RelicIds.RustyCrown, RelicIds.JestersRattle }, effects.Relics);
            Assert.AreEqual(2, game.Sinner.Jokers);

            HellPokerGame again = WinningGame(Jester(19), effects);
            ToShowdown(again);
            Assert.IsTrue(again.LastRound.JokerJackpot, "The deck is cleared again...");
            Assert.IsFalse(again.LastRound.RattleGiven, "...but the Rattle comes once a run.");
            Assert.AreEqual(3, effects.Relics.Count);
        }

        [Test]
        public void TheRattle_AWinForgivesHalfAnAnteMore_ALossAddsHalfAnAnteMore()
        {
            int Change(string player, string house, bool rattle)
            {
                var effects = new RunEffects();
                if (rattle) effects.AddRelic(RelicIds.JestersRattle);
                HellPokerGame game = Game(player, house, null);
                game.UseEffects(effects);
                ToShowdown(game);
                return game.LastRound.YearsChange;
            }
            // 1000 years: the ante is 100, half of it 50.
            Assert.AreEqual(-50, Change("AS AH AD KC KD", Nothing, true) - Change("AS AH AD KC KD", Nothing, false));
            Assert.AreEqual(50, Change(Nothing, "AS AH AD KC KD", true) - Change(Nothing, "AS AH AD KC KD", false));
        }

        [Test]
        public void TheRattle_IsNeverOffered_AndDoesNotCountTowardsTheLimit()
        {
            var effects = new RunEffects();
            Assert.IsTrue(effects.AddRelic(RelicIds.JestersRattle));
            Assert.IsTrue(effects.AddRelic(RelicIds.BoneDie));
            Assert.IsTrue(effects.AddRelic(RelicIds.RustyCrown));
            Assert.IsFalse(effects.AddRelic(RelicIds.FerrymansCoin), "Two offered relics, as before.");
            Assert.AreEqual(2, effects.CarriedOffered);
            CollectionAssert.DoesNotContain(RelicRoster.Offered.Select(r => r.Id).ToList(), RelicIds.JestersRattle);

            var one = new RunEffects();
            one.AddRelic(RelicIds.JestersRattle);
            HellPokerGame game = Game(Nothing, "2D 2H 5S 7H 9D", null);
            game.UseEffects(one);
            var offer = new RelicEvent(EventIds.GraveRobber);
            Assert.IsTrue(offer.CanAppear(game, "mammon"), "The Rattle does not fill a relic slot.");
            var random = new SystemRandomSource(5);
            for (int i = 0; i < 3; i++) offer.Apply(EventOptions.Accept, game, "mammon", random);
            Assert.AreEqual(3, one.Relics.Count);
            Assert.IsFalse(offer.CanAppear(game, "mammon"));
        }

        [Test]
        public void TheSave_KeepsTheRattle_PastTheTwoRelicLimit()
        {
            var effects = new RunEffects();
            effects.Restore(null, 0, 0, 0, new[] { RelicIds.BoneDie, RelicIds.RustyCrown, RelicIds.JestersRattle });
            CollectionAssert.AreEqual(new[] { RelicIds.BoneDie, RelicIds.RustyCrown, RelicIds.JestersRattle }, effects.Relics);

            var state = new RunEventState(null, 0, null, 0, 0, 0, effects.Relics);
            var snapshot = new RunSnapshot("mammon", 900, 4, new RunStats(1000, "mammon"), classId: "jester", events: state);
            Assert.IsTrue(RunSnapshot.TryDecode(snapshot.Encode(), out RunSnapshot back));
            var restored = new RunEffects();
            restored.Restore(null, 0, 0, 0, back.Events.Relics);
            CollectionAssert.AreEqual(effects.Relics, restored.Relics);

            // Too many offered relics in a garbled save: the extra one goes, the Rattle stays.
            var garbled = new RunEffects();
            garbled.Restore(null, 0, 0, 0, new[] { RelicIds.BoneDie, RelicIds.RustyCrown, RelicIds.FerrymansCoin, RelicIds.JestersRattle, "nonsense" });
            CollectionAssert.AreEqual(new[] { RelicIds.BoneDie, RelicIds.RustyCrown, RelicIds.JestersRattle }, garbled.Relics);
        }

        // ------------------------------------------------------------------ the save

        [Test]
        public void TheSave_KeepsTheJokers_AnOldOrGarbledOneReadsAsTheStart()
        {
            var snapshot = new RunSnapshot("mammon", 900, 4, new RunStats(1000, "mammon"), classId: "jester", classJokers: 7);
            Assert.IsTrue(RunSnapshot.TryDecode(snapshot.Encode(), out RunSnapshot back));
            Assert.AreEqual("jester", back.ClassId);
            Assert.AreEqual(7, back.ClassJokers);
            StringAssert.Contains("class.jokers=7", snapshot.Encode());

            string old = new RunSnapshot("mammon", 900, 4, new RunStats(1000, "mammon"), classId: "jester").Encode();
            StringAssert.DoesNotContain("class.jokers", old);
            Assert.IsTrue(RunSnapshot.TryDecode(old, out RunSnapshot oldBack));
            Assert.AreEqual(2, new Sinner(SinnerRoster.Find(oldBack.ClassId), jokers: oldBack.ClassJokers).Jokers);

            Assert.IsTrue(RunSnapshot.TryDecode(old + "\nclass.jokers=lots", out RunSnapshot garbled), "A garbled count does not cost the run.");
            Assert.AreEqual(2, new Sinner(SinnerRoster.Jester, jokers: garbled.ClassJokers).Jokers);
            Assert.IsTrue(RunSnapshot.TryDecode(old + "\nclass.jokers=1", out RunSnapshot low));
            Assert.AreEqual(2, new Sinner(SinnerRoster.Jester, jokers: low.ClassJokers).Jokers);

            Assert.IsTrue(RunSnapshot.TryDecode(old.Replace("class=jester", "class=thief"), out RunSnapshot unknown));
            Assert.IsNull(SinnerRoster.Find(unknown.ClassId), "An unknown class falls back to the Peasant at the table.");
        }
    }
}
