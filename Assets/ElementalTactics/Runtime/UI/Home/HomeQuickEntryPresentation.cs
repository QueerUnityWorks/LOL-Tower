using System;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    public static class HomeQuickEntryPresentation
    {
        public static Button Build(Transform root, bool daily, Action open)
            => Build(root, daily, false, open);

        public static Button BuildTutorial(Transform root, Action open)
            => Build(root, false, true, open);

        private static Button Build(Transform root, bool daily, bool tutorial, Action open)
        {
            string name = tutorial ? "Tutorial Entry" : daily ? "Daily Challenge Entry" : "Leaderboard Entry";
            var position = new Vector2(tutorial ? 293 : daily ? -51 : -293, 552);
            var size = new Vector2(224, 78);
            var accent = PrototypeUI.Hex(tutorial ? "C5BBEE" : daily ? "87D8DE" : "E4C786");
            PrototypeUI.Panel(name + " Shadow", root, position + new Vector2(0, -4), size, new Color(0, 0, 0, .25f));
            var panel = JourneyPresentation.Panel(name, root, position, size,
                PrototypeUI.Hex(tutorial ? "252B45" : daily ? "102F3D" : "12283A"),
                PrototypeUI.Hex(tutorial ? "80799E" : daily ? "52858E" : "9C895E"));
            panel.raycastTarget = true;
            var button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.22f, 1.22f, 1.22f);
            colors.pressedColor = new Color(.7f, .7f, .7f);
            colors.fadeDuration = .12f;
            button.colors = colors;
            UiAudioFeedback.Bind(button);
            button.onClick.AddListener(() => open());
            PrototypeUI.Panel("Top Accent", panel.transform, new Vector2(-60, 37), new Vector2(72, 2), accent);
            var icon = JourneyPresentation.Panel("Icon Plate", panel.transform, new Vector2(-76, 0), new Vector2(46, 46),
                PrototypeUI.Hex(tutorial ? "363B59" : daily ? "18414D" : "283B45"),
                PrototypeUI.Hex(tutorial ? "6F698F" : daily ? "426E78" : "726748"));
            if (tutorial) Guidebook(icon.transform, accent);
            else if (daily) Calendar(icon.transform, accent);
            else MapPresentation.Art("Ranking Crown", icon.transform, "Crown", Vector2.zero, new Vector2(33, 26), accent);
            string title = tutorial ? TutorialRun.Active ? "继续教程" : "新手教程" : daily ? "每日挑战" : "排行榜";
            string subtitle = tutorial ? TutorialRun.Active ? "返回教学远征" : "阿狸带你入门" : daily ? "今日限定远征" : "最高分排名";
            JourneyPresentation.Label("Label", panel.transform, title, new Vector2(20, 13), new Vector2(130, 31), 22,
                PrototypeUI.Hex("F4EDD9"), true);
            JourneyPresentation.Label("Entry Subtitle", panel.transform, subtitle, new Vector2(20, -17), new Vector2(130, 23), 13,
                PrototypeUI.Hex(tutorial ? "B8B4D0" : daily ? "99C1C6" : "B6B6A5"));
            JourneyPresentation.Label("Entry Arrow", panel.transform, "›", new Vector2(99, 0), new Vector2(18, 34), 26, accent,
                false, TextAnchor.MiddleCenter);
            return button;
        }

        private static void Guidebook(Transform parent, Color accent)
        {
            var book = MainHubController.Art(parent, "Tutorial Guidebook", MainHubArt.Motif.Book,
                new Vector2(-2, -3), new Vector2(39, 37), accent);
            book.raycastTarget = false;
            var ink = PrototypeUI.Hex("363B59");
            for (int index = 0; index < 4; index++)
            {
                bool rightPage = index >= 2;
                var line = PrototypeUI.Panel("Tutorial Page Line " + index, parent,
                    new Vector2(rightPage ? 6 : -10, 2 - index % 2 * 6), new Vector2(7, 1.6f), ink);
                line.transform.localRotation = Quaternion.Euler(0, 0, rightPage ? 13 : -13);
            }
            var star = PrototypeUI.Rect("Tutorial Guiding Star", parent, new Vector2(12, 13), new Vector2(14, 14));
            PrototypeUI.Panel("Star Halo", star, Vector2.zero, new Vector2(12, 12), ink)
                .transform.localRotation = Quaternion.Euler(0, 0, 45);
            PrototypeUI.Panel("Star Vertical", star, Vector2.zero, new Vector2(2.5f, 13), PrototypeUI.Hex("F4EDD9"));
            PrototypeUI.Panel("Star Horizontal", star, Vector2.zero, new Vector2(10, 2.5f), PrototypeUI.Hex("F4EDD9"));
            PrototypeUI.Panel("Star Center", star, Vector2.zero, new Vector2(4, 4), accent)
                .transform.localRotation = Quaternion.Euler(0, 0, 45);
        }

        private static void Calendar(Transform parent, Color accent)
        {
            var frame = PrototypeUI.Rect("Calendar Icon", parent, new Vector2(0, -1), new Vector2(29, 27));
            PrototypeUI.Border(frame, new Vector2(34, 32), accent);
            PrototypeUI.Panel("Calendar Header", frame, new Vector2(0, 6), new Vector2(28, 2), accent);
            PrototypeUI.Panel("Calendar Binding Left", frame, new Vector2(-8, 14), new Vector2(3, 8), accent);
            PrototypeUI.Panel("Calendar Binding Right", frame, new Vector2(8, 14), new Vector2(3, 8), accent);
            for (int index = 0; index < 4; index++)
                PrototypeUI.Panel("Calendar Day " + index, frame, new Vector2(-6 + index % 2 * 12, 0 - index / 2 * 8),
                    new Vector2(5, 4), index == 3 ? PrototypeUI.Hex("F4EDD9") : accent);
        }
    }
}
