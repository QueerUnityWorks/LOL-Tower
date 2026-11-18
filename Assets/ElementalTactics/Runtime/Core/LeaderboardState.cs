using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ElementalTactics
{
    [Serializable]
    public sealed class LeaderboardBest
    {
        public string runId;
        public int commonScore, difficulty;
        public int TotalScore => AdventureDifficulty.Scale(commonScore, AdventureDifficulty.Factor(difficulty));

        internal void Validate()
        {
            if (string.IsNullOrEmpty(runId) || commonScore < 0 || commonScore > 1000000 || difficulty < 1 || difficulty > 10)
                throw new InvalidOperationException("排行榜最高分记录无效");
        }
    }

    [Serializable]
    public sealed class SimulatedUser
    {
        public string id, name;
        public int commonScore, difficulty;
        public int TotalScore => AdventureDifficulty.Scale(commonScore, AdventureDifficulty.Factor(difficulty));
    }

    [Serializable]
    public sealed class SimulatedUserTable
    {
        public SimulatedUser[] users;
    }

    public sealed class LeaderboardEntry
    {
        public string Id, Name;
        public int Rank, CommonScore, Difficulty, Score;
        public bool IsPlayer;
    }

    public static class LeaderboardState
    {
        public const string PlayerName = "玩家";
        public static LeaderboardBest Best { get; private set; }

        internal static void Reset() { Best = null; }

        internal static void Record(SettlementResult result)
        {
            if (Best == null || result.TotalScore > Best.TotalScore)
                Best = new LeaderboardBest { runId = result.runId, commonScore = result.CommonScore, difficulty = result.difficulty };
        }

        internal static void Restore(LeaderboardBest best, SettlementResult settlement, IEnumerable<AdventureRecord> history)
        {
            Best = best;
            if (settlement != null) Record(settlement);
            foreach (var record in history.Where(record => record != null && (record.outcome == "已通关" || record.outcome == "挑战失败")))
            {
                var ending = GameContent.Endings.rows.FirstOrDefault(entry => entry.name == record.ending);
                int legacyEnding = Array.IndexOf(new[] { "森林的黎明", "斩断傀儡之丝", "与暴风龙的约定" }, record.ending);
                if (ending == null && legacyEnding >= 0) ending = GameContent.Endings.Get("ENDING_00" + (legacyEnding + 1));
                if (record.outcome == "已通关" && ending == null) continue;
                var candidate = new LeaderboardBest {
                    runId = record.id, difficulty = AdventureDifficulty.Clamp(record.difficulty),
                    commonScore = record.cleared * 200 + record.battles * 400 + (record.outcome == "已通关" ? 2000 + ending.priority * 500 : 0)
                };
                if (!string.IsNullOrEmpty(candidate.runId) && candidate.commonScore >= 0 && (Best == null || candidate.TotalScore > Best.TotalScore)) Best = candidate;
            }
        }

        public static LeaderboardEntry Player => new LeaderboardEntry {
            Id = "PLAYER", Name = PlayerName, IsPlayer = true,
            CommonScore = Best == null ? 0 : Best.commonScore, Difficulty = Best == null ? 1 : Best.difficulty,
            Score = Best == null ? 0 : Best.TotalScore
        };

        public static List<LeaderboardEntry> BuildEntries()
        {
            var asset = Resources.Load<TextAsset>("Data/SimulatedUsers");
            if (asset == null) throw new InvalidOperationException("请导入模拟用户 Excel 表");
            var users = JsonUtility.FromJson<SimulatedUserTable>(asset.text).users;
            var entries = users.Select(user => new LeaderboardEntry {
                Id = user.id, Name = user.name, CommonScore = user.commonScore, Difficulty = user.difficulty, Score = user.TotalScore
            }).ToList();
            if (Best != null) entries.Add(Player);
            entries = entries.OrderByDescending(entry => entry.Score).ThenBy(entry => entry.Id, StringComparer.Ordinal).ToList();
            for (int index = 0; index < entries.Count; index++)
                entries[index].Rank = index > 0 && entries[index].Score == entries[index - 1].Score ? entries[index - 1].Rank : index + 1;
            return entries;
        }
    }
}
