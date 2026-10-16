using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Chapters;
using HellPoker.Core.Cheats;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using HellPoker.Core.Sinners;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Phase 2's class mechanics (the satchel, the prophecy, the treasury) and the demons' own rules, on games they hook into.</summary>
    public class ClassKitTests
    {
        private static readonly IHandEvaluator Evaluator = HandEvaluator.CreateDefault();

        private sealed class Always : IRandomSource
        {
            private readonly int _value;
            public Always(int value) { _value = value; }
            public int Next(int maxExclusive) => System.Math.Min(_value, maxExclusive - 1);
        }

        /// <summary>A game dealing exactly <paramref name="cards"/> (the player's five, the House's five, then the draws); no cheats,
        /// a House that never re-raises.</summary>
        private static HellPokerGame Stacked(string cards, Sinner sinner = null)
        {
            var rules = new GameRules(1000, 5000, luciferGateYears: 0, continuousDeck: false);
            return new HellPokerGame(rules, TestDecks.Stacked(cards), Evaluator, new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)),
                new HouseDrawStrategy(rules.MaxDiscards), PayoutTable.CreateDefault(), sinner: sinner);
        }

        private static HellPokerGame Shuffled(int seed, Sinner sinner = null)
        {
            var rules = new GameRules(1000, 5000, luciferGateYears: 0);
            return new HellPokerGame(rules, new Deck(new FisherYatesShuffler(new SystemRandomSource(seed))), Evaluator,
                new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)), new HouseDrawStrategy(rules.MaxDiscards), PayoutTable.CreateDefault(),
                sinner: sinner);
        }

        private static void PassToTheEnd(HellPokerGame game)
        {
            while (game.Phase == GamePhase.DrawReveal || game.Phase == GamePhase.HouseReveal)
                game.Bet(BetAction.Pass);
        }

        private static Card C(string token) => TestCards.Card(token);

        // ------------------------------------------------------------------ the Peasant's satchel

        [Test]
        public void TheSatchel_KeepsOneThrownCardAHand()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Peasant));
            HellPokerGame game = Stacked("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", kit.Sinner);
            ChapterHooks hooks = ChapterHooks.Attach(game, kit, null, 0, 1);
            game.PlaceBet();
            game.CheckToDraw();
            game.Draw(new[] { 3, 4 });
            CollectionAssert.AreEquivalent(new[] { C("4S"), C("2D") }, hooks.Satchel.Offered);
            Assert.IsTrue(hooks.Satchel.Keep(game, C("4S")));
            Assert.IsFalse(hooks.Satchel.Keep(game, C("2D")), "one card a hand");
            CollectionAssert.AreEqual(new[] { C("4S") }, kit.Satchel);
            Assert.IsEmpty(hooks.Satchel.Offered);
        }

        [Test]
        public void TheSatchel_FullPushesTheOldestOut()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Peasant));
            string[] kept = { "2C", "3C", "4C", "5C" };
            foreach (string c in kept) Assert.IsTrue(kit.Keep(C(c)));
            Assert.AreEqual(ClassKit.SatchelStart, kit.SatchelCapacity, "Demo 2.3: the satchel starts with one card");
            CollectionAssert.AreEqual(kept.Skip(kept.Length - kit.SatchelCapacity).Select(C), kit.Satchel, "the satchel's size at most, the oldest out");
            Assert.IsFalse(new ClassKit(new Sinner(SinnerRoster.King)).Keep(C("2C")), "only the Peasant has a satchel");
        }

        [Test]
        public void TheSatchel_GivesACardInTheDraw_TheSilentFieldKeepsItUnseen()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Peasant));
            kit.Restore(new[] { C("AD") }, 0, null, 0);
            HellPokerGame game = Stacked("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", kit.Sinner);
            ChapterHooks hooks = ChapterHooks.Attach(game, kit, null, 0, 1);
            game.PlaceBet();
            game.CheckToDraw();
            var from = new Dictionary<int, Card> { [4] = C("AD") };
            Assert.IsTrue(game.CanDraw(new[] { 3, 4 }, from, out _));
            game.Draw(new[] { 3, 4 }, from);
            Assert.AreEqual(C("AD"), game.PlayerHand[4], "the satchel's card in the thrown place");
            Assert.AreEqual(C("6H"), game.PlayerHand[3], "the other from the deck");
            Assert.IsEmpty(kit.Satchel);
            Assert.IsFalse(hooks.Satchel.SeenThisHand, "the passive SILENT FIELD: the hand's first satchel card goes unseen");
            Assert.AreEqual(C("AD"), hooks.Satchel.SilentCard);
            Assert.IsTrue(hooks.Satchel.PassiveThisHand);
            Assert.AreEqual(1, kit.Passives);
            Assert.AreEqual(1, kit.TakenFromSatchel);
        }

        [Test]
        public void TheSilentField_ASecondSatchelCardTheDemonSees_AndHeReRaisesMore()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Peasant));
            kit.Relics = () => HellPoker.Core.Relics.RelicRoster.Combined(new[] { HellPoker.Core.Relics.ChapterRelicIds.RopeBelt });   // a step: two cards
            kit.Restore(new[] { C("AD"), C("AC") }, 0, null, 0);
            Assert.AreEqual(2, kit.SatchelCapacity);
            HellPokerGame game = Stacked("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", kit.Sinner);
            ChapterHooks hooks = ChapterHooks.Attach(game, kit, null, 0, 1);
            game.PlaceBet();
            game.CheckToDraw();
            game.Draw(new[] { 3, 4 }, new Dictionary<int, Card> { [3] = C("AD"), [4] = C("AC") });
            Assert.IsTrue(hooks.Satchel.SeenThisHand, "the second card the demon sees");
            Assert.AreEqual(HandCategory.ThreeOfAKind, Evaluator.Evaluate(game.PlayerHand).Category);
        }

        [Test]
        public void TheSatchel_RefusesACardItDoesNotHold()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Peasant));
            HellPokerGame game = Stacked("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", kit.Sinner);
            ChapterHooks.Attach(game, kit, null, 0, 1);
            game.PlaceBet();
            game.CheckToDraw();
            Assert.IsFalse(game.CanDraw(new[] { 4 }, new Dictionary<int, Card> { [4] = C("AD") }, out _));
            Assert.IsFalse(game.CanDraw(new[] { 4 }, new Dictionary<int, Card> { [3] = C("AD") }, out _), "only into a thrown place");
        }

        [Test]
        public void TheSatchelsCards_AreNeverDealt_EvenAfterAShuffle()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Peasant));
            Card[] kept = { C("AS"), C("KS"), C("QS") };
            // Three cards: a class relic and the Wide Satchel lift the satchel two steps.
            kit.Relics = () => HellPoker.Core.Relics.RelicRoster.Combined(new[] { HellPoker.Core.Relics.ChapterRelicIds.RopeBelt });
            kit.Restore(kept, 0, null, 0, new[] { UpgradeIds.WideSatchel });
            Assert.AreEqual(3, kit.Satchel.Count);
            HellPokerGame game = Shuffled(3, kit.Sinner);
            ChapterHooks.Attach(game, kit, null, 0, 1);
            Assert.IsFalse(game.Deck.Remaining.Intersect(kept).Any(), "out of the deck as the hooks come");
            for (int hand = 0; hand < 40; hand++)
            {
                game.PlaceBet();
                Assert.IsFalse(game.PlayerHand.Concat(game.HouseHand).Intersect(kept).Any());
                game.Bet(BetAction.Fold);
                game.NextRound();
            }
            Assert.Greater(game.AutoShuffles, 0, "the deck was shuffled on the way");
        }

        [Test]
        public void ThePeasantsDemoPower_IsGone_AtAChaptersTable()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Peasant, charge: 5));
            HellPokerGame game = Stacked("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", kit.Sinner);
            ChapterHooks.Attach(game, kit, null, 0, 1);
            game.PlaceBet();
            Assert.AreEqual(PowerRefusal.NoPower, game.WhyNoPower(), "the demo's free fold is gone");
            Assert.AreEqual(5, kit.Sinner.Charge, "the gauge is untouched");
        }

        // ------------------------------------------------------------------ the Oracle's foresight

        [Test]
        public void TheOraclesForesight_StartsWithOneCard_InEveryChapter_AndGrowsWithItsLevel()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Warlock));
            Assert.AreEqual(1, kit.ProphecyCards);
            kit.EnterChapter(3);
            kit.EnterLucifer();
            Assert.AreEqual(1, kit.ProphecyCards, "the chapter does not change it (Demo 2.3: its level does)");
            kit.AddUpgrade(UpgradeIds.DeepProphecy);
            Assert.AreEqual(2, kit.ProphecyCards, "the Third Eye: a level");
            kit.Relics = () => HellPoker.Core.Relics.RelicRoster.Combined(new[] { HellPoker.Core.Relics.ChapterRelicIds.CrackedOrb,
                HellPoker.Core.Relics.ChapterRelicIds.SeersSpectacles, HellPoker.Core.Relics.ChapterRelicIds.StarChart });
            Assert.AreEqual(4, kit.ProphecyCards, "at most four: the highest level");
            Assert.AreEqual(0, new ClassKit(new Sinner(SinnerRoster.Peasant)).ProphecyCards);
        }

        [Test]
        public void TheProphecy_ShowsTheNextCards_TheDrawTakesThem_AndTheDemonKnowsACard()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Warlock, charge: 5));
            kit.AddUpgrade(UpgradeIds.DeepProphecy);   // two cards
            HellPokerGame game = Stacked("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", kit.Sinner);
            ChapterHooks hooks = ChapterHooks.Attach(game, kit, null, 0, 1);
            CollectionAssert.IsEmpty(hooks.Prophecy.TopCards(game), "shut between hands");
            game.PlaceBet();
            CollectionAssert.AreEqual(new[] { C("6H"), C("10C") }, hooks.Prophecy.TopCards(game));
            Assert.IsNotNull(hooks.Prophecy.KnownCard, "the demon knows one of the Oracle's cards");
            CollectionAssert.Contains(game.PlayerHand, hooks.Prophecy.KnownCard.Value);
            game.CheckToDraw();
            game.Draw(new[] { 3, 4 });
            Assert.AreEqual(C("6H"), game.PlayerHand[3], "the draw takes the cards seen, in order");
            Assert.AreEqual(C("10C"), game.PlayerHand[4]);
            CollectionAssert.IsEmpty(hooks.Prophecy.TopCards(game), "shut after the draw");
            Assert.AreEqual(5, kit.Sinner.Charge, "the demo's gauge is untouched");
        }

        [Test]
        public void TheSeersSpectacles_AStepOfForesight_AndTheirCurseMakesTheDemonBullyMore()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Warlock));
            kit.Relics = () => HellPoker.Core.Relics.RelicRoster.Combined(new[] { HellPoker.Core.Relics.ChapterRelicIds.SeersSpectacles });
            Assert.AreEqual(2, kit.ProphecyCards, "a step: one card more");
            Assert.AreEqual(10, kit.Mark(HellPoker.Core.Relics.RelicExtras.ProphecyBully), "the curse: the demon bullies a low card more");
        }

        // ------------------------------------------------------------------ the King's treasury

        [Test]
        public void TheTreasury_ATaxForAWin_HalfGoneWithADemonsHandLost()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.King));
            // The King's aces beat the House's queen high.
            HellPokerGame win = Stacked("AH AD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", kit.Sinner);
            ChapterHooks.Attach(win, kit, null, 0, 1);
            win.PlaceBet();
            win.CheckToDraw();
            win.Draw(new int[0]);
            PassToTheEnd(win);
            Assert.AreEqual(1, kit.Taxes);

            kit.Restore(null, 5, null, 0);
            HellPokerGame loss = Stacked("QS 9H 8D 5C 3H  AH AD 7C 4S 2D  6H 10C JH 5D 2C 3S 4H", kit.Sinner);
            ChapterHooks.Attach(loss, kit, DealerRoster.MammonId, 1000, 1);
            loss.PlaceBet();
            loss.CheckToDraw();
            loss.Draw(new int[0]);
            PassToTheEnd(loss);
            Assert.AreEqual(2, kit.Taxes, "a demon's hand lost: 5 → 2");
        }

        [Test]
        public void TheTreasury_RevealsAFaceDownCard_ForOneTax_OnceAHand()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.King));
            kit.Restore(null, 3, null, 0);
            HellPokerGame game = Stacked("AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", kit.Sinner);
            ChapterHooks hooks = ChapterHooks.Attach(game, kit, null, 0, 1);
            Assert.AreEqual(KitRefusal.NotNow, hooks.Treasury.WhyNot(game, TaxAct.Reveal), "not between hands");
            game.PlaceBet();
            Assert.IsTrue(hooks.Treasury.Reveal(game));
            Assert.AreEqual(1, game.HouseCardsRevealed);
            Assert.AreEqual(2, kit.Taxes);
            Assert.AreEqual(KitRefusal.AlreadyUsed, hooks.Treasury.WhyNot(game, TaxAct.Reveal));
        }

        [Test]
        public void TheTreasury_AFreeRaise_CostsNothingWhenTheHandIsLost()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.King));
            kit.Restore(null, 2, null, 0);
            HellPokerGame game = Stacked("QS 9H 8D 5C 3H  AH AD 7C 4S 2D  6H 10C JH 5D 2C 3S 4H", kit.Sinner);
            ChapterHooks hooks = ChapterHooks.Attach(game, kit, null, 0, 1);
            game.PlaceBet();
            int ante = game.Ante;
            Assert.IsTrue(hooks.Treasury.FreeRaise(game));
            game.Bet(BetAction.Raise);
            Assert.Greater(game.FreeStake, 0);
            while (game.Phase == GamePhase.PlayerReveal) game.Bet(BetAction.Pass);
            game.Draw(new int[0]);
            PassToTheEnd(game);
            Assert.AreEqual(ShowdownOutcome.HouseWins, game.LastRound.Showdown.Outcome);
            Assert.AreEqual(ante, game.LastRound.YearsChange, "only the ante is lost (aces pay ×1): the raise was the demon's");
        }

        [Test]
        public void TheSeizure_TakesTheDemonsBestOpenCard_ForOneOfTheKings()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.King));
            kit.Restore(null, 5, null, 0);
            HellPokerGame game = Stacked("AD KD 7C 4S 2D  AH 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", kit.Sinner);
            ChapterHooks hooks = ChapterHooks.Attach(game, kit, null, 0, 1);
            game.PlaceBet();
            Assert.IsTrue(hooks.Treasury.Reveal(game), "the A♥ turns");
            Assert.IsNull(hooks.Treasury.Seize(game, 3), "a card the King has not seen yet cannot be given");
            Card? taken = hooks.Treasury.Seize(game, 2);
            Assert.AreEqual(C("AH"), taken);
            Assert.AreEqual(C("AH"), game.PlayerHand[2]);
            Assert.AreEqual(C("7C"), game.HouseHand[0], "the King's card goes to the demon, face up");
            Assert.AreEqual(2, kit.Taxes, "a tax to open, two to seize (the royal discount: half of four)");
            Assert.IsNull(hooks.Treasury.Seize(game, 2), "once a hand");
        }

        [Test]
        public void TheRoyalDiscount_HalvesTheSeizure_OnItsOwn_AndOpensACardWhenNoneIsOpen()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.King, charge: 5));
            kit.Restore(null, 2, null, 0);
            Assert.AreEqual(2, kit.PriceOf(TaxAct.Seize), "the passive ROYAL DISCOUNT: half of four");
            Assert.AreEqual(ClassKit.SeizePrice, kit.SeizeFullPrice);
            HellPokerGame game = Stacked("AD KD 7C 4S 2D  AH 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", kit.Sinner);
            ChapterHooks hooks = ChapterHooks.Attach(game, kit, null, 0, 1);
            game.PlaceBet();
            Assert.AreEqual(C("AH"), hooks.Treasury.Seize(game, 2));
            Assert.AreEqual(1, game.HouseCardsRevealed, "the face-down card turned to be taken");
            Assert.AreEqual(0, kit.Taxes);
            Assert.IsTrue(hooks.Treasury.PassiveThisHand, "the passive worked");
            Assert.AreEqual(5, kit.Sinner.Charge, "nothing of the demo's gauge");

            kit.AddUpgrade(UpgradeIds.CheapSeizure);
            Assert.AreEqual(1, kit.PriceOf(TaxAct.Seize), "the Cheap Seizure: a quarter");
            kit.Relics = () => HellPoker.Core.Relics.RelicRoster.Combined(new[] { HellPoker.Core.Relics.ChapterRelicIds.KingsSignet });
            Assert.AreEqual(5, kit.SeizeFullPrice, "the signet's curse: a tax more");
            Assert.AreEqual(2, kit.PriceOf(TaxAct.Seize), "a quarter of five, rounded up");
        }

        [Test]
        public void ThePalace_NoCheatTouchesAJackQueenOrKing()
        {
            var hooks = ChapterHooks.For(new ClassKit(new Sinner(SinnerRoster.King)), null, 0, 1);
            Assert.IsTrue(hooks.IsPlayerCardGuarded(C("JD")));
            Assert.IsTrue(hooks.IsPlayerCardGuarded(C("QS")));
            Assert.IsTrue(hooks.IsPlayerCardGuarded(C("KH")));
            Assert.IsFalse(hooks.IsPlayerCardGuarded(C("AH")));
            var table = new CheatTable(TestCards.Hand("KD JH 7C 4S QD"), TestCards.Hand("AS 9H 8D 5C 3H"), TestDecks.Stacked("2C 3C"), Evaluator,
                new Always(0), new CheatMarks(), 10) { Guarded = hooks.IsPlayerCardGuarded };
            CollectionAssert.AreEqual(new[] { 2, 3 }, table.PlayerTargets().ToArray());
        }

        // ------------------------------------------------------------------ the demons' rules

        [Test]
        public void MammonsInterest_GrowsTheBar_ForEveryHandNotWon()
        {
            var sinner = new Sinner(SinnerRoster.Peasant);
            HellPokerGame game = BossTable.Create(GameRules.Default, DealerRoster.Mammon, 1300, 200, 175, 125, 7, sinner, sinner);
            ChapterHooks hooks = ChapterHooks.Attach(game, new ClassKit(sinner), DealerRoster.MammonId, 1300, 7);
            game.PlaceBet();
            game.Bet(BetAction.Fold);
            int fold = new ScaledPayoutTable(DealerRoster.Mammon.Payouts, 175, 125).GetFoldPenalty(game.LastRound.Stake, false);
            Assert.AreEqual(26, game.HookYearsThisHand, "2% of 1300");
            Assert.AreEqual((fold + 1) / 2 + 26, game.LastRound.YearsChange, "on top of the fold (half of it heals the bar)");
            Assert.IsTrue(hooks.Rule.StruckThisHand);
            Assert.AreEqual(1, hooks.Rule.Triggers);
        }

        [Test]
        public void BelialsRoleSwap_EveryFourthHand_AndTheWarlockSeesItComing()
        {
            var sinner = new Sinner(SinnerRoster.Warlock);
            HellPokerGame game = Shuffled(11, sinner);
            var rule = new BelialRoleSwap(1000, new SystemRandomSource(2));
            game.UseHooks(new HookChain(new TableHooks[] { rule }));
            for (int hand = 1; hand <= 4; hand++)
            {
                Assert.AreEqual(hand == 4, BelialRoleSwap.ComingIn(game), $"hand {hand}");
                game.PlaceBet();
                Hand player = game.PlayerHand, house = game.HouseHand;
                game.CheckToDraw();
                if (hand < 4)
                {
                    Assert.IsNull(rule.Given);
                    Assert.AreEqual(player, game.PlayerHand);
                }
                else
                {
                    Assert.IsNotNull(rule.Given);
                    CollectionAssert.Contains(game.PlayerHand, rule.Given.Value);
                    CollectionAssert.Contains(game.HouseHand, rule.Taken.Value);
                    CollectionAssert.Contains(house, rule.Given.Value);
                    CollectionAssert.Contains(player, rule.Taken.Value);
                }
                game.Draw(new int[0]);
                PassToTheEnd(game);
                game.NextRound();
            }
            Assert.AreEqual(1, rule.Triggers);
        }

        [Test]
        public void LilithsTemptation_TakesAThrownCardThatBettersHerHand()
        {
            int was = LilithTemptation.TakePercent;
            LilithTemptation.TakePercent = 100;
            try
            {
                HellPokerGame game = Stacked("AC KD 7C 4S 2D  AS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H");
                var rule = new LilithTemptation(1000, new SystemRandomSource(1));
                game.UseHooks(new HookChain(new TableHooks[] { rule }));
                game.PlaceBet();
                game.CheckToDraw();
                game.Draw(new[] { 0 });
                Assert.AreEqual(C("AC"), rule.Tempted);
                CollectionAssert.Contains(game.HouseHand, C("AC"));
                Assert.GreaterOrEqual(Evaluator.Evaluate(game.HouseHand).Category, HandCategory.OnePair);
            }
            finally
            {
                LilithTemptation.TakePercent = was;
            }
        }

        [Test]
        public void LucifersMirror_AgainstTheKing_SeizesWithFourTaxes_ButNotFromThePalace()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.King));
            kit.Restore(null, 0, null, ClassKit.SeizePrice);
            HellPokerGame game = Stacked("AH KD 7C 4S 2D  AS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", kit.Sinner);
            ChapterHooks hooks = ChapterHooks.Attach(game, kit, DealerRoster.LuciferId, 1000, 1);
            game.PlaceBet();
            game.CheckToDraw();
            var mirror = (LuciferMirror)hooks.Rule;
            Assert.AreEqual(C("AH"), mirror.Seized, "the ace that pairs his");
            CollectionAssert.Contains(game.HouseHand, C("AH"));
            CollectionAssert.Contains(game.PlayerHand, C("KD"), "the palace holds the king");
            Assert.AreEqual(0, kit.LuciferTaxes);
        }

        [Test]
        public void LucifersMirror_AgainstTheWarlock_DrawsTheTopCardWhenItServesHim()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Warlock));
            HellPokerGame game = Stacked("AH KD 7C 4S 2D  9S 9H 9D 5C 5H  6H 9C JH 5D 2C 3S 4H", kit.Sinner);
            ChapterHooks.Attach(game, kit, DealerRoster.LuciferId, 1000, 1);
            game.PlaceBet();
            game.CheckToDraw();
            game.Draw(new[] { 4 });
            Assert.AreEqual(HandCategory.FourOfAKind, Evaluator.Evaluate(game.HouseHand).Category, "a full house breaks for the fourth nine on top");
        }

        [Test]
        public void LucifersMirror_AgainstThePeasant_KeepsACard_AndPlaysIt()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Peasant));
            kit.Restore(null, 0, C("AD"), 0);
            HellPokerGame game = Stacked("KD QC 7C 4S 2D  AS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H", kit.Sinner);
            ChapterHooks hooks = ChapterHooks.Attach(game, kit, DealerRoster.LuciferId, 1000, 1);
            Assert.IsFalse(game.Deck.Remaining.Contains(C("AD")));
            game.PlaceBet();
            game.CheckToDraw();
            game.Draw(new int[0]);
            CollectionAssert.Contains(game.HouseHand, C("AD"), "his satchel's ace pairs his");
            Assert.IsNotNull(kit.LuciferBag, "and he keeps one of his thrown cards");
            Assert.AreEqual(1, hooks.Rule.Triggers);
        }

        [Test]
        public void ACorneredDemon_FillsHisGaugeFaster()
        {
            var rule = new MammonInterest(1000, new SystemRandomSource(1));
            HellPokerGame game = Shuffled(5);
            game.UseHooks(new HookChain(new TableHooks[] { rule }));
            Assert.AreEqual(0, rule.ExtraMalicePerHand(game));
            game.TakeOver(249, 0);
            Assert.AreEqual(1, rule.ExtraMalicePerHand(game), "under a quarter of the bar's start");
        }

        // ------------------------------------------------------------------ the run

        [Test]
        public void ANewChapter_TopsThePurseUpToTheClasssStart()
        {
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.King, 3);
            Assert.AreEqual(200, journey.Run.Purse.Coins);
            journey.Run.Purse.Add(-190);
            journey.Run.PayTribute();
            journey.Run.LeaveBossTable(0);
            journey.Run.TakeLootCoins();
            int left = journey.Run.Purse.Coins;
            journey.NextChapter();
            Assert.AreEqual(200, journey.Run.Purse.Coins);
            Assert.AreEqual(200 - left, journey.PurseToppedUp);
        }

        [Test]
        public void TheKit_GoesFromChapterToChapter_AndIntoTheSave()
        {
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Peasant, 3);
            journey.Kit.Keep(C("AS"));
            journey.Kit.Keep(C("7D"));
            journey.Kit.LuciferBag = C("2H");
            journey.Run.PayTribute();
            journey.Run.LeaveBossTable(0);
            journey.Run.TakeLootCoins();
            ClassKit kit = journey.Kit;
            journey.NextChapter();
            Assert.AreSame(kit, journey.Kit);

            Assert.IsTrue(ChapterSave.TryDecode(journey.Capture().Encode(), out ChapterSave save));
            ChapterJourney back = ChapterJourney.Restore(save);
            CollectionAssert.AreEqual(new[] { C("7D") }, back.Kit.Satchel, "a satchel of one at the starting level: the newest stays");
            Assert.AreEqual(C("2H"), back.Kit.LuciferBag);
            Assert.AreEqual(2, back.Kit.Chapter);

            ChapterJourney king = ChapterJourney.Begin(SinnerRoster.King, 4);
            king.Kit.AddTax(6);
            Assert.AreEqual(ClassKit.TaxStart, king.Kit.Taxes, "the treasury's starting room");
            Assert.IsTrue(ChapterSave.TryDecode(king.Capture().Encode(), out ChapterSave kingSave));
            Assert.AreEqual(ClassKit.TaxStart, ChapterJourney.Restore(kingSave).Kit.Taxes);
        }

        [Test]
        public void EveryTableOfAChapter_PlaysWithTheKit_AndOnlyTheDemonsWithARule()
        {
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Warlock, 8);
            journey.Run.MoveTo(journey.Run.Choices.First());
            FloorTable table = journey.Run.OpenTable();
            Assert.AreSame(journey.Run.Hooks, table.Game.Hooks);
            Assert.IsNull(journey.Run.Hooks.Rule, "the floors' imps keep no rule");
            Assert.IsNotNull(journey.Run.Hooks.Prophecy);

            journey.Run.PayTribute();
            HellPokerGame boss = journey.Run.OpenBossTable(GameRules.Default);
            Assert.IsInstanceOf<MammonInterest>(journey.Run.Hooks.Rule);
            Assert.AreSame(journey.Run.Hooks, boss.Hooks);
        }
    }
}
