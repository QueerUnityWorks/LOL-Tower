using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    public sealed class BattleStatusStrip : MonoBehaviour
    {
        public const int MaximumVisible = 3;
        private Fighter fighter;
        private RectTransform card;
        private readonly Image[] badges = new Image[MaximumVisible];
        private readonly BattleStatusIcon[] icons = new BattleStatusIcon[MaximumVisible];
        private readonly Text[] durations = new Text[MaximumVisible];
        private int version = -1;
        private Vector2 lastSize;
        public int VisibleCount { get; private set; }

        public void Bind(Fighter value, Font font)
        {
            if (card == null) card = (RectTransform)transform.parent;
            if (badges[0] == null)
            {
                for (int i=0;i<MaximumVisible;i++)
                {
                    badges[i]=PrototypeUI.Panel("Effect " + (i+1),transform,Vector2.zero,new Vector2(28,28),PrototypeUI.Hex("142536"));
                    icons[i]=PrototypeUI.Rect("Symbol",badges[i].transform,new Vector2(0,2),new Vector2(20,20)).gameObject.AddComponent<BattleStatusIcon>();
                    icons[i].raycastTarget=false;
                    durations[i]=PrototypeUI.Label("Duration",badges[i].transform,"",new Vector2(7,-8),new Vector2(14,12),9,BattlePresentation.Ivory);
                    durations[i].font=font;durations[i].fontStyle=FontStyle.Bold;
                }
            }
            if (fighter != value) { fighter=value;version=-1; }
            Refresh();
        }
        private void LateUpdate()
        {
            if(card!=null && (lastSize!=card.rect.size || (fighter!=null && version!=fighter.statuses.Version))) Refresh();
        }
        private void Refresh()
        {
            if(card==null || badges[0]==null)return;
            VisibleCount=fighter==null?0:Mathf.Min(MaximumVisible,fighter.statuses.Entries.Count);
            version=fighter==null?-1:fighter.statuses.Version;lastSize=card.rect.size;
            float size=Mathf.Clamp(card.rect.width*.19f,24,38);
            for(int i=0;i<MaximumVisible;i++)
            {
                badges[i].gameObject.SetActive(i<VisibleCount);
                if(i>=VisibleCount)continue;
                var status=fighter.statuses.Entries[i];
                badges[i].rectTransform.sizeDelta=new Vector2(size,size);
                badges[i].rectTransform.anchoredPosition=new Vector2(card.rect.width/2-size/2-7,card.rect.height*.29f-i*(size+5));
                icons[i].rectTransform.sizeDelta=new Vector2(size*.68f,size*.68f);
                icons[i].SetKind(status.Kind);
                durations[i].text=status.RemainingTurns<0?"":status.RemainingTurns.ToString();
                durations[i].rectTransform.anchoredPosition=new Vector2(size*.26f,-size*.28f);
                badges[i].color=Color.Lerp(PrototypeUI.Hex("0A1626"),BattleStatusIcon.Tint(status.Kind),.17f);
            }
            transform.SetAsLastSibling();
        }
    }
}
