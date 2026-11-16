using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ElementalTactics
{
    public sealed class MapNode
    {
        public readonly int Id, Row, Lane, Floor;
        public NodeRecord Definition { get; internal set; }
        public string Title => Definition.name;
        public string Description => Definition.description;
        public readonly float X, Y;
        public readonly int Bend;
        public readonly bool HasLayout;
        public readonly List<int> Next = new List<int>();
        public MapNode(int id, int row, int lane, NodeRecord definition, int floor = 1, float x = 0, float y = 0, int bend = 0, bool hasLayout = false)
        { Id = id; Row = row; Lane = lane; Definition = definition; Floor = floor; X = x; Y = y; Bend = bend; HasLayout = hasLayout; }
    }
    public static partial class MapRun
    {
        public const int BaseFloorCount = 3;
        public const int FloorCount = 4;
        public const int RowsPerFloor = 7;
        public const int NodesPerFloor = 24;
        private static IReadOnlyList<MapNode> nodes;
        public static IReadOnlyList<MapNode> Nodes => nodes ?? (nodes = CreateNodes());
        private static readonly HashSet<int> cleared = new HashSet<int>();
        private static readonly HashSet<string> enteredEvents = new HashSet<string>();
        public static int Current { get; private set; } = -1;
        public static int Pending { get; private set; } = -1;
        public static int ClearedCount => cleared.Count;
        public static int CompletedAtEntry { get; private set; }
        public static int Seed { get; private set; }
        public static bool Finished => TutorialRun.Active ? TutorialRun.Finished : EndingRun.Completed != null;
        public static int CurrentFloor => TutorialRun.Active || Current < 0 ? 1 : !Finished && Nodes[Current].Definition.isFinal ? Nodes[Current].Floor + 1 : Nodes[Current].Floor;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { ResetWithSeed(Environment.TickCount); }
        public static void ResetWithSeed(int seed)
        {
            AdventureRun.ResetCurrent();
            InventoryState.Reset();
            RunState.Reset();
            EndingRun.Reset();
            nodes = null;
            Seed = seed;
            Current = Pending = -1;
            CompletedAtEntry = 0;
            cleared.Clear();
            enteredEvents.Clear();
        }
        public static bool IsCleared(int id) => cleared.Contains(id);
        public static bool HasEnteredEvent(string eventId) => enteredEvents.Contains(eventId);
        public static bool CanEnter(int id)
        {
            if (RunState.IsDefeated || id < 0 || id >= Nodes.Count || Finished || cleared.Contains(id)) return false;
            if (id != Pending && Nodes[id].Definition.type == NodeKind.Event && IsSingleEntryEvent(Nodes[id].Definition.contentId)
                && HasEnteredEvent(Nodes[id].Definition.contentId)) return false;
            if (Nodes[id].Floor > BaseFloorCount && !EndingRun.ExtraFloorUnlocked(Nodes[id].Floor)) return false;
            RefreshFinalBoss();
            return Current < 0 ? Nodes[id].Floor == 1 && Nodes[id].Row == 0 : Nodes[Current].Next.Contains(id);
        }
        public static bool TryEnter(int id)
        {
            if (Pending >= 0 || !CanEnter(id)) return false;
            Pending = id;
            CompletedAtEntry = ClearedCount;
            if (Nodes[id].Definition.type == NodeKind.Battle) EndingRun.BeginBattle(id);
            if (Nodes[id].Definition.type == NodeKind.Event && IsSingleEntryEvent(Nodes[id].Definition.contentId))
            {
                enteredEvents.Add(Nodes[id].Definition.contentId);
                ReplaceEnteredEvents(id);
            }
            return true;
        }
        public static bool Complete(int id)
        {
            if (id != Pending || !CanEnter(id)) return false;
            cleared.Add(id);
            Current = id;
            Pending = -1;
            EndingRun.Complete(Nodes[id]);
            return true;
        }
        private static void RefreshFinalBoss()
        {
            if (TutorialRun.Active) return;
            var final = Nodes.First(node => node.Floor == BaseFloorCount && node.Definition.isFinal);
            if (Pending == final.Id || IsCleared(final.Id)) return;
            final.Definition = GameContent.Nodes.Get(EndingRun.BaseBoss.bossNodeId);
        }
        private static bool IsSingleEntryEvent(string eventId) => GameContent.Nodes.rows.Any(entry =>
            entry.type == NodeKind.Event && entry.contentId == eventId && entry.uniquePerRun);
        private static void ReplaceEnteredEvents(int activeNode)
        {
            foreach (var node in Nodes.Where(entry => entry.Id != activeNode && !IsCleared(entry.Id)
                && entry.Definition.type == NodeKind.Event && HasEnteredEvent(entry.Definition.contentId)))
            {
                var candidates = GameContent.Nodes.rows.Where(entry => entry.type == NodeKind.Event && !entry.isFinal
                    && !entry.guaranteed && !IsSingleEntryEvent(entry.contentId)
                    && entry.minFloor <= node.Floor && entry.maxFloor >= node.Floor
                    && entry.minRow <= node.Row + 1 && entry.maxRow >= node.Row + 1).ToArray();
                if (candidates.Length == 0) throw new InvalidOperationException("缺少特殊事件的普通替代事件");
                var random = new System.Random(unchecked(Seed * 397 ^ node.Id * 7919));
                double roll = random.NextDouble() * candidates.Sum(entry => (double)entry.weight);
                node.Definition = candidates[candidates.Length - 1];
                foreach (var entry in candidates) { roll -= entry.weight; if (roll < 0) { node.Definition = entry; break; } }
            }
        }
        public static void LeaveEncounter()
        {
            if (RewardFlow.Pending != null) return;
            Pending = -1;
            if (enteredEvents.Count > 0) ReplaceEnteredEvents(-1);
        }
        private static IReadOnlyList<MapNode> CreateNodes()
        {
            if (TutorialRun.Active) return TutorialContent.CreateMap();
            var result = new List<MapNode>();
            var random = new System.Random(Seed);
            var alternateBosses = new HashSet<string>(GameContent.Endings.rows.Where(entry => entry.priority > 0).Select(entry => entry.bossNodeId));
            foreach (var floor in GameContent.Floors.rows.OrderBy(entry => entry.floor))
            {
                var layout = MapLayout.ForFloor(floor.floor, Seed);
                int finalRow = layout.Max(point => point.Row);
                if (floor.rows != finalRow + 1) throw new InvalidOperationException("地图模板与楼层排数不匹配");
                int start = result.Count, shops = 0;
                var guaranteed = GameContent.Nodes.rows.Where(entry => entry.guaranteed && entry.minFloor == floor.floor).ToArray();
                for (int row = 0; row <= finalRow; row++)
                {
                    var indices = Enumerable.Range(0, layout.Length).Where(index => layout[index].Row == row).ToArray();
                    var used = new HashSet<string>();
                    int battleLane = random.Next(indices.Length);
                    int eventLane = (battleLane + 1) % indices.Length;
                    var forced = guaranteed.FirstOrDefault(entry => entry.minRow == row + 1);
                    int lastShopRow = Math.Min(4, finalRow - 1);
                    for (int lane = 0; lane < indices.Length; lane++)
                    {
                        int local = indices[lane];
                        bool final = row == finalRow;
                        bool forceEvent = forced != null && lane == eventLane && !final;
                        bool followsShop = result.Skip(start).Any(previous => previous.Definition.type == NodeKind.Shop && previous.Next.Contains(start + local));
                        NodeKind kind = NodeKind.Battle;
                        if (forceEvent) kind = NodeKind.Event;
                        else if (row > 0 && !final && lane != battleLane)
                        {
                            int shopWeight = row >= 2 && row <= lastShopRow && shops < floor.maxShops && !followsShop ? floor.shopWeight : 0;
                            int roll = random.Next(floor.battleWeight + floor.eventWeight + shopWeight);
                            kind = roll < floor.battleWeight ? NodeKind.Battle : roll < floor.battleWeight + floor.eventWeight ? NodeKind.Event : NodeKind.Shop;
                        }
                        if (row == lastShopRow && lane != battleLane && shops == 0 && floor.maxShops > 0 && !forceEvent && !followsShop) kind = NodeKind.Shop;
                        var eligible = GameContent.Nodes.rows.Where(entry => entry.type == kind && entry.isFinal == final
                            && entry.minFloor <= floor.floor && entry.maxFloor >= floor.floor
                            && entry.minRow <= row + 1 && entry.maxRow >= row + 1
                            && !alternateBosses.Contains(entry.id)).ToList();
                        if (forceEvent) eligible = new List<NodeRecord> { forced };
                        if (final && floor.optional) eligible = GameContent.Nodes.rows.Where(entry => alternateBosses.Contains(entry.id)
                            && entry.minFloor == floor.floor).ToList();
                        eligible = eligible.Where(entry => entry.type != NodeKind.Battle
                            || StageRules.FitsEarlyNode(GameContent.Battles.Get(entry.contentId), floor.floor, row + 1, GameContent.Floors.rows)).ToList();
                        var unused = eligible.Where(entry => !used.Contains(entry.id)).ToList();
                        if (unused.Count > 0) eligible = unused;
                        if (eligible.Count == 0) throw new InvalidOperationException("节点配置不足：第" + floor.floor + "层第" + (row + 1) + "排 " + kind);
                        double rollDefinition = random.NextDouble() * eligible.Sum(entry => (double)entry.weight);
                        var chosen = eligible[eligible.Count - 1];
                        foreach (var entry in eligible) { rollDefinition -= entry.weight; if (rollDefinition < 0) { chosen = entry; break; } }
                        used.Add(chosen.id);
                        if (kind == NodeKind.Shop) shops++;
                        var point = layout[local];
                        var node = new MapNode(result.Count, row, lane, chosen, floor.floor, point.X, point.Y, point.Bend, true);
                        node.Next.AddRange(point.Next.Select(target => start + target));
                        result.Add(node);
                    }
                }
                if (start > 0)
                    foreach (var entry in result.Skip(start).Where(node => node.Row == 0)) result[start - 1].Next.Add(entry.Id);
            }
            return result.AsReadOnly();
        }
    }
}
