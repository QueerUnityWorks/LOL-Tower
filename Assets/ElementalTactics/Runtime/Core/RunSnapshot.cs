using System;
using System.Collections.Generic;
using System.Linq;

namespace ElementalTactics
{
    // Explicit DTOs: JsonUtility does not serialize properties, dictionaries or sets.
    [Serializable] public sealed class OwnedSnapshot { public string id, instance; }
    [Serializable] public sealed class ItemSnapshot { public string id; public int quantity; }
    [Serializable] public sealed class NodeSnapshot { public int id, row, lane, floor, bend; public float x, y; public bool hasLayout; public string definition; public int[] next; }
    [Serializable] public sealed class StoreSnapshot { public int node, refreshes; public string[] offers, boughtIds; public int[] boughtCounts; }
    [Serializable] public sealed class RewardSnapshot
    {
        public int node, gold, experience, choices;
        public double power;
        public string name;
        public EffectRecord[] candidates, drops;
    }
    [Serializable] public sealed class RunSnapshot
    {
        public int seed, current, pending, completedAtEntry, gold, level, experience, battleNode;
        public int lives;
        public bool hasLives;
        public int[] cleared, rewarded;
        public string[] enteredEvents, contracts, formation;
        public string ending;
        public OwnedSnapshot[] characters;
        public ItemSnapshot[] items;
        public NodeSnapshot[] nodes;
        public StoreSnapshot[] stores;
        public bool hasReward, hasFormation;
        public RewardSnapshot reward;
        public static RunSnapshot Capture()
        {
            var s = new RunSnapshot();
            MapRun.Capture(s); RunState.Capture(s); EndingRun.Capture(s); ShopRun.Capture(s); RewardFlow.Capture(s);
            s.level = Progression.Level; s.experience = Progression.Experience;
            s.items = InventoryState.Items.Select(i => new ItemSnapshot { id = i.Id, quantity = i.Quantity }).ToArray();
            return s;
        }
        private static void Require(bool condition) { if (!condition) throw new InvalidOperationException("冒险存档内容不完整或与当前配置不匹配"); }
        public void Validate()
        {
            Require(nodes != null && nodes.Length > 0 && nodes.Length <= 300 && cleared != null && enteredEvents != null
                && characters != null && characters.Length > 0 && items != null && stores != null && rewarded != null && contracts != null);
            Require(current >= -1 && current < nodes.Length && pending >= -1 && pending < nodes.Length && gold >= 0 && experience >= 0);
            Require(!hasLives || lives >= 0 && lives <= RunState.InitialLives);
            Require(GameContent.Levels.rows.Any(l => l.level == level && (l.experienceToNext == 0 ? experience == 0 : experience < l.experienceToNext)));
            for (int i = 0; i < nodes.Length; i++)
            {
                var n = nodes[i]; Require(n != null && n.id == i && n.floor >= 1 && n.floor <= 4 && n.row >= 0 && n.lane >= 0 && n.lane < MapLayout.EntryCount && n.next != null);
                Require(!n.hasLayout || !float.IsNaN(n.x) && !float.IsInfinity(n.x) && !float.IsNaN(n.y) && !float.IsInfinity(n.y) && Math.Abs(n.x) <= 2000 && Math.Abs(n.y) <= 2000 && (n.bend == -1 || n.bend == 1));
                GameContent.Nodes.Get(n.definition); Require(n.next.All(id => id >= 0 && id < nodes.Length));
            }
            Require(cleared.Distinct().Count() == cleared.Length && cleared.All(id => id >= 0 && id < nodes.Length)
                && (current == -1 || cleared.Contains(current)) && !cleared.Contains(pending));
            Require(characters.All(c => c != null && !string.IsNullOrEmpty(c.instance)) && characters.Select(c => c.instance).Distinct().Count() == characters.Length);
            foreach (var c in characters) GameContent.Characters.Get(c.id);
            Require(!hasFormation || formation != null && formation.Length == 6 && formation.All(id => string.IsNullOrEmpty(id) || characters.Any(c => c.instance == id)));
            Require(items.All(i => i != null && i.quantity > 0) && items.Select(i => i.id).Distinct().Count() == items.Length);
            foreach (var i in items) GameContent.Items.Get(i.id);
            foreach (var id in enteredEvents) GameContent.Events.Get(id);
            foreach (var id in contracts) GameContent.Endings.Get(id);
            if (!string.IsNullOrEmpty(ending)) GameContent.Endings.Get(ending);
            Require(battleNode >= -1 && battleNode < nodes.Length && rewarded.All(id => id >= 0 && id < nodes.Length));
            Require(stores.All(s => s != null) && stores.Select(s => s.node).Distinct().Count() == stores.Length);
            foreach (var s in stores)
            {
                Require(s.node >= 0 && s.node < nodes.Length && s.refreshes >= 0 && s.offers != null && s.boughtIds != null && s.boughtCounts != null
                    && s.boughtIds.Length == s.boughtCounts.Length);
                var node = GameContent.Nodes.Get(nodes[s.node].definition); Require(node.type == NodeKind.Shop);
                var goods = GameContent.Shops.Get(node.contentId).goods;
                Require(s.offers.Distinct().Count() == s.offers.Length && s.offers.All(id => goods.Any(g => g.id == id))
                    && s.boughtIds.Distinct().Count() == s.boughtIds.Length);
                for (int i = 0; i < s.boughtIds.Length; i++) Require(s.offers.Contains(s.boughtIds[i]) && s.boughtCounts[i] >= 0
                    && s.boughtCounts[i] <= goods.First(g => g.id == s.boughtIds[i]).stock);
            }
            if (hasReward)
            {
                Require(reward != null && reward.node == pending && pending >= 0 && GameContent.Nodes.Get(nodes[pending].definition).type == NodeKind.Battle
                    && rewarded.Contains(pending) && reward.gold >= 0 && reward.candidates != null && reward.drops != null
                    && reward.choices >= 0 && reward.choices <= reward.candidates.Length);
                foreach (var e in reward.candidates.Concat(reward.drops))
                {
                    Require(e != null && e.amount > 0 && (e.type == EffectKind.GiveItem || e.type == EffectKind.GiveCharacter));
                    if (e.type == EffectKind.GiveItem) GameContent.Items.Get(e.targetId); else GameContent.Characters.Get(e.targetId);
                }
            }
        }
        internal void Restore()
        {
            MapRun.Restore(this); RunState.Restore(this); EndingRun.Restore(this); ShopRun.Restore(this); RewardFlow.Restore(this);
            Progression.Restore(this);
            foreach (var item in items) { var config = GameContent.Items.Get(item.id); InventoryState.TryAdd(item.id, config.name, config.description, item.quantity); }
        }
    }
    public static partial class MapRun
    {
        internal static void Capture(RunSnapshot s)
        {
            s.seed = Seed; s.current = Current; s.pending = Pending; s.completedAtEntry = CompletedAtEntry;
            s.cleared = cleared.OrderBy(i => i).ToArray(); s.enteredEvents = enteredEvents.OrderBy(i => i).ToArray();
            s.nodes = Nodes.Select(n => new NodeSnapshot { id = n.Id, row = n.Row, lane = n.Lane, floor = n.Floor, definition = n.Definition.id, next = n.Next.ToArray(), x = n.X, y = n.Y, bend = n.Bend, hasLayout = n.HasLayout }).ToArray();
        }
        internal static void Restore(RunSnapshot s)
        {
            Seed = s.seed; Current = s.current; Pending = s.pending; CompletedAtEntry = s.completedAtEntry;
            cleared.Clear(); cleared.UnionWith(s.cleared); enteredEvents.Clear(); enteredEvents.UnionWith(s.enteredEvents);
            nodes = s.nodes.Select(n => { var node = new MapNode(n.id, n.row, n.lane, GameContent.Nodes.Get(n.definition), n.floor, n.x, n.y, n.bend, n.hasLayout); node.Next.AddRange(n.next); return node; }).ToArray();
        }
    }
    public static partial class RunState
    {
        internal static void Capture(RunSnapshot s)
        {
            Ensure(); s.gold = Gold; s.hasFormation = formationSlots != null; s.formation = formationSlots == null ? null : (string[])formationSlots.Clone();
            s.hasLives = true; s.lives = Lives;
            s.characters = characters.Select(c => new OwnedSnapshot { id = c.Definition.id, instance = c.InstanceId }).ToArray();
        }
        internal static void Restore(RunSnapshot s)
        {
            characters.Clear(); characters.AddRange(s.characters.Select(c => new OwnedCharacter(c.id, c.instance)));
            initialized = true; Gold = s.gold; formationSlots = s.hasFormation ? (string[])s.formation.Clone() : null;
            Lives = s.hasLives ? s.lives : InitialLives;
        }
    }
    public static partial class EndingRun
    {
        internal static void Capture(RunSnapshot s) { s.rewarded = rewardedBattles.OrderBy(i => i).ToArray(); s.contracts = battleContracts.OrderBy(i => i).ToArray(); s.battleNode = battleNodeId; s.ending = Completed == null ? null : Completed.id; }
        internal static void Restore(RunSnapshot s) { rewardedBattles.Clear(); rewardedBattles.UnionWith(s.rewarded); battleContracts.Clear(); battleContracts.UnionWith(s.contracts); battleNodeId = s.battleNode; Completed = string.IsNullOrEmpty(s.ending) ? null : GameContent.Endings.Get(s.ending); }
    }
    public static partial class ShopRun
    {
        internal static void Capture(RunSnapshot s) { s.stores = stores.OrderBy(p => p.Key).Select(p => new StoreSnapshot { node = p.Key, refreshes = p.Value.refreshes, offers = p.Value.offers.Select(g => g.id).ToArray(), boughtIds = p.Value.bought.Keys.ToArray(), boughtCounts = p.Value.bought.Values.ToArray() }).ToArray(); }
        internal static void Restore(RunSnapshot s)
        {
            stores.Clear();
            foreach (var entry in s.stores) { var state = new StockState { refreshes = entry.refreshes, offers = entry.offers.Select(id => Config(entry.node).goods.First(g => g.id == id)).ToArray() }; for (int i = 0; i < entry.boughtIds.Length; i++) state.bought.Add(entry.boughtIds[i], entry.boughtCounts[i]); stores.Add(entry.node, state); }
        }
    }
    public static partial class Progression { internal static void Restore(RunSnapshot s) { Level = s.level; Experience = s.experience; } }
    public static partial class RewardFlow
    {
        internal static void Capture(RunSnapshot s) { s.hasReward = Pending != null; if (Pending != null) s.reward = new RewardSnapshot { node = Pending.NodeId, gold = Pending.Gold, experience = Pending.Experience, choices = Pending.ChooseCount, power = Pending.EnemyPower, name = Pending.DifficultyName, candidates = Pending.Candidates, drops = Pending.GuaranteedDrops }; }
        internal static void Restore(RunSnapshot s) { var r = s.hasReward ? s.reward : null; Pending = r == null ? null : new PendingReward { NodeId = r.node, Gold = r.gold, Experience = r.experience, ChooseCount = r.choices, EnemyPower = r.power, DifficultyName = r.name, Candidates = r.candidates, GuaranteedDrops = r.drops }; }
    }
}
