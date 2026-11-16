using System;
using System.Collections.Generic;
using System.Linq;

namespace ElementalTactics
{
    public sealed class PendingReward
    {
        public int NodeId { get; internal set; }
        public int Gold { get; internal set; }
        public int Experience { get; internal set; }
        public int ChooseCount { get; internal set; }
        public double EnemyPower { get; internal set; }
        public string DifficultyName { get; internal set; }
        public EffectRecord[] Candidates { get; internal set; }
        public EffectRecord[] GuaranteedDrops { get; internal set; }
    }
    public static partial class RewardFlow
    {
        public static PendingReward Pending { get; private set; }
        public static void Reset() { Pending = null; }
        public static PendingReward Victory(int nodeId)
        {
            if (RunState.IsDefeated) return null;
            if (Pending != null) return Pending.NodeId == nodeId ? Pending : null;
            if (nodeId != MapRun.Pending || nodeId < 0 || MapRun.IsCleared(nodeId)) return null;
            var node = MapRun.Nodes[nodeId].Definition;
            if (node.type != NodeKind.Battle) return null;
            var battle = GameContent.Battles.Get(node.contentId);
            var reward = GameContent.Rewards.Get(battle.rewardId);
            double power = CombatPower.RewardPower(MapRun.Nodes[nodeId]);
            var difficulty = CombatPower.Difficulty(power);
            string itemPool = string.IsNullOrEmpty(battle.itemPoolId) ? difficulty.itemPoolId : battle.itemPoolId;
            string characterPool = string.IsNullOrEmpty(battle.characterPoolId) ? difficulty.characterPoolId : battle.characterPoolId;
            int itemCount = battle.itemCandidates < 0 ? difficulty.itemCandidates : battle.itemCandidates;
            int characterCount = battle.characterCandidates < 0 ? difficulty.characterCandidates : battle.characterCandidates;
            int choices = battle.chooseCount < 0 ? difficulty.chooseCount : battle.chooseCount;
            var random = new Random(unchecked(MapRun.Seed * 397 ^ nodeId * 7919));
            int experience = reward.experienceBase + reward.experiencePerNode * MapRun.CompletedAtEntry;
            var guaranteed = EndingRun.VictoryDrops(nodeId);
            // Validate the guaranteed grant before changing progression or generating the reward.
            if (!RunState.CanApply(guaranteed)) return null;
            if (!EndingRun.AwardVictory(nodeId, guaranteed)) return null;
            Progression.AddExperience(experience);
            var candidates = new List<EffectRecord>();
            Draw(itemPool, itemCount, random, candidates);
            Draw(characterPool, characterCount, random, candidates);
            Pending = new PendingReward { NodeId = nodeId, Gold = reward.goldBase + reward.goldPerNode * MapRun.CompletedAtEntry,
                Experience = experience, ChooseCount = Math.Min(choices, candidates.Count), Candidates = candidates.ToArray(),
                GuaranteedDrops = guaranteed,
                EnemyPower = power, DifficultyName = difficulty.name };
            return Pending;
        }
        private static void Draw(string poolId, int count, Random random, List<EffectRecord> result)
        {
            var pool = GameContent.Pools.rows.Where(row => row.poolId == poolId).ToList();
            for (int index = 0; index < count; index++)
            {
                var chosen = RarityRules.Pick(pool, entry => entry.type == PrizeKind.Item ? GameContent.Items.Get(entry.targetId).rarity
                    : GameContent.Characters.Get(entry.targetId).rarity, entry => entry.weight, random, Progression.Level);
                result.Add(new EffectRecord { type = chosen.type == PrizeKind.Item ? EffectKind.GiveItem : EffectKind.GiveCharacter,
                    targetId = chosen.targetId, amount = random.Next(chosen.minQuantity, chosen.maxQuantity + 1) });
                pool.Remove(chosen);
            }
        }
        public static bool Claim(IEnumerable<int> selected)
        {
            if (Pending == null || Pending.NodeId != MapRun.Pending) return false;
            var indices = selected.ToArray();
            if (indices.Distinct().Count() != indices.Length || indices.Length > Pending.ChooseCount
                || indices.Any(index => index < 0 || index >= Pending.Candidates.Length)) return false;
            var effects = indices.Select(index => Pending.Candidates[index]).ToList();
            if (Pending.Gold > 0) effects.Insert(0, new EffectRecord { type = EffectKind.AddGold, amount = Pending.Gold });
            if (!RunState.Apply(effects)) return false;
            int nodeId = Pending.NodeId;
            if (!MapRun.Complete(nodeId)) throw new InvalidOperationException("奖励节点状态已失效");
            Pending = null;
            return true;
        }
    }
}
