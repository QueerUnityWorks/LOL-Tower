using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace ElementalTactics
{
    public enum DailyTaskKind { OwnedPartyLimit, HeroDeployments, BattleWins, RemainingLives }
    public enum DailyTaskStatus { InProgress, Ready, Completed, Failed }

    public sealed class DailyTaskDefinition
    {
        public string Id, Title, Description, CharacterId;
        public DailyTaskKind Kind;
        public int Target;
    }

    [Serializable]
    public sealed class DailyChallengeSave
    {
        public string date, suspendedProfile;
        public int seed, maxOwned, remainingLives;
        public string[] taskIds;
        public int[] heroBattles = Array.Empty<int>(), wonBattles = Array.Empty<int>();
        public bool finished, victory;
    }

    [Serializable]
    public sealed class DailyChallengeRecord
    {
        public string date;
        public int completedTasks, taskCount, score;
        public bool victory;
    }

    public static class DailyChallengeState
    {
        private static readonly DailyTaskDefinition[] definitions = {
            new DailyTaskDefinition { Id = "PARTY_4", Kind = DailyTaskKind.OwnedPartyLimit, Target = 4,
                Title = "全程持有伙伴不超过4人并通关", Description = "出战、候补均计入，重复伙伴分别计数；超过4人后，本局任务失败。" },
            new DailyTaskDefinition { Id = "YASUO_11", Kind = DailyTaskKind.HeroDeployments, Target = 11, CharacterId = "HERO_001",
                Title = "亚索上阵至少11场不同战斗并通关", Description = "实际开战时计数；同节点重试、多名亚索同场都只计1次。" },
            new DailyTaskDefinition { Id = "BATTLES_12", Kind = DailyTaskKind.BattleWins, Target = 12,
                Title = "赢得至少12场战斗并通关", Description = "击败敌人并确认领取战利品后计数，每个战斗节点只计1次。" },
            new DailyTaskDefinition { Id = "LIVES_2", Kind = DailyTaskKind.RemainingLives, Target = 2,
                Title = "剩余至少2点远征生命时通关", Description = "每次战败扣除1点远征生命。剩余生命少于2点后，本局此任务失败。" }
        };
        private static readonly List<DailyChallengeRecord> records = new List<DailyChallengeRecord>();
        public static event Action Changed;
        public static DailyChallengeSave Current { get; private set; }
        public static bool IsDailyRun => Current != null && !TutorialRun.Active;
        public static string Today => DateAt(DateTimeOffset.UtcNow);
        public static IReadOnlyList<DailyChallengeRecord> Records => records.AsReadOnly();
        public static string DateAt(DateTimeOffset utc) => utc.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        public static int SeedFor(string date) => int.Parse(ParseDate(date).ToString("yyyyMMdd", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        public static DailyTaskDefinition Task(string id) => definitions.Single(task => task.Id == id);
        public static DailyTaskDefinition[] TasksFor(string date)
            => new[] { definitions[(SeedFor(date) - 1) % definitions.Length] };
        private static string[] LegacyTasksFor(string date)
        {
            int rotation = SeedFor(date) % 3;
            return rotation == 0 ? new[] { "PARTY_4", "BATTLES_12" }
                : rotation == 1 ? new[] { "YASUO_11", "LIVES_2" } : new[] { "PARTY_4", "YASUO_11" };
        }
        public static DailyTaskDefinition[] Tasks => Current == null ? Array.Empty<DailyTaskDefinition>() : Current.taskIds.Select(Task).ToArray();

        private static DateTime ParseDate(string date)
        {
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                throw new InvalidOperationException("每日挑战日期无效");
            return parsed;
        }

        public static void Start(string date)
        {
            int seed = SeedFor(date);
            if (TutorialRun.Active) throw new InvalidOperationException("请先退出新手教程，再开始每日挑战。");
            if (Current != null && AdventureRun.Active) throw new InvalidOperationException("请先继续或结束当前每日挑战。");
            string suspended = Current == null ? JsonUtility.ToJson(AdventureRun.Capture()) : Current.suspendedProfile;
            AdventureRun.StartSeeded(seed, 10);
            Current = new DailyChallengeSave { date = date, seed = seed, suspendedProfile = suspended,
                taskIds = TasksFor(date).Select(task => task.Id).ToArray(), maxOwned = RunState.Characters.Count, remainingLives = RunState.Lives };
            Changed?.Invoke();
        }

        internal static void ResetCurrent() { Current = null; Changed?.Invoke(); }
        internal static void ClearProfile() { ResetCurrent(); records.Clear(); }
        internal static void Restore(DailyChallengeSave state, DailyChallengeRecord[] history)
        {
            Current = state;
            if (Current != null) Current.taskIds = TasksFor(Current.date).Select(task => task.Id).ToArray();
            records.Clear();
            if (history != null) records.AddRange(history.Select(record => record.taskCount == 1 ? record : new DailyChallengeRecord {
                date = record.date, victory = record.victory, score = record.score,
                taskCount = 1, completedTasks = record.completedTasks == record.taskCount
                    && LegacyTasksFor(record.date).Contains(TasksFor(record.date).Single().Id) ? 1 : 0
            }));
            var result = SettlementRun.Current;
            if (IsDailyRun && Current.finished && result != null && result.dailyDate == Current.date)
            {
                result.dailyTaskCount = 1;
                result.dailyCompletedTasks = Tasks.Count(task => Status(task) == DailyTaskStatus.Completed);
                RecordScore(result.TotalScore);
            }
            Changed?.Invoke();
        }

        internal static void ObserveRoster()
        {
            if (!IsDailyRun || Current.finished) return;
            Current.maxOwned = Math.Max(Current.maxOwned, RunState.Characters.Count);
            Current.remainingLives = RunState.Lives;
            Changed?.Invoke();
        }

        public static void BattleStarted(int nodeId, IEnumerable<CharacterDefinition> lineup)
        {
            if (!IsDailyRun || Current.finished || nodeId < 0 || nodeId != MapRun.Pending || MapRun.IsCleared(nodeId)
                || MapRun.Nodes[nodeId].Definition.type != NodeKind.Battle || RewardFlow.Pending != null) return;
            if (lineup.Any(unit => unit != null && unit.id == "HERO_001") && !Current.heroBattles.Contains(nodeId))
                Current.heroBattles = Current.heroBattles.Concat(new[] { nodeId }).ToArray();
            ObserveRoster();
        }

        internal static void NodeCompleted(MapNode node)
        {
            if (!IsDailyRun || Current.finished) return;
            if (node.Definition.type == NodeKind.Battle && !Current.wonBattles.Contains(node.Id))
                Current.wonBattles = Current.wonBattles.Concat(new[] { node.Id }).ToArray();
            ObserveRoster();
        }

        public static int Value(DailyTaskDefinition task)
        {
            if (Current == null) return 0;
            switch (task.Kind)
            {
                case DailyTaskKind.OwnedPartyLimit: return Current.maxOwned;
                case DailyTaskKind.HeroDeployments: return Current.heroBattles.Length;
                case DailyTaskKind.BattleWins: return Current.wonBattles.Length;
                default: return Current.remainingLives;
            }
        }

        public static DailyTaskStatus Status(DailyTaskDefinition task)
        {
            int value = Value(task);
            bool satisfied = task.Kind == DailyTaskKind.OwnedPartyLimit ? value <= task.Target : value >= task.Target;
            if (Current.finished) return Current.victory && satisfied ? DailyTaskStatus.Completed : DailyTaskStatus.Failed;
            if ((task.Kind == DailyTaskKind.OwnedPartyLimit || task.Kind == DailyTaskKind.RemainingLives) && !satisfied) return DailyTaskStatus.Failed;
            return satisfied && task.Kind != DailyTaskKind.OwnedPartyLimit && task.Kind != DailyTaskKind.RemainingLives ? DailyTaskStatus.Ready : DailyTaskStatus.InProgress;
        }

        public static string Progress(DailyTaskDefinition task)
        {
            var status = Status(task);
            string number = Value(task) + "/" + task.Target;
            return (task.Kind == DailyTaskKind.OwnedPartyLimit ? "峰值 " : "") + number + " · "
                + (status == DailyTaskStatus.Completed ? "已完成" : status == DailyTaskStatus.Failed ? "未达成"
                    : status == DailyTaskStatus.Ready ? "待通关" : task.Kind == DailyTaskKind.OwnedPartyLimit || task.Kind == DailyTaskKind.RemainingLives ? "保持中" : "进行中");
        }

        internal static void Finish(bool victory)
        {
            if (!IsDailyRun || Current.finished) return;
            ObserveRoster();
            Current.finished = true; Current.victory = victory;
            Changed?.Invoke();
            RecordScore(0);
        }

        internal static void RecordScore(int score)
        {
            if (!IsDailyRun || !Current.finished) return;
            var candidate = new DailyChallengeRecord { date = Current.date, victory = Current.victory, score = score,
                taskCount = Tasks.Length, completedTasks = Tasks.Count(task => Status(task) == DailyTaskStatus.Completed) };
            var previous = records.FirstOrDefault(record => record.date == candidate.date);
            if (previous != null && (previous.completedTasks > candidate.completedTasks
                || previous.completedTasks == candidate.completedTasks && previous.score >= candidate.score)) return;
            records.RemoveAll(record => record.date == candidate.date);
            records.Add(candidate);
            records.Sort((left, right) => string.CompareOrdinal(right.date, left.date));
            if (records.Count > 30) records.RemoveRange(30, records.Count - 30);
        }

        public static void ReturnToRegular()
        {
            if (Current == null) return;
            if (AdventureRun.Active) throw new InvalidOperationException("进行中的每日挑战应先结束。");
            var profile = JsonUtility.FromJson<AdventureSave>(Current.suspendedProfile);
            profile.history = AdventureRun.History.Concat(profile.history).GroupBy(record => record.id).Select(group => group.First()).Take(50).ToArray();
            if (LeaderboardState.Best != null && (profile.personalBest == null || string.IsNullOrEmpty(profile.personalBest.runId)
                || LeaderboardState.Best.TotalScore > profile.personalBest.TotalScore)) profile.personalBest = LeaderboardState.Best;
            profile.dailyRecords = records.ToArray();
            profile.hasDailyChallenge = false; profile.dailyChallenge = null;
            AdventureRun.Restore(profile);
        }

        internal static void Validate(AdventureSave save)
        {
            if (save.dailyRecords != null)
            {
                if (save.dailyRecords.Length > 30 || save.dailyRecords.Any(record => record == null || record.completedTasks < 0
                    || record.taskCount < 1 || record.taskCount > 2 || record.completedTasks > record.taskCount || record.score < 0)
                    || save.dailyRecords.Select(record => record.date).Distinct().Count() != save.dailyRecords.Length)
                    throw new InvalidOperationException("每日挑战记录无效");
                foreach (var record in save.dailyRecords) ParseDate(record.date);
            }
            if (!save.hasDailyChallenge) return;
            var state = save.dailyChallenge;
            if (state == null || state.seed != SeedFor(state.date) || state.taskIds == null
                || !state.taskIds.SequenceEqual(TasksFor(state.date).Select(task => task.Id)) && !state.taskIds.SequenceEqual(LegacyTasksFor(state.date))
                || state.maxOwned < 1 || state.maxOwned > 1000000 || state.remainingLives < 0 || state.remainingLives > RunState.InitialLives
                || state.heroBattles == null || state.wonBattles == null
                || state.heroBattles.Length > 300 || state.wonBattles.Length > 300
                || state.heroBattles.Distinct().Count() != state.heroBattles.Length || state.wonBattles.Distinct().Count() != state.wonBattles.Length
                || state.heroBattles.Concat(state.wonBattles).Any(id => id < 0 || id >= 300)
                || string.IsNullOrEmpty(state.suspendedProfile) || state.suspendedProfile.Length > 1000000
                || state.finished == save.active || state.victory && !state.finished)
                throw new InvalidOperationException("每日挑战进度无效");
            if (save.active && (save.difficulty != 10 || save.run.seed != state.seed || state.maxOwned < save.run.characters.Length
                || state.remainingLives != (save.run.hasLives ? save.run.lives : RunState.InitialLives)
                || state.heroBattles.Concat(state.wonBattles).Any(id => id >= save.run.nodes.Length || GameContent.Nodes.Get(save.run.nodes[id].definition).type != NodeKind.Battle)
                || state.wonBattles.Any(id => !save.run.cleared.Contains(id))))
                throw new InvalidOperationException("每日挑战地图或任务数据不匹配");
            var suspended = JsonUtility.FromJson<AdventureSave>(state.suspendedProfile);
            if (suspended == null || suspended.hasDailyChallenge || suspended.tutorial != null && suspended.tutorial.active)
                throw new InvalidOperationException("每日挑战暂存远征无效");
            AdventureRun.Validate(suspended);
        }
    }
}
