using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ElementalTactics
{
    public static class PrototypeUI
    {
        public static readonly Color Background = Hex("D7DDE6");
        public static readonly Color Card = Hex("FFF8E9");
        public static readonly Color Paper = Hex("F3E9D2");
        public static readonly Color Ink = Hex("182943");
        public static readonly Color Indigo = Hex("13253E");
        public static readonly Color Vermilion = Hex("80632B");
        public static readonly Color Muted = Hex("64748B");
        public static readonly Color Gold = Hex("B68C42");
        public static readonly Color HealthBarRed = Hex("C54F4F");
        public static readonly Color SkillBarBlue = Hex("4288C7");
        public static Font font;

        public static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out Color result);
            return result;
        }

        public static Color ElementColor(Element element)
        {
            return element == Element.Fire ? Hex("B45045") : element == Element.Water ? Hex("477E96") : Hex("657D4C");
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static Image Panel(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var image = Rect(name, parent, position, size).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Text Label(string name, Transform parent, string value, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var label = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
            label.font = font;
            label.text = value;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.supportRichText = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = size.y < fontSize * 1.6f ? VerticalWrapMode.Overflow : VerticalWrapMode.Truncate;
            return label;
        }

        public static Button Button(string name, Transform parent, string caption, Vector2 position, Vector2 size, bool primary = true)
        {
            Image background = Panel(name, parent, position, size, primary ? Vermilion : Indigo);
            Border(background.transform, size, primary ? Hex("E8C778") : Hex("A88C57"));
            background.raycastTarget = true;
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f);
            colors.disabledColor = new Color(0.48f, 0.48f, 0.48f, 0.7f);
            button.colors = colors;
            Label("Label", button.transform, caption, Vector2.zero, size - new Vector2(20, 8), 27, Paper);
            return button;
        }

        public static void Border(Transform parent, Vector2 size, Color tint)
        {
            Panel("Top Border", parent, new Vector2(0, size.y / 2 - 3), new Vector2(size.x - 6, 2), tint);
            Panel("Bottom Border", parent, new Vector2(0, -size.y / 2 + 3), new Vector2(size.x - 6, 2), tint);
            Panel("Left Border", parent, new Vector2(-size.x / 2 + 3, 0), new Vector2(2, size.y - 6), tint);
            Panel("Right Border", parent, new Vector2(size.x / 2 - 3, 0), new Vector2(2, size.y - 6), tint);
        }

        public static Image PaperPanel(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var panel = Panel(name, parent, position, size, Paper);
            Border(panel.transform, size, Hex("98BBC9"));
            return panel;
        }

        public static AdventureArt Landscape(Transform parent, Vector2 position, Vector2 size)
        {
            var art = Rect("Painted Mountains", parent, position, size).gameObject.AddComponent<AdventureArt>();
            art.raycastTarget = false;
            return art;
        }

        public static RectTransform CreateCanvas()
        {
            var canvasObject = new GameObject("Portrait Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            var backdrop = Panel("Backdrop", canvas.transform, Vector2.zero, Vector2.zero, Background);
            backdrop.rectTransform.anchorMin = Vector2.zero;
            backdrop.rectTransform.anchorMax = Vector2.one;
            backdrop.rectTransform.offsetMin = backdrop.rectTransform.offsetMax = Vector2.zero;
            var safe = Rect("Safe Area", canvas.transform, Vector2.zero, Vector2.zero);
            safe.anchorMin = Vector2.zero;
            safe.anchorMax = Vector2.one;
            var content = Rect("Portrait Content", safe, Vector2.zero, new Vector2(900, 1600));
            Landscape(content, new Vector2(0, 300), new Vector2(900, 1000));
            Panel("Left Trim", content, new Vector2(-429, 0), new Vector2(2, 1530), Hex("A1C2CF"));
            Panel("Right Trim", content, new Vector2(429, 0), new Vector2(2, 1530), Hex("A1C2CF"));
            var layout = canvasObject.AddComponent<PortraitLayout>();
            layout.safeArea = safe;
            layout.content = content;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            return content;
        }

        public static void Frame(Transform root, string section, string title, string subtitle)
        {
            Panel("Top Accent", root, new Vector2(-367, 723), new Vector2(6, 63), Gold);
            Label("Section", root, section, new Vector2(15, 748), new Vector2(738, 30), 18, Gold, TextAnchor.MiddleLeft);
            Label("Title", root, title, new Vector2(15, 704), new Vector2(738, 56), 38, Ink, TextAnchor.MiddleLeft);
            Label("Subtitle", root, subtitle, new Vector2(0, 652), new Vector2(760, 36), 20, Muted, TextAnchor.MiddleLeft);
            Panel("Header Rule", root, new Vector2(0, 620), new Vector2(760, 2), Gold);
            Label("Footer", root, "金铲铲之战　／　符文远征录", new Vector2(0, -758), new Vector2(760, 30), 17, Muted);
        }

        public static CardView CreateCard(Transform root, string name, Vector2 position, CharacterDefinition definition)
        {
            var body = Panel(name, root, position, new Vector2(220, 216), Card);
            Border(body.transform, new Vector2(220, 216), Hex("8BB0BD"));
            body.raycastTarget = true;
            var view = body.gameObject.AddComponent<CardView>();
            view.body = body;
            view.group = body.gameObject.AddComponent<CanvasGroup>();
            view.button = body.gameObject.AddComponent<Button>();
            view.button.transition = Selectable.Transition.None;
            view.button.targetGraphic = body;
            view.homePosition = position;
            var selected = Panel("Selected", body.transform, Vector2.zero, new Vector2(228, 224), Gold);
            selected.transform.SetAsFirstSibling();
            var outline = selected.gameObject.AddComponent<Outline>();
            outline.effectColor = Vermilion;
            outline.effectDistance = new Vector2(2, 2);
            selected.color = new Color(Vermilion.r, Vermilion.g, Vermilion.b, 0.24f);
            outline.effectColor = Vermilion;
            view.selection = selected;
            view.selection.enabled = false;
            view.accent = Panel("Element Edge", body.transform, new Vector2(0, 106), new Vector2(220, 4), Gold);
            view.classification = Label("Class", body.transform, "", new Vector2(0, 83), new Vector2(194, 30), 20, Muted);
            view.portrait = ResourceArtwork.Create(body.transform, "Adventurer Portrait", null, new Vector2(0, 30), new Vector2(135, 76));
            view.emblem = Rect("Role Emblem", body.transform, new Vector2(83, 32), new Vector2(27, 27)).gameObject.AddComponent<RoleEmblem>();
            view.emblem.raycastTarget = false;
            view.title = Label("Name", body.transform, "", new Vector2(0, -17), new Vector2(200, 36), 24, Ink);
            Panel("Health Track", body.transform, new Vector2(0, -47), new Vector2(186, 7), Background);
            view.healthFill = Panel("Health Fill", body.transform, new Vector2(0, -47), new Vector2(186, 7), HealthBarRed);
            view.healthFill.rectTransform.pivot = new Vector2(0, 0.5f);
            view.healthFill.rectTransform.anchoredPosition = new Vector2(-93, -47);
            view.healthText = Label("Health", body.transform, "", new Vector2(-25, -66), new Vector2(140, 26), 16, Ink);
            view.shieldText = Label("Shield", body.transform, "", new Vector2(64, -65), new Vector2(78, 26), 14, ElementColor(Element.Water));
            view.energyText = Label("Energy", body.transform, "", new Vector2(0, -90), new Vector2(185, 26), 19, Muted);
            view.feedback = Label("Feedback", body.transform, "", new Vector2(0, 25), new Vector2(214, 82), 32, Ink);
            view.feedback.fontStyle = FontStyle.Bold;
            var shadow = view.feedback.gameObject.AddComponent<Shadow>();
            shadow.effectColor = Background;
            shadow.effectDistance = new Vector2(2, -2);
            SetCombatCard(view);
            view.Show(definition);
            return view;
        }

        // Fit the existing row spacing without changing the page's panels or slot centres.
        public const float FormationCardWidth = 108f;
        public const float CombatCardWidth = 136f;
        public const float ScoutCardWidth = 156f;
        public const float ReserveCardWidth = 180f;
        public const float ReserveRowSpacing = 310f;
        public const float ReserveFirstRowY = -147f;
        public const float ReserveStatusOffset = -151f;

        public static CardView CreateFormationCard(Transform root, string name, Vector2 position, CharacterDefinition definition, float width = FormationCardWidth)
        {
            var view = CreateCard(root, name, position, definition);
            SetPortraitCard(view, width);
            return view;
        }

        public static void SetCombatCard(CardView view) { SetPortraitCard(view, CombatCardWidth); }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Image CardPanel(CardView view, string name, Vector2 position, Vector2 size, Color tint)
        {
            var child = view.transform.Find(name);
            var panel = child == null ? Panel(name, view.transform, position, size, tint) : child.GetComponent<Image>();
            Place(panel.rectTransform, position, size);
            panel.color = tint;
            panel.raycastTarget = false;
            return panel;
        }

        private static void CardText(Text text, Vector2 position, Vector2 size, int fontSize, Color tint, TextAnchor alignment)
        {
            Place(text.rectTransform, position, size);
            text.fontSize = fontSize;
            text.color = tint;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            // CJK glyph line metrics are taller than compact card labels. Allow the
            // single line to render instead of silently dropping it at small sizes.
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(7, fontSize - 4);
            text.resizeTextMaxSize = fontSize;
            text.gameObject.SetActive(true);
        }

        public static void SetPortraitCard(CardView view, float width)
        {
            float height = width * 1.5f;
            float inset = width * .05f;
            float inner = width - inset * 2;
            Color navy = Hex("101E30"), gold = Hex("D9B979"), ivory = Hex("F4EAD4");
            view.Rect.sizeDelta = new Vector2(width, height);
            view.body.color = navy;
            Place(view.selection.rectTransform, Vector2.zero, new Vector2(width + 6, height + 6));
            view.selection.color = new Color(.35f, .85f, .93f, .18f);
            var outline = view.selection.GetComponent<Outline>();
            if (outline != null) { outline.effectColor = Hex("7BDBE9"); outline.effectDistance = new Vector2(2, 2); }
            view.selection.transform.SetAsFirstSibling();
            view.portrait.cropToFill = true;
            Place(view.portrait.rectTransform, new Vector2(0, height * .1f), new Vector2(width - 6, height * .8f - 6));
            view.portrait.transform.SetAsLastSibling();
            var shadeTransform = view.transform.Find("Portrait Name Shade");
            var shade = shadeTransform == null
                ? Rect("Portrait Name Shade", view.transform, Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>()
                : shadeTransform.GetComponent<RawImage>();
            shade.texture = Resources.Load<Texture2D>("CardPortraits/NameShade");
            shade.raycastTarget = false;
            Place(shade.rectTransform, new Vector2(0, -height * .17f), new Vector2(width - 6, height * .26f));
            shade.transform.SetAsLastSibling();
            CardPanel(view, "Status Panel", new Vector2(0, -height * .4f), new Vector2(width - 6, height * .2f), navy).transform.SetAsLastSibling();
            CardPanel(view, "Status Divider", new Vector2(0, -height * .3f), new Vector2(inner, 1), gold).transform.SetAsLastSibling();
            view.LayoutElementBadge();
            CardText(view.title, new Vector2(0, -height * .205f), new Vector2(inner, height * .095f), Mathf.RoundToInt(width * .12f), ivory, TextAnchor.MiddleLeft);
            view.title.fontStyle = FontStyle.Bold;
            CardText(view.classification, new Vector2(0, -height * .267f), new Vector2(inner, height * .055f), Mathf.RoundToInt(width * .073f), Hex("C2D0DB"), TextAnchor.MiddleLeft);
            view.title.transform.SetAsLastSibling();
            view.classification.transform.SetAsLastSibling();
            view.emblem.gameObject.SetActive(false);
            view.accent.gameObject.SetActive(false);
            view.EnableSkillBar();
            int barFont = Mathf.Max(9, Mathf.RoundToInt(width * .075f));
            CardText(view.healthText, new Vector2(0, -height * .329f), new Vector2(inner, height * .053f), barFont, ivory, TextAnchor.MiddleLeft);
            CardText(view.energyText, new Vector2(0, -height * .423f), new Vector2(inner, height * .053f), barFont, Hex("BBCDDD"), TextAnchor.MiddleLeft);
            CardPanel(view, "Health Track", new Vector2(0, -height * .372f), new Vector2(inner, height * .025f), Hex("392830")).transform.SetAsLastSibling();
            Place(view.healthFill.rectTransform, new Vector2(-inner / 2, -height * .372f), new Vector2(inner, height * .025f));
            view.healthFill.rectTransform.pivot = new Vector2(0, .5f);
            view.healthFill.type = Image.Type.Simple;
            view.healthFill.color = HealthBarRed;
            Place(view.energyTrack.rectTransform, new Vector2(0, -height * .465f), new Vector2(inner, height * .018f));
            view.energyTrack.color = Hex("233B53");
            Place(view.energyFill.rectTransform, new Vector2(-inner / 2, -height * .465f), new Vector2(inner, height * .018f));
            view.energyFill.rectTransform.pivot = new Vector2(0, .5f);
            view.energyFill.type = Image.Type.Simple;
            view.healthFill.transform.SetAsLastSibling();
            // Reattach the overlay after layout changes, including serialized scene cards.
            view.AlignShieldBar();
            view.energyTrack.transform.SetAsLastSibling();
            view.energyFill.transform.SetAsLastSibling();
            view.healthText.transform.SetAsLastSibling();
            view.energyText.transform.SetAsLastSibling();
            CardText(view.shieldText, new Vector2(width * .15f, height * .428f), new Vector2(width * .54f, height * .08f), barFont, Hex("B9F8FA"), TextAnchor.MiddleRight);
            view.shieldText.transform.SetAsLastSibling();
            CardText(view.feedback, new Vector2(0, height * .1f), new Vector2(inner, height * .35f), Mathf.RoundToInt(width * .18f), ivory, TextAnchor.MiddleCenter);
            var shadow = view.feedback.GetComponent<Shadow>();
            if (shadow != null) shadow.effectColor = navy;
            view.feedback.transform.SetAsLastSibling();
            CardPanel(view, "Top Border", new Vector2(0, height / 2 - 1), new Vector2(width, 2), gold).transform.SetAsLastSibling();
            CardPanel(view, "Bottom Border", new Vector2(0, -height / 2 + 1), new Vector2(width, 2), gold).transform.SetAsLastSibling();
            CardPanel(view, "Left Border", new Vector2(-width / 2 + 1, 0), new Vector2(2, height), gold).transform.SetAsLastSibling();
            CardPanel(view, "Right Border", new Vector2(width / 2 - 1, 0), new Vector2(2, height), gold).transform.SetAsLastSibling();
        }

        public static void SetEmptyFormationSlot(Button button)
        {
            Vector2 size = new Vector2(FormationCardWidth, FormationCardWidth * 1.5f);
            ((RectTransform)button.transform).sizeDelta = size;
            button.image.color = Hex("172B40");
            foreach (string name in new[] { "Top Border", "Bottom Border", "Left Border", "Right Border" })
            {
                var border = button.transform.Find(name).GetComponent<RectTransform>();
                bool horizontal = name == "Top Border" || name == "Bottom Border";
                Place(border, horizontal ? new Vector2(0, (name == "Top Border" ? 1 : -1) * (size.y / 2 - 2))
                    : new Vector2((name == "Right Border" ? 1 : -1) * (size.x / 2 - 2), 0),
                    horizontal ? new Vector2(size.x - 4, 1) : new Vector2(1, size.y - 4));
                border.GetComponent<Image>().color = Hex("9E865C");
            }
            var caption = button.GetComponentInChildren<Text>();
            Place(caption.rectTransform, Vector2.zero, size - new Vector2(12, 12));
            caption.color = Hex("D9B979");
        }
    }
}
