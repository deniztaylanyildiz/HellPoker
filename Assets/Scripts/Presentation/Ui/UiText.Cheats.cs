using HellPoker.Core.Cards;
using HellPoker.Core.Cheats;

namespace HellPoker.Presentation.Ui
{
    /// <summary>The demons' cheats in words: names, what they do, what the demon says, what the result line tells.</summary>
    internal static partial class UiText
    {
        public const string MaliceLabel = "MALICE";
        public const string LiarFlash = "LIAR";
        public const string FallAwaits = "THE FALL AWAITS";
        public const string CheatsButton = "CHEATS";
        public const string CheatsTitle = "THE DEMONS' CHEATS";

        public static string CheatName(string id)
        {
            switch (id)
            {
                case CheatIds.Collateral: return "COLLATERAL";
                case CheatIds.Tithe: return "TITHE";
                case CheatIds.Buyout: return "BUYOUT";
                case CheatIds.FalseFace: return "FALSE FACE";
                case CheatIds.ForkedTongue: return "FORKED TONGUE";
                case CheatIds.SerpentSwap: return "SERPENT SWAP";
                case CheatIds.NightVeil: return "NIGHT VEIL";
                case CheatIds.Thorn: return "THORN";
                case CheatIds.Moonless: return "MOONLESS";
                case CheatIds.Gaze: return "GAZE";
                case CheatIds.Rewrite: return "REWRITE";
                case CheatIds.BurningCard: return "BURNING CARD";
                case CheatIds.TheFall: return FallAwaits;
                default: return (id ?? "").ToUpperInvariant();
            }
        }

        /// <summary>What a cheat does, in one sentence (the intent's tooltip, the H panel, the Cheats page). No numbers.</summary>
        public static string CheatDescription(string id)
        {
            switch (id)
            {
                case CheatIds.Collateral: return "A card you would throw back is chained: it must stay.";
                case CheatIds.Tithe: return "If you win this hand, a unit of what you win is his.";
                case CheatIds.Buyout: return "Your highest card is traded for one of the House's lowest.";
                case CheatIds.FalseFace: return "One card the House shows is not what it seems — until the showdown.";
                case CheatIds.ForkedTongue: return "After the draw a card changes suit. Flushes beware. His tongue may slip.";
                case CheatIds.SerpentSwap: return "A card of yours slips to the House; what comes back stays dark.";
                case CheatIds.NightVeil: return "One of your cards will come to you in the dark.";
                case CheatIds.Thorn: return "A thorn in a card you would throw back: letting it go costs a unit.";
                case CheatIds.Moonless: return "The cards you draw stay dark until the showdown.";
                case CheatIds.Gaze: return "He sees your hand. His raises will hurt.";
                case CheatIds.Rewrite: return "After the draw a card is rewritten into something weaker.";
                case CheatIds.BurningCard: return "Your best combination's top card burns into a random one.";
                case CheatIds.TheFall: return "If you would win, both best cards are dealt again. You may fold.";
                default: return "";
            }
        }

        /// <summary>What the demon says the moment a cheat strikes.</summary>
        public static string CheatLine(string id)
        {
            switch (id)
            {
                case CheatIds.Collateral: return "Collateral. This one stays with you — on my terms.";
                case CheatIds.Tithe: return "A small tithe on your good fortune.";
                case CheatIds.Buyout: return "I'll buy that from you. My price, of course.";
                case CheatIds.FalseFace: return "Look closer, darling. Or don't.";
                case CheatIds.ForkedTongue: return "Did I say spades? I meant hearts.";
                case CheatIds.SerpentSwap: return "A little trade between friends.";
                case CheatIds.NightVeil: return "Let the night keep this one.";
                case CheatIds.Thorn: return "Careful. It bites if you let go.";
                case CheatIds.Moonless: return "No moon tonight. Draw in the dark.";
                case CheatIds.Gaze: return "I see everything you hold.";
                case CheatIds.Rewrite: return "That is not what you had. It is what I say you had.";
                case CheatIds.BurningCard: return "Burn.";
                case CheatIds.TheFall: return "Fall.";
                default: return "";
            }
        }

        /// <summary>Belial, caught announcing one cheat and playing another.</summary>
        public const string LiarLine = "Did you believe me? How sweet.";

        /// <summary>Over the card a cheat helped instead of hurt.</summary>
        public const string BackfireFlash = "BACKFIRE";

        /// <summary>The result screen's line about the hand's cheat. Never a number while the soul is on the table.</summary>
        public static string CheatLog(string dealerName, CheatResult result, bool soul, int thornYears, int titheYears)
        {
            if (result == null || result.Outcome != CheatOutcome.Played) return null;
            string who = Capitalised(dealerName);
            string lost = result.Lost.HasValue ? result.Lost.Value.ToString() : "a card";
            string gained = result.Gained.HasValue ? result.Gained.Value.ToString() : "a card";
            if (result.Backfired)
            {
                // The cheat turned on its demon: said so, plainly.
                switch (result.CheatId)
                {
                    case CheatIds.ForkedTongue: return $"{who}'s tongue slipped: your {lost} became the {gained}. It backfired!";
                    case CheatIds.BurningCard: return $"{who} burned your {lost} — the {gained} rose from the ashes. It backfired!";
                    case CheatIds.TheFall: return $"{who}: the {lost} fell, and the {gained} rose. It backfired!";
                    default: return $"{who}'s cheat backfired!";
                }
            }
            switch (result.CheatId)
            {
                case CheatIds.Collateral: return $"{who} chained your {lost} as collateral.";
                case CheatIds.Tithe:
                    return soul || titheYears <= 0 ? $"{who} kept a tithe of your win." : $"{who} kept a tithe: {titheYears} years of your win.";
                case CheatIds.Buyout: return $"{who} took your {lost} for a {gained}.";
                case CheatIds.FalseFace: return $"{who} showed you a false face.";
                case CheatIds.ForkedTongue: return $"{who}'s tongue turned your {lost} into the {gained}.";
                case CheatIds.SerpentSwap: return $"{who}'s serpent stole your {lost}.";
                case CheatIds.NightVeil: return $"{who} veiled one of your cards.";
                case CheatIds.Thorn:
                    return thornYears <= 0 ? $"{who}'s thorn found no hand to prick."
                        : soul ? $"{who}'s thorn drew blood from your soul." : $"{who}'s thorn cost you {thornYears} years.";
                case CheatIds.Moonless: return $"{who} kept your new cards in the dark.";
                case CheatIds.Gaze: return $"{who} saw your cards.";
                case CheatIds.Rewrite: return $"{who} rewrote your {lost} as the {gained}.";
                case CheatIds.BurningCard: return $"{who} burned your {lost} into the {gained}.";
                case CheatIds.TheFall: return $"{who}: the {lost} fell. The hands were judged anew.";
                default: return null;
            }
        }

        private static string Capitalised(string name)
        {
            if (string.IsNullOrEmpty(name)) return "The House";
            // Every word: "THE MORNING STAR" → "The Morning Star".
            string[] words = name.Split(' ');
            for (int i = 0; i < words.Length; i++)
                if (words[i].Length > 0)
                    words[i] = words[i].Substring(0, 1) + words[i].Substring(1).ToLowerInvariant();
            return string.Join(" ", words);
        }

        // ------------------------------------------------------------------ first-game tips: each demon's first cheat, in their voice

        public const string TipCheatPrefix = "tip.cheat.";

        public static string TipCheatFor(string dealerId) => TipCheatPrefix + dealerId;

        public static string CheatTipText(string dealerId)
        {
            switch (dealerId)
            {
                case Core.Dealers.DealerRoster.MammonId: return "When my purse of malice is full, I collect. The sign above me says how.";
                case Core.Dealers.DealerRoster.BelialId: return "My malice is full. That sign says what I will do... most of the time.";
                case Core.Dealers.DealerRoster.LilithId: return "My thorns are full. Read the sign, little soul — then suffer it.";
                case Core.Dealers.DealerRoster.LuciferId: return "Every hand, I take something. I will always tell you what.";
                default: return null;
            }
        }

        // ------------------------------------------------------------------ How to Play: the Cheats page

        public static string CheatsPage()
        {
            string Line(string id) => $"   {CheatName(id)} — {CheatDescription(id)}";
            return
                "Every demon has a MALICE gauge under the portrait. It fills every hand (faster when you win, and under 500 years).\n" +
                "Full, the demon announces a cheat above the portrait, then plays it on the cards. Under 400 the big ones come out.\n" +
                "A♠ A♣ 8♠ 8♣ are beyond any cheat. A cheat left to chance may BACKFIRE and help you (tongue, fire, The Fall).\n\n" +
                "MAMMON (always honest)\n" + Line(CheatIds.Collateral) + "\n" + Line(CheatIds.Tithe) + "\n" + Line(CheatIds.Buyout) + "\n" +
                "BELIAL (a quarter of his intents are lies)\n" + Line(CheatIds.FalseFace) + "\n" + Line(CheatIds.ForkedTongue) + "\n" +
                Line(CheatIds.SerpentSwap) + "\n" +
                "LILITH\n" + Line(CheatIds.NightVeil) + "\n" + Line(CheatIds.Thorn) + "\n" + Line(CheatIds.Moonless) + "\n" +
                "THE MORNING STAR (every hand)\n" + Line(CheatIds.Gaze) + "\n" + Line(CheatIds.Rewrite) + "\n" + Line(CheatIds.BurningCard) + "\n" +
                Line(CheatIds.TheFall);
        }
    }
}
