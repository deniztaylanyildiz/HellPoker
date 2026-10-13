using System;
using System.Collections.Generic;
using HellPoker.Core.Relics;

namespace HellPoker.Presentation.Ui
{
    /// <summary>Demo 2.1's Purgatory Camp and the relics' bag in words.</summary>
    internal static partial class UiText
    {
        // ------------------------------------------------------------------ the camp

        public static string CampTitle => L("PURGATORY CAMP", "ARAF KAMPI");

        /// <summary>{0} the next demon's name.</summary>
        public static string CampTextFormat => L("A fire in the dark, and a moment's peace before {0}. Do one thing here. (Your relics you may arrange as you like.)",
            "Karanlıkta bir ateş, {0} öncesi bir anlık huzur. Burada bir şey yap. (Emanetlerini istediğin gibi düzenleyebilirsin.)");
        public static string CampTwoNote => L("(The fourth circle: the camp offers only two.)", "(Dördüncü daire: kamp yalnızca ikisini sunar.)");

        public static string CampRest => L("REST", "DİNLEN");

        /// <summary>{0} the years off the sentence, {1} the demon's name (genitive), {2} the percent off his bar.</summary>
        public static string CampRestDetailFormat => L("Your sentence −{0} years; {1} bar −{2}%.", "Cezan −{0} yıl; {1} barı −%{2}.");

        public static string CampWhet => L("WHET THE SOUL", "RUHUNU BİLE");

        /// <summary>{0} the demon's name (genitive), {1} the percent on his bar.</summary>
        public static string CampWhetDetailFormat => L("A class upgrade now; {0} bar +{1}%.", "Şimdi bir sınıf yükseltmesi; {0} barı +%{1}.");

        public static string CampRite => L("RITE", "TÖREN");
        public static string CampRiteDetail => L("Break the demon's first cheat, or silence a relic's curse for this chapter.",
            "Şeytanın ilk hilesini boz ya da bir emanetin lanetini bu bölüm boyunca sustur.");
        public static string CampRiteWhich => L("THE RITE", "TÖREN");
        public static string CampRiteText => L("Which rite?", "Hangi tören?");

        public static string CampForge => L("FORGE", "DÖV");
        public static string CampForgeDetail => L("A worn relic's gift half again — its curse stays. Once a relic.",
            "Takılı bir emanetin lütfü yarısı kadar artar — laneti kalır. Emanet başına bir kez.");
        public static string CampForgeNone => L("Nothing to forge (no worn relic not yet forged).", "Dövülecek bir şey yok (dövülmemiş takılı emanet yok).");
        public static string CampForgeWhich => L("THE FORGE", "DÖVME");
        public static string CampForgeText => L("Which relic goes into the fire?", "Hangi emanet ateşe girsin?");

        /// <summary>{0} the relic forged.</summary>
        public static string CampForgedFormat => L("{0} comes out of the fire glowing: its gift is half again.", "{0} ateşten kor gibi çıktı: lütfü yarısı kadar arttı.");

        /// <summary>{0} the years off the sentence.</summary>
        public static string CampRestedFormat => L("You rest by the fire. Your sentence: −{0} years; the demon's bar a little shorter.",
            "Ateşin başında dinlendin. Cezan: −{0} yıl; şeytanın barı biraz kısaldı.");

        public static string CampArrange => L("ARRANGE RELICS", "EMANET DÜZENİ");
        public static string CampArrangeDetail => L("Change what you wear and what stays in the bag. Not an action: as often as you like.",
            "Takılı olanla çantadakini değiştir. Eylem sayılmaz: istediğin kadar.");

        // ------------------------------------------------------------------ the bag

        public static string BagTitle => L("RELICS", "EMANETLER");
        public static string BagWornLabel => L("WORN (they act)", "TAKILI (etki eder)");
        public static string BagBagLabel => L("BAG (they wait)", "ÇANTA (bekler)");
        public static string BagDone => L("DONE", "TAMAM");
        public static string BagDrop => L("LEAVE IT BEHIND", "GERİDE BIRAK");
        public static string BagDropSure => L("SURE? LEAVE IT", "EMİN MİSİN? BIRAK");
        public static string BagHint => L("Pick a relic, then where it goes.", "Bir emanet seç, sonra gideceği yeri.");
        public static string BagLookOnly => L("Only at a camp can they change places.", "Yerleri yalnızca kampta değişir.");
        public static string BagPickFirst => L("Pick the relic to leave first.", "Önce bırakılacak emaneti seç.");
        public static string BagForgedMark => L("(forged)", "(dövülmüş)");
        public static string LeaveNewRelic => L("LEAVE THE NEW ONE", "YENİSİNİ BIRAK");
        public static string LossSpared => L("A relic spares you: the hand counts as a push.", "Bir emanet seni korudu: el berabere sayıldı.");

        /// <summary>{0} the relic left behind.</summary>
        public static string BagDroppedFormat => L("{0} is left behind.", "{0} geride kaldı.");

        /// <summary>{0} the relic put on, {1} the one put away (empty for none).</summary>
        public static string BagMovedFormat => L("Worn: {0}.", "Takıldı: {0}.");

        /// <summary>{0} worn, {1} the slots, {2} in the bag, {3} the bag's room (the map's line).</summary>
        public static string MapRelicsCountFormat => L("Worn {0}/{1} · bag {2}/{3}", "Takılı {0}/{1} · çanta {2}/{3}");
        public static string MapRelicsButton => L("RELICS", "EMANETLER");
        public static string MapBagLabel => L("In the bag:", "Çantada:");

        /// <summary>The worn relics' sum in words, the gifts and the curses that move a hand: "Win +12%, ante −10%, malice +1 a hand".</summary>
        public static string RelicTotals(RelicEffects e)
        {
            var parts = new List<string>();
            void Percent(int value, string en, string tr)
            {
                if (value == 100) return;
                int d = value - 100;
                parts.Add(L(en, tr) + " " + (d > 0 ? "+" : "−") + L(Math.Abs(d) + "%", "%" + Math.Abs(d)));
            }
            void Count(int value, string en, string tr)
            {
                if (value != 0) parts.Add(L(en, tr) + " " + (value > 0 ? "+" : "−") + Math.Abs(value));
            }
            Percent(e.WinPercent, "win", "kazanç");
            Percent(e.AntePercent, "ante", "ante");
            Count(e.HouseCardsDelta, "house cards shown", "kasanın açık kartı");
            Count(e.ReRaiseExtraUnits, "house re-raise", "kasanın yeniden artırışı");
            Count(e.MaliceExtraPerHand, "malice a hand", "her el kötülük");
            Count(e.RedrawsPerTable, "redraws a table", "masa başına yeniden çekiş");
            Percent(e.TreasurePercent, "treasure", "hazine");
            Percent(e.MarketPercent, "market", "pazar");
            Percent(e.TributePercent, "tribute", "haraç");
            Percent(e.ImpPursePercent, "imps' purses", "iblis keseleri");
            Percent(e.BossBarPercent, "demons' bars", "şeytan barları");
            if (e.SoulLossPercent >= 0) parts.Add(L("soul hand losses ×", "ruh elinde kayıp ×") + (e.SoulLossPercent / 100.0).ToString("0.##"));
            if (e.WinAntePercent > 0) parts.Add(L("win +", "kazanç +") + L(e.WinAntePercent + "% of an ante", "ante'nin %" + e.WinAntePercent + " kadarı"));
            if (e.LossAntePercent > 0) parts.Add(L("loss +", "kayıp +") + L(e.LossAntePercent + "% of an ante", "ante'nin %" + e.LossAntePercent + " kadarı"));
            if (parts.Count == 0) return L("Worn: nothing that moves a hand.", "Takılı: elleri değiştiren bir şey yok.");
            string text = string.Join(", ", parts);
            return L("Worn: ", "Takılı: ") + Upper(text.Substring(0, 1)) + text.Substring(1);
        }
    }
}
