using System;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    // Presentation only: uses the established 900 x 1600 safe-area coordinates.
    public static class JourneyPresentation
    {
        public static Image Panel(string name, Transform root, Vector2 pos, Vector2 size, Color? fill = null, Color? edge = null)
        {
            var image = InventoryPresentation.Panel(name, root, pos, size, fill ?? InventoryPresentation.Surface, edge ?? InventoryPresentation.Border);
            image.pixelsPerUnitMultiplier = 2;
            image.transform.Find("Frame").GetComponent<Image>().pixelsPerUnitMultiplier = 2;
            return image;
        }

        public static Text Label(string name, Transform root, string value, Vector2 pos, Vector2 size, int fontSize = 23, Color? color = null,
            bool bold = false, TextAnchor align = TextAnchor.MiddleLeft)
            => InventoryPresentation.Label(name, root, value, pos, size, fontSize, color ?? MapPresentation.Ivory, bold, align);

        public static Button Button(string name, Transform root, string caption, Vector2 pos, Vector2 size, Action click = null, bool primary = false)
        {
            var image = Panel(name, root, pos, size, primary ? PrototypeUI.Hex("CFAD70") : InventoryPresentation.Surface, MapPresentation.Gold);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f);
            colors.pressedColor = new Color(.75f, .75f, .75f);
            colors.disabledColor = new Color(.48f, .52f, .56f, .85f);
            button.colors = colors;
            var label = Label("Label", image.transform, caption, Vector2.zero, size - new Vector2(20, 8), 23,
                primary ? MapPresentation.Navy : MapPresentation.Ivory, true, TextAnchor.MiddleCenter);
            InventoryPresentation.FitLine(label, 17);
            if (click != null) button.onClick.AddListener(() => click());
            return button;
        }

        public static RectTransform Shell(Transform root, string name, string title, string section, string subtitle, Action close = null)
        {
            var overlay = PrototypeUI.Panel(name, root, Vector2.zero, new Vector2(900, 1600), PrototypeUI.Hex("081522"));
            overlay.raycastTarget = true;
            PrototypeUI.Panel("Header", overlay.transform, new Vector2(0, 699), new Vector2(900, 202), MapPresentation.Navy);
            PrototypeUI.Panel("Header Accent", overlay.transform, new Vector2(-405, 710), new Vector2(3, 78), MapPresentation.Gold);
            Label("Section", overlay.transform, "金铲铲之战 / " + section, new Vector2(-70, 751), new Vector2(610, 30), 18, MapPresentation.Gold);
            var heading = Label("Title", overlay.transform, title, new Vector2(-70, 702), new Vector2(610, 62), 40, null, true);
            InventoryPresentation.FitLine(heading, 27);
            var sub = Label("Subtitle", overlay.transform, subtitle, new Vector2(-10, 642), new Vector2(790, 38), 20, MapPresentation.Muted);
            InventoryPresentation.FitLine(sub, 17);
            PrototypeUI.Panel("Header Rule", overlay.transform, new Vector2(0, 597), new Vector2(810, 1), InventoryPresentation.Border);
            if (close != null) Button("Close Panel", overlay.transform, "← 返回", new Vector2(331, 744), new Vector2(148, 54), close);
            return overlay.rectTransform;
        }

        public static void TopAnchor(RectTransform rect) => rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);

        public static string EffectName(EffectRecord effect)
        {
            if (effect.type == EffectKind.GiveCharacter) return GameContent.Characters.Get(effect.targetId).name;
            if (effect.type == EffectKind.GiveItem) return GameContent.Items.Get(effect.targetId).name;
            return effect.type == EffectKind.AddExperience ? "海克斯经验宝典" : RunState.Describe(effect);
        }

        public static string EffectKindLabel(EffectRecord effect) => effect.type == EffectKind.GiveCharacter ? "角色"
            : effect.type == EffectKind.GiveItem ? "道具" : effect.type == EffectKind.AddExperience ? "经验" : "补给";

        public static string EffectDetail(EffectRecord effect)
        {
            if (effect.type == EffectKind.GiveItem) return ItemBonuses.Summary(GameContent.Items.Get(effect.targetId), effect.amount);
            if (effect.type == EffectKind.GiveCharacter)
            {
                var unit = GameContent.Characters.Get(effect.targetId).Create();
                return unit.ElementName + " · " + unit.RoleName + " ×" + effect.amount + "\n生命 " + unit.maxHealth + "  攻击 " + unit.attack;
            }
            return effect.type == EffectKind.AddExperience ? "立即获得 " + effect.amount + " 经验\n升级提高优质掉落概率" : RunState.Describe(effect);
        }

        public static void EffectArtwork(Transform root, EffectRecord effect, Vector2 pos, Vector2 size)
        {
            PrototypeUI.Panel("Artwork Backdrop", root, pos, size, InventoryPresentation.Field);
            if (effect.type == EffectKind.GiveCharacter || effect.type == EffectKind.GiveItem)
            {
                string path = effect.type == EffectKind.GiveCharacter ? GameContent.Characters.Get(effect.targetId).imagePath : GameContent.Items.Get(effect.targetId).imagePath;
                ResourceArtwork.Create(root, "Effect Artwork", path, pos, size, effect.type == EffectKind.GiveCharacter);
            }
            else
            {
                MapPresentation.Art("Experience Emblem", root, "HexFrame", pos, new Vector2(size.y * .8f, size.y * .8f), MapPresentation.Gold);
                Label("Experience Value", root, "EXP\n+" + effect.amount, pos, size, 27, MapPresentation.Gold, true, TextAnchor.MiddleCenter);
            }
        }

        public static void Progress(Transform root, string prefix, Vector2 pos, float width, float value, Color color)
        {
            PrototypeUI.Panel(prefix + " Track", root, pos, new Vector2(width, 7), PrototypeUI.Hex("2B414E"));
            float filled = width * Mathf.Clamp01(value);
            PrototypeUI.Panel(prefix + " Fill", root, pos + new Vector2((filled - width) / 2, 0), new Vector2(filled, 7), color);
        }
    }
}
