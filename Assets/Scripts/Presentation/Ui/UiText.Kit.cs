using HellPoker.Core.Chapters;
using HellPoker.Core.Sinners;

namespace HellPoker.Presentation.Ui
{
    /// <summary>Phase 2's class mechanics (the satchel, the prophecy, the treasury) and the demons' own rules, in words.</summary>
    internal static partial class UiText
    {
        // ------------------------------------------------------------------ the class choice and the Sinners page (Phase 2)

        /// <summary>What the class does in Phase 2's chapters (the class card of a chapter run).</summary>
        public static string ChapterSinnerAbility(string id)
        {
            switch (id)
            {
                case Peasant.ClassId: return L("Satchel: keep a thrown card (4 at most) and draw it back. Power: harvest unseen.",
                    "Heybe: attığın bir kartı sakla (en çok 4), sonra ondan çek. Güç: gizli hasat.");
                case Warlock.ClassId: return L("Sees lies and the deck's top 1-3 cards. Power: send seen cards to the bottom.",
                    "Yalanları ve destenin üstündeki 1-3 kartı görür. Güç: gördüğünü dibe yolla.");
                case King.ClassId: return L("A tax per win: open a card 1, free raise 2, seize 4. No cheat touches his J Q K.",
                    "Kazanç başına vergi: kart aç 1, bedava artır 2, müsadere 4. J Q K'sına hile işlemez.");
                default: return SinnerAbility(id);
            }
        }

        /// <summary>The price of the class in Phase 2's chapters.</summary>
        public static string ChapterSinnerDetail(string id)
        {
            switch (id)
            {
                case Peasant.ClassId: return L("Dip into the satchel and the demon re-raises more.", "Heybeye uzanırsan şeytan daha çok artırır.");
                case Warlock.ClassId: return L("The demon knows one of your cards.", "Şeytan kartlarından birini bilir.");
                case King.ClassId: return L("A demon's hand lost halves the taxes.", "Kaybedilen boss eli vergiyi yarıya indirir.");
                default: return SinnerDetail(id);
            }
        }

        /// <summary>The Sinners page's Phase 2 part: the chapters' powers in place of the demo's.</summary>
        public static string SinnersPagePhaseTwo()
        {
            string Line(string id) => $"{SinnerName(id)}: {ChapterSinnerAbility(id)} {ChapterSinnerDetail(id)}\n";
            return L("PHASE 2 (the chapters): the gauge charges as ever, but the powers change.\n",
                       "PHASE 2 (bölümler): gösterge aynı dolar, ama güçler değişir.\n")
                   + Line(Peasant.ClassId) + Line(Warlock.ClassId) + Line(King.ClassId)
                   + L("JESTER: his jokers, as ever.", "SOYTARI: jokerleri, her zamanki gibi.");
        }

        // ------------------------------------------------------------------ the panel under the dialog box

        /// <summary>{0} cards in the satchel, {1} its size.</summary>
        public static string KitSatchelTitleFormat => L("SATCHEL {0}/{1}", "HEYBE {0}/{1}");
        public static string KitKeepTitle => L("KEEP ONE?", "BİRİNİ SAKLA?");
        public static string KitProphecyTitle => L("TOP OF THE DECK", "DESTENİN ÜSTÜ");

        /// <summary>{0} cards that may still go to the bottom.</summary>
        public static string KitRearrangeTitleFormat => L("TO THE BOTTOM: {0}", "DİBE YOLLA: {0}");
        public static string KitShuffleComing => L("SHUFFLE COMING", "DESTE KARILACAK");

        /// <summary>{0} the taxes, {1} the most.</summary>
        public static string KitTreasuryTitleFormat => L("TAXES {0}/{1}", "VERGİ {0}/{1}");

        /// <summary>The treasury's buttons: {0} the price.</summary>
        public static string KitRevealButtonFormat => L("OPEN {0}", "AÇ {0}");
        public static string KitFreeRaiseButtonFormat => L("FREE {0}", "BEDAVA {0}");
        public static string KitSeizeButtonFormat => L("SEIZE {0}", "MÜSADERE {0}");

        public static string KitSatchelHint => L(
            "In the draw, click a satchel card: it fills a thrown place (the demon sees it: he re-raises more). After the draw, " +
            "click a thrown card to keep it. K (charged): HARVEST, every thrown place from the satchel, unseen.",
            "Draw'da bir heybe kartına tıkla: atılan bir yerin yerine gelir (şeytan görür: daha çok artırır). Draw'dan sonra " +
            "atılan bir karta tıklayıp sakla. K (dolu): HASAT, atılan her yer heybeden, gizlice.");

        public static string KitProphecyHint => L(
            "The next cards of the deck, the first on the left. The demon knows one of your cards each hand. K (charged): send up to " +
            "2 of them to the bottom (click them).",
            "Destenin sıradaki kartları, ilki solda. Şeytan her elde kartlarından birini bilir. K (dolu): en çok 2 tanesini dibe " +
            "yolla (tıkla).");

        /// <summary>{0} the most taxes, {1} open, {2} free raise, {3} seize.</summary>
        public static string KitTreasuryHintFormat => L(
            "A tax for every hand you win (at most {0}); a demon's hand lost takes half. Before the draw, once each: OPEN ({1}) turns a " +
            "card of the demon's, FREE ({2}) makes your next raise his money, SEIZE ({3}) swaps a card of yours for his best open one. " +
            "K (charged): a seizure for nothing. No cheat touches your J, Q or K.",
            "Kazandığın her elde bir vergi (en çok {0}); kaybedilen boss eli yarısını alır. Draw'dan önce, her biri bir kez: AÇ ({1}) " +
            "şeytanın bir kartını çevirir, BEDAVA ({2}) sıradaki artırmanı onun parasıyla yapar, MÜSADERE ({3}) bir kartını onun en iyi " +
            "açık kartıyla değiştirir. K (dolu): bedava müsadere. J, Q ve K'na hiçbir hile dokunamaz.");

        public static string KitTreasuryHint =>
            string.Format(KitTreasuryHintFormat, ClassKit.TaxMax, ClassKit.RevealPrice, ClassKit.FreeRaisePrice, ClassKit.SeizePrice);

        // ------------------------------------------------------------------ what the acts say

        /// <summary>{0} satchel cards picked for the draw.</summary>
        public static string SatchelTakeFormat => L("From the satchel in the draw: {0} card(s).", "Draw'da heybeden: {0} kart.");
        public static string SatchelOnlyInDraw => L("The satchel opens in the draw.", "Heybe draw'da açılır.");
        public static string SatchelTooMany => L("More satchel cards than thrown places: throw more, or put one back.",
            "Atılan yerden fazla heybe kartı: daha çok at ya da birini geri koy.");

        /// <summary>{0} the card kept.</summary>
        public static string SatchelKeptFormat => L("{0} goes into the satchel.", "{0} heybeye girdi.");
        public static string HarvestDone => L("Harvest: the satchel fills your thrown places, unseen.", "Hasat: heybe attığın yerleri doldurdu, kimse görmedi.");

        /// <summary>{0} cards that may still go to the bottom.</summary>
        public static string RearrangePromptFormat => L("Click a top card: it goes to the bottom ({0} left).", "Üstteki bir karta tıkla: dibe gider ({0} kaldı).");

        /// <summary>{0} the card.</summary>
        public static string RearrangedFormat => L("{0} goes to the bottom of the deck.", "{0} destenin dibine gitti.");
        public static string SeizePrompt => L("Seize: click the card you give.", "Müsadere: vereceğin karta tıkla.");

        /// <summary>{0} the card taken, {1} the card given.</summary>
        public static string SeizedFormat => L("Seized: {0}, for your {1}.", "Müsadere: {1} karşılığında {0} senin.");
        public static string RevealedMessage => L("One of the demon's cards turns.", "Şeytanın bir kartı açıldı.");
        public static string FreeRaiseMessage => L("Your next raise this hand is the demon's money.", "Bu eldeki sıradaki artırman şeytanın parasıyla.");
        public static string TaxesHalved => L("A demon's hand lost: half your taxes are gone.", "Boss eli kaybedildi: vergilerinin yarısı gitti.");

        public static string KitNotNow => L("Not now.", "Şimdi olmaz.");
        public static string KitNotBeforeDraw => L("Only before the draw.", "Sadece draw'dan önce.");
        public static string KitAlreadyUsed => L("Already done this hand.", "Bu el zaten yapıldı.");
        public static string KitEmpty => L("The satchel is empty.", "Heybe boş.");
        public static string KitNothingThrown => L("Pick the cards to throw first.", "Önce atacağın kartları seç.");
        public static string KitNoCard => L("No card for it.", "Bunun için kart yok.");
        public static string KitCannotRaise => L("No raise can be made this hand.", "Bu el artırma yapılamaz.");

        /// <summary>{0} the taxes, {1} the price.</summary>
        public static string KitNoTaxesFormat => L("Not enough taxes: {0} of {1}.", "Vergi yetmiyor: {0} / {1}.");

        public static string KitRefused(KitRefusal refusal, int charge, int full, int taxes = 0, int price = 0)
        {
            switch (refusal)
            {
                case KitRefusal.NotCharged:
                    return string.Format(PowerChargingFormat, charge, full, ChargeRules.Default.PerWin, ChargeRules.Default.PerLoss, ChargeRules.Default.PerFold);
                case KitRefusal.NoTaxes: return string.Format(KitNoTaxesFormat, taxes, price);
                case KitRefusal.AlreadyUsed: return KitAlreadyUsed;
                case KitRefusal.Empty: return KitEmpty;
                case KitRefusal.NothingThrown: return KitNothingThrown;
                case KitRefusal.NoCard: return KitNoCard;
                case KitRefusal.CannotRaise: return KitCannotRaise;
                default: return KitNotNow;
            }
        }

        // ------------------------------------------------------------------ the demons' rules

        /// <summary>The rule's name (the log, the hover).</summary>
        public static string DemonRuleName(string ruleId)
        {
            switch (ruleId)
            {
                case "interest": return L("INTEREST", "FAİZ");
                case "role_swap": return L("ROLE SWAP", "ROL TAKASI");
                case "temptation": return L("TEMPTATION", "BAŞTAN ÇIKARMA");
                case "mirror": return L("MIRROR", "AYNA");
                default: return "";
            }
        }

        /// <summary>The rule as the demon says it when the table opens (Lucifer's mirror after the class it copies).</summary>
        public static string DemonRuleLine(DemonRule rule)
        {
            switch (rule)
            {
                case MammonInterest _:
                    return string.Format(L("My rule is INTEREST: every hand you do not win, my bar grows {0}%.",
                        "Kuralım FAİZ: kazanamadığın her elde barım %{0} uzar."), MammonInterest.InterestPercent);
                case BelialRoleSwap _:
                    return L("My rule is the ROLE SWAP: every fourth hand, before the draw, one of my cards trades places with one of yours.",
                        "Kuralım ROL TAKASI: her dört elde bir, draw'dan önce kartlarımdan biri seninkiyle yer değiştirir.");
                case LilithTemptation _:
                    return string.Format(L("My rule is TEMPTATION: what you throw away, I may keep ({0}%).",
                        "Kuralım BAŞTAN ÇIKARMA: attığın kartı alabilirim (%{0})."), LilithTemptation.TakePercent);
                case LuciferMirror mirror:
                    switch (mirror.Mirrors)
                    {
                        case KitKind.Satchel: return L("My rule is the MIRROR: I too keep a thrown card, and play it.",
                            "Kuralım AYNA: ben de attığım bir kartı saklar, sonra oynarım.");
                        case KitKind.Prophecy: return L("My rule is the MIRROR: I too see the deck's top card, and draw by it.",
                            "Kuralım AYNA: ben de destenin üstündeki kartı görür, ona göre çekerim.");
                        case KitKind.Treasury:
                            return string.Format(L("My rule is the MIRROR: a tax for every hand I win; at {0} I seize one of your cards.",
                                "Kuralım AYNA: kazandığım her elde vergi alırım; {0} vergide kartlarından birine el koyarım."), ClassKit.SeizePrice);
                        default: return L("My rule is the MIRROR: your jokers are mine as well.", "Kuralım AYNA: jokerlerin benim de jokerim.");
                    }
                default: return null;
            }
        }

        /// <summary>{0} the interest in percent of the bar.</summary>
        public static string InterestFormat => L("Interest: the bar grows {0}%.", "Faiz: bar %{0} uzadı.");

        /// <summary>{0} the card given, {1} the card got.</summary>
        public static string RoleSwapFormat => L("Role swap: your {0} went, {1} came.", "Rol takası: {0} gitti, {1} geldi.");
        public static string RoleSwapComing => L("The prophecy: next hand, Belial's role swap.", "Kehanet: sonraki el Belial'in rol takası.");

        /// <summary>{0} the card she kept.</summary>
        public static string TemptedFormat => L("Temptation: Lilith kept your {0}.", "Baştan çıkarma: Lilith attığın {0} kartını aldı.");

        /// <summary>{0} the card he took, {1} the card he gave.</summary>
        public static string MirrorSeizedFormat => L("The mirror: Lucifer seized your {0} for his {1}.", "Ayna: Lucifer {1} karşılığında {0} kartına el koydu.");
        public static string MirrorSatchel => L("The mirror: Lucifer drew from his own satchel.", "Ayna: Lucifer kendi heybesinden çekti.");
        public static string MirrorTopCard => L("The mirror: Lucifer saw the top card and drew it.", "Ayna: Lucifer üstteki kartı görüp çekti.");
        public static string MirrorSentDown => L("The mirror: Lucifer saw the top card and sent it to the bottom.", "Ayna: Lucifer üstteki kartı görüp destenin dibine yolladı.");
        public static string CorneredLine => L("Cornered. Now I cheat faster.", "Köşeye sıkıştım. Artık daha hızlı hile yaparım.");

        /// <summary>The chapter's opening words: {0} the purse it was topped up to.</summary>
        public static string PurseToppedUpFormat => L("Your purse was filled up to {0} coins.", "Kesen {0} coin'e tamamlandı.");
    }
}
