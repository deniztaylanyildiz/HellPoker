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

        /// <summary>Not enough taxes (or, for an imp on the floors, coins).</summary>
        NoTaxes,

        /// <summary>Done once already this hand.</summary>
        AlreadyUsed,

        /// <summary>No card to do it with (no face-down card to turn, no card the act may take).</summary>
        NoCard,

        /// <summary>No raise can be made this hand.</summary>
        CannotRaise,

        /// <summary>Not open yet: the Summoner's Hand and Whisper wait for his relics (the power's level).</summary>
        Locked
    }

    /// <summary>
    /// The class's part of a Phase 2 table: its main power (the satchel, FORESIGHT, the treasury, the imps, the steal) and its passive,
    /// which works on its own (Demo 2.3: nothing is pressed for — the old K powers, the harvest, the bending of fate, the charged seizure
    /// and the Legion, are gone). The demo's powers and gauge stay the demo's.
    /// </summary>
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

        /// <summary>The class's passive worked in this hand (the badge glows, the log says it).</summary>
        public bool PassiveThisHand { get; private set; }

        /// <summary>The passive worked: once a hand it counts for the run.</summary>
        protected void PassiveWorked()
        {
            if (!PassiveThisHand) Kit.Passives++;
            PassiveThisHand = true;
        }

        public sealed override void HandDealt(HellPokerGame game)
        {
            PassiveThisHand = false;
            Dealt(game);
        }

        public sealed override void HandSettled(HellPokerGame game, RoundResult round) => Settled(game, round);

        /// <summary>A hand was dealt (the class's own part).</summary>
        protected virtual void Dealt(HellPokerGame game) { }

        /// <summary>The hand was settled (the class's own part).</summary>
        protected virtual void Settled(HellPokerGame game, RoundResult round) { }

        /// <summary>Before the draw is done: the hand is being played and the draw still to come.</summary>
        protected static bool BeforeTheDraw(HellPokerGame game) =>
            game.HandInPlay && !game.IsAfterDraw && (game.Phase == GamePhase.PlayerReveal || game.Phase == GamePhase.Drawing);
    }

    /// <summary>
    /// The Peasant's satchel. After the draw one of the thrown cards may go into it (<see cref="Keep"/>); it holds
    /// <see cref="ClassKit.SatchelCapacity"/> (its level), the oldest falls out. In a draw a thrown place may take a satchel card instead of
    /// the deck's (<see cref="HellPokerGame.Draw(IReadOnlyCollection{int}, IReadOnlyDictionary{int, Card})"/>). The passive, the SILENT
    /// FIELD: the hand's first satchel card goes unseen; a second one the demon sees — that hand his re-raise comes
    /// <see cref="ClassKit.SatchelRisk"/> more often.
    /// </summary>
    public sealed class SatchelPowers : ClassPowers
    {
        private readonly List<Card> _offered = new List<Card>();
        private bool _seen;
        private int _kept, _taken;

        public SatchelPowers(ClassKit kit, IRandomSource random) : base(kit, random)
        {
        }

        public override IEnumerable<Card> CardsOutOfDeck => Kit.Satchel;

        /// <summary>The cards thrown in this hand's draw that may still go into the satchel (one of them, until the next deal).</summary>
        public IReadOnlyList<Card> Offered => _kept >= Kit.KeepsPerHand ? (IReadOnlyList<Card>)Array.Empty<Card>() : _offered;

        /// <summary>The demon saw the satchel opened this hand (a second card taken from it).</summary>
        public bool SeenThisHand => _seen;

        /// <summary>The satchel card the Silent Field kept from the demon's eyes this hand; null when none was taken.</summary>
        public Card? SilentCard { get; private set; }

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

        protected override void Dealt(HellPokerGame game)
        {
            _offered.Clear();
            _seen = false;
            _kept = 0;
            _taken = 0;
            SilentCard = null;
        }

        public override bool CanTakeFromOutside(HellPokerGame game, Card card) => Kit.Satchel.Contains(card);

        public override void TookFromOutside(HellPokerGame game, IReadOnlyCollection<Card> cards)
        {
            if (cards == null || cards.Count == 0) return;
            foreach (Card card in cards)
                if (Kit.TakeOut(card)) Kit.TakenFromSatchel++;
            if (_taken == 0)
            {
                // The Silent Field: the hand's first satchel card goes unseen.
                SilentCard = cards.First();
                PassiveWorked();
            }
            _taken += cards.Count;
            if (_taken >= 2) _seen = true;
        }

        public override void HandDrawn(HellPokerGame game, ExchangeResult player, ExchangeResult house)
        {
            _offered.Clear();
            // A thrown card the demon took up (Lilith's temptation) is no longer there to keep.
            _offered.AddRange(player.Discarded.Where(c => !c.IsJoker && !game.HouseHand.Contains(c)));
        }

        public override bool HouseReRaises(HellPokerGame game, bool wants) =>
            wants || (_seen && Random.Next(100) < Kit.SatchelRisk);
    }

    /// <summary>
    /// The Oracle's sight (the class id is still "warlock"). FORESIGHT: after the deal he sees the deck's top cards
    /// (<see cref="ClassKit.ProphecyCards"/>, its level), the ones his draw will take; nothing moves them. His passive is his old one: he sees
    /// the demons' lies and the swindlers' traps. The price, the MIRROR CURSE: every hand the demon knows one of his cards, and bets and
    /// draws by it (the Fate Weaver: the Oracle sees which).
    /// </summary>
    public sealed class ProphecyPowers : ClassPowers
    {
        /// <summary>The demon knowing a low card bullies: his re-raise comes this much (percent) more often (the Seer's Spectacles: more).</summary>
        public const int BullyPercent = 20;

        public ProphecyPowers(ClassKit kit, IRandomSource random) : base(kit, random)
        {
        }

        /// <summary>The card of the Oracle's the demon knows this hand; null between hands.</summary>
        public Card? KnownCard { get; private set; }

        /// <summary>The Fate Weaver: the Oracle sees which of his cards the demon knows.</summary>
        public bool ShowsKnownCard => Kit.Has(UpgradeIds.Thrice);

        /// <summary>The deck's top cards as the Oracle sees them, the next one first — the cards his draw will take. Only while the
        /// prophecy is open (<see cref="IsOpen"/>: dealt, the draw still to come); empty otherwise.</summary>
        public IReadOnlyList<Card> TopCards(HellPokerGame game) =>
            IsOpen(game) ? game.Deck.Remaining.Reverse().Take(Kit.ProphecyCards).ToArray() : Array.Empty<Card>();

        /// <summary>The prophecy is open: a hand is dealt and its draw is still to come (between hands and after the draw it is shut).</summary>
        public static bool IsOpen(HellPokerGame game) => BeforeTheDraw(game);

        protected override void Dealt(HellPokerGame game)
        {
            // The Blind Mirror: half the hands the demon learns nothing (the dice are only asked with the upgrade).
            KnownCard = Kit.DemonKnowsPercent >= 100 || Random.Next(100) < Kit.DemonKnowsPercent ? game.PlayerHand[Random.Next(Hand.Size)] : (Card?)null;
        }

        protected override void Settled(HellPokerGame game, RoundResult round) => KnownCard = null;

        /// <summary>The demon knows a high card: with less than two pair he does not raise back; a low one: he bullies.</summary>
        public override bool HouseReRaises(HellPokerGame game, bool wants)
        {
            if (!KnownCard.HasValue || KnownCard.Value.IsJoker) return wants;
            if (KnownCard.Value.Rank >= Rank.Jack)
                return wants && game.Evaluator.Evaluate(game.HouseHand).Category >= HandCategory.TwoPair;
            if (KnownCard.Value.Rank <= Rank.Nine && !wants)
                return Random.Next(100) < BullyPercent + Kit.Mark(HellPoker.Core.Relics.RelicExtras.ProphecyBully);
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
    /// The King's treasury: a tax for every hand he wins (at most <see cref="ClassKit.TaxCapacity"/>, its level), spent before the draw on
    /// three acts (<see cref="TaxAct"/>). His palace: no cheat — and no demon's rule — may touch a jack, queen or king in his hand. His
    /// passive, the ROYAL DISCOUNT: a hand's seizure costs half (<see cref="ClassKit.PriceOf"/>). The price: a demon's hand lost takes half
    /// the taxes.
    /// </summary>
    public sealed class TreasuryPowers : ClassPowers
    {
        private readonly HashSet<TaxAct> _done = new HashSet<TaxAct>();
        private readonly bool _bossTable;

        public TreasuryPowers(ClassKit kit, IRandomSource random, bool bossTable) : base(kit, random)
        {
            _bossTable = bossTable;
        }

        /// <summary>The taxes an act costs (the seizure under the royal discount).</summary>
        public int PriceOf(TaxAct act) => Kit.PriceOf(act);

        public bool DoneThisHand(TaxAct act) => _done.Contains(act);

        protected override void Dealt(HellPokerGame game) => _done.Clear();

        public static bool IsPalaceCard(Card card) => !card.IsJoker && (card.Rank == Rank.Jack || card.Rank == Rank.Queen || card.Rank == Rank.King);

        public override bool IsPlayerCardGuarded(Card card) => IsPalaceCard(card);

        protected override void Settled(HellPokerGame game, RoundResult round)
        {
            bool won = round.HouseFolded || (!round.Folded && round.Showdown?.Outcome == ShowdownOutcome.PlayerWins);
            if (won && Kit.Taxes < Kit.TaxCapacity)
            {
                Kit.AddTax();
                Kit.TaxesEarned++;
            }
            if (_bossTable && !round.Folded && round.Showdown?.Outcome == ShowdownOutcome.HouseWins) Kit.HalveTaxes();
        }

        /// <summary>Why the act cannot be done now.</summary>
        public KitRefusal WhyNot(HellPokerGame game, TaxAct act)
        {
            if (!BeforeTheDraw(game)) return KitRefusal.NotNow;
            if (_done.Contains(act)) return KitRefusal.AlreadyUsed;
            if (Kit.Taxes < PriceOf(act)) return KitRefusal.NoTaxes;
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

        /// <summary>The seizure (at the royal discount): the King gives the card at <paramref name="give"/> and takes the demon's open card
        /// that serves his hand best. Returns the card taken, or null.</summary>
        public Card? Seize(HellPokerGame game, int give)
        {
            if (WhyNot(game, TaxAct.Seize) != KitRefusal.None || !CanGive(game, give)) return null;
            int[] targets = SeizeTargets(game).ToArray();
            int take = targets.OrderByDescending(h => game.Evaluator.Evaluate(game.PlayerHand.With(give, game.HouseHand[h])), Comparer<HandEvaluation>.Default)
                .First();
            if (take >= game.HouseCardsRevealed) game.RevealNextHouseCard();
            Card taken = game.HouseHand[take], given = game.PlayerHand[give];
            game.SetHands(game.PlayerHand.With(give, taken), game.HouseHand.With(take, given));
            Kit.SpendTaxes(PriceOf(TaxAct.Seize));
            _done.Add(TaxAct.Seize);
            Kit.Seizures++;
            PassiveWorked();   // the royal discount paid the other half
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
    /// The Summoner's imps: the Eye from the start, the Hand and the Whisper with his relics (<see cref="ClassKit.ImpsOpen"/>), each once a
    /// hand before the draw. On the floors they cost coins (<see cref="CoinPrices"/>; never the purse's last), at a demon's table the bar
    /// grows a share of where it began (<see cref="BarPercents"/>, written on as the hand is settled); at the highest level half. A summoned
    /// imp runs wild <see cref="WildPercent"/> of the time and lowers one of the Summoner's cards. The passive, the LOYAL IMP: the hand's
    /// first imp comes for nothing (<see cref="ClassKit.FreeImpsPerHand"/>).
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
        private int _barCost, _freeLeft;

        /// <param name="barStart">The demon's bar as his table began; 0 on the floors (the imps cost coins there).</param>
        public SummonPowers(ClassKit kit, IRandomSource random, int barStart) : base(kit, random)
        {
            _barStart = Math.Max(0, barStart);
        }

        /// <summary>The run's purse (the floors' price); set by the chapter as the table opens.</summary>
        public CoinPurse Purse { get; set; }

        public bool AtDemonsTable => _barStart > 0;

        /// <summary>Imps the loyal imp still frees this hand.</summary>
        public int FreeLeft => _freeLeft;

        /// <summary>The imp the loyal imp freed first this hand (the log's line); null when none was.</summary>
        public SummonAct? FreedThisHand { get; private set; }

        /// <summary>The Whisper's answer this hand: the cheat coming (null: none); <see cref="Whispered"/> tells whether it was asked.</summary>
        public Cheats.ICheat WhisperedCheat { get; private set; }
        public bool Whispered { get; private set; }

        /// <summary>An imp ran wild this hand: the card it lowered and what it became; null when none did. <see cref="WildOnDemon"/>: the
        /// Blood Oath turned it on one of the demon's cards.</summary>
        public Card? WildFrom { get; private set; }
        public Card? WildTo { get; private set; }
        public bool WildOnDemon { get; private set; }

        /// <summary>The imps summoned over the run, and those that ran wild (the log, the simulation).</summary>
        public int Summons { get; private set; }
        public int WildImps { get; private set; }

        public bool DoneThisHand(SummonAct act) => _done.Contains(act);

        /// <summary>The imp is open to the Summoner (the Hand and the Whisper wait for his relics).</summary>
        public bool IsOpen(SummonAct act) => (int)act < Kit.ImpsOpen;

        /// <summary>What an imp costs here now: nothing while the loyal imp frees one, else its full price.</summary>
        public int PriceOf(SummonAct act) => _freeLeft > 0 ? 0 : FullPriceOf(act);

        /// <summary>An imp's own price: coins on the floors, years on the bar at a demon's table (half at the imps' highest level).</summary>
        public int FullPriceOf(SummonAct act) =>
            AtDemonsTable ? Math.Max(1, _barStart * BarPercentOf(act) / 100)
            : Half(CoinPrices[(int)act] + Kit.Mark(HellPoker.Core.Relics.RelicExtras.ImpPrice));

        /// <summary>An imp's price at a demon's table, in percent of the bar's start (the relics' marks counted, halved at the highest level:
        /// what the button says).</summary>
        public int BarPercentOf(SummonAct act) => Half(BarPercents[(int)act] + Kit.Mark(HellPoker.Core.Relics.RelicExtras.ImpBarPrice));

        private int Half(int price) => Kit.ImpsHalfPrice ? Math.Max(1, (price + 1) / 2) : price;

        protected override void Dealt(HellPokerGame game)
        {
            _done.Clear();
            _freeLeft = Kit.FreeImpsPerHand;
            FreedThisHand = null;
            Whispered = false;
            WhisperedCheat = null;
            WildFrom = WildTo = null;
            WildOnDemon = false;
        }

        public override int YearsAfterHand(HellPokerGame game, ShowdownOutcome? outcome, bool folded, bool houseFolded) => _barCost;

        protected override void Settled(HellPokerGame game, RoundResult round) => _barCost = 0;

        /// <summary>Why the imp cannot be summoned now.</summary>
        public KitRefusal WhyNot(HellPokerGame game, SummonAct act)
        {
            if (!IsOpen(act)) return KitRefusal.Locked;
            if (!BeforeTheDraw(game)) return KitRefusal.NotNow;
            if (_done.Contains(act)) return KitRefusal.AlreadyUsed;
            // On the floors an imp the loyal imp does not free costs coins, never the purse's last.
            int price = PriceOf(act);
            if (!AtDemonsTable && price > 0 && (Purse == null || Purse.Coins <= price)) return KitRefusal.NoTaxes;
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

        /// <summary>The Eye: the demon's next closed card turns (the Keen Eye: two).</summary>
        public bool Eye(HellPokerGame game)
        {
            if (WhyNot(game, SummonAct.Eye) != KitRefusal.None || !game.RevealNextHouseCard()) return false;
            if (Kit.Has(UpgradeIds.KeenEye) && game.HouseCardsRevealed < Hand.Size - 1) game.RevealNextHouseCard();
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
            if (_freeLeft > 0)
            {
                _freeLeft--;
                if (FreedThisHand == null) FreedThisHand = act;
                PassiveWorked();   // the loyal imp
            }
            if (AtDemonsTable) _barCost += price;
            else if (price > 0) Purse?.TrySpend(price);
            _done.Add(act);
            Summons++;
            Kit.Summons++;
            if (!Kit.Has(UpgradeIds.TameImps) && Random.Next(100) < WildPercent) RunWild(game);
        }

        /// <summary>The imp runs wild: one of the Summoner's cards (the Blood Oath: the demon's; not a joker, not a two) drops a rank — to a
        /// card still in the deck.</summary>
        private void RunWild(HellPokerGame game)
        {
            bool demon = Kit.Has(UpgradeIds.BloodOath);
            Hand hand = demon ? game.HouseHand : game.PlayerHand;
            var deck = game.Deck.Remaining.Reverse().ToList();   // the top first
            int[] places = Enumerable.Range(0, Hand.Size).Where(i => !hand[i].IsJoker && hand[i].Rank > Rank.Two
                && !(demon && Cheats.CheatRules.IsImmune(hand[i])) && deck.Contains(new Card(hand[i].Rank - 1, hand[i].Suit))).ToArray();
            if (places.Length == 0) return;
            int at = places[Random.Next(places.Length)];
            Card from = hand[at], to = new Card(from.Rank - 1, from.Suit);
            deck.Remove(to);
            game.Deck.Restore(deck);   // the lowered card leaves the deck; the old one is the imp's
            if (demon) game.SetHands(game.PlayerHand, hand.With(at, to));
            else game.SetHands(hand.With(at, to), game.HouseHand);
            WildFrom = from;
            WildTo = to;
            WildOnDemon = demon;
            WildImps++;
            Kit.WildImps++;
        }
    }

    /// <summary>
    /// The Thief's quick hand. His glance (one of the demon's face-down cards plays face up from the deal) is set as the table opens
    /// (<see cref="ChapterHooks.Attach"/>). His main power, the STEAL, once a hand from the deal until the showdown begins (Demo 2.3: after
    /// the draw too — its bets and a re-raise to answer): one of his cards for one of the demon's face-down ones, blind (the Master Thief
    /// takes the best open one when there is one). A sealed hand has no moment after its draw (its decisions pass by themselves): there
    /// the steal is shut after the draw. Caught (<see cref="ClassKit.ThiefCaughtPercent"/>, its level) nothing changes hands and he pays a
    /// fine: an ante's coins on the floors (never the purse's last), <see cref="FineBarPercent"/> of the bar at a demon's table.
    /// </summary>
    public sealed class TheftPowers : ClassPowers
    {
        /// <summary>A demon's fine for a caught Thief, in percent of his bar's start; the Loot Sack's coins for a steal on the floors.</summary>
        public static int FineBarPercent = 3, LootCoins = 5;

        private readonly int _barStart;
        private int _barCost;

        public TheftPowers(ClassKit kit, IRandomSource random, int barStart) : base(kit, random)
        {
            _barStart = Math.Max(0, barStart);
        }

        /// <summary>The run's purse (the floors' fines and loot); set by the chapter as the table opens.</summary>
        public CoinPurse Purse { get; set; }

        public bool AtDemonsTable => _barStart > 0;

        /// <summary>This hand's steal: the card taken and the card given; null when none was made.</summary>
        public Card? Taken { get; private set; }
        public Card? Given { get; private set; }

        /// <summary>The steal was tried this hand (taken or caught): once a hand.</summary>
        public bool TriedThisHand { get; private set; }

        /// <summary>Caught this hand, and the fine paid (coins on the floors, bar years at a demon's).</summary>
        public bool CaughtThisHand { get; private set; }
        public int Fine { get; private set; }

        protected override void Dealt(HellPokerGame game)
        {
            Taken = Given = null;
            TriedThisHand = false;
            CaughtThisHand = false;
            Fine = 0;
        }

        public override int YearsAfterHand(HellPokerGame game, ShowdownOutcome? outcome, bool folded, bool houseFolded) => _barCost;

        protected override void Settled(HellPokerGame game, RoundResult round) => _barCost = 0;

        /// <summary>A moment of the hand the steal may come in: from the deal until the showdown begins (the cards still face down).</summary>
        public static bool InTheHand(HellPokerGame game)
        {
            if (!game.HandInPlay) return false;
            switch (game.Phase)
            {
                case GamePhase.PlayerReveal:
                case GamePhase.Drawing:
                case GamePhase.DrawReveal:
                case GamePhase.HouseReRaise:
                case GamePhase.HouseReveal:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>A card of the Thief's he may give (one he sees, not chained, not a joker).</summary>
        public bool CanGive(HellPokerGame game, int index) =>
            index >= 0 && index < Hand.Size && index < game.PlayerCardsRevealed && !game.IsPlayerCardHidden(index) && !game.IsPlayerCardChained(index)
            && !game.PlayerHand[index].IsJoker;

        /// <summary>The demon's cards a steal may take: his face-down ones (and, for the Master Thief, his open ones) — never a false face or a
        /// card beyond cheats.</summary>
        private IEnumerable<int> Targets(HellPokerGame game, bool open) =>
            Enumerable.Range(0, Hand.Size).Where(i => (open ? i < game.HouseCardsRevealed : i >= game.HouseCardsRevealed)
                && !game.IsHouseCardFalse(i) && !Cheats.CheatRules.IsImmune(game.HouseHand[i]));

        public KitRefusal WhyNoSteal(HellPokerGame game)
        {
            if (!InTheHand(game)) return KitRefusal.NotNow;
            if (TriedThisHand) return KitRefusal.AlreadyUsed;
            if (!Targets(game, false).Any() && !(Kit.Has(UpgradeIds.MasterThief) && Targets(game, true).Any())) return KitRefusal.NoCard;
            if (!Enumerable.Range(0, Hand.Size).Any(i => CanGive(game, i))) return KitRefusal.NoCard;
            return KitRefusal.None;
        }

        /// <summary>The fine a caught Thief pays here.</summary>
        public int FineNow(HellPokerGame game) => AtDemonsTable ? Math.Max(1, _barStart * FineBarPercent / 100) : Math.Max(1, game.Ante);

        /// <summary>The STEAL: the card at <paramref name="give"/> for one of the demon's. True when the hand's steal was made (look at
        /// <see cref="CaughtThisHand"/>: caught, nothing changed hands); false when it could not be done.</summary>
        public bool Steal(HellPokerGame game, int give)
        {
            if (WhyNoSteal(game) != KitRefusal.None || !CanGive(game, give)) return false;
            TriedThisHand = true;
            if (Random.Next(100) < Kit.ThiefCaughtPercent)
            {
                CaughtThisHand = true;
                Fine = FineNow(game);
                if (AtDemonsTable) _barCost += Fine;
                else if (Purse != null)
                {
                    Fine = Math.Min(Fine, Math.Max(0, Purse.Coins - 1));   // never the purse's last coin
                    Purse.TrySpend(Fine);
                }
                Kit.Caught++;
                return true;
            }
            int[] open = Kit.Has(UpgradeIds.MasterThief) ? Targets(game, true).ToArray() : Array.Empty<int>();
            int take;
            if (open.Length > 0)
                take = open.OrderByDescending(h => game.Evaluator.Evaluate(game.PlayerHand.With(give, game.HouseHand[h])), Comparer<HandEvaluation>.Default).First();
            else
            {
                int[] blind = Targets(game, false).ToArray();
                take = blind[Random.Next(blind.Length)];
            }
            Taken = game.HouseHand[take];
            Given = game.PlayerHand[give];
            game.SetHands(game.PlayerHand.With(give, Taken.Value), game.HouseHand.With(take, Given.Value));
            Kit.Steals++;
            if (game.IsAfterDraw) Kit.StealsAfterDraw++;
            if (!AtDemonsTable && Kit.Has(UpgradeIds.LootSack)) Purse?.Add(LootCoins);
            return true;
        }
    }
}
