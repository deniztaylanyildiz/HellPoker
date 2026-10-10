using System;
using HellPoker.Core.Dealers;

namespace HellPoker.Presentation.Ui
{
    /// <summary>What each demon dealer is called and says.</summary>
    internal sealed class DealerText
    {
        public string Name;

        /// <summary>
        /// Turkish only: the name with the possessive suffix, in capitals like <see cref="Name"/> ("MAMMON'UN", "BELIAL'IN").
        /// Turkish suffixes follow the sound of the name, so they are written by hand. English leaves it null: Name + "'s".
        /// Read through <see cref="UiText.GenitiveOf"/>.
        /// </summary>
        public string Genitive;

        /// <summary>Turkish only: the name as it reads inside a sentence ("Lilith", "Sabah Yıldızı"); null: the name, capitalised.</summary>
        public string Called;

        /// <summary>Turkish only: <see cref="Called"/> with the possessive suffix ("Lilith'in", "Sabah Yıldızı'nın").</summary>
        public string CalledGenitive;

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

        /// <summary>The player came back from walking out on a hand the demon meant to cheat: scorn, and a grudge.</summary>
        public string[] Hunted;

        /// <summary>The player wants to start over (New Game over a run in progress): the demon mocks them for it.</summary>
        public string[] Scorn;

        /// <summary>The demon's own cheat helped the player (a backfire): said with an angry look.</summary>
        public string[] Backfire;

        /// <summary>The Warlock warded off the demon's cheat: said with an angry look.</summary>
        public string[] Blocked;

        /// <summary>An event between hands: the player took the offer / let it pass.</summary>
        public string[] EventAccepted;
        public string[] EventDeclined;

        /// <summary>The first word to a new run, by the sinner's class (null: the usual greeting).</summary>
        public string GreetingAsPeasant;
        public string GreetingAsWarlock;
        public string GreetingAsKing;
        public string GreetingAsJester;

        /// <summary>The demon held two jokers at the showdown and lost the hand for it (said angry).</summary>
        public string[] JokerBust;

        /// <summary>The Jester's deck reached twenty jokers (the demon is caught off guard).</summary>
        public string[] JokerJackpot;

        /// <summary>The greeting for a run of this class; null when the demon has none for it.</summary>
        public string GreetingFor(string classId)
        {
            switch (classId)
            {
                case HellPoker.Core.Sinners.Peasant.ClassId: return GreetingAsPeasant;
                case HellPoker.Core.Sinners.Warlock.ClassId: return GreetingAsWarlock;
                case HellPoker.Core.Sinners.King.ClassId: return GreetingAsKing;
                case HellPoker.Core.Sinners.Jester.ClassId: return GreetingAsJester;
                default: return null;
            }
        }

        /// <summary>The player is summoned to Lucifer from this demon's table: the demon's last word.</summary>
        public string[] Farewell;

        /// <summary>Lucifer cast the player down and they land back at this demon's table.</summary>
        public string[] Returned;

        /// <summary>Lucifer only: the player climbed back above the gate and is cast down.</summary>
        public string[] CastDown;

        /// <summary>Lucifer only: the greeting of the second summons, then of every later one (he remembers).</summary>
        public string[] Remembers;

        /// <summary>Lucifer only: what he says when the locked card on the choice screen is clicked.</summary>
        public string NotYet;

        /// <summary>The warning before sitting at this demon's table with the soul already past their line.</summary>
        public string SoulWarning;
        public string Absolved;
        public string Damned;
    }

    internal static partial class UiText
    {
        public static string ChooseDealerTitle => L("CHOOSE YOUR DEALER", "KURPİYERİNİ SEÇ");
        public static string SoulLineCardFormat => L("SOUL AT {0}", "RUH {0} YILDA");
        public static string ChooseDealerSubtitle => L("Three demons keep a table in Hell. Each plays by their own rules.",
            "Cehennemde üç şeytan masa kurar. Her biri kendi kuralıyla oynar.");
        public static string Challenge => L("SIT DOWN", "OTUR");

        public static string TraitDrawFormat => L("Exchange up to {0} cards", "En fazla {0} kart değişir");
        public static string TraitRevealFormat => L("Shows {0} House cards before your last bet", "Son bahisten önce kasa {0} kart açar");
        public static string TraitRevealOne => L("Shows only 1 House card before your last bet", "Son bahisten önce kasa yalnızca 1 kart açar");
        public static string TraitRevealNone => L("Shows none of his cards before the showdown", "Eller açılana kadar hiç kart göstermez");
        public static string TraitFixedStakesFormat => L("Fixed stakes: ante {0}, at most {1} on the table", "Sabit bahis: ante {0}, masada en fazla {1}");
        public static string TraitPayoutFormat => L("Pair ×{0} · Flush ×{1} · Full ×{2} · Royal ×{3}", "Çift ×{0} · Renk ×{1} · Full ×{2} · Royal ×{3}");
        /// <summary>{0} loss surcharge (see <see cref="UiText.LossSurcharge"/>).</summary>
        public static string TraitLossFormat => L("Lose: same, on the House's hand{0}", "Kayıp: aynısı, kasanın eliyle{0}");
        /// <summary>{0}, {1}: <see cref="UiText.ShareShort"/> before and after the draw.</summary>
        public static string TraitFoldFormat => L("Fold: {0} before the draw, {1} after", "Çekil: değişten önce {0}, sonra {1}");
        public static string TraitFoldSameFormat => L("Fold: always {0} of the stake", "Çekil: her zaman {0}");
        /// <summary>{0} weakest strong hand, {1} re-raise percent with it, {2} bluff percent.</summary>
        public static string TraitTemperFormat => L("Re-raises with {0}+ {1}% · bluffs {2}%", "{0}+ ile %{1} artırır · %{2} blöf");

        /// <summary>How a share of the stake reads in rule texts: "half the stake", "the stake", "1.5 × the stake"...</summary>
        public static string StakeShare(int percent)
        {
            switch (percent)
            {
                case 0: return L("nothing", "hiçbir şey");
                case 25: return L("a quarter of the stake", "bahsin çeyreği");
                case 50: return L("half the stake", "bahsin yarısı");
                case 100: return L("the stake", "bahsin tamamı");
                case 200: return L("twice the stake", "bahsin iki katı");
                default: return (percent / 100f).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + L(" × the stake", " × bahis");
            }
        }

        /// <summary>What a demon is called and says, in the current language.</summary>
        public static DealerText Dealer(string id)
        {
            bool tr = Lang.IsTurkish;
            switch (id)
            {
                case DealerRoster.MammonId: return tr ? MammonTr : Mammon;
                case DealerRoster.BelialId: return tr ? BelialTr : Belial;
                case DealerRoster.LilithId: return tr ? LilithTr : Lilith;
                case DealerRoster.LuciferId: return tr ? LuciferTr : Lucifer;
                default: return ChapterFace(id, tr) ?? (tr ? UnknownTr : Unknown);
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
            Hunted = new[]
            {
                "You ran from my collateral? Where to? This is Hell. I charge interest on running.",
                "Skipped out before I could collect? Then I collect twice. Soon, and often."
            },
            Scorn = new[]
            {
                "Leaving with your account open? Afraid of the interest?",
                "Run along. But this is Hell, debtor. Where would you go?",
                "Scared of a few numbers? Close the book, then. I keep a copy."
            },
            JokerJackpot = new[] { "Twenty jokers?! That is not in any ledger I keep.", "Twenty! Who let the fools into my vault?" },
            JokerBust = new[] { "Two jokers. My own ledger laughs at me.", "Two fools in my hand — and I am the third." },
            Backfire = new[]
            {
                "A clerical error. It will not happen twice.",
                "That entry was... unfortunate. Strike it from the record.",
                "Even my ledger has a bad day. Enjoy it."
            },
            Blocked = new[] { "A seal on the account? Who taught you that trick?", "Warded. I will note the expense." },
            EventAccepted = new[] { "A deal struck. I do love a signature.", "Accepted. The terms will find you." },
            EventDeclined = new[] { "Prudent. Unprofitable, but prudent.", "No? The offer goes back in the drawer." },
            GreetingAsPeasant = "A peasant. Nothing to your name but the debt. Sit, let us count it.",
            GreetingAsWarlock = "A warlock. Keep your little signs off my ledger. Sit.",
            GreetingAsKing = "A king! How fortunate. A crown counts as collateral.",
            GreetingAsJester = "A jester. Your jokes are not in my ledger — but your jokers are. Sit.",
            Farewell = new[]
            {
                "Your debt is nearly paid... and someone else has noticed you. My condolences.",
                "Ah. The account is being transferred. Downstairs."
            },
            Returned = new[]
            {
                "Back already? The ledger reopens. With interest.",
                "Thrown down like an old coin. Sit. We have accounts to settle."
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
            Hunted = new[]
            {
                "Ran from my little trick? Darling, this is Hell. Every exit leads back to my stage.",
                "You fled the scene. I'll write you a crueller one, and soon."
            },
            Scorn = new[]
            {
                "Leaving before the final act? Stage fright, darling?",
                "Exit, pursued by nothing. Where would you go? It's Hell all the way down.",
                "Walking out on me? How dull. I had such lies left to tell you."
            },
            JokerJackpot = new[] { "Twenty jokers! Darling, you stole my show.", "Twenty! Even I could not have written that." },
            JokerBust = new[] { "Two jokers! The joke was on me, darling. Applause, I suppose.", "A double act — and I was the punchline." },
            Backfire = new[]
            {
                "My tongue... slipped.",
                "That was not the line I rehearsed.",
                "Applause, darling. Do not get used to it."
            },
            Blocked = new[] { "A ward? Darling, you have read ahead in the script.", "Spoiled my best trick. How rude." },
            EventAccepted = new[] { "Bravo! A player who says yes.", "Oh, this will be a lovely scene." },
            EventDeclined = new[] { "No? You disappoint the audience, darling.", "Cautious. How very... dull." },
            GreetingAsPeasant = "A peasant in the front row! Do try to keep up, darling.",
            GreetingAsWarlock = "A warlock. How tiresome — you will see through my lines.",
            GreetingAsKing = "A king! Finally, an audience worthy of the show.",
            GreetingAsJester = "A fellow performer! Bring your jokers, darling — two in a hand and the joke is on you.",
            Farewell = new[]
            {
                "Oh dear. He has noticed you. Do try to be entertaining, darling.",
                "The curtain falls for me. Yours is about to rise... below."
            },
            Returned = new[]
            {
                "An encore! I knew you could not stay away.",
                "Fell all the way back to me? How touching."
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
            Hunted = new[]
            {
                "You fled my thorns? Where to? This is Hell. I find you in every dark.",
                "You ran from the night. The night remembers, and it grows hungrier."
            },
            Scorn = new[]
            {
                "Afraid of the dark already, little one?",
                "Run, then. The night is everywhere here.",
                "You came to my garden and you flee it? How very mortal."
            },
            JokerJackpot = new[] { "Twenty jokers... the night itself is laughing.", "Twenty. How did you— no matter. Take your toy." },
            JokerBust = new[] { "Two jokers in my hand. Even the night laughs at me.", "Two fools. I will remember this, little jester." },
            Backfire = new[]
            {
                "The night chose you over me. How rude.",
                "Even my thorns have favourites. Not for long."
            },
            Blocked = new[] { "You turned my night aside. Clever little soul.", "A ward against me? I will find the gap." },
            EventAccepted = new[] { "Mm. You reach for what the dark offers.", "Yes. Let the night take its share." },
            EventDeclined = new[] { "You fear the gift. Wise, little soul.", "No? The night will ask again." },
            GreetingAsPeasant = "A peasant, all calloused hands. Come, rest them in the dark.",
            GreetingAsWarlock = "A warlock. You know a little of the night. Not enough.",
            GreetingAsKing = "A king. Crowns slip so easily in the dark.",
            GreetingAsJester = "A jester. Let us see who laughs last in the dark.",
            Farewell = new[]
            {
                "Shh. Someone older than the night is calling you. Go.",
                "He has noticed you, little soul. Even I do not keep him waiting."
            },
            Returned = new[]
            {
                "You fell back into my dark. I kept your place warm.",
                "Cast down? Come here. The night forgives what he does not."
            },
            SoulWarning = "Your soul will be on her table. She does not give back what she holds.",
            Absolved = "Go, then. The dawn will find you dull. You will miss me.",
            Damned = "Mine. All mine. For every night that ever was."
        };

        /// <summary>The Morning Star. Calm, proud, never raises his voice; short sentences. His name is never spoken at the table.</summary>
        private static readonly DealerText Lucifer = new DealerText
        {
            Name = "THE MORNING STAR",
            Title = "Waits below 250 years",
            Description = "Every sentence ends at his table. Nobody has seen more of him than his eyes.",
            Greeting = new[] { "You climbed down far. Sit. Only I can let you go." },
            Remembers = new[]
            {
                "Again. I remember your hands. Sit.",
                "Once more. I have watched you fall every time. Sit."
            },
            PlayerWins = new[]
            {
                "Take it. It changes nothing.",
                "A small mercy. I can afford many.",
                "Good. Hope burns brighter before it goes out."
            },
            HouseWins = new[]
            {
                "As expected.",
                "You were never going to win that one.",
                "Climb. I will wait."
            },
            PlayerFolds = new[] { "Fear. At last, something honest.", "Folding at my table costs everything. You knew." },
            Push = new[] { "Nothing. For now." },
            FinalStretch = new[] { "So close. I can hear you hoping." },
            ReRaise = new[]
            {
                "Higher.",
                "Do you see me? Then pay to see more.",
                "I raise. You tremble. That is the order of things."
            },
            Sealed = new[]
            {
                "Bound. As every soul is, in the end.",
                "No more choices. Only cards.",
                "Sealed. Watch them turn."
            },
            Fled = new[] { "You closed your eyes. I did not. The wager is mine.", "There is no leaving in the middle. Not from me." },
            Hunted = new[] { "You ran from my fire. There is nowhere that is not mine.", "Flee the flame, and it follows. It always follows." },
            Scorn = new[] { "You think you can leave me? I am where everything ends.", "Go. Begin again. You will fall to me all the same." },
            JokerJackpot = new[] { "Twenty jokers. Amusing. Briefly." },
            JokerBust = new[] { "Two jokers. Laugh, then. Laugh while you can." },
            Backfire = new[]
            {
                "The fire chose. Not I.",
                "Chance is the one thing I do not rule. Remember that.",
                "Keep it. You will need it."
            },
            Blocked = new[] { "A ward. Against me. Amusing.", "Your little magic held. Once." },
            CastDown = new[]
            {
                "Too heavy. Fall back where you came from.",
                "Not yet worthy. Down you go.",
                "Climb back to me. If you can."
            },
            SoulTaken = new[] { "Your soul. I have held finer ones." },
            SoulReleased = new[] { "Keep it. It was never worth much." },
            SoulLocked = new[] { "Leave? Nothing leaves my table.", "Sit down." },
            SoulWarning = "",
            Farewell = new[] { "" },
            Returned = new[] { "" },
            NotYet = "Not yet. Come down to me.",
            Absolved = "...Go. The morning will come without me.",
            Damned = "Forever, then. You will learn to like the dark."
        };

        private static readonly DealerText Unknown = new DealerText
        {
            Farewell = new[] { "Someone below wants you." },
            Returned = new[] { "Back again." },
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
            Hunted = new[] { "You ran from my trick. I will not forget it." },
            Scorn = new[] { "Leaving so soon? Afraid?" },
            JokerJackpot = new[] { "Twenty jokers?!" },
            JokerBust = new[] { "Two jokers. The joke is on me." },
            Backfire = new[] { "That was not meant for you." },
            Blocked = new[] { "Warded. Hm." },
            EventAccepted = new[] { "So be it." },
            EventDeclined = new[] { "As you wish." },
            SoulWarning = "Your soul will be on this table.",
            Absolved = "You are free.",
            Damned = "Damned."
        };

        // ================================================================== Türkçe (the same demons, the same voices)

        private static readonly DealerText MammonTr = new DealerText
        {
            Name = "MAMMON",
            Genitive = "MAMMON'UN",
            Called = "Mammon",
            CalledGenitive = "Mammon'un",
            Title = "Dokuzuncu Kasanın Tefecisi",
            Description = "Her yılı iki kez sayar ve kitabına harfiyen uyar. Cehennemin en adil masası — ki bu pek bir şey söylemez.",
            Greeting = new[] { "Otur, otur. Borçlu olduğun her yıl burada yazılı. Bakalım kaçını geri alabileceksin." },
            PlayerWins = new[]
            {
                "Bir borç affedildi... Başkasının hesabına yazarım.",
                "Hıh. Deftere işle. Kırmızı mürekkeple.",
                "Tadını çıkar. Faiz bir yerlerde işliyor."
            },
            HouseWins = new[]
            {
                "Ahh, büyüyen bir borcun tatlı sesi.",
                "Deftere bir kalem daha. Beni hep meşgul ediyorsun.",
                "Altın, yıl, ruh — hepsi toplanır, dostum."
            },
            PlayerFolds = new[] { "Kısmi ödeme. Akıllıca, yine de kârlı.", "Kötü borçtan mı kaçıyorsun? Yarısı peşinden gelir." },
            Push = new[] { "Berabere. Ne korkunç derecede kârsız." },
            FinalStretch = new[] { "Hesabı kapatmaya bu kadar yakınken ucuz bahis yok. Öde ya da çekil." },
            ReRaise = new[]
            {
                "Görürüm, üstüne faiz de koyarım.",
                "Güzel bir rakam. Daha da güzelleştirelim.",
                "Teminat lütfen. Biraz daha."
            },
            SoulTaken = new[] { "Hesap eksiye düştü. Ruhunu teminat olarak alıyorum." },
            SoulReleased = new[] { "Ödendi mi? Hıh. Teminat iade edildi. Şimdilik." },
            SoulLocked = new[] { "Gitmek mi? Teminatımla mı? Otur ve öde." },
            Sealed = new[]
            {
                "İmzalandı, tanıklandı. Bu sözleşmeden dönüş yok.",
                "Şartlar kesin. Hesabı kartlar kapatsın.",
                "Mumla ve mürekkeple mühürlendi. Bakalım kim ödeyecek."
            },
            Fled = new[]
            {
                "Elin ortasında çıkıp gittin mi? Defter fark etti. Faiziyle borçlandırıldın.",
                "Açık hesabı bırakıp kaçmak mı? Ceza olarak işledim."
            },
            Hunted = new[]
            {
                "Teminatımdan mı kaçtın? Nereye? Burası cehennem. Kaçmanın da faizi var.",
                "Tahsil etmeden sıvıştın mı? O zaman iki kez tahsil ederim. Yakında ve sık sık."
            },
            Scorn = new[]
            {
                "Hesabın açıkken mi gidiyorsun? Faizden mi korktun?",
                "Git bakalım. Ama burası cehennem, borçlu. Nereye gideceksin?",
                "Birkaç rakamdan mı korktun? Kapat defteri. Bende bir kopyası var."
            },
            JokerJackpot = new[] { "Yirmi joker mi?! Bu hiçbir defterimde yok.", "Yirmi! Soytarıları kasama kim soktu?" },
            JokerBust = new[] { "İki joker. Kendi defterim bana gülüyor.", "Elimde iki soytarı — üçüncüsü de benim." },
            Backfire = new[]
            {
                "Bir kâtip hatası. İki kez olmaz.",
                "O kayıt... talihsizdi. Defterden silin.",
                "Defterimin bile kötü günü olur. Tadını çıkar."
            },
            Blocked = new[] { "Hesaba mühür mü? Bu numarayı sana kim öğretti?", "Savuşturuldu. Masrafını not ediyorum." },
            EventAccepted = new[] { "Anlaşma tamam. İmzaya bayılırım.", "Kabul edildi. Şartlar seni bulur." },
            EventDeclined = new[] { "Tedbirli. Kârsız, ama tedbirli.", "Hayır mı? Teklif çekmeceye geri döner." },
            GreetingAsPeasant = "Bir köylü. Adına borçtan başka bir şey yok. Otur, sayalım.",
            GreetingAsWarlock = "Bir büyücü. İşaretlerini defterimden uzak tut. Otur.",
            GreetingAsKing = "Bir kral! Ne şans. Taç da teminat sayılır.",
            GreetingAsJester = "Bir soytarı. Şakaların defterimde yok — ama jokerlerin var. Otur.",
            Farewell = new[]
            {
                "Borcun neredeyse bitti... ve biri seni fark etti. Başın sağ olsun.",
                "Ah. Hesap devrediliyor. Aşağıya."
            },
            Returned = new[]
            {
                "Bu kadar çabuk mu döndün? Defter yeniden açıldı. Faiziyle.",
                "Eski bir sikke gibi fırlatıldın. Otur. Kapatacak hesaplarımız var."
            },
            SoulWarning = "Ruhun onun masasına konacak — ve o teminatı asla bırakmaz.",
            Absolved = "Tamamen mi ödendi?! İmkânsız... Defterde bir hata bulmadan defol.",
            Damned = "Hesabın kapandı. Sonsuza dek. Sıradaki!"
        };

        private static readonly DealerText BelialTr = new DealerText
        {
            Name = "BELIAL",
            Genitive = "BELIAL'IN",
            Called = "Belial",
            CalledGenitive = "Belial'in",
            Title = "Gümüş Dil",
            Description = "Prens gibi öder, nefes alır gibi yalan söyler. Elinin çoğunu iş işten geçene kadar saklar.",
            Greeting = new[] { "Büyülendim. Bunu ilginç hale getirelim mi? Israr ediyorum." },
            PlayerWins = new[]
            {
                "Bravo! Gerçekten. İçinde biraz direniş olan ruhlara bayılırım.",
                "Al. Kazanmak seni masamda tutmamın yolu.",
                "Şanslısın. Şans bir hanımefendidir ve bana borçludur."
            },
            HouseWins = new[]
            {
                "Sana her şeyi göstereceğimi mi sandın?",
                "Yazık. Kendinden o kadar emindin ki.",
                "İnce yazıyı oku, canım. Hep vardır."
            },
            PlayerFolds = new[] { "Bu kadar erken mi? Gece daha gençti.", "Tedbir. Ne kadar da... ölümlüce." },
            Push = new[] { "Berabere. Ne kadar da kibarız." },
            FinalStretch = new[] { "Kapılar! Umudunu neredeyse görebiliyorum. Artır — gösteri devam etmeli." },
            ReRaise = new[]
            {
                "Ah, canım. Beni mi artırıyorsun? Ben de seni.",
                "Bende var mı? Belki. Öğrenmeye cesaretin var mı?",
                "Daha fazla. Kıvranmanı seviyorum."
            },
            SoulTaken = new[] { "Sonunda gerçek bahis. Ruhun, canım, masamda." },
            SoulReleased = new[] { "Oltadan kurtuldun. Ne kadar da... beklenmedik eğlenceli." },
            SoulLocked = new[] { "Gösterinin ortasında mı? Ruhun elimdeyken olmaz." },
            Sealed = new[]
            {
                "Artık çıkış yok, canım. Perde açık kalıyor.",
                "Mühürlendi! Şimdi hangimizin yalan söylediğini göreceğiz.",
                "Her şey masada. Ne nefis bir son."
            },
            Fled = new[]
            {
                "Perde ortasında kaybolmak mı? Biletini sakladım. Bahsini de.",
                "Son sahneden önce çıktın, canım. Bahis bende kaldı."
            },
            Hunted = new[]
            {
                "Küçük numaramdan mı kaçtın? Canım, burası cehennem. Her çıkış sahneme döner.",
                "Sahneden kaçtın. Sana daha acımasız birini yazacağım, hem de yakında."
            },
            Scorn = new[]
            {
                "Son perdeden önce mi gidiyorsun? Sahne korkusu mu, canım?",
                "Çıkış, peşinde kimse yok. Nereye gideceksin? Aşağısı da cehennem.",
                "Beni mi bırakıyorsun? Ne sıkıcı. Daha anlatacak ne yalanlarım vardı."
            },
            JokerJackpot = new[] { "Yirmi joker! Canım, gösterimi çaldın.", "Yirmi! Bunu ben bile yazamazdım." },
            JokerBust = new[] { "İki joker! Şaka bana döndü, canım. Alkış, galiba.", "Çifte numara — ve espri bendim." },
            Backfire = new[]
            {
                "Dilim... kaydı.",
                "Prova ettiğim replik bu değildi.",
                "Alkış, canım. Alışma."
            },
            Blocked = new[] { "Koruma mı? Canım, senaryoyu önden okumuşsun.", "En iyi numaramı bozdun. Ne kaba." },
            EventAccepted = new[] { "Bravo! Evet diyen bir oyuncu.", "Ah, bu çok güzel bir sahne olacak." },
            EventDeclined = new[] { "Hayır mı? Seyirciyi hayal kırıklığına uğratıyorsun, canım.", "Temkinli. Ne kadar da... sıkıcı." },
            GreetingAsPeasant = "Ön sırada bir köylü! Yetişmeye çalış, canım.",
            GreetingAsWarlock = "Bir büyücü. Ne yorucu — repliklerimin içini göreceksin.",
            GreetingAsKing = "Bir kral! Sonunda gösteriye layık bir seyirci.",
            GreetingAsJester = "Bir meslektaş! Jokerlerini getir, canım — elde iki tane olursa şaka sana döner.",
            Farewell = new[]
            {
                "Eyvah. O seni fark etti. Eğlendirici olmaya çalış, canım.",
                "Benim perdem iniyor. Seninki açılmak üzere... aşağıda."
            },
            Returned = new[]
            {
                "Bis! Uzak duramayacağını biliyordum.",
                "Bana kadar mı düştün? Ne dokunaklı."
            },
            SoulWarning = "Ruhun onun masasına konacak — ve o seyircinin gitmesine izin vermez.",
            Absolved = "İyi oynadın. Yine gel — sonunda hep ben kazanırım.",
            Damned = "Evine hoş geldin. Sana bir koltuk ayırdım. Sonsuza dek."
        };

        private static readonly DealerText LilithTr = new DealerText
        {
            Name = "LILITH",
            Genitive = "LILITH'IN",
            Called = "Lilith",
            CalledGenitive = "Lilith'in",
            Title = "Gecenin Kraliçesi",
            Description = "Dört kart değiştirmene izin verir, umutlu kalpleri sever. Ama kimse masasından ucuza kalkamaz.",
            Greeting = new[] { "Yaklaş. Nefret ettiğini at — sana daha iyisini veririm. Belki." },
            PlayerWins = new[]
            {
                "Mmm. O yıllar sende kalsın. Şimdilik.",
                "Ne hoş. Umut sana yakışıyor.",
                "Gece uzun, küçük ruh."
            },
            HouseWins = new[]
            {
                "Şşş. Sadece sonsuza dek acıtır.",
                "Ay her şeyi gördü. Ben de.",
                "Benimle karanlıkta bir yıl daha. O kadar kötü mü?"
            },
            PlayerFolds = new[] { "Kimse benden sıvışamaz. Tamamını ödersin.", "Kaçıyor musun? Bahsin tamamı, tatlım." },
            Push = new[] { "Bu gece ikimiz de kanamıyoruz." },
            FinalStretch = new[] { "Kapılar yakın... onları ne kadar istediğini göster bana." },
            ReRaise = new[]
            {
                "Cesur. Cesuru severim. Daha cesur o zaman.",
                "Bana mı uzanıyorsun? Ben de sana uzanırım.",
                "Mm. Karanlığımda kalmak için biraz daha öde."
            },
            SoulTaken = new[] { "İşte orada. Ruhun, soğuk ve güzel, ellerimde." },
            SoulReleased = new[] { "Geri aldın. Nasıl hissettirdiğini unutmayacağım." },
            SoulLocked = new[] { "Gitmek mi? Ruhun benimle kalır, sen de." },
            Sealed = new[]
            {
                "Artık bağlısın. Kıpırdama, bırak gece karar versin.",
                "Bundan kaçış yok, küçük ruh.",
                "Bir öpücükle mühürlendi. Geri alamazsın."
            },
            Fled = new[]
            {
                "Karanlıkta sıvıştın. Masamda bıraktığını aldım.",
                "Elin ortasında kaçmak mı? Benden hiçbir şey kurtulmaz. O bahis benim."
            },
            Hunted = new[]
            {
                "Dikenlerimden mi kaçtın? Nereye? Burası cehennem. Seni her karanlıkta bulurum.",
                "Geceden kaçtın. Gece unutmaz, ve daha da acıkır."
            },
            Scorn = new[]
            {
                "Karanlıktan şimdiden mi korktun, küçüğüm?",
                "Kaç o zaman. Burada gece her yerde.",
                "Bahçeme geldin, şimdi kaçıyor musun? Ne kadar da ölümlüce."
            },
            JokerJackpot = new[] { "Yirmi joker... gece bile gülüyor.", "Yirmi. Nasıl— neyse. Oyuncağını al." },
            JokerBust = new[] { "Elimde iki joker. Gece bile bana gülüyor.", "İki soytarı. Bunu unutmayacağım, küçük soytarı." },
            Backfire = new[]
            {
                "Gece seni bana tercih etti. Ne kaba.",
                "Dikenlerimin bile gözdeleri var. Uzun sürmez."
            },
            Blocked = new[] { "Gecemi yana çevirdin. Akıllı küçük ruh.", "Bana karşı koruma mı? Açığını bulurum." },
            EventAccepted = new[] { "Mm. Karanlığın sunduğuna uzanıyorsun.", "Evet. Bırak gece payını alsın." },
            EventDeclined = new[] { "Hediyeden korkuyorsun. Akıllıca, küçük ruh.", "Hayır mı? Gece yine soracak." },
            GreetingAsPeasant = "Bir köylü, nasırlı ellerle. Gel, onları karanlıkta dinlendir.",
            GreetingAsWarlock = "Bir büyücü. Geceden biraz anlarsın. Yetmez.",
            GreetingAsKing = "Bir kral. Taçlar karanlıkta ne kolay kayar.",
            GreetingAsJester = "Bir soytarı. Bakalım karanlıkta son gülen kim olacak.",
            Farewell = new[]
            {
                "Şşş. Geceden de yaşlı biri seni çağırıyor. Git.",
                "Seni fark etti, küçük ruh. Ben bile onu bekletmem."
            },
            Returned = new[]
            {
                "Karanlığıma geri düştün. Yerini sıcak tuttum.",
                "Düşürüldün mü? Gel buraya. Gece onun affetmediğini affeder."
            },
            SoulWarning = "Ruhun onun masasına konacak. O tuttuğunu geri vermez.",
            Absolved = "Git o zaman. Şafak seni sıkıcı bulacak. Beni özleyeceksin.",
            Damned = "Benim. Tamamen benim. Var olmuş her gece boyunca."
        };

        private static readonly DealerText LuciferTr = new DealerText
        {
            Name = "SABAH YILDIZI",
            Genitive = "SABAH YILDIZI'NIN",
            Called = "Sabah Yıldızı",
            CalledGenitive = "Sabah Yıldızı'nın",
            Title = "250 yılın altında bekler",
            Description = "Her ceza onun masasında biter. Kimse ondan gözlerinden fazlasını görmedi.",
            Greeting = new[] { "Çok derine indin. Otur. Seni yalnızca ben bırakabilirim." },
            Remembers = new[]
            {
                "Yine. Ellerini hatırlıyorum. Otur.",
                "Bir kez daha. Her düşüşünü izledim. Otur."
            },
            PlayerWins = new[]
            {
                "Al. Hiçbir şey değişmez.",
                "Küçük bir merhamet. Çoğunu karşılayabilirim.",
                "İyi. Umut sönmeden önce daha parlak yanar."
            },
            HouseWins = new[]
            {
                "Beklendiği gibi.",
                "O eli asla kazanamayacaktın.",
                "Tırman. Beklerim."
            },
            PlayerFolds = new[] { "Korku. Sonunda dürüst bir şey.", "Masamda çekilmek her şeye mal olur. Biliyordun." },
            Push = new[] { "Hiçbir şey. Şimdilik." },
            FinalStretch = new[] { "Çok yakın. Umut ettiğini duyabiliyorum." },
            ReRaise = new[]
            {
                "Daha yüksek.",
                "Beni görüyor musun? O zaman daha fazlası için öde.",
                "Ben artırırım. Sen titrersin. Düzen budur."
            },
            Sealed = new[]
            {
                "Bağlandın. Sonunda her ruh gibi.",
                "Artık seçim yok. Sadece kartlar.",
                "Mühürlendi. Dönüşlerini izle."
            },
            Fled = new[] { "Sen gözlerini kapadın. Ben kapamadım. Bahis benim.", "Ortadan kalkmak yok. Benden olmaz." },
            Hunted = new[] { "Ateşimden kaçtın. Benim olmayan bir yer yok.", "Alevden kaç, peşinden gelir. Hep gelir." },
            Scorn = new[] { "Beni bırakabileceğini mi sanıyorsun? Her şey bende biter.", "Git. Baştan başla. Yine de bana düşeceksin." },
            JokerJackpot = new[] { "Yirmi joker. Eğlenceli. Kısa bir süre." },
            JokerBust = new[] { "İki joker. Gül o zaman. Gülebildiğin kadar." },
            Backfire = new[]
            {
                "Ateş seçti. Ben değil.",
                "Şans hükmetmediğim tek şey. Bunu unutma.",
                "Sende kalsın. İhtiyacın olacak."
            },
            Blocked = new[] { "Bir koruma. Bana karşı. Eğlenceli.", "Küçük büyün tuttu. Bir kez." },
            CastDown = new[]
            {
                "Fazla ağır. Geldiğin yere düş.",
                "Henüz layık değilsin. Aşağı.",
                "Bana geri tırman. Yapabilirsen."
            },
            SoulTaken = new[] { "Ruhun. Daha iyilerini tuttum." },
            SoulReleased = new[] { "Sende kalsın. Zaten pek değerli değildi." },
            SoulLocked = new[] { "Gitmek mi? Masamdan hiçbir şey gitmez.", "Otur." },
            SoulWarning = "",
            Farewell = new[] { "" },
            Returned = new[] { "" },
            NotYet = "Henüz değil. Bana in.",
            Absolved = "...Git. Sabah bensiz de gelecek.",
            Damned = "Sonsuza dek o halde. Karanlığı sevmeyi öğreneceksin."
        };

        private static readonly DealerText UnknownTr = new DealerText
        {
            Farewell = new[] { "Aşağıda biri seni istiyor." },
            Returned = new[] { "Yine geldin." },
            Name = "KASA",
            Genitive = "KASANIN",
            Called = "Kasa",
            CalledGenitive = "Kasanın",
            Title = "",
            Description = "",
            Greeting = new[] { "Otur." },
            PlayerWins = new[] { "Hm." },
            HouseWins = new[] { "Kasa kazanır." },
            PlayerFolds = new[] { "Çekildin." },
            Push = new[] { "Berabere." },
            FinalStretch = new[] { "Artık pas yok." },
            ReRaise = new[] { "Artırıyorum." },
            SoulTaken = new[] { "Ruhun artık oynanacak." },
            SoulReleased = new[] { "Ruhun yeniden senin." },
            SoulLocked = new[] { "Ruhun masadayken olmaz." },
            Sealed = new[] { "Mühürlendi." },
            Fled = new[] { "Elin ortasında gittin. Ceza yazıldı." },
            Hunted = new[] { "Hilemden kaçtın. Unutmayacağım." },
            Scorn = new[] { "Bu kadar erken mi? Korktun mu?" },
            JokerJackpot = new[] { "Yirmi joker mi?!" },
            JokerBust = new[] { "İki joker. Şaka bana döndü." },
            Backfire = new[] { "O sana değildi." },
            Blocked = new[] { "Savuşturuldu. Hm." },
            EventAccepted = new[] { "Öyle olsun." },
            EventDeclined = new[] { "Nasıl istersen." },
            SoulWarning = "Ruhun bu masaya konacak.",
            Absolved = "Özgürsün.",
            Damned = "Lanetlendin."
        };
        /// <summary>Picks a line by a counter, so the same moment does not always say the same thing (and stays testable).</summary>
        public static string Pick(string[] lines, int counter)
        {
            if (lines == null || lines.Length == 0) return "";
            return lines[Math.Abs(counter) % lines.Length];
        }
    }
}
