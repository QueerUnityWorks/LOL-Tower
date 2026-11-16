using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics.Editor
{
    public static class ResourceArtworkValidation
    {
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        [MenuItem("Tools/Elemental Tactics/Validate Resource Images")]
        public static void ValidateMenu() { Debug.Log(Validate()); }

        public static string Validate()
        {
            var units = GameContent.Characters.rows.Concat(GameContent.Enemies.rows).ToArray();
            var paths = units.Select(unit => unit.imagePath).Concat(GameContent.Items.rows.Select(item => item.imagePath)).ToArray();
            Check(paths.Length == 80 && paths.Distinct().Count() == 80, "80 条图片路径应独立配置");
            foreach (var path in paths) Check(Resources.Load<Texture2D>(path) != null, "图片无法加载：" + path);
            foreach (var unit in units) Check(unit.Create().Clone().imagePath == unit.imagePath, "战斗单位复制丢失图片路径：" + unit.id);
            foreach (var entry in GameContent.Events.rows) Check(Resources.Load<Texture2D>(entry.cg) != null, "事件图片回归：" + entry.id);

            var scene = EditorSceneManager.NewPreviewScene();
            var previousFont = PrototypeUI.font;
            try
            {
                var root = new GameObject("Resource Image Verification", typeof(RectTransform), typeof(Canvas));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                var rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(900, 1600);
                PrototypeUI.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/ElementalTactics/Fonts/NotoSansCJKsc-Regular.otf");
                var card = PrototypeUI.CreateCard(root.transform, "Image Check Card", Vector2.zero, units[0].Create());
                foreach (var unit in units)
                {
                    card.Show(unit.Create());
                    var cardTexture = Resources.Load<Texture2D>("CardPortraits/" + unit.imagePath) ?? Resources.Load<Texture2D>(unit.imagePath);
                    Check(card.portrait.texture == cardTexture, "战斗卡片未切换图片：" + unit.id);
                }
                for (int tab = 0; tab < 3; tab++)
                {
                    var host = new GameObject("Codex " + tab, typeof(RectTransform));
                    host.transform.SetParent(root.transform, false);
                    var codex = host.AddComponent<CodexUI>();
                    codex.Initialize(host.GetComponent<RectTransform>(), PrototypeUI.font);
                    codex.Open(tab);
                    int expected = tab == 0 ? GameContent.Characters.rows.Length : tab == 1 ? GameContent.Enemies.rows.Length : GameContent.Items.rows.Length;
                    var art = host.GetComponentsInChildren<ResourceArtwork>();
                    Check(codex.VisibleCount == expected && art.Length == expected && art.All(image => image.texture != null), "图鉴图片不完整：" + tab);
                    var method = typeof(CodexUI).GetMethod(tab == 2 ? "ShowItem" : "ShowUnit", BindingFlags.Instance | BindingFlags.NonPublic);
                    object record = tab == 0 ? (object)GameContent.Characters.rows[0] : tab == 1 ? (object)GameContent.Enemies.rows[0] : GameContent.Items.rows[0];
                    method.Invoke(codex, new[] { record });
                    Check(host.GetComponentsInChildren<ResourceArtwork>().All(image => image.texture != null), "图鉴详情图片加载失败");
                    UnityEngine.Object.DestroyImmediate(host);
                }
                var imageTest = ResourceArtwork.Create(root.transform, "Sizing", paths[0], Vector2.zero, new Vector2(200, 100));
                var populate = typeof(ResourceArtwork).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null);
                using (var vh = new VertexHelper())
                {
                    populate.Invoke(imageTest, new object[] { vh });
                    var a = new UIVertex(); var b = new UIVertex();
                    vh.PopulateUIVertex(ref a, 0); vh.PopulateUIVertex(ref b, 2);
                    Check(Mathf.Approximately(b.position.x - a.position.x, 100) && Mathf.Approximately(b.position.y - a.position.y, 100), "图片被拉伸");
                    imageTest.cropToFill = true;
                    populate.Invoke(imageTest, new object[] { vh });
                    vh.PopulateUIVertex(ref a, 0); vh.PopulateUIVertex(ref b, 2);
                    Check(Mathf.Approximately(b.position.x - a.position.x, 200) && Mathf.Approximately(b.uv0.y - a.uv0.y, .5f), "事件裁切错误");
                    imageTest.rectTransform.sizeDelta = new Vector2(100, 200);
                    populate.Invoke(imageTest, new object[] { vh });
                    vh.PopulateUIVertex(ref a, 0); vh.PopulateUIVertex(ref b, 2);
                    Check(Mathf.Approximately(b.uv0.x - a.uv0.x, .5f), "横向裁切错误");
                    imageTest.SetPath(null);
                    populate.Invoke(imageTest, new object[] { vh });
                    Check(vh.currentVertCount == 0 && imageTest.texture == null, "空路径仍显示旧图");
                }
                var validatePath = typeof(GameWorkbookImporter).GetMethod("ValidateArt", BindingFlags.Static | BindingFlags.NonPublic);
                foreach (var invalid in new[] { "Characters/HERO_001.png", "Resources/Characters/HERO_001", "Characters/DOES_NOT_EXIST", "../Characters/HERO_001", "" })
                {
                    bool rejected = false;
                    try { validatePath.Invoke(null, new object[] { invalid, "校验用例" }); }
                    catch (TargetInvocationException error) { rejected = error.InnerException is InvalidOperationException; }
                    Check(rejected, "错误资源路径未被阻止：" + invalid);
                }
            }
            finally { PrototypeUI.font = previousFont; EditorSceneManager.ClosePreviewScene(scene); }
            return "资源图片验证通过：80 张图片、47 个战斗单位、3 类图鉴与详情、15 张事件引用、比例适配、空路径和错误路径校验。";
        }
    }
}
