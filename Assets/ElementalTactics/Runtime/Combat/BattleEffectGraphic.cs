using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    // Lightweight canvas meshes keep VFX aligned with UI cards at every resolution.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BattleEffectGraphic : MaskableGraphic
    {
        public int mode;
        public float progress;
        public Vector2 origin;
        public readonly List<Vector2> targets = new List<Vector2>();
        public Color tint = Color.cyan;
        public bool beam, support, melee;
        private void Segment(VertexHelper vh, Vector2 a, Vector2 b, float width, Color c)
        {
            if((b-a).sqrMagnitude<.001f)return;
            Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int i=vh.currentVertCount;
            vh.AddVert(a-n,c,Vector2.zero);vh.AddVert(a+n,c,Vector2.zero);vh.AddVert(b+n,c,Vector2.zero);vh.AddVert(b-n,c,Vector2.zero);
            vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i+2,i+3,i);
        }
        private void Ring(VertexHelper vh, Vector2 center, float radius, float aspect, float width, Color c)
        {
            for(int i=0;i<48;i++)
            {float a=i*Mathf.PI/24,b=(i+1)*Mathf.PI/24;Segment(vh,center+new Vector2(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius*aspect),center+new Vector2(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius*aspect),width,c);}
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(mode==0)return;
            Color glow=tint;glow.a=.16f;
            if(mode==1)
            {
                Ring(vh,origin,62+progress*35,.75f,12,glow);Ring(vh,origin,62+progress*35,.75f,2,tint);
                Ring(vh,origin,105-progress*35,.5f,2,tint);return;
            }
            foreach(Vector2 target in targets)
            {
                if(mode==2&&!support)
                {
                    if(beam)
                    {
                        Vector2 end=Vector2.Lerp(origin,target,Mathf.Clamp01(progress*2));
                        Segment(vh,origin,end,50,glow);Segment(vh,origin,end,18,tint);Segment(vh,origin,end,5,Color.white);
                    }
                    else
                    {
                        Vector2 head=Vector2.Lerp(origin,target,progress);Vector2 tail=Vector2.Lerp(origin,target,Mathf.Max(0,progress-.15f));
                        Segment(vh,tail,head,15,glow);Segment(vh,tail,head,4,tint);
                    }
                }
                else if(mode==3||support)
                {
                    Color c=tint;c.a=1-progress;glow.a=.18f*(1-progress);
                    if(mode==3&&beam&&progress<.4f&&!support)
                    {Segment(vh,origin,target,32,glow);Segment(vh,origin,target,9,c);Segment(vh,origin,target,3,Color.white);}
                    Ring(vh,target,25+progress*70,support?.6f:1,10,glow);Ring(vh,target,25+progress*70,support?.6f:1,2,c);
                    for(int i=0;i<12;i++)
                    {float a=i*Mathf.PI/6+.2f;Vector2 v=new Vector2(Mathf.Cos(a),Mathf.Sin(a));Segment(vh,target+v*(38+progress*55),target+v*(52+progress*70),2,c);}
                    if(melee&&!support){Segment(vh,target+new Vector2(-45,-50)*(1+progress),target+new Vector2(45,50)*(1+progress),8,glow);Segment(vh,target+new Vector2(-45,-50)*(1+progress),target+new Vector2(45,50)*(1+progress),3,c);}
                }
            }
        }
    }
}
