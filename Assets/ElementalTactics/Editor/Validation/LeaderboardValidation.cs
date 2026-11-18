using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElementalTactics.Editor
{
    public static class LeaderboardValidation
    {
        private const string Output = "Temp/LeaderboardValidation";
        private static int checks;

        [MenuItem("Tools/Elemental Tactics/Validate Leaderboard")]
        public static void Validate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在编辑模式验证排行榜。");
            Directory.CreateDirectory(Output);
            var previous = AdventureRun.Capture();
            var previousFont = PrototypeUI.font;
            checks = 0;
            try
            {
                PrototypeUI.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/ElementalTactics/Fonts/NotoSansCJKsc-Regular.otf");
                AdventureRun.ClearProfile();
                var users = LeaderboardState.BuildEntries();
                Require(users.Count == 30 && users.All(entry => !entry.IsPlayer), "未结算玩家不应占据名次。");
                Render("unranked");
                for (int difficulty = 1; difficulty <= 10; difficulty++)
                {
                    foreach (string endingId in new[] { "", "ENDING_001", "ENDING_002", "ENDING_003" })
                    {
                        var result = new SettlementResult { victory = endingId != "", endingId = endingId, cleared = 17, battles = 12, difficulty = difficulty };
                        int common = 17 * 200 + 12 * 400 + (endingId == "" ? 0 : 2000 + GameContent.Endings.Get(endingId).priority * 500);
                        Require(result.CommonScore == common && result.TotalScore == (int)Math.Round(common * AdventureDifficulty.Factor(difficulty), MidpointRounding.AwayFromZero), "总分未按合计后的通用评分乘难度系数。");
                    }
                }
                RestoreBest(17600, 10);
                var entries = LeaderboardState.BuildEntries();
                Require(entries.Single(entry => entry.IsPlayer).Rank == 1 && entries.Single(entry => entry.Id == "SIM_001").Rank == 1
                    && entries.Single(entry => entry.Id == "SIM_003").Rank == 3, "同分并列规则不正确。");
                RestoreBest(10000, 4);
                entries = LeaderboardState.BuildEntries();
                var player = entries.Single(entry => entry.IsPlayer);
                Require(player.Rank == entries.Count(entry => entry.Score > player.Score) + 1 && player.Score == 13900, "当前玩家排名或最高分计算错误。");
                Render("ranked");
                var save = JsonUtility.FromJson<AdventureSave>(JsonUtility.ToJson(AdventureRun.Capture()));
                AdventureRun.ClearProfile(); AdventureRun.Restore(save);
                Require(LeaderboardState.Best.TotalScore == 13900, "最高分无法随存档恢复。");
                AdventureRun.StartNew();
                Require(LeaderboardState.Best.TotalScore == 13900, "开启新局清除了最高分。");
                AdventureRun.Abandon();
                Require(LeaderboardState.Best.TotalScore == 13900, "放弃本局清除了最高分。");
                TutorialRun.Begin();
                var tutorialSave = JsonUtility.FromJson<AdventureSave>(JsonUtility.ToJson(AdventureRun.Capture()));
                AdventureRun.Restore(tutorialSave); TutorialRun.Exit(false);
                Require(LeaderboardState.Best.TotalScore == 13900, "教程或教程中重启清除了最高分。");
                AdventureRun.StartNew();
                LoseRun();
                Require(SettlementRun.Pending && LeaderboardState.Best.TotalScore == 13900, "较低结算分覆盖了历史最高分。");
                var legacy = new AdventureSave { history = new[] { new AdventureRecord { id = "legacy", outcome = "已通关", ending = GameContent.Endings.Get("ENDING_003").name, cleared = 21, battles = 15, difficulty = 3 } } };
                AdventureRun.Restore(legacy);
                Require(LeaderboardState.Best.TotalScore == AdventureDifficulty.Scale(13200, AdventureDifficulty.Factor(3)), "旧存档的最高分无法从历史记录恢复。");
                legacy.history[0].ending = "与暴风龙的约定";
                AdventureRun.Restore(legacy);
                Require(LeaderboardState.Best != null && LeaderboardState.Best.TotalScore == AdventureDifficulty.Scale(13200, AdventureDifficulty.Factor(3)), "旧结局名称未保留正确最高分。");
                legacy = new AdventureSave { history = Array.Empty<AdventureRecord>() };
                AdventureRun.Restore(JsonUtility.FromJson<AdventureSave>(JsonUtility.ToJson(legacy)));
                Require(LeaderboardState.Best == null, "空旧档被错误地标记为已上榜。");
                AdventureRun.StartNew();
                LoseRun();
                Require(LeaderboardState.Best != null && LeaderboardState.Best.TotalScore == 0 && LeaderboardState.BuildEntries().Any(entry => entry.IsPlayer), "首次零分结算无法上榜。");
                string message = "PASS: " + checks + " leaderboard checks. Score multipliers, ties, best-score persistence, new runs, tutorial reload, old saves, zero scores and UI verified.";
                File.WriteAllText(Output + "/result.txt", message); Debug.Log(message);
            }
            finally { AdventureRun.Restore(previous); PrototypeUI.font = previousFont; }
        }

        private static void RestoreBest(int commonScore, int difficulty)
        {
            AdventureRun.Restore(new AdventureSave { history = Array.Empty<AdventureRecord>(), personalBest = new LeaderboardBest { runId = "best", commonScore = commonScore, difficulty = difficulty } });
        }

        private static void LoseRun()
        {
            var node = MapRun.Nodes.First(entry => MapRun.CanEnter(entry.Id) && entry.Definition.type == NodeKind.Battle);
            Require(MapRun.TryEnter(node.Id), "无法进入失败测试关卡。");
            for (int attempt = 0; attempt < RunState.InitialLives; attempt++)
                Require(RunState.RecordBattleDefeat(node.Id), "测试战败未扣除生命。");
        }

        private static void Render(string filename)
        {
            var preview = EditorSceneManager.NewPreviewScene();
            RenderTexture texture = null;
            try
            {
                var canvasObject = new GameObject("Leaderboard Preview", typeof(RectTransform), typeof(Canvas));
                SceneManager.MoveGameObjectToScene(canvasObject, preview);
                var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
                var parent = (RectTransform)canvas.transform; parent.sizeDelta = new Vector2(900, 1600);
                int closes = 0;
                var root = LeaderboardPresentation.Build(parent, () => closes++);
                root.GetComponentsInChildren<Button>().Single(button => button.name == "Close Panel").onClick.Invoke();
                Require(closes == 1, "排行榜关闭按钮未绑定。");
                var scroll = root.GetComponentInChildren<ScrollRect>();
                Require(scroll.content.childCount == LeaderboardState.BuildEntries().Count, "排行榜条目缺失。");
                var cameraObject = new GameObject("Leaderboard Preview Camera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, preview);
                var camera = cameraObject.GetComponent<Camera>(); camera.scene = preview;
                camera.orthographic = true; camera.orthographicSize = 800;
                camera.transform.position = new Vector3(0, 0, -10); camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = MapPresentation.Navy; canvas.worldCamera = camera;
                texture = new RenderTexture(1080, 1920, 24); camera.targetTexture = texture;
                Canvas.ForceUpdateCanvases();
                scroll.verticalNormalizedPosition = 0; Canvas.ForceUpdateCanvases();
                Require(scroll.content.anchoredPosition.y > 100, "排行榜无法滚动到末尾。");
                InventoryPresentation.Top(scroll); camera.Render();
                var previousTexture = RenderTexture.active;
                var image = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
                try
                {
                    RenderTexture.active = texture; image.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0); image.Apply();
                    File.WriteAllBytes(Output + "/" + filename + ".png", image.EncodeToPNG());
                }
                finally { RenderTexture.active = previousTexture; UnityEngine.Object.DestroyImmediate(image); }
                camera.targetTexture = null;
            }
            finally
            {
                if (texture != null) { texture.Release(); UnityEngine.Object.DestroyImmediate(texture); }
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        private static void Require(bool condition, string message)
        {
            checks++;
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
