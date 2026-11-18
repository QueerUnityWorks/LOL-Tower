using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElementalTactics.Editor
{
    [InitializeOnLoad]
    public static class SettlementValidation
    {
        private const string Output = "Temp/SettlementValidation";
        private static int checks;

        static SettlementValidation()
        {
            if (SessionState.GetBool("ElementalTactics.Settlement.PlayValidation", false))
                AdventurePersistence.EditorSaveDirectory = Path.GetFullPath(Output + "/PlayProfile");
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            string request = Output + "/request.txt";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            Validate();
        }

        [MenuItem("Tools/Elemental Tactics/Validate Settlement")]
        public static void Validate()
        {
            Directory.CreateDirectory(Output);
            try { File.WriteAllText(Output + "/result.txt", RunChecks()); }
            catch (Exception error) { File.WriteAllText(Output + "/result.txt", error.ToString()); Debug.LogException(error); }
        }

        public static void RunBatch()
        {
            Directory.CreateDirectory(Output);
            try { File.WriteAllText(Output + "/result.txt", RunChecks()); EditorApplication.Exit(0); }
            catch (Exception error) { File.WriteAllText(Output + "/result.txt", error.ToString()); Debug.LogException(error); EditorApplication.Exit(1); }
        }

        public static string RunChecks()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "请在编辑模式检查结算。");
            checks = 0;
            var previous = AdventureRun.Capture();
            var previousFont = PrototypeUI.font;
            try
            {
                PrototypeUI.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/ElementalTactics/Fonts/NotoSansCJKsc-Regular.otf");
                var results = new List<SettlementResult>();
                results.Add(CheckFailure());
                for (int ending = 0; ending < 3; ending++) results.Add(CheckEnding(ending));
                foreach (var result in results)
                {
                    Require(result.CommonScore == result.ExplorationScore + result.BattleScore + result.EndingScore
                        && result.TotalScore == AdventureDifficulty.Scale(result.CommonScore, result.DifficultyFactor), "通用评分与难度系数计算错误。");
                    File.WriteAllText(Output + "/" + (result.victory ? result.endingId : "failure") + ".json", JsonUtility.ToJson(result));
                    Render(result, result.victory ? result.endingId : "failure");
                }
                CheckItemOverflow(results[3]);
                CheckEmptyInventory(results[0]);
                CheckOldSave();
                string message = "PASS: " + checks + " settlement checks. Four outcomes, reward timing, hidden-floor continuation, failure retries, snapshot persistence, old saves, all item icons and UI rendering verified.";
                Debug.Log(message);
                return message;
            }
            finally { AdventureRun.Restore(previous); PrototypeUI.font = previousFont; }
        }

        private static void StartRun()
        {
            AdventureRun.ClearProfile();
            AdventureRun.SelectDifficulty(1);
            AdventureRun.StartNew();
            var effects = new[] { "HERO_002", "HERO_005", "HERO_003", "HERO_004", "HERO_010" }
                .Select(id => new EffectRecord { type = EffectKind.GiveCharacter, targetId = id, amount = 1 })
                .Concat(new[] { "ITEM_014", "ITEM_009", "ITEM_004", "ITEM_013" }.Select(id => new EffectRecord { type = EffectKind.GiveItem, targetId = id, amount = 1 }));
            Require(RunState.Apply(effects), "无法准备测试阵容。");
            var formation = RunState.RestoreFormation();
            for (int slot = 0; slot < 6; slot++) formation.Place(slot, slot);
            RunState.SaveFormation(formation);
        }

        private static SettlementResult CheckFailure()
        {
            StartRun();
            var node = MapRun.Nodes.First(entry => MapRun.CanEnter(entry.Id) && entry.Definition.type == NodeKind.Battle);
            Require(MapRun.TryEnter(node.Id), "无法进入测试战斗。");
            for (int attempt = 0; attempt < RunState.InitialLives; attempt++)
            {
                if (attempt == RunState.InitialLives - 1)
                    File.WriteAllText(Output + "/before_failure.json", JsonUtility.ToJson(AdventureRun.Capture()));
                Require(RunState.RecordBattleDefeat(node.Id), "战败扣除生命失败。");
                if (RunState.Lives > 0) Require(AdventureRun.Active && SettlementRun.Current == null, "仍可重试时不应进入整局结算。");
            }
            Require(!AdventureRun.Active && SettlementRun.Pending && !SettlementRun.Current.victory, "耗尽生命后未生成失败结算。");
            var result = SettlementRun.Current;
            Require(result.EndingScore == 0 && result.characters[5] == "HERO_010" && result.items.Length == 4, "失败阵容、道具或评分记录错误。");
            CheckPersistence(result);
            return result;
        }

        private static SettlementResult CheckEnding(int ending)
        {
            StartRun();
            if (ending > 0)
                Require(RunState.Apply(new[] { new EffectRecord { type = EffectKind.GiveItem, targetId = ending == 2 ? "ITEM_032" : "ITEM_031", amount = 1 } }), "无法准备分支道具。");
            bool continued = false;
            int steps = 0;
            while (!MapRun.Finished)
            {
                Require(++steps <= 40, "结局路线未能结束。");
                var available = MapRun.Nodes.Where(node => MapRun.CanEnter(node.Id)).ToArray();
                Require(available.Length > 0, "结局路线没有下一节点。");
                var forced = MapRun.Nodes.FirstOrDefault(node => node.Floor == MapRun.CurrentFloor && node.Definition.guaranteed);
                var next = available.FirstOrDefault(node => forced != null && node.Row <= forced.Row && node.Lane == forced.Lane)
                    ?? available.FirstOrDefault(node => node.Definition.type == NodeKind.Battle) ?? available[0];
                Require(MapRun.TryEnter(next.Id), "结局路线节点进入失败。");
                if (next.Definition.type == NodeKind.Battle)
                {
                    Require(RewardFlow.Victory(next.Id) != null, "结局路线未生成战利品。");
                    AdventureRun.FinishIfNeeded();
                    Require(SettlementRun.Current == null && AdventureRun.Active, "领奖前不应结算整局。");
                    if (next.Definition.isFinal && next.Floor == (ending == 2 ? 4 : 3))
                        File.WriteAllText(Output + "/before_ending_00" + (ending + 1) + "_claim.json", JsonUtility.ToJson(AdventureRun.Capture()));
                    Require(RewardFlow.Claim(Array.Empty<int>()), "结局路线领奖失败。");
                    if (ending == 2 && next.Floor == 3 && next.Definition.isFinal)
                    {
                        continued = true;
                        AdventureRun.FinishIfNeeded();
                        Require(!MapRun.Finished && AdventureRun.Active && SettlementRun.Current == null && MapRun.CurrentFloor == 4, "隐藏第四层被提前结算。");
                    }
                }
                else if (next.Definition.type == NodeKind.Event)
                {
                    var record = GameContent.Events.Get(next.Definition.contentId);
                    string option = record.id == "EVENT_010" ? (ending == 0 ? "OPTION_10_1" : "OPTION_10_2")
                        : record.id == "EVENT_011" ? (ending == 2 ? "OPTION_11_1" : "OPTION_11_2")
                        : record.options.First(entry => RunState.CanApply(RunState.Effects(entry))).id;
                    Require(RunState.ChooseEvent(option), "结局事件选择失败。");
                }
                else Require(MapRun.Complete(next.Id), "结局路线商店退出失败。");
            }
            AdventureRun.FinishIfNeeded();
            var result = SettlementRun.Current;
            Require(result != null && result.victory && result.endingId == "ENDING_00" + (ending + 1), "成功结局匹配错误。");
            Require(ending != 2 || continued, "未验证第三层到第四层的继续探索。");
            Require(result.cleared == MapRun.ClearedCount && result.battles > 0 && result.floor == (ending == 2 ? 4 : 3), "结算未使用实际探索记录。");
            CheckPersistence(result);
            return result;
        }

        private static void CheckPersistence(SettlementResult result)
        {
            AdventureRun.FinishIfNeeded();
            AdventureRun.FinishIfNeeded();
            Require(AdventureRun.History.Count == 1, "重复结算生成了重复历史。");
            string json = JsonUtility.ToJson(AdventureRun.Capture());
            AdventureRun.Restore(JsonUtility.FromJson<AdventureSave>(json));
            Require(SettlementRun.Pending && SettlementRun.Current.runId == result.runId && SettlementRun.Current.TotalScore == result.TotalScore, "结算不能随存档恢复。");
            Require(SettlementRun.Current.characters.SequenceEqual(result.characters) && SettlementRun.Current.ItemCount == result.ItemCount, "读档后结算阵容或道具丢失。");
            SettlementRun.Acknowledge();
            AdventureRun.Restore(JsonUtility.FromJson<AdventureSave>(JsonUtility.ToJson(AdventureRun.Capture())));
            Require(!SettlementRun.Pending, "返回主城后不应重复弹出结算。");
            AdventureRun.SelectDifficulty(4);
            AdventureRun.StartNew();
            Require(AdventureRun.Active && AdventureRun.Difficulty == 4 && SettlementRun.Current == null && InventoryState.Items.Count == 0, "再来一局没有重置远征。");
        }

        private static void CheckOldSave()
        {
            AdventureRun.ClearProfile();
            var save = new AdventureSave { history = Array.Empty<AdventureRecord>() };
            AdventureRun.Restore(JsonUtility.FromJson<AdventureSave>(JsonUtility.ToJson(save)));
            Require(!SettlementRun.Pending && SettlementRun.Current == null, "不含结算的旧存档不能恢复。");
            AdventureRun.Restore(JsonUtility.FromJson<AdventureSave>("{\"version\":1,\"selectedDifficulty\":1,\"difficulty\":1,\"active\":false,\"history\":[]}"));
            Require(!SettlementRun.Pending && SettlementRun.Current == null, "没有结算字段的旧版本存档不能恢复。");
        }

        private static void CheckItemOverflow(SettlementResult original)
        {
            var result = JsonUtility.FromJson<SettlementResult>(JsonUtility.ToJson(original));
            result.items = GameContent.Items.rows.Select(item => new ItemSnapshot { id = item.id, quantity = item.id == "ITEM_033" ? 3 : 1 }).ToArray();
            Render(result, "all_items");
        }

        private static void CheckEmptyInventory(SettlementResult original)
        {
            var result = JsonUtility.FromJson<SettlementResult>(JsonUtility.ToJson(original));
            result.items = Array.Empty<ItemSnapshot>();
            result.characters = new string[6];
            Render(result, "empty_slots");
        }

        private static void Render(SettlementResult result, string filename)
        {
            var preview = EditorSceneManager.NewPreviewScene();
            RenderTexture texture = null;
            try
            {
                var canvasObject = new GameObject("Settlement Preview", typeof(RectTransform), typeof(Canvas));
                SceneManager.MoveGameObjectToScene(canvasObject, preview);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                var parent = (RectTransform)canvas.transform;
                parent.sizeDelta = new Vector2(900, 1600);
                int homeClicks = 0, restartClicks = 0;
                var root = SettlementPresentation.Build(parent, result, () => homeClicks++, () => restartClicks++);
                Require(root.GetComponentsInChildren<RawImage>().All(image => image.texture != null), "结算图片缺失。");
                Require(root.GetComponentsInChildren<Text>().All(text => text.font != null), "结算字体缺失。");
                Require(root.GetComponentsInChildren<Text>().Any(text => text.text == result.Title), "未显示对应结局标题。");
                Require(root.GetComponentsInChildren<Text>().Any(text => text.text == result.TotalScore.ToString("N0")), "未显示实际评分。");
                var buttons = root.GetComponentsInChildren<Button>();
                Require(buttons.Length == 3 && !buttons.Single(button => button.name == "Share Settlement").interactable, "结算应有两个操作按钮和一个仅展示的分享按钮。");
                buttons.Single(button => button.name == "Return Home").onClick.Invoke();
                buttons.Single(button => button.name == "Restart Adventure").onClick.Invoke();
                Require(homeClicks == 1 && restartClicks == 1, "结算按钮未绑定。");
                var scroll = root.GetComponentInChildren<ScrollRect>();
                Require(scroll.content.childCount == (result.items.Length == 0 ? 1 : result.items.Length), "道具列表截断了实际背包。");
                Require(scroll.horizontal == (result.items.Length > 6), "道具溢出时不能横向滚动。");
                var cameraObject = new GameObject("Settlement Preview Camera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, preview);
                var camera = cameraObject.GetComponent<Camera>();
                camera.scene = preview;
                camera.orthographic = true;
                camera.orthographicSize = 800;
                camera.transform.position = new Vector3(0, 0, -10);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = PrototypeUI.Hex("101922");
                canvas.worldCamera = camera;
                texture = new RenderTexture(1080, 1920, 24);
                camera.targetTexture = texture;
                Canvas.ForceUpdateCanvases();
                foreach (string name in new[] { "Ending Title", "Total Score", "Score Grade" })
                {
                    var text = root.Find(name).GetComponentInChildren<Text>();
                    Require(text.cachedTextGenerator.vertexCount >= 4, "大字号文字未生成可见内容：" + name);
                }
                if (scroll.horizontal)
                {
                    scroll.horizontalNormalizedPosition = 1;
                    Canvas.ForceUpdateCanvases();
                    Require(scroll.content.anchoredPosition.x < -10, "列表不能滚动到末尾道具。");
                    if (filename != "all_items") scroll.horizontalNormalizedPosition = 0;
                }
                camera.Render();
                var previousTexture = RenderTexture.active;
                var image = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
                try
                {
                    RenderTexture.active = texture;
                    image.ReadPixels(new UnityEngine.Rect(0, 0, 1080, 1920), 0, 0);
                    image.Apply();
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
