using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElementalTactics.Editor
{
    [InitializeOnLoad]
    public static class ElementBadgePreview
    {
        const string Output = "D:/自走棋/outputs/element-badges-20261105/";
        static ElementBadgePreview() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (!File.Exists(Output + "unity-request.txt") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Output + "unity-request.txt");
            Render();
        }
        [MenuItem("Tools/Elemental Tactics/Preview Element Badges")]
        public static void Render()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var oldFont = PrototypeUI.font;
            RenderTexture rt = null; Texture2D image = null;
            try
            {
                PrototypeUI.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/ElementalTactics/Fonts/NotoSansCJKsc-Regular.otf");
                var go = new GameObject("Element Preview", typeof(RectTransform), typeof(Canvas));
                SceneManager.MoveGameObjectToScene(go, scene);
                var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
                ((RectTransform)go.transform).sizeDelta = new Vector2(1000, 1000);
                var cameraObject = new GameObject("Preview Camera"); SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var cam = cameraObject.AddComponent<Camera>(); cam.scene = scene; cam.orthographic = true; cam.orthographicSize = 500;
                cam.transform.position = new Vector3(0, 0, -10); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = PrototypeUI.Hex("101E30");
                canvas.worldCamera = cam;
                for (int side = 0; side < 2; side++)
                    for (int i = 0; i < 3; i++)
                    {
                        var element = (Element)i;
                        var record = (side == 0 ? GameContent.Characters.rows : GameContent.Enemies.rows).First(u => u.element == element);
                        var definition = record.Create();
                        var position = new Vector2((i - 1) * 290, side == 0 ? 230 : -230);
                        var card = PrototypeUI.CreateFormationCard(go.transform, record.id, position, definition, 220);
                        // Exercise a legacy serialized label and a live attribute change.
                        card.rarityLabel = PrototypeUI.Label("Rarity", card.transform, "普通", Vector2.zero, new Vector2(60, 25), 16, Color.white);
                        PrototypeUI.Panel("Rarity Plate", card.transform, Vector2.zero, new Vector2(65, 28), Color.black);
                        definition.element = (Element)((i + 1) % 3); card.Show(definition);
                        definition.element = element;
                        var fighter = new Fighter(definition, side == 1, i);
                        fighter.shield = definition.maxHealth / 3;
                        card.Show(fighter.definition, fighter);
                        PrototypeUI.SetPortraitCard(card, 145);
                        if (card.rarityLabel.gameObject.activeSelf || card.transform.Find("Rarity Plate").gameObject.activeSelf
                            || card.elementBadge.CurrentElement != element || card.elementBadge.raycastTarget
                            || card.elementBadge.rectTransform.sizeDelta.x > card.Rect.rect.width)
                            throw new InvalidOperationException("旧卡兼容或属性图标刷新失败：" + record.id);
                        PrototypeUI.SetPortraitCard(card, 220);
                        PrototypeUI.Label("Caption", go.transform, (side == 0 ? "角色 · " : "敌人 · ") + definition.ElementName,
                            position + new Vector2(0, -195), new Vector2(270, 36), 23, Color.white);
                    }
                // Build both real codex categories independently in the isolated preview scene.
                for (int tab = 0; tab < 2; tab++)
                {
                    var root = PrototypeUI.Rect("Codex QA " + tab, go.transform, new Vector2(4000 + tab * 1000, 0), new Vector2(900,1600));
                    var codex = root.gameObject.AddComponent<CodexUI>(); codex.Initialize(root, PrototypeUI.font); codex.Open(tab);
                    var badges = root.GetComponentsInChildren<ElementBadge>();
                    int expected = tab == 0 ? GameContent.Characters.rows.Length : GameContent.Enemies.rows.Length;
                    if (badges.Length != expected || root.GetComponentsInChildren<Text>().Any(t => t.gameObject.name == "Rarity"))
                        throw new InvalidOperationException("图鉴立绘属性角标未完整更新");
                }
                rt = new RenderTexture(1400, 1400, 24); cam.targetTexture = rt;
                Canvas.ForceUpdateCanvases(); cam.Render(); var old = RenderTexture.active; RenderTexture.active = rt;
                try
                {
                    image = new Texture2D(1400, 1400, TextureFormat.RGB24, false);
                    image.ReadPixels(new Rect(0,0,1400,1400),0,0); image.Apply();
                    File.WriteAllBytes(Output + "element-preview.png", image.EncodeToPNG());
                }
                finally { RenderTexture.active = old; cam.targetTexture = null; }
                File.WriteAllText(Output + "unity-result.json", "{\"status\":\"PASS\",\"elements\":3,\"previewCards\":6,\"codexUnits\":" + (GameContent.Characters.rows.Length + GameContent.Enemies.rows.Length) + "}");
            }
            catch (Exception e) { File.WriteAllText(Output + "unity-error.txt", e.ToString()); Debug.LogException(e); }
            finally
            {
                PrototypeUI.font = oldFont; if (image != null) UnityEngine.Object.DestroyImmediate(image);
                if (rt != null) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
