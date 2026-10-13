using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;

namespace HellPoker.Core.Chapters
{
    /// <summary>Why a class's Phase 2 act cannot be done now (the presentation says it in words).</summary>
    public enum KitRefusal
    {
        None,

        /// <summary>Not at this moment of the hand.</summary>
        NotNow,

        /// <summary>The charge gauge is not full.</summary>
        NotCharged,

        /// <summary>Not enough taxes.</summary>
        NoTaxes,

        /// <summary>Done once already this hand.</summary>
        AlreadyUsed,

        /// <summary>The satchel is empty.</summary>
        Empty,

        /// <summary>No card was chosen to throw (the harvest fills thrown places).</summary>
        NothingThrown,

        /// <summary>No card to do it with (no face-down card to turn, no card the act may take).</summary>
        NoCard,

        /// <summary>No raise can be made this hand.</summary>
        CannotRaise
    }

    /// <summary>The class's part of a Phase 2 table: its power replaces the demo's (the free fold, the ward, the crown).</summary>
    public abstract class ClassPowers : TableHooks
    {
        protected ClassPowers(ClassKit kit, IRandomSource random)
        {
            Kit = kit ?? throw new ArgumentNullException(nameof(kit));
            Random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public ClassKit Kit { get; }
        protected IRandomSource Random { get; }

        public override bool ReplacesClassPower => true;

        /// <summary>True when the gauge is full (the charged power can be paid).</summary>
        public bool IsCharged => Kit.Sinner.Charge >= Kit.Sinner.Rules.Full;

        /// <summary>Before the draw is done: the hand is being played and the draw still to come.</summary>
        protected static bool BeforeTheDraw(HellPokerGame game) =>
            game.HandInPlay && !game.IsAfterDraw && (game.Phase == GamePhase.PlayerReveal || game.Phase == GamePhase.Drawing);

        protected static bool Improves(HellPokerGame game, Hand after, Hand before) =>
            game.Evaluator.Evaluate(after).CompareTo(game.Evaluator.Evaluate(before)) > 0;
    }

    /// <summary>
    /// The Peasant's satchel. After the draw one of the thrown cards may go into it (<see cref="Keep"/>); it holds <see cref="ClassKit.SatchelSize"/>, the oldest falls
    /// out. In a draw a thrown place may take a satchel card instead of the deck's (<see cref="HellPokerGame.Draw(IReadOnlyCollection{int}, IReadOnlyDictionary{int, Card})"/>)
    /// — but the demon sees it: that hand his re-raise comes <see cref="ClassKit.SatchelReRaisePercent"/> more often. The charged power,
    /// the harvest (<see cref="Harvest"/>), fills every thrown place from the satchel unseen.
    /// </summary>
    public sealed class SatchelPowers : ClassPowers
    {
        private readonly List<Card> _offered = new List<Card>();
        private bool _seen, _harvesting;
        private int _kept;

        public SatchelPowers(ClassKit kit, IRandomSource random) : base(kit, random)
        {
        }

        public override IEnumerable<Card> CardsOutOfDeck => Kit.Satchel;

        /// <summary>The cards thrown in this hand's draw that may still go into the satchel (one of them, until the next deal).</summary>
        public IReadOnlyList<Card> Offered => _kept >= Kit.KeepsPerHand ? (IReadOnlyList<Card>)Array.Empty<Card>() : _offered;

        /// <summary>The demon saw the satchel opened this hand.</summary>
        public bool SeenThisHand => _seen;

        public bool CanKeep(Card card) => _kept < Kit.KeepsPerHand && _offered.Contains(card);

        /// <summary>One of the thrown cards into the satchel (once a hand).</summary>
        public bool Keep(HellPokerGame game, Card card)
        {
            if (!CanKeep(card) || game.HouseHand?.Contains(card) == true) return false;
            if (!Kit.Keep(card)) return false;
            _kept++;
            _offered.Remove(card);   // Abundance: the other thrown cards may still go in
            return true;
        }

        public override void HandDealt(HellPokerGame game)
        {
            _offered.Clear();
            _seen = false;
            _kept = 0;
        }

        public override bool CanTakeFromOutside(HellPokerGame game, Card card) => Kit.Satchel.Contains(card);

        public override void TookFromOutside(HellPokerGame game, IReadOnlyCollection<Card> cards)
        {
            foreach (Card card in cards)
                if (Kit.TakeOut(card)) Kit.TakenFromSatchel++;
            if (!_harvesting) _seen = true;
        }

        public override void HandDrawn(HellPokerGame game, ExchangeResult player, ExchangeResult house)
        {
            _offered.Clear();
            // A thrown card the demon took up (Lilith's temptation) is no longer there to keep.
            _offered.AddRange(player.Discarded.Where(c => !c.IsJoker && !game.HouseHand.Contains(c)));
        }

        public override bool HouseReRaises(HellPokerGame game, bool wants) =>
            wants || (_seen && Random.Next(100) < Kit.SatchelRisk);

        /// <summary>Why the harvest cannot be made now (in the draw, a full gauge, cards in the satchel and cards thrown).</summary>
        public KitRefusal WhyNoHarvest(HellPokerGame game, IReadOnlyCollection<int> discards)
        {
            if (game.Phase != GamePhase.Drawing || !game.HandInPlay) return KitRefusal.NotNow;
            if (!IsCharged) return KitRefusal.NotCharged;
            if (Kit.Satchel.Count == 0) return KitRefusal.Empty;
            if (discards == null || discards.Count == 0) return KitRefusal.NothingThrown;
            return KitRefusal.None;
        }

        /// <summary>The harvest: the draw, with every thrown place the satchel can fill filled from it (the best cards for the hand); the
        /// demon does not see it. Spends the gauge. Null when it cannot be done.</summary>
        public ExchangeResult Harvest(HellPokerGame game, IReadOnlyCollection<int> discards)
        {
            if (WhyNoHarvest(game, discards) != KitRefusal.None) return null;
            IReadOnlyDictionary<int, Card> fill = BestFill(game, game.PlayerHand, discards, Kit.Satchel);
            if (!game.CanDraw(discards, fill, out _) || !Kit.Sinner.SpendCharge()) return null;
            _harvesting = true;
            try
            {
                Kit.Harvests++;
                return game.Draw(discards, fill);
            }
            finally
            {
                _harvesting = false;
            }
        }

        /// <summary>
        /// The satchel cards that fill the thrown places best: as many as there are of either, each tried in every place; places left
        /// empty count as the card thrown there (nothing better is promised).
        /// </summary>
        public static IReadOnlyDictionary<int, Card> BestFill(HellPokerGame game, Hand hand, IReadOnlyCollection<int> discards, IReadOnlyList<Card> satchel)
        {
            int[] places = discards.OrderBy(i => i).ToArray();
            int count = Math.Min(places.Length, satchel.Count);
            Dictionary<int, Card> best = null;
            HandEvaluation bestEval = null;
            foreach (int[] chosenPlaces in Subsets(places, count))
            foreach (Card[] cards in Arrangements(satchel.ToArray(), count))
            {
                Hand h = hand;
                for (int k = 0; k < count; k++) h = h.With(chosenPlaces[k], cards[k]);
                HandEvaluation eval = game.Evaluator.Evaluate(h);
                if (bestEval != null && eval.CompareTo(bestEval) <= 0) continue;
                bestEval = eval;
                best = new Dictionary<int, Card>();
                for (int k = 0; k < count; k++) best[chosenPlaces[k]] = cards[k];
            }
            return best ?? new Dictionary<int, Card>();
        }

        private static IEnumerable<int[]> Subsets(int[] items, int size)
        {
            if (size == 0)
            {
                yield return Array.Empty<int>();
                yield break;
            }
            for (int i = 0; i <= items.Length - size; i++)
            foreach (int[] rest in Subsets(items.Skip(i + 1).ToArray(), size - 1))
                yield return new[] { items[i] }.Concat(rest).ToArray();
        }

        private static IEnumerable<Card[]> Arrangements(Card[] items, int size)
        {
            if (size == 0)
            {
                yield return Array.Empty<Card>();
                yield break;
            }
            for (int i = 0; i < items.Length; i++)
            {
                Card[] others = items.Where((_, j) => j != i).ToArray();
                foreach (Card[] rest in Arrangements(others, size - 1))
                    yield return new[] { items[i] }.Concat(rest).ToArray();
            }
        }
    }

    /// <summary>
    /// The Warlock's prophecy: he sees the deck's top cards (<see cref="ClassKit.ProphecyCards"/>). The price: every hand the demon knows
    /// one of his cards, and bets and draws by it. The charged power rearranges the deck: up to <see cref="ClassKit.RearrangesPerUse"/> of the
    /// seen cards go to the bottom.
    /// </summary>
    public sealed class ProphecyPowers : ClassPowers
    {
        /// <summary>The demon knowing a low card bullies: his re-raise comes this much (percent) more often.</summary>
        public const int BullyPercent = 20;

        public ProphecyPowers(ClassKit kit, IRandomSource random) : base(kit, random)
        {
        }

        /// <summary>The card of the Warlock's the demon knows this hand; null between hands.</summary>
        public Card? KnownCard { get; private set; }

        /// <summary>Cards the charged power may still send to the bottom (this hand).</summary>
        public int RearrangesLeft { get; private set; }

        /// <summary>The deck's top cards as the Warlock sees them, the next one first.</summary>
        public IReadOnlyList<Card> TopCards(HellPokerGame game) =>
            game.Deck.Remaining.Reverse().Take(Kit.ProphecyCards).ToArray();

        public override void HandDealt(HellPokerGame game)
        {
            // The Blind Mirror: half the hands the demon learns nothing (the dice are only asked with the upgrade).
            KnownCard = Kit.DemonKnowsPercent >= 100 || Random.Next(100) < Kit.DemonKnowsPercent ? game.PlayerHand[Random.Next(Hand.Size)] : (Card?)null;
        }

        public override void HandSettled(HellPokerGame game, RoundResult round)
        {
            KnownCard = null;
            RearrangesLeft = 0;
        }

        public KitRefusal WhyNoRearrange(HellPokerGame game)
        {
            if (game.IsGameOver || game.IsAfterDraw && game.HandInPlay) return KitRefusal.NotNow;
            if (RearrangesLeft > 0) return KitRefusal.AlreadyUsed;
            if (!IsCharged) return KitRefusal.NotCharged;
            if (TopCards(game).Count == 0) return KitRefusal.NoCard;
            return KitRefusal.None;
        }

        /// <summary>The charged power: up to <see cref="ClassKit.RearrangesPerUse"/> seen cards may now go to the bottom.</summary>
        public bool StartRearranging(HellPokerGame game)
        {
            if (!Kit.CanSendToBottom || WhyNoRearrange(game) != KitRefusal.None || !Kit.Sinner.SpendCharge()) return false;
            RearrangesLeft = Kit.RearrangesPerCharge;
            return true;
        }

        /// <summary>The seen card at <paramref name="index"/> (0: the top) goes to the bottom of the deck.</summary>
        public bool SendToBottom(HellPokerGame game, int index)
        {
            if (RearrangesLeft <= 0 || (game.IsAfterDraw && game.HandInPlay)) return false;
            List<Card> order = game.Deck.Remaining.Reverse().ToList();
            if (index < 0 || index >= Math.Min(Kit.ProphecyCards, order.Count)) return false;
            Card card = order[index];
            order.RemoveAt(index);
            order.Add(card);
            game.Deck.Restore(order);
            RearrangesLeft--;
            Kit.Rearranges++;
            return true;
        }

        /// <summary>The demon knows a high card: with less than two pair he does not raise back; a low one: he bullies.</summary>
        public override bool HouseReRaises(HellPokerGame game, bool wants)
        {
            if (!KnownCard.HasValue || KnownCard.Value.IsJoker) return wants;
            if (KnownCard.Value.Rank >= Rank.Jack)
                return wants && game.Evaluator.Evaluate(game.HouseHand).Category >= HandCategory.TwoPair;
            if (KnownCard.Value.Rank <= Rank.Nine && !wants) return Random.Next(100) < BullyPercent;
            return wants;
        }

        /// <summary>The demon knows a queen or better: a House holding nothing chases with every card it may throw.</summary>
        public override IReadOnlyList<int> AdjustHouseDiscards(HellPokerGame game, IReadOnlyList<int> discards)
        {
            if (!KnownCard.HasValue || KnownCard.Value.IsJoker || KnownCard.Value.Rank < Rank.Queen) return discards;
            Hand house = game.HouseHand;
            if (game.Evaluator.Evaluate(house).Category != HandCategory.HighCard || discards.Count >= game.Rules.MaxDiscards) return discards;
            return Enumerable.Range(0, Hand.Size).Where(i => !house[i].IsJoker).OrderBy(i => house[i].Rank).Take(game.Rules.MaxDiscards)
                .OrderBy(i => i).ToArray();
        }
    }

    /// <summary>The King's acts of the treasury (each once a hand, before the draw).</summary>
    public enum TaxAct
    {
        /// <summary>One of the demon's face-down cards turns.</summary>
        Reveal,

        /// <summary>The King's next raise this hand is the demon's money.</summary>
        FreeRaise,

        /// <summary>One of the demon's open cards is taken, one of the King's given for it.</summary>
        Seize
    }

    /// <summary>
    /// The King's treasury: a tax for every hand he wins (at most <see cref="ClassKit.TaxMax"/>), spent before the draw on three acts
    /// (<see cref="TaxAct"/>). His palace: no cheat — and no demon's rule — may touch a jack, queen or king in his hand. The price: a demon's
    /// hand lost takes half the taxes. The charged power is a seizure for nothing.
    /// </summary>
    public sealed class TreasuryPowers : ClassPowers
    {
        private readonly HashSet<TaxAct> _done = new HashSet<TaxAct>();
        private readonly bool _bossTable;

        public TreasuryPowers(ClassKit kit, IRandomSource random, bool bossTable) : base(kit, random)
        {
            _bossTable = bossTable;
        }

        /// <summary>The taxes an act costs (the class's upgrades may make it cheaper).</summary>
        public int PriceOf(TaxAct act) => Kit.PriceOf(act);

        public bool DoneThisHand(TaxAct act) => _done.Contains(act);

        public override void HandDealt(HellPokerGame game) => _done.Clear();

        public static bool IsPalaceCard(Card card) => !card.IsJoker && (card.Rank == Rank.Jack || card.Rank == Rank.Queen || card.Rank == Rank.King);

        public override bool IsPlayerCardGuarded(Card card) => IsPalaceCard(card);

        public override void HandSettled(HellPokerGame game, RoundResult round)
        {
            bool won = round.HouseFolded || (!round.Folded && round.Showdown?.Outcome == ShowdownOutcome.PlayerWins);
            if (won && Kit.Taxes < Kit.TaxCapacity)
            {
                Kit.AddTax();
                Kit.TaxesEarned++;
            }
            if (_bossTable && !round.Folded && round.Showdown?.Outcome == ShowdownOutcome.HouseWins) Kit.HalveTaxes();
        }

        /// <summary>Why the act cannot be done now; <paramref name="charged"/>: a seizure paid with the full gauge instead of taxes.</summary>
        public KitRefusal WhyNot(HellPokerGame game, TaxAct act, bool charged = false)
        {
            if (!BeforeTheDraw(game)) return KitRefusal.NotNow;
            if (_done.Contains(act)) return KitRefusal.AlreadyUsed;
            if (charged ? act != TaxAct.Seize || !IsCharged : Kit.Taxes < PriceOf(act)) return charged ? KitRefusal.NotCharged : KitRefusal.NoTaxes;
            switch (act)
            {
                case TaxAct.Reveal:
                    return game.HouseCardsRevealed >= Hand.Size - 1 ? KitRefusal.NoCard : KitRefusal.None;
                case TaxAct.FreeRaise:
                    return game.IsCommitted || game.RaiseForbidden || game.FreeRaiseWaiting ? KitRefusal.CannotRaise : KitRefusal.None;
                default:
                    return SeizeTargets(game).Any() && Enumerable.Range(0, Hand.Size).Any(i => CanGive(game, i)) ? KitRefusal.None : KitRefusal.NoCard;
            }
        }

        public bool Reveal(HellPokerGame game)
        {
            if (WhyNot(game, TaxAct.Reveal) != KitRefusal.None || !game.RevealNextHouseCard()) return false;
            Kit.SpendTaxes(PriceOf(TaxAct.Reveal));
            _done.Add(TaxAct.Reveal);
            Kit.Reveals++;
            return true;
        }

        public bool FreeRaise(HellPokerGame game)
        {
            if (WhyNot(game, TaxAct.FreeRaise) != KitRefusal.None || !game.GrantFreeRaise()) return false;
            Kit.SpendTaxes(PriceOf(TaxAct.FreeRaise));
            _done.Add(TaxAct.FreeRaise);
            Kit.FreeRaises++;
            return true;
        }

        /// <summary>A card of the King's he may give in a seizure (one he can see, not chained as collateral).</summary>
        public bool CanGive(HellPokerGame game, int index) =>
            index >= 0 && index < Hand.Size && index < game.PlayerCardsRevealed && !game.IsPlayerCardHidden(index) && !game.IsPlayerCardChained(index);

        /// <summary>The House's cards a seizure may take: its open ones (not a false face, not a card beyond cheats) — or, with none open,
        /// the next face-down one (the seizure turns it).</summary>
        private static IEnumerable<int> SeizeTargets(HellPokerGame game)
        {
            int[] open = Enumerable.Range(0, game.HouseCardsRevealed).Where(i => !game.IsHouseCardFalse(i) && !Cheats.CheatRules.IsImmune(game.HouseHand[i])).ToArray();
            if (open.Length > 0) return open;
            int next = game.HouseCardsRevealed;
            return next < Hand.Size - 1 && !Cheats.CheatRules.IsImmune(game.HouseHand[next]) ? new[] { next } : Array.Empty<int>();
        }

        /// <summary>The seizure: the King gives the card at <paramref name="give"/> and takes the demon's open card that serves his hand best.
        /// <paramref name="charged"/>: paid with the full gauge. Returns the card taken, or null.</summary>
        public Card? Seize(HellPokerGame game, int give, bool charged = false)
        {
            if (WhyNot(game, TaxAct.Seize, charged) != KitRefusal.None || !CanGive(game, give)) return null;
            int[] targets = SeizeTargets(game).ToArray();
            int take = targets.OrderByDescending(h => game.Evaluator.Evaluate(game.PlayerHand.With(give, game.HouseHand[h])), Comparer<HandEvaluation>.Default)
                .First();
            if (take >= game.HouseCardsRevealed) game.RevealNextHouseCard();
            Card taken = game.HouseHand[take], given = game.PlayerHand[give];
            game.SetHands(game.PlayerHand.With(give, taken), game.HouseHand.With(take, given));
            if (charged) Kit.Sinner.SpendCharge();
            else Kit.SpendTaxes(PriceOf(TaxAct.Seize));
            _done.Add(TaxAct.Seize);
            Kit.Seizures++;
            return taken;
        }
    }

    /// <summary>The Summoner's imps (each once a hand, before the draw).</summary>
    public enum SummonAct
    {
        /// <summary>One of the demon's closed cards turns.</summary>
        Eye,

        /// <summary>One of the Summoner's cards for one of the demon's open ones.</summary>
        Hand,

        /// <summary>The demon's next cheat, told truthfully (his lie, if he told one, falls).</summary>
        Whisper
    }

    /// <summary>
    /// The Summoner's imps: the Eye, the Hand and the Whisper, each once a hand before the draw. On the floors they cost coins
    /// (<see cref="CoinPrices"/>; never the purse's last), at a demon's table the bar grows a share of where it began
    /// (<see cref="BarPercents"/>, written on as the hand is settled). A summoned imp runs wild <see cref="WildPercent"/> of the time and
    /// lowers one of the Summoner's cards. The charged power, the LEGION, makes all three free for the hand.
    /// </summary>
    public sealed class SummonPowers : ClassPowers
    {
        /// <summary>A summoned imp runs wild this often (percent).</summary>
        public const int WildPercent = 10;

        /// <summary>The coins of the Eye, the Hand, the Whisper on the floors; the bar's growth at a demon's (percent of its start).</summary>
        public static readonly int[] CoinPrices = { 5, 15, 8 };
        public static readonly int[] BarPercents = { 2, 5, 3 };

        private readonly HashSet<SummonAct> _done = new HashSet<SummonAct>();
        private readonly int _barStart;
        private int _barCost;

        /// <param name="barStart">The demon's bar as his table began; 0 on the floors (the imps cost coins there).</param>
        public SummonPowers(ClassKit kit, IRandomSource random, int barStart) : base(kit, random)
        {
            _barStart = Math.Max(0, barStart);
        }

        /// <summary>The run's purse (the floors' price); set by the chapter as the table opens.</summary>
        public CoinPurse Purse { get; set; }

        public bool AtDemonsTable => _barStart > 0;

        /// <summary>The Legion is called this hand: every imp is free.</summary>
        public bool Legion { get; private set; }

        /// <summary>The Whisper's answer this hand: the cheat coming (null: none); <see cref="Whispered"/> tells whether it was asked.</summary>
        public Cheats.ICheat WhisperedCheat { get; private set; }
        public bool Whispered { get; private set; }

        /// <summary>An imp ran wild this hand: the card it lowered and what it became; null when none did.</summary>
        public Card? WildFrom { get; private set; }
        public Card? WildTo { get; private set; }

        /// <summary>The imps summoned over the run, and those that ran wild (the log, the simulation).</summary>
        public int Summons { get; private set; }
        public int WildImps { get; private set; }

        public bool DoneThisHand(SummonAct act) => _done.Contains(act);

        /// <summary>What an imp costs here: coins on the floors, years on the bar at a demon's table (0 under the Legion).</summary>
        public int PriceOf(SummonAct act) =>
            Legion ? 0 : AtDemonsTable ? Math.Max(1, _barStart * (BarPercents[(int)act] + Kit.Mark(HellPoker.Core.Relics.RelicExtras.ImpBarPrice)) / 100)
            : CoinPrices[(int)act] + Kit.Mark(HellPoker.Core.Relics.RelicExtras.ImpPrice);

        public override void HandDealt(HellPokerGame game)
        {
            _done.Clear();
            Legion = false;
            Whispered = false;
            WhisperedCheat = null;
            WildFrom = WildTo = null;
        }

        public override int YearsAfterHand(HellPokerGame game, ShowdownOutcome? outcome, bool folded, bool houseFolded) => _barCost;

        public override void HandSettled(HellPokerGame game, RoundResult round) => _barCost = 0;

        /// <summary>Why the imp cannot be summoned now.</summary>
        public KitRefusal WhyNot(HellPokerGame game, SummonAct act)
        {
            if (!BeforeTheDraw(game)) return KitRefusal.NotNow;
            if (_done.Contains(act)) return KitRefusal.AlreadyUsed;
            if (!AtDemonsTable && !Legion && (Purse == null || Purse.Coins <= PriceOf(act))) return KitRefusal.NoTaxes;
            switch (act)
            {
                case SummonAct.Eye:
                    return game.HouseCardsRevealed >= Hand.Size - 1 ? KitRefusal.NoCard : KitRefusal.None;
                case SummonAct.Hand:
                    return OpenTargets(game).Any() && Enumerable.Range(0, Hand.Size).Any(i => CanGive(game, i)) ? KitRefusal.None : KitRefusal.NoCard;
                default:
                    return KitRefusal.None;
            }
        }

        public KitRefusal WhyNoLegion(HellPokerGame game)
        {
            if (!BeforeTheDraw(game)) return KitRefusal.NotNow;
            if (Legion) return KitRefusal.AlreadyUsed;
            return IsCharged ? KitRefusal.None : KitRefusal.NotCharged;
        }

        /// <summary>The charged power: every imp free this hand.</summary>
        public bool CallLegion(HellPokerGame game)
        {
            if (WhyNoLegion(game) != KitRefusal.None || !Kit.Sinner.SpendCharge()) return false;
            Legion = true;
            return true;
        }

        /// <summary>The Eye: the demon's next closed card turns.</summary>
        public bool Eye(HellPokerGame game)
        {
            if (WhyNot(game, SummonAct.Eye) != KitRefusal.None || !game.RevealNextHouseCard()) return false;
            Summoned(game, SummonAct.Eye);
            return true;
        }

        /// <summary>The Whisper: the cheat coming, truthfully (null: none comes).</summary>
        public bool Whisper(HellPokerGame game)
        {
            if (WhyNot(game, SummonAct.Whisper) != KitRefusal.None) return false;
            WhisperedCheat = game.PendingCheatTruth;
            Whispered = true;
            Summoned(game, SummonAct.Whisper);
            return true;
        }

        /// <summary>A card of the Summoner's he may give (one he sees, not chained).</summary>
        public bool CanGive(HellPokerGame game, int index) =>
            index >= 0 && index < Hand.Size && index < game.PlayerCardsRevealed && !game.IsPlayerCardHidden(index) && !game.IsPlayerCardChained(index)
            && !game.PlayerHand[index].IsJoker;

        private static IEnumerable<int> OpenTargets(HellPokerGame game) =>
            Enumerable.Range(0, game.HouseCardsRevealed).Where(i => !game.IsHouseCardFalse(i) && !Cheats.CheatRules.IsImmune(game.HouseHand[i]));

        /// <summary>The Hand: the card at <paramref name="give"/> for the demon's open card that serves the hand best. The card taken, or null.</summary>
        public Card? Swap(HellPokerGame game, int give)
        {
            if (WhyNot(game, SummonAct.Hand) != KitRefusal.None || !CanGive(game, give)) return null;
            int take = OpenTargets(game).OrderByDescending(h => game.Evaluator.Evaluate(game.PlayerHand.With(give, game.HouseHand[h])),
                Comparer<HandEvaluation>.Default).First();
            Card taken = game.HouseHand[take], given = game.PlayerHand[give];
            game.SetHands(game.PlayerHand.With(give, taken), game.HouseHand.With(take, given));
            Summoned(game, SummonAct.Hand);
            return taken;
        }

        private void Summoned(HellPokerGame game, SummonAct act)
        {
            int price = PriceOf(act);
            if (AtDemonsTable) _barCost += price;
            else if (price > 0) Purse?.TrySpend(price);
            _done.Add(act);
            Summons++;
            Kit.Summons++;
            if (Random.Next(100) < WildPercent + Kit.Mark(HellPoker.Core.Relics.RelicExtras.ImpWild)) RunWild(game);   // the Summoner's Leash: tamer
        }

        /// <summary>The imp runs wild: one of the Summoner's cards (not a joker, not a two) drops a rank — to a card still in the deck.</summary>
        private void RunWild(HellPokerGame game)
        {
            Hand hand = game.PlayerHand;
            var deck = game.Deck.Remaining.Reverse().ToList();   // the top first
            int[] places = Enumerable.Range(0, Hand.Size).Where(i => !hand[i].IsJoker && hand[i].Rank > Rank.Two
                && deck.Contains(new Card(hand[i].Rank - 1, hand[i].Suit))).ToArray();
            if (places.Length == 0) return;
            int at = places[Random.Next(places.Length)];
            Card from = hand[at], to = new Card(from.Rank - 1, from.Suit);
            deck.Remove(to);
            game.Deck.Restore(deck);   // the lowered card leaves the deck; the old one is the imp's
            game.SetHands(hand.With(at, to), game.HouseHand);
            WildFrom = from;
            WildTo = to;
            WildImps++;
            Kit.WildImps++;
        }
    }}
