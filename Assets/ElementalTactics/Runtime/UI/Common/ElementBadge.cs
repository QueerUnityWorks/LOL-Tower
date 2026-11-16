using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    // Geometry keeps the three attributes readable at mobile card sizes.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ElementBadge : MaskableGraphic
    {
        [SerializeField] private Element element;
        public Element CurrentElement => element;
        public static ElementBadge Create(Transform parent, Vector2 position, float size, Element value)
        {
            var badge = PrototypeUI.Rect("Element Badge", parent, position, Vector2.one * size).gameObject.AddComponent<ElementBadge>();
            badge.raycastTarget = false;
            badge.SetElement(value);
            return badge;
        }
        public void SetElement(Element value)
        {
            if (element == value) return;
            element = value;
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Disc(vh, .49f, PrototypeUI.Hex("D9B979"));
            Disc(vh, .445f, PrototypeUI.Hex("101E30"));
            var tint = PrototypeUI.Hex(element == Element.Fire ? "FFA66B" : element == Element.Water ? "72D8FF" : "A6E27B");
            if (element == Element.Water)
            {
                Fan(vh, new Vector2(0,-.08f), tint, new[] {
                    new Vector2(0,.34f), new Vector2(.12f,.15f), new Vector2(.22f,-.04f),
                    new Vector2(.23f,-.15f), new Vector2(.16f,-.26f), new Vector2(0,-.30f),
                    new Vector2(-.16f,-.26f), new Vector2(-.23f,-.15f), new Vector2(-.22f,-.04f), new Vector2(-.12f,.15f) });
                Line(vh,new Vector2(-.13f,-.13f),new Vector2(-.08f,-.21f),.035f,Color.white);
            }
            else if (element == Element.Fire)
            {
                Fan(vh,new Vector2(0,-.12f),tint,new[] {
                    new Vector2(.02f,.34f),new Vector2(.11f,.10f),new Vector2(.22f,.20f),
                    new Vector2(.27f,-.09f),new Vector2(.18f,-.27f),new Vector2(0,-.33f),
                    new Vector2(-.18f,-.27f),new Vector2(-.27f,-.08f),new Vector2(-.17f,.18f),new Vector2(-.10f,.01f) });
                Fan(vh,new Vector2(0,-.20f),PrototypeUI.Hex("FFE6A1"),new[] {
                    new Vector2(.02f,.03f),new Vector2(.10f,-.16f),new Vector2(.05f,-.26f),
                    new Vector2(-.05f,-.26f),new Vector2(-.10f,-.17f) });
            }
            else
            {
                Fan(vh,Vector2.zero,tint,new[] { new Vector2(.27f,.30f),new Vector2(.28f,.04f),
                    new Vector2(.15f,-.19f),new Vector2(-.09f,-.25f),new Vector2(-.23f,-.12f),
                    new Vector2(-.24f,.09f),new Vector2(-.07f,.24f) });
                var vein=PrototypeUI.Hex("29543E");
                Line(vh,new Vector2(-.23f,-.31f),new Vector2(.18f,.21f),.04f,vein);
                Line(vh,new Vector2(-.04f,-.07f),new Vector2(-.14f,.10f),.028f,vein);
                Line(vh,new Vector2(.04f,.03f),new Vector2(.19f,.02f),.028f,vein);
            }
        }
        private void Disc(VertexHelper vh,float radius,Color tint)
        {
            var points=new Vector2[40];
            for(int i=0;i<points.Length;i++){float a=i*Mathf.PI*2/points.Length;points[i]=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;}
            Fan(vh,Vector2.zero,tint,points);
        }
        private void Fan(VertexHelper vh,Vector2 center,Color tint,Vector2[] points)
        {
            int start=vh.currentVertCount;Vertex(vh,center,tint);
            foreach(var point in points)Vertex(vh,point,tint);
            for(int i=0;i<points.Length;i++)vh.AddTriangle(start,start+1+i,start+1+(i+1)%points.Length);
        }
        private void Vertex(VertexHelper vh,Vector2 point,Color tint)
        {
            var r=rectTransform.rect;
            vh.AddVert(new Vector3(r.center.x+point.x*r.width,r.center.y+point.y*r.height),tint*color,Vector2.zero);
        }
        private void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint)
        {
            var d=(b-a).normalized;var offset=new Vector2(-d.y,d.x)*width*.5f;
            Fan(vh,(a+b)*.5f,tint,new[]{a+offset,b+offset,b-offset,a-offset});
        }
    }
}
