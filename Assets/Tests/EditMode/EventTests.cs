using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HellPoker.Core.Cheats;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Events;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Settings;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// Events between hands: when they may appear, what each does to the table and the next hand, the cooldown and
    /// once-a-run rules, none at Lucifer's table, no numbers with the soul on the table, the save, and an event left open.
    /// </summary>
    public class EventTests
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

        private static HellPokerGame Game(string player = Flush, string house = HouseTwos, int years = 1000, Dealer dealer = null,
            ICheat cheat = null, int maliceMax = 4)
        {
            dealer = dealer ?? DealerRoster.Mammon;
            var random = new FirstChoice();
            GameRules rules = dealer.ApplyTo(new GameRules(1000, 5000, luciferGateYears: 0));
            var game = new HellPokerGame(rules, TestDecks.Stacked($"{player} {house} {Blanks}"), HandEvaluator.CreateDefault(),
                new CardExchanger(new MaxDiscardPolicy(rules.MaxDiscards)), new HouseDrawStrategy(rules.MaxDiscards), dealer.Payouts, null,
                new CheatSession(cheat == null ? null : new OnlyCheat(cheat), maliceMax, random), random);
            if (years != 1000) game.TakeOver(years, 3);
            return game;
        }

        private static void PlayOut(HellPokerGame game)
        {
            game.PlaceBet();
            if (game.Phase == GamePhase.PlayerReveal) game.CheckToDraw();
            game.Draw(new int[0]);
            while (game.Phase == GamePhase.DrawReveal || game.Phase == GamePhase.HouseReveal) game.Bet(BetAction.Pass);
        }

        private static readonly IRandomSource Dice = new FirstChoice();

        // ------------------------------------------------------------------ the Ferryman

        [Test]
        public void Charon_HalvesTheNextAnte_AndTheWin()
        {
            HellPokerGame plain = Game();
            PlayOut(plain);
            int plainWin = -plain.LastRound.YearsChange;

            HellPokerGame game = Game();
            new CharonEvent().Apply(EventOptions.Accept, game, "mammon", Dice);
            PlayOut(game);

            Assert.AreEqual(50, game.Ante, "Half the ante.");
            Assert.Less(-game.LastRound.YearsChange, plainWin);
            Assert.IsTrue(game.Effects.NextHand.IsNone, "Only the next hand.");
        }

        [Test]
        public void Passing_ChangesNothing()
        {
            HellPokerGame game = Game();
            foreach (IHellEvent e in EventDeck.Standard)
                e.Apply(EventOptions.Pass, game, "mammon", Dice);

            Assert.AreEqual(1000, game.Years);
            Assert.IsTrue(game.Effects.NextHand.IsNone);
            Assert.AreEqual(0, game.Effects.DeferredYears);
            Assert.AreEqual(0, game.Effects.SoulSold);
        }

        // ------------------------------------------------------------------ the Soul Broker

        [Test]
        public void TheSoulBroker_ComesOnlyForASoulOnTheTable_ThatCanPay()
        {
            var broker = new SoulBrokerEvent();
            Assert.IsFalse(broker.CanAppear(Game(years: 1500), "mammon"), "No soul on the table.");
            Assert.IsTrue(broker.CanAppear(Game(years: 2200), "mammon"), "800 of the soul left: it can pay 250.");
            Assert.IsFalse(broker.CanAppear(Game(years: 2800), "mammon"), "200 left: not enough.");
        }

        [Test]
        public void TheSoulBroker_TakesAQuarterOfTheSoul_AndStrikes300Years()
        {
            HellPokerGame game = Game(years: 2200);
            int damnation = game.DamnationYears;

            new SoulBrokerEvent().Apply(EventOptions.Accept, game, "mammon", Dice);

            Assert.AreEqual(1900, game.Years);
            Assert.AreEqual(250, game.Effects.SoulSold);
            Assert.AreEqual(damnation - 250, game.DamnationYears, "Damnation comes sooner.");
            Assert.IsFalse(game.IsSoulAtStake, "Below the line: the soul goes back — a smaller one.");
        }

        // ------------------------------------------------------------------ the lost soul

        [Test]
        public void TheLostSoul_DealsAMadeHand_AndALossCostsTriple()
        {
            HellPokerGame plain = Game(Nothing, HouseFullHouse);
            HellPokerGame game = Game(Nothing, HouseFullHouse);
            new LostSoulEvent().Apply(EventOptions.Accept, game, "mammon", new SystemRandomSource(5));

            game.PlaceBet();
            HandCategory made = HandEvaluator.CreateDefault().Evaluate(game.PlayerHand).Category;
            Assert.That(made, Is.EqualTo(HandCategory.TwoPair).Or.EqualTo(HandCategory.ThreeOfAKind));
            Assert.AreEqual(5, game.PlayerHand.Distinct().Count());
            Assert.AreEqual(300, game.ThisHand.LossPercent, "A lost showdown costs triple.");
        }

        [Test]
        public void ALostShowdown_UnderTheGhost_CostsDouble()
        {
            int Loss(bool ghost)
            {
                HellPokerGame game = Game(Nothing, HouseFullHouse);
                if (ghost) game.Effects.NextHand = new HandModifier(lossPercent: 200);
                PlayOut(game);
                return game.LastRound.YearsChange;
            }

            Assert.AreEqual(Loss(false) * 2, Loss(true));
        }

        // ------------------------------------------------------------------ the demons' own ledger

        [Test]
        public void MammonsLedger_StrikesNow_AndComesDueFiveHandsLater()
        {
            HellPokerGame game = Game(Nothing, HouseFullHouse);
            new DevilsLedgerEvent().Apply(EventOptions.Accept, game, DealerRoster.MammonId, Dice);
            Assert.AreEqual(800, game.Years);
            Assert.IsFalse(new DevilsLedgerEvent().CanAppear(game, DealerRoster.MammonId), "One debt at a time.");

            for (int hand = 1; hand <= 5; hand++)
            {
                game.PlaceBet();
                game.Bet(BetAction.Fold);
                int before = game.Years;
                game.NextRound();
                if (hand < 5) Assert.AreEqual(0, game.DeferredPaid, $"Hand {hand}: not yet.");
                else
                {
                    Assert.AreEqual(300, game.DeferredPaid);
                    Assert.AreEqual(before + 300, game.Years);
                }
            }
        }

        [Test]
        public void BelialsShow_HidesTheHousesCards_AndDoublesAWin()
        {
            HellPokerGame plain = Game(dealer: DealerRoster.Belial, years: 1700);
            PlayOut(plain);

            HellPokerGame game = Game(dealer: DealerRoster.Belial, years: 1700);
            new DevilsLedgerEvent().Apply(EventOptions.Accept, game, DealerRoster.BelialId, Dice);
            game.PlaceBet();
            Assert.AreEqual(0, game.HouseCardsShown);
            game.CheckToDraw();
            game.Draw(new int[0]);
            while (game.Phase == GamePhase.DrawReveal || game.Phase == GamePhase.HouseReveal) game.Bet(BetAction.Pass);

            Assert.AreEqual(plain.LastRound.YearsChange * 2, game.LastRound.YearsChange);
        }

        [Test]
        public void LilithsBargain_EmptiesTheGauge_For100Years_AndNeedsMaliceToEmpty()
        {
            HellPokerGame game = Game(dealer: DealerRoster.Lilith, cheat: new ThornCheat());
            Assert.IsFalse(new DevilsLedgerEvent().CanAppear(game, DealerRoster.LilithId), "Nothing to empty.");
            game.RestoreMalice(3, false);

            new DevilsLedgerEvent().Apply(EventOptions.Accept, game, DealerRoster.LilithId, Dice);

            Assert.AreEqual(0, game.Malice);
            Assert.AreEqual(1100, game.Years);
        }

        [Test]
        public void TheLedger_IsTheDemonsOwn_NeverLucifers()
        {
            Assert.IsFalse(new DevilsLedgerEvent().CanAppear(Game(), DealerRoster.LuciferId));
            Assert.AreEqual(DealerRoster.BelialId, new DevilsLedgerEvent().OwnerId(DealerRoster.BelialId));
        }

        // ------------------------------------------------------------------ the burning bridge

        [Test]
        public void TheBurningBridge_OnlyDeepDown_ThenAWinBringsTheSentenceTo1000()
        {
            Assert.IsFalse(new BurningBridgeEvent().CanAppear(Game(years: 2400), "mammon"));
            HellPokerGame game = Game(years: 2600);
            Assert.IsTrue(new BurningBridgeEvent().CanAppear(game, "mammon"));

            new BurningBridgeEvent().Apply(EventOptions.Accept, game, "mammon", Dice);
            PlayOut(game);

            Assert.AreEqual(ShowdownOutcome.PlayerWins, game.LastRound.Showdown.Outcome);
            Assert.AreEqual(1000, game.Years);
            Assert.IsFalse(game.IsSoulAtStake, "The soul goes back.");
        }

        [Test]
        public void TheBurningBridge_ThreeUnits_NoCap()
        {
            HellPokerGame game = Game(years: 2600);
            new BurningBridgeEvent().Apply(EventOptions.Accept, game, "mammon", Dice);

            game.PlaceBet();

            Assert.AreEqual(3 * game.Unit, game.Ante);
            Assert.AreEqual(game.SoulRemaining, game.TableCap, "No table limit: everything left may go on it.");
        }

        // ------------------------------------------------------------------ when they happen

        [Test]
        public void TheSession_WaitsTheCooldown_ThenOffers_EachEventOnceARun()
        {
            var session = new EventSession(new IHellEvent[] { new CharonEvent(), new LostSoulEvent() }, Dice, chancePercent: 100, cooldownHands: 4);
            HellPokerGame game = Game();

            Assert.IsNull(session.Roll(game, "mammon", false));
            Assert.IsNull(session.Roll(game, "mammon", false));
            Assert.IsNull(session.Roll(game, "mammon", false));
            IHellEvent first = session.Roll(game, "mammon", false);
            Assert.IsNotNull(first, "The fourth hand.");
            Assert.IsNull(session.Roll(game, "mammon", false), "The cooldown again.");
            for (int i = 0; i < 2; i++) session.Roll(game, "mammon", false);
            IHellEvent second = session.Roll(game, "mammon", false);
            Assert.IsNotNull(second);
            Assert.AreNotEqual(first.Id, second.Id, "Each once a run.");
            for (int i = 0; i < 10; i++) Assert.IsNull(session.Roll(game, "mammon", false), "Nothing left to offer.");
        }

        [Test]
        public void NoEvent_AtLucifersTable_OrBelowTheChance()
        {
            var always = new EventSession(EventDeck.Standard, Dice, 100, 0);
            for (int i = 0; i < 10; i++) Assert.IsNull(always.Roll(Game(), "lucifer", finalTable: true));

            var never = new EventSession(EventDeck.Standard, new SystemRandomSource(1), 0, 0);
            for (int i = 0; i < 50; i++) Assert.IsNull(never.Roll(Game(), "mammon", false));
        }

        [Test]
        public void TheStandardRules_Are12Percent_FourHandsApart()
        {
            Assert.AreEqual(12, GameRules.Default.EventChancePercent);
            Assert.AreEqual(4, GameRules.Default.EventCooldownHands);
            Assert.AreEqual(7, EventDeck.Standard.Count);
            Assert.AreEqual(EventDeck.Standard.Count, EventDeck.Standard.Select(e => e.Id).Distinct().Count());
            Assert.IsTrue(EventDeck.Standard.All(e => e.Options.Count >= 2 && e.Options.Last() == EventOptions.Pass));
        }

        // ------------------------------------------------------------------ the save

        [Test]
        public void TheSave_KeepsTheEvents_AndAnOlderSaveHasNone()
        {
            var state = new RunEventState(new[] { "charon", "lost_soul" }, 2, new HandModifier(antePercent: 50, winPercent: 50), 300, 4, 250);
            var snapshot = new RunSnapshot("mammon", 900, 7, new RunStats(1000, "mammon"), events: state);

            Assert.IsTrue(RunSnapshot.TryDecode(snapshot.Encode(), out RunSnapshot back));

            CollectionAssert.AreEqual(new[] { "charon", "lost_soul" }, back.Events.Seen);
            Assert.AreEqual(2, back.Events.HandsSince);
            Assert.AreEqual(50, back.Events.Next.AntePercent);
            Assert.AreEqual(50, back.Events.Next.WinPercent);
            Assert.AreEqual(300, back.Events.DeferredYears);
            Assert.AreEqual(4, back.Events.DeferredHands);
            Assert.AreEqual(250, back.Events.SoulSold);

            var plain = new RunSnapshot("mammon", 900, 7, new RunStats(1000, "mammon"));
            Assert.IsTrue(RunSnapshot.TryDecode(plain.Encode(), out RunSnapshot none));
            Assert.IsEmpty(none.Events.Seen);
            Assert.IsTrue(none.Events.Next.IsNone);
        }

        [Test]
        public void AModifier_SurvivesTheRoundTrip()
        {
            var modifier = new HandModifier(anteUnits: 3, noCap: true, winSetsYears: 1000, houseCardsShown: 0, lossPercent: 200, ghostSeed: 42);
            HandModifier back = HandModifier.Decode(modifier.Encode());

            Assert.AreEqual(3, back.AnteUnits);
            Assert.IsTrue(back.NoCap);
            Assert.AreEqual(1000, back.WinSetsYears);
            Assert.AreEqual(0, back.HouseCardsShown);
            Assert.AreEqual(200, back.LossPercent);
            Assert.AreEqual(42, back.GhostSeed);
            Assert.IsTrue(HandModifier.Decode("").IsNone);
        }

        // ------------------------------------------------------------------ at the table

        private FakeTableView _view;
        private TablePresenter _presenter;
        private HellPokerGame _game;

        [TearDown]
        public void TearDown() => _presenter?.Dispose();

        private TablePresenter Table(EventSession events, RunArchive archive = null, string player = Nothing, string house = HouseFullHouse)
        {
            _view = new FakeTableView();
            return _presenter = new TablePresenter((d, sinner) =>
            {
                var random = new FirstChoice();
                return _game = new HellPokerGame(d.ApplyTo(new GameRules(1000, 5000, luciferGateYears: 0)),
                    TestDecks.Stacked($"{player} {house} {Blanks}"), HandEvaluator.CreateDefault(), new CardExchanger(new MaxDiscardPolicy()),
                    new HouseDrawStrategy(), d.Payouts, null, new CheatSession(null, 0, random), random, sinner);
            }, _view, null, archive, null, events);
        }

        private void PlayAHand()
        {
            _view.PressAction();
            _view.PressBet(BetAction.Fold);
            _view.PressAction();   // next hand: between hands, an event may come
        }

        [Test]
        public void AnEvent_WaitsForItsAnswer_TheTableTakesNoOtherInput()
        {
            Table(new EventSession(new IHellEvent[] { new CharonEvent() }, Dice, 100, 1)).StartNewRun(DealerRoster.Mammon);
            Assert.IsNotNull(_view.Event, "The very first between-hands, with no cooldown.");
            Assert.AreEqual("THE FERRYMAN", _view.Event.OwnerName);
            Assert.AreEqual("A FERRY ACROSS", _view.Event.Title);
            CollectionAssert.AreEqual(new[] { "ACCEPT", "PASS" }, _view.Event.Options);
            Assert.IsNull(_view.ActionLabel, "No deal while it waits.");

            _view.PressAction();
            Assert.AreEqual(GamePhase.Betting, _game.Phase, "Nothing dealt.");

            _view.PressEventOption(0);

            Assert.IsNull(_view.Event);
            Assert.IsNull(_presenter.PendingEvent);
            Assert.AreEqual(50, _game.Effects.NextHand.AntePercent);
            CollectionAssert.Contains(new[] { "A deal struck. I do love a signature.", "Accepted. The terms will find you." }, _view.DealerView.LastLine);
            Assert.AreEqual("DEAL", _view.ActionLabel);
        }

        [Test]
        public void Escape_LetsTheEventPass()
        {
            Table(new EventSession(new IHellEvent[] { new CharonEvent() }, Dice, 100, 1)).StartNewRun(DealerRoster.Mammon);

            Assert.IsTrue(_presenter.CloseOverlay());

            Assert.IsNull(_presenter.PendingEvent);
            Assert.IsTrue(_game.Effects.NextHand.IsNone);
            CollectionAssert.Contains(new[] { "Prudent. Unprofitable, but prudent.", "No? The offer goes back in the drawer." }, _view.DealerView.LastLine);
        }

        [Test]
        public void WithTheSoulOnTheTable_AnEventSpeaksNoNumbers()
        {
            Table(new EventSession(new IHellEvent[] { new SoulBrokerEvent() }, Dice, 100, 1)).StartNewRun(DealerRoster.Mammon);
            _game.TakeOver(2200, 3);
            _presenter.SwitchTable(DealerRoster.Mammon);   // the table looks again: between hands, soul on the table

            Assert.IsNotNull(_view.Event);
            Assert.IsFalse(Regex.IsMatch(_view.Event.Text, @"\d"), _view.Event.Text);
        }

        [Test]
        public void AnEventLeftOnScreen_CountsAsPassed_WhenTheGameComesBack()
        {
            var archive = new RunArchive(new MemoryStore());
            Table(new EventSession(new IHellEvent[] { new CharonEvent() }, Dice, 100, 1), archive).StartNewRun(DealerRoster.Mammon);
            Assert.IsNotNull(_view.Event);
            RunSnapshot saved = archive.LoadRun();
            CollectionAssert.Contains(saved.Events.Seen, EventIds.Charon, "Seen the moment it showed.");
            _presenter.Dispose();

            var events = new EventSession(new IHellEvent[] { new CharonEvent() }, Dice, 100, 1);
            Table(events, archive).Resume(DealerRoster.Mammon, saved);

            Assert.IsNull(_view.Event, "Not offered again.");
            Assert.IsNull(_presenter.PendingEvent);
            Assert.IsTrue(_game.Effects.NextHand.IsNone, "Passed: nothing changes.");
            CollectionAssert.Contains(events.Seen, EventIds.Charon);
        }

        [Test]
        public void TheRunsMarks_GoAlongToTheNextTable()
        {
            Table(new EventSession(new IHellEvent[] { new DevilsLedgerEvent() }, Dice, 100, 1)).StartNewRun(DealerRoster.Mammon);
            _view.PressEventOption(0);   // Mammon's ledger: 200 now, 300 later
            Assert.AreEqual(800, _game.Years);

            _presenter.SwitchTable(DealerRoster.Belial);

            Assert.AreEqual(300, _game.Effects.DeferredYears, "The debt follows the player.");
        }

        [Test]
        public void ADeferredDebt_ComingDue_IsTold()
        {
            Table(null).StartNewRun(DealerRoster.Mammon);
            _game.Effects.Defer(300, 1);

            PlayAHand();

            StringAssert.Contains("+300", _view.Message);
        }
    }
}
