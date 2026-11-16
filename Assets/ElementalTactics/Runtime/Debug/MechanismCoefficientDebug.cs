using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ElementalTactics
{
    [DisallowMultipleComponent]
    public sealed class MechanismCoefficientDebug : MonoBehaviour
    {
        public Font uiFont;
        public GameObject generatedUI;
        public RectTransform DropSlot { get; private set; }
        public RectTransform DragLayer { get; private set; }
        public ScrollRect RosterScroll { get; private set; }
        public UnitRecord Selected { get; private set; }
        public ItemRecord SelectedItem { get; private set; }
        bool HasSelection => Selected != null || SelectedItem != null;
        public MechanismResult LastResult { get; private set; }
        public bool Busy { get; private set; }
        public int VisibleCount { get; private set; }
        public MechanismCoefficientSimulator.Duel Replay { get; private set; }
        public string StatusText => status == null ? "" : status.text;
        static readonly Color Gold = PrototypeUI.Hex("CFAD6F"), Ivory = PrototypeUI.Hex("F1E8D3"), Muted = PrototypeUI.Hex("8EA8B7"), Cyan = PrototypeUI.Hex("77DCE1"), PanelColor = PrototypeUI.Hex("112D3D");
        Text resultLabel, status, selectedName, originalLabel, mirrorLabel, logLabel, countLabel;
        Text subtitle, originalDefinition, mirrorDefinition, originalName, mirrorName, scope, coefficientTitle;
        RawImage selectedArt;
        Image originalBar, mirrorBar;
        InputField lower, upper, precision, trialCoefficient, search;
        Button runButton, stepButton, batchButton, previousInterval, nextInterval;
        int intervalIndex;
        RectTransform root, rosterContent;
        string category = "", selectedCategory = "角色";
        readonly List<MechanismResult> batch = new List<MechanismResult>();
        readonly List<GameObject> cards = new List<GameObject>();
        Coroutine task;
        void Start() { BuildInterface(); }
        void OnDisable() { if (task != null) StopCoroutine(task); task = null; Busy = false; }
        RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
        }
        Image Panel(string name, Transform parent, float x, float y, float w, float h, Color color, bool hit = false)
        {
            var img = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Image>(); img.color = color; img.raycastTarget = hit; return img;
        }
        Text Label(string name, Transform parent, string value, float x, float y, float w, float h, int size, Color color, bool bold = false)
        {
            var label = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Text>(); label.font = uiFont; label.fontSize = size;
            label.text = value; label.color = color; label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.alignment = TextAnchor.MiddleLeft; label.raycastTarget = false; label.supportRichText = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = h < size * 1.7f ? VerticalWrapMode.Overflow : VerticalWrapMode.Truncate; return label;
        }
        Button Button(string name, Transform parent, string title, float x, float y, float w, float h, Action action, bool primary = false)
        {
            var img = Panel(name, parent, x, y, w, h, primary ? Gold : PrototypeUI.Hex("203D4C"), true);
            var button = img.gameObject.AddComponent<Button>(); button.targetGraphic = img;
            var text = Label("Label", img.transform, title, 6, 0, w - 12, h, 25, primary ? PanelColor : Ivory, true); text.alignment = TextAnchor.MiddleCenter;
            button.onClick.AddListener(() => action()); return button;
        }
        InputField Input(string name, Transform parent, string value, float x, float y, float w, float h, bool numeric = true)
        {
            var img = Panel(name, parent, x, y, w, h, PrototypeUI.Hex("0B2231"), true); var input = img.gameObject.AddComponent<InputField>();
            input.targetGraphic = img; input.textComponent = Label("Value", img.transform, "", 10, 0, w - 20, h, 24, Ivory);
            input.contentType = numeric ? InputField.ContentType.DecimalNumber : InputField.ContentType.Standard;
            input.text = value; return input;
        }
        public void BuildInterface()
        {
            if (generatedUI != null) { generatedUI.SetActive(false); if (Application.isPlaying) Destroy(generatedUI); else DestroyImmediate(generatedUI); }
            cards.Clear(); SelectedItem = null; Selected = null; LastResult = null; Replay = null; Busy = false; category = "";
            if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            generatedUI = new GameObject("Mechanism Debug UI", typeof(RectTransform)); generatedUI.transform.SetParent(transform, false);
            var canvasObj = new GameObject("Debug Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.transform.SetParent(generatedUI.transform, false); var canvas = canvasObj.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080, 1920); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var safe = Rect("Safe Area", canvas.transform, 0, 0, 1080, 1920); safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one; safe.offsetMin = safe.offsetMax = Vector2.zero;
            root = Rect("Portrait Content", safe, 0, 0, 1080, 1920); root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f); root.anchoredPosition = Vector2.zero;
            var adapter = canvasObj.AddComponent<MobileResolutionController>(); adapter.safeArea = safe; adapter.content = root; adapter.referenceResolution = new Vector2(1080, 1920); adapter.Refresh();
            Panel("Background", root, 0, 0, 1080, 1920, PrototypeUI.Hex("0C1D2B"));
            Panel("Accent", root, 42, 42, 4, 94, Gold);
            Label("Brand", root, "金铲铲之战 / 数值调试", 66, 35, 700, 35, 23, Gold);
            Label("Heading", root, "技能机制强度实验室", 64, 76, 900, 62, 43, Ivory, true);
            subtitle = Label("Subtitle", root, "对象＋5亚索 vs 无技能镜像＋5亚索 · 6对6测算", 66, 142, 950, 38, 25, Muted);
            Panel("Rule", root, 42, 187, 996, 2, Gold);
            Panel("Subject Panel", root, 42, 211, 996, 372, PanelColor);
            Label("Subject", root, "测试对象", 66, 229, 250, 35, 25, Gold, true);
            DropSlot = Panel("Only Drop Slot", root, 70, 278, 174, 261, PrototypeUI.Hex("0B2231"), true).rectTransform;
            DropSlot.gameObject.AddComponent<Outline>().effectColor = Gold;
            selectedArt = Rect("Selected Portrait", DropSlot, 2, 2, 170, 217).gameObject.AddComponent<RawImage>(); selectedArt.raycastTarget = false; selectedArt.enabled = false;
            selectedName = Label("Slot Name", DropSlot, "+\n拖入立绘\n仅 1 个槽位", 8, 18, 158, 220, 25, Ivory, true); selectedName.alignment = TextAnchor.MiddleCenter;
            coefficientTitle = Label("Coefficient Title", root, "技能机制强度系数 · 原版胜端点", 281, 282, 700, 40, 29, Gold, true);
            resultLabel = Label("Coefficient", root, "— —", 280, 323, 565, 60, 44, Cyan, true);
            previousInterval = Button("Previous Interval", root, "上一段", 284, 384, 126, 32, () => ShowInterval(intervalIndex - 1));
            nextInterval = Button("Next Interval", root, "下一段", 426, 384, 126, 32, () => ShowInterval(intervalIndex + 1));
            Button("Clear", root, "清空", 864, 337, 142, 52, ClearSelection);
            originalDefinition = Label("Original Definition", root, "原版队：对象（保留机制）＋5个亚索", 284, 420, 710, 36, 24, Ivory);
            mirrorDefinition = Label("Mirror Definition", root, "镜像队：无技能对象×系数＋5个原始亚索", 284, 460, 720, 42, 23, Muted);
            status = Label("Status", root, "拖入角色、敌人或增益道具后开始模拟", 282, 510, 724, 60, 21, Cyan);
            Panel("Controls", root, 42, 605, 996, 107, PanelColor);
            Label("Range Label", root, "搜索范围", 65, 613, 290, 28, 20, Muted);
            lower = Input("Minimum", root, "0.10", 65, 648, 108, 48); Label("To", root, "—", 180, 648, 30, 48, 24, Muted); upper = Input("Maximum", root, "5.00", 219, 648, 108, 48);
            Label("Precision Label", root, "精度", 350, 613, 140, 28, 20, Muted); precision = Input("Precision", root, "0.001", 350, 648, 128, 48);
            runButton = Button("Run Selected", root, "开始模拟", 504, 636, 208, 58, RunSelected, true);
            stepButton = Button("Single Step", root, "单步调试", 727, 636, 142, 58, StepReplay);
            batchButton = Button("Run All", root, "全部测算", 882, 636, 132, 58, RunAll);
            Panel("Duel", root, 42, 734, 996, 276, PanelColor);
            Label("Duel Title", root, "对战过程", 65, 747, 230, 42, 26, Gold, true);
            Label("Trial Label", root, "试算系数", 615, 751, 150, 34, 20, Muted); trialCoefficient = Input("Trial Coefficient", root, "1.000", 770, 749, 155, 40);
            trialCoefficient.onEndEdit.AddListener(_ => Replay = null);
            originalName = Label("Original Name", root, "原版 A ＋ 5 亚索", 66, 795, 420, 35, 24, Ivory, true); mirrorName = Label("Mirror Name", root, "无技能镜像 A ＋ 5 亚索", 561, 795, 440, 35, 24, Ivory, true);
            Panel("Original Track", root, 66, 839, 421, 12, PrototypeUI.Hex("233B48")); Panel("Mirror Track", root, 561, 839, 421, 12, PrototypeUI.Hex("233B48"));
            originalBar = Panel("Original Health", root, 66, 839, 421, 12, PrototypeUI.Hex("65CBA8")); mirrorBar = Panel("Mirror Health", root, 561, 839, 421, 12, PrototypeUI.Hex("CB6A69"));
            originalLabel = Label("Original Stats", root, "队伍生命 — / —", 66, 852, 480, 60, 18, Muted); mirrorLabel = Label("Mirror Stats", root, "队伍生命 — / —", 561, 852, 450, 60, 18, Muted);
            logLabel = Label("Duel Log", root, "一方全队阵亡才判负；只缩放无技能镜像对象，亚索正常施法。\n取原版队获胜端点；多段取最小值，50轮超时不算胜负。", 66, 915, 940, 81, 21, Muted);
            Label("Library Title", root, "角色与道具库", 43, 1032, 380, 48, 31, Ivory, true); countLabel = Label("Count", root, "", 650, 1038, 380, 40, 22, Muted);
            Panel("Library", root, 42, 1092, 996, 740, PanelColor);
            Button("All Filter", root, "全部", 64, 1108, 116, 46, () => SetFilter("")); Button("Heroes Filter", root, "角色", 192, 1108, 116, 46, () => SetFilter("角色")); Button("Enemies Filter", root, "敌人", 320, 1108, 116, 46, () => SetFilter("敌人"));
            Button("Items Filter", root, "增益道具", 448, 1108, 172, 46, () => SetFilter("道具"));
            search = Input("Search", root, "", 653, 1108, 355, 46, false); search.placeholder = Label("Placeholder", search.transform, "搜索名称…", 10, 0, 330, 46, 22, Muted); search.onValueChanged.AddListener(_ => RefreshRoster());
            var view = Panel("Roster Viewport", root, 60, 1170, 959, 627, PrototypeUI.Hex("102535"), true).rectTransform; view.gameObject.AddComponent<RectMask2D>();
            RosterScroll = view.gameObject.AddComponent<ScrollRect>(); RosterScroll.viewport = view; RosterScroll.horizontal = false; RosterScroll.movementType = ScrollRect.MovementType.Clamped; RosterScroll.scrollSensitivity = 36;
            rosterContent = Rect("Roster Content", view, 0, 0, 945, 630); RosterScroll.content = rosterContent;
            Label("Library Hint", root, "长按立绘拖入上方槽位；短按也可选择；上下滑动浏览", 65, 1800, 960, 31, 20, Muted);
            scope = Label("Scope", root, "对象固定前排左侧；同速按槽位，同槽原队先手；仅本阵容有效。", 43, 1850, 994, 37, 21, Muted);
            Label("Footer", root, "独立调试 · 不改变冒险存档或战斗配置", 43, 1890, 990, 28, 18, Gold);
            DragLayer = Rect("Drag Layer", root, 0, 0, 1080, 1920);
            var es = new GameObject("Debug EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)); es.transform.SetParent(generatedUI.transform, false);
            RefreshRoster(); RefreshButtons();
        }
        public void SetFilter(string value) { category = value; RefreshRoster(); }
        public void RefreshRoster()
        {
            if (rosterContent == null) return;
            foreach (var card in cards) { card.SetActive(false); if (Application.isPlaying) Destroy(card); else DestroyImmediate(card); } cards.Clear();
            int row = 0; VisibleCount = 0;
            foreach (var group in new[] { "角色", "敌人" })
            {
                if (category != "" && category != group) continue;
                var units = (group == "角色" ? GameContent.Characters.rows : GameContent.Enemies.rows).Where(u => string.IsNullOrEmpty(search.text) || u.name.IndexOf(search.text, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                if (units.Length == 0) continue;
                var title = Label(group + " Header", rosterContent, group + "  " + units.Length, 8, row, 930, 37, 25, Gold, true); cards.Add(title.gameObject); row += 48;
                for (int i = 0; i < units.Length; i++)
                {
                    var unit = units[i]; int x = 9 + (i % 4) * 237, y = row + i / 4 * 326;
                    var body = Panel(unit.id, rosterContent, x, y, 206, 309, PrototypeUI.Hex("0B2231"), true); cards.Add(body.gameObject);
                    body.gameObject.AddComponent<Outline>().effectColor = Selected != null && Selected.id == unit.id ? Cyan : Gold;
                    var art = Rect("Portrait", body.transform, 2, 2, 202, 246).gameObject.AddComponent<RawImage>(); art.texture = Portrait(unit, group); art.raycastTarget = false; Fit(art);
                    var nameLabel = Label("Name", body.transform, unit.name, 9, 247, 190, 30, 21, Ivory, true);
                    nameLabel.resizeTextForBestFit = true; nameLabel.resizeTextMinSize = 12; nameLabel.resizeTextMaxSize = 21; nameLabel.verticalOverflow = VerticalWrapMode.Truncate;
                    Label("Drag Hint", body.transform, "长按拖入测试槽", 9, 278, 190, 26, 17, Muted);
                    string rarity = group == "敌人" ? unit.bossMechanic != BossMechanic.None ? "boss" : unit.rarity == Rarity.Common ? "普通" : "精英" : new[] { "普通", "稀有", "史诗" }[(int)unit.rarity];
                    Panel("Badge", body.transform, 9, 10, 69, 32, PrototypeUI.Hex("102535")); Label("Rarity", body.transform, rarity, 16, 11, 60, 30, 18, Gold, true);
                    var drag = body.gameObject.AddComponent<MechanismPortraitDrag>(); drag.owner = this; drag.unit = unit; drag.category = group; drag.art = art;
                    VisibleCount++;
                }
                row += ((units.Length + 3) / 4) * 326 + 12;
            }
            if (category == "" || category == "道具")
            {
                var items = GameContent.Items.rows.Where(MechanismCoefficientSimulator.IsBuffItem)
                    .Where(i => string.IsNullOrEmpty(search.text) || i.name.IndexOf(search.text, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                if (items.Length > 0)
                {
                    cards.Add(Label("Items Header", rosterContent, "增益道具  " + items.Length, 8, row, 930, 37, 25, Gold, true).gameObject); row += 48;
                    for (int i = 0; i < items.Length; i++)
                    {
                        var item = items[i]; int x = 9 + i % 4 * 237, y = row + i / 4 * 326;
                        var body = Panel(item.id, rosterContent, x, y, 206, 309, PrototypeUI.Hex("0B2231"), true); cards.Add(body.gameObject);
                        body.gameObject.AddComponent<Outline>().effectColor = SelectedItem != null && SelectedItem.id == item.id ? Cyan : Gold;
                        var art = Rect("Portrait", body.transform, 2, 2, 202, 246).gameObject.AddComponent<RawImage>();
                        art.texture = Resources.Load<Texture2D>(item.imagePath); art.raycastTarget = false; Fit(art);
                        var title = Label("Name", body.transform, item.name, 9, 247, 190, 30, 21, Ivory, true);
                        title.resizeTextForBestFit = true; title.resizeTextMinSize = 12; title.resizeTextMaxSize = 21;
                        Label("Drag Hint", body.transform, "持有1件 · 全队生效", 9, 278, 190, 26, 17, Muted);
                        var drag = body.gameObject.AddComponent<MechanismPortraitDrag>(); drag.owner = this; drag.item = item; drag.category = "道具"; drag.art = art;
                        VisibleCount++;
                    }
                    row += ((items.Length + 3) / 4) * 326 + 12;
                }
            }
            rosterContent.sizeDelta = new Vector2(945, Mathf.Max(627, row)); RosterScroll.verticalNormalizedPosition = 1;
            countLabel.text = VisibleCount + " 个对象 · 长按拖动";
        }
        public static Texture2D Portrait(UnitRecord unit, string group) => Resources.Load<Texture2D>("CardPortraits/" + (group == "角色" ? "Characters/" : "Enemies/") + unit.id) ?? Resources.Load<Texture2D>(unit.imagePath);
        static void Fit(RawImage image)
        {
            if (image.texture == null) return;
            float source = (float)image.texture.width / image.texture.height, target = image.rectTransform.rect.width / image.rectTransform.rect.height;
            image.uvRect = source > target ? new Rect((1 - target / source) / 2, 0, target / source, 1) : new Rect(0, (1 - source / target) / 2, 1, source / target);
        }
        public void Select(UnitRecord unit, string group)
        {
            if (Busy || unit == null) return;
            SelectedItem = null; Selected = unit; selectedCategory = group; UpdateModeLabels(); LastResult = null; Replay = null;
            selectedArt.texture = Portrait(unit, group); selectedArt.enabled = true; Fit(selectedArt);
            selectedName.rectTransform.anchoredPosition = new Vector2(7, -221); selectedName.rectTransform.sizeDelta = new Vector2(160, 36); selectedName.fontSize = 21; selectedName.text = unit.name;
            selectedName.resizeTextForBestFit = true; selectedName.resizeTextMinSize = 12; selectedName.resizeTextMaxSize = 21;
            resultLabel.fontSize = 44; resultLabel.text = "— —"; status.text = "已选择「" + unit.name + "」，点击开始模拟"; ResetBars(); RefreshButtons();
        }
        public void Select(ItemRecord item)
        {
            if (Busy || !MechanismCoefficientSimulator.IsBuffItem(item)) return;
            Selected = null; SelectedItem = item; selectedCategory = "道具"; LastResult = null; Replay = null;
            selectedArt.texture = Resources.Load<Texture2D>(item.imagePath); selectedArt.enabled = true; Fit(selectedArt);
            selectedName.rectTransform.anchoredPosition = new Vector2(7, -221); selectedName.rectTransform.sizeDelta = new Vector2(160, 36);
            selectedName.fontSize = 21; selectedName.text = item.name; selectedName.resizeTextForBestFit = true;
            selectedName.resizeTextMinSize = 12; selectedName.resizeTextMaxSize = 21;
            resultLabel.fontSize = 44; resultLabel.text = "— —";
            status.text = "已选择「" + item.name + "」，按持有1件测算";
            UpdateModeLabels(); ResetBars(); RefreshButtons();
        }
        void UpdateModeLabels()
        {
            bool item = SelectedItem != null;
            coefficientTitle.text = item ? "道具增益系数 · 亚索基础强度比值" : "技能机制强度系数 · 原版胜端点";
            subtitle.text = item ? "6亚索持有道具 vs 6亚索基础数值×系数" : "对象＋5亚索 vs 无技能镜像＋5亚索 · 6对6测算";
            originalDefinition.text = item ? "原版队：6个亚索，持有1件道具全队生效" : "原版队：对象（保留机制）＋5个亚索";
            mirrorDefinition.text = item ? "对照队：6个亚索无道具，四项基础数值×系数" : "镜像队：无技能对象×系数＋5个原始亚索";
            originalName.text = item ? "持有方 · 6 亚索" : "原版 A ＋ 5 亚索";
            mirrorName.text = item ? "未持有方 · 6 亚索×系数" : "无技能镜像 A ＋ 5 亚索";
            logLabel.text = item ? "双方亚索均保留技能；只持有待测道具1件。\n取持有方获胜端点；多段取最小值，50轮超时不算胜负。" : "一方全队阵亡才判负；只缩放无技能镜像对象，亚索正常施法。\n取原版队获胜端点；多段取最小值，50轮超时不算胜负。";
            scope.text = item ? "仅增益道具；同速按槽位，同槽持有方先手；配置变化需重测。" : "对象固定前排左侧；同速按槽位，同槽原队先手；仅本阵容有效。";
        }
        public void ClearSelection()
        {
            if (Busy) return; SelectedItem = null; Selected = null; UpdateModeLabels(); LastResult = null; Replay = null; selectedArt.enabled = false;
            selectedName.rectTransform.anchoredPosition = new Vector2(8, -18); selectedName.rectTransform.sizeDelta = new Vector2(158, 220); selectedName.fontSize = 25; selectedName.text = "+\n拖入立绘\n仅 1 个槽位";
            selectedName.resizeTextForBestFit = false;
            resultLabel.text = "— —"; status.text = "拖入角色、敌人或增益道具后开始模拟"; ResetBars(); RefreshButtons();
        }
        void ResetBars() { originalLabel.text = mirrorLabel.text = "队伍生命 — / —"; originalBar.rectTransform.sizeDelta = mirrorBar.rectTransform.sizeDelta = new Vector2(421, 12); }
        MechanismSearchSettings Settings()
        {
            var s = new MechanismSearchSettings { minimum = double.Parse(lower.text, CultureInfo.InvariantCulture), maximum = double.Parse(upper.text, CultureInfo.InvariantCulture), precision = double.Parse(precision.text, CultureInfo.InvariantCulture) }; s.Validate(); return s;
        }
        void RefreshButtons()
        {
            runButton.interactable = stepButton.interactable = HasSelection && !Busy; batchButton.interactable = !Busy;
            lower.interactable = upper.interactable = precision.interactable = !Busy;
            previousInterval.interactable = !Busy && LastResult != null && intervalIndex > 0;
            nextInterval.interactable = !Busy && LastResult != null && intervalIndex + 1 < LastResult.intervals.Length;
        }
        public void RunSelected()
        {
            if (Busy || !HasSelection) return; MechanismSearchSettings settings;
            try { settings = Settings(); } catch (Exception e) { status.text = "参数错误：" + e.Message; return; }
            Busy = true; RefreshButtons(); task = StartCoroutine(RunSearch(settings));
        }
        IEnumerator RunSearch(MechanismSearchSettings settings)
        {
            var job = SelectedItem != null ? new MechanismCoefficientSimulator.Search(SelectedItem, settings) : new MechanismCoefficientSimulator.Search(Selected, selectedCategory, settings);
            while (!job.Finished) { job.Advance(24); status.text = "正在搜索 " + (job.Progress * 100).ToString("0") + "%"; yield return null; }
            ShowResult(job.Result()); Busy = false; task = null; RefreshButtons();
        }
        public void ShowResult(MechanismResult result)
        {
            LastResult = result; intervalIndex = 0; resultLabel.fontSize = result.hasTransitions ? 54 : 30;
            if (result.hasTransitions) ShowInterval(0);
            else
            {
                resultLabel.text = result.status;
                status.text = "原胜 " + result.originalWins + " / 镜胜 " + result.mirrorWins + " / 超时 " + result.timedOutSamples;
                ResetBars(); logLabel.text = "未发现相邻有效胜负转换；可调整范围或回合上限。\n" + result.limitation;
            }
            Replay = null; RefreshButtons();
        }
        string WinnerName(int winner) => SelectedItem != null ? (winner == 0 ? "持有方胜" : "未持有方胜") : (winner == 0 ? "原队胜" : "镜队胜");
        public void ShowInterval(int index)
        {
            if (LastResult == null || index < 0 || index >= LastResult.intervals.Length) return;
            intervalIndex = index; var i = LastResult.intervals[index];
            string format = LastResult.settings.precision < .001 ? "0.0000" : "0.000";
            bool itemResult = LastResult.category == "道具";
            resultLabel.text = (itemResult ? LastResult.itemGainCoefficient : LastResult.coefficient).ToString(itemResult ? "0.000000" : format);
            status.text = "区间 " + (index + 1) + "/" + LastResult.intervals.Length + "：" + i.minimum.ToString(format) + "～" + i.maximum.ToString(format)
                + "（" + WinnerName(i.lowerWinner) + "→" + WinnerName(i.upperWinner) + "）"
                + "\n" + (itemResult ? "等效属性系数 " + LastResult.coefficient.ToString(format) : "本段原版胜端点 " + i.originalWinningCoefficient.ToString(format)) + " · 超时 " + LastResult.timedOutSamples + " 次";
            trialCoefficient.text = i.originalWinningCoefficient.ToString("0.0000", CultureInfo.InvariantCulture);
            ShowTrial(i.lowerWinner == 0 ? i.lowerEvidence : i.upperEvidence);
            Replay = null; RefreshButtons();
        }
        public void StepReplay()
        {
            if (Busy || !HasSelection) return;
            if (Replay == null || Replay.Finished)
            {
                double value;
                if (!double.TryParse(trialCoefficient.text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || double.IsNaN(value) || double.IsInfinity(value) || value <= 0 || value > 100) { status.text = "试算系数须大于0且不超过100。"; return; }
                Replay = SelectedItem != null ? new MechanismCoefficientSimulator.Duel(SelectedItem, value, CombatRules.MaxRounds, true) : new MechanismCoefficientSimulator.Duel(Selected.Create(), value, CombatRules.MaxRounds, true);
            }
            Replay.Step(); ShowTrial(Replay.Snapshot()); status.text = Replay.Finished ? Replay.TimedOut ? "本次试算超时，无胜负" : WinnerName(Replay.Winner) : "第" + Replay.Round + "轮 · 再点单步继续";
        }
        void ShowTrial(MechanismTrial t)
        {
            originalLabel.text = "队伍生命 " + t.originalHealth + "/" + t.originalMaxHealth + " · 存活 " + t.originalAlive + "/6\n对象生命 " + t.originalTeam[0].health + " · 技力 " + t.originalEnergy + " · 全队护盾 " + t.originalShield;
            mirrorLabel.text = "队伍生命 " + t.mirrorHealth + "/" + t.mirrorMaxHealth + " · 存活 " + t.mirrorAlive + "/6\n对象生命 " + t.mirrorTeam[0].health + " · 技力 " + t.mirrorEnergy + " · 全队护盾 " + t.mirrorShield;
            originalBar.rectTransform.sizeDelta = new Vector2(421 * Mathf.Clamp01((float)t.originalHealth / t.originalMaxHealth), 12);
            mirrorBar.rectTransform.sizeDelta = new Vector2(421 * Mathf.Clamp01((float)t.mirrorHealth / t.mirrorMaxHealth), 12);
            logLabel.text = string.Join("\n", t.log.Skip(Math.Max(0, t.log.Length - 2)).Select(line => line.Length > 72 ? line.Substring(0, 72) + "…" : line));
        }
        public void RunAll()
        {
            if (Busy) return; MechanismSearchSettings settings; try { settings = Settings(); } catch (Exception e) { status.text = e.Message; return; }
            Busy = true; RefreshButtons(); task = StartCoroutine(RunBatch(settings));
        }
        IEnumerator RunBatch(MechanismSearchSettings settings)
        {
            if (category == "道具" || SelectedItem != null)
            {
                yield return RunItemBatch(settings); yield break;
            }
            batch.Clear(); var units = GameContent.Characters.rows.Concat(GameContent.Enemies.rows).ToArray();
            foreach (var unit in units)
            {
                var job = new MechanismCoefficientSimulator.Search(unit, unit.id.StartsWith("HERO_") ? "角色" : "敌人", settings);
                while (!job.Finished) { job.Advance(24); status.text = "批量 " + (batch.Count + 1) + "/" + units.Length + " · " + unit.name + " " + (job.Progress * 100).ToString("0") + "%"; yield return null; }
                batch.Add(job.Result());
            }
            var report = new MechanismReport { generatedUtc = DateTime.UtcNow.ToString("o"), results = batch.ToArray() };
            string path = Path.Combine(Application.persistentDataPath, "MechanismCoefficients.json");
            try { File.WriteAllText(path, JsonUtility.ToJson(report, true)); status.text = "发现转换 " + batch.Count(r => r.hasTransitions) + "/" + batch.Count + " · JSON已保存（路径见Console）"; Debug.Log("机制系数结果：" + path); }
            catch (Exception e) { status.text = "计算完成，保存失败：" + e.Message; }
            Busy = false; task = null; RefreshButtons();
        }
        IEnumerator RunItemBatch(MechanismSearchSettings settings)
        {
            batch.Clear(); var items = GameContent.Items.rows.Where(MechanismCoefficientSimulator.IsBuffItem).ToArray();
            foreach (var item in items)
            {
                var job = new MechanismCoefficientSimulator.Search(item, settings);
                while (!job.Finished) { job.Advance(24); status.text = "道具 " + (batch.Count + 1) + "/" + items.Length + " · " + item.name + " " + (job.Progress * 100).ToString("0") + "%"; yield return null; }
                batch.Add(job.Result());
            }
            string path = Path.Combine(Application.persistentDataPath, "ItemMechanismCoefficients.json");
            try { File.WriteAllText(path, JsonUtility.ToJson(MechanismCoefficientSimulator.ItemReport(batch.ToArray()), true)); status.text = "增益道具完成 " + batch.Count + " 件 · JSON已保存（路径见Console）"; Debug.Log("道具系数结果：" + path); }
            catch (Exception e) { status.text = "计算完成，保存失败：" + e.Message; }
            Busy = false; task = null; RefreshButtons();
        }
    }
}
