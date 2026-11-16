using System;
using System.IO;
using System.Linq;
using ElementalTactics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SerpentineTemplateValidation
{
    private const string Output = "D:/自走棋/outputs/map-s-template-applied/";
    private static int checks;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new InvalidOperationException(message);
    }

    [MenuItem("Tools/Elemental Tactics/Validate Serpentine Template")]
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        var saved = AdventureRun.Capture();
        var font = PrototypeUI.font;
        checks = 0;
        try
        {
            PrototypeUI.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/ElementalTactics/Fonts/NotoSansCJKsc-Regular.otf");
            Check(MapLayout.TemplateCount == 4, "Expected four templates in the production pool.");
            for (int template = 0; template < MapLayout.TemplateCount; template++) Capture(template);
            string result = "PASS: " + checks + " Unity checks; four production templates, 180-unit S-template spacing, 34 real route graphics, safe curves, visible arrows, working node selection, zoom and minimap.\n";
            File.WriteAllText(Output + "unity-validation.txt", result);
            Debug.Log("SERPENTINE_TEMPLATE_UNITY_PASS: " + result);
        }
        catch (Exception error)
        {
            File.WriteAllText(Output + "unity-validation.txt", error.ToString());
            throw;
        }
        finally
        {
            AdventureRun.Restore(saved);
            PrototypeUI.font = font;
        }
    }

    private static void Capture(int template)
    {
        int seed = Enumerable.Range(0, 10000).First(value => MapLayout.SelectTemplate(value, 1) == template);
        MapRun.ResetWithSeed(seed);
        var nodes = MapRun.Nodes.Where(node => node.Floor == 1).ToArray();
        var scene = EditorSceneManager.NewPreviewScene();
        RenderTexture target = null;
        Camera camera = null;
        try
        {
            var host = new GameObject("Production Map Validation", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(host, scene);
            var canvas = host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = (RectTransform)canvas.transform;
            canvasRect.sizeDelta = new Vector2(900, 1600);
            var root = PrototypeUI.Rect("Content", canvasRect, Vector2.zero, new Vector2(900, 1600));
            var controller = host.AddComponent<MapController>();
            controller.Build(root);
            var world = root.Find("Map Viewport/Map World");
            var graphics = world.GetComponentsInChildren<MapRouteGraphic>();
            Check(graphics.Length == nodes.Sum(node => node.Next.Count(id => MapRun.Nodes[id].Floor == 1)), "Visible route graph differs from playable graph.");
            if (template == 3)
            {
                var plan = MapRoutePlan.Create(MapLayout.GetTemplate(3, 7), 4, .22f);
                Check(graphics.Length == 34 && nodes.Length == 31, "S-template graph incomplete.");
                foreach (var edge in plan)
                {
                    var graphic = graphics.Single(route => route.name == "Route " + nodes[edge.From].Id + " to " + nodes[edge.To].Id);
                    Check(graphic.UseVisibleArrow && Mathf.Abs(graphic.Bend - edge.Bend) < .0001f, "Renderer does not use safe planned curves.");
                    Check(Mathf.Abs(Vector2.Distance(graphic.From, graphic.To) - 180) < .01f, "S-template spacing differs from approved preview.");
                }
                var mini = (RectTransform)root.Find("Map Viewport/Map Minimap/Minimap World");
                var center = new Vector2((nodes.Min(node => node.X) + nodes.Max(node => node.X)) / 2,
                    (nodes.Min(node => node.Y) + nodes.Max(node => node.Y)) / 2);
                Check((mini.anchoredPosition + center * mini.localScale.x).sqrMagnitude < .001f, "S-template minimap is not centered.");
            }
            else Check(graphics.All(route => !route.UseVisibleArrow), "Existing template style changed.");
            Check(nodes.Count(node => MapRun.CanEnter(node.Id)) == 5, "Expected five selectable entrances.");

            var cameraHost = new GameObject("Production Map Camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraHost, scene);
            camera = cameraHost.GetComponent<Camera>();
            camera.scene = scene;
            camera.orthographic = true;
            camera.orthographicSize = 800;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = MapPresentation.Navy;
            canvas.worldCamera = camera;
            Canvas.ForceUpdateCanvases();
            target = new RenderTexture(1080, 1920, 24);
            camera.targetTexture = target;
            camera.Render();
            var previous = RenderTexture.active;
            Texture2D image = null;
            try
            {
                RenderTexture.active = target;
                image = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
                image.Apply();
                File.WriteAllBytes(Output + "applied-template-" + (template + 1) + ".png", image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
            }
            var viewport = root.Find("Map Viewport").GetComponent<MapViewport>();
            float before = viewport.Zoom;
            root.Find("Map Viewport/Map Zoom In").GetComponent<Button>().onClick.Invoke();
            Check(viewport.Zoom > before, "Zoom-in button does not zoom.");
            root.Find("Map Viewport/Map Fit").GetComponent<Button>().onClick.Invoke();
            Check(Mathf.Abs(viewport.Zoom - before) < .001f, "Fit does not restore default zoom.");
            controller.SelectNode(nodes[0].Id);
            Check(controller.SelectedNodeId == nodes[0].Id, "Node selection failed.");
        }
        finally
        {
            if (camera != null) camera.targetTexture = null;
            if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
