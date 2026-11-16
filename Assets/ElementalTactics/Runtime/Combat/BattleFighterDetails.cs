using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    // Bottom sheet in the existing 900 x 1600 safe-area coordinate system.
    public sealed class BattleFighterDetails : MonoBehaviour
    {
        private Fighter fighter;
        private ResourceArtwork portrait;
        private Text heading, subtitle, identity, health, energy, attack, defense, speed, shield;
        private Text skillName, skillBody, effectHeading, empty;
        private ScrollRect skillScroll, effectScroll;
        private int statusVersion = -1;
        private string skillValue;
        public bool IsOpen => gameObject.activeSelf;
        public Fighter InspectedFighter => fighter;
        public ScrollRect EffectsScroll => effectScroll;

        public static BattleFighterDetails Create(BattleController owner, Transform parent)
        {
            var shade=PrototypeUI.Panel("Battle Character Details",parent,Vector2.zero,new Vector2(900,1600),new Color(.01f,.03f,.06f,.64f));
            shade.raycastTarget=true;
            var result=shade.gameObject.AddComponent<BattleFighterDetails>();
            var dismiss=shade.gameObject.AddComponent<Button>();dismiss.targetGraphic=shade;
            dismiss.transition=Selectable.Transition.None;dismiss.onClick.AddListener(result.Close);
            result.Build();result.gameObject.SetActive(false);return result;
        }
        private Text Label(string name,Transform parent,string value,Vector2 pos,Vector2 size,int fontSize,Color color)
        {
            return PrototypeUI.Label(name,parent,value,pos,size,fontSize,color,TextAnchor.MiddleLeft);
        }
        private void Build()
        {
            var paper=InventoryPresentation.Panel("Character Bottom Sheet",transform,new Vector2(0,-348),new Vector2(864,880),BattlePresentation.Navy,BattlePresentation.Gold);
            paper.raycastTarget=true;
            // Consume clicks inside the sheet; only its outside backdrop dismisses it.
            var blocker=paper.gameObject.AddComponent<Button>();blocker.targetGraphic=paper;blocker.transition=Selectable.Transition.None;
            var root=paper.transform;
            PrototypeUI.Panel("Drag Accent",root,new Vector2(0,424),new Vector2(64,3),BattlePresentation.Gold);
            heading=Label("Character Heading",root,"角色详情",new Vector2(-66,387),new Vector2(660,48),30,BattlePresentation.Ivory);
            subtitle=Label("Side and Position",root,"",new Vector2(-66,350),new Vector2(660,28),18,BattlePresentation.Cyan);
            var close=PrototypeUI.Button("Close Character Details",root,"关闭",new Vector2(345,386),new Vector2(106,48),false);
            close.GetComponentInChildren<Text>().fontSize=20;close.onClick.AddListener(Close);
            InventoryPresentation.ArtworkFrame("Portrait Frame",root,new Vector2(-320,216),new Vector2(148,222),BattlePresentation.Gold);
            portrait=ResourceArtwork.Create(root,"Character Portrait",null,new Vector2(-320,216),new Vector2(142,216),true);
            identity=Label("Character Identity",root,"",new Vector2(91,306),new Vector2(590,34),23,BattlePresentation.Gold);
            health=Label("Health Stat",root,"",new Vector2(91,260),new Vector2(590,32),22,BattlePresentation.Ivory);
            energy=Label("Energy Stat",root,"",new Vector2(91,220),new Vector2(590,32),22,BattlePresentation.Cyan);
            attack=Label("Attack Stat",root,"",new Vector2(-59,177),new Vector2(290,32),22,BattlePresentation.Ivory);
            defense=Label("Defense Stat",root,"",new Vector2(241,177),new Vector2(290,32),22,BattlePresentation.Ivory);
            speed=Label("Speed Stat",root,"",new Vector2(-59,134),new Vector2(290,32),22,BattlePresentation.Ivory);
            shield=Label("Shield Stat",root,"",new Vector2(241,134),new Vector2(290,32),22,BattlePresentation.Ivory);
            PrototypeUI.Panel("Skill Section Rule",root,new Vector2(0,88),new Vector2(804,1),InventoryPresentation.Border);
            skillName=Label("Skill Heading",root,"技能介绍",new Vector2(0,62),new Vector2(798,36),23,BattlePresentation.Gold);
            InventoryPresentation.FitLine(heading,22);InventoryPresentation.FitLine(identity,18);InventoryPresentation.FitLine(skillName,18);
            skillBody=InventoryPresentation.Description("Skill Description",root,new Vector2(0,-29),new Vector2(804,138),out skillScroll);
            skillBody.color=BattlePresentation.Ivory;skillBody.fontSize=20;
            PrototypeUI.Panel("Effects Section Rule",root,new Vector2(0,-118),new Vector2(804,1),InventoryPresentation.Border);
            effectHeading=Label("Effects Heading",root,"当前效果",new Vector2(-75,-144),new Vector2(648,36),23,BattlePresentation.Gold);
            Label("Effects Scroll Hint",root,"上下滑动查看",new Vector2(306,-144),new Vector2(184,30),17,PrototypeUI.Hex("9AAFC0"));
            effectScroll=InventoryPresentation.Scroll("Current Status Details",root,new Vector2(0,-283),new Vector2(804,234),760);
            empty=Label("No Current Effects",effectScroll.content,"当前无状态效果",new Vector2(0,-90),new Vector2(700,45),22,PrototypeUI.Hex("9AAFC0"));
            empty.rectTransform.anchorMin=empty.rectTransform.anchorMax=new Vector2(.5f,1);
            empty.alignment=TextAnchor.MiddleCenter;
            var foot=Label("Inspection Pause Hint",root,"查看详情时战斗暂停 · 点击空白处或关闭返回",new Vector2(0,-421),new Vector2(804,26),16,PrototypeUI.Hex("9AAFC0"));
            foot.alignment=TextAnchor.MiddleCenter;
        }
        public void Open(Fighter value)
        {
            if(value==null)return;
            fighter=value;statusVersion=-1;skillValue=null;
            gameObject.SetActive(true);transform.SetAsLastSibling();
            string path="CardPortraits/"+value.definition.imagePath;
            portrait.SetPath(!string.IsNullOrEmpty(value.definition.imagePath)&&Resources.Load<Texture2D>(path)!=null?path:value.definition.imagePath);
            Refresh();InventoryPresentation.Top(effectScroll);
        }
        public void Close() { gameObject.SetActive(false);fighter=null; }
        private void LateUpdate()
        {
            if(fighter==null)return;
            // An animation started in the same frame must not cover the modal.
            transform.SetAsLastSibling();Refresh();
        }
        private void Refresh()
        {
            var d=fighter.definition;
            heading.text=d.displayName+" · 角色详情";
            subtitle.text=(fighter.enemy?"敌方":"我方")+" / "+(fighter.slot<3?"前排":"后排")+" · "+new[]{"左位","中位","右位"}[Mathf.Clamp(fighter.slot%3,0,2)]
                +(fighter.Alive?"":" · 已阵亡");
            identity.text=RarityRules.Name(d.rarity)+" · "+d.RoleName+" · "+d.ElementName+"属性";
            health.text="生命  "+fighter.health+" / "+d.maxHealth;
            energy.text="技力  "+fighter.energy+" / "+CombatRules.SkillEnergyCost+(fighter.energy>=CombatRules.SkillEnergyCost?"  · 技能就绪":"");
            attack.text="攻击  "+d.attack;defense.text="防御  "+CombatRules.EffectiveDefense(fighter)
                +(fighter.statuses.Has(BattleStatusKind.ArmorBreak)?"（基础 "+d.defense+"）":"");
            speed.text="速度  "+d.speed;shield.text="护盾  "+fighter.shield;
            skillName.text="技能介绍 · "+d.SkillName;
            string description="消耗 "+CombatRules.SkillEnergyCost+" 点技力\n"+d.SkillDescription+"\n"+d.TraitDescription;
            if(d.doubleAttack)description+="\n普攻二连击，每次行动仅获得1点技力。";
            if(d.openingShield>0)description+="\n额外开战护盾："+d.openingShield+"。";
            if(d.regeneration>0)description+="\n额外行动回血："+d.regeneration+"。";
            if(d.lifesteal>0)description+="\n额外吸血："+d.lifesteal+"%。";
            if(description!=skillValue){skillValue=description;InventoryPresentation.SetDescription(skillBody,skillScroll,description);}
            if(statusVersion!=fighter.statuses.Version)RefreshEffects();
        }
        private void RefreshEffects()
        {
            statusVersion=fighter.statuses.Version;
            float previous=effectScroll.content.rect.height<=effectScroll.viewport.rect.height+1?1:effectScroll.verticalNormalizedPosition;
            for(int i=effectScroll.content.childCount-1;i>=0;i--)
            {
                var child=effectScroll.content.GetChild(i);if(child==empty.transform)continue;
                child.gameObject.SetActive(false);Destroy(child.gameObject);
            }
            var statuses=fighter.statuses.Entries;
            empty.gameObject.SetActive(statuses.Count==0);
            effectHeading.text="当前效果 · "+statuses.Count;
            float top=8;
            foreach(var status in statuses)
            {
                var row=PrototypeUI.Rect("Status Detail "+status.Kind,effectScroll.content,new Vector2(0,-top),new Vector2(744,120));
                row.anchorMin=row.anchorMax=row.pivot=new Vector2(.5f,1);
                var tile=PrototypeUI.Panel("Effect Tile",row,new Vector2(-336,-36),new Vector2(48,48),Color.Lerp(BattlePresentation.Navy,BattleStatusIcon.Tint(status.Kind),.16f));
                tile.rectTransform.anchorMin=tile.rectTransform.anchorMax=new Vector2(.5f,1);
                var icon=PrototypeUI.Rect("Effect Symbol",tile.transform,Vector2.zero,new Vector2(34,34)).gameObject.AddComponent<BattleStatusIcon>();
                icon.raycastTarget=false;icon.SetKind(status.Kind);
                var name=Label("Effect Name",row,status.Name+"  ·  "+status.DurationText+(status.Stacks>1?"  ·  "+status.Stacks+"层":""),new Vector2(32,-25),new Vector2(624,32),22,BattleStatusIcon.Tint(status.Kind));
                name.rectTransform.anchorMin=name.rectTransform.anchorMax=new Vector2(.5f,1);
                string desc=status.Description+(string.IsNullOrEmpty(status.Source)?"":"\n来源："+status.Source);
                var text=Label("Effect Description",row,desc,new Vector2(32,-49),new Vector2(624,80),20,BattlePresentation.Ivory);
                text.rectTransform.anchorMin=text.rectTransform.anchorMax=text.rectTransform.pivot=new Vector2(.5f,1);
                text.alignment=TextAnchor.UpperLeft;
                float height=Mathf.Max(50,text.preferredHeight+4);text.rectTransform.sizeDelta=new Vector2(624,height);
                float rowHeight=height+66;row.sizeDelta=new Vector2(744,rowHeight);
                var line=PrototypeUI.Panel("Effect Divider",row,new Vector2(0,-rowHeight+6),new Vector2(720,1),InventoryPresentation.Border);
                line.rectTransform.anchorMin=line.rectTransform.anchorMax=new Vector2(.5f,1);
                top+=rowHeight;
            }
            effectScroll.content.sizeDelta=new Vector2(760,Mathf.Max(effectScroll.viewport.rect.height,top+4));
            Canvas.ForceUpdateCanvases();effectScroll.verticalNormalizedPosition=previous;
        }
    }
}
