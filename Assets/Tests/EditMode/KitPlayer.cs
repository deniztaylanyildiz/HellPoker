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
    /// The simulations' player with Phase 2's class mechanics, by simple rules: the Peasant draws a satchel card that betters the hand
    /// (the harvest when charged), keeps the highest thrown card; the Warlock sends a top card that would not help to the bottom (charged);
    /// the King seizes at four taxes (or charged), else turns a card for one.
    /// </summary>
    internal static class KitPlayer
    {
        /// <summary>Before the draw is made (every card seen): the King's acts.</summary>
        public static void BeforeDraw(HellPokerGame game, ChapterHooks kit)
        {
            if (kit?.Summon != null && game.Phase == GamePhase.Drawing) Summon(game, kit.Summon);
            TreasuryPowers treasury = kit?.Treasury;
            if (treasury == null || game.Phase != GamePhase.Drawing) return;
            int give = WorstCard(game, i => treasury.CanGive(game, i));
            if (give >= 0 && treasury.WhyNot(game, TaxAct.Seize) == KitRefusal.None) treasury.Seize(game, give);
            else if (give >= 0 && treasury.WhyNot(game, TaxAct.Seize, charged: true) == KitRefusal.None) treasury.Seize(game, give, charged: true);
            // A card turned for a tax only with taxes to spare: the rest wait for the K seizure (2.1.1: half price every hand).
            else if (kit.Kit.Taxes > treasury.PowerSeizePrice && treasury.WhyNot(game, TaxAct.Reveal) == KitRefusal.None) treasury.Reveal(game);
        }

        /// <summary>The draw, with the class's help; <paramref name="discards"/> are what the player throws.</summary>
        public static void Draw(HellPokerGame game, ChapterHooks kit, int[] discards)
        {
            if (kit?.Satchel != null && discards.Length > 0 && kit.Kit.Satchel.Count > 0)
            {
                SatchelPowers satchel = kit.Satchel;
                if (satchel.WhyNoHarvest(game, discards) == KitRefusal.None && Betters(game, SatchelPowers.BestFill(game, game.PlayerHand, discards, kit.Kit.Satchel)))
                {
                    satchel.Harvest(game, discards);
                    return;
                }
                IReadOnlyDictionary<int, Card> fill = Helpful(game, discards, kit.Kit.Satchel);
                if (fill.Count > 0 && game.CanDraw(discards, fill, out _))
                {
                    game.Draw(discards, fill);
                    return;
                }
            }
            if (kit?.Prophecy != null && discards.Length > 0) Rearrange(game, kit.Prophecy, discards);
            game.Draw(discards);
        }

        /// <summary>After the draw: the Peasant keeps the highest thrown card.</summary>
        public static void AfterDraw(HellPokerGame game, ChapterHooks kit)
        {
            SatchelPowers satchel = kit?.Satchel;
            if (satchel == null || satchel.Offered.Count == 0) return;
            satchel.Keep(game, satchel.Offered.OrderByDescending(c => c.Rank).First());
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

        /// <summary>The harvest is worth its unseen draw: the satchel alone makes two pair or better of the hand (2.1.1: it comes every hand,
        /// so a small gain is not spent on it — the satchel's cards wait for a better one).</summary>
        private static bool Betters(HellPokerGame game, IReadOnlyDictionary<int, Card> fill)
        {
            Hand hand = game.PlayerHand;
            foreach (var pair in fill) hand = hand.With(pair.Key, pair.Value);
            HandCategory after = game.Evaluator.Evaluate(hand).Category;
            return after > game.Evaluator.Evaluate(game.PlayerHand).Category && after >= HandCategory.TwoPair;
        }

        /// <summary>What the kept cards make (the thrown places not counted: the hand's cards that stay).</summary>
        private static HandCategory Kept(HellPokerGame game, Hand hand, int[] thrown)
        {
            var kept = Enumerable.Range(0, Hand.Size).Where(i => !thrown.Contains(i)).Select(i => hand[i]).ToList();
            return VisibleHandReader.Read(kept, game.Evaluator) ?? HandCategory.HighCard;
        }

        /// <summary>The Oracle's BEND FATE: when the top card would not better the kept cards, a deeper seen card that would comes up;
        /// with none, the top card goes down (as many moves as the power gives).</summary>
        private static void Rearrange(HellPokerGame game, ProphecyPowers prophecy, int[] discards)
        {
            if (TopHelps(game, prophecy, discards) || prophecy.WhyNoRearrange(game) != KitRefusal.None) return;
            for (int k = 0; k < prophecy.Kit.RearrangesPerCharge && !TopHelps(game, prophecy, discards); k++)
            {
                IReadOnlyList<Card> top = prophecy.TopCards(game);
                int up = Enumerable.Range(1, Math.Max(0, top.Count - 1)).FirstOrDefault(i => Helps(game, top[i], discards));
                bool moved = up > 0 ? prophecy.Bend(game, up, toTop: true) : prophecy.Kit.CanSendToBottom && prophecy.Bend(game, 0, toTop: false);
                if (!moved) break;
            }
        }

        private static bool TopHelps(HellPokerGame game, ProphecyPowers prophecy, int[] discards)
        {
            IReadOnlyList<Card> top = prophecy.TopCards(game);
            return top.Count == 0 || Helps(game, top[0], discards);
        }

        /// <summary>The card, drawn into the first thrown place, betters what the kept cards make.</summary>
        private static bool Helps(HellPokerGame game, Card card, int[] discards)
        {
            int place = discards.OrderBy(i => i).First();
            int[] rest = discards.Where(d => d != place).ToArray();
            return Kept(game, game.PlayerHand.With(place, card), rest) > Kept(game, game.PlayerHand, discards);
        }

        /// <summary>
        /// The Summoner: the Legion whenever charged (then all three imps, free); otherwise the Hand only when a demon's open card betters
        /// the hand to two pair or better (on the floors only with coins to spare).
        /// </summary>
        private static void Summon(HellPokerGame game, SummonPowers summon)
        {
            if (summon.WhyNoLegion(game) == KitRefusal.None) summon.CallLegion(game);
            // 2.1.1: the Legion frees the hand's first imp — the Hand when it betters the hand (below), else the Eye.
            bool handHelps = HandHelps(game, summon);
            if (summon.LegionFreeLeft > 0 && !handHelps)
            {
                if (summon.WhyNot(game, SummonAct.Eye) == KitRefusal.None) summon.Eye(game);
                if (summon.LegionFreeLeft > 0 && summon.WhyNot(game, SummonAct.Whisper) == KitRefusal.None) summon.Whisper(game);
            }
            if (summon.WhyNot(game, SummonAct.Hand) != KitRefusal.None) return;
            int give = WorstCard(game, i => summon.CanGive(game, i));
            if (give < 0) return;
            HandCategory now = game.Evaluator.Evaluate(game.PlayerHand).Category;
            HandCategory best = Enumerable.Range(0, game.HouseCardsRevealed).Where(h => !game.IsHouseCardFalse(h) && !game.HouseHand[h].IsJoker)
                .Select(h => game.Evaluator.Evaluate(game.PlayerHand.With(give, game.HouseHand[h])).Category).DefaultIfEmpty(now).Max();
            if (best <= now) return;
            bool worth = summon.LegionFreeLeft > 0 || (best >= HandCategory.TwoPair && (summon.AtDemonsTable || summon.Purse != null && summon.Purse.Coins - summon.PriceOf(SummonAct.Hand) >= 40));
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
