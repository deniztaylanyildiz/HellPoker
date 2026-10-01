using HellPoker.Core.Evaluation;

namespace HellPoker.Presentation.Ui
{
    /// <summary>All player-facing strings in one place, ready for localisation.</summary>
    internal static class UiText
    {
        public const string Title = "HELL POKER";
        public const string Subtitle = "Five Card Draw against the House";
        public const string YearsLabel = "YEARS LEFT IN HELL";
        public const string DamnationFormat = "Eternal damnation at {0} years";
        public const string StakeLabel = "ANTE";
        public const string PayoutsTitle = "THE DEVIL'S PAYOUTS";
        public const string PayoutLoss = "Lose: + stake  ·  Fold: + half the stake";
        public const string Absolution = "ABSOLUTION";
        public const string HouseCaption = "THE HOUSE";
        public const string PlayerCaption = "YOUR HAND";
        public const string FoldedCaption = "FOLDED";
        public const string DiscardTag = "DISCARD";
        public const string Hint = "Space deal · draw · pass   ·   R raise   ·   F fold   ·   1-5 pick cards   ·   ↑ ↓ ante   ·   Esc menu";
        public const string PotFormat = "ON THE TABLE: {0} YEARS";
        public const string FinalStretchBannerFormat = "THE GATES ARE IN SIGHT  ·  UNDER {0} YEARS, NO PASSING";

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

        /// <summary>{0} starting years, {1} damnation limit, {2} max discards, {3} forced-raise threshold.</summary>
        public const string RulesFormat =
            "You start with {0} years. Bring them down to 0 and you walk free; reach {1} and you are damned for eternity.\n\n" +
            "1.  Choose an ante — the years you put on the table. They come straight off your sentence counter.\n" +
            "2.  Your five cards turn one by one. After each: RAISE (add the ante again), PASS, or FOLD.\n" +
            "3.  Throw back up to {2} cards and draw new ones. The House draws too.\n" +
            "4.  The House turns its cards one by one — raise, pass or fold again — then the showdown.\n\n" +
            "Win: years forgiven = everything on the table × your hand's multiplier.\n" +
            "Lose: everything on the table is added to your sentence.   Fold: half of it is added.\n" +
            "You can never wager more years than you have.\n\n" +
            "Under {3} years the gates are in sight: passing is forbidden until you are all in.\n" +
            "A♠ A♣ 8♠ 8♣ — the Dead Man's Hand — beats everything and wipes your sentence clean.";

        public const string Deal = "DEAL";
        public const string Stand = "STAND PAT";
        public const string DrawFormat = "DRAW {0}";
        public const string Next = "NEXT HAND";
        public const string Again = "PLAY AGAIN";
        public const string RaiseFormat = "RAISE +{0}";
        public const string AllInFormat = "ALL IN +{0}";
        public const string AllInDone = "ALL IN";
        public const string Pass = "PASS";
        public const string Fold = "FOLD";

        public const string PromptBet = "Choose your ante in years, then deal.";
        public const string PromptPlayerCardFormat = "Your card {0} of {1} turns.";
        public const string PromptHouseCardFormat = "The House turns card {0} of {1}.";
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
