using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ElementalTactics.Editor
{
    public static class MechanismCoefficientTools
    {
        public const string ScenePath = "Assets/ElementalTactics/Scenes/MechanismCoefficientDebugScene.unity";
        [MenuItem("Tools/Elemental Tactics/Mechanism Coefficient/Create Debug Scene")]
        public static void BuildScene()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("请退出运行模式后创建调试场景。");
            var previous = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(previous.path)) throw new InvalidOperationException("请先保存当前未命名场景，再创建独立调试场景。");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var camera = new GameObject("Main Camera", typeof(Camera)); camera.tag = "MainCamera";
                camera.transform.position = new Vector3(0, 0, -10); camera.GetComponent<Camera>().orthographic = true;
                camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor; camera.GetComponent<Camera>().backgroundColor = PrototypeUI.Hex("0C1D2B");
                var light = new GameObject("Directional Light", typeof(Light)); light.GetComponent<Light>().type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
                var debugger = new GameObject("Mechanism Coefficient Debugger").AddComponent<MechanismCoefficientDebug>();
                debugger.uiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/ElementalTactics/Fonts/NotoSansCJKsc-Regular.otf");
                if (debugger.uiFont == null) throw new InvalidOperationException("缺少项目中文字体");
                debugger.BuildInterface();
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("调试场景保存失败");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
            Debug.Log("已创建独立调试场景：" + ScenePath);
        }
        [MenuItem("Tools/Elemental Tactics/Mechanism Coefficient/Measure All Units")]
        public static void MeasureAll()
        {
            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "MechanismCoefficients.json");
            File.WriteAllText(path, JsonUtility.ToJson(Measure(), true)); Debug.Log("机制系数实测结果：" + path);
        }
        public static MechanismReport Measure()
        {
            var results = new System.Collections.Generic.List<MechanismResult>();
            foreach (var unit in GameContent.Characters.rows.Concat(GameContent.Enemies.rows))
            {
                results.Add(MechanismCoefficientSimulator.Evaluate(unit, unit.id.StartsWith("HERO_") ? "角色" : "敌人"));
                Debug.Log("6v6 measurement " + results.Count + "/47: " + unit.name);
            }
            return new MechanismReport { generatedUtc = DateTime.UtcNow.ToString("o"), results = results.ToArray() };
        }
        [MenuItem("Tools/Elemental Tactics/Mechanism Coefficient/Measure Buff Items")]
        public static void MeasureBuffItems()
        {
            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ItemMechanismCoefficients.json");
            File.WriteAllText(path, JsonUtility.ToJson(MeasureItems(), true)); Debug.Log("道具系数实测结果：" + path);
        }
        public static MechanismReport MeasureItems()
        {
            var results = new System.Collections.Generic.List<MechanismResult>();
            foreach (var item in GameContent.Items.rows.Where(MechanismCoefficientSimulator.IsBuffItem))
            {
                results.Add(MechanismCoefficientSimulator.Evaluate(item));
                Debug.Log("Item 6v6 measurement " + results.Count + ": " + item.name);
            }
            return MechanismCoefficientSimulator.ItemReport(results.ToArray());
        }
    }
}
