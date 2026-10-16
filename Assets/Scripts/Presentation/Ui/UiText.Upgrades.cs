using HellPoker.Core.Chapters;

namespace HellPoker.Presentation.Ui
{
    /// <summary>The class upgrades of Phase 2 in words: names, what each does, where they come from (the spoils, the fire).</summary>
    internal static partial class UiText
    {
        public static string UpgradeName(string id)
        {
            switch (id)
            {
                case UpgradeIds.WideSatchel: return L("WIDE SATCHEL", "GENİŞ HEYBE");
                case UpgradeIds.QuietSatchel: return L("QUIET SATCHEL", "SESSİZ HEYBE");
                case UpgradeIds.Seed: return L("SEED", "TOHUM");
                case UpgradeIds.Abundance: return L("ABUNDANCE", "BEREKET");
                case UpgradeIds.DeepProphecy: return L("THIRD EYE", "ÜÇÜNCÜ GÖZ");
                case UpgradeIds.BlindMirror: return L("BLIND MIRROR", "KÖR AYNA");
                case UpgradeIds.Thrice: return L("FATE WEAVER", "KADER DOKUMACISI");
                case UpgradeIds.EyeOfTruth: return L("PROPHECY SEAL", "KEHANET MÜHRÜ");
                case UpgradeIds.OverflowingTreasury: return L("OVERFLOWING TREASURY", "TAŞAN HAZİNE");
                case UpgradeIds.CheapSeizure: return L("CHEAP SEIZURE", "UCUZ MÜSADERE");
                case UpgradeIds.CrownGuard: return L("CROWN GUARD", "TAÇ MUHAFIZI");
                case UpgradeIds.RoyalDecree: return L("ROYAL DECREE", "KRALİYET FERMANI");
                case UpgradeIds.ControlledChaos: return L("CONTROLLED CHAOS", "KONTROLLÜ KAOS");
                case UpgradeIds.DoubleLaugh: return L("DOUBLE LAUGH", "ÇİFT KAHKAHA");
                case UpgradeIds.JokerPurse: return L("JOKER PURSE", "JOKER KESESİ");
                case UpgradeIds.Buffoon: return L("BUFFOON", "ŞAKLABAN");
                default: return (id ?? "").ToUpperInvariant();
            }
        }

        public static string UpgradeText(string id)
        {
            switch (id)
            {
                case UpgradeIds.WideSatchel: return L("The satchel holds four cards.", "Heybe dört kart alır.");
                case UpgradeIds.QuietSatchel: return L("The demon notices the satchel half as often.", "Şeytan heybeyi yarı yarıya daha az fark eder.");
                case UpgradeIds.Seed: return L("Every new chapter puts a ten or better in your satchel.", "Her yeni bölüm heybene onlu ya da daha iyi bir kart koyar.");
                case UpgradeIds.Abundance: return L("After a draw two thrown cards may go into the satchel.", "Değişten sonra atılan iki kart heybeye girebilir.");
                case UpgradeIds.DeepProphecy: return L("FORESIGHT sees one card more.", "ÖNSEZİ bir kart fazla görür.");
                case UpgradeIds.BlindMirror: return L("Half the hands the demon learns none of your cards.", "Ellerin yarısında şeytan kartlarından hiçbirini öğrenemez.");
                case UpgradeIds.Thrice: return L("BEND FATE moves two cards.", "KADERİ BÜK iki kart oynatır.");
                case UpgradeIds.EyeOfTruth: return string.Format(L("Walk away from a swindler's offer: +{0} coins.",
                    "Bir dolandırıcının teklifini geri çevir: +{0} coin."), ClassKit.SealCoins);
                case UpgradeIds.OverflowingTreasury: return L("The treasury holds fifteen taxes.", "Hazine on beş vergi alır.");
                case UpgradeIds.CheapSeizure: return L("A seizure costs 3 taxes.", "Müsadere 3 vergi.");
                case UpgradeIds.CrownGuard: return L("A demon's hand lost takes a quarter of your taxes, not half.",
                    "Kaybedilen boss eli vergilerinin yarısını değil dörtte birini alır.");
                case UpgradeIds.RoyalDecree: return L("A free raise costs 1 tax.", "Bedava artırma 1 vergi.");
                case UpgradeIds.ControlledChaos: return L("Every lost hand takes a joker out of your deck (never below two).",
                    "Kaybedilen her el destenden bir joker çıkarır (ikinin altına inmez).");
                case UpgradeIds.DoubleLaugh: return L("A draw that brings you two jokers throws one back for the next card.",
                    "Sana iki joker getiren değiş birini geri atar, yerine sıradaki kart gelir.");
                case UpgradeIds.JokerPurse: return L("The black market's joker service costs half.", "Kara Pazar'ın joker hizmeti yarı fiyat.");
                case UpgradeIds.Buffoon: return L("With one joker in your hand the demon re-raises half as often.",
                    "Elinde tek joker varken şeytan yarı yarıya daha az artırır.");
                default: return "";
            }
        }

        /// <summary>The tag of an upgrade on a panel.</summary>
        public static string UpgradeTag => L("UPGRADE", "YÜKSELTME");

        /// <summary>The loot's upgrade option: {0} the upgrade.</summary>
        public static string LootUpgradeFormat => L("UPGRADE: {0}", "YÜKSELTME: {0}");

        /// <summary>{0} the upgrade, {1} what it does.</summary>
        public static string UpgradeTakenFormat => L("{0}: {1}", "{0}: {1}");

        /// <summary>The upgrades on the class's hover: {0} the list.</summary>
        public static string UpgradesLineFormat => L("Upgrades: {0}", "Yükseltmeler: {0}");


        public static string MapUpgradesLabel => L("UPGRADES", "YÜKSELTMELER");

        public static string FireWhet => L("WHET YOUR SOUL", "RUHUNU BİLE");

        /// <summary>{0} the demon's name with its genitive, in a sentence; {1} the percent.</summary>
        public static string FireWhetDetailFormat => L("A class upgrade at once. {0} bar starts {1}% longer.",
            "Hemen bir sınıf yükseltmesi. {0} barı %{1} uzun başlar.");
        public static string FireWhetNone => L("Your class has every upgrade it may get.", "Sınıfının alabileceği bütün yükseltmeler sende.");

        /// <summary>{0} the upgrade, {1} what it does.</summary>
        public static string FireDoneWhetFormat => L("The fire whets your soul. {0}: {1}\nThe demon's bar will start longer.",
            "Ateş ruhunu biledi. {0}: {1}\nŞeytanın barı uzun başlayacak.");

        /// <summary>{0} the card the Seed put in the satchel.</summary>
        public static string SownFormat => L("The Seed: {0} lies in your satchel.", "Tohum: {0} heybende.");

        public static string TaxesQuartered => L("A demon's hand lost: the Crown Guard kept three quarters of your taxes.",
            "Boss eli kaybedildi: Taç Muhafızı vergilerinin dörtte üçünü korudu.");
        public static string DoubleLaughLine => L("The Double Laugh: your second joker went back for the next card.",
            "Çift Kahkaha: ikinci jokerin geri gitti, yerine sıradaki kart geldi.");
    }
}
