using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Randomness;

namespace HellPoker.Core.Chapters
{
    /// <summary>
    /// Demo 2.1's chapter map (<see cref="ChapterMap.CurrentVersion"/>): every floor 2-5 nodes, never as many as the floor above it; the
    /// first floor three (a table at least, the others tables, events or unknowns), the last a single camp before the gate. Paths split
    /// and join but never cross: between two floors they are a staircase from the leftmost pair to the rightmost, so every node leads on
    /// and every node is reached. One or two treasures on separate floors of the chapter's middle third; a camp in the middle
    /// (<see cref="CampWindow"/>) every start can reach; <see cref="ChapterRules.MaxWardens"/> wardens from the fourth floor on, at least
    /// one reachable from every start; never two black markets or two camps in a row on a path. An unknown node's content is rolled with
    /// the map (<see cref="UnknownWeights"/>). A node's lane is its place on its floor, left to right. The same seed gives the same map.
    /// </summary>
    internal static class MapMaker
    {
        public const int MinNodes = 2, MaxNodes = 5, FirstFloorNodes = 3;

        /// <summary>A map has a second treasure this often (percent).</summary>
        public const int SecondTreasurePercent = 70;

        /// <summary>What an unknown node holds (percent): an event, a table, the black market, a treasure, a warden.</summary>
        public static readonly (NodeKind kind, int weight)[] UnknownWeights =
        {
            (NodeKind.Event, 55), (NodeKind.Table, 20), (NodeKind.BlackMarket, 10), (NodeKind.Treasure, 10), (NodeKind.Warden, 5)
        };

        /// <summary>The floors (0-based, inclusive) of the middle camp: the first chapter's 3rd-5th, the second's 4th-6th, the third's 5th-8th.</summary>
        public static (int from, int to) CampWindow(int floors) => floors <= 8 ? (2, 4) : floors <= 10 ? (3, 5) : (4, 7);

        /// <summary>The floors (0-based, inclusive) of the chapter's middle third, where the treasures lie.</summary>
        public static (int from, int to) TreasureWindow(int floors) => (floors / 3, 2 * floors / 3 - 1);

        /// <summary>Demo 2.2 (version 3): on a path two markets, or two camps, have at least this many floors between them.</summary>
        public const int FloorsBetween = 2;

        /// <summary>Demo 2.2: a path holds at most this many black markets (the unknowns' counted).</summary>
        public const int MostMarketsOnAPath = 2;

        public static ChapterMap Make(ChapterRules rules, IRandomSource random, int version = ChapterMap.CurrentVersion)
        {
            bool spaced = version >= 3;
            int floors = Math.Max(4, rules.Floors);

            // The floors' sizes: three first, one camp last, 2-5 between (never the floor above's).
            var sizes = new int[floors];
            sizes[0] = FirstFloorNodes;
            sizes[floors - 1] = 1;
            for (int f = 1; f < floors - 1; f++)
            {
                int size;
                do size = MinNodes + random.Next(MaxNodes - MinNodes + 1);
                while (size == sizes[f - 1]);
                sizes[f] = size;
            }

            // The paths: a staircase between each floor and the next.
            var next = new List<int>[floors][];
            for (int f = 0; f < floors; f++)
            {
                next[f] = Enumerable.Range(0, sizes[f]).Select(_ => new List<int>()).ToArray();
                if (f == floors - 1) continue;
                int i = 0, j = 0, n = sizes[f], m = sizes[f + 1];
                next[f][0].Add(0);
                while (i < n - 1 || j < m - 1)
                {
                    // Step down the left floor, the right floor, or both (both most often: fewer, longer ways).
                    int roll = random.Next(100);
                    if (i < n - 1 && j < m - 1)
                    {
                        if (roll < 50) { i++; j++; }
                        else if (roll < 75) i++;
                        else j++;
                    }
                    else if (i < n - 1) i++;
                    else j++;
                    if (!next[f][i].Contains(j)) next[f][i].Add(j);
                }
            }

            var kinds = new NodeKind[floors][];
            for (int f = 0; f < floors; f++)
            {
                kinds[f] = new NodeKind[sizes[f]];
                for (int l = 0; l < sizes[f]; l++) kinds[f][l] = KindAt(f, floors, random);
            }
            kinds[0][random.Next(sizes[0])] = NodeKind.Table;   // a table among the starts, always
            kinds[floors - 1][0] = NodeKind.PurgatoryFire;

            // One or two treasures, on separate floors of the middle third.
            var (tFrom, tTo) = TreasureWindow(floors);
            int treasures = random.Next(100) < SecondTreasurePercent ? 2 : 1;
            var treasureFloors = Enumerable.Range(tFrom, tTo - tFrom + 1).OrderBy(_ => random.Next(1000)).Take(treasures).ToList();
            foreach (int f in treasureFloors) kinds[f][random.Next(sizes[f])] = NodeKind.Treasure;

            // The middle camp: every start reaches one; never next to another camp.
            var (cFrom, cTo) = CampWindow(floors);
            Cover(kinds, next, sizes, NodeKind.PurgatoryFire, f => f >= cFrom && f <= cTo, random, int.MaxValue, spaced ? FloorsBetween : 0);

            // The wardens: from the fourth floor on (never the last), every start reaches one, then more up to the chapter's number.
            int wardenLimit = Math.Max(1, rules.MaxWardens);
            Func<int, bool> wardenFloor = f => f >= ChapterMap.WardenFromFloor && f < floors - 1;
            int placed = Cover(kinds, next, sizes, NodeKind.Warden, wardenFloor, random, int.MaxValue);
            for (int tries = 0; placed < wardenLimit && tries < 50; tries++)
            {
                int f = ChapterMap.WardenFromFloor + random.Next(Math.Max(1, floors - 1 - ChapterMap.WardenFromFloor));
                int l = random.Next(sizes[f]);
                if (!Replaceable(kinds[f][l])) continue;
                kinds[f][l] = NodeKind.Warden;
                placed++;
            }

            // Never two markets in a row on a path: the later one becomes an event.
            for (int f = 0; f < floors - 1; f++)
                for (int l = 0; l < sizes[f]; l++)
                    if (kinds[f][l] == NodeKind.BlackMarket)
                        foreach (int j in next[f][l])
                            if (kinds[f + 1][j] == NodeKind.BlackMarket) kinds[f + 1][j] = NodeKind.Event;

            // The unknowns' contents, rolled in the nodes' order (as Demo 2.1 rolled them while building the nodes).
            var hidden = new NodeKind?[floors][];
            for (int f = 0; f < floors; f++)
            {
                hidden[f] = new NodeKind?[sizes[f]];
                for (int l = 0; l < sizes[f]; l++)
                    if (kinds[f][l] == NodeKind.Unknown) hidden[f][l] = HiddenAt(f, random);
            }
            if (spaced) SpaceTheMarkets(kinds, hidden, next, sizes, random);

            var map = new MapNode[floors][];
            for (int f = 0; f < floors; f++)
                map[f] = Enumerable.Range(0, sizes[f]).Select(l => new MapNode(f, l, kinds[f][l], next[f][l].OrderBy(x => x).ToArray(),
                    hidden[f][l])).ToArray();
            return ChapterMap.Of(map, Math.Max(2, version));
        }

        /// <summary>
        /// Demo 2.2: walking the floors down, a market (or an unknown holding one) that some path reaches with another market fewer than
        /// <see cref="FloorsBetween"/> floors above it — or with <see cref="MostMarketsOnAPath"/> behind it — becomes something else: a plain
        /// node an event, an unknown's content rolled again without the market. What every path carries is kept per node (the floor of its
        /// last market and how many it passed), so no path is missed.
        /// </summary>
        private static void SpaceTheMarkets(NodeKind[][] kinds, NodeKind?[][] hidden, List<int>[][] next, int[] sizes, IRandomSource random)
        {
            const int none = -100;
            var states = new HashSet<(int last, int count)>[kinds.Length][];
            for (int f = 0; f < kinds.Length; f++)
            {
                states[f] = new HashSet<(int, int)>[sizes[f]];
                for (int l = 0; l < sizes[f]; l++)
                {
                    var arriving = new HashSet<(int last, int count)>();
                    if (f == 0) arriving.Add((none, 0));
                    else
                        for (int p = 0; p < sizes[f - 1]; p++)
                            if (next[f - 1][p].Contains(l)) arriving.UnionWith(states[f - 1][p]);
                    bool market = IsMarket(kinds[f][l], hidden[f][l]);
                    if (market && arriving.Any(s => f - s.last <= FloorsBetween || s.count >= MostMarketsOnAPath))
                    {
                        if (kinds[f][l] == NodeKind.BlackMarket) kinds[f][l] = NodeKind.Event;
                        else
                        {
                            NodeKind again;
                            do again = HiddenAt(f, random);
                            while (again == NodeKind.BlackMarket);
                            hidden[f][l] = again;
                        }
                        market = false;
                    }
                    states[f][l] = market ? new HashSet<(int, int)>(arriving.Select(s => (f, s.count + 1))) : arriving;
                }
            }
        }

        /// <summary>A node that is a black market, open or behind a "?".</summary>
        public static bool IsMarket(NodeKind kind, NodeKind? hidden) => kind == NodeKind.BlackMarket || (kind == NodeKind.Unknown && hidden == NodeKind.BlackMarket);

        /// <summary>A plain node's kind on <paramref name="floor"/>: tables and events early, the market from the third floor, more of the
        /// market and the unknown late. The treasures, the camps and the wardens are placed apart.</summary>
        private static NodeKind KindAt(int floor, int floors, IRandomSource random)
        {
            bool late = floor >= floors / 2;
            var weights = new List<(NodeKind kind, int weight)>
            {
                (NodeKind.Table, late ? 25 : 55),
                (NodeKind.Event, late ? 25 : 25),
                (NodeKind.Unknown, late ? 25 : 10)
            };
            if (floor >= 2) weights.Add((NodeKind.BlackMarket, late ? 30 : 15));
            return Roll(weights, random);
        }

        /// <summary>An unknown node's content: <see cref="UnknownWeights"/> (no warden before the fourth floor, no market before the third).</summary>
        private static NodeKind HiddenAt(int floor, IRandomSource random)
        {
            NodeKind kind = Roll(UnknownWeights, random);
            if (kind == NodeKind.Warden && floor < ChapterMap.WardenFromFloor) return NodeKind.Table;
            if (kind == NodeKind.BlackMarket && floor < 2) return NodeKind.Event;
            return kind;
        }

        private static NodeKind Roll(IEnumerable<(NodeKind kind, int weight)> weights, IRandomSource random)
        {
            var list = weights.ToList();
            int roll = random.Next(list.Sum(w => w.weight));
            foreach (var (kind, weight) in list)
            {
                if (roll < weight) return kind;
                roll -= weight;
            }
            return list[0].kind;
        }

        /// <summary>A node a treasure, a camp or a warden may not take.</summary>
        private static bool Replaceable(NodeKind kind) => kind != NodeKind.Treasure && kind != NodeKind.PurgatoryFire && kind != NodeKind.Warden;

        /// <summary>
        /// Places <paramref name="kind"/> on the floors <paramref name="allowed"/> until every start reaches one: each time on the node that
        /// the most starts still without one reach (ties at random), never on a treasure, a camp or a warden, never (a camp) next to a camp.
        /// Returns how many there are.
        /// </summary>
        private static int Cover(NodeKind[][] kinds, List<int>[][] next, int[] sizes, NodeKind kind, Func<int, bool> allowed, IRandomSource random, int limit,
            int spacing = 0)
        {
            int floors = kinds.Length;
            int count = 0;
            for (int f = 0; f < floors; f++) count += kinds[f].Count(k => k == kind && allowed(f));
            for (int round = 0; round < 16 && count < limit; round++)
            {
                var reach = Enumerable.Range(0, sizes[0]).Select(s => Reach(next, sizes, s)).ToList();
                var uncovered = Enumerable.Range(0, sizes[0]).Where(s => !reach[s].Any(n => allowed(n.f) && kinds[n.f][n.l] == kind)).ToList();
                if (uncovered.Count == 0) break;
                var candidates = reach[uncovered[0]].Where(n => allowed(n.f) && Replaceable(kinds[n.f][n.l]) &&
                                                                (kind != NodeKind.PurgatoryFire || !NextToA(kinds, next, n, kind)) &&
                                                                (spacing == 0 || !NearA(kinds, next, n, kind, spacing))).ToList();
                if (candidates.Count == 0) break;
                int best = candidates.Max(n => uncovered.Count(s => reach[s].Contains(n)));
                var top = candidates.Where(n => uncovered.Count(s => reach[s].Contains(n)) == best).ToList();
                var (cf, cl) = top[random.Next(top.Count)];
                kinds[cf][cl] = kind;
                count++;
            }
            return count;
        }

        /// <summary>Every node a start leads to (floor, lane), the start included.</summary>
        private static HashSet<(int f, int l)> Reach(List<int>[][] next, int[] sizes, int start)
        {
            var all = new HashSet<(int f, int l)> { (0, start) };
            var here = new HashSet<int> { start };
            for (int f = 0; f + 1 < next.Length; f++)
            {
                here = new HashSet<int>(here.SelectMany(l => next[f][l]));
                foreach (int l in here) all.Add((f + 1, l));
            }
            return all;
        }

        /// <summary>A path joins the node to one of <paramref name="kind"/> within <paramref name="span"/> floors above or below it.</summary>
        private static bool NearA(NodeKind[][] kinds, List<int>[][] next, (int f, int l) node, NodeKind kind, int span)
        {
            // Down: the nodes the node leads to, floor by floor.
            var here = new HashSet<int> { node.l };
            for (int f = node.f; f < node.f + span && f + 1 < kinds.Length; f++)
            {
                here = new HashSet<int>(here.SelectMany(l => next[f][l]));
                if (here.Any(l => kinds[f + 1][l] == kind)) return true;
            }
            // Up: the nodes that lead to it.
            here = new HashSet<int> { node.l };
            for (int f = node.f; f > node.f - span && f > 0; f--)
            {
                var above = new HashSet<int>(Enumerable.Range(0, kinds[f - 1].Length).Where(p => next[f - 1][p].Any(here.Contains)));
                if (above.Any(p => kinds[f - 1][p] == kind)) return true;
                here = above;
            }
            return false;
        }

        /// <summary>A path joins the node to one of <paramref name="kind"/> just above or below it.</summary>
        private static bool NextToA(NodeKind[][] kinds, List<int>[][] next, (int f, int l) node, NodeKind kind)
        {
            if (node.f + 1 < kinds.Length && next[node.f][node.l].Any(j => kinds[node.f + 1][j] == kind)) return true;
            if (node.f == 0) return false;
            for (int l = 0; l < kinds[node.f - 1].Length; l++)
                if (kinds[node.f - 1][l] == kind && next[node.f - 1][l].Contains(node.l)) return true;
            return false;
        }
    }
}
