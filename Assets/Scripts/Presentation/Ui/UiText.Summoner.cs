using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Core.Sinners;

namespace HellPoker.Presentation.Ui
{
    /// <summary>Phase 2's progression in words: the Summoner's imps, the circles of Hell, the locks and their milestones.</summary>
    internal static partial class UiText
    {
        // ------------------------------------------------------------------ the Summoner's panel

        public static string KitSummonTitle => L("IMPS", "İBLİSLER");

        /// <summary>The panel's title while the loyal imp frees one (Demo 2.3's passive).</summary>
        public static string KitSummonLoyalTitle => L("IMPS: ONE FREE", "İBLİSLER: BİRİ BEDAVA");

        /// <summary>An imp not open yet, on its button.</summary>
        public static string KitLockedShort => L("LOCK", "KİLİT");

        /// <summary>The imps' buttons: {0} the price (coins on a floor; "%" of the bar at a demon's).</summary>
        public static string KitEyeButtonFormat => L("EYE {0}", "GÖZ {0}");
        public static string KitHandButtonFormat => L("HAND {0}", "EL {0}");
        public static string KitWhisperButtonFormat => L("WHISPER {0}", "FISILTI {0}");

        /// <summary>{0} a percent of the bar (a demon's table's price).</summary>
        public static string KitBarPriceFormat => L("+{0}%", "+%{0}");
        public static string KitFree => L("FREE", "BEDAVA");

        /// <summary>The imps' hover: {0} the imps open, {1} the wild ones (percent).</summary>
        public static string KitSummonHintFormat => L("Before the draw: EYE turns a closed card of the demon's, HAND trades a card of yours for an open one of " +
            "his, WHISPER tells his next cheat. Open now: {0} of 3 (his relics open the HAND, then the WHISPER, then halve the prices). " +
            "LOYAL IMP: a hand's first imp is free. One imp in ten runs wild.",
            "Değişten önce: GÖZ şeytanın kapalı bir kartını açar, EL bir kartını onun açık bir kartıyla değişir, FISILTI sıradaki hilesini " +
            "söyler. Açık: {0}/3 (emanetleri önce EL'i, sonra FISILTI'yı açar, sonra fiyatları yarıya indirir). SADIK İBLİS: elin ilk iblisi " +
            "bedava. Onda bir iblis azar.");

        public static string KitSummonHint(ClassKit kit) => string.Format(KitSummonHintFormat, kit?.ImpsOpen ?? 1);

        /// <summary>An imp not open yet: the Hand or the Whisper waits for the Summoner's relics.</summary>
        public static string ImpLockedText(SummonAct act) => act == SummonAct.Hand
            ? L("The HAND opens with a Summoner's relic.", "EL bir Çağırıcı emanetiyle açılır.")
            : L("The WHISPER opens with a second Summoner's relic.", "FISILTI ikinci bir Çağırıcı emanetiyle açılır.");

        /// <summary>The loyal imp freed this one.</summary>
        public static string LoyalImpLine => L("Loyal imp: this one is free.", "Sadık iblis: bu bedava.");

        /// <summary>The Blood Oath: {0} the demon's card lowered, {1} what it became.</summary>
        public static string WildDemonFormat => L("The imp runs wild — on the demon: his {0} is a {1} now.", "İblis azdı — şeytana: onun {0} kartı artık {1}.");

        public static string EyeOpened => L("The imp's eye turns one of his cards.", "İblisin gözü kartlarından birini açtı.");
        public static string SwapPrompt => L("Pick the card the imp's hand trades (the button again: put away).", "İblisin elinin değişeceği kartı seç (yine bas: vazgeç).");

        /// <summary>{0} the card taken, {1} the card given.</summary>
        public static string SwappedFormat => L("The imp's hand: {0} is yours, {1} is his.", "İblisin eli: {0} senin, {1} onun.");

        /// <summary>{0} the cheat's name.</summary>
        public static string WhisperFormat => L("The imp whispers: \"{0}. And that is the truth.\"", "İblis fısıldıyor: \"{0}. Bu da işin doğrusu.\"");
        public static string WhisperNothing => L("The imp whispers: \"Nothing comes. Not this hand.\"", "İblis fısıldıyor: \"Bir şey gelmiyor. Bu elde değil.\"");

        /// <summary>{0} the card lowered, {1} what it became.</summary>
        public static string WildFormat => L("The imp runs wild: your {0} is a {1} now.", "İblis azdı: {0} artık {1}.");
        public static string SummonNoCoins => L("Not enough coins: the imps never take your last.", "Yeterli coin yok: iblisler son coinini almaz.");

        // ------------------------------------------------------------------ the locks

        public static string LockedLabel => L("LOCKED", "KİLİTLİ");

        /// <summary>{0} what opened (a class, a relic).</summary>
        public static string UnlockedFormat => L("UNLOCKED: {0}", "AÇILDI: {0}");

        /// <summary>What a locked thing waits for.</summary>
        public static string MilestoneText(Milestone m)
        {
            switch (m?.Key)
            {
                case "runs": return string.Format(L("Play {0} runs.", "{0} koşu oyna."), m.Count);
                case "bought": return string.Format(L("Buy {0} relics at the black market.", "Kara Pazar'dan {0} emanet al."), m.Count);
                case "warden": return L("Beat a warden.", "Bir bekçiyi yen.");
                case "wardens3": return string.Format(L("Beat {0} wardens in one chapter.", "Bir bölümde {0} bekçi yen."), m.Count);
                case "chapter2": return L("Reach Belial's chapter.", "Belial'in bölümüne ulaş.");
                case "tribute": return L("Pay a gate's tribute in full.", "Bir kapının haracını tam öde.");
                case "belial": return L("Beat Belial.", "Belial'i yen.");
                case "chapter3": return L("Reach Lilith's chapter.", "Lilith'in bölümüne ulaş.");
                case "lilith": return L("Beat Lilith.", "Lilith'i yen.");
                case "lucifer": return L("Reach Lucifer.", "Lucifer'e ulaş.");
                case "freed": return L("Walk free once.", "Bir kez kurtul.");
                default: return "";
            }
        }

        /// <summary>Demo 3.4: a class's freedoms per level with the fewest years left ("E 2× 420 · M 1× 610 · IRONMAN 1× 900"); "—" for
        /// none. Ironman's are spelled out (marked apart from the rest).</summary>
        private static string FreedomsByLevel(ChapterRecords r, string classId)
        {
            var parts = new List<string>();
            for (Difficulty d = Difficulty.Easy; d <= DifficultyRules.Hardest; d++)
            {
                int n = r.FreedAt(classId, d);
                if (n <= 0) continue;
                int? years = r.LowestSentence(classId, d);
                string label = d == Difficulty.Ironman ? DifficultyName(d) : DifficultyInitial(d);
                parts.Add(years.HasValue ? string.Format(RecordLevelFormat, label, n, years.Value) : string.Format(RecordLevelCountFormat, label, n));
            }
            return parts.Count == 0 ? "—" : string.Join(" · ", parts);
        }

        /// <summary>How many locked things the records list by name (the rest are counted).</summary>
        public const int LocksListed = 6;

        /// <summary>The records' lines of the progression (their right column): each class's freedoms per difficulty (the fewest years
        /// left), then what is still locked and what opens it.</summary>
        public static IReadOnlyList<string> ProgressRecordLines(ChapterRecords r)
        {
            r = r ?? new ChapterRecords();
            var lines = new List<string> { RecordLevelsTitle };
            lines.AddRange(SinnerRoster.PhaseTwo.Select(c => "  " + SinnerName(c.Id) + ": " + FreedomsByLevel(r, c.Id)));
            lines.Add("");
            var locked = new List<string>();
            for (Difficulty d = Difficulty.Hard; d <= DifficultyRules.Hardest; d++)
                if (!r.IsDifficultyOpen(d)) locked.Add("  " + DifficultyName(d) + " — " + DifficultyUnlockText(d));
            if (!ChapterUnlocks.IsClassOpen(Summoner.ClassId, r))
                locked.Add("  " + SinnerName(Summoner.ClassId) + " — " + MilestoneText(ChapterUnlocks.SummonerMilestone));
            locked.AddRange(ChapterUnlocks.Relics.Where(m => !m.IsReached(r)).Select(m => "  " + RelicName(m.Id) + " — " + MilestoneText(m)));
            if (locked.Count == 0)
            {
                lines.Add(L("Everything is open.", "Her şey açık."));
                return lines;
            }
            lines.Add(L("Locked:", "Kilitli:"));
            lines.AddRange(locked.Take(LocksListed));
            if (locked.Count > LocksListed) lines.Add(string.Format(L("  ...and {0} more.", "  ...ve {0} tane daha."), locked.Count - LocksListed));
            return lines;
        }    }
}
