using System;
using System.Collections.Generic;
using System.Linq;

namespace ElementalTactics
{
    public static class AdventureDifficulty
    {
        public static int Clamp(int level) => Math.Max(1, Math.Min(10, level));
        public static double Factor(int level) { int n = Clamp(level) - 1; return (100 + 10 * n + n * n) / 100.0; }
        public static int Scale(int value, double factor, int minimum = 0)
            => (int)Math.Max(minimum, Math.Min(int.MaxValue, decimal.Round(value * (decimal)factor, MidpointRounding.AwayFromZero)));
        public static string Name(int level) => new[] { "初踏旅途", "森林试炼", "危机渐起", "深入险境", "强敌环伺", "绝境远征", "命运逆流", "破败前线", "极限挑战", "终焉试炼" }[Clamp(level) - 1];
    }

    [Serializable] public sealed class AdventureRecord
    {
        public string id, startedAt, endedAt, outcome, ending;
        public int difficulty, floor, cleared, battles, heroes, items;
    }

    public static class AdventureRun
    {
        public static bool Active { get; private set; }
        public static int SelectedDifficulty { get; private set; } = 1;
        public static int Difficulty { get; private set; } = 1;
        public static string Id { get; private set; }
        public static string StartedAt { get; private set; }
        public static bool ResumeEncounter { get; set; }
        private static readonly List<AdventureRecord> history = new List<AdventureRecord>();
        public static IReadOnlyList<AdventureRecord> History => history.AsReadOnly();
        public static void SelectDifficulty(int level) { if (!Active) SelectedDifficulty = AdventureDifficulty.Clamp(level); }
        public static void StartNew()
        {
            if (Active) return;
            MapRun.Reset();
            Difficulty = SelectedDifficulty;
            Id = Guid.NewGuid().ToString("N"); StartedAt = DateTime.UtcNow.ToString("o");
            Active = true;
            RunState.Ensure();
        }
        // MapRun's test/debug resets must also reset the locked multiplier.
        internal static void ResetCurrent() { Active = false; Difficulty = 1; Id = null; StartedAt = null; ResumeEncounter = false; }
        public static void ClearProfile() { ResetCurrent(); SelectedDifficulty = 1; history.Clear(); TutorialRun.ClearProfile(); }
        public static string ContinueScene
        {
            get
            {
                bool battle = MapRun.Pending >= 0 && RewardFlow.Pending == null && MapRun.Nodes[MapRun.Pending].Definition.type == NodeKind.Battle;
                ResumeEncounter = !battle;
                return battle ? "LevelScene" : "MapScene";
            }
        }
        public static void FinishIfNeeded()
        {
            if (!Active || RewardFlow.Pending != null) return;
            if (RunState.IsDefeated) Archive("挑战失败");
            else if (MapRun.Finished) Archive("已通关");
        }
        public static void Abandon() { if (!Active) return; Archive("已放弃"); MapRun.Reset(); }
        private static void Archive(string outcome)
        {
            if (!history.Any(entry => entry.id == Id)) history.Insert(0, new AdventureRecord {
                id = Id, startedAt = StartedAt, endedAt = DateTime.UtcNow.ToString("o"), outcome = outcome,
                ending = EndingRun.Completed == null ? "" : EndingRun.Completed.name, difficulty = Difficulty,
                floor = MapRun.CurrentFloor, cleared = MapRun.ClearedCount,
                battles = MapRun.Nodes.Count(node => MapRun.IsCleared(node.Id) && node.Definition.type == NodeKind.Battle),
                heroes = RunState.Characters.Select(c => c.Definition.id).Distinct().Count(), items = InventoryState.Items.Count });
            if (history.Count > 50) history.RemoveRange(50, history.Count - 50);
            Active = false;
        }
        public static AdventureSave Capture()
        {
            if (TutorialRun.Active) return TutorialRun.CaptureProfile();
            FinishIfNeeded();
            return new AdventureSave { selectedDifficulty = SelectedDifficulty, difficulty = Active ? Difficulty : 1, active = Active,
                id = Active ? Id : null, startedAt = Active ? StartedAt : null, history = history.ToArray(), run = Active ? RunSnapshot.Capture() : null, tutorial = TutorialRun.Capture() };
        }
        public static void Restore(AdventureSave save)
        {
            if (save == null || save.version != 1 || save.selectedDifficulty < 1 || save.selectedDifficulty > 10 || save.history == null)
                throw new InvalidOperationException("冒险存档版本或内容无效");
            TutorialRun.Validate(save.tutorial);
            if (save.active)
            {
                if (string.IsNullOrEmpty(save.id) || save.difficulty < 1 || save.difficulty > 10 || save.run == null)
                    throw new InvalidOperationException("冒险记录不完整");
                save.run.Validate();
            }
            // Validation happens before mutating any live state.
            MapRun.Reset(); ClearProfile();
            SelectedDifficulty = save.selectedDifficulty;
            history.AddRange(save.history.Where(r => r != null).Take(50));
            if (save.active)
            {
                save.run.Restore();
                Difficulty = save.difficulty; Id = save.id; StartedAt = save.startedAt; Active = true;
            }
            TutorialRun.Restore(save.tutorial, save);
        }
    }
    [Serializable] public sealed class AdventureSave
    {
        public int version = 1, selectedDifficulty = 1, difficulty = 1;
        public bool active;
        public string id, startedAt;
        public AdventureRecord[] history;
        public RunSnapshot run;
        public TutorialSave tutorial;
    }
}
