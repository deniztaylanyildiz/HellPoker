using HellPoker.Core.Evaluation;

namespace HellPoker.Presentation.Ui
{
    /// <summary>All player-facing strings in one place, ready for localisation.</summary>
    internal static partial class UiText
    {
        public const string Title = "HELL POKER";
        public const string Subtitle = "Five Card Draw against the House";
        public const string YearsLabel = "YEARS LEFT IN HELL";
        public const string SoulLineFormat = "Soul at stake at {0}";
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
        public const string Hint = "Space deal/draw/pass  R raise  D check to draw  C call  F fold  1-5 cards  H hands  Esc menu";
        public const string PotFormat = "ON THE TABLE: {0}";
        public const string FinalStretchBannerFormat = "UNDER {0}: NO PASSING";
        public const string LastMomentsBanner = "ONE HAND FROM FREEDOM";

        /// <summary>Said by an ordinary demon when a win would end the sentence at their table: the last year stays.</summary>
        public const string LastYearLine = "The last year is not mine to take. He is waiting.";

        /// <summary>Under the counter at 1 year: why it did not reach 0.</summary>
        public const string LastYearNote = "The last year is his";
        public const string CastDownLineFormat = "Cast down above {0}";
        public const string AttemptLabelFormat = "LUCIFER: ATTEMPT {0}";

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

        /// <summary>{0} starting years, {1} (unused), {2} forced-raise threshold, {3} table cap percent, {4} Lucifer's gate,
        /// {5} where a fall from his table lands, {6} his ante, {7} his table cap.</summary>
        public const string RulesFormat =
            "You start with {0} years. Bring them down to 0 and you walk free — but only at the last table.\n" +
            "Every demon has a soul line. Reach it and your soul goes on the table — lose it all and you are damned for eternity.\n" +
            "You may change tables between hands — never while your soul is on one.\n\n" +
            "1.  The ante is a tenth of your sentence (1000 years: 100, 500: 50, 250: 25). Deal.\n" +
            "2.  Two cards turn, then one by one: RAISE, PASS or FOLD on cards 3, 4 and 5.\n" +
            "3.  Throw back cards and draw (the dealer says how many) — then one more decision. Raises now count double.\n" +
            "4.  The House shows some of its cards — a last decision — then the showdown.\n" +
            "    Raise after the draw and the House may raise back: CALL or FOLD.\n\n" +
            "Win: forgiven = the table + the ante × (your multiplier − 1).   Lose: the same on the House's hand.\n" +
            "At most {3}% of your sentence may be on the table. Fill it and the pact is sealed: no folding, the cards play out.\n" +
            "A♠ A♣ 8♠ 8♣ — the Dead Man's Hand — sets you free at once, wherever you sit.\n\n" +
            "THE MORNING STAR.  At {4} years or less, Lucifer summons you to his table, wherever you sit. Only there can a sentence end.\n" +
            "His stakes are his own: ante {6}, at most {7} on the table. He shows no cards. You cannot leave.\n" +
            "Climb back above {4} at his table and he casts you down: to the demon you came from, with at least {5} years.";

        public const string Deal = "DEAL";
        public const string Stand = "STAND PAT";
        public const string DrawFormat = "DRAW {0}";
        public const string Next = "NEXT HAND";
        public const string TheEnd = "THE END";
        public const string RaiseFormat = "RAISE +{0}";
        public const string AllInFormat = "ALL IN +{0}";
        public const string AllInDone = "ALL IN";
        public const string TableFull = "TABLE FULL";
        public const string Pass = "PASS";
        public const string CheckToDrawButton = "CHECK\nTO DRAW";
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
        public const string DamnedMessage = "Your soul is ash. The House owns you for eternity.";
        public const string MorningStarFallsMessage = "His eyes go dark. The gates of Hell open — you walk free.";

        // ------------------------------------------------------------------ the pact (table full: the hand plays out on its own)

        public const string PactSealed = "THE PACT IS SEALED";
        public const string SealedMessage = "The pact is sealed. No folding now — the cards decide.";
        public const string SealedDrawPrompt = "The pact is sealed. Pick up to {0} cards to throw back — the rest plays out.";

        // ------------------------------------------------------------------ a hand left behind (the game was closed mid-hand)

        public const string FledFormat = "You left mid-hand. It counts as a fold.  +{0} years.";
        public const string FledSealedFormat = "You left a sealed hand. The whole wager is lost.  +{0} years.";
        public const string FledSoul = "You left mid-hand. A piece of your soul stays on the table.";
        public const string FledSealedSoul = "You left a sealed hand. The whole wager burns your soul.";

        // ------------------------------------------------------------------ guidance for new players

        public const string HandNowFormat = "NOW: {0}";
        public const string WinnerFormat = "{0}  WINS";
        public const string GoodHandFormat = "{0}!";
        public const string HandRanksTitle = "HAND RANKS";
        public const string HandRanksSubtitle = "Strongest first. A higher hand beats a lower one.";
        public const string HandRanksTableFooter = "H or Esc closes";
        public const string HandsButton = "HANDS";
        public const string RulesButton = "RULES";

        /// <summary>An example of each hand, for the hand ranking panel.</summary>
        public static string HandExample(HandCategory category)
        {
            switch (category)
            {
                case HandCategory.DeadMansHand: return "A♠ A♣ 8♠ 8♣ + any";
                case HandCategory.RoyalFlush: return "10♥ J♥ Q♥ K♥ A♥";
                case HandCategory.StraightFlush: return "5♣ 6♣ 7♣ 8♣ 9♣";
                case HandCategory.FourOfAKind: return "9 9 9 9 K";
                case HandCategory.FullHouse: return "Q Q Q 4 4";
                case HandCategory.Flush: return "2♦ 7♦ 9♦ J♦ K♦";
                case HandCategory.Straight: return "4 5 6 7 8";
                case HandCategory.ThreeOfAKind: return "7 7 7 K 2";
                case HandCategory.TwoPair: return "J J 5 5 A";
                case HandCategory.OnePair: return "8 8 K 4 2";
                default: return "A J 9 5 2";
            }
        }

        // ------------------------------------------------------------------ first-game tips (one line each, in the dealer's voice)

        public const string TipFirstDecision = "tip.decision";
        public const string TipFirstDraw = "tip.draw";
        public const string TipFirstReRaise = "tip.reraise";
        public const string TipFinalStretch = "tip.final";
        public const string TipSoul = "tip.soul";
        public const string TipLucifer = "tip.lucifer";

        public static string TipText(string tip, int forcedRaiseYears, int gateYears = 250)
        {
            switch (tip)
            {
                case TipFirstDecision: return "Like your cards? RAISE. Unsure? PASS. Afraid? FOLD — and lose less.";
                case TipFirstDraw: return "Click the cards to throw back, then DRAW. Keep your pairs, sinner.";
                case TipFirstReRaise: return "I raise you back. CALL to stay in — or FOLD and leave the table to me.";
                case TipFinalStretch: return string.Format("Under {0} years, no passing. Raise or fold — so close to freedom.", forcedRaiseYears);
                case TipSoul: return "Past my line your soul is the stake. Lose all of it and you are mine forever.";
                case TipLucifer: return string.Format("Only I can set you free. Fall above {0} and you will be cast down.", gateYears);
                default:
                    return tip != null && tip.StartsWith(TipCheatPrefix) ? CheatTipText(tip.Substring(TipCheatPrefix.Length)) : null;
            }
        }

        // ------------------------------------------------------------------ the end of a run, and records

        public const string AbsolvedTitle = "ABSOLVED";
        public const string DamnedTitle = "DAMNED";
        public const string AbsolvedSubtitle = "The gates of Hell open. You walk free.";
        public const string DamnedSubtitle = "Your soul is ash. The House keeps you for eternity.";
        public const string EndHandsFormat = "Hands played: {0}";
        public const string EndLowestFormat = "Lowest sentence: {0} years";
        public const string EndHighestFormat = "Highest sentence: {0} years";
        public const string EndHighestSoul = "Highest sentence: past the soul line";
        public const string EndBestFormat = "Best hand: {0}";
        public const string EndBestNone = "Best hand: none shown";
        public const string EndDealersFormat = "Tables: {0}";
        public const string EndSoulStaked = "Your soul went on the table.";
        public const string EndSoulKept = "Your soul never left you.";
        public const string MorningStarFallsTitle = "THE MORNING STAR FALLS";
        public const string MorningStarFallsSubtitle = "His eyes go dark. Nothing stands between you and the morning.";
        public const string WildBillTitle = "WILD BILL'S ESCAPE";
        public const string WildBillSubtitle = "Aces and eights. You walked out before he ever looked up.";
        public const string EndBeatLuciferFormat = "The Morning Star fell on attempt {0}";
        public const string EndLuciferTriedFormat = "You faced the Morning Star {0} time(s)";
        public const string EndNeverMetLucifer = "You never met the Morning Star";
        public const string EndMorningStarRemembers = "The Morning Star will remember this.";
        public const string RecordsLuciferReachedFormat = "Faced the Morning Star: {0}";
        public const string RecordsLuciferDefeatedFormat = "Morning Star fallen: {0}";
        public const string RecordsFewestAttemptsFormat = "Fewest attempts: {0}";
        public const string RecordsFewestAttemptsNone = "Fewest attempts: not yet";
        public const string RecordsWildBillFormat = "Wild Bill's escapes: {0}";
        public const string ToMenu = "MENU";
        public const string Records = "RECORDS";
        public const string RecordsTitle = "RECORDS";
        public const string RecordsRunsFormat = "Runs started: {0}";
        public const string RecordsAbsolvedFormat = "Walked free: {0}";
        public const string RecordsDamnedFormat = "Damned: {0}";
        public const string RecordsFastestFormat = "Fastest freedom: {0} hands";
        public const string RecordsFastestNone = "Fastest freedom: not yet";
        public const string RecordsDealerFormat = "Freed at {0}'s table: {1}";

        // ------------------------------------------------------------------ settings

        public const string SettingsButton = "SETTINGS";
        public const string SettingsTitle = "SETTINGS";
        public const string SettingSpeed = "ANIMATION SPEED";
        public const string SettingSpeedHint = "Any key or click also hurries an animation along.";
        public const string SettingFullscreen = "FULL SCREEN";
        public const string SettingFullscreenHint = "Alt+Enter switches at any time.";
        public const string SettingHandGuide = "HAND GUIDE";
        public const string SettingHandGuideHint = "Names your hand as it stands and hints which cards to keep.";
        public const string SettingTips = "FIRST-GAME TIPS";
        public const string SettingTipsHint = "The dealers explain each new moment once.";
        public const string On = "ON";
        public const string Off = "OFF";
        public const string ResetTips = "SHOW AGAIN";
        public const string TipsFresh = "ALL NEW";

        public static string SpeedName(HellPoker.Presentation.Settings.AnimationSpeed speed)
        {
            switch (speed)
            {
                case HellPoker.Presentation.Settings.AnimationSpeed.Fast: return "FAST";
                case HellPoker.Presentation.Settings.AnimationSpeed.VeryFast: return "VERY FAST";
                default: return "NORMAL";
            }
        }

        // ------------------------------------------------------------------ why a button is locked

        public const string LockedTableFull = "TABLE FULL — the table is at its limit.";
        public const string LockedAllIn = "ALL IN — every year you have is on the table.";
        public const string LockedAllOfIt = "ALL OF IT — your whole soul is on the table.";
        public const string LockedPassFormat = "No passing under {0} years: raise or fold.";
        public const string LockedCheckToDrawFormat = "No checking under {0} years: raise or fold.";
        public const string LockedAnswer = "The House has raised: call — or fold.";
        public const string LockedAnswerSoul = "The House wagers more: match it — or fold.";
        public const string LockedNothingToCall = "There is nothing to call.";

        // ------------------------------------------------------------------ the soul (no numbers, ever)

        public const string SoulLabel = "YOUR SOUL";
        public const string SoulOnTable = "on the table";
        public const string WagerMore = "WAGER MORE";
        public const string WagerAll = "ALL OF IT";
        public const string MatchIt = "MATCH IT";
        public const string SoulPromptBet = "Your soul is on the table. Deal when you dare.";
        public const string SoulPromptReRaise = "The House wagers more of your soul! Match it — or fold?";
        public const string SoulStakeInfo = "Win: your soul mends   ·   Lose: it burns";
        public const string SoulWinFormat = "{0} beats {1}.  Your soul mends.";
        public const string SoulLossFormat = "{0} beats your {1}.  Your soul burns.";
        public const string SoulFold = "You fold. A piece of your soul stays on the table.";
        public const string SoulTakenMessage = "Your soul is on the table now.";
        public const string SoulReleasedMessage = "Your soul is your own again.";

        // ------------------------------------------------------------------ changing tables

        public const string LeaveTable = "LEAVE TABLE";
        public const string SoulBound = "SOUL BOUND";
        public const string NoEscape = "NO ESCAPE";
        public const string ChangeTable = "CHANGE TABLE";
        public const string SitAnyway = "SIT ANYWAY";
        public const string Locked = "LOCKED";
        public const string ReturnToTable = "RETURN";
        public const string Safe = "SAFE";
        public const string SoulAtStake = "SOUL AT STAKE";
        public const string ChooseTableSubtitle = "Your sentence goes with you. Mind each demon's soul line.";

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
