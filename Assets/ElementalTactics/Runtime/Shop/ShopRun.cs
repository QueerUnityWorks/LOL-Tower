using System;
using System.Collections.Generic;
using System.Linq;

namespace ElementalTactics
{
    public static partial class ShopRun
    {
        private sealed class StockState
        {
            public ShopGoodRecord[] offers;
            public int refreshes;
            public readonly Dictionary<string, int> bought = new Dictionary<string, int>();
        }
        private static readonly Dictionary<int, StockState> stores = new Dictionary<int, StockState>();
        public static void Reset() { stores.Clear(); }
        private static ShopRecord Config(int nodeId) => GameContent.Shops.Get(MapRun.Nodes[nodeId].Definition.contentId);
        private static StockState State(int nodeId)
        {
            if (!stores.TryGetValue(nodeId, out var state))
            {
                state = new StockState { offers = Roll(nodeId, 0) };
                stores.Add(nodeId, state);
            }
            return state;
        }
        public static IReadOnlyList<ShopGoodRecord> Offers(int nodeId) => Array.AsReadOnly(State(nodeId).offers);
        public static int RefreshPrice(int nodeId)
        {
            var config = Config(nodeId);
            return (int)Math.Min(int.MaxValue, config.refreshCost + (long)config.refreshIncrease * State(nodeId).refreshes);
        }
        public static int Stock(int nodeId, ShopGoodRecord good)
        {
            var state = State(nodeId);
            if (!state.offers.Any(entry => entry.id == good.id)) return 0;
            state.bought.TryGetValue(good.id, out int count);
            return good.stock - count;
        }
        public static bool Buy(string id)
        {
            int nodeId = MapRun.Pending;
            if (!IsActive(nodeId)) return false;
            var state = State(nodeId);
            var good = state.offers.FirstOrDefault(entry => entry.id == id);
            if (good == null || Stock(nodeId, good) <= 0) return false;
            var effect = GameContent.Effects.Get(good.effectId);
            if (effect.type == EffectKind.AddExperience && Progression.IsMaxLevel) return false;
            if (!RunState.Apply(new[] { effect }, good.price)) return false;
            state.bought.TryGetValue(id, out int count);
            state.bought[id] = count + 1;
            if (TutorialRun.Active && id == TutorialContent.SwordGood) TutorialRun.Mark("shop.buy");
            return true;
        }
        public static bool Refresh()
        {
            int nodeId = MapRun.Pending;
            if (!IsActive(nodeId)) return false;
            var state = State(nodeId);
            int price = RefreshPrice(nodeId);
            if (RunState.Gold < price || state.refreshes == int.MaxValue) return false;
            var offers = Roll(nodeId, state.refreshes + 1);
            if (!RunState.Apply(Array.Empty<EffectRecord>(), price)) return false;
            state.refreshes++;
            state.offers = offers;
            state.bought.Clear();
            return true;
        }
        private static bool IsActive(int nodeId) => nodeId >= 0 && MapRun.Nodes[nodeId].Definition.type == NodeKind.Shop && !MapRun.IsCleared(nodeId);
        private static ShopGoodRecord[] Roll(int nodeId, int refreshes)
        {
            var config = Config(nodeId);
            var result = config.goods.Where(good => good.isFixed).OrderBy(good => good.order).ToList();
            var pool = config.goods.Where(good => !good.isFixed).ToList();
            var random = new Random(unchecked(MapRun.Seed * 397 ^ nodeId * 7919 ^ refreshes * 104729));
            while (result.Count < config.offerCount && pool.Count > 0)
            {
                var good = RarityRules.Pick(pool, entry => RarityRules.Of(GameContent.Effects.Get(entry.effectId)), entry => entry.weight, random, Progression.Level);
                result.Add(good);
                pool.Remove(good);
            }
            return result.ToArray();
        }
    }
}
