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
                case Peasant.ClassId: return string.Format(L("SATCHEL: keep {0} thrown cards to draw later. Power: HARVEST, unseen.",
                    "HEYBE: attığın bir kartı sakla (en çok {0}), sonra çek. Güç: gizli HASAT."), ClassKit.SatchelSize);
                case Warlock.ClassId: return string.Format(L("FORESIGHT: your next {0} cards, and the lies. Power: BEND FATE, one up or down.",
                    "ÖNSEZİ: gelecek {0} kart ve yalanlar. Güç: KADERİ BÜK, biri üste ya da dibe."), ClassKit.ForesightCards);
                case King.ClassId: return L("A tax per win: open a card 1, free raise 2, seize 4. No cheat touches his J Q K.",
                    "Kazanç başına vergi: kart aç 1, bedava artır 2, müsadere 4. J Q K'sına hile işlemez.");
                case Summoner.ClassId: return L("IMPS: EYE 5, HAND 15, WHISPER 8 coins. Power: one imp free.",
                    "İBLİSLER: GÖZ 5, EL 15, FISILTI 8 coin. Güç: bir iblis bedava.");
                case Thief.ClassId: return L("GLANCE: one closed card of the demon's shows. Power: STEAL one, blind.",
                    "GÖZ UCU: şeytanın bir kapalı kartı açık. Güç: ÇAL, körlemesine.");
                case Jester.ClassId: return L("Jokers in the deck: one is any card, two lose. A win adds one; at twenty they fall to ten.",
                    "Destede joker: biri istediğin kart, ikisi kayıp. Kazanç bir ekler; yirmide sayı ona iner.");
                default: return SinnerAbility(id);
            }
        }

        /// <summary>The price of the class in Phase 2's chapters.</summary>
        public static string ChapterSinnerDetail(string id)
        {
            switch (id)
            {
                case Peasant.ClassId: return L("Draw from it: he raises more.", "Kullanırsan şeytan çok artırır.");
                case Warlock.ClassId: return L("MIRROR CURSE: the demon knows a card.", "AYNA LANETİ: şeytan bir kartını bilir.");
                case King.ClassId: return L("A boss's hand lost halves the taxes.", "Boss eli kaybı vergiyi yarılar.");
                case Thief.ClassId: return L("CAUGHT 1 in 4: a fine.", "4'te 1 YAKALANIR: ceza.");
                case Summoner.ClassId: return L("Imps cost bar at a boss. 1 in 10 runs wild.",
                    "Boss'ta iblis bar ister. 10'da 1 azar.");
                default: return SinnerDetail(id);
            }
        }

        public static string ChapterRulesButton => L("POWERS", "GÜÇLER");

        /// <summary>{0} the satchel's size, {1} its re-raise risk, {2} the taxes' most, {3}-{5} the treasury's prices, {6} the interest,
        /// {7} the temptation's chance, {8} the cornered line.</summary>
        public static string ChapterRulesPageFormat => L(
            "POWERS AND RULES. K (or the badge): the class's power, once every hand, whenever it makes sense.\n" +
            "PEASANT, SATCHEL: after the draw keep one thrown card ({0} at most). In the draw click it to take it, but the demon sees: " +
            "his re-raise comes {1}% more often. K, in the draw: HARVEST, every thrown place from the satchel, unseen.\n" +
            "ORACLE, FORESIGHT: once the cards are dealt, sees the {10} cards his draw will take, and the lies. MIRROR CURSE: the demon " +
            "knows one of his cards every hand. K, before the draw: BEND FATE, {9} seen card to the top (the draw takes it first) or to the bottom.\n" +
            "KING, TREASURY: a tax for every hand won (at most {2}). Before the draw: OPEN {3}, FREE raise {4}, SEIZE {5}. No cheat " +
            "touches his J, Q or K. A demon's hand lost halves the taxes. K, before the draw: a seizure at half price.\n" +
            "JESTER: his jokers, as ever. SUMMONER, IMPS: EYE (a closed card) 5, HAND (yours for an open one) 15, WHISPER (the next cheat, " +
            "true) 8 coins — at a demon's table the bar grows 2 / 5 / 3%; one imp in ten runs wild and lowers a card. K, before the draw: the LEGION, the hand's first imp free.\n" +
            "THIEF, QUICK HAND: one of the demon's face-down cards plays face up. K, before the draw: STEAL, a card of yours for one of his face-down ones, " +
            "blind; caught ({10}%) no swap and a fine (an ante's coins, at a demon's {11}% of the bar).\n\n" +
            "THE DEMONS' RULES (their own tables): MAMMON, INTEREST: every hand you do not win, the bar grows {6}%. BELIAL, ROLE SWAP: " +
            "every 4th hand one of his face-down cards trades places with one of yours. LILITH, TEMPTATION: a card you throw that betters " +
            "her hand, she may keep ({7}%). LUCIFER, MIRROR: he copies your class. Under {8}% of the bar a demon cheats faster.",
            "GÜÇLER VE KURALLAR. K (ya da rozet): sınıfın gücü, her elde bir kez, anlamlı olduğu her an.\n" +
            "KÖYLÜ, HEYBE: draw'dan sonra attığın bir kartı sakla (en çok {0}). Draw'da ona tıklayıp al, ama şeytan görür: artırması " +
            "%{1} daha sık gelir. K, draw'da: HASAT, atılan her yer heybeden, gizlice.\n" +
            "KÂHİN, ÖNSEZİ: kartlar dağıtılınca draw'da gelecek {10} kartı ve yalanları görür. AYNA LANETİ: şeytan her elde " +
            "kartlarından birini bilir. K, draw'dan önce: KADERİ BÜK, gördüğü {9} kartı en üste (draw'da ilk o gelir) ya da dibe yollar.\n" +
            "KRAL, HAZİNE: kazandığı her elde vergi (en çok {2}). Draw'dan önce: AÇ {3}, BEDAVA artırma {4}, MÜSADERE {5}. J, Q ve " +
            "K'sına hile işlemez. Kaybedilen boss eli vergiyi yarıya indirir. K, draw'dan önce: yarı vergiye müsadere.\n" +
            "SOYTARI: jokerleri, her zamanki gibi. ÇAĞIRICI, İBLİSLER: GÖZ (kapalı bir kart) 5, EL (seninki açık biriyle) 15, FISILTI " +
            "(sıradaki hile, doğrusu) 8 coin — şeytan masasında bar %2 / 5 / 3 uzar; onda bir iblis azar ve bir kartını düşürür. K, draw'dan önce: LEJYON, elin ilk iblisi bedava.\n" +
            "HIRSIZ, EL ÇABUKLUĞU: şeytanın kapalı kartlarından biri açık gelir. K, draw'dan önce: ÇAL, bir kartını onun kapalı biriyle körlemesine " +
            "değiş; yakalanırsan (%{10}) takas yok, ceza var (bir ante coin, şeytan masasında barın %{11} kadarı).\n\n" +
            "ŞEYTANLARIN KURALLARI (kendi masalarında): MAMMON, FAİZ: kazanamadığın her elde bar %{6} uzar. BELIAL, ROL TAKASI: her " +
            "4. elde kapalı kartlarından biri seninkiyle yer değiştirir. LILITH, BAŞTAN ÇIKARMA: attığın kart elini iyileştiriyorsa " +
            "alabilir (%{7}). LUCIFER, AYNA: sınıfını kopyalar. Bar %{8} altına inince şeytan daha hızlı hile yapar.");

        public static string ChapterRulesPage() =>
            string.Format(ChapterRulesPageFormat, ClassKit.SatchelSize, ClassKit.SatchelReRaisePercent, ClassKit.TaxMax, ClassKit.RevealPrice,
                ClassKit.FreeRaisePrice, ClassKit.SeizePrice, MammonInterest.InterestPercent, LilithTemptation.TakePercent, DemonRule.CorneredPercent,
                ClassKit.RearrangesPerUse, ClassKit.ForesightCards, ClassKit.CaughtPercent, TheftPowers.FineBarPercent);
        // ------------------------------------------------------------------ the panel under the dialog box

        /// <summary>{0} cards in the satchel, {1} its size.</summary>
        public static string KitSatchelTitleFormat => L("SATCHEL {0}/{1}", "HEYBE {0}/{1}");
        public static string KitKeepTitle => L("KEEP ONE?", "BİRİNİ SAKLA?");
        public static string KitProphecyTitle => L("FORESIGHT", "ÖNSEZİ");

        /// <summary>{0} cards that may still go to the bottom.</summary>
        public static string KitRearrangeTitleFormat => L("BEND FATE: {0}", "KADERİ BÜK: {0}");
        public static string KitShuffleComing => L("SHUFFLE COMING", "DESTE KARILACAK");

        /// <summary>{0} the taxes, {1} the most.</summary>
        public static string KitTreasuryTitleFormat => L("TAXES {0}/{1}", "VERGİ {0}/{1}");

        /// <summary>The treasury's buttons: {0} the price.</summary>
        public static string KitRevealButtonFormat => L("OPEN {0}", "AÇ {0}");
        public static string KitFreeRaiseButtonFormat => L("FREE {0}", "BEDAVA {0}");
        public static string KitSeizeButtonFormat => L("SEIZE {0}", "MÜSADERE {0}");

        public static string KitSatchelHint => L(
            "In the draw, click a satchel card: it fills a thrown place (the demon sees it: he re-raises more). After the draw, " +
            "click a thrown card to keep it. K, once a hand in the draw: HARVEST, every thrown place from the satchel, unseen.",
            "Draw'da bir heybe kartına tıkla: atılan bir yerin yerine gelir (şeytan görür: daha çok artırır). Draw'dan sonra " +
            "atılan bir karta tıklayıp sakla. K, elde bir kez draw'da: HASAT, atılan her yer heybeden, gizlice.");

        /// <summary>{0}: the cards K may send down.</summary>
        public static string KitProphecyHintFormat => L(
            "FORESIGHT, once the cards are dealt: the cards your draw will take, the first on the left (shut between hands and after the draw). " +
            "MIRROR CURSE: the demon knows one of your cards each hand. BEND FATE (K or an arrow, once a hand before the draw): {0} seen card " +
            "to the top (up arrow: the draw takes it first) or to the bottom (down arrow).",
            "ÖNSEZİ, kartlar dağıtılınca: draw'da gelecek kartlar, ilki solda (eller arasında ve draw'dan sonra kapalı). AYNA LANETİ: " +
            "şeytan her elde kartlarından birini bilir. KADERİ BÜK (K ya da bir ok, elde bir kez draw'dan önce): gördüğün {0} kartı en üste " +
            "(yukarı ok: draw'da ilk o gelir) ya da dibe (aşağı ok) yolla.");

        public static string KitProphecyHint => string.Format(KitProphecyHintFormat, ClassKit.RearrangesPerUse);

        /// <summary>The prophecy's panel while it is shut: its title and its line (between hands; after the draw).</summary>
        public static string KitProphecyShutTitle => L("FORESIGHT", "ÖNSEZİ");
        public static string KitProphecyAfterDeal => L("Shows after the deal.", "Dağıtımdan sonra görünür.");
        public static string KitProphecyNextDeal => L("Shut until the next deal.", "Sonraki dağıtıma kadar kapalı.");

        // ------------------------------------------------------------------ the K power: once a hand (2.1.1)

        public static string KitUsedThisHand => L("You used it this hand.", "Bu elde kullandın.");
        public static string KitAfterTheDeal => L("After the deal.", "Dağıtımdan sonra.");
        public static string KitNotAfterDraw => L("Not after the draw.", "Draw'dan sonra olmaz.");
        public static string KitHarvestInDraw => L("The harvest is made in the draw.", "Hasat draw'da yapılır.");
        public static string KitNoBottom => L("The Seer's Spectacles: no card goes to the bottom.", "Kâhinin Gözlüğü: hiçbir kart dibe gidemez.");

        /// <summary>The class badge's line over it in Phase 2: the power ready (K), or spent for this hand.</summary>
        public static string PowerUsedThisHand => L("USED THIS HAND", "BU ELDE KULLANILDI");
        public static string PowerOncePerHandHint => L("K: the power, once every hand.", "K: güç, her elde bir kez.");

        /// <summary>{0} the most taxes, {1} open, {2} free raise, {3} seize.</summary>
        public static string KitTreasuryHintFormat => L(
            "A tax for every hand you win (at most {0}); a demon's hand lost takes half. Before the draw, once each: OPEN ({1}) turns a " +
            "card of the demon's, FREE ({2}) makes your next raise his money, SEIZE ({3}) swaps a card of yours for his best open one. " +
            "K, once a hand: a seizure at half price. No cheat touches your J, Q or K.",
            "Kazandığın her elde bir vergi (en çok {0}); kaybedilen boss eli yarısını alır. Draw'dan önce, her biri bir kez: AÇ ({1}) " +
            "şeytanın bir kartını çevirir, BEDAVA ({2}) sıradaki artırmanı onun parasıyla yapar, MÜSADERE ({3}) bir kartını onun en iyi " +
            "açık kartıyla değiştirir. K, elde bir kez: yarı vergiye müsadere. J, Q ve K'na hiçbir hile dokunamaz.");

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
        public static string RearrangePromptFormat => L("Bend fate: the up arrow brings a card to the top, the down arrow sends it to the bottom ({0} left).",
            "Kaderi bük: yukarı ok kartı en üste çeker, aşağı ok dibe yollar ({0} kaldı).");

        /// <summary>{0} the card brought to the top.</summary>
        public static string BentUpFormat => L("{0} comes to the top: your draw takes it first.", "{0} en üste çıktı: draw'da ilk o gelir.");
        public static string BendAlreadyOnTop => L("That card is already on top.", "O kart zaten en üstte.");

        /// <summary>The Prophecy Seal paid for a swindler walked away from: {0} the coins (signed).</summary>
        public static string ProphecySealFormat => L("Prophecy Seal: you saw the swindle and walked away. {0} coins.",
            "Kehanet Mührü: dolandırıcılığı görüp yürüdün. {0} coin.");

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
                case KitRefusal.NoBottom: return KitNoBottom;
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
                {
                    string line = MirrorLine(mirror.Mirrors);
                    // The fifth circle: a second mechanic too.
                    return mirror.Second.HasValue ? line + " " + string.Format(L("And {0}", "Ve {0}"), MirrorSecondLine(mirror.Second.Value)) : line;
                }
                default: return null;
            }
        }

        private static string MirrorLine(KitKind kind)
        {
            switch (kind)
            {
                case KitKind.Satchel: return L("My rule is the MIRROR: I too keep a thrown card, and play it.",
                    "Kuralım AYNA: ben de attığım bir kartı saklar, sonra oynarım.");
                case KitKind.Prophecy: return L("My rule is the MIRROR: I too see the deck's top card, and draw by it.",
                    "Kuralım AYNA: ben de destenin üstündeki kartı görür, ona göre çekerim.");
                case KitKind.Treasury:
                    return string.Format(L("My rule is the MIRROR: a tax for every hand I win; at {0} I seize one of your cards.",
                        "Kuralım AYNA: kazandığım her elde vergi alırım; {0} vergide kartlarından birine el koyarım."), ClassKit.SeizePrice);
                case KitKind.Theft:
                    return string.Format(L("My rule is the MIRROR: I steal too; once every {0} hands, as the draw begins, a card of yours for my worst.",
                        "Kuralım AYNA: ben de çalarım; her {0} elde bir, draw başlarken kartlarından birini en kötü kartımla."), LuciferMirror.StealEveryHands);
                case KitKind.Summon:
                    return string.Format(L("My rule is the MIRROR: I have an Eye too; once every {0} hands it shows me one of your cards.",
                        "Kuralım AYNA: benim de bir Gözüm var; her {0}. elde kartlarından birini bana gösterir."), LuciferMirror.EyeEveryHands);
                default: return L("My rule is the MIRROR: your jokers are mine as well.", "Kuralım AYNA: jokerlerin benim de jokerim.");
            }
        }

        /// <summary>The fifth circle's second copy, after "And": the satchel or the prophecy.</summary>
        private static string MirrorSecondLine(KitKind kind)
        {
            switch (kind)
            {
                case KitKind.Prophecy: return L("I see the deck's top card.", "destenin üstündeki kartı görürüm.");
                default: return L("I keep a thrown card in a satchel of my own.", "attığım bir kartı kendi heybemde saklarım.");
            }
        }

        /// <summary>{0} the interest in percent of the bar.</summary>
        public static string InterestFormat => L("Interest: the bar grows {0}%.", "Faiz: bar %{0} uzadı.");

        /// <summary>{0} the card given, {1} the card got.</summary>
        public static string RoleSwapFormat => L("Role swap: your {0} went, {1} came.", "Rol takası: {0} gitti, {1} geldi.");
        public static string RoleSwapComing => L("Foresight: next hand, Belial's role swap.", "Önsezi: sonraki el Belial'in rol takası.");

        /// <summary>{0} the card she kept.</summary>
        public static string TemptedFormat => L("Temptation: Lilith kept your {0}.", "Baştan çıkarma: Lilith attığın {0} kartını aldı.");

        /// <summary>{0} the card he took, {1} the card he gave.</summary>
        public static string MirrorSeizedFormat => L("The mirror: Lucifer seized your {0} for his {1}.", "Ayna: Lucifer {1} karşılığında {0} kartına el koydu.");
        public static string MirrorSatchel => L("The mirror: Lucifer drew from his own satchel.", "Ayna: Lucifer kendi heybesinden çekti.");
        public static string MirrorTopCard => L("The mirror: Lucifer saw the top card and drew it.", "Ayna: Lucifer üstteki kartı görüp çekti.");
        public static string MirrorEye => L("The mirror: Lucifer's eye saw one of your cards.", "Ayna: Lucifer'in gözü kartlarından birini gördü.");
        public static string MirrorSentDown => L("The mirror: Lucifer saw the top card and sent it to the bottom.", "Ayna: Lucifer üstteki kartı görüp destenin dibine yolladı.");
        public static string CorneredLine => L("Cornered. Now I cheat faster.", "Köşeye sıkıştım. Artık daha hızlı hile yaparım.");

        /// <summary>The chapter's opening words: {0} the purse it was topped up to.</summary>
        public static string PurseToppedUpFormat => L("Your purse was filled up to {0} coins.", "Kesen {0} coin'e tamamlandı.");
    }
}
