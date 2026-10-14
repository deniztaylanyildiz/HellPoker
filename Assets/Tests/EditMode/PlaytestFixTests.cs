using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Chapters;
using HellPoker.Core.Cheats;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Events;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Settings;
using HellPoker.Presentation.Ui;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// 2.1.1's playtest fixes: the prophecy only after the deal (A), the four-colour deck (B), the result band (C), the Devil's Compass
    /// (D), the cheats' cards found by themselves (F), the demons' fixed stakes and the last blow (G), the class powers once a hand (H),
    /// relics that never leave unasked (I).
    /// </summary>
    public class PlaytestFixTests
    {
        private static readonly IHandEvaluator Evaluator = HandEvaluator.CreateDefault();
        private static Card C(string token) => TestCards.Card(token);

        private sealed class OnlyCheat : ICheatPolicy
        {
            private readonly ICheat _cheat;
            public OnlyCheat(ICheat cheat) { _cheat = cheat; }
            public IReadOnlyList<ICheat> Cheats => new[] { _cheat };
            public CheatPick Choose(CheatContext context, IRandomSource random) => new CheatPick(_cheat);
            public ICheat Find(string id) => id == _cheat.Id ? _cheat : null;
        }

        /// <summary>A plain game dealing exactly <paramref name="cards"/> again every hand (no cheats, a House that never re-raises).</summary>
        private static HellPokerGame Stacked(string cards, Sinner sinner = null)
        {
            var rules = new GameRules(1000, 5000, luciferGateYears: 0, continuousDeck: false);
            return new HellPokerGame(rules, TestDecks.Stacked(cards), Evaluator, new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)),
                new HouseDrawStrategy(rules.MaxDiscards), PayoutTable.CreateDefault(), sinner: sinner);
        }

        private static void PassToTheEnd(HellPokerGame game)
        {
            while (game.Phase == GamePhase.DrawReveal || game.Phase == GamePhase.HouseReveal)
                game.Bet(BetAction.Pass);
        }

        private const string Deal = "AH KD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H AS 8S";

        // ------------------------------------------------------------------ A: the prophecy, after the deal and before the draw

        [Test]
        public void TheProphecy_IsShutBetweenHands_OpensAfterTheDeal_AndShutsAfterTheDraw()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Warlock));
            kit.EnterChapter(2);
            HellPokerGame game = Stacked(Deal, kit.Sinner);
            ChapterHooks hooks = ChapterHooks.Attach(game, kit, null, 0, 1);

            Assert.IsEmpty(hooks.Prophecy.TopCards(game), "between hands: the deal would take the very cards shown");
            Assert.AreEqual(KitRefusal.NotNow, hooks.Prophecy.WhyNoRearrange(game), "no sending down before the deal");
            Assert.IsFalse(ProphecyPowers.IsOpen(game));

            game.PlaceBet();
            Assert.IsTrue(ProphecyPowers.IsOpen(game));
            CollectionAssert.AreEqual(new[] { C("6H"), C("10C") }, hooks.Prophecy.TopCards(game), "the cards the draw will take");
            Assert.AreEqual(KitRefusal.None, hooks.Prophecy.WhyNoRearrange(game));
            game.CheckToDraw();
            Assert.AreEqual(GamePhase.Drawing, game.Phase);
            Assert.AreEqual(2, hooks.Prophecy.TopCards(game).Count, "still open in the draw");

            game.Draw(new[] { 4 });
            Assert.AreEqual(C("6H"), game.PlayerHand[4], "the prophecy told the truth");
            Assert.IsEmpty(hooks.Prophecy.TopCards(game), "shut after the draw");
            Assert.AreEqual(KitRefusal.NotNow, hooks.Prophecy.WhyNoRearrange(game));
            Assert.IsFalse(hooks.Prophecy.SendToBottom(game, 0));
        }

        [Test]
        public void TheProphecyShutsOnTheTable_AndOpensOnceTheCardsAreDealt()
        {
            var view = new FakeTableView();
            var presenter = new TablePresenter((d, s) => HellPokerGameFactory.Create(GameRules.Default, d, 1, sinner: s), view);
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Warlock, 77);
            HellPokerGame boss = journey.Run.OpenBossTable(GameRules.Default);
            presenter.SitAtBoss(boss, journey.Run, journey.Run.BossBarStart);

            Assert.AreEqual(UiText.KitProphecyShutTitle, view.Kit.Title);
            Assert.AreEqual(UiText.KitProphecyAfterDeal, view.Kit.Note);
            Assert.IsEmpty(view.Kit.Cards);
            presenter.UsePower();
            Assert.AreEqual(UiText.KitAfterTheDeal, view.Message, "K before the deal: after the deal");

            presenter.PerformAction();   // the deal
            Assert.AreEqual(UiText.KitProphecyTitle, view.Kit.Title);
            Assert.AreEqual(journey.Kit.ProphecyCards, view.Kit.Cards.Count);
            presenter.UsePower();
            Assert.AreEqual(string.Format(UiText.RearrangePromptFormat, journey.Kit.RearrangesPerCharge), view.Message);
            presenter.UsePower();
            Assert.AreEqual(UiText.KitUsedThisHand, view.Message, "once a hand");
            presenter.Dispose();
        }

        // ------------------------------------------------------------------ B: the four-colour deck

        [Test]
        public void TheFourColourDeck_IsOnByDefault_SavedAndApplied()
        {
            try
            {
                var store = new MemoryStore();
                var settings = new GameSettings(store);
                Assert.IsTrue(settings.FourColorDeck);
                Assert.IsTrue(SuitColors.FourColor);
                settings.ToggleFourColor();
                Assert.IsFalse(settings.FourColorDeck);
                Assert.IsFalse(SuitColors.FourColor, "applied at once");
                Assert.AreEqual(0, store.GetInt("settings.fourcolor", 1));
                Assert.IsFalse(new GameSettings(store).FourColorDeck, "read back");
            }
            finally
            {
                SuitColors.Set(true);
            }
        }

        [Test]
        public void TheSuitsInk_FourColoursOrTwo()
        {
            Assert.AreEqual(Palette.BlackSuit, Palette.SuitInk(Suit.Spades, true));
            Assert.AreEqual(Palette.RedSuit, Palette.SuitInk(Suit.Hearts, true));
            Assert.AreEqual(Palette.Green, Palette.SuitInk(Suit.Clubs, true));
            Assert.AreEqual(Palette.Blue, Palette.SuitInk(Suit.Diamonds, true));
            Assert.AreEqual(Palette.BlackSuit, Palette.SuitInk(Suit.Clubs, false));
            Assert.AreEqual(Palette.RedSuit, Palette.SuitInk(Suit.Diamonds, false));
        }

        [Test]
        public void TheSettingsScreen_ShowsTheFourColourRow()
        {
            try
            {
                var view = new MainMenuPresenterTests.FakeSettingsView();
                var settings = new GameSettings(new MemoryStore());
                var presenter = new SettingsPresenter(settings, view, new FakeDisplay());
                Assert.IsTrue(view.FourColor);
                view.PressFourColor();
                Assert.IsFalse(view.FourColor);
                Assert.IsFalse(SuitColors.FourColor);
                presenter.Dispose();
            }
            finally
            {
                SuitColors.Set(true);
            }
        }

        private sealed class FakeDisplay : IDisplayMode
        {
            public int DisplayWidth => 1920;
            public int DisplayHeight => 1080;
            public void Apply(bool fullscreen, int windowScale, bool fill, bool vSync) { }
        }

        // ------------------------------------------------------------------ G: the demons' fixed stakes, the last blow

        private static HellPokerGame StackedBoss(Dealer boss, int barStart, string cards)
        {
            GameRules rules = BossTable.Rules(GameRules.Default, boss, barStart, 200);
            var game = new HellPokerGame(rules, TestDecks.Stacked(cards), Evaluator, new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)),
                new HouseDrawStrategy(rules.MaxDiscards), new ScaledPayoutTable(boss.Payouts, 175, 250));
            game.FixedStakeYears = barStart;
            return game;
        }

        [Test]
        public void ADemonsBets_StayAShareOfTheBarAsItBegan()
        {
            HellPokerGame game = StackedBoss(DealerRoster.Mammon, 1300, Deal);
            game.PlaceBet();
            Assert.AreEqual(StakeScale.Default.UnitFor(1300), game.Unit, "the first hand: as before");
            Assert.AreEqual(StakeScale.Default.CapFor(1300), game.TableCap);
            game.Bet(BetAction.Fold);
            game.NextRound();

            game.TakeOver(700, 3);
            game.PlaceBet();
            Assert.AreEqual(100, game.Unit, "the bar at 700: the unit is still a tenth of 1300 (it was 50)");
            Assert.AreEqual(390, game.TableCap);

            HellPokerGame old = StackedBoss(DealerRoster.Mammon, 1300, Deal);
            old.FixedStakeYears = null;
            old.TakeOver(700, 3);
            old.PlaceBet();
            Assert.AreEqual(50, old.Unit, "without the fixed stakes: a tenth of what is left");
            Assert.AreEqual(1300, BossTable.Create(GameRules.Default, DealerRoster.Mammon, 1300, 200, 175, 250, 1, null, null).FixedStakeYears);
        }

        [Test]
        public void AtThreePercentOfTheBar_OneWinEndsTheDemon()
        {
            // The player's aces against nothing.
            HellPokerGame game = StackedBoss(DealerRoster.Mammon, 1300, "AH AD 7C 4S 2D  QS 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H AS 8S");
            game.TakeOver(39, 20);   // 3% of 1300
            Assert.IsTrue(game.NextWinEndsIt, "the last blow");
            Assert.AreEqual(39, game.UpcomingAnte, "less than a unit left: all of it");
            game.PlaceBet();
            Assert.AreEqual(39, game.Ante);
            Assert.AreEqual(GamePhase.Drawing, game.Phase, "all in: the pact is sealed, straight to the draw");
            game.Draw(new int[0]);
            PassToTheEnd(game);
            Assert.AreEqual(GamePhase.Absolved, game.Phase, "one win, and the bar is empty");
            Assert.AreEqual(0, game.Years);
        }

        [Test]
        public void AtLucifersTable_TheSameFixedStakes()
        {
            var sinner = new Sinner(SinnerRoster.Peasant);
            HellPokerGame game = BossTable.Create(GameRules.Default, BossTable.Lucifer, 1050, 101, 110, 90, 3, sinner, sinner);
            Assert.AreEqual(1050, game.FixedStakeYears);
            game.TakeOver(40, 9);
            Assert.IsTrue(game.NextWinEndsIt);
            game.PlaceBet();
            Assert.AreEqual(40, game.Ante, "less than his unit left: all in");
            Assert.AreEqual(100, game.Unit);
        }

        // ------------------------------------------------------------------ H: the class powers, once every hand

        [Test]
        public void TheWarlocksPower_OnceEveryHand_NoGauge()
        {
            var kit = new ClassKit(new Sinner(SinnerRoster.Warlock));
            HellPokerGame game = Stacked(Deal, kit.Sinner);
            ChapterHooks hooks = ChapterHooks.Attach(game, kit, null, 0, 1);
            for (int hand = 0; hand < 3; hand++)
            {
                Assert.AreEqual(0, kit.Sinner.Charge >= kit.Sinner.Rules.Full ? 1 : 0, "the gauge is never full: it does not matter");
                game.PlaceBet();
                Assert.IsTrue(hooks.Prophecy.IsReady);
                Assert.IsTrue(hooks.Prophecy.StartRearranging(game), $"hand {hand}");
                Assert.AreEqual(KitRefusal.AlreadyUsed, hooks.Prophecy.WhyNoRearrange(game));
                Assert.IsTrue(hooks.Prophecy.UsedThisHand);
                game.Bet(BetAction.Fold);
                Assert.IsFalse(hooks.Prophecy.UsedThisHand, "ready again once the hand is settled");
                game.NextRound();
            }
            Assert.AreEqual(3, kit.PowerUses);
        }

        [Test]
        public void TheKingsFreeSeizure_TheLegion_TheHarvest_OnceEveryHand()
        {
            // The King: a seizure at half its price, every hand (4 taxes: two seizures, then none).
            var king = new ClassKit(new Sinner(SinnerRoster.King));
            king.Restore(null, 4, null, 0);
            HellPokerGame kingGame = Stacked("AD KD 7C 4S 2D  AH 9H 8D 5C 3H  6H 10C JH 5D 2C 3S 4H AS 8S", king.Sinner);
            ChapterHooks kingHooks = ChapterHooks.Attach(kingGame, king, null, 0, 1);
            for (int hand = 0; hand < 2; hand++)
            {
                kingGame.PlaceBet();
                if (hand == 1) Assert.AreEqual(KitRefusal.NoTaxes, kingHooks.Treasury.WhyNot(kingGame, TaxAct.Seize), "2 taxes: no full-price seizure");
                Assert.IsNotNull(kingHooks.Treasury.Seize(kingGame, 2, charged: true), $"hand {hand}");
                Assert.AreEqual(KitRefusal.AlreadyUsed, kingHooks.Treasury.WhyNot(kingGame, TaxAct.Seize, charged: true));
                kingGame.Bet(BetAction.Fold);
                kingGame.NextRound();
            }

            // The Summoner: the Legion every hand.
            var summoner = new ClassKit(new Sinner(SinnerRoster.Find(Summoner.ClassId)));
            HellPokerGame summonGame = Stacked(Deal, summoner.Sinner);
            ChapterHooks summonHooks = ChapterHooks.Attach(summonGame, summoner, null, 0, 1);
            for (int hand = 0; hand < 2; hand++)
            {
                summonGame.PlaceBet();
                Assert.IsTrue(summonHooks.Summon.CallLegion(summonGame), $"hand {hand}");
                Assert.AreEqual(KitRefusal.AlreadyUsed, summonHooks.Summon.WhyNoLegion(summonGame));
                summonGame.Bet(BetAction.Fold);
                summonGame.NextRound();
            }

            // The Peasant: a harvest every hand the satchel has a card for.
            var peasant = new ClassKit(new Sinner(SinnerRoster.Peasant));
            peasant.Restore(new[] { C("AC"), C("AS"), C("KS") }, 0, null, 0);
            HellPokerGame peasantGame = Stacked(Deal, peasant.Sinner);
            ChapterHooks peasantHooks = ChapterHooks.Attach(peasantGame, peasant, null, 0, 1);
            for (int hand = 0; hand < 2; hand++)
            {
                peasantGame.PlaceBet();
                Assert.AreEqual(KitRefusal.NotNow, peasantHooks.Satchel.WhyNoHarvest(peasantGame, new[] { 4 }), "the harvest is made in the draw");
                peasantGame.CheckToDraw();
                Assert.IsNotNull(peasantHooks.Satchel.Harvest(peasantGame, new[] { 4 }), $"hand {hand}");
                PassToTheEnd(peasantGame);
                peasantGame.NextRound();
            }
            Assert.AreEqual(2, peasant.Harvests);
        }

        [Test]
        public void TheBadge_SaysReady_ThenUsedThisHand()
        {
            var view = new FakeTableView();
            var presenter = new TablePresenter((d, s) => HellPokerGameFactory.Create(GameRules.Default, d, 1, sinner: s), view);
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Warlock, 78);
            presenter.SitAtBoss(journey.Run.OpenBossTable(GameRules.Default), journey.Run, journey.Run.BossBarStart);
            Assert.AreEqual(UiText.PowerReadyHint, view.Sinner.PowerState);
            Assert.IsTrue(view.Sinner.PowerReady);
            presenter.PerformAction();
            presenter.UsePower();
            Assert.AreEqual(UiText.PowerUsedThisHand, view.Sinner.PowerState);
            Assert.IsFalse(view.Sinner.PowerReady);
            presenter.Dispose();
        }

        // ------------------------------------------------------------------ F: every card cheat, at a Phase 2 demon's table

        private static readonly string[] Relics = { RelicIds.BoneDie, ChapterRelicIds.CandleStub, ChapterRelicIds.SilverBell, ChapterRelicIds.IronVow };

        /// <summary>A cheat, the demon whose it is, and how many of the player's cards it changes (a mark is no change).</summary>
        private static readonly (ICheat cheat, Dealer boss, int changes)[] CardCheats =
        {
            (new BuyoutCheat(), DealerRoster.Mammon, 1), (new CollateralCheat(), DealerRoster.Mammon, 0),
            (new FalseFaceCheat(), DealerRoster.Belial, 0), (new ForkedTongueCheat(), DealerRoster.Belial, 1),
            (new SerpentSwapCheat(), DealerRoster.Belial, 1),
            (new NightVeilCheat(), DealerRoster.Lilith, 0), (new ThornCheat(), DealerRoster.Lilith, 0), (new MoonlessCheat(), DealerRoster.Lilith, 0),
            (new BurningCardCheat(), BossTable.Lucifer, 1), (new RewriteCheat(), BossTable.Lucifer, 1), (new TheFallCheat(), BossTable.Lucifer, 1)
        };

        private static int Changed(Hand before, Hand after) => Enumerable.Range(0, Hand.Size).Count(i => !before[i].Equals(after[i]));

        [Test]
        public void EveryCardCheat_AtAPhaseTwoDemonsTable_ChangesExactlyItsOwnCards()
        {
            SinnerClass[] classes = { SinnerRoster.Peasant, SinnerRoster.Warlock, SinnerRoster.King, SinnerRoster.Find(Jester.ClassId), SinnerRoster.Find(Summoner.ClassId) };
            foreach (var (cheat, boss, changes) in CardCheats)
            {
                int struck = 0;
                foreach (SinnerClass cls in classes)
                    for (int seed = 0; seed < 25; seed++)
                        struck += PlayOneHand(cheat, boss, changes, cls, seed);
                Assert.Greater(struck, 0, $"{cheat.Id} never struck: the test proves nothing");
            }
        }

        /// <summary>One hand under <paramref name="cheat"/> (its gauge full every hand), the class's acts and the relics with it; after every
        /// command that struck, the player's changed cards are counted. Returns 1 when the cheat struck (and was measured).</summary>
        private static int PlayOneHand(ICheat cheat, Dealer boss, int changes, SinnerClass cls, int seed)
        {
            var sinner = new Sinner(cls) { JackpotFallsTo = Jester.PhaseTwoJackpotFallsTo };
            var kit = new ClassKit(sinner);
            kit.Restore(new[] { C("AC"), C("QC") }, 10, null, ClassKit.SeizePrice);   // a satchel, taxes, Lucifer's own taxes (his seizure)
            GameRules rules = BossTable.Rules(GameRules.Default, boss, 1300, 200);
            var random = new SystemRandomSource(seed);
            var game = new HellPokerGame(rules, new Deck(new FisherYatesShuffler(new SystemRandomSource(seed + 101))), Evaluator,
                new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)), new HouseDrawStrategy(rules.MaxDiscards),
                new ScaledPayoutTable(boss.Payouts, 175, 250), new Betting.HandStrengthBettingStrategy(boss.Betting, new SystemRandomSource(seed + 5)),
                new CheatSession(new OnlyCheat(cheat), 1, random, sinner), random, sinner)
            {
                FixedStakeYears = 1300
            };
            var effects = new RunEffects { MaxCarried = ChapterRelics.Slots, MaxBag = ChapterRelics.BagSlots };
            foreach (string id in Relics) effects.Give(id);
            game.UseEffects(effects);
            kit.Relics = () => effects.CombinedRelics;
            ChapterHooks hooks = ChapterHooks.Attach(game, kit, boss.Id, 1300, seed);

            int measured = 0;
            void Command(Func<Hand> act)
            {
                Hand before = game.PlayerHand;
                int results = game.CheatsThisHand.Count;
                Hand baseline = act() ?? before ?? game.PlayerHand;   // the deal: nothing to compare with (its cheats only mark)
                if (game.CheatsThisHand.Count == results) return;
                CheatResult result = game.CheatsThisHand.Last();
                if (result.Outcome != CheatOutcome.Played) return;
                // A demon's rule that moved a card in the same breath (Belial's swap, Lucifer's seizure) is not the cheat's doing.
                if (hooks.Rule is BelialRoleSwap swap && swap.Given.HasValue) return;
                if (hooks.Rule is LuciferMirror mirror && mirror.Seized.HasValue) return;
                Assert.AreEqual(changes, Changed(baseline, game.PlayerHand), $"{cheat.Id} ({cls.Id}, seed {seed}) changed the wrong number of cards");
                Assert.AreEqual(result.PlayerCards.Count, result.PlayerCardsHit.Count, "every card it touched is known by itself");
                Assert.AreEqual(result.PlayerCardsHit.Count, result.PlayerCardsIn(game.PlayerHand).Count, "and found again where it lies now");
                measured = 1;
            }

            Command(() => { game.PlaceBet(); return null; });
            // The class's acts before the draw (their own commands: a turned card, a free seizure, the imps, the deck rearranged).
            if (hooks.Treasury != null && hooks.Treasury.WhyNot(game, TaxAct.Reveal) == KitRefusal.None) hooks.Treasury.Reveal(game);
            if (hooks.Summon != null && hooks.Summon.WhyNot(game, SummonAct.Eye) == KitRefusal.None) hooks.Summon.Eye(game);
            if (hooks.Prophecy != null && hooks.Prophecy.StartRearranging(game)) hooks.Prophecy.SendToBottom(game, 0);
            while (game.Phase == GamePhase.PlayerReveal) Command(() => { game.Bet(BetAction.Pass); return null; });
            if (game.Phase == GamePhase.Drawing)
            {
                int[] discards = game.SuggestedDiscards().ToArray();
                Command(() => game.Draw(discards).Hand);
            }
            while (game.Phase == GamePhase.DrawReveal || game.Phase == GamePhase.HouseReveal)
                Command(() => { game.Bet(BetAction.Pass); return null; });
            return measured;
        }

        // ------------------------------------------------------------------ I: relics leave only when the player says so

        [Test]
        public void EveryRelic_ComesBackFromASave_WornBaggedForgedCleansedAndCounted()
        {
            var all = ChapterRelics.All.ToList();
            for (int batch = 0; batch * 10 < all.Count; batch++)
            {
                var ids = all.Skip(batch * 10).Take(10).ToList();
                ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Peasant, 300 + batch);
                RunEffects fx = journey.Effects;
                foreach (string id in ids) fx.Give(id);
                fx.AddRelic(RelicIds.JestersRattle);
                fx.Forge(fx.Relics[0]);
                if (fx.Relics.Count > 1) fx.SilenceCurse(fx.Relics[1]);
                if (fx.Bag.Count > 0) fx.Cleanse(fx.Bag[0]);
                fx.SpendRedraw();

                Assert.IsTrue(ChapterSave.TryDecode(journey.Capture().Encode(), out ChapterSave save));
                ChapterJourney back = ChapterJourney.Restore(save);
                CollectionAssert.AreEqual(fx.Relics, back.Effects.Relics, $"batch {batch}: worn");
                CollectionAssert.AreEqual(fx.Bag, back.Effects.Bag, $"batch {batch}: the bag");
                CollectionAssert.AreEquivalent(fx.Forged, back.Effects.Forged);
                CollectionAssert.AreEquivalent(fx.Cleansed, back.Effects.Cleansed);
                Assert.AreEqual(fx.SilencedCurse, back.Effects.SilencedCurse);
                Assert.AreEqual(fx.RedrawsLeft, back.Effects.RedrawsLeft, "the redraws' count");
            }
        }

        [Test]
        public void ANewChapter_AndLucifersTable_KeepEveryRelic()
        {
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Peasant, 41);
            foreach (string id in ChapterRelics.All.Take(8)) journey.Effects.Give(id);
            var worn = journey.Effects.Relics.ToList();
            var bag = journey.Effects.Bag.ToList();
            for (int chapter = 1; chapter <= ChapterRules.Chapters; chapter++)
            {
                journey.Run.PayTribute();
                journey.Run.LeaveBossTable(0);
                if (chapter == ChapterRules.Chapters) break;
                journey.Run.TakeLootCoins();
                journey.NextChapter();
                CollectionAssert.AreEqual(worn, journey.Effects.Relics, $"chapter {chapter + 1}");
                CollectionAssert.AreEqual(bag, journey.Effects.Bag);
            }
            journey.OpenLuciferTable(GameRules.Default);
            CollectionAssert.AreEqual(worn, journey.Effects.Relics, "Lucifer's table");
            CollectionAssert.AreEqual(bag, journey.Effects.Bag);
        }

        [Test]
        public void TheSinSwap_WithNothingToOfferForIt_KeepsTheRelic()
        {
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Peasant, 42);
            ChapterRun run = journey.Run;
            run.Effects.Give(ChapterRelicIds.AshLedger);
            run.RelicAvailable = id => false;   // nothing left to draw
            EventOutcome outcome = new SinSwap().Choose(run, EventChoices.GivePrefix + ChapterRelicIds.AshLedger);
            Assert.AreEqual("none", outcome.Result);
            Assert.IsFalse(outcome.Waits);
            CollectionAssert.Contains(run.Effects.Relics, ChapterRelicIds.AshLedger, "a relic leaves only for what the player chose");
        }

        // ------------------------------------------------------------------ D: the Devil's Compass

        [Test]
        public void TheCompass_StepsAcrossAFloor_TwiceAChapter_FortyYearsEach()
        {
            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Peasant, 43);
            ChapterRun run = journey.Run;
            Assert.AreEqual(0, run.CompassStepsLeft, "no compass, no steps");
            run.Effects.Give(ChapterRelicIds.DevilsCompass);
            Assert.AreEqual(ChapterRelics.CompassSteps, run.CompassStepsLeft);
            Assert.IsEmpty(run.CompassTargets, "before the first floor: nowhere to step from");

            MapNode first = run.Map.Floors[0][0];
            run.MoveTo(first);
            CollectionAssert.AreEquivalent(run.Map.Floors[0].Where(n => n != first), run.CompassTargets, "the floor's other nodes");
            MapNode second = run.Choices.First();
            run.MoveTo(second);
            var targets = run.CompassTargets.ToList();
            CollectionAssert.AreEquivalent(run.Map.Floors[1].Where(n => n != second), targets);
            // Best a node the first floor's node has no way to (the save must not cut the trail there).
            MapNode to = targets.FirstOrDefault(n => !run.Map.NextFrom(first).Contains(n)) ?? targets.First();
            int sentence = journey.Sentence.Years;
            Assert.IsTrue(run.StepWithCompass(to));
            Assert.AreEqual(to, run.Current);
            Assert.AreEqual(to, run.Trail.Last(), "the trail stands where the player does");
            Assert.AreEqual(2, run.Trail.Count);
            Assert.AreEqual(sentence + ChapterRelics.CompassYears, journey.Sentence.Years, "the curse");
            CollectionAssert.AreEquivalent(run.Map.NextFrom(to), run.Choices, "the way goes on from the new node");
            Assert.AreEqual(1, run.CompassStepsLeft);
            Assert.IsFalse(run.StepWithCompass(to), "not where it stands");

            // Saved and continued: where the player stands, the steps taken.
            Assert.IsTrue(ChapterSave.TryDecode(journey.Capture().Encode(), out ChapterSave save));
            ChapterJourney back = ChapterJourney.Restore(save);
            Assert.AreEqual(1, back.Run.Current.Floor);
            Assert.AreEqual(to.Lane, back.Run.Current.Lane, "the step survives a save (the trail is not cut there)");
            Assert.AreEqual(1, back.Run.CompassStepsLeft);

            Assert.IsTrue(run.StepWithCompass(second));
            Assert.AreEqual(0, run.CompassStepsLeft);
            Assert.IsEmpty(run.CompassTargets, "two a chapter");
            Assert.IsFalse(run.StepWithCompass(to));
        }

        [Test]
        public void TheCompass_IsLocked_UntilThreeWardensFallInOneChapter()
        {
            var records = new ChapterRecords();
            Assert.IsFalse(ChapterUnlocks.IsRelicOpen(ChapterRelicIds.DevilsCompass, records));
            records.WardenBeaten(1);
            records.WardenBeaten(2);
            Assert.IsFalse(ChapterUnlocks.IsRelicOpen(ChapterRelicIds.DevilsCompass, records));
            records.WardenBeaten(3);
            Assert.IsTrue(ChapterUnlocks.IsRelicOpen(ChapterRelicIds.DevilsCompass, records));
            Assert.AreEqual(3, ChapterRecords.Decode(records.Encode()).MostWardensInAChapter, "kept in the book");
            Assert.AreEqual(RelicRarity.Rare, ChapterRelics.RarityOf(ChapterRelicIds.DevilsCompass));
            CollectionAssert.Contains(ChapterRelics.All, ChapterRelicIds.DevilsCompass);
            Assert.AreEqual(RelicRoster.IconOrder.Count - 1, RelicRoster.IconOrder.ToList().IndexOf(ChapterRelicIds.DevilsCompass), "its icon is the strip's last");
        }

        // ------------------------------------------------------------------ the presenter: the compass on the map, the result band

        private sealed class Chapters : IDisposable
        {
            public readonly FakeChapterMapView Map = new FakeChapterMapView();
            public readonly FakeChapterPanelView Panel = new FakeChapterPanelView();
            public readonly FakeTableView View = new FakeTableView();
            public readonly TablePresenter Table;
            public readonly ChapterPresenter Presenter;

            public Chapters()
            {
                Table = new TablePresenter((d, s) => HellPokerGameFactory.Create(GameRules.Default, d, 1, sinner: s), View);
                Presenter = new ChapterPresenter(Map, Panel, () => (Table, View), null, null, GameRules.Default, () => 4242);
                Presenter.Start(SinnerRoster.Peasant);
                Panel.Press(0);   // DESCEND
            }

            public void Dispose() => Presenter.Dispose();
        }

        [Test]
        public void TheMapsCompass_LightsTheFloor_StepsWhereClicked_EscPutsItAway()
        {
            using (var c = new Chapters())
            {
                ChapterRun run = c.Presenter.Run;
                run.Effects.Give(ChapterRelicIds.DevilsCompass);
                c.Map.PressCompass();
                Assert.IsFalse(c.Presenter.IsCompassOut, "before the first floor it cannot be used");
                Assert.AreEqual(UiText.CompassNotYet, c.Map.State.Prompt);

                MapNode first = run.Map.Floors[0].First();
                run.MoveTo(first);
                c.Map.PressCompass();
                Assert.IsTrue(c.Presenter.IsCompassOut);
                Assert.AreEqual(run.Map.Floors[0].Count - 1, c.Map.State.CompassTargets.Count);
                Assert.IsEmpty(c.Map.State.Choices, "the ways wait while the compass picks");
                Assert.IsTrue(c.Presenter.Back(), "Esc puts it away");
                Assert.IsFalse(c.Presenter.IsCompassOut);

                c.Map.PressCompass();
                MapNode to = c.Map.State.CompassTargets.First();
                c.Map.Press(to);
                Assert.AreEqual(to, run.Current);
                Assert.AreEqual(1, c.Map.Steps.Count, "the flicker across the floor");
                Assert.AreEqual(string.Format(UiText.CompassButtonFormat, 1), c.Map.State.CompassLabel);
                Assert.AreEqual(string.Format(UiText.CompassDoneFormat, ChapterRelics.CompassYears), c.Map.State.Prompt);
            }
        }

        [Test]
        public void AFloorHand_EndsWithTheBand_AFlash_AFloatAndAPip_AndNextHandTakesTheBandDown()
        {
            using (var c = new Chapters())
            {
                MapNode table = c.Map.State.Choices.First(n => ChapterRun.KindOf(n) == NodeKind.Table);
                c.Map.Press(table);
                Assert.IsTrue(c.Presenter.IsAtTable);
                int guard = 0;
                while (c.Table.Game.Phase != GamePhase.RoundOver && guard++ < 50) c.Table.PerformAction();
                Assert.AreEqual(GamePhase.RoundOver, c.Table.Game.Phase);

                ResultBand band = c.View.Band;
                Assert.IsNotNull(band, "the result band");
                Assert.That(new[] { UiText.BandTitle(HandOutcome.Win), UiText.BandTitle(HandOutcome.Loss), UiText.BandTitle(HandOutcome.Push),
                    UiText.BandTitle(HandOutcome.Fold) }, Does.Contain(band.Title));
                int coins = c.Table.LastFloorHand.Coins;
                Assert.AreEqual(coins == 0 ? UiText.BandCoinsStay : string.Format(UiText.BandCoinsFormat, UiText.SignedYears(coins)), band.Amount);
                Assert.AreEqual(1, c.View.RecentHands.Count, "the hand's pip");
                if (coins != 0) Assert.AreEqual(UiText.SignedYears(coins), c.View.Floats.Last().text);
                if (band.Outcome == HandOutcome.Win) Assert.IsTrue(c.View.Moments.Any(m => m.moment == TableMoment.WinGlow));
                if (band.Outcome == HandOutcome.Loss) Assert.IsTrue(c.View.Moments.Any(m => m.moment == TableMoment.LossGlow));
                Assert.AreEqual(0, c.View.Pot, "the pot is gone: the band says the amount");

                c.Table.PerformAction();   // NEXT HAND
                Assert.IsNull(c.View.Band, "down with the next hand");
                Assert.AreEqual(1, c.View.RecentHands.Count, "the pips stay");
            }
        }
    }
}
