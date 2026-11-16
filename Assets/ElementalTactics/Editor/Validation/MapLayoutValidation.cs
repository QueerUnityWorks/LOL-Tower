using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElementalTactics.Editor
{
    public static class MapLayoutValidation
    {
        private const string Output = "D:/自走棋/outputs/map-v7-applied/";
        private static int checks;
        private static void Require(bool condition, string message)
        { checks++; if (!condition) throw new InvalidOperationException(message); }
        [MenuItem("Tools/Elemental Tactics/Preview Approved Map")]
        public static void Run()
        {
            Directory.CreateDirectory(Output);
            var saved = AdventureRun.Capture();
            var font = PrototypeUI.font;
            var previousEvents = EventSystem.current;
            checks = 0;
            try
            {
                for (int seed = 0; seed < 100; seed++) CheckModel(seed);
                CheckTemplateSelection();
                CheckPersistence();
                for (int template = 0; template < MapLayout.TemplateCount; template++)
                    foreach (int rows in new[] { 4, 7 }) CheckGeometry(template, rows);
                PrototypeUI.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/ElementalTactics/Fonts/NotoSansCJKsc-Regular.otf");
                for (int floor = 1; floor <= 3; floor++) CaptureFloor(floor);
                File.WriteAllLines(Output + "preview-templates.txt", Enumerable.Range(1, 3).Select(floor => MapLayout.TemplateName(MapLayout.SelectTemplate(42, floor)) + " · 第" + floor + "层"));
                File.WriteAllText(Output + "validation.txt", "PASS: " + checks + " checks; 100 seeds; independent template selection on every floor, repeats allowed, deterministic generation; all three templates on normal and short floors; five entrances; three Boss predecessors; equal steps, spacing and curve lengths; no crossings; persistence and viewport controls; three Unity previews.");
                Debug.Log("MAP_V7_APPLIED_PASS: " + checks);
            }
            catch (Exception error) { File.WriteAllText(Output + "error.txt", error.ToString()); throw; }
            finally
            {
                AdventureRun.Restore(saved); PrototypeUI.font = font;
                if (previousEvents != null && previousEvents.isActiveAndEnabled) EventSystem.current = previousEvents;
            }
        }
        private static void CheckModel(int seed)
        {
            MapRun.ResetWithSeed(seed);
            var nodes = MapRun.Nodes;
            foreach (var floor in GameContent.Floors.rows)
            {
                var local = nodes.Where(node => node.Floor == floor.floor).ToArray();
                var expected = MapLayout.ForFloor(floor.floor, seed);
                Require(local.Length == expected.Length && local.Select((node, index) => node.X == expected[index].X && node.Y == expected[index].Y && node.Bend == expected[index].Bend).All(match => match), "Generated floor uses its randomly selected template");
                var boss = local.Single(node => node.Definition.isFinal);
                Require(local.Count(node => node.Row == 0) == 5, "Five entries per floor");
                Require(local.Count(node => node.Next.Contains(boss.Id)) >= 3, "Three Boss predecessors per floor");
                Require(local.Count(node => node.Definition.type == NodeKind.Shop) >= 1 && local.Count(node => node.Definition.type == NodeKind.Shop) <= floor.maxShops, "Shop bounds");
                for (int row = 0; row < floor.rows - 1; row++) Require(local.Any(node => node.Row == row && node.Definition.type == NodeKind.Battle), "Battle per layer");
                foreach (var guaranteed in GameContent.Nodes.rows.Where(entry => entry.guaranteed && entry.minFloor == floor.floor))
                    Require(local.Any(node => node.Definition.id == guaranteed.id), "Guaranteed event remains present");
                var reached = new HashSet<int>();
                foreach (var entry in local.Where(node => node.Row == 0))
                {
                    Require(!local.Any(node => node.Next.Contains(entry.Id)) && entry.Next.Count == 1, "Independent initial routes");
                    Walk(entry, boss, 0, floor.rows - 1, reached);
                }
                Require(reached.Count == local.Length, "No orphan nodes");
                foreach (var node in local)
                    foreach (int target in node.Next)
                    {
                        var next = nodes[target];
                        Require(next.Floor == node.Floor ? next.Row == node.Row + 1 : node == boss && next.Floor == node.Floor + 1 && next.Row == 0, "Forward edges only");
                        Require(node.Definition.type != NodeKind.Shop || next.Definition.type != NodeKind.Shop, "No adjacent shops");
                    }
            }
            Require(nodes.Where(node => MapRun.CanEnter(node.Id)).Count() == 5, "Only first floor entrances initially available");
        }
        private static void CheckTemplateSelection()
        {
            foreach (var floor in GameContent.Floors.rows)
            {
                var selected = Enumerable.Range(0, 100).Select(seed => MapLayout.SelectTemplate(seed, floor.floor)).ToArray();
                Require(selected.Distinct().Count() == MapLayout.TemplateCount, "Every floor can select all three templates");
            }
            Require(Enumerable.Range(0, 100).Any(seed => MapLayout.SelectTemplate(seed, 1) == MapLayout.SelectTemplate(seed, 2)), "Consecutive floors may repeat a template");
            Require(Enumerable.Range(0, 100).Any(seed => MapLayout.SelectTemplate(seed, 1) != MapLayout.SelectTemplate(seed, 2)), "Floor selection is independent");
            MapRun.ResetWithSeed(42);
            string signature = string.Join(";", MapRun.Nodes.Select(node => node.Definition.id + ":" + node.X + ":" + node.Y + ":" + string.Join(",", node.Next)));
            MapRun.ResetWithSeed(42);
            Require(signature == string.Join(";", MapRun.Nodes.Select(node => node.Definition.id + ":" + node.X + ":" + node.Y + ":" + string.Join(",", node.Next))), "Same run seed regenerates the same map and contents");
        }
        private static void Walk(MapNode node, MapNode boss, int steps, int expected, HashSet<int> reached)
        {
            Require(steps <= expected, "Acyclic map"); reached.Add(node.Id);
            if (node == boss) { Require(steps == expected, "Every entrance-to-Boss path has equal steps"); return; }
            Require(node.Next.Count > 0, "No dead ends");
            foreach (int target in node.Next) Walk(MapRun.Nodes[target], boss, steps + 1, expected, reached);
        }
        private static void CheckPersistence()
        {
            MapRun.ResetWithSeed(42); RunState.Ensure();
            var snapshot = JsonUtility.FromJson<RunSnapshot>(JsonUtility.ToJson(RunSnapshot.Capture()));
            snapshot.Validate();
            var save = new AdventureSave { active = true, id = "map-layout-validation", startedAt = DateTime.UtcNow.ToString("o"), history = new AdventureRecord[0], run = snapshot };
            AdventureRun.Restore(save);
            Require(MapRun.Nodes.All(node => node.HasLayout && node.X == snapshot.nodes[node.Id].x && node.Y == snapshot.nodes[node.Id].y && node.Bend == snapshot.nodes[node.Id].bend), "Save restores geometry");
            foreach (var node in snapshot.nodes) { node.hasLayout = false; node.x = node.y = 0; node.bend = 0; node.lane %= 3; }
            snapshot.Validate(); AdventureRun.Restore(save);
            Require(MapRun.Nodes.All(node => !node.HasLayout), "Legacy saves retain their existing graph");
        }
        private static float Turn(Vector2 start, Vector2 end, Vector2 point)
            => (end.x - start.x) * (point.y - start.y) - (end.y - start.y) * (point.x - start.x);
        private static Vector2 Point(MapLayoutPoint start, MapLayoutPoint end, float fraction)
        {
            if (fraction <= 0) return new Vector2(start.X, start.Y);
            if (fraction >= 1) return new Vector2(end.X, end.Y);
            var direction = new Vector2(end.X - start.X, end.Y - start.Y);
            return new Vector2(start.X, start.Y) + direction * fraction + new Vector2(direction.y, -direction.x) * (.38f * start.Bend * 3 * fraction * (1 - fraction));
        }
        private static void CheckGeometry(int template, int rows)
        {
            var layout = MapLayout.GetTemplate(template, rows);
            var curves = new List<Vector2[]>();
            foreach (var node in layout)
                foreach (int target in node.Next)
                {
                    var next = layout[target];
                    Require(Math.Abs(Vector2.Distance(new Vector2(node.X, node.Y), new Vector2(next.X, next.Y)) - MapLayout.Spacing) < .001f, "Uniform node spacing");
                    var samples = Enumerable.Range(0, 193).Select(index => Point(node, next, index / 192f)).ToArray();
                    float length = Enumerable.Range(1, 192).Sum(index => Vector2.Distance(samples[index - 1], samples[index]));
                    Require(Math.Abs(length - 149.52688f) < .01f, "Uniform curve lengths"); curves.Add(samples);
                }
            for (int first = 0; first < curves.Count; first++)
                for (int second = first + 1; second < curves.Count; second++)
                    for (int segment = 1; segment < curves[first].Length; segment++)
                        for (int other = 1; other < curves[second].Length; other++)
                        {
                            var start = curves[first][segment - 1]; var end = curves[first][segment];
                            var otherStart = curves[second][other - 1]; var otherEnd = curves[second][other];
                            if (Mathf.Max(start.x, end.x) < Mathf.Min(otherStart.x, otherEnd.x) || Mathf.Min(start.x, end.x) > Mathf.Max(otherStart.x, otherEnd.x)
                                || Mathf.Max(start.y, end.y) < Mathf.Min(otherStart.y, otherEnd.y) || Mathf.Min(start.y, end.y) > Mathf.Max(otherStart.y, otherEnd.y)) continue;
                            Require(!(Turn(start, end, otherStart) * Turn(start, end, otherEnd) < -.00000001f && Turn(otherStart, otherEnd, start) * Turn(otherStart, otherEnd, end) < -.00000001f), "Curves must not cross in template " + template + " with " + rows + " rows");
                        }
        }
        private static void CaptureFloor(int floor)
        {
            MapRun.ResetWithSeed(42);
            if (floor > 1)
            {
                var previous = MapRun.Nodes.Single(node => node.Floor == floor - 1 && node.Definition.isFinal);
                typeof(MapRun).GetProperty("Current").SetValue(null, previous.Id);
            }
            var scene = EditorSceneManager.NewPreviewScene();
            RenderTexture target = null;
            Camera camera = null;
            try
            {
                var canvasObject = new GameObject("Map Preview", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
                SceneManager.MoveGameObjectToScene(canvasObject, scene);
                var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
                var root = (RectTransform)canvasObject.transform; root.sizeDelta = new Vector2(900, 1600);
                var content = PrototypeUI.Rect("Portrait Content", root, Vector2.zero, new Vector2(900, 1600));
                var controller = canvasObject.AddComponent<MapController>(); controller.Build(content);
                var cameraObject = new GameObject("Map Preview Camera", typeof(Camera)); SceneManager.MoveGameObjectToScene(cameraObject, scene);
                camera = cameraObject.GetComponent<Camera>(); camera.scene = scene; camera.orthographic = true; camera.orthographicSize = 800;
                camera.transform.position = new Vector3(0, 0, -10); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = MapPresentation.Navy;
                canvas.worldCamera = camera;
                var eventsObject = new GameObject("Preview Events", typeof(EventSystem)); SceneManager.MoveGameObjectToScene(eventsObject, scene);
                var events = eventsObject.GetComponent<EventSystem>();
                Canvas.ForceUpdateCanvases();
                var viewport = content.GetComponentInChildren<MapViewport>();
                float initial = viewport.Zoom;
                content.Find("Map Viewport/Map Zoom In").GetComponent<Button>().onClick.Invoke(); Require(viewport.Zoom > initial, "Zoom in button");
                content.Find("Map Viewport/Map Zoom Out").GetComponent<Button>().onClick.Invoke(); Require(Math.Abs(viewport.Zoom - initial) < .001f, "Zoom out button");
                viewport.OnBeginDrag(new PointerEventData(events));
                var before = viewport.World.anchoredPosition;
                viewport.OnDrag(new PointerEventData(events) { position = new Vector2(600, 900), delta = new Vector2(60, 20), pressPosition = new Vector2(540, 880) });
                viewport.OnEndDrag(new PointerEventData(events));
                Require(Vector2.Distance(before, viewport.World.anchoredPosition) > 1, "Map drag");
                viewport.Fit();
                Require(content.GetComponentsInChildren<Text>(true).All(label => !label.text.Contains("第 1 步") && !label.text.Contains("第 5 步")), "No step captions");
                Require(content.GetComponentsInChildren<Transform>(true).All(transform => !transform.name.Contains("Dashed") && !transform.name.Contains("Boundary")), "No dashed shape boundary");
                var entry = MapRun.Nodes.First(node => node.Floor == floor && node.Row == 0);
                controller.SelectNode(entry.Id);
                Require(content.Find("Map Details").gameObject.activeSelf, "Node selection opens details");
                content.Find("Map Details").gameObject.SetActive(false);
                target = new RenderTexture(1080, 1920, 24); camera.targetTexture = target;
                Canvas.ForceUpdateCanvases(); camera.Render();
                var previous = RenderTexture.active; RenderTexture.active = target;
                var image = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0); image.Apply();
                File.WriteAllBytes(Output + "map-floor-" + floor + ".png", image.EncodeToPNG());
                RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(image);
            }
            finally
            {
                if (camera != null) camera.targetTexture = null;
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
