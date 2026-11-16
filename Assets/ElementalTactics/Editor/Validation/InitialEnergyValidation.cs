using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ElementalTactics.Editor
{
    [InitializeOnLoad]
    public static class InitialEnergyValidation
    {
        const string Output = "D:/自走棋/outputs/initial-energy-20261105/";
        static InitialEnergyValidation() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (!File.Exists(Output + "unity-request.txt") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Output + "unity-request.txt");
            var scene = EditorSceneManager.NewPreviewScene();
            var oldFont = PrototypeUI.font;
            try
            {
                string model = PrototypeValidation.RunModelChecks();
                PrototypeUI.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/ElementalTactics/Fonts/NotoSansCJKsc-Regular.otf");
                var root = new GameObject("Initial Energy QA", typeof(RectTransform), typeof(Canvas));
                SceneManager.MoveGameObjectToScene(root, scene);
                var definition = CharacterDefinition.Create("Energy QA", Role.Guardian, Element.Water);
                var card = PrototypeUI.CreateFormationCard(root.transform, "Energy QA", Vector2.zero, definition, 220);
                CheckCard(card, 2);
                foreach (bool enemy in new[] { false, true })
                {
                    var actor = new Fighter(definition, enemy, 0);
                    var opponent = new Fighter(definition, !enemy, 0);
                    var allies = new List<Fighter> { actor };
                    var enemies = new List<Fighter> { opponent };
                    card.Show(actor.definition, actor); CheckCard(card, 2);
                    CombatRules.Resolve(actor, allies, enemies, false);
                    card.Show(actor.definition, actor); CheckCard(card, 3);
                    CombatRules.Resolve(actor, allies, enemies, true);
                    card.Show(actor.definition, actor); CheckCard(card, 0);
                    card.Show(actor.definition, actor); CheckCard(card, 0);
                    var nextBattle = new Fighter(definition, enemy, 0);
                    card.Show(nextBattle.definition, nextBattle); CheckCard(card, 2);
                }
                File.WriteAllText(Output + "unity-result.json", "{\"status\":\"PASS\",\"initialEnergy\":2,\"skillCost\":3,\"sidesChecked\":2,\"modelBattles\":40}");
                Debug.Log("Initial energy QA: " + model);
            }
            catch (Exception exception)
            {
                File.WriteAllText(Output + "unity-error.txt", exception.ToString());
                Debug.LogException(exception);
            }
            finally { PrototypeUI.font = oldFont; EditorSceneManager.ClosePreviewScene(scene); }
        }
        static void CheckCard(CardView card, int expected)
        {
            if (!card.energyText.text.StartsWith("技力 " + expected + "/3")
                || Mathf.Abs(card.energyFill.rectTransform.localScale.x - expected / 3f) > 0.001f)
                throw new InvalidOperationException("Card energy display mismatch: " + card.energyText.text);
        }
    }
}
