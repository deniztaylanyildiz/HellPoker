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
        public static string KitSummonLegionTitle => L("THE LEGION: ALL FREE", "LEJYON: HEPSİ BEDAVA");

        /// <summary>The imps' buttons: {0} the price (coins on a floor; "%" of the bar at a demon's).</summary>
        public static string KitEyeButtonFormat => L("EYE {0}", "GÖZ {0}");
        public static string KitHandButtonFormat => L("HAND {0}", "EL {0}");
        public static string KitWhisperButtonFormat => L("WHISPER {0}", "FISILTI {0}");

        /// <summary>{0} a percent of the bar (a demon's table's price).</summary>
        public static string KitBarPriceFormat => L("+{0}%", "+%{0}");
        public static string KitFree => L("FREE", "BEDAVA");

        public static string KitSummonHint => L("Before the draw: EYE turns a closed card of the demon's, HAND trades a card of yours for an open one of " +
            "his, WHISPER tells his next cheat. One imp in ten runs wild. K: the Legion.",
            "Değişten önce: GÖZ şeytanın kapalı bir kartını açar, EL bir kartını onun açık bir kartıyla değişir, FISILTI sıradaki hilesini " +
            "söyler. Onda bir iblis azar. K: Lejyon.");

        public static string EyeOpened => L("The imp's eye turns one of his cards.", "İblisin gözü kartlarından birini açtı.");
        public static string SwapPrompt => L("Pick the card the imp's hand trades (the button again: put away).", "İblisin elinin değişeceği kartı seç (yine bas: vazgeç).");

        /// <summary>{0} the card taken, {1} the card given.</summary>
        public static string SwappedFormat => L("The imp's hand: {0} is yours, {1} is his.", "İblisin eli: {0} senin, {1} onun.");

        /// <summary>{0} the cheat's name.</summary>
        public static string WhisperFormat => L("The imp whispers: \"{0}. And that is the truth.\"", "İblis fısıldıyor: \"{0}. Bu da işin doğrusu.\"");
        public static string WhisperNothing => L("The imp whispers: \"Nothing comes. Not this hand.\"", "İblis fısıldıyor: \"Bir şey gelmiyor. Bu elde değil.\"");

        /// <summary>{0} the card lowered, {1} what it became.</summary>
        public static string WildFormat => L("The imp runs wild: your {0} is a {1} now.", "İblis azdı: {0} artık {1}.");
        public static string LegionCalled => L("The Legion answers: every imp is free this hand.", "Lejyon cevap verdi: bu elde her iblis bedava.");
        public static string SummonNoCoins => L("Not enough coins: the imps never take your last.", "Yeterli coin yok: iblisler son coinini almaz.");

        // ------------------------------------------------------------------ the circles of Hell

        /// <summary>{0} the circle.</summary>
        public static string CircleLabelFormat => L("CIRCLE {0}", "{0}. DAİRE");
        public static string CircleNone => L("NO CIRCLE", "DAİRESİZ");

        /// <summary>What a circle adds (each adds to the ones above it).</summary>
        public static string CircleEffect(int circle)
        {
            switch (circle)
            {
                case 1: return L("The imps' purses +25%.", "İblislerin keseleri +%25.");
                case 2: return L("The tribute +20 coins.", "Haraç +20 coin.");
                case 3: return L("The demons' bars +15%.", "Şeytanların barları +%15.");
                case 4: return L("The market ×1.25; the fire offers two boons.", "Pazar ×1,25; ateş iki lütuf sunar.");
                case 5: return L("Demons sit down one malice short of a cheat; Lucifer copies two powers.",
                    "Şeytanlar bir hileye bir kala başlar; Lucifer iki gücü kopyalar.");
                default: return L("The designer's Hell.", "Tasarımcının cehennemi.");
            }
        }

        /// <summary>A circle's note on the class choice: its number and every rule down to it.</summary>
        public static string CircleNote(int circle) =>
            circle <= 0 ? CircleEffect(0)
                : string.Format(CircleLabelFormat, circle) + ": " + string.Join(" ", Enumerable.Range(1, circle).Select(CircleEffect)) + " " +
                  string.Format(CircleWinNoteFormat, Circles.WinStepPercent * circle);

        /// <summary>The circles' toll on every demon's table: {0} the percent a win is worth less there.</summary>
        public static string CircleWinNoteFormat => L("Wins against a demon: −{0}%.", "Şeytana karşı kazanç: −%{0}.");

        /// <summary>How to Play's line on the circles: {0} the percent each circle takes off a win against a demon.</summary>
        public static string CircleWinRuleFormat => L("Every circle also makes a win against a demon take {0}% less off his bar.",
            "Her daire, şeytana karşı kazancın barından sildiğini de %{0} azaltır.");

        /// <summary>{0} the deepest circle open for the class.</summary>
        public static string CirclesOpenFormat => L("Circles open: 0-{0}", "Açık daireler: 0-{0}");
        public static string CirclesClosedNote => L("Walk free to open the next circle.", "Sonraki daire için kurtul.");
        public static string FireCircleNote => L("(The fourth circle: the fire offers only two boons.)", "(Dördüncü daire: ateş yalnızca iki lütuf sunar.)");

        /// <summary>{0} the class, {1} the circle.</summary>
        public static string CircleOpenedFormat => L("A new circle opens for the {0}: {1}.", "{0} için yeni bir daire açıldı: {1}.");

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

        /// <summary>How many locked things the records list by name (the rest are counted).</summary>
        public const int LocksListed = 6;

        /// <summary>The records' lines of the progression (their right column): the deepest circle freed per class, then what is still
        /// locked and what opens it.</summary>
        public static IReadOnlyList<string> ProgressRecordLines(ChapterRecords r)
        {
            r = r ?? new ChapterRecords();
            var lines = new List<string> { L("Deepest circle freed:", "Kurtulunan en derin daire:") };
            lines.AddRange(SinnerRoster.PhaseTwo.Select(c => "  " + SinnerName(c.Id) + ": " + (r.BestCircle(c.Id) >= 0 ? r.BestCircle(c.Id).ToString() : "—")));
            lines.Add("");
            var locked = new List<string>();
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
