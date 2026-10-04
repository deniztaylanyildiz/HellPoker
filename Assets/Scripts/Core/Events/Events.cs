using System;
using System.Collections.Generic;
using HellPoker.Core.Dealers;
using HellPoker.Core.Randomness;

namespace HellPoker.Core.Events
{
    /// <summary>Ids of the events: the save and the presentation's words.</summary>
    public static class EventIds
    {
        public const string Charon = "charon";
        public const string SoulBroker = "soul_broker";
        public const string LostSoul = "lost_soul";
        public const string DevilsLedger = "devils_ledger";
        public const string BurningBridge = "burning_bridge";
        public const string GraveRobber = "grave_robber";
        public const string CursedChest = "cursed_chest";
    }

    /// <summary>A betting unit at the table as it stands (what the next hand would be measured in).</summary>
    internal static class EventMath
    {
        public static int Unit(IEventTable table) =>
            Math.Max(1, table.Rules.Stakes.UnitFor(table.IsSoulAtStake ? table.SoulWorth : table.Years));
    }

    /// <summary>
    /// The Ferryman: "the next hand, half the ante — but a win forgives only half." Lighter stakes for a hand, both ways.
    /// </summary>
    public sealed class CharonEvent : IHellEvent
    {
        private readonly int _percent;

        public CharonEvent(int percent = 50)
        {
            if (percent <= 0 || percent > 100) throw new ArgumentOutOfRangeException(nameof(percent));
            _percent = percent;
        }

        public string Id => EventIds.Charon;
        public string OwnerId(string dealerId) => EventIds.Charon;
        public IReadOnlyList<string> Options => EventOptions.AcceptOrPass;

        public bool CanAppear(IEventTable table, string dealerId) => true;

        public void Apply(string option, IEventTable table, string dealerId, IRandomSource random)
        {
            if (option != EventOptions.Accept) return;
            table.Effects.NextHand = new HandModifier(antePercent: _percent, winPercent: _percent);
        }

        /// <summary>Even: less risk, less to gain.</summary>
        public int ExpectedYears(IEventTable table, string dealerId) => 0;
    }

    /// <summary>
    /// The Soul Broker (only with the soul on the table): "give me a quarter of your soul and I strike 300 years." The soul is
    /// smaller for the rest of the run (damnation comes that much sooner), the sentence drops at once. Not offered when what
    /// is left of the soul could not pay.
    /// </summary>
    public sealed class SoulBrokerEvent : IHellEvent
    {
        private readonly int _sharePercent;
        private readonly int _years;

        public SoulBrokerEvent(int sharePercent = 25, int years = 300)
        {
            if (sharePercent <= 0 || sharePercent >= 100) throw new ArgumentOutOfRangeException(nameof(sharePercent));
            if (years <= 0) throw new ArgumentOutOfRangeException(nameof(years));
            _sharePercent = sharePercent;
            _years = years;
        }

        public string Id => EventIds.SoulBroker;
        public string OwnerId(string dealerId) => EventIds.SoulBroker;
        public IReadOnlyList<string> Options => EventOptions.AcceptOrPass;

        private int Share(IEventTable table) => table.SoulWorth * _sharePercent / 100;

        public bool CanAppear(IEventTable table, string dealerId) => table.IsSoulAtStake && table.SoulRemaining > Share(table);

        public void Apply(string option, IEventTable table, string dealerId, IRandomSource random)
        {
            if (option != EventOptions.Accept) return;
            table.Effects.SellSoul(Share(table));
            table.ForgiveYears(_years);
        }

        public int ExpectedYears(IEventTable table, string dealerId) => _years - Share(table);
    }

    /// <summary>
    /// A lost soul sits down: "play my hand." The next hand the player's cards are a ghost's made hand (two pair to three of a
    /// kind) — but a lost showdown costs triple (tuned with the balance simulation: at double it added ~4 points of absolution).
    /// </summary>
    public sealed class LostSoulEvent : IHellEvent
    {
        private readonly int _lossPercent;

        public LostSoulEvent(int lossPercent = 300)
        {
            if (lossPercent < 100) throw new ArgumentOutOfRangeException(nameof(lossPercent));
            _lossPercent = lossPercent;
        }

        public string Id => EventIds.LostSoul;
        public string OwnerId(string dealerId) => EventIds.LostSoul;
        public IReadOnlyList<string> Options => EventOptions.AcceptOrPass;

        public bool CanAppear(IEventTable table, string dealerId) => true;

        public void Apply(string option, IEventTable table, string dealerId, IRandomSource random)
        {
            if (option != EventOptions.Accept) return;
            table.Effects.NextHand = new HandModifier(lossPercent: _lossPercent, ghostSeed: random.Next(int.MaxValue));
        }

        /// <summary>A made hand wins about three times in four (two units won) against a loss at the multiplier.</summary>
        public int ExpectedYears(IEventTable table, string dealerId) => EventMath.Unit(table) * (150 - _lossPercent / 2) / 100;
    }

    /// <summary>
    /// The demon's own ledger, a different offer at each table:
    /// Mammon — "defer your debt": 200 years struck now, 300 added five hands later.
    /// Belial — "a show": the next hand the House shows no cards, but a win pays double.
    /// Lilith — "a night's bargain": the malice gauge empties, for 100 years.
    /// </summary>
    public sealed class DevilsLedgerEvent : IHellEvent
    {
        private readonly int _deferNow, _deferLater, _deferHands, _showWinPercent, _nightYears;

        public DevilsLedgerEvent(int deferNow = 200, int deferLater = 300, int deferHands = 5, int showWinPercent = 200, int nightYears = 100)
        {
            _deferNow = deferNow;
            _deferLater = deferLater;
            _deferHands = deferHands;
            _showWinPercent = showWinPercent;
            _nightYears = nightYears;
        }

        public string Id => EventIds.DevilsLedger;
        public string OwnerId(string dealerId) => dealerId;
        public IReadOnlyList<string> Options => EventOptions.AcceptOrPass;

        public bool CanAppear(IEventTable table, string dealerId)
        {
            switch (dealerId)
            {
                case DealerRoster.MammonId: return table.Effects.DeferredYears == 0 && table.Years > _deferNow + 1;
                case DealerRoster.BelialId: return true;
                case DealerRoster.LilithId:
                    return table.Malice > 0 && (!table.IsSoulAtStake || table.SoulRemaining > _nightYears);
                default: return false;
            }
        }

        public void Apply(string option, IEventTable table, string dealerId, IRandomSource random)
        {
            if (option != EventOptions.Accept) return;
            switch (dealerId)
            {
                case DealerRoster.MammonId:
                    table.ForgiveYears(_deferNow);
                    table.Effects.Defer(_deferLater, _deferHands);
                    break;
                case DealerRoster.BelialId:
                    table.Effects.NextHand = new HandModifier(houseCardsShown: 0, winPercent: _showWinPercent);
                    break;
                case DealerRoster.LilithId:
                    table.EmptyMalice();
                    table.AddYears(_nightYears);
                    break;
            }
        }

        public int ExpectedYears(IEventTable table, string dealerId)
        {
            switch (dealerId)
            {
                case DealerRoster.MammonId: return _deferNow - _deferLater;
                case DealerRoster.BelialId: return EventMath.Unit(table) / 3;
                case DealerRoster.LilithId: return table.Malice * 25 - _nightYears;
                default: return 0;
            }
        }
    }

    /// <summary>
    /// The burning bridge (deep down, at 2500 years or more): "put everything on one hand." The next hand has no table cap and
    /// an ante of three units; a win brings the sentence down to 1000 (the soul goes back), a loss is an ordinary loss.
    /// </summary>
    public sealed class BurningBridgeEvent : IHellEvent
    {
        private readonly int _fromYears, _anteUnits, _winSetsYears;

        public BurningBridgeEvent(int fromYears = 2500, int anteUnits = 3, int winSetsYears = 1000)
        {
            _fromYears = fromYears;
            _anteUnits = anteUnits;
            _winSetsYears = winSetsYears;
        }

        public string Id => EventIds.BurningBridge;
        public string OwnerId(string dealerId) => EventIds.BurningBridge;
        public IReadOnlyList<string> Options => EventOptions.AcceptOrPass;

        public bool CanAppear(IEventTable table, string dealerId) => table.Years >= _fromYears;

        public void Apply(string option, IEventTable table, string dealerId, IRandomSource random)
        {
            if (option != EventOptions.Accept) return;
            table.Effects.NextHand = new HandModifier(anteUnits: _anteUnits, noCap: true, winSetsYears: _winSetsYears);
        }

        /// <summary>About even odds of a huge drop against an ordinary (if bigger) loss.</summary>
        public int ExpectedYears(IEventTable table, string dealerId) =>
            (table.Years - _winSetsYears) * 45 / 100 - EventMath.Unit(table) * _anteUnits * 2 * 55 / 100;
    }

    /// <summary>The events that may happen, in one list. A new event: one class and one line here.</summary>
    public static class EventDeck
    {
        public static IReadOnlyList<IHellEvent> Standard { get; } = new IHellEvent[]
        {
            new CharonEvent(),
            new SoulBrokerEvent(),
            new LostSoulEvent(),
            new DevilsLedgerEvent(),
            new BurningBridgeEvent(),
            new RelicEvent(EventIds.GraveRobber),
            new RelicEvent(EventIds.CursedChest)
        };

        public static IHellEvent Find(string id)
        {
            foreach (IHellEvent e in Standard)
                if (e.Id == id) return e;
            return null;
        }
    }
}

namespace HellPoker.Core.Events
{
    /// <summary>
    /// A cursed relic offered (the grave robber, the cursed chest): take it, and a relic not yet carried joins the run, chosen
    /// by the dice. Not offered when the run carries all it may, or every relic already.
    /// </summary>
    public sealed class RelicEvent : IHellEvent
    {
        public RelicEvent(string id) => Id = id ?? throw new System.ArgumentNullException(nameof(id));

        public string Id { get; }
        public string OwnerId(string dealerId) => Id;
        public System.Collections.Generic.IReadOnlyList<string> Options => EventOptions.AcceptOrPass;

        /// <summary>The relic the last accepted offer gave (for the table's words); null before.</summary>
        public string LastGiven { get; private set; }

        private static string[] Open(IEventTable table) =>
            System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(System.Linq.Enumerable.Select(Relics.RelicRoster.All, r => r.Id),
                id => !System.Linq.Enumerable.Contains(table.Effects.Relics, id)));

        public bool CanAppear(IEventTable table, string dealerId) =>
            table.Effects.Relics.Count < Relics.RelicRoster.MaxCarried && Open(table).Length > 0;

        public void Apply(string option, IEventTable table, string dealerId, Randomness.IRandomSource random)
        {
            LastGiven = null;
            if (option != EventOptions.Accept) return;
            string[] open = Open(table);
            if (open.Length == 0) return;
            string id = open[random.Next(open.Length)];
            if (table.Effects.AddRelic(id)) LastGiven = id;
        }

        /// <summary>The relics are worth about nothing on average: a gift and a curse each.</summary>
        public int ExpectedYears(IEventTable table, string dealerId)
        {
            string[] open = Open(table);
            return open.Length == 0 ? 0 : (int)System.Linq.Enumerable.Average(System.Linq.Enumerable.Select(open, id => Relics.RelicRoster.Find(id).ExpectedYears));
        }
    }
}
