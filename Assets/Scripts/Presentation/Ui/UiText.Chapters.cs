using HellPoker.Core.Chapters;
using HellPoker.Core.Dealers;

namespace HellPoker.Presentation.Ui
{
    /// <summary>Phase 2's words: the chapters' map, its panels, the floors' coin tables, the demons' bars and the faces met there.</summary>
    internal static partial class UiText
    {
        // ------------------------------------------------------------------ the title menu

        public static string ChaptersButton => L("PHASE 2: NEW RUN", "PHASE 2: YENİ KOŞU");
        public static string ChaptersContinueButton => L("PHASE 2: CONTINUE", "PHASE 2: DEVAM ET");
        public static string ChaptersAbandonWarning => L("Your Phase 2 run will be lost — and counted as damned.",
            "Süren Phase 2 koşun kaybedilir — ve lanet sayılır.");

        /// <summary>{0} runs, {1} freed, {2} damned, {3} Lucifer reached, {4} fewest hands to freedom.</summary>
        public static string RecordsPhase2Format => L("PHASE 2: runs {0} · free {1} · damned {2} · Lucifer met {3} · fastest {4}",
            "PHASE 2: koşu {0} · kurtulan {1} · lanet {2} · Lucifer'e varan {3} · en hızlı {4}");

        /// <summary>{0} the class's whole sentence, {1} its starting purse (the class choice of a Phase 2 run).</summary>
        public static string ChapterSinnerStartFormat => L("Starts: {0} years · {1} coins", "Başlangıç: {0} yıl · {1} coin");

        // ------------------------------------------------------------------ the map

        public static string ChapterName(int chapter)
        {
            switch (chapter)
            {
                case 1: return L("MAMMON'S VAULT", "MAMMON'UN KASASI");
                case 2: return L("BELIAL'S STAGE", "BELIAL'İN SAHNESİ");
                default: return L("LILITH'S NIGHT", "LILITH'İN GECESİ");
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
        public static string MapDesired => L("desired: twice", "arzu: iki kat");
        public static string MapEyeReady => L("An eye waits for your next table.", "Sıradaki masan için bir göz bekliyor.");
        public static string MapSpectacleReady => L("The Spectacle waits at your next table.", "Sıradaki masanda Gösteri bekliyor.");
        public static string MapInsomniaReady => L("Insomnia: free hands at your next table.", "Uykusuzluk: sıradaki masanda ante'siz eller.");

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

        /// <summary>{0} his coins, {1} the first ante, {2} hands between the ante's steps, {3} his toll percent, {4} the toll's most a hand,
        /// {5} the warden's name.</summary>
        public static string NodeWardenFormat => L("{5}: {0} coins. Empty that purse: a relic. Ante {1}, +1 every {2} hands. Every hand you win, {3}% of your coins goes to the warden (at most {4}).",
            "{5}: {0} coin. Kesesini boşalt: bir emanet. Ante {1}, her {2} elde +1. Kazandığın her elde coin'inin %{3} kadarını alır (en çok {4}).");

        /// <summary>The warden's trick in a chapter.</summary>
        public static string WardenTrick(string bossId)
        {
            switch (bossId)
            {
                case DealerRoster.BelialId: return L("Every hand one of the warden's open cards is a lie. A warlock sees it.",
                    "Her elde açık kartlarından biri yalandır. Büyücü yalanı görür.");
                case DealerRoster.LilithId: return L("One of the cards you draw comes face down; you see it at the showdown.",
                    "Değiştirdiğin kartlardan biri kapalı gelir; showdown'da açılır.");
                default: return L("The warden plays Mammon's minor cheats.", "Mammon'un küçük hileleriyle oynar.");
            }
        }
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
            "At the gate {1} takes a tribute of {2} coins. Every coin you lack is {3} years on the bar.\n" +
            "Then the demon's table: your share of the sentence is the health bar. Empty it to pass.\n\nYour purse: {4} coins.",
            "{1} masasına {0} kat. Katlarda COIN için oynarsın: cezan olduğu gibi kalır. " +
            "Bir masada kesen boşalırsa koşu biter.\n" +
            "Kapıda {1} {2} coin haraç alır. Eksik her coin barına {3} yıl ekler.\n" +
            "Sonra masası: cezanın ona düşen payı onun can barıdır. Geçmek için boşalt.\n\nKesen: {4} coin.");

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
        public static string MatchTollFormat => L("The warden's share: −{0} coins.", "Bekçinin payı: −{0} coin.");

        /// <summary>{0} relic name, {1} gift, {2} curse.</summary>
        public static string MatchRelicFormat => L("It drops a relic: {0}.\n+ {1}\n− {2}", "Bir emanet düşürdü: {0}.\n+ {1}\n− {2}");
        public static string MatchPurseFormat => L("Coins now: {0}.", "Şimdi coin: {0}.");

        public static string WardenRelicTitle => L("THE WARDEN'S RELIC", "BEKÇİNİN EMANETİ");

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
        public static string MarketEyeDetail => L("At your next table one of the house's cards plays face up from the deal.",
            "Sıradaki masanda karşı tarafın kartlarından biri dağıtımdan itibaren açık oynanır.");
        public static string MarketJokerInFormat => L("A JOKER IN · {0}", "JOKER EKLE · {0}");
        public static string MarketJokerOutFormat => L("A JOKER OUT · {0}", "JOKER ÇIKAR · {0}");

        /// <summary>{0} jokers in the deck now.</summary>
        public static string MarketJokerDetailFormat => L("Your deck holds {0} jokers. Two in one hand lose it.", "Destende {0} joker var. Bir elde iki joker eli kaybettirir.");
        public static string MarketNoCoins => L("Not enough coins. Nothing here takes your last coin.", "Yeterli coin yok. Burada hiçbir şey son coinini almaz.");
        public static string MarketFull => L("You carry all the relics you may.", "Taşıyabileceğin kadar emanet taşıyorsun.");
        public static string MarketBoughtFormat => L("Bought: {0}.", "Alındı: {0}.");
        public static string MarketEyeWaits => L("An eye already waits for your next table.", "Sıradaki masan için zaten bir göz bekliyor.");
        public static string MarketLeaveDetail => L("Back to the map.", "Haritaya dön.");

        public static string FireTitle => L("PURGATORY FIRE", "ARAF ATEŞİ");

        /// <summary>{0} the demon's name.</summary>
        public static string FireTextFormat => L("The last floor before the gate. The fire grants one boon — for {0}'s table.",
            "Kapıdan önceki son kat. Ateş bir lütuf verir — {0} masası için.");
        public static string FireBreak => L("BREAK THE FIRST CHEAT", "İLK HİLESİNİ BOZ");

        /// <summary>{0} the demon's name.</summary>
        public static string FireBreakDetailFormat => L("The first minor cheat {0} tries at the table is broken.", "{0} masasında denediği ilk küçük hile bozulur.");
        public static string FireSilence => L("SILENCE A CURSE", "BİR LANETİ SUSTUR");
        public static string FireSilenceDetail => L("A relic's curse is silent until the chapter ends — the demon's table included. Its gift stays.",
            "Bir emanetin laneti bölüm bitene kadar susar — şeytanın masası dahil. Lütfu kalır.");
        public static string FireSilenceNone => L("You carry no relic.", "Hiç emanet taşımıyorsun.");
        public static string FireRest => L("REST BY THE FIRE", "ATEŞTE DİNLEN");

        /// <summary>{0} the demon's name with its genitive, in a sentence; {1} the percent.</summary>
        public static string FireRestDetailFormat => L("{0} bar starts {1}% shorter.", "{0} barı %{1} kısa başlar.");
        public static string FireSilenceWhich => L("WHICH CURSE?", "HANGİ LANET?");
        public static string FireSilenceWhichText => L("The fire burns one curse quiet.", "Ateş bir laneti susturur.");

        public static string FireDoneBreak => L("The fire marks the demon's first cheat. It will fail.", "Ateş şeytanın ilk hilesini damgaladı. Tutmayacak.");

        /// <summary>{0} relic name.</summary>
        public static string FireDoneSilenceFormat => L("{0} is silent until the chapter ends.", "{0} bölüm bitene kadar sessiz.");
        public static string FireDoneRest => L("You rest by the fire. The demon's bar will start shorter.", "Ateşin başında dinlendin. Şeytanın barı kısa başlayacak.");

        /// <summary>The gate under a chapter's last floor.</summary>
        public static string GateTitle(int chapter)
        {
            switch (chapter)
            {
                case 2: return L("THE STAGE DOOR", "SAHNE KAPISI");
                case 3: return L("THE NIGHT GATE", "GECE KAPISI");
                default: return L("THE VAULT GATE", "KASA KAPISI");
            }
        }

        /// <summary>{0} the demon's name, {1} the tribute, {2} coins on hand.</summary>
        public static string GateTextFormat => L("{0} takes the tribute: {1} coins. You have {2}.", "{0} haracını alır: {1} coin. Sende {2} var.");

        /// <summary>{0} coins missing, {1} the years.</summary>
        public static string GateShortFormat => L("You lack {0} coins: +{1} years on the bar.", "{0} coin eksiğin var: barına +{1} yıl.");

        /// <summary>{0} coins left over.</summary>
        public static string GatePaidFormat => L("Paid in full. {0} coins go on with you.", "Tamamı ödendi. {0} coin seninle devam eder.");
        public static string GateOwedFormat => L("The floors' bargains come due: +{0} years.", "Katlardaki pazarlıkların bedeli: +{0} yıl.");

        /// <summary>{0} the demon's bar as his table begins (years).</summary>
        public static string GateAfterFormat => L("The bar: {0} years of your sentence. Empty it — or lose your soul trying.",
            "Barı: cezandan {0} yıl. Boşalt — ya da bu uğurda ruhunu kaybet.");
        public static string GatePay => L("PAY AND SIT", "ÖDE VE OTUR");

        // ------------------------------------------------------------------ a beaten demon's spoils

        public static string LootTitle => L("SPOILS", "GANİMET");

        /// <summary>{0} the demon, {1} hands, {2} the coins instead.</summary>
        public static string LootTextFormat => L("{0} is beaten: his bar emptied in {1} hands. He leaves his spoils: take one relic — or {2} coins.",
            "{0} yenildi: barı {1} elde boşaldı. Ganimetini bırakıyor: bir emanet seç — ya da {2} coin.");
        public static string LootSwapNote => L("You carry all you may: one of yours goes in its place.", "Taşıyabileceğin kadar taşıyorsun: yerine birini bırakırsın.");
        public static string LootSwapTitle => L("WHICH RELIC GOES?", "HANGİ EMANET GİDER?");

        /// <summary>{0} the relic taken.</summary>
        public static string LootSwapTextFormat => L("{0} comes. Which one of yours goes?", "{0} gelir. Seninkilerden hangisi gider?");

        // ------------------------------------------------------------------ Lucifer and the end of a run

        public static string LuciferCallsTitle => L("THE MORNING STAR", "SABAH YILDIZI");

        /// <summary>{0} how far past its start the bar may climb (percent) before the player falls.</summary>
        public static string LuciferCallsTextFormat => L(
            "Lilith falls. Below her there is no map, no purse, no gate: only his table.\nThe rest of your sentence is his bar. Empty it and you are free. " +
            "Let it climb more than {0}% past where it began, and you fall — for good.",
            "Lilith düştü. Onun altında harita yok, kese yok, kapı yok: sadece onun masası.\nCezanın geri kalanı onun barı. Boşaltırsan özgürsün. " +
            "Başladığı yerin %{0} kadar üstüne çıkarsa düşersin — sonsuza dek.");
        public static string LuciferSit => L("SIT DOWN", "OTUR");
        public static string FallTitle => L("YOU FELL", "DÜŞTÜN");
        public static string FallText => L("His bar climbed past the line. The Morning Star does not call twice: you are cast down, and damned.",
            "Barı çizgiyi geçti. Sabah Yıldızı iki kez çağırmaz: aşağı atıldın, lanetlendin.");
        public static string FreedTitle => L("SALVATION", "KURTULUŞ");
        public static string FreedText => L("The Morning Star's bar is empty. Every year is paid. You walk out of Hell.",
            "Sabah Yıldızı'nın barı boşaldı. Her yıl ödendi. Cehennemden yürüyerek çıkıyorsun.");

        /// <summary>{0} the class, {1} hands in all, {2} coins left, {3}-{6} each demon's hands (Mammon, Belial, Lilith, Lucifer).</summary>
        public static string RunSummaryFormat => L("{0} · {1} hands · {2} coins left\nMammon {3} · Belial {4} · Lilith {5} · Lucifer {6} hands",
            "{0} · {1} el · kalan coin {2}\nMammon {3} · Belial {4} · Lilith {5} · Lucifer {6} el");
        public static string ChapterDamnedTitle => L("DAMNED", "LANETLENDİN");

        /// <summary>{0} the demon.</summary>
        public static string ChapterDamnedTextFormat => L("Your soul burned away at {0}'s table. The run is over.", "Ruhun {0} masasında yanıp kül oldu. Koşu bitti.");
        public static string ChapterNewRun => L("NEW RUN", "YENİ KOŞU");

        public static string LostHandTitle => L("THE HAND YOU LEFT", "BIRAKTIĞIN EL");

        /// <summary>{0} coins lost.</summary>
        public static string LostHandCoinsFormat => L("You closed the game in the middle of a hand. That hand is lost: −{0} coins.",
            "Oyunu bir elin ortasında kapattın. O el kaybedildi: −{0} coin.");
        public static string LostHandBar => L("You closed the game in the middle of a hand. That hand is lost: the bar grew.",
            "Oyunu bir elin ortasında kapattın. O el kaybedildi: bar uzadı.");

        public static string DesireWhich => L("WHICH RELIC?", "HANGİ EMANET?");
        public static string DesireWhichText => L("One relic counts twice until the chapter ends — its gift and its curse.",
            "Bir emanet bölüm bitene kadar iki kat sayılır — lütfu da, laneti de.");

        // ------------------------------------------------------------------ the floors' offers (events)

        public static string FloorEventOwner(string eventId)
        {
            switch (eventId)
            {
                case FloorEventIds.Usurer: return L("A USURER", "BİR TEFECİ");
                case FloorEventIds.MammonsLedger: return L("MAMMON", "MAMMON");
                case FloorEventIds.LyingWitness: return L("A WITNESS", "BİR TANIK");
                case FloorEventIds.Spectacle: return L("A STAGEHAND", "BİR SAHNE AMELESİ");
                case FloorEventIds.FalseCoin: return L("A COINER", "BİR KALPAZAN");
                case FloorEventIds.NightBargain: return L("A NIGHT TRADER", "BİR GECE TÜCCARI");
                case FloorEventIds.Desire: return L("A WHISPER", "BİR FISILTI");
                case FloorEventIds.Insomnia: return L("THE SLEEPLESS", "UYKUSUZ");
                default: return L("A GHOST", "BİR HAYALET");
            }
        }

        public static string FloorEventTitle(string eventId)
        {
            switch (eventId)
            {
                case FloorEventIds.Usurer: return L("THE USURER OF PURGATORY", "ARAF TEFECİSİ");
                case FloorEventIds.MammonsLedger: return L("MAMMON'S LEDGER", "MAMMON'UN DEFTERİ");
                case FloorEventIds.LyingWitness: return L("THE LYING WITNESS", "YALANCI TANIK");
                case FloorEventIds.Spectacle: return L("THE SPECTACLE", "GÖSTERİ");
                case FloorEventIds.FalseCoin: return L("THE FALSE COIN", "SAHTE SİKKE");
                case FloorEventIds.NightBargain: return L("THE NIGHT BARGAIN", "GECE PAZARLIĞI");
                case FloorEventIds.Desire: return L("DESIRE", "ARZU");
                case FloorEventIds.Insomnia: return L("INSOMNIA", "UYKUSUZLUK");
                default: return L("THE GAMBLER'S GHOST", "KUMARBAZ HAYALET");
            }
        }

        /// <summary>
        /// Usurer: {0} coins, {1} years, {2} the demon. Ledger: {0} years now, {1} years back. Ghost: {0} the stake, {1} the multiplier cap.
        /// Witness: {0} price, {1} lie percent. Spectacle: {0} the win's multiplier. False coin / night bargain: {0} coins, {1} years.
        /// Insomnia: {0} hands. Desire: none.
        /// </summary>
        public static string FloorEventTextFormat(string eventId)
        {
            switch (eventId)
            {
                case FloorEventIds.Usurer: return L("\"{0} coins, friend, right now. {2} will add {1} years to his bar when you sit at his table. A fair rate. For Hell.\"",
                    "\"{0} coin, dostum, hemen şimdi. Masasına oturduğunda {2} barına {1} yıl ekleyecek. Makul bir faiz. Cehennem için.\"");
                case FloorEventIds.MammonsLedger: return L("\"I strike {0} years off your sentence now. At my table I write back {1}. Interest, you understand.\"",
                    "\"Cezandan şimdi {0} yıl siliyorum. Masamda {1} yıl geri yazarım. Faiz, anlarsın.\"");
                case FloorEventIds.LyingWitness: return L("\"{0} coins and I tell you one of the next imp's cards. I saw it. Mostly. One time in four I lie — {1}%, you understand.\"",
                    "\"{0} coin ver, sıradaki iblisin kartlarından birini söyleyeyim. Gördüm. Çoğunlukla. Dört seferde bir yalan söylerim — %{1}, anlarsın.\"");
                case FloorEventIds.Spectacle: return L("\"At your next table the imp shows nothing — not one card. But every hand you win pays ×{0}. Never past the floor's ×3. The show must go on.\"",
                    "\"Sıradaki masanda iblis hiçbir şey göstermez — tek kart bile. Ama kazandığın her el ×{0} öder. Katın ×3'ünü geçmez. Gösteri devam etmeli.\"");
                case FloorEventIds.FalseCoin: return L("\"{0} coins, fresh from the mould. Belial will notice, of course: his bar starts {1} years longer.\"",
                    "\"{0} coin, kalıptan yeni çıktı. Belial fark eder elbette: barı {1} yıl uzun başlar.\"");
                case FloorEventIds.NightBargain: return L("\"{0} coins in the dark, no questions. Lilith keeps the receipt: her bar starts {1} years longer.\"",
                    "\"Karanlıkta {0} coin, soru yok. Makbuzu Lilith tutar: barı {1} yıl uzun başlar.\"");
                case FloorEventIds.Desire: return L("\"Want it more. One of your relics will count twice until this chapter ends — its gift, and its curse.\"",
                    "\"Daha çok iste. Emanetlerinden biri bu bölüm bitene kadar iki kat sayılır — lütfu da, laneti de.\"");
                case FloorEventIds.Insomnia: return L("\"Who sleeps here? At your next table the first {0} hands cost no ante. But you will be too tired to raise.\"",
                    "\"Burada kim uyur? Sıradaki masanda ilk {0} el ante'siz. Ama artıramayacak kadar yorgun olursun.\"");
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

        /// <summary>{0} the imp's coins, {1} free hands left (Insomnia: no ante, no raise).</summary>
        public static string FloorFreeSeatTitleFormat => L("Purse {0} · free hands: {1}, no raise", "Kesesi {0} · ante'siz el: {1}, artırma yok");

        /// <summary>{0} the stake (the gambler's ghost: no purse to show).</summary>
        public static string GambleSeatTitleFormat => L("One hand · {0} coins", "Tek el · {0} coin");

        /// <summary>{0} this hand.</summary>
        public static string BossSeatTitleFormat => L("Hand {0} · empty the bar", "El {0} · barı boşalt");

        /// <summary>{0} this hand (the soul is on the table).</summary>
        public static string BossSoulSeatTitleFormat => L("Hand {0} · your soul is on the table", "El {0} · ruhun masada");

        /// <summary>{0} the demon's name with its genitive ("MAMMON'S" / "MAMMON'UN"): the counter's label over the health bar.</summary>
        public static string BarLabelFormat => L("{0} BAR", "{0} BARI");

        /// <summary>The end of the bar the counter fills toward (no number).</summary>
        public static string BarSoulLine => L("Your soul at the end", "Sonunda ruhun");

        /// <summary>Lucifer's bar: the line past which the player is cast down.</summary>
        public static string BarCastDownLine => L("Past it, you fall", "Geçersen düşersin");

        // A demon's table counts in shares of the bar ({0}: percent of the bar the table began with), never in years.
        public static string BarAnteFormat => L("{0}% OF THE BAR", "BARIN %{0} KADARI");
        public static string BarPotFormat => L("ON THE TABLE: {0}% OF THE BAR", "MASADA: BARIN %{0} KADARI");
        public static string BarPromptBetFormat => L("The bet: {0}% of the bar. Deal when you dare.", "Bahis: barın %{0} kadarı. Cesaretin varsa dağıt.");
        public static string BarPromptReRaiseFormat => L("A raise of {0}% of the bar! Call — or fold?", "Barın %{0} kadarı artırdı! Gör — ya da çekil?");
        public static string BarRaiseFormat => L("RAISE +{0}%", "ARTIR +%{0}");
        public static string BarAllInFormat => L("ALL IN +{0}%", "HEPSİ +%{0}");
        public static string BarCallFormat => L("CALL +{0}%", "GÖR +%{0}");
        public static string BarStakeInfoFormat => L("Win: the bar −{0}% at least   ·   Lose: +{1}% at least",
            "Kazanç: bar en az −%{0}   ·   Kayıp: en az +%{1}");

        /// <summary>{0} player hand, {1} house hand, {2} percent of the bar.</summary>
        public static string BarWinFormat => L("{0} beats {1}.  You cut the bar by {2}%.", "{0}, {1} elini yendi.  Barı %{2} kısalttın.");

        /// <summary>{0} house hand, {1} player hand, {2} percent of the bar.</summary>
        public static string BarLossFormat => L("{0} beats your {1}.  The bar grows {2}%.", "{0}, senin {1} elini yendi.  Barı %{2} uzattın.");
        public static string BarFoldFormat => L("You fold.  The bar grows {0}%.", "Çekildin.  Barı %{0} uzattın.");
        public static string BarPushFormat => L("Push — {0} against {1}. The bar stands.", "Berabere — {0}, {1} karşısında. Bar aynı.");

        /// <summary>{0} the demon's name with its genitive, in a sentence ("Mammon's" / "Mammon'un").</summary>
        public static string BossBeatenFormat => L("{0} bar is empty. Beaten.", "{0} barı boşaldı. Yenildi.");

        /// <summary>{0} the demon's name with its genitive, in a sentence.</summary>
        public static string BossDeadMansHandFormat => L("The Dead Man's Hand empties {0} bar at once. Beaten.",
            "Ölü Adamın Eli {0} barını bir anda boşalttı. Yenildi.");
        public static string ToTheMap => L("TO THE MAP", "HARİTAYA");
        public static string LeaveTheVault => L("ONWARD", "İLERİ");
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

        private static readonly DealerText BelialImp = new DealerText
        {
            Name = "MASK IMP",
            Title = "One of Belial's players",
            Description = "Wears a smile painted on a mask, and another under it. Cannot keep a straight face — or a straight hand.",
            Greeting = new[] { "Ladies, gentlemen, the damned! Take your seat, the show begins.", "An audience! Sit, sit, the curtain rises." },
            PlayerWins = new[] { "That was not in the script!", "Booo. Boo, I say.", "An unrehearsed ending." },
            HouseWins = new[] { "Applause, please!", "A bow, a bow.", "Encore! Your coins, I mean." },
            PlayerFolds = new[] { "Leaving before the second act?", "The audience walks out. Rude." },
            Push = new[] { "A draw? The critics will hate it." },
            FinalStretch = new[] { "The finale!" },
            ReRaise = new[] { "Twist! I raise!", "The plot thickens." },
            Sealed = new[] { "No more lines. Play it out.", "The curtain is down on the bets." },
            Backfire = new[] { "I missed my cue." },
            Blocked = new[] { "Heckler!" },
            JokerBust = new[] { "Two fools on my stage? I was miscast!" },
            JokerJackpot = new[] { "Twenty jokers! Now that is a show." },
            GreetingAsPeasant = "A peasant in the front row! Free seats for the poor. Sit.",
            GreetingAsWarlock = "A warlock. No tricks from the audience, please. Sit.",
            GreetingAsKing = "A king in the royal box! Sit, Majesty, the play is for you.",
            GreetingAsJester = "A jester! Finally, a colleague. Sit."
        };

        private static readonly DealerText BelialImpTr = new DealerText
        {
            Name = "MASKE İBLİSİ",
            Genitive = "MASKE İBLİSİNİN",
            Called = "Maske İblisi",
            CalledGenitive = "Maske İblisinin",
            Title = "Belial'in oyuncularından",
            Description = "Maskesine boyanmış bir gülümseme takar, altında bir tane daha. Yüzünü düz tutamaz — elini de.",
            Greeting = new[] { "Hanımlar, beyler, lanetliler! Yerinizi alın, gösteri başlıyor.", "Seyirci! Otur otur, perde açılıyor." },
            PlayerWins = new[] { "Bu senaryoda yoktu!", "Yuuu. Yuuu diyorum.", "Prova edilmemiş bir son." },
            HouseWins = new[] { "Alkış lütfen!", "Bir reverans, bir reverans.", "Bis! Coinlerini kastediyorum." },
            PlayerFolds = new[] { "İkinci perdeden önce mi gidiyorsun?", "Seyirci salonu terk ediyor. Kaba." },
            Push = new[] { "Berabere mi? Eleştirmenler bayılmayacak." },
            FinalStretch = new[] { "Final!" },
            ReRaise = new[] { "Sürpriz! Artırıyorum!", "Olay kızışıyor." },
            Sealed = new[] { "Repliğin bitti. Oyna.", "Bahislerin perdesi indi." },
            Backfire = new[] { "Repliğimi kaçırdım." },
            Blocked = new[] { "Laf atan seyirci!" },
            JokerBust = new[] { "Sahnemde iki soytarı mı? Yanlış rol verilmiş!" },
            JokerJackpot = new[] { "Yirmi joker! İşte buna gösteri denir." },
            GreetingAsPeasant = "Ön sırada bir köylü! Yoksullara bedava koltuk. Otur.",
            GreetingAsWarlock = "Bir büyücü. Seyirciden numara istemeyiz. Otur.",
            GreetingAsKing = "Kraliyet locasında bir kral! Buyrun Majesteleri, oyun sizin için.",
            GreetingAsJester = "Bir soytarı! Sonunda bir meslektaş. Otur."
        };

        private static readonly DealerText Prophet = new DealerText
        {
            Name = "THE FALSE PROPHET",
            Title = "Belial's warden, who never tells the truth",
            Description = "Guards the stage door with a prophecy for every hand — and every prophecy a lie. One of his open cards always is.",
            Greeting = new[] { "I foresaw you. I foresee your loss. One of us is lying. Sit." },
            PlayerWins = new[] { "As I foretold. Backwards.", "The prophecy was... reinterpreted.", "A miracle. A false one, surely." },
            HouseWins = new[] { "As it was written.", "Believe, and lose.", "The vision was true. This once." },
            PlayerFolds = new[] { "You doubted. Wise, for once.", "The faithless walk away." },
            Push = new[] { "Neither saved nor damned. How dull." },
            FinalStretch = new[] { "Repent." },
            ReRaise = new[] { "Heaven raises.", "A sign! I raise." },
            Sealed = new[] { "It is written.", "The scripture is sealed." },
            Backfire = new[] { "The vision clouded." },
            Blocked = new[] { "You see through me? Heresy!", "A warding. Blasphemy." },
            JokerBust = new[] { "Two fools. I did not foresee that." },
            JokerJackpot = new[] { "Twenty jokers. The end of days." },
            GreetingAsPeasant = "A peasant. Your faith is simple. Your purse, simpler. Sit.",
            GreetingAsWarlock = "A warlock. You will see my lies. Seeing is not winning. Sit.",
            GreetingAsKing = "A king. Kings have always loved a prophet. Sit.",
            GreetingAsJester = "A jester. Finally, someone who lies as well as I do. Sit."
        };

        private static readonly DealerText ProphetTr = new DealerText
        {
            Name = "SAHTE PEYGAMBER",
            Genitive = "SAHTE PEYGAMBER'İN",
            Called = "Sahte Peygamber",
            CalledGenitive = "Sahte Peygamber'in",
            Title = "Belial'in bekçisi, hiç doğru söylemez",
            Description = "Sahne kapısını her ele bir kehanetle bekler — her kehanet bir yalan. Açık kartlarından biri hep yalandır.",
            Greeting = new[] { "Seni gördüm. Kaybını görüyorum. İkimizden biri yalan söylüyor. Otur." },
            PlayerWins = new[] { "Dediğim gibi. Tersinden.", "Kehanet... yeniden yorumlandı.", "Bir mucize. Sahte bir tane, eminim." },
            HouseWins = new[] { "Yazıldığı gibi.", "İnan ve kaybet.", "Görüm doğruydu. Bu seferlik." },
            PlayerFolds = new[] { "Şüphe ettin. Bir kez olsun akıllıca.", "İmansızlar çekip gider." },
            Push = new[] { "Ne kurtuldun ne lanetlendin. Ne sıkıcı." },
            FinalStretch = new[] { "Tövbe et." },
            ReRaise = new[] { "Gök artırıyor.", "Bir işaret! Artırıyorum." },
            Sealed = new[] { "Yazıldı.", "Kitap mühürlendi." },
            Backfire = new[] { "Görüm bulanıklaştı." },
            Blocked = new[] { "Beni görüyor musun? Sapkınlık!", "Bir koruma. Küfür." },
            JokerBust = new[] { "İki soytarı. Bunu görmemiştim." },
            JokerJackpot = new[] { "Yirmi joker. Kıyamet günü." },
            GreetingAsPeasant = "Bir köylü. İmanın sade. Kesen daha da sade. Otur.",
            GreetingAsWarlock = "Bir büyücü. Yalanlarımı göreceksin. Görmek kazanmak değildir. Otur.",
            GreetingAsKing = "Bir kral. Krallar peygamberleri hep sevmiştir. Otur.",
            GreetingAsJester = "Bir soytarı. Sonunda benim kadar iyi yalan söyleyen biri. Otur."
        };

        private static readonly DealerText LilithImp = new DealerText
        {
            Name = "MOTH IMP",
            Title = "One of Lilith's night-flyers",
            Description = "Drawn to candlelight and to purses. Whispers sweetly while its hand is in your pocket.",
            Greeting = new[] { "Mm, a warm one. Sit close. Closer.", "The night is long. So is my purse — almost. Sit." },
            PlayerWins = new[] { "You burned me.", "Cruel. I like cruel.", "Lilith will hear you were... sweet." },
            HouseWins = new[] { "Sweet.", "Mine, softly.", "Shh. It does not hurt." },
            PlayerFolds = new[] { "Leaving so soon?", "Afraid of the dark?" },
            Push = new[] { "Neither of us wins. How lonely." },
            FinalStretch = new[] { "Closer." },
            ReRaise = new[] { "I want more of you.", "Mm. Higher." },
            Sealed = new[] { "No turning back now.", "Bound till dawn." },
            Backfire = new[] { "The candle went out." },
            Blocked = new[] { "Spoilsport." },
            JokerBust = new[] { "Two fools in the dark. Embarrassing." },
            JokerJackpot = new[] { "Twenty jokers. What a night." },
            GreetingAsPeasant = "A peasant. Calloused hands. Warm, though. Sit.",
            GreetingAsWarlock = "A warlock. Your candles burn brighter than mine. Sit.",
            GreetingAsKing = "A king, alone at night. Sit, Majesty. I will keep you company.",
            GreetingAsJester = "A jester. Make me laugh, then lose. Sit."
        };

        private static readonly DealerText LilithImpTr = new DealerText
        {
            Name = "PERVANE İBLİSİ",
            Genitive = "PERVANE İBLİSİNİN",
            Called = "Pervane İblisi",
            CalledGenitive = "Pervane İblisinin",
            Title = "Lilith'in gece uçanlarından",
            Description = "Mum ışığına ve keselere çekilir. Eli cebindeyken tatlı tatlı fısıldar.",
            Greeting = new[] { "Mm, sıcak biri. Yakın otur. Daha yakın.", "Gece uzun. Kesem de — neredeyse. Otur." },
            PlayerWins = new[] { "Beni yaktın.", "Zalim. Zalimi severim.", "Lilith senin... tatlı olduğunu duyacak." },
            HouseWins = new[] { "Tatlı.", "Benim, usulca.", "Şşş. Acıtmaz." },
            PlayerFolds = new[] { "Bu kadar erken mi?", "Karanlıktan mı korkuyorsun?" },
            Push = new[] { "İkimiz de kazanmadık. Ne yalnız." },
            FinalStretch = new[] { "Daha yakın." },
            ReRaise = new[] { "Senden daha fazlasını istiyorum.", "Mm. Daha yükseğe." },
            Sealed = new[] { "Artık dönüş yok.", "Şafağa kadar bağlıyız." },
            Backfire = new[] { "Mum söndü." },
            Blocked = new[] { "Oyunbozan." },
            JokerBust = new[] { "Karanlıkta iki soytarı. Utanç verici." },
            JokerJackpot = new[] { "Yirmi joker. Ne gece ama." },
            GreetingAsPeasant = "Bir köylü. Nasırlı eller. Yine de sıcak. Otur.",
            GreetingAsWarlock = "Bir büyücü. Mumların benimkilerden parlak yanıyor. Otur.",
            GreetingAsKing = "Gece yalnız bir kral. Otur Majesteleri. Sana eşlik ederim.",
            GreetingAsJester = "Bir soytarı. Önce güldür, sonra kaybet. Otur."
        };

        private static readonly DealerText Nurse = new DealerText
        {
            Name = "THE NIGHT NURSE",
            Title = "Lilith's warden, who tends the sleepless",
            Description = "Guards the night gate with a lamp turned low. Hands you your new cards in the dark — you will see one only at the end.",
            Greeting = new[] { "Hush. Lie still. I will hand you your cards. Sit." },
            PlayerWins = new[] { "A good patient.", "Recovering, are we?", "You will live. For now." },
            HouseWins = new[] { "Rest now.", "This will not hurt. Much.", "Lights out." },
            PlayerFolds = new[] { "Back to bed.", "Wise. You need your rest." },
            Push = new[] { "No change in the patient." },
            FinalStretch = new[] { "Breathe." },
            ReRaise = new[] { "A stronger dose.", "Doctor's orders: more." },
            Sealed = new[] { "The treatment is set.", "No refusing the medicine now." },
            Backfire = new[] { "Wrong bottle." },
            Blocked = new[] { "Refusing your treatment?", "A warded patient. Difficult." },
            JokerBust = new[] { "Two fools in one bed. Unhygienic." },
            JokerJackpot = new[] { "Twenty jokers. Delirium." },
            GreetingAsPeasant = "A peasant. You have slept on worse. Sit.",
            GreetingAsWarlock = "A warlock. Your eyes are open; your cards will not be. Sit.",
            GreetingAsKing = "A king who cannot sleep. Sit, Majesty. The night is mine.",
            GreetingAsJester = "A jester. Laughter keeps them awake. Sit."
        };

        private static readonly DealerText NurseTr = new DealerText
        {
            Name = "GECE HEMŞİRESİ",
            Genitive = "GECE HEMŞİRESİ'NİN",
            Called = "Gece Hemşiresi",
            CalledGenitive = "Gece Hemşiresi'nin",
            Title = "Lilith'in bekçisi, uykusuzlara bakar",
            Description = "Gece kapısını kısık bir lambayla bekler. Yeni kartlarını karanlıkta verir — birini ancak sonunda görürsün.",
            Greeting = new[] { "Şşş. Kıpırdama. Kartlarını ben vereceğim. Otur." },
            PlayerWins = new[] { "İyi bir hasta.", "İyileşiyor muyuz?", "Yaşayacaksın. Şimdilik." },
            HouseWins = new[] { "Şimdi dinlen.", "Acıtmayacak. Çok.", "Işıklar sönsün." },
            PlayerFolds = new[] { "Yatağına dön.", "Akıllıca. Dinlenmen lazım." },
            Push = new[] { "Hastada değişiklik yok." },
            FinalStretch = new[] { "Nefes al." },
            ReRaise = new[] { "Daha güçlü bir doz.", "Doktor emri: daha fazla." },
            Sealed = new[] { "Tedavi belli.", "Artık ilacı reddedemezsin." },
            Backfire = new[] { "Yanlış şişe." },
            Blocked = new[] { "Tedavini mi reddediyorsun?", "Korunan bir hasta. Zor." },
            JokerBust = new[] { "Bir yatakta iki soytarı. Hijyenik değil." },
            JokerJackpot = new[] { "Yirmi joker. Sayıklama." },
            GreetingAsPeasant = "Bir köylü. Daha kötüsünde de uyudun. Otur.",
            GreetingAsWarlock = "Bir büyücü. Gözlerin açık; kartların olmayacak. Otur.",
            GreetingAsKing = "Uyuyamayan bir kral. Otur Majesteleri. Gece benim.",
            GreetingAsJester = "Bir soytarı. Kahkaha onları uyanık tutar. Otur."
        };

        /// <summary>The floors' faces; null for anyone else (the demons are in <see cref="Dealer"/>).</summary>
        private static DealerText ChapterFace(string id, bool turkish)
        {
            switch (id)
            {
                case ChapterCast.ImpId: return turkish ? ImpTr : Imp;
                case ChapterCast.CollectorId: return turkish ? CollectorTr : Collector;
                case ChapterCast.BelialImpId: return turkish ? BelialImpTr : BelialImp;
                case ChapterCast.ProphetId: return turkish ? ProphetTr : Prophet;
                case ChapterCast.LilithImpId: return turkish ? LilithImpTr : LilithImp;
                case ChapterCast.NurseId: return turkish ? NurseTr : Nurse;
                default: return null;
            }
        }
    }
}
