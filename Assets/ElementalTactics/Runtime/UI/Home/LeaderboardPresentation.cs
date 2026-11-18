using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    public static class LeaderboardPresentation
    {
        public static RectTransform Build(RectTransform parent, Action close)
        {
            var root = JourneyPresentation.Shell(parent, "Leaderboard Overlay", "远征排行榜", "最高分排行", "本地模拟榜 · 按历史最高分排名 · 同分并列", close);
            var rule = JourneyPresentation.Panel("Score Rule", root, new Vector2(0, 505), new Vector2(810, 140));
            JourneyPresentation.Label("Formula", rule.transform, "评分 = 通用评分 × 难度系数", new Vector2(0, 28), new Vector2(744, 45), 28, MapPresentation.Gold, true);
            JourneyPresentation.Label("Common Score Rule", rule.transform, "通用评分 = 探索分 + 战斗分 + 通关分", new Vector2(0, -24), new Vector2(744, 36), 21, MapPresentation.Muted);
            Header(root, "Rank Header", "排名", -326, 90);
            Header(root, "Name Header", "弈士", -108, 320);
            Header(root, "Difficulty Header", "难度", 115, 100);
            Header(root, "Score Header", "最高分数", 279, 175, TextAnchor.MiddleRight);
            var entries = LeaderboardState.BuildEntries();
            var scroll = InventoryPresentation.Scroll("Leaderboard", root, new Vector2(0, -102), new Vector2(810, 900), 766);
            for (int index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                var row = JourneyPresentation.Panel("Leaderboard Row " + entry.Id, scroll.content, Vector2.zero, new Vector2(748, 96),
                    entry.IsPlayer ? InventoryPresentation.Selected : InventoryPresentation.Surface,
                    entry.IsPlayer ? MapPresentation.Gold : InventoryPresentation.Border);
                JourneyPresentation.TopAnchor(row.rectTransform);
                row.rectTransform.anchoredPosition = new Vector2(0, -12 - index * 108 - 48);
                Color accent = entry.IsPlayer || entry.Rank <= 3 ? MapPresentation.Gold : MapPresentation.Muted;
                JourneyPresentation.Label("Rank", row.transform, entry.Rank.ToString("00"), new Vector2(-322, 0), new Vector2(76, 60), 32, accent, true, TextAnchor.MiddleCenter);
                var name = JourneyPresentation.Label("Player Name", row.transform, entry.Name, new Vector2(-100, 18), new Vector2(330, 38), 25, entry.IsPlayer ? MapPresentation.Gold : MapPresentation.Ivory, true);
                InventoryPresentation.FitLine(name, 19);
                JourneyPresentation.Label("Score Detail", row.transform, (entry.IsPlayer ? "当前玩家" : "虚拟弈士") + " · " + entry.CommonScore.ToString("N0") + " × " + AdventureDifficulty.Factor(entry.Difficulty).ToString("0.00"), new Vector2(-70, -25), new Vector2(390, 30), 17, MapPresentation.Muted);
                JourneyPresentation.Label("Difficulty", row.transform, entry.Difficulty.ToString("00"), new Vector2(124, 15), new Vector2(80, 40), 23, MapPresentation.Muted, false, TextAnchor.MiddleCenter);
                JourneyPresentation.Label("Highest Score", row.transform, entry.Score.ToString("N0"), new Vector2(263, 13), new Vector2(180, 52), 30, accent, true, TextAnchor.MiddleRight);
            }
            scroll.content.sizeDelta = new Vector2(766, Mathf.Max(scroll.viewport.sizeDelta.y, 24 + entries.Count * 108));
            InventoryPresentation.Top(scroll);
            var mine = entries.FirstOrDefault(entry => entry.IsPlayer);
            var footer = JourneyPresentation.Panel("My Ranking", root, new Vector2(0, -659), new Vector2(810, 162), InventoryPresentation.Selected, MapPresentation.Gold);
            JourneyPresentation.Label("My Rank", footer.transform, mine == null ? "未上榜" : "第 " + mine.Rank + " 名", new Vector2(-270, 27), new Vector2(205, 56), 32, MapPresentation.Gold, true);
            JourneyPresentation.Label("My Name", footer.transform, LeaderboardState.PlayerName, new Vector2(-58, 30), new Vector2(230, 44), 26, null, true);
            JourneyPresentation.Label("My Highest Score", footer.transform, mine == null ? "暂无成绩" : mine.Score.ToString("N0"), new Vector2(253, 28), new Vector2(240, 60), 38, MapPresentation.Gold, true, TextAnchor.MiddleRight);
            JourneyPresentation.Label("My Ranking Hint", footer.transform, mine == null ? "完成一次正式远征后上榜，失败也会计分。" : "历史最高分 · 难度 " + mine.Difficulty + " · 新局与较低分不会覆盖纪录", new Vector2(0, -40), new Vector2(744, 38), 20, MapPresentation.Muted);
            return root;
        }

        private static void Header(Transform parent, string name, string text, float left, float width, TextAnchor align = TextAnchor.MiddleLeft)
            => JourneyPresentation.Label(name, parent, text, new Vector2(left, 384), new Vector2(width, 42), 22, MapPresentation.Muted, false, align);
    }
}
