using HellPoker.Core.Sinners;

namespace HellPoker.Presentation.Ui
{
    /// <summary>Demo 3.1: the player's panel in a chapter table's right column (the class, its passive in a word, the worn relics).</summary>
    internal static partial class UiText
    {
        /// <summary>The passive's short name for the panel's row (the Jester has none: his row counts the jokers).</summary>
        public static string ChapterPassiveName(string id)
        {
            switch (id)
            {
                case Peasant.ClassId: return L("SILENT FIELD", "SESSİZ TARLA");
                case Warlock.ClassId: return L("SEES THE TRUTH", "GERÇEĞİ GÖRÜR");
                case King.ClassId: return L("ROYAL DISCOUNT", "KRALİYET İNDİRİMİ");
                case Summoner.ClassId: return L("LOYAL IMP", "SADIK İBLİS");
                case Thief.ClassId: return L("GLANCE", "GÖZ UCU");
                default: return null;
            }
        }

        /// <summary>The Jester's row: the jokers in the deck.</summary>
        public static string PanelJokersFormat => L("JOKERS ×{0}", "JOKER ×{0}");

        /// <summary>The panel's hover over the portrait and the passive: what the class does, and how to use it.</summary>
        public static string PanelClickHint => L("Click the portrait (or K).", "Portreye tıkla (ya da K).");
    }
}
