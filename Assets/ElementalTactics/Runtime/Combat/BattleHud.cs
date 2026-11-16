using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    public sealed class BattleHud
    {
        private readonly BattleController battle;
        private readonly Font font;
        private readonly Text orderText, logText, logDetails;
        private readonly GameObject pauseOverlay;
        private readonly Text pauseTitle, pauseDescription, exitCaption;
        private readonly Button pauseButton, statusButton;
        private readonly ResourceArtwork[] portraits = new ResourceArtwork[6];
        private readonly Image[] portraitBorders = new Image[6];
        private readonly Text[] arrows = new Text[5];
        private bool confirmingExit;

        public BattleHud(BattleController owner, Transform parent, Font chineseFont)
        {
            battle=owner;font=chineseFont;PrototypeUI.font=font;
            var header=PrototypeUI.Panel("Turn Order Panel",parent,new Vector2(0,695),new Vector2(900,62),BattlePresentation.Navy);
            orderText=Label("Turn Order",header.transform,"行动顺序",new Vector2(-350,0),new Vector2(110,40),15);
            orderText.color=PrototypeUI.Hex("9AAFC0");
            for(int i=0;i<portraits.Length;i++)
            {
                float x=-269+i*108;
                portraitBorders[i]=PrototypeUI.Panel("Order Border "+i,header.transform,new Vector2(x,0),new Vector2(41,41),PrototypeUI.Hex("657480"));
                portraits[i]=ResourceArtwork.Create(portraitBorders[i].transform,"Order Portrait "+i,null,Vector2.zero,new Vector2(37,37),true);
                if(i<arrows.Length)arrows[i]=Label("Order Arrow "+i,header.transform,"›",new Vector2(x+54,0),new Vector2(25,34),22);
            }
            PrototypeUI.Panel("Battle Footer Panel",parent,new Vector2(0,-717),new Vector2(900,166),BattlePresentation.Navy);
            PrototypeUI.Panel("Battle Footer Rule",parent,new Vector2(0,-634),new Vector2(842,1),BattlePresentation.Gold);
            logText=Label("Combat Log",parent,"等待战斗开始",new Vector2(0,-662),new Vector2(818,48),16);
            logText.resizeTextForBestFit=true;logText.resizeTextMinSize=12;logText.resizeTextMaxSize=16;
            statusButton=Button("Automatic Battle Status",parent,"自动战斗 · 战报",new Vector2(0,-731),new Vector2(440,62),true);
            statusButton.onClick.AddListener(ShowLog);
            pauseButton=Button("Pause Battle",parent,"暂停",new Vector2(332,-731),new Vector2(170,62));
            pauseButton.onClick.AddListener(battle.PauseBattle);
            var overlay=PrototypeUI.Panel("Pause Overlay",parent,Vector2.zero,new Vector2(900,1600),new Color(.02f,.05f,.09f,.9f));
            overlay.raycastTarget=true;pauseOverlay=overlay.gameObject;
            var paper=PrototypeUI.Panel("Pause Menu",overlay.transform,Vector2.zero,new Vector2(760,760),BattlePresentation.Navy);
            PrototypeUI.Border(paper.transform,new Vector2(760,760),BattlePresentation.Gold);
            pauseTitle=Label("Pause Heading",paper.transform,"战斗暂停",new Vector2(0,290),new Vector2(680,70),36);
            pauseDescription=Label("Pause Description",paper.transform,"",new Vector2(0,135),new Vector2(650,170),22);
            logDetails=Label("Detailed Combat Log",paper.transform,"",new Vector2(0,70),new Vector2(660,330),17);
            logDetails.alignment=TextAnchor.UpperLeft;logDetails.gameObject.SetActive(false);
            var resume=Button("Resume Battle",paper.transform,"继续战斗",new Vector2(0,-194),new Vector2(530,72),true);
            resume.onClick.AddListener(battle.ResumeBattle);
            var exit=Button("Exit Battle",paper.transform,"退出到地图",new Vector2(0,-290),new Vector2(530,68));
            exitCaption=exit.GetComponentInChildren<Text>();
            exit.onClick.AddListener(()=>{if(confirmingExit)battle.ConfirmExitBattle();else ShowPause(true);});
            pauseOverlay.SetActive(false);
        }
        private Text Label(string name,Transform parent,string value,Vector2 position,Vector2 size,int fontSize)
        {
            var text=PrototypeUI.Label(name,parent,value,position,size,fontSize,BattlePresentation.Ivory);text.font=font;return text;
        }
        private Button Button(string name,Transform parent,string value,Vector2 pos,Vector2 size,bool primary=false)
        {
            var button=PrototypeUI.Button(name,parent,value,pos,size,primary);
            button.image.color=PrototypeUI.Hex(primary?"69532D":"1B3046");
            BattlePresentation.ResizeButton(button,pos,size,value,22);return button;
        }
        public void SetOrder(IEnumerable<Fighter> remaining,Fighter current=null)
        {
            var actors=remaining.Where(f=>f.Alive).Take(6).ToArray();
            orderText.text=actors.Length==0?"回合结束":"行动顺序";
            for(int i=0;i<portraits.Length;i++)
            {
                portraitBorders[i].gameObject.SetActive(i<actors.Length);
                if(i<arrows.Length)arrows[i].gameObject.SetActive(i<actors.Length-1);
                if(i>=actors.Length)continue;
                portraits[i].SetPath(actors[i].definition.imagePath);
                portraitBorders[i].color=actors[i]==current?BattlePresentation.Cyan:actors[i].enemy?PrototypeUI.Hex("B86C66"):PrototypeUI.Hex("657480");
            }
        }
        public void SetLog(IReadOnlyList<string> entries)
        {
            logText.text=entries.Count==0?"等待战斗开始":entries[entries.Count-1];
            logDetails.text=string.Join("\n\n",entries.Skip(System.Math.Max(0,entries.Count-6)));
        }
        private void ShowLog()
        {
            if(!battle.IsBattling)return;
            battle.PauseBattle();pauseTitle.text="最近战报";
            pauseDescription.gameObject.SetActive(false);logDetails.gameObject.SetActive(true);
        }
        public void ShowPause(bool confirmExit=false)
        {
            confirmingExit=confirmExit;
            pauseTitle.text=confirmExit?"确定退出战斗？":"战斗暂停";
            pauseDescription.text=confirmExit?"退出后返回地图，本场战斗进度不会保留。":"战斗与技能演出均已暂停。\n继续战斗，或退出返回地图。";
            pauseDescription.gameObject.SetActive(true);logDetails.gameObject.SetActive(false);
            exitCaption.text=confirmExit?"确认退出":"退出到地图";
            pauseOverlay.SetActive(true);pauseOverlay.transform.SetAsLastSibling();
        }
        public void HidePause(){pauseOverlay.SetActive(false);}
        public void Finish()
        {
            HidePause();pauseButton.interactable=false;statusButton.interactable=false;
            statusButton.GetComponentInChildren<Text>().text="战斗已结束";orderText.text="战斗结束";
        }
    }
}
