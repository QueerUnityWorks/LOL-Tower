using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElementalTactics.Editor
{
    public static class AudioIntegrationValidation
    {
        [MenuItem("Tools/Elemental Tactics/Validate Audio Setup")]
        public static void Validate() { Debug.Log(RunChecks()); }

        public static string RunChecks()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "请在编辑模式检查音频配置。");
            var catalog = Resources.Load<GameAudioCatalog>("Audio/GameAudioCatalog");
            Require(catalog != null, "音频配置未导入。");
            var tracks = (MusicTrack[])Enum.GetValues(typeof(MusicTrack));
            var cues = ((SoundCue[])Enum.GetValues(typeof(SoundCue))).Where(cue => cue != SoundCue.None).ToArray();
            Require(catalog.music.Length == tracks.Length && catalog.music.Select(entry => entry.track).Distinct().Count() == tracks.Length, "配乐配置有缺项或重复。");
            Require(catalog.sounds.Length == cues.Length && catalog.sounds.Select(entry => entry.cue).Distinct().Count() == cues.Length, "音效配置有缺项或重复。");
            foreach (var track in tracks) CheckClip(catalog.Find(track)?.clip, true);
            foreach (var cue in cues) CheckClip(catalog.Find(cue)?.clip, false);
            CheckSettings();
            return "音频检查通过：5 首配乐、36 个音效引用和导入方式正确，音量滑块可调节、静音并保存。设置预览：Temp/AudioIntegration/settings.png";
        }

        private static void CheckClip(AudioClip clip, bool music)
        {
            Require(clip != null && clip.length > 0, "音频引用缺失或无法解码。");
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip)) as AudioImporter;
            Require(importer != null, "缺少音频导入器：" + clip.name);
            Require(importer.defaultSampleSettings.loadType == (music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad), "音频加载方式错误：" + clip.name);
            Require(importer.defaultSampleSettings.preloadAudioData != music, "音频预加载方式错误：" + clip.name);
        }

        private static void CheckSettings()
        {
            bool hadMusic = PlayerPrefs.HasKey(GameAudio.MusicVolumeKey), hadEffects = PlayerPrefs.HasKey(GameAudio.EffectsVolumeKey);
            float savedMusic = PlayerPrefs.GetFloat(GameAudio.MusicVolumeKey), savedEffects = PlayerPrefs.GetFloat(GameAudio.EffectsVolumeKey);
            var previousFont = PrototypeUI.font;
            var preview = EditorSceneManager.NewPreviewScene();
            RenderTexture texture = null;
            try
            {
                PrototypeUI.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/ElementalTactics/Fonts/NotoSansCJKsc-Regular.otf");
                var canvasObject = new GameObject("Audio Settings Preview", typeof(RectTransform), typeof(Canvas));
                SceneManager.MoveGameObjectToScene(canvasObject, preview);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                var root = (RectTransform)canvas.transform;
                root.sizeDelta = new Vector2(900, 1600);
                var settings = SettingsPresentation.BuildSettings(root, () => { }, () => { });
                var sliders = settings.GetComponentsInChildren<Slider>();
                Require(sliders.Length == 2, "设置页应包含音乐和音效两个滑块。");
                var music = sliders.Single(slider => slider.name == "Music Volume");
                var effects = sliders.Single(slider => slider.name == "Effects Volume");
                float initialMusic = music.value, initialEffects = effects.value;
                music.value = effects.value = 0;
                Require(GameAudio.MusicVolume == 0 && GameAudio.EffectsVolume == 0, "音量滑块不能静音。");
                music.value = .42f;
                effects.value = .56f;
                Require(Mathf.Approximately(GameAudio.MusicVolume, .42f) && Mathf.Approximately(GameAudio.EffectsVolume, .56f), "音量修改未保存。");
                Require(settings.Find("Panel/背景音乐 Value").GetComponent<Text>().text == "42%", "音乐百分比没有更新。");
                Require(settings.Find("Panel/游戏音效 Value").GetComponent<Text>().text == "56%", "音效百分比没有更新。");
                music.value = initialMusic;
                effects.value = initialEffects;
                var cameraObject = new GameObject("Audio Preview Camera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, preview);
                var camera = cameraObject.GetComponent<Camera>();
                camera.scene = preview;
                camera.orthographic = true;
                camera.orthographicSize = 800;
                camera.transform.position = new Vector3(0, 0, -10);
                canvas.worldCamera = camera;
                texture = new RenderTexture(1080, 1920, 24);
                camera.targetTexture = texture;
                Canvas.ForceUpdateCanvases();
                Require(music.handleRect.rect.height >= 24 && music.handleRect.rect.height <= 48, "音量滑块拖柄超出触控区域。");
                camera.Render();
                var previousTexture = RenderTexture.active;
                var image = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
                try
                {
                    RenderTexture.active = texture;
                    image.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
                    image.Apply();
                    Directory.CreateDirectory("Temp/AudioIntegration");
                    File.WriteAllBytes("Temp/AudioIntegration/settings.png", image.EncodeToPNG());
                }
                finally { RenderTexture.active = previousTexture; UnityEngine.Object.DestroyImmediate(image); }
                camera.targetTexture = null;
            }
            finally
            {
                if (hadMusic) PlayerPrefs.SetFloat(GameAudio.MusicVolumeKey, savedMusic); else PlayerPrefs.DeleteKey(GameAudio.MusicVolumeKey);
                if (hadEffects) PlayerPrefs.SetFloat(GameAudio.EffectsVolumeKey, savedEffects); else PlayerPrefs.DeleteKey(GameAudio.EffectsVolumeKey);
                PlayerPrefs.Save();
                PrototypeUI.font = previousFont;
                if (texture != null) { texture.Release(); UnityEngine.Object.DestroyImmediate(texture); }
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
