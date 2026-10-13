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
                case ChapterRelicIds.BloodyDie: return L("BLOODY DIE", "KANLI ZAR");
                case ChapterRelicIds.LeadPurse: return L("LEAD PURSE", "KURŞUN KESE");
                case ChapterRelicIds.AshLedger: return L("ASH LEDGER", "KÜL DEFTERİ");
                case ChapterRelicIds.CandleStub: return L("CANDLE STUB", "MUM DİBİ");
                case ChapterRelicIds.RatTooth: return L("RAT'S TOOTH", "FARE DİŞİ");
                case ChapterRelicIds.IronVow: return L("IRON VOW", "DEMİR YEMİN");
                case ChapterRelicIds.PilgrimsLantern: return L("PILGRIM'S LANTERN", "HACI FENERİ");
                case ChapterRelicIds.RavenFeather: return L("RAVEN FEATHER", "KUZGUN TÜYÜ");
                case ChapterRelicIds.BloodRing: return L("BLOOD RING", "KANLI YÜZÜK");
                case ChapterRelicIds.OldMap: return L("OLD MAP", "ESKİ HARİTA");
                case ChapterRelicIds.SilverBell: return L("SILVER BELL", "GÜMÜŞ ÇAN");
                case ChapterRelicIds.SaltCircle: return L("SALT CIRCLE", "TUZ ÇEMBERİ");
                case ChapterRelicIds.BlackBook: return L("BLACK BOOK", "KARA KİTAP");
                case ChapterRelicIds.HangmansRope: return L("HANGMAN'S ROPE", "CELLAT İPİ");
                case ChapterRelicIds.LilithsTear: return L("LILITH'S TEAR", "LİLİTH'İN GÖZYAŞI");
                case ChapterRelicIds.AbyssCoin: return L("ABYSS COIN", "UÇURUM SİKKESİ");
                case ChapterRelicIds.MammonsSeal: return L("MAMMON'S SEAL", "MAMMON'UN MÜHRÜ");
                case ChapterRelicIds.MammonsScales: return L("MAMMON'S SCALES", "MAMMON'UN TERAZİSİ");
                case ChapterRelicIds.BelialsMask: return L("BELIAL'S MASK", "BELIAL'IN MASKESİ");
                case ChapterRelicIds.BelialsTongue: return L("BELIAL'S TONGUE", "BELIAL'IN DİLİ");
                case ChapterRelicIds.AshPurse: return L("ASH PURSE", "KÜL KESESİ");
                case ChapterRelicIds.FerrymansLantern: return L("FERRYMAN'S LANTERN", "KAYIKÇI'NIN FENERİ");
                case ChapterRelicIds.RustedChain: return L("RUSTED CHAIN", "PASLI ZİNCİR");
                case ChapterRelicIds.LeadSeal: return L("LEAD SEAL", "KURŞUN MÜHÜR");
                case ChapterRelicIds.LiarsTongue: return L("LIAR'S TONGUE", "YALANCININ DİLİ");
                case ChapterRelicIds.BloodPact: return L("BLOOD PACT", "KAN SÖZLEŞMESİ");
                case ChapterRelicIds.LostSoulsLetter: return L("LOST SOUL'S LETTER", "KAYIP RUHUN MEKTUBU");
                case ChapterRelicIds.TwinDice: return L("TWIN DICE", "ÇİFT ZAR");
                case ChapterRelicIds.JestersHat: return L("JESTER'S HAT", "SOYTARI'NIN ŞAPKASI");
                case ChapterRelicIds.SeersSpectacles: return L("SEER'S SPECTACLES", "KÂHİNİN GÖZLÜĞÜ");
                case ChapterRelicIds.PeasantsScythe: return L("PEASANT'S SCYTHE", "KÖYLÜNÜN TIRPANI");
                case ChapterRelicIds.KingsSignet: return L("KING'S SIGNET", "KRALIN MÜHÜR YÜZÜĞÜ");
                case ChapterRelicIds.SummonersLeash: return L("SUMMONER'S LEASH", "ÇAĞIRICININ TASMASI");
                case ChapterRelicIds.LucifersFeather: return L("LUCIFER'S FEATHER", "LUCIFER'İN TÜYÜ");
                case ChapterRelicIds.MammonsGoldTooth: return L("MAMMON'S GOLD TOOTH", "MAMMON'UN ALTIN DİŞİ");
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
                case ChapterRelicIds.BloodyDie: return L("A won hand: a quarter of an ante more for you.", "Kazanılan el: ante'nin çeyreği kadar fazlası senin.");
                case ChapterRelicIds.LeadPurse: return L("Treasures give half again.", "Hazineler yarısı kadar fazla verir.");
                case ChapterRelicIds.AshLedger: return L("The gate's tribute is a tenth less.", "Kapının haracı onda bir az.");
                case ChapterRelicIds.CandleStub: return L("The house shows one card more.", "Karşı taraf bir kart fazla gösterir.");
                case ChapterRelicIds.RatTooth: return L("The black market asks 15% less.", "Kara Pazar %15 az ister.");
                case ChapterRelicIds.IronVow: return L("The soul burns slower when you lose (×1.1).", "Kaybedince ruhun daha yavaş yanar (×1,1).");
                case ChapterRelicIds.PilgrimsLantern: return L("The house shows one card more.", "Karşı taraf bir kart fazla gösterir.");
                case ChapterRelicIds.RavenFeather: return L("A demon's bar starts 8% shorter.", "Şeytanın barı %8 kısa başlar.");
                case ChapterRelicIds.BloodRing: return L("A win is a tenth more.", "Kazanç onda bir fazla.");
                case ChapterRelicIds.OldMap: return L("The black market asks a quarter less.", "Kara Pazar dörtte bir az ister.");
                case ChapterRelicIds.SilverBell: return L("Once at each demon's table, before the draw, redraw a card.", "Her şeytanın masasında bir kez, değişten önce bir kartı yeniden çek.");
                case ChapterRelicIds.SaltCircle: return L("Treasures give double.", "Hazineler iki kat verir.");
                case ChapterRelicIds.BlackBook: return L("A win is a quarter more.", "Kazanç dörtte bir fazla.");
                case ChapterRelicIds.HangmansRope: return L("The ante is three quarters.", "Ante dörtte üç.");
                case ChapterRelicIds.LilithsTear: return L("With your soul on the table a loss burns no faster.", "Ruhun masadayken kayıp onu daha hızlı yakmaz.");
                case ChapterRelicIds.AbyssCoin: return L("A win is 40% more.", "Kazanç %40 fazla.");
                case ChapterRelicIds.MammonsSeal: return L("The gate's tribute is a quarter less.", "Kapının haracı dörtte bir az.");
                case ChapterRelicIds.MammonsScales: return L("A won hand: half an ante more for you.", "Kazanılan el: ante'nin yarısı kadar fazlası senin.");
                case ChapterRelicIds.BelialsMask: return L("The house shows one card more.", "Karşı taraf bir kart fazla gösterir.");
                case ChapterRelicIds.BelialsTongue: return L("Once at each demon's table, before the draw, redraw a card.", "Her şeytanın masasında bir kez, değişten önce bir kartı yeniden çek.");
                case ChapterRelicIds.AshPurse: return L("A floor hand won: +2 coins.", "Kazanılan kat eli: +2 coin.");
                case ChapterRelicIds.FerrymansLantern: return L("The map's unknown nodes show what they hold.", "Haritadaki bilinmeyen düğümlerin içi görünür.");
                case ChapterRelicIds.RustedChain: return L("A camp's rest is half again as good.", "Kampta dinlenmek yarısı kadar daha etkili.");
                case ChapterRelicIds.LeadSeal: return L("At every demon's table the first lost hand counts as a push.", "Her şeytan masasında ilk kaybedilen el berabere sayılır.");
                case ChapterRelicIds.LiarsTongue: return L("A swindler's trap reads plainly.", "Dolandırıcının tuzağı açıkça okunur.");
                case ChapterRelicIds.BloodPact: return L("A lost hand heals a demon's bar half as much.", "Kaybedilen el şeytanın barını yarı yarıya az iyileştirir.");
                case ChapterRelicIds.LostSoulsLetter: return L("A helper's good turn is a fifth better.", "Yardımcıların iyiliği beşte bir fazla.");
                case ChapterRelicIds.TwinDice: return L("Once at each table, before the draw, redraw a card.", "Her masada bir kez, değişten önce bir kartı yeniden çek.");
                case ChapterRelicIds.JestersHat: return L("A hand won with a joker in it: +25%.", "Jokerle kazanılan el: +%25.");
                case ChapterRelicIds.SeersSpectacles: return L("The prophecy sees one card more.", "KEHANET bir kart fazla görür.");
                case ChapterRelicIds.PeasantsScythe: return L("The satchel holds one card more.", "HEYBE bir kart fazla taşır.");
                case ChapterRelicIds.KingsSignet: return L("The treasury holds five taxes more.", "Hazine beş vergi fazla tutar.");
                case ChapterRelicIds.SummonersLeash: return L("An imp runs wild half as often.", "İblis yarı yarıya daha az azar.");
                case ChapterRelicIds.LucifersFeather: return L("Once a run, a lost hand at a demon's table counts as a push.", "Koşuda bir kez, şeytan masasında kaybedilecek el berabere sayılır.");
                case ChapterRelicIds.MammonsGoldTooth: return L("Every 50 coins in the purse: a demon's win +3% (15% at most).", "Kesedeki her 50 coin: şeytana karşı kazanç +%3 (en çok %15).");
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
                case ChapterRelicIds.BloodyDie: return L("A lost hand: a fifth of an ante more against you.", "Kaybedilen el: ante'nin beşte biri kadar fazlası aleyhine.");
                case ChapterRelicIds.LeadPurse: return L("The black market asks a fifth more.", "Kara Pazar beşte bir fazla ister.");
                case ChapterRelicIds.AshLedger: return L("A win is a twentieth less.", "Kazanç yirmide bir eksik.");
                case ChapterRelicIds.CandleStub: return L("The ante is 15% higher.", "Ante %15 yüksek.");
                case ChapterRelicIds.RatTooth: return L("A lost hand: 15% of an ante more against you.", "Kaybedilen el: ante'nin yüzde 15'i kadar fazlası aleyhine.");
                case ChapterRelicIds.IronVow: return L("The ante is 15% higher.", "Ante %15 yüksek.");
                case ChapterRelicIds.PilgrimsLantern: return L("The House re-raises two units.", "Kasa iki birim artırır.");
                case ChapterRelicIds.RavenFeather: return L("The demon's malice grows faster.", "Şeytanın kötülüğü daha hızlı dolar.");
                case ChapterRelicIds.BloodRing: return L("The demon's malice grows faster.", "Şeytanın kötülüğü daha hızlı dolar.");
                case ChapterRelicIds.OldMap: return L("The gate's tribute is a tenth more.", "Kapının haracı onda bir fazla.");
                case ChapterRelicIds.SilverBell: return L("The ante is a tenth higher.", "Ante onda bir yüksek.");
                case ChapterRelicIds.SaltCircle: return L("The imps' purses are 15% fuller.", "İblislerin keseleri %15 daha dolu.");
                case ChapterRelicIds.BlackBook: return L("The demon's malice grows much faster.", "Şeytanın kötülüğü çok daha hızlı dolar.");
                case ChapterRelicIds.HangmansRope: return L("The soul burns faster when you lose (×1.75).", "Kaybedince ruhun daha hızlı yanar (×1,75).");
                case ChapterRelicIds.LilithsTear: return L("A win is a tenth less.", "Kazanç onda bir eksik.");
                case ChapterRelicIds.AbyssCoin: return L("A lost hand: a whole ante more against you.", "Kaybedilen el: bir ante fazlası aleyhine.");
                case ChapterRelicIds.MammonsSeal: return L("The demon's malice grows faster.", "Şeytanın kötülüğü daha hızlı dolar.");
                case ChapterRelicIds.MammonsScales: return L("The ante is a fifth higher.", "Ante beşte bir yüksek.");
                case ChapterRelicIds.BelialsMask: return L("A win is a twentieth less.", "Kazanç yirmide bir eksik.");
                case ChapterRelicIds.BelialsTongue: return L("A demon's bar starts 5% longer.", "Şeytanın barı %5 uzun başlar.");
                case ChapterRelicIds.AshPurse: return L("A floor table's first ante is one more.", "Kat masasında ilk ante bir fazla.");
                case ChapterRelicIds.FerrymansLantern: return L("Market prices +10%.", "Pazarda fiyatlar +%10.");
                case ChapterRelicIds.RustedChain: return L("No forging at a camp.", "Kampta DÖVME seçilemez.");
                case ChapterRelicIds.LeadSeal: return L("Wins at that table are 10% less.", "O masada kazançlar %10 az.");
                case ChapterRelicIds.LiarsTongue: return L("A helper's good turn is 15% less.", "Yardımcıların iyiliği %15 az.");
                case ChapterRelicIds.BloodPact: return L("A lost hand puts a quarter more on your sentence.", "Kaybedilen el cezana dörtte bir fazla yazılır.");
                case ChapterRelicIds.LostSoulsLetter: return L("Every offer taken: +20 years on your sentence.", "Kabul edilen her teklif: cezana +20 yıl.");
                case ChapterRelicIds.TwinDice: return L("The demon's malice grows one more every hand.", "Şeytanın kötülüğü her el bir fazla dolar.");
                case ChapterRelicIds.JestersHat: return L("A hand lost to two jokers: +25%.", "İki jokerle kaybedilen el: +%25.");
                case ChapterRelicIds.SeersSpectacles: return L("Seen cards can no longer go to the bottom.", "Görülen kartlar artık dibe yollanamaz.");
                case ChapterRelicIds.PeasantsScythe: return L("A satchel hand: the demon re-raises 5% more often.", "Heybe eli: şeytan %5 daha sık artırır.");
                case ChapterRelicIds.KingsSignet: return L("A seizure costs one tax more.", "MÜSADERE bir vergi fazla.");
                case ChapterRelicIds.SummonersLeash: return L("Imps cost 2 coins more (a demon's table: 1% more).", "İblis bedeli 2 coin fazla (şeytan masasında %1).");
                case ChapterRelicIds.LucifersFeather: return L("As you take it: your sentence 10% longer.", "Aldığın an: cezan %10 uzar.");
                case ChapterRelicIds.MammonsGoldTooth: return L("The gate's tribute +25%.", "Kapıda haraç +%25.");
                default: return "";
            }
        }

        /// <summary>A Phase 2 relic's rarity, as its tag.</summary>
        public static string RarityName(RelicRarity rarity)
        {
            switch (rarity)
            {
                case RelicRarity.Rare: return L("RARE", "NADİR");
                case RelicRarity.Legendary: return L("LEGENDARY", "EFSANEVİ");
                case RelicRarity.Boss: return L("DEMON'S OWN", "ŞEYTANIN KENDİ");
                case RelicRarity.Reward: return L("EARNED", "KAZANILMIŞ");
                default: return L("COMMON", "YAYGIN");
            }
        }

        /// <summary>A relic's line on a panel: {0} its rarity, {1} the gift, {2} the curse.</summary>
        public static string RelicPanelDetailFormat => L("{0} · + {1}   − {2}", "{0} · + {1}   − {2}");

        public static string RelicPanelDetail(string id) =>
            string.Format(RelicPanelDetailFormat, RarityName(ChapterRelics.RarityOf(id)), RelicGift(id), RelicCurse(id));

        /// <summary>The relic's hover box: {0} gift, {1} curse.</summary>
        public static string RelicDescriptionFormat => L("+ {0}\n− {1}", "+ {0}\n− {1}");

        /// <summary>{0} the relic, {1} the gift, {2} the curse.</summary>
        public static string RelicTakenFormat => L("You carry the {0} now. {1} But: {2}", "Artık {0} sende. {1} Ama: {2}");

        public static string RedrawPrompt => L("Pick the card the Bone Die throws back (click the die again: cancel).", "Kemik Zar'ın geri atacağı kartı seç (zara yine tıkla: vazgeç).");
        public static string RedrawNotNow => L("The Bone Die rolls once at each demon's table (full again at every summons to Lucifer), before the draw, on a card you can see.",
            "Kemik Zar her şeytanın masasında bir kez (Lucifer'e her çağrılmada dolar), değişten önce, gördüğün bir karta atılır.");
        /// <summary>{0}: the relic (Phase 2's bell, tongue).</summary>
        public static string RedrawPromptFormat => L("{0}: pick the card it throws back (click it again: cancel).", "{0}: geri atacağı kartı seç (yine tıkla: vazgeç).");

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
