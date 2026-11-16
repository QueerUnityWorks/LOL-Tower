using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ElementalTactics.Editor
{
    public static class PrototypeValidation
    {
        public static string RunModelChecks()
        {
            CheckSkillCountdown();
            CheckTargetPreviews();
            var roster = CombatRules.AvailableCharacters();
            Require(roster.Count >= 6, "Candidate roster must support a six character team");
            var formation = new FormationState(roster.Count, 1);
            Require(formation.Count == 1 && formation[0] == 0 && formation[1] == -1, "Start with one adventurer and five open positions");
            Require(formation.Place(2, 1) && formation.Count == 2, "Place a candidate in an open position");
            Require(formation.Place(3, 0) && formation[0] == 3 && formation[1] == 2, "Replacing a position returns the old adventurer to reserve");
            Require(formation.Place(2, 0) && formation[0] == 2 && formation[1] == 3, "Moving an active adventurer swaps positions");
            formation.Remove(1);
            Require(formation.Count == 1 && formation[1] == -1, "Removing an active adventurer opens a position");
            var lineup = formation.Lineup(roster);
            Require(lineup.Count == 6 && lineup[0] == roster[2] && lineup[1] == null, "Battle lineup preserves empty positions");
            formation.Reset();
            Require(formation.Count == 1 && formation[0] == 0, "Reset restores the initial solo formation");
            Require(CombatRules.ElementMultiplier(Element.Fire, Element.Grass) == 1.25f, "Fire must counter grass");
            Require(CombatRules.ElementMultiplier(Element.Grass, Element.Water) == 1.25f, "Grass must counter water");
            Require(CombatRules.ElementMultiplier(Element.Water, Element.Fire) == 1.25f, "Water must counter fire");
            Require(CombatRules.ElementMultiplier(Element.Grass, Element.Fire) == 0.8f, "Reverse counter");
            Require(CombatRules.ElementMultiplier(Element.Fire, Element.Fire) == 1f, "Same element");
            var actor = Make(Role.Warrior, 1, false);
            var front = Make(Role.Guardian, 0, true);
            var alignedBack = Make(Role.Mage, 4, true);
            var opponents = new List<Fighter> { front, alignedBack };
            Require(CombatRules.FindTarget(actor, opponents) == front, "Front must protect aligned rear");
            front.health = 0;
            Require(CombatRules.FindTarget(actor, opponents) == alignedBack, "Dead front must not block");
            front.health = front.definition.maxHealth;
            front.shield = 1000;
            CombatRules.Resolve(actor, new List<Fighter> { actor }, opponents, false);
            Require(front.health == front.definition.maxHealth && front.shield < 1000, "Shield must absorb damage");
            Require(actor.energy == 3, "First basic must fill the initial 2/3 energy bar");
            var mage = Make(Role.Mage, 0, false);
            opponents = Enumerable.Range(0, 6).Select(slot => Make(Role.Warrior, slot, true)).ToList();
            var effects = CombatRules.Resolve(mage, new List<Fighter> { mage }, opponents, true);
            Require(effects.Count == 3 && effects.All(effect => effect.target.slot < 3), "Mage must hit front row");
            var ranger = Make(Role.Ranger, 1, false);
            effects = CombatRules.Resolve(ranger, new List<Fighter> { ranger }, opponents, true);
            Require(effects.Count == 2 && effects.All(effect => effect.target.slot % 3 == 1), "Ranger must hit a column");
            var guardian = Make(Role.Guardian, 0, false);
            actor.health = 20;
            effects = CombatRules.Resolve(guardian, new List<Fighter> { guardian, actor }, opponents, true);
            Require(actor.health == 120 && actor.shield == 90 && guardian.shield == 90, "Guardian must heal and protect ally");
            actor.health = 0;
            CombatRules.Resolve(guardian, new List<Fighter> { guardian, actor }, opponents, true);
            Require(actor.health == 0, "Healing must not resurrect");
            actor.health = 100;
            actor.energy = 3;
            effects = CombatRules.Resolve(actor, new List<Fighter> { actor }, opponents, true);
            Require(actor.health > 100 && actor.energy == 0, "Warrior must drain and consume energy");
            int victories = 0;
            for (int seed = 0; seed < 40; seed++)
            {
                var random = new System.Random(seed);
                var players = CombatRules.DefaultTeam(false).Select((definition, slot) => new Fighter(definition, false, slot)).ToList();
                var enemies = CombatRules.DefaultTeam(true).Select((definition, slot) => new Fighter(definition, true, slot)).ToList();
                var fighters = players.Concat(enemies).OrderBy(fighter => random.Next()).ToList();
                for (int index = 0; index < fighters.Count; index++) fighters[index].tieOrder = index;
                for (int round = 1; round <= CombatRules.MaxRounds && players.Any(fighter => fighter.Alive) && enemies.Any(fighter => fighter.Alive); round++)
                {
                    foreach (var fighter in fighters.OrderByDescending(unit => unit.definition.speed).ThenBy(unit => unit.tieOrder))
                    {
                        if (fighter.Alive) CombatRules.Resolve(fighter, fighter.enemy ? enemies : players, fighter.enemy ? players : enemies, fighter.energy >= 3);
                        Require(fighters.All(unit => unit.health >= 0 && unit.health <= unit.definition.maxHealth && unit.energy >= 0 && unit.energy <= 3 && unit.shield >= 0), "Combat state invariant");
                    }
                }
                Require(!players.Any(fighter => fighter.Alive) || !enemies.Any(fighter => fighter.Alive), "Default battle should finish before cap");
                if (players.Any(fighter => fighter.Alive)) victories++;
            }
            return "PASS: skill countdown across two charge/cast cycles, counters, targeting, shields, energy, all four skills, no resurrection; 40 complete battles; player wins " + victories + "/40.";
        }

        private static void CheckSkillCountdown()
        {
            var actor = Make(Role.Guardian, 0, false);
            var opponent = Make(Role.Guardian, 0, true);
            var allies = new List<Fighter> { actor };
            var opponents = new List<Fighter> { opponent };
            Require(actor.energy == 2 && opponent.energy == 2, "Both sides must start each battle at 2/3 energy");
            int expectedCastRound = 2;
            for (int round = 1; round <= 8; round++)
            {
                Require(round + CombatRules.RoundsUntilSkill(actor.energy, false) == expectedCastRound,
                    "Countdown before acting must predict the actual cast round");
                bool skill = actor.energy >= CombatRules.SkillEnergyCost;
                Require(skill == (round == expectedCastRound), "A full bar must cast on this turn");
                CombatRules.Resolve(actor, allies, opponents, skill);
                if (skill) expectedCastRound += 4;
                Require(round + CombatRules.RoundsUntilSkill(actor.energy, true) == expectedCastRound,
                    "Countdown after acting must account for the spent turn and reset after casting");
            }
        }

        private static void CheckTargetPreviews()
        {
            foreach (Role role in Enum.GetValues(typeof(Role)))
            {
                var actor = Make(role, 1, false);
                var wounded = Make(Role.Warrior, 0, false);
                wounded.health = 1;
                var allies = new List<Fighter> { actor, wounded };
                var enemies = Enumerable.Range(0, 6).Select(slot => Make(Role.Guardian, slot, true)).ToList();
                enemies[0].health = 0;
                var targets = CombatRules.FindTargets(actor, allies, enemies, true);
                Require(targets.All(target => target.Alive) && actor.energy == CombatRules.InitialEnergy && wounded.health == 1,
                    "Target preview excludes dead units and must not mutate combat state");
                var effects = CombatRules.Resolve(actor, allies, enemies, true);
                var actualTargets = effects.Where(effect => role != Role.Warrior || effect.target != actor)
                    .Select(effect => effect.target).Distinct().ToList();
                Require(targets.SequenceEqual(actualTargets), "Every skill preview must match actual affected targets");
            }
        }

        private static Fighter Make(Role role, int slot, bool enemy)
        {
            return new Fighter(CharacterDefinition.Create("Test", role, Element.Fire), enemy, slot);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
