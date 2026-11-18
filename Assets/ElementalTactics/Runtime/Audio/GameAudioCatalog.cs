using System;
using UnityEngine;

namespace ElementalTactics
{
    public enum MusicTrack { Home, Exploration, Shadow, Battle, Boss }
    public enum SoundCue
    {
        None = -1,
        Click, Back, PanelOpen, PanelClose, CardSelect, CardDrop, Confirm, Error, Toggle,
        MapStep, NodeUnlock, Purchase, Coins, Item, Rare, InventoryOpen, CodexPage,
        BladeOne, BladeTwo, SwordDraw, WindSlash, HitMetal, EnemyHurt, EnemyDie, BossRoar,
        ArcaneCast, ArcaneHit, FireCast, FireHit, Heal, Shield, Energy,
        Victory, Defeat, LevelUp, Recruit
    }

    [CreateAssetMenu(menuName = "Elemental Tactics/Audio Catalog")]
    public sealed class GameAudioCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class MusicEntry
        {
            public MusicTrack track;
            public AudioClip clip;
            [Range(0, 1)] public float gain = 1;
        }

        [Serializable]
        public sealed class SoundEntry
        {
            public SoundCue cue;
            public AudioClip clip;
            [Range(0, 1)] public float gain = 1;
        }

        public MusicEntry[] music = Array.Empty<MusicEntry>();
        public SoundEntry[] sounds = Array.Empty<SoundEntry>();

        public MusicEntry Find(MusicTrack track) => Array.Find(music, entry => entry.track == track);
        public SoundEntry Find(SoundCue cue) => Array.Find(sounds, entry => entry.cue == cue);
    }
}
