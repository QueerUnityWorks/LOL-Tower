using System;
using System.Collections.Generic;
using System.Linq;

namespace ElementalTactics
{
    public static partial class EndingRun
    {
        private static readonly HashSet<int> rewardedBattles = new HashSet<int>();
        private static readonly HashSet<string> battleContracts = new HashSet<string>();
        private static int battleNodeId = -1;
        public static EndingRecord Completed { get; private set; }
        public static EndingRecord Active => GameContent.Endings.rows.OrderByDescending(entry => entry.priority)
            .First(entry => Eligible(entry));
        public static void Reset()
        {
            rewardedBattles.Clear();
            battleContracts.Clear();
            battleNodeId = -1;
            Completed = null;
        }
        public static EndingRecord BaseBoss => GameContent.Endings.rows
            .Where(entry => entry.extraFloor == 0 && Eligible(entry)).OrderByDescending(entry => entry.priority).First();
        public static void BeginBattle(int nodeId)
        {
            battleNodeId = nodeId;
            battleContracts.Clear();
            foreach (var ending in GameContent.Endings.rows.Where(entry => entry.requiredWins > 0 && Quantity(entry.requiredItemId) > 0))
                battleContracts.Add(ending.id);
        }
        public static int Quantity(string id) => string.IsNullOrEmpty(id) ? 0 : InventoryState.Items.FirstOrDefault(item => item.Id == id)?.Quantity ?? 0;
        public static bool Eligible(EndingRecord ending) => string.IsNullOrEmpty(ending.requiredItemId)
            || Quantity(ending.requiredItemId) > 0 && (ending.requiredWins == 0 || Quantity(ending.fragmentItemId) >= ending.requiredWins);
        public static bool ExtraFloorUnlocked(int floor) => GameContent.Endings.rows.Any(ending => ending.extraFloor == floor && Eligible(ending));
        public static EffectRecord[] VictoryDrops(int nodeId)
        {
            if (nodeId != battleNodeId || nodeId != MapRun.Pending || nodeId < 0 || MapRun.IsCleared(nodeId)
                || MapRun.Nodes[nodeId].Definition.type != NodeKind.Battle || rewardedBattles.Contains(nodeId)) return Array.Empty<EffectRecord>();
            return GameContent.Endings.rows.Where(ending => battleContracts.Contains(ending.id) && Quantity(ending.requiredItemId) > 0
                && Quantity(ending.fragmentItemId) < ending.requiredWins).Select(ending => new EffectRecord
                { type = EffectKind.GiveItem, targetId = ending.fragmentItemId, amount = 1 }).ToArray();
        }
        public static bool AwardVictory(int nodeId, EffectRecord[] drops)
        {
            if (nodeId != battleNodeId || nodeId != MapRun.Pending || nodeId < 0 || MapRun.IsCleared(nodeId)
                || MapRun.Nodes[nodeId].Definition.type != NodeKind.Battle || rewardedBattles.Contains(nodeId)) return false;
            var expected = VictoryDrops(nodeId);
            if (drops == null || drops.Length != expected.Length || drops.Where((drop, index) => drop == null
                || drop.type != expected[index].type || drop.targetId != expected[index].targetId || drop.amount != expected[index].amount).Any()) return false;
            if (!RunState.Apply(drops)) return false;
            rewardedBattles.Add(nodeId);
            return true;
        }
        public static void Complete(MapNode node)
        {
            if (!node.Definition.isFinal || node.Floor < MapRun.BaseFloorCount) return;
            if (node.Floor == MapRun.BaseFloorCount && ExtraFloorUnlocked(MapRun.BaseFloorCount + 1)) return;
            Completed = GameContent.Endings.rows.Single(ending => ending.bossNodeId == node.Definition.id);
        }
        public static string ProgressLabel
        {
            get
            {
                if (Completed != null) return "结局 · " + Completed.name;
                var quest = GameContent.Endings.rows.FirstOrDefault(ending => ending.requiredWins > 0 && Quantity(ending.requiredItemId) > 0);
                if (quest != null) return Eligible(quest) ? (MapRun.CurrentFloor >= quest.extraFloor
                    ? "深入星界封印 · 挑战失控奥瑞利安·索尔" : "第四层已解锁 · 击败第三层守关后前往")
                    : GameContent.Items.Get(quest.fragmentItemId).name + " " + Quantity(quest.fragmentItemId) + " / " + quest.requiredWins + " · 持契约战胜敌人收集";
                var active = Active;
                return active.priority > 0 ? "命运分支 · " + active.name : "旅途中的抉择，可能改变最终的敌人";
            }
        }
    }
}
