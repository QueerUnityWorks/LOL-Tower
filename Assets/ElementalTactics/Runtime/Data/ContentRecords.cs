using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ElementalTactics
{
    public enum NodeKind { Battle, Event, Shop }
    public enum EffectKind { GiveItem, GiveCharacter, AddGold, SpendGold, AttackBonus, HealthBonus, DefenseBonus, SpeedBonus, DoubleAttack, AddExperience, OpeningShield, Regeneration, Lifesteal, HealthPenalty }
    public enum PrizeKind { Item, Character }
    [Serializable] public class ContentRecord { public string id; }
    [Serializable] public class UnitRecord : ContentRecord
    {
        public string name;
        public string skillName, biography, appearanceId, imagePath;
        public Role role;
        public Element element;
        public Rarity rarity;
        public int maxHealth, attack, defense, speed;
        public SkillKind skillKind;
        public string statusEffect;
        public int statusTurns, statusPower;
        public bool statusAllEnemies;
        public TraitKind trait;
        public int skillPower, traitPower;
        public BossMechanic bossMechanic;
        public int bossPower, bossInterval;
        public CharacterDefinition Create()
        {
            return new CharacterDefinition { id = id, displayName = name, role = role, element = element, rarity = rarity,
                skillName = skillName, biography = biography, appearanceId = appearanceId, imagePath = imagePath,
                skillKind = skillKind, skillPower = skillPower, statusEffect = statusEffect,
                statusTurns = statusTurns, statusPower = statusPower, statusAllEnemies = statusAllEnemies,
                trait = trait, traitPower = traitPower,
                bossMechanic = bossMechanic, bossPower = bossPower, bossInterval = bossInterval,
                maxHealth = maxHealth, attack = attack, defense = defense, speed = speed };
        }
    }
    [Serializable] public class ItemRecord : ContentRecord { public string name, description, kind, effectId, imagePath; public Rarity rarity; }
    [Serializable] public class LevelRecord : ContentRecord
    {
        public int level, experienceToNext, commonWeight, rareWeight, epicWeight;
    }
    [Serializable] public class NodeRecord : ContentRecord
    {
        public string name, description, contentId;
        public NodeKind type;
        public int minRow, maxRow, weight;
        public int minFloor = 1, maxFloor = 3;
        public bool isFinal;
        public bool uniquePerRun, guaranteed;
        public string TypeName => type == NodeKind.Battle ? "战斗" : type == NodeKind.Event ? "事件" : "商店";
    }
    [Serializable] public class BattleRecord : ContentRecord
    {
        public string slot1, slot2, slot3, slot4, slot5, slot6, rewardId;
        public string itemPoolId, characterPoolId;
        public int itemCandidates = -1, characterCandidates = -1, chooseCount = -1;
        public string[] Slots => new[] { slot1, slot2, slot3, slot4, slot5, slot6 };
    }
    [Serializable] public class EventRecord : ContentRecord
    {
        public string name, cg, description;
        public EventOptionRecord[] options;
    }
    [Serializable] public class EventOptionRecord : ContentRecord
    {
        public string eventId, text;
        public int order;
        public string[] effectIds;
    }
    [Serializable] public class EffectRecord : ContentRecord { public EffectKind type; public string targetId; public int amount, bonusAttack; }
    [Serializable] public class RewardRecord : ContentRecord
    {
        public string itemPoolId, characterPoolId;
        public int itemCandidates, characterCandidates, chooseCount, goldBase, goldPerNode, experienceBase, experiencePerNode;
    }
    [Serializable] public class PoolRecord : ContentRecord
    {
        public string poolId, targetId;
        public PrizeKind type;
        public int weight, minQuantity, maxQuantity;
    }
    [Serializable] public class FloorRecord : ContentRecord
    {
        public int floor, battleWeight, eventWeight, shopWeight, maxShops, healthPercent, attackPercent;
        public string name;
        public int rows = 7;
        public bool optional;
    }
    [Serializable] public class EndingRecord : ContentRecord
    {
        public string name, requiredItemId, fragmentItemId, bossNodeId, description, epilogue;
        public int priority, requiredWins, extraFloor;
    }
    [Serializable] public class DifficultyRecord : ContentRecord
    {
        public string name, itemPoolId, characterPoolId;
        public int minPower, itemCandidates, characterCandidates, chooseCount;
    }
    [Serializable] public class ShopRecord : ContentRecord
    {
        public string name, description, background;
        public int offerCount, refreshCost, refreshIncrease;
        public ShopGoodRecord[] goods;
    }
    [Serializable] public class ShopGoodRecord : ContentRecord
    {
        public string shopId, name, effectId;
        public int price, stock, order;
        public int weight;
        public bool isFixed;
    }
    public abstract class RecordDatabase<T> : ScriptableObject where T : ContentRecord
    {
        public T[] rows = Array.Empty<T>();
        private Dictionary<string, T> index;
        public void Replace(T[] records) { rows = records; index = null; }
        public T Get(string id)
        {
            if (index == null) index = rows.ToDictionary(row => row.id, StringComparer.Ordinal);
            if (!index.TryGetValue(id, out T value)) throw new InvalidOperationException(name + "中找不到ID：" + id);
            return value;
        }
        protected virtual void OnEnable() { index = null; }
    }
}
