using System;
using UnityEngine;
using UnityEngine.UI;

// Lightweight vector diagrams. Geometry is driven by the experiment state; no downloaded sprites are required.
[RequireComponent(typeof(CanvasRenderer))]
public class PracticeExperimentDiagram : MaskableGraphic
{
    public PracticeExperiment Session;
    float changedAt;
    VertexHelper mesh;
    static readonly Color Ink=new Color(.18f,.22f,.25f), Blue=new Color(.16f,.63f,.9f), Gold=new Color(1,.7f,.16f), Green=new Color(.27f,.7f,.37f), Red=new Color(.9f,.3f,.25f), Pale=new Color(.9f,.96f,1), Grey=new Color(.55f,.62f,.67f);
    public void Refresh(){changedAt=Time.unscaledTime;SetVerticesDirty();}
    void Update(){if(Session!=null)SetVerticesDirty();}
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();mesh=vh;if(Session==null)return;
        var s=Session;var c=s.C;int sample=Mathf.Clamp((int)s.Get("sample"),0,7);
        switch(s.Type){
            case "CanKhoiLuong":Balance();break;
            case "DoTheTich":Cylinder();break;
            case "LucBienDang":Spring(-150,135,120+s.Get("force")/c.forceLevelsN[2]*130);Arrow(0,-70,130,0,Gold);break;
            case "HaiLucNguocChieu":CartForces();break;
            case "TrongLuc":ForceMeter();break;
            case "MaSatBeMat":Friction();break;
            case "LucCanChatLong":WaterDrag();break;
            case "MaSatDoiSong":DailyFriction(sample);break;
            case "ChuyenHoaNangLuong":EnergyChain();break;
            case "TruyenNhiet":Heat();break;
            case "NangLuongHaoPhi":EnergyBars();break;
            case "NgayDem":DayNight();break;
            case "PhaMatTrang":Moon();break;
            case "HeMatTroi":Planets();break;
            case "TrangBiBaoHo":Protection();break;
            case "NhanCanhBao":Hazards();break;
            case "TimLoiAnToan":LabTable();break;
            case "BaTheCuaChat":Particles();break;
            case "NongChay":Melting();break;
            case "BayHoiVaSoi":Boiling();break;
            case "ThanhPhanKhongKhi":Air();break;
            case "OxygenSuChay":Flames();break;
            case "BaoVeKhongKhi":Pollution();break;
            case "ChonVatLieu":Materials();break;
            case "NhienLieu":Fuels();break;
            case "BaoQuanThucPham":Food();break;
            case "LocHonHop":Filter();break;
            case "BayHoiThuMuoi":Evaporate();break;
            case "RayHonHop":Sieve();break;
        }
    }
    Vector2 P(float x,float y){return new Vector2(x*rectTransform.rect.width/900,y*rectTransform.rect.height/350);}
    void Tri(Vector2 a,Vector2 b,Vector2 c,Color color){int n=mesh.currentVertCount;mesh.AddVert(P(a.x,a.y),color,Vector2.zero);mesh.AddVert(P(b.x,b.y),color,Vector2.zero);mesh.AddVert(P(c.x,c.y),color,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);}
    void Quad(float x,float y,float w,float h,Color color){Tri(new Vector2(x-w/2,y-h/2),new Vector2(x+w/2,y-h/2),new Vector2(x+w/2,y+h/2),color);Tri(new Vector2(x-w/2,y-h/2),new Vector2(x+w/2,y+h/2),new Vector2(x-w/2,y+h/2),color);}
    void Line(float x,float y,float xx,float yy,float width,Color color){var a=new Vector2(x,y);var b=new Vector2(xx,yy);var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*width/2;Tri(a-n,b-n,b+n,color);Tri(a-n,b+n,a+n,color);}
    void Circle(float x,float y,float r,Color color,int count=36){for(int i=0;i<count;i++){float a=i*2*Mathf.PI/count,b=(i+1)*2*Mathf.PI/count;Tri(new Vector2(x,y),new Vector2(x+Mathf.Cos(a)*r,y+Mathf.Sin(a)*r),new Vector2(x+Mathf.Cos(b)*r,y+Mathf.Sin(b)*r),color);}}
    void Ring(float x,float y,float r,float width,Color color){for(int i=0;i<48;i++){float a=i*2*Mathf.PI/48,b=(i+1)*2*Mathf.PI/48;Line(x+Mathf.Cos(a)*r,y+Mathf.Sin(a)*r,x+Mathf.Cos(b)*r,y+Mathf.Sin(b)*r,width,color);}}
    void Arrow(float x,float y,float dx,float dy,Color color){Line(x,y,x+dx,y+dy,9,color);var d=new Vector2(dx,dy).normalized;var tip=new Vector2(x+dx,y+dy);var n=new Vector2(-d.y,d.x)*14;Tri(tip,tip-d*25+n,tip-d*25-n,color);}
    void Cup(float x,float y,float width,float height,float fill,Color liquid){Quad(x,y,width,height,Pale);Quad(x,y-height/2+Mathf.Clamp01(fill)*height/2,width-10,Mathf.Clamp01(fill)*height,liquid);Line(x-width/2,y+height/2,x-width/2,y-height/2,5,Ink);Line(x+width/2,y+height/2,x+width/2,y-height/2,5,Ink);Line(x-width/2,y-height/2,x+width/2,y-height/2,5,Ink);}
    void Cart(float x,float y,Color color){Quad(x,y,100,45,color);Quad(x-12,y+30,55,25,color);Circle(x-30,y-28,15,Ink);Circle(x+30,y-28,15,Ink);Circle(x-30,y-28,6,Grey);Circle(x+30,y-28,6,Grey);}
    void Spring(float x,float top,float length){Quad(x,top+8,180,14,Ink);Line(x,top,x,top-15,5,Ink);float previous=x;float y=top-15;for(int i=0;i<15;i++){float xx=x+(i%2==0?-30:30);float yy=top-15-(i+1)*(length-30)/15;Line(previous,y,xx,yy,5,Blue);previous=xx;y=yy;}Line(previous,y,x,top-length,5,Blue);Quad(x,top-length-20,70,35,Gold);}
    void Balance(){float difference=Session.C.massG*Session.Get("object")-Session.WeightSum();float tilt=Mathf.Clamp(difference/Session.C.massG,-1,1)*38;Line(-290,55-tilt,290,55+tilt,10,Ink);Tri(new Vector2(-25,-120),new Vector2(25,-120),new Vector2(0,55),Grey);Quad(0,-130,170,15,Ink);for(int side=-1;side<=1;side+=2){float x=side*240,y=55+side*tilt;Line(x,y,x-80,y-95,3,Grey);Line(x,y,x+80,y-95,3,Grey);Quad(x,y-100,180,14,Gold);if(side<0&&Session.Get("object")>0)Quad(x,y-70,68,50,Blue);if(side>0)for(int i=0;i<Session.C.weightsG.Length;i++)if(Session.Get("weight"+i)>0)Quad(x-50+i*40,y-76,30,40,Grey);}}
    void Cylinder(){float max=Session.CylinderCapacityMl();float water=Session.Get("stone")>0?Session.C.afterMl:Session.C.beforeMl;Cup(0,0,190,260,water/max,new Color(.2f,.65f,.92f,.8f));for(int i=0;i<=10;i++)Line(75,-130+i*26,105,-130+i*26,3,Ink);if(Session.Get("stone")>0)Circle(-25,-88,26,Grey);else Circle(-220,50,28,Grey);if(Session.Get("eye")>0){Circle(210,-130+water/max*260,16,Ink);Circle(210,-130+water/max*260,8,Color.white);Line(120,-130+water/max*260,190,-130+water/max*260,2,Red);}}
    public float ForceMeterScaleY(float force){return 25-force/Mathf.Max(Session.C.massesG[1]*Session.C.gravityNPerKg/1000,1)*60;}
    void ForceMeter()
    {
        int i=Mathf.Clamp((int)Session.Get("sample"),0,1);
        float force=Session.C.massesG[i]/1000*Session.C.gravityNPerKg;
        Cup(-80,20,90,220,0,Pale);
        Spring(-80,125,100+force/Mathf.Max(Session.C.massesG[1]*Session.C.gravityNPerKg/1000,1)*60);
        for(int tick=0;tick<=5;tick++) {
            float y=ForceMeterScaleY(tick);
            Line(-25,y,10,y,3,Ink);
        }
        Arrow(140,90,0,-160,Red);
    }
    void CartForces(){Line(-400,-100,400,-100,9,Grey);float net=Session.Get("right",Session.C.leftN)-Session.C.leftN;float t=Mathf.Min(Time.unscaledTime-changedAt,3);float x=net>0?-120+Mathf.Min(300,t*t*35):0;Cart(x,-55,Blue);Arrow(x-55,-35,-Session.C.leftN/Session.C.rightLevelsN[1]*140,0,Red);Arrow(x+55,-35,Session.Get("right",Session.C.leftN)/Session.C.rightLevelsN[1]*140,0,Green);}
    void Friction(){int i=Mathf.Clamp((int)Session.Get("sample"),0,2);float mu=Session.C.frictionLevels[i],v=Session.C.initialSpeed;float stop=v/(mu*10),t=Mathf.Min(Time.unscaledTime-changedAt,stop);float d=v*t-.5f*mu*10*t*t;float scale=650/(v*v/(2*Mathf.Min(Session.C.frictionLevels)*10));Line(-390,-100,390,-100,16,new[]{Pale,new Color(.6f,.35f,.14f),Green}[i]);Cart(-330+d*scale,-55,Gold);Arrow(-330+d*scale-55,-45,-mu*160,0,Red);}
    void WaterDrag(){int i=Mathf.Clamp((int)Session.Get("sample"),0,2);Quad(0,0,800,280,new Color(.7f,.9f,1));for(int j=0;j<8;j++){float x=-390+Mathf.Repeat(j*110-Time.unscaledTime*50,780);Line(x,110-j%3*85,x+60,110-j%3*85,3,Blue);}float length=i==2?180:90;if(i==1)Circle(-60,0,55,Gold);else if(i==2){Tri(new Vector2(-155,0),new Vector2(15,45),new Vector2(45,0),Gold);Tri(new Vector2(-155,0),new Vector2(45,0),new Vector2(15,-45),Gold);}else Quad(-60,0,length,100,Gold);Line(25,0,180,0,5,Ink);Quad(230,0,110,45,Pale);Arrow(295,0,Session.C.dragN[i]/Mathf.Max(Session.C.dragN)*100,0,Red);}
    void DailyFriction(int i){Line(-360,-90,360,-90,12,Grey);if(i==0){Quad(-50,-50,240,60,Blue);for(int j=0;j<8;j++)Quad(-150+j*30,-84,14,14,Ink);Arrow(100,0,150,0,Green);}else if(i==1){Ring(0,0,90,12,Ink);Quad(-100,30,35,65,Red);Quad(100,30,35,65,Red);Arrow(-170,40,60,0,Gold);Arrow(170,40,-60,0,Gold);}else{Quad(-60,0,100,180,Gold);Quad(60,0,100,180,Gold);Quad(0,0,20,200,Grey);Circle(0,90,15,Blue);}}
    void EnergyChain(){float speed=Session.Get("speed");Ring(-270,0,65,9,Ink);for(int i=0;i<4;i++){float a=Time.unscaledTime*speed+i*Mathf.PI/2;Line(-270,0,-270+Mathf.Cos(a)*58,Mathf.Sin(a)*58,7,Gold);}Arrow(-190,0,100,0,Blue);Quad(0,0,125,100,Blue);Circle(0,0,30,Pale);Arrow(90,0,100,0,Blue);Circle(280,15,52,speed>0?Gold:Grey);Quad(280,-45,45,35,Ink);if(speed>0)for(int j=0;j<8;j++){float a=j*Mathf.PI/4;Line(280+Mathf.Cos(a)*68,15+Mathf.Sin(a)*68,280+Mathf.Cos(a)*(75+speed*12),15+Mathf.Sin(a)*(75+speed*12),4,Gold);}}
    void Heat(){Cup(-220,0,170,210,.7f,Blue);Cup(200,0,170,210,.7f,Blue);if(Session.Get("insulation")>0){Line(105,95,105,-110,15,Green);Line(295,95,295,-110,15,Green);Line(105,-110,295,-110,15,Green);}for(int i=0;i<2;i++){float temp=Session.C.initialC+((i==1&&Session.Get("insulation")==0?Session.C.afterC[0]:Session.C.afterC[i])-Session.C.initialC)*Session.Get("time")/100;float x=i==0?-80:340;Quad(x,0,24,230,Grey);Quad(x,-110+temp/Session.C.initialC*100,12,temp/Session.C.initialC*200,Red);Circle(x,-115,18,Red);}}
    void EnergyBars(){for(int i=0;i<2;i++){float y=65-i*130;Quad(0,y,680,70,Grey);float useful=Session.C.useful[i]/Session.C.input*680;Quad(-340+useful/2,y,useful,70,Gold);Quad(useful/2,y,680-useful,70,Red);}}
    void DayNight(){Circle(-300,0,65,Gold);for(int y=-80;y<=80;y+=40)Arrow(-210,y,130,0,Gold);Circle(120,0,120,new Color(.04f,.13f,.25f));for(int j=0;j<36;j++){float a=Mathf.PI/2+j*Mathf.PI/36,b=Mathf.PI/2+(j+1)*Mathf.PI/36;Tri(new Vector2(120,0),new Vector2(120+Mathf.Cos(a)*120,Mathf.Sin(a)*120),new Vector2(120+Mathf.Cos(b)*120,Mathf.Sin(b)*120),Blue);}if(Session.Get("marked")>0){float a=Session.Get("angle")*Mathf.Deg2Rad;Circle(120+Mathf.Cos(a)*100,Mathf.Sin(a)*100,12,Red);}}
    void Moon(){Circle(-360,0,45,Gold);Circle(-70,0,40,Blue);Ring(-70,0,125,2,Grey);float angle=Session.Get("angle")*Mathf.Deg2Rad;Circle(-70-Mathf.Cos(angle)*125,Mathf.Sin(angle)*125,23,Grey);Circle(260,0,100,new Color(.08f,.12f,.2f));float k=Mathf.Cos(angle);for(int i=0;i<70;i++){float y=-99+i*198f/70;float edge=Mathf.Sqrt(Mathf.Max(0,10000-y*y));float start=k*edge;float w=edge-start;if(w>0)Quad(260+start+w/2,y,w,3,Color.white);}}
    void Planets(){Circle(-405,0,35,Gold);Color[] colors={Grey,Gold,Blue,Red,new Color(.7f,.45f,.25f),new Color(.8f,.7f,.4f),new Color(.4f,.85f,.85f),new Color(.25f,.3f,.9f)};float[] radii={13,22,24,18,47,40,30,29};for(int i=0;i<8;i++){float x=-320+i*96;Circle(x,0,radii[i],colors[i]);if(i==5)Line(x-60,-15,x+60,15,8,Gold);if((int)Session.Get("sample")==i)Ring(x,0,radii[i]+10,3,Green);}}
    void Protection(){Circle(0,95,40,new Color(1,.78f,.56f));Quad(0,-25,110,160,Session.Get("gear1")>0?Color.white:Blue);Quad(-32,-125,30,50,Ink);Quad(32,-125,30,50,Ink);if(Session.Get("gear2")>0){Quad(-40,-150,55,20,Ink);Quad(40,-150,55,20,Ink);}if(Session.Get("gear0")>0){Quad(-20,100,32,22,Blue);Quad(20,100,32,22,Blue);Line(-5,100,5,100,4,Ink);}Circle(0,132,28,Ink);if(Session.Get("gear3")>0)Circle(35,140,18,Ink);else Line(28,125,55,40,14,Ink);if(Session.Get("badGear")>0){Quad(-40,-150,55,12,Red);Quad(40,-150,55,12,Red);}}
    void Hazards(){for(int i=0;i<3;i++){float x=-260+i*260;Tri(new Vector2(x,90),new Vector2(x+90,0),new Vector2(x,-90),Red);Tri(new Vector2(x,90),new Vector2(x,-90),new Vector2(x-90,0),Red);Tri(new Vector2(x,74),new Vector2(x+74,0),new Vector2(x,-74),Color.white);Tri(new Vector2(x,74),new Vector2(x,-74),new Vector2(x-74,0),Color.white);if(i==0){Tri(new Vector2(x,45),new Vector2(x+30,-30),new Vector2(x-30,-30),Ink);}else if(i==1){Line(x-30,30,x+20,10,14,Ink);Circle(x+25,-10,7,Ink);Line(x-40,-35,x+40,-35,8,Ink);}else{Circle(x,15,25,Ink);Circle(x-10,20,6,Color.white);Circle(x+10,20,6,Color.white);Line(x-30,-25,x+30,-45,7,Ink);Line(x+30,-25,x-30,-45,7,Ink);}}}
    void LabTable(){Quad(0,-105,800,30,new Color(.55f,.3f,.12f));Circle(-270,-45,45,Gold);Cup(-50,-20,95,140,.5f,Blue);Quad(200,-10,90,150,Green);Quad(200,80,40,30,Green);for(int i=0;i<3;i++)if(Session.Get("error"+i)>0)Ring(-270+i*235,0,80,5,Red);Quad(340,-65,90,25,Blue);}
    void Particles(){int state=Mathf.Clamp((int)Session.Get("sample"),0,2);bool narrow=state==1?Session.Get("containerLiquid")>0:state==2&&Session.Get("containerGas")>0;float width=narrow?220:430;Cup(0,0,width,290,0,Pale);float t=Time.unscaledTime;for(int i=0;i<48;i++){float x,y;if(state==0){x=(i%8-3.5f)*28+Mathf.Sin(t*3+i)*2;y=-100+i/8*28+Mathf.Cos(t*3+i)*2;}else if(state==1){x=Mathf.Sin(i*3.71f+t*.4f)*(width/2-15);float liquidHeight=narrow?250:130;y=-125+(Mathf.Sin(i*2.19f+t*.5f)+1)*liquidHeight/2;}else{x=Mathf.Repeat(i*83+t*(20+i%5*5),width-25)-width/2+12;y=Mathf.Repeat(i*47+t*(35-i%4*5),265)-132;}Circle(x,y,7,state==0?Blue:state==1?Green:Red,12);}}
    void Melting(){float low=Mathf.Min(-10,Session.C.startC), high=15; Quad(-355,0,22,250,Pale); float top=-115+(Session.Temperature()-low)/(high-low)*230; Quad(-355,(-115+top)/2,12,top+115,Red); Circle(-355,-125,16,Red); for(float degree=low;degree<=high;degree+=5){float yy=-115+(degree-low)/(high-low)*230;Line(-375,yy,-342,yy,2,Ink);} float time=Session.Get("time");Cup(-230,0,190,250,Mathf.Clamp01((time-30)/40)*.65f,Blue);float size=100*(1-Mathf.Clamp01((time-30)/40));if(size>0)Quad(-230,-75+size/2,size,size,new Color(.65f,.88f,1));Line(0,-100,360,-100,3,Ink);Line(0,-100,0,130,3,Ink);Line(0,-60,95,0,5,Red);Line(95,0,235,0,5,Red);Line(235,0,340,110,5,Red);float x=time*3.4f,y=time<30?-60+time*2:time<=70?0:(time-70)*110/30;Circle(x,y,9,Gold);}
    void Boiling(){bool boil=Session.Get("sample")>0;Cup(0,0,330,250,.7f,Blue);for(int i=0;i<14;i++){float x=-140+(i*79)%280;float y=boil?-110+Mathf.Repeat(Time.unscaledTime*50+i*19,230):65+Mathf.Repeat(Time.unscaledTime*25+i*10,90);Ring(x,y,boil?6:4,2,Color.white);}if(boil)Quad(0,-145,230,18,Red);}
    void Air(){float nitrogen=Session.C.nitrogenPercent,oxygen=Session.C.oxygenPercent;if(Session.Stage==1){string text;float n;if(Session.Answers.TryGetValue(0,out text)&&float.TryParse(text,out n))nitrogen=Mathf.Clamp(n,0,100);if(Session.Answers.TryGetValue(1,out text)&&float.TryParse(text,out n))oxygen=Mathf.Clamp(n,0,100);}for(int i=0;i<100;i++)Quad(-135+i%10*30,135-i/10*30,25,25,i<nitrogen?Blue:i<nitrogen+oxygen?Gold:Grey);}
    void Flames(){int sample=Mathf.Clamp((int)Session.Get("sample"),0,2);for(int i=0;i<3;i++){float x=-270+i*270;Cup(x,0,210,280,0,Pale);Quad(x,-85,45,65,Grey);if(i>0&&Session.Stage>0){float h=(i==1?55:100)*(1+.05f*Mathf.Sin(Time.unscaledTime*8));Tri(new Vector2(x,-48+h),new Vector2(x+25,-48),new Vector2(x-25,-48),Red);Tri(new Vector2(x,-40+h*.65f),new Vector2(x+12,-48),new Vector2(x-12,-48),Gold);}if(i==sample)Line(x-100,-150,x+100,-150,6,Green);}}
    void Pollution(){for(int i=0;i<3;i++){float x=-270+i*270;if(i==0)Cart(x,-70,Blue);else if(i==1){Quad(x,-65,85,85,Grey);Tri(new Vector2(x,40),new Vector2(x-30,-25),new Vector2(x+30,-25),Red);}else{Quad(x,-40,110,140,Gold);Line(x-90,90,x+90,90,8,Ink);}bool handled=Session.Stage>=2||(Session.Stage==1&&Session.Placements.ContainsKey(i)&&Session.Placements[i]==Session.Current.Matches[i].Answer);for(int j=0;j<(handled?2:8);j++){float yy=25+Mathf.Repeat(j*18+Time.unscaledTime*20,100);Circle(x+Mathf.Sin(j*2f)*40,yy,14,new Color(.45f,.49f,.52f,.55f));}}}
    void Materials(){Cup(-210,-20,220,160,.45f,Grey);Quad(-70,-5,100,25,new Color(.5f,.25f,.1f));Quad(220,15,210,220,Blue);for(int i=-1;i<=1;i+=2){Line(220+i*105,-95,220+i*105,125,12,Ink);Line(115,15+i*110,325,15+i*110,12,Ink);}Line(220,-95,220,125,8,Color.white);Line(115,15,325,15,8,Color.white);Arrow(70,90,100,0,Gold);}
    void Fuels(){for(int i=0;i<5;i++){float x=-340+i*170;if(i==0){Line(x-40,-40,x+40,30,25,new Color(.55f,.3f,.1f));Line(x-40,30,x+40,-40,25,new Color(.65f,.4f,.15f));}else if(i==1){Circle(x,-10,38,Ink);Circle(x+25,-35,25,Ink);}else if(i==2){Quad(x,0,65,130,Blue);Quad(x,78,30,25,Grey);}else if(i==3){for(int j=0;j<18;j++)Circle(x-50+j%6*20,-50+j/6*16,6,Gold);}else Cup(x,0,75,120,0,Pale);if((int)Session.Get("sample")==i)Line(x-65,-100,x+65,-100,6,Green);}}
    void Food(){for(int i=0;i<3;i++){float x=-260+i*260;Quad(x,0,155,210,new[]{Gold,Blue,Red}[i]);Quad(x,0,115,115,Pale);for(int j=0;j<3;j++)Line(x-40,30-j*25,x+40,30-j*25,5,Grey);if(Session.Stage>0&&Session.Placements.ContainsKey(i)){Ring(x,0,125,4,Green);}if(i==(int)Session.Get("sample"))Line(x-90,-130,x+90,-130,6,Ink);}}
    void Filter(){Tri(new Vector2(-150,90),new Vector2(150,90),new Vector2(0,-10),Pale);Line(-150,90,0,-10,6,Ink);Line(150,90,0,-10,6,Ink);Quad(0,-35,24,65,Grey);if(Session.Get("filter")>0){Tri(new Vector2(-125,80),new Vector2(125,80),new Vector2(0,0),new Color(1,.9f,.7f));}if(Session.Get("cup")>0)Cup(0,-110,180,95,Session.Get("pour")>0?.65f:0,Blue);if(Session.Get("pour")>0){for(int j=0;j<15;j++)Circle(-75+j%5*35,45+j/5*12,7,Gold);for(int i=0;i<5;i++)Circle(0,-45-Mathf.Repeat(Time.unscaledTime*40+i*13,35),4,Blue);}else{Cup(-300,0,100,160,.6f,Blue);for(int j=0;j<10;j++)Circle(-330+j%4*20,-50+j/4*13,5,Gold);}}
    void Evaporate(){float progress=Session.Get("time")/100;Cup(0,-30,340,170,(1-progress)*.8f,Blue);Quad(0,-135,260,20,Red);for(int i=0;i<20;i++){if(i<progress*20)Quad(-140+i%10*30,-102+i/10*13,9,9,Color.white);else Circle(-140+i%10*30,-65+i/10*30,3,Color.white);}if(progress>0&&progress<1)for(int i=0;i<10;i++)Circle(-130+i*28,45+Mathf.Repeat(Time.unscaledTime*35+i*20,100),5,new Color(.65f,.8f,.9f,.7f));}
    void Sieve(){float meshSize=Session.Get("mesh",Session.C.sieveSizes[0]);float gap=Mathf.Clamp(meshSize*7,6,55);for(float x=-300;x<300;x+=gap+10)Quad(x,0,10,20,Grey);Line(-320,10,320,10,3,Ink);Cup(0,-105,640,140,0,Pale);for(int i=0;i<20;i++){float size=i%2==0?Session.C.particleSizes[0]:Session.C.particleSizes[1];float y=meshSize>size?-115+i%3*15:45+i%3*20;Circle(-270+i%10*60,y,Mathf.Clamp(size*2,4,14),i%2==0?Gold:Color.white);}}
}
