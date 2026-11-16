using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElementalTactics.Editor
{
    [InitializeOnLoad]
    public static class HomeInterfaceValidation
    {
        const string Output = "D:/自走棋/outputs/home-ui-20261105/";
        static readonly string[] Names = { "Hero Codex", "Enemy Codex", "Item Codex", "Achievements House", "Settings House" };
        static readonly string[] Captions = { "角色图鉴", "敌人图鉴", "道具图鉴", "成就", "设置" };
        static HomeInterfaceValidation() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (!File.Exists(Output + "unity-request.txt") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Output + "unity-request.txt");
            try { Run(); }
            catch(Exception e) { File.WriteAllText(Output + "unity-error.txt", e.ToString()); Debug.LogException(e); }
        }
        static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        static void Run()
        {
            var preview = EditorSceneManager.NewPreviewScene();
            var previousFont = PrototypeUI.font;
            var previousEvents = EventSystem.current;
            RenderTexture texture = null;
            try
            {
                // Avoid finishing or archiving any retained play-mode run during editor validation.
                Require(!AdventureRun.Active, "请先结束当前运行状态后执行主界面验证，避免改变冒险进度。");
                var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/ElementalTactics/Fonts/NotoSansCJKsc-Regular.otf");
                var canvasObject = new GameObject("Home Preview", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
                SceneManager.MoveGameObjectToScene(canvasObject, preview);
                var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
                var root = (RectTransform)canvasObject.transform; root.sizeDelta = new Vector2(900,1600);
                var hub = canvasObject.AddComponent<MainHubController>(); hub.Build(root,font);
                var cameraObject = new GameObject("Home Preview Camera",typeof(Camera)); SceneManager.MoveGameObjectToScene(cameraObject,preview);
                var camera = cameraObject.GetComponent<Camera>(); camera.scene=preview; camera.orthographic=true;
                camera.transform.position=new Vector3(0,0,-10); camera.orthographicSize=800;
                camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=MainHubController.Navy;
                canvas.worldCamera=camera;
                var eventsObject = new GameObject("Home Preview Events",typeof(EventSystem)); SceneManager.MoveGameObjectToScene(eventsObject,preview);
                var events = eventsObject.GetComponent<EventSystem>();
                // Exercise the same runtime initialization and callbacks used by MainScene.
                hub.SendMessage("Start");
                var raycaster=canvasObject.GetComponent<GraphicRaycaster>();
                var codex=canvasObject.GetComponent<CodexUI>(); var panels=canvasObject.GetComponent<MainHubPanels>();
                var sizes=new[] {new Vector2Int(1080,1920),new Vector2Int(1080,2400),new Vector2Int(1536,2048)};
                int clicks=0;
                foreach(var size in sizes)
                {
                    texture=new RenderTexture(size.x,size.y,24); camera.targetTexture=texture;
                    camera.orthographicSize=Mathf.Max(800,450f*size.y/size.x);
                    Canvas.ForceUpdateCanvases(); camera.Render();
                    for(int i=0;i<Names.Length;i++)
                    {
                        var button=root.Find(Names[i]).GetComponent<Button>();
                        var rect=(RectTransform)button.transform;
                        var area=button.GetComponent<HomeTowerHitArea>();
                        Require(area!=null,"Tower hit area missing: "+Names[i]);
                        Require(button.GetComponentInChildren<Text>().text.Replace("\n","")==Captions[i],"Wrong caption: "+Names[i]);
                        var tower=area.tower;
                        // Exercise visible crown, base edges, body and both ends of the original plaque.
                        foreach(var point in new[] {new Vector2(0,tower.yMin+tower.height*.96f),new Vector2(tower.xMin+tower.width*.07f,tower.yMin+tower.height*.10f),new Vector2(tower.xMin+tower.width*.93f,tower.yMin+tower.height*.10f),tower.center,area.plaque.center+Vector2.up*(area.plaque.height/2-5),area.plaque.center-Vector2.up*(area.plaque.height/2-5)})
                        {
                            var hits=new List<RaycastResult>();
                            var data=new PointerEventData(events) {position=camera.WorldToScreenPoint(rect.TransformPoint(point))};
                            raycaster.Raycast(data,hits);
                            Require(hits.Count>0 && hits[0].gameObject.GetComponentInParent<Button>()==button,"Wrong hit: "+Names[i]+" "+point+" @ "+size);
                            clicks++;
                        }
                    }
                    Capture(camera,texture,Output+"home-"+size.x+"x"+size.y+".png");
                    camera.targetTexture=null; texture.Release(); UnityEngine.Object.DestroyImmediate(texture); texture=null;
                }
                for(int i=0;i<3;i++)
                {
                    root.Find(Names[i]).GetComponent<Button>().onClick.Invoke();
                    int expected=i==0?GameContent.Characters.rows.Length:i==1?GameContent.Enemies.rows.Length:GameContent.Items.rows.Length;
                    Require(codex.IsOpen && codex.VisibleCount==expected,"Wrong codex destination: "+Names[i]); codex.Close();
                }
                foreach(var pair in new[]{new[]{Names[3],"Achievements Overlay"},new[]{Names[4],"Settings Overlay"}})
                {
                    root.Find(pair[0]).GetComponent<Button>().onClick.Invoke();
                    Require(panels.IsOpen && root.Find(pair[1]).gameObject.activeSelf,"Wrong destination: "+pair[0]); panels.Close();
                }
                SaveMainScene(font);
                File.WriteAllText(Output+"unity-result.json","{\"status\":\"PASS\",\"raycastChecks\":"+clicks+",\"destinations\":5,\"resolutions\":3,\"sceneSaved\":true}");
            }
            finally
            {
                PrototypeUI.font=previousFont;
                if(texture!=null) { texture.Release(); UnityEngine.Object.DestroyImmediate(texture); }
                EditorSceneManager.ClosePreviewScene(preview);
                if(previousEvents != null && previousEvents.isActiveAndEnabled) EventSystem.current=previousEvents;
            }
        }
        static void Capture(Camera camera, RenderTexture target, string path)
        {
            var previous=RenderTexture.active; Texture2D image=null;
            try
            {
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active=target;
                image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply(); File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally { RenderTexture.active=previous; if(image!=null) UnityEngine.Object.DestroyImmediate(image); }
        }
        static void SaveMainScene(Font font)
        {
            var active=SceneManager.GetActiveScene();
            var scene=SceneManager.GetSceneByPath(MainSceneBuilder.ScenePath);
            bool opened=!scene.IsValid() || !scene.isLoaded;
            if(!opened) Require(!scene.isDirty,"主场景有未保存更改，未覆盖场景。");
            if(opened) scene=EditorSceneManager.OpenScene(MainSceneBuilder.ScenePath,OpenSceneMode.Additive);
            try
            {
                var hub=scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<MainHubController>(true)).Single();
                var root=(RectTransform)new SerializedObject(hub).FindProperty("root").objectReferenceValue;
                for(int i=root.childCount-1;i>=0;i--) UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
                hub.Build(root,font); EditorUtility.SetDirty(hub); EditorSceneManager.MarkSceneDirty(scene);
                Require(EditorSceneManager.SaveScene(scene),"MainScene save failed");
                File.Copy(MainSceneBuilder.ScenePath,"D:/自走棋/PrototypeSource/Scenes/MainScene.unity",true);
            }
            finally
            {
                if(opened) EditorSceneManager.CloseScene(scene,true);
                if(active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            }
        }
    }
}
