using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Randomness;
using HellPoker.Core.Relics;

namespace HellPoker.Core.Chapters
{
    /// <summary>A relic on the black market's table, and its price this chapter.</summary>
    public readonly struct RelicOffer
    {
        public string RelicId { get; }
        public int Price { get; }

        public RelicOffer(string relicId, int price)
        {
            RelicId = relicId;
            Price = price;
        }
    }

    /// <summary>Who keeps the black market this time (Demo 2.1): each has a stall of his own, and comes once a chapter.</summary>
    public enum MarketVendor
    {
        /// <summary>The smuggler: Demo 2.0's market (three relics, a relic thrown away, the imp's eye, the Jester's jokers) and a word on the
        /// next warden's purse.</summary>
        Smuggler,

        /// <summary>Mammon's pawnshop: a relic sold for half its price, the soul pawned (coins now, years at the gate), two relics.</summary>
        Pawnshop,

        /// <summary>The bone merchant: three relics, rare and legendary most of them, cheaper — but every one bought is years on the sentence.</summary>
        BoneMerchant,

        /// <summary>The wandering alchemist: two relics of a rarity for one of the next; a relic's curse burned away for good; one relic.</summary>
        Alchemist,

        /// <summary>The gambler's stall: a closed box (its rarity rolled), and double or nothing on it.</summary>
        GamblersStall,

        /// <summary>The devil's tailor: a class upgrade (one of three), the Jester's jokers, the imp's eye.</summary>
        Tailor
    }

    /// <summary>
    /// The black market (a floor node), kept by one of six vendors (<see cref="MarketVendor"/>, rolled as the player steps in, never the same
    /// twice in a chapter). Relics are drawn and priced by rarity (55 / 65 / 75 coins in the first chapter, growing with the chapter); every
    /// market haggles once (half the time every price a fifth less, else a fifth more) and turns its stall over once for coins. Nothing takes
    /// the purse's last coin.
    /// </summary>
    public sealed class BlackMarket
    {
        /// <summary>The first chapter's prices of a relic by rarity: common, rare, legendary.</summary>
        public static readonly int[] RelicPrices = { ChapterRelics.CommonPrice, ChapterRelics.RarePrice, ChapterRelics.LegendaryPrice };

        public const int DropRelicPrice = 25;
        public const int ImpsEyePrice = 20;
        public const int JokerPrice = 20;
        public const int RelicsOnTable = 3;

        /// <summary>The haggle: half the time every price this much (percent) less, else this much more; the stall turned over for these coins.</summary>
        public const int HagglePercent = 20, HaggleWinPercent = 50, RerollPrice = 10;

        /// <summary>The pawnshop: a relic sells for this share of its price; the soul pawned brings these coins and these years at the gate.</summary>
        public const int SellPercent = 50, PawnCoins = 40, PawnYears = 80;

        /// <summary>The bone merchant: this much (percent) of a price, and these years on the sentence for every relic.</summary>
        public const int BonePricePercent = 70, BoneYears = 40;

        /// <summary>The alchemist: a curse burned away for good.</summary>
        public const int CleansePrice = 35;

        /// <summary>The gambler's box: its price, and the odds of its rarity (common, rare, legendary).</summary>
        public const int BoxPrice = 30;
        public static readonly RelicWeights BoxOdds = new RelicWeights(60, 32, 8);

        /// <summary>The tailor's upgrade: its price, and how many he shows.</summary>
        public const int UpgradePrice = 80, UpgradesShown = 3;

        private readonly ChapterRun _run;
        private readonly IRandomSource _random;
        private List<RelicOffer> _relics;

        public MarketVendor Vendor { get; }

        public IReadOnlyList<RelicOffer> Relics => _relics;

        /// <summary>How many things were bought on this visit.</summary>
        public int Purchases { get; private set; }

        public int RelicsBought { get; private set; }

        /// <summary>The prices, in percent, after the haggle (100 before it).</summary>
        public int PricePercent { get; private set; } = 100;

        public bool Haggled { get; private set; }
        public bool Rerolled { get; private set; }

        public BlackMarket(ChapterRun run, IRandomSource random, MarketVendor vendor = MarketVendor.Smuggler)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            Vendor = vendor;
            Stock();
            if (vendor == MarketVendor.Tailor) _upgrades = run.UpgradesForSale(UpgradesShown, random).ToList();
        }

        /// <summary>How many relics the stall shows, and how they are drawn.</summary>
        private void Stock()
        {
            int count = Vendor == MarketVendor.Pawnshop ? 2 : Vendor == MarketVendor.Alchemist ? 1
                : Vendor == MarketVendor.GamblersStall || Vendor == MarketVendor.Tailor ? 0 : RelicsOnTable;
            RelicWeights weights = Vendor == MarketVendor.BoneMerchant ? new RelicWeights(10, 60, 30) : RelicWeights.Market;
            // Drawn by rarity, priced by it, the cheapest first.
            IReadOnlyList<string> picked = count == 0 ? Array.Empty<string>()
                : ChapterRelics.Draw(_random, weights, count, _run.Effects.AllHeld.ToList(), _run.RelicAvailable, _run.Sinner.Id);
            _relics = picked.Select(id => new RelicOffer(id, Price(_run.PriceOf(ChapterRelics.PriceOf(id)) *
                                                               (Vendor == MarketVendor.BoneMerchant ? BonePricePercent : 100) / 100)))
                .OrderBy(o => o.Price).ToList();
        }

        /// <summary>A price after the haggle.</summary>
        private int Price(int price) => Math.Max(1, price * PricePercent / 100);

        /// <summary>The run holds all it may (five worn, the bag full).</summary>
        public bool SellsNoMore => _run.RelicsFull;

        /// <summary>A price the purse can pay and still hold a coin: an empty purse would lose at the next table before a card is dealt.</summary>
        public bool CanPay(int price) => _run.Purse.Coins > price;

        public int DropPrice => Price(_run.PriceOf(DropRelicPrice));
        public int EyePrice => Price(_run.PriceOf(ImpsEyePrice));

        /// <summary>A joker in or out (the Joker Purse: half).</summary>
        public int JokerServicePrice => Math.Max(1, Price(_run.PriceOf(JokerPrice)) / (_run.Kit.Has(UpgradeIds.JokerPurse) ? 2 : 1));

        public bool CanBuyRelic(string id)
        {
            int i = _relics.FindIndex(o => o.RelicId == id);
            return i >= 0 && !SellsNoMore && CanPay(_relics[i].Price);
        }

        public bool BuyRelic(string id)
        {
            if (!CanBuyRelic(id)) return false;
            RelicOffer offer = _relics.First(o => o.RelicId == id);
            if (!_run.Purse.TrySpend(offer.Price)) return false;
            _run.Effects.Give(id);   // worn, else in the bag
            if (Vendor == MarketVendor.BoneMerchant) _run.OweAtGate(_run.Scaled(BoneYears));   // bones for bones: years on the sentence
            _relics.Remove(offer);
            Purchases++;
            RelicsBought++;
            return true;
        }

        public bool CanDropRelic(string id) => Vendor == MarketVendor.Smuggler && _run.Effects.Holds(id) && !RelicRoster.IsReward(id) && CanPay(DropPrice);

        public bool DropRelic(string id)
        {
            if (!CanDropRelic(id) || !_run.Purse.TrySpend(DropPrice)) return false;
            _run.Effects.Drop(id);
            Purchases++;
            return true;
        }

        public bool OffersEye => Vendor == MarketVendor.Smuggler || Vendor == MarketVendor.Tailor;

        public bool CanBuyImpsEye => OffersEye && !_run.ImpsEyeNext && CanPay(EyePrice);

        /// <summary>One of the next imp's cards plays face up from the deal.</summary>
        public bool BuyImpsEye()
        {
            if (!CanBuyImpsEye || !_run.Purse.TrySpend(EyePrice)) return false;
            _run.BuyImpsEye();
            Purchases++;
            return true;
        }

        public bool OffersJokers => (Vendor == MarketVendor.Smuggler || Vendor == MarketVendor.Tailor) && _run.Sinner.Class.StartingJokers > 0;

        /// <summary>The Jester's deck: a joker in (+1) or out (−1; never below the start).</summary>
        public bool CanChangeJokers(int delta) =>
            OffersJokers && CanPay(JokerServicePrice) && (delta > 0 || _run.Sinner.Jokers > _run.Sinner.Class.StartingJokers);

        public bool ChangeJokers(int delta)
        {
            if (delta == 0 || !CanChangeJokers(delta) || !_run.Purse.TrySpend(JokerServicePrice)) return false;
            _run.Sinner.ChangeJokers(Math.Sign(delta));
            Purchases++;
            return true;
        }

        // ------------------------------------------------------------------ every market: the haggle and the stall turned over

        /// <summary>The haggle, once: half the time every price a fifth less, else a fifth more. True when it went the player's way.</summary>
        public bool Haggle()
        {
            if (Haggled) return false;
            Haggled = true;
            bool won = _random.Next(100) < HaggleWinPercent;
            int before = PricePercent;
            PricePercent = won ? 100 - HagglePercent : 100 + HagglePercent;
            _relics = _relics.Select(o => new RelicOffer(o.RelicId, Math.Max(1, o.Price * PricePercent / before))).ToList();
            return won;
        }

        public int RerollCost => Price(_run.PriceOf(RerollPrice));

        /// <summary>A stall that shows relics (or the tailor's upgrades) may be turned over, once.</summary>
        public bool CanReroll => !Rerolled && CanPay(RerollCost) &&
                                 (Vendor == MarketVendor.Tailor ? _upgrades.Count > 0 : Vendor != MarketVendor.GamblersStall);

        /// <summary>The stall turned over, once, for coins: new relics (the tailor: new upgrades).</summary>
        public bool Reroll()
        {
            if (!CanReroll || !_run.Purse.TrySpend(RerollCost)) return false;
            Rerolled = true;
            if (Vendor == MarketVendor.Tailor) _upgrades = _run.UpgradesForSale(UpgradesShown, _random).ToList();
            else Stock();
            Purchases++;
            return true;
        }

        // ------------------------------------------------------------------ the pawnshop

        /// <summary>What a held relic fetches at the pawnshop: half its price (an earned one is not for sale).</summary>
        public int SellPrice(string id) => Math.Max(1, _run.PriceOf(ChapterRelics.PriceOf(id)) * SellPercent / 100 * PricePercent / 100);

        public bool CanSellRelic(string id) => Vendor == MarketVendor.Pawnshop && _run.Effects.Holds(id) && !RelicRoster.IsReward(id);

        public bool SellRelic(string id)
        {
            if (!CanSellRelic(id)) return false;
            int coins = SellPrice(id);
            _run.Effects.Drop(id);
            _run.Purse.Add(coins);
            Purchases++;
            return true;
        }

        public bool Pawned { get; private set; }
        public int PawnCoinsNow => _run.Scaled(PawnCoins);
        public int PawnYearsNow => _run.Scaled(PawnYears);
        public bool CanPawn => Vendor == MarketVendor.Pawnshop && !Pawned;

        /// <summary>The soul pawned: coins now, years on the sentence (and the demon's bar at the gate).</summary>
        public bool Pawn()
        {
            if (!CanPawn) return false;
            Pawned = true;
            _run.Purse.Add(PawnCoinsNow);
            _run.OweAtGate(PawnYearsNow);
            Purchases++;
            return true;
        }

        // ------------------------------------------------------------------ the alchemist

        /// <summary>The rarities the alchemist turns two of into one of the next (common → rare, rare → legendary) that the run holds two of.</summary>
        public IReadOnlyList<RelicRarity> Transmutable =>
            Vendor != MarketVendor.Alchemist ? Array.Empty<RelicRarity>()
                : new[] { RelicRarity.Common, RelicRarity.Rare }.Where(r => HeldOf(r).Count() >= 2 && _run.CanDrawRelic(Next(r))).ToArray();

        public IEnumerable<string> HeldOf(RelicRarity rarity) =>
            _run.Effects.AllHeld.Where(id => !RelicRoster.IsReward(id) && ChapterRelics.RarityOf(id) == rarity);

        private static RelicRarity Next(RelicRarity rarity) => rarity == RelicRarity.Common ? RelicRarity.Rare : RelicRarity.Legendary;

        /// <summary>Two held relics of the same rarity for one of the next; the relic given, or null.</summary>
        public string Transmute(string first, string second)
        {
            if (Vendor != MarketVendor.Alchemist || first == null || second == null || first == second) return null;
            RelicRarity rarity = ChapterRelics.RarityOf(first);
            if (!Transmutable.Contains(rarity) || ChapterRelics.RarityOf(second) != rarity || !_run.Effects.Holds(first) || !_run.Effects.Holds(second))
                return null;
            string made = _run.DrawRelic(Next(rarity));
            if (made == null) return null;
            _run.Effects.Drop(first);
            _run.Effects.Drop(second);
            _run.Effects.Give(made);
            Purchases++;
            return made;
        }

        public int CleanseCost => Price(_run.PriceOf(CleansePrice));

        public bool CanCleanse(string id) => Vendor == MarketVendor.Alchemist && _run.Effects.Holds(id) && !_run.Effects.Cleansed.Contains(id) &&
                                             !RelicRoster.IsReward(id) && CanPay(CleanseCost);

        /// <summary>A relic's curse burned away for good (its gift stays).</summary>
        public bool Cleanse(string id)
        {
            if (!CanCleanse(id) || !_run.Purse.TrySpend(CleanseCost)) return false;
            _run.Effects.Cleanse(id);
            Purchases++;
            return true;
        }

        // ------------------------------------------------------------------ the gambler's stall

        /// <summary>The box bought and not yet opened: its rarity; null for none.</summary>
        public RelicRarity? Box { get; private set; }

        public int BoxCost => Price(_run.PriceOf(BoxPrice));

        public bool CanBuyBox => Vendor == MarketVendor.GamblersStall && Box == null && !SellsNoMore && CanPay(BoxCost);

        public bool BuyBox()
        {
            if (!CanBuyBox || !_run.Purse.TrySpend(BoxCost)) return false;
            RelicWeights odds = BoxOdds;
            int roll = _random.Next(odds.Common + odds.Rare + odds.Legendary);
            Box = roll < odds.Common ? RelicRarity.Common : roll < odds.Common + odds.Rare ? RelicRarity.Rare : RelicRarity.Legendary;
            Purchases++;
            return true;
        }

        /// <summary>The box opened: a relic of its rarity (worn, else in the bag); null when none was left.</summary>
        public string OpenBox()
        {
            if (Box == null) return null;
            string relic = _run.DrawRelic(Box.Value) ?? _run.DrawRelic(RelicRarity.Common);
            Box = null;
            if (relic != null) _run.GiveRelic(relic);
            RelicsBought++;
            return relic;
        }

        /// <summary>Double or nothing on the box: half the time two relics of its rarity, else nothing. The relics given (empty: lost).</summary>
        public IReadOnlyList<string> DoubleOrNothing()
        {
            if (Box == null) return Array.Empty<string>();
            RelicRarity rarity = Box.Value;
            Box = null;
            if (_random.Next(100) >= 50) return Array.Empty<string>();
            var given = new List<string>();
            for (int i = 0; i < 2; i++)
            {
                string relic = _run.DrawRelic(rarity) ?? _run.DrawRelic(RelicRarity.Common);
                if (relic == null) break;
                _run.GiveRelic(relic);
                given.Add(relic);
                if (_run.WardenRelicWaiting != null) break;   // full: the second waits for a choice
            }
            RelicsBought += given.Count;
            return given;
        }

        // ------------------------------------------------------------------ the tailor

        private List<string> _upgrades = new List<string>();

        /// <summary>The upgrades the tailor shows (the class's, not yet had).</summary>
        public IReadOnlyList<string> Upgrades => _upgrades;

        public int UpgradeCost => Price(_run.PriceOf(UpgradePrice));

        public bool CanBuyUpgrade(string id) => Vendor == MarketVendor.Tailor && _upgrades.Contains(id) && CanPay(UpgradeCost);

        public bool BuyUpgrade(string id)
        {
            if (!CanBuyUpgrade(id) || !_run.Purse.TrySpend(UpgradeCost) || !_run.Kit.AddUpgrade(id)) return false;
            _upgrades.Clear();   // one a visit
            Purchases++;
            return true;
        }

        // ------------------------------------------------------------------ the smuggler

        /// <summary>The next warden's purse, as the smuggler tells it.</summary>
        public int NextWardenPurse => Math.Max(1, _run.Rules.WardenCoins * _run.NextWardenPursePercent / 100);
    }
}
