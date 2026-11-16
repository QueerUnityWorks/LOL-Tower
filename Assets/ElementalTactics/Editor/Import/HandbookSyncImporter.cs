using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace ElementalTactics.Editor
{
    public static class HandbookSyncImporter
    {
        public const string SnapshotPath = "Assets/ElementalTactics/Data/HandbookBalance.json";
        public const string AssetPath = "Assets/ElementalTactics/Resources/Data/HandbookBalance.asset";

        [MenuItem("Tools/Elemental Tactics/Import Synced Handbook")]
        public static void Import()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("请退出运行模式后同步手册。");
            var incoming = JsonUtility.FromJson<HandbookSnapshot>(File.ReadAllText(SnapshotPath));
            Require(incoming != null && incoming.schemaVersion == 1, "手册数据版本无效。");
            Require(Digest(GameWorkbookImporter.WorkbookPath) == incoming.gameWorkbookSha256,
                "GameContent.xlsx 与手册快照不配套，请重新运行手册转换工具后成对同步。");
            Require(incoming.units != null && incoming.items != null && incoming.stages != null && incoming.poolEntries != null, "手册快照不完整。");
            GameWorkbookImporter.Import();
            // Validate in memory before replacing the existing balance asset.
            var probe = ScriptableObject.CreateInstance<HandbookBalanceConfig>();
            try { probe.snapshot = incoming; Validate(probe); }
            finally { UnityEngine.Object.DestroyImmediate(probe); }
            var asset = AssetDatabase.LoadAssetAtPath<HandbookBalanceConfig>(AssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<HandbookBalanceConfig>();
                AssetDatabase.CreateAsset(asset, AssetPath);
            }
            asset.snapshot = incoming;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("手册同步成功：" + incoming.units.Length + "名英雄/敌人、" + incoming.items.Length
                + "件道具、" + incoming.stages.Length + "条分层关卡、" + incoming.events.Length
                + "个事件、" + incoming.nodeExpectations.Length + "个分析节点。实战配置与平衡数据已核对。");
        }

        [MenuItem("Tools/Elemental Tactics/Validate Synced Handbook")]
        public static void ValidateImported()
        {
            var asset = AssetDatabase.LoadAssetAtPath<HandbookBalanceConfig>(AssetPath);
            Require(asset != null, "尚未导入手册平衡数据。");
            Validate(asset);
            Debug.Log("手册配置验证通过：单位属性、基础/综合强度、实际技力、道具系数、敌阵与楼层缩放、奖励池、事件和节点记录均有效。");
        }

        public static void Validate(HandbookBalanceConfig config)
        {
            var s = config.snapshot;
            Require(Digest(GameWorkbookImporter.WorkbookPath) == s.gameWorkbookSha256, "配置工作簿已改变，需重新同步手册。");
            var all = GameContent.Characters.rows.Concat(GameContent.Enemies.rows).ToArray();
            Require(s.units.Length == all.Length && s.units.Select(u => u.id).Distinct().Count() == s.units.Length, "单位数量/ID不一致。");
            foreach (var u in s.units)
            {
                var actual = (u.isEnemy ? GameContent.Enemies.rows : GameContent.Characters.rows).Single(a => a.id == u.id);
                Require(actual.name == u.name && actual.maxHealth == u.maxHealth && actual.attack == u.attack
                    && actual.defense == u.defense && actual.speed == u.speed, u.id + "：属性未同步。");
                Require(u.skillEnergyCost == CombatRules.SkillEnergyCost, u.id + "：技力规则不一致。");
                Near(config.BaseStrength(actual.Create()), u.baseStrength, u.id + "基础强度");
                Near(u.baseStrength * u.mechanismCoefficient, u.totalStrength, u.id + "综合强度");
            }
            Require(s.items.Length == GameContent.Items.rows.Length && s.items.Select(i => i.id).Distinct().Count() == s.items.Length, "道具数量/ID不一致。");
            foreach (var item in s.items)
            {
                var actual = GameContent.Items.Get(item.id);
                Require(actual.name == item.name && actual.description == item.runtimeDescription, item.id + "：道具说明未同步。");
                Require(!item.hasGainCoefficient || (!double.IsNaN(item.gainCoefficient) && !double.IsInfinity(item.gainCoefficient) && item.gainCoefficient >= 1), item.id + "：增益系数无效。");
            }
            foreach (var stage in s.stages)
            {
                var battle = GameContent.Battles.Get(stage.battleId);
                StageRules.ValidatePopulation(battle, GameContent.Nodes.rows, GameContent.Floors.rows, id => config.Unit(id)?.quality);
                var node = GameContent.Nodes.Get(stage.nodeId);
                Require(battle.Slots.Select(v => v ?? "").SequenceEqual(stage.enemyIds.Select(v => v ?? "")), stage.name + "：敌阵不一致。");
                Require(node.minFloor <= stage.floor && node.maxFloor >= stage.floor && node.minRow == stage.minRow
                    && node.maxRow == stage.maxRow && node.weight == stage.weight, stage.name + "：出现配置不一致。");
                var units = GameContent.EnemyLineup(battle).Where(unit => unit != null).ToArray();
                Near(config.EvaluatedLineup(units), stage.baseDifficulty, stage.name + "基础关卡难度");
                var floor = GameContent.Floors.rows.Single(f => f.floor == stage.floor);
                double factor = AdventureDifficulty.Factor(s.analysisDifficulty);
                foreach (var unit in units)
                {
                    unit.maxHealth = AdventureDifficulty.Scale(unit.maxHealth, factor * floor.healthPercent / 100, 1);
                    unit.attack = AdventureDifficulty.Scale(unit.attack, factor * floor.attackPercent / 100, 1);
                    unit.defense = AdventureDifficulty.Scale(unit.defense, factor);
                    unit.speed = AdventureDifficulty.Scale(unit.speed, factor, 1);
                }
                Near(config.EvaluatedLineup(units), stage.scaledDifficulty, stage.name + "缩放关卡难度");
            }
            var pools = GameContent.Pools.rows.Where(p => s.poolEntries.Any(e => e.poolId == p.poolId)).ToArray();
            Require(pools.Length == s.poolEntries.Length, "六个手册奖励池条目数不同。");
            foreach (var entry in s.poolEntries)
            {
                var actual = pools.Single(p => p.poolId == entry.poolId && p.targetId == entry.targetId);
                Require(actual.weight == entry.weight && actual.minQuantity == entry.minQuantity && actual.maxQuantity == entry.maxQuantity
                    && actual.type.ToString() == entry.type, entry.id + "：奖池权重/数量不一致。");
            }
            Require(s.events.Length == GameContent.Events.rows.Length, "事件数量不一致。");
            foreach (var entry in s.events) Require(GameContent.Events.Get(entry.id).name == entry.name, "事件名称不一致。");
            Require(s.nodeExpectations.Length > 0 && s.nodeExpectations.Select(n => n.x).SequenceEqual(Enumerable.Range(1, s.nodeExpectations.Length)), "分析节点不连续。");
            foreach (var n in s.nodeExpectations) Require(n.instantPowerExpectation > 0 && n.levelExpectation >= 1 && n.levelExpectation <= GameContent.Levels.rows.Length, "节点期望无效。");
        }

        private static string Digest(string path)
        {
            using (var hash = SHA256.Create()) using (var file = File.OpenRead(path))
                return BitConverter.ToString(hash.ComputeHash(file)).Replace("-", "").ToLowerInvariant();
        }
        private static void Near(double actual, double expected, string label)
            => Require(!double.IsNaN(actual) && !double.IsInfinity(actual) && Math.Abs(actual - expected) <= Math.Max(0.00001, Math.Abs(expected) * 0.000000001), label + "不一致。");
        private static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    }
}
