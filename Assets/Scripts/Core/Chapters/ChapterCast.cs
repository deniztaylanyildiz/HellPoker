namespace HellPoker.Core.Chapters
{
    /// <summary>
    /// Who sits across the table on a chapter's floors: the imps of the chapter's demon, and the warden (Mammon's chapter: the
    /// Golden-Eyed Collector). Ids for their portraits, halls and words; the rules they play by are the chapter demon's
    /// (<see cref="FloorTable"/>).
    /// </summary>
    public static class ChapterCast
    {
        public const string ImpId = "imp";
        public const string CollectorId = "collector";

        /// <summary>The warden of a chapter. Only Mammon's (the Collector) is drawn yet: the later chapters borrow him until theirs are.</summary>
        public static string WardenOf(ChapterRules rules) => CollectorId;

        /// <summary>The one across the table at a floor match.</summary>
        public static string HouseOf(FloorTable table, ChapterRules rules) => table != null && table.IsWarden ? WardenOf(rules) : ImpId;
    }
}
