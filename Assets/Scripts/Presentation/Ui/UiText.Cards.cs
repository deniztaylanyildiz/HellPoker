namespace HellPoker.Presentation.Ui
{
    /// <summary>Demo 3.5: the relic cards' words (the card itself, the warden's relic, the choice of what to leave behind).</summary>
    internal static partial class UiText
    {
        /// <summary>The coins card's name: {0} the coins.</summary>
        public static string CoinsCardFormat => L("+{0} COINS", "+{0} COIN");
        public static string CoinsCardText => L("Into your purse, at once.", "Hemen kesene girer.");
        public static string UpgradeCardLabel => L("UPGRADE", "YÜKSELTME");

        /// <summary>A card's foot: {0} the price in coins.</summary>
        public static string CardPriceFormat => L("{0} COINS", "{0} COIN");

        /// <summary>A card's foot: {0} the years it adds to the sentence.</summary>
        public static string CardYearsFormat => L("+{0} years", "+{0} yıl");

        public static string CardBagFull => L("Bag full", "Çanta dolu");
        public static string CardNoCoins => L("Not enough coins", "Coin yetmiyor");
        public static string CardYours => L("YOURS", "SENİN");
        public static string CardSoldStamp => L("SOLD", "SATILDI");
        public static string CardTaken => L("TAKEN", "ALINDI");

        /// <summary>A card's two lines: the gift (+) and the curse (−).</summary>
        public static string CardGiftFormat => L("+ {0}", "+ {0}");
        public static string CardCurseFormat => L("− {0}", "− {0}");

        public static string WardenTake => L("TAKE IT", "AL");

        // ------------------------------------------------------------------ the spoils' scene (ARAM's choice)

        public static string LootSceneTitle => L("SPOILS", "GANİMET");

        /// <summary>{0} the demon (as in a sentence).</summary>
        public static string LootSceneSubtitleFormat => L("{0} has fallen. Choose one.", "{0} düştü. Birini seç.");
        public static string LootReroll => L("REROLL", "YENİLE");
        public static string LootRerolled => L("REROLLED", "YENİLENDİ");

        /// <summary>The warden's relic waits (its card says what it is): {0} the coins instead.</summary>
        public static string WardenFullFormat => L("You carry all you may. Take +{0} coins instead — or give up a relic for it.",
            "Taşıyabileceğin kadar taşıyorsun. Yerine +{0} coin al — ya da bir emanetini bunun için bırak.");
        public static string WardenTakeDetail => L("Every place is full: choose what it replaces.", "Her yer dolu: neyin yerine geçeceğini seç.");
        public static string SwapTitle => L("LEAVE WHAT IN ITS PLACE?", "YERİNE NE BIRAKILSIN?");

        /// <summary>{0} the relic that comes.</summary>
        public static string SwapTextFormat => L("The {0} comes; one of yours stays behind for good.", "{0} geliyor; seninkilerden biri burada kalır.");
    }
}
