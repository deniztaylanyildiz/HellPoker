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
    /// The black market (a floor node): three cursed relics the run does not carry, drawn and priced by rarity (45 / 55 / 65
    /// coins in the first chapter), and services — throw a relic away (25), the imp's eye (20: one of the next imp's cards plays face
    /// up), a joker in or out of the Jester's deck (20). Prices grow with the chapter (<see cref="ChapterRules.PricePercent"/>). Nothing takes the purse's last coin,
    /// and nothing turns coins into years.
    /// </summary>
    public sealed class BlackMarket
    {
        /// <summary>The first chapter's prices of a relic by rarity: common, rare, legendary.</summary>
        public static readonly int[] RelicPrices = { ChapterRelics.CommonPrice, ChapterRelics.RarePrice, ChapterRelics.LegendaryPrice };

        public const int DropRelicPrice = 25;
        public const int ImpsEyePrice = 20;
        public const int JokerPrice = 20;
        public const int RelicsOnTable = 3;

        /// <summary>The market sells to a run carrying fewer relics than this: the third slot is a warden's or a demon's to fill (bought, it
        /// emptied the purses before Belial's gate: tribute paid in full 37% instead of 49% for the Peasant).</summary>
        public const int SellsUpTo = 2;

        /// <summary>The run carries as many relics as the market sells to.</summary>
        public bool SellsNoMore => _run.Effects.CarriedOffered >= Math.Min(_run.Effects.MaxCarried, SellsUpTo);

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
            // Drawn by rarity (rare 25%, legendary 8%), priced by it, the cheapest first.
            IReadOnlyList<string> picked = ChapterRelics.Draw(random, RelicWeights.Market, RelicsOnTable, run.Effects.Relics.ToList(), run.RelicAvailable);
            _relics = picked.Select(id => new RelicOffer(id, run.PriceOf(ChapterRelics.PriceOf(id)))).OrderBy(o => o.Price).ToList();
        }

        /// <summary>A price the purse can pay and still hold a coin: an empty purse would lose at the next table before a card is dealt.</summary>
        public bool CanPay(int price) => _run.Purse.Coins > price;

        public int DropPrice => _run.PriceOf(DropRelicPrice);
        public int EyePrice => _run.PriceOf(ImpsEyePrice);

        /// <summary>A joker in or out (the Joker Purse: half).</summary>
        public int JokerServicePrice => Math.Max(1, _run.PriceOf(JokerPrice) / (_run.Kit.Has(UpgradeIds.JokerPurse) ? 2 : 1));

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
            _run.Effects.AddRelic(id);
            _relics.Remove(offer);
            Purchases++;
            RelicsBought++;
            return true;
        }

        public bool CanDropRelic(string id) => _run.Effects.Relics.Contains(id) && !RelicRoster.IsReward(id) && CanPay(DropPrice);

        public bool DropRelic(string id)
        {
            if (!CanDropRelic(id) || !_run.Purse.TrySpend(DropPrice)) return false;
            _run.Effects.RemoveRelic(id);
            Purchases++;
            return true;
        }

        public bool CanBuyImpsEye => !_run.ImpsEyeNext && CanPay(EyePrice);

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
            _run.Sinner.Class.StartingJokers > 0 && CanPay(JokerServicePrice) && (delta > 0 || _run.Sinner.Jokers > _run.Sinner.Class.StartingJokers);

        public bool ChangeJokers(int delta)
        {
            if (delta == 0 || !CanChangeJokers(delta) || !_run.Purse.TrySpend(JokerServicePrice)) return false;
            _run.Sinner.ChangeJokers(Math.Sign(delta));
            Purchases++;
            return true;
        }
    }
}
