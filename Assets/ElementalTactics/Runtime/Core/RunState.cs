using System;
using System.Collections.Generic;
using System.Linq;

namespace ElementalTactics
{
    public sealed class OwnedCharacter
    {
        public readonly string InstanceId = Guid.NewGuid().ToString("N");
        public readonly CharacterDefinition Definition;
        public OwnedCharacter(string id, string instanceId = null) { InstanceId = instanceId ?? Guid.NewGuid().ToString("N"); Definition = GameContent.Characters.Get(id).Create(); }
    }

    public static partial class RunState
    {
        private static readonly List<OwnedCharacter> characters = new List<OwnedCharacter>();
        private static bool initialized;
        private static string[] formationSlots;
        public const int InitialLives = 3;
        public static int Lives { get; private set; } = InitialLives;
        public static bool IsDefeated => Lives == 0;
        public static int Gold { get; private set; }
        public static IReadOnlyList<OwnedCharacter> Characters { get { Ensure(); return characters.AsReadOnly(); } }
        public static void Ensure()
        {
            if (initialized) return;
            characters.Add(new OwnedCharacter("HERO_001"));
            initialized = true;
        }
        public static void Reset()
        {
            initialized = false;
            Lives = InitialLives;
            Gold = 0;
            characters.Clear();
            ShopRun.Reset();
            Progression.Reset();
            formationSlots = null;
            RewardFlow.Reset();
        }
        public static bool RecordBattleDefeat(int nodeId)
        {
            if (IsDefeated || nodeId < 0 || nodeId != MapRun.Pending || RewardFlow.Pending != null
                || MapRun.Nodes[nodeId].Definition.type != NodeKind.Battle || MapRun.IsCleared(nodeId)) return false;
            Lives--;
            DailyChallengeState.ObserveRoster();
            AdventureRun.FinishIfNeeded();
            return true;
        }
        public static FormationState RestoreFormation()
        {
            Ensure();
            var state = new FormationState(characters.Count, 1);
            if (formationSlots == null) return state;
            for (int slot = 0; slot < 6; slot++) state.Remove(slot);
            for (int slot = 0; slot < formationSlots.Length; slot++)
            {
                string instanceId = formationSlots[slot];
                int index = characters.FindIndex(character => character.InstanceId == instanceId);
                if (index >= 0 && state.FindSlot(index) < 0) state.Place(index, slot);
            }
            return state;
        }
        public static void SaveFormation(FormationState state)
        {
            Ensure();
            formationSlots = new string[6];
            for (int slot = 0; slot < formationSlots.Length; slot++)
            {
                int index = state[slot];
                if (index >= 0 && index < characters.Count) formationSlots[slot] = characters[index].InstanceId;
            }
        }
        public static bool CanApply(IEnumerable<EffectRecord> effects, int cost = 0)
        {
            if (cost < 0 || Gold < cost) return false;
            long gold = (long)Gold - cost;
            var amounts = InventoryState.Items.ToDictionary(item => item.Id, item => (long)item.Quantity);
            foreach (var effect in effects)
            {
                if (effect.amount <= 0) return false;
                if (effect.type == EffectKind.AddGold) gold += effect.amount;
                else if (effect.type == EffectKind.AddExperience) { }
                else if (effect.type == EffectKind.SpendGold) gold -= effect.amount;
                else if (effect.type == EffectKind.GiveItem)
                {
                    GameContent.Items.Get(effect.targetId);
                    amounts.TryGetValue(effect.targetId, out long amount);
                    amounts[effect.targetId] = amount + effect.amount;
                    if (amounts[effect.targetId] > int.MaxValue) return false;
                }
                else if (effect.type == EffectKind.GiveCharacter) GameContent.Characters.Get(effect.targetId);
                else return false;
                if (gold < 0 || gold > int.MaxValue) return false;
            }
            return true;
        }
        public static bool Apply(IEnumerable<EffectRecord> effects, int cost = 0)
        {
            Ensure();
            var batch = effects.ToArray();
            if (!CanApply(batch, cost)) return false;
            Gold -= cost;
            foreach (var effect in batch)
            {
                if (effect.type == EffectKind.AddGold) Gold += effect.amount;
                else if (effect.type == EffectKind.AddExperience) Progression.AddExperience(effect.amount);
                else if (effect.type == EffectKind.SpendGold) Gold -= effect.amount;
                else if (effect.type == EffectKind.GiveCharacter)
                    for (int index = 0; index < effect.amount; index++) characters.Add(new OwnedCharacter(effect.targetId));
                else
                {
                    var item = GameContent.Items.Get(effect.targetId);
                    InventoryState.TryAdd(item.id, item.name, item.description, effect.amount);
                }
            }
            DailyChallengeState.ObserveRoster();
            return true;
        }
        public static EffectRecord[] Effects(EventOptionRecord option) => option.effectIds.Select(id => GameContent.Effects.Get(id)).ToArray();
        public static bool ChooseEvent(string optionId)
        {
            if (MapRun.Pending < 0) return false;
            var node = MapRun.Nodes[MapRun.Pending];
            if (node.Definition.type != NodeKind.Event) return false;
            var option = GameContent.Events.Get(node.Definition.contentId).options.FirstOrDefault(entry => entry.id == optionId);
            if (option == null || !Apply(Effects(option))) return false;
            return MapRun.Complete(node.Id);
        }
        public static int Stock(int nodeId, ShopGoodRecord good)
        {
            return ShopRun.Stock(nodeId, good);
        }
        public static bool Buy(string goodId)
        {
            return ShopRun.Buy(goodId);
        }
        public static string Describe(EffectRecord effect)
        {
            if (effect.type == EffectKind.AddGold) return "金币 +" + effect.amount;
            if (effect.type == EffectKind.AddExperience) return "冒险经验 +" + effect.amount;
            if (effect.type == EffectKind.SpendGold) return "消耗 " + effect.amount + " 金币";
            return (effect.type == EffectKind.GiveItem ? GameContent.Items.Get(effect.targetId).name : GameContent.Characters.Get(effect.targetId).name) + " ×" + effect.amount;
        }
    }
}
