using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;

namespace HellPoker.Core.Chapters
{
    /// <summary>What a class plays with in Phase 2 (in place of the demo's power): the Peasant's satchel, the Oracle's foresight, the
    /// King's treasury, the Summoner's imps, the Thief's quick hand; the Jester keeps his jokers.</summary>
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
    /// Peasant's satchel of thrown cards, the King's taxes, how far the Oracle sees into the deck; and what Lucifer's mirror keeps of
    /// them at his table. What happens within a hand lives in the table's hooks (<see cref="ChapterHooks"/>).
    /// Demo 2.3: no power is pressed for any more — each class's main power works on its own beside a passive, and grows by
    /// <see cref="Level"/>: the worn class relics (a step each, a forged one two) and the class's one "step" upgrade.
    /// </summary>
    public sealed class ClassKit
    {
        // ------------------------------------------------------------------ the levels of the main power (Demo 2.3; tuning knobs)

        /// <summary>The main power's highest level (the starting value is level 0).</summary>
        public const int MaxLevel = 3;

        /// <summary>The Peasant's satchel: this many cards at level 0, one more a level.</summary>
        public static int SatchelStart = 1;

        /// <summary>The Oracle's FORESIGHT: this many cards at level 0, one more a level.</summary>
        public static int ForesightStart = 1;

        /// <summary>The King's treasury: this many taxes at level 0, this many more a level.</summary>
        public static int TaxStart = 5, TaxStep = 5;

        /// <summary>The Thief is caught this often (percent) at level 0, this much less a level, never under the least.</summary>
        /// <remarks>Demo 2.3: 40 → 45 (the steal after the draw lifted him 30.0% → 43.2% at 40; 45: 37.2%, 50: 31.6%; 500 whole runs).</remarks>
        public static int CaughtStart = 45, CaughtStep = 10, CaughtLeast = 5;

        /// <summary>A hand the Peasant draws two satchel cards in or more the demon sees it: his re-raise comes this much (percent) more often
        /// (the first card is never seen: the Silent Field).</summary>
        public static int SatchelReRaisePercent = 30;   // a tuning knob (the simulation's)

        /// <summary>The prices of the treasury's three acts; Lucifer's mirror holds at most <see cref="LuciferTaxCap"/> taxes.</summary>
        public const int RevealPrice = 1, FreeRaisePrice = 2, SeizePrice = 4, LuciferTaxCap = 10;

        /// <summary>The King's passive, the ROYAL DISCOUNT: a hand's seizure costs this share (percent) of its price, rounded up (the Cheap
        /// Seizure: the smaller share).</summary>
        public const int RoyalDiscountPercent = 50, CheapSeizurePercent = 25;

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

        /// <summary>The worn relics' effects (the class relics lift the main power: Demo 2.3); null: none.</summary>
        public Func<RelicEffects> Relics { get; set; }

        /// <summary>A mark of the worn relics (<see cref="RelicExtras"/>).</summary>
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

        // ------------------------------------------------------------------ the main power's level (Demo 2.3)

        /// <summary>The class's upgrade that lifts the main power a level (the Wide Satchel, the Third Eye, the Overflowing Treasury, Light
        /// Fingers); null for a class without one.</summary>
        public static string StepUpgradeOf(string classId)
        {
            switch (classId)
            {
                case Peasant.ClassId: return UpgradeIds.WideSatchel;
                case Warlock.ClassId: return UpgradeIds.DeepProphecy;
                case King.ClassId: return UpgradeIds.OverflowingTreasury;
                case Thief.ClassId: return UpgradeIds.LightFingers;
                default: return null;
            }
        }

        /// <summary>The steps the worn class relics give (a forged one two).</summary>
        public int RelicSteps => Math.Max(0, Mark(RelicExtras.ClassStep));

        /// <summary>The main power's level, 0 to <see cref="MaxLevel"/>: the worn class relics' steps and the class's step upgrade.</summary>
        public int Level
        {
            get
            {
                string upgrade = StepUpgradeOf(Sinner.Id);
                return Math.Min(MaxLevel, RelicSteps + (upgrade != null && Has(upgrade) ? 1 : 0));
            }
        }

        /// <summary>The main power's number at a level (the hover's "now" and "at most"): the satchel's cards, FORESIGHT's cards, the
        /// treasury's taxes, the Thief's caught percent, the imps open (the Summoner's fourth level: half price), the Jester's shields.</summary>
        public int ValueAt(int level)
        {
            level = Math.Max(0, Math.Min(MaxLevel, level));
            switch (Kind)
            {
                case KitKind.Satchel: return SatchelStart + level;
                case KitKind.Prophecy: return ForesightStart + level;
                case KitKind.Treasury: return TaxStart + TaxStep * level;
                case KitKind.Theft: return Math.Max(CaughtLeast, CaughtStart - CaughtStep * level);
                case KitKind.Summon: return Math.Min(ImpCount, 1 + level);
                default: return IsJester ? level : 0;
            }
        }

        /// <summary>The main power's number now, and at its highest level.</summary>
        public int PowerValue => ValueAt(Level);
        public int PowerMax => ValueAt(MaxLevel);

        private bool IsJester => Sinner.Class.StartingJokers > 0;

        /// <summary>The class has a power that grows (every class but one without a kit).</summary>
        public bool Grows => Kind != KitKind.None || IsJester;

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

        // ------------------------------------------------------------------ the Peasant's satchel

        /// <summary>The satchel holds this many (its level: <see cref="SatchelStart"/> and a card a level).</summary>
        public int SatchelCapacity => Kind == KitKind.Satchel ? ValueAt(Level) : 0;

        /// <summary>Thrown cards that may go into the satchel after a draw (Abundance: two).</summary>
        public int KeepsPerHand => Has(UpgradeIds.Abundance) ? 2 : 1;

        /// <summary>How much more often the demon re-raises a hand he saw the satchel in (the Quiet Satchel: half).</summary>
        public int SatchelRisk => (Has(UpgradeIds.QuietSatchel) ? SatchelReRaisePercent / 2 : SatchelReRaisePercent) + Mark(RelicExtras.SatchelRisk);

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

        /// <summary>The cards in the satchel, the oldest first. They are out of the deck while they lie here.</summary>
        public IReadOnlyList<Card> Satchel => _satchel;

        /// <summary>A thrown card goes into the satchel; full, the oldest falls out (it is gone until a shuffle). False for a joker, a card
        /// already in it, or a class without a satchel.</summary>
        public bool Keep(Card card)
        {
            if (Kind != KitKind.Satchel || card.IsJoker || _satchel.Contains(card)) return false;
            while (_satchel.Count >= Math.Max(1, SatchelCapacity)) _satchel.RemoveAt(0);
            _satchel.Add(card);
            Kept++;
            return true;
        }

        /// <summary>A card taken out of the satchel (into a hand).</summary>
        public bool TakeOut(Card card) => _satchel.Remove(card);

        // ------------------------------------------------------------------ the King's treasury

        /// <summary>The treasury holds this many taxes (its level: <see cref="TaxStart"/> and <see cref="TaxStep"/> a level).</summary>
        public int TaxCapacity => Kind == KitKind.Treasury ? ValueAt(Level) : 0;

        /// <summary>A seizure's price before the royal discount (the King's Signet: one more).</summary>
        public int SeizeFullPrice => SeizePrice + Mark(RelicExtras.SeizePrice);

        /// <summary>The taxes an act of the treasury costs: the seizure under the ROYAL DISCOUNT (half, the Cheap Seizure a quarter, rounded
        /// up); the Royal Decree's free raise for 1.</summary>
        public int PriceOf(TaxAct act) =>
            act == TaxAct.Reveal ? RevealPrice
            : act == TaxAct.FreeRaise ? (Has(UpgradeIds.RoyalDecree) ? FreeRaisePrice - 1 : FreeRaisePrice)
            : Math.Max(1, (SeizeFullPrice * (Has(UpgradeIds.CheapSeizure) ? CheapSeizurePercent : RoyalDiscountPercent) + 99) / 100);

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

        // ------------------------------------------------------------------ the Oracle's foresight

        /// <summary>How many cards of the deck's top the Oracle sees (FORESIGHT): its level, <see cref="ForesightStart"/> and a card a level.</summary>
        public int ProphecyCards => Kind != KitKind.Prophecy ? 0 : ValueAt(Level);

        /// <summary>The Prophecy Seal: coins for every swindler's offer the Oracle walks away from.</summary>
        public const int SealCoins = 10;

        /// <summary>How often (percent) the demon learns one of the Oracle's cards in a hand (the Mirror Curse; the Blind Mirror: half the hands).</summary>
        public int DemonKnowsPercent => ChapterTuning.DemonKnowsPercent * (Has(UpgradeIds.BlindMirror) ? 50 : 100) / 100;

        // ------------------------------------------------------------------ the Summoner's imps

        /// <summary>The imps: the Eye, the Hand, the Whisper (<see cref="SummonAct"/>).</summary>
        public const int ImpCount = 3;

        /// <summary>The imps the Summoner may call: the Eye from the start, the Hand and the Whisper with his relics (levels 1 and 2).</summary>
        public int ImpsOpen => Kind == KitKind.Summon ? ValueAt(Level) : 0;

        /// <summary>At the imps' highest level every imp costs half (rounded up).</summary>
        public bool ImpsHalfPrice => Kind == KitKind.Summon && Level >= MaxLevel;

        /// <summary>The Summoner's passive, the LOYAL IMP: this many imps a hand come for nothing (Two Loyal Imps: two).</summary>
        public int FreeImpsPerHand => Kind != KitKind.Summon ? 0 : Has(UpgradeIds.LoyalPair) ? 2 : 1;

        // ------------------------------------------------------------------ the Thief's quick hand

        /// <summary>How often (percent) the Thief is caught: its level, from <see cref="CaughtStart"/> down.</summary>
        public int ThiefCaughtPercent => Kind == KitKind.Theft ? ValueAt(Level) : CaughtStart;

        /// <summary>The demon's face-down cards the Thief glances at from the deal (the Night Eye: two).</summary>
        public int GlanceCards => Kind != KitKind.Theft ? 0 : Has(UpgradeIds.NightEye) ? 2 : 1;

        // ------------------------------------------------------------------ the Jester's shields

        /// <summary>Hands lost to the Jester's own two jokers that his relics spare at every table (each one a push).</summary>
        public int BustShieldsPerTable => IsJester ? ValueAt(Level) : 0;

        // ------------------------------------------------------------------ Lucifer's mirror (his own copy of the class's mechanic)

        /// <summary>Lucifer's one-card satchel against the Peasant; null while it is empty.</summary>
        public Card? LuciferBag { get; set; }

        /// <summary>Lucifer's taxes against the King.</summary>
        public int LuciferTaxes { get; set; }

        // ------------------------------------------------------------------ what the run did with it (the log, the simulation)

        public int Kept { get; private set; }
        public int TakenFromSatchel { get; set; }

        /// <summary>The passives that worked over the run (the log, the simulation): the Peasant's unseen satchel cards, the King's
        /// discounted seizures, the Summoner's free imps.</summary>
        public int Passives { get; set; }

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

        /// <summary>The Thief's steals, and the times he was caught; of the steals, those made after the draw.</summary>
        public int Steals { get; set; }
        public int Caught { get; set; }
        public int StealsAfterDraw { get; set; }

        /// <summary>The Jester's lost hands his relics spared.</summary>
        public int Shields { get; set; }

        // ------------------------------------------------------------------ the save

        /// <summary>The kit as the save keeps it (a 2.2.1 save's satchel and taxes are cut to the starting level's room unless the worn
        /// relics give more — they are restored first).</summary>
        public void Restore(IEnumerable<Card> satchel, int taxes, Card? luciferBag, int luciferTaxes, IEnumerable<string> upgrades = null)
        {
            _upgrades.Clear();
            foreach (string id in upgrades ?? Enumerable.Empty<string>()) AddUpgrade(id);
            _satchel.Clear();
            if (Kind == KitKind.Satchel && satchel != null)
            {
                // The newest cards are kept when the satchel is smaller now.
                Card[] cards = satchel.Where(c => !c.IsJoker).Distinct().ToArray();
                foreach (Card card in cards.Skip(Math.Max(0, cards.Length - SatchelCapacity))) _satchel.Add(card);
            }
            Taxes = Kind == KitKind.Treasury ? Math.Max(0, Math.Min(TaxCapacity, taxes)) : 0;
            LuciferBag = luciferBag.HasValue && !luciferBag.Value.IsJoker && !_satchel.Contains(luciferBag.Value) ? luciferBag : null;
            LuciferTaxes = Math.Max(0, Math.Min(LuciferTaxCap, luciferTaxes));
        }
    }
}
