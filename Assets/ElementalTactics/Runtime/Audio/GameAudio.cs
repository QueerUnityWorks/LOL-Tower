using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElementalTactics
{
    public sealed class GameAudio : MonoBehaviour
    {
        public const string MusicVolumeKey = "ElementalTactics.Audio.Music";
        public const string EffectsVolumeKey = "ElementalTactics.Audio.Effects";
        private const float FadeDuration = 1.2f;
        private static GameAudio instance;
        private static bool quitting;
        private readonly AudioSource[] musicSources = new AudioSource[2];
        private readonly float[] musicWeights = new float[2];
        private readonly float[] fadeStarts = new float[2];
        private readonly float[] musicGains = new float[2];
        private readonly Dictionary<SoundCue, float> lastSounds = new Dictionary<SoundCue, float>();
        private Voice[] voices;
        private GameAudioCatalog catalog;
        private BattleController battle;
        private int activeMusic = -1;
        private float fadeTime = FadeDuration;
        private float saveAt = -1;
        private bool focused = true, applicationPaused, suspended, combatPaused;
        private Voice feedbackVoice;
        private int feedbackFrame = -1;
        private SoundCue feedbackCue;

        private sealed class Voice
        {
            public AudioSource source;
            public bool combat;
            public float gain, started;
        }

        public static float MusicVolume
        {
            get => ReadVolume(MusicVolumeKey, .25f);
            set => SetVolume(MusicVolumeKey, value);
        }
        public static float EffectsVolume
        {
            get => ReadVolume(EffectsVolumeKey, .7f);
            set => SetVolume(EffectsVolumeKey, value);
        }
        public static MusicTrack ExplorationTrack => MapRun.CurrentFloor >= 3 ? MusicTrack.Shadow : MusicTrack.Exploration;

        private static float ReadVolume(string key, float fallback)
        {
            float value = PlayerPrefs.GetFloat(key, fallback);
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp01(value);
        }

        private static void SetVolume(string key, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return;
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            var audio = Ensure();
            if (audio == null) { PlayerPrefs.Save(); return; }
            audio.ApplyVolumes();
            audio.saveAt = Time.unscaledTime + .35f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { instance = null; quitting = false; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap() { Ensure(); }

        private static GameAudio Ensure()
        {
            if (!Application.isPlaying || quitting) return null;
            if (instance == null) new GameObject("Game Audio").AddComponent<GameAudio>();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            catalog = Resources.Load<GameAudioCatalog>("Audio/GameAudioCatalog");
            if (catalog == null) Debug.LogError("缺少音频配置：Resources/Audio/GameAudioCatalog");
            for (int index = 0; index < musicSources.Length; index++)
            {
                musicSources[index] = CreateSource("Music " + index, true);
                musicSources[index].volume = 0;
            }
            voices = new Voice[9];
            for (int index = 0; index < voices.Length; index++)
                voices[index] = new Voice { source = CreateSource("Sound " + index, false), combat = index >= 3 };
            focused = Application.isFocused;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0;
            source.priority = loop ? 32 : 128;
            return source;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            battle = null;
            StopCombat();
            lastSounds.Clear();
            if (scene.name == "StartScene" || scene.name == "MainScene") PlayMusic(MusicTrack.Home);
            else if (scene.name == "MapScene" || scene.name == "LevelScene") PlayMusic(ExplorationTrack);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var button in root.GetComponentsInChildren<Button>(true))
                    if (button.GetComponent<UiAudioFeedback>() == null) UiAudioFeedback.Bind(button);
        }

        public static void PlayMusic(MusicTrack track)
        {
            var audio = Ensure();
            var entry = audio == null || audio.catalog == null ? null : audio.catalog.Find(track);
            if (entry == null || entry.clip == null) return;
            if (audio.activeMusic >= 0 && audio.musicSources[audio.activeMusic].clip == entry.clip) return;
            int next = audio.activeMusic == 0 ? 1 : 0;
            for (int index = 0; index < audio.musicSources.Length; index++) audio.fadeStarts[index] = audio.musicWeights[index];
            var source = audio.musicSources[next];
            source.Stop();
            source.clip = entry.clip;
            audio.musicWeights[next] = audio.fadeStarts[next] = 0;
            audio.musicGains[next] = entry.gain;
            source.volume = 0;
            source.Play();
            if (!audio.focused || audio.applicationPaused) source.Pause();
            audio.activeMusic = next;
            audio.fadeTime = 0;
        }

        public static void Play(SoundCue cue)
        {
            if (cue == SoundCue.None) return;
            var audio = Ensure();
            if (audio == null || audio.catalog == null || !audio.focused || audio.applicationPaused || EffectsVolume <= 0) return;
            bool combat = cue >= SoundCue.BladeOne && cue <= SoundCue.Energy;
            if (combat && audio.battle != null && audio.battle.IsPaused) return;
            if (audio.lastSounds.TryGetValue(cue, out float last) && Time.unscaledTime - last < .06f) return;
            var entry = audio.catalog.Find(cue);
            if (entry == null || entry.clip == null) return;
            if (!combat && audio.feedbackFrame == Time.frameCount)
            {
                if (FeedbackPriority(cue) < FeedbackPriority(audio.feedbackCue)) return;
                if (audio.feedbackVoice != null) audio.feedbackVoice.source.Stop();
            }
            var voice = audio.voices.Where(candidate => candidate.combat == combat)
                .OrderBy(candidate => candidate.source.isPlaying ? 1 : 0).ThenBy(candidate => candidate.started).First();
            voice.source.Stop();
            voice.source.clip = entry.clip;
            voice.gain = entry.gain;
            voice.started = Time.unscaledTime;
            voice.source.volume = EffectsVolume * voice.gain;
            voice.source.Play();
            audio.lastSounds[cue] = Time.unscaledTime;
            if (!combat)
            {
                audio.feedbackVoice = voice;
                audio.feedbackFrame = Time.frameCount;
                audio.feedbackCue = cue;
            }
        }

        public static void BeginBattle(BattleController controller, bool boss)
        {
            var audio = Ensure();
            if (audio == null) return;
            audio.StopCombat();
            audio.battle = controller;
            PlayMusic(boss ? MusicTrack.Boss : MusicTrack.Battle);
            Play(boss ? SoundCue.BossRoar : SoundCue.SwordDraw);
        }

        public static void EndBattle(bool victory, bool levelUp)
        {
            var audio = Ensure();
            if (audio == null) return;
            audio.battle = null;
            audio.StopCombat();
            PlayMusic(ExplorationTrack);
            Play(victory ? SoundCue.Victory : SoundCue.Defeat);
            if (levelUp) audio.StartCoroutine(audio.DelayedSound(SoundCue.LevelUp, .9f));
        }

        private IEnumerator DelayedSound(SoundCue cue, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            Play(cue);
        }

        public static void PlayReward(IEnumerable<EffectRecord> effects, int previousLevel, SoundCue fallback = SoundCue.Coins)
        {
            if (!Application.isPlaying) return;
            var rewards = effects.ToArray();
            var cue = Progression.Level > previousLevel ? SoundCue.LevelUp
                : rewards.Any(effect => effect.type == EffectKind.GiveCharacter) ? SoundCue.Recruit
                : rewards.Any(effect => effect.type == EffectKind.GiveItem && RarityRules.Of(effect) != Rarity.Common) ? SoundCue.Rare
                : fallback == SoundCue.Purchase ? SoundCue.Purchase
                : rewards.Any(effect => effect.type == EffectKind.GiveItem) ? SoundCue.Item : fallback;
            Play(cue);
        }

        private void StopCombat()
        {
            foreach (var voice in voices) if (voice.combat) voice.source.Stop();
            combatPaused = false;
        }

        private void LateUpdate()
        {
            bool suspend = !focused || applicationPaused;
            bool pauseCombat = battle != null && battle.IsBattling && battle.IsPaused;
            if (suspend != suspended || pauseCombat != combatPaused)
            {
                foreach (var source in musicSources) { if (suspend) source.Pause(); else source.UnPause(); }
                foreach (var voice in voices)
                {
                    if (suspend || (voice.combat && pauseCombat)) voice.source.Pause();
                    else voice.source.UnPause();
                }
                suspended = suspend;
                combatPaused = pauseCombat;
            }
            if (!suspended && fadeTime < FadeDuration)
            {
                fadeTime = Mathf.Min(FadeDuration, fadeTime + Time.unscaledDeltaTime);
                for (int index = 0; index < musicSources.Length; index++)
                {
                    musicWeights[index] = Mathf.Lerp(fadeStarts[index], index == activeMusic ? 1 : 0, fadeTime / FadeDuration);
                    if (fadeTime >= FadeDuration && index != activeMusic) musicSources[index].Stop();
                }
            }
            ApplyVolumes();
            if (saveAt >= 0 && Time.unscaledTime >= saveAt) SaveSettings();
        }

        private void ApplyVolumes()
        {
            float musicVolume = MusicVolume * (combatPaused ? .4f : 1);
            float effectsVolume = EffectsVolume;
            for (int index = 0; index < musicSources.Length; index++)
                musicSources[index].volume = musicVolume * musicGains[index] * musicWeights[index];
            foreach (var voice in voices) voice.source.volume = effectsVolume * voice.gain;
        }

        private static int FeedbackPriority(SoundCue cue) => cue == SoundCue.Click ? 0
            : cue == SoundCue.CardDrop || cue == SoundCue.Confirm || cue == SoundCue.Toggle ? 2
            : cue <= SoundCue.CardSelect ? 1 : 3;

        private void SaveSettings() { if (saveAt < 0) return; PlayerPrefs.Save(); saveAt = -1; }
        private void OnApplicationFocus(bool hasFocus) { focused = hasFocus; if (!hasFocus) SaveSettings(); }
        private void OnApplicationPause(bool paused) { applicationPaused = paused; if (paused) SaveSettings(); }
        private void OnApplicationQuit() { quitting = true; SaveSettings(); }
        private void OnDestroy()
        {
            if (instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SaveSettings();
            instance = null;
        }
    }
}
