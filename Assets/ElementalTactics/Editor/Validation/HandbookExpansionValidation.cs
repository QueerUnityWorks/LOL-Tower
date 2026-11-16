using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ElementalTactics.Editor
{
    [InitializeOnLoad]
    public static class HandbookExpansionValidation
    {
        const string Output = "D:/自走棋/outputs/unity-content-sync-20261110/";
        static int checks;
        static HandbookExpansionValidation() { EditorApplication.update += CheckRequest; }
        static void CheckRequest()
        {
            if (Application.isBatchMode || !File.Exists(Output + "unity-request.txt") || EditorApplication.isCompiling
                || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Output + "unity-request.txt");
            try { Run(); }
            catch (Exception error) { File.WriteAllText(Output + "unity-error.txt", error.ToString()); Debug.LogException(error); }
        }
        static void Check(bool condition, string message) { checks++; if (!condition) throw new InvalidOperationException(message); }
        static void Reject(Action action, string message)
        {
            bool rejected = false;
            try { action(); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, message);
        }
        public static void RunBatch()
        {
            try { Run(); EditorApplication.Exit(0); }
            catch (Exception error)
            {
                File.WriteAllText(Output + "unity-error.txt", error.ToString());
                Debug.LogException(error); EditorApplication.Exit(1);
            }
        }
        [MenuItem("Tools/Elemental Tactics/Validate Handbook Expansion")]
        public static void Run()
        {
            checks = 0;
            HandbookSyncImporter.Import();
            HandbookSyncImporter.ValidateImported();
            var config = HandbookBalanceConfig.Current;
            Check(GameContent.Battles.rows.Length == 53, "应有53种敌阵");
            Check(config.snapshot.stages.Length == 63, "应有63条分层关卡");
            Check(GameContent.Events.rows.Length == 33, "应有33种事件");
            Check(GameContent.Events.rows.Sum(e => e.options.Length) == 92, "应有92个事件选项");
            foreach (var battle in GameContent.Battles.rows)
                StageRules.ValidatePopulation(battle, GameContent.Nodes.rows, GameContent.Floors.rows, id => config.Unit(id)?.quality);
            var firstFloor = new[] { new FloorRecord { floor = 1, rows = 7 } };
            for (int x = 1; x <= 4; x++)
            {
                var battle = new BattleRecord { id = "CAP_TEST", slot1 = "E", slot2 = x >= 2 ? "E" : "", slot3 = x >= 3 ? "E" : "", slot4 = x >= 4 ? "E" : "" };
                var node = new NodeRecord { id = "CAP_NODE", type = NodeKind.Battle, contentId = battle.id, minFloor = 1, maxFloor = 1, minRow = x, maxRow = x };
                StageRules.ValidatePopulation(battle, new[] { node }, firstFloor, _ => "普通");
                Check(StageRules.FitsEarlyNode(battle, 1, x, firstFloor), "等于节点数时应通过");
                if (x == 1) battle.slot2 = "E"; else if (x == 2) battle.slot3 = "E"; else if (x == 3) battle.slot4 = "E"; else battle.slot5 = "E";
                foreach (string quality in new[] { "普通", "精英", "boss" })
                    Reject(() => StageRules.ValidatePopulation(battle, new[] { node }, firstFloor, _ => quality), "所有品质均须拒绝超人数敌阵");
            }
            var six = new BattleRecord { slot1 = "E", slot2 = "E", slot3 = "E", slot4 = "E", slot5 = "E", slot6 = "E" };
            Check(StageRules.FitsEarlyNode(six, 1, 5, firstFloor), "第5节点不应用前4节点上限");
            Check(StageRules.CumulativeNode(2, 1, new[] { new FloorRecord { floor = 1, rows = 3 }, new FloorRecord { floor = 2, rows = 7 } }) == 4, "累计节点必须使用实际楼层排数");
            Check(!StageRules.FitsEarlyNode(six, 2, 1, new[] { new FloorRecord { floor = 1, rows = 3 }, new FloorRecord { floor = 2, rows = 7 } }), "跨层累计第4节点仍受限");
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int seed = 0; seed < 200; seed++)
            {
                MapRun.ResetWithSeed(seed);
                Check(MapRun.Nodes.Count == 67, "地图节点数变化");
                foreach (var node in MapRun.Nodes)
                {
                    seen.Add(node.Definition.contentId);
                    if (node.Definition.type != NodeKind.Battle) continue;
                    var battle = GameContent.Battles.Get(node.Definition.contentId);
                    Check(StageRules.FitsEarlyNode(battle, node.Floor, node.Row + 1, GameContent.Floors.rows), "生成了超出前4节点上限的战斗");
                }
            }
            foreach (var battle in GameContent.Battles.rows.Where(b => b.id.StartsWith("BATTLE_HB_"))) Check(seen.Contains(battle.id), "新关卡未生成：" + battle.id);
            foreach (var ev in GameContent.Events.rows.Where(e => e.id.StartsWith("EVENT_HB_"))) Check(seen.Contains(ev.id), "新事件未生成：" + ev.id);
            foreach (var ev in GameContent.Events.rows.Where(e => e.id.StartsWith("EVENT_HB_")))
            {
                Check(Resources.Load<Texture2D>(ev.cg) != null, "新事件图片缺失");
                foreach (var option in ev.options)
                {
                    MapRun.ResetWithSeed(1); RunState.Ensure();
                    var effects = RunState.Effects(option);
                    Check(effects.Length == 1 && RunState.Apply(effects), "新事件选项无法领取：" + option.id);
                    var effect = effects[0];
                    if (effect.type == EffectKind.GiveCharacter) Check(RunState.Characters.Count == 1 + effect.amount, "角色奖励数量错误");
                    if (effect.type == EffectKind.GiveItem) Check(InventoryState.Items.Single(i => i.Id == effect.targetId).Quantity == effect.amount, "道具奖励数量错误");
                    if (effect.type == EffectKind.AddGold) Check(RunState.Gold == effect.amount, "金币奖励错误");
                    if (effect.type == EffectKind.AddExperience)
                        Check(Progression.Experience + GameContent.Levels.rows.Where(l => l.level < Progression.Level).Sum(l => l.experienceToNext)
                            == effect.amount, "经验增量错误");
                }
            }
            var difficultyField = typeof(AdventureRun).GetField("<Difficulty>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static);
            MapRun.ResetWithSeed(2);
            difficultyField.SetValue(null, config.snapshot.analysisDifficulty);
            foreach (var stage in config.snapshot.stages)
            {
                var node = new MapNode(0, stage.minRow - 1, 0, GameContent.Nodes.Get(stage.nodeId), stage.floor);
                double power = CombatPower.RewardPower(node);
                Check(Math.Abs(power - stage.rewardPower) < 0.00001, "手册奖励判定战力不一致：" + stage.name);
                Check(CombatPower.Difficulty(power).id == "DIFF_" + stage.rewardTier.ToString("000"), "奖励档位不一致");
            }
            for (int difficulty = 1; difficulty <= 10; difficulty++)
            {
                MapRun.ResetWithSeed(17);
                difficultyField.SetValue(null, difficulty);
                var first = MapRun.Nodes.First(n => n.Floor == 1 && n.Row == 0);
                Check(GameContent.EnemyLineup(first).Count(u => u != null) == 1, "首战必须1名敌人");
                Check(MapRun.TryEnter(first.Id), "无法进入首战");
                var reward = RewardFlow.Victory(first.Id);
                Check(reward != null && reward.Gold == 30 && reward.Experience == 20, "首战结算错误");
                Check(RewardFlow.Claim(new[] { 0 }), "无法领取首战奖励");
            }
            MapRun.ResetWithSeed(0);
            File.WriteAllText(Output + "unity-result.json", "{\"status\":\"PASS\",\"checks\":" + checks + ",\"maps\":200,\"battles\":53,\"events\":33,\"options\":92}");
            Debug.Log("手册关卡事件同步验证通过：" + checks + "项。");
        }
    }
}
