using System;
using System.Collections.Generic;
using System.Linq;

namespace ElementalTactics
{
    public static class ItemBonuses
    {
        public static bool IsPassive(EffectKind type) => type == EffectKind.AttackBonus || type == EffectKind.HealthBonus
            || type == EffectKind.DefenseBonus || type == EffectKind.SpeedBonus || type == EffectKind.DoubleAttack
            || type == EffectKind.OpeningShield || type == EffectKind.Regeneration || type == EffectKind.Lifesteal || type == EffectKind.HealthPenalty;

        public static string Summary(ItemRecord item, int quantity = 1)
        {
            if (string.IsNullOrEmpty(item.effectId)) return item.kind == "剧情" ? item.description : "无战斗加成";
            var effect = GameContent.Effects.Get(item.effectId);
            long amount = (long)effect.amount * quantity;
            switch (effect.type)
            {
                case EffectKind.AttackBonus: return "我方全体攻击 +" + amount;
                case EffectKind.HealthBonus: return "我方全体最大生命 +" + amount;
                case EffectKind.DefenseBonus: return "我方全体防御 +" + amount;
                case EffectKind.SpeedBonus: return "我方全体速度 +" + amount
                    + (effect.bonusAttack > 0 ? "，攻击 +" + (long)effect.bonusAttack * quantity : "");
                case EffectKind.DoubleAttack: return "我方全体普攻连续攻击2次（不叠加）";
                case EffectKind.OpeningShield: return "我方全体开战护盾 +" + amount;
                case EffectKind.Regeneration: return "我方全体每次行动回复 " + amount + " 生命";
                case EffectKind.Lifesteal: return "我方全体伤害吸血 +" + amount + "%（上限100%）";
                case EffectKind.HealthPenalty: return "我方全体最大生命 −" + amount + "（最低1）";
                default: throw new InvalidOperationException(item.id + "的效果不是被动加成");
            }
        }

        public static string Description(ItemRecord item, int quantity = 1)
        {
            if (string.IsNullOrEmpty(item.effectId)) return item.description;
            var kind = GameContent.Effects.Get(item.effectId).type;
            bool doubleAttack = kind == EffectKind.DoubleAttack;
            return Summary(item, quantity) + "\n" + item.description + "\n持有即生效，下场战斗自动应用。"
                + (doubleAttack ? "每次普攻行动仅获得1点技力；技能不连击，目标阵亡后转向下一敌人。"
                    : kind == EffectKind.Lifesteal ? "按造成的实际生命伤害回复，护盾吸收部分不计；总吸血最高100%。"
                    : kind == EffectKind.OpeningShield ? "数量可叠加，开战时一次获得；护盾不超过角色最大生命。"
                    : kind == EffectKind.Regeneration ? "数量可叠加，每次行动仅触发一次；不复活、不超过最大生命。"
                    : "按持有数量叠加，以上为当前总加成；单项属性最高100万。")
                + "\n本局有效，无需消耗，敌人不享受加成。";
        }

        public static CharacterDefinition Apply(CharacterDefinition original) => Apply(original, InventoryState.Items);

        // Explicit inventory lets balance experiments use the exact game rules without touching the player's items.
        public static CharacterDefinition Apply(CharacterDefinition original, IEnumerable<InventoryItem> items)
        {
            var result = original.Clone();
            long healthPenalty = 0;
            foreach (var stack in items)
            {
                var item = GameContent.Items.rows.FirstOrDefault(entry => entry.id == stack.Id);
                if (item == null || string.IsNullOrEmpty(item.effectId)) continue;
                var effect = GameContent.Effects.Get(item.effectId);
                long amount = (long)effect.amount * stack.Quantity;
                switch (effect.type)
                {
                    case EffectKind.AttackBonus: result.attack = Add(result.attack, amount); break;
                    case EffectKind.HealthBonus: result.maxHealth = Add(result.maxHealth, amount); break;
                    case EffectKind.DefenseBonus: result.defense = Add(result.defense, amount); break;
                    case EffectKind.SpeedBonus:
                        result.speed = Add(result.speed, amount);
                        result.attack = Add(result.attack, (long)effect.bonusAttack * stack.Quantity);
                        break;
                    case EffectKind.DoubleAttack: result.doubleAttack = true; break;
                    case EffectKind.OpeningShield: result.openingShield = Add(result.openingShield, amount); break;
                    case EffectKind.Regeneration: result.regeneration = Add(result.regeneration, amount); break;
                    case EffectKind.Lifesteal: result.lifesteal = (int)Math.Min(100L, result.lifesteal + amount); break;
                    case EffectKind.HealthPenalty: healthPenalty += amount; break;
                }
            }
            result.maxHealth = (int)Math.Max(1L, result.maxHealth - healthPenalty);
            return result;
        }
        private static int Add(int value, long amount) => (int)Math.Min(1000000L, value + amount);
    }
}
