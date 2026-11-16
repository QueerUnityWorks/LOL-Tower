using System;
using System.Collections.Generic;
using System.Linq;

namespace ElementalTactics
{
    public static class StageRules
    {
        public static int CumulativeNode(int floor, int row, IEnumerable<FloorRecord> floors)
            => floors.Where(f => f.floor < floor).Sum(f => f.rows) + row;

        public static bool FitsEarlyNode(BattleRecord battle, int floor, int row, IEnumerable<FloorRecord> floors)
        {
            int node = CumulativeNode(floor, row, floors);
            return node > 4 || battle.Slots.Count(id => !string.IsNullOrEmpty(id)) <= node;
        }

        public static int MinimumPopulation(BattleRecord battle, IEnumerable<NodeRecord> nodes,
            IEnumerable<FloorRecord> floors)
        {
            var floorRows = floors.ToArray();
            int latestNode = nodes.Where(n => n.type == NodeKind.Battle && n.contentId == battle.id)
                .SelectMany(n => floorRows.Where(f => f.floor >= n.minFloor && f.floor <= n.maxFloor)
                    .Select(f => floorRows.Where(previous => previous.floor < f.floor).Sum(previous => previous.rows)
                        + (n.isFinal ? f.rows : Math.Min(n.maxRow, f.rows - 1))))
                .DefaultIfEmpty(1).Max();
            return Math.Min(6, Math.Max(1, latestNode));
        }

        public static void ValidatePopulation(BattleRecord battle, IEnumerable<NodeRecord> nodes,
            IEnumerable<FloorRecord> floors, Func<string, string> enemyQuality)
        {
            var ids = battle.Slots.Where(id => !string.IsNullOrEmpty(id)).ToArray();
            var nodeRows = nodes.ToArray();
            var floorRows = floors.ToArray();
            foreach (var node in nodeRows.Where(n => n.type == NodeKind.Battle && n.contentId == battle.id))
                foreach (var floor in floorRows.Where(f => f.floor >= node.minFloor && f.floor <= node.maxFloor))
                {
                    int first = node.isFinal ? floor.rows : node.minRow;
                    int last = node.isFinal ? floor.rows : Math.Min(node.maxRow, floor.rows - 1);
                    for (int row = first; row <= last; row++)
                        if (!FitsEarlyNode(battle, floor.floor, row, floorRows))
                            throw new InvalidOperationException(battle.id + "：前4个累计节点的敌人数不得超过节点数，"
                                + "第" + CumulativeNode(floor.floor, row, floorRows) + "节点配置了" + ids.Length + "名敌人（含精英和boss）。");
                }
            bool hasSpecialEnemy = false;
            foreach (string id in ids)
            {
                string quality = enemyQuality(id);
                if (quality == "精英" || string.Equals(quality, "boss", StringComparison.OrdinalIgnoreCase))
                    hasSpecialEnemy = true;
                else if (quality != "普通")
                    throw new InvalidOperationException(id + "：缺少有效敌人分类，请同步手册中的普通、精英或boss分类。");
            }
            int minimum = MinimumPopulation(battle, nodeRows, floorRows);
            if (!hasSpecialEnemy && ids.Length < minimum)
                throw new InvalidOperationException(battle.id + "：仅含普通敌人时，数量须不少于min(6,可出现的最晚累计节点序号)，至少"
                    + minimum + "名，当前为" + ids.Length + "名。累计节点序号包含前面所有楼层的节点。");
        }
    }
}
