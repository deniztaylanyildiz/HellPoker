using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Cheats;
using HellPoker.Core.Dealers;
using HellPoker.Core.Draw;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;
using HellPoker.Core.Randomness;
using HellPoker.Core.Sinners;

namespace HellPoker.Core.Chapters
{
    /// <summary>
    /// A demon's own rule at their Phase 2 table (only there: the floors' imps play without one), on top of their house rules, cheats and
    /// gauge. Every rule also corners the demon: with the bar under <see cref="CorneredPercent"/> of its start, the gauge fills one more a hand.
    /// </summary>
    public abstract class DemonRule : TableHooks
    {
        /// <summary>Under this share of its start (percent) the bar corners the demon.</summary>
        public const int CorneredPercent = 25;

        protected DemonRule(int barStart, IRandomSource random)
        {
            if (barStart <= 0) throw new ArgumentOutOfRangeException(nameof(barStart));
            BarStart = barStart;
            Random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>Stable id (the words, the log): interest, role_swap, temptation, mirror.</summary>
        public abstract string Id { get; }

        public int BarStart { get; }
        protected IRandomSource Random { get; }

        /// <summary>How many times the rule struck at this table.</summary>
        public int Triggers { get; protected set; }

        /// <summary>The rule struck in the last hand (or, for a rule before the draw, in this one): what the table says about it.</summary>
        public bool StruckThisHand { get; protected set; }

        /// <summary>Hands the demon played cornered.</summary>
        public int CorneredHands { get; private set; }

        public bool IsCornered(HellPokerGame game) => (long)game.Years * 100 < (long)BarStart * CorneredPercent;

        public override int ExtraMalicePerHand(HellPokerGame game)
        {
            if (!IsCornered(game)) return 0;
            CorneredHands++;
            return 1;
        }

        public override void HandDealt(HellPokerGame game) => StruckThisHand = false;

        /// <summary>The rule for a demon's table (Lucifer's mirrors the class); null for a demon without one.</summary>
        public static DemonRule For(string bossId, int barStart, ClassKit kit, IRandomSource random)
        {
            switch (bossId)
            {
                case DealerRoster.MammonId: return new MammonInterest(barStart, random);
                case DealerRoster.BelialId: return new BelialRoleSwap(barStart, random);
                case DealerRoster.LilithId: return new LilithTemptation(barStart, random);
                case DealerRoster.LuciferId: return new LuciferMirror(barStart, random, kit);
                default: return null;
            }
        }

        /// <summary>The demon's weakest card for <paramref name="card"/>: the position whose swap makes the House's hand the best; -1 when no
        /// swap betters it.</summary>
        protected static int BestPlaceFor(HellPokerGame game, Hand house, Card card, Func<int, bool> allowed = null)
        {
            HandEvaluation now = game.Evaluator.Evaluate(house);
            int best = -1;
            HandEvaluation bestEval = now;
            for (int i = 0; i < Hand.Size; i++)
            {
                if (CheatRules.IsImmune(house[i]) || (allowed != null && !allowed(i))) continue;
                HandEvaluation eval = game.Evaluator.Evaluate(house.With(i, card));
                if (eval.CompareTo(bestEval) <= 0) continue;
                bestEval = eval;
                best = i;
            }
            return best;
        }

        /// <summary>A player's card a rule may move: not a joker or a Dead Man's card, not guarded (the King's palace), not chained, not hidden.</summary>
        protected static bool Movable(HellPokerGame game, int index) =>
            !CheatRules.IsImmune(game.PlayerHand[index]) && !(game.Hooks?.IsPlayerCardGuarded(game.PlayerHand[index]) ?? false)
            && !game.IsPlayerCardChained(index) && !game.IsPlayerCardHidden(index);
    }

    /// <summary>Mammon's INTEREST: every hand the player does not win (a loss, a fold, a push) the bar grows <see cref="InterestPercent"/> of its start.</summary>
    public sealed class MammonInterest : DemonRule
    {
        /// <summary>The interest, in percent of the bar's start (a tuning knob).</summary>
        public static int InterestPercent = 2;

        public MammonInterest(int barStart, IRandomSource random) : base(barStart, random)
        {
        }

        public override string Id => "interest";

        public int Interest => Math.Max(1, (BarStart * InterestPercent + 99) / 100);

        public override int YearsAfterHand(HellPokerGame game, ShowdownOutcome? outcome, bool folded, bool houseFolded)
        {
            StruckThisHand = false;
            if (houseFolded || (!folded && outcome == ShowdownOutcome.PlayerWins)) return 0;
            StruckThisHand = true;
            Triggers++;
            return Interest;
        }
    }

    /// <summary>
    /// Belial's ROLE SWAP: every <see cref="Every"/>th hand, as the draw begins, Belial puts one of his face-down cards in place of one of the
    /// player's (picked at random) and takes that card. The player sees the card he got. The Warlock sees it coming a hand early.
    /// </summary>
    public sealed class BelialRoleSwap : DemonRule
    {
        public const int Every = 4;

        public BelialRoleSwap(int barStart, IRandomSource random) : base(barStart, random)
        {
        }

        public override string Id => "role_swap";

        /// <summary>The card the player got in this hand's swap (and the one he lost); null when none was made.</summary>
        public Card? Given { get; private set; }
        public Card? Taken { get; private set; }

        /// <summary>The swap comes in hand <paramref name="round"/>.</summary>
        public static bool SwapIn(int round) => round > 0 && round % Every == 0;

        /// <summary>The coming hand (or, between hands, the next) is a swap's: what the Warlock sees.</summary>
        public static bool ComingIn(HellPokerGame game) => SwapIn(game.RoundNumber + 1);

        public override void HandDealt(HellPokerGame game)
        {
            base.HandDealt(game);
            Given = Taken = null;
        }

        public override void DrawBegins(HellPokerGame game)
        {
            if (!SwapIn(game.RoundNumber)) return;
            int[] mine = Enumerable.Range(0, Hand.Size).Where(i => Movable(game, i)).ToArray();
            int[] his = Enumerable.Range(game.HouseCardsRevealed, Hand.Size - game.HouseCardsRevealed)
                .Where(i => !CheatRules.IsImmune(game.HouseHand[i]) && !game.IsHouseCardFalse(i)).ToArray();
            if (mine.Length == 0 || his.Length == 0) return;
            int p = mine[Random.Next(mine.Length)], h = his[Random.Next(his.Length)];
            Taken = game.PlayerHand[p];
            Given = game.HouseHand[h];
            game.SetHands(game.PlayerHand.With(p, Given.Value), game.HouseHand.With(h, Taken.Value));
            StruckThisHand = true;
            Triggers++;
        }
    }

    /// <summary>
    /// Lilith's TEMPTATION: in the draw, when a card the player threw away would better Lilith's hand, she takes it
    /// <see cref="TakePercent"/> of the time and throws her weakest card. The card she kept shows marked at the showdown.
    /// </summary>
    public sealed class LilithTemptation : DemonRule
    {
        /// <summary>How often a tempting card is taken (percent; a tuning knob).</summary>
        public static int TakePercent = 50;

        public LilithTemptation(int barStart, IRandomSource random) : base(barStart, random)
        {
        }

        public override string Id => "temptation";

        /// <summary>The player's thrown card Lilith kept this hand; null when she took none.</summary>
        public Card? Tempted { get; private set; }

        public override void HandDealt(HellPokerGame game)
        {
            base.HandDealt(game);
            Tempted = null;
        }

        public override void HandDrawn(HellPokerGame game, ExchangeResult player, ExchangeResult house)
        {
            Hand hand = game.HouseHand;
            Card? best = null;
            int place = -1;
            HandEvaluation bestEval = game.Evaluator.Evaluate(hand);
            foreach (Card thrown in player.Discarded.Where(c => !c.IsJoker))
            {
                int at = BestPlaceFor(game, hand, thrown);
                if (at < 0) continue;
                HandEvaluation eval = game.Evaluator.Evaluate(hand.With(at, thrown));
                if (eval.CompareTo(bestEval) <= 0) continue;
                bestEval = eval;
                best = thrown;
                place = at;
            }
            if (!best.HasValue || Random.Next(100) >= TakePercent) return;
            game.SetHands(null, hand.With(place, best.Value));
            Tempted = best;
            StruckThisHand = true;
            Triggers++;
        }
    }

    /// <summary>
    /// Lucifer's MIRROR: he plays the player's own class mechanic against them. Against the Jester the jokers are his too (as ever);
    /// against the Peasant a satchel of one card; against the Warlock he sees the deck's top card and draws by it; against the King he
    /// takes a tax for every hand he wins and at <see cref="ClassKit.SeizePrice"/> seizes one of the King's cards (the palace holds).
    /// </summary>
    public sealed class LuciferMirror : DemonRule
    {
        private readonly ClassKit _kit;

        public LuciferMirror(int barStart, IRandomSource random, ClassKit kit) : base(barStart, random)
        {
            _kit = kit ?? throw new ArgumentNullException(nameof(kit));
        }

        public override string Id => "mirror";

        public KitKind Mirrors => _kit.Kind;

        /// <summary>The card Lucifer seized this hand (and gave); null when none.</summary>
        public Card? Seized { get; private set; }
        public Card? GivenBack { get; private set; }

        /// <summary>Against the Warlock: this hand he sent the top card to the bottom (rather than drawing it).</summary>
        public bool SentDown { get; private set; }

        public override IEnumerable<Card> CardsOutOfDeck => _kit.Kind == KitKind.Satchel && _kit.LuciferBag.HasValue
            ? new[] { _kit.LuciferBag.Value } : Enumerable.Empty<Card>();

        public override void HandDealt(HellPokerGame game)
        {
            base.HandDealt(game);
            Seized = GivenBack = null;
            SentDown = false;
        }

        // ---- against the Peasant: a satchel of one

        public override IReadOnlyDictionary<int, Card> HouseTakesFromOutside(HellPokerGame game, IReadOnlyList<int> discards)
        {
            if (_kit.Kind != KitKind.Satchel || !_kit.LuciferBag.HasValue || discards.Count == 0) return null;
            Card bag = _kit.LuciferBag.Value;
            int at = BestPlaceFor(game, game.HouseHand, bag, discards.Contains);
            if (at < 0) return null;
            _kit.LuciferBag = null;
            StruckThisHand = true;
            Triggers++;
            return new Dictionary<int, Card> { [at] = bag };
        }

        public override void HandDrawn(HellPokerGame game, ExchangeResult player, ExchangeResult house)
        {
            if (_kit.Kind != KitKind.Satchel || _kit.LuciferBag.HasValue) return;
            Card[] thrown = house.Discarded.Where(c => !c.IsJoker && !game.PlayerHand.Contains(c)).ToArray();
            if (thrown.Length > 0) _kit.LuciferBag = thrown.OrderByDescending(c => c.Rank).First();
        }

        // ---- against the Warlock: he sees the top card

        public override IReadOnlyList<int> AdjustHouseDiscards(HellPokerGame game, IReadOnlyList<int> discards)
        {
            if (_kit.Kind != KitKind.Prophecy || game.Deck.Count < 2) return discards;
            Card top = game.Deck.Remaining[game.Deck.Remaining.Count - 1];
            if (top.IsJoker) return discards;
            int at = BestPlaceFor(game, game.HouseHand, top, discards.Count > 0 ? (Func<int, bool>)discards.Contains : null);
            if (discards.Count == 0)
            {
                // Standing pat, he draws the top card when it serves him.
                if (at < 0) return discards;
                StruckThisHand = true;
                Triggers++;
                return new[] { at };
            }
            if (at >= 0) return discards;
            // Drawing, and the top card would not serve him: it goes to the bottom (his own rearranging).
            SentDown = true;
            List<Card> order = game.Deck.Remaining.Reverse().ToList();
            order.RemoveAt(0);
            order.Add(top);
            game.Deck.Restore(order);
            StruckThisHand = true;
            Triggers++;
            return discards;
        }

        // ---- against the King: taxes, and a seizure

        public override void HandSettled(HellPokerGame game, RoundResult round)
        {
            if (_kit.Kind == KitKind.Treasury && !round.Folded && !round.HouseFolded && round.Showdown?.Outcome == ShowdownOutcome.HouseWins)
                _kit.LuciferTaxes = Math.Min(ClassKit.TaxMax, _kit.LuciferTaxes + 1);
        }

        public override void DrawBegins(HellPokerGame game)
        {
            if (_kit.Kind != KitKind.Treasury || _kit.LuciferTaxes < ClassKit.SeizePrice) return;
            Hand house = game.HouseHand;
            int bestMine = -1, bestHis = -1;
            HandEvaluation bestEval = game.Evaluator.Evaluate(house);
            for (int p = 0; p < Hand.Size; p++)
            {
                if (!Movable(game, p)) continue;
                int at = BestPlaceFor(game, house, game.PlayerHand[p], i => i >= game.HouseCardsRevealed);
                if (at < 0) continue;
                HandEvaluation eval = game.Evaluator.Evaluate(house.With(at, game.PlayerHand[p]));
                if (eval.CompareTo(bestEval) <= 0) continue;
                bestEval = eval;
                bestMine = p;
                bestHis = at;
            }
            if (bestMine < 0) return;
            Seized = game.PlayerHand[bestMine];
            GivenBack = house[bestHis];
            game.SetHands(game.PlayerHand.With(bestMine, GivenBack.Value), house.With(bestHis, Seized.Value));
            _kit.LuciferTaxes -= ClassKit.SeizePrice;
            StruckThisHand = true;
            Triggers++;
        }
    }
}
