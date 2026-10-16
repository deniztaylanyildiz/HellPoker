using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Cards;
using HellPoker.Core.Chapters;
using HellPoker.Core.Evaluation;
using HellPoker.Core.Game;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// The simulations' player with Phase 2's class mechanics, by simple rules (Demo 2.3: nothing is pressed for — the passives work on
    /// their own): the Peasant draws a satchel card that betters the hand and keeps the highest thrown card; the Oracle draws by what
    /// FORESIGHT shows; the King seizes when he can (at the royal discount), else turns a card with taxes to spare; the Summoner spends
    /// his free imp and buys the Hand when it pays; the Thief steals after the draw (before it only in a sealed hand).
    /// </summary>
    internal static class KitPlayer
    {
        /// <summary>Before the draw is made (every card seen): the King's acts, the imps, the Thief's steal in a sealed hand.</summary>
        public static void BeforeDraw(HellPokerGame game, ChapterHooks kit)
        {
            if (kit?.Summon != null && game.Phase == GamePhase.Drawing) Summon(game, kit.Summon);
            // The Thief: a sealed hand has no moment after its draw — he steals now, with less than two pair.
            if (kit?.Theft != null && game.Phase == GamePhase.Drawing && game.IsCommitted
                && game.Evaluator.Evaluate(game.PlayerHand).Category < HandCategory.TwoPair)
                Steal(game, kit.Theft);
            TreasuryPowers treasury = kit?.Treasury;
            if (treasury == null || game.Phase != GamePhase.Drawing) return;
            int give = WorstCard(game, i => treasury.CanGive(game, i));
            if (give >= 0 && treasury.WhyNot(game, TaxAct.Seize) == KitRefusal.None) treasury.Seize(game, give);
            // A card turned for a tax only with taxes to spare: the rest wait for a seizure.
            else if (kit.Kit.Taxes > treasury.PriceOf(TaxAct.Seize) && treasury.WhyNot(game, TaxAct.Reveal) == KitRefusal.None) treasury.Reveal(game);
        }

        /// <summary>
        /// After the draw, at the hand's next decision (a bet or a re-raise to answer): the Thief steals with a hand that his loneliest card
        /// does not make (trips or less), one of his cards for one of the demon's face-down ones (Demo 2.3).
        /// </summary>
        public static void AfterDrawDecision(HellPokerGame game, ChapterHooks kit)
        {
            if (kit?.Theft == null || !game.IsAfterDraw || kit.Theft.WhyNoSteal(game) != KitRefusal.None) return;
            if (game.Evaluator.Evaluate(game.PlayerHand).Category > HandCategory.ThreeOfAKind) return;
            Steal(game, kit.Theft);
        }

        private static void Steal(HellPokerGame game, TheftPowers theft)
        {
            if (theft.WhyNoSteal(game) != KitRefusal.None) return;
            int lone = WorstCard(game, i => theft.CanGive(game, i));
            if (lone >= 0) theft.Steal(game, lone);
        }

        /// <summary>The draw, with the class's help; <paramref name="discards"/> are what the player throws.</summary>
        public static void Draw(HellPokerGame game, ChapterHooks kit, int[] discards)
        {
            if (kit?.Satchel != null && discards.Length > 0 && kit.Kit.Satchel.Count > 0)
            {
                IReadOnlyDictionary<int, Card> fill = Helpful(game, discards, kit.Kit.Satchel);
                if (fill.Count > 0 && game.CanDraw(discards, fill, out _))
                {
                    game.Draw(discards, fill);
                    return;
                }
            }
            if (kit?.Prophecy != null) discards = Foreseen(game, kit.Prophecy, discards);
            game.Draw(discards);
        }

        /// <summary>After the draw: the Peasant keeps the highest thrown card.</summary>
        public static void AfterDraw(HellPokerGame game, ChapterHooks kit)
        {
            SatchelPowers satchel = kit?.Satchel;
            if (satchel == null || satchel.Offered.Count == 0) return;
            satchel.Keep(game, satchel.Offered.OrderByDescending(c => c.Rank).First());
        }

        /// <summary>
        /// The Oracle draws by what FORESIGHT shows: every set of throws the seen cards would fill is tried (its result is known), and the
        /// best known hand is taken when it is at least what the usual throws keep and a pair or better; otherwise the usual throws.
        /// </summary>
        private static int[] Foreseen(HellPokerGame game, ProphecyPowers prophecy, int[] discards)
        {
            IReadOnlyList<Card> top = prophecy.TopCards(game);
            if (top.Count == 0 || Enumerable.Range(0, Hand.Size).Any(game.IsPlayerCardHidden)) return discards;
            Hand hand = game.PlayerHand;
            int most = Math.Min(top.Count, game.Rules.MaxDiscards);
            int[] places = Enumerable.Range(0, Hand.Size).Where(i => !game.IsPlayerCardChained(i) && !game.IsPlayerCardThorned(i)).ToArray();
            int[] best = null;
            HandEvaluation bestEval = null;
            foreach (int[] set in Subsets(places).Where(s => s.Length <= most))
            {
                if (!game.CanDraw(set, out _)) continue;
                Hand h = hand;
                for (int k = 0; k < set.Length; k++) h = h.With(set[k], top[k]);
                HandEvaluation eval = game.Evaluator.Evaluate(h);
                if (bestEval != null && eval.CompareTo(bestEval) <= 0) continue;
                bestEval = eval;
                best = set;
            }
            if (best == null) return discards;
            if (discards.Length <= top.Count) return best;   // the usual throws are seen too: the best known hand is the best
            return bestEval.Category >= HandCategory.OnePair && bestEval.Category >= Kept(game, hand, discards) ? best : discards;
        }

        private static IEnumerable<int[]> Subsets(int[] items)
        {
            for (int mask = 0; mask < 1 << items.Length; mask++)
                yield return Enumerable.Range(0, items.Length).Where(i => (mask & (1 << i)) != 0).Select(i => items[i]).ToArray();
        }

        /// <summary>The satchel cards that alone better the hand in a thrown place (a pair made, a set grown).</summary>
        private static IReadOnlyDictionary<int, Card> Helpful(HellPokerGame game, int[] discards, IReadOnlyList<Card> bag)
        {
            var fill = new Dictionary<int, Card>();
            Hand hand = game.PlayerHand;
            HandCategory now = Kept(game, hand, discards);
            foreach (Card card in bag)
            {
                int place = discards.FirstOrDefault(d => !fill.ContainsKey(d));
                if (fill.Count >= discards.Length) break;
                Hand tried = hand.With(place, card);
                if (Kept(game, tried, discards.Where(d => d != place && !fill.ContainsKey(d)).ToArray()) <= now) continue;
                fill[place] = card;
                hand = tried;
                now = Kept(game, hand, discards.Where(d => !fill.ContainsKey(d)).ToArray());
            }
            return fill;
        }

        /// <summary>What the kept cards make (the thrown places not counted: the hand's cards that stay).</summary>
        private static HandCategory Kept(HellPokerGame game, Hand hand, int[] thrown)
        {
            var kept = Enumerable.Range(0, Hand.Size).Where(i => !thrown.Contains(i)).Select(i => hand[i]).ToList();
            return VisibleHandReader.Read(kept, game.Evaluator) ?? HandCategory.HighCard;
        }

        /// <summary>
        /// The Summoner: the loyal imp's free one on the Hand when a demon's open card betters the hand, else on the Eye; then the Hand
        /// bought only when it makes two pair or better (on the floors only with coins to spare).
        /// </summary>
        private static void Summon(HellPokerGame game, SummonPowers summon)
        {
            bool handHelps = HandHelps(game, summon);
            if (summon.FreeLeft > 0 && !handHelps)
            {
                if (summon.WhyNot(game, SummonAct.Eye) == KitRefusal.None) summon.Eye(game);
                if (summon.FreeLeft > 0 && summon.WhyNot(game, SummonAct.Whisper) == KitRefusal.None) summon.Whisper(game);
            }
            if (summon.WhyNot(game, SummonAct.Hand) != KitRefusal.None) return;
            int give = WorstCard(game, i => summon.CanGive(game, i));
            if (give < 0) return;
            HandCategory now = game.Evaluator.Evaluate(game.PlayerHand).Category;
            HandCategory best = Enumerable.Range(0, game.HouseCardsRevealed).Where(h => !game.IsHouseCardFalse(h) && !game.HouseHand[h].IsJoker)
                .Select(h => game.Evaluator.Evaluate(game.PlayerHand.With(give, game.HouseHand[h])).Category).DefaultIfEmpty(now).Max();
            if (best <= now) return;
            bool worth = summon.FreeLeft > 0 || (best >= HandCategory.TwoPair && (summon.AtDemonsTable || summon.Purse != null && summon.Purse.Coins - summon.PriceOf(SummonAct.Hand) >= 40));
            if (worth) summon.Swap(game, give);
        }

        /// <summary>A demon's open card would better the hand through the Hand.</summary>
        private static bool HandHelps(HellPokerGame game, SummonPowers summon)
        {
            if (summon.WhyNot(game, SummonAct.Hand) != KitRefusal.None) return false;
            int give = WorstCard(game, i => summon.CanGive(game, i));
            if (give < 0) return false;
            HandCategory now = game.Evaluator.Evaluate(game.PlayerHand).Category;
            return Enumerable.Range(0, game.HouseCardsRevealed).Where(h => !game.IsHouseCardFalse(h) && !game.HouseHand[h].IsJoker)
                .Any(h => game.Evaluator.Evaluate(game.PlayerHand.With(give, game.HouseHand[h])).Category > now);
        }

        /// <summary>The lowest card that makes nothing (a card in no pair); -1 when none may be given.</summary>
        private static int WorstCard(HellPokerGame game, System.Func<int, bool> allowed)
        {
            Hand hand = game.PlayerHand;
            int[] lone = Enumerable.Range(0, Hand.Size).Where(i => allowed(i) && !hand[i].IsJoker && hand.Count(c => c.Rank == hand[i].Rank) == 1)
                .OrderBy(i => hand[i].Rank).ToArray();
            return lone.Length > 0 ? lone[0] : -1;
        }
    }
}
