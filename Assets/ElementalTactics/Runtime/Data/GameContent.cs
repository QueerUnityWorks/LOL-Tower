using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ElementalTactics
{
    public static class GameContent
    {
        public static EnemyDatabaseSO Enemies => Load<EnemyDatabaseSO>("Enemies");
        public static CharacterDatabaseSO Characters => Load<CharacterDatabaseSO>("Characters");
        public static ItemDatabaseSO Items => Load<ItemDatabaseSO>("Items");
        public static NodeDatabaseSO Nodes => Load<NodeDatabaseSO>("Nodes");
        public static BattleDatabaseSO Battles => Load<BattleDatabaseSO>("Battles");
        public static EventDatabaseSO Events => Load<EventDatabaseSO>("Events");
        public static EffectDatabaseSO Effects => Load<EffectDatabaseSO>("Effects");
        public static RewardDatabaseSO Rewards => Load<RewardDatabaseSO>("Rewards");
        public static RewardPoolDatabaseSO Pools => Load<RewardPoolDatabaseSO>("Pools");
        public static ShopDatabaseSO Shops => Load<ShopDatabaseSO>("Shops");
        public static LevelDatabaseSO Levels => Load<LevelDatabaseSO>("Levels");
        public static FloorDatabaseSO Floors => Load<FloorDatabaseSO>("Floors");
        public static DifficultyDatabaseSO Difficulties => Load<DifficultyDatabaseSO>("Difficulties");
        public static EndingDatabaseSO Endings => Load<EndingDatabaseSO>("Endings");
        private static T Load<T>(string name) where T : ScriptableObject
        {
            var cached = TutorialContent.CachedDatabase<T>();
            if (cached != null) return cached;
            var database = Resources.Load<T>("Data/" + name);
            if (database == null) throw new InvalidOperationException("缺少配置库：" + name + "，请执行 Import Game Workbook。");
            return database;
        }
        public static List<CharacterDefinition> EnemyLineup(BattleRecord battle)
        {
            if (battle.id.StartsWith(TutorialContent.Prefix, StringComparison.Ordinal)) return TutorialContent.EnemyLineup(battle);
            return battle.Slots.Select(id => string.IsNullOrEmpty(id) ? null : Enemies.Get(id).Create()).ToList();
        }
        public static CharacterDefinition StrongestEnemy(MapNode node)
        {
            var balance = HandbookBalanceConfig.Current;
            var units = EnemyLineup(node).Where(unit => unit != null).ToArray();
            bool hasScores = balance != null && units.All(unit => balance.Unit(unit.id) != null);
            // EnemyLineup applies this floor and adventure difficulty before ranking.
            // LINQ's stable ordering keeps the first occupied slot for equal scores.
            return units.OrderByDescending(unit => hasScores
                    ? balance.EvaluatedStrength(unit) : CombatPower.Evaluate(new[] { unit }))
                .FirstOrDefault();
        }
        public static List<CharacterDefinition> EnemyLineup(MapNode node)
        {
            var lineup = EnemyLineup(Battles.Get(node.Definition.contentId));
            if (node.Definition.id.StartsWith(TutorialContent.Prefix, StringComparison.Ordinal)) return lineup;
            var floor = Floors.rows.Single(entry => entry.floor == node.Floor);
            double factor = AdventureDifficulty.Factor(AdventureRun.Difficulty);
            foreach (var unit in lineup.Where(entry => entry != null))
            {
                unit.maxHealth = AdventureDifficulty.Scale(unit.maxHealth, factor * floor.healthPercent / 100.0, 1);
                unit.attack = AdventureDifficulty.Scale(unit.attack, factor * floor.attackPercent / 100.0, 1);
                unit.guardianHealing = AdventureDifficulty.Scale(unit.guardianHealing, factor);
                unit.guardianShield = AdventureDifficulty.Scale(unit.guardianShield, factor);
                unit.defense = AdventureDifficulty.Scale(unit.defense, factor);
                unit.speed = AdventureDifficulty.Scale(unit.speed, factor, 1);
                if (unit.skillKind == SkillKind.AllAllies) unit.skillPower = AdventureDifficulty.Scale(unit.skillPower, factor * floor.healthPercent / 100.0);
                if (unit.trait == TraitKind.OpeningShield || unit.trait == TraitKind.Regeneration)
                    unit.traitPower = AdventureDifficulty.Scale(unit.traitPower, factor * floor.healthPercent / 100.0);
                if (unit.bossMechanic == BossMechanic.BarrierCycle || unit.bossMechanic == BossMechanic.SoulDrain || unit.bossMechanic == BossMechanic.StormCycle)
                    unit.bossPower = AdventureDifficulty.Scale(unit.bossPower, factor * floor.healthPercent / 100.0);
            }
            return lineup;
        }
    }
}
