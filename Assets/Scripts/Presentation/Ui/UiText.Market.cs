using HellPoker.Core.Chapters;

namespace HellPoker.Presentation.Ui
{
    /// <summary>Demo 2.1's black markets: six vendors, each with a stall, a voice and wares of his own.</summary>
    internal static partial class UiText
    {
        public static string VendorTitle(MarketVendor vendor)
        {
            switch (vendor)
            {
                case MarketVendor.Pawnshop: return L("MAMMON'S PAWNSHOP", "MAMMON'UN REHİNCİ DÜKKÂNI");
                case MarketVendor.BoneMerchant: return L("THE BONE MERCHANT", "KEMİK TÜCCARI");
                case MarketVendor.Alchemist: return L("THE WANDERING ALCHEMIST", "GEZGİN SİMYACI");
                case MarketVendor.GamblersStall: return L("THE GAMBLER'S STALL", "KUMARBAZ TEZGÂHI");
                case MarketVendor.Tailor: return L("THE DEVIL'S TAILOR", "ŞEYTANIN TERZİSİ");
                default: return L("THE SMUGGLER", "KAÇAKÇI");
            }
        }

        public static string VendorOwner(MarketVendor vendor)
        {
            switch (vendor)
            {
                case MarketVendor.Pawnshop: return L("A pawnbroker in green", "Yeşiller içinde bir rehinci");
                case MarketVendor.BoneMerchant: return L("A merchant of bones", "Bir kemik tüccarı");
                case MarketVendor.Alchemist: return L("An alchemist on the road", "Yollarda bir simyacı");
                case MarketVendor.GamblersStall: return L("A grinning stallkeeper", "Sırıtan bir tezgâhtar");
                case MarketVendor.Tailor: return L("The devil's tailor", "Şeytanın terzisi");
                default: return L("A smuggler", "Bir kaçakçı");
            }
        }

        /// <summary>The vendor's greeting; {0} the purse.</summary>
        public static string VendorGreetingFormat(MarketVendor vendor)
        {
            switch (vendor)
            {
                case MarketVendor.Pawnshop: return L("\"Everything has a price, sinner. Even you.\" Your purse: {0} coins.",
                    "\"Her şeyin bir fiyatı var, günahkâr. Senin bile.\" Kesen: {0} coin.");
                case MarketVendor.BoneMerchant: return L("\"Fine relics, cheap. The bones pay the rest — your bones.\" Your purse: {0} coins.",
                    "\"Güzel emanetler, ucuza. Gerisini kemikler öder — senin kemiklerin.\" Kesen: {0} coin.");
                case MarketVendor.Alchemist: return L("\"Two lesser things make one greater. And a curse is only an impurity.\" Your purse: {0} coins.",
                    "\"İki küçük şey bir büyük eder. Lanet de bir kirden ibaret.\" Kesen: {0} coin.");
                case MarketVendor.GamblersStall: return L("\"A box! What's inside? Nobody knows — that's the fun.\" Your purse: {0} coins.",
                    "\"Bir kutu! İçinde ne var? Kimse bilmez — eğlencesi de bu.\" Kesen: {0} coin.");
                case MarketVendor.Tailor: return L("\"A new cut for your soul? It will fit you like a sin.\" Your purse: {0} coins.",
                    "\"Ruhuna yeni bir biçim? Sana bir günah gibi oturur.\" Kesen: {0} coin.");
                default: return L("\"Quiet. What do you need?\" Your purse: {0} coins.", "\"Sessiz. Ne lazım?\" Kesen: {0} coin.");
            }
        }

        public static string VendorHaggleWon(MarketVendor vendor) =>
            vendor == MarketVendor.Pawnshop ? L("\"Fine, fine. A fifth off — for the soul I'll get one day.\"", "\"Peki, peki. Beşte bir indirim — bir gün alacağım ruh için.\"")
            : L("\"You drive a hard bargain. A fifth off, then.\"", "\"Sıkı pazarlıkçısın. Beşte bir indirim o zaman.\"");

        public static string VendorHaggleLost(MarketVendor vendor) =>
            vendor == MarketVendor.BoneMerchant ? L("\"Insults? Prices are up a fifth. Now.\"", "\"Hakaret mi? Fiyatlar beşte bir arttı. Hemen.\"")
            : L("\"You think I'm a fool? A fifth more, then.\"", "\"Beni aptal mı sandın? Beşte bir fazla o zaman.\"");

        public static string MarketHaggle => L("HAGGLE", "PAZARLIK");
        public static string MarketHaggleDetail => L("Once: half the time every price a fifth less — else a fifth more.",
            "Bir kez: yarı yarıya bütün fiyatlar beşte bir iner — ya da beşte bir artar.");

        /// <summary>{0} the coins the stall is turned over for.</summary>
        public static string MarketRerollFormat => L("NEW STALL {0}", "VİTRİNİ YENİLE {0}");
        public static string MarketRerollDetail => L("Once: the wares turned over for new ones.", "Bir kez: mallar yenileriyle değişir.");
        public static string MarketRerolled => L("New wares on the table.", "Masada yeni mallar.");

        /// <summary>{0} the relic, {1} the coins it fetches.</summary>
        public static string MarketSellFormat => L("SELL {0} +{1}", "SAT: {0} +{1}");
        public static string MarketSellDetail => L("Half its price. It is gone for good.", "Fiyatının yarısı. Sonsuza dek gider.");

        /// <summary>{0} coins now, {1} years.</summary>
        public static string MarketPawnFormat => L("PAWN YOUR SOUL +{0}", "RUHUNU REHİN BIRAK +{0}");
        public static string MarketPawnDetailFormat => L("Coins now; {0} years on your sentence (and the demon's bar at the gate).",
            "Şimdi coin; cezana {0} yıl (kapıda şeytanın barına da).");

        /// <summary>{0} the years on the sentence for a relic of the bone merchant.</summary>
        public static string MarketBoneDetailFormat => L("+{0} years on your sentence.", "Cezana +{0} yıl.");

        /// <summary>{0} the rarity given (two of it), {1} the rarity got.</summary>
        public static string MarketTransmuteFormat => L("TWO {0} → ONE {1}", "İKİ {0} → BİR {1}");
        public static string MarketTransmuteDetail => L("Pick two relics of the rarity; one of the next takes their place.",
            "Aynı nadirlikten iki emanet seç; yerine bir üst nadirlikten biri gelir.");
        public static string MarketTransmuteWhich => L("Which two go into the crucible?", "Hangi ikisi potaya girsin?");

        /// <summary>{0} the first relic picked.</summary>
        public static string MarketTransmuteSecondFormat => L("{0} is in the crucible. And the second?", "{0} potada. İkincisi?");

        /// <summary>{0} the relic made.</summary>
        public static string MarketTransmutedFormat => L("The crucible hisses: {0}.", "Pota tısladı: {0}.");

        /// <summary>{0} the price.</summary>
        public static string MarketCleanseFormat => L("BURN A CURSE {0}", "LANETİ SİL {0}");
        public static string MarketCleanseDetail => L("A relic's curse burned away for good; its gift stays.", "Bir emanetin laneti kalıcı silinir; lütfü kalır.");
        public static string MarketCleanseWhich => L("Whose curse burns?", "Kimin laneti yansın?");

        /// <summary>{0} the relic cleansed.</summary>
        public static string MarketCleansedFormat => L("{0} is clean now: its gift alone.", "{0} artık temiz: yalnızca lütfü.");

        /// <summary>{0} the price.</summary>
        public static string MarketBoxFormat => L("A CLOSED BOX {0}", "KAPALI KUTU {0}");
        public static string MarketBoxDetail => L("A relic inside; its rarity rolled: common 60%, rare 32%, legendary 8%.",
            "İçinde bir emanet; nadirliği zarla: yaygın %60, nadir %32, efsanevi %8.");

        /// <summary>{0} the box's rarity.</summary>
        public static string MarketBoxBoughtFormat => L("A box rattles in your hands: {0}.", "Elinde bir kutu tıkırdıyor: {0}.");
        public static string MarketBoxOpen => L("OPEN IT", "AÇ");
        public static string MarketBoxDouble => L("DOUBLE OR NOTHING", "ÇİFT YA DA HİÇ");
        public static string MarketBoxDoubleDetail => L("Half the time two of the box's rarity — else the box is gone.",
            "Yarı yarıya kutunun nadirliğinden iki tane — ya da kutu gider.");
        public static string MarketBoxLost => L("The stallkeeper laughs and pockets the box.", "Tezgâhtar güldü ve kutuyu cebine attı.");

        /// <summary>{0} the upgrade, {1} the price.</summary>
        public static string MarketUpgradeFormat => L("{0} {1}", "{0} {1}");

        /// <summary>{0} the next warden's purse.</summary>
        public static string MarketWardenTipFormat => L("\"The next warden sits on {0} coins.\"", "\"Sıradaki bekçi {0} coin üstünde oturuyor.\"");
    }
}
