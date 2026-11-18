using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ElementalTactics.Editor
{
    public sealed class LeaderboardWorkbookImporter : AssetPostprocessor
    {
        public const string WorkbookPath = "Assets/ElementalTactics/Data/SimulatedUsers.xlsx";
        public const string OutputPath = "Assets/ElementalTactics/Resources/Data/SimulatedUsers.json";

        [MenuItem("Tools/Elemental Tactics/Import Simulated Users")]
        public static void Import()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("请先退出运行模式，再导入排行榜。");
            var tables = GameWorkbookImporter.ReadWorkbook(WorkbookPath, "模拟用户");
            if (!tables.ContainsKey("模拟用户")) throw new InvalidOperationException("缺少模拟用户工作表");
            var users = tables["模拟用户"].Where(row => !string.IsNullOrWhiteSpace(row["id"]))
                .Select(row => new SimulatedUser {
                    id = row["id"], name = row["name"],
                    commonScore = int.Parse(row["commonScore"], CultureInfo.InvariantCulture),
                    difficulty = int.Parse(row["difficulty"], CultureInfo.InvariantCulture)
                }).ToArray();
            if (users.Length == 0 || users.Length > 1000 || users.Select(user => user.id).Distinct().Count() != users.Length
                || users.Any(user => user.id == "PLAYER" || string.IsNullOrWhiteSpace(user.name) || user.name.Length > 16
                    || user.commonScore < 0 || user.commonScore > 1000000 || user.difficulty < 1 || user.difficulty > 10))
                throw new InvalidOperationException("模拟用户的编号、昵称、通用评分或难度无效");
            string json = JsonUtility.ToJson(new SimulatedUserTable { users = users }, true);
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            if (!File.Exists(OutputPath) || File.ReadAllText(OutputPath) != json)
            {
                File.WriteAllText(OutputPath, json);
                AssetDatabase.ImportAsset(OutputPath);
            }
            Debug.Log("排行榜已导入 " + users.Length + " 名模拟用户。");
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] previous)
        {
            if (!imported.Contains(WorkbookPath) && !moved.Contains(WorkbookPath)) return;
            EditorApplication.delayCall += ImportWhenReady;
        }

        private static void ImportWhenReady()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            {
                EditorApplication.delayCall += ImportWhenReady;
                return;
            }
            try { Import(); }
            catch (Exception error) { Debug.LogException(error); }
        }
    }
}
