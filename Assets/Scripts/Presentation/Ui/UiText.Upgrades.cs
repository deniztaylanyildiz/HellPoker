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
                case UpgradeIds.LightFingers: return L("LIGHT FINGERS", "HAFİF PARMAKLAR");
                case UpgradeIds.NightEye: return L("NIGHT EYE", "GECE GÖZÜ");
                case UpgradeIds.MasterThief: return L("MASTER THIEF", "USTA HIRSIZ");
                case UpgradeIds.LootSack: return L("LOOT SACK", "GANİMET ÇUVALI");
                case UpgradeIds.LoyalPair: return L("TWO LOYAL IMPS", "İKİ SADIK İBLİS");
                case UpgradeIds.TameImps: return L("TAME IMPS", "EVCİL İBLİSLER");
                case UpgradeIds.KeenEye: return L("KEEN EYE", "KESKİN GÖZ");
                case UpgradeIds.BloodOath: return L("BLOOD OATH", "KAN YEMİNİ");
                default: return (id ?? "").ToUpperInvariant();
            }
        }

        public static string UpgradeText(string id)
        {
            switch (id)
            {
                case UpgradeIds.WideSatchel: return L("The SATCHEL a step up: one card more.", "HEYBE bir adım büyür: bir kart fazla.");
                case UpgradeIds.QuietSatchel: return L("The demon notices the satchel half as often.", "Şeytan heybeyi yarı yarıya daha az fark eder.");
                case UpgradeIds.Seed: return L("Every new chapter puts a ten or better in your satchel.", "Her yeni bölüm heybene onlu ya da daha iyi bir kart koyar.");
                case UpgradeIds.Abundance: return L("After a draw two thrown cards may go into the satchel.", "Değişten sonra atılan iki kart heybeye girebilir.");
                case UpgradeIds.DeepProphecy: return ForesightStepText;
                case UpgradeIds.BlindMirror: return L("Half the hands the demon learns none of your cards.", "Ellerin yarısında şeytan kartlarından hiçbirini öğrenemez.");
                case UpgradeIds.Thrice: return L("You see which of your cards the demon knows.", "Şeytanın hangi kartını bildiğini görürsün.");
                case UpgradeIds.EyeOfTruth: return string.Format(L("Walk away from a swindler's offer: +{0} coins.",
                    "Bir dolandırıcının teklifini geri çevir: +{0} coin."), ClassKit.SealCoins);
                case UpgradeIds.OverflowingTreasury: return string.Format(L("The TREASURY a step up: {0} taxes more.", "HAZİNE bir adım büyür: {0} vergi fazla."),
                    ClassKit.TaxStep);
                case UpgradeIds.CheapSeizure: return string.Format(L("The royal discount takes three quarters off a seizure ({0} taxes).",
                    "Kraliyet indirimi müsaderenin dörtte üçünü siler ({0} vergi)."),
                    System.Math.Max(1, (ClassKit.SeizePrice * ClassKit.CheapSeizurePercent + 99) / 100));
                case UpgradeIds.CrownGuard: return L("A demon's hand lost takes a quarter of your taxes, not half.",
                    "Kaybedilen boss eli vergilerinin yarısını değil dörtte birini alır.");
                case UpgradeIds.RoyalDecree: return L("A free raise costs 1 tax.", "Bedava artırma 1 vergi.");
                case UpgradeIds.ControlledChaos: return L("Every lost hand takes a joker out of your deck (never below two).",
                    "Kaybedilen her el destenden bir joker çıkarır (ikinin altına inmez).");
                case UpgradeIds.DoubleLaugh: return L("A draw that brings you two jokers throws one back for the next card.",
                    "Sana iki joker getiren değiş birini geri atar, yerine sıradaki kart gelir.");
                case UpgradeIds.JokerPurse: return L("The black market's joker service costs half.", "Kara Pazar'ın joker hizmeti yarı fiyat.");
                case UpgradeIds.LightFingers: return string.Format(L("The QUICK HAND a step up: caught {0}% less often.",
                    "EL ÇABUKLUĞU bir adım büyür: %{0} daha az yakalanırsın."), ClassKit.CaughtStep);
                case UpgradeIds.LoyalPair: return L("A hand's first two imps are free.", "Elin ilk iki iblisi bedava.");
                case UpgradeIds.TameImps: return L("An imp never runs wild.", "İblis asla azmaz.");
                case UpgradeIds.KeenEye: return L("The EYE turns two of the demon's cards.", "GÖZ şeytanın iki kartını açar.");
                case UpgradeIds.BloodOath: return L("An imp that runs wild lowers one of the demon's cards, not yours.",
                    "Azan iblis senin değil, şeytanın bir kartını düşürür.");
                case UpgradeIds.NightEye: return L("Two of the demon's closed cards show from the deal.", "Şeytanın iki kapalı kartı dağıtımdan açık gelir.");
                case UpgradeIds.MasterThief: return L("STEAL takes the demon's best open card when he shows one.",
                    "ÇAL, şeytanın açık kartı varsa en iyisini alır.");
                case UpgradeIds.LootSack: return string.Format(L("A steal on the floors brings {0} coins too.", "Katta her çalış {0} coin de getirir."), TheftPowers.LootCoins);
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
