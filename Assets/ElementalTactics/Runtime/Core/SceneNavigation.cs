using UnityEngine;
using UnityEngine.SceneManagement;

namespace ElementalTactics
{
    public class SceneNavigation : MonoBehaviour
    {
        private bool loading;

        private void Start()
        {
            if (SceneManager.GetActiveScene().name != "MapScene") return;
            var layout = FindObjectOfType<PortraitLayout>();
            if (layout == null) return;
            var label = layout.content.GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (label != null) PrototypeUI.font = label.font;
            foreach (Transform child in layout.content) child.gameObject.SetActive(false);
            var root = PrototypeUI.Rect("Journey Map", layout.content, Vector2.zero, new Vector2(900, 1600));
            gameObject.AddComponent<MapController>().Build(root);
        }

        public void OpenMap()
        {
            // Existing StartScene buttons retain their serialized OpenMap binding.
            if (SceneManager.GetActiveScene().name == "StartScene") { OpenMain(); return; }
            if (SceneManager.GetActiveScene().name == "MainScene")
            {
                if (!AdventureRun.Active) AdventureRun.StartNew();
                Open(AdventureRun.ContinueScene); return;
            }
            Open("MapScene");
        }
        public void OpenMain() { Open("MainScene"); }
        public void OpenLevel() { Open("LevelScene"); }
        public void OpenStart() { Open("StartScene"); }

        private void Open(string scene)
        {
            if (loading) return;
            loading = true;
            AdventurePersistence.SaveNow();
            SceneManager.LoadSceneAsync(scene);
        }
    }
}
