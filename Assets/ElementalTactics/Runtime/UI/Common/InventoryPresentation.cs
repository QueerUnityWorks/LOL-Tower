using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    // Shared modal styling. Coordinates are in the established 900 x 1600 safe-area canvas.
    public static class InventoryPresentation
    {
        public static readonly Color Surface = PrototypeUI.Hex("102535");
        public static readonly Color Border = PrototypeUI.Hex("5B655F");
        public static readonly Color Field = PrototypeUI.Hex("0A1B29");
        public static readonly Color Selected = PrototypeUI.Hex("193D4A");
        public static Image Panel(string name, Transform root, Vector2 pos, Vector2 size, Color color, Color edge)
        {
            var body = MapPresentation.Art(name, root, "Panel", pos, size, color);
            MapPresentation.Art("Frame", body.transform, "PanelBorder", Vector2.zero, size, edge);
            return body;
        }
        public static Text Label(string name, Transform root, string value, Vector2 pos, Vector2 size, int fontSize, Color color, bool bold = false, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var text = PrototypeUI.Label(name, root, value, pos, size, fontSize, color, align);
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
        public static void ArtworkFrame(string name, Transform root, Vector2 pos, Vector2 size, Color color)
        {
            var frame = PrototypeUI.Panel(name, root, pos, size, Field);
            PrototypeUI.Border(frame.transform, size, color);
        }
        public static void FitLine(Text text, int minimum = 16)
        {
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = minimum; text.resizeTextMaxSize = text.fontSize;
        }
        public static RectTransform Shell(Transform parent, string prefix, string title, string icon, System.Action close, out GameObject overlay, out Text summary)
        {
            var shade = PrototypeUI.Panel(prefix + " Overlay", parent, Vector2.zero, new Vector2(900,1600), new Color(.02f,.05f,.08f,.92f));
            shade.raycastTarget = true; overlay = shade.gameObject;
            var paper = Panel(prefix + " Paper", shade.transform, Vector2.zero, new Vector2(852,1460), MapPresentation.Navy, MapPresentation.Gold);
            MapPresentation.Art(prefix + " Header Icon", paper.transform, icon, new Vector2(-355,655), new Vector2(46,46), MapPresentation.Gold);
            Label(prefix + " Eyebrow", paper.transform, "符文远征 / 行囊", new Vector2(-105,694), new Vector2(415,26), 17, MapPresentation.Gold);
            Label(prefix + " Heading", paper.transform, title, new Vector2(-80,649), new Vector2(470,60), 36, MapPresentation.Ivory, true);
            var buttonBody = Panel("Close " + prefix, paper.transform, new Vector2(330,654), new Vector2(124,58), Surface, MapPresentation.Gold);
            buttonBody.raycastTarget = true;
            var button = buttonBody.gameObject.AddComponent<Button>(); button.targetGraphic = buttonBody; button.onClick.AddListener(() => close());
            Label("Label", button.transform, "关闭", Vector2.zero, new Vector2(116,48), 23, MapPresentation.Ivory, false, TextAnchor.MiddleCenter);
            summary = Label(prefix + " Summary", paper.transform, "", new Vector2(0,588), new Vector2(780,36), 21, MapPresentation.Muted);
            FitLine(summary);
            PrototypeUI.Panel(prefix + " Header Rule", paper.transform, new Vector2(0,555), new Vector2(780,1), Border);
            return paper.rectTransform;
        }
        public static ScrollRect Scroll(string prefix, Transform parent, Vector2 pos, Vector2 size, float contentWidth)
        {
            var area = Panel(prefix + " Scroll", parent, pos, size, Field, PrototypeUI.Hex("2B4656"));
            area.raycastTarget = true;
            var scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 48;
            var viewport = PrototypeUI.Rect(prefix + " Viewport", area.transform, new Vector2(-8,0), new Vector2(size.x - 28,size.y - 16));
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport;
            scroll.content = PrototypeUI.Rect(prefix + " Content", viewport, Vector2.zero, new Vector2(contentWidth,viewport.sizeDelta.y));
            scroll.content.anchorMin = scroll.content.anchorMax = scroll.content.pivot = new Vector2(.5f,1);
            var track = PrototypeUI.Panel(prefix + " Scrollbar", area.transform, new Vector2(size.x / 2 - 12,0), new Vector2(8,size.y - 32), PrototypeUI.Hex("203746"));
            track.raycastTarget = true;
            var handle = PrototypeUI.Panel(prefix + " Scroll Handle", track.transform, Vector2.zero, Vector2.zero, MapPresentation.Gold);
            handle.raycastTarget = true;
            var bar = track.gameObject.AddComponent<Scrollbar>(); bar.handleRect = handle.rectTransform; bar.targetGraphic = handle; bar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return scroll;
        }
        public static Text Description(string name, Transform root, Vector2 pos, Vector2 size, out ScrollRect scroll)
        {
            scroll = Scroll(name, root, pos, size, size.x - 44);
            var text = Label(name, scroll.content, "", Vector2.zero, new Vector2(size.x - 52, size.y), 19, MapPresentation.Muted, false, TextAnchor.UpperLeft);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = text.rectTransform.pivot = new Vector2(.5f,1);
            text.rectTransform.anchoredPosition = new Vector2(0,-8);
            return text;
        }
        public static void SetDescription(Text text, ScrollRect scroll, string value)
        {
            text.text = value;
            float height = Mathf.Max(scroll.viewport.rect.height - 16, text.preferredHeight + 6);
            text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height + 16);
            Top(scroll);
        }
        public static void Top(ScrollRect scroll)
        {
            scroll.StopMovement(); Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = 1;
        }
        public static Color Rarity(Rarity rarity) => rarity == ElementalTactics.Rarity.Epic ? PrototypeUI.Hex("CCA8FF") : rarity == ElementalTactics.Rarity.Rare ? PrototypeUI.Hex("75D3F0") : PrototypeUI.Hex("B9C9D1");
    }
}
