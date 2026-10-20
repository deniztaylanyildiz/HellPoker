using HellPoker.Core.Chapters;

namespace HellPoker.Presentation.Ui
{
    /// <summary>Demo 3.5: the black market's scene (the stall's cards, the services' tiles, the vendor's thanks).</summary>
    internal static partial class UiText
    {
        /// <summary>The relics carried at the top: {0} held, {1} the most (worn and in the bag).</summary>
        public static string MarketRelicsFormat => L("RELICS {0}/{1}", "EMANETLER {0}/{1}");

        public static string MarketSoldOut => L("Sold. Every relic here is sold once.", "Satıldı. Buradaki her emanet bir kez satılır.");
        public static string MarketBoxWon => L("Yours now: out of the box.", "Artık senin: kutudan çıktı.");
        public static string MarketBoxName => L("A CLOSED BOX", "KAPALI KUTU");

        /// <summary>Under the box bought: {0} its rarity.</summary>
        public static string MarketBoxRarityFormat => L("{0} BOX", "{0} KUTU");

        public static string MarketTileFree => L("FREE", "BEDAVA");
        public static string MarketTileDoublePrice => L("THE BOX", "KUTU");
        public static string MarketTilePawn => L("PAWN YOUR SOUL", "RUHUNU REHİN VER");

        /// <summary>The crucible's tile: two of {0} for one of {1}.</summary>
        public static string MarketTileTransmuteFormat => L("2 {0} = 1 {1}", "2 {0} = 1 {1}");
        public static string MarketTileTransmutePrice => L("FREE", "BEDAVA");
        public static string MarketTileCleanse => L("BURN A CURSE", "LANETİ SİL");
        public static string MarketTileEye => L("IMP'S EYE", "İBLİSİN GÖZÜ");
        public static string MarketTileJokerIn => L("A JOKER IN", "JOKER EKLE");
        public static string MarketTileJokerOut => L("A JOKER OUT", "JOKER ÇIKAR");
        public static string MarketJokersRefused => L("Not now: the coins, or the jokers, won't allow it.", "Şimdi olmaz: ya coin ya da jokerler buna izin vermez.");
        public static string MarketTileDrop => L("THROW ONE AWAY", "BİRİNİ AT");
        public static string MarketTileSell => L("SELL A RELIC", "EMANET SAT");
        public static string MarketTileSellPrice => L("HALF PRICE", "YARI FİYAT");
        public static string MarketTileHagglePrice => L("20% OFF OR ON", "%20 İNER YA DA ÇIKAR");
        public static string MarketTileReroll => L("NEW STALL", "VİTRİNİ YENİLE");

        public static string MarketPickDrop => L("Which one goes? It is gone for good, its gift and its curse with it.",
            "Hangisi gitsin? Lütfüyle, lanetiyle sonsuza dek gider.");
        public static string MarketPickSell => L("Which one do you sell? Half its price, and it is gone for good.",
            "Hangisini satıyorsun? Fiyatının yarısı; sonsuza dek gider.");

        /// <summary>The vendor's word when something is bought.</summary>
        public static string VendorThanks(MarketVendor vendor)
        {
            switch (vendor)
            {
                case MarketVendor.Pawnshop: return L("\"A pleasure. Mammon thanks you, in his way.\"", "\"Memnun oldum. Mammon teşekkür eder, kendince.\"");
                case MarketVendor.BoneMerchant: return L("\"Bones for bones. A fair trade.\"", "\"Kemiğe kemik. Adil bir takas.\"");
                case MarketVendor.Alchemist: return L("\"Careful — it is still warm.\"", "\"Dikkat — hâlâ sıcak.\"");
                case MarketVendor.GamblersStall: return L("\"Ha! Lady Luck smiles. Today.\"", "\"Ha! Şans gülümsüyor. Bugünlük.\"");
                case MarketVendor.Tailor: return L("\"It fits you like a curse.\"", "\"Sana bir lanet gibi oturdu.\"");
                default: return L("\"You never saw me. Off you go.\"", "\"Beni hiç görmedin. Haydi yolun açık olsun.\"");
            }
        }
    }
}
