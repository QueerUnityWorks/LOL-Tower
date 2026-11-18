using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElementalTactics
{
    public sealed class InventoryUI : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        public static bool BlocksBattleInput => IsOpen || closedFrame == Time.frameCount;
        private static int closedFrame = -1;
        private GameObject overlay;
        private GameObject empty;
        private RectTransform content;
        private ScrollRect scroll;
        private Text summary;
        private Text itemTitle;
        private Text itemDescription;
        private Text itemCategory, itemEffect;
        private ResourceArtwork itemArtwork;
        private Image itemFallback;
        private ScrollRect detailScroll;
        private readonly Dictionary<string, Image> itemRows = new Dictionary<string, Image>();
        private Font font;
        private string selectedId;
        private bool opened;
        private Button openButton;
        private FormationController formation;
        private int openButtonMode = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            IsOpen = false;
            closedFrame = -1;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "StartScene" || scene.name == "MainScene" || mode == LoadSceneMode.Additive) return;
            if (FindObjectOfType<InventoryUI>() != null) return;
            new GameObject("Inventory Controller").AddComponent<InventoryUI>();
        }

        private IEnumerator Start()
        {
            yield return null;
            var layout = FindObjectOfType<PortraitLayout>();
            if (layout == null) yield break;
            var label = layout.content.GetComponentInChildren<Text>(true);
            font = label != null ? label.font : PrototypeUI.font;
            PrototypeUI.font = font;
            var root = PrototypeUI.Rect("Inventory Interface", layout.content, Vector2.zero, new Vector2(900, 1600));
            var open = PrototypeUI.Button("Open Inventory", root, "背\n包", new Vector2(-405, 0), new Vector2(68, 112), false);
            openButton = open;
            formation = FindObjectOfType<FormationController>();
            open.GetComponentInChildren<Text>().fontSize = 24;
            open.onClick.AddListener(Open);
            RefreshOpenButton();
            var paper = InventoryPresentation.Shell(root, "Inventory", "道具背包", "bag", Close, out overlay, out summary);
            scroll = InventoryPresentation.Scroll("Inventory", paper, new Vector2(0,165), new Vector2(780,740), 736);
            content = scroll.content;
            empty = PrototypeUI.Rect("Inventory Empty", scroll.viewport, Vector2.zero, new Vector2(690,320)).gameObject;
            MapPresentation.Art("Empty Bag Icon", empty.transform, "bag", new Vector2(0,92), new Vector2(100,100), MapPresentation.Gold);
            InventoryPresentation.Label("Empty Bag Title", empty.transform, "行囊还是空的", new Vector2(0,-6), new Vector2(650,54), 30, MapPresentation.Ivory, true, TextAnchor.MiddleCenter);
            InventoryPresentation.Label("Empty Bag Hint", empty.transform, "探索事件、战斗或商店，收集远征物资。", new Vector2(0,-65), new Vector2(650,68), 21, MapPresentation.Muted, false, TextAnchor.MiddleCenter);
            InventoryPresentation.Panel("Inventory Detail Paper", paper, new Vector2(0,-420), new Vector2(780,390), InventoryPresentation.Surface, InventoryPresentation.Border);
            InventoryPresentation.ArtworkFrame("Inventory Artwork Frame", paper, new Vector2(-302,-315), new Vector2(122,122), MapPresentation.Gold);
            itemArtwork = ResourceArtwork.Create(paper, "Inventory Selected Artwork", null, new Vector2(-302,-315), new Vector2(108,108), true);
            itemFallback = MapPresentation.Art("Inventory Artwork Fallback", paper, "bag", new Vector2(-302,-315), new Vector2(64,64), MapPresentation.Gold);
            itemTitle = InventoryPresentation.Label("Inventory Item Title", paper, "", new Vector2(68,-277), new Vector2(542,48), 30, MapPresentation.Ivory, true);
            InventoryPresentation.FitLine(itemTitle,20);
            itemCategory = InventoryPresentation.Label("Inventory Item Category", paper, "", new Vector2(68,-318), new Vector2(542,34), 20, MapPresentation.Muted);
            InventoryPresentation.FitLine(itemCategory);
            itemEffect = InventoryPresentation.Label("Inventory Item Effect", paper, "", new Vector2(68,-360), new Vector2(542,38), 21, MapPresentation.Cyan);
            InventoryPresentation.FitLine(itemEffect);
            itemDescription = InventoryPresentation.Description("Inventory Item Description", paper, new Vector2(0,-491), new Vector2(732,184), out detailScroll);
            InventoryPresentation.Label("Inventory Footnote", paper, "点击道具查看当前总加成 · 上下滑动浏览物资", new Vector2(0,-663), new Vector2(780,60), 21, MapPresentation.Muted, false, TextAnchor.MiddleCenter);
            InventoryState.Changed += OnInventoryChanged;
            overlay.SetActive(false);
        }

        public void Open()
        {
            if (overlay == null || IsOpen || SettlementRun.Pending) return;
            var map = FindObjectOfType<MapController>();
            if (map != null && map.GetComponent<OwnedCharactersUI>() != null && map.GetComponent<OwnedCharactersUI>().IsOpen) return;
            var battle = FindObjectOfType<BattleController>();
            if (battle != null && battle.IsBattling && !battle.IsPaused) battle.PauseBattle();
            IsOpen = true;
            GameAudio.Play(SoundCue.InventoryOpen);
            if (TutorialRun.Active && MapRun.Pending == 2) TutorialRun.Mark("shop.bag.open");
            opened = true;
            overlay.transform.parent.SetAsLastSibling();
            overlay.SetActive(true);
            Refresh();
            InventoryPresentation.Top(scroll);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            GameAudio.Play(SoundCue.PanelClose);
            opened = false;
            closedFrame = Time.frameCount;
            scroll.StopMovement();
            detailScroll.StopMovement();
            overlay.SetActive(false);
        }

        private void Update()
        {
            RefreshOpenButton();
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        private void RefreshOpenButton()
        {
            if (openButton == null) return;
            bool available = !SettlementRun.Pending;
            if (openButton.gameObject.activeSelf != available) openButton.gameObject.SetActive(available);
            if (!available)
            {
                if (IsOpen) Close();
                return;
            }
            bool preparing = formation != null && formation.preparationRoot.activeInHierarchy;
            bool fighting = formation != null && !preparing;
            int mode = preparing ? 1 : fighting ? 2 : 0;
            if (mode == openButtonMode) return;
            openButtonMode = mode;
            if (mode == 0 && SceneManager.GetActiveScene().name == "MapScene")
            {
                MapPresentation.StyleNavigation(openButton, MapPresentation.Point(530, 1824), MapPresentation.Size(304, 86), "背包", "bag");
                return;
            }
            if (preparing)
            {
                FormationPresentation.StyleInventoryButton(openButton);
                return;
            }
            if (fighting) FormationPresentation.RestoreBattleInventory(openButton);
            var rect = (RectTransform)openButton.transform;
            Vector2 size = preparing ? new Vector2(180, 76) : fighting ? new Vector2(170, 62) : new Vector2(68, 112);
            rect.anchoredPosition = preparing ? new Vector2(-316, -649) : fighting ? new Vector2(-332, -731) : new Vector2(-405, 0);
            rect.sizeDelta = size;
            var caption = openButton.GetComponentInChildren<Text>();
            caption.text = preparing || fighting ? "背包" : "背\n包";
            caption.fontSize = preparing ? 27 : fighting ? 22 : 24;
            caption.rectTransform.sizeDelta = size - new Vector2(20, 8);
            foreach (string name in new[] { "Top Border", "Bottom Border", "Left Border", "Right Border" })
            {
                var border = openButton.transform.Find(name).GetComponent<RectTransform>();
                bool horizontal = name == "Top Border" || name == "Bottom Border";
                border.anchoredPosition = horizontal
                    ? new Vector2(0, (name == "Top Border" ? 1 : -1) * (size.y / 2 - 3))
                    : new Vector2((name == "Right Border" ? 1 : -1) * (size.x / 2 - 3), 0);
                border.sizeDelta = horizontal ? new Vector2(size.x - 6, 2) : new Vector2(2, size.y - 6);
            }
        }

        private void OnInventoryChanged()
        {
            if (overlay != null && overlay.activeSelf) Refresh();
        }

        private void Refresh()
        {
            PrototypeUI.font = font;
            float previousScroll = scroll.verticalNormalizedPosition;
            foreach (Transform child in content)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            int count = InventoryState.Items.Count;
            itemRows.Clear();
            summary.text = "物资 " + count + " 种 · 共 " + InventoryState.Items.Sum(item => (long)item.Quantity) + " 件　 /　金币 " + RunState.Gold;
            empty.SetActive(count == 0);
            content.sizeDelta = new Vector2(736, Mathf.Max(scroll.viewport.rect.height, count * 148 + 32));
            InventoryItem selected = null;
            for (int index = 0; index < count; index++)
            {
                InventoryItem item = InventoryState.Items[index];
                if (item.Id == selectedId) selected = item;
                var definition = GameContent.Items.rows.FirstOrDefault(entry => entry.id == item.Id);
                Color rarity = definition == null ? MapPresentation.Gold : InventoryPresentation.Rarity(definition.rarity);
                var row = InventoryPresentation.Panel("Inventory Item " + item.Id, content, new Vector2(0, -82 - index * 148), new Vector2(728,132), InventoryPresentation.Surface, InventoryPresentation.Border);
                row.rectTransform.anchorMin = row.rectTransform.anchorMax = new Vector2(0.5f, 1);
                row.raycastTarget = true;
                var button = row.gameObject.AddComponent<Button>();
                UiAudioFeedback.Bind(button, SoundCue.CardSelect);
                button.targetGraphic = row;
                button.onClick.AddListener(() => ShowItem(item));
                itemRows.Add(item.Id,row);
                InventoryPresentation.ArtworkFrame("Item Icon Frame", row.transform, new Vector2(-292,0), new Vector2(102,102), rarity);
                var artwork = ResourceArtwork.Create(row.transform, "Item Artwork", definition?.imagePath, new Vector2(-292,0), new Vector2(92,92), true);
                if (artwork.texture == null) MapPresentation.Art("Item Symbol", row.transform, "bag", new Vector2(-292,0), new Vector2(55,55),rarity);
                var name = InventoryPresentation.Label("Item Name", row.transform, item.Name, new Vector2(-24,34), new Vector2(410,38),25,MapPresentation.Ivory,true);
                InventoryPresentation.FitLine(name,19);
                var category = InventoryPresentation.Label("Item Category", row.transform,definition == null ? "远征物资" : RarityRules.Name(definition.rarity) + " · " + definition.kind,new Vector2(-24,0),new Vector2(410,28),17,rarity);
                InventoryPresentation.FitLine(category);
                string effect = definition == null ? item.Description : ItemBonuses.Summary(definition,item.Quantity);
                int stop = effect.IndexOf('。'); if (stop >= 0) effect = effect.Substring(0,stop);
                var brief = InventoryPresentation.Label("Item Brief", row.transform,effect,new Vector2(20,-36),new Vector2(498,28),18,MapPresentation.Muted);
                InventoryPresentation.FitLine(brief,16);
                InventoryPresentation.Panel("Item Quantity Badge",row.transform,new Vector2(285,26),new Vector2(116,44),InventoryPresentation.Field,InventoryPresentation.Border);
                var quantity = InventoryPresentation.Label("Item Quantity",row.transform,"×"+item.Quantity,new Vector2(285,26),new Vector2(108,38),22,MapPresentation.Gold,true,TextAnchor.MiddleCenter);
                InventoryPresentation.FitLine(quantity,12);
            }
            ShowItem(selected ?? (count > 0 ? InventoryState.Items[0] : null));
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = Mathf.Clamp01(previousScroll);
        }

        private void ShowItem(InventoryItem item)
        {
            selectedId = item?.Id;
            itemTitle.text = item == null ? "道具详情" : item.Name;
            itemTitle.color = MapPresentation.Ivory;
            itemCategory.text = item == null ? "暂无物资" : "持有 ×" + item.Quantity;
            itemCategory.color = MapPresentation.Muted; itemEffect.text = "";
            string description = item == null ? "获得道具后，可在这里查看效果、数量与介绍。" : item.Description;
            itemArtwork.SetPath(null);
            if (item != null)
            {
                var definition = GameContent.Items.rows.FirstOrDefault(entry => entry.id == item.Id);
                if (definition != null)
                {
                    itemTitle.text = definition.name;
                    itemCategory.text = RarityRules.Name(definition.rarity) + " · " + definition.kind + " · 持有 ×" + item.Quantity;
                    itemCategory.color = InventoryPresentation.Rarity(definition.rarity);
                    itemArtwork.SetPath(definition.imagePath);
                    itemEffect.text = string.IsNullOrEmpty(definition.effectId) ? "远征中的特殊物资" : ItemBonuses.Summary(definition,item.Quantity);
                    description = ItemBonuses.Description(definition, item.Quantity);
                }
            }
            itemFallback.gameObject.SetActive(itemArtwork.texture == null);
            InventoryPresentation.SetDescription(itemDescription,detailScroll,description);
            foreach (var pair in itemRows)
            {
                pair.Value.color = pair.Key == selectedId ? InventoryPresentation.Selected : InventoryPresentation.Surface;
                pair.Value.transform.Find("Frame").GetComponent<Image>().color = pair.Key == selectedId ? MapPresentation.Cyan : InventoryPresentation.Border;
            }
        }

        private void OnDestroy()
        {
            InventoryState.Changed -= OnInventoryChanged;
            if (opened) IsOpen = false;
        }
    }
}
