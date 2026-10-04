using HellPoker.Core.Dealers;
using HellPoker.Core.Events;

namespace HellPoker.Presentation.Ui
{
    /// <summary>The events between hands in words: who offers, what, and the buttons. With the soul on the table, never a number.</summary>
    internal static partial class UiText
    {
        public static string EventAccept => L("ACCEPT", "KABUL ET");
        public static string EventPass => L("PASS", "GEÇ");

        /// <summary>Who makes the offer: a stranger's name, or the demon's.</summary>
        public static string EventOwner(string ownerId)
        {
            switch (ownerId)
            {
                case EventIds.Charon: return L("THE FERRYMAN", "KAYIKÇI");
                case EventIds.SoulBroker: return L("THE SOUL BROKER", "RUH SİMSARI");
                case EventIds.LostSoul: return L("A LOST SOUL", "KAYIP RUH");
                case EventIds.BurningBridge: return L("THE BURNING BRIDGE", "YANAN KÖPRÜ");
                default: return RelicEventOwner(ownerId) ?? Dealer(ownerId).Name;
            }
        }

        public static string EventTitle(string eventId, string dealerId)
        {
            switch (eventId)
            {
                case EventIds.Charon: return L("A FERRY ACROSS", "KARŞIYA BİR KAYIK");
                case EventIds.SoulBroker: return L("A BUYER FOR SOULS", "RUH ALICISI");
                case EventIds.LostSoul: return L("PLAY MY HAND", "ELİMİ OYNA");
                case EventIds.BurningBridge: return L("EVERYTHING ON ONE HAND", "HER ŞEY TEK ELE");
                case EventIds.GraveRobber:
                case EventIds.CursedChest: return RelicEventTitle(eventId);
                case EventIds.DevilsLedger:
                    switch (dealerId)
                    {
                        case DealerRoster.MammonId: return L("DEFER YOUR DEBT", "BORCUNU ERTELE");
                        case DealerRoster.BelialId: return L("A SHOW", "BİR GÖSTERİ");
                        case DealerRoster.LilithId: return L("A NIGHT'S BARGAIN", "GECE PAZARLIĞI");
                    }
                    break;
            }
            return "";
        }

        /// <summary>The offer in the owner's voice. <paramref name="soul"/>: the soul is on the table — no numbers of years.</summary>
        public static string EventText(string eventId, string dealerId, bool soul)
        {
            switch (eventId)
            {
                case EventIds.Charon:
                    return L("\"Your next hand rides my boat: half the ante. But the far shore is near — a win forgives only half.\"",
                        "\"Sonraki elin benim kayığımda: ante yarı. Ama karşı kıyı yakın — kazanırsan da yarısı silinir.\"");
                case EventIds.SoulBroker:
                    return soul
                        ? L("\"A quarter of your soul, and I strike a long stretch off your sentence. The rest of you will burn sooner.\"",
                            "\"Ruhunun dörtte biri, karşılığında cezandan uzun bir parça silerim. Kalanın daha çabuk yanar.\"")
                        : L("\"A quarter of your soul, and I strike 300 years. The rest of you will burn sooner.\"",
                            "\"Ruhunun dörtte biri, karşılığında 300 yıl silerim. Kalanın daha çabuk yanar.\"");
                case EventIds.GraveRobber:
                case EventIds.CursedChest:
                    return RelicEventText(eventId);
                case EventIds.LostSoul:
                    return L("A pale thing sits down beside you. \"Play my hand. It is a good one. But if it loses, you lose three times over.\"",
                        "Yanına solgun bir şey oturuyor. \"Elimi oyna. İyi bir eldir. Ama kaybederse, sen üç kat kaybedersin.\"");
                case EventIds.BurningBridge:
                    return soul
                        ? L("The bridge behind you is on fire. One hand, no table limit, three times the ante: win, and you climb out of the soul's reach — far up. Lose, and it is an ordinary loss.",
                            "Arkandaki köprü yanıyor. Tek el, masa sınırı yok, üç kat ante: kazanırsan ruhunu kurtarıp çok yukarı çıkarsın. Kaybedersen sıradan bir kayıp.")
                        : L("The bridge behind you is on fire. One hand, no table limit, three times the ante: win, and the sentence drops to 1000. Lose, and it is an ordinary loss.",
                            "Arkandaki köprü yanıyor. Tek el, masa sınırı yok, üç kat ante: kazanırsan ceza 1000'e iner. Kaybedersen sıradan bir kayıp.");
                case EventIds.DevilsLedger:
                    switch (dealerId)
                    {
                        case DealerRoster.MammonId:
                            return soul
                                ? L("\"I can ease your burden now — and add a heavier one later. Five hands from now, with interest.\"",
                                    "\"Yükünü şimdi hafifletebilirim — sonra daha ağırını eklerim. Beş el sonra, faiziyle.\"")
                                : L("\"200 years struck now. Five hands from now, 300 come back. Interest, you understand.\"",
                                    "\"Şimdi 200 yıl silerim. Beş el sonra 300 geri gelir. Faiz, anlarsın.\"");
                        case DealerRoster.BelialId:
                            return L("\"Next hand, I show you nothing. Not a single card. And if you win anyway — double, darling.\"",
                                "\"Sonraki elde sana hiçbir şey göstermem. Tek bir kart bile. Yine de kazanırsan — iki kat, canım.\"");
                        case DealerRoster.LilithId:
                            return soul
                                ? L("\"I can forget my malice. Every drop of it. For a little more of your night.\"",
                                    "\"Kötülüğümü unutabilirim. Her damlasını. Gecenden biraz daha fazlası karşılığında.\"")
                                : L("\"I can forget my malice. Every drop of it. For 100 more years in my dark.\"",
                                    "\"Kötülüğümü unutabilirim. Her damlasını. Karanlığımda 100 yıl daha karşılığında.\"");
                    }
                    break;
            }
            return "";
        }

        /// <summary>{0}: the years that came due (Mammon's deferred debt). The soul's version has no number.</summary>
        public static string DeferredDueFormat => L("The deferred debt comes due: +{0} years.", "Ertelenen borç geldi: +{0} yıl.");
        public static string DeferredDueSoul => L("The deferred debt comes due: your soul burns.", "Ertelenen borç geldi: ruhun yanıyor.");

        /// <summary>The lost soul's hand is on the table.</summary>
        public static string GhostHandMessage => L("The lost soul's hand is yours this time. Do not lose it.", "Bu sefer kayıp ruhun eli senin. Kaybetme.");
    }
}
