using HellPoker.Core.Evaluation;

namespace HellPoker.Presentation.Ui
{
    /// <summary>
    /// All player-facing strings in one place, in every language the game speaks (<see cref="Lang"/>): each one is
    /// <c>Lang.Pick(English, Turkish)</c>. A new string is always added in both. Format strings keep the same {n} in both
    /// languages. Keys (tips, save fields, ids) are constants and are never translated.
    /// </summary>
    internal static partial class UiText
    {
        private static string L(string english, string turkish) => Lang.Pick(english, turkish);

        public static string Title => L("HELL POKER", "CEHENNEM POKERİ");
        public static string Subtitle => L("Five Card Draw against the House", "Kasaya karşı beş kartlı poker");
        public static string YearsLabel => L("YEARS LEFT IN HELL", "CEHENNEMDE KALAN YIL");
        public static string SoulLineFormat => L("Soul at stake at {0}", "Ruh {0} yılda masada");
        public static string StakeLabel => L("ANTE", "ANTE");
        public static string PayoutsTitle => L("PAYOUTS", "ÖDEMELER");
        /// <summary>Payout panel footer. {0} loss surcharge (see <see cref="LossSurcharge"/>), {1} fold before the draw, {2} after (see <see cref="ShareShort"/>).</summary>
        public static string PayoutLossFormat => L("× pays on the ante,\nraises pay 1 : 1.\nLose: House hand{0}\nFold: {1} / {2}",
            "× ante'ye işler,\nartırmalar 1 : 1.\nKayıp: kasanın eli{0}\nÇekil: {1} / {2}");

        /// <summary>Payout panel footer when folding costs the same before and after the draw.</summary>
        public static string PayoutLossSameFoldFormat => L("× pays on the ante,\nraises pay 1 : 1.\nLose: House hand{0}\nFold: always {1}",
            "× ante'ye işler,\nartırmalar 1 : 1.\nKayıp: kasanın eli{0}\nÇekil: her zaman {1}");

        /// <summary>A share of the stake in a word: "half", "all", "quarter", or a percentage.</summary>
        public static string ShareShort(int percent)
        {
            switch (percent)
            {
                case 25: return L("quarter", "çeyrek");
                case 50: return L("half", "yarı");
                case 100: return L("all", "hepsi");
                default: return Lang.IsTurkish ? "%" + percent : percent + "%";
            }
        }

        /// <summary>Nothing for a plain loss; " × 1.25" when the dealer charges more.</summary>
        public static string LossSurcharge(int lossPercent)
        {
            return lossPercent == 100 ? "" : " × " + (lossPercent / 100f).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static string AnteFormat => L("{0} YEARS", "{0} YIL");
        public static string StakeInfoFormat => L("Win: at least −{0} years   ·   Lose: at least +{1} years",
            "Kazanç: en az −{0} yıl   ·   Kayıp: en az +{1} yıl");
        public static string Absolution => L("FREE", "ÖZGÜR");
        public static string HouseCaption => L("THE HOUSE", "KASA");
        public static string PlayerCaption => L("YOUR HAND", "ELİN");
        public static string FoldedCaption => L("FOLDED", "ÇEKİLDİN");
        public static string ThornedCaption => L("THE THORN BIT", "DİKEN ISIRDI");
        public static string DiscardTag => L("TOSS", "AT");
        public static string Hint => L("Space deal/draw/pass  R raise  D check to draw  C call  F fold  1-5 cards  H hands  Esc menu",
            "Boşluk dağıt/değiş/pas  R artır  D hep pas  C gör  F çekil  1-5 kart  H eller  Esc menü");
        public static string PotFormat => L("ON THE TABLE: {0}", "MASADA: {0}");
        public static string FinalStretchBannerFormat => L("UNDER {0}: NO PASSING", "{0} ALTI: PAS YOK");
        public static string LastMomentsBanner => L("ONE HAND FROM FREEDOM", "ÖZGÜRLÜĞE BİR EL");

        /// <summary>Said by an ordinary demon when a win would end the sentence at their table: the last year stays.</summary>
        public static string LastYearLine => L("The last year is not mine to take. He is waiting.", "Son yılı almak bana düşmez. O bekliyor.");

        /// <summary>Under the counter at 1 year: why it did not reach 0.</summary>
        public static string LastYearNote => L("The last year is his", "Son yıl onun");
        public static string CastDownLineFormat => L("Cast down above {0}", "{0} üstünde düşersin");
        public static string AttemptLabelFormat => L("LUCIFER: ATTEMPT {0}", "LUCIFER: {0}. DENEME");

        public static string Menu => L("MENU", "MENÜ");
        public static string MenuTaglineFormat => L("You have been sentenced to {0} years in Hell.\nThe House offers you a game.",
            "Cehennemde {0} yıla mahkûm edildin.\nKasa sana bir oyun teklif ediyor.");
        public static string MenuSubtagline => L("Five Card Draw  ·  Wild Bill's Dead Man's Hand sets you free",
            "Beş kartlı poker  ·  Wild Bill'in Ölü Adamın Eli seni özgür bırakır");
        public static string NewGame => L("NEW GAME", "YENİ OYUN");
        public static string Continue => L("CONTINUE", "DEVAM ET");
        public static string HowToPlay => L("HOW TO PLAY", "NASIL OYNANIR");
        public static string Quit => L("QUIT", "ÇIKIŞ");
        public static string Back => L("BACK", "GERİ");
        public static string MenuFooter => L("Esc — menu", "Esc — menü");

        /// <summary>The build's version in the menu's corner ("v0.1.1").</summary>
        /// <summary>Under the title of a demo build (the same in both languages).</summary>
        public static string DemoLabel => L("DEMO", "DEMO");

        /// <summary>The development FPS readout (F3): average and slowest frame. Developer text: never translated.</summary>
        public const string FpsFormat = "FPS {0}  min {1}";
        public static string RulesTitle => L("THE RULES OF THE HOUSE", "KASANIN KURALLARI");

        /// <summary>{0} starting years, {1} (unused), {2} forced-raise threshold, {3} table cap percent, {4} Lucifer's gate,
        /// {5} where a fall from his table lands, {6} his ante, {7} his table cap.</summary>
        public static string RulesFormat => L(
            "You start with {0} years. Bring them down to 0 and you walk free — but only at the last table.\n" +
            "Every demon has a soul line. Reach it and your soul goes on the table — lose it all and you are damned for eternity.\n" +
            "You may change tables between hands — never while your soul is on one.\n\n" +
            "1.  The ante is a tenth of your sentence (1000 years: 100, 500: 50, 250: 25). Deal.\n" +
            "2.  Two cards turn, then one by one: RAISE, PASS or FOLD on cards 3, 4 and 5.\n" +
            "3.  Throw back cards and draw (the dealer says how many) — then one more decision. Raises now count double.\n" +
            "4.  The House shows some of its cards — a last decision — then the showdown.\n" +
            "    Raise after the draw and the House may raise back: CALL or FOLD.\n\n" +
            "Win: forgiven = the table + the ante × (your multiplier − 1).   Lose: the same on the House's hand.\n" +
            "At most {3}% of your sentence may be on the table. Fill it and the pact is sealed: no folding, the cards play out.\n" +
            "A♠ A♣ 8♠ 8♣ — the Dead Man's Hand — sets you free at once, wherever you sit.\n\n" +
            "THE MORNING STAR.  At {4} years or less, Lucifer summons you to his table, wherever you sit. Only there can a sentence end.\n" +
            "His stakes are his own: ante {6}, at most {7} on the table. He shows no cards. You cannot leave.\n" +
            "Climb back above {4} at his table and he casts you down: to the demon you came from, with at least {5} years.",
            "{0} yılla başlarsın. 0'a indir, özgür kal — ama ancak son masada.\n" +
            "Her şeytanın bir ruh çizgisi var. Oraya varınca ruhun masaya konur — hepsini kaybedersen sonsuza dek lanetlenirsin.\n" +
            "Eller arasında masa değiştirebilirsin — ruhun bir masadayken asla.\n\n" +
            "1.  Ante cezanın onda biri (1000 yıl: 100, 500: 50, 250: 25). Dağıt.\n" +
            "2.  İki kart açılır, sonra teker teker: 3., 4. ve 5. kartta ARTIR, PAS ya da ÇEKİL.\n" +
            "3.  Kart at ve yenisini çek (kaç tane, şeytan söyler) — sonra bir karar daha. Artırmalar artık çift sayılır.\n" +
            "4.  Kasa kartlarının bir kısmını açar — son karar — sonra eller açılır.\n" +
            "    Değişten sonra artırırsan kasa da artırabilir: GÖR ya da ÇEKİL.\n\n" +
            "Kazanç: silinen = masa + ante × (çarpanın − 1).   Kayıp: aynısı, kasanın eliyle.\n" +
            "Masaya en fazla cezanın %{3} kadarı konabilir. Dolunca anlaşma mühürlenir: çekilmek yok, kartlar kendi açılır.\n" +
            "A♠ A♣ 8♠ 8♣ — Ölü Adamın Eli — nerede oturursan otur seni anında özgür bırakır.\n\n" +
            "SABAH YILDIZI.  {4} yıl ya da altında Lucifer seni nerede olursan ol masasına çağırır. Ceza ancak orada biter.\n" +
            "Bahsi kendine özgü: ante {6}, masada en fazla {7}. Kart göstermez. Kalkamazsın.\n" +
            "Masasında {4} üstüne çıkarsan seni düşürür: geldiğin şeytana, en az {5} yılla.");

        public static string Deal => L("DEAL", "DAĞIT");
        public static string Stand => L("STAND PAT", "ELİ TUT");
        public static string DrawFormat => L("DRAW {0}", "KART DEĞİŞ {0}");
        public static string DrawThornFormat => L("DRAW {0}\n(+{1} YEARS)", "DEĞİŞ {0}\n(+{1} YIL)");
        public static string DrawThornSoulFormat => L("DRAW {0}\n(THORN BITES)", "DEĞİŞ {0}\n(DİKEN ISIRIR)");
        public static string ThornWarningFormat => L("A thorn! Throwing it back costs +{0} YEARS, at once.",
            "Diken! Onu atmak hemen +{0} YIL tutar.");
        public static string ThornWarningSoul => L("A thorn! Throwing it back costs a piece of your soul, at once.",
            "Diken! Onu atmak hemen ruhundan bir parça koparır.");
        public static string Next => L("NEXT HAND", "SONRAKİ EL");
        public static string TheEnd => L("THE END", "SON");
        public static string RaiseFormat => L("RAISE +{0}", "ARTIR +{0}");
        public static string AllInFormat => L("ALL IN +{0}", "HEPSİ +{0}");
        public static string AllInDone => L("ALL IN", "HEPSİ ORTADA");
        public static string TableFull => L("TABLE FULL", "MASA DOLU");
        public static string Pass => L("PASS", "PAS");
        public static string CheckToDrawButton => L("CHECK\nTO DRAW", "HEP\nPAS");
        public static string Fold => L("FOLD", "ÇEKİL");
        public static string CallFormat => L("CALL +{0}", "GÖR +{0}");

        public static string PromptBetFormat => L("The ante is {0} years. Deal when you dare.", "Ante {0} yıl. Cesaretin varsa dağıt.");
        public static string PromptPlayerCardFormat => L("Your card {0} of {1} turns.", "{1} kartından {0}. kartın açılıyor.");
        public static string PromptAfterDraw => L("Your hand is set. Raises count double now.", "Elin belli. Artırmalar artık çift sayılır.");
        public static string PromptHouseShowsFormat => L("The House shows {0} of its {1} cards.", "Kasa {1} kartının {0} tanesini açıyor.");
        public static string PromptReRaiseFormat => L("The House raises {0} years! Call — or fold?", "Kasa {0} yıl artırdı! Gör — ya da çekil?");
        public static string PromptChoice => L("  Raise, pass — or fold?", "  Artır, pas — ya da çekil?");
        public static string PromptForcedChoice => L("  No passing now: raise or fold.", "  Artık pas yok: artır ya da çekil.");
        public static string PromptDrawFormat => L("Pick up to {0} cards to throw back into the fire.", "Ateşe atmak için en fazla {0} kart seç.");
        public static string HouseDrewFormat => L("{0} — drew {1}", "{0} — {1} değişti");
        public static string WinFormat => L("{0} beats {1}.  {2} years forgiven.", "{0}, {1} elini yendi.  {2} yıl silindi.");
        public static string LossFormat => L("{0} beats your {1}.  +{2} years.", "{0}, senin {1} elini yendi.  +{2} yıl.");
        public static string PushFormat => L("Push — {0} against {1}. The sentence stands.", "Berabere — {0}, {1} karşısında. Ceza aynı.");
        public static string FoldFormat => L("You fold and slink away from the table.  +{0} years.", "Çekilip masadan sıvışıyorsun.  +{0} yıl.");
        public static string AbsolvedMessage => L("DEAD MAN'S HAND!  Wild Bill vouches for you. You walk free.",
            "ÖLÜ ADAMIN ELİ!  Wild Bill sana kefil. Özgürsün.");
        public static string ServedMessage => L("Your sentence is served. The gates of Hell open — you walk free.",
            "Cezan bitti. Cehennemin kapıları açılıyor — özgürsün.");
        public static string DamnedMessage => L("Your soul is ash. The House owns you for eternity.", "Ruhun kül oldu. Kasa artık sonsuza dek senin sahibin.");
        public static string MorningStarFallsMessage => L("His eyes go dark. The gates of Hell open — you walk free.",
            "Gözleri sönüyor. Cehennemin kapıları açılıyor — özgürsün.");

        // ------------------------------------------------------------------ the pact (table full: the hand plays out on its own)

        public static string PactSealed => L("THE PACT IS SEALED", "ANLAŞMA MÜHÜRLENDİ");
        public static string SealedMessage => L("The pact is sealed. No folding now — the cards decide.", "Anlaşma mühürlendi. Çekilmek yok — kartlar karar verecek.");
        public static string SealedDrawPrompt => L("The pact is sealed. Pick up to {0} cards to throw back — the rest plays out.",
            "Anlaşma mühürlendi. Atmak için en fazla {0} kart seç — gerisi kendi oynanır.");

        // ------------------------------------------------------------------ a hand left behind (the game was closed mid-hand)

        public static string FledFormat => L("You left mid-hand. It counts as a fold.  +{0} years.", "Elin ortasında kaçtın. Çekilmiş sayılır.  +{0} yıl.");
        public static string FledSealedFormat => L("You left a sealed hand. The whole wager is lost.  +{0} years.",
            "Mühürlü eli bıraktın. Bahsin tamamı gitti.  +{0} yıl.");
        public static string FledSoul => L("You left mid-hand. A piece of your soul stays on the table.", "Elin ortasında kaçtın. Ruhundan bir parça masada kaldı.");
        public static string FledSealedSoul => L("You left a sealed hand. The whole wager burns your soul.", "Mühürlü eli bıraktın. Bahsin tamamı ruhunu yakıyor.");
        public static string GrudgeMessage => L("You ran from a cheat. The demon holds a grudge: the next ones come sooner.",
            "Bir hileden kaçtın. Şeytan kin tutuyor: sıradakiler daha çabuk gelecek.");

        // ------------------------------------------------------------------ walking away (New Game over a run)

        public static string AbandonRunWarning => L("Abandon this run? Your sentence will be forgotten.", "Bu koşuyu bırakıyor musun? Cezan unutulacak.");
        public static string AbandonHandWarning => L("Abandon this run? The hand on the table counts as folded.",
            "Bu koşuyu bırakıyor musun? Masadaki el çekilmiş sayılır.");
        public static string AbandonSoulWarning => L("Your soul is on the table. Walking away counts as damnation.",
            "Ruhun masada. Kalkıp gitmek lanet sayılır.");
        public static string AbandonButton => L("ABANDON", "BIRAK");

        /// <summary>Quit mid-hand (or with the soul on the table): the hand left behind is forfeited on the next launch.</summary>
        public static string QuitHandWarning => L("Leave now and the hand is lost.", "Şimdi gidersen el kaybedilir.");

        // ------------------------------------------------------------------ guidance for new players

        public static string HandNowFormat => L("NOW: {0}", "ŞU AN: {0}");
        public static string WinnerFormat => L("{0}  WINS", "{0}  KAZANDI");
        public const string GoodHandFormat = "{0}!";
        public static string HandRanksTitle => L("HAND RANKS", "EL SIRALAMASI");
        public static string HandRanksSubtitle => L("Strongest first. A higher hand beats a lower one.", "En güçlüsü başta. Üstteki el alttakini yener.");
        public static string HandRanksTableFooter => L("H or Esc closes", "H ya da Esc kapatır");
        public static string HandsButton => L("HANDS", "ELLER");
        public static string RulesButton => L("RULES", "KURALLAR");

        /// <summary>An example of each hand, for the hand ranking panel.</summary>
        public static string HandExample(HandCategory category)
        {
            switch (category)
            {
                case HandCategory.DeadMansHand: return L("A♠ A♣ 8♠ 8♣ + any", "A♠ A♣ 8♠ 8♣ + herhangi");
                case HandCategory.RoyalFlush: return "10♥ J♥ Q♥ K♥ A♥";
                case HandCategory.StraightFlush: return "5♣ 6♣ 7♣ 8♣ 9♣";
                case HandCategory.FourOfAKind: return "9 9 9 9 K";
                case HandCategory.FullHouse: return "Q Q Q 4 4";
                case HandCategory.Flush: return "2♦ 7♦ 9♦ J♦ K♦";
                case HandCategory.Straight: return "4 5 6 7 8";
                case HandCategory.ThreeOfAKind: return "7 7 7 K 2";
                case HandCategory.TwoPair: return "J J 5 5 A";
                case HandCategory.OnePair: return "8 8 K 4 2";
                default: return "A J 9 5 2";
            }
        }

        // ------------------------------------------------------------------ first-game tips (one line each, in the dealer's voice)

        // Keys (saved as seen in the settings): never translated.
        public const string TipFirstDecision = "tip.decision";
        public const string TipFirstDraw = "tip.draw";
        public const string TipFirstReRaise = "tip.reraise";
        public const string TipFinalStretch = "tip.final";
        public const string TipSoul = "tip.soul";
        public const string TipLucifer = "tip.lucifer";
        public const string TipPowerReady = "tip.power";

        public static string TipText(string tip, int forcedRaiseYears, int gateYears = 250)
        {
            switch (tip)
            {
                case TipPowerReady: return L("Your sin is charged. When the moment comes, press K — or touch your badge.",
                    "Günahın doldu. Anı gelince K'ya bas ya da rozetine dokun.");
                case TipFirstDecision: return L("Like your cards? RAISE. Unsure? PASS. Afraid? FOLD — and lose less.",
                    "Kartların iyi mi? ARTIR. Emin değil misin? PAS. Korktun mu? ÇEKİL — daha az kaybet.");
                case TipFirstDraw: return L("Click the cards to throw back, then DRAW. Keep your pairs, sinner.",
                    "Atacağın kartlara tıkla, sonra KART DEĞİŞ. Çiftlerini tut, günahkâr.");
                case TipFirstReRaise: return L("I raise you back. CALL to stay in — or FOLD and leave the table to me.",
                    "Ben de artırıyorum. Kalmak için GÖR — ya da ÇEKİL, masayı bana bırak.");
                case TipFinalStretch: return string.Format(L("Under {0} years, no passing. Raise or fold — so close to freedom.",
                    "{0} yılın altında pas yok. Artır ya da çekil — özgürlüğe bu kadar yakınken."), forcedRaiseYears);
                case TipSoul: return L("Past my line your soul is the stake. Lose all of it and you are mine forever.",
                    "Çizgimi geçtin, artık bahis ruhun. Hepsini kaybedersen sonsuza dek benimsin.");
                case TipLucifer: return string.Format(L("Only I can set you free. Fall above {0} and you will be cast down.",
                    "Seni yalnızca ben özgür bırakabilirim. {0} üstüne çıkarsan düşersin."), gateYears);
                default:
                    return tip != null && tip.StartsWith(TipCheatPrefix) ? CheatTipText(tip.Substring(TipCheatPrefix.Length)) : null;
            }
        }

        // ------------------------------------------------------------------ the end of a run, and records

        public static string AbsolvedTitle => L("ABSOLVED", "AKLANDIN");
        public static string DamnedTitle => L("DAMNED", "LANETLENDİN");
        public static string AbsolvedSubtitle => L("The gates of Hell open. You walk free.", "Cehennemin kapıları açılıyor. Özgürsün.");
        public static string DamnedSubtitle => L("Your soul is ash. The House keeps you for eternity.", "Ruhun kül oldu. Kasa seni sonsuza dek tutacak.");
        public static string EndHandsFormat => L("Hands played: {0}", "Oynanan el: {0}");
        public static string EndLowestFormat => L("Lowest sentence: {0} years", "En düşük ceza: {0} yıl");
        public static string EndHighestFormat => L("Highest sentence: {0} years", "En yüksek ceza: {0} yıl");
        public static string EndHighestSoul => L("Highest sentence: past the soul line", "En yüksek ceza: ruh çizgisinin ötesi");
        public static string EndBestFormat => L("Best hand: {0}", "En iyi el: {0}");
        public static string EndBestNone => L("Best hand: none shown", "En iyi el: hiç açılmadı");
        public static string EndDealersFormat => L("Tables: {0}", "Masalar: {0}");
        public static string EndSoulStaked => L("Your soul went on the table.", "Ruhun masaya kondu.");
        public static string EndSoulKept => L("Your soul never left you.", "Ruhun seni hiç bırakmadı.");
        public static string MorningStarFallsTitle => L("THE MORNING STAR FALLS", "SABAH YILDIZI DÜŞTÜ");
        public static string MorningStarFallsSubtitle => L("His eyes go dark. Nothing stands between you and the morning.",
            "Gözleri sönüyor. Seninle sabah arasında artık hiçbir şey yok.");
        public static string WildBillTitle => L("WILD BILL'S ESCAPE", "WILD BILL'İN KAÇIŞI");
        public static string WildBillSubtitle => L("Aces and eights. You walked out before he ever looked up.",
            "Aslar ve sekizler. O daha başını kaldırmadan çıkıp gittin.");
        public static string EndBeatLuciferFormat => L("The Morning Star fell on attempt {0}", "Sabah Yıldızı {0}. denemede düştü");
        public static string EndLuciferTriedFormat => L("You faced the Morning Star {0} time(s)", "Sabah Yıldızı'nın karşısına {0} kez oturdun");
        public static string EndNeverMetLucifer => L("You never met the Morning Star", "Sabah Yıldızı'yla hiç karşılaşmadın");
        public static string EndMorningStarRemembers => L("The Morning Star will remember this.", "Sabah Yıldızı bunu unutmayacak.");
        public static string RecordsLuciferReachedFormat => L("Faced the Morning Star: {0}", "Sabah Yıldızı'na ulaşma: {0}");
        public static string RecordsLuciferDefeatedFormat => L("Morning Star fallen: {0}", "Sabah Yıldızı düşürüldü: {0}");
        public static string RecordsFewestAttemptsFormat => L("Fewest attempts: {0}", "En az deneme: {0}");
        public static string RecordsFewestAttemptsNone => L("Fewest attempts: not yet", "En az deneme: henüz yok");
        public static string RecordsWildBillFormat => L("Wild Bill's escapes: {0}", "Wild Bill kaçışları: {0}");
        public static string RecordsBackfiresFormat => L("Backfires seen: {0}", "Geri tepen hileler: {0}");
        public static string ToMenu => L("MENU", "MENÜ");
        public static string Records => L("RECORDS", "REKORLAR");
        public static string RecordsTitle => L("RECORDS", "REKORLAR");
        public static string RecordsRunsFormat => L("Runs started: {0}", "Başlanan koşu: {0}");
        public static string RecordsAbsolvedFormat => L("Walked free: {0}", "Özgür kalan: {0}");
        public static string RecordsDamnedFormat => L("Damned: {0}", "Lanetlenen: {0}");
        public static string RecordsFastestFormat => L("Fastest freedom: {0} hands", "En hızlı özgürlük: {0} el");
        public static string RecordsFastestNone => L("Fastest freedom: not yet", "En hızlı özgürlük: henüz yok");
        public static string RecordsDealerFormat => L("Freed at {0} table: {1}", "{0} masasında aklanma: {1}");   // {0}: GenitiveOf

        // ------------------------------------------------------------------ settings

        public static string SettingsButton => L("SETTINGS", "AYARLAR");
        public static string SettingsTitle => L("SETTINGS", "AYARLAR");
        public static string SettingSpeed => L("ANIMATION SPEED", "ANİMASYON HIZI");
        public static string SettingSpeedHint => L("Any key or click also hurries an animation along.", "Herhangi bir tuş ya da tık da animasyonu hızlandırır.");
        public static string SettingFullscreen => L("FULL SCREEN", "TAM EKRAN");
        public static string SettingFullscreenHint => L("Alt+Enter switches at any time.", "Alt+Enter ile her an değişir.");

        public static string SettingsTabGame => L("GAME", "OYUN");
        public static string SettingsTabDisplay => L("DISPLAY", "GÖRÜNTÜ");
        public static string SettingsTabSound => L("SOUND", "SES");
        public static string SettingsTabsHint => L("Q / E or the arrow keys switch tabs. Esc: back.", "Q / E ya da ok tuşları sekmeyi değiştirir. Esc: geri.");

        public static string SettingWindowMode => L("DISPLAY MODE", "GÖRÜNTÜ MODU");
        public static string WindowModeWindow => L("WINDOW", "PENCERE");
        public static string SettingWindowScale => L("WINDOW SIZE", "PENCERE BOYUTU");
        public static string SettingWindowScaleHint => L("In a window only: multiples of 480×270 that fit your display.",
            "Sadece pencerede: ekranına sığan 480×270 katları.");
        public static string WindowScaleAuto => L("AUTO", "OTOMATİK");
        /// <summary>A window size: {0} the multiple, {1}×{2} the pixels.</summary>
        public static string WindowScaleFormat => L("×{0}  {1}×{2}", "×{0}  {1}×{2}");
        public static string SettingPixelScale => L("PIXEL SCALE", "PİKSEL ÖLÇEĞİ");
        public static string SettingPixelScaleHint => L("Full screen only. Fill: text may look blurry, WHOLE PIXELS recommended.",
            "Tam ekranda. Doldur: yazılar bulanık olabilir, TAM PİKSEL önerilir.");
        public static string PixelScaleWhole => L("WHOLE PIXELS", "TAM PİKSEL");
        public static string PixelScaleFill => L("FILL SCREEN", "EKRANI DOLDUR");
        public static string SettingVSync => L("VERTICAL SYNC", "DİKEY SENKRON");
        public static string SettingVSyncHint => L("Off: the frame rate is held at 60.", "Kapalıyken kare hızı 60'a sabitlenir.");
        public static string SettingMaster => L("MASTER VOLUME", "ANA SES");
        public static string SettingMasterHint => L("Music and effects are each a share of it.", "Müzik ve efektler bunun payıdır.");
        public static string SettingHandGuide => L("HAND GUIDE", "EL REHBERİ");
        public static string SettingHandGuideHint => L("Names your hand as it stands and hints which cards to keep.",
            "Elinin adını söyler, hangi kartları tutacağını gösterir.");
        public static string SettingTips => L("FIRST-GAME TIPS", "İLK OYUN İPUÇLARI");
        public static string SettingTipsHint => L("The dealers explain each new moment once.", "Şeytanlar her yeni anı bir kez anlatır.");
        public static string SettingLanguage => L("LANGUAGE / DİL", "DİL / LANGUAGE");
        public static string SettingLanguageHint => L("On the title menu, L switches it too.", "Ana menüde L tuşu da değiştirir.");
        public static string SettingMusic => L("MUSIC", "MÜZİK");
        public static string SettingMusicHint => L("Every demon plays a tune of their own.", "Her şeytanın kendi ezgisi var.");
        public static string SettingSfx => L("EFFECTS", "EFEKTLER");
        public static string SettingSfxHint => L("Cards, chips, gongs and the rest.", "Kartlar, fişler, gonglar ve gerisi.");

        /// <summary>A volume as shown: "7 / 10", or OFF at 0.</summary>
        public static string Volume(int volume, int max) => volume <= 0 ? Off : volume + " / " + max;
        public static string On => L("ON", "AÇIK");
        public static string Off => L("OFF", "KAPALI");
        public static string ResetTips => L("SHOW AGAIN", "YİNE GÖSTER");
        public static string TipsFresh => L("ALL NEW", "HEPSİ YENİ");

        /// <summary>A language by its own name, the same in every language ("ENGLISH", "TÜRKÇE").</summary>
        public static string LanguageName(Language language) => language == Language.Turkish ? "TÜRKÇE" : "ENGLISH";

        /// <summary>The language button's label: the one a press switches to.</summary>
        public static string LanguageButton => L("TR", "EN");

        public static string SpeedName(HellPoker.Presentation.Settings.AnimationSpeed speed)
        {
            switch (speed)
            {
                case HellPoker.Presentation.Settings.AnimationSpeed.Fast: return L("FAST", "HIZLI");
                case HellPoker.Presentation.Settings.AnimationSpeed.VeryFast: return L("VERY FAST", "ÇOK HIZLI");
                default: return L("NORMAL", "NORMAL");
            }
        }

        // ------------------------------------------------------------------ why a button is locked

        public static string LockedTableFull => L("TABLE FULL — the table is at its limit.", "MASA DOLU — masa sınırında.");
        public static string LockedAllIn => L("ALL IN — every year you have is on the table.", "HEPSİ ORTADA — bütün yılların masada.");
        public static string LockedAllOfIt => L("ALL OF IT — your whole soul is on the table.", "HEPSİ — ruhunun tamamı masada.");
        public static string LockedPassFormat => L("No passing under {0} years: raise or fold.", "{0} yılın altında pas yok: artır ya da çekil.");
        public static string LockedCheckToDrawFormat => L("No checking under {0} years: raise or fold.", "{0} yılın altında pas geçilmez: artır ya da çekil.");
        public static string LockedAnswer => L("The House has raised: call — or fold.", "Kasa artırdı: gör — ya da çekil.");
        public static string LockedAnswerSoul => L("The House wagers more: match it — or fold.", "Kasa daha fazlasını koydu: karşıla — ya da çekil.");
        public static string LockedNothingToCall => L("There is nothing to call.", "Görülecek bir şey yok.");
        public static string LockedChained => L("That card is chained as collateral: it stays this hand.", "O kart rehin zincirinde: bu el kalıyor.");
        public static string LockedTooManyFormat => L("You may throw back at most {0} cards.", "En fazla {0} kart atabilirsin.");
        public static string LockedLeaveMidHand => L("You can only change tables between hands.", "Masayı ancak eller arasında değiştirebilirsin.");

        // ------------------------------------------------------------------ the soul (no numbers, ever)

        public static string SoulLabel => L("YOUR SOUL", "RUHUN");
        public static string SoulOnTable => L("on the table", "masada");
        public static string WagerMore => L("WAGER MORE", "DAHA KOY");
        public static string WagerAll => L("ALL OF IT", "HEPSİ");
        public static string MatchIt => L("MATCH IT", "KARŞILA");
        public static string SoulPromptBet => L("Your soul is on the table. Deal when you dare.", "Ruhun masada. Cesaretin varsa dağıt.");
        public static string SoulPromptReRaise => L("The House wagers more of your soul! Match it — or fold?",
            "Kasa ruhundan daha fazlasını koydu! Karşıla — ya da çekil?");
        public static string SoulStakeInfo => L("Win: your soul mends   ·   Lose: it burns", "Kazanç: ruhun onarılır   ·   Kayıp: yanar");
        public static string SoulWinFormat => L("{0} beats {1}.  Your soul mends.", "{0}, {1} elini yendi.  Ruhun onarılıyor.");
        public static string SoulLossFormat => L("{0} beats your {1}.  Your soul burns.", "{0}, senin {1} elini yendi.  Ruhun yanıyor.");
        public static string SoulFold => L("You fold. A piece of your soul stays on the table.", "Çekildin. Ruhundan bir parça masada kaldı.");
        public static string SoulTakenMessage => L("Your soul is on the table now.", "Ruhun artık masada.");
        public static string SoulReleasedMessage => L("Your soul is your own again.", "Ruhun yeniden senin.");

        // ------------------------------------------------------------------ changing tables

        public static string LeaveTable => L("LEAVE TABLE", "MASADAN KALK");
        public static string SoulBound => L("SOUL BOUND", "RUH BAĞLI");
        public static string NoEscape => L("NO ESCAPE", "KAÇIŞ YOK");
        public static string ChangeTable => L("CHANGE TABLE", "MASA DEĞİŞTİR");
        public static string SitAnyway => L("SIT ANYWAY", "YİNE DE OTUR");
        public static string Locked => L("LOCKED", "KİLİTLİ");
        public static string ReturnToTable => L("RETURN", "DÖN");
        public static string Safe => L("SAFE", "GÜVENLİ");
        public static string SoulAtStake => L("SOUL AT STAKE", "RUH MASADA");
        public static string ChooseTableSubtitle => L("Your sentence goes with you. Mind each demon's soul line.",
            "Cezan seninle gelir. Her şeytanın ruh çizgisine dikkat et.");

        public static string CategoryName(HandCategory category)
        {
            switch (category)
            {
                case HandCategory.HighCard: return L("High Card", "Yüksek Kart");
                case HandCategory.OnePair: return L("One Pair", "Bir Çift");
                case HandCategory.TwoPair: return L("Two Pair", "İki Çift");
                case HandCategory.ThreeOfAKind: return L("Three of a Kind", "Üçlü");
                case HandCategory.Straight: return L("Straight", "Kent");
                case HandCategory.Flush: return L("Flush", "Renk");
                case HandCategory.FullHouse: return L("Full House", "Full");
                case HandCategory.FourOfAKind: return L("Four of a Kind", "Kare");
                case HandCategory.StraightFlush: return L("Straight Flush", "Floş");
                case HandCategory.RoyalFlush: return L("Royal Flush", "Floş Royal");
                case HandCategory.DeadMansHand: return L("Dead Man's Hand", "Ölü Adamın Eli");
                default: return category.ToString();
            }
        }

        /// <summary>A hand's name in capitals. Turkish is upper-cased by hand: the invariant culture would leave "i" undotted.</summary>
        public static string CategoryNameUpper(HandCategory category) => Upper(CategoryName(category));

        /// <summary>Upper case that knows Turkish (i → İ, ı → I) without ever changing the current culture.</summary>
        public static string Upper(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (!Lang.IsTurkish) return text.ToUpperInvariant();
            return text.Replace('i', 'İ').Replace('ı', 'I').ToUpperInvariant();
        }
        // ------------------------------------------------------------------ the deck, counted from hand to hand

        public static string DeckCountFormat => L("DECK {0}", "DESTE {0}");
        public static string DeckCountHintFormat => L("Cards left in the deck: {0}. When it runs out, or with SHUFFLE, all 52 are shuffled again.",
            "Destede kalan: {0} kart. Deste bitince ya da KARIŞTIR ile baştan karılır.");
        /// <summary>Under ten cards the counter shows no number: the deck is about to be shuffled.</summary>
        public static string DeckShufflingLabel => L("SHUFFLING", "KARIŞIYOR");
        public static string DeckShufflingHint => L("Only a few cards are left: the demon shuffles all 52 before the next hand.",
            "Destede birkaç kart kaldı: şeytan bir sonraki elden önce 52 kartı karar.");
        public static string ShuffleButtonFormat => L("SHUFFLE +{0}", "KARIŞTIR +{0}");
        public static string DeckRanOut => L("The deck ran out; the demon shuffles.", "Deste bitti, şeytan desteyi karıyor.");
        /// <summary>{0} the years paid.</summary>
        public static string ShuffledFormat => L("You paid {0} years: the demon shuffles all 52.", "{0} yıl ödedin: şeytan 52 kartı karıyor.");
        public static string ShuffleNotBetweenHands => L("Only between hands, before the deal.", "Ancak eller arasında, dağıtmadan önce karıştırılır.");
        public static string ShuffleAlreadyDone => L("Once before each hand.", "Her elden önce bir kez.");
        /// <summary>{0} the least sentence.</summary>
        public static string ShuffleTooFewYearsFormat => L("Below {0} years the demon will not shuffle for you.", "{0} yılın altında şeytan senin için karıştırmaz.");
        public static string ShuffleSoul => L("Not with your soul on the table.", "Ruhun masadayken olmaz.");
        public static string ShuffleDeckFull => L("The deck is already full.", "Deste zaten tam.");
        public static string ShuffleComing => L("The demon will shuffle before the next hand anyway.", "Şeytan bir sonraki elden önce desteyi zaten karacak.");
        /// <summary>{0} the shuffle's years, {1} the soul line.</summary>
        public static string ShuffleWouldStakeSoulFormat => L("The shuffle's {0} years would take you to {1}: your soul would go on the table.",
            "Karıştırmanın {0} yılı seni {1} yıla taşır: ruhun masaya gelir.");
        public static string ShuffleJesterDeck => L("The Jester's deck is shuffled every hand.", "Soytarı'nın destesi her el karılır.");

        public static string ShuffleRefused(HellPoker.Core.Game.ShuffleRefusal refusal, int minYears, int years = 10, int soulLine = 0)
        {
            switch (refusal)
            {
                case HellPoker.Core.Game.ShuffleRefusal.DeckFull: return ShuffleDeckFull;
                case HellPoker.Core.Game.ShuffleRefusal.ShuffleComing: return ShuffleComing;
                case HellPoker.Core.Game.ShuffleRefusal.WouldStakeSoul: return string.Format(ShuffleWouldStakeSoulFormat, years, soulLine);
                case HellPoker.Core.Game.ShuffleRefusal.NotBetweenHands: return ShuffleNotBetweenHands;
                case HellPoker.Core.Game.ShuffleRefusal.AlreadyShuffled: return ShuffleAlreadyDone;
                case HellPoker.Core.Game.ShuffleRefusal.TooFewYears: return string.Format(ShuffleTooFewYearsFormat, minYears);
                case HellPoker.Core.Game.ShuffleRefusal.SoulOnTable: return ShuffleSoul;
                default: return ShuffleJesterDeck;
            }
        }
    }
}
