using System;
using System.Linq;
using UnityEngine;

namespace ElementalTactics
{
    public static class DailyChallengePresentation
    {
        public static RectTransform Build(RectTransform parent, Action close, Action start)
        {
            bool continuing = DailyChallengeState.IsDailyRun && AdventureRun.Active;
            string date = continuing ? DailyChallengeState.Current.date : DailyChallengeState.Today;
            var tasks = continuing ? DailyChallengeState.Tasks : DailyChallengeState.TasksFor(date);
            var root = JourneyPresentation.Shell(parent, "Daily Challenge Overlay", "每日挑战", "每日同图远征", date + " · 北京时间零点更新 · 固定难度10", close);
            var summary = JourneyPresentation.Panel("Daily Rules", root, new Vector2(0, 486), new Vector2(810, 176), null, MapPresentation.Gold);
            JourneyPresentation.Label("Daily Difficulty", summary.transform, "难度 10", new Vector2(-230, 36), new Vector2(280, 58), 39, MapPresentation.Gold, true);
            JourneyPresentation.Label("Daily Map", summary.transform, "统一地图 · 统一任务", new Vector2(136, 38), new Vector2(430, 48), 27, null, true);
            JourneyPresentation.Label("Daily Seed", summary.transform, "地图编号 " + DailyChallengeState.SeedFor(date) + " · 评分系数 ×2.71", new Vector2(0, -35), new Vector2(744, 40), 23, MapPresentation.Muted);
            JourneyPresentation.Label("Daily Tasks Heading", root, "今日任务 · 每天一项", new Vector2(0, 345), new Vector2(790, 44), 27, MapPresentation.Gold, true);
            for (int index = 0; index < tasks.Length; index++)
            {
                var task = tasks[index];
                var card = JourneyPresentation.Panel("Daily Task " + task.Id, root, new Vector2(0, 119), new Vector2(810, 342));
                JourneyPresentation.Label("Task Number", card.transform, "01", new Vector2(-329, 99), new Vector2(76, 56), 39, MapPresentation.Gold, true);
                JourneyPresentation.Label("Task Title", card.transform, task.Title, new Vector2(50, 96), new Vector2(636, 110), 29, null, true);
                JourneyPresentation.Label("Task Description", card.transform, task.Description, new Vector2(0, -13), new Vector2(744, 100), 24, MapPresentation.Muted);
                var progress = JourneyPresentation.Panel("Task Progress Strip", card.transform, new Vector2(0, -113), new Vector2(744, 64), PrototypeUI.Hex("0B202D"));
                JourneyPresentation.Label("Task Progress Caption", progress.transform, "当前进度", new Vector2(-259, 0), new Vector2(178, 40), 21, MapPresentation.Muted);
                JourneyPresentation.Label("Task Progress", progress.transform, continuing ? DailyChallengeState.Progress(task) : "等待开始远征", new Vector2(93, 0), new Vector2(510, 40), 23, MapPresentation.Cyan, false, TextAnchor.MiddleRight);
            }
            var record = DailyChallengeState.Records.FirstOrDefault(entry => entry.date == date);
            JourneyPresentation.Label("Daily Best", root, record == null ? "本日尚无挑战成绩" : "本日最好：" + (record.completedTasks > 0 ? "已完成" : "未完成") + " · 评分 " + record.score.ToString("N0"), new Vector2(0, -135), new Vector2(790, 44), 24, MapPresentation.Gold);
            var rules = JourneyPresentation.Panel("Daily Instructions", root, new Vector2(0, -338), new Vector2(810, 250));
            JourneyPresentation.Label("Daily Instructions Heading", rules.transform, "挑战说明", new Vector2(0, 76), new Vector2(744, 42), 26, MapPresentation.Gold, true);
            JourneyPresentation.Label("Daily Persistence", rules.transform, "每天轮换一项独立任务，达成目标并通关即可完成。\n可反复挑战，保留本日最好成绩。\n进度自动保存，跨日继续沿用开局时的地图与任务。", new Vector2(0, -22), new Vector2(744, 136), 23, MapPresentation.Muted);
            string note = TutorialRun.Active ? "请先返回主城退出新手教程，再开始每日挑战。"
                : continuing && date != DailyChallengeState.Today ? "当前为 " + date + " 的挑战，结束或放弃后可开始今日内容。"
                : "普通远征会暂存，结束每日挑战后可继续原来的进度。";
            JourneyPresentation.Label("Daily Resume Hint", root, note, new Vector2(0, -532), new Vector2(790, 70), 22, MapPresentation.Muted);
            var button = JourneyPresentation.Button("Start Daily Challenge", root, continuing ? "继续远征 · 难度10" : "开始远征 · 难度10", new Vector2(0, -652), new Vector2(810, 86), start, true);
            button.interactable = !TutorialRun.Active;
            JourneyPresentation.Label("Daily Completion Hint", root, "完成今日目标并通关，即可完成本日挑战。", new Vector2(0, -727), new Vector2(810, 36), 19, MapPresentation.Muted, false, TextAnchor.MiddleCenter);
            return root;
        }
    }
}
