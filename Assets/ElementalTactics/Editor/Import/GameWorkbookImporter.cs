using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace ElementalTactics.Editor
{
    public static class GameWorkbookImporter
    {
        public const string WorkbookPath = "Assets/ElementalTactics/Data/GameContent.xlsx";
        private const string LibraryPath = "Assets/ElementalTactics/Resources/Data";
        private static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly HashSet<string> ConfigurationSheets = new HashSet<string>
        {
            "敌人", "角色", "道具", "节点", "战斗关卡", "事件", "事件选项", "效果", "奖励", "奖励池", "商店", "商店商品", "等级", "楼层规则", "难度掉落", "结局规则"
        };

        [MenuItem("Tools/Elemental Tactics/Import Game Workbook")]
        public static void Import()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("请先退出运行模式，再导入配置。");
            var tables = ReadWorkbook(WorkbookPath);
            var enemies = Read<UnitRecord>(tables, "敌人");
            var characters = Read<UnitRecord>(tables, "角色");
            var items = Read<ItemRecord>(tables, "道具");
            var nodes = Read<NodeRecord>(tables, "节点");
            var battles = Read<BattleRecord>(tables, "战斗关卡");
            var events = Read<EventRecord>(tables, "事件");
            var options = Read<EventOptionRecord>(tables, "事件选项");
            var effects = Read<EffectRecord>(tables, "效果");
            var rewards = Read<RewardRecord>(tables, "奖励");
            var pools = Read<PoolRecord>(tables, "奖励池");
            var shops = Read<ShopRecord>(tables, "商店");
            var goods = Read<ShopGoodRecord>(tables, "商店商品");
            var levels = Read<LevelRecord>(tables, "等级").OrderBy(row => row.level).ToArray();
            var floors = Read<FloorRecord>(tables, "楼层规则").OrderBy(row => row.floor).ToArray();
            var difficulties = Read<DifficultyRecord>(tables, "难度掉落").OrderBy(row => row.minPower).ToArray();
            var endings = Read<EndingRecord>(tables, "结局规则").OrderBy(row => row.priority).ToArray();
            // Enemy design ranks are maintained in the paired handbook snapshot,
            // separately from reward rarity. Validate before writing any databases.
            if (File.Exists(HandbookSyncImporter.SnapshotPath))
            {
                var ranks = JsonUtility.FromJson<HandbookSnapshot>(File.ReadAllText(HandbookSyncImporter.SnapshotPath));
                Check(ranks != null && ranks.units != null, "手册敌人分类快照无效。");
                foreach (var battle in battles)
                    StageRules.ValidatePopulation(battle, nodes, floors, id => ranks.units.FirstOrDefault(u => u.isEnemy && u.id == id)?.quality);
            }
            Check(floors.Length == 4 && floors.Select(row => row.floor).SequenceEqual(new[] { 1, 2, 3, 4 }), "楼层规则必须配置第1至4层");
            foreach (var floor in floors)
            {
                Check(floor.optional == (floor.floor == 4) && floor.rows == (floor.optional ? 4 : 7), floor.id + "：前三层7排，隐藏第四层3排普通节点加1排首领");
                Check(!string.IsNullOrWhiteSpace(floor.name) && floor.maxShops >= 0 && floor.maxShops <= 2,
                    floor.id + "：楼层名称不能为空，每层商店上限为0至2");
                Check(floor.battleWeight > 0 && floor.eventWeight > 0 && floor.shopWeight >= 0
                    && floor.battleWeight <= 100000 && floor.eventWeight <= 100000 && floor.shopWeight <= 100000,
                    floor.id + "：节点类型权重无效");
                Check(floor.healthPercent >= 100 && floor.healthPercent <= 1000 && floor.attackPercent >= 100 && floor.attackPercent <= 1000,
                    floor.id + "：敌人生命与攻击倍率需在100至1000百分比之间");
            }
            Check(levels.Length >= 3 && levels.Length <= 100, "等级表需3至100级");
            for (int index = 0; index < levels.Length; index++)
            {
                var level = levels[index];
                Check(level.level == index + 1 && level.experienceToNext >= 0 && level.experienceToNext <= 1000000
                    && (level.experienceToNext == 0) == (index == levels.Length - 1), "等级必须从1连续递增，仅末级经验为0");
                Check(level.commonWeight > 0 && level.rareWeight > 0 && level.epicWeight > 0
                    && level.commonWeight + level.rareWeight + level.epicWeight == 100, level.id + "：三级概率须为正整数且合计100");
                if (index > 0) Check(level.commonWeight <= levels[index - 1].commonWeight && level.epicWeight >= levels[index - 1].epicWeight,
                    level.id + "：升级后普通概率不能增加，史诗概率不能降低");
            }
            foreach (var unit in enemies.Concat(characters))
            {
                ValidateArt(unit.imagePath, unit.id + " / imagePath");
                Check(unit.maxHealth > 0 && unit.attack > 0 && unit.defense >= 0 && unit.speed > 0 && !string.IsNullOrWhiteSpace(unit.name), unit.id + "：单位数值无效");
                Check(unit.skillKind == SkillKind.RoleDefault ? unit.skillPower == 0 : unit.skillPower > 0 && unit.skillPower <= 500,
                    unit.id + "：职业默认技能数值填0，自定义技能填1至500");
                if (string.IsNullOrEmpty(unit.statusEffect))
                    Check(unit.statusTurns == 0 && unit.statusPower == 0 && !unit.statusAllEnemies, unit.id + "：无状态技能时状态参数须为0/false");
                else
                {
                    BattleStatusKind status;
                    Check(Enum.TryParse(unit.statusEffect, out status) && Enum.IsDefined(typeof(BattleStatusKind), status), unit.id + "：状态种类无效");
                    if (status == BattleStatusKind.Cleanse)
                        Check(unit.Create().SupportSkill && unit.statusTurns == 0 && unit.statusPower == 0 && !unit.statusAllEnemies,
                            unit.id + "：净化须为友方治疗技能，持续/数值填0");
                    else
                    {
                        Check(unit.statusTurns >= 1 && unit.statusTurns <= 5, unit.id + "：状态持续1至5次目标行动");
                        bool percent = status == BattleStatusKind.Poison || status == BattleStatusKind.Burn || status == BattleStatusKind.ArmorBreak || status == BattleStatusKind.GrievousWounds;
                        Check(percent ? unit.statusPower >= 1 && unit.statusPower <= 100 : unit.statusPower == 0, unit.id + "：伤害/减益百分比1至100，控制数值填0");
                        Check(!unit.Create().SupportSkill || unit.statusAllEnemies, unit.id + "：治疗技能的负面效果必须施加给敌方");
                    }
                }
                Check(unit.trait == TraitKind.None ? unit.traitPower == 0 : unit.traitPower > 0 && unit.traitPower <= 10000,
                    unit.id + "：无特性填0，特性数值必须为正数且不超过10000");
                if (unit.trait == TraitKind.DoubleStrike) Check(unit.traitPower == 1, unit.id + "：连击特性填1");
                if (unit.trait == TraitKind.Lifesteal) Check(unit.traitPower <= 100, unit.id + "：吸血不能超过100%");
                if (unit.trait == TraitKind.Enraged) Check(unit.traitPower <= 200, unit.id + "：狂暴伤害增幅不能超过200%");
                Check(unit.bossMechanic == BossMechanic.None ? unit.bossPower == 0 && unit.bossInterval == 0
                    : unit.bossPower > 0 && unit.bossPower <= 10000, unit.id + "：首领机制参数无效");
                if (unit.bossMechanic == BossMechanic.PackLeader || unit.bossMechanic == BossMechanic.Devour)
                    Check(unit.bossPower <= 100, unit.id + "：首领百分比参数不能超过100");
                if (unit.bossMechanic == BossMechanic.BarrierCycle || unit.bossMechanic == BossMechanic.StormCycle)
                    Check(unit.bossInterval >= 1 && unit.bossInterval <= 10, unit.id + "：首领周期需1至10次行动");
            }
            Ref(characters, "HERO_001");
            foreach (var item in items)
            {
                ValidateArt(item.imagePath, item.id + " / imagePath");
                Check(!string.IsNullOrWhiteSpace(item.name), item.id + "：缺少名称");
                if (string.IsNullOrEmpty(item.effectId)) continue;
                Ref(effects, item.effectId);
                Check(ItemBonuses.IsPassive(effects.First(effect => effect.id == item.effectId).type), item.id + "：道具必须引用被动战斗效果");
            }
            foreach (var effect in effects)
            {
                Check(effect.amount > 0 && effect.amount <= 100000, effect.id + "：效果数量需在1至100000之间");
                Check(effect.bonusAttack >= 0 && effect.bonusAttack <= 100000
                    && (effect.bonusAttack == 0 || effect.type == EffectKind.SpeedBonus), effect.id + "：附加攻击仅支持速度道具，范围0至100000");
                if (effect.type == EffectKind.GiveItem) Ref(items, effect.targetId);
                if (effect.type == EffectKind.GiveCharacter) { Ref(characters, effect.targetId); Check(effect.amount <= 6, effect.id + "：一次最多获得6张角色卡"); }
                if (ItemBonuses.IsPassive(effect.type)) Check(string.IsNullOrEmpty(effect.targetId), effect.id + "：全队被动效果的目标ID必须留空");
                if (effect.type == EffectKind.DoubleAttack) Check(effect.amount == 1, effect.id + "：二连击开关数值必须为1，不叠加");
                if (effect.type == EffectKind.Lifesteal) Check(effect.amount <= 100, effect.id + "：单件吸血不能超过100%");
                if (effect.type == EffectKind.AddExperience) Check(string.IsNullOrEmpty(effect.targetId), effect.id + "：经验效果目标留空");
            }
            foreach (var option in options)
            {
                Ref(events, option.eventId);
                Check(option.order > 0 && !string.IsNullOrWhiteSpace(option.text) && option.effectIds.Length > 0, option.id + "：选项缺少文案、顺序或效果");
                foreach (string effect in option.effectIds)
                {
                    Ref(effects, effect);
                    Check(!ItemBonuses.IsPassive(effects.First(entry => entry.id == effect).type), option.id + "：请通过GiveItem获得道具，不直接调用被动效果");
                }
            }
            foreach (var entry in events)
            {
                entry.options = options.Where(option => option.eventId == entry.id).OrderBy(option => option.order).ToArray();
                Check(entry.options.Length >= 1 && entry.options.Length <= 3, entry.id + "：事件选项必须1至3项");
                Check(entry.options.Select(option => option.order).Distinct().Count() == entry.options.Length, entry.id + "：选项顺序重复");
                Check(!string.IsNullOrWhiteSpace(entry.name) && !string.IsNullOrWhiteSpace(entry.description), entry.id + "：缺少事件文案");
                ValidateArt(entry.cg);
            }
            foreach (var prize in pools)
            {
                Check(!string.IsNullOrEmpty(prize.poolId) && prize.weight > 0 && prize.weight <= 100000 && prize.minQuantity > 0 && prize.maxQuantity >= prize.minQuantity && prize.maxQuantity <= 1000, prize.id + "：奖励池数值无效");
                if (prize.type == PrizeKind.Item) Ref(items, prize.targetId); else { Ref(characters, prize.targetId); Check(prize.maxQuantity <= 6, prize.id + "：角色数量不能超过6"); }
            }
            foreach (var group in pools.GroupBy(prize => prize.poolId))
                Check(group.Select(prize => prize.type).Distinct().Count() == 1 && group.Select(prize => prize.targetId).Distinct().Count() == group.Count(), group.Key + "：池内类型混合或目标重复");
            foreach (var reward in rewards)
            {
                ValidatePool(pools, reward.itemPoolId, PrizeKind.Item, reward.itemCandidates);
                ValidatePool(pools, reward.characterPoolId, PrizeKind.Character, reward.characterCandidates);
                Check(reward.chooseCount >= 0 && reward.chooseCount <= reward.itemCandidates + reward.characterCandidates && reward.itemCandidates + reward.characterCandidates <= 6, reward.id + "：候选或可选数量无效（最多6候选）");
                Check(reward.goldBase >= 0 && reward.goldPerNode >= 0 && reward.goldBase <= 100000 && reward.goldPerNode <= 100000, reward.id + "：金币数值无效");
                Check(reward.experienceBase > 0 && reward.experienceBase <= 100000 && reward.experiencePerNode > 0 && reward.experiencePerNode <= 100000, reward.id + "：经验基数与增长必须为正数");
            }
            Check(difficulties.Length >= 3 && difficulties[0].minPower == 0
                && difficulties.Select(entry => entry.minPower).Distinct().Count() == difficulties.Length, "难度档位至少3档，从0战力开始且门槛不重复");
            foreach (var difficulty in difficulties)
            {
                Check(difficulty.minPower >= 0 && !string.IsNullOrWhiteSpace(difficulty.name), difficulty.id + "：难度门槛或名称无效");
                ValidatePool(pools, difficulty.itemPoolId, PrizeKind.Item, difficulty.itemCandidates);
                ValidatePool(pools, difficulty.characterPoolId, PrizeKind.Character, difficulty.characterCandidates);
                Check(difficulty.chooseCount >= 0 && difficulty.chooseCount <= difficulty.itemCandidates + difficulty.characterCandidates
                    && difficulty.itemCandidates + difficulty.characterCandidates <= 6, difficulty.id + "：最多6候选，可选数量不能超过候选数量");
            }
            foreach (var battle in battles)
            {
                Check(battle.Slots.Any(id => !string.IsNullOrEmpty(id)), battle.id + "：敌阵不能全空");
                foreach (string enemy in battle.Slots.Where(id => !string.IsNullOrEmpty(id))) Ref(enemies, enemy);
                Ref(rewards, battle.rewardId);
                Check(battle.itemCandidates >= -1 && battle.characterCandidates >= -1 && battle.chooseCount >= -1, battle.id + "：继承数量填-1，覆盖数量填0至6");
                foreach (var difficulty in difficulties)
                {
                    int itemsCount = battle.itemCandidates < 0 ? difficulty.itemCandidates : battle.itemCandidates;
                    int heroesCount = battle.characterCandidates < 0 ? difficulty.characterCandidates : battle.characterCandidates;
                    int choices = battle.chooseCount < 0 ? difficulty.chooseCount : battle.chooseCount;
                    ValidatePool(pools, string.IsNullOrEmpty(battle.itemPoolId) ? difficulty.itemPoolId : battle.itemPoolId, PrizeKind.Item, itemsCount);
                    ValidatePool(pools, string.IsNullOrEmpty(battle.characterPoolId) ? difficulty.characterPoolId : battle.characterPoolId, PrizeKind.Character, heroesCount);
                    Check(choices >= 0 && choices <= itemsCount + heroesCount && itemsCount + heroesCount <= 6, battle.id + "：覆盖配置导致候选或可选数量无效");
                }
            }
            foreach (var good in goods)
            {
                Ref(shops, good.shopId); Ref(effects, good.effectId);
                Check(!ItemBonuses.IsPassive(effects.First(effect => effect.id == good.effectId).type), good.id + "：商品请引用GiveItem等即时效果");
                Check(good.price >= 0 && good.stock > 0 && good.stock <= 1000 && good.order > 0 && !string.IsNullOrWhiteSpace(good.name), good.id + "：商品数值无效");
                Check(good.price <= 100000 && good.weight > 0 && good.weight <= 100000, good.id + "：价格或权重超出范围");
                var effectType = effects.First(effect => effect.id == good.effectId).type;
                Check(effectType == EffectKind.GiveItem || effectType == EffectKind.GiveCharacter || effectType == EffectKind.AddExperience, good.id + "：商品只支持道具、角色、经验");
                if (effectType == EffectKind.AddExperience) Check(good.isFixed && good.price > 0, good.id + "：经验商品必须固定出售且价格大于0");
            }
            foreach (var shop in shops)
            {
                shop.goods = goods.Where(good => good.shopId == shop.id).OrderBy(good => good.order).ToArray();
                Check(shop.goods.Length > 0, shop.id + "：商店没有商品");
                Check(shop.goods.Select(good => good.order).Distinct().Count() == shop.goods.Length, shop.id + "：商品顺序重复");
                Check(shop.offerCount >= 1 && shop.offerCount <= 9 && shop.goods.Length >= shop.offerCount
                    && shop.goods.Count(good => good.isFixed) <= shop.offerCount, shop.id + "：上架数必须1至9且商品池足够");
                Check(shop.goods.Count(good => effects.First(effect => effect.id == good.effectId).type == EffectKind.AddExperience) == 1,
                    shop.id + "：必须配置一个固定经验商品");
                Check(shop.refreshCost > 0 && shop.refreshCost <= 100000 && shop.refreshIncrease >= 0 && shop.refreshIncrease <= 100000, shop.id + "：刷新费用无效");
                ValidateArt(shop.background);
            }
            foreach (var node in nodes)
            {
                Check(node.minRow >= 1 && node.maxRow <= 7 && node.minRow <= node.maxRow && node.weight > 0 && node.weight <= 100000 && !string.IsNullOrWhiteSpace(node.name), node.id + "：节点层数、名称或权重无效");
                Check(node.minFloor >= 1 && node.maxFloor <= 4 && node.minFloor <= node.maxFloor, node.id + "：楼层范围必须在1至4之间");
                if (node.isFinal) Check(node.type == NodeKind.Battle && node.minFloor == node.maxFloor
                    && node.minRow == floors[node.minFloor - 1].rows && node.maxRow == node.minRow, node.id + "：守关节点必须位于对应层最后一排");
                if (node.guaranteed) Check(node.uniquePerRun && node.type == NodeKind.Event && node.minFloor == node.maxFloor
                    && node.minRow == node.maxRow && node.minRow > 1 && node.minRow < floors[node.minFloor - 1].rows,
                    node.id + "：保底事件需限制单局进入一次，并限定到一层的一排普通节点");
                if (node.type == NodeKind.Event && node.uniquePerRun)
                    foreach (var floor in floors.Where(entry => entry.floor >= node.minFloor && entry.floor <= node.maxFloor))
                        for (int row = node.minRow; row <= Math.Min(node.maxRow, floor.rows - 1); row++)
                            Check(nodes.Any(entry => entry.type == NodeKind.Event && !entry.isFinal && !entry.guaranteed
                                && !nodes.Any(other => other.type == NodeKind.Event && other.contentId == entry.contentId && other.uniquePerRun)
                                && entry.minFloor <= floor.floor && entry.maxFloor >= floor.floor && entry.minRow <= row && entry.maxRow >= row),
                                node.id + "：缺少进入后替换重复节点的普通事件");
                if (node.type == NodeKind.Battle) Ref(battles, node.contentId);
                else if (node.type == NodeKind.Event) Ref(events, node.contentId);
                else Ref(shops, node.contentId);
            }
            foreach (var floor in floors)
            {
                for (int row = 1; row < floor.rows; row++)
                {
                    NodeKind[] kinds = row == 1 ? new[] { NodeKind.Battle } : row >= 3 && row <= Math.Min(5, floor.rows - 1) && floor.maxShops > 0
                        ? new[] { NodeKind.Battle, NodeKind.Event, NodeKind.Shop } : new[] { NodeKind.Battle, NodeKind.Event };
                    foreach (NodeKind kind in kinds) Check(nodes.Any(node => !node.isFinal && node.type == kind && node.minRow <= row && node.maxRow >= row
                        && node.minFloor <= floor.floor && node.maxFloor >= floor.floor && !node.guaranteed && !node.uniquePerRun), "第" + floor.floor + "层第" + row + "排缺少可重复" + kind + "节点");
                }
                Check(nodes.Any(node => node.isFinal && node.minRow == floor.rows && node.minFloor == floor.floor), "第" + floor.floor + "层缺少守关节点");
            }
            Check(nodes.Where(node => node.guaranteed).GroupBy(node => node.minFloor + ":" + node.minRow).All(group => group.Count() == 1), "同一排不能安排多个保底事件");
            Check(endings.Length == 3 && endings.Select(ending => ending.priority).SequenceEqual(new[] { 0, 1, 2 }), "结局规则需默认、二结局、三结局三条，优先级0、1、2");
            Check(string.IsNullOrEmpty(endings[0].requiredItemId) && string.IsNullOrEmpty(endings[0].fragmentItemId)
                && endings[0].requiredWins == 0 && endings[0].extraFloor == 0, "默认结局不能有解锁条件");
            Check(endings[1].requiredWins == 0 && endings[1].extraFloor == 0 && string.IsNullOrEmpty(endings[1].fragmentItemId), "二结局仅要求持有道具");
            Check(endings[2].requiredWins == 3 && endings[2].extraFloor == 4, "三结局需持契约胜利3场收集3枚碎片解锁第四层");
            foreach (var ending in endings)
            {
                Ref(nodes, ending.bossNodeId);
                var boss = nodes.First(node => node.id == ending.bossNodeId);
                Check(boss.isFinal && boss.minFloor == (ending.extraFloor > 0 ? ending.extraFloor : 3), ending.id + "：结局首领楼层不正确");
                Check(!string.IsNullOrWhiteSpace(ending.name) && !string.IsNullOrWhiteSpace(ending.description) && !string.IsNullOrWhiteSpace(ending.epilogue), ending.id + "：缺少结局文案");
                if (ending.priority == 0) continue;
                Ref(items, ending.requiredItemId);
                var sources = effects.Where(effect => effect.type == EffectKind.GiveItem && effect.targetId == ending.requiredItemId).ToArray();
                var sourceOptions = options.Where(option => sources.Any(effect => option.effectIds.Contains(effect.id))).ToArray();
                Check(sources.Length == 1 && sources[0].amount == 1, ending.id + "：解锁道具必须只有一个授予效果，且每次授予1个");
                Check(sourceOptions.Length == 1, ending.id + "：解锁道具必须且只能由一个事件选项授予");
                string eventId = sourceOptions[0].eventId;
                var eventNodes = nodes.Where(node => node.type == NodeKind.Event && node.contentId == eventId).ToArray();
                Check(eventNodes.Length > 0 && eventNodes.All(entry => entry.uniquePerRun) && eventNodes.Any(entry => entry.guaranteed),
                    ending.id + "：特殊事件必须限制单局进入一次，并至少有一个保底生成配置");
                if (ending.priority == 1)
                {
                    Check(options.Count(option => option.eventId == eventId) == 2, "二结局事件必须恰好包含正向与负向两个选项");
                    var key = items.First(item => item.id == ending.requiredItemId);
                    Check(effects.Any(effect => effect.id == key.effectId && effect.type == EffectKind.HealthPenalty), "二结局钥匙必须有实际负面效果");
                    Check(options.Any(option => option.eventId == eventId && option.id != sourceOptions[0].id
                        && option.effectIds.Any(id => effects.Any(effect => effect.id == id && effect.type == EffectKind.GiveItem
                            && items.Any(item => item.id == effect.targetId && effects.Any(passive => passive.id == item.effectId && ItemBonuses.IsPassive(passive.type) && passive.type != EffectKind.HealthPenalty))))), "二结局事件需有正向道具选项");
                }
                if (ending.requiredWins > 0)
                {
                    Check(ending.requiredItemId != ending.fragmentItemId, "契约与战斗碎片必须为不同道具");
                    Ref(items, ending.fragmentItemId);
                    Check(string.IsNullOrEmpty(items.First(item => item.id == ending.fragmentItemId).effectId), "碎片不应附加战斗属性");
                    Check(!effects.Any(effect => effect.type == EffectKind.GiveItem && effect.targetId == ending.fragmentItemId), "碎片只能由符合条件的战斗胜利掉落");
                }
                foreach (string key in new[] { ending.requiredItemId, ending.fragmentItemId }.Where(id => !string.IsNullOrEmpty(id)))
                {
                    Check(!pools.Any(prize => prize.type == PrizeKind.Item && prize.targetId == key), key + "：剧情道具不能进入随机奖池");
                    Check(!goods.Any(good => effects.Any(effect => effect.id == good.effectId && effect.type == EffectKind.GiveItem && effect.targetId == key)), key + "：剧情道具不能出售");
                }
            }
            Directory.CreateDirectory(LibraryPath);
            AssetDatabase.Refresh();
            Save<EnemyDatabaseSO, UnitRecord>("Enemies", enemies);
            Save<CharacterDatabaseSO, UnitRecord>("Characters", characters);
            Save<ItemDatabaseSO, ItemRecord>("Items", items);
            Save<NodeDatabaseSO, NodeRecord>("Nodes", nodes);
            Save<BattleDatabaseSO, BattleRecord>("Battles", battles);
            Save<EventDatabaseSO, EventRecord>("Events", events);
            Save<EffectDatabaseSO, EffectRecord>("Effects", effects);
            Save<RewardDatabaseSO, RewardRecord>("Rewards", rewards);
            Save<RewardPoolDatabaseSO, PoolRecord>("Pools", pools);
            Save<ShopDatabaseSO, ShopRecord>("Shops", shops);
            Save<LevelDatabaseSO, LevelRecord>("Levels", levels);
            Save<FloorDatabaseSO, FloorRecord>("Floors", floors);
            Save<DifficultyDatabaseSO, DifficultyRecord>("Difficulties", difficulties);
            Save<EndingDatabaseSO, EndingRecord>("Endings", endings);
            AssetDatabase.SaveAssets();
            Debug.Log("配置导入成功：16张工作表 → 14个SO库。结局、剧情道具来源、保底事件与隐藏楼层已验证。");
        }

        private static void ValidatePool(PoolRecord[] pools, string id, PrizeKind type, int count)
        {
            Check(count >= 0 && count <= 6, id + "：抽取数量无效");
            if (count == 0 && string.IsNullOrEmpty(id)) return;
            Check(pools.Any(row => row.poolId == id), "找不到奖励池：" + id);
            Check(pools.Where(row => row.poolId == id).All(row => row.type == type) && pools.Count(row => row.poolId == id) >= count, id + "：奖励池类型或候选数不足");
        }
        private static void ValidateArt(string path, string owner = "图片")
        {
            Check(!string.IsNullOrWhiteSpace(path) && path == path.Trim() && !path.StartsWith("/")
                && !path.Contains("\\") && !path.Contains("..") && !path.Contains(":")
                && !path.StartsWith("Assets/") && !path.StartsWith("Resources/") && string.IsNullOrEmpty(Path.GetExtension(path)),
                owner + "：填写 Resources 下的相对路径，使用 / 且不含扩展名：" + path);
            Check(Resources.Load<Texture2D>(path) != null, owner + "：找不到图片资源 Resources/" + path);
        }
        private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static void Ref<T>(IEnumerable<T> records, string id) where T : ContentRecord { Check(records.Any(row => row.id == id), "无效引用ID：" + id); }
        private static void Save<TLibrary, TRow>(string name, TRow[] rows) where TLibrary : RecordDatabase<TRow> where TRow : ContentRecord
        {
            string path = LibraryPath + "/" + name + ".asset";
            var library = AssetDatabase.LoadAssetAtPath<TLibrary>(path);
            if (library == null) { library = ScriptableObject.CreateInstance<TLibrary>(); AssetDatabase.CreateAsset(library, path); }
            library.Replace(rows);
            EditorUtility.SetDirty(library);
        }
        private static T[] Read<T>(Dictionary<string, List<Dictionary<string, string>>> tables, string name) where T : ContentRecord, new()
        {
            Check(tables.ContainsKey(name), "缺少工作表：" + name);
            var result = new List<T>();
            foreach (var values in tables[name])
            {
                var record = new T();
                foreach (var field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (field.Name == "options" || field.Name == "goods") continue;
                    if (typeof(T) == typeof(EffectRecord) && field.Name == "bonusAttack" && !values.ContainsKey(field.Name)) continue;
                    Check(values.ContainsKey(field.Name), name + "缺少字段：" + field.Name);
                    string value = values[field.Name];
                    try
                    {
                        object parsed = field.FieldType == typeof(string) ? value : field.FieldType == typeof(int) ? (object)int.Parse(value, CultureInfo.InvariantCulture)
                            : field.FieldType == typeof(bool) ? (value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
                            : field.FieldType == typeof(string[]) ? value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(part => part.Trim()).ToArray()
                            : Enum.Parse(field.FieldType, value);
                        if (field.FieldType.IsEnum) Check(Enum.IsDefined(field.FieldType, parsed), "枚举值无效");
                        if (field.FieldType == typeof(bool)) Check(new[] { "true", "false", "0", "1" }.Contains(value.ToLowerInvariant()), "布尔值无效");
                        field.SetValue(record, parsed);
                    }
                    catch (Exception exception) { throw new InvalidOperationException(name + " / " + values["id"] + " / " + field.Name + "：" + value, exception); }
                }
                result.Add(record);
            }
            Check(result.Count > 0 && result.All(row => !string.IsNullOrWhiteSpace(row.id)) && result.Select(row => row.id).Distinct().Count() == result.Count, name + "：ID为空或重复");
            return result.ToArray();
        }

        private static Dictionary<string, List<Dictionary<string, string>>> ReadWorkbook(string path)
        {
            var result = new Dictionary<string, List<Dictionary<string, string>>>();
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
            {
                var sharedEntry = zip.GetEntry("xl/sharedStrings.xml");
                string[] shared = sharedEntry == null ? Array.Empty<string>() : ReadXml(sharedEntry).Descendants(Spreadsheet + "si").Select(entry => string.Concat(entry.Descendants(Spreadsheet + "t").Select(text => text.Value))).ToArray();
                var relations = ReadXml(zip.GetEntry("xl/_rels/workbook.xml.rels")).Root.Elements().ToDictionary(entry => (string)entry.Attribute("Id"), entry => (string)entry.Attribute("Target"));
                foreach (var sheet in ReadXml(zip.GetEntry("xl/workbook.xml")).Descendants(Spreadsheet + "sheet"))
                {
                    if (!ConfigurationSheets.Contains((string)sheet.Attribute("name"))) continue;
                    string target = relations[(string)sheet.Attribute(XName.Get("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships"))];
                    string sheetPath = target.StartsWith("/") ? target.TrimStart('/') : "xl/" + target;
                    var rows = ReadXml(zip.GetEntry(sheetPath)).Descendants(Spreadsheet + "row").ToArray();
                    var header = rows.Single(row => (int)row.Attribute("r") == 3).Elements(Spreadsheet + "c").ToDictionary(cell => Column((string)cell.Attribute("r")), cell => Cell(cell, shared));
                    var entries = new List<Dictionary<string, string>>();
                    foreach (var row in rows.Where(row => (int)row.Attribute("r") >= 5))
                    {
                        var cells = row.Elements(Spreadsheet + "c").ToDictionary(cell => Column((string)cell.Attribute("r")), cell => Cell(cell, shared));
                        if (cells.Values.All(string.IsNullOrWhiteSpace)) continue;
                        entries.Add(header.ToDictionary(pair => pair.Value, pair => cells.TryGetValue(pair.Key, out string value) ? value.Trim() : ""));
                    }
                    result.Add((string)sheet.Attribute("name"), entries);
                }
            }
            return result;
        }
        private static XDocument ReadXml(ZipArchiveEntry entry) { using (var stream = entry.Open()) return XDocument.Load(stream); }
        private static string Column(string address) { return new string(address.TakeWhile(char.IsLetter).ToArray()); }
        private static string Cell(XElement cell, string[] shared)
        {
            Check(cell.Element(Spreadsheet + "f") == null, "配置数据请填写固定值，不支持公式：" + cell.Attribute("r"));
            string value = (string)cell.Element(Spreadsheet + "v") ?? "";
            string type = (string)cell.Attribute("t");
            if (type == "s") return shared[int.Parse(value, CultureInfo.InvariantCulture)];
            if (type == "inlineStr") return string.Concat(cell.Descendants(Spreadsheet + "t").Select(text => text.Value));
            return value;
        }
    }
}
