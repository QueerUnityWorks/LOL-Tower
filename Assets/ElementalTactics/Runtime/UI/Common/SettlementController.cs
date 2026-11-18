using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ElementalTactics
{
    public sealed class SettlementController : MonoBehaviour
    {
        private SettlementResult result;
        private bool loading;

        public static bool TryOpen(Transform parent)
        {
            if (TutorialRun.Active || parent == null) return false;
            AdventureRun.FinishIfNeeded();
            if (!SettlementRun.Pending || SettlementRun.Current == null) return false;
            if (parent.Find("Expedition Settlement") != null) return true;
            var controller = new GameObject("Settlement Controller").AddComponent<SettlementController>();
            controller.transform.SetParent(parent, false);
            controller.result = SettlementRun.Current;
            SettlementPresentation.Build(parent, controller.result, controller.ReturnHome, controller.Restart);
            GameAudio.PlayMusic(controller.result.endingId == "ENDING_002" ? MusicTrack.Shadow : MusicTrack.Home);
            return true;
        }

        private void ReturnHome()
        {
            if (loading) return;
            loading = true;
            SettlementRun.Acknowledge();
            if (!string.IsNullOrEmpty(result.dailyDate)) DailyChallengeState.ReturnToRegular();
            AdventurePersistence.SaveNow();
            SceneManager.LoadSceneAsync("MainScene");
        }

        private void Restart()
        {
            if (loading) return;
            loading = true;
            SettlementRun.Acknowledge();
            if (!string.IsNullOrEmpty(result.dailyDate)) DailyChallengeState.Start(result.dailyDate);
            else
            {
                AdventureRun.SelectDifficulty(result.difficulty);
                AdventureRun.StartNew();
            }
            AdventurePersistence.SaveNow();
            SceneManager.LoadSceneAsync("MapScene");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) ReturnHome();
        }
    }
}
