using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ElementalTactics
{
    // Planning scores are metadata: never multiply these coefficients into a
    // fighter's attributes or ItemBonuses; the real mechanisms already apply.
    public sealed class HandbookBalanceConfig : ScriptableObject
    {
        public HandbookSnapshot snapshot = new HandbookSnapshot();
        public static HandbookBalanceConfig Current => Resources.Load<HandbookBalanceConfig>("Data/HandbookBalance");

        public HandbookUnitScore Unit(string id) => snapshot.units.FirstOrDefault(entry => entry.id == id);
        public HandbookItemScore Item(string id) => snapshot.items.FirstOrDefault(entry => entry.id == id);

        public double BaseStrength(CharacterDefinition unit)
        {
            var p = snapshot.parameters;
            double speed = 1 + Math.Max(-p.speedLimit, Math.Min(p.speedLimit, p.speedWeight * (unit.speed / p.referenceSpeed - 1)));
            return p.referenceScore * Math.Sqrt(unit.attack / p.referenceAttack
                * (unit.maxHealth * (1 + unit.defense / p.defenseConstant)
                    / (p.referenceHealth * (1 + p.referenceDefense / p.defenseConstant))) * speed);
        }

        public double EvaluatedStrength(CharacterDefinition unit)
        {
            var score = Unit(unit.id);
            if (score == null) throw new InvalidOperationException("缺少手册强度记录：" + unit.id);
            // Preserve an explicitly entered total while scaling changed base stats.
            return score.baseStrength > 0 ? score.totalStrength * BaseStrength(unit) / score.baseStrength : score.totalStrength;
        }

        public double EvaluatedLineup(IEnumerable<CharacterDefinition> units)
            => units.Where(unit => unit != null).Sum(EvaluatedStrength);
    }

    [Serializable] public sealed class HandbookSnapshot
    {
        public int schemaVersion, analysisDifficulty;
        public string sourceWorkbook, sourceSha256, gameWorkbookSha256, analysisNotes;
        public HandbookStrengthParameters parameters = new HandbookStrengthParameters();
        public HandbookUnitScore[] units = Array.Empty<HandbookUnitScore>();
        public HandbookItemScore[] items = Array.Empty<HandbookItemScore>();
        public HandbookStageScore[] stages = Array.Empty<HandbookStageScore>();
        public HandbookEventScore[] events = Array.Empty<HandbookEventScore>();
        public HandbookNodeExpectation[] nodeExpectations = Array.Empty<HandbookNodeExpectation>();
        public HandbookPoolEntry[] poolEntries = Array.Empty<HandbookPoolEntry>();
    }

    [Serializable] public sealed class HandbookStrengthParameters
    {
        public double referenceHealth, referenceAttack, referenceDefense, referenceSpeed;
        public double referenceScore, speedWeight, speedLimit, defenseConstant;
    }
    [Serializable] public sealed class HandbookUnitScore
    {
        public string id, name, quality, skillDescription;
        public bool isEnemy;
        public int maxHealth, attack, defense, speed, skillEnergyCost;
        public double baseStrength, mechanismCoefficient, totalStrength;
    }
    [Serializable] public sealed class HandbookItemScore
    {
        public string id, name, acquisitionRules, effectDescription, runtimeDescription;
        public bool hasGainCoefficient;
        public double gainCoefficient;
    }
    [Serializable] public sealed class HandbookStageScore
    {
        public string nodeId, battleId, name;
        public int floor, minRow, maxRow, weight, rewardTier;
        public string[] enemyIds;
        public double baseDifficulty, scaledDifficulty, rewardPower;
    }
    [Serializable] public sealed class HandbookEventScore
    {
        public string id, name;
        public double itemGainExpectation, heroPowerExpectation, heroQuantityExpectation, itemQuantityExpectation;
    }
    [Serializable] public sealed class HandbookNodeExpectation
    {
        public int x, floor, localRow;
        public double battleWeight, eventWeight, shopWeight, battleExperience;
        public double levelExpectation, heroPowerExpectation, itemGainExpectation;
        public double instantPowerExpectation, powerDeltaExpectation, powerGrowthRate, difficultyExpectation;
    }
    [Serializable] public sealed class HandbookPoolEntry
    {
        public string id, poolId, targetId, type;
        public int weight, minQuantity, maxQuantity;
    }
}
