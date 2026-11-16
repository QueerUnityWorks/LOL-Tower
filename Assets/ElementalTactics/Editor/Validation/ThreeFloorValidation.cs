using System;
using System.IO;
using System.Linq;
using ElementalTactics;
using ElementalTactics.Editor;
using UnityEditor;
using UnityEngine;

public static class ThreeFloorValidation
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
    }
    public static void Run()
    {
        try
        {
            GameWorkbookImporter.Import();
            string result = RunChecks();
            AssetDatabase.SaveAssets();
            File.WriteAllText("three-floor-validation.txt", result);
            Debug.Log("THREE_FLOOR_VALIDATION_PASS: " + checks);
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            File.WriteAllText("three-floor-validation.txt", exception.ToString());
            EditorApplication.Exit(1);
        }
    }
    public static string RunChecks()
    {
        checks = 0;
        try
        {
            for (int seed = 0; seed < 500; seed++) ValidateMap(seed);
            ValidateProgression();
            ValidateOverrides();
            ValidateEndings();
            ValidateSingleEntryEvents();
            ValidateContractBoundaries();
            ValidateBossMechanics();
            return "PASS: " + checks + " three-floor checks\n" + string.Join("\n", PrototypeValidation.RunModelChecks());
        }
        finally { MapRun.Reset(); }
    }
    private static void ValidateMap(int seed)
    {
        MapRun.ResetWithSeed(seed);
        Check(MapRun.Nodes.Count == 67, "67 nodes including hidden floor");
        foreach (string eventId in new[] { "EVENT_010", "EVENT_011" })
            Check(MapRun.Nodes.Count(node => node.Definition.contentId == eventId) >= 1, "Unlock event appears at least once, duplicates allowed");
        var reachable = new System.Collections.Generic.HashSet<int>(MapRun.Nodes.Where(node => node.Floor == 1 && node.Row == 0).Select(node => node.Id));
        foreach (var node in MapRun.Nodes)
        {
            Check(node.Id >= 0 && MapRun.Nodes[node.Id] == node, "Stable node ID");
            Check(reachable.Contains(node.Id), "Unreachable node");
            foreach (int target in node.Next)
            {
                var next = MapRun.Nodes[target];
                Check(next.Floor == node.Floor ? next.Row == node.Row + 1 : node.Definition.isFinal && next.Floor == node.Floor + 1 && next.Row == 0, "Invalid edge");
                Check(node.Definition.type != NodeKind.Shop || next.Definition.type != NodeKind.Shop, "Consecutive shops");
                reachable.Add(target);
            }
            if (node.Floor > 1) Check(!MapRun.CanEnter(node.Id), "Future floor unlocked");
        }
        foreach (var floor in GameContent.Floors.rows)
        {
            var nodes = MapRun.Nodes.Where(node => node.Floor == floor.floor).ToArray();
            int shopCount = nodes.Count(node => node.Definition.type == NodeKind.Shop);
            Check(shopCount >= 1 && shopCount <= 2, "Shop bounds");
            Check(nodes.Where(node => node.Definition.type == NodeKind.Shop).All(node => node.Row >= 2 && node.Row <= Math.Min(4, floor.rows - 2)), "Shop rows");
            Check(nodes.Single(node => node.Definition.isFinal).Definition.contentId == (floor.floor == 1 ? "BATTLE_003" : floor.floor == 2 ? "BATTLE_011" : floor.floor == 3 ? "BATTLE_012" : "BATTLE_028"), "Floor boss");
            Check(nodes.Length == (floor.rows - 1) * 3 + 1, "Configured row count");
            for (int row = 0; row < floor.rows - 1; row++) Check(nodes.Any(node => node.Row == row && node.Definition.type == NodeKind.Battle), "Guaranteed battle");
        }
        string signature = string.Join(";", MapRun.Nodes.Select(node => node.Definition.id + ":" + string.Join(",", node.Next)));
        MapRun.ResetWithSeed(seed);
        Check(signature == string.Join(";", MapRun.Nodes.Select(node => node.Definition.id + ":" + string.Join(",", node.Next))), "Seed determinism");
    }
    private static void ValidateProgression()
    {
        MapRun.ResetWithSeed(42);
        RunState.Ensure();
        string instance = RunState.Characters[0].InstanceId;
        var formation = RunState.RestoreFormation();
        formation.Place(0, 5);
        RunState.SaveFormation(formation);
        InventoryState.TryAdd("ITEM_001", "测试道具", "测试", 1);
        Check(CombatPower.Difficulty(2499.99).id == "DIFF_001" && CombatPower.Difficulty(2500).id == "DIFF_002", "2500 threshold");
        Check(CombatPower.Difficulty(5499.99).id == "DIFF_002" && CombatPower.Difficulty(5500).id == "DIFF_003", "5500 threshold");
        while (!MapRun.Finished)
        {
            var next = MapRun.Nodes.First(node => MapRun.CanEnter(node.Id));
            int completed = MapRun.ClearedCount;
            Check(MapRun.TryEnter(next.Id), "Enter node");
            Check(!MapRun.TryEnter(next.Id), "Double enter blocked");
            if (next.Definition.type == NodeKind.Battle)
            {
                var reward = RewardFlow.Victory(next.Id);
                int experience = Progression.Experience, level = Progression.Level;
                Check(ReferenceEquals(reward, RewardFlow.Victory(next.Id)), "Reward cached");
                Check(experience == Progression.Experience && level == Progression.Level, "Experience once");
                Check(Math.Abs(reward.EnemyPower - CombatPower.Evaluate(GameContent.EnemyLineup(next))) < 1e-8, "Scaled enemy power");
                var battle = GameContent.Battles.Get(next.Definition.contentId);
                var difficulty = CombatPower.Difficulty(reward.EnemyPower);
                string itemPool = string.IsNullOrEmpty(battle.itemPoolId) ? difficulty.itemPoolId : battle.itemPoolId;
                foreach (var candidate in reward.Candidates.Where(effect => effect.type == EffectKind.GiveItem))
                    Check(GameContent.Pools.rows.Any(entry => entry.poolId == itemPool && entry.targetId == candidate.targetId), "Difficulty pool");
                Check(reward.Candidates.Select(effect => effect.type + effect.targetId).Distinct().Count() == reward.Candidates.Length, "Unique candidates");
                Check(!RewardFlow.Claim(new[] { 0, 0 }), "Duplicate selection blocked");
                Check(!RewardFlow.Claim(new[] { -1 }), "Invalid selection blocked");
                if (reward.ChooseCount < reward.Candidates.Length)
                    Check(!RewardFlow.Claim(Enumerable.Range(0, reward.ChooseCount + 1)), "Over-selection blocked");
                int gold = RunState.Gold;
                int award = reward.Gold;
                Check(RewardFlow.Claim(Enumerable.Range(0, reward.ChooseCount)), "Claim allowed count");
                Check(RunState.Gold == gold + award && !RewardFlow.Claim(Array.Empty<int>()), "Gold and claim once");
            }
            else Check(MapRun.Complete(next.Id), "Complete non-battle");
            Check(MapRun.ClearedCount == completed + 1, "Progress once");
            Check(RunState.Characters[0].InstanceId == instance && RunState.RestoreFormation()[5] == 0, "Formation persists");
            Check(InventoryState.Items.Any(item => item.Id == "ITEM_001"), "Inventory persists");
            if (next.Definition.isFinal) Check(MapRun.CurrentFloor == Math.Min(3, next.Floor + 1), "Floor advances after boss");
            if (MapRun.ClearedCount < 21) Check(!MapRun.Finished, "No early completion");
        }
        Check(MapRun.ClearedCount == 21 && MapRun.Current == 56, "Three-floor finish");
        MapRun.ResetWithSeed(42);
        Check(RewardFlow.Pending == null && MapRun.CurrentFloor == 1 && InventoryState.Items.Count == 0 && RunState.Gold == 0, "New run resets");
    }
    private static void ValidateOverrides()
    {
        MapRun.ResetWithSeed(7);
        var node = MapRun.Nodes[0];
        var battle = GameContent.Battles.Get(node.Definition.contentId);
        int items = battle.itemCandidates, characters = battle.characterCandidates, choices = battle.chooseCount;
        string pool = battle.itemPoolId;
        try
        {
            battle.itemPoolId = "POOL_DIFF_HIGH";
            battle.itemCandidates = 3;
            battle.characterCandidates = 0;
            battle.chooseCount = 2;
            Check(MapRun.TryEnter(0), "Override entry");
            var reward = RewardFlow.Victory(0);
            Check(reward.Candidates.Length == 3 && reward.ChooseCount == 2, "Explicit counts");
            Check(reward.Candidates.All(effect => GameContent.Items.Get(effect.targetId).rarity != Rarity.Common), "Explicit high pool");
            Check(RewardFlow.Claim(new[] { 0, 1 }), "Override claim");
            MapRun.ResetWithSeed(7);
            battle.itemCandidates = battle.characterCandidates = battle.chooseCount = 0;
            Check(MapRun.TryEnter(0), "Zero entry");
            reward = RewardFlow.Victory(0);
            Check(reward.Candidates.Length == 0 && reward.ChooseCount == 0 && RewardFlow.Claim(Array.Empty<int>()), "Zero overrides");
        }
        finally
        {
            battle.itemCandidates = items;
            battle.characterCandidates = characters;
            battle.chooseCount = choices;
            battle.itemPoolId = pool;
            MapRun.ResetWithSeed(7);
        }
    }

    private static void AddItem(string id)
    {
        var item = GameContent.Items.Get(id);
        Check(InventoryState.TryAdd(id, item.name, item.description), "Add quest item");
    }
    private static void ValidateEndings()
    {
        for (int ending = 0; ending < 3; ending++)
        {
            MapRun.ResetWithSeed(120 + ending);
            int victoriesWithContract = 0;
            while (!MapRun.Finished)
            {
                var available = MapRun.Nodes.Where(node => MapRun.CanEnter(node.Id)).ToArray();
                Check(available.Length > 0, "Ending route has next node");
                var forced = MapRun.Nodes.FirstOrDefault(node => node.Floor == MapRun.CurrentFloor && node.Definition.guaranteed);
                var next = available.FirstOrDefault(node => forced != null && node.Row <= forced.Row && node.Lane == forced.Lane)
                    ?? available.FirstOrDefault(node => node.Definition.type == NodeKind.Battle) ?? available[0];
                Check(MapRun.TryEnter(next.Id), "Ending node entry");
                if (next.Definition.type == NodeKind.Battle)
                {
                    int before = EndingRun.Quantity("ITEM_033");
                    bool contract = EndingRun.Quantity("ITEM_032") > 0;
                    var reward = RewardFlow.Victory(next.Id);
                    if (contract) victoriesWithContract++;
                    Check(EndingRun.Quantity("ITEM_033") == (contract ? Math.Min(3, before + 1) : before), "One guaranteed fragment per win, capped at three");
                    Check(reward.GuaranteedDrops.Length == (contract && before < 3 ? 1 : 0), "Guaranteed reward presented");
                    Check(ReferenceEquals(reward, RewardFlow.Victory(next.Id)) && EndingRun.Quantity("ITEM_033") == (contract ? Math.Min(3, victoriesWithContract) : 0), "Repeated victory does not duplicate fragments");
                    Check(RewardFlow.Claim(Array.Empty<int>()), "Skip selectable rewards retains guaranteed fragment");
                }
                else if (next.Definition.contentId == "EVENT_010")
                    Check(RunState.ChooseEvent(ending == 0 ? "OPTION_10_1" : "OPTION_10_2"), "Positive or cursed branch");
                else if (next.Definition.contentId == "EVENT_011")
                    Check(RunState.ChooseEvent(ending == 2 ? "OPTION_11_1" : "OPTION_11_2"), "Contract choice");
                else Check(MapRun.Complete(next.Id), "Complete ordinary encounter");
            }
            Check(EndingRun.Completed.priority == ending, "Correct ending priority");
            Check(MapRun.ClearedCount == (ending == 2 ? 25 : 21), "Ending route length");
            Check(MapRun.CurrentFloor == (ending == 2 ? 4 : 3), "Ending floor");
            Check(!MapRun.Nodes.Any(node => MapRun.CanEnter(node.Id)), "Finished route locked");
        }
        MapRun.ResetWithSeed(14);
        AddItem("ITEM_032");
        Check(MapRun.TryEnter(0), "Failed battle entry");
        MapRun.LeaveEncounter();
        Check(EndingRun.Quantity("ITEM_033") == 0, "Leaving without victory grants nothing");
        Check(MapRun.TryEnter(0), "Retry battle");
        RewardFlow.Victory(0);
        Check(EndingRun.Quantity("ITEM_033") == 1, "Retry grants exactly one");
        Check(RewardFlow.Claim(Array.Empty<int>()), "Claim retry");
        Check(!EndingRun.AwardVictory(0, EndingRun.VictoryDrops(0)), "Award idempotent");
        InventoryState.TryRemove("ITEM_032");
        Check(EndingRun.VictoryDrops(1).Length == 0, "Contract must be held");
        MapRun.ResetWithSeed(15);
        AddItem("ITEM_031");
        var weak = CharacterDefinition.Create("test", Role.Warrior, Element.Fire);
        weak.maxHealth = 30;
        Check(ItemBonuses.Apply(weak).maxHealth == 1, "Curse health floor");
        AddItem("ITEM_019");
        Check(ItemBonuses.Apply(weak).maxHealth == 100, "Curse applies after positive health bonuses");
        MapRun.CanEnter(0);
        Check(MapRun.Nodes.Single(node => node.Floor == 3 && node.Definition.isFinal).Definition.contentId == "BATTLE_027", "Cursed boss replacement");
        MapRun.ResetWithSeed(15);
        Check(EndingRun.Completed == null && EndingRun.Quantity("ITEM_033") == 0 && EndingRun.Active.priority == 0, "Ending reset");
    }
    private static void WinAndClaim(int id)
    {
        Check(MapRun.TryEnter(id), "Enter test battle");
        Check(RewardFlow.Victory(id) != null, "Victory creates reward");
        Check(RewardFlow.Claim(Array.Empty<int>()), "Claim test reward");
    }
    private static void ValidateSingleEntryEvents()
    {
        int duplicateSeeds = 0;
        for (int seed = 0; seed < 200; seed++)
        {
            MapRun.ResetWithSeed(seed);
            var special = MapRun.Nodes.First(node => node.Definition.contentId == "EVENT_010");
            if (MapRun.Nodes.Count(node => node.Definition.contentId == "EVENT_010") > 1) duplicateSeeds++;
            WinAndClaim(MapRun.Nodes.First(node => node.Floor == 1 && node.Row == 0 && node.Lane == special.Lane).Id);
            Check(!MapRun.HasEnteredEvent("EVENT_010"), "Generation does not consume special event");
            var repeat = MapRun.Nodes.First(node => node.Floor == 2 && node.Row == 2 && node.Lane == special.Lane);
            typeof(MapNode).GetProperty("Definition").SetValue(repeat, special.Definition);
            int[] links = repeat.Next.ToArray();
            Check(MapRun.TryEnter(special.Id), "First special event entry succeeds");
            Check(MapRun.HasEnteredEvent("EVENT_010"), "Entry consumes event before choice");
            Check(repeat.Definition.type == NodeKind.Event && repeat.Definition.contentId != "EVENT_010", "Future duplicate replaced with ordinary event");
            Check(repeat.Next.SequenceEqual(links), "Replacement preserves route edges");
            Check(MapRun.Nodes.Count(node => !MapRun.IsCleared(node.Id) && node.Id != special.Id && node.Definition.contentId == "EVENT_010") == 0,
                "No unvisited duplicate remains");
            if (seed % 2 == 0)
            {
                Check(RunState.ChooseEvent("OPTION_10_2"), "Curse choice succeeds once");
                Check(!RunState.ChooseEvent("OPTION_10_2") && EndingRun.Quantity("ITEM_031") == 1, "Duplicate click cannot grant another key");
                Check(!MapRun.TryEnter(special.Id), "Cannot re-enter completed special event");
            }
            else
            {
                MapRun.LeaveEncounter();
                Check(EndingRun.Quantity("ITEM_031") == 0, "Abandoned event does not grant item");
                Check(special.Definition.contentId != "EVENT_010" && MapRun.CanEnter(special.Id), "Abandoned special becomes ordinary encounter without blocking path");
            }
            MapRun.ResetWithSeed(seed);
            Check(!MapRun.HasEnteredEvent("EVENT_010"), "New run resets event visit history");
        }
        Check(duplicateSeeds > 0, "Special events may be generated more than once before entry");
    }
    private static void ValidateContractBoundaries()
    {
        MapRun.ResetWithSeed(31);
        Check(MapRun.TryEnter(0), "Enter before contract");
        AddItem("ITEM_032");
        Check(RewardFlow.Victory(0).GuaranteedDrops.Length == 0 && EndingRun.Quantity("ITEM_033") == 0,
            "Acquiring contract during a battle does not count that battle");
        Check(RewardFlow.Claim(Array.Empty<int>()), "Claim no-contract battle");
        MapRun.ResetWithSeed(32);
        AddItem("ITEM_032");
        Check(MapRun.TryEnter(0), "Enter with contract");
        InventoryState.TryRemove("ITEM_032");
        Check(RewardFlow.Victory(0).GuaranteedDrops.Length == 0, "Removed contract does not earn fragments");
        Check(RewardFlow.Claim(Array.Empty<int>()), "Claim removed-contract battle");
        MapRun.ResetWithSeed(33);
        Check(!EndingRun.AwardVictory(0, new[] { new EffectRecord { type = EffectKind.GiveItem, targetId = "ITEM_033", amount = 3 } }),
            "Cannot award arbitrary fragments outside a battle");
        AddItem("ITEM_031");
        AddItem("ITEM_032");
        AddItem("ITEM_033");
        AddItem("ITEM_033");
        Check(!EndingRun.ExtraFloorUnlocked(4), "Two fragments do not unlock floor four");
        MapRun.CanEnter(0);
        Check(MapRun.Nodes.Single(node => node.Floor == 3 && node.Definition.isFinal).Definition.contentId == "BATTLE_027", "Curse selects second boss");
        AddItem("ITEM_033");
        MapRun.CanEnter(0);
        Check(EndingRun.ExtraFloorUnlocked(4), "Third fragment unlocks extra floor");
        Check(MapRun.Nodes.Single(node => node.Floor == 3 && node.Definition.isFinal).Definition.contentId == "BATTLE_027", "Third route preserves held curse boss on floor three");
        Check(MapRun.Nodes.Where(node => node.Floor == 4).All(node => !MapRun.CanEnter(node.Id)), "Unlock does not bypass route progression");
        InventoryState.TryRemove("ITEM_032");
        Check(!EndingRun.ExtraFloorUnlocked(4), "Fragments without contract do not unlock floor four");
        InventoryState.TryRemove("ITEM_031");
        MapRun.CanEnter(0);
        Check(MapRun.Nodes.Single(node => node.Floor == 3 && node.Definition.isFinal).Definition.contentId == "BATTLE_012", "Removing curse restores default boss before entry");
        // The third fragment can arrive from the third-floor boss itself.
        MapRun.ResetWithSeed(34);
        while (MapRun.ClearedCount < 20)
        {
            var next = MapRun.Nodes.First(node => MapRun.CanEnter(node.Id));
            if (next.Definition.type == NodeKind.Battle) WinAndClaim(next.Id);
            else { Check(MapRun.TryEnter(next.Id), "Enter boundary encounter"); Check(MapRun.Complete(next.Id), "Complete boundary encounter"); }
        }
        AddItem("ITEM_032"); AddItem("ITEM_033"); AddItem("ITEM_033");
        var boss = MapRun.Nodes.Single(node => node.Floor == 3 && node.Definition.isFinal);
        Check(MapRun.TryEnter(boss.Id), "Enter floor-three boss with two fragments");
        string bossId = boss.Definition.id;
        var reward = RewardFlow.Victory(boss.Id);
        Check(reward.GuaranteedDrops.Length == 1 && EndingRun.ExtraFloorUnlocked(4), "Boss victory grants third fragment");
        Check(boss.Definition.id == bossId, "Boss identity frozen through victory and claim");
        Check(RewardFlow.Claim(Array.Empty<int>()) && !MapRun.Finished && MapRun.CurrentFloor == 4, "Final-battle threshold opens fourth floor instead of ending run");
        Check(MapRun.Nodes.Count(node => node.Floor == 4 && !node.Definition.isFinal) == 9, "Fourth floor has three rows of three ordinary nodes");
        Check(MapRun.Nodes.Count(node => node.Floor == 4 && node.Definition.isFinal) == 1, "Fourth floor has one separate boss");
        MapRun.ResetWithSeed(35);
    }
    private static Fighter Boss(BossMechanic mechanic, int power, int interval = 2)
    {
        var definition = CharacterDefinition.Create("test", Role.Warrior, Element.Fire);
        definition.maxHealth = 1000;
        definition.attack = 100;
        definition.bossMechanic = mechanic;
        definition.bossPower = power;
        definition.bossInterval = interval;
        return new Fighter(definition, true, 0);
    }
    private static void ValidateBossMechanics()
    {
        var victim = Boss(BossMechanic.None, 0);
        victim.definition.defense = 0;
        var wolf = Boss(BossMechanic.PackLeader, 15);
        var ally = Boss(BossMechanic.None, 0);
        var allies = new System.Collections.Generic.List<Fighter> { wolf, ally };
        var opponents = new System.Collections.Generic.List<Fighter> { victim };
        CombatRules.Resolve(wolf, allies, opponents, false);
        Check(victim.health == 885, "Pack leader living ally bonus");
        ally.health = 0;
        victim.health = 1000;
        CombatRules.Resolve(wolf, allies, opponents, false);
        Check(victim.health == 900, "Pack bonus removed when ally dies");
        var devour = Boss(BossMechanic.Devour, 12);
        devour.health = 500;
        allies = new System.Collections.Generic.List<Fighter> { devour, ally };
        CombatRules.Resolve(devour, allies, opponents, false);
        Check(devour.health == 620, "Devour heals fallen ally once");
        CombatRules.Resolve(devour, allies, opponents, false);
        Check(devour.health == 620, "No repeated devour");
        var barrier = Boss(BossMechanic.BarrierCycle, 100);
        allies = new System.Collections.Generic.List<Fighter> { barrier };
        CombatRules.Resolve(barrier, allies, opponents, false);
        Check(barrier.shield == 0, "Barrier waits interval");
        CombatRules.Resolve(barrier, allies, opponents, false);
        Check(barrier.shield == 100, "Barrier every two actions");
        var drain = Boss(BossMechanic.SoulDrain, 35);
        drain.definition.skillKind = SkillKind.AllEnemies;
        drain.definition.skillPower = 10;
        victim.health = 1000;
        victim.energy = 2;
        CombatRules.Resolve(drain, new System.Collections.Generic.List<Fighter> { drain }, opponents, true);
        Check(victim.energy == 1 && drain.shield == 35, "Soul drain converts energy to shield");
        var storm = Boss(BossMechanic.StormCycle, 150);
        var source = storm.definition;
        var clone = new Fighter(source, true, 0);
        allies = new System.Collections.Generic.List<Fighter> { clone };
        victim.health = 1000;
        CombatRules.Resolve(clone, allies, opponents, false);
        CombatRules.Resolve(clone, allies, opponents, false);
        Check(clone.definition.element == Element.Water && source.element == Element.Fire && clone.shield == 150, "Storm rotates local definition only");
        clone.shield = 0;
        clone.health = 501;
        victim.definition.attack = 10;
        CombatRules.Resolve(victim, opponents, allies, false);
        Check(clone.phaseTriggered && clone.energy == 3 && clone.shield == 150, "Storm half health phase");
        clone.shield = 0;
        clone.energy = 0;
        CombatRules.Resolve(victim, opponents, allies, false);
        Check(clone.energy == 0 && clone.shield == 0, "Storm half health phase once");
    }
}
