using System;
using System.Collections.Generic;
using System.Linq;

namespace ElementalTactics
{
    [Serializable]
    public sealed class MechanismSearchSettings
    {
        public double minimum = .1, maximum = 5, precision = .001;
        public int maxRounds = CombatRules.MaxRounds;
        public void Validate()
        {
            if (double.IsNaN(minimum) || double.IsNaN(maximum) || double.IsNaN(precision)
                || double.IsInfinity(minimum) || double.IsInfinity(maximum) || double.IsInfinity(precision)
                || minimum <= 0 || maximum < minimum || maximum > 100 || precision < .0001 || precision > 1
                || (maximum - minimum) / precision > 100000 || maxRounds < 1 || maxRounds > 1000)
                throw new ArgumentException("范围须为 0 < 下限 ≤ 上限 ≤ 100；精度 0.0001～1；最多 10 万个采样点。");
        }
    }
    [Serializable]
    public sealed class MechanismInterval
    {
        public double minimum, maximum, originalWinningCoefficient;
        public int lowerWinner, upperWinner;
        public MechanismTrial lowerEvidence, upperEvidence;
    }
    [Serializable]
    public sealed class MechanismOutcomeRange { public double minimum, maximum; public int winner, samples; }
    [Serializable]
    public sealed class MechanismFighterState
    {
        public string id, name;
        public int slot, health, maxHealth, shield, energy, actions, attack, defense, speed;
        public bool isSubject, skillDisabled;
    }
    [Serializable]
    public sealed class MechanismTrial
    {
        public double coefficient;
        public bool timedOut;
        public int winner = -1; // -1: unresolved/timeout, 0: original, 1: mirror
        public int round, firstDefeated = -1, originalHealth, mirrorHealth, originalShield, mirrorShield;
        public int originalEnergy, mirrorEnergy, originalMaxHealth, mirrorMaxHealth;
        public int originalAlive, mirrorAlive;
        public MechanismFighterState[] originalTeam, mirrorTeam;
        public string[] log;
    }
    [Serializable]
    public sealed class MechanismResult
    {
        public string id, name, category, status, limitation;
        public bool hasTransitions;
        public double coefficient; // Valid only when hasTransitions; minimum ORIGINAL-winning transition endpoint.
        // Item mode only: coefficient remains the equivalent-stat multiplier used for combat replay.
        public double itemGainCoefficient;
        public int tested, originalWins, mirrorWins, timedOutSamples;
        public MechanismSearchSettings settings;
        public MechanismInterval[] intervals;
        public MechanismOutcomeRange[] outcomes;
    }
    [Serializable]
    public sealed class MechanismReport
    {
        public string version = "3.0", generatedUtc;
        public string rule = "原版对象+5个亚索 vs 无技能镜像+5个亚索，双方均为2排3列，对象在前排左侧0号槽、亚索占1至5号槽。一方全队阵亡才判负。仅镜像对象生命/攻击/防御/速度乘系数并取整，10个亚索不缩放且正常释放技能。每轮按速度降序，同速按槽位升序、同槽原版队先手，跳过已阵亡单位。相邻采样胜者不同且均未超时才形成转换区间；取原版队获胜端点，多段时取这些端点的最小值并保留所有区间。超时不算胜负，不跨超时拼接；无转换时系数无效。";
        public string scope = "使用基础配置，无道具、楼层或冒险倍率。亚索来自HERO_001，双方同配置且各自独立。群攻、治疗、护盾及队友联动按实际战斗规则结算。镜像对象禁用技能/特性/首领机制，保留普攻技力积攒；队友不禁用。系数只代表固定五亚索阵容与站位下的测算结果。";
        public MechanismResult[] results;
    }

    /// <summary>Isolated balance experiment. Does not mutate game databases, inventory or progression.</summary>
    public static class MechanismCoefficientSimulator
    {
        public static bool IsBuffItem(ItemRecord item)
        {
            if (item == null || item.kind == "剧情" || string.IsNullOrEmpty(item.effectId)) return false;
            var effect = GameContent.Effects.Get(item.effectId);
            return ItemBonuses.IsPassive(effect.type) && effect.type != EffectKind.HealthPenalty
                && (effect.type == EffectKind.DoubleAttack || effect.amount > 0);
        }
        public static MechanismReport ItemReport(MechanismResult[] results) => new MechanismReport
        {
            version = "4.1-items", generatedUtc = DateTime.UtcNow.ToString("o"), results = results,
            rule = "持有1件增益道具的6个亚索 vs 未持有道具的6个亚索。双方技能均保留；仅未持有方6人的生命、攻击、防御、速度乘系数并按游戏规则取整。双方2排3列，按速度降序、同速按槽位、同槽持有方先手。一方全队阵亡判负。相邻有效采样胜者不同形成转换区间，取持有方获胜端点，多段取这些端点最小值。超时不计胜负、不跨超时拼接。",
            scope = "仅增益道具，每次持有1件、全队生效，双方亚索来自HERO_001，无其他道具或难度倍率。coefficient为等效属性系数。itemGainCoefficient为亚索四项数值直接乘该系数后的基础强度÷原基础强度，换算保留小数、不减1。基础强度沿用手册默认模型：标准440生命/90攻击/24防御/70速度为1000分，防御常数100，速度权重0.2、修正上限0.1。剧情、负面道具不测算；配置变化需重测，Excel参数修改不会自动导入Unity。"
        };
        public static double BaseStatStrength(double health, double attack, double defense, double speed)
        {
            double effectiveHealth = health * (1 + defense / 100.0);
            double speedFactor = 1 + Math.Max(-.1, Math.Min(.1, .2 * (speed / 70.0 - 1)));
            return 1000 * Math.Sqrt((attack / 90.0) * (effectiveHealth / (440 * 1.24)) * speedFactor);
        }
        public static double ItemGain(double equivalentStatCoefficient)
        {
            if (equivalentStatCoefficient <= 0 || double.IsNaN(equivalentStatCoefficient) || double.IsInfinity(equivalentStatCoefficient))
                throw new ArgumentException("等效属性系数须为正数");
            var yasuo = Yasuo(); double k = equivalentStatCoefficient;
            return BaseStatStrength(yasuo.maxHealth * k, yasuo.attack * k, yasuo.defense * k, yasuo.speed * k)
                / BaseStatStrength(yasuo.maxHealth, yasuo.attack, yasuo.defense, yasuo.speed);
        }
        public static CharacterDefinition Yasuo()
        {
            var record = GameContent.Characters.rows.FirstOrDefault(u => u.id == "HERO_001");
            if (record == null) throw new InvalidOperationException("缺少陪测亚索 HERO_001");
            return record.Create();
        }
        public static CharacterDefinition Mirror(CharacterDefinition source, double coefficient)
        {
            var d = ScaleStats(source, coefficient);
            d.doubleAttack = false; d.trait = TraitKind.None; d.traitPower = 0;
            d.bossMechanic = BossMechanic.None; d.bossPower = d.bossInterval = 0;
            d.openingShield = d.regeneration = d.lifesteal = 0;
            // The runner forces skill=false for this fighter; RoleDefault is not a no-skill enum.
            d.skillPower = 0;
            d.statusEffect = ""; d.statusTurns = d.statusPower = 0; d.statusAllEnemies = false;
            return d;
        }
        public static CharacterDefinition ScaleStats(CharacterDefinition source, double coefficient)
        {
            var d = source.Clone();
            d.maxHealth = AdventureDifficulty.Scale(source.maxHealth, coefficient, 1);
            d.attack = AdventureDifficulty.Scale(source.attack, coefficient, 1);
            d.defense = AdventureDifficulty.Scale(source.defense, coefficient);
            d.speed = AdventureDifficulty.Scale(source.speed, coefficient, 1);
            return d;
        }
        public sealed class Duel
        {
            public readonly Fighter original, mirror;
            public bool Finished { get; private set; }
            public int Winner { get; private set; } = -1;
            public bool TimedOut { get; private set; }
            public int Round { get; private set; }
            public int FirstDefeated { get; private set; } = -1;
            public readonly List<string> Log = new List<string>();
            readonly List<Fighter> originals, mirrors;
            public IReadOnlyList<Fighter> OriginalTeam => originals;
            public IReadOnlyList<Fighter> MirrorTeam => mirrors;
            readonly int maxRounds;
            readonly bool captureLog;
            readonly double coefficient;
            readonly bool itemMode;
            List<Fighter> turnOrder;
            int actionInRound;
            public Duel(CharacterDefinition source, double value, int rounds = CombatRules.MaxRounds, bool trace = false, CharacterDefinition teammate = null)
            {
                if (source == null || value <= 0 || double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentException("无效测试对象或系数");
                coefficient = value; maxRounds = rounds; captureLog = trace;
                original = new Fighter(source, false, 0); mirror = new Fighter(Mirror(source, value), true, 0);
                originals = new List<Fighter> { original }; mirrors = new List<Fighter> { mirror };
                var yasuo = teammate ?? Yasuo();
                for (int slot = 1; slot < 6; slot++)
                {
                    originals.Add(new Fighter(yasuo, false, slot));
                    mirrors.Add(new Fighter(yasuo, true, slot));
                }
                foreach (var f in originals.Concat(mirrors)) f.tieOrder = f.slot * 2 + (f.enemy ? 1 : 0);
            }
            public Duel(ItemRecord item, double value, int rounds = CombatRules.MaxRounds, bool trace = false)
            {
                if (!IsBuffItem(item) || value <= 0 || double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentException("只能测算增益道具，系数须为正数");
                itemMode = true; coefficient = value; maxRounds = rounds; captureLog = trace;
                var yasuo = Yasuo();
                var buffed = ItemBonuses.Apply(yasuo, new[] { new InventoryItem(item.id, item.name, item.description, 1) });
                var scaled = ScaleStats(yasuo, value);
                originals = new List<Fighter>(); mirrors = new List<Fighter>();
                for (int slot = 0; slot < 6; slot++)
                {
                    originals.Add(new Fighter(buffed, false, slot)); mirrors.Add(new Fighter(scaled, true, slot));
                }
                original = originals[0]; mirror = mirrors[0];
                foreach (var f in originals.Concat(mirrors)) f.tieOrder = f.slot * 2 + (f.enemy ? 1 : 0);
            }
            string Side(Fighter f) => (f.enemy ? "镜队" : "原队") + (f.slot + 1) + "号" + f.definition.displayName;
            void Note(string s) { if (captureLog) Log.Add(s); }
            void Act(Fighter actor)
            {
                bool skill = (itemMode || actor != mirror) && actor.energy >= CombatRules.SkillEnergyCost;
                var effects = CombatRules.Resolve(actor, actor.enemy ? mirrors : originals,
                    actor.enemy ? originals : mirrors, skill);
                if (captureLog) Note("第" + Round + "轮 " + Side(actor) + "「" + (skill ? actor.definition.SkillName : "普攻") + "」 "
                    + string.Join("；", effects.Select(e => Side(e.target) + " 伤害" + e.damage + " 格挡" + e.absorbed + " 治疗" + e.healing + " 护盾" + e.shielding + (string.IsNullOrEmpty(e.status) ? "" : " " + e.status)))
                    + " | 队伍生命 " + originals.Sum(f => f.health) + "/" + mirrors.Sum(f => f.health));
            }
            /// <summary>One living fighter's full action per call. A team wipe ends the battle.</summary>
            public void Step()
            {
                if (Finished) return;
                if (turnOrder == null || actionInRound >= turnOrder.Count)
                {
                    if (Round >= maxRounds) { Finished = TimedOut = true; Note("达到回合上限，双方仍有存活单位。"); return; }
                    Round++;
                    turnOrder = originals.Concat(mirrors).Where(f => f.Alive).OrderByDescending(f => f.definition.speed).ThenBy(f => f.tieOrder).ToList();
                    actionInRound = 0;
                }
                while (actionInRound < turnOrder.Count && !turnOrder[actionInRound].Alive) actionInRound++;
                if (actionInRound >= turnOrder.Count) { Step(); return; }
                var actor = turnOrder[actionInRound++];
                Act(actor);
                bool originalAlive = originals.Any(f => f.Alive), mirrorAlive = mirrors.Any(f => f.Alive);
                if (!originalAlive || !mirrorAlive)
                {
                    FirstDefeated = !originalAlive ? 0 : 1;
                    Winner = FirstDefeated == 0 ? 1 : 0;
                    Finished = true;
                    Note((Winner == 0 ? "原版队" : "镜像队") + "获胜：对方全队阵亡，战斗结束。");
                }
            }
            public MechanismTrial Snapshot() => new MechanismTrial { coefficient = coefficient, winner = Winner, timedOut = TimedOut,
                round = Round, firstDefeated = FirstDefeated, originalHealth = originals.Sum(f => f.health), mirrorHealth = mirrors.Sum(f => f.health),
                originalShield = originals.Sum(f => f.shield), mirrorShield = mirrors.Sum(f => f.shield), originalEnergy = original.energy, mirrorEnergy = mirror.energy,
                originalMaxHealth = originals.Sum(f => f.definition.maxHealth), mirrorMaxHealth = mirrors.Sum(f => f.definition.maxHealth),
                originalAlive = originals.Count(f => f.Alive), mirrorAlive = mirrors.Count(f => f.Alive),
                originalTeam = captureLog ? originals.Select(State).ToArray() : null, mirrorTeam = captureLog ? mirrors.Select(State).ToArray() : null, log = Log.ToArray() };
            MechanismFighterState State(Fighter f) => new MechanismFighterState { id = f.definition.id, name = f.definition.displayName,
                slot = f.slot, health = f.health, maxHealth = f.definition.maxHealth, shield = f.shield, energy = f.energy, actions = f.actionCount,
                attack = f.definition.attack, defense = f.definition.defense, speed = f.definition.speed,
                isSubject = !itemMode && (f == original || f == mirror), skillDisabled = !itemMode && f == mirror };
        }
        public static MechanismTrial Simulate(CharacterDefinition source, double coefficient, int rounds = CombatRules.MaxRounds, bool trace = false, CharacterDefinition teammate = null)
        {
            var duel = new Duel(source, coefficient, rounds, trace, teammate);
            while (!duel.Finished) duel.Step();
            return duel.Snapshot();
        }
        public sealed class Search
        {
            readonly CharacterDefinition source;
            readonly ItemRecord item;
            readonly CharacterDefinition teammate;
            readonly string category;
            readonly MechanismSearchSettings settings;
            readonly List<MechanismInterval> intervals = new List<MechanismInterval>();
            readonly int total;
            int tested, originalWins, mirrorWins, timeouts;
            readonly List<MechanismOutcomeRange> outcomes = new List<MechanismOutcomeRange>();
            MechanismTrial previous;
            public bool Finished => tested >= total;
            public float Progress => (float)tested / total;
            public Search(UnitRecord unit, string category, MechanismSearchSettings settings)
            {
                settings.Validate(); source = unit.Create(); teammate = Yasuo(); this.category = category;
                this.settings = new MechanismSearchSettings { minimum = settings.minimum, maximum = settings.maximum, precision = settings.precision, maxRounds = settings.maxRounds };
                total = (int)Math.Floor((settings.maximum - settings.minimum) / settings.precision + 1e-8) + 1;
            }
            public Search(ItemRecord item, MechanismSearchSettings settings)
            {
                settings.Validate();
                if (!IsBuffItem(item)) throw new ArgumentException("只能测算增益道具");
                this.item = item; category = "道具";
                this.settings = new MechanismSearchSettings { minimum = settings.minimum, maximum = settings.maximum, precision = settings.precision, maxRounds = settings.maxRounds };
                total = (int)Math.Floor((settings.maximum - settings.minimum) / settings.precision + 1e-8) + 1;
            }
            MechanismTrial Trial(double value, bool trace)
            {
                if (item == null) return Simulate(source, value, settings.maxRounds, trace, teammate);
                var duel = new Duel(item, value, settings.maxRounds, trace);
                while (!duel.Finished) duel.Step();
                return duel.Snapshot();
            }
            public void Advance(int count = 64)
            {
                for (int i = 0; i < count && !Finished; i++)
                {
                    double value = Math.Round(settings.minimum + tested * settings.precision, 8);
                    var result = Trial(value, false);
                    if (result.timedOut) timeouts++;
                    if (result.winner == 0) originalWins++;
                    if (result.winner == 1) mirrorWins++;
                    if (IsTransition(previous, result))
                    {
                        intervals.Add(new MechanismInterval { minimum = previous.coefficient, maximum = value,
                            lowerWinner = previous.winner, upperWinner = result.winner });
                    }
                    var band = outcomes.LastOrDefault();
                    if (band == null || band.winner != result.winner)
                    { band = new MechanismOutcomeRange { minimum = value, winner = result.winner }; outcomes.Add(band); }
                    band.maximum = value; band.samples++;
                    previous = result;
                    tested++;
                }
            }
            public MechanismResult Result()
            {
                if (!Finished) throw new InvalidOperationException("搜索未完成");
                foreach (var interval in intervals)
                {
                    interval.lowerEvidence = Trial(interval.minimum, true);
                    interval.upperEvidence = Trial(interval.maximum, true);
                    interval.originalWinningCoefficient = OriginalWinningEndpoint(interval);
                    if (!IsTransition(interval.lowerEvidence, interval.upperEvidence)
                        || interval.lowerEvidence.winner != interval.lowerWinner || interval.upperEvidence.winner != interval.upperWinner)
                        throw new InvalidOperationException("转换区间端点复测未通过");
                }
                return new MechanismResult { id = item == null ? source.id : item.id, name = item == null ? source.displayName : item.name, category = category, hasTransitions = intervals.Count > 0,
                    coefficient = intervals.Count > 0 ? intervals.Min(i => i.originalWinningCoefficient) : 0,
                    itemGainCoefficient = item != null && intervals.Count > 0 ? ItemGain(intervals.Min(i => i.originalWinningCoefficient)) : 0,
                    status = intervals.Count > 0 ? (intervals.Count > 1 ? "多次胜负转换" : "发现胜负转换") + (timeouts > 0 ? "（含超时）" : "")
                        : timeouts == tested ? "全部超时" : timeouts > 0 ? "未找到相邻转换（含超时）" : "范围内无胜负转换",
                    tested = tested, originalWins = originalWins, mirrorWins = mirrorWins, timedOutSamples = timeouts,
                    settings = settings, intervals = intervals.ToArray(), outcomes = outcomes.ToArray(),
                    limitation = item == null ? "固定五亚索队友、前排左侧测试位的6对6结果；阵容、站位或技力变化后需重测。"
                        : "每次仅持有1件道具，全队6亚索生效；双方保留技能，仅未持有方四项基础数值缩放。配置变化后需重测。" };
            }
        }
        // Called only for neighboring samples; timeout breaks adjacency and cannot count as a winner.
        public static double OriginalWinningEndpoint(MechanismInterval interval)
        {
            if (interval.lowerWinner == 0 && interval.upperWinner == 1) return interval.minimum;
            if (interval.lowerWinner == 1 && interval.upperWinner == 0) return interval.maximum;
            throw new ArgumentException("区间必须有且仅有一个原版获胜端点");
        }
        public static bool IsTransition(MechanismTrial lower, MechanismTrial upper) => lower != null && upper != null
            && !lower.timedOut && !upper.timedOut && lower.winner >= 0 && upper.winner >= 0 && lower.winner != upper.winner;
        public static MechanismResult Evaluate(UnitRecord unit, string category, MechanismSearchSettings settings = null)
        {
            var job = new Search(unit, category, settings ?? new MechanismSearchSettings());
            while (!job.Finished) job.Advance(256);
            return job.Result();
        }
        public static MechanismResult Evaluate(ItemRecord item, MechanismSearchSettings settings = null)
        {
            var job = new Search(item, settings ?? new MechanismSearchSettings());
            while (!job.Finished) job.Advance(256);
            return job.Result();
        }
    }
}
