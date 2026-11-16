using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    // Native vector art keeps the home scene editable and independent of external textures.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MainHubArt : MaskableGraphic, ICanvasRaycastFilter
    {
        public enum Motif { City, Book, Hero, Enemy, Item, Compass, AdventureFan, HouseButton }
        public Motif motif;
        public float houseRoofBase=.65f, houseInset=.1f;
        private VertexHelper mesh;
        private Color C(string hex) => PrototypeUI.Hex(hex);
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            mesh = helper; mesh.Clear();
            if (motif == Motif.City) City();
            else if(motif == Motif.AdventureFan) Fan();
            else if(motif == Motif.HouseButton)
            {
                // Keep the invisible interaction art aligned with the turret silhouette.
                // Tint the hover silhouette down so it never masks the landmark behind it.
                var hint = new Color(color.r,color.g,color.b,color.a*.22f);
                Box(houseInset,.06f,1-2*houseInset,houseRoofBase*.72f,hint);
                Poly(hint,new Vector2(.16f,houseRoofBase*.70f),new Vector2(.30f,houseRoofBase*.88f),new Vector2(.70f,houseRoofBase*.88f),new Vector2(.84f,houseRoofBase*.70f));
                Poly(hint,new Vector2(.30f,houseRoofBase*.88f),new Vector2(.50f,1),new Vector2(.70f,houseRoofBase*.88f));
            }
            else Icon();
        }
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if(motif!=Motif.AdventureFan && motif!=Motif.HouseButton) return true;
            Vector2 local;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,screenPoint,eventCamera,out local)) return false;
            var rect=rectTransform.rect;
            if(motif==Motif.HouseButton)
            {
                float x=(local.x-rect.xMin)/rect.width, y=(local.y-rect.yMin)/rect.height;
                if(y<0 || y>1) return false;
                float bodyTop=houseRoofBase*.70f;
                if(y>=.06f && y<=bodyTop) return x>=houseInset && x<=1-houseInset;
                if(y>bodyTop && y<=houseRoofBase*.88f)
                {
                    float t=(y-bodyTop)/(houseRoofBase*.18f);
                    float half=Mathf.Lerp(.34f,.20f,t);
                    return Mathf.Abs(x-.5f)<=half;
                }
                if(y>houseRoofBase*.88f) return Mathf.Abs(x-.5f)<=.20f;
                return false;
            }
            var offset=new Vector2((rect.xMax-local.x)/rect.width,(local.y-rect.yMin)/rect.height);
            return offset.x>=0 && offset.y>=0 && offset.sqrMagnitude<=1;
        }
        private Vector2 FanPoint(float angle,float radius)
            => new Vector2(1+Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius);
        private void Fan()
        {
            var center=new Vector2(1,0);
            var points=new Vector2[66]; points[0]=center;
            for(int i=0;i<=64;i++) points[i+1]=FanPoint(Mathf.PI*(.5f+i/128f),1);
            Poly(color,points);
            var gold=C("E8CE8E");
            for(int i=0;i<64;i++)
            {
                float a=Mathf.PI*(.5f+i/128f), b=Mathf.PI*(.5f+(i+1)/128f);
                Poly(gold,FanPoint(a,.975f),FanPoint(a,.988f),FanPoint(b,.988f),FanPoint(b,.975f));
                Poly(gold,FanPoint(a,.918f),FanPoint(a,.922f),FanPoint(b,.922f),FanPoint(b,.918f));
            }
            for(int i=1;i<6;i++)
            {
                float angle=Mathf.PI*(.5f+i/12f);
                Line(FanPoint(angle,.08f),FanPoint(angle,.36f),.004f,gold);
                Line(FanPoint(angle,.93f),FanPoint(angle,.97f),.004f,gold);
            }
            Line(center,new Vector2(0,0),.012f,gold);
            Line(center,new Vector2(1,1),.012f,gold);
        }
        private void Poly(Color tint, params Vector2[] points)
        {
            int start = mesh.currentVertCount;
            var rect = rectTransform.rect;
            foreach (var point in points)
                mesh.AddVert(new Vector3(rect.xMin + point.x * rect.width, rect.yMin + point.y * rect.height), tint, Vector2.zero);
            for (int i = 1; i < points.Length - 1; i++) mesh.AddTriangle(start, start + i, start + i + 1);
        }
        private void Gradient(Color top, Color bottom, params Vector2[] points)
        {
            float lo=1,hi=0; foreach(var p in points) { lo=Mathf.Min(lo,p.y); hi=Mathf.Max(hi,p.y); }
            int start=mesh.currentVertCount; var rect=rectTransform.rect;
            foreach(var p in points) mesh.AddVert(new Vector3(rect.xMin+p.x*rect.width,rect.yMin+p.y*rect.height),Color.Lerp(bottom,top,(p.y-lo)/Mathf.Max(.0001f,hi-lo)),Vector2.zero);
            for(int i=1;i<points.Length-1;i++) mesh.AddTriangle(start,start+i,start+i+1);
        }
        private void GradientBox(float x,float y,float w,float h,string top,string bottom)
            => Gradient(C(top),C(bottom),new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x,y+h));
        private void Box(float x, float y, float w, float h, Color c) => Poly(c, new Vector2(x,y), new Vector2(x+w,y), new Vector2(x+w,y+h), new Vector2(x,y+h));
        private void Oval(float x, float y, float rx, float ry, Color c, int segments = 24)
        {
            var points = new Vector2[segments+1];
            for (int i=0;i<points.Length;i++) { float angle = i * Mathf.PI * 2 / segments; points[i] = new Vector2(x+Mathf.Cos(angle)*rx,y+Mathf.Sin(angle)*ry); }
            Poly(c, points);
        }
        private void Line(Vector2 a, Vector2 b, float width, Color c)
        {
            var n = new Vector2(-(b-a).y,(b-a).x).normalized * width / 2;
            Poly(c,a+n,b+n,b-n,a-n);
        }
        private void House(float x, float y, float w, float h, bool grand = false)
        {
            // A compact blue-gold defensive turret, sized to the former house footprint.
            // The visual silhouette intentionally grows beyond the plaque so the tower
            // reads as the landmark behind its label while the button bounds stay fixed.
            x -= w * .175f;
            y -= h * .14f;
            w *= 1.35f;
            h *= 1.35f;
            var stoneDark = C("24384F");
            var stone = C("3C5870");
            var stoneLight = C("58718A");
            var gold = C("D7B56A");
            var goldLight = C("F0D89A");
            var core = C("4CC4E7");
            var coreDark = C("1E789D");

            // Three stepped stone courses anchor the tower.
            Box(x+w*.08f,y,w*.84f,.055f*h,stoneDark);
            Box(x+w*.03f,y+.055f*h,w*.94f,.07f*h,stone);
            Box(x+w*.12f,y+.125f*h,w*.76f,.055f*h,stoneLight);
            Line(new Vector2(x+w*.04f,y+.055f*h),new Vector2(x+w*.96f,y+.055f*h),.004f,gold);
            Line(new Vector2(x+w*.12f,y+.125f*h),new Vector2(x+w*.88f,y+.125f*h),.003f,C("9B8453"));

            float bodyBottom=y+h*.16f, bodyTop=y+h*.78f;
            Poly(stone,new Vector2(x+w*.19f,bodyBottom),new Vector2(x+w*.81f,bodyBottom),
                new Vector2(x+w*.66f,bodyTop),new Vector2(x+w*.34f,bodyTop));
            Poly(C("2D465E"),new Vector2(x+w*.19f,bodyBottom),new Vector2(x+w*.34f,bodyTop),
                new Vector2(x+w*.43f,bodyTop),new Vector2(x+w*.40f,bodyBottom));
            Poly(C("66829A"),new Vector2(x+w*.60f,bodyBottom),new Vector2(x+w*.57f,bodyTop),
                new Vector2(x+w*.66f,bodyTop),new Vector2(x+w*.81f,bodyBottom));
            for(int i=1;i<5;i++)
            {
                float t=i/5f, yy=Mathf.Lerp(bodyBottom,bodyTop,t);
                float half=Mathf.Lerp(.31f,.16f,t);
                Line(new Vector2(x+w*(.5f-half),yy),new Vector2(x+w*(.5f+half),yy),.0012f,C("71879A"));
            }
            Line(new Vector2(x+w*.34f,bodyBottom),new Vector2(x+w*.42f,bodyTop),.002f,C("8BA0AF"));
            Line(new Vector2(x+w*.66f,bodyBottom),new Vector2(x+w*.58f,bodyTop),.002f,C("182D44"));

            // Gold armor bands and the front gate identify the turret at a glance.
            Box(x+w*.16f,y+h*.25f,w*.68f,.018f,gold);
            Box(x+w*.27f,y+h*.73f,w*.46f,.022f,goldLight);
            Box(x+w*.40f,y+h*.16f,w*.20f,h*.27f,C("162B43"));
            Poly(C("30485E"),new Vector2(x+w*.40f,y+h*.43f),new Vector2(x+w*.50f,y+h*.50f),new Vector2(x+w*.60f,y+h*.43f));
            Line(new Vector2(x+w*.50f,y+h*.18f),new Vector2(x+w*.50f,y+h*.45f),.0018f,gold);
            Oval(x+w*.53f,y+h*.29f,.008f,.006f,goldLight,10);

            // Crown platform, crenellations, and a cyan power crystal.
            Box(x+w*.25f,y+h*.76f,w*.50f,.045f,stoneDark);
            Box(x+w*.30f,y+h*.80f,w*.40f,.025f,gold);
            for(int i=0;i<4;i++) Box(x+w*(.30f+i*.11f),y+h*.825f,w*.065f,.045f,stoneLight);
            Poly(C("17334B"),new Vector2(x+w*.37f,y+h*.87f),new Vector2(x+w*.50f,y+h*.99f),new Vector2(x+w*.63f,y+h*.87f));
            Poly(coreDark,new Vector2(x+w*.43f,y+h*.875f),new Vector2(x+w*.50f,y+h*.97f),new Vector2(x+w*.50f,y+h*.875f));
            Poly(core,new Vector2(x+w*.50f,y+h*.97f),new Vector2(x+w*.57f,y+h*.875f),new Vector2(x+w*.50f,y+h*.895f),new Vector2(x+w*.43f,y+h*.875f));
            Line(new Vector2(x+w*.50f,y+h*.97f),new Vector2(x+w*.50f,y+h*.895f),.002f,C("D8FAFF"));
            Line(new Vector2(x+w*.43f,y+h*.875f),new Vector2(x+w*.57f,y+h*.875f),.002f,goldLight);
            if(grand)
            {
                Line(new Vector2(x+w*.50f,y+h*.97f),new Vector2(x+w*.50f,y+h*1.04f),.002f,gold);
                Oval(x+w*.50f,y+h*1.045f,.008f,.008f,core,12);
            }
        }

        private void HouseLegacy(float x, float y, float w, float h, bool grand = false)
        {
            Oval(x+w*.53f,y-.012f,w*.68f,.018f,C("364150"));
            GradientBox(x,y,w,h,"49637B","283C55"); GradientBox(x+w*.68f,y,w*.32f,h,"344B63","1C3049");
            string[] stone={"4E5C70","58687E","5D6E85"};
            for(int row=0;row<4;row++) for(int col=0;col<9;col++)
                Box(x+col*w/9+.001f,y+row*h*.045f+.001f,w/9-.002f,h*.045f-.002f,C(stone[(col+row)%3]));
            for(int row=0;row<12;row++) for(int col=0;col<5;col++)
            {
                float xx=x+w*(.03f+col*.19f+(row%2)*.035f), yy=y+h*(.22f+row*.06f);
                Line(new Vector2(xx,yy),new Vector2(xx+w*.07f,yy),.0006f,C("738398"));
            }
            foreach(float u in new[]{.035f,.32f,.68f,.965f})
            {
                Box(x+w*u-.003f,y+h*.17f,.006f,h*.83f,C("847355"));
                Box(x+w*u-.003f,y+h*.17f,.0015f,h*.83f,C("B8A17A"));
            }
            Box(x,y+h*.83f,w,.005f,C("8D7958"));
            Line(new Vector2(x+w*.04f,y+h*.84f),new Vector2(x+w*.31f,y+h*.98f),.004f,C("8D7958"));
            Line(new Vector2(x+w*.69f,y+h*.98f),new Vector2(x+w*.96f,y+h*.84f),.004f,C("8D7958"));
            Box(x+.008f,y+h*.07f,w-.016f,h*.05f,C("C2AA74"));
            Gradient(C("21516E"),C("142B47"),new Vector2(x-.025f,y+h),new Vector2(x+w*.5f,y+h+.075f),new Vector2(x+w+.025f,y+h));
            Gradient(C("3E7C97"),C("1D4260"),new Vector2(x-.025f,y+h),new Vector2(x+w*.5f,y+h+.075f),new Vector2(x+w*.58f,y+h));
            Line(new Vector2(x-.026f,y+h),new Vector2(x+w+.026f,y+h),.005f,C("D2B36C"));
            for(int r=1;r<=8;r++)
            {
                float f=r/9f, yy=y+h+.075f*(1-f);
                float left=x+w*.5f-(w*.5f+.025f)*f, right=x+w*.5f+(w*.5f+.025f)*f;
                Line(new Vector2(left,yy),new Vector2(right,yy),.0012f,C("4D7F98"));
                for(int col=1;col<12;col++)
                {
                    float u=(col+(r%2)*.5f)/12, xx=Mathf.Lerp(left,right,u);
                    // Keep seams inside the triangular roof, including the narrow top courses.
                    float upperY=yy+.005f;
                    float upperHalf=(w*.5f+.025f)*((y+h+.075f-upperY)/.075f);
                    float upperX=Mathf.Clamp(xx-(u-.5f)*.004f,x+w*.5f-upperHalf,x+w*.5f+upperHalf);
                    Line(new Vector2(xx,yy),new Vector2(upperX,upperY),.0008f,C("183A55"));
                }
            }
            Line(new Vector2(x+w*.5f,y+h+.075f),new Vector2(x+w*.58f,y+h),.002f,C("4B596C"));
            Box(x-.02f,y+h-.009f,w+.04f,.006f,C("6C654B"));
            for(int i=0;i<9;i++) Box(x+i*w/9,y+h-.018f,.007f,.012f,C("8B7958"));
            float door=x+w*.5f;
            Oval(door,y+h*.4f,w*.115f,w*.084f,C("B6A382"));
            Box(door-w*.115f,y,w*.23f,h*.4f,C("B6A382"));
            Oval(door,y+h*.4f,w*.085f,w*.065f,C("645940"));
            Box(door-w*.085f,y,w*.17f,h*.4f,C("645940"));
            string[] wood={"8F7952","A28A5D","967E53"};
            for(int i=0;i<6;i++) Box(door-w*.08f+i*w*.027f,y+.002f,w*.023f,h*.4f,C(wood[i%3]));
            foreach(float v in new[]{.12f,.31f}) Box(door-w*.08f,y+h*v,w*.16f,.003f,C("272F38"));
            Oval(door+w*.035f,y+h*.22f,.0025f,.002f,C("E1C17C"));
            Box(door-w*.15f,y-.010f,w*.30f,.009f,C("59697F"));
            Box(door-w*.18f,y-.018f,w*.36f,.008f,C("1D3654"));
            for(int i=0;i<2;i++)
            {
                float wx=x+w*(.12f+i*.63f), wy=y+h*.51f, ww=w*.12f, wh=h*.22f;
                Box(wx-.005f,wy-.004f,ww+.01f,wh+.008f,C("8A7654"));
                GradientBox(wx,wy,ww,wh,"7085A1","DDA85B");
                Line(new Vector2(wx+.003f,wy+wh*.85f),new Vector2(wx+ww*.4f,wy+wh*.55f),.002f,C("7085A1"));
                Line(new Vector2(wx+ww*.5f,wy),new Vector2(wx+ww*.5f,wy+wh),.0023f,C("8C7959"));
                Line(new Vector2(wx,wy+wh*.48f),new Vector2(wx+ww,wy+wh*.48f),.0023f,C("8C7959"));
                Box(wx-.01f,wy-.009f,ww+.02f,.006f,C("B7A17A"));
                Box(wx-.015f,wy,.008f,wh,C("343E4C")); Box(wx+ww+.007f,wy,.008f,wh,C("343E4C"));
                for(int slat=0;slat<5;slat++) Line(new Vector2(wx-.015f,wy+slat*wh/5),new Vector2(wx-.007f,wy+slat*wh/5),.001f,C("475465"));
                Box(wx-.004f,wy-.024f,ww+.008f,.011f,C("8C6143"));
                string[] flowers={"D6AC71","F0D7A6","B87C66"};
                for(int flower=0;flower<5;flower++)
                {
                    Oval(wx+flower*ww/4,wy-.012f,.005f,.005f,C("394454"));
                    Oval(wx+flower*ww/4,wy-.009f,.002f,.002f,C(flowers[flower%3]));
                }
            }
            foreach(float side in new[]{.04f,.94f})
            {
                float lx=x+w*side,ly=y+h*.34f;
                Line(new Vector2(lx,ly+.02f),new Vector2(lx+.01f,ly+.02f),.002f,C("625D47"));
                Box(lx+.003f,ly,.012f,.018f,C("2A323C"));
                GradientBox(lx+.005f,ly+.002f,.008f,.014f,"FFECC0","DDA65C");
                Poly(C("7E6D4E"),new Vector2(lx,ly+.018f),new Vector2(lx+.009f,ly+.025f),new Vector2(lx+.018f,ly+.018f));
            }
            for(int v=0;v<12;v++)
            {
                float vx=x+w*.97f+Mathf.Sin(v*1.8f)*.005f,vy=y+.008f+v*h*.035f;
                Line(new Vector2(x+w*.97f,y),new Vector2(vx,vy),.0006f,C("384351"));
                Oval(vx,vy,.004f,.002f,C("394351"),8);
            }
            for(int v=0;v<8;v++) { float a=v*Mathf.PI/7; Oval(door+Mathf.Cos(a)*w*.103f,y+h*.4f+Mathf.Sin(a)*w*.075f,.004f,.003f,C("E2D5AE"),8); }
            if(grand)
            {
                Box(x+w*.44f,y+h+.07f,w*.12f,.07f,C("E8D9B5"));
                Box(x+w*.52f,y+h+.07f,w*.04f,.07f,C("B9AA83"));
                for(int r=0;r<7;r++) Line(new Vector2(x+w*.44f,y+h+.071f+r*.009f),new Vector2(x+w*.56f,y+h+.071f+r*.009f),.001f,C("C3B391"));
                Box(x+w*.485f,y+h+.098f,w*.03f,.022f,C("212C3B"));
                Poly(C("244550"),new Vector2(x+w*.4f,y+h+.14f),new Vector2(x+w*.5f,y+h+.185f),new Vector2(x+w*.6f,y+h+.14f));
                Box(x+w*.49f,y+h+.18f,.003f,.05f,C("D2B36C"));
                Poly(C("AC6749"),new Vector2(x+w*.5f,y+h+.23f),new Vector2(x+w*.64f,y+h+.21f),new Vector2(x+w*.5f,y+h+.19f));
            }
        }
        private void Tree(float x,float y,float size)
        {
            Oval(x+size*.05f,y-.003f,size*.43f,size*.07f,C("404C5B"));
            Poly(C("665E46"),new Vector2(x-size*.055f,y),new Vector2(x-size*.035f,y+size*.7f),new Vector2(x+size*.025f,y+size*.7f),new Vector2(x+size*.065f,y));
            Line(new Vector2(x-.001f,y+.004f),new Vector2(x-.001f,y+size*.65f),size*.022f,C("A09467"));
            Line(new Vector2(x,y+size*.3f),new Vector2(x-size*.22f,y+size*.62f),size*.035f,C("665E46"));
            Line(new Vector2(x,y+size*.42f),new Vector2(x+size*.20f,y+size*.70f),size*.03f,C("665E46"));
            Oval(x,y+size*.72f,size*.39f,size*.46f,C("1E2B3C"));
            string[] leaves={"212C3A","2E3A4A","3A4758","485667","546478"};
            for(int t=0;t<22;t++)
            {
                float angle=t*2.39996f, radius=.28f*Mathf.Sqrt((t+.5f)/22);
                float lx=x+Mathf.Cos(angle)*size*radius, ly=y+size*.76f+Mathf.Sin(angle)*size*radius*1.2f;
                int shade=Mathf.Clamp(Mathf.RoundToInt(2+(ly-y-size*.76f)/size*5-(lx-x)/size*2),0,4);
                Oval(lx,ly,size*(.14f+(t%3)*.018f),size*.15f,C(leaves[shade]));
            }
            for(int t=0;t<85;t++)
            {
                float angle=t*2.39996f,radius=.33f*Mathf.Sqrt((t+.5f)/85);
                float lx=x+Mathf.Cos(angle)*size*radius,ly=y+size*.78f+Mathf.Sin(angle)*size*radius;
                Oval(lx,ly,size*.025f,size*.010f,C(new[]{"536377","3B4B5F","254362","364353"}[t%4]),8);
            }
            for(int t=0;t<5;t++) Line(new Vector2(x+(t-2)*size*.06f,y),new Vector2(x+(t-2)*size*.08f,y+size*.055f),size*.012f,C("353F4C"));
        }
        private static float RiverCenter(float y)
        {
            if(y<.18f) return Mathf.Lerp(.88f,.72f,Mathf.SmoothStep(0,1,y/.18f));
            if(y<.34f) return Mathf.Lerp(.72f,.81f,Mathf.SmoothStep(0,1,(y-.18f)/.16f));
            return Mathf.Lerp(.81f,.765f,Mathf.SmoothStep(0,1,(y-.34f)/.20f));
        }
        private static float RiverWidth(float y) => Mathf.Lerp(.075f,.031f,y/.54f);
        private void RiverBand(float y0,float y1,float left,float right,float margin,Color tint)
        {
            float a=RiverCenter(y0), b=RiverCenter(y1);
            float w0=RiverWidth(y0)+margin, w1=RiverWidth(y1)+margin;
            Poly(tint,new Vector2(a+w0*left,y0),new Vector2(a+w0*right,y0),
                new Vector2(b+w1*right,y1),new Vector2(b+w1*left,y1));
        }
        private void River()
        {
            // Short, convex strips follow a smooth channel without the old polygon fan seams.
            for(int i=0;i<80;i++)
            {
                float y0=i*.54f/80, y1=(i+1)*.54f/80;
                RiverBand(y0,y1,-1,1,.014f,C("344B59"));
                RiverBand(y0,y1,-1,1,.006f,C("567075"));
                RiverBand(y0,y1,-1,1,0,Color.Lerp(C("235872"),C("24465C"),y0/.54f));
                RiverBand(y0,y1,-.78f,.78f,0,Color.Lerp(C("327E98"),C("2C627E"),y0/.54f));
                RiverBand(y0,y1,-.41f,.42f,0,Color.Lerp(C("4599AC"),C("347D96"),y0/.54f));
                RiverBand(y0,y1,-.93f,-.86f,0,C("739FAD"));
            }
            // Sparse reflections follow the current instead of crossing the river as stripes.
            var reflection=C("ADE2DF"); reflection.a=.42f;
            for(int i=0;i<19;i++)
            {
                float y=.045f+i*.025f, lane=(i%3-1)*.43f;
                for(int j=0;j<4;j++)
                {
                    float a=y+j*.003f, b=a+.003f;
                    Line(new Vector2(RiverCenter(a)+RiverWidth(a)*lane,a),
                        new Vector2(RiverCenter(b)+RiverWidth(b)*lane,b),.0011f,reflection);
                }
            }
        }
        private static Vector2 RoadEdges(float y)
        {
            float t=Mathf.Clamp01(y/.39f);
            float center=Mathf.Lerp(.405f,.50f,Mathf.SmoothStep(0,1,t));
            float half=Mathf.Lerp(.245f,.115f,t);
            return new Vector2(center-half,center+half);
        }
        private void Road()
        {
            // Paving extends behind the central tower's first stone step (base y = .372).
            for(int row=0;row<27;row++)
            {
                float y0=row*.015f,y1=(row+1)*.015f;
                Vector2 a=RoadEdges(y0),b=RoadEdges(y1);
                Poly(C("827559"),new Vector2(a.x,y0),new Vector2(a.y,y0),new Vector2(b.y,y1),new Vector2(b.x,y1));
                for(int col=0;col<5;col++)
                {
                    float l=col/5f,r=(col+1)/5f;
                    var bl=new Vector2(Mathf.Lerp(a.x,a.y,l)+.003f,y0+.0015f);
                    var br=new Vector2(Mathf.Lerp(a.x,a.y,r)-.003f,y0+.0015f);
                    var tr=new Vector2(Mathf.Lerp(b.x,b.y,r)-.003f,y1-.0015f);
                    var tl=new Vector2(Mathf.Lerp(b.x,b.y,l)+.003f,y1-.0015f);
                    Poly(C(new[]{"A09070","B7A77E","938568","A69774"}[(col+row)%4]),bl,br,tr,tl);
                    Line(tl,tr,.0008f,C("D0B778"));
                }
                Line(new Vector2(a.x,y0),new Vector2(b.x,y1),.003f,C("BBA16B"));
                Line(new Vector2(a.y,y0),new Vector2(b.y,y1),.003f,C("BBA16B"));
            }
        }
        private void City()
        {
            GradientBox(0,0,1,1,"102342","172C4A");
            Oval(.72f,.84f,.19f,.16f,C("1A3B61")); Oval(.72f,.84f,.12f,.10f,C("234B74"));
            Poly(C("254362"),new Vector2(0,.58f),new Vector2(0,.75f),new Vector2(.19f,.86f),new Vector2(.42f,.70f),new Vector2(.68f,.82f),new Vector2(1,.69f),new Vector2(1,.5f));
            Poly(C("1D3654"),new Vector2(0,.45f),new Vector2(0,.65f),new Vector2(.23f,.73f),new Vector2(.51f,.56f),new Vector2(.79f,.72f),new Vector2(1,.62f),new Vector2(1,.4f));
            for(int i=0;i<13;i++) Tree(i*.086f,.48f,.08f+(i%3)*.015f);
            Poly(C("162D47"),new Vector2(0,0),new Vector2(0,.43f),new Vector2(.48f,.57f),new Vector2(1,.43f),new Vector2(1,0));
            River();
            Road();
            House(.13f,.46f,.18f,.12f); House(.72f,.48f,.15f,.11f);
            House(.36f,.40f,.28f,.20f);
            House(.04f,.22f,.24f,.14f); House(.73f,.20f,.22f,.14f);
            for(int i=0;i<5;i++) { Tree(.02f+i*.055f,.11f+(i%2)*.04f,.075f); Tree(.84f+i*.044f,.06f+(i%2)*.04f,.07f); }
            for(int i=0;i<9;i++) {
                float x=.06f+i*.11f, y=.69f+(i%3)*.035f;
                Poly(C("42AACB"),new Vector2(x,y+.045f),new Vector2(x+.012f,y),new Vector2(x,y-.022f),new Vector2(x-.012f,y));
                Line(new Vector2(x,y+.038f),new Vector2(x,y-.016f),.0015f,C("D2F4FF"));
            }
            // Foreground stone terrace.
            Poly(C("233956"),new Vector2(0,0),new Vector2(0,.055f),new Vector2(.45f,.16f),new Vector2(1,.06f),new Vector2(1,0));
            Oval(.46f,.065f,.286f,.042f,C("796841"));
            Oval(.46f,.068f,.28f,.040f,C("C4A466")); Oval(.46f,.068f,.252f,.035f,C("254362")); Oval(.46f,.068f,.245f,.033f,C("324E69"));
            for(int i=0;i<32;i++) { float a=i*Mathf.PI/16; Line(new Vector2(.46f+Mathf.Cos(a)*.255f,.068f+Mathf.Sin(a)*.036f),new Vector2(.46f+Mathf.Cos(a)*.28f,.068f+Mathf.Sin(a)*.04f),.0008f,C("D8B975")); }
            for(int i=0;i<14;i++) Oval(.05f+Mathf.Repeat(i*.173f,.9f),.25f+Mathf.Repeat(i*.117f,.48f),.002f,.002f,C("FFF0BC"));
        }
        private void Icon()
        {
            Color gold = color;
            switch(motif)
            {
                case Motif.Book:
                    Poly(gold,new Vector2(.13f,.2f),new Vector2(.13f,.78f),new Vector2(.47f,.7f),new Vector2(.47f,.12f));
                    Poly(gold,new Vector2(.53f,.12f),new Vector2(.53f,.7f),new Vector2(.87f,.78f),new Vector2(.87f,.2f));
                    break;
                case Motif.Hero:
                    Oval(.5f,.68f,.17f,.19f,gold);
                    Poly(gold,new Vector2(.14f,.12f),new Vector2(.23f,.4f),new Vector2(.5f,.49f),new Vector2(.77f,.4f),new Vector2(.86f,.12f)); break;
                case Motif.Enemy:
                    Poly(gold,new Vector2(.18f,.83f),new Vector2(.39f,.68f),new Vector2(.61f,.68f),new Vector2(.82f,.83f),new Vector2(.76f,.28f),new Vector2(.5f,.1f),new Vector2(.24f,.28f));
                    Line(new Vector2(.28f,.53f),new Vector2(.43f,.46f),.065f,C("101E35")); Line(new Vector2(.57f,.46f),new Vector2(.72f,.53f),.065f,C("101E35")); break;
                case Motif.Item:
                    Poly(gold,new Vector2(.22f,.67f),new Vector2(.37f,.85f),new Vector2(.63f,.85f),new Vector2(.78f,.67f),new Vector2(.5f,.10f));
                    Line(new Vector2(.23f,.64f),new Vector2(.77f,.64f),.035f,C("101E35")); Line(new Vector2(.38f,.81f),new Vector2(.5f,.15f),.025f,C("101E35")); break;
                default:
                    Oval(.5f,.5f,.4f,.4f,gold); Oval(.5f,.5f,.34f,.34f,C("101E35"));
                    Poly(gold,new Vector2(.70f,.80f),new Vector2(.58f,.42f),new Vector2(.30f,.20f),new Vector2(.42f,.58f)); break;
            }
        }
    }
}
