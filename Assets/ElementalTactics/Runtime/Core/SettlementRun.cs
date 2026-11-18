using System;
using System.Globalization;
using System.Linq;

namespace ElementalTactics
{
    [Serializable]
    public sealed class SettlementResult
    {
        public string runId, endingId, endedAt;
        public string dailyDate;
        public int dailyCompletedTasks, dailyTaskCount;
        public bool victory;
        public int difficulty, floor, cleared, battles, elapsedSeconds;
        public string[] characters;
        public ItemSnapshot[] items;
        public int ExplorationScore => cleared * 200;
        public int BattleScore => battles * 400;
        public int EndingScore => victory ? 2000 + Ending.priority * 500 : 0;
        public int CommonScore => ExplorationScore + BattleScore + EndingScore;
        public double DifficultyFactor => AdventureDifficulty.Factor(difficulty);
        public int TotalScore => AdventureDifficulty.Scale(CommonScore, DifficultyFactor);
        public string Grade => TotalScore >= 10000 ? "S" : TotalScore >= 7500 ? "A" : TotalScore >= 5000 ? "B" : TotalScore >= 2500 ? "C" : "D";
        public string GradeName => TotalScore >= 10000 ? "卓越" : TotalScore >= 7500 ? "优秀" : TotalScore >= 5000 ? "良好" : TotalScore >= 2500 ? "成长" : "启程";
        public EndingRecord Ending => victory ? GameContent.Endings.Get(endingId) : null;
        public string Title => victory ? Ending.name : "远征失败";
        public long ItemCount => items.Sum(item => (long)item.quantity);
        public string Duration => elapsedSeconds >= 3600 ? TimeSpan.FromSeconds(elapsedSeconds).ToString(@"hh\:mm\:ss") : TimeSpan.FromSeconds(elapsedSeconds).ToString(@"mm\:ss");

        internal static SettlementResult Capture(bool victory, string endedAt)
        {
            var formation = RunState.RestoreFormation();
            var roster = RunState.Characters;
            var result = new SettlementResult {
                runId = AdventureRun.Id ?? Guid.NewGuid().ToString("N"), victory = victory,
                dailyDate = DailyChallengeState.IsDailyRun ? DailyChallengeState.Current.date : null,
                dailyTaskCount = DailyChallengeState.IsDailyRun ? DailyChallengeState.Tasks.Length : 0,
                dailyCompletedTasks = DailyChallengeState.IsDailyRun ? DailyChallengeState.Tasks.Count(task => DailyChallengeState.Status(task) == DailyTaskStatus.Completed) : 0,
                endingId = victory ? EndingRun.Completed.id : null, endedAt = endedAt,
                difficulty = AdventureRun.Difficulty,
                floor = MapRun.Pending >= 0 ? MapRun.Nodes[MapRun.Pending].Floor : MapRun.CurrentFloor,
                cleared = MapRun.ClearedCount,
                battles = MapRun.Nodes.Count(node => MapRun.IsCleared(node.Id) && node.Definition.type == NodeKind.Battle),
                characters = Enumerable.Range(0, 6).Select(slot => formation[slot] >= 0 ? roster[formation[slot]].Definition.id : null).ToArray(),
                items = InventoryState.Items.Select(item => new ItemSnapshot { id = item.Id, quantity = item.Quantity }).ToArray()
            };
            if (DateTimeOffset.TryParse(AdventureRun.StartedAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var started)
                && DateTimeOffset.TryParse(endedAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var ended))
                result.elapsedSeconds = (int)Math.Min(86399, Math.Max(0, (ended - started).TotalSeconds));
            return result;
        }

        internal void Validate()
        {
            if (string.IsNullOrEmpty(runId) || difficulty < 1 || difficulty > 10 || floor < 1 || floor > MapRun.FloorCount
                || cleared < 0 || cleared > 300 || battles < 0 || battles > cleared || elapsedSeconds < 0 || elapsedSeconds > 86399
                || characters == null || characters.Length != 6 || items == null || items.Any(item => item == null || item.quantity <= 0)
                || items.Select(item => item.id).Distinct().Count() != items.Length)
                throw new InvalidOperationException("远征结算存档内容无效");
            if (victory) GameContent.Endings.Get(endingId);
            foreach (string id in characters.Where(id => !string.IsNullOrEmpty(id))) GameContent.Characters.Get(id);
            foreach (var item in items) GameContent.Items.Get(item.id);
        }
    }

    public static class SettlementRun
    {
        public static SettlementResult Current { get; private set; }
        public static bool Pending { get; private set; }
        internal static void Reset() { Current = null; Pending = false; }
        internal static void Complete(bool victory, string endedAt)
        {
            if (Current != null) return;
            Current = SettlementResult.Capture(victory, endedAt);
            LeaderboardState.Record(Current);
            DailyChallengeState.RecordScore(Current.TotalScore);
            Pending = true;
        }
        internal static void Restore(SettlementResult result, bool pending) { Current = result; Pending = pending; }
        public static void Acknowledge() { Pending = false; }
    }
}
