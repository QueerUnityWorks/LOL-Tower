using System;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    public static class SettingsPresentation
    {
        private static Text Label(string name, Transform root, string value, Vector2 pos, Vector2 size,
            int fontSize, Color tint, bool bold = false, TextAnchor align = TextAnchor.MiddleLeft)
            => InventoryPresentation.Label(name, root, value, pos, size, fontSize, tint, bold, align);

        private static Image Panel(string name, Transform root, Vector2 pos, Vector2 size, Color tint, Color border)
        {
            var panel = InventoryPresentation.Panel(name, root, pos, size, tint, border);
            panel.pixelsPerUnitMultiplier = 2;
            panel.transform.Find("Frame").GetComponent<Image>().pixelsPerUnitMultiplier = 2;
            return panel;
        }

        private static Button Button(string name, Transform root, string caption, Vector2 pos, Vector2 size, Action click, bool primary = false)
        {
            var panel = Panel(name, root, pos, size, primary ? PrototypeUI.Hex("CFAD70") : InventoryPresentation.Surface, MapPresentation.Gold);
            panel.raycastTarget = true;
            var button = panel.gameObject.AddComponent<Button>();
            UiAudioFeedback.Bind(button);
            button.targetGraphic = panel;
            button.onClick.AddListener(() => click());
            var label = Label("Label", panel.transform, caption, Vector2.zero, size - new Vector2(24, 8), 24,
                primary ? MapPresentation.Navy : MapPresentation.Ivory, true, TextAnchor.MiddleCenter);
            InventoryPresentation.FitLine(label, 18);
            return button;
        }

        private static RectTransform Shell(Transform root, string name, string title, string subtitle, Action close, out RectTransform paper)
        {
            var shade = PrototypeUI.Panel(name, root, Vector2.zero, new Vector2(900, 1600), new Color(.02f, .05f, .08f, .94f));
            shade.raycastTarget = true;
            paper = Panel("Panel", shade.transform, Vector2.zero, new Vector2(852, 1460), MapPresentation.Navy, MapPresentation.Gold).rectTransform;
            PrototypeUI.Panel("Header Accent", paper, new Vector2(-390, 657), new Vector2(3, 90), MapPresentation.Gold);
            Label("Eyebrow", paper, "金铲铲之战 / 远征作战室", new Vector2(-76, 696), new Vector2(576, 28), 17, MapPresentation.Gold);
            Label("Title", paper, title, new Vector2(-76, 645), new Vector2(576, 64), 38, MapPresentation.Ivory, true);
            Label("Subtitle", paper, subtitle, new Vector2(0, 582), new Vector2(780, 38), 20, MapPresentation.Muted);
            Button("Close Panel", paper, "返回", new Vector2(326, 653), new Vector2(128, 58), close);
            PrototypeUI.Panel("Header Rule", paper, new Vector2(0, 546), new Vector2(780, 1), InventoryPresentation.Border);
            return shade.rectTransform;
        }

        private static void Section(Transform root, string name, string title, string number, float y)
        {
            Label(name + " Number", root, number, new Vector2(-344, y), new Vector2(48, 36), 20, MapPresentation.Gold, true);
            Label(name + " Heading", root, title, new Vector2(-64, y), new Vector2(484, 40), 26, MapPresentation.Ivory, true);
        }

        private static void Unavailable(Transform root, string name, Vector2 position)
        {
            var tag = Panel(name, root, position, new Vector2(124, 34), InventoryPresentation.Field, PrototypeUI.Hex("405362"));
            Label("Status", tag.transform, "暂未开放", Vector2.zero, new Vector2(116, 30), 17, MapPresentation.Muted, false, TextAnchor.MiddleCenter);
        }

        private static void SoundRow(Transform parent, string title, float y, bool music)
        {
            Label(title + " Caption", parent, title, new Vector2(-170, y + 24), new Vector2(370, 40), 24, MapPresentation.Ivory);
            float volume = music ? GameAudio.MusicVolume : GameAudio.EffectsVolume;
            var value = Label(title + " Value", parent, Mathf.RoundToInt(volume * 100) + "%", new Vector2(291, y + 24), new Vector2(124, 36), 22, MapPresentation.Cyan, false, TextAnchor.MiddleRight);
            var control = PrototypeUI.Panel(music ? "Music Volume" : "Effects Volume", parent, new Vector2(0, y - 26), new Vector2(706, 48), Color.clear);
            control.raycastTarget = true;
            PrototypeUI.Panel("Track", control.transform, Vector2.zero, new Vector2(680, 6), PrototypeUI.Hex("314A59"));
            var fillArea = PrototypeUI.Rect("Fill Area", control.transform, Vector2.zero, new Vector2(680, 6));
            var fill = PrototypeUI.Panel("Fill", fillArea, Vector2.zero, Vector2.zero, MapPresentation.Gold);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            var handleArea = PrototypeUI.Rect("Handle Area", control.transform, Vector2.zero, new Vector2(680, 34));
            var handle = PrototypeUI.Panel("Handle", handleArea, Vector2.zero, new Vector2(26, 0), MapPresentation.Ivory);
            handle.raycastTarget = true;
            var slider = control.gameObject.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.wholeNumbers = false;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.SetValueWithoutNotify(volume);
            slider.onValueChanged.AddListener(amount =>
            {
                if (music) GameAudio.MusicVolume = amount;
                else GameAudio.EffectsVolume = amount;
                value.text = Mathf.RoundToInt(amount * 100) + "%";
            });
        }

        public static RectTransform BuildSettings(RectTransform root, Action close, Action instructions)
        {
            var overlay = Shell(root, "Settings Overlay", "冒险设置", "声音、画面与操作说明", close, out var paper);
            Panel("Sound Panel", paper, new Vector2(0, 350), new Vector2(780, 332), InventoryPresentation.Surface, InventoryPresentation.Border);
            Section(paper, "Sound", "声音", "01", 470);
            SoundRow(paper, "背景音乐", 382, true);
            SoundRow(paper, "游戏音效", 270, false);

            Panel("Display Panel", paper, new Vector2(0, -41), new Vector2(780, 398), InventoryPresentation.Surface, InventoryPresentation.Border);
            Section(paper, "Display", "画面与操作", "02", 110);
            Label("Quality Caption", paper, "画面品质", new Vector2(-170, 23), new Vector2(370, 42), 24, MapPresentation.Ivory);
            Unavailable(paper, "Quality Status", new Vector2(291, 23));
            PrototypeUI.Panel("Display Divider 1", paper, new Vector2(0, -24), new Vector2(706, 1), PrototypeUI.Hex("2D4553"));
            Label("Effects Caption", paper, "战斗特效", new Vector2(-170, -70), new Vector2(370, 42), 24, MapPresentation.Ivory);
            Label("Effects Value", paper, "默认开启", new Vector2(134, -70), new Vector2(146, 36), 18, MapPresentation.Muted, false, TextAnchor.MiddleRight);
            Unavailable(paper, "Effects Status", new Vector2(291, -70));
            PrototypeUI.Panel("Display Divider 2", paper, new Vector2(0, -117), new Vector2(706, 1), PrototypeUI.Hex("2D4553"));
            Label("Language Caption", paper, "界面语言", new Vector2(-170, -165), new Vector2(370, 42), 24, MapPresentation.Ivory);
            Label("Language Value", paper, "简体中文", new Vector2(249, -165), new Vector2(210, 42), 22, MapPresentation.Cyan, false, TextAnchor.MiddleRight);

            var help = Button("Gameplay Instructions", paper, "", new Vector2(0, -350), new Vector2(780, 148), instructions);
            help.GetComponentInChildren<Text>().gameObject.SetActive(false);
            Label("Help Title", help.transform, "玩法说明", new Vector2(-46, 32), new Vector2(610, 46), 27, MapPresentation.Gold, true);
            Label("Help Hint", help.transform, "布阵与索敌、属性克制、探索与进度保存", new Vector2(-46, -26), new Vector2(610, 38), 20, MapPresentation.Muted);
            Label("Help Arrow", help.transform, "›", new Vector2(336, 0), new Vector2(48, 64), 40, MapPresentation.Gold, false, TextAnchor.MiddleCenter);
            Label("Settings Availability", paper, "音量自动保存，拖到 0% 可静音\n画质与特效开关暂未开放", new Vector2(0, -480), new Vector2(780, 60), 18, MapPresentation.Muted, false, TextAnchor.MiddleCenter);
            Button("Done", paper, "完成", new Vector2(0, -568), new Vector2(780, 78), close, true);
            Label("Footer", paper, "金铲铲之战 / 符文远征", new Vector2(0, -665), new Vector2(780, 32), 17, MapPresentation.Dim, false, TextAnchor.MiddleCenter);
            return overlay;
        }

        public static RectTransform BuildInstructions(RectTransform root, Action back)
        {
            var overlay = Shell(root, "Instructions Overlay", "玩法说明", "从启程到结局，了解远征的基本规则", back, out var paper);
            var scroll = InventoryPresentation.Scroll("Instructions", paper, new Vector2(0, -5), new Vector2(780, 1050), 736);
            string[] titles = { "启程与难度", "布阵与战斗", "普通攻击如何索敌", "职业默认技能目标", "特殊技能目标", "路线与探索", "伙伴与道具", "保存与继续" };
            string[] bodies =
            {
                "在主城底栏选择 1～10 级难度，开局后固定。\n难度越高，敌人的生命、攻击、防御与速度越高。",
                "战斗前可布置最多六名角色，分为前排与后排。点击候补再点阵位可上阵，长按卡片可拖动换位。\n双方每场战斗初始技力为 2/3；普通攻击积攒 1 点技力，满 3 点后，下次行动自动释放技能，释放后技力归零。\n火克草、草克水、水克火，克制伤害增加 25%。",
                "敌我双方遵循相同规则，只选择存活目标。\n优先顺序：前排优先 → 同列优先 → 左、中、右。敌方前排全部阵亡后，才会攻击后排。\n例如：我方中列行动时，敌方前排中间已阵亡、左右仍存活，会先攻击前排左侧，而非后排中间。\n每一击都会重新索敌。二连击首击击杀目标后，第二击会重新选择目标。\n索敌不考虑画面距离、属性克制或仇恨值。",
                "战士：按普通攻击规则选择一个敌人。\n法师：攻击普通索敌目标所在的一整排存活敌人。\n射手：攻击普通索敌目标所在的一整列存活敌人，可同时命中前后排；该列取决于选中的目标。\n守护：治疗并为生命百分比最低的存活友军添加护盾，同时为自己添加护盾。若目标就是自己，护盾只添加一次。",
                "角色配置了特殊技能时，优先使用特殊技能的目标规则。\n最弱敌人：攻击生命百分比最低的存活敌人，可越过前排；比较的是剩余生命比例，而非生命数值。\n全体攻击／治疗：作用于所有存活敌人／友军。\n穿甲：按普通攻击规则选择单体，伤害忽略防御。\n最低生命比例相同时，按前排左、中、右，再后排左、中、右的顺序选择。守护技能选择友军时也遵循此规则。",
                "沿地图连线前进，进入战斗、事件或商店。获胜后选择奖励，再前往下一站。\n击败第三层首领可达成结局，特殊契约可解锁第四层。",
                "通过奖励、事件和商店收集伙伴与道具。\n特殊事件的选择可能改变最终首领，每局仅可进入一次。持有契约后继续战斗，收集碎片可推进隐藏结局。",
                "冒险进度自动保存在本机，地图可返回主城。\n继续冒险会恢复本局进度；战斗中退出则回到布阵。\n放弃会清空本局进度，并在成就中留下冒险记录。"
            };
            float top = 16;
            for (int i = 0; i < titles.Length; i++)
            {
                var panel = Panel("Rule " + i, scroll.content, Vector2.zero, new Vector2(724, 180), InventoryPresentation.Surface, InventoryPresentation.Border);
                panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = new Vector2(.5f, 1);
                var title = Label("Rule Title", panel.transform, (i + 1).ToString("00") + "  " + titles[i], Vector2.zero, new Vector2(674, 42), 25, MapPresentation.Gold, true);
                var body = Label("Rule Body", panel.transform, bodies[i], Vector2.zero, new Vector2(674, 100), 23, MapPresentation.Ivory, false, TextAnchor.UpperLeft);
                float textHeight = body.preferredHeight + body.fontSize * 1.6f;
                float height = textHeight + 98;
                panel.rectTransform.sizeDelta = new Vector2(724, height);
                panel.rectTransform.anchoredPosition = new Vector2(0, -top - height / 2);
                panel.transform.Find("Frame").GetComponent<RectTransform>().sizeDelta = panel.rectTransform.sizeDelta;
                title.rectTransform.anchoredPosition = new Vector2(0, height / 2 - 36);
                body.rectTransform.sizeDelta = new Vector2(674, textHeight);
                body.rectTransform.anchoredPosition = new Vector2(0, height / 2 - 72 - textHeight / 2);
                top += height + 20;
            }
            scroll.content.sizeDelta = new Vector2(736, Mathf.Max(scroll.viewport.rect.height, top + 8));
            InventoryPresentation.Top(scroll);
            Button("Back To Settings", paper, "返回设置", new Vector2(0, -623), new Vector2(780, 76), back, true);
            return overlay;
        }
    }
}
