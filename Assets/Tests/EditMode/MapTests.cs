using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Core.Randomness;
using HellPoker.Core.Sinners;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Demo 2.1's chapter map: floors of 2-5 nodes, paths that split and join but never cross, the unknown "?", the treasures of the
    /// middle third, the camps, the wardens; an older save's map from its own generator.</summary>
    public class MapTests
    {
        private static IEnumerable<(int chapter, ChapterMap map, int seed)> Maps(int seeds = 200)
        {
            for (int chapter = 1; chapter <= 3; chapter++)
                for (int seed = 0; seed < seeds; seed++)
                    yield return (chapter, ChapterMap.Generate(ChapterRules.For(chapter), new SystemRandomSource(seed)), seed);
        }

        private static HashSet<MapNode> Reach(ChapterMap map, MapNode start)
        {
            var all = new HashSet<MapNode> { start };
            var here = new List<MapNode> { start };
            while (here.Count > 0)
            {
                here = here.SelectMany(map.NextFrom).Distinct().ToList();
                foreach (MapNode n in here) all.Add(n);
            }
            return all;
        }

        [Test]
        public void TheFloors_TwoToFiveNodes_ThreeFirst_OneCampLast_NeverTheSameCountTwiceInARow()
        {
            var counts = new HashSet<int>();
            foreach (var (chapter, map, seed) in Maps())
            {
                Assert.AreEqual(ChapterMap.CurrentVersion, map.Version);
                Assert.AreEqual(ChapterRules.For(chapter).Floors, map.FloorCount);
                Assert.AreEqual(3, map.Floors[0].Count, "three starts");
                Assert.AreEqual(1, map.Floors[map.FloorCount - 1].Count, "one camp before the gate");
                Assert.AreEqual(NodeKind.PurgatoryFire, map.Floors[map.FloorCount - 1][0].Kind);
                for (int f = 1; f < map.FloorCount - 1; f++)
                {
                    int n = map.Floors[f].Count;
                    counts.Add(n);
                    Assert.That(n, Is.InRange(2, 5));
                    Assert.AreNotEqual(map.Floors[f - 1].Count, n, $"chapter {chapter} seed {seed}: floor {f + 1} as many as the one above");
                    for (int l = 0; l < n; l++) Assert.AreEqual(l, map.Floors[f][l].Lane, "a node's lane is its place");
                }
            }
            CollectionAssert.AreEquivalent(new[] { 2, 3, 4, 5 }, counts);
        }

        [Test]
        public void EveryNodeLeadsOn_EveryNodeIsReached_NoPathsCross()
        {
            bool split = false, join = false;
            foreach (var (chapter, map, seed) in Maps())
                for (int f = 0; f + 1 < map.FloorCount; f++)
                {
                    var floor = map.Floors[f];
                    Assert.That(floor.All(n => n.Next.Count > 0), $"chapter {chapter} seed {seed}: a dead end on floor {f + 1}");
                    var reached = new HashSet<int>(floor.SelectMany(n => n.Next));
                    Assert.AreEqual(map.Floors[f + 1].Count, reached.Count, $"chapter {chapter} seed {seed}: a node on floor {f + 2} nobody reaches");
                    var edges = floor.SelectMany(n => n.Next.Select(j => (i: n.Lane, j))).ToList();
                    foreach (var a in edges)
                    foreach (var b in edges)
                        if (a.i < b.i) Assert.LessOrEqual(a.j, b.j, $"chapter {chapter} seed {seed}: paths cross on floor {f + 1}");
                    split |= floor.Any(n => n.Next.Count > 1);
                    join |= reached.Any(j => floor.Count(n => n.Next.Contains(j)) > 1);
                }
            Assert.IsTrue(split, "paths split");
            Assert.IsTrue(join, "and join");
        }

        [Test]
        public void TheFirstFloor_ATableAtLeast_ElseTablesEventsUnknowns()
        {
            foreach (var (_, map, _) in Maps())
            {
                Assert.That(map.Floors[0].Any(n => n.Kind == NodeKind.Table));
                Assert.That(map.Floors[0].All(n => n.Kind == NodeKind.Table || n.Kind == NodeKind.Event || n.Kind == NodeKind.Unknown));
            }
        }

        [Test]
        public void OneOrTwoTreasures_OnSeparateFloorsOfTheMiddleThird()
        {
            var seen = new HashSet<int>();
            foreach (var (chapter, map, seed) in Maps())
            {
                var floors = map.Floors.SelectMany(f => f).Where(n => n.Kind == NodeKind.Treasure).Select(n => n.Floor).ToList();
                Assert.That(floors.Count, Is.InRange(1, 2), $"chapter {chapter} seed {seed}");
                Assert.AreEqual(floors.Count, floors.Distinct().Count(), "separate floors");
                var (from, to) = MapMaker.TreasureWindow(map.FloorCount);
                Assert.That(floors.All(f => f >= from && f <= to), $"chapter {chapter} seed {seed}: the middle third");
                seen.Add(floors.Count);
            }
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, seen);
        }

        [Test]
        public void AMiddleCamp_EveryStartReachesOne_NeverTwoCampsOrMarketsInARow()
        {
            foreach (var (chapter, map, seed) in Maps())
            {
                var (from, to) = MapMaker.CampWindow(map.FloorCount);
                foreach (MapNode start in map.Floors[0])
                    Assert.That(Reach(map, start).Any(n => n.Kind == NodeKind.PurgatoryFire && n.Floor >= from && n.Floor <= to),
                        $"chapter {chapter} seed {seed}: no camp on the way from start {start.Lane}");
                foreach (MapNode node in map.Floors.SelectMany(f => f))
                foreach (MapNode next in map.NextFrom(node))
                {
                    Assert.IsFalse(node.Kind == NodeKind.BlackMarket && next.Kind == NodeKind.BlackMarket, $"chapter {chapter} seed {seed}: two markets in a row");
                    Assert.IsFalse(node.Kind == NodeKind.PurgatoryFire && next.Kind == NodeKind.PurgatoryFire, $"chapter {chapter} seed {seed}: two camps in a row");
                }
            }
            Assert.AreEqual((2, 4), MapMaker.CampWindow(8), "the first chapter: floors 3-5");
            Assert.AreEqual((3, 5), MapMaker.CampWindow(10), "the second: 4-6");
            Assert.AreEqual((4, 7), MapMaker.CampWindow(12), "the third: 5-8");
        }

        [Test]
        public void Wardens_FromTheFourthFloor_EveryStartReachesOne()
        {
            foreach (var (chapter, map, seed) in Maps())
            {
                var wardens = map.Floors.SelectMany(f => f).Where(n => n.Kind == NodeKind.Warden).ToList();
                Assert.That(wardens.All(w => w.Floor >= ChapterMap.WardenFromFloor && w.Floor < map.FloorCount - 1));
                Assert.GreaterOrEqual(wardens.Count, 1);
                foreach (MapNode start in map.Floors[0])
                    Assert.That(Reach(map, start).Any(n => n.Kind == NodeKind.Warden), $"chapter {chapter} seed {seed}: no warden from start {start.Lane}");
                Assert.That(map.Floors.Take(2).SelectMany(f => f).All(n => n.Kind != NodeKind.BlackMarket), "no market before the third floor");
            }
        }

        [Test]
        public void TheUnknown_HoldsAnEventATableTheMarketATreasureOrAWarden_ByItsOdds()
        {
            var held = new Dictionary<NodeKind, int>();
            foreach (var (_, map, _) in Maps(400))
            foreach (MapNode node in map.Floors.SelectMany(f => f).Where(n => n.Kind == NodeKind.Unknown))
            {
                held[node.Hidden] = (held.TryGetValue(node.Hidden, out int n) ? n : 0) + 1;
                Assert.AreEqual(node.Hidden, ChapterRun.KindOf(node));
                if (node.Floor < ChapterMap.WardenFromFloor) Assert.AreNotEqual(NodeKind.Warden, node.Hidden);
            }
            int all = held.Values.Sum();
            Assert.Greater(all, 500);
            Assert.AreEqual(55, 100.0 * held[NodeKind.Event] / all, 8, "events about 55%");
            Assert.AreEqual(20, 100.0 * held[NodeKind.Table] / all, 7, "tables about 20% (and the early wardens)");
            Assert.That(held.ContainsKey(NodeKind.BlackMarket) && held.ContainsKey(NodeKind.Treasure) && held.ContainsKey(NodeKind.Warden));
        }

        [Test]
        public void AnUnknownShowsWhatItHeld_OnceWalkedOrRevealed()
        {
            for (int seed = 0; seed < 300; seed++)
            {
                ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Peasant, seed);
                ChapterRun run = journey.Run;
                MapNode unknown = run.Map.Floors.SelectMany(f => f).FirstOrDefault(n => n.Kind == NodeKind.Unknown && n.Hidden != NodeKind.Event);
                if (unknown == null) continue;
                Assert.AreEqual(NodeKind.Unknown, run.ShownKind(unknown), "a ? until then");
                run.Reveal(unknown);
                Assert.AreEqual(unknown.Hidden, run.ShownKind(unknown));
                MapNode start = run.Map.Floors[0].FirstOrDefault(n => n.Kind == NodeKind.Unknown);
                if (start == null) continue;
                run.MoveTo(start);
                Assert.AreEqual(start.Hidden, run.CurrentKind, "stepped in: what it held");
                Assert.AreEqual(start.Hidden, run.ShownKind(start));
                return;
            }
            Assert.Fail("no seed with an unknown start");
        }

        [Test]
        public void TheSameSeedGivesTheSameMap_AnOlderSaveTheOlderMap()
        {
            ChapterRules rules = ChapterRules.For(2);
            string Draw(ChapterMap m) => string.Join(" ", m.Floors.SelectMany(f => f).Select(n => n + "/" + n.Hidden + "->" + string.Join(",", n.Next)));
            Assert.AreEqual(Draw(ChapterMap.Generate(rules, new SystemRandomSource(42))), Draw(ChapterMap.Generate(rules, new SystemRandomSource(42))));
            Assert.AreNotEqual(Draw(ChapterMap.Generate(rules, new SystemRandomSource(42))), Draw(ChapterMap.Generate(rules, new SystemRandomSource(43))));

            ChapterJourney journey = ChapterJourney.Begin(SinnerRoster.Peasant, 77);
            ChapterSave save = journey.Capture();
            Assert.AreEqual(ChapterMap.CurrentVersion, save.MapVersion);
            Assert.IsTrue(ChapterSave.TryDecode(save.Encode(), out ChapterSave read));
            Assert.AreEqual(Draw(journey.Run.Map), Draw(ChapterJourney.Restore(read).Run.Map), "a new run: the new map again");

            // A Demo 2.0 save (no "map" key): its map from Demo 2.0's generator, six nodes a floor.
            string old = string.Join("\n", save.Encode().Split('\n').Where(l => !l.StartsWith("map=", StringComparison.Ordinal)));
            Assert.IsTrue(ChapterSave.TryDecode(old, out ChapterSave older));
            Assert.AreEqual(1, older.MapVersion);
            ChapterJourney back = ChapterJourney.Restore(older);
            Assert.AreEqual(1, back.Run.Map.Version);
            Assert.That(back.Run.Map.Floors.All(f => f.Count == ChapterRules.For(1).Lanes));
            Assert.AreEqual(1, back.Capture().MapVersion, "and it keeps it");
        }
    }
}
