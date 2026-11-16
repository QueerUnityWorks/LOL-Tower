using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class AdventureArt : MaskableGraphic
    {
        public bool portrait;
        public Role role;
        public Element element;
        public string appearanceId;
        private VertexHelper mesh;

        protected override void OnEnable()
        {
            if (GetComponent<CanvasRenderer>() == null) gameObject.AddComponent<CanvasRenderer>();
            base.OnEnable();
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            mesh = helper;
            mesh.Clear();
            if (portrait) DrawPortrait();
            else DrawLandscape();
        }

        private void DrawLandscape()
        {
            Quad(0,0,1,1,PrototypeUI.Hex("D7DFE9"));
            Circle(new Vector2(.75f,.78f),.13f,PrototypeUI.Hex("B8C9DB"));
            Circle(new Vector2(.75f,.78f),.115f,PrototypeUI.Hex("D4DFE9"));
            for(int row=0;row<12;row++) for(int col=0;col<8;col++) {
                float x=col*.15f+(row%2)*.075f,y=row*.085f;
                var pts=new Vector2[6];
                for(int i=0;i<6;i++){float a=i*Mathf.PI/3;pts[i]=new Vector2(Mathf.Clamp01(x+Mathf.Cos(a)*.072f),Mathf.Clamp01(y+Mathf.Sin(a)*.046f));}
                Polygon(PrototypeUI.Hex((row+col)%3==0?"C4D3E2":"CFDBE7"),pts);
            }
            for(int i=0;i<15;i++) {
                float x=.04f+(i*.137f)%.92f,y=.08f+(i*.093f)%.85f;
                Polygon(PrototypeUI.Hex("D1AF67"),new Vector2(x,y+.007f),new Vector2(x+.003f,y),new Vector2(x,y-.007f),new Vector2(x-.003f,y));
            }
        }

        private void DrawPortrait()
        {
            if (!string.IsNullOrEmpty(appearanceId)) { DrawCharacter(); return; }
            Color accent = PrototypeUI.ElementColor(element);
            Circle(new Vector2(0.5f, 0.52f), 0.44f, Color.Lerp(PrototypeUI.Paper, accent, 0.16f));
            Color dark = PrototypeUI.Indigo;
            Polygon(dark, new Vector2(0.17f, 0.03f), new Vector2(0.29f, 0.39f), new Vector2(0.5f, 0.48f), new Vector2(0.73f, 0.34f), new Vector2(0.83f, 0.03f));
            Polygon(accent, new Vector2(0.25f, 0.04f), new Vector2(0.32f, 0.36f), new Vector2(0.48f, 0.43f), new Vector2(0.59f, 0.04f));
            Polygon(PrototypeUI.Paper, new Vector2(0.42f, 0.41f), new Vector2(0.5f, 0.28f), new Vector2(0.61f, 0.40f), new Vector2(0.50f, 0.10f));
            Circle(new Vector2(0.5f, 0.67f), 0.225f, dark);
            Circle(new Vector2(0.5f, 0.61f), 0.175f, PrototypeUI.Hex("EBC4A2"));
            Polygon(dark, new Vector2(0.28f, 0.67f), new Vector2(0.38f, 0.88f), new Vector2(0.61f, 0.89f), new Vector2(0.72f, 0.68f), new Vector2(0.57f, 0.77f), new Vector2(0.47f, 0.60f), new Vector2(0.45f, 0.76f));
            Quad(0.40f, 0.60f, 0.445f, 0.624f, dark);
            Quad(0.56f, 0.60f, 0.605f, 0.624f, dark);
            Quad(0.31f, 0.71f, 0.68f, 0.75f, accent);
            if (role == Role.Mage)
            {
                Polygon(accent, new Vector2(0.21f, 0.77f), new Vector2(0.43f, 1f), new Vector2(0.70f, 0.77f));
                Quad(0.79f, 0.05f, 0.82f, 0.7f, PrototypeUI.Gold);
                Circle(new Vector2(0.80f, 0.79f), 0.1f, accent);
            }
            else if (role == Role.Guardian)
                Polygon(PrototypeUI.Gold, new Vector2(0.64f, 0.46f), new Vector2(0.96f, 0.43f), new Vector2(0.92f, 0.13f), new Vector2(0.8f, 0.02f), new Vector2(0.65f, 0.14f));
            else if (role == Role.Warrior)
            {
                Polygon(PrototypeUI.Hex("F6F0DB"), new Vector2(0.76f, 0.19f), new Vector2(0.83f, 0.91f), new Vector2(0.88f, 0.98f), new Vector2(0.86f, 0.21f));
                Quad(0.70f, 0.18f, 0.92f, 0.22f, PrototypeUI.Gold);
            }
            else
            {
                Polygon(PrototypeUI.Gold, new Vector2(0.78f, 0.02f), new Vector2(0.91f, 0.40f), new Vector2(0.80f, 0.79f), new Vector2(0.97f, 0.40f));
                Quad(0.78f, 0.04f, 0.79f, 0.77f, dark);
            }
        }

        private void DrawCharacter()
        {
            // ResourceArtwork supplies named hero portraits; this is a neutral role fallback.
            string saved=appearanceId;
            appearanceId=null;
            DrawPortrait();
            appearanceId=saved;
        }

        private void Ellipse(Vector2 center, Vector2 radius, Color tint)
        {
            var points = new Vector2[48];
            for (int index = 0; index < points.Length; index++)
            {
                float angle = index * Mathf.PI * 2 / points.Length;
                points[index] = center + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y);
            }
            Polygon(tint, points);
        }

        private void Quad(float left, float bottom, float right, float top, Color tint)
        {
            Polygon(tint, new Vector2(left, bottom), new Vector2(left, top), new Vector2(right, top), new Vector2(right, bottom));
        }

        private void Circle(Vector2 center, float radius, Color tint)
        {
            var points = new Vector2[32];
            for (int index = 0; index < points.Length; index++)
            {
                float angle = index * Mathf.PI * 2 / points.Length;
                points[index] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            Polygon(tint, points);
        }

        private void Polygon(Color tint, params Vector2[] points)
        {
            int start = mesh.currentVertCount;
            Rect bounds = rectTransform.rect;
            foreach (var point in points)
                mesh.AddVert(new Vector3(bounds.xMin + point.x * bounds.width, bounds.yMin + point.y * bounds.height), tint, Vector2.zero);
            for (int index = 1; index < points.Length - 1; index++) mesh.AddTriangle(start, start + index, start + index + 1);
        }
    }
}
