using HellPoker.Core.Evaluation;

namespace HellPoker.Presentation.Ui
{
    /// <summary>All player-facing strings in one place, ready for localisation.</summary>
    internal static partial class UiText
    {
        public const string Title = "HELL POKER";
        public const string Subtitle = "Five Card Draw against the House";
        public const string YearsLabel = "YEARS LEFT IN HELL";
        public const string DamnationFormat = "Damned forever at {0}";
        public const string StakeLabel = "ANTE";
        public const string PayoutsTitle = "PAYOUTS";
        /// <summary>Payout panel footer. {0} loss surcharge (see <see cref="LossSurcharge"/>), {1} fold before the draw, {2} after (see <see cref="ShareShort"/>).</summary>
        public const string PayoutLossFormat = "× pays on the ante,\nraises pay 1 : 1.\nLose: House hand{0}\nFold: {1} / {2}";

        /// <summary>Payout panel footer when folding costs the same before and after the draw.</summary>
        public const string PayoutLossSameFoldFormat = "× pays on the ante,\nraises pay 1 : 1.\nLose: House hand{0}\nFold: always {1}";

        /// <summary>A share of the stake in a word: "half", "all", "quarter", or a percentage.</summary>
        public static string ShareShort(int percent)
        {
            switch (percent)
            {
                case 25: return "quarter";
                case 50: return "half";
                case 100: return "all";
                default: return percent + "%";
            }
        }

        /// <summary>Nothing for a plain loss; " × 1.25" when the dealer charges more.</summary>
        public static string LossSurcharge(int lossPercent)
        {
            return lossPercent == 100 ? "" : " × " + (lossPercent / 100f).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        }
        public const string AnteFormat = "{0} YEARS";
        public const string StakeInfoFormat = "Win: at least −{0} years   ·   Lose: at least +{1} years";
        public const string Absolution = "FREE";
        public const string HouseCaption = "THE HOUSE";
        public const string PlayerCaption = "YOUR HAND";
        public const string FoldedCaption = "FOLDED";
        public const string DiscardTag = "TOSS";
        public const string Hint = "Space deal · draw · pass · call    R raise    C call    F fold    1-5 pick cards    Esc menu";
        public const string PotFormat = "ON THE TABLE: {0}";
        public const string FinalStretchBannerFormat = "UNDER {0}: NO PASSING";

        public const string Menu = "MENU";
        public const string MenuTaglineFormat = "You have been sentenced to {0} years in Hell.\nThe House offers you a game.";
        public const string MenuSubtagline = "Five Card Draw  ·  Wild Bill's Dead Man's Hand sets you free";
        public const string NewGame = "NEW GAME";
        public const string Continue = "CONTINUE";
        public const string HowToPlay = "HOW TO PLAY";
        public const string Quit = "QUIT";
        public const string Back = "BACK";
        public const string MenuFooter = "Esc — menu";
        public const string RulesTitle = "THE RULES OF THE HOUSE";

        /// <summary>{0} starting years, {1} damnation limit, {2} forced-raise threshold, {3} table cap percent.</summary>
        public const string RulesFormat =
            "You start with {0} years. Bring them down to 0 and you walk free; reach {1} and you are damned for eternity.\n" +
            "Choose your dealer first — every demon keeps their own house rules, payouts and temper.\n\n" +
            "1.  The ante is a tenth of your sentence (1000 years: 100, 500: 50, 250: 25). Deal.\n" +
            "2.  Two cards turn, then one by one: RAISE, PASS or FOLD on cards 3, 4 and 5.\n" +
            "3.  Throw back cards and draw (the dealer says how many) — then one more decision. Raises now count double.\n" +
            "4.  The House shows some of its cards — a last decision — then the showdown.\n" +
            "    Raise after the draw and the House may raise back: CALL or FOLD.\n\n" +
            "Win: forgiven = the table + the ante × (your multiplier − 1).   Lose: the same on the House's hand.\n" +
            "At most {3}% of your sentence may ever be on the table.\n" +
            "Under {2} years passing is forbidden until the table is full.   A♠ A♣ 8♠ 8♣ — the Dead Man's Hand — sets you free.";

        public const string Deal = "DEAL";
        public const string Stand = "STAND PAT";
        public const string DrawFormat = "DRAW {0}";
        public const string Next = "NEXT HAND";
        public const string Again = "PLAY AGAIN";
        public const string RaiseFormat = "RAISE +{0}";
        public const string AllInFormat = "ALL IN +{0}";
        public const string AllInDone = "ALL IN";
        public const string TableFull = "TABLE FULL";
        public const string Pass = "PASS";
        public const string Fold = "FOLD";
        public const string CallFormat = "CALL +{0}";

        public const string PromptBetFormat = "The ante is {0} years. Deal when you dare.";
        public const string PromptPlayerCardFormat = "Your card {0} of {1} turns.";
        public const string PromptAfterDraw = "Your hand is set. Raises count double now.";
        public const string PromptHouseShowsFormat = "The House shows {0} of its {1} cards.";
        public const string PromptReRaiseFormat = "The House raises {0} years! Call — or fold?";
        public const string PromptChoice = "  Raise, pass — or fold?";
        public const string PromptForcedChoice = "  No passing now: raise or fold.";
        public const string PromptDrawFormat = "Pick up to {0} cards to throw back into the fire.";
        public const string HouseDrewFormat = "{0} — drew {1}";
        public const string WinFormat = "{0} beats {1}.  {2} years forgiven.";
        public const string LossFormat = "{0} beats your {1}.  +{2} years.";
        public const string PushFormat = "Push — {0} against {1}. The sentence stands.";
        public const string FoldFormat = "You fold and slink away from the table.  +{0} years.";
        public const string AbsolvedMessage = "DEAD MAN'S HAND!  Wild Bill vouches for you. You walk free.";
        public const string ServedMessage = "Your sentence is served. The gates of Hell open — you walk free.";
        public const string DamnedFormat = "{0} years. The House owns your soul for eternity.";

        public static string CategoryName(HandCategory category)
        {
            switch (category)
            {
                case HandCategory.HighCard: return "High Card";
                case HandCategory.OnePair: return "One Pair";
                case HandCategory.TwoPair: return "Two Pair";
                case HandCategory.ThreeOfAKind: return "Three of a Kind";
                case HandCategory.Straight: return "Straight";
                case HandCategory.Flush: return "Flush";
                case HandCategory.FullHouse: return "Full House";
                case HandCategory.FourOfAKind: return "Four of a Kind";
                case HandCategory.StraightFlush: return "Straight Flush";
                case HandCategory.RoyalFlush: return "Royal Flush";
                case HandCategory.DeadMansHand: return "Dead Man's Hand";
                default: return category.ToString();
            }
        }
    }
}
