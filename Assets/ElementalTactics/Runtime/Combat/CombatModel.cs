using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ElementalTactics
{
    public enum Role { Warrior, Mage, Guardian, Ranger }
    public enum Element { Fire, Water, Grass }
    public enum Rarity { Common, Rare, Epic }
    public enum SkillKind { RoleDefault, AllEnemies, WeakestEnemy, AllAllies, ArmorPierce, SingleEnemy }
    public enum TraitKind { None, OpeningShield, Regeneration, Enraged, DoubleStrike, Lifesteal }
    public enum BossMechanic { None, PackLeader, Devour, BarrierCycle, SoulDrain, StormCycle }

    [Serializable]
    public class CharacterDefinition
    {
        public string id;
        public Rarity rarity;
        public string displayName;
        public string skillName;
        public string biography;
        public string appearanceId;
        public string imagePath;
        public Role role;
        public Element element;
        public int maxHealth;
        public int attack;
        public int defense;
        public int speed;
        public bool doubleAttack;
        public SkillKind skillKind;
        public int skillPower;
        public string statusEffect;
        public int statusTurns, statusPower;
        public bool statusAllEnemies;
        public TraitKind trait;
        public int traitPower;
        public int openingShield, regeneration, lifesteal;
        public BossMechanic bossMechanic;
        public int bossPower, bossInterval;
        public int guardianHealing = 100, guardianShield = 90;
        public bool SupportSkill => skillKind == SkillKind.AllAllies || (skillKind == SkillKind.RoleDefault && role == Role.Guardian);
        public int OpeningShield => openingShield + (trait == TraitKind.OpeningShield ? traitPower : 0);
        public int Regeneration => regeneration + (trait == TraitKind.Regeneration ? traitPower : 0);
        public int Lifesteal => Mathf.Min(100, lifesteal + (trait == TraitKind.Lifesteal ? traitPower : 0));
        public string TraitDescription => (trait == TraitKind.OpeningShield ? "开战获得" + traitPower + "护盾。"
            : trait == TraitKind.Regeneration ? "每次行动开始回复" + traitPower + "生命。"
            : trait == TraitKind.Enraged ? "生命不高于一半时，造成伤害增加" + traitPower + "%。"
            : trait == TraitKind.DoubleStrike ? "普攻攻击两次，每次行动仅获得1点技力。"
            : trait == TraitKind.Lifesteal ? "造成生命伤害时回复伤害的" + traitPower + "%。" : "无额外特性。")
            + (bossMechanic == BossMechanic.None ? "" : "\n首领机制：" + BossDescription);
        public string BossDescription => bossMechanic == BossMechanic.PackLeader ? "每名存活友军使自身伤害增加" + bossPower + "%，优先击败狼群可削弱首领。"
            : bossMechanic == BossMechanic.Devour ? "每有一名友军阵亡，下次行动恢复" + bossPower + "%最大生命；每名阵亡友军使伤害增加15%。集中击败首领可阻止持续吞噬。"
            : bossMechanic == BossMechanic.BarrierCycle ? "每" + bossInterval + "次行动获得" + bossPower + "护盾。集中输出，在护盾重生前突破。"
            : bossMechanic == BossMechanic.SoulDrain ? "技能对存活目标各夺取1点技力，每夺取1点获得" + bossPower + "护盾。利用速度优先释放技能，减少被夺取的技力。"
            : bossMechanic == BossMechanic.StormCycle ? "每" + bossInterval + "次行动按水、草、火循环切换属性并获得" + bossPower + "护盾；首次降至半血时立即蓄满技力并获得等量护盾。准备多属性输出与恢复。" : "";
        public CharacterDefinition Clone() => (CharacterDefinition)MemberwiseClone();

        public static CharacterDefinition Create(string name, Role role, Element element)
        {
            var definition = new CharacterDefinition { displayName = name, role = role, element = element };
            switch (role)
            {
                case Role.Warrior: definition.SetStats(440, 90, 24, 70); break;
                case Role.Mage: definition.SetStats(290, 112, 10, 60); break;
                case Role.Guardian: definition.SetStats(650, 58, 44, 40); break;
                case Role.Ranger: definition.SetStats(320, 98, 14, 90); break;
            }
            return definition;
        }

        private void SetStats(int health, int power, int armor, int initiative)
        {
            maxHealth = health;
            attack = power;
            defense = armor;
            speed = initiative;
        }

        public string RoleName => new[] { "战士", "法师", "盾卫", "射手" }[(int)role];
        public string ElementName => new[] { "火", "水", "草" }[(int)element];
        public string SkillName => string.IsNullOrEmpty(skillName) ? new[] { "嗜血斩", "元素爆发", "守护之光", "穿透射击" }[(int)role] : skillName;
        public string SkillDescription => BaseSkillDescription + StatusSkillDescription;
        public string StatusSkillDescription
        {
            get
            {
                if (string.IsNullOrEmpty(statusEffect)) return "";
                var kind = (BattleStatusKind)Enum.Parse(typeof(BattleStatusKind), statusEffect);
                if (kind == BattleStatusKind.Cleanse) return "施放时先净化技能目标的全部负面状态，再治疗。";
                string target = statusAllEnemies ? "所有存活敌人" : "命中的存活目标";
                return "对" + target + "施加" + BattleStatusCatalog.Name(kind) + "，持续目标" + statusTurns + "次行动。"
                    + BattleStatusCatalog.Rule(kind, statusPower) + "同类刷新，不叠层。";
            }
        }
        private string BaseSkillDescription => skillKind == SkillKind.AllEnemies ? "对所有敌人造成" + skillPower + "%攻击伤害。"
            : skillKind == SkillKind.WeakestEnemy ? "攻击生命比例最低的敌人，造成" + skillPower + "%攻击伤害，可越过前排。"
            : skillKind == SkillKind.AllAllies ? "回复所有存活队友" + skillPower + "生命，不复活。"
            : skillKind == SkillKind.ArmorPierce ? "对单体造成" + skillPower + "%攻击伤害，忽略防御，仍受护盾吸收。"
            : skillKind == SkillKind.SingleEnemy ? "对单体造成" + skillPower + "%攻击伤害。"
            : new[]
        {
            "对单体造成150%攻击伤害，回复实际伤害的50%。",
            "对敌方最前方存活一排造成95%攻击伤害。",
            "治疗生命比例最低的队友" + guardianHealing + "点；自己与该队友获得" + guardianShield + "护盾。",
            "对目标所在列的所有敌人造成120%攻击伤害。"
        }[(int)role];
    }

    // Runtime statuses expire after the affected fighter's action opportunities.
    // Append values to preserve existing status identifiers. Cleanse is instantaneous.
    public enum BattleStatusKind { Stun, Poison, Charm, Burn, Silence, Taunt, ArmorBreak, GrievousWounds, Cleanse }

    public static class BattleStatusCatalog
    {
        public static string Name(BattleStatusKind kind)
        {
            switch (kind)
            {
                case BattleStatusKind.Stun: return "晕眩";
                case BattleStatusKind.Poison: return "中毒";
                case BattleStatusKind.Charm: return "魅惑";
                case BattleStatusKind.Burn: return "灼烧";
                case BattleStatusKind.Silence: return "沉默";
                case BattleStatusKind.Taunt: return "嘲讽";
                case BattleStatusKind.ArmorBreak: return "破甲";
                case BattleStatusKind.GrievousWounds: return "重伤";
                case BattleStatusKind.Cleanse: return "净化";
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
        public static bool IsPersistent(BattleStatusKind kind)
        {
            return Enum.IsDefined(typeof(BattleStatusKind), kind) && kind != BattleStatusKind.Cleanse;
        }
        public static string Rule(BattleStatusKind kind, int power)
        {
            switch(kind)
            {
                case BattleStatusKind.Stun: return "跳过行动，不获得技力。";
                case BattleStatusKind.Poison:
                case BattleStatusKind.Burn: return "每次行动前受到施加者施放时攻击的" + power + "%持续伤害，忽略防御与属性克制，可被护盾吸收。";
                case BattleStatusKind.Charm: return "强制对己方其他存活角色普攻一次；没有目标时跳过行动。";
                case BattleStatusKind.Silence: return "无法释放技能，仍可普攻并积攒技力，已有技力保留。";
                case BattleStatusKind.Taunt: return "单体攻击优先选择施加者；群攻与治疗不改目标，施加者阵亡后解除。";
                case BattleStatusKind.ArmorBreak: return "防御降低" + power + "%，最低为0；不改变基础配置。";
                case BattleStatusKind.GrievousWounds: return "受到的治疗、回血和吸血回复降低" + power + "%，不影响护盾。";
                case BattleStatusKind.Cleanse: return "立即移除目标全部负面状态，不留下持续状态。";
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }

    public sealed class BattleStatus
    {
        public BattleStatusKind Kind { get; private set; }
        public int RemainingTurns { get; private set; }
        public int Stacks { get; private set; }
        public string Description { get; private set; }
        public string Source { get; private set; }
        public int Magnitude { get; private set; }
        public Fighter SourceFighter { get; private set; }
        public string Name => BattleStatusCatalog.Name(Kind);
        public string DurationText => RemainingTurns < 0 ? "持续生效" : "剩余 " + RemainingTurns + " 次行动";

        internal BattleStatus(BattleStatusKind kind, int turns, string description, string source, int stacks, int magnitude, Fighter sourceFighter)
        {
            Kind = kind; RemainingTurns = turns; Description = description;
            Source = source ?? ""; Stacks = stacks;
            Magnitude = magnitude; SourceFighter = sourceFighter;
        }
    }

    // One entry per kind, stable insertion order. Refreshing an entry does not reorder icons.
    // An action snapshots status instances so effects newly applied during that action do not expire early.
    public sealed class BattleStatusCollection
    {
        private readonly List<BattleStatus> entries = new List<BattleStatus>();
        private readonly System.Collections.ObjectModel.ReadOnlyCollection<BattleStatus> readOnly;
        public IReadOnlyList<BattleStatus> Entries => readOnly;
        public int Version { get; private set; }

        public BattleStatusCollection() { readOnly = entries.AsReadOnly(); }

        public void Set(BattleStatusKind kind, int remainingTurns, string description, string source = "", int stacks = 1, int magnitude = 0, Fighter sourceFighter = null)
        {
            if (!Enum.IsDefined(typeof(BattleStatusKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (!BattleStatusCatalog.IsPersistent(kind)) throw new ArgumentException("净化是即时动作，请使用 Cleanse 接口。", nameof(kind));
            if (remainingTurns < -1) throw new ArgumentOutOfRangeException(nameof(remainingTurns));
            if (remainingTurns == 0) { Remove(kind); return; }
            if (stacks < 1) throw new ArgumentOutOfRangeException(nameof(stacks));
            if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("状态需要提供效果说明。", nameof(description));
            if (magnitude < 0) throw new ArgumentOutOfRangeException(nameof(magnitude));
            var status = new BattleStatus(kind, remainingTurns, description, source, stacks, magnitude, sourceFighter);
            int index = entries.FindIndex(entry => entry.Kind == kind);
            if (index >= 0) entries[index] = status; else entries.Add(status);
            Version++;
        }

        public bool Remove(BattleStatusKind kind)
        {
            int count = entries.RemoveAll(entry => entry.Kind == kind);
            if (count > 0) Version++;
            return count > 0;
        }

        public BattleStatus Get(BattleStatusKind kind) => entries.FirstOrDefault(entry => entry.Kind == kind);
        public bool Has(BattleStatusKind kind) => Get(kind) != null;
        public void Advance(BattleStatus[] atActionStart)
        {
            foreach(var old in atActionStart)
            {
                if(old.RemainingTurns<0 || !ReferenceEquals(Get(old.Kind),old)) continue;
                Set(old.Kind,old.RemainingTurns-1,old.Description,old.Source,old.Stacks,old.Magnitude,old.SourceFighter);
            }
        }

        // Used by cleansing skills before healing their targets.
        // With no filter, removes all eight supported negative statuses. A filter removes only named kinds.
        // Future positive/undispellable statuses must define their own cleansing policy before being added.
        public int Cleanse(params BattleStatusKind[] kinds)
        {
            if (kinds == null) throw new ArgumentNullException(nameof(kinds));
            foreach (var kind in kinds)
                if (!BattleStatusCatalog.IsPersistent(kind)) throw new ArgumentOutOfRangeException(nameof(kinds));
            int count = entries.RemoveAll(entry => BattleStatusCatalog.IsPersistent(entry.Kind)
                && (kinds.Length == 0 || Array.IndexOf(kinds, entry.Kind) >= 0));
            if (count > 0) Version++;
            return count;
        }

        public void Clear() { if (entries.Count == 0) return; entries.Clear(); Version++; }
    }

    public sealed class Fighter
    {
        public CharacterDefinition definition;
        public bool enemy;
        public int slot;
        public int health;
        public int shield;
        public int energy;
        public int tieOrder;
        public int actionCount, consumedAllies;
        public bool phaseTriggered;
        public readonly BattleStatusCollection statuses = new BattleStatusCollection();
        public bool Alive => health > 0;

        public Fighter(CharacterDefinition definition, bool enemy, int slot)
        {
            this.definition = definition.Clone();
            this.enemy = enemy;
            this.slot = slot;
            health = definition.maxHealth;
            energy = CombatRules.InitialEnergy;
            shield = Mathf.Clamp(definition.OpeningShield, 0, definition.maxHealth);
        }
    }

    public sealed class CombatEffect
    {
        public Fighter target;
        public int damage;
        public int absorbed;
        public int healing;
        public int shielding;
        public int energyLost;
        public string status;
    }

    public sealed class CombatAction
    {
        public Fighter actor;
        public bool skill, canAct;
        public BattleStatus[] startingStatuses;
        public List<CombatEffect> openingEffects;
        public List<Fighter> allies, opponents;
    }

    public static class CombatRules
    {
        public const int MaxRounds = 50;
        public const int SkillEnergyCost = 3;
        public const int InitialEnergy = 2;

        public static int RoundsUntilSkill(int energy, bool actedThisRound)
        {
            return SkillEnergyCost - Mathf.Clamp(energy, 0, SkillEnergyCost) + (actedThisRound ? 1 : 0);
        }

        public static float ElementMultiplier(Element attacker, Element defender)
        {
            if (attacker == defender) return 1f;
            return (attacker == Element.Fire && defender == Element.Grass)
                || (attacker == Element.Grass && defender == Element.Water)
                || (attacker == Element.Water && defender == Element.Fire) ? 1.25f : 0.8f;
        }

        public static Fighter FindTarget(Fighter actor, IEnumerable<Fighter> opponents)
        {
            return opponents.Where(target => target.Alive)
                .OrderBy(target => target.slot / 3)
                .ThenBy(target => target.slot % 3 == actor.slot % 3 ? 0 : 1)
                .ThenBy(target => target.slot).FirstOrDefault();
        }

        public static List<Fighter> FindTargets(Fighter actor, List<Fighter> allies, List<Fighter> opponents, bool skill)
        {
            skill = EffectiveSkill(actor, skill);
            if (actor.statuses.Has(BattleStatusKind.Charm))
            {
                var friendly = FindTarget(actor, allies.Where(ally => ally != actor));
                return actor.Alive && friendly != null ? new List<Fighter> { friendly } : new List<Fighter>();
            }
            var target = FindTarget(actor, opponents);
            if (!actor.Alive || target == null) return new List<Fighter>();
            bool single = !skill || actor.definition.skillKind == SkillKind.SingleEnemy
                || actor.definition.skillKind == SkillKind.ArmorPierce || actor.definition.skillKind == SkillKind.WeakestEnemy
                || (actor.definition.skillKind == SkillKind.RoleDefault && actor.definition.role == Role.Warrior);
            var taunt = actor.statuses.Get(BattleStatusKind.Taunt);
            if (single && taunt != null && taunt.SourceFighter != null && taunt.SourceFighter.Alive && opponents.Contains(taunt.SourceFighter))
                return new List<Fighter> { taunt.SourceFighter };
            if (skill && actor.definition.skillKind == SkillKind.AllAllies) return allies.Where(ally => ally.Alive).ToList();
            if (skill && actor.definition.skillKind == SkillKind.AllEnemies) return opponents.Where(opponent => opponent.Alive).ToList();
            if (skill && actor.definition.skillKind == SkillKind.WeakestEnemy)
                return new List<Fighter> { opponents.Where(opponent => opponent.Alive).OrderBy(opponent => (float)opponent.health / opponent.definition.maxHealth).ThenBy(opponent => opponent.slot).First() };
            if (skill && (actor.definition.skillKind == SkillKind.ArmorPierce || actor.definition.skillKind == SkillKind.SingleEnemy)) return new List<Fighter> { target };
            if (skill && actor.definition.role == Role.Guardian)
            {
                var patient = allies.Where(ally => ally.Alive)
                    .OrderBy(ally => (float)ally.health / ally.definition.maxHealth)
                    .ThenBy(ally => ally.slot).First();
                return patient == actor ? new List<Fighter> { actor } : new List<Fighter> { patient, actor };
            }
            if (skill && actor.definition.role == Role.Mage)
                return opponents.Where(opponent => opponent.Alive && opponent.slot / 3 == target.slot / 3).ToList();
            if (skill && actor.definition.role == Role.Ranger)
                return opponents.Where(opponent => opponent.Alive && opponent.slot % 3 == target.slot % 3).ToList();
            return new List<Fighter> { target };
        }

        public static List<CombatEffect> Resolve(Fighter actor, List<Fighter> allies, List<Fighter> opponents, bool skill)
        {
            var action = BeginAction(actor, allies, opponents, skill);
            var effects = new List<CombatEffect>(action.openingEffects);
            int strikes = action.canAct ? ActionHitCount(actor, action.skill) : 0;
            for (int strike = 0; strike < strikes; strike++)
                effects.AddRange(ResolveStrike(actor, allies, opponents, action.skill, strike == 0));
            EndAction(action);
            return effects;
        }

        public static bool EffectiveSkill(Fighter actor, bool requested) => requested
            && !actor.statuses.Has(BattleStatusKind.Silence) && !actor.statuses.Has(BattleStatusKind.Charm);
        public static int ActionHitCount(Fighter actor, bool skill) => !actor.statuses.Has(BattleStatusKind.Charm)
            && !skill && (actor.definition.doubleAttack || actor.definition.trait == TraitKind.DoubleStrike) ? 2 : 1;

        public static CombatAction BeginAction(Fighter actor, List<Fighter> allies, List<Fighter> opponents, bool requestedSkill)
        {
            var taunt = actor.statuses.Get(BattleStatusKind.Taunt);
            if (taunt != null && (taunt.SourceFighter == null || !taunt.SourceFighter.Alive)) actor.statuses.Remove(BattleStatusKind.Taunt);
            var action = new CombatAction { actor = actor, allies = allies, opponents = opponents,
                startingStatuses = actor.statuses.Entries.ToArray(), openingEffects = StartAction(actor) };
            action.skill = EffectiveSkill(actor, requestedSkill);
            action.canAct = actor.Alive && !actor.statuses.Has(BattleStatusKind.Stun)
                && (!actor.statuses.Has(BattleStatusKind.Charm) || allies.Any(ally => ally != actor && ally.Alive));
            if (actor.Alive && !action.canAct) action.openingEffects.Add(new CombatEffect { target = actor,
                status = actor.statuses.Has(BattleStatusKind.Stun) ? "晕眩：跳过行动" : "魅惑：无可攻击友军，跳过行动" });
            return action;
        }
        public static void EndAction(CombatAction action)
        {
            action.actor.statuses.Advance(action.startingStatuses);
            foreach(var fighter in action.allies.Concat(action.opponents))
            {
                var taunt = fighter.statuses.Get(BattleStatusKind.Taunt);
                if(taunt != null && (taunt.SourceFighter == null || !taunt.SourceFighter.Alive)) fighter.statuses.Remove(BattleStatusKind.Taunt);
            }
        }

        public static List<CombatEffect> ResolveStrike(Fighter actor, List<Fighter> allies, List<Fighter> opponents, bool skill, bool chargeEnergy)
        {
            var effects = new List<CombatEffect>();
            if (!actor.Alive) return effects;
            if (actor.statuses.Has(BattleStatusKind.Stun)) return effects;
            if (actor.statuses.Has(BattleStatusKind.Silence) || actor.statuses.Has(BattleStatusKind.Charm)) skill = false;
            var targets = FindTargets(actor, allies, opponents, skill);
            if (targets.Count == 0) return effects;
            int fallen = allies.Count(ally => !ally.Alive);
            if (chargeEnergy)
            {
                actor.actionCount++;
                if (actor.definition.bossMechanic == BossMechanic.Devour && fallen > actor.consumedAllies)
                {
                    var devour = Heal(actor, Mathf.RoundToInt(actor.definition.maxHealth * actor.definition.bossPower / 100f * (fallen - actor.consumedAllies)));
                    devour.status = "饥饿吞噬";
                    effects.Add(devour);
                    actor.consumedAllies = fallen;
                }
                if ((actor.definition.bossMechanic == BossMechanic.BarrierCycle || actor.definition.bossMechanic == BossMechanic.StormCycle)
                    && actor.actionCount % Math.Max(1, actor.definition.bossInterval) == 0)
                {
                    if (actor.definition.bossMechanic == BossMechanic.StormCycle) actor.definition.element = (Element)(((int)actor.definition.element + 1) % 3);
                    effects.Add(new CombatEffect { target = actor, shielding = AddShield(actor, actor.definition.bossPower),
                        status = actor.definition.bossMechanic == BossMechanic.StormCycle ? "属性轮转：" + actor.definition.ElementName : "破败屏障重生" });
                }
            }
            float bossDamage = actor.definition.bossMechanic == BossMechanic.PackLeader ? 1 + Math.Max(0, allies.Count(ally => ally.Alive) - 1) * actor.definition.bossPower / 100f
                : actor.definition.bossMechanic == BossMechanic.Devour ? 1 + fallen * .15f : 1;
            if (chargeEnergy && actor.definition.Regeneration > 0 && actor.health < actor.definition.maxHealth)
                effects.Add(Heal(actor, actor.definition.Regeneration));
            if (skill && actor.definition.statusEffect == nameof(BattleStatusKind.Cleanse))
                ApplySkillStatus(actor, targets, allies, opponents, true, effects);
            if (skill && actor.definition.skillKind != SkillKind.RoleDefault)
            {
                foreach (var victim in targets)
                    effects.Add(actor.definition.SupportSkill ? Heal(victim, actor.definition.skillPower)
                        : Damage(actor, victim, actor.definition.skillPower / 100f * bossDamage, actor.definition.skillKind == SkillKind.ArmorPierce));
            }
            else if (skill && actor.definition.role == Role.Guardian)
            {
                var patient = targets[0];
                var healing = Heal(patient, actor.definition.guardianHealing);
                healing.shielding = AddShield(patient, actor.definition.guardianShield);
                effects.Add(healing);
                if (patient != actor) effects.Add(new CombatEffect { target = actor, shielding = AddShield(actor, actor.definition.guardianShield) });
            }
            else
            {
                float power = 1f;
                if (skill)
                {
                    switch (actor.definition.role)
                    {
                        case Role.Warrior: power = 1.5f; break;
                        case Role.Mage:
                            power = 0.95f;
                            break;
                        case Role.Ranger:
                            power = 1.2f;
                            break;
                    }
                }
                foreach (var victim in targets.ToList()) effects.Add(Damage(actor, victim, power * bossDamage));
                if (skill && actor.definition.role == Role.Warrior)
                    effects.Add(Heal(actor, Mathf.RoundToInt(effects.Sum(effect => effect.damage) * 0.5f)));
            }
            int lifeDamage = effects.Sum(effect => effect.damage);
            if (lifeDamage > 0 && actor.definition.Lifesteal > 0)
                effects.Add(Heal(actor, Mathf.RoundToInt(lifeDamage * actor.definition.Lifesteal / 100f)));
            if (skill && actor.definition.bossMechanic == BossMechanic.SoulDrain)
            {
                int drained = 0;
                foreach (var victim in targets.Where(target => target.Alive && target.energy > 0))
                {
                    victim.energy--;
                    drained++;
                    effects.Add(new CombatEffect { target = victim, energyLost = 1, status = "黑雾夺能" });
                }
                if (drained > 0) effects.Add(new CombatEffect { target = actor, shielding = AddShield(actor, drained * actor.definition.bossPower), status = "夺能结界" });
            }
            if (actor.definition.statusEffect != nameof(BattleStatusKind.Cleanse))
                ApplySkillStatus(actor, targets, allies, opponents, skill, effects);
            if (chargeEnergy) actor.energy = skill ? 0 : Mathf.Min(SkillEnergyCost, actor.energy + 1);
            return effects;
        }

        private static List<CombatEffect> StartAction(Fighter actor)
        {
            var effects = new List<CombatEffect>();
            foreach (var status in actor.statuses.Entries.Where(entry => entry.Kind == BattleStatusKind.Poison || entry.Kind == BattleStatusKind.Burn).ToArray())
            {
                if (!actor.Alive) break;
                var effect = ApplyDamage(actor, Mathf.Max(1, status.Magnitude));
                effect.status = status.Name + "持续伤害" + (string.IsNullOrEmpty(effect.status) ? "" : "；" + effect.status);
                effects.Add(effect);
            }
            return effects;
        }

        private static void ApplySkillStatus(Fighter actor, List<Fighter> targets, List<Fighter> allies, List<Fighter> opponents,
            bool skill, List<CombatEffect> effects)
        {
            if (!skill || string.IsNullOrEmpty(actor.definition.statusEffect)) return;
            BattleStatusKind kind;
            if (!Enum.TryParse(actor.definition.statusEffect, out kind) || !BattleStatusCatalog.IsPersistent(kind) && kind != BattleStatusKind.Cleanse) return;
            if (kind == BattleStatusKind.Cleanse)
            {
                foreach (var target in targets.Where(target => target.Alive))
                {
                    int removed = target.statuses.Cleanse();
                    effects.Add(new CombatEffect { target = target, status = "净化" + (removed > 0 ? "：移除" + removed + "个负面效果" : "：无可移除效果") });
                }
                return;
            }
            var selected = actor.definition.statusAllEnemies ? opponents.Where(target => target.Alive).ToList() : targets.Where(target => target.Alive).ToList();
            foreach (var target in selected)
            {
                bool dot = kind == BattleStatusKind.Poison || kind == BattleStatusKind.Burn;
                int magnitude = dot ? Mathf.Max(1, Mathf.RoundToInt(actor.definition.attack * actor.definition.statusPower / 100f)) : actor.definition.statusPower;
                string description = BattleStatusCatalog.Rule(kind, actor.definition.statusPower)
                    + (dot ? "当前每次持续伤害：" + magnitude + "。" : "");
                target.statuses.Set(kind, Mathf.Max(1, actor.definition.statusTurns), description,
                    actor.definition.displayName + " · " + actor.definition.SkillName, 1, magnitude, actor);
                effects.Add(new CombatEffect { target = target, status = "施加" + BattleStatusCatalog.Name(kind) });
            }
        }

        private static CombatEffect Damage(Fighter actor, Fighter target, float power, bool ignoreDefense = false)
        {
            float frenzy = actor.definition.trait == TraitKind.Enraged && actor.health <= actor.definition.maxHealth / 2f
                ? 1 + actor.definition.traitPower / 100f : 1;
            int defense = EffectiveDefense(target);
            int amount = Mathf.Max(1, Mathf.RoundToInt(actor.definition.attack * power
                * ElementMultiplier(actor.definition.element, target.definition.element)
                * frenzy * 100f / (100f + (ignoreDefense ? 0 : defense))));
            return ApplyDamage(target, amount);
        }

        private static CombatEffect ApplyDamage(Fighter target, int amount)
        {
            int absorbed = Mathf.Min(target.shield, amount);
            target.shield -= absorbed;
            int damage = Mathf.Min(target.health, amount - absorbed);
            target.health -= damage;
            if (!target.Alive) target.statuses.Clear();
            var effect = new CombatEffect { target = target, damage = damage, absorbed = absorbed };
            if (target.Alive && target.definition.bossMechanic == BossMechanic.StormCycle && !target.phaseTriggered && target.health <= target.definition.maxHealth / 2f)
            {
                target.phaseTriggered = true;
                target.energy = SkillEnergyCost;
                effect.shielding = AddShield(target, target.definition.bossPower);
                effect.status = "星穹觉醒：技力蓄满";
            }
            return effect;
        }

        private static CombatEffect Heal(Fighter target, int amount)
        {
            var wounds = target.statuses.Get(BattleStatusKind.GrievousWounds);
            int reduced = wounds == null ? amount : Mathf.FloorToInt(amount * Mathf.Clamp01(1 - wounds.Magnitude / 100f));
            int healing = target.Alive ? Mathf.Min(reduced, target.definition.maxHealth - target.health) : 0;
            target.health += healing;
            return new CombatEffect { target = target, healing = healing };
        }

        public static int EffectiveDefense(Fighter fighter)
        {
            var status = fighter.statuses.Get(BattleStatusKind.ArmorBreak);
            return status == null ? fighter.definition.defense : Mathf.Max(0,
                Mathf.RoundToInt(fighter.definition.defense * Mathf.Clamp01(1 - status.Magnitude / 100f)));
        }

        private static int AddShield(Fighter target, int amount)
        {
            int added = Mathf.Min(amount, target.definition.maxHealth - target.shield);
            target.shield += added;
            return added;
        }

        public static List<CharacterDefinition> AvailableCharacters()
        {
            var roster = new List<CharacterDefinition>
            {
                CharacterDefinition.Create("亚索", Role.Warrior, Element.Fire)
            };
            roster.AddRange(DefaultTeam(false).Where(definition => definition.role != Role.Warrior || definition.element != Element.Fire));
            roster.Add(CharacterDefinition.Create("瑟庄妮", Role.Guardian, Element.Fire));
            roster.Add(CharacterDefinition.Create("盖伦", Role.Warrior, Element.Water));
            roster.Add(CharacterDefinition.Create("易", Role.Warrior, Element.Grass));
            roster.Add(CharacterDefinition.Create("阿狸", Role.Mage, Element.Fire));
            roster.Add(CharacterDefinition.Create("奥瑞利安·索尔", Role.Mage, Element.Grass));
            roster.Add(CharacterDefinition.Create("金克丝", Role.Ranger, Element.Water));
            return roster;
        }

        public static List<CharacterDefinition> DefaultTeam(bool enemy)
        {
            return enemy ? new List<CharacterDefinition>
            {
                CharacterDefinition.Create("诺克萨斯重卫", Role.Guardian, Element.Water),
                CharacterDefinition.Create("诺克萨斯突击兵", Role.Warrior, Element.Fire),
                CharacterDefinition.Create("沃里克·追猎者", Role.Warrior, Element.Grass),
                CharacterDefinition.Create("诺克萨斯战地医师", Role.Mage, Element.Fire),
                CharacterDefinition.Create("伊莉丝·蛛群", Role.Ranger, Element.Grass),
                CharacterDefinition.Create("斯维因·鸦群", Role.Mage, Element.Water)
            } : new List<CharacterDefinition>
            {
                CharacterDefinition.Create("布隆", Role.Guardian, Element.Grass),
                CharacterDefinition.Create("亚索", Role.Warrior, Element.Fire),
                CharacterDefinition.Create("蕾欧娜", Role.Guardian, Element.Water),
                CharacterDefinition.Create("拉克丝", Role.Mage, Element.Water),
                CharacterDefinition.Create("艾希", Role.Ranger, Element.Grass),
                CharacterDefinition.Create("图奇", Role.Ranger, Element.Fire)
            };
        }
    }
}
