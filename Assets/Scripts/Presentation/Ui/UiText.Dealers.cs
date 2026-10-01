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
        public string Absolved;
        public string Damned;
    }

    internal static partial class UiText
    {
        public const string ChooseDealerTitle = "CHOOSE YOUR DEALER";
        public const string ChooseDealerSubtitle = "Three demons keep a table in Hell. Each plays by their own rules.";
        public const string Challenge = "SIT DOWN";

        public const string TraitDrawFormat = "Exchange up to {0} cards";
        public const string TraitRevealFormat = "Shows {0} of the House's cards before you must commit";
        public const string TraitRevealOne = "Shows only 1 House card before you must commit";
        public const string TraitPayoutFormat = "Pays Pair ×{0}  ·  Flush ×{1}  ·  Full House ×{2}";
        public const string TraitLossFormat = "Lose: + {0}   ·   Fold: + {1}";

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
            Absolved = "Paid in full?! Impossible... Get out before I find an error in the books.",
            Damned = "Your account is closed. Forever. Next!"
        };

        private static readonly DealerText Belial = new DealerText
        {
            Name = "BELIAL",
            Title = "The Silver Tongue",
            Description = "Pays like a prince and collects like a tyrant. Keeps most of his hand hidden until it is far too late.",
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
                "Half again what you bet. Read the fine print, darling."
            },
            PlayerFolds = new[] { "Leaving so soon? The night was young.", "Prudence. How very... mortal." },
            Push = new[] { "A draw. How terribly polite of us." },
            FinalStretch = new[] { "The gates! I can almost see your hope. Raise — the show must go on." },
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
