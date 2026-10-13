using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HellPoker.Core.Events
{
    /// <summary>
    /// How the next hand differs, by an event the player accepted (later also a relic): the ante, what a win forgives, what a
    /// loss costs, the table cap, the House's open cards, a win that sets the sentence, a hand dealt by a ghost. Immutable;
    /// <see cref="None"/> changes nothing. Saved as "key:value" pairs joined by ";".
    /// </summary>
    public sealed class HandModifier
    {
        public static readonly HandModifier None = new HandModifier();

        /// <summary>The ante, in percent of the usual one (Charon: 50).</summary>
        public int AntePercent { get; }

        /// <summary>The ante in betting units instead of one (the burning bridge: 3); 0 for the usual.</summary>
        public int AnteUnits { get; }

        /// <summary>What a win forgives, in percent (Charon: 50, Belial's show: 200).</summary>
        public int WinPercent { get; }

        /// <summary>What a lost showdown adds, in percent (the lost soul: 200).</summary>
        public int LossPercent { get; }

        /// <summary>No table cap: everything left may go on the table (the burning bridge).</summary>
        public bool NoCap { get; }

        /// <summary>House cards turned before the last decision; -1 for the table's own number (Belial's show: 0).</summary>
        public int HouseCardsShown { get; }

        /// <summary>A win sets the sentence to this (the burning bridge: 1000; the soul goes back); -1 for none.</summary>
        public int WinSetsYears { get; }

        /// <summary>The lost soul plays this hand: the player's cards are replaced by a made hand (two pair to trips), drawn with this seed.</summary>
        public int? GhostSeed { get; }

        public HandModifier(int antePercent = 100, int anteUnits = 0, int winPercent = 100, int lossPercent = 100, bool noCap = false,
            int houseCardsShown = -1, int winSetsYears = -1, int? ghostSeed = null)
        {
            if (antePercent <= 0) throw new ArgumentOutOfRangeException(nameof(antePercent));
            if (anteUnits < 0) throw new ArgumentOutOfRangeException(nameof(anteUnits));
            if (winPercent < 0) throw new ArgumentOutOfRangeException(nameof(winPercent));
            if (lossPercent < 0) throw new ArgumentOutOfRangeException(nameof(lossPercent));
            AntePercent = antePercent;
            AnteUnits = anteUnits;
            WinPercent = winPercent;
            LossPercent = lossPercent;
            NoCap = noCap;
            HouseCardsShown = houseCardsShown;
            WinSetsYears = winSetsYears;
            GhostSeed = ghostSeed;
        }

        public bool IsNone => AntePercent == 100 && AnteUnits == 0 && WinPercent == 100 && LossPercent == 100 && !NoCap &&
                              HouseCardsShown < 0 && WinSetsYears < 0 && !GhostSeed.HasValue;

        public string Encode()
        {
            var parts = new List<string>();
            void Add(string key, int value, int usual) { if (value != usual) parts.Add(key + ":" + value.ToString(CultureInfo.InvariantCulture)); }
            Add("ante", AntePercent, 100);
            Add("units", AnteUnits, 0);
            Add("win", WinPercent, 100);
            Add("loss", LossPercent, 100);
            Add("nocap", NoCap ? 1 : 0, 0);
            Add("house", HouseCardsShown, -1);
            Add("sets", WinSetsYears, -1);
            if (GhostSeed.HasValue) parts.Add("ghost:" + GhostSeed.Value.ToString(CultureInfo.InvariantCulture));
            return string.Join(";", parts);
        }

        /// <exception cref="FormatException">A broken value.</exception>
        public static HandModifier Decode(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return None;
            var values = text.Split(';').Where(p => p.Length > 0).Select(p => p.Split(':'))
                .ToDictionary(p => p[0], p => p.Length == 2 ? int.Parse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture)
                    : throw new FormatException("Not a key:value pair."));
            int Get(string key, int usual) => values.TryGetValue(key, out int v) ? v : usual;
            return new HandModifier(Get("ante", 100), Get("units", 0), Get("win", 100), Get("loss", 100), Get("nocap", 0) != 0,
                Get("house", -1), Get("sets", -1), values.TryGetValue("ghost", out int g) ? g : (int?)null);
        }
    }

    /// <summary>
    /// What the run's events (and relics) leave behind, across every table: the next hand's modifier, a sentence deferred to
    /// later (Mammon's ledger), and the part of the soul that was sold. Every table's game shares the run's one.
    /// </summary>
    public sealed class RunEffects
    {
        /// <summary>How the next hand dealt differs; taken by the deal.</summary>
        public HandModifier NextHand { get; set; } = HandModifier.None;

        /// <summary>Years coming due later, and the hands left until they do (Mammon's ledger).</summary>
        public int DeferredYears { get; private set; }
        public int DeferredHands { get; private set; }

        /// <summary>Years of the soul's worth sold (the soul broker): the soul is that much smaller for the rest of the run.</summary>
        public int SoulSold { get; private set; }

        private readonly List<string> _relics = new List<string>();

        /// <summary>The cursed relics the run carries (at most <see cref="Relics.RelicRoster.MaxCarried"/>), in the order won.</summary>
        public IReadOnlyList<string> Relics => _relics;

        /// <summary>The relics that count towards the carry limit (an earned reward does not).</summary>
        public int CarriedOffered => _relics.Count(id => !HellPoker.Core.Relics.RelicRoster.IsReward(id));

        /// <summary>How many offered relics the run may carry: the demo's <see cref="Relics.RelicRoster.MaxCarried"/>, a Phase 2 run's
        /// <see cref="Relics.ChapterRelics.Slots"/>.</summary>
        public int MaxCarried { get; set; } = HellPoker.Core.Relics.RelicRoster.MaxCarried;

        /// <summary>The run carries all the offered relics it may.</summary>
        public bool RelicSlotsFull => CarriedOffered >= MaxCarried;

        /// <summary>
        /// A relic joins the run; false when it is already carried, unknown, or (an offered one) the run carries all it may. An earned
        /// reward (the Jester's Rattle) is beyond the limit.
        /// </summary>
        public bool AddRelic(string id)
        {
            HellPoker.Core.Relics.IRelic relic = HellPoker.Core.Relics.RelicRoster.Find(id);
            if (relic == null || _relics.Contains(id) || (!relic.IsReward && CarriedOffered >= MaxCarried))
                return false;
            _relics.Add(id);
            if (HellPoker.Core.Relics.RelicRoster.Find(id).Effects.RedrawsPerTable > 0)
                _redraws.Refill();   // a new die comes full, at every demon's table
            return true;
        }

        /// <summary>A relic leaves the run (sold off at a floor's black market, swapped for a warden's); false when it is not carried.</summary>
        public bool RemoveRelic(string id)
        {
            if (!_relics.Remove(id)) return false;
            if (SilencedCurse == id) SilencedCurse = null;
            if (AmplifiedRelic == id) AmplifiedRelic = null;
            return true;
        }

        // ---- Phase 2 (Demo 2.1): the bag — relics carried but not worn; only the worn ones act at a table.

        private readonly List<string> _bag = new List<string>();

        /// <summary>The relics in the bag (not worn: they do nothing at a table), in the order put in.</summary>
        public IReadOnlyList<string> Bag => _bag;

        /// <summary>How many the bag holds (0: no bag — the demo's run).</summary>
        public int MaxBag { get; set; }

        public bool BagFull => _bag.Count >= MaxBag;

        /// <summary>The relic is worn or in the bag.</summary>
        public bool Holds(string id) => _relics.Contains(id) || _bag.Contains(id);

        /// <summary>Every relic the run holds: worn, then the bag.</summary>
        public IEnumerable<string> AllHeld => _relics.Concat(_bag);

        /// <summary>A relic joins the run: worn while a slot is free, else in the bag; false when it is held already, unknown, or both are full.</summary>
        public bool Give(string id)
        {
            if (HellPoker.Core.Relics.RelicRoster.Find(id) == null || Holds(id)) return false;
            if (AddRelic(id)) return true;
            if (MaxBag <= 0 || BagFull || HellPoker.Core.Relics.RelicRoster.IsReward(id)) return false;
            _bag.Add(id);
            return true;
        }

        /// <summary>A worn relic goes into the bag (its curse silenced or desire lifted with it); false when the bag is full.</summary>
        public bool Unequip(string id)
        {
            if (!_relics.Contains(id) || BagFull || HellPoker.Core.Relics.RelicRoster.IsReward(id)) return false;
            RemoveRelic(id);
            _bag.Add(id);
            return true;
        }

        /// <summary>A relic of the bag is worn; false when every slot is taken.</summary>
        public bool Equip(string id)
        {
            if (!_bag.Contains(id) || RelicSlotsFull) return false;
            _bag.Remove(id);
            if (AddRelic(id)) return true;
            _bag.Add(id);
            return false;
        }

        /// <summary>A worn relic and one of the bag change places.</summary>
        public bool Swap(string worn, string bagged)
        {
            if (!_relics.Contains(worn) || !_bag.Contains(bagged) || HellPoker.Core.Relics.RelicRoster.IsReward(worn)) return false;
            int at = _bag.IndexOf(bagged);
            RemoveRelic(worn);
            _bag[at] = worn;
            AddRelic(bagged);
            return true;
        }

        /// <summary>A relic is left behind for good (worn or bagged); false when the run does not hold it (an earned reward stays).</summary>
        public bool Drop(string id)
        {
            if (HellPoker.Core.Relics.RelicRoster.IsReward(id)) return false;
            return RemoveRelic(id) || _bag.Remove(id);
        }

        private readonly HashSet<string> _cleansed = new HashSet<string>();

        /// <summary>The relics whose curse the alchemist burned away for good (their gift alone, worn or bagged).</summary>
        public IReadOnlyCollection<string> Cleansed => _cleansed;

        public bool Cleanse(string id)
        {
            if (!Holds(id)) return false;
            _cleansed.Add(id);
            return true;
        }

        private readonly HashSet<string> _forged = new HashSet<string>();

        /// <summary>The relics forged at a camp: their gift half again (once each; the mark stays in the bag too).</summary>
        public IReadOnlyCollection<string> Forged => _forged;

        /// <summary>A worn relic is forged; false when it is not worn or forged already.</summary>
        public bool Forge(string id)
        {
            if (!_relics.Contains(id) || _forged.Contains(id)) return false;
            _forged.Add(id);
            return true;
        }

        /// <summary>The bag and the forged marks of a saved run.</summary>
        public void RestoreBag(IEnumerable<string> bag, IEnumerable<string> forged, IEnumerable<string> cleansed = null)
        {
            _cleansed.Clear();
            foreach (string id in cleansed ?? Enumerable.Empty<string>())
                if (HellPoker.Core.Relics.RelicRoster.Find(id) != null) _cleansed.Add(id);
            _bag.Clear();
            foreach (string id in bag ?? Enumerable.Empty<string>())
                if (HellPoker.Core.Relics.RelicRoster.Find(id) != null && !Holds(id) && !HellPoker.Core.Relics.RelicRoster.IsReward(id) && _bag.Count < MaxBag)
                    _bag.Add(id);
            _forged.Clear();
            foreach (string id in forged ?? Enumerable.Empty<string>())
                if (HellPoker.Core.Relics.RelicRoster.Find(id) != null) _forged.Add(id);
        }

        /// <summary>The relic whose curse is silenced for now (a floor's purgatory fire, until the chapter ends); null for none.</summary>
        public string SilencedCurse { get; private set; }

        /// <summary>Silences a carried relic's curse; false when it is not carried.</summary>
        public bool SilenceCurse(string id)
        {
            if (!_relics.Contains(id)) return false;
            SilencedCurse = id;
            return true;
        }

        public void LiftSilence() => SilencedCurse = null;

        /// <summary>The relic whose gift and curse count twice for now (Phase 2's Desire, until the chapter ends); null for none.</summary>
        public string AmplifiedRelic { get; private set; }

        /// <summary>A carried relic counts twice until <see cref="LiftAmplify"/>; false when it is not carried.</summary>
        public bool Amplify(string id)
        {
            if (!_relics.Contains(id)) return false;
            AmplifiedRelic = id;
            return true;
        }

        public void LiftAmplify() => AmplifiedRelic = null;

        /// <summary>What the relics carried do to a hand, a silenced curse left out (a desired relic twice).</summary>
        public HellPoker.Core.Relics.RelicEffects CombinedRelics =>
            HellPoker.Core.Relics.RelicRoster.Combined(_relics, SilencedCurse, AmplifiedRelic, _forged, _cleansed);

        private readonly Game.TableCharges _redraws;

        public RunEffects()
        {
            _redraws = new Game.TableCharges(() => HellPoker.Core.Relics.RelicRoster.Combined(_relics).RedrawsPerTable);
        }

        /// <summary>Cards the relics may still redraw at this demon's table (the Bone Die: one at each demon's table).</summary>
        public int RedrawsLeft => _redraws.Left;

        /// <summary>The per-demon redraws for the save ("relics.redraws.tables").</summary>
        public string RedrawTablesCode => _redraws.Encode();

        /// <summary>
        /// The player sits at <paramref name="dealerId"/>'s table: the redraws left there (full at a table never sat at;
        /// <paramref name="fresh"/> — Lucifer's summons — full again). Changing seats does not refill a spent die.
        /// </summary>
        public void SitAt(string dealerId, bool fresh = false) => _redraws.SitAt(dealerId, fresh);

        /// <summary>A redraw used; false when none is left.</summary>
        public bool SpendRedraw() => _redraws.TrySpend();

        public void Defer(int years, int hands)
        {
            if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
            if (hands <= 0) throw new ArgumentOutOfRangeException(nameof(hands));
            DeferredYears += years;
            DeferredHands = hands;
        }

        /// <summary>A hand was settled: the deferred years come one hand closer. Returns the years that came due now (0 if none).</summary>
        public int HandSettled()
        {
            if (DeferredYears <= 0) return 0;
            if (--DeferredHands > 0) return 0;
            int due = DeferredYears;
            DeferredYears = 0;
            DeferredHands = 0;
            return due;
        }

        public void SellSoul(int years)
        {
            if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
            SoulSold += years;
        }

        /// <summary>
        /// A saved run comes back. <paramref name="redrawsLeft"/>: the redraws left at the table it was saved at (-1, an older
        /// save: full); <paramref name="redrawTables"/>: the per-demon list (null in an older save).
        /// </summary>
        public void Restore(HandModifier next, int deferredYears, int deferredHands, int soulSold, IEnumerable<string> relics = null,
            int redrawsLeft = -1, string redrawTables = null)
        {
            _relics.Clear();
            foreach (string id in relics ?? Enumerable.Empty<string>()) AddRelic(id);
            _redraws.Restore(redrawTables, redrawsLeft);
            if (deferredYears < 0 || deferredHands < 0 || soulSold < 0) throw new ArgumentOutOfRangeException(nameof(deferredYears));
            NextHand = next ?? HandModifier.None;
            DeferredYears = deferredYears;
            DeferredHands = deferredYears > 0 ? Math.Max(1, deferredHands) : 0;
            SoulSold = soulSold;
        }
    }
}
