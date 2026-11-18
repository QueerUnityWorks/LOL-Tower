using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    public static class SettlementPresentation
    {
        private static readonly Color White = PrototypeUI.Hex("F2F0E8");
        private static readonly Color Muted = PrototypeUI.Hex("929CA7");
        private static readonly Color Border = PrototypeUI.Hex("35414C");
        private static readonly Color Navy = PrototypeUI.Hex("101922");
        private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        public static Color Accent(SettlementResult result) => PrototypeUI.Hex(!result.victory ? "D9867E"
            : result.endingId == "ENDING_002" ? "87C8B7" : result.endingId == "ENDING_003" ? "A8B9EF" : "D8BB79");

        public static RectTransform Build(Transform parent, SettlementResult result, Action home, Action restart)
        {
            var root = PrototypeUI.Rect("Expedition Settlement", parent, Vector2.zero, new Vector2(1080, 1920));
            root.localScale = Vector3.one / 1.2f;
            var blocker = root.gameObject.AddComponent<Image>();
            blocker.color = Navy;
            blocker.raycastTarget = true;
            Artwork(root, "Settlement Background", "SettlementUI/Background", 0, 0, 1080, 1920);
            Panel(root, "Top Accent", 0, 0, 1080, 3, Accent(result));
            BuildHeader(root, result);
            BuildCharacters(root, result);
            BuildItems(root, result);
            BuildScore(root, result);
            BuildFooter(root, result, home, restart);
            root.SetAsLastSibling();
            return root;
        }

        private static RectTransform Rect(Transform parent, string name, float left, float top, float width, float height)
        {
            var rect = PrototypeUI.Rect(name, parent, new Vector2(left + width / 2, -top - height / 2), new Vector2(width, height));
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            return rect;
        }

        private static Image Panel(Transform parent, string name, float left, float top, float width, float height, Color color)
        {
            var image = Rect(parent, name, left, top, width, height).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image CutPanel(Transform parent, string name, float left, float top, float width, float height, Color color, bool topCorner = false)
        {
            var image = Panel(parent, name, left, top, width, height, color);
            string path = topCorner ? "CutTopRight" : "CutBottomRight";
            if (!sprites.TryGetValue(path, out var sprite) || sprite == null)
            {
                var texture = Resources.Load<Texture2D>("SettlementUI/" + path);
                sprite = Sprite.Create(texture, new UnityEngine.Rect(0, 0, texture.width, texture.height), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect, Vector4.one * 25);
                sprites[path] = sprite;
            }
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            return image;
        }

        private static Text Label(Transform parent, string name, string value, float left, float top, float width, float height, int size,
            Color color, bool bold = false, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var rect = Rect(parent, name, left, top, width, height);
            var text = PrototypeUI.Label(name + " Text", rect, value, Vector2.zero, new Vector2(width, height), size, color, align);
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.resizeTextForBestFit = true;
            text.resizeTextMaxSize = size;
            text.resizeTextMinSize = Mathf.Max(13, size - 8);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = value.Contains("\n") ? VerticalWrapMode.Truncate : VerticalWrapMode.Overflow;
            return text;
        }

        private static RawImage Artwork(Transform parent, string name, string path, float left, float top, float width, float height, bool crop = false)
        {
            var image = Rect(parent, name, left, top, width, height).gameObject.AddComponent<RawImage>();
            image.texture = Resources.Load<Texture2D>(path);
            image.raycastTarget = false;
            if (crop && image.texture != null)
            {
                float sourceAspect = (float)image.texture.width / image.texture.height;
                float targetAspect = width / height;
                float uvWidth = .91f * Mathf.Min(1, targetAspect / sourceAspect);
                float uvHeight = .91f * Mathf.Min(1, sourceAspect / targetAspect);
                image.uvRect = new UnityEngine.Rect((1 - uvWidth) / 2, (1 - uvHeight) / 2, uvWidth, uvHeight);
            }
            return image;
        }

        private static void Frame(Transform parent, string name, float left, float top, float width, float height, Color color)
        {
            Panel(parent, name + " Top", left, top, width, 1, color);
            Panel(parent, name + " Bottom", left, top + height - 1, width, 1, color);
            Panel(parent, name + " Left", left, top, 1, height, color);
            Panel(parent, name + " Right", left + width - 1, top, 1, height, color);
        }

        private static void BuildHeader(Transform root, SettlementResult result)
        {
            Color accent = Accent(result);
            string theme = !result.victory ? "failure" : result.endingId == "ENDING_002" ? "mist" : result.endingId == "ENDING_003" ? "stars" : "dawn";
            Artwork(root, "Ending Artwork", "SettlementUI/Header_" + theme, 0, 112, 1080, 328);
            var mark = Artwork(root, "Brand Mark", "SettlementUI/BrandMark", 60, 39, 36, 36);
            mark.color = accent;
            Panel(root, "Brand Underline", 62, 79, 30, 2, accent);
            Label(root, "Brand", "符文远征录", 111, 31, 350, 44, 28, White, true);
            Label(root, "Report Caption", "R U N  R E P O R T", 734, 38, 247, 32, 18, Muted);
            Panel(root, "Status Light", 1004, 43, 14, 14, accent);
            CutPanel(root, "Outcome Tag", 63, 150, 252, 35, accent);
            string endingNumber = result.victory ? (result.Ending.priority + 1).ToString("00") : "";
            Label(root, "Outcome", result.victory ? "结局 " + endingNumber + " · 远征通关" : "生命耗尽 · 远征失败", 80, 151, 229, 33, 21, Navy, true);
            Label(root, "Ending Title", result.Title, 57, 198, result.victory ? 655 : 570, 106, result.victory ? 66 : 96, White, true);
            string story = !result.victory ? "生命耗尽，本次远征结束。\n整备队伍，再次出发。"
                : theme == "mist" ? "王冠碎裂，黑雾散尽。\n英雄们终于走出暗影岛。"
                : theme == "stars" ? "星界重归平静，契约得到回应。\n英雄与群星见证新的旅程。"
                : "巨像收起双翼，英雄凯旋。\n符文之地迎来新的黎明。";
            Panel(root, "Story Accent", 64, 311, 5, 58, accent);
            Label(root, "Ending Story", story, 87, 301, 570, 77, 23, PrototypeUI.Hex("C3C9CB"));
            string english = !result.victory ? "EXPEDITION FAILED" : theme == "mist" ? "THE MIST RECEDES" : theme == "stars" ? "PACT AMONG THE STARS" : "DAWN OF RUNETERRA";
            Label(root, "Ending Caption", english, 65, 384, 650, 40, 21, accent);
            Panel(root, "Banner Rule", 60, 440, 960, 2, accent);
            string[] labels = { "挑战难度", "到达楼层", "本局用时" };
            string[] values = { "难度 " + result.difficulty, "第 " + result.floor + " 层", result.Duration };
            for (int column = 0; column < labels.Length; column++)
            {
                float left = 64 + column * 330;
                Label(root, "Metric Label " + column, labels[column], left, 455, 114, 43, 20, Muted);
                Label(root, "Metric Value " + column, values[column], left + 120, 451, 171, 49, 29, White, true);
                if (column < 2) Panel(root, "Metric Divider " + column, left + 298, 461, 1, 32, Border);
            }
        }

        private static void Section(Transform root, float top, string number, string title, string detail)
        {
            Panel(root, title + " Number Background", 60, top + 2, 39, 33, PrototypeUI.Hex("D8DBD9"));
            Label(root, title + " Number", number, 60, top + 1, 39, 35, 19, Navy, false, TextAnchor.MiddleCenter);
            Label(root, title + " Heading", title, 115, top - 5, 400, 47, 31, White, true);
            Label(root, title + " Count", detail, 607, top, 413, 42, 20, Muted, false, TextAnchor.MiddleRight);
            Panel(root, title + " Rule", 60, top + 51, 960, 1, Border);
        }

        private static void BuildCharacters(Transform root, SettlementResult result)
        {
            Section(root, 539, "01", "出战角色", "最终阵容  /  " + result.characters.Count(id => !string.IsNullOrEmpty(id)) + " 人");
            for (int slot = 0; slot < 6; slot++)
            {
                float left = 60 + slot % 3 * 328, top = 610 + slot / 3 * 256;
                var card = Rect(root, "Settlement Character " + slot, left, top, 304, 238);
                Panel(card, "Card Background", 0, 0, 304, 238, PrototypeUI.Hex("16212B"));
                string id = result.characters[slot];
                if (!string.IsNullOrEmpty(id))
                {
                    var unit = GameContent.Characters.Get(id);
                    var artwork = Artwork(card, "Character Portrait", unit.imagePath, 0, 0, 304, 238, true);
                    artwork.color = result.victory ? Color.white : new Color(.72f, .72f, .72f, 1);
                    Artwork(card, "Portrait Shade", "SettlementUI/PortraitShade", 0, 0, 304, 238);
                    Label(card, "Character Name", unit.name, 18, 159, 270, 49, 31, White, true);
                    Label(card, "Character Rarity Stars", new string('★', (int)unit.rarity + 1), 18, 205, 140, 26, 22, Accent(result));
                    Label(card, "Character Quality", RarityRules.Name(unit.rarity), 189, 207, 97, 25, 17, Muted, false, TextAnchor.MiddleRight);
                }
                else
                {
                    Label(card, "Empty Slot Symbol", "+", 115, 66, 74, 66, 58, Border, false, TextAnchor.MiddleCenter);
                    Label(card, "Empty Slot", "未上阵", 18, 166, 270, 44, 26, Muted, false, TextAnchor.MiddleCenter);
                }
                Frame(card, "Card Frame", 0, 0, 304, 238, PrototypeUI.Hex("49535A"));
                CutPanel(card, "Slot Tag", 0, 0, 68, 51, PrototypeUI.Hex("15202A"));
                Panel(card, "Slot Accent", 0, 0, 4, 41, Accent(result));
                Label(card, "Slot Number", (slot + 1).ToString("00"), 18, 4, 50, 42, 23, White);
            }
        }

        private static void BuildItems(Transform root, SettlementResult result)
        {
            Section(root, 1148, "02", "携带道具", "最终持有  /  " + result.ItemCount.ToString("N0") + " 件" + (result.items.Length > 6 ? " · 左右滑动" : ""));
            var area = Panel(root, "Settlement Items", 60, 1220, 960, 190, Color.clear);
            area.raycastTarget = true;
            var scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = result.items.Length > 6;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 80;
            var viewport = Rect(area.transform, "Items Viewport", 0, 0, 960, 190);
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport;
            var content = Rect(viewport, "Items Content", 0, 0, Mathf.Max(960, result.items.Length * 165 - 25), 190);
            content.pivot = new Vector2(0, 1);
            content.anchoredPosition = Vector2.zero;
            scroll.content = content;
            for (int index = 0; index < result.items.Length; index++)
            {
                var item = result.items[index];
                var record = GameContent.Items.Get(item.id);
                var cell = Rect(content, "Settlement Item " + item.id, 4 + index * 165, 0, 132, 186);
                Artwork(cell, "Item Icon", record.imagePath, 4, 4, 124, 124, true);
                Frame(cell, "Icon Frame", 0, 0, 132, 132, PrototypeUI.Hex("4D5458"));
                Panel(cell, "Item Accent", 0, 129, 132, 3, Accent(result));
                Panel(cell, "Quantity Backdrop", 65, 101, 67, 28, Navy);
                Label(cell, "Item Quantity", "×" + item.quantity.ToString("N0"), 65, 99, 60, 32, 20, White, false, TextAnchor.MiddleRight);
                Label(cell, "Item Name", record.name, -10, 141, 152, 42, record.name.Length > 4 ? 21 : 23, PrototypeUI.Hex("C7CDCF"), false, TextAnchor.MiddleCenter);
                if (record.kind == "剧情")
                {
                    Panel(cell, "Story Item Tag", 0, 0, 57, 25, Accent(result));
                    Label(cell, "Story Item Caption", "剧情", 0, 0, 57, 25, 15, Navy, true, TextAnchor.MiddleCenter);
                }
            }
            if (result.items.Length == 0) Label(content, "No Items", "本次远征未携带道具", 0, 55, 960, 60, 28, Muted, false, TextAnchor.MiddleCenter);
            if (scroll.horizontal)
            {
                var track = Panel(root, "Items Scrollbar", 64, 1418, 952, 5, Border);
                var handle = PrototypeUI.Panel("Handle", track.transform, Vector2.zero, Vector2.zero, Accent(result));
                var bar = track.gameObject.AddComponent<Scrollbar>();
                bar.direction = Scrollbar.Direction.LeftToRight;
                bar.handleRect = handle.rectTransform;
                bar.targetGraphic = handle;
                scroll.horizontalScrollbar = bar;
            }
            scroll.horizontalNormalizedPosition = 0;
        }

        private static void BuildScore(Transform root, SettlementResult result)
        {
            Color accent = Accent(result);
            CutPanel(root, "Score Panel", 60, 1451, 960, 276, PrototypeUI.Hex("1D2933"), true);
            Panel(root, "Score Accent", 60, 1451, 6, 276, accent);
            Panel(root, "Score Top Rule", 80, 1451, 909, 1, PrototypeUI.Hex("53616A"));
            Label(root, "Score Number", "03", 90, 1470, 40, 39, 22, accent);
            Label(root, "Score Heading", "总评分", 136, 1464, 131, 50, 29, White, true);
            Label(root, "Score Caption", "通用 " + result.CommonScore.ToString("N0") + " × 难度系数 " + result.DifficultyFactor.ToString("0.00"), 274, 1477, 460, 31, 18, Muted);
            Label(root, "Total Score", result.TotalScore.ToString("N0"), 87, 1502, 641, 142, 118, White);
            Label(root, "Score Grade", result.Grade, 785, 1490, 145, 163, 144, accent, false, TextAnchor.MiddleCenter);
            Label(root, "Score Grade Name", result.GradeName, 918, 1596, 81, 39, 23, accent, false, TextAnchor.MiddleCenter);
            Panel(root, "Grade Divider", 757, 1514, 1, 124, PrototypeUI.Hex("4C5962"));
            Panel(root, "Score Detail Rule", 90, 1650, 900, 1, PrototypeUI.Hex("40505A"));
            string[] labels = { "探索", "战斗", "通关" };
            int[] scores = { result.ExplorationScore, result.BattleScore, result.EndingScore };
            for (int column = 0; column < labels.Length; column++)
            {
                float left = 91 + column * 303;
                Label(root, "Score Detail Label " + column, labels[column], left, 1670, 98, 42, 22, Muted);
                Label(root, "Score Detail Value " + column, scores[column].ToString("N0"), left + 100, 1667, 131, 45, 28, White, false, TextAnchor.MiddleRight);
                if (column < 2) Panel(root, "Score Detail Divider " + column, left + 266, 1675, 1, 28, PrototypeUI.Hex("40505A"));
            }
        }

        private static void BuildFooter(Transform root, SettlementResult result, Action home, Action restart)
        {
            var homeImage = Panel(root, "Return Home", 60, 1774, 333, 88, PrototypeUI.Hex("17222B"));
            Frame(root, "Return Home Frame", 60, 1774, 333, 88, PrototypeUI.Hex("58646D"));
            AddButton(homeImage, "返回主界面", White, home);
            var shareImage = Panel(root, "Share Settlement", 418, 1774, 177, 88, PrototypeUI.Hex("17222B"));
            Frame(root, "Share Settlement Frame", 418, 1774, 177, 88, Accent(result));
            shareImage.raycastTarget = true;
            var shareButton = shareImage.gameObject.AddComponent<Button>();
            shareButton.targetGraphic = shareImage;
            shareButton.transition = Selectable.Transition.None;
            shareButton.interactable = false;
            Label(shareImage.transform, "Caption", "分享", 12, 12, 153, 64, 31, Accent(result), true, TextAnchor.MiddleCenter);
            var restartImage = CutPanel(root, "Restart Adventure", 620, 1774, 400, 88, Accent(result));
            AddButton(restartImage, "再来一局", Navy, restart);
            Label(restartImage.transform, "Restart Arrow", "→", 328, 13, 55, 60, 40, Navy, false, TextAnchor.MiddleCenter);
            string footer = string.IsNullOrEmpty(result.dailyDate) ? "本次远征已结束"
                : "每日挑战 " + result.dailyDate + " · 任务完成 " + result.dailyCompletedTasks + "/" + result.dailyTaskCount;
            Label(root, "Settlement Footer", footer, 60, 1875, 960, 38, 17, PrototypeUI.Hex("5C6976"), false, TextAnchor.MiddleCenter);
        }

        private static void AddButton(Image image, string caption, Color textColor, Action action)
        {
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            UiAudioFeedback.Bind(button, SoundCue.Confirm);
            var colors = button.colors;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(.75f, .75f, .75f, 1);
            button.colors = colors;
            Label(image.transform, "Caption", caption, 12, 12, image.rectTransform.sizeDelta.x - 24, 64, 31, textColor, true, TextAnchor.MiddleCenter);
            if (action != null) button.onClick.AddListener(() => action());
        }
    }
}
