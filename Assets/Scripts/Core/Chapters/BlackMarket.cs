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

    /// <summary>
    /// The black market (a floor node): three cursed relics the run does not carry, the stronger gift the dearer (45 / 50 / 60
    /// coins in the first chapter), and services — throw a relic away (25), the imp's eye (20: one of the next imp's cards plays face
    /// up), a joker in or out of the Jester's deck (20). Prices grow with the chapter (<see cref="ChapterRules.PricePercent"/>). Nothing is bought on debt, and nothing
    /// turns coins into years.
    /// </summary>
    public sealed class BlackMarket
    {
        /// <summary>The first chapter's prices of the three relics on the table, cheapest first.</summary>
        public static readonly int[] RelicPrices = { 45, 50, 60 };

        public const int DropRelicPrice = 25;
        public const int ImpsEyePrice = 20;
        public const int JokerPrice = 20;
        public const int RelicsOnTable = 3;

        /// <summary>The offered relics from the weakest gift to the strongest (the price follows it).</summary>
        public static readonly string[] StrengthOrder = { RelicIds.ThornedRosary, RelicIds.RustyCrown, RelicIds.FerrymansCoin, RelicIds.BoneDie };

        private readonly ChapterRun _run;
        private readonly List<RelicOffer> _relics;

        public IReadOnlyList<RelicOffer> Relics => _relics;

        /// <summary>How many things were bought on this visit.</summary>
        public int Purchases { get; private set; }

        public int RelicsBought { get; private set; }

        public BlackMarket(ChapterRun run, IRandomSource random)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            if (random == null) throw new ArgumentNullException(nameof(random));
            var pool = RelicRoster.Offered.Select(r => r.Id).Where(id => !run.Effects.Relics.Contains(id)).ToList();
            var picked = new List<string>();
            while (picked.Count < RelicsOnTable && pool.Count > 0)
            {
                int i = random.Next(pool.Count);
                picked.Add(pool[i]);
                pool.RemoveAt(i);
            }
            // The stronger the gift, the higher the price.
            var byStrength = picked.OrderBy(id => Array.IndexOf(StrengthOrder, id)).ToList();
            int skip = RelicPrices.Length - byStrength.Count;
            _relics = byStrength.Select((id, i) => new RelicOffer(id, run.Rules.Price(RelicPrices[skip + i]))).ToList();
        }

        public int DropPrice => _run.Rules.Price(DropRelicPrice);
        public int EyePrice => _run.Rules.Price(ImpsEyePrice);
        public int JokerServicePrice => _run.Rules.Price(JokerPrice);

        public bool CanBuyRelic(string id)
        {
            int i = _relics.FindIndex(o => o.RelicId == id);
            return i >= 0 && _run.Effects.CarriedOffered < RelicRoster.MaxCarried && _run.Purse.Coins >= _relics[i].Price;
        }

        public bool BuyRelic(string id)
        {
            if (!CanBuyRelic(id)) return false;
            RelicOffer offer = _relics.First(o => o.RelicId == id);
            if (!_run.Purse.TrySpend(offer.Price)) return false;
            _run.Effects.AddRelic(id);
            _relics.Remove(offer);
            Purchases++;
            RelicsBought++;
            return true;
        }

        public bool CanDropRelic(string id) => _run.Effects.Relics.Contains(id) && !RelicRoster.IsReward(id) && _run.Purse.Coins >= DropPrice;

        public bool DropRelic(string id)
        {
            if (!CanDropRelic(id) || !_run.Purse.TrySpend(DropPrice)) return false;
            _run.Effects.RemoveRelic(id);
            Purchases++;
            return true;
        }

        public bool CanBuyImpsEye => !_run.ImpsEyeNext && _run.Purse.Coins >= EyePrice;

        /// <summary>One of the next imp's cards plays face up from the deal.</summary>
        public bool BuyImpsEye()
        {
            if (!CanBuyImpsEye || !_run.Purse.TrySpend(EyePrice)) return false;
            _run.BuyImpsEye();
            Purchases++;
            return true;
        }

        /// <summary>The Jester's deck: a joker in (+1) or out (−1; never below the start).</summary>
        public bool CanChangeJokers(int delta) =>
            _run.Sinner.Class.StartingJokers > 0 && _run.Purse.Coins >= JokerServicePrice && (delta > 0 || _run.Sinner.Jokers > _run.Sinner.Class.StartingJokers);

        public bool ChangeJokers(int delta)
        {
            if (delta == 0 || !CanChangeJokers(delta) || !_run.Purse.TrySpend(JokerServicePrice)) return false;
            _run.Sinner.ChangeJokers(Math.Sign(delta));
            Purchases++;
            return true;
        }
    }
}
