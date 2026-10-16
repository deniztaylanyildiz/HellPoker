using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Dealers;
using HellPoker.Core.Relics;
using HellPoker.Core.Sinners;

namespace HellPoker.Core.Chapters
{
    /// <summary>What a locked thing waits for: its id, the milestone's key (the presentation's words) and the count that opens it.</summary>
    public sealed class Milestone
    {
        public string Id { get; }

        /// <summary>The milestone's stable key (UiText says it): "runs", "bought", "warden", "chapter2", "tribute", "belial", "chapter3",
        /// "lilith", "lucifer", "freed".</summary>
        public string Key { get; }

        /// <summary>How many times (the runs, the relics bought); 1 for the others.</summary>
        public int Count { get; }

        private readonly Func<ChapterRecords, bool> _reached;

        public Milestone(string id, string key, int count, Func<ChapterRecords, bool> reached)
        {
            Id = id;
            Key = key;
            Count = count;
            _reached = reached;
        }

        public bool IsReached(ChapterRecords records) => _reached(records ?? new ChapterRecords());
    }

    /// <summary>
    /// Phase 2's locks: a run starts with the four classes and fourteen relics; the Summoner opens with the first freedom, the other ten
    /// relics with milestones written on their cards (the records keep the counts). A class's circles open one by one
    /// (<see cref="ChapterRecords.CirclesOpen"/>). Nothing here touches the Boss Simulator.
    /// </summary>
    public static class ChapterUnlocks
    {
        public static readonly IReadOnlyList<Milestone> Relics = new[]
        {
            new Milestone(ChapterRelicIds.BloodyDie, "runs", 3, r => r.Runs >= 3),
            new Milestone(ChapterRelicIds.RatTooth, "bought", 3, r => r.RelicsBought >= 3),
            new Milestone(ChapterRelicIds.SaltCircle, "warden", 1, r => r.WardensBeaten >= 1),
            new Milestone(ChapterRelicIds.OldMap, "chapter2", 1, r => r.DeepestChapter >= 2),
            new Milestone(ChapterRelicIds.SilverBell, "tribute", 1, r => r.TributesPaid >= 1),
            new Milestone(ChapterRelicIds.RavenFeather, "belial", 1, r => r.Beaten(DealerRoster.BelialId) >= 1),
            new Milestone(ChapterRelicIds.BlackBook, "chapter3", 1, r => r.DeepestChapter >= 3),
            new Milestone(ChapterRelicIds.HangmansRope, "lilith", 1, r => r.Beaten(DealerRoster.LilithId) >= 1),
            new Milestone(ChapterRelicIds.LilithsTear, "lucifer", 1, r => r.LuciferReached >= 1),
            new Milestone(ChapterRelicIds.AbyssCoin, "freed", 1, r => r.Freed >= 1),
            // Demo 2.1: five of the fifteen new ones are locked too.
            new Milestone(ChapterRelicIds.BloodPact, "runs", 5, r => r.Runs >= 5),
            new Milestone(ChapterRelicIds.TwinDice, "bought", 6, r => r.RelicsBought >= 6),
            new Milestone(ChapterRelicIds.LostSoulsLetter, "chapter2", 1, r => r.DeepestChapter >= 2),
            new Milestone(ChapterRelicIds.MammonsGoldTooth, "belial", 1, r => r.Beaten(DealerRoster.BelialId) >= 1),
            new Milestone(ChapterRelicIds.LucifersFeather, "lucifer", 1, r => r.LuciferReached >= 1),
            // 2.1.1
            new Milestone(ChapterRelicIds.DevilsCompass, "wardens3", 3, r => r.MostWardensInAChapter >= 3),
        };

        public static readonly Milestone SummonerMilestone = new Milestone(Summoner.ClassId, "freed", 1, r => r.Freed >= 1);

        /// <summary>The milestone a relic waits for; null for one open from the start.</summary>
        public static Milestone ForRelic(string id) => Relics.FirstOrDefault(m => m.Id == id);

        public static bool IsRelicOpen(string id, ChapterRecords records) => ForRelic(id)?.IsReached(records) ?? true;

        /// <summary>Every class is open from the start (Demo 2.2, the user's call: the test builds try them all). False brings the old lock
        /// back: the Summoner opens with the first freedom (<see cref="SummonerMilestone"/>).</summary>
        public static bool AllClassesOpen = true;

        public static bool IsClassOpen(string classId, ChapterRecords records) =>
            AllClassesOpen || classId != Summoner.ClassId || SummonerMilestone.IsReached(records);

        /// <summary>The relics a run may meet now.</summary>
        public static IEnumerable<string> OpenRelics(ChapterRecords records) => ChapterRelics.All.Where(id => IsRelicOpen(id, records));

        /// <summary>What opened between two books (a run's end): the classes and relics, by id.</summary>
        public static IReadOnlyList<string> Opened(ChapterRecords before, ChapterRecords after)
        {
            var opened = new List<string>();
            if (!IsClassOpen(Summoner.ClassId, before) && IsClassOpen(Summoner.ClassId, after)) opened.Add(Summoner.ClassId);
            opened.AddRange(Relics.Where(m => !m.IsReached(before) && m.IsReached(after)).Select(m => m.Id));
            return opened;
        }
    }
}