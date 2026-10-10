using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Core.Randomness;

namespace HellPoker.Core.Chapters
{
    /// <summary>What waits at a node of a chapter's map.</summary>
    public enum NodeKind
    {
        /// <summary>An imp's table: a match for coins until a purse is empty (the imp's: <see cref="ChapterRules.ImpCoins"/>).</summary>
        Table,

        /// <summary>A stranger's offer (the chapter's events).</summary>
        Event,

        /// <summary>The black market: cursed relics and services for coins.</summary>
        BlackMarket,

        /// <summary>A warden: a richer purse, a demon's temper and minor cheats; a relic for emptying it.</summary>
        Warden,

        /// <summary>Coins on the floor (the fifth floor, always).</summary>
        Treasure,

        /// <summary>The purgatory fire (the last floor, always): one boon of three.</summary>
        PurgatoryFire
    }

    /// <summary>One node: its floor (0 is the first), its lane, what waits there and the lanes of the next floor it leads to.</summary>
    public sealed class MapNode
    {
        public int Floor { get; }
        public int Lane { get; }
        public NodeKind Kind { get; }
        public IReadOnlyList<int> Next { get; }

        public MapNode(int floor, int lane, NodeKind kind, IReadOnlyList<int> next)
        {
            Floor = floor;
            Lane = lane;
            Kind = kind;
            Next = next ?? Array.Empty<int>();
        }

        public override string ToString() => $"F{Floor + 1}L{Lane}:{Kind}";
    }

    /// <summary>
    /// A chapter's map: <see cref="ChapterRules.Floors"/> floors of <see cref="ChapterRules.Lanes"/> nodes. The first floor is all
    /// tables (the player picks where to start), the fifth all treasure, the last all purgatory fire. In between: tables and events
    /// early, the black market from the third floor, the hardest mix on the sixth and seventh. <see cref="ChapterRules.MaxWardens"/>
    /// wardens stand from the fourth floor on, one in each group of neighbouring lanes, and every start has a path to one. Every node
    /// leads straight on, sometimes also one lane aside; paths never cross. The same seed gives the same map.
    /// </summary>
    public sealed class ChapterMap
    {
        /// <summary>The floor (0-based) of the treasure: the fifth.</summary>
        public const int TreasureFloor = 4;

        /// <summary>A node leads one lane aside this often (each side).</summary>
        public const int BranchPercent = 35;

        private readonly MapNode[][] _floors;

        public IReadOnlyList<IReadOnlyList<MapNode>> Floors => _floors;

        public int FloorCount => _floors.Length;

        public MapNode this[int floor, int lane] => _floors[floor][lane];

        private ChapterMap(MapNode[][] floors)
        {
            _floors = floors;
        }

        /// <summary>The nodes a player at <paramref name="node"/> may go to next (none from the last floor).</summary>
        public IEnumerable<MapNode> NextFrom(MapNode node) =>
            node.Floor + 1 >= _floors.Length ? Enumerable.Empty<MapNode>() : node.Next.Select(lane => _floors[node.Floor + 1][lane]);

        public int Count(NodeKind kind) => _floors.Sum(f => f.Count(n => n.Kind == kind));

        public static ChapterMap Generate(ChapterRules rules, IRandomSource random)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (random == null) throw new ArgumentNullException(nameof(random));

            int floors = rules.Floors, lanes = rules.Lanes;
            var kinds = new NodeKind[floors][];
            for (int f = 0; f < floors; f++)
            {
                kinds[f] = new NodeKind[lanes];
                for (int l = 0; l < lanes; l++) kinds[f][l] = KindAt(f, floors, random);
            }

            // The wardens: one in each group of neighbouring lanes, on a floor from the fourth on (not the treasure's, not the fire's).
            int[] wardenFloors = Enumerable.Range(WardenFromFloor, floors - 1 - WardenFromFloor).Where(f => f != TreasureFloor).ToArray();
            var wardens = new List<(int floor, int lane)>();
            for (int g = 0; g < rules.MaxWardens; g++)
            {
                int from = g * lanes / rules.MaxWardens, to = (g + 1) * lanes / rules.MaxWardens;
                var spot = (floor: wardenFloors[random.Next(wardenFloors.Length)], lane: from + random.Next(to - from));
                kinds[spot.floor][spot.lane] = NodeKind.Warden;
                wardens.Add(spot);
            }

            // The paths: straight on always; one lane aside sometimes — never across a neighbour's path the other way.
            var next = new List<int>[floors][];
            for (int f = 0; f < floors; f++)
            {
                next[f] = new List<int>[lanes];
                for (int l = 0; l < lanes; l++) next[f][l] = new List<int> { l };
                if (f == floors - 1)
                {
                    foreach (List<int> list in next[f]) list.Clear();
                    continue;
                }
                for (int l = 0; l < lanes; l++)
                {
                    if (l > 0 && !next[f][l - 1].Contains(l) && random.Next(100) < BranchPercent) next[f][l].Add(l - 1);
                    if (l < lanes - 1 && random.Next(100) < BranchPercent) next[f][l].Add(l + 1);
                }
            }

            // Every start can reach a warden: a start that cannot is given the side steps to its own group's warden (at most
            // a group's width, before the warden's floor). A side step never crosses a neighbour's: that one goes (straight on stays).
            for (int round = 0; round < 4 * lanes; round++)
            {
                int lost = Enumerable.Range(0, lanes).Where(s => !ReachesAny(next, s, wardens)).DefaultIfEmpty(-1).First();
                if (lost < 0) break;
                var (wf, wl) = wardens[lost * rules.MaxWardens / lanes];
                int dir = Math.Sign(wl - lost), steps = Math.Abs(wl - lost);
                var stepFloors = new HashSet<int>(Enumerable.Range(0, wf).OrderBy(_ => random.Next(1000)).Take(steps));
                for (int f = 0, x = lost; f < wf && x != wl; f++)
                {
                    if (!stepFloors.Contains(f)) continue;
                    next[f][x + dir].Remove(x);
                    if (!next[f][x].Contains(x + dir)) next[f][x].Add(x + dir);
                    x += dir;
                }
            }

            var map = new MapNode[floors][];
            for (int f = 0; f < floors; f++)
                map[f] = Enumerable.Range(0, lanes).Select(l => new MapNode(f, l, kinds[f][l], next[f][l].OrderBy(x => x).ToArray())).ToArray();
            return new ChapterMap(map);
        }

        /// <summary>Wardens stand from this floor (0-based: the fourth) on.</summary>
        public const int WardenFromFloor = 3;

        private static bool ReachesAny(List<int>[][] next, int start, List<(int floor, int lane)> targets)
        {
            var here = new HashSet<int> { start };
            for (int f = 0; f < next.Length; f++)
            {
                if (targets.Any(t => t.floor == f && here.Contains(t.lane))) return true;
                here = new HashSet<int>(here.SelectMany(l => next[f][l]));
            }
            return false;
        }

        /// <summary>What a node on floor <paramref name="floor"/> holds (the weights: tables and events early, the hardest mix late; the
        /// wardens are placed apart).</summary>
        private static NodeKind KindAt(int floor, int floors, IRandomSource random)
        {
            if (floor == 0) return NodeKind.Table;
            if (floor == TreasureFloor) return NodeKind.Treasure;
            if (floor == floors - 1) return NodeKind.PurgatoryFire;

            bool late = floor > TreasureFloor;
            var weights = new List<(NodeKind kind, int weight)>
            {
                (NodeKind.Table, late ? 20 : 55),
                (NodeKind.Event, late ? 30 : 30)
            };
            if (floor >= 2) weights.Add((NodeKind.BlackMarket, late ? 25 : 15));

            int roll = random.Next(weights.Sum(w => w.weight));
            foreach (var (kind, weight) in weights)
            {
                if (roll < weight) return kind;
                roll -= weight;
            }
            return NodeKind.Table;
        }
    }
}
