using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    // Presentation only: retain the scene's buttons, cards, ScrollRect and formation model.
    public sealed class FormationPresentation
    {
        private readonly FormationController owner;
        private readonly Text reserveCount, ready, classification, selectionTitle, selectionHint, skillHeading;
        private readonly GameObject prompt, emptyReserve;
        private readonly ResourceArtwork portrait;
        private readonly ScrollRect skillScroll;
        private static Vector2 P(float x,float y) => MapPresentation.Point(x,y);
        private static Vector2 S(float w,float h) => MapPresentation.Size(w,h);

        private static void Place(Transform target,Vector2 pos,Vector2 size)
        {
            var rect=(RectTransform)target;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=pos;rect.sizeDelta=size;
        }
        private static void Hide(Transform parent,string name)
        {var child=parent.Find(name);if(child!=null)child.gameObject.SetActive(false);}
        private static Text Label(Transform parent,string name,string value,float x,float y,float w,float h,int font,Color color,bool bold=false,TextAnchor align=TextAnchor.MiddleLeft)
            => MapPresentation.Label(name,parent,value,x,y,w,h,font,color,align,bold);
        private static void TextStyle(Text label,float x,float y,float w,float h,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft,bool bold=false)
        {
            Place(label.transform,P(x,y),S(w,h));
            label.fontSize=Mathf.RoundToInt(size/1.2f);label.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;
            label.color=color;label.alignment=align;label.verticalOverflow=VerticalWrapMode.Overflow;
            label.resizeTextForBestFit=true;label.resizeTextMinSize=Mathf.Max(12,label.fontSize-5);label.resizeTextMaxSize=label.fontSize;
        }
        public static void StyleButton(Button button,Vector2 pos,Vector2 size,string caption,int font,bool primary=false)
        {
            Place(button.transform,pos,size);
            button.image.enabled=true;button.image.sprite=MapPresentation.Sprite("Panel");button.image.type=Image.Type.Sliced;
            button.image.pixelsPerUnitMultiplier=2.4f;
            button.image.color=PrototypeUI.Hex(primary?"CFAD70":"172D3D");
            foreach(string name in new[]{"Top Border","Bottom Border","Left Border","Right Border"})Hide(button.transform,name);
            var frame=button.transform.Find("Formation Button Frame");
            if(frame==null)frame=MapPresentation.Art("Formation Button Frame",button.transform,"PanelBorder",Vector2.zero,size,MapPresentation.Gold).transform;
            Place(frame,Vector2.zero,size);frame.GetComponent<Image>().color=PrototypeUI.Hex(primary?"F0D49A":"857859");
            frame.gameObject.SetActive(true);frame.GetComponent<Image>().pixelsPerUnitMultiplier=2.4f;
            var label=button.GetComponentInChildren<Text>(true);
            Place(label.transform,Vector2.zero,size-new Vector2(14,6));
            label.text=caption;label.fontSize=font;label.fontStyle=FontStyle.Bold;
            label.color=primary?PrototypeUI.Hex("152635"):MapPresentation.Ivory;
            label.alignment=TextAnchor.MiddleCenter;label.verticalOverflow=VerticalWrapMode.Overflow;
            label.resizeTextForBestFit=true;label.resizeTextMinSize=Mathf.Max(12,font-5);label.resizeTextMaxSize=font;
        }
        public static void StyleInventoryButton(Button button)
        {
            StyleButton(button,new Vector2(-316,-649),new Vector2(180,76),"背包",24);
            var text=button.GetComponentInChildren<Text>();text.rectTransform.anchoredPosition=new Vector2(14,0);
            var old=button.transform.Find("Formation Bag Icon");
            if(old==null)MapPresentation.Art("Formation Bag Icon",button.transform,"bag",new Vector2(-48,0),new Vector2(35,35),MapPresentation.Gold);
            else old.gameObject.SetActive(true);
        }
        public static void RestoreBattleInventory(Button button)
        {
            Hide(button.transform,"Formation Button Frame");Hide(button.transform,"Formation Bag Icon");
            button.image.sprite=null;button.image.type=Image.Type.Simple;button.image.color=PrototypeUI.Indigo;
            var label=button.GetComponentInChildren<Text>();label.rectTransform.anchoredPosition=Vector2.zero;label.fontStyle=FontStyle.Normal;
            label.resizeTextForBestFit=false;label.color=PrototypeUI.Paper;
            foreach(string name in new[]{"Top Border","Bottom Border","Left Border","Right Border"})
            {var child=button.transform.Find(name);if(child!=null)child.gameObject.SetActive(true);}
        }

        public FormationPresentation(FormationController formation)
        {
            owner=formation;Transform root=owner.preparationRoot.transform;
            ((RectTransform)root).sizeDelta=new Vector2(900,1600);
            foreach(string name in new[]{"Section","Title","Phase","Element Legend","Selection Plaque","Selection Prompt","Formation Field","Formation Field Label","Front Row","Rear Row","Candidate Cards Paper","Roster Label","Preparation Footer"})Hide(root,name);
            var canvas=root.GetComponentInParent<Canvas>();var backdrop=canvas.transform.Find("Backdrop");
            if(backdrop!=null)backdrop.GetComponent<Image>().color=PrototypeUI.Hex("081522");
            var visual=PrototypeUI.Rect("Formation Presentation",root,Vector2.zero,new Vector2(900,1600));
            visual.SetAsFirstSibling();
            PrototypeUI.Panel("Formation Background",visual,Vector2.zero,new Vector2(900,1600),PrototypeUI.Hex("081522"));
            // Quiet diagonal runes, native graphics rather than additional raster assets.
            for(int i=-5;i<9;i++)
            {
                var line=PrototypeUI.Panel("Formation Rune "+i,visual,new Vector2(i*150,30),new Vector2(1,1100),PrototypeUI.Hex("142B3C"));
                line.rectTransform.localRotation=Quaternion.Euler(0,0,29);
            }
            PrototypeUI.Panel("Formation Header",visual,P(540,101),S(1080,202),MapPresentation.Navy);
            MapPresentation.Rule("Formation Top Accent",visual,53,87,4,95,MapPresentation.Gold);
            Label(visual,"Formation Section","金铲铲之战 / 远征作战室",390,54,624,35,23,MapPresentation.Gold);
            Label(visual,"Formation Title","战前布阵",360,109,570,70,47,MapPresentation.Ivory,true);
            string[] elements={"火","草","水","火"};
            string[] colors={"EB9A88","B9D786","89C5DA","EB9A88"};
            for(int i=0;i<4;i++)
            {
                float x=99+i*128;
                if(i<3)MapPresentation.Panel("Formation Element Chip "+i,visual,x,171,54,36,PrototypeUI.Hex("172D3B"),PrototypeUI.Hex("4B6068"));
                Label(visual,"Formation Element "+i,elements[i],x,171,45,32,23,PrototypeUI.Hex(colors[i]),true,TextAnchor.MiddleCenter);
                if(i<3)Label(visual,"Formation Advantage Arrow "+i,"克",x+64,171,36,30,18,MapPresentation.Muted,false,TextAnchor.MiddleCenter);
            }
            Label(visual,"Formation Advantage Bonus","克制伤害 +25%",653,172,260,34,21,MapPresentation.Muted);
            Label(visual,"Adventure Lives","生命 " + RunState.Lives + " / " + RunState.InitialLives,633,110,220,34,23,MapPresentation.Gold,true,TextAnchor.MiddleRight);
            MapPresentation.Rule("Formation Header Rule",visual,540,210,984,2,PrototypeUI.Hex("746B53"));
            MapPresentation.Panel("Formation Selection Panel",visual,540,295,988,128,PrototypeUI.Hex("102737"),PrototypeUI.Hex("536875"));
            var intro=PrototypeUI.Rect("Formation Selection Intro",visual,Vector2.zero,new Vector2(900,1600));prompt=intro.gameObject;
            MapPresentation.At("Selection Role Icon",intro,"role",102,291,53,53,MapPresentation.Cyan);
            selectionTitle=Label(intro,"Selection Intro Title","选择伙伴，安排前后排",567,276,832,42,29,MapPresentation.Ivory,true);
            selectionHint=Label(intro,"Selection Intro Hint","点击候补再点阵位，或长按卡片拖入；已上阵角色可交换位置。",567,322,832,32,20,MapPresentation.Muted);
            MapPresentation.Panel("Formation Board",visual,540,717,1008,672,InventoryPresentation.Surface,PrototypeUI.Hex("506772"));
            Label(visual,"Formation Board Title","我方阵形",243,417,354,42,27,MapPresentation.Gold,true);
            Label(visual,"Formation Board Hint","六个阵位 · 长按拖动调整",800,417,416,32,20,MapPresentation.Muted,false,TextAnchor.MiddleRight);
            MapPresentation.Rule("Formation Row Divider",visual,585,738,791,1,PrototypeUI.Hex("2F4856"));
            for(int row=0;row<2;row++)
            {
                int cy=row==0?582:890;
                MapPresentation.Panel("Formation Rank "+row,visual,88,cy,61,124,PrototypeUI.Hex("172D3B"),PrototypeUI.Hex("4B6068"));
                Label(visual,"Formation Rank Name "+row,row==0?"前\n排":"后\n排",88,cy-10,50,76,23,MapPresentation.Gold,true,TextAnchor.MiddleCenter);
                Label(visual,"Formation Rank Number "+row,row==0?"01":"02",88,cy+35,40,22,14,MapPresentation.Muted,false,TextAnchor.MiddleCenter);
            }
            for(int i=0;i<owner.slotButtons.Length;i++)
            {
                var pos=P(258+i%3*300,i<3?585:893);
                StyleButton(owner.slotButtons[i],pos,new Vector2(150,225),"＋\n空阵位",25);
                owner.slotButtons[i].image.color=PrototypeUI.Hex("102332");
                owner.slotCards[i].Rect.anchoredPosition=pos;owner.slotCards[i].homePosition=pos;
            }
            MapPresentation.Panel("Formation Reserve Panel",visual,540,1330,1008,507,InventoryPresentation.Surface,PrototypeUI.Hex("506772"));
            Label(visual,"Formation Reserve Title","候补伙伴",238,1117,340,42,27,MapPresentation.Gold,true);
            reserveCount=Label(visual,"Formation Reserve Count","",807,1118,402,32,20,MapPresentation.Muted,false,TextAnchor.MiddleRight);
            var scroll=owner.rosterScroll;
            Place(scroll.viewport,P(527,1360.5f),S(936,417));
            scroll.viewport.GetComponent<Image>().color=PrototypeUI.Hex("0C1F2E");
            var mask=scroll.viewport.GetComponent<Mask>();if(mask!=null)mask.showMaskGraphic=true;
            scroll.content.anchorMin=scroll.content.anchorMax=scroll.content.pivot=new Vector2(0,1);
            scroll.content.anchoredPosition=Vector2.zero;scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,780);
            scroll.scrollSensitivity=40;scroll.movementType=ScrollRect.MovementType.Clamped;
            Place(scroll.verticalScrollbar.transform,P(1017,1360),S(8,416));
            scroll.verticalScrollbar.GetComponent<Image>().color=PrototypeUI.Hex("2A4151");
            scroll.verticalScrollbar.targetGraphic.color=MapPresentation.Gold;
            foreach(var status in owner.reserveStatus)
            {status.fontSize=16;status.color=MapPresentation.Muted;status.rectTransform.sizeDelta=new Vector2(185,24);}
            emptyReserve=Label(visual,"Formation Empty Reserve","暂无候补伙伴 · 已拥有的角色均已上阵",540,1350,870,70,25,MapPresentation.Muted,false,TextAnchor.MiddleCenter).gameObject;
            emptyReserve.transform.SetParent(scroll.viewport,false);
            Place(emptyReserve.transform,Vector2.zero,S(870,70));
            Place(owner.informationWindow.transform,Vector2.zero,new Vector2(900,1600));
            owner.informationWindow.GetComponent<Image>().enabled=false;
            foreach(string name in new[]{"Top Border","Bottom Border","Left Border","Right Border","Detail Element Accent","Detail Role Emblem"})Hide(owner.informationWindow.transform,name);
            Transform detail=owner.informationWindow.transform;
            PrototypeUI.Panel("Formation Detail Shadow",detail,P(540,1374),S(1028,632),new Color(0,0,0,.45f));
            var sheet=MapPresentation.Panel("Formation Detail Panel",detail,540,1368,1008,620,PrototypeUI.Hex("102737"),MapPresentation.Gold);
            sheet.raycastTarget=true;
            MapPresentation.Rule("Formation Detail Accent",detail,540,1070,94,3,MapPresentation.Gold);
            Label(detail,"Formation Detail Heading","伙伴详情",225,1102,316,38,25,MapPresentation.Gold,true);
            InventoryPresentation.ArtworkFrame("Formation Portrait Frame",detail,P(155,1238),S(146,212),MapPresentation.Gold);
            portrait=ResourceArtwork.Create(detail,"Formation Selected Portrait",null,P(155,1238),S(140,206),true);
            TextStyle(owner.informationTitle,568,1170,614,56,36,MapPresentation.Ivory,TextAnchor.MiddleLeft,true);
            classification=Label(detail,"Formation Selected Classification","",622,1220,722,36,23,MapPresentation.Cyan);
            TextStyle(owner.informationStats,622,1291,722,94,25,MapPresentation.Ivory);
            MapPresentation.Rule("Formation Skill Divider",detail,540,1364,924,1,PrototypeUI.Hex("3F5661"));
            skillHeading=Label(detail,"Formation Skill Heading","技能介绍",540,1400,924,42,25,MapPresentation.Gold,true);
            owner.informationSkill.gameObject.SetActive(false);
            owner.informationSkill=InventoryPresentation.Description("Formation Selected Skill",detail,P(540,1502),S(924,142),out skillScroll);
            owner.informationSkill.fontSize=20;
            owner.informationSkill.color=MapPresentation.Ivory;
            StyleButton(owner.removeButton,P(814,1625),S(436,64),"撤下伙伴",23);
            StyleButton(owner.closeInformationButton,P(938,1102),S(150,50),"收起  ×",21);
            var cancel=JourneyPresentation.Button("Cancel Formation Selection",detail,"取消选择",P(266,1625),S(436,64),owner.ClearSelection);
            cancel.GetComponentInChildren<Text>().fontSize=20;
            foreach(var t in new[]{owner.informationTitle.transform,owner.informationStats.transform,owner.removeButton.transform,owner.closeInformationButton.transform})t.SetAsLastSibling();
            TextStyle(owner.teamCount,887,119,242,38,23,MapPresentation.Cyan,TextAnchor.MiddleRight,true);
            TextStyle(owner.hint,540,1625,970,62,22,MapPresentation.Muted,TextAnchor.MiddleCenter);
            if(DailyChallengeState.IsDailyRun)
            {
                owner.hint.gameObject.SetActive(false);
                DailyTaskProgressUI.Build(root,P(540,1640),S(988,84),true);
            }
            StyleButton(owner.battle.backButton,P(938.5f,53.5f),S(207,57),"← 返回地图",21);
            StyleButton(owner.enemyButton,P(938.5f,169.5f),S(207,49),"敌方情报  ›",19);
            StyleButton(owner.battle.startButton,new Vector2(0,-649),new Vector2(412,76),"开始战斗    →",27,true);
            StyleButton(owner.battle.resetButton,new Vector2(316,-649),new Vector2(180,76),"重置布阵",22);
            ready=Label(visual,"Formation Ready","",540,1837,920,38,21,MapPresentation.Cyan,false,TextAnchor.MiddleCenter);
            Label(visual,"Formation Footer","金铲铲之战 / 符文远征",540,1888,900,28,18,PrototypeUI.Hex("627F90"),false,TextAnchor.MiddleCenter);
            foreach(var image in visual.GetComponentsInChildren<Image>(true))if(image.type==Image.Type.Sliced)image.pixelsPerUnitMultiplier=2f;
            var detailPanel=detail.Find("Formation Detail Panel");
            foreach(var image in detailPanel.GetComponentsInChildren<Image>(true))if(image.type==Image.Type.Sliced)image.pixelsPerUnitMultiplier=2f;
            StyleScout();
        }
        public void Refresh()
        {
            bool chosen=owner.SelectedIndex>=0&&owner.SelectedIndex<owner.availableRoster.Count;
            owner.informationWindow.SetActive(chosen&&owner.InformationOpen);prompt.SetActive(true);
            selectionTitle.text=chosen?"已选择「"+owner.availableRoster[owner.SelectedIndex].displayName+"」":"选择伙伴，安排前后排";
            selectionHint.text=chosen?"点击上方阵位可上阵或换位；点击角色卡片可再次查看详情。":"点击候补再点阵位，或长按卡片拖入；已上阵角色可交换位置。";
            int count=owner.availableRoster.Count-owner.State.Count;
            reserveCount.text=count==0?"0 名":count+" 名 · 上下滑动查看";emptyReserve.SetActive(count==0);
            owner.teamCount.text="出战队伍  "+owner.State.Count+" / 6";
            ready.text=owner.State.Count==0?"请至少上阵一名伙伴":"队伍已就绪 · 出战 "+owner.State.Count+" / 6";
            ready.color=owner.State.Count==0?PrototypeUI.Hex("E4A89A"):MapPresentation.Cyan;
            if(!chosen)return;
            var unit=owner.availableRoster[owner.SelectedIndex];int slot=owner.State.FindSlot(owner.SelectedIndex);
            owner.informationTitle.text=unit.displayName;
            classification.text=RarityRules.Name(unit.rarity)+" · "+unit.RoleName+" · "+unit.ElementName+"属性 · "+(slot<0?"候补":(slot<3?"前排":"后排")+new[]{"左","中","右"}[slot%3]);
            owner.informationStats.text="生命  "+unit.maxHealth+"       攻击  "+unit.attack+"\n防御  "+unit.defense+"        速度  "+unit.speed;
            string path="CardPortraits/"+unit.imagePath;
            portrait.SetPath(!string.IsNullOrEmpty(unit.imagePath)&&Resources.Load<Texture2D>(path)!=null?path:unit.imagePath);
            skillHeading.text="技能介绍 · "+unit.SkillName;
            string description="消耗 "+CombatRules.SkillEnergyCost+" 点技力，战斗中自动释放。\n"+unit.SkillDescription
                +(unit.doubleAttack?"\n道具加成：普攻二连击，每次行动仅获得1点技力。":"");
            InventoryPresentation.SetDescription(owner.informationSkill,skillScroll,description);
        }
        private void StyleScout()
        {
            var root=owner.enemyOverlay.transform;owner.enemyOverlay.GetComponent<Image>().color=new Color(.02f,.05f,.08f,.94f);
            var report=root.Find("Scout Report");report.GetComponent<Image>().color=MapPresentation.Navy;
            foreach(var label in root.GetComponentsInChildren<Text>(true))label.color=MapPresentation.Muted;
            foreach(var image in report.GetComponentsInChildren<Image>(true))if(image.transform!=report)image.color=MapPresentation.Gold;
            root.Find("Scout Eyebrow").GetComponent<Text>().color=MapPresentation.Gold;
            var title=root.Find("Scout Heading").GetComponent<Text>();title.text="敌方阵容";title.color=MapPresentation.Ivory;
            owner.enemyDetails.color=MapPresentation.Ivory;
            owner.enemyDetails.resizeTextForBestFit=true;owner.enemyDetails.resizeTextMinSize=14;owner.enemyDetails.resizeTextMaxSize=18;
            StyleButton(owner.closeEnemyButton,new Vector2(0,-456),new Vector2(530,71),"返回布阵",24,true);
        }
    }
}
