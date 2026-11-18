using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElementalTactics
{
    public sealed class MainHubController : MonoBehaviour
    {
        [SerializeField] private Font hubFont;
        [SerializeField] private RectTransform root;
        [SerializeField] private Button adventure, heroes, enemies, items, achievements, settings, back;
        private bool loading;
        private CodexUI codex;
        private MainHubPanels panels;
        private AdventureControls controls;
        public static readonly Color Navy = PrototypeUI.Hex("101E35");
        public static readonly Color Gold = PrototypeUI.Hex("D7AF59");
        public static readonly Color Cream = PrototypeUI.Hex("FFF1CE");
        private void Start()
        {
            PrototypeUI.font = hubFont;
            root.Find("Profile Caption").GetComponent<Text>().text = "弈士 · " + LeaderboardState.PlayerName;
            var profile = root.Find("Profile Frame/Profile Rimuru");
            if (profile != null) profile.GetComponent<ResourceArtwork>().SetPath(GameContent.Characters.Get("HERO_001").imagePath);
            codex = gameObject.AddComponent<CodexUI>();
            codex.Initialize(root, hubFont);
            panels=gameObject.AddComponent<MainHubPanels>();
            panels.Initialize(root,hubFont);
            controls=gameObject.AddComponent<AdventureControls>();
            controls.Initialize(root);
            adventure.onClick.AddListener(StartAdventure);
            heroes.onClick.AddListener(() => codex.Open(0));
            enemies.onClick.AddListener(() => codex.Open(1));
            items.onClick.AddListener(() => codex.Open(2));
            achievements.onClick.AddListener(panels.OpenAchievements);
            settings.onClick.AddListener(panels.OpenSettings);
            back.onClick.AddListener(() => { if (!loading) { loading = true; SceneManager.LoadSceneAsync("StartScene"); } });
            HomeQuickEntryPresentation.Build(root, false, panels.OpenLeaderboard);
            HomeQuickEntryPresentation.Build(root, true, panels.OpenDailyChallenge);
            TutorialGuide.BuildHomeEntry(root);
            SettlementController.TryOpen(root);
        }
        public void StartAdventure()
        {
            if (loading || (codex != null && codex.IsOpen) || (panels != null && panels.IsOpen) || (controls != null && controls.IsOpen)) return;
            loading = true;
            if (TutorialRun.Active || TutorialGuide.WantsTraining) { TutorialGuide.OpenTraining(); return; }
            if (!AdventureRun.Active) AdventureRun.StartNew();
            string scene=AdventureRun.ContinueScene;
            AdventurePersistence.SaveNow();
            SceneManager.LoadSceneAsync(scene);
        }
        public static MainHubArt Art(Transform parent,string name,MainHubArt.Motif motif,Vector2 pos,Vector2 size,Color color)
        {
            var art = PrototypeUI.Rect(name,parent,pos,size).gameObject.AddComponent<MainHubArt>();
            art.motif = motif; art.color = color; art.raycastTarget = false;
            return art;
        }
        public static Button GoldButton(string name,Transform parent,string caption,Vector2 pos,Vector2 size)
        {
            var button=PrototypeUI.Button(name,parent,caption,pos,size);
            button.targetGraphic.color=Navy;
            foreach(var image in button.GetComponentsInChildren<Image>()) if(image!=button.targetGraphic) image.color=Gold;
            button.GetComponentInChildren<Text>().color=Cream;
            return button;
        }
        private Text Label(string name,Transform parent,string text,float x,float y,float w,float h,int size,Color tint,TextAnchor align=TextAnchor.MiddleCenter)
            => PrototypeUI.Label(name,parent,text,new Vector2(x,y),new Vector2(w,h),size,tint,align);
        // Called by the scene builder. All home visuals and button references are serialized in MainScene.
        public void Build(RectTransform content, Font font)
        {
            root=content; hubFont=font; PrototypeUI.font=font;
            PrototypeUI.Panel("Home Background",root,Vector2.zero,new Vector2(900,1600),PrototypeUI.Hex("233956"));
            Art(root,"Tempest City",MainHubArt.Motif.City,new Vector2(0,110),new Vector2(900,1420),Color.white);
            PrototypeUI.Panel("Profile Bar",root,new Vector2(0,710),new Vector2(900,180),Navy);
            PrototypeUI.Panel("Header Gold Rule",root,new Vector2(0,618),new Vector2(900,4),Gold);
            var portrait=PrototypeUI.Panel("Profile Frame",root,new Vector2(-348,711),new Vector2(112,112),Gold);
            PrototypeUI.Panel("Profile Inset",portrait.transform,Vector2.zero,new Vector2(104,104),PrototypeUI.Hex("284668"));
            ResourceArtwork.Create(portrait.transform,"Profile Rimuru",GameContent.Characters.Get("HERO_001").imagePath,new Vector2(0,-3),new Vector2(104,82));
            Label("Profile Caption",root,"弈士 · " + LeaderboardState.PlayerName,-96,747,340,34,20,Gold,TextAnchor.MiddleLeft);
            Label("Home Title",root,"金铲铲之战",-65,699,400,58,38,Cream,TextAnchor.MiddleLeft);
            Label("Home Subtitle",root,"艾欧尼亚林地的冒险，从这里启程",-65,654,400,30,17,PrototypeUI.Hex("BDCBE3"),TextAnchor.MiddleLeft);
            back=GoldButton("Return Title",root,"返回\n标题",new Vector2(351,716),new Vector2(108,92));
            back.GetComponentInChildren<Text>().fontSize=21;
            heroes=HouseButton("Hero Codex","角\n色\n图\n鉴",.13f,.46f,.18f,.12f);
            enemies=HouseButton("Enemy Codex","敌\n人\n图\n鉴",.36f,.40f,.28f,.20f);
            items=HouseButton("Item Codex","道\n具\n图\n鉴",.72f,.48f,.15f,.11f);
            achievements=HouseButton("Achievements House","成\n就",.04f,.22f,.24f,.14f);
            settings=HouseButton("Settings House","设\n置",.73f,.20f,.22f,.14f);
            var hero=PrototypeUI.Rect("Home Rimuru",root,new Vector2(-36,-360),new Vector2(350,275)).gameObject.AddComponent<StartScreenArt>();
            hero.layer=StartScreenArt.Layer.Spatula; hero.raycastTarget=false;
            PrototypeUI.Panel("Bottom Bar",root,new Vector2(0,-700),new Vector2(900,200),Navy);
            PrototypeUI.Panel("Bottom Bar Gold Rule",root,new Vector2(0,-602),new Vector2(900,3),Gold);
            PrototypeUI.Panel("Bottom Bar Inner Top",root,new Vector2(0,-617),new Vector2(860,1),PrototypeUI.Hex("8C794F"));
            PrototypeUI.Panel("Bottom Bar Inner Bottom",root,new Vector2(0,-783),new Vector2(860,1),PrototypeUI.Hex("8C794F"));
            var fan=Art(root,"Start Adventure",MainHubArt.Motif.AdventureFan,new Vector2(290,-640),new Vector2(320,320),PrototypeUI.Hex("AF7E38"));
            fan.raycastTarget=true;
            adventure=fan.gameObject.AddComponent<Button>(); adventure.targetGraphic=fan;
            UiAudioFeedback.Bind(adventure, SoundCue.Confirm);
            Art(fan.transform,"Adventure Compass",MainHubArt.Motif.Compass,new Vector2(24,31),new Vector2(48,48),Cream);
            Label("Adventure Caption",fan.transform,"开始冒险",24,-24,186,46,29,Cream);
            Label("Adventure Note",fan.transform,"踏上全新旅途",24,-69,178,26,16,Cream);
        }
        private Button HouseButton(string name,string title,float x,float y,float width,float height)
        {
            // Preserve the original artwork and plaque coordinates while covering the enlarged tower.
            var plaquePosition=new Vector2((x+width/2-.5f)*900,-620+(y+(height+.075f)/2)*1420);
            float plaqueHeight=title.Split('\n').Length>2?174:124;
            float towerWidth=width*1.35f*900, towerHeight=height*1.35f*1420;
            float baseY=-600+(y-height*.14f)*1420;
            float bottom=Mathf.Min(baseY-43,plaquePosition.y-plaqueHeight/2);
            float top=Mathf.Max(baseY+Mathf.Max(towerHeight*.99f,towerHeight*.825f+63.9f),plaquePosition.y+plaqueHeight/2);
            var position=new Vector2(plaquePosition.x,(bottom+top)/2);
            var size=new Vector2(towerWidth*1.12f,top-bottom);
            var hit=PrototypeUI.Panel(name,root,position,size,Color.clear);
            hit.raycastTarget=true;
            var button=hit.gameObject.AddComponent<Button>(); button.targetGraphic=hit;
            UiAudioFeedback.Bind(button, SoundCue.None);
            var area=hit.gameObject.AddComponent<HomeTowerHitArea>();
            area.tower=new Rect(-towerWidth/2,baseY-position.y,towerWidth,towerHeight);
            area.plaque=new Rect(-27,plaquePosition.y-position.y-plaqueHeight/2,54,plaqueHeight);
            var colors=button.colors;
            colors.normalColor=Color.white;
            colors.highlightedColor=new Color(1.3f,1.4f,1.5f,1);
            colors.pressedColor=new Color(.7f,.8f,.9f,1);
            colors.selectedColor=colors.normalColor;
            button.colors=colors;
            var plaque=PrototypeUI.Panel("Vertical Plaque",hit.transform,plaquePosition-position,new Vector2(54,plaqueHeight),Navy);
            button.targetGraphic=plaque;
            PrototypeUI.Border(plaque.transform,new Vector2(54,plaqueHeight),Gold);
            Label("Caption",plaque.transform,title,0,0,42,plaqueHeight-8,26,Cream);
            return button;
        }
    }
}
