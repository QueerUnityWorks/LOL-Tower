using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElementalTactics
{
    public sealed class MapController : MonoBehaviour
    {
        private readonly Dictionary<int, RectTransform> rings = new Dictionary<int, RectTransform>();
        private readonly Dictionary<int, Image> selectionFrames = new Dictionary<int, Image>();
        private readonly Dictionary<(int From, int To), MapPlannedRoute> plannedRoutes = new Dictionary<(int From, int To), MapPlannedRoute>();
        private GameObject details;
        private Text title;
        private ResourceArtwork detailArt;
        private Image detailSymbol;
        private Button enter;
        private int selected = -1;
        private bool loading;
        private RectTransform mapRoot;
        private OwnedCharactersUI characters;
        private int viewFloor;
        private MapViewport mapViewport;
        public int SelectedNodeId => selected;

        public void Build(RectTransform root)
        {
            mapRoot = root;
            RunState.Ensure();
            bool resume = AdventureRun.ResumeEncounter;
            AdventureRun.ResumeEncounter = false;
            if (!resume) MapRun.LeaveEncounter();
            viewFloor = MapRun.CurrentFloor;
            var floor = System.Array.Find(GameContent.Floors.rows, entry => entry.floor == viewFloor);
            MapPresentation.BuildHeader(root, floor);
            BuildMap(root);
            string[] icons = { "battle", "event", "shop" }, labels = { "战斗", "事件", "商店" };
            for (int i = 0; i < 3; i++)
            {
                MapPresentation.At("Legend Icon " + i, root, icons[i], 268 + i * 207, 1400, 32, 32, MapPresentation.Muted);
                MapPresentation.Label("Legend Label " + i, root, labels[i], 334 + i * 207, 1400, 82, 28, 19, MapPresentation.Muted);
            }
            BuildDetails(root);
            characters = gameObject.AddComponent<OwnedCharactersUI>();
            characters.Build(root);
            var open = root.Find("Open Characters").GetComponent<Button>();
            MapPresentation.StyleNavigation(open, MapPresentation.Point(200, 1824), MapPresentation.Size(304, 86), "角色", "role");
            open.onClick.RemoveAllListeners();
            open.onClick.AddListener(() => { if (!loading && !InventoryUI.IsOpen) characters.Open(); });
            var home = PrototypeUI.Button("Return Home", root, "返回主城", Vector2.zero, Vector2.one, false);
            MapPresentation.StyleNavigation(home, MapPresentation.Point(870, 1824), MapPresentation.Size(324, 86), "返回主城", "home");
            home.onClick.AddListener(ReturnHome);
            MapPresentation.Label("Ending Progress", root, TutorialRun.Active ? "新手练习 · 战斗 → 事件 → 商店 → Boss" : EndingRun.ProgressLabel, 540, 1762, 980, 26, 18, MapPresentation.Gold, TextAnchor.MiddleCenter);
            MapPresentation.Label("Footer", root, RouteHint(), 540, 1897, 990, 30, 18, MapPresentation.Muted, TextAnchor.MiddleCenter);
            if (RewardFlow.Pending != null) { loading = true; EncounterUI.OpenReward(root); }
            else if (resume && MapRun.Pending >= 0)
            {
                loading = true;
                if (MapRun.Nodes[MapRun.Pending].Definition.type == NodeKind.Event) EncounterUI.OpenEvent(root);
                else if (MapRun.Nodes[MapRun.Pending].Definition.type == NodeKind.Shop) EncounterUI.OpenShop(root);
            }
            else if (!MapRun.Finished)
            {
                var available = MapRun.Nodes.Where(n => n.Floor == viewFloor && MapRun.CanEnter(n.Id)).ToArray();
                // Inspect a sole next node automatically; a branch remains the player's choice.
                if (available.Length == 1 && !TutorialRun.Active) SelectNode(available[0].Id);
            }
        }

        private void BuildMap(RectTransform root)
        {
            var panel = PrototypeUI.Panel("Map Viewport", root, MapPresentation.Point(540, 826), MapPresentation.Size(986, 1084), new Color(.035f, .09f, .14f, .76f));
            panel.raycastTarget = true;
            panel.gameObject.AddComponent<RectMask2D>();
            var world = PrototypeUI.Rect("Map World", panel.transform, Vector2.zero, new Vector2(1060, 1060));
            var nodes = MapRun.Nodes.Where(node => node.Floor == viewFloor).ToArray();
            PrepareRoutePlan(nodes);
            foreach (var node in nodes)
                foreach (int target in node.Next.Where(id => MapRun.Nodes[id].Floor == viewFloor))
                    CreateRoute(world, node, MapRun.Nodes[target], false);
            foreach (var node in nodes) CreateNode(world, node);
            mapViewport = panel.gameObject.AddComponent<MapViewport>();
            var minimum = new Vector2(nodes.Min(node => Position(node).x), nodes.Min(node => Position(node).y));
            var maximum = new Vector2(nodes.Max(node => Position(node).x), nodes.Max(node => Position(node).y));
            var focus = nodes.FirstOrDefault(node => node.Id == MapRun.Current) ?? nodes.First(node => node.Definition.isFinal);
            mapViewport.Initialize(world, new Rect(minimum, maximum - minimum), Position(focus), plannedRoutes.Count > 0 ? 300 : 140);
            BuildMapControls(panel.rectTransform, nodes);
            PrototypeUI.Border(panel.transform, panel.rectTransform.sizeDelta, PrototypeUI.Hex("806D43"));
        }
        private void PrepareRoutePlan(MapNode[] nodes)
        {
            plannedRoutes.Clear();
            var layout = MapLayout.GetTemplate(3, 7);
            if (TutorialRun.Active || nodes.Length != layout.Length) return;
            for (int index = 0; index < nodes.Length; index++)
                if (!nodes[index].HasLayout || Mathf.Abs(nodes[index].X - layout[index].X) > .01f
                    || Mathf.Abs(nodes[index].Y - layout[index].Y) > .01f) return;
            foreach (var route in MapRoutePlan.Create(layout, 4, .22f))
                if (nodes[route.From].Next.Contains(nodes[route.To].Id))
                    plannedRoutes.Add((nodes[route.From].Id, nodes[route.To].Id), route);
        }
        private void CreateRoute(RectTransform parent, MapNode node, MapNode destination, bool miniature)
        {
            bool travelled = MapRun.IsCleared(node.Id) && MapRun.IsCleared(destination.Id);
            bool reachable = node.Id == MapRun.Current && MapRun.CanEnter(destination.Id);
            var route = PrototypeUI.Rect("Route " + node.Id + " to " + destination.Id, parent, Vector2.zero, new Vector2(1060, 1060)).gameObject.AddComponent<MapRouteGraphic>();
            route.From = Position(node); route.To = Position(destination); route.Bend = node.HasLayout ? node.Bend : 0;
            route.ToBoss = destination.Definition.isFinal;
            route.Thickness = miniature ? 8 : reachable || travelled ? 4.5f : 3.5f;
            route.color = reachable ? MapPresentation.Cyan : travelled ? MapPresentation.Gold : PrototypeUI.Hex("63838E");
            if (plannedRoutes.TryGetValue((node.Id, destination.Id), out var planned))
            {
                route.Bend = planned.Bend;
                route.FromBoss = node.Definition.isFinal;
                route.UseVisibleArrow = true;
                route.ArrowScale = miniature ? 1 : 1.7f;
                if (!reachable && !travelled) route.color = planned.Added ? MapPresentation.Gold : PrototypeUI.Hex("7CB6C0");
            }
            route.raycastTarget = false;
        }
        private void BuildMapControls(RectTransform panel, MapNode[] nodes)
        {
            float right = panel.rect.width / 2 - 38, bottom = -panel.rect.height / 2 + 38;
            var label = PrototypeUI.Label("Map Gesture Hint", panel, "单指拖动 · 双指缩放", new Vector2(-panel.rect.width / 2 + 149, panel.rect.height / 2 - 25), new Vector2(272, 30), 17, MapPresentation.Muted);
            var zoomIn = PrototypeUI.Button("Map Zoom In", panel, "+", new Vector2(right, bottom + 210), new Vector2(52, 52), false);
            zoomIn.onClick.AddListener(() => mapViewport.ZoomBy(1.2f));
            var zoomLabel = PrototypeUI.Label("Map Zoom Value", panel, "", new Vector2(right, bottom + 166), new Vector2(65, 30), 15, MapPresentation.Muted);
            var zoomOut = PrototypeUI.Button("Map Zoom Out", panel, "−", new Vector2(right, bottom + 124), new Vector2(52, 52), false);
            zoomOut.onClick.AddListener(() => mapViewport.ZoomBy(1 / 1.2f));
            var fit = PrototypeUI.Button("Map Fit", panel, "全图", new Vector2(right, bottom + 61), new Vector2(58, 44), false);
            fit.GetComponentInChildren<Text>().fontSize = 18; fit.onClick.AddListener(mapViewport.Fit);
            var locate = PrototypeUI.Button("Map Locate", panel, "定位", new Vector2(right, bottom + 7), new Vector2(58, 44), false);
            locate.GetComponentInChildren<Text>().fontSize = 18; locate.onClick.AddListener(mapViewport.Locate);
            var mini = PrototypeUI.Panel("Map Minimap", panel, new Vector2(-panel.rect.width / 2 + 78, bottom + 42), new Vector2(128, 128), PrototypeUI.Hex("0A1B28"));
            mini.gameObject.AddComponent<RectMask2D>();
            var miniWorld = PrototypeUI.Rect("Minimap World", mini.transform, Vector2.zero, new Vector2(1060, 1060));
            float miniScale = .11f;
            Vector2 miniCenter = Vector2.zero, miniExtent = Vector2.one * 1060;
            if (plannedRoutes.Count > 0)
            {
                var minimum = new Vector2(nodes.Min(node => Position(node).x), nodes.Min(node => Position(node).y));
                var maximum = new Vector2(nodes.Max(node => Position(node).x), nodes.Max(node => Position(node).y));
                miniCenter = (minimum + maximum) / 2;
                miniExtent = maximum - minimum + Vector2.one * 140;
                miniScale = Mathf.Min(128 / miniExtent.x, 128 / miniExtent.y);
            }
            miniWorld.localScale = Vector3.one * miniScale;
            miniWorld.anchoredPosition = -miniCenter * miniScale;
            foreach (var node in nodes)
            {
                foreach (int target in node.Next.Where(id => MapRun.Nodes[id].Floor == viewFloor)) CreateRoute(miniWorld, node, MapRun.Nodes[target], true);
                var dot = PrototypeUI.Rect("Minimap Node", miniWorld, Position(node), Vector2.one * (node.Definition.isFinal ? 36 : 22)).gameObject.AddComponent<MapCircle>();
                dot.color = node.Definition.isFinal ? MapPresentation.Gold : MapRun.CanEnter(node.Id) ? MapPresentation.Cyan : MapPresentation.Dim;
                dot.raycastTarget = false;
            }
            var window = PrototypeUI.Panel("Minimap Window", mini.transform, Vector2.zero, Vector2.one * 100, new Color(.4f, .9f, .9f, .05f));
            var outline = window.gameObject.AddComponent<Outline>(); outline.effectColor = MapPresentation.Cyan; outline.effectDistance = Vector2.one;
            mapViewport.SetControls(zoomLabel, window.rectTransform, miniScale, miniCenter, miniExtent);
        }

        private string RouteHint()
        {
            if (TutorialRun.Active) return "新手练习进度会自动保存 · 可随时返回主城继续";
            if (MapRun.Finished) return "远征已完成 · 返回主城开启新的冒险";
            if (viewFloor == MapRun.FloorCount) return "击败失控奥瑞利安·索尔，完成铸星龙王的约定";
            if (viewFloor == MapRun.BaseFloorCount) return "击败守关敌人迎来结局 · 满足契约条件可前往第四层";
            return "沿箭头探索中心首领 · 单指拖动 / 双指缩放";
        }
        private static Vector2 Position(MapNode node)
        {
            if (node.HasLayout) return new Vector2(node.X, node.Y);
            int rows = System.Array.Find(GameContent.Floors.rows, floor => floor.floor == node.Floor).rows;
            return MapPresentation.Point(540 + (node.Lane - 1) * 305, node.Definition.isFinal ? 367 : 1300 - node.Row * (750f / Mathf.Max(1, rows - 2)));
        }
        private static CharacterDefinition PortraitUnit(MapNode node)
        {
            return GameContent.StrongestEnemy(node);
        }
        private void CreateNode(Transform root, MapNode node)
        {
            bool reachable = MapRun.CanEnter(node.Id), cleared = MapRun.IsCleared(node.Id), current = node.Id == MapRun.Current;
            bool boss = node.Definition.isFinal;
            float size = boss ? 106 : 62;
            var rect = PrototypeUI.Rect("Map Node " + node.Id, root, Position(node), new Vector2(size, size));
            rings.Add(node.Id, rect);
            bool bossApproach = node.Next.Any(target => MapRun.Nodes[target].Floor == node.Floor && MapRun.Nodes[target].Definition.isFinal);
            Color edge = current || cleared || boss ? MapPresentation.Gold : reachable ? MapPresentation.Cyan : bossApproach ? MapPresentation.Gold : PrototypeUI.Hex("425E6B");
            if (current || reachable) MapPresentation.Art("Node Glow", rect, "Glow", Vector2.zero, new Vector2(size + 22, size + 22), new Color(edge.r, edge.g, edge.b, .6f));
            var fill = MapPresentation.Art("Node Fill", rect, "HexFill", Vector2.zero, new Vector2(size, size), current ? PrototypeUI.Hex("C7A264") : cleared ? PrototypeUI.Hex("443D2E") : reachable ? PrototypeUI.Hex("113B48") : PrototypeUI.Hex("102633"));
            fill.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.onClick.AddListener(() => SelectNode(node.Id));
            if (boss)
            {
                var mask = MapPresentation.Art("Boss Portrait Mask", rect, "HexFill", Vector2.zero, new Vector2(size - 8, size - 8), Color.white);
                mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                var unit = PortraitUnit(node);
                ResourceArtwork.Create(mask.transform, "Boss Portrait", unit == null ? null : unit.imagePath, Vector2.zero, new Vector2(size - 8, size - 8), true);
                MapPresentation.Art("Boss Crown", rect, "Crown", new Vector2(0, size * .48f), new Vector2(56, 28), MapPresentation.Gold);
                var badge = MapPresentation.Art("Boss Badge", rect, "Panel", new Vector2(0, -45), new Vector2(100, 25), PrototypeUI.Hex("29303A"));
                PrototypeUI.Label("Boss Caption", badge.transform, "BOSS", Vector2.zero, new Vector2(96, 23), 14, MapPresentation.Gold);

            }
            else
            {
                MapPresentation.Art("Node Ring", rect, "Ring", Vector2.zero, new Vector2(size - 5, size - 5), reachable ? PrototypeUI.Hex("479EAD") : PrototypeUI.Hex("2B444F"));
                MapPresentation.Art("Node Symbol", rect, cleared && !current ? "Check" : Symbol(node), Vector2.zero, new Vector2(size * .65f, size * .65f), current ? PrototypeUI.Hex("24313A") : reachable ? MapPresentation.Cyan : cleared ? MapPresentation.Gold : MapPresentation.Dim);
            }
            MapPresentation.Art("Node Frame", rect, "HexFrame", Vector2.zero, new Vector2(size, size), edge);
            var selectedFrame = MapPresentation.Art("Selected Node", rect, "HexFrame", Vector2.zero, new Vector2(size + 9, size + 9), edge);
            selectedFrame.gameObject.SetActive(false); selectionFrames.Add(node.Id, selectedFrame);
        }
        private static string Symbol(MapNode node) => node.Definition.type == NodeKind.Battle ? "battle" : node.Definition.type == NodeKind.Event ? "event" : "shop";
        private void BuildDetails(RectTransform root)
        {
            MapPresentation.Panel("Map Hint Paper", root, 540, 1594, 986, 307, PrototypeUI.Hex("0F2535"), PrototypeUI.Hex("65756F"));
            MapPresentation.Label("Map Prompt", root, TutorialRun.Finished ? "新手试炼完成\n返回主城，开启正式远征" : MapRun.Finished ? EndingRun.Completed.name + "\n" + EndingRun.Completed.epilogue : TutorialRun.Active ? "点击亮起的节点，查看关卡详情\n战斗 → 事件 → 商店 → Boss" : "点击亮起的节点，查看关卡详情\n沿箭头方向探索中心首领", 540, 1575, 870, 180, 28, MapPresentation.Ivory, TextAnchor.MiddleCenter);
            if (MapRun.Finished)
            {
                var restart = PrototypeUI.Button("New Journey", root, "返回主城 · 新的远征", MapPresentation.Point(540, 1696), MapPresentation.Size(600, 62));
                restart.onClick.AddListener(ReturnHome);
            }
            details = PrototypeUI.Rect("Map Details", root, Vector2.zero, new Vector2(900, 1600)).gameObject;
            MapPresentation.Panel("Map Details Panel", details.transform, 540, 1594, 986, 307, PrototypeUI.Hex("0F2535"), PrototypeUI.Hex("65756F"));
            MapPresentation.Rule("Detail Accent", details.transform, 209, 1442, 286, 3, MapPresentation.Cyan);
            MapPresentation.Panel("Detail Artwork Frame", details.transform, 180, 1555, 222, 180, PrototypeUI.Hex("12313E"), MapPresentation.Dim);
            detailArt = ResourceArtwork.Create(details.transform, "Node Detail Artwork", null, MapPresentation.Point(180, 1555), MapPresentation.Size(218, 176), true);
            detailSymbol = MapPresentation.At("Node Detail Symbol", details.transform, "event", 180, 1555, 92, 92, MapPresentation.Cyan);
            title = MapPresentation.Label("Map Node Title", details.transform, "", 662, 1555, 684, 120, 35, MapPresentation.Ivory, TextAnchor.MiddleLeft, true);
            var body = MapPresentation.Panel("Enter Level", details.transform, 540, 1696, 940, 62, PrototypeUI.Hex("CFAD70"), PrototypeUI.Hex("F1D89E"));
            body.raycastTarget = true; enter = body.gameObject.AddComponent<Button>(); enter.targetGraphic = body;
            var caption = PrototypeUI.Label("Label", body.transform, "进入关卡　→", Vector2.zero, MapPresentation.Size(850, 55), 24, PrototypeUI.Hex("14232C"));
            caption.fontStyle = FontStyle.Bold; enter.onClick.AddListener(EnterSelected);
            details.SetActive(false);
        }
        public void SelectNode(int id)
        {
            if (mapViewport != null && mapViewport.IsDragging) return;
            if (id < 0 || id >= MapRun.Nodes.Count || loading || InventoryUI.IsOpen || characters.IsOpen || MapRun.Nodes[id].Floor != viewFloor) return;
            selected = id;
            if (TutorialRun.Active && MapRun.CanEnter(id)) TutorialRun.Mark("map.select." + id);
            foreach (var pair in rings) { pair.Value.localScale = Vector3.one; selectionFrames[pair.Key].gameObject.SetActive(pair.Key == id); }
            bool reachable = MapRun.CanEnter(id), cleared = MapRun.IsCleared(id);
            MapNode node = MapRun.Nodes[id];
            details.SetActive(true);
            title.text = reachable || cleared ? node.Title : node.Definition.TypeName + "节点";
            detailArt.SetPath(reachable || cleared ? Artwork(node) : null);
            detailSymbol.sprite = MapPresentation.Sprite(Symbol(node));
            detailSymbol.gameObject.SetActive(detailArt.texture == null);
            enter.GetComponentInChildren<Text>().text = node.Definition.type == NodeKind.Event ? (node.Definition.contentId == "EVENT_001" ? "探索遗迹　→" : "进入事件　→") : node.Definition.type == NodeKind.Shop ? "进入商店　→" : "查看敌情 · 准备战斗　→";
            enter.gameObject.SetActive(reachable); enter.interactable = reachable;
        }
        private static string Artwork(MapNode node)
        {
            if (node.Definition.type == NodeKind.Event) return GameContent.Events.Get(node.Definition.contentId).cg;
            if (node.Definition.type == NodeKind.Shop) return GameContent.Shops.Get(node.Definition.contentId).background;
            return PortraitUnit(node)?.imagePath;
        }
        public void EnterSelected()
        {
            if (loading || InventoryUI.IsOpen || characters.IsOpen || !MapRun.TryEnter(selected)) return;
            loading = true; enter.interactable = false;
            var kind = MapRun.Nodes[selected].Definition.type;
            if (kind == NodeKind.Battle) SceneManager.LoadSceneAsync("LevelScene");
            else if (kind == NodeKind.Event) EncounterUI.OpenEvent(mapRoot);
            else EncounterUI.OpenShop(mapRoot);
        }
        public void ReturnHome()
        {
            if (loading || InventoryUI.IsOpen || characters.IsOpen) return;
            loading = true; AdventureRun.FinishIfNeeded(); AdventurePersistence.SaveNow();
            SceneManager.LoadSceneAsync("MainScene");
        }
    }
}
