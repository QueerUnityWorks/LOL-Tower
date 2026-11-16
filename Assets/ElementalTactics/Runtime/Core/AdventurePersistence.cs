using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ElementalTactics
{
    // Save after complete UI transactions, never from inside an inventory-change callback.
    public sealed class AdventurePersistence : MonoBehaviour
    {
        public static string SavePath
        {
            get
            {
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(EditorSaveDirectory)) return Path.Combine(EditorSaveDirectory, "adventure-v1.json");
#endif
                return Path.Combine(Application.persistentDataPath, "adventure-v1.json");
            }
        }
#if UNITY_EDITOR
        public static string EditorSaveDirectory { get; set; }
#endif
        public static string LastError { get; private set; }
        private static string lastJson;
        private float nextSave;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            AdventureRun.ClearProfile(); lastJson = null; LastError = null;
            Load();
            var host = new GameObject("Adventure Save Service");
            DontDestroyOnLoad(host); host.AddComponent<AdventurePersistence>(); host.AddComponent<TutorialGuide>();
        }
        public static bool Load()
        {
            if (!File.Exists(SavePath)) return false;
            try { AdventureRun.Restore(JsonUtility.FromJson<AdventureSave>(File.ReadAllText(SavePath))); LastError = null; return true; }
            catch (Exception error)
            {
                // Keep an unreadable save for recovery instead of overwriting the only copy.
                try { File.Copy(SavePath, SavePath + ".unreadable-" + DateTime.UtcNow.Ticks); } catch (Exception) { }
                LastError = "冒险存档无法读取，已保留原文件。";
                Debug.LogWarning(LastError + " " + error.Message); return false;
            }
        }
        public static bool SaveNow()
        {
            try
            {
                var json = JsonUtility.ToJson(AdventureRun.Capture());
                if (json == lastJson) return true;
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                string temp = SavePath + ".tmp";
                File.WriteAllText(temp, json);
                if (File.Exists(SavePath)) File.Replace(temp, SavePath, SavePath + ".bak"); else File.Move(temp, SavePath);
                lastJson = json; LastError = null; return true;
            }
            catch (Exception error) { LastError = "冒险进度暂时无法保存，请检查磁盘空间。"; Debug.LogWarning(LastError + " " + error.Message); return false; }
        }
        private void LateUpdate()
        {
            if (Time.unscaledTime < nextSave) return;
            nextSave = Time.unscaledTime + .5f;
            SaveNow();
        }
        private void OnEnable() { SceneManager.activeSceneChanged += SceneChanged; }
        private void OnDisable() { SceneManager.activeSceneChanged -= SceneChanged; }
        private void SceneChanged(Scene before, Scene after) { SaveNow(); }
        private void OnApplicationPause(bool paused) { if (paused) SaveNow(); }
        private void OnApplicationQuit() { SaveNow(); }
    }
}
