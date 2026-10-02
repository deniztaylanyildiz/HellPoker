using System;
using HellPoker.Core.Dealers;

namespace HellPoker.Presentation.Ui
{
    /// <summary>What each demon dealer is called and says.</summary>
    internal sealed class DealerText
    {
        public string Name;
        public string Title;
        public string Description;
        public string[] Greeting;
        public string[] PlayerWins;
        public string[] HouseWins;
        public string[] PlayerFolds;
        public string[] Push;
        public string[] FinalStretch;
        public string[] ReRaise;
        public string[] SoulTaken;
        public string[] SoulReleased;
        public string[] SoulLocked;

        /// <summary>The table is full and the pact sealed: the rest of the hand plays out on its own.</summary>
        public string[] Sealed;

        /// <summary>The player closed the game mid-hand and came back: the hand was forfeited.</summary>
        public string[] Fled;

        /// <summary>The warning before sitting at this demon's table with the soul already past their line.</summary>
        public string SoulWarning;
        public string Absolved;
        public string Damned;
    }

    internal static partial class UiText
    {
        public const string ChooseDealerTitle = "CHOOSE YOUR DEALER";
        public const string SoulLineCardFormat = "SOUL AT {0}";
        public const string ChooseDealerSubtitle = "Three demons keep a table in Hell. Each plays by their own rules.";
        public const string Challenge = "SIT DOWN";

        public const string TraitDrawFormat = "Exchange up to {0} cards";
        public const string TraitRevealFormat = "Shows {0} House cards before your last bet";
        public const string TraitRevealOne = "Shows only 1 House card before your last bet";
        public const string TraitPayoutFormat = "Pair ×{0} · Flush ×{1} · Full ×{2} · Royal ×{3}";
        /// <summary>{0} loss surcharge (see <see cref="UiText.LossSurcharge"/>).</summary>
        public const string TraitLossFormat = "Lose: same, on the House's hand{0}";
        /// <summary>{0}, {1}: <see cref="UiText.ShareShort"/> before and after the draw.</summary>
        public const string TraitFoldFormat = "Fold: {0} before the draw, {1} after";
        public const string TraitFoldSameFormat = "Fold: always {0} of the stake";
        /// <summary>{0} weakest strong hand, {1} re-raise percent with it, {2} bluff percent.</summary>
        public const string TraitTemperFormat = "Re-raises with {0}+ {1}% · bluffs {2}%";

        /// <summary>How a share of the stake reads in rule texts: "half the stake", "the stake", "1.5 × the stake"...</summary>
        public static string StakeShare(int percent)
        {
            switch (percent)
            {
                case 0: return "nothing";
                case 25: return "a quarter of the stake";
                case 50: return "half the stake";
                case 100: return "the stake";
                case 200: return "twice the stake";
                default: return (percent / 100f).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " × the stake";
            }
        }

        public static DealerText Dealer(string id)
        {
            switch (id)
            {
                case DealerRoster.MammonId: return Mammon;
                case DealerRoster.BelialId: return Belial;
                case DealerRoster.LilithId: return Lilith;
                default: return Unknown;
            }
        }

        private static readonly DealerText Mammon = new DealerText
        {
            Name = "MAMMON",
            Title = "The Usurer of the Ninth Vault",
            Description = "Counts every year twice and plays strictly by the book. The fairest table in Hell — which is not saying much.",
            Greeting = new[] { "Sit, sit. Every year you owe is written here. Let us see how many you can buy back." },
            PlayerWins = new[]
            {
                "A loan forgiven... I shall add it to someone else's bill.",
                "Hmph. Mark it in the ledger. In red ink.",
                "Enjoy it. Interest is compounding somewhere."
            },
            HouseWins = new[]
            {
                "Ahh, the sweet sound of a growing debt.",
                "Another entry for the ledger. You do keep me busy.",
                "Gold, years, souls — it all adds up, my friend."
            },
            PlayerFolds = new[] { "A partial payment. Wise, and still profitable.", "Running from a bad debt? Half of it follows you." },
            Push = new[] { "Even. How dreadfully unprofitable." },
            FinalStretch = new[] { "So close to settling the account. No more cheap bets — pay up or walk away." },
            ReRaise = new[]
            {
                "I'll see that, and I'll charge interest.",
                "A fine figure. Let us make it finer.",
                "Collateral, please. A little more."
            },
            SoulTaken = new[] { "The account is overdrawn. I'll take your soul as collateral." },
            SoulReleased = new[] { "Paid back? Hmph. The collateral is returned. For now." },
            SoulLocked = new[] { "Leave? With my collateral? Sit down and pay." },
            Sealed = new[]
            {
                "Signed and witnessed. No backing out of this contract.",
                "The terms are final. Let the cards settle the account.",
                "Sealed in wax and ink. Now we see who pays."
            },
            Fled = new[]
            {
                "You walked out mid-hand? The ledger noticed. Debited, with interest.",
                "Skipping out on an open account? I charged it as forfeit."
            },
            SoulWarning = "Your soul will be on his table — and he never lets collateral walk.",
            Absolved = "Paid in full?! Impossible... Get out before I find an error in the books.",
            Damned = "Your account is closed. Forever. Next!"
        };

        private static readonly DealerText Belial = new DealerText
        {
            Name = "BELIAL",
            Title = "The Silver Tongue",
            Description = "Pays like a prince and lies like breathing. Keeps most of his hand hidden until it is far too late.",
            Greeting = new[] { "Charmed. Shall we make this interesting? I insist." },
            PlayerWins = new[]
            {
                "Bravo! Truly. I adore a soul with a little fight in it.",
                "Take it. Winning is how I keep you at my table.",
                "Lucky. Luck is a lady, and she owes me favours."
            },
            HouseWins = new[]
            {
                "Did you really think I'd show you everything?",
                "Pity. You were so sure of yourself.",
                "Read the fine print, darling. There always is some."
            },
            PlayerFolds = new[] { "Leaving so soon? The night was young.", "Prudence. How very... mortal." },
            Push = new[] { "A draw. How terribly polite of us." },
            FinalStretch = new[] { "The gates! I can almost see your hope. Raise — the show must go on." },
            ReRaise = new[]
            {
                "Oh, darling. Raise me? I raise you back.",
                "Do I have it? Perhaps. Do you dare find out?",
                "More. I love it when you squirm."
            },
            SoulTaken = new[] { "At last, the real stakes. Your soul, darling, on my table." },
            SoulReleased = new[] { "You slipped the hook. How... unexpectedly entertaining." },
            SoulLocked = new[] { "Leaving mid-performance? Not with your soul in my hands." },
            Sealed = new[]
            {
                "No exits now, darling. The curtain stays up.",
                "Sealed! Now we find out which of us was lying.",
                "Everything on the table. How deliciously final."
            },
            Fled = new[]
            {
                "Vanishing mid-act? I kept your ticket. And your wager.",
                "You left before the final scene, darling. The stakes stayed with me."
            },
            SoulWarning = "Your soul will be on his table — and he does not let an audience leave.",
            Absolved = "Well played. Do come back — I always win in the end.",
            Damned = "Welcome home. I saved you a seat. Forever."
        };

        private static readonly DealerText Lilith = new DealerText
        {
            Name = "LILITH",
            Title = "Queen of the Night",
            Description = "Lets you trade away four cards, for she enjoys a hopeful heart. But none leave her table cheaply.",
            Greeting = new[] { "Come closer. Throw away what you hate — I will give you something better. Perhaps." },
            PlayerWins = new[]
            {
                "Mmm. You may keep those years. For now.",
                "How delightful. Hope looks lovely on you.",
                "The night is long, little soul."
            },
            HouseWins = new[]
            {
                "Shh. It only hurts until forever.",
                "The moon saw everything. So did I.",
                "Another year in the dark with me. Is that so bad?"
            },
            PlayerFolds = new[] { "No one slips away from me. You pay in full.", "Running? The whole wager, sweet thing." },
            Push = new[] { "Neither of us bleeds tonight." },
            FinalStretch = new[] { "The gates are near... show me how badly you want them." },
            ReRaise = new[]
            {
                "Bold. I like bold. Bolder, then.",
                "You reach for me? I reach back.",
                "Mm. Pay a little more to stay in my dark."
            },
            SoulTaken = new[] { "There it is. Your soul, cold and lovely, in my hands." },
            SoulReleased = new[] { "You took it back. I will remember how it felt." },
            SoulLocked = new[] { "Go? Your soul stays with me, and so do you." },
            Sealed = new[]
            {
                "Bound now. Sit still and let the night decide.",
                "No running from this one, little soul.",
                "Sealed with a kiss. You cannot take it back."
            },
            Fled = new[]
            {
                "You slipped away in the dark. I kept what you left on my table.",
                "Running mid-hand? Nothing leaves me. That wager is mine."
            },
            SoulWarning = "Your soul will be on her table. She does not give back what she holds.",
            Absolved = "Go, then. The dawn will find you dull. You will miss me.",
            Damned = "Mine. All mine. For every night that ever was."
        };

        private static readonly DealerText Unknown = new DealerText
        {
            Name = "THE HOUSE",
            Title = "",
            Description = "",
            Greeting = new[] { "Sit down." },
            PlayerWins = new[] { "Hm." },
            HouseWins = new[] { "The House wins." },
            PlayerFolds = new[] { "Folded." },
            Push = new[] { "Push." },
            FinalStretch = new[] { "No passing now." },
            ReRaise = new[] { "Raise." },
            SoulTaken = new[] { "Your soul is mine to play for." },
            SoulReleased = new[] { "Your soul is yours again." },
            SoulLocked = new[] { "Not with your soul on the table." },
            Sealed = new[] { "Sealed." },
            Fled = new[] { "You left mid-hand. It is forfeit." },
            SoulWarning = "Your soul will be on this table.",
            Absolved = "You are free.",
            Damned = "Damned."
        };

        /// <summary>Picks a line by a counter, so the same moment does not always say the same thing (and stays testable).</summary>
        public static string Pick(string[] lines, int counter)
        {
            if (lines == null || lines.Length == 0) return "";
            return lines[Math.Abs(counter) % lines.Length];
        }
    }
}
