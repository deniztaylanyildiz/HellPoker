using HellPoker.Core.Chapters;

namespace HellPoker.Presentation.Ui
{
    /// <summary>Phase 2's words: the chapters' map, its panels, the floors' coin tables and the faces met there.</summary>
    internal static partial class UiText
    {
        // ------------------------------------------------------------------ the title menu

        public static string ChaptersButton => L("PHASE 2 TEST", "PHASE 2 TESTİ");
        public static string ChaptersContinueButton => L("PHASE 2: GO ON", "PHASE 2: DEVAM");

        // ------------------------------------------------------------------ the map

        public static string ChapterName(int chapter)
        {
            switch (chapter)
            {
                case 1: return L("MAMMON'S VAULT", "MAMMON'UN KASASI");
                case 2: return L("BELIAL'S THEATRE", "BELIAL'IN TİYATROSU");
                default: return L("LILITH'S GARDEN", "LILITH'İN BAHÇESİ");
            }
        }

        /// <summary>{0} the chapter's number, {1} its name.</summary>
        public static string ChapterTitleFormat => L("CHAPTER {0}: {1}", "{0}. BÖLÜM: {1}");
        public static string MapFloorFormat => L("FLOOR {0}", "KAT {0}");
        public static string MapCoinsLabel => L("COINS", "COIN");
        public static string MapTributeFormat => L("Tribute at the gate: {0}", "Kapıda haraç: {0}");
        /// <summary>{0} the years of the run's share at the chapter's demon (his bar), {1} his name with its genitive, in a sentence.</summary>
        public static string MapYearsFormat => L("{1} bar: {0} years", "{1} barı: {0} yıl");
        public static string MapOwedFormat => L("Owed at the gate: +{0} years", "Kapıda borç: +{0} yıl");
        public static string MapPromptStart => L("Choose where to start.", "Nereden başlayacağını seç.");
        public static string MapPrompt => L("Choose your way down.", "Aşağı inen yolunu seç.");
        public static string MapKeysHint => L("Click a lit node  ·  arrows + Enter  ·  Esc menu", "Yanan düğüme tıkla  ·  oklar + Enter  ·  Esc menü");
        public static string MapRelicsLabel => L("RELICS", "EMANETLER");
        public static string MapNoRelics => L("none", "yok");
        public static string MapSilenced => L("silenced", "susturuldu");
        public static string MapEyeReady => L("The imp's eye waits for the next imp.", "İblisin gözü sıradaki iblisi bekliyor.");

        public static string NodeName(NodeKind kind)
        {
            switch (kind)
            {
                case NodeKind.Table: return L("IMP'S TABLE", "İBLİS MASASI");
                case NodeKind.Event: return L("A STRANGER", "BİR YABANCI");
                case NodeKind.BlackMarket: return L("BLACK MARKET", "KARA PAZAR");
                case NodeKind.Warden: return L("THE WARDEN", "BEKÇİ");
                case NodeKind.Treasure: return L("TREASURE", "HAZİNE");
                default: return L("PURGATORY FIRE", "ARAF ATEŞİ");
            }
        }

        /// <summary>{0} the imp's coins, {1} the first ante, {2} hands between the ante's steps.</summary>
        public static string NodeTableFormat => L("An imp with {0} coins. Play until one purse is empty. Ante {1}, +1 every {2} hands.",
            "{0} coinli bir iblis. Bir kese boşalana kadar oynanır. Ante {1}, her {2} elde +1.");

        /// <summary>{0} his coins, {1} the first ante, {2} hands between the ante's steps, {3} his toll percent, {4} the toll's most a hand.</summary>
        public static string NodeWardenFormat => L("The Collector, {0} coins and his cheats. Empty his purse: a relic. Ante {1}, +1 every {2} hands. He takes {3}% of your coins from every hand you win (at most {4}).",
            "Tahsildar: {0} coin ve hileleri. Kesesini boşalt: bir emanet. Ante {1}, her {2} elde +1. Kazandığın her elde coin'inin %{3} kadarını alır (en çok {4}).");
        public static string NodeEvent => L("Someone waits here with an offer.", "Burada biri bir teklifle bekliyor.");
        public static string NodeMarket => L("Cursed relics and services, for coins.", "Lanetli emanetler ve hizmetler, coin karşılığı.");
        public static string NodeTreasureFormat => L("Coins between the bones: +{0}.", "Kemiklerin arasında coin: +{0}.");
        public static string NodeFire => L("The last floor: one boon for the demon's table.", "Son kat: şeytanın masası için bir lütuf.");

        // ------------------------------------------------------------------ panels

        public static string PanelOn => L("ON", "DEVAM");
        public static string PanelLeave => L("LEAVE", "AYRIL");
        public static string PanelDescend => L("DESCEND", "İN");

        /// <summary>{0} floors, {1} the demon's name, {2} tribute, {3} years a missing coin, {4} coins to start.</summary>
        public static string IntroFormat => L(
            "{0} floors down to {1}'s table. On the floors you play for COINS: your sentence stays as it is. " +
            "If your purse empties at a table, the run is over.\n" +
            "At the gate {1} takes a tribute of {2} coins. Every coin you lack is {3} years on his bar.\n" +
            "Then his table: your share of the sentence is his health bar. Empty it to pass.\n\nYou start with {4} coins.",
            "{1} masasına {0} kat. Katlarda COIN için oynarsın: cezan olduğu gibi kalır. " +
            "Bir masada kesen boşalırsa koşu biter.\n" +
            "Kapıda {1} {2} coin haraç alır. Eksik her coin barına {3} yıl ekler.\n" +
            "Sonra masası: cezanın ona düşen payı onun can barıdır. Geçmek için boşalt.\n\n{4} coin ile başlarsın.");

        public static string TreasureTitle => L("TREASURE", "HAZİNE");
        public static string TreasureTextFormat => L("Coins scattered between the bones of the last ones who came this way.  +{0} coins.",
            "Bu yoldan son geçenlerin kemikleri arasına saçılmış coinler.  +{0} coin.");
        public static string TreasureTake => L("TAKE", "AL");

        public static string MatchWonTitle => L("ITS PURSE IS EMPTY", "KESESİ BOŞALDI");

        /// <summary>{0} hands played, {1} hands won, {2} coins the hands brought (signed).</summary>
        public static string MatchTextFormat => L("Its purse ran dry. Hands played: {0}, won: {1}.\nThe hands: {2} coins.",
            "Kesesi boşaldı. Oynanan el: {0}, kazanılan: {1}.\nEller: {2} coin.");
        public static string GambleOverTitle => L("THE GHOST'S HAND", "HAYALETİN ELİ");

        /// <summary>{0} coins the hand brought (signed).</summary>
        public static string GambleTextFormat => L("One hand, played. {0} coins.", "Tek el oynandı. {0} coin.");
        public static string PurseEmptyTitle => L("YOUR PURSE IS EMPTY", "KESEN BOŞALDI");

        /// <summary>{0} who emptied it, {1} hands at that table.</summary>
        public static string PurseEmptyTextFormat => L("{0} took your last coin (hands at its table: {1}). Without a coin there is no way down: the run is over, and you are damned.",
            "{0} son coinini aldı (masasında oynanan el: {1}). Coinsiz aşağı inilmez: koşu bitti, lanetlendin.");
        public static string MatchTollFormat => L("The Collector's share: −{0} coins.", "Tahsildar'ın payı: −{0} coin.");

        /// <summary>{0} relic name, {1} gift, {2} curse.</summary>
        public static string MatchRelicFormat => L("He drops a relic: {0}.\n+ {1}\n− {2}", "Bir emanet düşürdü: {0}.\n+ {1}\n− {2}");
        public static string MatchPurseFormat => L("Coins now: {0}.", "Şimdi coin: {0}.");

        public static string WardenRelicTitle => L("THE COLLECTOR'S RELIC", "TAHSİLDAR'IN EMANETİ");

        /// <summary>{0} relic name, {1} gift, {2} curse, {3} the coins instead.</summary>
        public static string WardenRelicTextFormat => L("{0}\n+ {1}\n− {2}\n\nYou carry all you may. Take +{3} coins instead — or give up a relic for it.",
            "{0}\n+ {1}\n− {2}\n\nTaşıyabileceğin kadar taşıyorsun. Yerine +{3} coin al — ya da bir emanetini bunun için bırak.");
        public static string WardenCoinsFormat => L("+{0} COINS", "+{0} COIN");
        public static string WardenSwapFormat => L("SWAP: {0}", "DEĞİŞ: {0}");
        public static string WardenSwapDetailFormat => L("{0} goes; the new relic comes. No coins.", "{0} gider, yeni emanet gelir. Coin yok.");

        public static string MarketTitle => L("BLACK MARKET", "KARA PAZAR");
        public static string MarketOwner => L("THE FENCE", "ÇALINTI MAL TÜCCARI");
        public static string MarketTextFormat => L("Everything here is cursed. Everything here is for sale. You have {0} coins.",
            "Burada her şey lanetli. Burada her şey satılık. {0} coin'in var.");

        /// <summary>{0} name, {1} price.</summary>
        public static string MarketRelicFormat => L("{0} · {1}", "{0} · {1}");

        /// <summary>{0} gift, {1} curse.</summary>
        public static string MarketRelicDetailFormat => L("+ {0}   − {1}", "+ {0}   − {1}");
        public static string MarketDropFormat => L("THROW AWAY {0} · {1}", "AT: {0} · {1}");
        public static string MarketDropDetail => L("The relic goes, its gift and its curse with it: a free slot.",
            "Emanet gider, lütfu ve laneti de: bir yer boşalır.");
        public static string MarketEyeFormat => L("IMP'S EYE · {0}", "İBLİSİN GÖZÜ · {0}");
        public static string MarketEyeName => L("the imp's eye", "iblisin gözü");
        public static string MarketEyeDetail => L("At the next imp's table one of its cards plays face up from the deal.",
            "Sıradaki iblis masasında kartlarından biri dağıtımdan itibaren açık oynanır.");
        public static string MarketJokerInFormat => L("A JOKER IN · {0}", "JOKER EKLE · {0}");
        public static string MarketJokerOutFormat => L("A JOKER OUT · {0}", "JOKER ÇIKAR · {0}");

        /// <summary>{0} jokers in the deck now.</summary>
        public static string MarketJokerDetailFormat => L("Your deck holds {0} jokers. Two in one hand lose it.", "Destende {0} joker var. Bir elde iki joker eli kaybettirir.");
        public static string MarketNoCoins => L("Not enough coins. Nothing here takes your last coin.", "Yeterli coin yok. Burada hiçbir şey son coinini almaz.");
        public static string MarketFull => L("You carry all the relics you may.", "Taşıyabileceğin kadar emanet taşıyorsun.");
        public static string MarketBoughtFormat => L("Bought: {0}.", "Alındı: {0}.");
        public static string MarketEyeWaits => L("An eye already waits for the next imp.", "Sıradaki iblis için zaten bir göz bekliyor.");
        public static string MarketLeaveDetail => L("Back to the map.", "Haritaya dön.");

        public static string FireTitle => L("PURGATORY FIRE", "ARAF ATEŞİ");

        /// <summary>{0} the demon's name.</summary>
        public static string FireTextFormat => L("The last floor before the gate. The fire grants one boon — for {0}'s table.",
            "Kapıdan önceki son kat. Ateş bir lütuf verir — {0} masası için.");
        public static string FireBreak => L("BREAK HIS FIRST CHEAT", "İLK HİLESİNİ BOZ");

        /// <summary>{0} the demon's name.</summary>
        public static string FireBreakDetailFormat => L("The first minor cheat {0} tries at his table is broken.", "{0} masasında denediği ilk küçük hile bozulur.");
        public static string FireSilence => L("SILENCE A CURSE", "BİR LANETİ SUSTUR");
        public static string FireSilenceDetail => L("A relic's curse is silent until the chapter ends — the demon's table included. Its gift stays.",
            "Bir emanetin laneti bölüm bitene kadar susar — şeytanın masası dahil. Lütfu kalır.");
        public static string FireSilenceNone => L("You carry no relic.", "Hiç emanet taşımıyorsun.");
        public static string FireShuffle => L("SHUFFLE · FREE ANTE", "KARIŞTIR · ANTE'SİZ");

        /// <summary>{0} the free ante's years.</summary>
        public static string FireShuffleDetailFormat => L("The deck is shuffled whole, and the first hand at the demon's table has an ante of only {0} year.",
            "Deste baştan karılır ve şeytanın masasındaki ilk elin antesi yalnızca {0} yıl olur.");
        public static string FireSilenceWhich => L("WHICH CURSE?", "HANGİ LANET?");
        public static string FireSilenceWhichText => L("The fire burns one curse quiet.", "Ateş bir laneti susturur.");

        public static string FireDoneBreak => L("The fire marks the demon's first cheat. It will fail.", "Ateş şeytanın ilk hilesini damgaladı. Tutmayacak.");

        /// <summary>{0} relic name.</summary>
        public static string FireDoneSilenceFormat => L("{0} is silent until the chapter ends.", "{0} bölüm bitene kadar sessiz.");
        public static string FireDoneShuffle => L("The deck is whole again; the demon's first ante will be light.", "Deste yeniden tam; şeytanın ilk antesi hafif olacak.");

        public static string GateTitle => L("THE VAULT GATE", "KASA KAPISI");

        /// <summary>{0} the demon's name, {1} the tribute, {2} coins on hand.</summary>
        public static string GateTextFormat => L("{0} takes his tribute: {1} coins. You have {2}.", "{0} haracını alır: {1} coin. Sende {2} var.");

        /// <summary>{0} coins missing, {1} the years.</summary>
        public static string GateShortFormat => L("You lack {0} coins: +{1} years on your sentence.", "{0} coin eksiğin var: cezana +{1} yıl.");

        /// <summary>{0} coins left over.</summary>
        public static string GatePaidFormat => L("Paid in full. {0} coins go on with you.", "Tamamı ödendi. {0} coin seninle devam eder.");
        public static string GateOwedFormat => L("The floors' bargains come due: +{0} years.", "Katlardaki pazarlıkların bedeli: +{0} yıl.");

        /// <summary>{0} the demon's bar as his table begins (years).</summary>
        public static string GateAfterFormat => L("His bar: {0} years of your sentence. Empty it — or lose your soul trying.",
            "Barı: cezandan {0} yıl. Boşalt — ya da bu uğurda ruhunu kaybet.");
        public static string GatePay => L("PAY AND SIT", "ÖDE VE OTUR");

        public static string ChapterDoneTitle => L("THE VAULT IS BEHIND YOU", "KASA ARKANDA KALDI");

        /// <summary>{0} the demon, {1} hands, {2} coins carried on.</summary>
        public static string ChapterDoneTextFormat => L("{0} is beaten: his bar emptied in {1} hands.\nCoins carried on: {2}.\n\nThe test ends here: the next chapter is not open yet.",
            "{0} yenildi: barı {1} elde boşaldı.\nYanında kalan coin: {2}.\n\nTest burada bitiyor: sonraki bölüm henüz açık değil.");
        public static string ChapterDamnedTitle => L("DAMNED", "LANETLENDİN");

        /// <summary>{0} the demon.</summary>
        public static string ChapterDamnedTextFormat => L("Your soul burned away at {0}'s table. The run is over.", "Ruhun {0} masasında yanıp kül oldu. Koşu bitti.");
        public static string ChapterNewRun => L("NEW RUN", "YENİ KOŞU");

        // ------------------------------------------------------------------ the floors' offers (events)

        public static string FloorEventOwner(string eventId)
        {
            switch (eventId)
            {
                case FloorEventIds.Usurer: return L("A USURER", "BİR TEFECİ");
                case FloorEventIds.MammonsLedger: return L("MAMMON", "MAMMON");
                default: return L("A GHOST", "BİR HAYALET");
            }
        }

        public static string FloorEventTitle(string eventId)
        {
            switch (eventId)
            {
                case FloorEventIds.Usurer: return L("THE USURER OF PURGATORY", "ARAF TEFECİSİ");
                case FloorEventIds.MammonsLedger: return L("MAMMON'S LEDGER", "MAMMON'UN DEFTERİ");
                default: return L("THE GAMBLER'S GHOST", "KUMARBAZ HAYALET");
            }
        }

        /// <summary>Usurer: {0} coins, {1} years. Ledger: {0} years now, {1} years back. Ghost: {0} the stake, {1} the multiplier cap.</summary>
        public static string FloorEventTextFormat(string eventId)
        {
            switch (eventId)
            {
                case FloorEventIds.Usurer: return L("\"{0} coins, friend, right now. Mammon will add {1} years to your sentence when you sit at his table. A fair rate. For Hell.\"",
                    "\"{0} coin, dostum, hemen şimdi. Masasına oturduğunda Mammon cezana {1} yıl ekleyecek. Makul bir faiz. Cehennem için.\"");
                case FloorEventIds.MammonsLedger: return L("\"I strike {0} years off your sentence now. At my table I write back {1}. Interest, you understand.\"",
                    "\"Cezandan şimdi {0} yıl siliyorum. Masamda {1} yıl geri yazarım. Faiz, anlarsın.\"");
                default: return L("\"Half your purse — {0} coins — on one hand. No raises, no folding. Lose and the half is mine; win and it pays up to ×{1}.\"",
                    "\"Kesenin yarısı — {0} coin — tek bir ele. Artırma yok, çekilme yok. Kaybedersen yarısı benim; kazanırsan ×{1} katına kadar öder.\"");
            }
        }

        // ------------------------------------------------------------------ the coin tables

        public static string CoinsLabel => L("COINS", "COIN");
        public static string TributeLineFormat => L("Tribute {0}", "Haraç {0}");
        public static string CoinAnteFormat => L("{0} COINS", "{0} COIN");
        public static string CoinPromptBetFormat => L("The ante is {0} coins. Deal.", "Ante {0} coin. Dağıt.");
        public static string CoinStakeInfoFormat => L("Win: at least +{0} coins   ·   Lose: at least −{1} coins",
            "Kazanç: en az +{0} coin   ·   Kayıp: en az −{1} coin");
        public static string CoinPromptReRaiseFormat => L("Raised back {0} coins! Call — or fold?", "{0} coin artırdı! Gör — ya da çekil?");

        /// <summary>{0} player hand, {1} house hand, {2} coins.</summary>
        public static string CoinWinFormat => L("{0} beats {1}.  +{2} coins.", "{0}, {1} elini yendi.  +{2} coin.");

        /// <summary>{0} house hand, {1} player hand, {2} coins.</summary>
        public static string CoinLossFormat => L("{0} beats your {1}.  −{2} coins.", "{0}, senin {1} elini yendi.  −{2} coin.");
        public static string CoinFoldFormat => L("You fold.  −{0} coins.", "Çekildin.  −{0} coin.");
        public static string CoinPushFormat => L("Push — {0} against {1}. The coins stay.", "Berabere — {0}, {1} karşısında. Coinler yerinde.");
        public static string CoinHouseFoldedFormat => L("It gives up its hand rather than face your raise.  +{0} coins.",
            "Artırmana karşı durmak yerine elini bıraktı.  +{0} coin.");

        /// <summary>{0} coins lost (the jokers' own wording, signed by the outcome).</summary>
        public static string CoinJokerLossFormat => L("Two jokers break your hand.  −{0} coins.", "İki joker elini bozdu.  −{0} coin.");
        public static string CoinJokerWinFormat => L("Two jokers break its hand.  +{0} coins.", "İki joker onun elini bozdu.  +{0} coin.");
        public static string CoinTollFormat => L("The Collector takes his share: −{0} coins.", "Tahsildar payını alıyor: −{0} coin.");
        public static string HouseFoldedCaption => L("GAVE UP", "ELİ BIRAKTI");

        /// <summary>{0} the imp's coins, {1} the ante now, {2} hands until it grows.</summary>
        public static string FloorSeatTitleFormat => L("Purse {0} · ante {1} (+1 in {2})", "Kesesi {0} · ante {1} ({2} elde +1)");

        /// <summary>{0} the stake (the gambler's ghost: no purse to show).</summary>
        public static string GambleSeatTitleFormat => L("One hand · {0} coins", "Tek el · {0} coin");

        /// <summary>{0} this hand.</summary>
        public static string BossSeatTitleFormat => L("Hand {0} · empty his bar", "El {0} · barını boşalt");

        /// <summary>{0} this hand (the soul is on the table).</summary>
        public static string BossSoulSeatTitleFormat => L("Hand {0} · your soul is on the table", "El {0} · ruhun masada");

        /// <summary>{0} the demon's name with its genitive ("MAMMON'S" / "MAMMON'UN"): the counter's label over the health bar.</summary>
        public static string BarLabelFormat => L("{0} BAR", "{0} BARI");

        /// <summary>The end of the bar the counter fills toward (no number).</summary>
        public static string BarSoulLine => L("Your soul at the end", "Sonunda ruhun");

        /// <summary>{0} the demon's name with its genitive, in a sentence ("Mammon's" / "Mammon'un").</summary>
        public static string BossBeatenFormat => L("{0} bar is empty. He is beaten.", "{0} barı boşaldı. Yenildi.");

        /// <summary>{0} the demon's name with its genitive, in a sentence.</summary>
        public static string BossDeadMansHandFormat => L("The Dead Man's Hand empties {0} bar at once. He is beaten.",
            "Ölü Adamın Eli {0} barını bir anda boşalttı. Yenildi.");
        public static string ToTheMap => L("TO THE MAP", "HARİTAYA");
        public static string LeaveTheVault => L("LEAVE THE VAULT", "KASADAN ÇIK");
        public static string ChapterTheEnd => L("ON", "DEVAM");

        // ------------------------------------------------------------------ the faces of the floors

        private static readonly DealerText Imp = new DealerText
        {
            Name = "COIN IMP",
            Title = "One of Mammon's counters",
            Description = "Small, greedy and bad at cards. Counts every coin twice — badly.",
            Greeting = new[] { "Coins! You have coins! Sit, sit, let me count them for you.", "A customer! Ante up, ante up." },
            PlayerWins = new[] { "Hey! Those were MY coins!", "That is not how I counted it.", "Mammon will hear of this..." },
            HouseWins = new[] { "Mine! Mine mine mine.", "Shiny. Thank you.", "Into the purse you go." },
            PlayerFolds = new[] { "Running? Leave the ante, leave the ante!", "Ha! Scared of a little imp?" },
            Push = new[] { "Even? I hate even." },
            FinalStretch = new[] { "Raise, raise!" },
            ReRaise = new[] { "More! I want more!", "I raise too! Look how big I am!" },
            Sealed = new[] { "No take-backs!", "The pot is full. Ooh." },
            Backfire = new[] { "That was not supposed to happen." },
            Blocked = new[] { "No fair!" },
            JokerBust = new[] { "Two of them?! Who put two in?" },
            JokerJackpot = new[] { "Twenty?! I can't count that high!" },
            GreetingAsPeasant = "A peasant! Still has coins, though. Sit.",
            GreetingAsWarlock = "A warlock. Don't hex my purse. Sit.",
            GreetingAsKing = "A king! Kings have the most coins. Sit, Your Majesty!",
            GreetingAsJester = "A jester! I like jokers. I like coins more. Sit."
        };

        private static readonly DealerText ImpTr = new DealerText
        {
            Name = "SİKKE İBLİSİ",
            Genitive = "SİKKE İBLİSİNİN",
            Called = "Sikke İblisi",
            CalledGenitive = "Sikke İblisinin",
            Title = "Mammon'un sayıcılarından",
            Description = "Küçük, açgözlü ve kartta kötü. Her coini iki kez sayar — yanlış.",
            Greeting = new[] { "Coin! Coinin var! Otur otur, senin için sayayım.", "Bir müşteri! Ante, ante!" },
            PlayerWins = new[] { "Hey! Onlar BENİM coinlerimdi!", "Ben öyle saymamıştım.", "Mammon bunu duyacak..." },
            HouseWins = new[] { "Benim! Benim benim benim.", "Parlak. Teşekkürler.", "Haydi keseye." },
            PlayerFolds = new[] { "Kaçıyor musun? Anteyi bırak, anteyi bırak!", "Ha! Küçük bir iblisten mi korktun?" },
            Push = new[] { "Berabere mi? Berabereden nefret ederim." },
            FinalStretch = new[] { "Artır, artır!" },
            ReRaise = new[] { "Daha fazla! Daha fazla istiyorum!", "Ben de artırıyorum! Bak ne kadar büyüğüm!" },
            Sealed = new[] { "Geri almak yok!", "Masa doldu. Ooo." },
            Backfire = new[] { "Böyle olmamalıydı." },
            Blocked = new[] { "Haksızlık!" },
            JokerBust = new[] { "İki tane mi?! İki tane kim koydu?" },
            JokerJackpot = new[] { "Yirmi mi?! O kadar sayamam!" },
            GreetingAsPeasant = "Bir köylü! Yine de coini var. Otur.",
            GreetingAsWarlock = "Bir büyücü. Keseme büyü yapma. Otur.",
            GreetingAsKing = "Bir kral! Krallarda en çok coin olur. Buyrun Majesteleri!",
            GreetingAsJester = "Bir soytarı! Jokerleri severim. Coinleri daha çok. Otur."
        };

        private static readonly DealerText Collector = new DealerText
        {
            Name = "THE COLLECTOR",
            Title = "Golden-Eyed, Mammon's warden",
            Description = "Guards the way to the vault and takes his share of every coin that passes. His eyes see every purse.",
            Greeting = new[] { "Halt. Nothing passes to the vault without my counting it. Sit." },
            PlayerWins = new[] { "A win. My share, then.", "Noted. And taxed.", "You win; I collect. That is the arrangement." },
            HouseWins = new[] { "Collected.", "Into the strongbox.", "Every coin finds its way to the vault." },
            PlayerFolds = new[] { "A withdrawal. The ledger approves.", "Leaving coins on the table is a kind of tax too." },
            Push = new[] { "Even. Nothing to collect. Regrettable." },
            FinalStretch = new[] { "Pay." },
            ReRaise = new[] { "I raise the levy.", "An additional assessment." },
            Sealed = new[] { "Sealed. Assessed. Final.", "The levy is fixed." },
            Backfire = new[] { "An error in the books. It will be corrected." },
            Blocked = new[] { "You dare refuse a collection?", "Warded. I will remember the expense." },
            JokerBust = new[] { "Two fools in my hand. That is not in the regulations." },
            JokerJackpot = new[] { "Twenty jokers. Unregistered. Irregular." },
            GreetingAsPeasant = "A peasant. Small purse, small share. Sit.",
            GreetingAsWarlock = "A warlock. Your signs do not exempt you. Sit.",
            GreetingAsKing = "A king. The crown is taxable. Sit.",
            GreetingAsJester = "A jester. Your jokers are declared, I trust. Sit."
        };

        private static readonly DealerText CollectorTr = new DealerText
        {
            Name = "TAHSİLDAR",
            Genitive = "TAHSİLDAR'IN",
            Called = "Tahsildar",
            CalledGenitive = "Tahsildar'ın",
            Title = "Altın Gözlü, Mammon'un bekçisi",
            Description = "Kasaya giden yolu bekler ve oradan geçen her coinden payını alır. Gözleri her keseyi görür.",
            Greeting = new[] { "Dur. Ben saymadan kasaya hiçbir şey geçmez. Otur." },
            PlayerWins = new[] { "Bir kazanç. O zaman payım.", "Kaydedildi. Ve vergilendirildi.", "Sen kazanırsın, ben toplarım. Anlaşma bu." },
            HouseWins = new[] { "Tahsil edildi.", "Kasaya.", "Her coin kasaya yolunu bulur." },
            PlayerFolds = new[] { "Bir çekilme. Defter onaylıyor.", "Masada coin bırakmak da bir çeşit vergidir." },
            Push = new[] { "Berabere. Tahsil edilecek bir şey yok. Üzücü." },
            FinalStretch = new[] { "Öde." },
            ReRaise = new[] { "Vergiyi artırıyorum.", "Ek bir tahakkuk." },
            Sealed = new[] { "Mühürlendi. Tahakkuk etti. Kesin.", "Vergi sabitlendi." },
            Backfire = new[] { "Defterde bir hata. Düzeltilecek." },
            Blocked = new[] { "Bir tahsilatı reddetmeye mi cüret ediyorsun?", "Koruma. Masrafı unutmayacağım." },
            JokerBust = new[] { "Elimde iki soytarı. Bu yönetmelikte yok." },
            JokerJackpot = new[] { "Yirmi joker. Kayıt dışı. Usulsüz." },
            GreetingAsPeasant = "Bir köylü. Küçük kese, küçük pay. Otur.",
            GreetingAsWarlock = "Bir büyücü. İşaretlerin seni vergiden muaf tutmaz. Otur.",
            GreetingAsKing = "Bir kral. Taç da vergiye tabidir. Otur.",
            GreetingAsJester = "Bir soytarı. Jokerlerini beyan ettin, umarım. Otur."
        };

        /// <summary>The floors' faces; null for anyone else (the demons are in <see cref="Dealer"/>).</summary>
        private static DealerText ChapterFace(string id, bool turkish)
        {
            switch (id)
            {
                case ChapterCast.ImpId: return turkish ? ImpTr : Imp;
                case ChapterCast.CollectorId: return turkish ? CollectorTr : Collector;
                default: return null;
            }
        }
    }
}
