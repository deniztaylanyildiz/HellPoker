using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Sinners;

namespace HellPoker.Core.Chapters
{
    /// <summary>What a class plays with in Phase 2 (in place of the demo's power): the Peasant's satchel, the Warlock's prophecy, the
    /// King's treasury; the Jester keeps his jokers.</summary>
    public enum KitKind
    {
        None,
        Satchel,
        Prophecy,
        Treasury,

        /// <summary>The Summoner's imps (the Eye, the Hand, the Whisper).</summary>
        Summon,

        /// <summary>The Thief's quick hand (a glance, a steal).</summary>
        Theft
    }

    /// <summary>
    /// The run's class mechanics in Phase 2 — the parts that last from table to table and chapter to chapter (and go into the save): the
    /// Peasant's satchel of thrown cards, the King's taxes, how far the Warlock sees into the deck; and what Lucifer's mirror keeps of
    /// them at his table. What happens within a hand lives in the table's hooks (<see cref="ChapterHooks"/>). The charge gauge is the
    /// sinner's (<see cref="Sinner"/>): a full gauge pays for each class's charged power.
    /// </summary>
    public sealed class ClassKit
    {
        /// <summary>The satchel holds this many cards; a new one pushes the oldest out.</summary>
        /// <remarks>Demo 2.2: 3 → 2 (the Peasant freed 49.2% → 41.8%, 500 whole runs; the re-raise risk barely mattered, a harvest a table neither).</remarks>
        public static int SatchelSize = 2;   // a tuning knob

        /// <summary>A hand the Peasant draws from his satchel the demon sees it: its re-raise comes this much (percent) more often.</summary>
        public static int SatchelReRaisePercent = 30;   // a tuning knob (the simulation's)

        /// <summary>The King's treasury holds at most this many taxes; the prices of its three acts.</summary>
        public const int TaxMax = 10, RevealPrice = 1, FreeRaisePrice = 2, SeizePrice = 4;

        /// <summary>The Oracle's K power (BEND FATE) moves this many seen cards in a hand, each to the top or to the bottom.</summary>
        public static int RearrangesPerUse = 1;

        /// <summary>The Oracle's FORESIGHT: the cards of the deck's top he sees after the deal (Demo 2.2: two in every chapter).</summary>
        public static int ForesightCards = 2;

        /// <summary>The King's K power: a seizure for this share (percent) of its price in taxes (2.1.1: once every hand, so not for nothing).</summary>
        public static int PowerSeizePercent = 50;

        private readonly List<Card> _satchel = new List<Card>();

        public Sinner Sinner { get; }
        public KitKind Kind { get; }

        /// <summary>The chapter being played (1-3) or, at Lucifer's table, past the third.</summary>
        public int Chapter { get; private set; } = 1;
        public bool AtLucifer { get; private set; }

        public ClassKit(Sinner sinner)
        {
            Sinner = sinner ?? throw new ArgumentNullException(nameof(sinner));
            Kind = KindOf(sinner.Class);
        }

        /// <summary>The worn relics' effects (the class relics bend the kit: Demo 2.1); null: none.</summary>
        public Func<HellPoker.Core.Relics.RelicEffects> Relics { get; set; }

        /// <summary>A mark of the worn relics (<see cref="Relics.RelicExtras"/>).</summary>
        public int Mark(string key) => Relics?.Invoke()?.Extra(key) ?? 0;

        public static KitKind KindOf(SinnerClass sinnerClass)
        {
            switch (sinnerClass?.Id)
            {
                case Peasant.ClassId: return KitKind.Satchel;
                case Warlock.ClassId: return KitKind.Prophecy;
                case King.ClassId: return KitKind.Treasury;
                case Summoner.ClassId: return KitKind.Summon;
                case Thief.ClassId: return KitKind.Theft;
                default: return KitKind.None;
            }
        }

        public void EnterChapter(int chapter)
        {
            Chapter = Math.Max(1, chapter);
            AtLucifer = false;
        }

        public void EnterLucifer() => AtLucifer = true;

        // ------------------------------------------------------------------ the class's upgrades (once each a run)

        private readonly List<string> _upgrades = new List<string>();

        /// <summary>The class's upgrades the run has, in the order won.</summary>
        public IReadOnlyList<string> Upgrades => _upgrades;

        public bool Has(string upgradeId) => _upgrades.Contains(upgradeId);

        /// <summary>The class's upgrades the run does not have yet.</summary>
        public IReadOnlyList<string> UpgradesLeft => ClassUpgrades.For(Sinner.Id).Where(u => !_upgrades.Contains(u)).ToArray();

        /// <summary>An upgrade of the class's joins the run; false for another class's, an unknown one, or one it has.</summary>
        public bool AddUpgrade(string upgradeId)
        {
            if (!ClassUpgrades.For(Sinner.Id).Contains(upgradeId) || _upgrades.Contains(upgradeId)) return false;
            _upgrades.Add(upgradeId);
            return true;
        }

        /// <summary>The satchel holds this many (the Wide Satchel: one more).</summary>
        public int SatchelCapacity => SatchelSize + (Has(UpgradeIds.WideSatchel) ? 1 : 0) + Mark(HellPoker.Core.Relics.RelicExtras.SatchelRoom);

        /// <summary>Thrown cards that may go into the satchel after a draw (Abundance: two).</summary>
        public int KeepsPerHand => Has(UpgradeIds.Abundance) ? 2 : 1;

        /// <summary>How much more often the demon re-raises a hand drawn from the satchel (the Quiet Satchel: half).</summary>
        public int SatchelRisk => (Has(UpgradeIds.QuietSatchel) ? SatchelReRaisePercent / 2 : SatchelReRaisePercent) + Mark(HellPoker.Core.Relics.RelicExtras.SatchelRisk);

        /// <summary>Seen cards BEND FATE moves in a hand (the Fate Weaver: two).</summary>
        public int RearrangesPerCharge => RearrangesPerUse + (Has(UpgradeIds.Thrice) ? 1 : 0);

        /// <summary>The Prophecy Seal: coins for every swindler's offer the Oracle walks away from.</summary>
        public const int SealCoins = 10;

        /// <summary>How often (percent) the demon learns one of the Oracle's cards in a hand (the Mirror Curse; the Blind Mirror: half the hands).</summary>
        public int DemonKnowsPercent => ChapterTuning.DemonKnowsPercent * (Has(UpgradeIds.BlindMirror) ? 50 : 100) / 100;

        /// <summary>The Thief is caught this often (percent; Light Fingers: ten).</summary>
        public static int CaughtPercent = 25;

        public int ThiefCaughtPercent => Has(UpgradeIds.LightFingers) ? 10 : CaughtPercent;

        /// <summary>The demon's face-down cards the Thief glances at from the deal (the Night Eye: two).</summary>
        public int GlanceCards => Kind != KitKind.Theft ? 0 : Has(UpgradeIds.NightEye) ? 2 : 1;

        /// <summary>The treasury holds this many taxes (the Overflowing Treasury: fifteen).</summary>
        public int TaxCapacity => (Has(UpgradeIds.OverflowingTreasury) ? 15 : TaxMax) + Mark(HellPoker.Core.Relics.RelicExtras.TaxCap);

        /// <summary>The taxes an act of the treasury costs (the Cheap Seizure: 3; the Royal Decree: a free raise for 1).</summary>
        public int PriceOf(TaxAct act) =>
            act == TaxAct.Reveal ? RevealPrice
            : act == TaxAct.FreeRaise ? (Has(UpgradeIds.RoyalDecree) ? FreeRaisePrice - 1 : FreeRaisePrice)
            : (Has(UpgradeIds.CheapSeizure) ? SeizePrice - 1 : SeizePrice) + Mark(HellPoker.Core.Relics.RelicExtras.SeizePrice);

        /// <summary>The Seed: a chapter begins with a ten or better in the satchel (a card it does not hold yet).</summary>
        public Card? Sow(Randomness.IRandomSource random)
        {
            if (Kind != KitKind.Satchel || !Has(UpgradeIds.Seed) || random == null) return null;
            var cards = Deck.CreateStandardCards().Where(c => c.Rank >= Rank.Ten && !_satchel.Contains(c)).ToList();
            if (cards.Count == 0) return null;
            Card card = cards[random.Next(cards.Count)];
            if (_satchel.Count >= SatchelCapacity) _satchel.RemoveAt(0);
            _satchel.Add(card);
            return card;
        }

        // ------------------------------------------------------------------ the Peasant's satchel

        /// <summary>The cards in the satchel, the oldest first. They are out of the deck while they lie here.</summary>
        public IReadOnlyList<Card> Satchel => _satchel;

        /// <summary>A thrown card goes into the satchel; full, the oldest falls out (it is gone until a shuffle). False for a joker, a card
        /// already in it, or a class without a satchel.</summary>
        public bool Keep(Card card)
        {
            if (Kind != KitKind.Satchel || card.IsJoker || _satchel.Contains(card)) return false;
            if (_satchel.Count >= SatchelCapacity) _satchel.RemoveAt(0);
            _satchel.Add(card);
            Kept++;
            return true;
        }

        /// <summary>A card taken out of the satchel (into a hand).</summary>
        public bool TakeOut(Card card) => _satchel.Remove(card);

        // ------------------------------------------------------------------ the King's treasury

        /// <summary>The King's taxes, 0 to <see cref="TaxCapacity"/>.</summary>
        public int Taxes { get; private set; }

        public void AddTax(int count = 1)
        {
            if (Kind == KitKind.Treasury) Taxes = Math.Min(TaxCapacity, Taxes + Math.Max(0, count));
        }

        public bool SpendTaxes(int count)
        {
            if (count < 0 || Taxes < count) return false;
            Taxes -= count;
            return true;
        }

        /// <summary>A demon's hand lost: half the taxes go (rounded down; the Crown Guard: a quarter).</summary>
        public void HalveTaxes()
        {
            int before = Taxes;
            Taxes -= Has(UpgradeIds.CrownGuard) ? Taxes / 4 : Taxes - Taxes / 2;
            TaxesLost += before - Taxes;
        }

        // ------------------------------------------------------------------ the Warlock's prophecy

        /// <summary>How many cards of the deck's top the Oracle sees (FORESIGHT): <see cref="ForesightCards"/> in every chapter, one more
        /// with the Third Eye (and the Seer's Spectacles).</summary>
        public int ProphecyCards => Kind != KitKind.Prophecy ? 0
            : ForesightCards + (Has(UpgradeIds.DeepProphecy) ? 1 : 0) + Mark(HellPoker.Core.Relics.RelicExtras.ProphecyCards);

        /// <summary>The seen cards may go to the bottom (not with the Seer's Spectacles worn: they may still go to the top).</summary>
        public bool CanSendToBottom => Mark(HellPoker.Core.Relics.RelicExtras.ProphecyNoBottom) == 0;

        // ------------------------------------------------------------------ Lucifer's mirror (his own copy of the class's mechanic)

        /// <summary>Lucifer's one-card satchel against the Peasant; null while it is empty.</summary>
        public Card? LuciferBag { get; set; }

        /// <summary>Lucifer's taxes against the King.</summary>
        public int LuciferTaxes { get; set; }

        // ------------------------------------------------------------------ what the run did with it (the log, the simulation)

        public int Kept { get; private set; }
        public int TakenFromSatchel { get; set; }
        public int Harvests { get; set; }
        public int Rearranges { get; set; }

        /// <summary>Of the Oracle's bends, those that brought a card to the top.</summary>
        public int PulledUp { get; set; }

        /// <summary>Coins the Prophecy Seal paid (swindlers walked away from).</summary>
        public int SealCoinsEarned { get; set; }
        public int Reveals { get; set; }
        public int FreeRaises { get; set; }
        public int Seizures { get; set; }
        public int TaxesEarned { get; set; }
        public int TaxesLost { get; private set; }
        public int PalaceGuards { get; set; }
        public int Summons { get; set; }
        public int WildImps { get; set; }

        /// <summary>The Thief's steals, and the times he was caught.</summary>
        public int Steals { get; set; }
        public int Caught { get; set; }

        /// <summary>How many hands the K power was used in (2.1.1: once a hand).</summary>
        public int PowerUses { get; set; }

        // ------------------------------------------------------------------ the save

        /// <summary>The kit as the save keeps it.</summary>
        public void Restore(IEnumerable<Card> satchel, int taxes, Card? luciferBag, int luciferTaxes, IEnumerable<string> upgrades = null)
        {
            _upgrades.Clear();
            foreach (string id in upgrades ?? Enumerable.Empty<string>()) AddUpgrade(id);
            _satchel.Clear();
            if (Kind == KitKind.Satchel && satchel != null)
                foreach (Card card in satchel.Where(c => !c.IsJoker).Distinct().Take(SatchelCapacity)) _satchel.Add(card);
            Taxes = Kind == KitKind.Treasury ? Math.Max(0, Math.Min(TaxCapacity, taxes)) : 0;
            LuciferBag = luciferBag.HasValue && !luciferBag.Value.IsJoker && !_satchel.Contains(luciferBag.Value) ? luciferBag : null;
            LuciferTaxes = Math.Max(0, luciferTaxes);
        }
    }
}
