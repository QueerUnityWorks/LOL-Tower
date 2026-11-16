using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    // The approved 1080 x 1920 composition uses the existing 900 x 1600 safe-area canvas.
    public static class MapPresentation
    {
        public static readonly Color Navy = PrototypeUI.Hex("0C1B2B");
        public static readonly Color Ivory = PrototypeUI.Hex("F8EED8");
        public static readonly Color Gold = PrototypeUI.Hex("DCC18B");
        public static readonly Color Cyan = PrototypeUI.Hex("76E5F1");
        public static readonly Color Muted = PrototypeUI.Hex("92ABBA");
        public static readonly Color Dim = PrototypeUI.Hex("6F8994");
        private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        public static Vector2 Point(float x, float y) => new Vector2((x - 540) / 1.2f, (960 - y) / 1.2f);
        public static Vector2 Size(float w, float h) => new Vector2(w / 1.2f, h / 1.2f);
        public static Sprite Sprite(string name)
        {
            if (sprites.TryGetValue(name, out var cached) && cached != null) return cached;
            var texture = Resources.Load<Texture2D>("MapUI/" + name);
            if (texture == null) return null;
            var border = name == "Panel" || name == "PanelBorder" ? new Vector4(24, 24, 24, 24) : Vector4.zero;
            return sprites[name] = UnityEngine.Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, border);
        }
        public static Image Art(string name, Transform parent, string asset, Vector2 position, Vector2 size, Color tint)
        {
            var image = PrototypeUI.Panel(name, parent, position, size, tint);
            image.sprite = Sprite(asset);
            if (asset == "Panel" || asset == "PanelBorder") image.type = Image.Type.Sliced;
            return image;
        }
        public static Image At(string name, Transform parent, string asset, float x, float y, float w, float h, Color tint)
            => Art(name, parent, asset, Point(x, y), Size(w, h), tint);
        public static Image Panel(string name, Transform parent, float x, float y, float w, float h, Color tint, Color border)
        {
            var panel = At(name, parent, "Panel", x, y, w, h, tint);
            Art("Frame", panel.transform, "PanelBorder", Vector2.zero, Size(w, h), border);
            return panel;
        }
        public static Text Label(string name, Transform parent, string value, float x, float y, float w, float h, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft, bool bold = false)
        {
            var label = PrototypeUI.Label(name, parent, value, Point(x, y), Size(w, h), Mathf.RoundToInt(size / 1.2f), color, align);
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Max(12, Mathf.RoundToInt(size / 1.2f) - 5);
            label.resizeTextMaxSize = Mathf.RoundToInt(size / 1.2f);
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }
        public static void Rule(string name, Transform parent, float x, float y, float w, float h, Color tint)
            => PrototypeUI.Panel(name, parent, Point(x, y), Size(w, h), tint);
        public static void StyleNavigation(Button button, Vector2 position, Vector2 size, string caption, string icon)
        {
            var rect = (RectTransform)button.transform;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            button.image.sprite = Sprite("Panel"); button.image.type = Image.Type.Sliced;
            button.image.color = PrototypeUI.Hex("132838");
            foreach (string name in new[] { "Top Border", "Bottom Border", "Left Border", "Right Border" })
            { var old = button.transform.Find(name); if (old != null) old.gameObject.SetActive(false); }
            if (button.transform.Find("Map Navigation Frame") == null)
                Art("Map Navigation Frame", button.transform, "PanelBorder", Vector2.zero, size, PrototypeUI.Hex("8A7956"));
            var text = button.GetComponentInChildren<Text>();
            text.text = caption; text.fontSize = 23; text.fontStyle = FontStyle.Bold; text.color = Ivory;
            text.rectTransform.anchoredPosition = new Vector2(22, 0);
            text.rectTransform.sizeDelta = new Vector2(size.x - 72, size.y - 8);
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 18; text.resizeTextMaxSize = 23;
            if (button.transform.Find("Map Navigation Icon") == null)
                Art("Map Navigation Icon", button.transform, icon, new Vector2(caption.Length > 2 ? -76 : -55, 0), new Vector2(42, 42), Gold);
        }
        public static void BuildHeader(RectTransform root, FloorRecord floor)
        {
            var backdrop = root.GetComponentInParent<Canvas>().transform.Find("Backdrop");
            if (backdrop != null) backdrop.GetComponent<Image>().color = PrototypeUI.Hex("081522");
            ResourceArtwork.Create(root, "Map Forest Background", "MapUI/Background", Vector2.zero, new Vector2(900, 1600));
            PrototypeUI.Panel("Map Header", root, Point(540, 132), Size(1080, 264), Navy);
            Rule("Header Rule", root, 540, 263, 982, 2, PrototypeUI.Hex("75694E"));
            Rule("Top Accent", root, 52, 93, 4, 90, Gold);
            Label("Section", root, "金铲铲之战  /  符文远征", 326, 54, 500, 32, 23, Gold);
            Label("Title", root, TutorialRun.Active ? "林间启程 · 新手试炼" : floor.name, 420, 109, 690, 67, 49, Ivory, TextAnchor.MiddleLeft, true);
            Panel("Floor Badge", root, 905, 80, 247, 73, PrototypeUI.Hex("142A3A"), PrototypeUI.Hex("4A5C62"));
            Label("Floor Label", root, TutorialRun.Active ? "新手教程" : "第 " + floor.floor + " / " + (EndingRun.ExtraFloorUnlocked(4) ? 4 : 3) + " 层", 905, 80, 230, 50, 30, Ivory, TextAnchor.MiddleCenter, true);
            Label("Map Difficulty", root, TutorialRun.Active ? "练习关卡" : "难度 " + AdventureRun.Difficulty, 917, 149, 220, 32, 22, Muted, TextAnchor.MiddleRight);
            At("Gold Icon", root, "coin", 84, 191, 48, 48, Gold);
            Label("Map Gold", root, RunState.Gold.ToString(), 166, 191, 94, 42, 30, Ivory, TextAnchor.MiddleLeft, true);
            Rule("Resource Divider 1", root, 214, 190, 1, 30, PrototypeUI.Hex("3C505B"));
            At("Party Icon", root, "role", 255, 191, 44, 44, Muted);
            Label("Map Party", root, "伙伴  " + RunState.Characters.Count, 363, 191, 150, 40, 25, Ivory);
            Rule("Resource Divider 2", root, 442, 190, 1, 30, PrototypeUI.Hex("3C505B"));
            Label("Map Progress", root, "已通过  " + MapRun.ClearedCount, 599, 191, 248, 40, 25, Muted);
            Label("Adventure Lives", root, "生命 " + RunState.Lives + " / " + RunState.InitialLives, 917, 191, 220, 40, 25, Gold, TextAnchor.MiddleRight, true);
            Label("Map Level", root, "Lv. " + Progression.Level, 117, 234, 82, 30, 19, Gold, TextAnchor.MiddleLeft, true);
            Rule("Map XP Track", root, 493, 235, 658, 6, PrototypeUI.Hex("2A4050"));
            float fill = Progression.IsMaxLevel ? 1 : Mathf.Clamp01((float)Progression.Experience / Progression.Current.experienceToNext);
            Rule("Map XP Fill", root, 164 + 329 * fill, 235, 658 * fill, 6, Cyan);
            Label("Map Experience", root, Progression.IsMaxLevel ? "经验已满 · 满级" : "经验 " + Progression.Experience + " / " + Progression.Current.experienceToNext, 929, 234, 198, 30, 20, Muted, TextAnchor.MiddleRight);
        }
    }
}
