using System;
using System.Collections.Generic;
using System.Linq;

namespace ElementalTactics
{
    public static class CombatPower
    {
        public const double ReferenceDamage = 84.375;
        public const double ReferenceDurability = 636.275;
        public static double RewardPower(MapNode node)
        {
            var lineup = GameContent.EnemyLineup(node);
            var balance = HandbookBalanceConfig.Current;
            var stage = balance == null ? null : balance.snapshot.stages.FirstOrDefault(s => s.nodeId == node.Definition.id && s.floor == node.Floor);
            // The handbook's conversion ratio chooses reward tiers; it does not
            // multiply actual combat attributes or use the fixed analysis difficulty.
            return stage != null && stage.scaledDifficulty > 0
                ? balance.EvaluatedLineup(lineup) * (stage.rewardPower / stage.scaledDifficulty)
                : Evaluate(lineup);
        }

        public static double Evaluate(IEnumerable<CharacterDefinition> lineup)
        {
            var units = lineup.Where(unit => unit != null).ToList();
            double damage = 0, durability = 0;
            foreach (var unit in units)
            {
                double targets = unit.skillKind == SkillKind.AllEnemies ? 5.25
                    : unit.skillKind != SkillKind.RoleDefault ? 1
                    : unit.role == Role.Mage ? 1.85 : unit.role == Role.Ranger ? 1.425 : 1;
                double multiplier = unit.skillKind == SkillKind.AllAllies ? 0
                    : unit.skillKind != SkillKind.RoleDefault ? unit.skillPower / 100.0
                    : unit.role == Role.Warrior ? 1.5 : unit.role == Role.Mage ? 0.95 : unit.role == Role.Ranger ? 1.2 : 0;
                double mitigation = 100.0 / 120;
                double frenzy = unit.trait == TraitKind.Enraged ? 1 + unit.traitPower / 100.0 * 0.4 : 1;
                double hits = unit.doubleAttack || unit.trait == TraitKind.DoubleStrike ? 2 : 1;
                double rawDamage = unit.attack * (6 * hits * mitigation + 2 * multiplier * targets
                    * (unit.skillKind == SkillKind.ArmorPierce ? 1 : mitigation)) / 8 * frenzy;
                rawDamage *= unit.bossMechanic == BossMechanic.PackLeader ? 1 + Math.Max(0, units.Count - 1) * unit.bossPower / 100.0 * .6
                    : unit.bossMechanic == BossMechanic.Devour ? 1 + Math.Min(3, units.Count - 1) * .15 * .5 : 1;
                double skillHealing = unit.skillKind == SkillKind.AllAllies ? unit.skillPower * units.Count
                    : unit.skillKind != SkillKind.RoleDefault ? 0 : unit.role == Role.Guardian ? 100
                    : unit.role == Role.Warrior ? unit.attack * 1.5 * mitigation * frenzy * 0.5 : 0;
                double regeneration = unit.regeneration + (unit.trait == TraitKind.Regeneration ? unit.traitPower : 0);
                double lifesteal = Math.Min(100, unit.lifesteal + (unit.trait == TraitKind.Lifesteal ? unit.traitPower : 0));
                double healing = Math.Min(unit.maxHealth * (unit.skillKind == SkillKind.AllAllies ? units.Count : 1),
                    (7 * regeneration + 2 * skillHealing + rawDamage * 8 * lifesteal / 100) * 0.65);
                double shields = unit.skillKind == SkillKind.RoleDefault && unit.role == Role.Guardian
                    ? 2 * Math.Min(unit.maxHealth, 90) * Math.Min(2, units.Count) * 0.7 : 0;
                double opening = Math.Min(unit.maxHealth, unit.openingShield + (unit.trait == TraitKind.OpeningShield ? unit.traitPower : 0));
                double bossHealing = unit.bossMechanic == BossMechanic.Devour ? unit.maxHealth * unit.bossPower / 100.0 * Math.Max(0, units.Count - 1) * .5 * .65 : 0;
                double bossShield = unit.bossMechanic == BossMechanic.BarrierCycle || unit.bossMechanic == BossMechanic.StormCycle
                    ? Math.Floor(8.0 / Math.Max(1, unit.bossInterval)) * Math.Min(unit.maxHealth, unit.bossPower) * .7
                        + (unit.bossMechanic == BossMechanic.StormCycle ? Math.Min(unit.maxHealth, unit.bossPower) * .7 : 0)
                    : unit.bossMechanic == BossMechanic.SoulDrain ? 2 * Math.Min(unit.maxHealth, unit.bossPower * 3) * .7 : 0;
                damage += rawDamage * (1 + Math.Max(-0.1, Math.Min(0.1, 0.2 * (unit.speed / 70.0 - 1))));
                durability += (unit.maxHealth + opening + healing + shields + bossHealing + bossShield) * (1 + unit.defense / 100.0);
            }
            return 1000 * Math.Sqrt(damage / ReferenceDamage * durability / ReferenceDurability);
        }
        public static DifficultyRecord Difficulty(double power)
        {
            return GameContent.Difficulties.rows.Where(entry => power >= entry.minPower)
                .OrderByDescending(entry => entry.minPower).First();
        }
    }
}
