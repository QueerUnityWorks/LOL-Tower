using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StartScreenArt : MaskableGraphic
    {
        public enum Layer { Landscape, Spatula, Motes, Button }
        public Layer layer;
        private VertexHelper mesh;
        private Vector2 home;
        private float phase;
        private float designWidth;
        private float designHeight;

        protected override void OnEnable()
        {
            base.OnEnable();
            home = rectTransform.anchoredPosition;
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            phase = Time.unscaledTime;
            if (layer == Layer.Spatula) rectTransform.anchoredPosition = home + new Vector2(0, Mathf.Sin(phase * 1.25f) * 9);
            if (layer == Layer.Motes) SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            mesh = helper;
            mesh.Clear();
            designWidth = layer == Layer.Spatula ? 560 : layer == Layer.Button ? 620 : 900;
            designHeight = layer == Layer.Spatula ? 440 : layer == Layer.Button ? 108 : 1600;
            switch (layer)
            {
                case Layer.Landscape: DrawLandscape(); break;
                case Layer.Spatula: DrawSpatula(); break;
                case Layer.Motes: DrawMotes(); break;
                case Layer.Button: DrawButton(); break;
            }
        }

        private static Color Tint(string hex, float alpha = 1)
        {
            Color value = PrototypeUI.Hex(hex);
            value.a = alpha;
            return value;
        }

        private void DrawLandscape()
        {
            Gradient(0, 0, 900, 1600, Tint("172D4D"), Tint("080F21"));
            Glow(new Vector2(450, 790), new Vector2(590, 650), Tint("236DB4", .34f));
            Glow(new Vector2(450, 790), new Vector2(300, 360), Tint("42B5D3", .18f));
            for (int i=0;i<9;i++) {
                float x=70+i*96, y=550+Mathf.Sin(i*1.8f)*100;
                Quad(x,y,x+46,1090,Tint("183A5D"));
                Diamond(new Vector2(x+23,y),new Vector2(30,58),Tint("30618A"));
                Stroke(new Vector2(x+23,y-45),new Vector2(x+23,y+25),2,Tint("D6B777",.65f));
            }
            for (int row=0;row<7;row++) for(int col=0;col<7;col++) {
                float x=80+col*120+(row%2)*60, y=800+row*75;
                var pts=new Vector2[7];
                for(int j=0;j<=6;j++) {float a=j*Mathf.PI/3;pts[j]=new Vector2(x+Mathf.Cos(a)*59,y+Mathf.Sin(a)*38);}
                for(int j=0;j<6;j++) Stroke(pts[j],pts[j+1],1.3f,Tint("78BFD8",.18f));
            }
            Ring(new Vector2(450,840),new Vector2(315,315),2,Tint("C9A860",.35f));
            Ring(new Vector2(450,840),new Vector2(330,330),1,Tint("79CBE0",.22f));
            Gradient(0,1150,900,1600,Tint("0C1529",0),Tint("0C1529"));
            for(int i=0;i<48;i++) Diamond(new Vector2(60+Mathf.Repeat(i*137.3f,780),130+Mathf.Repeat(i*81.7f,1180)),new Vector2(1.3f,2.5f),Tint("F6DB99",.55f));
            Stroke(new Vector2(44,80),new Vector2(44,1510),1,Tint("D6B777",.35f));
            Stroke(new Vector2(856,80),new Vector2(856,1510),1,Tint("D6B777",.35f));
        }

        private void DrawSpatula()
        {
            Glow(new Vector2(280,220),new Vector2(260,210),Tint("57C8F2",.26f));
            Ring(new Vector2(280,220),new Vector2(190,190),2,Tint("D5B165",.8f));
            Ring(new Vector2(280,220),new Vector2(205,205),1,Tint("75D5F0",.5f));
            // Golden spatula emblem, drawn in the existing hero rectangle.
            Polygon(Tint("715024"),new Vector2(180,373),new Vector2(211,395),new Vector2(340,204),new Vector2(309,183));
            Polygon(Tint("EDC466"),new Vector2(173,363),new Vector2(201,383),new Vector2(333,192),new Vector2(305,173));
            Polygon(Tint("FFF0B0"),new Vector2(173,363),new Vector2(181,369),new Vector2(313,178),new Vector2(305,173));
            Polygon(Tint("9C7437"),new Vector2(259,178),new Vector2(290,102),new Vector2(337,65),new Vector2(422,125),new Vector2(403,182),new Vector2(343,229));
            Polygon(Tint("EEC775"),new Vector2(254,169),new Vector2(284,94),new Vector2(332,57),new Vector2(417,117),new Vector2(397,174),new Vector2(337,221));
            for(int i=0;i<3;i++) Stroke(new Vector2(300+i*28,107+i*19),new Vector2(278+i*28,155+i*19),7,Tint("8B692F"));
            Diamond(new Vector2(280,220),new Vector2(22,33),Tint("61D8F0"));
            Diamond(new Vector2(280,220),new Vector2(12,22),Tint("D6FFFF"));
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Diamond(new Vector2(280+Mathf.Cos(a)*198,220+Mathf.Sin(a)*198),new Vector2(4,7),Tint("EBCB80"));}
        }

        private void DrawMotes()
        {
            for (int index = 0; index < 27; index++)
            {
                float horizontal = 110 + Mathf.Repeat(index * 153.7f, 680) + Mathf.Sin(phase * 0.35f + index) * 12;
                float vertical = 480 + Mathf.Repeat(index * 97.3f - phase * (6 + index % 4), 690);
                float pulse = 0.4f + Mathf.Sin(phase * 1.3f + index * 2.1f) * 0.25f;
                Glow(new Vector2(horizontal, vertical), new Vector2(9, 9), Tint("6AE4DF", pulse * 0.35f));
                Diamond(new Vector2(horizontal, vertical), new Vector2(1.6f, 3.4f), Tint("D2FFF0", pulse));
            }
        }

        private void DrawButton()
        {
            Bevel(0,6,620,108,22,Tint("050B17",.8f));
            Bevel(0,0,620,100,22,Tint("E8C575"));
            Bevel(3,3,617,97,20,Tint("765526"));
            Bevel(7,7,613,93,18,Tint("213F61"));
            Gradient(28,11,592,89,Tint("375D80"),Tint("142B46"));
            Stroke(new Vector2(63,3),new Vector2(557,3),1,Tint("FFF0BC"));
            Diamond(new Vector2(42,50),new Vector2(4,7),Tint("E8C575"));
            Diamond(new Vector2(578,50),new Vector2(4,7),Tint("E8C575"));
        }

        private void Tree(float horizontal, float bottom, float height, float direction)
        {
            Color bark = Tint("4E5934");
            Stroke(new Vector2(horizontal, bottom), new Vector2(horizontal + direction * 28, bottom - height), 30, bark);
            for (int branch = 0; branch < 8; branch++)
            {
                float vertical = bottom - height * (0.24f + branch * 0.095f);
                float reach = 95 + Mathf.Repeat(branch * 43, 100);
                Vector2 tip = new Vector2(horizontal + direction * reach, vertical - 100);
                Stroke(new Vector2(horizontal, vertical + 30), tip, 9 - branch * 0.65f, bark);
                for (int leaf = 0; leaf < 5; leaf++)
                {
                    Vector2 stem = Vector2.Lerp(new Vector2(horizontal, vertical + 30), tip, 0.3f + leaf * 0.16f);
                    Leaf(stem, stem + new Vector2(direction * 52, -52 + leaf * 10), 18, branch % 2 == 0 ? Tint("356738") : Tint("50843F"));
                }
            }
        }

        private void Pine(float horizontal, float bottom, float height, Color tint)
        {
            for (int index = 0; index < 4; index++)
            {
                float top = bottom - height + index * height * 0.17f;
                float half = height * (0.16f + index * 0.035f);
                Triangle(new Vector2(horizontal, top), new Vector2(horizontal - half, top + height * 0.45f), new Vector2(horizontal + half, top + height * 0.45f), tint);
            }
        }

        private void Ridge(float baseline, float amplitude, float offset, Color tint)
        {
            for (int index = 0; index < 45; index++)
            {
                float left = index * 20;
                float right = left + 20;
                float first = baseline + Mathf.Sin(index * 0.14f + offset) * amplitude + Mathf.Cos(index * 0.31f + offset) * amplitude * 0.3f;
                float second = baseline + Mathf.Sin((index + 1) * 0.14f + offset) * amplitude + Mathf.Cos((index + 1) * 0.31f + offset) * amplitude * 0.3f;
                Polygon(tint, new Vector2(left, first), new Vector2(right, second), new Vector2(right, 1600), new Vector2(left, 1600));
            }
        }

        private void Leaf(Vector2 start, Vector2 end, float width, Color tint)
        {
            Vector2 direction = end - start;
            Vector2 normal = new Vector2(-direction.y, direction.x).normalized * width;
            var points = new Vector2[18];
            for (int index = 0; index < 9; index++)
            {
                float progress = index / 8f;
                Vector2 center = start + direction * progress;
                Vector2 spread = normal * Mathf.Sin(progress * Mathf.PI);
                points[index] = center + spread;
                points[17 - index] = center - spread;
            }
            Polygon(tint, points);
        }

        private void Bevel(float left, float top, float right, float bottom, float corner, Color tint)
        {
            Polygon(tint, new Vector2(left + corner, top), new Vector2(right - corner, top), new Vector2(right, top + corner), new Vector2(right, bottom - corner), new Vector2(right - corner, bottom), new Vector2(left + corner, bottom), new Vector2(left, bottom - corner), new Vector2(left, top + corner));
        }

        private void Diamond(Vector2 center, Vector2 radius, Color tint)
        {
            Polygon(tint, center + Vector2.up * radius.y, center + Vector2.right * radius.x, center + Vector2.down * radius.y, center + Vector2.left * radius.x);
        }

        private void Glow(Vector2 center, Vector2 radius, Color tint)
        {
            var points = new Vector2[65];
            for (int index = 0; index < points.Length; index++)
            {
                float angle = index * Mathf.PI * 2 / (points.Length - 1);
                points[index] = center + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y);
            }
            Color transparent = new Color(tint.r, tint.g, tint.b, 0);
            Fan(center, points, tint, transparent, transparent);
        }

        private void Fan(Vector2 center, Vector2[] points, Color middle, Color bottom, Color top)
        {
            int first = mesh.currentVertCount;
            Vertex(center, middle);
            float upper = float.MaxValue;
            float lower = float.MinValue;
            foreach (Vector2 point in points)
            {
                upper = Mathf.Min(upper, point.y);
                lower = Mathf.Max(lower, point.y);
            }
            foreach (Vector2 point in points) Vertex(point, Color.Lerp(top, bottom, Mathf.InverseLerp(upper, lower, point.y)));
            for (int index = 0; index < points.Length - 1; index++) mesh.AddTriangle(first, first + index + 1, first + index + 2);
        }

        private void Ellipse(Vector2 center, Vector2 radius, Color tint, float rotation = 0)
        {
            var points = new Vector2[64];
            for (int index = 0; index < points.Length; index++)
            {
                float angle = index * Mathf.PI * 2 / points.Length;
                Vector2 offset = new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y);
                points[index] = center + new Vector2(offset.x * Mathf.Cos(rotation) - offset.y * Mathf.Sin(rotation), offset.x * Mathf.Sin(rotation) + offset.y * Mathf.Cos(rotation));
            }
            Polygon(tint, points);
        }

        private void Ring(Vector2 center, Vector2 radius, float width, Color tint)
        {
            for (int index = 0; index < 128; index++)
            {
                float first = index * Mathf.PI * 2 / 128;
                float second = (index + 1) * Mathf.PI * 2 / 128;
                Stroke(center + new Vector2(Mathf.Cos(first) * radius.x, Mathf.Sin(first) * radius.y), center + new Vector2(Mathf.Cos(second) * radius.x, Mathf.Sin(second) * radius.y), width, tint);
            }
        }

        private void Stroke(Vector2 start, Vector2 end, float width, Color tint)
        {
            Vector2 direction = end - start;
            Vector2 normal = new Vector2(-direction.y, direction.x).normalized * width * 0.5f;
            Polygon(tint, start + normal, end + normal, end - normal, start - normal);
        }

        private void Gradient(float left, float top, float right, float bottom, Color upper, Color lower)
        {
            int first = mesh.currentVertCount;
            Vertex(new Vector2(left, top), upper);
            Vertex(new Vector2(right, top), upper);
            Vertex(new Vector2(right, bottom), lower);
            Vertex(new Vector2(left, bottom), lower);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }

        private void Quad(float left, float top, float right, float bottom, Color tint)
        {
            Polygon(tint, new Vector2(left, top), new Vector2(right, top), new Vector2(right, bottom), new Vector2(left, bottom));
        }

        private void Triangle(Vector2 first, Vector2 second, Vector2 third, Color tint) { Polygon(tint, first, second, third); }

        private void Polygon(Color tint, params Vector2[] points)
        {
            int first = mesh.currentVertCount;
            foreach (Vector2 point in points) Vertex(point, tint);
            for (int index = 1; index < points.Length - 1; index++) mesh.AddTriangle(first, first + index, first + index + 1);
        }

        private void Vertex(Vector2 point, Color tint)
        {
            Rect bounds = rectTransform.rect;
            mesh.AddVert(new Vector3(bounds.xMin + point.x / designWidth * bounds.width, bounds.yMax - point.y / designHeight * bounds.height), tint, Vector2.zero);
        }
    }
}
