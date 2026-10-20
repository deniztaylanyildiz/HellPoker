using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Core.Sinners;

namespace HellPoker.Presentation.Ui
{
    /// <summary>Phase 2's class mechanics (the satchel, the foresight, the treasury, the imps, the steal), their passives and the demons'
    /// own rules, in words. Every number is read from the code (the kit's level when a run is under way, its starting level otherwise).</summary>
    internal static partial class UiText
    {
        // ------------------------------------------------------------------ the class card, the badge's hover, How to Play (Phase 2)

        /// <summary>
        /// What the class does in Phase 2's chapters — the class card and the badge's hover say the same (Demo 2.3): its main power with its
        /// number now and at most; "PASSIVE: name, what it does"; the upgrades the run has. <paramref name="kit"/> null: a run not begun
        /// (the starting level, no upgrades).
        /// </summary>
        public static string ChapterSinnerAbility(string id, ClassKit kit = null)
        {
            string main = ChapterMainPower(id, kit);
            if (main == null) return SinnerAbility(id);
            string text = main;
            string passive = ChapterPassiveLine(id, kit);
            if (passive != null) text += "\n" + passive;
            if (kit != null && kit.Upgrades.Count > 0)
                text += "\n" + string.Format(UpgradesLineFormat, string.Join(", ", kit.Upgrades.Select(UpgradeName)));
            return text;
        }

        /// <summary>
        /// The whole text (Demo 2.3.1): the class card's hover box and the badge's hover — the main power in full with its numbers now and at
        /// most, the passive, what the short card leaves out (the King's palace, the Thief's steal after the draw), the upgrades the run has.
        /// </summary>
        public static string ChapterSinnerFull(string id, ClassKit kit = null)
        {
            ClassKit k = kit ?? KitOf(id);
            if (k == null) return SinnerAbility(id);
            int now = k.PowerValue, most = k.PowerMax;
            string main;
            switch (id)
            {
                case Peasant.ClassId:
                    main = string.Format(L("SATCHEL: after the draw keep one thrown card; in a later draw click it to take it instead of the deck's ({0} cards, at most {1}).",
                        "HEYBE: draw'dan sonra attığın bir kartı sakla; sonraki bir draw'da ona tıklarsan desteden değil ondan çekersin ({0} kart, en çok {1})."), now, most);
                    break;
                case Warlock.ClassId:
                    main = string.Format(L("FORESIGHT: once the cards are dealt, see the next {0} cards your draw will take (at most {1}).",
                        "ÖNSEZİ: kartlar dağıtılınca draw'da gelecek {0} kartı görürsün (en çok {1})."), now, most);
                    break;
                case King.ClassId:
                    main = string.Format(L("TREASURY: a tax for every hand won ({0}, at most {1}); before the draw OPEN {2}, FREE raise {3}, SEIZE {4}. " +
                        "PALACE: no cheat touches your J, Q or K.",
                        "HAZİNE: kazandığın her elde vergi ({0}, en çok {1}); draw'dan önce AÇ {2}, BEDAVA artırma {3}, MÜSADERE {4}. " +
                        "SARAY: J, Q ve K'na hiçbir hile işlemez (J Q K güvende)."), now, most,
                        k.PriceOf(TaxAct.Reveal), k.PriceOf(TaxAct.FreeRaise), k.PriceOf(TaxAct.Seize));
                    break;
                case Summoner.ClassId:
                    main = string.Format(L("IMPS, before the draw: EYE {1} (a closed card turns), HAND {2} (a card of yours for an open one), WHISPER {3} " +
                        "(his next cheat, true) coins; at a demon's table the bar grows. Open: {0} of 3 (his relics open the HAND, then the WHISPER, then halve the prices).",
                        "İBLİSLER, draw'dan önce: GÖZ {1} (kapalı bir kart açılır), EL {2} (bir kartın onun açık biriyle), FISILTI {3} (sıradaki hilesi, doğrusu) " +
                        "coin; şeytan masasında bar uzar. Açık: {0}/3 (emanetleri önce EL'i, sonra FISILTI'yı açar, sonra fiyatları yarıya indirir)."), now,
                        SummonPowers.CoinPrices[0], SummonPowers.CoinPrices[1], SummonPowers.CoinPrices[2]);
                    break;
                case Thief.ClassId:
                    main = string.Format(L("STEAL (K or the button), once a hand from the deal to the showdown — after the draw too, even answering a re-raise: " +
                        "a card of yours for one of the demon's closed ones, blind. Caught {0}% (at best {1}%): no swap, a fine.",
                        "ÇAL (K ya da düğme), elde bir kez, dağıtımdan showdown'a kadar — draw'dan sonra da çalar, şeytanın artırmasına cevap verirken bile: " +
                        "bir kartın onun kapalı biriyle körlemesine yer değişir. %{0} yakalanırsın (en iyi %{1}): takas yok, ceza."), now, most);
                    break;
                case Jester.ClassId:
                    main = string.Format(L("Jokers in the deck: one is any card you name, two lose. A win adds one. " +
                        "SHIELDS: {0} (at most {1}) — a hand lost to your own two jokers is a push.",
                        "Destede joker: biri istediğin kart, ikisi kayıp. Kazanç bir ekler. " +
                        "KALKAN: {0} (en çok {1}) — kendi iki jokerinle kaybettiğin el berabere sayılır."), now, most) + " " +
                        (ClassKit.ShieldsPerChapter ? L("(a chapter)", "(bölüm başına)") : L("(at every table)", "(her masada)"));
                    break;
                default:
                    return ChapterSinnerAbility(id, kit);
            }
            string text = main;
            string passive = ChapterPassiveFull(id, k);
            if (passive != null) text += "\n" + passive;
            if (kit != null && kit.Upgrades.Count > 0)
                text += "\n" + string.Format(UpgradesLineFormat, string.Join(", ", kit.Upgrades.Select(UpgradeName)));
            return text;
        }

        /// <summary>The passive in full: "PASSIVE: name, what it does".</summary>
        public static string ChapterPassiveFull(string id, ClassKit kit)
        {
            switch (id)
            {
                case Peasant.ClassId: return L("PASSIVE: SILENT FIELD, the demon never sees a hand's first satchel card (a second one he sees: his re-raise comes more often).",
                    "PASİF: SESSİZ TARLA, elin ilk heybe kartını şeytan görmez (ikincisini görür: o el daha sık artırır).");
                case Warlock.ClassId: return L("PASSIVE: SEES THE TRUTH, the demons' lies and the swindlers' traps.",
                    "PASİF: GERÇEĞİ GÖRÜR, şeytanların yalanlarını ve dolandırıcıların tuzaklarını görürsün.");
                case King.ClassId: return string.Format(L("PASSIVE: ROYAL DISCOUNT, a hand's seizure costs half ({0} taxes, not {1}).",
                    "PASİF: KRALİYET İNDİRİMİ, elin müsaderesi yarı fiyata ({1} değil {0} vergi)."), kit.PriceOf(TaxAct.Seize), kit.SeizeFullPrice);
                case Summoner.ClassId: return string.Format(L("PASSIVE: LOYAL IMP, a hand's first {0} imp(s) come free.", "PASİF: SADIK İBLİS, elin ilk {0} iblisi bedava."),
                    kit.FreeImpsPerHand);
                case Thief.ClassId: return L("PASSIVE: GLANCE, one of the demon's closed cards plays face up from the deal.",
                    "PASİF: GÖZ UCU, şeytanın kapalı kartlarından biri dağıtımdan açık gelir.");
                default: return null;
            }
        }

        /// <summary>The price of the class in full (the class card's hover box).</summary>
        public static string ChapterSinnerDetailFull(string id)
        {
            switch (id)
            {
                case Peasant.ClassId: return L("Price: a second satchel card in a hand the demon sees, and he raises more.", "Bedel: elin ikinci heybe kartını şeytan görür, daha çok artırır.");
                case Warlock.ClassId: return L("Price, MIRROR CURSE: every hand the demon knows one of your cards.", "Bedel, AYNA LANETİ: şeytan her elde kartlarından birini bilir.");
                case King.ClassId: return L("Price: a demon's hand lost takes half your taxes.", "Bedel: kaybedilen boss eli vergilerinin yarısını alır.");
                case Thief.ClassId: return string.Format(L("Price: caught, nothing changes hands and you pay a fine (an ante's coins; at a demon's table {0}% of the bar).",
                    "Bedel: yakalanırsan takas olmaz, ceza ödersin (bir ante coin; şeytan masasında barın %{0} kadarı)."), TheftPowers.FineBarPercent);
                case Summoner.ClassId: return L("Price: at a demon's table imps cost bar; one in ten runs wild and lowers a card.",
                    "Bedel: şeytan masasında iblisler bar ister; onda biri azar ve bir kartını düşürür.");
                default: return ChapterSinnerDetail(id);
            }
        }

        /// <summary>The main power in one sentence, its number now (<paramref name="kit"/>'s level, else the start) and at its highest.</summary>
        public static string ChapterMainPower(string id, ClassKit kit = null)
        {
            ClassKit k = kit ?? KitOf(id);
            if (k == null) return null;
            int now = k.PowerValue, most = k.PowerMax;
            // Short: six class cards side by side leave about thirteen letters a line and six lines for both sentences (the price below).
            switch (id)
            {
                case Peasant.ClassId: return string.Format(L("SATCHEL: keep a thrown card ({0}, max {1}).", "HEYBE: atılan kartı sakla ({0}, en çok {1})."), now, most);
                case Warlock.ClassId: return string.Format(L("FORESIGHT: see {0} next cards (max {1}).", "ÖNSEZİ: gelecek {0} kart (en çok {1})."), now, most);
                case King.ClassId: return string.Format(L("TREASURY: taxes ({0}, max {1}).", "HAZİNE: vergi ({0}, en çok {1})."), now, most);
                case Summoner.ClassId: return string.Format(L("IMPS: EYE {1}, HAND {2}, WHISPER {3}; {0}/3 open.",
                    "İBLİS: GÖZ {1}, EL {2}, FISILTI {3}; {0}/3 açık."), now,
                    SummonPowers.CoinPrices[0], SummonPowers.CoinPrices[1], SummonPowers.CoinPrices[2]);
                case Thief.ClassId: return string.Format(L("STEAL: caught {0}% (best {1}%).", "ÇAL: %{0} yakalanır (en iyi %{1})."), now, most);
                case Jester.ClassId: return string.Format(L("Jokers: one is any card, two lose; a win adds one. Shields {0}/{1}.",
                    "Joker: biri her kart, ikisi kayıp; kazanınca artar. Kalkan {0}/{1}."), now, most);
                default: return null;
            }
        }

        /// <summary>The class's passive: "PASSIVE: name, what it does" (the Jester has none — his jokers are his).</summary>
        public static string ChapterPassiveLine(string id, ClassKit kit = null)
        {
            switch (id)
            {
                case Peasant.ClassId: return L("PASSIVE: SILENT FIELD, 1st card unseen.", "PASİF: SESSİZ TARLA, ilk kart gizli.");
                case Warlock.ClassId: return L("PASSIVE: SEES THE TRUTH, lies, traps.", "PASİF: GERÇEĞİ GÖRÜR, yalan, tuzak.");
                case King.ClassId: return string.Format(L("PASSIVE: ROYAL DISCOUNT, seize for {0}.", "PASİF: KRALİYET İNDİRİMİ, müsadere {0}."),
                    (kit ?? KitOf(id)).PriceOf(TaxAct.Seize));
                case Summoner.ClassId: return L("PASSIVE: LOYAL IMP, 1st imp free.", "PASİF: SADIK İBLİS, ilki bedava.");
                case Thief.ClassId: return L("PASSIVE: GLANCE, a closed card shows.", "PASİF: GÖZ UCU, bir kartı açık.");
                default: return null;
            }
        }

        /// <summary>A class's kit at the starting level (no relics, no upgrades), for the words of a run not begun.</summary>
        private static ClassKit KitOf(string id)
        {
            SinnerClass cls = SinnerRoster.Find(id);
            return cls == null ? null : new ClassKit(new Sinner(cls));
        }

        /// <summary>K at a chapter's table for every class but the Thief (Demo 2.3): what the class does, nothing to press.</summary>
        public static string ClassPassiveInfo(string id, ClassKit kit) =>
            L("Your powers work on their own. ", "Güçlerin kendiliğinden çalışır. ") + (ChapterPassiveLine(id, kit) ?? ChapterMainPower(id, kit) ?? "");

        /// <summary>The price of the class in Phase 2's chapters.</summary>
        public static string ChapterSinnerDetail(string id)
        {
            switch (id)
            {
                case Peasant.ClassId: return L("2nd card: he raises more.", "2. kart: çok artırır.");
                case Warlock.ClassId: return L("MIRROR CURSE: he knows a card.", "AYNA LANETİ: bir kartını bilir.");
                case King.ClassId: return L("Boss loss: half the taxes.", "Boss kaybı: vergi yarıya.");
                case Thief.ClassId: return L("Caught: no swap, a fine.", "Yakalanırsan: takas yok, ceza.");
                case Summoner.ClassId: return L("Boss: bar. 1 in 10 runs wild.", "Boss'ta bar. 10'da 1 azar.");
                default: return SinnerDetail(id);
            }
        }

        /// <summary>How to Play's line about the classes' growth (Demo 2.3).</summary>
        public static string ClassGrowthLine => L("A class starts weak; its class relics and upgrades grow its power.",
            "Sınıf zayıf başlar; sınıf emanetleri ve yükseltmeler gücünü büyütür.");

        /// <summary>A class relic worn (Demo 2.3): what the main power is now ("FORESIGHT now sees 2 cards."); null for a class that does not grow.</summary>
        public static string ClassPowerNow(ClassKit kit)
        {
            if (kit == null || !kit.Grows) return null;
            int n = kit.PowerValue;
            switch (kit.Kind)
            {
                case KitKind.Satchel: return string.Format(L("The SATCHEL now holds {0} cards.", "HEYBE artık {0} kart taşır."), n);
                case KitKind.Prophecy: return string.Format(L("FORESIGHT now sees {0} cards.", "ÖNSEZİ artık {0} kart görür."), n);
                case KitKind.Treasury: return string.Format(L("The TREASURY now holds {0} taxes.", "HAZİNE artık {0} vergi tutar."), n);
                case KitKind.Theft: return string.Format(L("STEAL: now caught {0}% of the time.", "ÇAL: artık %{0} yakalanırsın."), n);
                case KitKind.Summon:
                    return kit.ImpsHalfPrice ? L("Every imp now costs half.", "Artık her iblis yarı fiyat.")
                        : string.Format(L("Imps open now: {0} of 3.", "Artık açık iblis: {0}/3."), n);
                default: return string.Format(L("Shields now: {0} a table.", "Kalkan artık: masada {0}."), n);
            }
        }

        /// <summary>The map's short line of the main power, now and at most ("FORESIGHT 2/4").</summary>
        public static string ClassPowerShort(ClassKit kit)
        {
            int n = kit.PowerValue, most = kit.PowerMax;
            switch (kit.Kind)
            {
                case KitKind.Satchel: return string.Format(L("SATCHEL {0}/{1}", "HEYBE {0}/{1}"), n, most);
                case KitKind.Prophecy: return string.Format(L("FORESIGHT {0}/{1}", "ÖNSEZİ {0}/{1}"), n, most);
                case KitKind.Treasury: return string.Format(L("TREASURY {0}/{1}", "HAZİNE {0}/{1}"), n, most);
                case KitKind.Theft: return string.Format(L("CAUGHT {0}% (best {1}%)", "YAKALANMA %{0} (en iyi %{1})"), n, most);
                case KitKind.Summon: return string.Format(L("IMPS {0}/3{1}", "İBLİS {0}/3{1}"), n, kit.ImpsHalfPrice ? L(", half price", ", yarı fiyat") : "");
                default: return string.Format(L("SHIELDS {0}/{1}", "KALKAN {0}/{1}"), n, most);
            }
        }

        public static string ChapterRulesButton => L("POWERS", "GÜÇLER");

        /// <summary>How to Play's POWERS page (the simulator keeps its own pages): every class's main power and passive, the growth, the
        /// demons' rules. Every number from the code.</summary>
        public static string ChapterRulesPage()
        {
            string Class(string id) => SinnerName(id) + ": " + ChapterMainPower(id) + (ChapterPassiveLine(id) == null ? "" : " " + ChapterPassiveLine(id));
            string classes = string.Join("\n", SinnerRoster.PhaseTwo.Select(c => Class(c.Id)));
            string rules = string.Format(L(
                "THE DEMONS' RULES (their own tables): MAMMON, INTEREST: every hand you do not win, the bar grows {0}%. BELIAL, ROLE SWAP: " +
                "every 4th hand one of his face-down cards trades places with one of yours. LILITH, TEMPTATION: a card you throw that betters " +
                "her hand, she may keep ({1}%). LUCIFER, MIRROR: he copies your class. Under {2}% of the bar a demon cheats faster.",
                "ŞEYTANLARIN KURALLARI (kendi masalarında): MAMMON, FAİZ: kazanamadığın her elde bar %{0} uzar. BELIAL, ROL TAKASI: her " +
                "4. elde kapalı kartlarından biri seninkiyle yer değiştirir. LILITH, BAŞTAN ÇIKARMA: attığın kart elini iyileştiriyorsa " +
                "alabilir (%{1}). LUCIFER, AYNA: sınıfını kopyalar. Bar %{2} altına inince şeytan daha hızlı hile yapar."),
                MammonInterest.InterestPercent, LilithTemptation.TakePercent, DemonRule.CorneredPercent);
            return L("POWERS AND RULES. Every power works on its own (K: the Thief's STEAL).",
                       "GÜÇLER VE KURALLAR. Her güç kendiliğinden çalışır (K: Hırsız'ın ÇAL'ı).") + "\n" + classes + "\n" + ClassGrowthLine + "\n\n" + rules;
        }

        // ------------------------------------------------------------------ the panel under the dialog box

        /// <summary>{0} cards in the satchel, {1} its size.</summary>
        public static string KitSatchelTitleFormat => L("SATCHEL {0}/{1}", "HEYBE {0}/{1}");
        public static string KitKeepTitle => L("KEEP ONE?", "BİRİNİ SAKLA?");
        public static string KitProphecyTitle => L("FORESIGHT", "ÖNSEZİ");

        /// <summary>{0} the taxes, {1} the most.</summary>
        public static string KitTreasuryTitleFormat => L("TAXES {0}/{1}", "VERGİ {0}/{1}");

        /// <summary>The treasury's buttons: {0} the price.</summary>
        public static string KitRevealButtonFormat => L("OPEN {0}", "AÇ {0}");
        public static string KitFreeRaiseButtonFormat => L("FREE {0}", "BEDAVA {0}");
        public static string KitSeizeButtonFormat => L("SEIZE {0}", "MÜSADERE {0}");

        /// <summary>The satchel's hover: {0} its cards now, {1} at most, {2} the risk of a second card.</summary>
        public static string KitSatchelHintFormat => L(
            "In the draw, click a satchel card: it fills a thrown place. SILENT FIELD: the hand's first one goes unseen; a second one the demon " +
            "sees (his re-raise {2}% more often). After the draw, click a thrown card to keep it. The satchel: {0} cards (at most {1}).",
            "Draw'da bir heybe kartına tıkla: atılan bir yerin yerine gelir. SESSİZ TARLA: elin ilk kartını şeytan görmez; ikincisini görür " +
            "(artırması %{2} daha sık). Draw'dan sonra atılan bir karta tıklayıp sakla. Heybe: {0} kart (en çok {1}).");

        public static string KitSatchelHint(ClassKit kit)
        {
            ClassKit k = kit ?? KitOf(Peasant.ClassId);
            return string.Format(KitSatchelHintFormat, k.SatchelCapacity, k.PowerMax, k.SatchelRisk);
        }

        /// <summary>FORESIGHT's hover: {0} its cards now, {1} at most.</summary>
        public static string KitProphecyHintFormat => L(
            "FORESIGHT, once the cards are dealt: the {0} cards your draw will take, the first on the left (at most {1}; shut between hands " +
            "and after the draw). You see the demons' lies and the swindlers' traps. MIRROR CURSE: the demon knows one of your cards each hand.",
            "ÖNSEZİ, kartlar dağıtılınca: draw'da gelecek {0} kart, ilki solda (en çok {1}; eller arasında ve draw'dan sonra kapalı). " +
            "Şeytanın yalanlarını ve dolandırıcıların tuzaklarını görürsün. AYNA LANETİ: şeytan her elde kartlarından birini bilir.");

        public static string KitProphecyHint(ClassKit kit)
        {
            ClassKit k = kit ?? KitOf(Warlock.ClassId);
            return string.Format(KitProphecyHintFormat, k.ProphecyCards, k.PowerMax);
        }

        public static string KitProphecyAfterDeal => L("Shows after the deal.", "Dağıtımdan sonra görünür.");
        public static string KitProphecyNextDeal => L("Shut until the next deal.", "Sonraki dağıtıma kadar kapalı.");

        /// <summary>The Fate Weaver: {0} the card the demon knows.</summary>
        public static string DemonKnowsFormat => L("KNOWS {0}", "BİLİYOR {0}");

        /// <summary>The treasury's hover: {0} the taxes' most now, {1} at most ever, {2} open, {3} free raise, {4} seize (discounted), {5} its full price.</summary>
        public static string KitTreasuryHintFormat => L(
            "A tax for every hand you win (at most {0}, grows to {1}); a demon's hand lost takes half. Before the draw, once each: OPEN ({2}) " +
            "turns a card of the demon's, FREE ({3}) makes your next raise his money, SEIZE ({4}) swaps a card of yours for his best open one. " +
            "ROYAL DISCOUNT: the seizure at half ({4}, not {5}). No cheat touches your J, Q or K.",
            "Kazandığın her elde bir vergi (en çok {0}, {1} olur); kaybedilen boss eli yarısını alır. Draw'dan önce, her biri bir kez: AÇ ({2}) " +
            "şeytanın bir kartını çevirir, BEDAVA ({3}) sıradaki artırmanı onun parasıyla yapar, MÜSADERE ({4}) bir kartını onun en iyi " +
            "açık kartıyla değiştirir. KRALİYET İNDİRİMİ: müsadere yarı fiyata ({5} değil {4}). J, Q ve K'na hiçbir hile dokunamaz.");

        public static string KitTreasuryHint(ClassKit kit)
        {
            ClassKit k = kit ?? KitOf(King.ClassId);
            return string.Format(KitTreasuryHintFormat, k.TaxCapacity, k.PowerMax, k.PriceOf(TaxAct.Reveal), k.PriceOf(TaxAct.FreeRaise),
                k.PriceOf(TaxAct.Seize), k.SeizeFullPrice);
        }

        // ------------------------------------------------------------------ what the acts say

        /// <summary>{0} satchel cards picked for the draw.</summary>
        public static string SatchelTakeFormat => L("From the satchel in the draw: {0} card, unseen.", "Draw'da heybeden: {0} kart, kimse görmez.");

        /// <summary>{0} satchel cards picked for the draw (two or more: the demon sees the second).</summary>
        public static string SatchelTakeSeenFormat => L("From the satchel in the draw: {0} cards. The demon sees the second.",
            "Draw'da heybeden: {0} kart. Şeytan ikincisini görür.");
        public static string SatchelOnlyInDraw => L("The satchel opens in the draw.", "Heybe draw'da açılır.");
        public static string SatchelTooMany => L("More satchel cards than thrown places: throw more, or put one back.",
            "Atılan yerden fazla heybe kartı: daha çok at ya da birini geri koy.");

        /// <summary>{0} the card kept.</summary>
        public static string SatchelKeptFormat => L("{0} goes into the satchel.", "{0} heybeye girdi.");

        /// <summary>The Prophecy Seal paid for a swindler walked away from: {0} the coins (signed).</summary>
        public static string ProphecySealFormat => L("Prophecy Seal: you saw the swindle and walked away. {0} coins.",
            "Kehanet Mührü: dolandırıcılığı görüp yürüdün. {0} coin.");

        public static string SeizePrompt => L("Seize: click the card you give.", "Müsadere: vereceğin karta tıkla.");

        /// <summary>{0} the card taken, {1} the card given.</summary>
        public static string SeizedFormat => L("Seized: {0}, for your {1}.", "Müsadere: {1} karşılığında {0} senin.");

        /// <summary>The King's passive at work: {0} the taxes paid, {1} the full price.</summary>
        public static string RoyalDiscountLineFormat => L("Royal discount: {0} taxes, not {1}.", "Kraliyet indirimi: {1} değil {0} vergi.");
        public static string RevealedMessage => L("One of the demon's cards turns.", "Şeytanın bir kartı açıldı.");
        public static string FreeRaiseMessage => L("Your next raise this hand is the demon's money.", "Bu eldeki sıradaki artırman şeytanın parasıyla.");
        public static string TaxesHalved => L("A demon's hand lost: half your taxes are gone.", "Boss eli kaybedildi: vergilerinin yarısı gitti.");

        public static string KitNotNow => L("Not now.", "Şimdi olmaz.");
        public static string KitNotBeforeDraw => L("Only before the draw.", "Sadece draw'dan önce.");
        public static string StealNotNow => L("STEAL: from the deal until the showdown.", "ÇAL: dağıtımdan showdown'a kadar.");
        public static string KitAlreadyUsed => L("Already done this hand.", "Bu el zaten yapıldı.");
        public static string KitNoCard => L("No card for it.", "Bunun için kart yok.");
        public static string KitCannotRaise => L("No raise can be made this hand.", "Bu el artırma yapılamaz.");

        /// <summary>{0} the taxes, {1} the price.</summary>
        public static string KitNoTaxesFormat => L("Not enough taxes: {0} of {1}.", "Vergi yetmiyor: {0} / {1}.");

        public static string KitRefused(KitRefusal refusal, int taxes = 0, int price = 0)
        {
            switch (refusal)
            {
                case KitRefusal.NoTaxes: return string.Format(KitNoTaxesFormat, taxes, price);
                case KitRefusal.AlreadyUsed: return KitAlreadyUsed;
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
                    return MirrorLine(mirror.Mirrors);
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
        public static string CorneredLine => L("Cornered. Now I cheat faster.", "Köşeye sıkıştım. Artık daha hızlı hile yaparım.");

        /// <summary>The Jester's shield spared a loss: {0} the shields left at this table.</summary>
        public static string JesterShieldFormat => L("A shield: your two jokers' loss is a push ({0} left at this table).",
            "Kalkan: iki jokerinin kaybı berabere sayıldı (bu masada {0} kaldı).");

        /// <summary>The chapter's opening words: {0} the purse it was topped up to.</summary>
        public static string PurseToppedUpFormat => L("Your purse was filled up to {0} coins.", "Kesen {0} coin'e tamamlandı.");
    }
}
