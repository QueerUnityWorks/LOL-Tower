using System;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    public sealed class MainHubPanels : MonoBehaviour
    {
        private RectTransform root, achievements, settings, leaderboard, daily, active;
        private bool dailyLoading;
        private Font font;
        public bool IsOpen => active != null && active.gameObject.activeSelf;
        public void Initialize(RectTransform parent, Font face) { root=parent; font=face; }
        public void OpenAchievements()
        {
            PrototypeUI.font=font;
            if(achievements!=null) { achievements.gameObject.SetActive(false); Destroy(achievements.gameObject); }
            achievements=AchievementsPresentation.Build(root, Close, OpenHistory);
            Open(achievements);
        }
        public void OpenSettings()
        {
            PrototypeUI.font=font;
            if(settings==null) settings=BuildSettings();
            Open(settings);
        }
        public void OpenLeaderboard()
        {
            PrototypeUI.font=font;
            if(leaderboard!=null) { leaderboard.gameObject.SetActive(false); Destroy(leaderboard.gameObject); }
            leaderboard=LeaderboardPresentation.Build(root, Close);
            Open(leaderboard);
        }
        private void Open(RectTransform panel)
        {
            Close(); active=panel; panel.gameObject.SetActive(true); panel.SetAsLastSibling();
            GameAudio.Play(SoundCue.PanelOpen);
        }
        public void OpenDailyChallenge()
        {
            PrototypeUI.font=font;
            if(daily!=null) { daily.gameObject.SetActive(false); Destroy(daily.gameObject); }
            daily=DailyChallengePresentation.Build(root, Close, StartDailyChallenge);
            Open(daily);
        }
        private void StartDailyChallenge()
        {
            if(dailyLoading || TutorialRun.Active) return;
            dailyLoading=true;
            if(!DailyChallengeState.IsDailyRun || !AdventureRun.Active) DailyChallengeState.Start(DailyChallengeState.Today);
            string scene=AdventureRun.ContinueScene;
            AdventurePersistence.SaveNow();
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scene);
        }
        public void Close()
        {
            if(IsOpen) GameAudio.Play(SoundCue.PanelClose);
            if(active!=null) { active.gameObject.SetActive(false); if(active.name=="History Overlay" || active.name=="Instructions Overlay") Destroy(active.gameObject); }
            active=null;
        }
        private void Update()
        {
            if(!IsOpen || !Input.GetKeyDown(KeyCode.Escape)) return;
            if(active.name=="Instructions Overlay") OpenSettings(); else Close();
        }
        private RectTransform BuildSettings()
            => SettingsPresentation.BuildSettings(root, Close, OpenInstructions);

        public void OpenInstructions()
        {
            PrototypeUI.font=font;
            Open(SettingsPresentation.BuildInstructions(root, OpenSettings));
        }

        public void OpenHistory()
        {
            AdventureRun.FinishIfNeeded();
            Open(AchievementsPresentation.History(root, Close, OpenAchievements));
        }
    }
}
