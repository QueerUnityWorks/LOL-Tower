using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    public static class AchievementsPresentation
    {
        public static RectTransform Build(RectTransform parent, Action close, Action history)
        {
            var root = JourneyPresentation.Shell(parent, "Achievements Overlay", "远征成就", "远征档案", "每一段旅途，都值得铭记", close);
            var records = AdventureRun.History;
            int[] totals = { records.Count + (AdventureRun.Active ? 1 : 0), records.Sum(r => r.battles),
                records.Select(r => r.heroes).DefaultIfEmpty(0).Max(), records.Select(r => r.items).DefaultIfEmpty(0).Max() };
            if (AdventureRun.Active)
            {
                totals[1] += MapRun.Nodes.Count(n => MapRun.IsCleared(n.Id) && n.Definition.type == NodeKind.Battle);
                totals[2] = Math.Max(totals[2], RunState.Characters.Select(c => c.Definition.id).Distinct().Count());
                totals[3] = Math.Max(totals[3], InventoryState.Items.Count);
            }
            int[] goals = { 1, 10, 6, 10 };
            int completed = Enumerable.Range(0, 4).Count(i => totals[i] >= goals[i]);
            var summary = JourneyPresentation.Panel("Achievement Summary", root, new Vector2(0, 464), new Vector2(810, 206), null, MapPresentation.Gold);
            MapPresentation.Art("Summary Medal", summary.transform, "Crown", new Vector2(-321, 12), new Vector2(102, 72), MapPresentation.Gold);
            JourneyPresentation.Label("Count Caption", summary.transform, "已达成成就", new Vector2(-84, 49), new Vector2(340, 40), 22, MapPresentation.Muted);
            JourneyPresentation.Label("Count", summary.transform, completed + " / 4", new Vector2(-84, -9), new Vector2(340, 70), 43, MapPresentation.Gold, true);
            JourneyPresentation.Label("Completion", summary.transform, (completed * 25) + "%", new Vector2(283, 22), new Vector2(170, 64), 35, MapPresentation.Cyan, true, TextAnchor.MiddleRight);
            JourneyPresentation.Progress(summary.transform, "Total Progress", new Vector2(0, -72), 736, completed / 4f, MapPresentation.Gold);
            JourneyPresentation.Label("List Heading", root, "远征足迹", new Vector2(0, 307), new Vector2(798, 42), 25, MapPresentation.Gold, true);
            string[] titles = { "初次启程", "棋盘的守望者", "伙伴集结", "宝物收藏家" };
            string[] descriptions = { "开启第一次冒险", "赢得 10 场战斗", "单局结识 6 位不同伙伴", "单局收集 10 种不同道具" };
            string[] icons = { "home", "battle", "role", "bag" };
            for (int i = 0; i < 4; i++)
            {
                bool done = totals[i] >= goals[i];
                var row = JourneyPresentation.Panel("Achievement " + i, root, new Vector2(0, 177 - i * 206), new Vector2(810, 184),
                    done ? InventoryPresentation.Selected : InventoryPresentation.Surface, done ? MapPresentation.Gold : InventoryPresentation.Border);
                MapPresentation.Art("Medal Frame", row.transform, "HexFrame", new Vector2(-327, 13), new Vector2(98, 98), done ? MapPresentation.Gold : MapPresentation.Dim);
                MapPresentation.Art("Medal Icon", row.transform, icons[i], new Vector2(-327, 13), new Vector2(52, 52), done ? MapPresentation.Gold : MapPresentation.Muted);
                JourneyPresentation.Label("Name", row.transform, titles[i], new Vector2(-30, 49), new Vector2(478, 46), 27, null, true);
                JourneyPresentation.Label("Description", row.transform, descriptions[i], new Vector2(-30, 0), new Vector2(478, 40), 20, MapPresentation.Muted);
                JourneyPresentation.Label("Status", row.transform, done ? "已达成" : "进行中", new Vector2(310, 46), new Vector2(136, 38), 20, done ? MapPresentation.Gold : MapPresentation.Muted, false, TextAnchor.MiddleRight);
                JourneyPresentation.Label("Progress", row.transform, Math.Min(totals[i], goals[i]) + " / " + goals[i], new Vector2(310, -34), new Vector2(136, 38), 22, done ? MapPresentation.Gold : MapPresentation.Cyan, true, TextAnchor.MiddleRight);
                JourneyPresentation.Progress(row.transform, "Achievement Progress", new Vector2(-22, -53), 486, (float)totals[i] / goals[i], done ? MapPresentation.Gold : MapPresentation.Cyan);
            }
            JourneyPresentation.Button("Adventure History", root, "查看冒险记录  ›", new Vector2(0, -615), new Vector2(810, 80), history, true);
            JourneyPresentation.Label("Achievement Footer", root, "依据最近 50 次远征与当前冒险统计", new Vector2(0, -699), new Vector2(810, 42), 19, MapPresentation.Muted, false, TextAnchor.MiddleCenter);
            return root;
        }

        public static RectTransform History(RectTransform parent, Action close, Action back)
        {
            var root = JourneyPresentation.Shell(parent, "History Overlay", "冒险记录", "远征档案", "最近 50 次远征 · 含通关、失败与放弃记录", close);
            var scroll = InventoryPresentation.Scroll("History", root, new Vector2(0, -8), new Vector2(810, 1140), 766);
            float top = 16; int index = 0;
            if (AdventureRun.Active) AddHistoryRow(scroll.content, index++, ref top, "进行中", AdventureRun.StartedAt, AdventureRun.Difficulty, MapRun.CurrentFloor, MapRun.ClearedCount, "可返回主城继续冒险");
            foreach (var record in AdventureRun.History) AddHistoryRow(scroll.content, index++, ref top, record.outcome, record.endedAt, record.difficulty, record.floor, record.cleared, record.ending);
            if (index == 0)
            {
                MapPresentation.Art("History Empty Icon", scroll.viewport, "Crown", new Vector2(0, 82), new Vector2(132, 90), MapPresentation.Gold);
                JourneyPresentation.Label("Empty History", scroll.viewport, "尚无冒险记录\n开启一段新的旅程吧", new Vector2(0, -38), new Vector2(690, 130), 27, MapPresentation.Muted, false, TextAnchor.MiddleCenter);
            }
            scroll.content.sizeDelta = new Vector2(766, Mathf.Max(scroll.viewport.rect.height, top));
            InventoryPresentation.Top(scroll);
            JourneyPresentation.Button("Back To Achievements", root, "返回成就", new Vector2(0, -677), new Vector2(810, 80), back, true);
            return root;
        }

        private static void AddHistoryRow(Transform parent, int index, ref float top, string outcome, string date, int difficulty, int floor, int cleared, string ending)
        {
            if (ending == "森林的黎明") ending = GameContent.Endings.Get("ENDING_001").name;
            else if (ending == "斩断傀儡之丝") ending = GameContent.Endings.Get("ENDING_002").name;
            else if (ending == "与暴风龙的约定") ending = GameContent.Endings.Get("ENDING_003").name;
            var row = JourneyPresentation.Panel("Run Record " + index, parent, Vector2.zero, new Vector2(748, 216));
            JourneyPresentation.TopAnchor(row.rectTransform);
            row.rectTransform.anchoredPosition = new Vector2(0, -top - 108);
            JourneyPresentation.Label("Outcome", row.transform, outcome + "  ·  难度 " + difficulty, new Vector2(0, 65), new Vector2(692, 42), 26, outcome == "已通关" ? MapPresentation.Gold : MapPresentation.Ivory, true);
            JourneyPresentation.Label("Route", row.transform, "第 " + floor + " 层 · 通过 " + cleared + " 个节点", new Vector2(0, 17), new Vector2(692, 34), 21, MapPresentation.Muted);
            JourneyPresentation.Label("Ending", row.transform, string.IsNullOrEmpty(ending) ? "旅途已记录" : ending, new Vector2(0, -27), new Vector2(692, 42), 21, MapPresentation.Cyan);
            DateTime parsed; string stamp = DateTime.TryParse(date, out parsed) ? parsed.ToLocalTime().ToString("yyyy.MM.dd  HH:mm") : "";
            JourneyPresentation.Label("Date", row.transform, stamp, new Vector2(0, -75), new Vector2(692, 30), 18, MapPresentation.Dim);
            top += 234;
        }
    }
}
