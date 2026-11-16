using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElementalTactics.Editor
{
    [InitializeOnLoad]
    public static class StageShieldValidation
    {
        const string Output = "D:/自走棋/outputs/stage-population-20261105/";
        static int checks;
        static StageShieldValidation() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (!File.Exists(Output + "unity-request.txt") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Output + "unity-request.txt");
            Run();
        }
        static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); checks++; }
        static void Reject(Action action, string message)
        { bool rejected = false; try { action(); } catch (InvalidOperationException) { rejected = true; } Check(rejected, message); }
        [MenuItem("Tools/Elemental Tactics/Validate Stage Portrait and Shield")]
        public static void Run()
        {
            try
            {
                checks = 0;
                HandbookSyncImporter.Import();
                HandbookSyncImporter.ValidateImported();
                var balance = HandbookBalanceConfig.Current;
                foreach (var battle in GameContent.Battles.rows) StageRules.ValidatePopulation(battle, GameContent.Nodes.rows, GameContent.Floors.rows, id => balance.Unit(id)?.quality);
                var basic = new BattleRecord { id = "TEST", slot1 = "N" };
                var nodes = new[] { new NodeRecord { contentId = "TEST", type = NodeKind.Battle, minFloor = 1, maxFloor = 1, maxRow = 3 } };
                Reject(() => StageRules.ValidatePopulation(basic, nodes, GameContent.Floors.rows, id => "普通"), "普通敌人不足时必须拒绝");
                basic.slot2 = basic.slot3 = "N"; StageRules.ValidatePopulation(basic, nodes, GameContent.Floors.rows, id => "普通");
                basic.slot2 = basic.slot3 = ""; StageRules.ValidatePopulation(basic, nodes, GameContent.Floors.rows, id => "精英");
                StageRules.ValidatePopulation(basic, nodes, GameContent.Floors.rows, id => "Boss");
                Reject(() => StageRules.ValidatePopulation(basic, nodes, GameContent.Floors.rows, id => null), "分类缺失必须拒绝");
                Check(GameContent.Battles.Get("BATTLE_001").Slots.Count(id => !string.IsNullOrEmpty(id)) == 6, "林缘需要六名敌人");
                
                var floors = GameContent.Floors.rows;
                foreach (int row in new[] { 1, 2, 5, 6, 7 })
                {
                    var n = new NodeRecord { type = NodeKind.Battle, contentId = "TEST", minFloor = 1, maxFloor = 1, minRow = row, maxRow = row, isFinal = row == 7 };
                    Check(StageRules.MinimumPopulation(basic, new[] { n }, floors) == Math.Min(6, row), "第一层数量边界错误");
                }
                var later = new NodeRecord { type = NodeKind.Battle, contentId = "TEST", minFloor = 2, maxFloor = 2, minRow = 3, maxRow = 3 };
                Check(StageRules.MinimumPopulation(basic, new[] { later }, floors) == 6, "第二层第三排必须六名");
                Reject(() => StageRules.ValidatePopulation(basic, new[] { later }, floors, id => "普通"), "跨层普通敌人不足必须拒绝");
                basic.slot2 = basic.slot3 = basic.slot4 = basic.slot5 = basic.slot6 = "N";
                StageRules.ValidatePopulation(basic, new[] { later }, floors, id => "普通");
                Check(StageRules.MinimumPopulation(basic, nodes.Concat(new[] { later }), floors) == 6, "共享敌阵应取最晚节点");
                var shorterFloors = new[] { new FloorRecord { floor = 1, rows = 2 }, new FloorRecord { floor = 2, rows = 4 } };
                later.minRow = later.maxRow = 1;
                Check(StageRules.MinimumPopulation(basic, new[] { later }, shorterFloors) == 3, "跨层累计须读取楼层实际排数");
                Check(GameContent.Battles.Get("BATTLE_002").Slots.Count(id => !string.IsNullOrEmpty(id)) == 6, "湿地需要六名");
                Check(GameContent.Battles.Get("BATTLE_013").Slots.Count(id => !string.IsNullOrEmpty(id)) == 2, "林口需要两名");
                int notFirst = 0;
                var portraitMethod = typeof(MapController).GetMethod("PortraitUnit", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                var difficultyField = typeof(AdventureRun).GetField("<Difficulty>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static);
                int originalDifficulty = AdventureRun.Difficulty;
                try
                {
                    foreach (int difficulty in new[] { 1, 10 })
                    {
                        difficultyField.SetValue(null, difficulty);
                        foreach (var record in GameContent.Nodes.rows.Where(n => n.type == NodeKind.Battle))
                            for (int f = record.minFloor; f <= record.maxFloor; f++)
                            {
                                var node = new MapNode(0, 0, 0, record, f);
                                var lineup = GameContent.EnemyLineup(node).Where(u => u != null).ToArray();
                                if (record.contentId == "BATTLE_001" || record.contentId == "BATTLE_002" || record.contentId == "BATTLE_013")
                                {
                                    var before = record.contentId == "BATTLE_002" ? new[] { lineup[0], lineup[2], lineup[4] }
                                        : lineup.Take(lineup.Length / 2).ToArray();
                                    Check(Math.Abs(CombatPower.Evaluate(lineup) / CombatPower.Evaluate(before) - 2) < 0.000001,
                                        "补充敌人后奖励换算比例不能直接沿用：" + record.contentId);
                                }
                                var expected = lineup.OrderByDescending(balance.EvaluatedStrength).First();
                                var actual = GameContent.StrongestEnemy(node);
                                Check(actual.id == expected.id && actual.maxHealth == expected.maxHealth, record.id + "立绘未采用当前最强敌人");
                                if (expected.id != lineup[0].id) notFirst++;
                            }
                    }
                }
                finally { difficultyField.SetValue(null, originalDifficulty); }
                Check(notFirst > 0, "需覆盖最强敌人不在首位的敌阵");
                Check(portraitMethod != null && portraitMethod.IsStatic, "地图未接入立绘选择入口");
                var sample = new MapNode(0, 0, 0, GameContent.Nodes.rows.First(n => n.type == NodeKind.Battle), 1);
                Check(((CharacterDefinition)portraitMethod.Invoke(null, new object[] { sample })).id == GameContent.StrongestEnemy(sample).id, "地图立绘入口不一致");
                ValidateCards();
                File.WriteAllText(Output + "unity-result.json", "{\"status\":\"PASS\",\"checks\":" + checks + ",\"battles\":" + GameContent.Battles.rows.Length + ",\"nonFirstStrongestCases\":" + notFirst + "}");
                Debug.Log("关卡数量、立绘和护盾验证通过：" + checks + "项。");
            }
            catch (Exception e) { File.WriteAllText(Output + "unity-error.txt", e.ToString()); Debug.LogException(e); }
        }
        static void ValidateCards()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var oldFont = PrototypeUI.font;
            RenderTexture rt = null; Texture2D image = null;
            try
            {
                PrototypeUI.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/ElementalTactics/Fonts/NotoSansCJKsc-Regular.otf");
                var go = new GameObject("Shield QA Canvas", typeof(RectTransform), typeof(Canvas));
                SceneManager.MoveGameObjectToScene(go, scene);
                var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
                ((RectTransform)go.transform).sizeDelta = new Vector2(1000, 700);
                var cameraObject = new GameObject("Shield QA Camera"); SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var cam = cameraObject.AddComponent<Camera>(); cam.scene = scene; cam.orthographic = true; cam.orthographicSize = 350;
                cam.transform.position = new Vector3(0, 0, -10); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = PrototypeUI.Hex("101E30");
                canvas.worldCamera = cam;
                var def = GameContent.Characters.rows.First(u => u.name.Contains("亚索")).Create();
                var card = PrototypeUI.CreateFormationCard(go.transform, "Shield Sample", Vector2.zero, def, 250);
                var fighter = new Fighter(def, false, 0);
                fighter.health = def.maxHealth / 4; fighter.shield = def.maxHealth / 2;
                card.Show(def, fighter);
                Check(Mathf.Abs(card.shieldFill.rectTransform.localScale.x - .5f) < .002f, "护盾比例错误");
                Check(card.shieldFill.transform.GetSiblingIndex() > card.healthFill.transform.GetSiblingIndex(), "护盾应覆盖血条");
                Check(!card.shieldFill.raycastTarget, "护盾不应拦截点击");
                PrototypeUI.SetPortraitCard(card, 280);
                Check(card.shieldFill.gameObject.activeSelf && Mathf.Abs(card.shieldFill.rectTransform.localScale.x - .5f) < .002f, "重排不得清空护盾显示");
                Check(card.shieldFill.rectTransform.sizeDelta == card.healthFill.rectTransform.sizeDelta && card.shieldFill.rectTransform.anchoredPosition == card.healthFill.rectTransform.anchoredPosition, "护盾与血条必须重合");
                fighter.health = 0; card.Show(def, fighter); Check(!card.shieldFill.gameObject.activeSelf, "阵亡应隐藏护盾");
                fighter.health = def.maxHealth; fighter.shield = 0; card.Show(def, fighter); Check(!card.shieldFill.gameObject.activeSelf, "零护盾应隐藏");
                fighter.shield = def.maxHealth * 2; card.Show(def, fighter); Check(card.shieldFill.rectTransform.localScale.x == 1, "超额护盾不应溢出");
                UnityEngine.Object.DestroyImmediate(card.shieldFill.gameObject); card.shieldFill = null;
                card.Show(def, fighter); Check(card.shieldFill != null, "旧场景卡片应自动补齐护盾层");
                for (int i = 0; i < 3; i++)
                {
                    var view = i == 0 ? card : PrototypeUI.CreateFormationCard(go.transform, "Shield Case " + i, Vector2.zero, def, 280);
                    view.Rect.anchoredPosition = new Vector2((i - 1) * 320, 0);
                    fighter.health = def.maxHealth * 3 / 4; fighter.shield = i == 0 ? 0 : i == 1 ? def.maxHealth / 2 : def.maxHealth * 2;
                    view.Show(def, fighter);
                    PrototypeUI.Label("Case Label", go.transform, new[] { "无护盾", "50% 护盾覆盖", "超额护盾显示满条" }[i], new Vector2((i - 1) * 320, -255), new Vector2(310, 38), 23, Color.white);
                }
                rt = new RenderTexture(1400, 980, 24); cam.targetTexture = rt;
                Canvas.ForceUpdateCanvases();cam.Render();var old = RenderTexture.active;RenderTexture.active = rt;
                try { image = new Texture2D(1400, 980, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 1400, 980), 0, 0);image.Apply();File.WriteAllBytes(Output + "shield-preview.png",image.EncodeToPNG()); }
                finally { RenderTexture.active = old;cam.targetTexture = null; }
            }
            finally
            {
                PrototypeUI.font = oldFont; if (image != null) UnityEngine.Object.DestroyImmediate(image);
                if (rt != null) {rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
