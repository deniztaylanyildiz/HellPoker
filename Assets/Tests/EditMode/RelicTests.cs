using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HellPoker.Core.Betting;
using HellPoker.Core.Cards;
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
    /// Cursed relics: each one's gift and curse at the table, two at most, the Bone Die's redraw and its limits, the save,
    /// the offers that bring them, and the table showing them beside the portrait.
    /// </summary>
    public class RelicTests
    {
        private const string Flush = "2C 9C JC 4C KC";
        private const string HouseTwos = "2D 2H 5S 7H 9D";
        private const string Nothing = "2C 5D 7H 9S JC";
        private const string HouseFullHouse = "KS KH KD 4C 4H";
        private const string Blanks = "3S 6D 10S 8H 3H QC 8C 7C 6H 4S 10D 3D";

        private sealed class FirstChoice : IRandomSource
        {
            public int Next(int maxExclusive) => 0;
        }

        private sealed class OnlyCheat : ICheatPolicy
        {
            private readonly ICheat _cheat;
            public OnlyCheat(ICheat cheat) => _cheat = cheat;
            public IReadOnlyList<ICheat> Cheats => new[] { _cheat };
            public CheatPick Choose(CheatContext context, IRandomSource random) => new CheatPick(_cheat, null);
            public ICheat Find(string id) => id == _cheat.Id ? _cheat : null;
        }

        private static HellPokerGame Game(string player = Flush, string house = HouseTwos, int years = 1000, IHouseBettingStrategy betting = null,
            ICheat cheat = null, int maliceMax = 10, Sinner sinner = null, params string[] relics)
        {
            Dealer dealer = DealerRoster.Mammon;
            var random = new FirstChoice();
            GameRules rules = dealer.ApplyTo(new GameRules(1000, 5000, luciferGateYears: 0));
            var game = new HellPokerGame(rules, TestDecks.Stacked($"{player} {house} {Blanks}"), HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)), new HouseDrawStrategy(rules.MaxDiscards), dealer.Payouts, betting,
                new CheatSession(cheat == null ? null : new OnlyCheat(cheat), maliceMax, random, sinner), random, sinner);
            if (years != 1000) game.TakeOver(years, 3);
            foreach (string id in relics) Assert.IsTrue(game.Effects.AddRelic(id));
            return game;
        }

        private static void PlayOut(HellPokerGame game)
        {
            if (game.Phase == GamePhase.Betting) game.PlaceBet();
            if (game.Phase == GamePhase.PlayerReveal) game.CheckToDraw();
            game.Draw(new int[0]);
            while (game.Phase == GamePhase.DrawReveal || game.Phase == GamePhase.HouseReveal) game.Bet(BetAction.Pass);
        }

        private static int Forgiven(params string[] relics)
        {
            HellPokerGame game = Game(relics: relics);
            PlayOut(game);
            return -game.LastRound.YearsChange;
        }

        // ------------------------------------------------------------------ the roster

        [Test]
        public void FourRelics_EachWithAGiftAndACurse_AndUniqueIds()
        {
            Assert.AreEqual(4, RelicRoster.All.Count);
            Assert.AreEqual(RelicRoster.All.Count, RelicRoster.All.Select(r => r.Id).Distinct().Count());
            foreach (IRelic relic in RelicRoster.All)
                Assert.IsTrue(relic.Effects != RelicEffects.None, relic.Id);
            Assert.IsNull(RelicRoster.Find("nonsense"));
        }

        [Test]
        public void ARunCarriesTwoAtMost_NeverTheSameTwice()
        {
            var effects = new RunEffects();
            Assert.IsTrue(effects.AddRelic(RelicIds.BoneDie));
            Assert.IsFalse(effects.AddRelic(RelicIds.BoneDie), "Not twice.");
            Assert.IsFalse(effects.AddRelic("nonsense"), "Not an unknown one.");
            Assert.IsTrue(effects.AddRelic(RelicIds.RustyCrown));
            Assert.IsFalse(effects.AddRelic(RelicIds.FerrymansCoin), "Two at most.");
            CollectionAssert.AreEqual(new[] { RelicIds.BoneDie, RelicIds.RustyCrown }, effects.Relics);
        }

        [Test]
        public void TwoRelics_Combine_PercentsMultiply_CountsAdd()
        {
            RelicEffects both = RelicRoster.Combined(new[] { RelicIds.RustyCrown, RelicIds.ThornedRosary });
            Assert.AreEqual(94, both.WinPercent, "105% of 90%.");
            Assert.AreEqual(1, both.MaliceExtraPerHand);
            Assert.AreEqual(125, both.SoulLossPercent);
            Assert.AreSame(RelicEffects.None, RelicRoster.Combined(null));
        }

        // ------------------------------------------------------------------ each relic at the table

        [Test]
        public void TheRustyCrown_ForgivesATwentiethMore_AndTheMaliceGrowsFaster()
        {
            Assert.AreEqual(Forgiven() * 105 / 100, Forgiven(RelicIds.RustyCrown));

            HellPokerGame plain = Game(cheat: new CollateralCheat());
            HellPokerGame crowned = Game(cheat: new CollateralCheat(), relics: RelicIds.RustyCrown);
            plain.PlaceBet();
            crowned.PlaceBet();
            Assert.AreEqual(plain.Malice + 1, crowned.Malice);
        }

        [Test]
        public void TheThornedRosary_ForgivesATenthLess_ButTheSoulBurnsSlower()
        {
            Assert.AreEqual(Forgiven() * 90 / 100, Forgiven(RelicIds.ThornedRosary));

            HellPokerGame plain = Game(Nothing, HouseFullHouse, 2100);
            HellPokerGame rosary = Game(Nothing, HouseFullHouse, 2100, relics: RelicIds.ThornedRosary);
            PlayOut(plain);
            PlayOut(rosary);
            Assert.IsTrue(plain.LastRound.YearsChange > 0);
            Assert.Less(rosary.LastRound.YearsChange, plain.LastRound.YearsChange, "×1.25 instead of ×1.5.");
        }

        [Test]
        public void ACutWin_StillEndsTheLastYears()
        {
            // Regression: the percent was taken of a win already capped at the sentence, so 30 years left became 3, then 1,
            // then 1 for ever (90% of 1 is 0) — with the Rosary nobody could beat Lucifer.
            HellPokerGame rosary = Game(years: 30, relics: RelicIds.ThornedRosary);
            PlayOut(rosary);
            Assert.AreEqual(GamePhase.Absolved, rosary.Phase);
            Assert.AreEqual(0, rosary.Years);

            HellPokerGame charon = Game(years: 20);   // the flush wins 50: half of it still covers 20 (capped first: only 10)
            charon.Effects.NextHand = new HandModifier(winPercent: 50);
            PlayOut(charon);
            Assert.AreEqual(0, charon.Years, "Charon's half win too.");
        }

        [Test]
        public void TheOutlook_KnowsTheRelics_TheAnteAndTheLeastWin()
        {
            HellPokerGame plain = Game();
            HellPokerGame coin = Game(relics: RelicIds.FerrymansCoin);
            HellPokerGame rosary = Game(relics: RelicIds.ThornedRosary);

            Assert.AreEqual(100, plain.UpcomingAnte);
            Assert.AreEqual(80, coin.UpcomingAnte, "The DEAL button tells the ante that will be placed.");
            Assert.AreEqual(plain.LeastYearsForgiven * 90 / 100, rosary.LeastYearsForgiven);
            rosary.PlaceBet();
            plain.PlaceBet();
            Assert.AreEqual(plain.LeastYearsForgiven * 90 / 100, rosary.LeastYearsForgiven, "In the hand too.");
        }

        [Test]
        public void TheFerrymansCoin_CutsTheAnte_ButTheHouseShowsACardFewer()
        {
            HellPokerGame plain = Game();
            HellPokerGame coin = Game(relics: RelicIds.FerrymansCoin);
            plain.PlaceBet();
            coin.PlaceBet();

            Assert.AreEqual(100, plain.Ante);
            Assert.AreEqual(80, coin.Ante);
            Assert.AreEqual(2, plain.HouseCardsShown);
            Assert.AreEqual(1, coin.HouseCardsShown);
        }

        [Test]
        public void TheBoneDie_MakesTheHouseReRaiseTwoUnits()
        {
            HellPokerGame game = Game(Nothing, HouseFullHouse, betting: new HellPokerGameTests.FixedHouseBetting(true), relics: RelicIds.BoneDie);
            game.PlaceBet();
            game.CheckToDraw();
            game.Draw(new int[0]);

            game.Bet(BetAction.Raise);

            Assert.AreEqual(GamePhase.HouseReRaise, game.Phase);
            Assert.AreEqual(200, game.HouseReRaiseAmount, "Two units instead of one.");
        }

        // ------------------------------------------------------------------ the Bone Die's redraw

        [Test]
        public void TheBoneDie_RedrawsOneSeenCard_OnceATable_BeforeTheDraw()
        {
            HellPokerGame plain = Game(Nothing, HouseFullHouse);
            plain.PlaceBet();
            Assert.AreEqual(0, plain.RedrawsLeft);
            Assert.IsFalse(plain.CanRedraw(0), "No die, no redraw.");

            HellPokerGame game = Game(Nothing, HouseFullHouse, relics: RelicIds.BoneDie);
            Assert.IsFalse(game.CanRedraw(0), "Nothing dealt yet.");
            game.PlaceBet();
            Assert.AreEqual(1, game.RedrawsLeft);
            Assert.IsFalse(game.CanRedraw(Hand.Size - 1), "A card not yet seen.");
            Assert.IsTrue(game.CanRedraw(0));

            Card? card = game.Redraw(0);

            Assert.AreEqual(TestCards.Card("3S"), card);
            Assert.AreEqual(TestCards.Card("3S"), game.PlayerHand[0]);
            Assert.AreEqual(0, game.RedrawsLeft);
            Assert.IsFalse(game.CanRedraw(1), "Once a table.");
            Assert.IsNull(game.Redraw(1));
        }

        [Test]
        public void TheBoneDie_IsNotRolledAfterTheDraw_AndAnUnusedRollWaits()
        {
            HellPokerGame game = Game(Nothing, HouseFullHouse, relics: RelicIds.BoneDie);
            game.PlaceBet();
            game.CheckToDraw();
            Assert.IsTrue(game.CanRedraw(4), "At the draw every card is seen.");
            game.Draw(new int[0]);
            Assert.IsFalse(game.CanRedraw(0), "After the draw: too late.");

            while (game.Phase == GamePhase.DrawReveal || game.Phase == GamePhase.HouseReveal) game.Bet(BetAction.Pass);
            game.NextRound();
            game.PlaceBet();
            Assert.AreEqual(1, game.RedrawsLeft, "Not rolled: still there next hand.");
        }

        [Test]
        public void TheBoneDie_RolledOnce_StaysSpentForTheTable_AndANewDemonFillsIt()
        {
            HellPokerGame game = Game(Nothing, HouseFullHouse, relics: RelicIds.BoneDie);
            game.Effects.SitAt("mammon");
            game.PlaceBet();
            Assert.IsNotNull(game.Redraw(0));
            PlayOut(game);
            for (int hand = 0; hand < 3 && !game.IsGameOver; hand++)
            {
                game.NextRound();
                game.PlaceBet();
                Assert.AreEqual(0, game.RedrawsLeft, "No new roll at a new hand.");
                Assert.IsFalse(game.CanRedraw(0));
                PlayOut(game);
            }

            game.Effects.SitAt("belial");   // another demon's table (the presenter does it on every change of seat)
            Assert.AreEqual(1, game.Effects.RedrawsLeft, "A demon never sat with: full.");
            game.Effects.SitAt("mammon");
            Assert.AreEqual(0, game.Effects.RedrawsLeft, "Back at Mammon's: still spent.");

            var plain = new RunEffects();
            plain.SitAt("belial");
            Assert.AreEqual(0, plain.RedrawsLeft, "No die, nothing to fill.");
        }

        [Test]
        public void TheBoneDie_CannotMoveAChainedCard()
        {
            HellPokerGame game = Game(Nothing, HouseFullHouse, cheat: new CollateralCheat(), maliceMax: 1, relics: RelicIds.BoneDie);
            game.PlaceBet();
            game.CheckToDraw();   // the chain strikes as the draw opens
            int chained = Enumerable.Range(0, Hand.Size).Single(game.IsPlayerCardChained);

            Assert.IsFalse(game.CanRedraw(chained));
        }

        [Test]
        public void TheBoneDie_CannotShakeOffAThorn()
        {
            HellPokerGame game = Game(Nothing, HouseFullHouse, cheat: new ThornCheat(), maliceMax: 1, relics: RelicIds.BoneDie);
            game.PlaceBet();
            game.CheckToDraw();   // the thorn strikes as the draw opens
            int thorned = Enumerable.Range(0, Hand.Size).Single(game.IsPlayerCardThorned);

            Assert.IsFalse(game.CanRedraw(thorned));
            Assert.IsNull(game.Redraw(thorned));
            Assert.IsTrue(game.CanRedraw(Enumerable.Range(0, Hand.Size).First(i => i != thorned)), "The other cards still may.");
        }

        [Test]
        public void TheBoneDie_CannotRollAVeiledCard()
        {
            HellPokerGame game = Game(Nothing, HouseFullHouse, cheat: new NightVeilCheat(), maliceMax: 1, relics: RelicIds.BoneDie);
            game.PlaceBet();      // the veil falls at the deal
            game.CheckToDraw();
            int veiled = Enumerable.Range(0, Hand.Size).Single(game.IsPlayerCardHidden);

            Assert.IsFalse(game.CanRedraw(veiled), "A card never seen cannot be thrown back this way.");
            Assert.AreEqual(1, game.RedrawsLeft);
        }

        [Test]
        public void TheBoneDie_LeavesTheKingsProtectedCardAlone()
        {
            var sinner = new Sinner(new King());
            HellPokerGame game = Game(Nothing, HouseFullHouse, sinner: sinner, relics: RelicIds.BoneDie);
            game.PlaceBet();
            Assert.IsTrue(game.Protect(0));

            Assert.IsFalse(game.CanRedraw(0), "Protected from everything — the die too.");
            Assert.IsTrue(game.CanRedraw(1));
        }

        // ------------------------------------------------------------------ the offers and the save

        [Test]
        public void ARelicOffer_GivesARelicNotCarried_AndStopsAtTwo()
        {
            HellPokerGame game = Game();
            var offer = new RelicEvent(EventIds.GraveRobber);
            Assert.IsTrue(offer.CanAppear(game, "mammon"));

            offer.Apply(EventOptions.Pass, game, "mammon", new FirstChoice());
            Assert.IsEmpty(game.Effects.Relics);
            Assert.IsNull(offer.LastGiven);

            offer.Apply(EventOptions.Accept, game, "mammon", new FirstChoice());
            Assert.AreEqual(RelicIds.BoneDie, offer.LastGiven);
            offer.Apply(EventOptions.Accept, game, "mammon", new FirstChoice());
            Assert.AreEqual(RelicIds.RustyCrown, offer.LastGiven, "Never one already carried.");

            Assert.IsFalse(offer.CanAppear(game, "mammon"), "Two carried: no more offers.");
            Assert.AreEqual((20 - 40) / 2, offer.ExpectedYears(game, "mammon"), "What is left to win: the coin and the rosary.");
            CollectionAssert.Contains(EventDeck.Standard.Select(e => e.Id).ToList(), EventIds.GraveRobber);
            CollectionAssert.Contains(EventDeck.Standard.Select(e => e.Id).ToList(), EventIds.CursedChest);
        }

        [Test]
        public void TheSave_KeepsTheRelics_AndAnOlderSaveHasNone()
        {
            var state = new RunEventState(null, 0, null, 0, 0, 0, new[] { RelicIds.FerrymansCoin, RelicIds.BoneDie });
            var snapshot = new RunSnapshot("mammon", 900, 7, new RunStats(1000, "mammon"), events: state);

            Assert.IsTrue(RunSnapshot.TryDecode(snapshot.Encode(), out RunSnapshot back));
            CollectionAssert.AreEqual(new[] { RelicIds.FerrymansCoin, RelicIds.BoneDie }, back.Events.Relics);

            var plain = new RunSnapshot("mammon", 900, 7, new RunStats(1000, "mammon"));
            Assert.IsTrue(RunSnapshot.TryDecode(plain.Encode(), out RunSnapshot none));
            Assert.IsEmpty(none.Events.Relics);

            var effects = new RunEffects();
            effects.Restore(null, 0, 0, 0, new[] { RelicIds.BoneDie, "nonsense", RelicIds.BoneDie, RelicIds.RustyCrown, RelicIds.ThornedRosary });
            CollectionAssert.AreEqual(new[] { RelicIds.BoneDie, RelicIds.RustyCrown }, effects.Relics, "A broken list is cleaned up.");
        }

        [Test]
        public void TheSave_KeepsTheDiesRollLeft_AndAnOlderSaveHasItFull()
        {
            var spent = new RunEventState(null, 0, null, 0, 0, 0, new[] { RelicIds.BoneDie }, relicRedraws: 0);
            Assert.IsTrue(RunSnapshot.TryDecode(new RunSnapshot("mammon", 900, 7, new RunStats(1000, "mammon"), events: spent).Encode(), out RunSnapshot back));
            StringAssert.Contains("relics.redraws=0", new RunSnapshot("mammon", 900, 7, new RunStats(1000, "mammon"), events: spent).Encode());
            Assert.AreEqual(0, back.Events.RelicRedraws);
            var effects = new RunEffects();
            effects.Restore(null, 0, 0, 0, back.Events.Relics, back.Events.RelicRedraws);
            Assert.AreEqual(0, effects.RedrawsLeft, "Spent at this table: still spent.");

            // An older v=4 save (relics, but no "relics.redraws"): the die is full.
            var older = new RunEventState(null, 0, null, 0, 0, 0, new[] { RelicIds.BoneDie });
            string text = new RunSnapshot("mammon", 900, 7, new RunStats(1000, "mammon"), events: older).Encode();
            StringAssert.DoesNotContain("relics.redraws", text);
            Assert.IsTrue(RunSnapshot.TryDecode(text, out RunSnapshot old));
            Assert.AreEqual(-1, old.Events.RelicRedraws);
            effects.Restore(null, 0, 0, 0, old.Events.Relics, old.Events.RelicRedraws);
            Assert.AreEqual(1, effects.RedrawsLeft);

            effects.Restore(null, 0, 0, 0, new[] { RelicIds.RustyCrown }, 5);
            Assert.AreEqual(0, effects.RedrawsLeft, "No die: a saved count means nothing.");
        }

        // ------------------------------------------------------------------ at the table

        private FakeTableView _view;
        private TablePresenter _presenter;
        private HellPokerGame _game;

        [TearDown]
        public void TearDown() => _presenter?.Dispose();

        private void Table(EventSession events, RunArchive archive = null, Dealer finalDealer = null)
        {
            _view = new FakeTableView();
            _presenter = new TablePresenter((d, sinner) =>
            {
                var random = new FirstChoice();
                return _game = new HellPokerGame(d.ApplyTo(new GameRules(1000, 5000, luciferGateYears: finalDealer == null ? 0 : 250)),
                    TestDecks.Stacked($"{Nothing} {HouseFullHouse} {Blanks}"), HandEvaluator.CreateDefault(), new CardExchanger(new MaxDiscardPolicy()),
                    new HouseDrawStrategy(), d.Payouts, null, new CheatSession(null, 0, random), random, sinner);
            }, _view, null, archive, finalDealer, events);
        }

        [Test]
        public void TheRelics_StayWithTheRun_AtANewTable()
        {
            Table(null);
            _presenter.StartNewRun(DealerRoster.Mammon);
            _game.Effects.AddRelic(RelicIds.FerrymansCoin);
            _game.Effects.AddRelic(RelicIds.RustyCrown);
            HellPokerGame first = _game;

            _presenter.SwitchTable(DealerRoster.Belial);

            Assert.AreNotSame(first, _game, "A new table, a new game.");
            CollectionAssert.AreEqual(new[] { RelicIds.FerrymansCoin, RelicIds.RustyCrown }, _game.Effects.Relics);
            Assert.AreEqual(2, _view.Relics.Count);
            _view.PressAction();
            Assert.AreEqual(80, _game.Relic.AntePercent, "And they work there.");
        }

        [Test]
        public void TheRelics_GoDownToLucifer_AndHisTableFillsTheDie()
        {
            Table(null, finalDealer: DealerRoster.Lucifer);
            _presenter.StartNewRun(DealerRoster.Mammon);
            _game.Effects.AddRelic(RelicIds.BoneDie);
            Assert.IsTrue(_game.Effects.SpendRedraw(), "Rolled at Mammon's table.");
            _game.TakeOver(200, 3);

            _presenter.SwitchTable(DealerRoster.Mammon);   // between hands below the gate: summoned

            Assert.IsTrue(_game.Rules.IsFinalTable, "At Lucifer's table.");
            CollectionAssert.AreEqual(new[] { RelicIds.BoneDie }, _game.Effects.Relics);
            Assert.AreEqual(1, _view.Relics.Count);
            Assert.AreEqual(1, _view.Relics[0].Uses, "Summoned: a new table, the die is full.");
            _view.PressAction();
            Assert.AreEqual(1, _game.RedrawsLeft, "The die rolls at his table too.");

            PlayAHand();           // a full house at his table: 700, above the gate
            _view.PressAction();   // between hands: cast down to Mammon

            Assert.IsFalse(_game.Rules.IsFinalTable, "Cast down.");
            Assert.AreEqual(0, _game.RedrawsLeft, "Back at Mammon's table: what was left there — nothing.");
        }

        // ------------------------------------------------------------------ per-table charges are kept per demon

        [Test]
        public void HoppingTables_DoesNotRefillTheDie()
        {
            Table(null);
            _presenter.StartNewRun(DealerRoster.Mammon);
            _game.Effects.AddRelic(RelicIds.BoneDie);
            _view.PressAction();
            _view.PressRelic(RelicIds.BoneDie);
            _view.PlayerView.Click(0);
            Assert.AreEqual(0, _game.RedrawsLeft, "Rolled at Mammon's.");
            PlayAHand();
            _view.PressAction();

            _presenter.SwitchTable(DealerRoster.Belial);
            Assert.AreEqual(1, _game.RedrawsLeft, "Belial's table, never sat at: full.");
            Assert.AreEqual(1, _view.Relics[0].Uses);

            _presenter.SwitchTable(DealerRoster.Mammon);
            Assert.AreEqual(0, _game.RedrawsLeft, "Back at Mammon's: still spent.");
            Assert.AreEqual(0, _view.Relics[0].Uses);
        }

        [TestCase(SinnerAbility.Ward)]
        [TestCase(SinnerAbility.Protect)]
        public void HoppingTables_DoesNotRefillTheWardOrTheCrown(SinnerAbility ability)
        {
            Table(null);
            _presenter.StartNewRun(DealerRoster.Mammon, ability == SinnerAbility.Ward ? SinnerRoster.Warlock : SinnerRoster.King);
            Assert.IsTrue(_game.Sinner.TrySpend(ability), "Used at Mammon's.");

            _presenter.SwitchTable(DealerRoster.Belial);
            Assert.AreEqual(1, _game.Sinner.Charges, "Belial's table, never sat at: full.");
            _presenter.SwitchTable(DealerRoster.Lilith);
            Assert.AreEqual(1, _game.Sinner.Charges, "Lilith's: full too.");
            _presenter.SwitchTable(DealerRoster.Mammon);
            Assert.AreEqual(0, _game.Sinner.Charges, "Back at Mammon's: still spent.");
        }

        [Test]
        public void ThePerDemonCharges_AreSaved_AndAnOlderSaveKeepsItsOneNumber()
        {
            var archive = new RunArchive(new MemoryStore());
            Table(null, archive);
            _presenter.StartNewRun(DealerRoster.Mammon, SinnerRoster.Warlock);
            _game.Effects.AddRelic(RelicIds.BoneDie);
            Assert.IsTrue(_game.Sinner.TrySpend(SinnerAbility.Ward));
            Assert.IsTrue(_game.Effects.SpendRedraw());
            _presenter.SwitchTable(DealerRoster.Belial);   // saves the run, sitting at Belial's

            RunSnapshot saved = archive.LoadRun();
            Assert.AreEqual("mammon:0", saved.ClassChargeTables);
            Assert.AreEqual("mammon:0", saved.Events.RelicRedrawTables);
            StringAssert.Contains("class.charges.tables=mammon:0", saved.Encode());
            StringAssert.Contains("relics.redraws.tables=mammon:0", saved.Encode());
            _presenter.Dispose();

            Table(null, archive);
            _presenter.Resume(DealerRoster.Belial, saved);
            Assert.AreEqual(1, _game.Sinner.Charges);
            Assert.AreEqual(1, _game.RedrawsLeft);
            _presenter.SwitchTable(DealerRoster.Mammon);
            Assert.AreEqual(0, _game.Sinner.Charges, "Closing and coming back refills nothing either.");
            Assert.AreEqual(0, _game.RedrawsLeft);

            // An older save: one number for the table it was saved at, no list — that table keeps it, the others are full.
            var older = new Sinner(new Warlock(), charges: 0);
            older.SitAt("lilith");
            Assert.AreEqual(0, older.Charges);
            older.SitAt("belial");
            Assert.AreEqual(1, older.Charges);
            older.SitAt("lilith");
            Assert.AreEqual(0, older.Charges);
            var oldEffects = new RunEffects();
            oldEffects.Restore(null, 0, 0, 0, new[] { RelicIds.BoneDie }, redrawsLeft: 0);
            oldEffects.SitAt("lilith");
            Assert.AreEqual(0, oldEffects.RedrawsLeft);
            oldEffects.SitAt("mammon");
            Assert.AreEqual(1, oldEffects.RedrawsLeft);
        }

        [Test]
        public void TableCharges_BrokenSaveEntriesAreSkipped()
        {
            var charges = new TableCharges(() => 1);
            charges.Restore("mammon:0,,belial:x,:3,lilith:-1,lucifer:7");
            charges.SitAt("mammon");
            Assert.AreEqual(0, charges.Left);
            charges.SitAt("belial");
            Assert.AreEqual(1, charges.Left);
            charges.SitAt("lucifer");
            Assert.AreEqual(1, charges.Left, "Never above full.");
            Assert.AreEqual("mammon:0", charges.Encode());
        }

        [Test]
        public void TheBoneDie_AtTheTable_OnceATable_ANewSeatFillsIt_AndASavedRunKeepsItSpent()
        {
            var archive = new RunArchive(new MemoryStore());
            Table(null, archive);
            _presenter.StartNewRun(DealerRoster.Mammon);
            _game.Effects.AddRelic(RelicIds.BoneDie);
            _view.PressAction();
            _view.PressRelic(RelicIds.BoneDie);
            _view.PlayerView.Click(0);
            Assert.AreEqual(0, _view.Relics[0].Uses);

            PlayAHand();
            _view.PressAction();   // the next hand
            if (_game.Phase == GamePhase.Betting) _view.PressAction();   // and its deal
            Assert.AreEqual(GamePhase.PlayerReveal, _game.Phase);
            _view.PressRelic(RelicIds.BoneDie);
            StringAssert.Contains("once at each demon's table", _view.Message, "No new roll at a new hand.");
            Assert.AreEqual(0, _view.Relics[0].Uses);

            RunSnapshot saved = archive.LoadRun();
            Assert.AreEqual(0, saved.Events.RelicRedraws, "The spent roll is saved.");
            _presenter.Dispose();
            Table(null, archive);
            _presenter.Resume(DealerRoster.Mammon, saved);
            Assert.AreEqual(0, _game.RedrawsLeft, "Closing the game does not refill it.");

            _presenter.SwitchTable(DealerRoster.Belial);
            Assert.AreEqual(1, _game.RedrawsLeft, "A new table fills it.");
            Assert.AreEqual(1, _view.Relics[0].Uses);
        }

        /// <summary>Plays the hand on the table to its end (stands pat, passes, folds to a re-raise).</summary>
        private void PlayAHand()
        {
            for (int guard = 0; guard < 20 && _game.Phase != GamePhase.RoundOver && !_game.IsGameOver; guard++)
            {
                if (_game.Phase == GamePhase.Drawing) _view.PressAction();
                else if (_game.Phase == GamePhase.HouseReRaise) _view.PressBet(BetAction.Fold);
                else _view.PressBet(BetAction.Pass);
            }
        }

        [Test]
        public void WithTheSoulOnTheTable_TheRelicsSpeakNoNumbers()
        {
            Table(new EventSession(new IHellEvent[] { new RelicEvent(EventIds.CursedChest) }, new FirstChoice(), 100, 1));
            _presenter.StartNewRun(DealerRoster.Mammon);
            _game.TakeOver(2200, 3);
            _presenter.SwitchTable(DealerRoster.Mammon);   // the table looks again: between hands, soul on the table
            Assert.IsNotNull(_view.Event);
            Assert.IsFalse(Regex.IsMatch(_view.Event.Text, @"\d"), _view.Event.Text);

            _view.PressEventOption(0);

            Assert.AreEqual(1, _view.Relics.Count);
            Assert.IsFalse(Regex.IsMatch(_view.Message, @"\d"), _view.Message);
            foreach (IRelic relic in RelicRoster.All)
            {
                string words = UiText.RelicGift(relic.Id) + UiText.RelicCurse(relic.Id);
                Assert.IsFalse(Regex.IsMatch(words, @"\d"), words);
            }
        }

        [Test]
        public void ATakenRelic_IsTold_AndSitsBesideThePortrait()
        {
            Table(new EventSession(new IHellEvent[] { new RelicEvent(EventIds.CursedChest) }, new FirstChoice(), 100, 1));
            _presenter.StartNewRun(DealerRoster.Mammon);
            Assert.AreEqual("A CURSED CHEST", _view.Event.OwnerName);
            Assert.IsEmpty(_view.Relics);

            _view.PressEventOption(0);

            Assert.AreEqual(1, _view.Relics.Count);
            Assert.AreEqual(RelicIds.BoneDie, _view.Relics[0].Id);
            Assert.AreEqual("BONE DIE", _view.Relics[0].Name);
            Assert.AreEqual(1, _view.Relics[0].Uses, "One roll a table.");
            StringAssert.Contains("BONE DIE", _view.Message);
            StringAssert.Contains("two units", _view.Message, "The curse is told too.");
        }

        [Test]
        public void TheBoneDie_AtTheTable_PicksACard_AndRedealsIt()
        {
            Table(null);
            _presenter.StartNewRun(DealerRoster.Mammon);
            _game.Effects.AddRelic(RelicIds.BoneDie);

            _view.PressRelic(RelicIds.BoneDie);
            StringAssert.Contains("once at each demon's table", _view.Message, "Not before the deal.");

            _view.PressAction();
            _view.PressRelic(RelicIds.BoneDie);
            StringAssert.Contains("Pick the card", _view.Message);
            _view.PlayerView.Click(0);

            Assert.AreEqual(TestCards.Card("3S"), _game.PlayerHand[0]);
            StringAssert.Contains("Bone Die rolls", _view.Message);
            Assert.AreEqual(0, _view.Relics[0].Uses);
            Assert.AreEqual(GamePhase.PlayerReveal, _game.Phase, "The hand goes on.");
        }

        [Test]
        public void TheRelics_AreSaved_AndComeBackWithTheRun()
        {
            var archive = new RunArchive(new MemoryStore());
            Table(new EventSession(new IHellEvent[] { new RelicEvent(EventIds.GraveRobber) }, new FirstChoice(), 100, 1), archive);
            _presenter.StartNewRun(DealerRoster.Mammon);
            _view.PressEventOption(0);
            RunSnapshot saved = archive.LoadRun();
            _presenter.Dispose();

            Table(null, archive);
            _presenter.Resume(DealerRoster.Mammon, saved);

            CollectionAssert.AreEqual(new[] { RelicIds.BoneDie }, _game.Effects.Relics);
            Assert.AreEqual(1, _view.Relics.Count);
        }
    }
}
