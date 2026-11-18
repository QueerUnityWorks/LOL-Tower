using System;
using System.Collections.Generic;
using System.Linq;

namespace ElementalTactics
{
    [Serializable]
    public sealed class TutorialSave
    {
        public bool active, completed, dismissed, homeRequested;
        public string[] seen = Array.Empty<string>();
        public RunSnapshot run;
    }

    public static class TutorialRun
    {
        private static readonly HashSet<string> seen = new HashSet<string>();
        private static AdventureSave suspended;
        public static bool Active { get; private set; }
        public static bool Completed { get; private set; }
        public static bool Dismissed { get; private set; }
        public static bool HomeRequested { get; private set; }
        public static bool ShowHome => !Active && !Dismissed && !SettlementRun.Pending && !DailyChallengeState.IsDailyRun && (!AdventureRun.Active || HomeRequested);
        public static bool Finished => Active && MapRun.IsCleared(3);
        public static bool HasSeen(string key) => seen.Contains(key);
        public static void Mark(string key) { if (Active || ShowHome) seen.Add(key); }

        internal static void ClearProfile()
        {
            Active = Completed = Dismissed = HomeRequested = false;
            suspended = null;
            seen.Clear();
        }

        public static void Replay()
        {
            if (Active) Exit(false);
            seen.Clear(); Dismissed = false; HomeRequested = true;
        }

        public static void Begin()
        {
            if (Active) return;
            TutorialContent.Ensure();
            suspended = CopyProfile(AdventureRun.Capture());
            MapRun.ResetWithSeed(20261113);
            Active = true; Dismissed = false; HomeRequested = false;
            RunState.Ensure();
            RunState.Apply(new[] { new EffectRecord { type = EffectKind.AddGold, amount = 30 } });
        }

        public static void Exit(bool complete)
        {
            var original = suspended;
            Active = false; Dismissed = true; HomeRequested = false;
            Completed |= complete;
            var state = Capture();
            if (original == null) { suspended = null; return; }
            original.tutorial = state;
            AdventureRun.Restore(original);
        }

        internal static TutorialSave Capture() => new TutorialSave {
            active = Active, completed = Completed, dismissed = Dismissed, homeRequested = HomeRequested,
            seen = seen.OrderBy(key => key).ToArray(), run = Active ? RunSnapshot.Capture() : null
        };

        internal static AdventureSave CaptureProfile()
        {
            var profile = CopyProfile(suspended);
            profile.tutorial = Capture();
            if (RunState.IsDefeated)
            {
                profile.tutorial.active = false;
                profile.tutorial.dismissed = true;
                profile.tutorial.run = null;
            }
            return profile;
        }

        private static AdventureSave CopyProfile(AdventureSave profile) => new AdventureSave {
            version = profile.version, active = profile.active, selectedDifficulty = profile.selectedDifficulty,
            difficulty = profile.difficulty, id = profile.id, startedAt = profile.startedAt,
            history = profile.history, run = profile.run, personalBest = profile.personalBest,
            settlement = profile.settlement, hasSettlement = profile.hasSettlement, settlementPending = profile.settlementPending,
            hasDailyChallenge = profile.hasDailyChallenge, dailyChallenge = profile.dailyChallenge, dailyRecords = profile.dailyRecords
        };

        internal static void Validate(TutorialSave state)
        {
            if (state == null) return;
            if (state.seen == null || state.seen.Any(key => string.IsNullOrEmpty(key)) || state.seen.Length > 150)
                throw new InvalidOperationException("新手教程进度无效");
            if (!state.active) return;
            TutorialContent.Ensure();
            if (state.run == null || state.run.nodes == null || state.run.nodes.Length != 4)
                throw new InvalidOperationException("新手教程地图不完整");
            state.run.Validate();
            for (int index = 0; index < 4; index++)
            {
                var node = state.run.nodes[index];
                if (node.definition != TutorialContent.NodeIds[index] || node.floor != 1 || node.row != index
                    || !node.next.SequenceEqual(index == 3 ? Array.Empty<int>() : new[] { index + 1 }))
                    throw new InvalidOperationException("新手教程路线无效");
            }
        }

        internal static void Restore(TutorialSave state, AdventureSave profile)
        {
            if (state == null) return;
            Completed = state.completed; Dismissed = state.dismissed; HomeRequested = state.homeRequested;
            seen.UnionWith(state.seen);
            if (!state.active) return;
            suspended = CopyProfile(profile);
            MapRun.ResetWithSeed(state.run.seed);
            Active = true;
            state.run.Restore();
        }
    }
}
