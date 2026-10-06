using HellPoker.Core.Events;
using HellPoker.Core.Relics;

namespace HellPoker.Presentation.Ui
{
    /// <summary>The cursed relics in words: names, the gift and the curse, the offers that bring them, the Bone Die's redraw.</summary>
    internal static partial class UiText
    {
        public static string RelicName(string id)
        {
            switch (id)
            {
                case RelicIds.BoneDie: return L("BONE DIE", "KEMİK ZAR");
                case RelicIds.RustyCrown: return L("RUSTY CROWN", "PASLI TAÇ");
                case RelicIds.FerrymansCoin: return L("FERRYMAN'S COIN", "KAYIKÇI SİKKESİ");
                case RelicIds.ThornedRosary: return L("THORNED ROSARY", "DİKENLİ TESPİH");
                case RelicIds.JestersRattle: return L("THE JESTER'S RATTLE", "SOYTARI'NIN ÇINGIRAĞI");
                default: return (id ?? "").ToUpperInvariant();
            }
        }

        public static string RelicGift(string id)
        {
            switch (id)
            {
                case RelicIds.BoneDie: return L("Once at each demon's table (full again at every summons to Lucifer), before the draw, redraw a card.",
                    "Her şeytanın masasında bir kez (Lucifer'e her çağrılmada dolar), değişten önce bir kartı yeniden çek.");
                case RelicIds.RustyCrown: return L("A win forgives a twentieth more.", "Kazanç yirmide bir fazla siler.");
                case RelicIds.FerrymansCoin: return L("The ante is four fifths.", "Ante beşte dört.");
                case RelicIds.ThornedRosary: return L("The soul burns slower when you lose.", "Kaybedince ruhun daha yavaş yanar.");
                case RelicIds.JestersRattle: return L("A won hand forgives half an ante more.", "Kazanılan el ante'nin yarısı kadar daha fazla yıl siler.");
                default: return "";
            }
        }

        public static string RelicCurse(string id)
        {
            switch (id)
            {
                case RelicIds.BoneDie: return L("The House re-raises two units.", "Kasa iki birim artırır.");
                case RelicIds.RustyCrown: return L("The demon's malice grows faster.", "Şeytanın kötülüğü daha hızlı dolar.");
                case RelicIds.FerrymansCoin: return L("The House shows one card fewer.", "Kasa bir kart eksik gösterir.");
                case RelicIds.ThornedRosary: return L("A win forgives a tenth less.", "Kazanç onda bir eksik siler.");
                case RelicIds.JestersRattle: return L("A lost hand adds half an ante more.", "Kaybedilen el ante'nin yarısı kadar daha fazla yıl ekler.");
                default: return "";
            }
        }

        /// <summary>The relic's hover box: {0} gift, {1} curse.</summary>
        public static string RelicDescriptionFormat => L("+ {0}\n− {1}", "+ {0}\n− {1}");

        /// <summary>{0} the relic, {1} the gift, {2} the curse.</summary>
        public static string RelicTakenFormat => L("You carry the {0} now. {1} But: {2}", "Artık {0} sende. {1} Ama: {2}");

        public static string RedrawPrompt => L("Pick the card the Bone Die throws back (click the die again: cancel).", "Kemik Zar'ın geri atacağı kartı seç (zara yine tıkla: vazgeç).");
        public static string RedrawNotNow => L("The Bone Die rolls once at each demon's table (full again at every summons to Lucifer), before the draw, on a card you can see.",
            "Kemik Zar her şeytanın masasında bir kez (Lucifer'e her çağrılmada dolar), değişten önce, gördüğün bir karta atılır.");
        /// <summary>{0}: the card that came.</summary>
        public static string RedrawnFormat => L("The Bone Die rolls: {0} comes in.", "Kemik Zar atıldı: {0} geldi.");

        // ------------------------------------------------------------------ the offers that bring a relic

        public static string RelicEventOwner(string ownerId)
        {
            switch (ownerId)
            {
                case EventIds.GraveRobber: return L("THE GRAVE ROBBER", "MEZAR SOYGUNCUSU");
                case EventIds.CursedChest: return L("A CURSED CHEST", "LANETLİ SANDIK");
                default: return null;
            }
        }

        public static string RelicEventTitle(string eventId) =>
            eventId == EventIds.GraveRobber ? L("SOMETHING DUG UP", "TOPRAKTAN ÇIKAN") : L("A LID THAT KNOCKS", "AÇILMAK İSTEYEN KAPAK");   // "...WANTS OPENING" ran past the panel

        public static string RelicEventText(string eventId) =>
            eventId == EventIds.GraveRobber
                ? L("\"Found it in a saint's grave. Cursed, of course. Everything down here is. Yours, if you want it.\" (A relic: one gift, one curse.)",
                    "\"Bir azizin mezarında buldum. Lanetli tabii. Burada her şey öyle. İstersen senin.\" (Bir emanet: bir lütuf, bir lanet.)")
                : L("A chest sits by your chair. Something inside knocks. (A relic: one gift, one curse. You may carry two.)",
                    "Sandalyenin yanında bir sandık duruyor. İçinden bir şey vuruyor. (Bir emanet: bir lütuf, bir lanet. İki tane taşıyabilirsin.)");
    }
}
