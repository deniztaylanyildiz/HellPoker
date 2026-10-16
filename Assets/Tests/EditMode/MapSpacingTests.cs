using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Chapters;
using HellPoker.Core.Randomness;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    /// <summary>Demo 2.2's map (version 3): on no path two black markets (an unknown's counted) or two camps with fewer than two floors
    /// between them, and no path with more than two markets — over ten thousand seeds, every chapter, every circle.</summary>
    public class MapSpacingTests
    {
        private static bool IsMarket(MapNode n) => MapMaker.IsMarket(n.Kind, n.Hidden);

        /// <summary>Every path's breach, found by carrying each path's last market, market count and last camp down the floors.</summary>
        private static string Breach(ChapterMap map)
        {
            const int none = -100;
            var states = new Dictionary<MapNode, HashSet<(int market, int markets, int camp)>>();
            foreach (MapNode start in map.Floors[0]) states[start] = new HashSet<(int, int, int)> { (none, 0, none) };
            for (int f = 0; f < map.FloorCount; f++)
                foreach (MapNode node in map.Floors[f])
                {
                    var arriving = states[node];
                    var leaving = new HashSet<(int, int, int)>();
                    foreach (var (market, markets, camp) in arriving)
                    {
                        int m = market, count = markets, c = camp;
                        if (IsMarket(node))
                        {
                            if (f - market <= MapMaker.FloorsBetween) return $"two markets, floors {market + 1} and {f + 1}";
                            if (markets >= MapMaker.MostMarketsOnAPath) return $"a third market on floor {f + 1}";
                            m = f;
                            count++;
                        }
                        if (node.Kind == NodeKind.PurgatoryFire)
                        {
                            if (f - camp <= MapMaker.FloorsBetween) return $"two camps, floors {camp + 1} and {f + 1}";
                            c = f;
                        }
                        leaving.Add((m, count, c));
                    }
                    foreach (MapNode on in map.NextFrom(node))
                    {
                        if (!states.TryGetValue(on, out var set)) states[on] = set = new HashSet<(int, int, int)>();
                        set.UnionWith(leaving);
                    }
                }
            return null;
        }

        [Test]
        public void TenThousandSeeds_NoPathBreaksTheSpacing_InAnyChapterOrCircle()
        {
            int maps = 0, markets = 0, unknownMarkets = 0, startsWithoutAMiddleCamp = 0, starts = 0;
            for (int chapter = 1; chapter <= 3; chapter++)
                for (int circle = 0; circle <= Circles.Deepest; circle++)
                {
                    ChapterRules rules = ChapterRules.For(chapter).WithCircle(circle);
                    var (cFrom, cTo) = MapMaker.CampWindow(rules.Floors);
                    for (int seed = 0; seed < 10000; seed++)
                    {
                        ChapterMap map = ChapterMap.Generate(rules, new SystemRandomSource(RandomSeeds.Derive(seed, chapter * 10 + circle)));
                        Assert.AreEqual(3, map.Version);
                        string breach = Breach(map);
                        Assert.IsNull(breach, $"chapter {chapter} circle {circle} seed {seed}: {breach}");
                        maps++;
                        markets += map.Floors.Sum(f => f.Count(n => n.Kind == NodeKind.BlackMarket));
                        unknownMarkets += map.Floors.Sum(f => f.Count(n => n.Kind == NodeKind.Unknown && n.Hidden == NodeKind.BlackMarket));
                        foreach (MapNode start in map.Floors[0])
                        {
                            starts++;
                            var reach = new HashSet<MapNode> { start };
                            var here = new List<MapNode> { start };
                            while (here.Count > 0)
                            {
                                here = here.SelectMany(map.NextFrom).Distinct().ToList();
                                reach.UnionWith(here);
                            }
                            if (!reach.Any(n => n.Kind == NodeKind.PurgatoryFire && n.Floor >= cFrom && n.Floor <= cTo)) startsWithoutAMiddleCamp++;
                        }
                    }
                }
            TestContext.Out.WriteLine($"{maps} maps: markets {markets / (double)maps:0.00} a map (+{unknownMarkets / (double)maps:0.00} behind a \"?\"), " +
                                      $"starts without a middle camp {100.0 * startsWithoutAMiddleCamp / starts:0.00}%");
            Assert.Greater(markets, maps, "the markets are still there (more than one a map on average)");
            Assert.Less(startsWithoutAMiddleCamp, starts / 100, "the middle camp still reaches (almost) every start");
        }

        [Test]
        public void AnOlderSavesMap_ComesBackUnchanged_FromItsOwnGenerator()
        {
            // Version 2 (Demo 2.1) still makes Demo 2.1's map: a save's map never changes under it.
            ChapterRules rules = ChapterRules.For(2);
            string Draw(ChapterMap map) => string.Join("|", map.Floors.Select(f => string.Join(" ", f.Select(n => $"{n.Kind}{n.Hidden}>{string.Join(",", n.Next)}"))));
            ChapterMap a = ChapterMap.Generate(rules, new SystemRandomSource(77), version: 2), b = ChapterMap.Generate(rules, new SystemRandomSource(77), version: 2);
            Assert.AreEqual(Draw(a), Draw(b));
            Assert.AreEqual(2, a.Version);
        }
    }
}
