using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElementalTactics.Editor
{
    public static class PrototypeSceneBuilder
    {
        private const string Root = "Assets/ElementalTactics";

        [MenuItem("Tools/Elemental Tactics/Build Prototype Scenes")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before rebuilding prototype scenes.");
            if (SceneManager.GetActiveScene().isDirty) throw new System.InvalidOperationException("Save the current scene before rebuilding.");
            PrototypeUI.font = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/NotoSansCJKsc-Regular.otf");
            if (PrototypeUI.font == null) throw new System.InvalidOperationException("Missing bundled Chinese font.");
            EnsureFolder(Root + "/Scenes");
            CreateStart();
            MainSceneBuilder.Build();
            CreateMap();
            CreateLevel();
            string[] paths = { ScenePath("StartScene"), ScenePath("MainScene"), ScenePath("MapScene"), ScenePath("LevelScene") };
            EditorBuildSettings.scenes = paths.Select(path => new EditorBuildSettingsScene(path, true))
                .Concat(EditorBuildSettings.scenes.Where(scene => !paths.Contains(scene.path))).ToArray();
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.defaultScreenWidth = PlayerSettings.defaultWebScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = PlayerSettings.defaultWebScreenHeight = 1920;
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ScenePath("StartScene"));
            Debug.Log("Elemental Tactics: Japanese adventure scenes rebuilt at 1080 x 1920 portrait. Open StartScene and press Play.");
        }

        [MenuItem("Tools/Elemental Tactics/Rebuild Start Scene")]
        public static void RebuildStartScene()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before rebuilding the start scene.");
            if (SceneManager.GetActiveScene().isDirty) throw new System.InvalidOperationException("Save the current scene before rebuilding.");
            PrototypeUI.font = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/NotoSansCJKsc-Regular.otf");
            if (PrototypeUI.font == null) throw new System.InvalidOperationException("Missing bundled Chinese font.");
            EnsureFolder(Root + "/Scenes");
            CreateStart();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Elemental Tactics/Rebuild Battle Scene")]
        public static void RebuildBattleScene()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before rebuilding the battle scene.");
            if (SceneManager.GetActiveScene().isDirty) throw new System.InvalidOperationException("Save the current scene before rebuilding.");
            PrototypeUI.font = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/NotoSansCJKsc-Regular.otf");
            if (PrototypeUI.font == null) throw new System.InvalidOperationException("Missing bundled Chinese font.");
            EnsureFolder(Root + "/Scenes");
            CreateLevel();
            AssetDatabase.SaveAssets();
            Debug.Log("Elemental Tactics: battle scene rebuilt with a four-column vertical roster.");
        }

        private static string ScenePath(string name) { return Root + "/Scenes/" + name + ".unity"; }

        private static void EnsureFolder(string path)
        {
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        private static RectTransform NewScene(string name)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            camera.GetComponent<Camera>().orthographic = true;
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor = PrototypeUI.Background;
            var light = new GameObject("Directional Light", typeof(Light));
            light.GetComponent<Light>().type = LightType.Directional;
            light.GetComponent<Light>().intensity = 0.8f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            return PrototypeUI.CreateCanvas();
        }

        private static void Save(string name)
        {
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath(name)))
                throw new System.InvalidOperationException("Failed to save " + name);
        }

        private static void CreateStart()
        {
            var root = NewScene("StartScene");
            for (int index = root.childCount - 1; index >= 0; index--) Object.DestroyImmediate(root.GetChild(index).gameObject);
            root.GetComponentInParent<Canvas>().transform.Find("Backdrop").GetComponent<Image>().color = PrototypeUI.Hex("111E35");
            var landscape = PrototypeUI.Rect("Start Forest", root, Vector2.zero, new Vector2(900, 1600)).gameObject.AddComponent<StartScreenArt>();
            landscape.layer = StartScreenArt.Layer.Landscape;
            landscape.raycastTarget = false;
            Color ivory = PrototypeUI.Hex("F2EDDA");
            Color gold = PrototypeUI.Hex("C7A665");
            Color mist = PrototypeUI.Hex("C4D4E6");
            PrototypeUI.Label("Start Eyebrow", root, "弈 士 集 结", new Vector2(0, 692), new Vector2(400, 35), 19, gold);
            PrototypeUI.Panel("Start Eyebrow Left", root, new Vector2(-168, 692), new Vector2(55, 1), gold);
            PrototypeUI.Panel("Start Eyebrow Right", root, new Vector2(168, 692), new Vector2(55, 1), gold);
            var heading = PrototypeUI.Label("Start Title", root, "金铲铲之战", new Vector2(0, 607), new Vector2(760, 128), 76, PrototypeUI.Hex("F5D58B"));
            heading.gameObject.AddComponent<Shadow>().effectColor = new Color(1, 0.98f, 0.8f, 0.8f);
            heading.GetComponent<Shadow>().effectDistance = new Vector2(0, -4);
            PrototypeUI.Label("Start Subtitle", root, "符 文 远 征 录", new Vector2(0, 524), new Vector2(510, 52), 29, gold);
            PrototypeUI.Panel("Start Title Rule", root, new Vector2(0, 471), new Vector2(122, 1), new Color(gold.r, gold.g, gold.b, 0.6f));
            var hero = PrototypeUI.Rect("Start Rimuru", root, new Vector2(0, -80), new Vector2(560, 440)).gameObject.AddComponent<StartScreenArt>();
            hero.layer = StartScreenArt.Layer.Spatula;
            hero.raycastTarget = false;
            var motes = PrototypeUI.Rect("Start Magic Motes", root, Vector2.zero, new Vector2(900, 1600)).gameObject.AddComponent<StartScreenArt>();
            motes.layer = StartScreenArt.Layer.Motes;
            motes.raycastTarget = false;
            PrototypeUI.Label("Start Invitation", root, "召集英雄，让策略点亮棋盘。", new Vector2(0, -399), new Vector2(730, 44), 22, mist);
            var navigation = new GameObject("Scene Navigation").AddComponent<SceneNavigation>();
            var buttonArt = PrototypeUI.Rect("Start Game", root, new Vector2(0, -535), new Vector2(620, 108)).gameObject.AddComponent<StartScreenArt>();
            buttonArt.layer = StartScreenArt.Layer.Button;
            buttonArt.raycastTarget = true;
            var start = buttonArt.gameObject.AddComponent<Button>();
            start.targetGraphic = buttonArt;
            var colors = start.colors;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.08f);
            colors.pressedColor = new Color(0.69f, 0.84f, 0.86f);
            colors.fadeDuration = 0.12f;
            start.colors = colors;
            PrototypeUI.Label("Label", start.transform, "开始游戏", new Vector2(0, 4), new Vector2(480, 68), 34, ivory);
            PrototypeUI.Label("Start Journey Note", root, "新的远征，从艾欧尼亚启程", new Vector2(0, -635), new Vector2(640, 36), 18, mist);
            PrototypeUI.Label("Start Footer", root, "金铲铲之战 · 符文远征", new Vector2(0, -737), new Vector2(740, 30), 14, PrototypeUI.Hex("90A5BF"));
            UnityEventTools.AddPersistentListener(start.onClick, navigation.OpenMain);
            Save("StartScene");
        }

        private static void CreateMap()
        {
            var root = NewScene("MapScene");
            var navigation = new GameObject("Scene Navigation").AddComponent<SceneNavigation>();
            PrototypeUI.Frame(root, "远征　·　第一章", "艾欧尼亚林地", "沿连线探索森林，点击节点查看远征情报。");
            Save("MapScene");
        }

        private static void CreateLevel()
        {
            var root = NewScene("LevelScene");
            var battle = new GameObject("Battle Controller").AddComponent<BattleController>();
            battle.playerRoster = GameContent.Characters.rows.Take(6).Select(entry => entry.Create()).ToList();
            battle.enemyRoster = GameContent.Enemies.rows.Take(6).Select(entry => entry.Create()).ToList();
            battle.playerLineup = GameContent.Characters.rows.Take(6).Select(entry => entry.Create()).ToList();
            battle.playerCards = new CardView[6];
            battle.enemyCards = new CardView[6];
            var preparationRoot = PrototypeUI.Rect("Preparation Interface", root, Vector2.zero, Vector2.zero);
            PrototypeUI.Label("Section", preparationRoot, "金铲铲之战　·　远征作战室", new Vector2(-130, 754), new Vector2(530, 38), 21, PrototypeUI.Vermilion, TextAnchor.MiddleLeft);
            PrototypeUI.Label("Title", preparationRoot, "编成远征小队", new Vector2(-143, 710), new Vector2(500, 52), 37, PrototypeUI.Indigo, TextAnchor.MiddleLeft);
            battle.phaseLabel = PrototypeUI.Label("Phase", preparationRoot, "战前布阵", new Vector2(265, 714), new Vector2(220, 40), 20, PrototypeUI.Gold, TextAnchor.MiddleRight);
            PrototypeUI.Label("Element Legend", preparationRoot, "火克草　·　草克水　·　水克火　／　克制伤害＋25%", new Vector2(-86, 658), new Vector2(605, 37), 19, PrototypeUI.Muted);
            battle.enemyButton = PrototypeUI.Button("Enemy Intelligence", preparationRoot, "敌方情报", new Vector2(332, 658), new Vector2(174, 48), false);
            PrototypeUI.PaperPanel("Selection Plaque", preparationRoot, new Vector2(0, 567), new Vector2(818, 124));
            PrototypeUI.Label("Selection Prompt", preparationRoot, "从下方选择同行者，点击空阵位即可布阵。", new Vector2(0, 570), new Vector2(770, 40), 19, PrototypeUI.Muted);
            PrototypeUI.PaperPanel("Formation Field", preparationRoot, new Vector2(0, 300), new Vector2(846, 388));
            PrototypeUI.Label("Formation Field Label", preparationRoot, "我方阵形　·　六个位置", new Vector2(0, 472), new Vector2(720, 30), 18, PrototypeUI.Gold);

            var formation = new GameObject("Formation Controller").AddComponent<FormationController>();
            formation.battle = battle;
            formation.availableRoster = GameContent.Characters.rows.Select(entry => entry.Create()).ToList();
            formation.slotCards = new CardView[6];
            formation.slotButtons = new Button[6];
            formation.reserveCards = new CardView[formation.availableRoster.Count];
            formation.reserveStatus = new Text[formation.availableRoster.Count];
            formation.previewCards = new CardView[6];
            formation.preparationRoot = preparationRoot.gameObject;
            formation.enemyButton = battle.enemyButton;
            battle.formation = formation;
            battle.combatRoot = PrototypeUI.Rect("Combat Interface", root, Vector2.zero, Vector2.zero).gameObject;
            battle.combatRoot.SetActive(false);
            BuildBattleScreen(battle);

            float[] columns = { -252, 0, 252 };
            for (int slot = 0; slot < 6; slot++)
            {
                float x = columns[slot % 3];
                float y = slot < 3 ? 365 : 190;
                formation.slotCards[slot] = PrototypeUI.CreateFormationCard(preparationRoot, "Formation Slot " + (slot + 1), new Vector2(x, y), formation.availableRoster[0]);
                formation.slotCards[slot].gameObject.SetActive(false);
                formation.slotButtons[slot] = PrototypeUI.Button("Open Slot " + (slot + 1), preparationRoot, "＋", new Vector2(x, y), new Vector2(212, 146), false);
                formation.slotButtons[slot].image.color = new Color(PrototypeUI.Paper.r, PrototypeUI.Paper.g, PrototypeUI.Paper.b, 0.82f);
                formation.slotButtons[slot].GetComponentInChildren<Text>().fontSize = 40;
                formation.slotButtons[slot].GetComponentInChildren<Text>().color = PrototypeUI.Indigo;
                PrototypeUI.SetEmptyFormationSlot(formation.slotButtons[slot]);
                battle.playerCards[slot] = PrototypeUI.CreateCard(battle.combatRoot.transform, "Ally in Battle " + (slot + 1), Vector2.zero, formation.availableRoster[0]);
                battle.enemyCards[slot] = PrototypeUI.CreateCard(battle.combatRoot.transform, "Enemy in Battle " + (slot + 1), Vector2.zero, battle.enemyRoster[slot]);
            }
            PrototypeUI.Label("Front Row", preparationRoot, "前排", new Vector2(-391, 365), new Vector2(42, 28), 14, PrototypeUI.Vermilion);
            PrototypeUI.Label("Rear Row", preparationRoot, "后排", new Vector2(-391, 190), new Vector2(42, 28), 14, PrototypeUI.Vermilion);

            BuildRosterScroll(formation, preparationRoot);
            formation.teamCount = PrototypeUI.Label("Team Count", preparationRoot, "出战队伍　1 ／ 6", new Vector2(-250, 48), new Vector2(280, 34), 21, PrototypeUI.Indigo, TextAnchor.MiddleLeft);
            PrototypeUI.Label("Roster Label", preparationRoot, "候补同行者　·　上下滑动浏览", new Vector2(170, 48), new Vector2(440, 34), 19, PrototypeUI.Muted, TextAnchor.MiddleRight);
            formation.hint = PrototypeUI.Label("Formation Hint", preparationRoot, "点击候补角色，再点击阵位上阵；点击两个阵位可以交换。", new Vector2(0, -552), new Vector2(790, 62), 18, PrototypeUI.Vermilion);
            battle.formationHint = formation.hint;
            BuildCharacterInfo(formation, preparationRoot);
            battle.backButton = PrototypeUI.Button("Back to Map", preparationRoot, "← 返回地图", new Vector2(332, 760), new Vector2(174, 44), false);
            battle.backButton.GetComponentInChildren<Text>().fontSize = 20;
            battle.startButton = PrototypeUI.Button("Start Battle", preparationRoot, "开始战斗　→", new Vector2(0, -649), new Vector2(412, 76));
            battle.resetButton = PrototypeUI.Button("Reset Formation", preparationRoot, "重置布阵", new Vector2(316, -649), new Vector2(180, 76), false);
            PrototypeUI.Label("Preparation Footer", preparationRoot, "选择同行者，从这里开始你的冒险。", new Vector2(0, -744), new Vector2(760, 31), 17, PrototypeUI.Muted);
            BuildEnemyReport(formation, preparationRoot);
            BattlePresentation.ApplyLayout(battle);
            Save("LevelScene");
        }

        private static void BuildRosterScroll(FormationController formation, Transform parent)
        {
            PrototypeUI.PaperPanel("Candidate Cards Paper", parent, new Vector2(0, -210), new Vector2(846, 588));
            var viewport = PrototypeUI.Rect("Roster Viewport", parent, new Vector2(-12, -232), new Vector2(792, 478));
            var viewportMask = viewport.gameObject.AddComponent<Image>();
            viewportMask.color = new Color(PrototypeUI.Paper.r, PrototypeUI.Paper.g, PrototypeUI.Paper.b, 0.62f);
            viewportMask.raycastTarget = true;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            const int columns = 4;
            const float columnSpacing = 196;
            const float rowSpacing = PrototypeUI.ReserveRowSpacing;
            int rows = Mathf.CeilToInt(formation.availableRoster.Count / (float)columns);
            float contentHeight = Mathf.Max(viewport.rect.height, rows * rowSpacing + 24);
            var content = PrototypeUI.Rect("Roster Scroll Content", viewport, Vector2.zero, new Vector2(792, contentHeight));
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0, 1);
            content.anchoredPosition = Vector2.zero;
            for (int index = 0; index < formation.reserveCards.Length; index++)
            {
                float x = 102 + index % columns * columnSpacing;
                float y = PrototypeUI.ReserveFirstRowY - index / columns * rowSpacing;
                var card = PrototypeUI.CreateFormationCard(content, "Reserve Card " + (index + 1), new Vector2(x, y), formation.availableRoster[index], 180);
                card.Rect.anchorMin = card.Rect.anchorMax = new Vector2(0, 1);
                formation.reserveCards[index] = card;
                var status = PrototypeUI.Label("Reserve Status " + (index + 1), content, "", new Vector2(x, y + PrototypeUI.ReserveStatusOffset), new Vector2(180, 24), 16, PrototypeUI.Muted);
                status.rectTransform.anchorMin = status.rectTransform.anchorMax = new Vector2(0, 1);
                formation.reserveStatus[index] = status;
            }
            var scrollbar = PrototypeUI.Rect("Candidate Slider", parent, new Vector2(400, -232), new Vector2(20, 478));
            var track = scrollbar.gameObject.AddComponent<Image>();
            track.color = PrototypeUI.Hex("A7C3D0");
            var slider = scrollbar.gameObject.AddComponent<Scrollbar>();
            slider.direction = Scrollbar.Direction.BottomToTop;
            slider.SetValueWithoutNotify(1);
            var handle = PrototypeUI.Rect("Handle", scrollbar, Vector2.zero, Vector2.zero);
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = Vector2.one;
            handle.offsetMin = new Vector2(2, 2);
            handle.offsetMax = new Vector2(-2, -2);
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = PrototypeUI.Vermilion;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.size = Mathf.Clamp01(viewport.rect.height / content.rect.height);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.scrollSensitivity = 35;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.verticalScrollbar = slider;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scroll.verticalNormalizedPosition = 1;
            formation.rosterScroll = scroll;
        }

        private static void BuildCharacterInfo(FormationController formation, Transform parent)
        {
            var panel = PrototypeUI.PaperPanel("Character Detail Window", parent, new Vector2(0, 567), new Vector2(818, 122));
            panel.raycastTarget = true;
            formation.informationWindow = panel.gameObject;
            formation.informationAccent = PrototypeUI.Panel("Detail Element Accent", panel.transform, new Vector2(-394, 0), new Vector2(5, 102), PrototypeUI.Vermilion);
            formation.informationEmblem = PrototypeUI.Rect("Detail Role Emblem", panel.transform, new Vector2(-356, 0), new Vector2(52, 60)).gameObject.AddComponent<RoleEmblem>();
            formation.informationEmblem.raycastTarget = false;
            formation.informationTitle = PrototypeUI.Label("Detail Name", panel.transform, "", new Vector2(3, 39), new Vector2(710, 34), 23, PrototypeUI.Vermilion, TextAnchor.MiddleLeft);
            formation.informationStats = PrototypeUI.Label("Detail Statistics", panel.transform, "", new Vector2(8, 7), new Vector2(690, 29), 16, PrototypeUI.Indigo, TextAnchor.MiddleLeft);
            formation.informationSkill = PrototypeUI.Label("Detail Skill", panel.transform, "", new Vector2(0, -28), new Vector2(720, 40), 16, PrototypeUI.Indigo, TextAnchor.MiddleLeft);
            formation.closeInformationButton = PrototypeUI.Button("Close Detail", panel.transform, "×", new Vector2(372, 45), new Vector2(43, 39), false);
            formation.closeInformationButton.GetComponentInChildren<Text>().fontSize = 22;
            formation.removeButton = PrototypeUI.Button("Remove Selected", panel.transform, "撤下", new Vector2(363, -42), new Vector2(76, 35), false);
            formation.removeButton.GetComponentInChildren<Text>().fontSize = 15;
            panel.gameObject.SetActive(false);
        }

        private static void BuildEnemyReport(FormationController formation, Transform parent)
        {
            var overlay = PrototypeUI.Panel("Enemy Intelligence Popup", parent, Vector2.zero, new Vector2(900, 1600), new Color(PrototypeUI.Indigo.r, PrototypeUI.Indigo.g, PrototypeUI.Indigo.b, 0.88f));
            overlay.raycastTarget = true;
            formation.enemyOverlay = overlay.gameObject;
            PrototypeUI.PaperPanel("Scout Report", overlay.transform, new Vector2(0, 0), new Vector2(830, 1060));
            PrototypeUI.Label("Scout Eyebrow", overlay.transform, "海克斯终端　·　敌方解析", new Vector2(0, 469), new Vector2(690, 38), 20, PrototypeUI.Vermilion);
            PrototypeUI.Label("Scout Heading", overlay.transform, "艾欧尼亚林地的威胁", new Vector2(0, 422), new Vector2(700, 51), 32, PrototypeUI.Indigo);
            PrototypeUI.Label("Scout Rear", overlay.transform, "后排", new Vector2(-375, 160), new Vector2(62, 26), 14, PrototypeUI.Vermilion);
            PrototypeUI.Label("Scout Front", overlay.transform, "前排", new Vector2(-375, -94), new Vector2(62, 26), 14, PrototypeUI.Vermilion);
            float[] columns = { -252, 0, 252 };
            for (int slot = 0; slot < formation.previewCards.Length; slot++)
            {
                float y = slot < 3 ? -94 : 160;
                formation.previewCards[slot] = PrototypeUI.CreateFormationCard(overlay.transform, "Scouted Enemy " + (slot + 1), new Vector2(columns[slot % 3], y), formation.battle.enemyRoster[slot], PrototypeUI.ScoutCardWidth);
            }
            formation.enemyDetails = PrototypeUI.Label("Enemy Detail Text", overlay.transform, "点击敌方角色，查看技能与被动特性。", new Vector2(0, -340), new Vector2(740, 132), 18, PrototypeUI.Indigo);
            formation.closeEnemyButton = PrototypeUI.Button("Return to Formation", overlay.transform, "返回布阵", new Vector2(0, -456), new Vector2(530, 71));
            overlay.gameObject.SetActive(false);
        }

        private static void BuildBattleScreen(BattleController battle)
        {
            Transform parent = battle.combatRoot.transform;
            PrototypeUI.Panel("Combat Wash", parent, Vector2.zero, new Vector2(900, 1600), new Color(PrototypeUI.Paper.r, PrototypeUI.Paper.g, PrototypeUI.Paper.b, 0.89f));
            PrototypeUI.Label("Combat Chapter", parent, "符文远征　·　战况解析", new Vector2(-130, 756), new Vector2(500, 40), 21, PrototypeUI.Vermilion, TextAnchor.MiddleLeft);
            battle.phaseLabel = PrototypeUI.Label("Combat Phase", parent, "自动战斗", new Vector2(-140, 711), new Vector2(480, 49), 34, PrototypeUI.Indigo, TextAnchor.MiddleLeft);
            battle.roundLabel = PrototypeUI.Label("Combat Round", parent, "回合　01", new Vector2(272, 710), new Vector2(210, 41), 19, PrototypeUI.Vermilion, TextAnchor.MiddleRight);
            battle.speedButton = PrototypeUI.Button("Combat Speed", parent, "1×", new Vector2(366, 758), new Vector2(85, 42), false);
            PrototypeUI.Label("Enemy Combat Banner", parent, "敌方　·　后排", new Vector2(0, 606), new Vector2(720, 28), 17, PrototypeUI.Vermilion);
            PrototypeUI.Label("Enemy Front Banner", parent, "前排", new Vector2(-386, 260), new Vector2(58, 28), 14, PrototypeUI.Vermilion);
            PrototypeUI.Label("Enemy Rear Banner", parent, "后排", new Vector2(-386, 478), new Vector2(58, 28), 14, PrototypeUI.Vermilion);
            PrototypeUI.Panel("Combat Divider", parent, new Vector2(0, 0), new Vector2(805, 2), PrototypeUI.Gold);
            battle.actionLabel = PrototypeUI.Label("Action Announcement", parent, "六位伙伴的旅途，从这里开始。", new Vector2(0, 48), new Vector2(760, 72), 24, PrototypeUI.Vermilion);
            battle.detailLabel = PrototypeUI.Label("Battle Rule Hint", parent, "火克草　·　草克水　·　水克火", new Vector2(0, -5), new Vector2(720, 35), 17, PrototypeUI.Muted);
            PrototypeUI.Label("Player Combat Banner", parent, "我方　·　后排", new Vector2(0, -481), new Vector2(720, 28), 17, PrototypeUI.Indigo);
            PrototypeUI.Label("Player Front Banner", parent, "前排", new Vector2(-386, -126), new Vector2(58, 28), 14, PrototypeUI.Indigo);
            PrototypeUI.Label("Player Rear Banner", parent, "后排", new Vector2(-386, -346), new Vector2(58, 28), 14, PrototypeUI.Indigo);
            PrototypeUI.Label("Combat Footer", parent, "每一次攻击，都会改变战局。", new Vector2(0, -750), new Vector2(740, 34), 17, PrototypeUI.Muted);
            var result = PrototypeUI.Panel("Battle Result", parent, Vector2.zero, new Vector2(900, 1600), new Color(PrototypeUI.Indigo.r, PrototypeUI.Indigo.g, PrototypeUI.Indigo.b, 0.94f));
            result.raycastTarget = true;
            battle.resultPanel = result.gameObject;
            PrototypeUI.PaperPanel("Battle Result Card", result.transform, Vector2.zero, new Vector2(720, 630));
            PrototypeUI.Label("Result Eyebrow", result.transform, "旅途记录　·　战斗完成", new Vector2(0, 225), new Vector2(630, 40), 21, PrototypeUI.Vermilion);
            battle.resultTitle = PrototypeUI.Label("Result Title", result.transform, "关卡胜利", new Vector2(0, 147), new Vector2(620, 80), 47, PrototypeUI.Vermilion);
            battle.resultDetails = PrototypeUI.Label("Result Details", result.transform, "", new Vector2(0, 20), new Vector2(620, 140), 23, PrototypeUI.Indigo);
            battle.retryButton = PrototypeUI.Button("Retry Battle", result.transform, "重新布阵", new Vector2(0, -139), new Vector2(535, 76));
            battle.mapButton = PrototypeUI.Button("Leave Battle", result.transform, "返回地图", new Vector2(0, -238), new Vector2(535, 68), false);
            result.gameObject.SetActive(false);
        }
    }
}
