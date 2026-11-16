using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ElementalTactics
{
    public static partial class Progression
    {
        public static int Level { get; private set; } = 1;
        public static int Experience { get; private set; }
        public static LevelRecord Current => GameContent.Levels.rows.First(row => row.level == Level);
        public static bool IsMaxLevel => Current.experienceToNext == 0;
        public static string Label => "等级 " + Level + (IsMaxLevel ? " · 已满级" : " · 经验 " + Experience + " / " + Current.experienceToNext);
        public static void Reset() { Level = 1; Experience = 0; }
        public static void AddExperience(int amount)
        {
            if (amount <= 0 || IsMaxLevel) return;
            long total = (long)Experience + amount;
            while (!IsMaxLevel && total >= Current.experienceToNext)
            {
                total -= Current.experienceToNext;
                Level++;
            }
            Experience = IsMaxLevel ? 0 : (int)total;
        }
    }

    public static class RarityRules
    {
        public static string Name(Rarity rarity) => rarity == Rarity.Epic ? "史诗" : rarity == Rarity.Rare ? "稀有" : "普通";
        public static Color Color(Rarity rarity) => rarity == Rarity.Epic ? PrototypeUI.Hex("9255AF") : rarity == Rarity.Rare ? PrototypeUI.Hex("377FAB") : PrototypeUI.Muted;
        public static Rarity Of(EffectRecord effect) => effect.type == EffectKind.GiveCharacter ? GameContent.Characters.Get(effect.targetId).rarity
            : effect.type == EffectKind.GiveItem ? GameContent.Items.Get(effect.targetId).rarity : Rarity.Common;
        public static T Pick<T>(IList<T> candidates, Func<T, Rarity> rarity, Func<T, int> weight, System.Random random, int level)
        {
            var config = GameContent.Levels.rows.First(row => row.level == level);
            var tiers = new[] { Rarity.Common, Rarity.Rare, Rarity.Epic }.Where(tier => candidates.Any(entry => rarity(entry) == tier)).ToList();
            Func<Rarity, int> tierWeight = tier => tier == Rarity.Common ? config.commonWeight : tier == Rarity.Rare ? config.rareWeight : config.epicWeight;
            var selectedTier = Weighted(tiers, tierWeight, random);
            return Weighted(candidates.Where(entry => rarity(entry) == selectedTier).ToList(), weight, random);
        }
        private static T Weighted<T>(IList<T> entries, Func<T, int> weight, System.Random random)
        {
            if (entries.Count == 0) throw new InvalidOperationException("抽取池不能为空");
            double roll = random.NextDouble() * entries.Sum(entry => (double)weight(entry));
            foreach (var entry in entries) { roll -= weight(entry); if (roll < 0) return entry; }
            return entries[entries.Count - 1];
        }
    }
}
