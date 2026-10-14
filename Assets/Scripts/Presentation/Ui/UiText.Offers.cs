using HellPoker.Core.Chapters;

namespace HellPoker.Presentation.Ui
{
    /// <summary>Demo 2.1's offers in words: the stranger, the offer (a swindler's with its small hint), the choices, how it went.</summary>
    internal static partial class UiText
    {
        public static string OfferTitle(string id)
        {
            switch (id)
            {
                case ChoiceEventIds.ChainedSoul: return L("THE CHAINED SOUL", "ZİNCİRLİ RUH");
                case ChoiceEventIds.FallenAngel: return L("THE FALLEN ANGEL", "DÜŞMÜŞ MELEK");
                case ChoiceEventIds.HungryGambler: return L("THE HUNGRY GAMBLER", "AÇ KUMARBAZ");
                case ChoiceEventIds.DebtorImp: return L("THE DEBTOR IMP", "BORÇLU İBLİS");
                case ChoiceEventIds.LostSoul: return L("THE LOST SOUL", "KAYIP RUH");
                case ChoiceEventIds.Accountant: return L("MAMMON'S ACCOUNTANT", "MAMMON'UN MUHASEBECİSİ");
                case ChoiceEventIds.ThreeCups: return L("THREE CUPS", "ÜÇ BARDAK");
                case ChoiceEventIds.FakePardon: return L("A PARDON FOR SALE", "SATILIK AF BELGESİ");
                case ChoiceEventIds.SoulInsurance: return L("SOUL INSURANCE", "RUH SİGORTASI");
                case ChoiceEventIds.Pickpocket: return L("THE PICKPOCKET", "CEP HIRSIZI");
                case ChoiceEventIds.LavaFerryman: return L("THE LAVA FERRYMAN", "LAV NEHRİ KAYIKÇISI");
                case ChoiceEventIds.HellLottery: return L("HELL'S LOTTERY", "CEHENNEM PİYANGOSU");
                case ChoiceEventIds.Confession: return L("THE CONFESSION CELL", "İTİRAF HÜCRESİ");
                case ChoiceEventIds.FireRain: return L("RAIN OF FIRE", "ATEŞ YAĞMURU");
                case ChoiceEventIds.SinSwap: return L("THE SIN SWAP", "GÜNAH TAKASI");
                default: return L("THE BLIND SEER", "KÖR KÂHİN");
            }
        }

        public static string OfferOwner(string id)
        {
            switch (id)
            {
                case ChoiceEventIds.ChainedSoul: return L("A soul in chains", "Zincirli bir ruh");
                case ChoiceEventIds.FallenAngel: return L("An angel, fallen", "Düşmüş bir melek");
                case ChoiceEventIds.HungryGambler: return L("A thin gambler", "Zayıf bir kumarbaz");
                case ChoiceEventIds.DebtorImp: return L("An imp in debt", "Borçlu bir iblis");
                case ChoiceEventIds.LostSoul: return L("A lost soul", "Kayıp bir ruh");
                case ChoiceEventIds.Accountant: return L("Mammon's accountant", "Mammon'un muhasebecisi");
                case ChoiceEventIds.ThreeCups: return L("A cup juggler", "Bardakçı");
                case ChoiceEventIds.FakePardon: return L("A pardoner", "Af satıcısı");
                case ChoiceEventIds.SoulInsurance: return L("An insurer", "Sigortacı");
                case ChoiceEventIds.Pickpocket: return L("A pickpocket", "Cep hırsızı");
                case ChoiceEventIds.LavaFerryman: return L("The ferryman", "Kayıkçı");
                case ChoiceEventIds.HellLottery: return L("The ticket seller", "Biletçi");
                case ChoiceEventIds.Confession: return L("A confessor", "Günah çıkaran");
                case ChoiceEventIds.FireRain: return L("The sky", "Gökyüzü");
                case ChoiceEventIds.SinSwap: return L("A sin trader", "Günah tüccarı");
                default: return L("A blind seer", "Kör bir kâhin");
            }
        }

        /// <summary>The offer's words; the numbers (<see cref="OfferArgs"/>) are the chapter's.</summary>
        public static string OfferText(string id)
        {
            switch (id)
            {
                case ChoiceEventIds.ChainedSoul: return L("\"Break my chain, sinner. {0} coins buys the file. I will remember you.\"",
                    "\"Zincirimi kır, günahkâr. {0} coin'e bir eğe alınır. Seni unutmam.\"");
                case ChoiceEventIds.FallenAngel: return L("\"My wing is broken. Carry {0} years of mine, and I will give you something of Heaven.\"",
                    "\"Kanadım kırık. Benim {0} yılımı taşı, sana Cennet'ten bir şey vereyim.\"");
                case ChoiceEventIds.HungryGambler: return L("\"{0} coins for a crust, and I'll tell you how the next imp holds his cards. His first hand, at least.\"",
                    "\"Bir lokma için {0} coin, ben de sana sıradaki iblisin kartlarını nasıl tuttuğunu söyleyeyim. İlk elini, en azından.\"");
                case ChoiceEventIds.DebtorImp: return L("\"Lend me {0}. At the gate you get {1} back, an imp's word of honour.\"",
                    "\"Bana {0} ver. Kapıda {1} geri alırsın, iblis sözü.\"");
                case ChoiceEventIds.LostSoul: return L("\"Which way is down? Walk me, and I will tell you what hides two floors ahead. I am slow, though.\"",
                    "\"Aşağısı ne tarafta? Bana yolu göster, iki kat ötede ne saklandığını söyleyeyim. Ama yavaşım.\"");
                case ChoiceEventIds.Accountant: return L("\"Mammon's books are wrong. Fix them with me: your next {0} tables are cheaper. Or report me — {1} coins, and the next warden will be furious.\"",
                    "\"Mammon'un defteri yanlış. Benimle düzelt: sonraki {0} masan ucuzlar. Ya da beni ihbar et — {1} coin, ama sonraki bekçi kudurur.\"");
                case ChoiceEventIds.ThreeCups: return L("\"Find the pea, double your coins. {0}, {1} or {2}. One cup in three — easy.\"",
                    "\"Nohudu bul, coin'ini ikiye katla. {0}, {1} ya da {2}. Üç bardaktan biri — kolay.\"");
                case ChoiceEventIds.FakePardon: return L("\"A pardon of the old church: {1} years off. Only {0} coins.\"",
                    "\"Eski kilisenin af belgesi: {1} yıl silinir. Yalnızca {0} coin.\"");
                case ChoiceEventIds.SoulInsurance: return L("\"{0} coins, and if the demon wins a hand, we pay. Sign here.\"",
                    "\"{0} coin, şeytan bir el alırsa biz öderiz. Şuraya imza.\"");
                case ChoiceEventIds.Pickpocket: return L("A hand in your purse — and {0} coins run off into the smoke.",
                    "Kesende bir el — ve {0} coin dumanın içine koşuyor.");
                case ChoiceEventIds.LavaFerryman: return L("\"{0} coins and I row you over the next floor. Or swim it: it only burns {1} years.\"",
                    "\"{0} coin ver, seni sonraki katın üstünden geçireyim. Ya da yüz: sadece {1} yıl yakar.\"");
                case ChoiceEventIds.HellLottery: return L("\"A ticket, {0} coins! {1} coins to win, and once in a blue moon a relic of legend.\"",
                    "\"Bilet, {0} coin! {1} coin kazanılır, ara sıra efsanevi bir emanet.\"");
                case ChoiceEventIds.Confession: return L("\"Confess, and {0} years are struck. But the demon below will hear of it.\"",
                    "\"İtiraf et, {0} yılın silinsin. Ama aşağıdaki şeytan duyar.\"");
                case ChoiceEventIds.FireRain: return L("Fire falls from the vault. Throw {0} coins up as a shield — or burn: {1} years.",
                    "Tavandan ateş yağıyor. Kalkan diye {0} coin fırlat — ya da yan: {1} yıl.");
                case ChoiceEventIds.SinSwap: return L("\"Give me one of your sins. I'll show you two others, take one.\"",
                    "\"Günahlarından birini ver. Sana iki tane daha göstereyim, birini al.\"");
                default: return L("\"{0} coins, and I tell you of the demon below: his rule, and how long his bar is.\"",
                    "\"{0} coin ver, aşağıdaki şeytanı anlatayım: kuralını ve barının boyunu.\"");
            }
        }

        /// <summary>The numbers of an offer's words, the chapter's (coins and years grow ×1 / ×1.25 / ×1.5).</summary>
        public static object[] OfferArgs(IChoiceEvent offer, ChapterRun run)
        {
            switch (offer.Id)
            {
                case ChoiceEventIds.ChainedSoul: return new object[] { run.Scaled(ChainedSoul.Price), run.Scaled(ChainedSoul.Years) };
                case ChoiceEventIds.FallenAngel: return new object[] { run.Scaled(FallenAngel.Years) };
                case ChoiceEventIds.HungryGambler: return new object[] { run.Scaled(HungryGambler.Price) };
                case ChoiceEventIds.DebtorImp: return new object[] { run.Scaled(DebtorImp.Loan), run.Scaled(DebtorImp.Back) };
                case ChoiceEventIds.Accountant: return new object[] { MammonsAccountant.Tables, run.Scaled(MammonsAccountant.Reward) };
                case ChoiceEventIds.ThreeCups: return new object[] { run.Scaled(ThreeCups.Stakes[0]), run.Scaled(ThreeCups.Stakes[1]), run.Scaled(ThreeCups.Stakes[2]) };
                case ChoiceEventIds.FakePardon: return new object[] { run.Scaled(FakePardon.Price), run.Scaled(FakePardon.Promised) };
                case ChoiceEventIds.SoulInsurance: return new object[] { run.Scaled(SoulInsurance.Price) };
                case ChoiceEventIds.Pickpocket: return new object[] { run.EventMemo };
                case ChoiceEventIds.LavaFerryman: return new object[] { run.Scaled(LavaFerryman.Price), run.Scaled(LavaFerryman.SwimYears) };
                case ChoiceEventIds.HellLottery: return new object[] { run.Scaled(HellLottery.Price), run.Scaled(HellLottery.Prize) };
                case ChoiceEventIds.Confession: return new object[] { run.Scaled(ConfessionCell.Years) };
                case ChoiceEventIds.FireRain: return new object[] { run.Scaled(FireRain.Price), run.Scaled(FireRain.Years) };
                case ChoiceEventIds.BlindSeer: return new object[] { run.Scaled(BlindSeer.Price) };
                default: return new object[0];
            }
        }

        /// <summary>A swindler's small hint, in every telling (a careful reader learns it).</summary>
        public static string OfferHint(string id)
        {
            switch (id)
            {
                case ChoiceEventIds.ThreeCups: return L("(His thumb never leaves the middle cup.)", "(Başparmağı ortadaki bardaktan hiç kalkmıyor.)");
                case ChoiceEventIds.FakePardon: return L("(The seal is still wet.)", "(Mühür henüz kurumamış.)");
                case ChoiceEventIds.SoulInsurance: return L("(The signature on the policy is written in ash.)", "(Poliçedeki imza külle atılmış.)");
                default: return null;
            }
        }

        /// <summary>A swindler's trap, plainly (the Warlock's prophecy, the Summoner's eye, the Liar's Tongue).</summary>
        public static string OfferTrap(string id)
        {
            switch (id)
            {
                case ChoiceEventIds.ThreeCups: return L("This is a trap: the pea is under a cup one time in five.", "Bu bir tuzak: nohut beş seferde bir bardağın altında.");
                case ChoiceEventIds.FakePardon: return L("This is a trap: half the pardons are forged; the real ones strike less than promised.",
                    "Bu bir tuzak: belgelerin yarısı sahte; gerçekleri vaat edilenden azını siler.");
                case ChoiceEventIds.SoulInsurance: return L("This is a trap: the insurer will be gone by the gate.", "Bu bir tuzak: sigortacı kapıya varmadan kaybolur.");
                default: return null;
            }
        }

        /// <summary>{0} the trap's words: the Warlock sees them in his prophecy.</summary>
        public static string ProphecyTrapFormat => L("PROPHECY: {0}", "KEHANET: {0}");

        /// <summary>{0} the trap's words: the Summoner's imp saw them.</summary>
        public static string EyeTrapFormat => L("THE IMP'S EYE: {0}", "İBLİSİN GÖZÜ: {0}");

        /// <summary>{0} the coins the Summoner's eye asks to look through a swindler.</summary>
        public static string OfferEyeFormat => L("EYE ({0} COINS)", "GÖZ ({0} COİN)");
        public static string OfferEyeDetail => L("An imp looks the stranger over, once.", "Bir iblis yabancıyı bir kez süzer.");

        /// <summary>A choice's button: {0} its coins (or the stake / the years it costs), when it has any.</summary>
        public static string OfferChoice(string id, string choice, int cost)
        {
            string label;
            switch (choice)
            {
                case EventChoices.Pass: return id == ChoiceEventIds.Pickpocket ? L("LET HIM GO", "BIRAK GİTSİN") : L("PASS", "GEÇ");
                case "pay": label = id == ChoiceEventIds.ChainedSoul ? L("BREAK THE CHAIN", "ZİNCİRİ KIR") : id == ChoiceEventIds.HungryGambler
                        ? L("FEED HIM", "DOYUR") : L("PAY", "ÖDE"); break;
                case "relic": label = L("A RELIC", "EMANET"); break;
                case "upgrade": label = L("AN UPGRADE", "YÜKSELTME"); break;
                case "lend": label = L("LEND", "BORÇ VER"); break;
                case "guide": label = L("SHOW THE WAY", "YOLU GÖSTER"); break;
                case "fix": label = L("FIX THE BOOKS", "DEFTERİ DÜZELT"); break;
                case "report": label = L("REPORT HIM", "İHBAR ET"); break;
                case "bet0": case "bet1": case "bet2": label = L("BET", "OYNA"); break;
                case "buy": label = L("BUY", "SATIN AL"); break;
                case "chase": label = L("CHASE HIM", "KOVALA"); break;
                case "let": return L("LET HIM GO", "BIRAK GİTSİN");
                case "ferry": label = L("ROW ME OVER", "GEÇİR BENİ"); break;
                case "swim": label = L("SWIM", "YÜZ"); break;
                case "ticket": label = L("A TICKET", "BİLET"); break;
                case "confess": label = L("CONFESS", "İTİRAF ET"); break;
                case "coins": label = L("THROW COINS", "COİN FIRLAT"); break;
                case "years": label = L("BURN", "YAN"); break;
                case "ask": label = L("ASK", "SOR"); break;
                default: label = L("TAKE IT", "AL"); break;
            }
            return cost > 0 ? label + " " + cost : label;
        }

        /// <summary>A choice's line under the buttons.</summary>
        public static string OfferChoiceDetail(string id, string choice)
        {
            switch (id + "/" + choice)
            {
                case ChoiceEventIds.FallenAngel + "/relic": return L("A rare relic. Years on your sentence.", "Nadir bir emanet. Cezana yıl eklenir.");
                case ChoiceEventIds.FallenAngel + "/upgrade": return L("A class upgrade. Years on your sentence.", "Bir sınıf yükseltmesi. Cezana yıl eklenir.");
                case ChoiceEventIds.Accountant + "/fix": return L("The next three tables: ante one less.", "Sonraki üç masa: ante bir eksik.");
                case ChoiceEventIds.Accountant + "/report": return L("Coins now; the next warden's purse half again.", "Şimdi coin; sonraki bekçinin kesesi yarısı kadar büyür.");
                case ChoiceEventIds.Pickpocket + "/chase": return L("Three hands against him: come out ahead and you get it all back, and more.",
                    "Ona karşı üç el: önde bitirirsen hepsini geri alırsın, fazlasıyla.");
                case ChoiceEventIds.LavaFerryman + "/ferry": return L("Skip the next floor.", "Sonraki katı atla.");
                case ChoiceEventIds.LavaFerryman + "/swim": return L("Skip the next floor; years on your sentence.", "Sonraki katı atla; cezana yıl eklenir.");
                case ChoiceEventIds.Confession + "/confess": return L("Years off now; the demon starts with his malice full.", "Şimdi yıl silinir; şeytan kötülüğü dolu başlar.");
                case ChoiceEventIds.FireRain + "/coins": return L("Coins from your purse.", "Kesenden coin.");
                case ChoiceEventIds.FireRain + "/years": return L("Years on your sentence.", "Cezana yıl.");
                default: return null;
            }
        }

        /// <summary>How it went: the outcome's words (<see cref="EventOutcome.Result"/>); {0} a number of it (the stake, the bar's share).</summary>
        public static string OfferResult(string id, string result)
        {
            switch (id + "/" + result)
            {
                case ChoiceEventIds.ChainedSoul + "/relic": return L("The chain breaks. The soul presses something into your hand.", "Zincir kırıldı. Ruh avucuna bir şey sıkıştırdı.");
                case ChoiceEventIds.ChainedSoul + "/years": return L("The chain breaks. \"I will speak for you below.\"", "Zincir kırıldı. \"Aşağıda senin için konuşacağım.\"");
                case ChoiceEventIds.ChainedSoul + "/imp": return L("The chain falls off by itself — it was an imp all along. He laughs and is gone with your coins.",
                    "Zincir kendiliğinden düştü — baştan beri bir iblismiş. Gülerek coin'lerinle kayboldu.");
                case ChoiceEventIds.FallenAngel + "/relic": return L("The angel gives you a feather of light — a relic.", "Melek sana ışıktan bir tüy verdi — bir emanet.");
                case ChoiceEventIds.FallenAngel + "/upgrade": return L("The angel lays a hand on you. Something in you grows.", "Melek elini üstüne koydu. İçinde bir şey büyüdü.");
                case ChoiceEventIds.HungryGambler + "/pay": return L("\"The next imp shows two cards with his first deal. You didn't hear it from me.\"",
                    "\"Sıradaki iblis ilk dağıtımda iki kartını gösterir. Benden duymadın.\"");
                case ChoiceEventIds.DebtorImp + "/lend": return L("\"See you at the gate!\" He counts the coins twice.", "\"Kapıda görüşürüz!\" Coin'leri iki kez saydı.");
                case ChoiceEventIds.LostSoul + "/guide": return L("The soul whispers what waits two floors ahead. Your next table will cost more: you are tired.",
                    "Ruh iki kat ötede ne beklediğini fısıldadı. Sıradaki masan daha pahalı: yorgunsun.");
                case ChoiceEventIds.Accountant + "/fix": return L("The ledger balances. The next three tables cost one less.", "Defter denkleşti. Sonraki üç masa bir eksik ante.");
                case ChoiceEventIds.Accountant + "/report": return L("Mammon pays the informer. The next warden heard about it.", "Mammon muhbire ödedi. Sonraki bekçi bunu duydu.");
                case ChoiceEventIds.ThreeCups + "/found": return L("The pea! Your stake comes back doubled.", "Nohut! Bahsin iki katı geri geldi.");
                case ChoiceEventIds.ThreeCups + "/lost": return L("Empty. And the next one. And the last.", "Boş. Bir sonraki de. Sonuncusu da.");
                case ChoiceEventIds.FakePardon + "/forged": return L("The ink runs in your hand. Forged.", "Mürekkep elinde akıyor. Sahte.");
                case ChoiceEventIds.FakePardon + "/real": return L("A real pardon — but it strikes fewer years than he said.", "Gerçek bir af — ama söylediğinden az yıl siliyor.");
                case ChoiceEventIds.SoulInsurance + "/buy": return L("You sign. He smiles, folds the policy, and blows on the signature.", "İmzaladın. Gülümsedi, poliçeyi katladı, imzaya üfledi.");
                case ChoiceEventIds.Pickpocket + "/let": return L("He is gone. So are the coins.", "Gitti. Coin'ler de.");
                case ChoiceEventIds.LavaFerryman + "/ferry": return L("The boat creaks over the lava. Your next step skips a floor.", "Kayık lavın üstünde gıcırdıyor. Sonraki adımın bir katı atlar.");
                case ChoiceEventIds.LavaFerryman + "/swim": return L("You swim. It burns. Your next step skips a floor.", "Yüzüyorsun. Yakıyor. Sonraki adımın bir katı atlar.");
                case ChoiceEventIds.HellLottery + "/blank": return L("Blank.", "Boş.");
                case ChoiceEventIds.HellLottery + "/coins": return L("A winner! Coins pour out of the machine.", "Kazandın! Makineden coin akıyor.");
                case ChoiceEventIds.HellLottery + "/relic": return L("The jackpot: a relic of legend.", "Büyük ikramiye: efsanevi bir emanet.");
                case ChoiceEventIds.Confession + "/confess": return L("Years fall away. Somewhere below, a demon smiles.", "Yıllar düştü. Aşağıda bir yerde bir şeytan gülümsedi.");
                case ChoiceEventIds.FireRain + "/coins": return L("The coins melt in the air. You are unburnt.", "Coin'ler havada eridi. Yanmadın.");
                case ChoiceEventIds.FireRain + "/years": return L("It burns.", "Yakıyor.");
                case ChoiceEventIds.SinSwap + "/give": return L("He weighs your sin and shows you two of his.", "Günahını tarttı ve sana iki tanesini gösterdi.");
                case ChoiceEventIds.SinSwap + "/take": return L("A fair trade, he says.", "Adil bir takas, diyor.");
                case ChoiceEventIds.SinSwap + "/none": return L("He has nothing to show you for it. Your sin stays yours.",
                    "Karşılığında gösterecek bir şeyi yok. Günahın sende kalıyor.");
                case ChoiceEventIds.BlindSeer + "/long": return L("\"His bar is long.\"", "\"Barı uzun.\"");
                case ChoiceEventIds.BlindSeer + "/medium": return L("\"His bar is neither long nor short.\"", "\"Barı ne uzun ne kısa.\"");
                case ChoiceEventIds.BlindSeer + "/short": return L("\"His bar is short.\"", "\"Barı kısa.\"");
                default: return L("You walk on.", "Yoluna devam ediyorsun.");
            }
        }

        /// <summary>{0} the coins, signed: the result panel's purse line.</summary>
        public static string OfferCoinsFormat => L("Purse: {0} coins", "Kese: {0} coin");

        /// <summary>{0} the years, signed: the result panel's sentence line.</summary>
        public static string OfferYearsFormat => L("Sentence: {0} years", "Ceza: {0} yıl");

        /// <summary>{0} the relic, {1} its gift, {2} its curse.</summary>
        public static string OfferRelicFormat => L("You carry {0}: {1} But: {2}", "Artık {0} sende: {1} Ama: {2}");

        /// <summary>{0} the upgrade, {1} what it does.</summary>
        public static string OfferUpgradeFormat => L("Upgrade: {0}. {1}", "Yükseltme: {0}. {1}");

        /// <summary>The chapter demon's rule in a few words (the blind seer's); null for none.</summary>
        public static string SeerRule(string bossId)
        {
            switch (bossId)
            {
                case Core.Dealers.DealerRoster.MammonId: return string.Format(L("INTEREST — every hand you do not win, his bar grows {0}%.",
                    "FAİZ — kazanamadığın her elde barı %{0} uzar."), MammonInterest.InterestPercent);
                case Core.Dealers.DealerRoster.BelialId: return L("ROLE SWAP — every fourth hand one of his cards trades places with one of yours.",
                    "ROL TAKASI — her dört elde bir, kartlarından biri seninkiyle yer değiştirir.");
                case Core.Dealers.DealerRoster.LilithId: return string.Format(L("TEMPTATION — what you throw away, she may keep ({0}%).",
                    "BAŞTAN ÇIKARMA — attığın kartı alabilir (%{0})."), LilithTemptation.TakePercent);
                default: return null;
            }
        }

        /// <summary>The seer's words on the demon's rule ({0}: the rule's line).</summary>
        public static string SeerRuleFormat => L("\"His rule: {0}\"", "\"Kuralı: {0}\"");

        // ------------------------------------------------------------------ the gate and the chase

        /// <summary>{0} the coins the debtor imp pays back.</summary>
        public static string GateLoanRepaidFormat => L("The debtor imp waits at the gate: +{0} coins.", "Borçlu iblis kapıda bekliyor: +{0} coin.");
        public static string GateLoanFled => L("The debtor imp is nowhere to be seen.", "Borçlu iblis ortada yok.");
        public static string GateInsurerGone => L("The insurer is gone. His office is a heap of ash.", "Sigortacı yok. Bürosu bir kül yığını.");

        public static string ChaseTitle => L("THE CHASE", "KOVALAMACA");

        /// <summary>{0} the coins the chase brought back.</summary>
        public static string ChaseWonFormat => L("You catch him. He gives back everything, and more: +{0} coins.", "Yakaladın. Her şeyi geri verdi, fazlasıyla: +{0} coin.");
        public static string ChaseLost => L("He gets away.", "Kaçtı.");
    }
}
