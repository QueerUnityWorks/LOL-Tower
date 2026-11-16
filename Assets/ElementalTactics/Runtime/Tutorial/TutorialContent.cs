using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ElementalTactics
{
    public static class TutorialContent
    {
        public const string Prefix = "TUTORIAL_";
        public const string SwordGood = "TUTORIAL_SWORD";
        public static readonly string[] NodeIds = { "TUTORIAL_BATTLE", "TUTORIAL_EVENT", "TUTORIAL_SHOP", "TUTORIAL_BOSS" };
        private static readonly Dictionary<Type, ScriptableObject> databases = new Dictionary<Type, ScriptableObject>();

        public static T CachedDatabase<T>() where T : ScriptableObject
            => databases.TryGetValue(typeof(T), out var database) && database != null ? (T)database : null;

        public static void Ensure()
        {
            Add(GameContent.Effects,
                new EffectRecord { id = "TUTORIAL_GOLD", type = EffectKind.AddGold, amount = 30 },
                new EffectRecord { id = "TUTORIAL_RECRUIT", type = EffectKind.GiveCharacter, targetId = "HERO_002", amount = 1 },
                new EffectRecord { id = "TUTORIAL_BELT", type = EffectKind.GiveItem, targetId = "ITEM_001", amount = 1 },
                new EffectRecord { id = "TUTORIAL_BLADE", type = EffectKind.GiveItem, targetId = "ITEM_003", amount = 1 });
            Add(GameContent.Events, new EventRecord {
                id = "TUTORIAL_CAMP", name = "林间的同行者", cg = "EventCG/village",
                description = "练习结束后，你在营地遇见了布隆。他准备了旅途补给，也愿意与你一同守护林地。选择一种帮助，结果会立即生效。",
                options = new[] {
                    new EventOptionRecord { id = "TUTORIAL_INVITE", text = "邀请布隆同行，并领取30金币", effectIds = new[] { "TUTORIAL_RECRUIT", "TUTORIAL_GOLD" } },
                    new EventOptionRecord { id = "TUTORIAL_SUPPLIES", text = "领取巨人腰带与30金币", effectIds = new[] { "TUTORIAL_BELT", "TUTORIAL_GOLD" } }
                }
            });
            Add(GameContent.Shops, new ShopRecord {
                id = "TUTORIAL_STORE", name = "新手补给站", description = "用金币购买伙伴、被动道具或经验，为最后的首领战做好准备。",
                background = "EventCG/village", offerCount = 3, refreshCost = 15, refreshIncrease = 5,
                goods = new[] {
                    new ShopGoodRecord { id = SwordGood, name = "暴风大剑", effectId = "TUTORIAL_BLADE", price = 15, stock = 1, isFixed = true, order = 1, weight = 1 },
                    new ShopGoodRecord { id = "TUTORIAL_EXPERIENCE", name = "海克斯经验宝典", effectId = "FX_EXP40", price = 20, stock = 1, isFixed = true, order = 2, weight = 1 },
                    new ShopGoodRecord { id = "TUTORIAL_PARTNER", name = "布隆", effectId = "TUTORIAL_RECRUIT", price = 30, stock = 1, isFixed = true, order = 3, weight = 1 }
                }
            });
            Add(GameContent.Pools,
                new PoolRecord { id = "TUTORIAL_PRIZE_BELT", poolId = "TUTORIAL_ITEMS", targetId = "ITEM_001", type = PrizeKind.Item, weight = 1, minQuantity = 1, maxQuantity = 1 },
                new PoolRecord { id = "TUTORIAL_PRIZE_BOW", poolId = "TUTORIAL_ITEMS", targetId = "ITEM_004", type = PrizeKind.Item, weight = 1, minQuantity = 1, maxQuantity = 1 });
            Add(GameContent.Rewards, new RewardRecord { id = "TUTORIAL_REWARD", goldBase = 35, experienceBase = 20 });
            Add(GameContent.Battles,
                new BattleRecord { id = "TUTORIAL_SKIRMISH", slot1 = "ENEMY_001", rewardId = "TUTORIAL_REWARD", itemPoolId = "TUTORIAL_ITEMS", itemCandidates = 2, characterCandidates = 0, chooseCount = 1 },
                new BattleRecord { id = "TUTORIAL_GUARDIAN", slot1 = "ENEMY_004", rewardId = "TUTORIAL_REWARD", itemPoolId = "TUTORIAL_ITEMS", itemCandidates = 2, characterCandidates = 0, chooseCount = 1 });
            string[] names = { "初战练习", "林间的同行者", "新手补给站", "林地守关者" };
            string[] contents = { "TUTORIAL_SKIRMISH", "TUTORIAL_CAMP", "TUTORIAL_STORE", "TUTORIAL_GUARDIAN" };
            for (int index = 0; index < NodeIds.Length; index++)
                Add(GameContent.Nodes, new NodeRecord { id = NodeIds[index], name = names[index], contentId = contents[index],
                    description = "新手教学专用节点", type = index == 1 ? NodeKind.Event : index == 2 ? NodeKind.Shop : NodeKind.Battle,
                    minFloor = 0, maxFloor = 0, minRow = index + 1, maxRow = index + 1, isFinal = index == 3 });
        }

        private static void Add<T>(RecordDatabase<T> database, params T[] records) where T : ContentRecord
        {
            var missing = records.Where(record => !database.rows.Any(existing => existing.id == record.id)).ToArray();
            if (missing.Length == 0) return;
            if (!databases.TryGetValue(database.GetType(), out var cached) || cached == null)
            {
                cached = ScriptableObject.CreateInstance(database.GetType());
                cached.name = database.name;
                databases[database.GetType()] = cached;
            }
            ((RecordDatabase<T>)cached).Replace(database.rows.Concat(missing).ToArray());
        }

        public static IReadOnlyList<MapNode> CreateMap()
        {
            Ensure();
            var result = new List<MapNode>();
            for (int index = 0; index < NodeIds.Length; index++)
            {
                var node = new MapNode(index, index, 0, GameContent.Nodes.Get(NodeIds[index]), 1, 0, 270 - index * 180, index % 2 == 0 ? 1 : -1, true);
                if (index < NodeIds.Length - 1) node.Next.Add(index + 1);
                result.Add(node);
            }
            return result.AsReadOnly();
        }

        public static List<CharacterDefinition> EnemyLineup(BattleRecord battle)
        {
            var result = battle.Slots.Select(id => string.IsNullOrEmpty(id) ? null : GameContent.Enemies.Get(id).Create()).ToList();
            var unit = result[0];
            bool boss = battle.id == "TUTORIAL_GUARDIAN";
            unit.displayName = boss ? "林地守关者" : "训练守卫";
            unit.maxHealth = boss ? 420 : 240;
            unit.attack = boss ? 24 : 12;
            unit.defense = boss ? 10 : 5;
            unit.speed = boss ? 32 : 28;
            unit.element = boss ? Element.Grass : Element.Fire;
            unit.role = Role.Warrior;
            unit.skillKind = SkillKind.SingleEnemy;
            unit.skillPower = 120;
            unit.skillName = "训练重击";
            unit.statusEffect = null;
            unit.trait = TraitKind.None;
            unit.bossMechanic = boss ? BossMechanic.BarrierCycle : BossMechanic.None;
            unit.bossPower = boss ? 25 : 0;
            unit.bossInterval = 2;
            return result;
        }
    }
}
