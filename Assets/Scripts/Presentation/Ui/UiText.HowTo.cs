using HellPoker.Core.Chapters;
using HellPoker.Core.Relics;

namespace HellPoker.Presentation.Ui
{
    /// <summary>
    /// Demo 2.2's How to Play: ten short pages (Demo 3.4: eleven, the difficulty's before the hands), each a picture from the game (Art/HowTo/&lt;page&gt;_&lt;lang&gt;.png, made by the PlayMode
    /// HowToShots) beside at most five short lines; the tenth is the hands table.
    /// </summary>
    internal static partial class UiText
    {
        /// <summary>How many pages the game's How to Play has (the last: the hands table).</summary>
        public const int HowToPages = 11;

        /// <summary>The picture's file name (without the language): "01_goal" ...</summary>
        public static string HowToImage(int page)
        {
            string[] names = { "goal", "map", "floor", "hand", "boss", "cheats", "classes", "relics", "markets", "difficulty", "hands" };
            return $"{page + 1:00}_{names[page]}";
        }

        public static string HowToTitle(int page)
        {
            switch (page)
            {
                case 0: return L("THE GOAL", "AMAÇ");
                case 1: return L("THE MAP", "HARİTA");
                case 2: return L("A FLOOR'S TABLE", "KAT MASASI");
                case 3: return L("A HAND", "BİR EL");
                case 4: return L("THE DEMON'S TABLE", "ŞEYTANIN MASASI");
                case 5: return L("CHEATS AND RULES", "HİLELER VE KURALLAR");
                case 6: return L("SINNERS AND POWERS", "GÜNAHKÂRLAR VE GÜÇLER");
                case 7: return L("RELICS AND CAMPS", "EMANETLER VE KAMP");
                case 8: return L("MARKETS AND STRANGERS", "PAZARLAR VE OLAYLAR");
                case 9: return L("DIFFICULTY AND SAVING", "ZORLUK VE KAYIT");
                default: return L("THE HANDS", "ELLER");
            }
        }

        /// <summary>The page's lines (at most five, short); none for the hands table.</summary>
        public static string[] HowToLines(int page)
        {
            switch (page)
            {
                case 0:
                    return new[]
                    {
                        L("You start with a sentence of thousands of years.", "Binlerce yıllık bir cezayla başlarsın."),
                        L("Three chapters: Mammon, Belial, Lilith — then Lucifer.", "Üç bölüm: Mammon, Belial, Lilith — sonra Lucifer."),
                        L("Each chapter: a map down, a gate, the demon's table.", "Her bölüm: aşağı inen bir harita, bir kapı, şeytanın masası."),
                        L("Empty a demon's bar to beat him. Beat Lucifer: you are free.", "Şeytanın barını bitir, onu yen. Lucifer'i yen: özgürsün."),
                        L("An empty purse or a burnt soul ends the run.", "Kese biterse ya da ruh yanarsa koşu biter.")
                    };
                case 1:
                    return new[]
                    {
                        L("Pick a node on the next floor: one step down at a time.", "Sonraki kattan bir düğüm seç: her seferinde bir kat."),
                        L("TABLE: an imp, for coins. WARDEN: a harder table, a relic.", "MASA: bir iblis, coin için. BEKÇİ: daha zor bir masa, emanet."),
                        L("?: what it holds shows when you step on it.", "?: ne olduğu üstüne basınca belli olur."),
                        L("TREASURE: coins. MARKET: relics. CAMP: rest and more.", "HAZİNE: coin. PAZAR: emanet. KAMP: dinlenme ve fazlası."),
                        L("No path has two markets or two camps close together.", "Hiçbir yolda iki pazar ya da iki kamp yan yana gelmez.")
                    };
                case 2:
                    return new[]
                    {
                        L("Floors are played for coins: the counter is your purse.", "Katlar coin için oynanır: sayaç senin kesen."),
                        L("The imp plays until his purse or yours is empty.", "İblis, onun kesesi ya da seninki bitene kadar oynar."),
                        L("The ante grows every few hands.", "Ante birkaç elde bir büyür."),
                        L("At the gate the demon asks his tribute in coins.", "Kapıda şeytan haracını coin olarak ister."),
                        L("Coins you lack become years on the sentence.", "Eksik coin cezana yıl olarak eklenir.")
                    };
                case 3:
                    return new[]
                    {
                        L("DEAL: the ante goes in and two of your cards turn.", "DAĞIT: ante masaya konur, iki kartın açılır."),
                        L("At each new card: RAISE, PASS or FOLD — D passes to the draw.", "Her yeni kartta: ARTIR, PAS ya da ÇEKİL — D draw'a kadar pas."),
                        L("DRAW: click the cards to throw, the deck replaces them.", "KART DEĞİŞ: atacağın kartlara tıkla, deste yenilerini verir."),
                        L("Then one more bet; the demon may raise back.", "Sonra bir bahis daha; şeytan karşı artırabilir."),
                        L("The better hand wins the stakes. H: the hands table.", "İyi el bahsi kazanır. H: el tablosu.")
                    };
                case 4:
                    return new[]
                    {
                        L("At a demon's table your share of the sentence is his BAR.", "Şeytanın masasında cezadaki payın onun BAR'ıdır."),
                        L("A win shortens the bar and your sentence alike.", "Kazanç barı da cezanı da aynı kadar kısaltır."),
                        string.Format(L("A loss: all of it on the sentence, {0}% back on the bar.", "Kayıp: tamamı cezaya, %{0} kadarı bara."), BossTable.BarHealPercent),
                        L("The stake is fixed; LAST BLOW: one more win ends him.", "Bahis sabit; SON DARBE: bir kazanç daha onu bitirir."),
                        L("Past his soul line your soul is on the table.", "Ruh çizgisini geçince ruhun masaya konur.")
                    };
                case 5:
                    return new[]
                    {
                        L("The pips under the demon fill hand by hand; full, he cheats.", "Şeytanın altındaki pipler el el dolar; dolunca hile yapar."),
                        L("The sign over him names the cheat coming (Belial may lie).", "Üstündeki işaret gelen hileyi söyler (Belial yalan söyleyebilir)."),
                        L("Each demon has a rule: interest, role swap, temptation.", "Her şeytanın bir kuralı var: faiz, rol takası, baştan çıkarma."),
                        L("Lucifer's rule is the MIRROR: he copies your class.", "Lucifer'in kuralı AYNA: sınıfını kopyalar."),
                        string.Format(L("Under {0}% of his bar a demon cheats faster.", "Barı %{0} altına inince şeytan daha hızlı hile yapar."), DemonRule.CorneredPercent)
                    };
                case 6:
                    return new[]
                    {
                        L("Six sinners: a power and a passive, both on their own; your panel (bottom left) shows them.",
                            "Altı günahkâr: bir güç ve bir pasif, ikisi de kendiliğinden; sol alttaki panelin gösterir."),
                        L("PEASANT: satchel, its first card unseen. ORACLE: foresight, sees lies.", "KÖYLÜ: heybe, ilk kartı görünmez. KÂHİN: önsezi, yalanı görür."),
                        L("KING: taxes, seizure at half. SUMMONER: imps, the first one free.", "KRAL: vergi, yarı fiyata müsadere. ÇAĞIRICI: iblis, ilki bedava."),
                        L("THIEF: a glance; K steals until the showdown. JESTER: jokers.", "HIRSIZ: göz ucu; K ile showdown'a kadar çalar. SOYTARI: joker."),
                        ClassGrowthLine
                    };
                case 7:
                    return new[]
                    {
                        L("A relic is a gift and a curse at once.", "Her emanet hem bir lütuf hem bir lanettir."),
                        string.Format(L("You wear {0} (on your panel: hover one) and carry {1} more in the bag.",
                            "{0} tanesini takarsın (panelinde: üstüne gel), {1} tanesi çantada durur."), ChapterRelics.Slots, ChapterRelics.BagSlots),
                        L("Wardens, markets and spoils give relics; your class's three come in order.",
                            "Bekçi, pazar ve ganimet emanet verir; sınıfının üç emaneti sırayla gelir."),
                        string.Format(L("A camp gives one act: {0}, {1}, {2} or {3}.", "Kamp bir eylem verir: {0}, {1}, {2} ya da {3}."), CampRest, CampWhet, CampRite, CampForge),
                        L("At a camp you also swap what you wear and what you carry.", "Kampta takılı emanetlerle çantadakileri de değiştirirsin.")
                    };
                case 8:
                    return new[]
                    {
                        L("Six kinds of sellers keep the markets, each once a chapter.", "Pazarları altı çeşit satıcı tutar, her biri bölümde bir kez."),
                        L("Haggle, or pay to refresh the stall.", "Pazarlık et ya da tezgâhı yenilemek için öde."),
                        L("Strangers on the map: some need help, some swindle.", "Haritada yabancılar: kimi yardım ister, kimi dolandırır."),
                        L("A swindler drops a hint; the Oracle sees the trap plainly.", "Dolandırıcı bir ipucu bırakır; Kâhin tuzağı açıkça görür."),
                        L("Each stranger comes once a run.", "Her yabancı koşuda bir kez gelir.")
                    };
                case 9:
                    return new[]
                    {
                        L("Choose the level under the class cards: Easy, Medium, Hard, Ironman.", "Seviyeyi sınıf kartlarının altında seç: Kolay, Orta, Zor, Ironman."),
                        L("Medium: faster ante, tribute from your purse, demons rage at half their bar.", "Orta: ante hızlı artar, haraç keseden, şeytanlar barın yarısında öfkelenir."),
                        L("Hard: the purse is topped up less, losses to demons hurt more.", "Zor: kese daha az dolar, şeytana kayıp daha çok acıtır."),
                        L("Ironman: faster cheats, stacked rules; saves only at the camps.", "Ironman: daha sık hile, üst üste kurallar; kayıt sadece kampta."),
                        L("SAVE on the map and its screens, never at a table.", "KAYDET haritada ve ekranlarında; masada asla.")
                    };
                default:
                    return new string[0];
            }
        }

        /// <summary>The pager under a page: {0} the page, {1} of how many.</summary>
        public static string HowToPagerFormat => L("{0} / {1}", "{0} / {1}");
    }
}
