using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    public sealed class CodexUI : MonoBehaviour
    {
        private RectTransform parent, overlay, detail;
        private Font font;
        private ScrollRect grid, detailScroll;
        private InputField search;
        private Text count;
        private Button clearSearch;
        private readonly Button[] tabs = new Button[3];
        private int category;
        private bool switching;
        public bool IsOpen => overlay != null && overlay.gameObject.activeSelf;
        public int VisibleCount { get; private set; }
        public void Initialize(RectTransform root, Font face) { parent = root; font = face; }

        public void Open(int tab)
        {
            PrototypeUI.font = font;
            if (overlay == null) Create();
            overlay.gameObject.SetActive(true);
            overlay.SetAsLastSibling();
            SelectCategory(tab);
        }

        public void Close()
        {
            CloseDetail();
            search.DeactivateInputField();
            grid.StopMovement();
            overlay.gameObject.SetActive(false);
        }

        private void CloseDetail()
        {
            if (detailScroll != null) detailScroll.StopMovement();
            if (detail != null) detail.gameObject.SetActive(false);
        }

        private Text Label(string name, Transform root, string value, Vector2 pos, Vector2 size,
            int fontSize, Color color, TextAnchor align = TextAnchor.MiddleLeft, bool bold = false)
        {
            var label = InventoryPresentation.Label(name, root, value, pos, size, fontSize, color, bold, align);
            if ((detailScroll != null && root == detailScroll.content) || (grid != null && root == grid.content))
                TopAnchor(label.rectTransform);
            return label;
        }

        private static void TopAnchor(RectTransform rect) { rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1); }
        private static Image Panel(string name, Transform root, Vector2 pos, Vector2 size, Color color, Color border)
        {
            var image = InventoryPresentation.Panel(name, root, pos, size, color, border);
            image.pixelsPerUnitMultiplier = 2;
            image.transform.Find("Frame").GetComponent<Image>().pixelsPerUnitMultiplier = 2;
            return image;
        }

        private Button Button(string name, Transform root, string caption, Vector2 pos, Vector2 size, int fontSize = 23)
        {
            var image = Panel(name, root, pos, size, InventoryPresentation.Surface, MapPresentation.Gold);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var label = Label("Label", image.transform, caption, Vector2.zero, size - new Vector2(18, 8),
                fontSize, MapPresentation.Ivory, TextAnchor.MiddleCenter, true);
            InventoryPresentation.FitLine(label, 16);
            return button;
        }

        private void Header(Transform root, string section, string title, string subtitle)
        {
            PrototypeUI.Panel("Header", root, new Vector2(0, 699), new Vector2(900, 202), MapPresentation.Navy);
            PrototypeUI.Panel("Header Accent", root, new Vector2(-405, 710), new Vector2(3, 78), MapPresentation.Gold);
            Label("Section", root, section, new Vector2(-70, 751), new Vector2(610, 30), 18, MapPresentation.Gold);
            var heading = Label("Heading", root, title, new Vector2(-70, 702), new Vector2(610, 62),
                40, MapPresentation.Ivory, TextAnchor.MiddleLeft, true);
            InventoryPresentation.FitLine(heading, 25);
            Label("Subtitle", root, subtitle, new Vector2(-10, 642), new Vector2(730, 32), 19, MapPresentation.Muted);
            PrototypeUI.Panel("Header Rule", root, new Vector2(0, 597), new Vector2(810, 1), InventoryPresentation.Border);
        }

        private void Create()
        {
            var panel = PrototypeUI.Panel("Codex Overlay", parent, Vector2.zero, new Vector2(900, 1600), PrototypeUI.Hex("081522"));
            panel.raycastTarget = true;
            overlay = panel.rectTransform;
            Header(overlay, "金铲铲之战 / 符文档案馆", "远征图鉴", "认识伙伴、研究敌人，发现道具的力量");
            Button("Close Codex", overlay, "← 返回", new Vector2(331, 744), new Vector2(148, 54), 21).onClick.AddListener(Close);
            string[] names = { "角色", "敌人", "道具" };
            int[] totals = { GameContent.Characters.rows.Length, GameContent.Enemies.rows.Length, GameContent.Items.rows.Length };
            for (int i = 0; i < 3; i++)
            {
                int tab = i;
                tabs[i] = Button("Category " + i, overlay, names[i] + "  " + totals[i], new Vector2((i - 1) * 270, 546), new Vector2(252, 68));
                tabs[i].onClick.AddListener(() => SelectCategory(tab));
            }
            var input = Panel("Codex Search", overlay, new Vector2(0, 448), new Vector2(804, 68),
                InventoryPresentation.Surface, PrototypeUI.Hex("4D6876"));
            input.raycastTarget = true;
            search = input.gameObject.AddComponent<InputField>();
            search.targetGraphic = input;
            search.textComponent = Label("Search Value", input.transform, "", new Vector2(-26, 0),
                new Vector2(700, 58), 22, MapPresentation.Ivory);
            var placeholder = Label("Search Placeholder", input.transform, "搜索名称 / 技能 / 介绍…",
                new Vector2(-26, 0), new Vector2(700, 58), 22, MapPresentation.Muted);
            search.placeholder = placeholder;
            search.characterLimit = 40;
            search.customCaretColor = true;
            search.caretColor = MapPresentation.Cyan;
            search.selectionColor = new Color(.3f, .7f, .8f, .35f);
            clearSearch = Button("Clear Codex Search", input.transform, "×", new Vector2(365, 0), new Vector2(48, 48), 25);
            clearSearch.image.color = Color.clear;
            clearSearch.transform.Find("Frame").gameObject.SetActive(false);
            clearSearch.onClick.AddListener(() => search.text = "");
            search.onValueChanged.AddListener(_ => { if (!switching) Refresh(); });
            count = Label("Codex Count", overlay, "", new Vector2(0, 382), new Vector2(798, 36), 20, MapPresentation.Gold);
            grid = InventoryPresentation.Scroll("Codex Grid", overlay, new Vector2(0, -159), new Vector2(804, 1008), 760);
            Label("Codex Footer", overlay, "点击卡片查看详情 · 上下滑动浏览", new Vector2(0, -710),
                new Vector2(800, 36), 20, MapPresentation.Muted, TextAnchor.MiddleCenter);
            Label("Codex Signature", overlay, "金铲铲之战 / 符文远征", new Vector2(0, -764),
                new Vector2(800, 28), 16, MapPresentation.Dim, TextAnchor.MiddleCenter);
        }

        private void SelectCategory(int tab)
        {
            category = Mathf.Clamp(tab, 0, 2);
            switching = true;
            search.text = "";
            search.DeactivateInputField();
            switching = false;
            CloseDetail();
            for (int i = 0; i < 3; i++)
            {
                bool selected = i == category;
                tabs[i].image.color = selected ? PrototypeUI.Hex("CFAD70") : InventoryPresentation.Surface;
                tabs[i].GetComponentInChildren<Text>().color = selected ? MapPresentation.Navy : MapPresentation.Muted;
                tabs[i].transform.Find("Frame").GetComponent<Image>().color = selected ? MapPresentation.Gold : InventoryPresentation.Border;
            }
            Refresh();
        }

        private bool Match(params string[] values) => string.IsNullOrWhiteSpace(search.text)
            || values.Any(value => !string.IsNullOrEmpty(value) && value.IndexOf(search.text.Trim(), StringComparison.OrdinalIgnoreCase) >= 0);

        private void Refresh()
        {
            foreach (Transform child in grid.content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            grid.StopMovement();
            clearSearch.gameObject.SetActive(search.text.Length > 0);
            int total = 0, index = 0;
            if (category < 2)
            {
                var rows = category == 0 ? GameContent.Characters.rows : GameContent.Enemies.rows;
                total = rows.Length;
                foreach (var unit in rows)
                    if (Match(unit.name, unit.skillName, unit.biography)) UnitCard(unit, index++);
            }
            else
            {
                total = GameContent.Items.rows.Length;
                foreach (var item in GameContent.Items.rows)
                    if (Match(item.name, item.description, ItemBonuses.Summary(item))) ItemCard(item, index++);
            }
            VisibleCount = index;
            count.text = (category == 0 ? "伙伴档案" : category == 1 ? "敌情档案" : "道具档案") + "  " + index + " / " + total
                + (category < 2 ? "     基础属性 · 不含本局加成" : "     持有效果与道具说明");
            float rowHeight = category < 2 ? 382 : 310;
            grid.content.sizeDelta = new Vector2(760, Mathf.Max(grid.viewport.rect.height, Mathf.CeilToInt(index / 3f) * rowHeight + 16));
            if (index == 0)
            {
                Label("No Results", grid.content, "没有找到匹配条目", new Vector2(0, -220), new Vector2(700, 58),
                    28, MapPresentation.Ivory, TextAnchor.MiddleCenter, true);
                Label("No Results Hint", grid.content, "试试其他名称、技能或介绍关键词", new Vector2(0, -280), new Vector2(700, 46),
                    21, MapPresentation.Muted, TextAnchor.MiddleCenter);
            }
            InventoryPresentation.Top(grid);
        }

        private static string PortraitPath(UnitRecord unit)
        {
            string path = "CardPortraits/" + unit.imagePath;
            return !string.IsNullOrEmpty(unit.imagePath) && Resources.Load<Texture2D>(path) != null ? path : unit.imagePath;
        }

        private Button Card(string id, int index, Rarity rarity, bool unit)
        {
            float height = unit ? 357 : 286;
            float stride = unit ? 382 : 310;
            var image = Panel("Codex Entry " + id, grid.content, new Vector2((index % 3 - 1) * 252, -16 - height / 2 - index / 3 * stride),
                new Vector2(238, height), InventoryPresentation.Surface, InventoryPresentation.Rarity(rarity));
            TopAnchor(image.rectTransform);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private void CardCaption(Transform root, string title, string tags, float y)
        {
            var name = Label("Name", root, title, new Vector2(0, y), new Vector2(214, 32), 23, MapPresentation.Ivory,
                TextAnchor.MiddleLeft, true);
            InventoryPresentation.FitLine(name, 14);
            var tag = Label("Classification", root, tags, new Vector2(0, y - 29), new Vector2(214, 24), 16, MapPresentation.Muted);
            InventoryPresentation.FitLine(tag, 13);
        }

        private void RarityBadge(Transform root, Rarity rarity, float y)
        {
            PrototypeUI.Panel("Rarity Badge", root, new Vector2(-73, y), new Vector2(74, 28), MapPresentation.Navy);
            Label("Rarity", root, RarityRules.Name(rarity), new Vector2(-73, y), new Vector2(70, 26), 16,
                InventoryPresentation.Rarity(rarity), TextAnchor.MiddleCenter, true);
        }

        private void UnitCard(UnitRecord unit, int index)
        {
            var button = Card(unit.id, index, unit.rarity, true);
            ResourceArtwork.Create(button.transform, "Portrait", PortraitPath(unit), new Vector2(0, 32), new Vector2(230, 285), true);
            PrototypeUI.Panel("Portrait Rule", button.transform, new Vector2(0, -111), new Vector2(222, 1), InventoryPresentation.Rarity(unit.rarity));
            var definition = unit.Create();
            CardCaption(button.transform, unit.name, definition.ElementName + " · " + definition.RoleName, -130);
            ElementBadge.Create(button.transform, new Vector2(-91, 151), 38, definition.element);
            if (unit.bossMechanic != BossMechanic.None)
            {
                PrototypeUI.Panel("Boss Badge", button.transform, new Vector2(76, 153), new Vector2(64, 28), PrototypeUI.Hex("663B35"));
                Label("Boss Tag", button.transform, "首领", new Vector2(76, 153), new Vector2(60, 26), 16, MapPresentation.Ivory, TextAnchor.MiddleCenter, true);
            }
            button.onClick.AddListener(() => ShowUnit(unit));
        }

        private void ItemCard(ItemRecord item, int index)
        {
            var button = Card(item.id, index, item.rarity, false);
            ResourceArtwork.Create(button.transform, "Item Emblem", item.imagePath, new Vector2(0, 24), new Vector2(180, 180));
            PrototypeUI.Panel("Portrait Rule", button.transform, new Vector2(0, -72), new Vector2(214, 1), InventoryPresentation.Border);
            CardCaption(button.transform, item.name, item.kind, -95);
            RarityBadge(button.transform, item.rarity, 119);
            button.onClick.AddListener(() => ShowItem(item));
        }

        private void PrepareDetail(string title, string subtitle)
        {
            grid.StopMovement();
            search.DeactivateInputField();
            if (detail != null) { detail.gameObject.SetActive(false); Destroy(detail.gameObject); }
            var panel = PrototypeUI.Panel("Codex Detail", overlay, Vector2.zero, new Vector2(900, 1600), PrototypeUI.Hex("081522"));
            panel.raycastTarget = true;
            detail = panel.rectTransform;
            Header(detail, "远征图鉴 / " + subtitle, title, "基础档案 · 技能、特性与背景");
            Button("Back To Entries", detail, "← 返回", new Vector2(331, 744), new Vector2(148, 54), 21).onClick.AddListener(CloseDetail);
            detailScroll = InventoryPresentation.Scroll("Detail", detail, new Vector2(0, -60), new Vector2(804, 1260), 760);
            Label("Detail Footer", detail, "上下滑动查看完整档案", new Vector2(0, -744), new Vector2(800, 38), 20, MapPresentation.Muted, TextAnchor.MiddleCenter);
        }

        private Image DetailPanel(string name, float top, float height)
        {
            var panel = Panel(name, detailScroll.content, new Vector2(0, -top - height / 2), new Vector2(744, height),
                InventoryPresentation.Surface, InventoryPresentation.Border);
            TopAnchor(panel.rectTransform);
            return panel;
        }

        private float Paragraph(string title, string text, float top)
        {
            var panel = DetailPanel("Section Panel " + title, top, 100);
            var heading = Label("Section " + title, panel.transform, title, Vector2.zero, new Vector2(696, 38), 24,
                MapPresentation.Gold, TextAnchor.MiddleLeft, true);
            var body = Label("Body " + title, panel.transform, string.IsNullOrEmpty(text) ? "暂无记录。" : text, Vector2.zero,
                new Vector2(696, 100), 23, MapPresentation.Ivory, TextAnchor.UpperLeft);
            // Leave a full line of slack for dynamic-font wrapping at different canvas scales.
            float bodyHeight = Mathf.Max(42, body.preferredHeight + body.fontSize * 1.6f);
            float height = bodyHeight + 94;
            panel.rectTransform.sizeDelta = new Vector2(744, height);
            panel.rectTransform.anchoredPosition = new Vector2(0, -top - height / 2);
            panel.transform.Find("Frame").GetComponent<RectTransform>().sizeDelta = panel.rectTransform.sizeDelta;
            heading.rectTransform.anchoredPosition = new Vector2(0, height / 2 - 32);
            body.rectTransform.sizeDelta = new Vector2(696, bodyHeight);
            body.rectTransform.anchoredPosition = new Vector2(0, height / 2 - 64 - bodyHeight / 2);
            return top + height + 20;
        }

        private void FinishDetail(float height)
        {
            detailScroll.content.sizeDelta = new Vector2(760, Mathf.Max(detailScroll.viewport.rect.height, height + 16));
            InventoryPresentation.Top(detailScroll);
        }

        private void Stat(Transform root, string caption, int value, int index)
        {
            var position = new Vector2(47 + index % 2 * 174, 18 - index / 2 * 96);
            var box = Panel("Stat " + caption, root, position, new Vector2(158, 82), InventoryPresentation.Field, PrototypeUI.Hex("34515E"));
            Label("Caption", box.transform, caption, new Vector2(0, 21), new Vector2(136, 26), 17, MapPresentation.Muted);
            Label("Value", box.transform, value.ToString(), new Vector2(0, -12), new Vector2(136, 38), 27, MapPresentation.Ivory, TextAnchor.MiddleLeft, true);
        }

        private void ShowUnit(UnitRecord unit)
        {
            var definition = unit.Create();
            PrepareDetail(unit.name, category == 0 ? "角色" : "敌人");
            var hero = DetailPanel("Unit Dossier", 16, 422);
            InventoryPresentation.ArtworkFrame("Detail Portrait Frame", hero.transform, new Vector2(-225, 0), new Vector2(250, 372), InventoryPresentation.Rarity(unit.rarity));
            ResourceArtwork.Create(hero.transform, "Portrait", PortraitPath(unit), new Vector2(-225, 0), new Vector2(242, 364), true);
            var tags = Label("Unit Tags", hero.transform, RarityRules.Name(unit.rarity) + " · " + definition.ElementName + " · " + definition.RoleName,
                new Vector2(140, 166), new Vector2(352, 40), 23, InventoryPresentation.Rarity(unit.rarity), TextAnchor.MiddleLeft, true);
            InventoryPresentation.FitLine(tags, 18);
            Label("Unit Record", hero.transform, unit.bossMechanic == BossMechanic.None ? "基础属性" : "首领档案 · 基础属性",
                new Vector2(140, 107), new Vector2(352, 36), 21, MapPresentation.Muted);
            Stat(hero.transform, "生命", unit.maxHealth, 0);
            Stat(hero.transform, "攻击", unit.attack, 1);
            Stat(hero.transform, "防御", unit.defense, 2);
            Stat(hero.transform, "速度", unit.speed, 3);
            Label("Unit Stat Hint", hero.transform, "战斗数值以当前关卡为准", new Vector2(140, -163), new Vector2(352, 32), 17, MapPresentation.Muted);
            float y = Paragraph(string.IsNullOrEmpty(unit.skillName) ? "战斗技能" : unit.skillName, definition.SkillDescription, 462);
            if (!string.IsNullOrEmpty(definition.TraitDescription)) y = Paragraph("特性与机制", definition.TraitDescription, y);
            y = Paragraph("角色介绍", unit.biography, y);
            y = Paragraph("图鉴说明", category == 0 ? "以上为角色基础属性，不包含冒险中的道具加成。"
                : "以上为敌人基础属性。实际战斗还会应用所在楼层的生命与攻击倍率。", y);
            FinishDetail(y);
        }

        private void ShowItem(ItemRecord item)
        {
            PrepareDetail(item.name, "道具");
            detail.Find("Subtitle").GetComponent<Text>().text = "物资档案 · 持有效果与道具说明";
            var hero = DetailPanel("Item Dossier", 16, 342);
            InventoryPresentation.ArtworkFrame("Detail Item Frame", hero.transform, new Vector2(-215, 0), new Vector2(270, 270), InventoryPresentation.Rarity(item.rarity));
            ResourceArtwork.Create(hero.transform, "Detail Item", item.imagePath, new Vector2(-215, 0), new Vector2(246, 246));
            Label("Item Rarity", hero.transform, RarityRules.Name(item.rarity), new Vector2(146, 91), new Vector2(350, 44),
                27, InventoryPresentation.Rarity(item.rarity), TextAnchor.MiddleLeft, true);
            Label("Item Tags", hero.transform, item.kind, new Vector2(146, 23), new Vector2(350, 74), 23, MapPresentation.Ivory);
            Label("Item Hint", hero.transform, "探索、战斗与商店中的远征物资", new Vector2(146, -90), new Vector2(350, 70), 20, MapPresentation.Muted);
            float y = Paragraph("道具效果", ItemBonuses.Description(item), 382);
            FinishDetail(y);
        }

        private void Update()
        {
            if (!IsOpen || !Input.GetKeyDown(KeyCode.Escape)) return;
            if (detail != null && detail.gameObject.activeSelf) CloseDetail(); else Close();
        }
    }
}
