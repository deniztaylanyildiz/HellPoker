using HellPoker.Core.Chapters;

namespace HellPoker.Presentation.Ui
{
    /// <summary>Demo 3.4 in words: the difficulty levels, the demons' rage and carried rules, SAVE and Ironman's warnings.</summary>
    internal static partial class UiText
    {
        // ------------------------------------------------------------------ the levels

        public static string DifficultyName(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Medium: return L("MEDIUM", "ORTA");
                case Difficulty.Hard: return L("HARD", "ZOR");
                case Difficulty.Ironman: return L("IRONMAN", "IRONMAN");
                default: return L("EASY", "KOLAY");
            }
        }

        /// <summary>A level's letter in the records' tight lines.</summary>
        public static string DifficultyInitial(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Medium: return L("M", "O");
                case Difficulty.Hard: return L("H", "Z");
                case Difficulty.Ironman: return "I";
                default: return L("E", "K");
            }
        }

        /// <summary>What an open level is (the row's hover, the line under the title).</summary>
        public static string DifficultyHover(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Medium:
                    return L("Medium: the ante grows faster, the tribute follows your purse, the demons rage at half their bar, the map is less safe.",
                        "Orta: ante daha hızlı artar, haraç kesene göre, boss'lar öfkelenir, harita daha az güvenli.");
                case Difficulty.Hard:
                    return L("Hard: all of Medium; the purse is topped up less between chapters, a loss to a demon hurts more.",
                        "Zor: Orta'nın hepsi; bölüm başında kese daha az dolar, şeytana kayıp daha çok acıtır.");
                case Difficulty.Ironman:
                    return L("Ironman: all of Hard, harsher; the demons cheat sooner and carry each other's rule; saves only at the camps, death deletes the run.",
                        "Ironman: Zor'un hepsi, daha sert; şeytanlar daha sık hile yapar ve birbirinin kuralını taşır; kayıt sadece kampta, ölünce koşu silinir.");
                default:
                    return L("Easy: the game as designed, no number changed.", "Kolay: oyunun kendisi, hiçbir sayı değişmez.");
            }
        }

        /// <summary>What opens a level.</summary>
        public static string DifficultyUnlockText(Difficulty d) => d == Difficulty.Ironman
            ? L("Walk free on Hard.", "Zor'da kurtul.")
            : L("Walk free on Medium with any class.", "Herhangi bir sınıfla Orta'da kurtul.");

        /// <summary>A locked level's hover.</summary>
        public static string DifficultyLockedHover(Difficulty d) => string.Format(DifficultyLockedFormat, DifficultyName(d), DifficultyUnlockText(d));

        /// <summary>{0} the level, {1} what opens it.</summary>
        public static string DifficultyLockedFormat => L("{0} is locked: {1}", "{0} kilitli: {1}");

        /// <summary>{0} the level that opened.</summary>
        public static string DifficultyOpenedFormat => L("A new difficulty opens: {0}.", "Yeni bir zorluk açıldı: {0}.");

        /// <summary>The map's side panel: {0} the level.</summary>
        public static string MapDifficultyFormat => L("Difficulty: {0}", "Zorluk: {0}");

        /// <summary>The continue card's line: {0} the level.</summary>
        public static string ContinueCardDifficultyFormat => L("{0}", "{0}");

        // ------------------------------------------------------------------ the records

        public static string RecordLevelsTitle => L("Freed per difficulty (times, fewest years left):", "Zorluk başına kurtuluş (kez, en az kalan ceza):");

        /// <summary>{0} the level, {1} the freedoms, {2} the fewest years left.</summary>
        public static string RecordLevelFormat => L("{0} {1}× {2}", "{0} {1}× {2}");

        /// <summary>{0} the level, {1} the freedoms (no score kept).</summary>
        public static string RecordLevelCountFormat => L("{0} {1}×", "{0} {1}×");

        // ------------------------------------------------------------------ the demons' rage (Medium on) and carried rules (Ironman)

        public static string RageFlash => L("ENRAGED", "ÖFKELENDİ");

        /// <summary>{0} the demon (as in a sentence).</summary>
        public static string RageLineFormat => L("{0} is enraged: the stakes are higher, the rule comes more often.",
            "{0} öfkelendi: bahisler büyüdü, kuralı daha sık işliyor.");

        /// <summary>{0} the stakes' percent.</summary>
        public static string RageHintFormat => L("Enraged: stakes {0}%, the rule more often.", "Öfkeli: bahis %{0}, kural daha sık.");

        /// <summary>A carried rule's icon hover: {0} its name, {1} its words.</summary>
        public static string CarriedRuleFormat => L("CARRIED RULE (half strength): {0}\n{1}", "TAŞINAN KURAL (yarım güç): {0}\n{1}");

        public static string CarriedRuleName(string ruleId) => ruleId == "interest"
            ? L("Mammon's INTEREST", "Mammon'un FAİZİ")
            : L("Belial's ROLE SWAP", "Belial'in ROL TAKASI");

        public static string CarriedRuleText(string ruleId) => ruleId == "interest"
            ? L("Every hand you do not win, the bar grows 1% of its start.", "Kazanmadığın her elde bar, başlangıcının %1'i kadar büyür.")
            : L("Every eighth hand, as the draw begins, one of his face-down cards swaps with one of yours.",
                "Her sekiz elde bir, kart değişimi başlarken kapalı kartlarından biri seninkilerden biriyle yer değiştirir.");

        // ------------------------------------------------------------------ SAVE and Ironman

        public static string SaveButton => L("SAVE", "KAYDET");
        public static string SavedFlash => L("SAVED", "KAYDEDİLDİ");
        public static string SaveAtTableHint => L("Can't save at the table", "Masada kayıt yapılamaz");

        public static string IronmanQuitWarning => L("Ironman: everything since the last camp will be lost. Leave anyway?",
            "Ironman: son kamptan beri yapılan ilerleme kaybolacak. Çıkılsın mı?");
        public static string Yes => L("YES", "EVET");
        public static string No => L("NO", "HAYIR");
    }
}
