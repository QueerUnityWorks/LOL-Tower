using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ElementalTactics.Editor
{
    public static class DailyChallengeValidation
    {
        private const string Output = "Temp/DailyChallengeValidation";
        private const string Date = "2026-11-17";
        private static int checks;

        [MenuItem("Tools/Elemental Tactics/Validate Daily Challenge")]
        public static void Validate()
        {
            Directory.CreateDirectory(Output);
            try { File.WriteAllText(Output + "/result.txt", RunChecks()); }
            catch (Exception error) { File.WriteAllText(Output + "/result.txt", error.ToString()); Debug.LogException(error); }
        }

        public static string RunChecks()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "请在编辑模式检查每日挑战。");
            checks = 0;
            Directory.CreateDirectory(Output);
            var original = Clone(AdventureRun.Capture());
            try
            {
                CheckCalendar();
                CheckSuspension();
                CheckRosterAndDeployments();
                CheckVictory();
                CheckLivesAndFailure();
                CheckTutorial();
                CheckLegacyTasks();
                CheckOldSave();
                string message = "PASS: " + checks + " daily challenge checks. Calendar, shared maps, roster limits, unique deployments, rewards, victory/failure, records, persistence, suspended runs and tutorial isolation verified.";
                Debug.Log(message);
                return message;
            }
            finally { AdventureRun.Restore(original); }
        }

        private static AdventureSave Clone(AdventureSave save) => JsonUtility.FromJson<AdventureSave>(JsonUtility.ToJson(save));
        private static void Reload() => AdventureRun.Restore(Clone(AdventureRun.Capture()));
        private static void Start(string date = Date)
        {
            AdventureRun.ClearProfile();
            DailyChallengeState.Start(date);
        }
        private static string MapSignature() => JsonUtility.ToJson(new NodeList { nodes = RunSnapshot.Capture().nodes });

        private static void CheckCalendar()
        {
            Require(DailyChallengeState.DateAt(new DateTimeOffset(2026, 11, 16, 15, 59, 59, TimeSpan.Zero)) == "2026-11-16", "北京时间零点前日期不正确。");
            Require(DailyChallengeState.DateAt(new DateTimeOffset(2026, 11, 16, 16, 0, 0, TimeSpan.Zero)) == Date, "北京时间零点没有更新日期。");
            var dates = new[] { Date, "2026-11-18", "2026-11-19", "2026-11-20" };
            Require(dates.All(date => DailyChallengeState.TasksFor(date).Length == 1), "每天应只发布一项任务。");
            Require(dates.Select(date => DailyChallengeState.TasksFor(date).Single().Id).Distinct().Count() == 4, "独立任务没有按日轮换。");
            Start();
            string map = MapSignature();
            string[] tasks = DailyChallengeState.Tasks.Select(task => task.Id).ToArray();
            Require(AdventureRun.Difficulty == 10 && MapRun.Seed == 20261117, "每日挑战未固定难度与日期地图。");
            Reload();
            Require(MapSignature() == map && DailyChallengeState.Current.date == Date, "读档改变了开局日期或地图。");
            AdventureRun.Abandon();
            DailyChallengeState.Start(Date);
            Require(MapSignature() == map && DailyChallengeState.Tasks.Select(task => task.Id).SequenceEqual(tasks), "同日重复挑战地图或任务不一致。");
            AdventureRun.Abandon();
            DailyChallengeState.Start("2026-11-18");
            Require(MapRun.Seed != 20261117 && MapSignature() != map, "次日没有生成新地图。");
        }

        private static void CheckSuspension()
        {
            AdventureRun.ClearProfile();
            AdventureRun.SelectDifficulty(4);
            AdventureRun.StartNew();
            Require(RunState.Apply(new[] {
                new EffectRecord { type = EffectKind.AddGold, amount = 123 },
                new EffectRecord { type = EffectKind.GiveCharacter, targetId = "HERO_002", amount = 1 }
            }), "无法准备普通远征。");
            var formation = RunState.RestoreFormation();
            formation.Place(1, 4);
            RunState.SaveFormation(formation);
            var next = MapRun.Nodes.First(node => MapRun.CanEnter(node.Id) && node.Definition.type == NodeKind.Battle);
            Require(MapRun.TryEnter(next.Id), "普通远征不能进入战斗。");
            var regular = Clone(AdventureRun.Capture());
            DailyChallengeState.Start(Date);
            Require(RunState.Gold == 0 && RunState.Characters.Count == 1 && MapRun.Pending == -1, "每日挑战继承了普通远征资源。");
            Reload();
            AdventureRun.Abandon();
            Require(AdventureRun.Active && !DailyChallengeState.IsDailyRun && AdventureRun.Id == regular.id && AdventureRun.Difficulty == 4, "放弃挑战后未恢复普通远征。");
            Require(JsonUtility.ToJson(RunSnapshot.Capture()) == JsonUtility.ToJson(regular.run), "普通远征的地图、待处理节点、金币、角色或布阵变化。");
            Require(DailyChallengeState.Records.Count == 1 && AdventureRun.History.Any(record => record.dailyDate == Date), "恢复普通远征时丢失每日记录。");
            DailyChallengeState.Start(Date);
            next = FirstBattle();
            for (int attempt = 0; attempt < RunState.InitialLives; attempt++) Require(RunState.RecordBattleDefeat(next.Id), "每日失败流程无法扣除生命。");
            Reload();
            Require(SettlementRun.Pending && SettlementRun.Current.dailyDate == Date, "失败结算未保存每日日期。");
            SettlementRun.Acknowledge();
            DailyChallengeState.ReturnToRegular();
            Require(AdventureRun.Id == regular.id && JsonUtility.ToJson(RunSnapshot.Capture()) == JsonUtility.ToJson(regular.run), "失败结算后未恢复原远征。");
        }

        private static MapNode FirstBattle()
        {
            var node = MapRun.Nodes.First(entry => MapRun.CanEnter(entry.Id) && entry.Definition.type == NodeKind.Battle);
            Require(MapRun.TryEnter(node.Id), "无法进入每日战斗节点。");
            return node;
        }

        private static void CheckRosterAndDeployments()
        {
            Start();
            var party = DailyChallengeState.Task("PARTY_4");
            var hero = DailyChallengeState.Task("YASUO_11");
            int updates = 0;
            Action changed = () => updates++;
            DailyChallengeState.Changed += changed;
            try
            {
                Require(RunState.Apply(new[] { new EffectRecord { type = EffectKind.GiveCharacter, targetId = "HERO_001", amount = 3 } }), "无法获得重复候补。");
                Require(RunState.Characters.Count == 4 && DailyChallengeState.Value(party) == 4 && DailyChallengeState.Status(party) == DailyTaskStatus.InProgress && updates > 0, "候补与重复伙伴没有计入人数或刷新界面。");
                Require(DailyChallengeState.Value(hero) == 0, "只布阵就增加了参战次数。");
                var node = FirstBattle();
                DailyChallengeState.BattleStarted(node.Id, new[] { GameContent.Characters.Get("HERO_002").Create() });
                Require(DailyChallengeState.Value(hero) == 0, "未上阵亚索也增加了次数。");
                DailyChallengeState.BattleStarted(node.Id, RunState.Characters.Select(character => character.Definition));
                Require(DailyChallengeState.Value(hero) == 1, "多名亚索同场应只计一次。");
                Require(RunState.RecordBattleDefeat(node.Id), "无法验证战斗重试。");
                Reload();
                DailyChallengeState.BattleStarted(node.Id, RunState.Characters.Select(character => character.Definition));
                Require(DailyChallengeState.Value(hero) == 1, "重试或重载同节点重复计数。");
                Require(RunState.Apply(new[] { new EffectRecord { type = EffectKind.GiveCharacter, targetId = "HERO_002", amount = 1 } }), "无法增加第五名伙伴。");
                Require(DailyChallengeState.Status(party) == DailyTaskStatus.Failed, "持有第五名伙伴后任务没有失败。");
                Reload();
                Require(DailyChallengeState.Value(party) == 5 && DailyChallengeState.Status(party) == DailyTaskStatus.Failed, "读档清除了人数超标记录。");
            }
            finally { DailyChallengeState.Changed -= changed; }
        }

        private static void CompleteRoute(bool deployYasuo = true)
        {
            int steps = 0;
            while (!MapRun.Finished)
            {
                Require(++steps <= 40, "每日挑战路线未结束。");
                var available = MapRun.Nodes.Where(node => MapRun.CanEnter(node.Id)).ToArray();
                Require(available.Length > 0, "每日地图没有后续节点。");
                var next = available.FirstOrDefault(node => node.Definition.type == NodeKind.Battle) ?? available[0];
                Require(MapRun.TryEnter(next.Id), "每日路线无法进入节点。");
                if (next.Definition.type == NodeKind.Battle)
                {
                    var deployed = RunState.Characters.First(character => character.Definition.id == (deployYasuo ? "HERO_001" : "HERO_002"));
                    DailyChallengeState.BattleStarted(next.Id, new[] { deployed.Definition });
                    int wins = DailyChallengeState.Current.wonBattles.Length;
                    Require(RewardFlow.Victory(next.Id) != null, "每日战斗没有生成战利品。");
                    AdventureRun.FinishIfNeeded();
                    Require(AdventureRun.Active && SettlementRun.Current == null && DailyChallengeState.Current.wonBattles.Length == wins, "领奖前提前结算或增加胜场。");
                    Require(RewardFlow.Claim(Array.Empty<int>()), "每日战利品无法领取。");
                    Require(DailyChallengeState.Current.wonBattles.Length == wins + 1, "领奖后没有记录胜场。");
                }
                else if (next.Definition.type == NodeKind.Event)
                {
                    var record = GameContent.Events.Get(next.Definition.contentId);
                    var option = record.id == "EVENT_010" ? record.options.Single(entry => entry.id == "OPTION_10_1")
                        : record.id == "EVENT_011" ? record.options.Single(entry => entry.id == "OPTION_11_2")
                        : record.options.Where(entry => RunState.CanApply(RunState.Effects(entry)))
                            .OrderBy(entry => RunState.Effects(entry).Where(effect => effect.type == EffectKind.GiveCharacter).Sum(effect => effect.amount)).First();
                    Require(RunState.ChooseEvent(option.id), "每日事件无法选择。");
                }
                else Require(MapRun.Complete(next.Id), "每日商店无法离开。");
                if (!MapRun.Finished && DailyChallengeState.Value(DailyChallengeState.Task("YASUO_11")) >= 11)
                    Require(DailyChallengeState.Status(DailyChallengeState.Task("YASUO_11")) == DailyTaskStatus.Ready, "达到次数后应等待通关。");
            }
            AdventureRun.FinishIfNeeded();
        }

        private static void CheckVictory()
        {
            Start();
            string map = MapSignature();
            Require(RunState.Apply(new[] { new EffectRecord { type = EffectKind.GiveCharacter, targetId = "HERO_002", amount = 1 } }), "无法准备人数任务阵容。");
            CompleteRoute(false);
            Require(SettlementRun.Pending && SettlementRun.Current.victory && SettlementRun.Current.difficulty == 10, "每日通关未进入难度10结算。");
            Require(DailyChallengeState.Tasks.Single().Id == "PARTY_4" && DailyChallengeState.Current.heroBattles.Length == 0
                && DailyChallengeState.Status(DailyChallengeState.Tasks.Single()) == DailyTaskStatus.Completed, "人数任务不应附带亚索参战条件。");
            var result = SettlementRun.Current;
            Require(result.dailyCompletedTasks == 1 && result.dailyTaskCount == 1 && result.dailyDate == Date, "每日结算任务数量错误。");
            int best = result.TotalScore;
            Require(DailyChallengeState.Records.Single().score == best && DailyChallengeState.Records.Single().completedTasks == 1, "每日最佳成绩没有记录。");
            File.WriteAllText(Output + "/victory.json", JsonUtility.ToJson(AdventureRun.Capture()));
            Reload();
            Require(DailyChallengeState.Tasks.All(task => DailyChallengeState.Status(task) == DailyTaskStatus.Completed), "读档丢失已完成任务。");
            SettlementRun.Acknowledge();
            DailyChallengeState.Start(Date);
            Require(MapSignature() == map && DailyChallengeState.Current.heroBattles.Length == 0 && DailyChallengeState.Current.maxOwned == 1, "再次挑战没有重置进度或改变了地图。");
            var node = FirstBattle();
            for (int attempt = 0; attempt < RunState.InitialLives; attempt++) RunState.RecordBattleDefeat(node.Id);
            Require(DailyChallengeState.Records.Single().score == best && DailyChallengeState.Records.Single().completedTasks == 1, "较差成绩覆盖了当日最佳。");
            SettlementRun.Acknowledge();
            DailyChallengeState.ReturnToRegular();
            Require(!AdventureRun.Active && !DailyChallengeState.IsDailyRun && LeaderboardState.Best.TotalScore >= best, "每日结束后没有保留最高分或退出挑战。");
            Start("2026-11-18");
            Require(RunState.Apply(new[] { new EffectRecord { type = EffectKind.GiveCharacter, targetId = "HERO_002", amount = 4 } }), "无法准备亚索任务阵容。");
            CompleteRoute();
            Require(DailyChallengeState.Tasks.Single().Id == "YASUO_11" && DailyChallengeState.Current.maxOwned > 4
                && DailyChallengeState.Status(DailyChallengeState.Tasks.Single()) == DailyTaskStatus.Completed, "亚索任务不应附带人数限制。");
            Start("2026-11-19");
            CompleteRoute();
            Require(DailyChallengeState.Tasks.Single().Id == "BATTLES_12" && DailyChallengeState.Status(DailyChallengeState.Tasks.Single()) == DailyTaskStatus.Completed, "胜场任务无法在正常路线中完成。");
        }

        private static void CheckLivesAndFailure()
        {
            Start("2026-11-20");
            var task = DailyChallengeState.Task("LIVES_2");
            var node = FirstBattle();
            Require(RunState.RecordBattleDefeat(node.Id) && RunState.RecordBattleDefeat(node.Id), "无法验证剩余生命任务。");
            Require(DailyChallengeState.Status(task) == DailyTaskStatus.Failed, "生命不足2点没有失败。");
            MapRun.LeaveEncounter();
            CompleteRoute();
            Require(DailyChallengeState.Status(task) == DailyTaskStatus.Failed, "生命不足仍被判定完成。");
            Reload();
            Require(DailyChallengeState.Value(task) == 1 && DailyChallengeState.Status(task) == DailyTaskStatus.Failed, "通关重载后错误恢复了任务生命。");
            Start();
            node = FirstBattle();
            for (int attempt = 0; attempt < RunState.InitialLives; attempt++) RunState.RecordBattleDefeat(node.Id);
            Require(!SettlementRun.Current.victory && DailyChallengeState.Tasks.All(entry => DailyChallengeState.Status(entry) == DailyTaskStatus.Failed), "远征失败不应完成任务。");
            Reload();
            Require(DailyChallengeState.Records.Single().completedTasks == 0, "失败记录包含完成任务。");
        }

        private static void CheckTutorial()
        {
            Start();
            string before = JsonUtility.ToJson(DailyChallengeState.Current);
            TutorialRun.Begin();
            Require(TutorialRun.Active && !DailyChallengeState.IsDailyRun, "教程被标记为每日对局。");
            Reload();
            Require(TutorialRun.Active && !DailyChallengeState.IsDailyRun, "教程读档污染了每日状态。");
            TutorialRun.Exit(false);
            Require(DailyChallengeState.IsDailyRun && JsonUtility.ToJson(DailyChallengeState.Current) == before, "退出教程没有恢复每日进度。");
        }

        private static void CheckOldSave()
        {
            AdventureRun.Restore(JsonUtility.FromJson<AdventureSave>("{\"version\":1,\"selectedDifficulty\":1,\"difficulty\":1,\"active\":false,\"history\":[]}"));
            Require(!DailyChallengeState.IsDailyRun && DailyChallengeState.Records.Count == 0, "旧存档无法兼容每日挑战。");
        }

        private static void CheckLegacyTasks()
        {
            Start();
            var save = Clone(AdventureRun.Capture());
            save.dailyChallenge.taskIds = new[] { "PARTY_4", "YASUO_11" };
            save.dailyRecords = new[] { new DailyChallengeRecord { date = "2026-11-16", taskCount = 2, completedTasks = 2, score = 321, victory = true } };
            AdventureRun.Restore(save);
            Require(DailyChallengeState.Tasks.Single().Id == "PARTY_4" && MapRun.Seed == 20261117, "旧双任务存档未转换为当天单任务。");
            Require(DailyChallengeState.Records.Single().taskCount == 1 && DailyChallengeState.Records.Single().completedTasks == 1, "旧每日记录未转换为单任务。");
            save = Clone(AdventureRun.Capture());
            save.dailyRecords = new[] { new DailyChallengeRecord { date = "2026-11-18", taskCount = 2, completedTasks = 2, score = 321, victory = true } };
            AdventureRun.Restore(save);
            Require(DailyChallengeState.Records.Single().completedTasks == 0, "旧记录不应自动完成未曾记录的新目标。");
            Reload();
            save = JsonUtility.FromJson<AdventureSave>(File.ReadAllText(Output + "/victory.json"));
            save.dailyChallenge.taskIds = new[] { "PARTY_4", "YASUO_11" };
            save.settlement.dailyTaskCount = 2;
            save.settlement.dailyCompletedTasks = 1;
            save.dailyRecords.Single().taskCount = 2;
            save.dailyRecords.Single().completedTasks = 1;
            AdventureRun.Restore(save);
            Require(SettlementRun.Current.dailyTaskCount == 1 && SettlementRun.Current.dailyCompletedTasks == 1
                && DailyChallengeState.Records.Single().completedTasks == 1, "旧结算没有按当前独立任务重新判定。");
        }

        private static void Require(bool condition, string message)
        {
            checks++;
            if (!condition) throw new InvalidOperationException(message);
        }

        [Serializable] private sealed class NodeList { public NodeSnapshot[] nodes; }
    }
}
