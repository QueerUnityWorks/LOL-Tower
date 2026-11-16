using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    // Presentation only: targeting, damage, energy and rewards remain in CombatRules.
    public sealed class BattlePresentation
    {
        public static readonly Color Navy = PrototypeUI.Hex("0C1929");
        public static readonly Color Ivory = PrototypeUI.Hex("F8EED8");
        public static readonly Color Cyan = PrototypeUI.Hex("76E5F1");
        public static readonly Color Gold = PrototypeUI.Hex("DCC18B");
        private readonly BattleController battle;
        private readonly RectTransform root;
        private readonly GameObject host;
        private readonly Image dim;
        private readonly BattleEffectGraphic effect;
        private readonly RectTransform banner;
        private readonly ResourceArtwork artwork;
        private readonly Text skillTitle, actorTitle, skillDetail;
        public string Stage { get; private set; } = "Idle";
        public float Progress => effect.progress;

        public static Vector2 SlotPosition(bool enemy, int slot)
        {
            int column = slot % 3; bool rear = slot >= 3;
            float x = enemy ? (rear ? 430 : 350) + column * 220 : (rear ? 210 : 290) + column * 220;
            float y = enemy ? (rear ? 320 : 605) : (rear ? 1535 : 1250);
            return new Vector2((x - 540) / 1.2f, (960 - y) / 1.2f);
        }

        private static void Place(Transform t, Vector2 pos, Vector2 size)
        {
            var rect = (RectTransform)t; rect.anchoredPosition = pos; rect.sizeDelta = size;
        }
        private static void PlaceLabel(Transform root, string name, Vector2 pos, Vector2 size, int font, Color color, string value = null)
        {
            var child = root.Find(name); if (child == null) return;
            Place(child, pos, size); var text = child.GetComponent<Text>();
            if (text == null) return;
            text.fontSize = font; text.color = color; text.verticalOverflow = VerticalWrapMode.Overflow;
            if (value != null) text.text = value;
        }
        public static void ApplyLayout(BattleController owner)
        {
            Transform parent = owner.combatRoot.transform;
            var wash = parent.Find("Combat Wash"); if (wash != null) wash.GetComponent<Image>().color = Navy;
            var arena = parent.Find("Battle Arena Artwork");
            if (arena == null) arena = ResourceArtwork.Create(parent, "Battle Arena Artwork", "BattlePresentation/Arena", Vector2.zero, new Vector2(900,1600)).transform;
            arena.SetAsFirstSibling(); if (wash != null) wash.SetAsFirstSibling();
            foreach (string old in new[] { "Combat Divider", "Battle Rule Hint", "Combat Footer" })
            { var child = parent.Find(old); if (child != null) child.gameObject.SetActive(false); }
            PlaceLabel(parent, "Combat Chapter", new Vector2(-292,763), new Vector2(245,40), 25, Gold, "符文远征");
            PlaceLabel(parent, "Combat Phase", new Vector2(-292,738), new Vector2(245,18), 13, PrototypeUI.Hex("9AAFC0"), "自动战斗");
            PlaceLabel(parent, "Combat Round", new Vector2(0,760), new Vector2(280,45), 27, Ivory);
            owner.roundLabel.alignment = TextAnchor.MiddleCenter;
            ResizeButton(owner.speedButton, new Vector2(373,756), new Vector2(82,45), "1×", 22);
            PlaceLabel(parent, "Enemy Combat Banner", new Vector2(-380,590), new Vector2(75,34), 22, PrototypeUI.Hex("E7A89E"), "敌方");
            PlaceLabel(parent, "Enemy Rear Banner", new Vector2(-380,552), new Vector2(75,26), 15, PrototypeUI.Hex("9AAFC0"));
            PlaceLabel(parent, "Enemy Front Banner", new Vector2(-380,310), new Vector2(75,26), 15, PrototypeUI.Hex("9AAFC0"));
            PlaceLabel(parent, "Player Combat Banner", new Vector2(364,-195), new Vector2(80,34), 22, Cyan, "我方");
            PlaceLabel(parent, "Player Front Banner", new Vector2(364,-230), new Vector2(80,26), 15, PrototypeUI.Hex("9AAFC0"));
            PlaceLabel(parent, "Player Rear Banner", new Vector2(364,-461), new Vector2(80,26), 15, PrototypeUI.Hex("9AAFC0"));
            PlaceLabel(parent, "Action Announcement", new Vector2(0,0), new Vector2(690,100), 27, Gold, "蓄势待发");
            for (int i = 0; i < owner.playerCards.Length; i++)
            {
                foreach (bool enemy in new[] { false, true })
                {
                    CardView card = enemy ? owner.enemyCards[i] : owner.playerCards[i];
                    PrototypeUI.SetPortraitCard(card, 145);
                    card.Rect.anchoredPosition = SlotPosition(enemy,i);
                    card.homePosition = card.Rect.anchoredPosition;
                }
            }
        }
        public static void ResizeButton(Button button, Vector2 pos, Vector2 size, string caption, int fontSize)
        {
            Place(button.transform,pos,size);
            var label = button.GetComponentInChildren<Text>();
            Place(label.transform,Vector2.zero,size-new Vector2(16,6)); label.text=caption; label.fontSize=fontSize; label.color=Ivory;
            foreach (string name in new[] { "Top Border", "Bottom Border", "Left Border", "Right Border" })
            {
                var child=button.transform.Find(name); if(child==null)continue;
                bool horizontal=name=="Top Border" || name=="Bottom Border";
                Place(child,horizontal ? new Vector2(0,(name=="Top Border"?1:-1)*(size.y/2-2)) : new Vector2((name=="Right Border"?1:-1)*(size.x/2-2),0),
                    horizontal?new Vector2(size.x-4,1):new Vector2(1,size.y-4));
                child.GetComponent<Image>().color=Gold;
            }
        }

        public BattlePresentation(BattleController owner)
        {
            battle=owner; root=(RectTransform)owner.combatRoot.transform;
            host=PrototypeUI.Rect("Battle Presentation",root,Vector2.zero,new Vector2(900,1600)).gameObject;
            var group=host.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
            dim=PrototypeUI.Panel("Skill Dim",host.transform,new Vector2(0,15),new Vector2(900,1290),new Color(0,.02f,.06f,.4f));
            effect=PrototypeUI.Rect("Animated Battle Effects",host.transform,Vector2.zero,new Vector2(900,1600)).gameObject.AddComponent<BattleEffectGraphic>();
            effect.raycastTarget=false;
            banner=PrototypeUI.Panel("Skill Cut In",host.transform,new Vector2(0,13),new Vector2(842,215),Navy).rectTransform;
            artwork=ResourceArtwork.Create(banner,"Skill Hero Artwork",null,new Vector2(-280,0),new Vector2(280,211),true);
            var fade=PrototypeUI.Rect("Skill Art Fade",banner,new Vector2(-150,0),new Vector2(215,65)).gameObject.AddComponent<RawImage>();
            fade.texture=Resources.Load<Texture2D>("CardPortraits/NameShade");fade.rectTransform.localRotation=Quaternion.Euler(0,0,90);fade.raycastTarget=false;
            PrototypeUI.Panel("Skill Top Line",banner,new Vector2(0,106),new Vector2(842,2),Gold);
            PrototypeUI.Panel("Skill Bottom Line",banner,new Vector2(0,-106),new Vector2(842,2),Cyan);
            actorTitle=PrototypeUI.Label("Skill Actor",banner,"",new Vector2(124,65),new Vector2(500,30),19,Cyan,TextAnchor.MiddleLeft);
            skillTitle=PrototypeUI.Label("Skill Title",banner,"",new Vector2(124,7),new Vector2(500,78),49,Ivory,TextAnchor.MiddleLeft);
            skillTitle.fontStyle=FontStyle.Bold;skillTitle.resizeTextForBestFit=true;skillTitle.resizeTextMinSize=25;skillTitle.resizeTextMaxSize=49;
            skillDetail=PrototypeUI.Label("Skill Type",banner,"",new Vector2(124,-68),new Vector2(500,30),19,Gold,TextAnchor.MiddleLeft);
            Reset();
        }
        private Vector2 Position(CardView view) { return root.InverseTransformPoint(view.Rect.position); }
        private IEnumerator Animate(float duration, Action<float> frame)
        {
            float time=0;frame(0);
            while(time<duration)
            {
                if(battle.IsPaused){yield return null;continue;}
                time+=Time.unscaledDeltaTime*battle.animationSpeed;
                frame(Mathf.Clamp01(time/duration));effect.SetVerticesDirty();yield return null;
            }
            while(battle.IsPaused) yield return null;
        }
        public IEnumerator Charge(Fighter actor, CardView view, bool skill)
        {
            host.SetActive(true);host.transform.SetAsLastSibling();Stage="Charge";
            effect.mode=1;effect.origin=Position(view);effect.tint=actor.definition.SupportSkill?PrototypeUI.Hex("8DE6B3"):Cyan;
            dim.gameObject.SetActive(skill);banner.gameObject.SetActive(false);
            yield return Animate(skill?.28f:.1f,p=>{ effect.progress=p;view.Rect.localScale=Vector3.one*(1+Mathf.Sin(p*Mathf.PI/2)*.06f); });
            if(!skill)yield break;
            Stage="CutIn";
            string portrait=actor.definition.id=="HERO_003"?"BattlePresentation/LuxSkill":"CardPortraits/"+actor.definition.imagePath;
            if(Resources.Load<Texture2D>(portrait)==null)portrait=actor.definition.imagePath;
            artwork.SetPath(portrait);actorTitle.text=actor.definition.displayName+" · 技能释放";skillTitle.text=actor.definition.SkillName;
            skillDetail.text=actor.definition.SupportSkill?"守护之光 / 援护队友":actor.definition.role==Role.Mage?"元素爆发 / 法术冲击":actor.definition.role==Role.Ranger?"精准射击 / 远程打击":"蓄力斩击 / 近战爆发";
            banner.gameObject.SetActive(true);
            yield return Animate(.42f,p=>{effect.progress=p;banner.anchoredPosition=new Vector2(Mathf.Lerp(-90,0,Mathf.SmoothStep(0,1,p)),13);});
        }
        public IEnumerator Travel(Fighter actor, CardView view, IList<CardView> targets, bool skill)
        {
            Stage="Travel";effect.mode=2;effect.origin=Position(view);effect.targets.Clear();
            foreach(var target in targets)effect.targets.Add(Position(target));
            effect.support=skill&&actor.definition.SupportSkill;effect.beam=skill&&actor.definition.role==Role.Mage;
            effect.melee=!effect.support&&(actor.definition.role==Role.Warrior||actor.definition.role==Role.Guardian);
            effect.tint=effect.support?PrototypeUI.Hex("8DE6B3"):skill?Cyan:Gold;
            yield return Animate(skill?.2f:.14f,p=>effect.progress=p);
        }
        public IEnumerator Impact(IList<CombatEffect> effects, Dictionary<Fighter,CardView> views, bool skill)
        {
            Stage="Impact";effect.mode=3;effect.targets.Clear();
            var floating=new List<Text>();var start=new List<Vector2>();var groups=new Dictionary<Fighter,string>();
            foreach(var hit in effects)
            {
                if(!views.ContainsKey(hit.target))continue;
                bool harmful=hit.damage>0||hit.absorbed>0||hit.energyLost>0;
                var view=views[hit.target];view.Tint(PrototypeUI.Hex(harmful?"B94D51":"477862"));
                if(!groups.ContainsKey(hit.target)){groups[hit.target]="";effect.targets.Add(Position(view));}
                string value=hit.damage>0?"−"+hit.damage:hit.absorbed>0?"格挡":hit.healing>0?"+"+hit.healing:"";
                if(hit.shielding>0)value+=(value.Length>0?"\n":"")+"护盾 +"+hit.shielding;
                if(hit.absorbed>0&&hit.damage>0)value+="\n护盾 −"+hit.absorbed;
                if(hit.energyLost>0)value+="\n技力 −"+hit.energyLost;
                if(!string.IsNullOrEmpty(hit.status))value+="\n"+hit.status;
                groups[hit.target]+=(groups[hit.target].Length>0?"\n":"")+value;
            }
            foreach(var pair in groups)
            {
                var pos=Position(views[pair.Key])+new Vector2(0,44);
                var text=PrototypeUI.Label("Floating Combat Number",host.transform,pair.Value,pos,new Vector2(235,115),skill?38:31,
                    pair.Value.StartsWith("+")||pair.Value.StartsWith("护盾 +")?PrototypeUI.Hex("ABF2BA"):PrototypeUI.Hex("FFDB9D"));
                text.fontStyle=FontStyle.Bold;text.verticalOverflow=VerticalWrapMode.Overflow;
                var shadow=text.gameObject.AddComponent<Outline>();shadow.effectColor=Navy;shadow.effectDistance=new Vector2(1.5f,-1.5f);
                floating.Add(text);start.Add(pos);
            }
            yield return Animate(skill?.52f:.36f,p=>
            {
                effect.progress=p;
                if(p>.38f){banner.gameObject.SetActive(false);dim.gameObject.SetActive(false);}
                for(int i=0;i<floating.Count;i++)
                {
                    floating[i].rectTransform.anchoredPosition=start[i]+new Vector2(0,p*35);
                    floating[i].rectTransform.localScale=Vector3.one*(1+Mathf.Sin(Mathf.Clamp01(p*3)*Mathf.PI)*.13f);
                    var c=floating[i].color;c.a=1-Mathf.Clamp01((p-.7f)/.3f);floating[i].color=c;
                }
                // Shake only the affected cards, keeping headers and controls readable.
                foreach(var pair in groups)
                {var view=views[pair.Key];view.Rect.anchoredPosition=view.homePosition+new Vector2(p<.35f?Mathf.Sin(p*75)*(1-p/.35f)*(skill?4:2):0,0);}
            });
            foreach(var text in floating){text.gameObject.SetActive(false);UnityEngine.Object.Destroy(text.gameObject);}
        }
        public void Reset()
        {
            Stage="Idle";effect.mode=0;effect.targets.Clear();effect.SetVerticesDirty();dim.gameObject.SetActive(false);banner.gameObject.SetActive(false);host.SetActive(false);
        }
    }
}
