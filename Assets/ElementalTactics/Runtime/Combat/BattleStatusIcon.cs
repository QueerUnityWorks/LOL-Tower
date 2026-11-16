using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    // Vector symbols stay sharp on mobile cards and in the larger detail rows.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BattleStatusIcon : MaskableGraphic
    {
        private BattleStatusKind kind;
        protected override void OnEnable()
        {
            if (GetComponent<CanvasRenderer>() == null) gameObject.AddComponent<CanvasRenderer>();
            base.OnEnable();
        }
        public static Color Tint(BattleStatusKind value)
        {
            switch(value)
            {
                case BattleStatusKind.Stun: return PrototypeUI.Hex("F6D77B");
                case BattleStatusKind.Poison: return PrototypeUI.Hex("9BDD78");
                case BattleStatusKind.Charm: return PrototypeUI.Hex("F398CB");
                case BattleStatusKind.Burn: return PrototypeUI.Hex("FFAB68");
                case BattleStatusKind.Silence: return PrototypeUI.Hex("B9A6F5");
                case BattleStatusKind.Taunt: return PrototypeUI.Hex("FFCF77");
                case BattleStatusKind.ArmorBreak: return PrototypeUI.Hex("8CC5EC");
                case BattleStatusKind.GrievousWounds: return PrototypeUI.Hex("F37F87");
                case BattleStatusKind.Cleanse: return PrototypeUI.Hex("AEF4E7");
                default: throw new System.ArgumentOutOfRangeException(nameof(value));
            }
        }
        public void SetKind(BattleStatusKind value) { kind = value; color = Tint(value); SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (kind == BattleStatusKind.Stun)
            {
                Star(vh, new Vector2(0, .08f), .39f, .17f, 5);
                Quad(vh, -.30f, -.40f, .30f, -.34f);
            }
            else if (kind == BattleStatusKind.Poison)
            {
                Fan(vh, new Vector2(0,-.1f), new[] { new Vector2(0,.47f),new Vector2(.28f,.02f),
                    new Vector2(.32f,-.18f),new Vector2(.22f,-.36f),new Vector2(0,-.43f),
                    new Vector2(-.22f,-.36f),new Vector2(-.32f,-.18f),new Vector2(-.28f,.02f) });
            }
            else if (kind == BattleStatusKind.Charm)
            {
                var points = new Vector2[40];
                for (int i = 0; i < points.Length; i++)
                {
                    float t = i * Mathf.PI * 2 / points.Length;
                    points[i] = new Vector2(16 * Mathf.Pow(Mathf.Sin(t),3) / 38,
                        (13*Mathf.Cos(t)-5*Mathf.Cos(2*t)-2*Mathf.Cos(3*t)-Mathf.Cos(4*t)) / 38 + .05f);
                }
                Fan(vh, Vector2.zero, points);
            }
            else if (kind == BattleStatusKind.Burn)
            {
                Fan(vh,new Vector2(0,-.14f),new[] {new Vector2(0,.49f),new Vector2(.13f,.17f),
                    new Vector2(.28f,.28f),new Vector2(.36f,-.12f),new Vector2(.22f,-.37f),
                    new Vector2(0,-.45f),new Vector2(-.25f,-.34f),new Vector2(-.36f,-.09f),
                    new Vector2(-.22f,.19f),new Vector2(-.15f,-.04f)});
            }
            else if (kind == BattleStatusKind.Silence)
            {
                // Closed mouth, crossed by a diagonal seal.
                Line(vh,new Vector2(-.36f,.05f),new Vector2(-.15f,.19f),.07f);
                Line(vh,new Vector2(-.15f,.19f),new Vector2(.15f,.19f),.07f);
                Line(vh,new Vector2(.15f,.19f),new Vector2(.36f,.05f),.07f);
                Line(vh,new Vector2(-.36f,.05f),new Vector2(-.15f,-.13f),.07f);
                Line(vh,new Vector2(-.15f,-.13f),new Vector2(.15f,-.13f),.07f);
                Line(vh,new Vector2(.15f,-.13f),new Vector2(.36f,.05f),.07f);
                Quad(vh,-.30f,.01f,.30f,.07f);
                Line(vh,new Vector2(-.34f,-.38f),new Vector2(.34f,.42f),.10f);
            }
            else if (kind == BattleStatusKind.Taunt)
            {
                Ring(vh,.28f,.07f);
                Quad(vh,-.055f,-.14f,.055f,.14f);
                Quad(vh,-.14f,-.055f,.14f,.055f);
                Quad(vh,-.045f,.29f,.045f,.46f);Quad(vh,-.045f,-.46f,.045f,-.29f);
                Quad(vh,-.46f,-.045f,-.29f,.045f);Quad(vh,.29f,-.045f,.46f,.045f);
            }
            else if (kind == BattleStatusKind.ArmorBreak)
            {
                // Shield halves separated by a lightning-shaped fracture.
                Fan(vh,new Vector2(-.18f,.06f),new[]{new Vector2(-.35f,.37f),new Vector2(-.02f,.37f),
                    new Vector2(-.11f,.07f),new Vector2(.01f,-.06f),new Vector2(-.10f,-.39f),new Vector2(-.30f,-.16f)});
                Fan(vh,new Vector2(.18f,.06f),new[]{new Vector2(.09f,.37f),new Vector2(.35f,.37f),
                    new Vector2(.30f,-.16f),new Vector2(.02f,-.45f),new Vector2(.15f,-.04f),new Vector2(.01f,.09f)});
            }
            else if (kind == BattleStatusKind.GrievousWounds)
            {
                // Broken healing cross with a diagonal wound.
                Quad(vh,-.10f,.11f,.10f,.40f);Quad(vh,-.10f,-.40f,.10f,-.11f);
                Quad(vh,-.40f,-.10f,-.11f,.10f);Quad(vh,.11f,-.10f,.40f,.10f);
                Line(vh,new Vector2(-.35f,-.35f),new Vector2(.35f,.35f),.075f);
            }
            else if (kind == BattleStatusKind.Cleanse)
            {
                Star(vh,new Vector2(-.07f,.01f),.37f,.10f,4);
                Star(vh,new Vector2(.29f,.29f),.14f,.045f,4);
                Star(vh,new Vector2(.25f,-.29f),.12f,.04f,4);
            }
        }
        private void Vertex(VertexHelper vh, Vector2 p)
        {
            var r = rectTransform.rect;
            vh.AddVert(new Vector3(r.center.x+p.x*r.width,r.center.y+p.y*r.height),color,Vector2.zero);
        }
        private void Fan(VertexHelper vh, Vector2 center, Vector2[] points)
        {
            int start=vh.currentVertCount; Vertex(vh,center);
            foreach(var point in points) Vertex(vh,point);
            for(int i=0;i<points.Length;i++) vh.AddTriangle(start,start+1+i,start+1+(i+1)%points.Length);
        }
        private void Star(VertexHelper vh,Vector2 center,float outer,float inner,int tips)
        {
            var points=new Vector2[tips*2];
            for(int i=0;i<points.Length;i++) {float a=Mathf.PI*.5f+i*Mathf.PI/tips;
                points[i]=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(i%2==0?outer:inner);}
            Fan(vh,center,points);
        }
        private void Quad(VertexHelper vh,float x0,float y0,float x1,float y1)
        { Fan(vh,new Vector2((x0+x1)/2,(y0+y1)/2),new[]{new Vector2(x0,y0),new Vector2(x0,y1),new Vector2(x1,y1),new Vector2(x1,y0)}); }
        private void Line(VertexHelper vh,Vector2 a,Vector2 b,float width)
        {
            Vector2 direction=(b-a).normalized;
            Vector2 offset=new Vector2(-direction.y,direction.x)*width*.5f;
            Fan(vh,(a+b)*.5f,new[]{a+offset,b+offset,b-offset,a-offset});
        }
        private void Ring(VertexHelper vh,float radius,float width)
        {
            for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI*2/24,b=(i+1)*Mathf.PI*2/24;
                Line(vh,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,width);
            }
        }
    }
}
