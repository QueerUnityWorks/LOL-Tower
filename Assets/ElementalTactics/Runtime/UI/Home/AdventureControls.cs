using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    public sealed class AdventureControls : MonoBehaviour
    {
        private RectTransform root, overlay, drawer;
        private Text entryText, entryNumber, caption, note, levelText, factorText, stateText;
        private Button abandon, confirm;
        private Slider slider;
        private readonly Button[] levels = new Button[10];
        private int draft;
        private float slideProgress;
        public bool IsOpen => overlay != null && overlay.gameObject.activeSelf;
        public void Initialize(RectTransform parent)
        {
            root = parent;
            caption = root.Find("Start Adventure/Adventure Caption").GetComponent<Text>();
            note = root.Find("Start Adventure/Adventure Note").GetComponent<Text>();
            var entry = MainHubController.GoldButton("Difficulty Entry", root, "", new Vector2(-165,-700), new Vector2(516,110));
            entry.GetComponentInChildren<Text>().text="";
            entryNumber=Label(entry.transform,"Selected Level","",-185,0,110,94,56,MainHubController.Gold,TextAnchor.MiddleCenter);
            entryText=Label(entry.transform,"Selected Description","",27,0,290,90,23,MainHubController.Cream);
            Label(entry.transform,"Expand Arrow","▴",217,0,45,70,24,MainHubController.Gold,TextAnchor.MiddleCenter);
            entry.onClick.AddListener(OpenDifficulty);
            entry.interactable = !TutorialRun.Active;
            abandon = MainHubController.GoldButton("Abandon Adventure", root, "放弃", new Vector2(314,-758), new Vector2(166,42));
            abandon.targetGraphic.color = PrototypeUI.Hex("9B4540"); abandon.GetComponentInChildren<Text>().fontSize = 21;
            abandon.onClick.AddListener(OpenAbandon);
            Refresh();
            if (!string.IsNullOrEmpty(AdventurePersistence.LastError))
                PrototypeUI.Label("Save Notice", root, AdventurePersistence.LastError, new Vector2(0,-580), new Vector2(830,40),20,MainHubController.Navy);
        }
        public void Refresh()
        {
            AdventureRun.FinishIfNeeded();
            int d = AdventureRun.Active ? AdventureRun.Difficulty : AdventureRun.SelectedDifficulty;
            if (TutorialRun.Active) d = 1;
            entryNumber.text=d.ToString("00");
            entryText.text = "远征难度" + (AdventureRun.Active ? "\n本局已锁定" : "");
            if (TutorialRun.Active) entryText.text = "新手试炼\n练习难度";
            if (AdventureRun.Active || TutorialRun.Active) entryText.text += " · 生命 " + RunState.Lives + "/" + RunState.InitialLives;
            caption.text = AdventureRun.Active ? "继续冒险" : "开始冒险";
            if (DailyChallengeState.IsDailyRun && AdventureRun.Active) caption.text = "继续每日挑战";
            if (TutorialRun.Active) caption.text = "继续教程";
            note.gameObject.SetActive(!AdventureRun.Active && !TutorialRun.Active);
            abandon.gameObject.SetActive(AdventureRun.Active);
        }
        private static Text Label(Transform p,string name,string text,float x,float y,float w,float h,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft)
            => PrototypeUI.Label(name,p,text,new Vector2(x,y),new Vector2(w,h),size,color,align);
        private void NewOverlay(string name)
        {
            Close();
            overlay = PrototypeUI.Panel(name,root,Vector2.zero,new Vector2(900,1600),new Color(.025f,.07f,.08f,.72f)).rectTransform;
            overlay.GetComponent<Image>().raycastTarget = true;
            var dismiss = overlay.gameObject.AddComponent<Button>(); dismiss.onClick.AddListener(Close);
        }
        public void Close()
        {
            if (overlay != null) { overlay.gameObject.SetActive(false); Destroy(overlay.gameObject); }
            overlay = drawer = null;
        }
        public void OpenDifficulty()
        {
            NewOverlay("Difficulty Overlay");
            draft = AdventureRun.Active ? AdventureRun.Difficulty : AdventureRun.SelectedDifficulty;
            drawer = PrototypeUI.Panel("Difficulty Drawer",overlay,new Vector2(0,-422),new Vector2(900,756),MainHubController.Navy).rectTransform;
            drawer.GetComponent<Image>().raycastTarget = true;
            // Empty button consumes panel clicks; only the outer shade dismisses the drawer.
            drawer.gameObject.AddComponent<Button>();
            PrototypeUI.Border(drawer,drawer.sizeDelta,MainHubController.Gold);
            Label(drawer,"Eyebrow","E X P E D I T I O N   /   远 征 挑 战",-30,321,750,35,17,MainHubController.Gold);
            Label(drawer,"Title","选择难度",-30,264,750,64,36,MainHubController.Cream);
            MainHubController.GoldButton("Collapse Difficulty",drawer,"收起",new Vector2(339,280),new Vector2(110,54)).onClick.AddListener(Close);
            levelText = Label(drawer,"Difficulty Level","",-220,171,340,100,62,MainHubController.Gold);
            Label(drawer,"Factor Caption","敌人属性倍率",190,196,390,42,23,MainHubController.Cream,TextAnchor.MiddleRight);
            factorText = Label(drawer,"Difficulty Factor","",190,134,390,66,40,MainHubController.Gold,TextAnchor.MiddleRight);
            var sliderRoot = PrototypeUI.Panel("Difficulty Slider",drawer,new Vector2(0,28),new Vector2(720,48),new Color(1,1,1,.015f)).rectTransform;
            sliderRoot.GetComponent<Image>().raycastTarget = true;
            PrototypeUI.Panel("Rail",sliderRoot,Vector2.zero,new Vector2(720,5),PrototypeUI.Hex("637C79"));
            var area = PrototypeUI.Rect("Handle Area",sliderRoot,Vector2.zero,new Vector2(720,0));
            var handle = PrototypeUI.Panel("Handle",area,Vector2.zero,new Vector2(22,34),MainHubController.Gold);
            handle.raycastTarget = true;
            slider = sliderRoot.gameObject.AddComponent<Slider>(); slider.minValue=1; slider.maxValue=10; slider.wholeNumbers=true;
            slider.handleRect=handle.rectTransform; slider.targetGraphic=handle; slider.direction=Slider.Direction.LeftToRight;
            slider.SetValueWithoutNotify(draft); slider.interactable=!AdventureRun.Active;
            slider.onValueChanged.AddListener(value => { draft=(int)value; RefreshDraft(); });
            for (int i=0;i<10;i++)
            {
                int level=i+1;
                levels[i]=MainHubController.GoldButton("Difficulty "+level,drawer,level.ToString("00"),new Vector2(-360+i*80,-38),new Vector2(63,55));
                levels[i].GetComponentInChildren<Text>().fontSize=22;
                levels[i].interactable=!AdventureRun.Active;
                levels[i].onClick.AddListener(() => { draft=level; slider.SetValueWithoutNotify(level); RefreshDraft(); });
            }
            Label(drawer,"Stats Explanation","敌人生命 / 攻击 / 防御 / 速度同步提升\n生命与攻击同时叠加楼层成长",0,-132,790,76,23,MainHubController.Cream,TextAnchor.MiddleCenter);
            stateText=Label(drawer,"Lock Hint","",0,-218,790,50,20,MainHubController.Gold,TextAnchor.MiddleCenter);
            MainHubController.GoldButton("Cancel Difficulty",drawer,"取消",new Vector2(-205,-306),new Vector2(350,64)).onClick.AddListener(Close);
            confirm=MainHubController.GoldButton("Confirm Difficulty",drawer,AdventureRun.Active ? "本局难度已锁定" : "确认难度",new Vector2(205,-306),new Vector2(350,64));
            confirm.targetGraphic.color=PrototypeUI.Hex("AD7D38"); confirm.interactable=!AdventureRun.Active;
            confirm.onClick.AddListener(() => { AdventureRun.SelectDifficulty(draft); AdventurePersistence.SaveNow(); Refresh(); Close(); });
            RefreshDraft(); slideProgress=0; drawer.anchoredPosition=new Vector2(0,-1178);
        }
        private void RefreshDraft()
        {
            levelText.text=draft.ToString("00");
            factorText.text="× "+AdventureDifficulty.Factor(draft).ToString("F2");
            stateText.text=AdventureRun.Active ? "进行中的冒险难度固定 · 放弃后可重新选择" : "仅影响新冒险 · 开始后难度固定";
            for(int i=0;i<10;i++) levels[i].targetGraphic.color=i+1==draft ? PrototypeUI.Hex("AD7D38") : PrototypeUI.Hex("294C53");
        }
        public void OpenAbandon()
        {
            if (!AdventureRun.Active) return;
            NewOverlay("Abandon Confirmation");
            var panel=PrototypeUI.Panel("Confirmation",overlay,Vector2.zero,new Vector2(740,380),MainHubController.Navy).rectTransform;
            panel.GetComponent<Image>().raycastTarget=true; panel.gameObject.AddComponent<Button>();
            PrototypeUI.Border(panel,panel.sizeDelta,MainHubController.Gold);
            Label(panel,"Title",DailyChallengeState.IsDailyRun ? "放弃本次每日挑战？" : "放弃本次冒险？",0,112,660,58,34,MainHubController.Cream,TextAnchor.MiddleCenter);
            Label(panel,"Description",DailyChallengeState.IsDailyRun ? "本次挑战记为未完成，保留本日最好成绩。\n结束后恢复原有的普通远征。" : "本局队伍、道具与路线将清空。\n本次远征会保留在成就的冒险记录中。",0,14,660,100,23,MainHubController.Cream,TextAnchor.MiddleCenter);
            MainHubController.GoldButton("Keep Adventure",panel,"保留冒险",new Vector2(-170,-115),new Vector2(290,64)).onClick.AddListener(Close);
            var yes=MainHubController.GoldButton("Confirm Abandon",panel,"确认放弃",new Vector2(170,-115),new Vector2(290,64));
            yes.targetGraphic.color=PrototypeUI.Hex("9B4540");
            yes.onClick.AddListener(() => { AdventureRun.Abandon(); AdventurePersistence.SaveNow(); Refresh(); Close(); });
        }
        private void Update()
        {
            if(IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
            if(drawer!=null && slideProgress<1) { slideProgress=Mathf.Min(1,slideProgress+Time.unscaledDeltaTime/.22f); float t=1-Mathf.Pow(1-slideProgress,3); drawer.anchoredPosition=new Vector2(0,Mathf.Lerp(-1178,-422,t)); }
        }
    }
}
