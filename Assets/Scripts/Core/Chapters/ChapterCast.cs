using HellPoker.Core.Dealers;

namespace HellPoker.Core.Chapters
{
    /// <summary>
    /// Who sits across the table on a chapter's floors: the imps of the chapter's demon, and the chapter's warden (Mammon's
    /// Golden-Eyed Collector, Belial's False Prophet, Lilith's Night Nurse). Ids for their portraits, halls and words; the rules
    /// they play by are the chapter demon's (<see cref="FloorTable"/>).
    /// </summary>
    public static class ChapterCast
    {
        public const string ImpId = "imp";
        public const string CollectorId = "collector";
        public const string BelialImpId = "imp_belial";
        public const string ProphetId = "prophet";
        public const string LilithImpId = "imp_lilith";
        public const string NurseId = "nurse";

        /// <summary>Every face of the floors (for preloading their halls).</summary>
        public static readonly string[] All = { ImpId, CollectorId, BelialImpId, ProphetId, LilithImpId, NurseId };

        /// <summary>The warden of a chapter.</summary>
        public static string WardenOf(ChapterRules rules)
        {
            switch (rules?.BossId)
            {
                case DealerRoster.BelialId: return ProphetId;
                case DealerRoster.LilithId: return NurseId;
                default: return CollectorId;
            }
        }

        /// <summary>The imps of a chapter.</summary>
        public static string ImpOf(ChapterRules rules)
        {
            switch (rules?.BossId)
            {
                case DealerRoster.BelialId: return BelialImpId;
                case DealerRoster.LilithId: return LilithImpId;
                default: return ImpId;
            }
        }

        /// <summary>The one across the table at a floor match.</summary>
        public static string HouseOf(FloorTable table, ChapterRules rules) => table != null && table.IsWarden ? WardenOf(rules) : ImpOf(rules);
    }
}
