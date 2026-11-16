using System;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    // Settings controls remain visual placeholders; instructions and adventure records are functional.
    public sealed class MainHubPanels : MonoBehaviour
    {
        private RectTransform root, achievements, settings, active;
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
        private void Open(RectTransform panel)
        {
            Close(); active=panel; panel.gameObject.SetActive(true); panel.SetAsLastSibling();
        }
        public void Close()
        {
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
