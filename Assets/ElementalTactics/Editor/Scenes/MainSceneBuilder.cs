using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ElementalTactics.Editor
{
    public static class MainSceneBuilder
    {
        public const string ScenePath="Assets/ElementalTactics/Scenes/MainScene.unity";
        [MenuItem("Tools/Elemental Tactics/Rebuild Main Scene")]
        public static void Build()
        {
            if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("请退出运行并保存当前场景后再生成主场景。");
            var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/ElementalTactics/Fonts/NotoSansCJKsc-Regular.otf");
            if(font==null) throw new InvalidOperationException("缺少中文字体。");
            PrototypeUI.font=font;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)); camera.tag="MainCamera";
            camera.transform.position=new Vector3(0,0,-10);
            camera.GetComponent<Camera>().orthographic=true;
            camera.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor=MainHubController.Cream;
            var light=new GameObject("Directional Light",typeof(Light)); light.GetComponent<Light>().type=LightType.Directional;
            light.transform.rotation=Quaternion.Euler(50,-30,0);
            var root=PrototypeUI.CreateCanvas();
            for(int i=root.childCount-1;i>=0;i--) UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
            var hub=new GameObject("Main Hub Controller").AddComponent<MainHubController>(); hub.Build(root,font);
            if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ScenePath)) throw new InvalidOperationException("主场景保存失败。");
            string[] paths={"Assets/ElementalTactics/Scenes/StartScene.unity",ScenePath,"Assets/ElementalTactics/Scenes/MapScene.unity","Assets/ElementalTactics/Scenes/LevelScene.unity"};
            EditorBuildSettings.scenes=paths.Select(p=>new EditorBuildSettingsScene(p,true))
                .Concat(EditorBuildSettings.scenes.Where(s=>!paths.Contains(s.path))).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("MainScene 已生成：主城、三类图鉴、开始冒险。初始场景进入主场景。");
        }
    }
}
