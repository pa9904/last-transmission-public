using System;
using UnityEngine;

namespace LastTransmission
{
    public sealed partial class Game
    {
        Texture2D rescueTerrain;
        bool mapReading;
        Vector2 mapLargeScroll;
        readonly Color mapInk=new Color(.16f,.23f,.20f), mapGreen=new Color(.19f,.38f,.32f), mapRed=new Color(.59f,.23f,.18f);
        readonly Color mapOchre=new Color(.60f,.44f,.13f);
        readonly Rect terrainArea=new Rect(164,289,811,415);
        internal bool MapReadingExpanded { get { return mapReading; } }
        internal void ExpandMapRecord() { mapReading=true; mapLargeScroll=Vector2.zero; }
        internal void ReturnToMap() { mapReading=false; }
        public void InspectMapLocation(string id)
        {
            var record=Array.Find(story.dossiers,d=>d.id==id&&d.category=="map");
            if(record==null||state.call<record.unlock) return;
            OpenPanel("map"); OpenDocument(id); mapReading=false;
        }
        Vector2 MapPoint(float x,float y) { return new Vector2(terrainArea.x+x*terrainArea.width,terrainArea.y+y*terrainArea.height); }
        Vector2[] MapPoints(params float[] pairs)
        {
            var points=new Vector2[pairs.Length/2];
            for(int i=0;i<points.Length;i++) points[i]=MapPoint(pairs[i*2],pairs[i*2+1]);
            return points;
        }
        void MapPath(Vector2[] points,Color color,float width,bool dashed=false)
        {
            float distance=0;
            for(int i=0;i<points.Length-1;i++)
            {
                var a=points[Mathf.Max(0,i-1)];var b=points[i];var c=points[i+1];var d=points[Mathf.Min(points.Length-1,i+2)];
                Vector2 previous=b;
                for(int s=1;s<=24;s++)
                {
                    float t=s/24f;
                    var next=.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);
                    distance+=Vector2.Distance(previous,next);
                    if(!dashed||Mathf.Repeat(distance,15)<9) Line(previous.x,previous.y,next.x,next.y,color,width);
                    previous=next;
                }
            }
        }
        void MapCross(Vector2 p,float size)
        {
            Disc(p.x,p.y,size+4,new Color(.80f,.79f,.66f,.92f));
            Line(p.x-size,p.y-size,p.x+size,p.y+size,mapRed,3);
            Line(p.x-size,p.y+size,p.x+size,p.y-size,mapRed,3);
        }
        void MapLabel(float x,float y,float width,string title,string id,bool warning=false)
        {
            var r=new Rect(x,y,width,27);
            bool selected=document==id;
            bool hover=GUI.enabled&&r.Contains(Event.current.mousePosition);
            Color mark=warning?mapRed:mapGreen;
            Box(x,y,width,27,selected||hover?new Color(.94f,.93f,.81f):new Color(.85f,.86f,.75f,.92f));
            Text(x+7,y+4,width-10,23,title,16,warning?mapRed:mapInk,FontStyle.Bold);
            if(hover||selected) Line(x,y+27,x+width,y+27,mark,2);
            if(GUI.Button(r,GUIContent.none,GUIStyle.none)) InspectMapLocation(id);
        }
        void MapPin(Vector2 p,string id,bool warning=false)
        {
            bool selected=document==id;
            if(selected) Disc(p.x,p.y,13,new Color(.13f,.33f,.27f,.20f));
            Disc(p.x,p.y,8,new Color(.93f,.92f,.81f)); Disc(p.x,p.y,5,warning?mapRed:mapGreen);
            if(GUI.Button(new Rect(p.x-15,p.y-15,30,30),GUIContent.none,GUIStyle.none)) InspectMapLocation(id);
        }
        void DrawRescueMap()
        {
            if(NarrowWindow)
            {
                DrawStackedMap();
                return;
            }
            Frame("산악구조대 / 북부 수색 구역 · 현장 통제본","북부 산악 수색 지도");
            if(mapReading) { DrawLargeMapRecord(); return; }
            DrawMapSheet();
            DrawMapInspector();
        }
        void DrawStackedMap()
        {
            // Fit both the map and its record between the top status and two-row dock.
            float available=viewport.height-366;
            float scale=Mathf.Min(1.76f,available/1260);
            var canvas=GUI.matrix;
            float x=800-425*scale,y=viewport.yMin+115+(available-1260*scale)/2;
            GUI.matrix=canvas*Matrix4x4.TRS(new Vector3(x,y,0),Quaternion.identity,new Vector3(scale,scale,1));
            Box(0,0,850,1260,new Color(.023f,.044f,.046f));
            Text(20,20,620,42,"북부 산악 수색 지도",29,ivory,FontStyle.Bold);
            if(Btn(678,10,153,50,"닫기  Esc")) OpenPanel("");
            if(mapReading)
            {
                var enlarged=Array.Find(story.dossiers,d=>d.id==document);
                PaperSurface(10,82,830,1062);
                Text(36,121,772,65,enlarged.title,36,ink,FontStyle.Bold);
                Text(36,190,772,75,enlarged.subtitle,24,mapGreen);
                small.fontSize=ReadingSize(32); small.normal.textColor=ink; WarmText(enlarged.text,small.fontSize);
                float height=Mathf.Max(810,small.CalcHeight(new GUIContent(enlarged.text),735)+24);
                mapLargeScroll=GUI.BeginScrollView(new Rect(36,289,779,810),mapLargeScroll,new Rect(0,0,735,height));
                GUI.Label(new Rect(0,0,735,height),enlarged.text,small); GUI.EndScrollView();
                if(Btn(22,1185,806,53,"지도로 돌아가기  [M]")) ReturnToMap();
                GUI.matrix=canvas; return;
            }
            var local=GUI.matrix;
            GUI.matrix=local*Matrix4x4.Translate(new Vector3(-144,72-209,0));
            DrawMapSheet();
            GUI.matrix=local;
            string[] ids={"routes","bridge","ridge","generator","entry"};
            string[] places={"A 임도","B 다리","C 대피소","D 발전기","E 정비구"};
            for(int i=0;i<ids.Length;i++)
                if(InterfaceButton(new Rect(12+166*i,678,157,55),places[i],true,ControlTone.Neutral,22,document==ids[i])) InspectMapLocation(ids[i]);
            var record=Array.Find(story.dossiers,d=>d.id==document&&d.category=="map");
            if(record!=null)
            {
                Text(24,765,795,46,record.title,32,paper,FontStyle.Bold);
                Text(24,819,795,40,record.subtitle,20,teal);
                small.fontSize=ReadingSize(24); small.normal.textColor=paper;
                WarmText(record.text,small.fontSize);
                float h=Mathf.Max(280,small.CalcHeight(new GUIContent(record.text),760)+18);
                docScroll=GUI.BeginScrollView(new Rect(24,880,802,280),docScroll,new Rect(0,0,760,h));
                GUI.Label(new Rect(0,0,760,h),record.text,small); GUI.EndScrollView();
                if(Btn(22,1185,806,53,"기록 원문 크게 읽기")) ExpandMapRecord();
            }
            GUI.matrix=canvas;
        }
        void DrawMapSheet()
        {
            if(rescueTerrain==null) rescueTerrain=Resources.Load<Texture2D>("Art/RescueTerrain-v1");
            Box(149,215,850,582,new Color(0,0,0,.3f));
            Box(144,209,850,582,new Color(.78f,.80f,.69f));
            Box(155,220,828,560,new Color(.31f,.39f,.31f,.20f));
            Box(158,223,822,554,new Color(.80f,.82f,.71f));
            Text(176,235,550,32,"북부 관할 / 야간 구조 경로도",24,mapInk,FontStyle.Bold);
            Text(176,268,585,20,"현장 갱신 02:35  ·  폭설 통제 중  ·  지점과 표시를 눌러 기록 확인",14,mapInk);
            Box(783,234,177,39,new Color(.59f,.23f,.18f,.09f));
            Text(797,244,162,27,"북쪽 다리 통행 금지",15,mapRed,FontStyle.Bold);
            GUI.DrawTexture(terrainArea,rescueTerrain!=null?rescueTerrain:Texture2D.whiteTexture,ScaleMode.StretchToFill);
            DrawMapTerrainDetails();
            Line(164,718,975,718,new Color(.27f,.36f,.28f,.4f),1);
            Line(179,737,216,737,mapGreen,4); Text(226,727,180,25,"남쪽 임도 · 통행 가능",14,mapInk);
            for(int x=442;x<481;x+=15) Line(x,737,x+9,737,mapOchre,3);
            Text(493,727,185,25,"정비 보행로 · 우회",14,mapInk);
            for(int x=718;x<758;x+=15) Line(x,737,x+9,737,mapRed,3);
            Text(769,727,189,25,"통제 · 옛 연결선",14,mapRed);
            Text(179,758,781,24,"지형·경로 개념도 / 거리 비례 아님      E 정비구를 선택하면 출입 상세 표시",13,mapInk);
        }
        void DrawMapTerrainDetails()
        {
            for(int x=0;x<=10;x++) { var p=MapPoint(x/10f,0); Line(p.x,p.y,p.x,p.y+415,new Color(.19f,.36f,.34f,.1f),1); }
            for(int y=0;y<=5;y++) { var p=MapPoint(0,y/5f); Line(p.x,p.y,p.x+811,p.y,new Color(.19f,.36f,.34f,.1f),1); }
            var river=MapPoints(.47f,-.01f,.44f,.14f,.42f,.34f,.46f,.51f,.42f,.65f,.33f,.78f,.30f,1.0f);
            MapPath(river,new Color(.27f,.43f,.44f,.7f),10); MapPath(river,new Color(.64f,.77f,.74f),5);
            Text(548,299,171,25,"북쪽 계곡",17,mapInk);
            Text(695,302,165,24,"북동 능선",15,new Color(.29f,.37f,.28f));
            var south=MapPoints(.18f,.71f,.25f,.83f,.40f,.84f,.50f,.73f,.57f,.63f,.64f,.52f);
            var bypass=MapPoints(.84f,.30f,.92f,.49f,.92f,.72f,.80f,.90f,.58f,.94f,.35f,.93f,.21f,.77f);
            MapPath(south,new Color(.94f,.93f,.82f),9); MapPath(south,mapGreen,5);
            MapPath(bypass,new Color(.94f,.91f,.74f),7); MapPath(bypass,mapOchre,3,true);
            var old=MapPoints(.18f,.71f,.21f,.53f,.31f,.40f,.42f,.34f,.51f,.36f,.58f,.43f,.64f,.52f);
            MapPath(old,mapRed,2.5f,true);
            MapPath(MapPoints(.64f,.52f,.73f,.44f,.84f,.30f),mapRed,2,true);
            // The bypass remains outside the hatched rockfall area.
            Box(762,433,109,87,new Color(.59f,.23f,.18f,.075f));
            for(int i=0;i<8;i++) Line(764+i*13,435,764+i*13,517,new Color(.59f,.23f,.18f,.10f),1);
            MapCross(MapPoint(.75f,.43f),7); Text(769,494,140,22,"주계단 낙석",14,mapRed,FontStyle.Bold);
            var a=MapPoint(.18f,.71f); var b=MapPoint(.42f,.34f); var c=MapPoint(.64f,.52f); var d=MapPoint(.84f,.30f);
            // Engraved facility silhouettes sit on the actual route junctions.
            Box(a.x-16,a.y-17,31,23,mapInk); Box(a.x-12,a.y-13,23,15,new Color(.84f,.84f,.72f));
            Box(a.x+8,a.y+6,24,9,mapInk); Disc(a.x+13,a.y+17,3,mapInk); Disc(a.x+27,a.y+17,3,mapInk);
            Line(c.x-17,c.y-7,c.x,c.y-24,mapInk,4); Line(c.x,c.y-24,c.x+17,c.y-7,mapInk,4); Box(c.x-12,c.y-7,24,18,mapInk);
            Box(d.x-17,d.y-14,33,26,mapInk); Box(d.x-9,d.y-7,17,12,new Color(.74f,.76f,.63f)); Line(d.x+7,d.y-14,d.x+7,d.y-25,mapInk,4);
            Line(b.x-20,b.y-8,b.x-7,b.y-8,mapInk,4); Line(b.x+7,b.y+7,b.x+20,b.y+7,mapInk,4);
            MapCross(b,9);
            if(GUI.Button(new Rect(b.x-17,b.y-17,34,34),GUIContent.none,GUIStyle.none)) InspectMapLocation("bridge");
            Text(439,568,145,24,"남쪽 임도",17,mapGreen,FontStyle.Bold);
            Text(672,645,236,23,"철망 바깥 정비 보행로",14,mapOchre,FontStyle.Bold);
            MapPin(a,"routes"); MapPin(c,"ridge"); MapPin(d,"generator");
            MapLabel(191,553,117,"A 구조 차량","routes");
            MapLabel(517,393,111,"B 북쪽 다리","bridge",true);
            MapLabel(699,522,118,"C 능선 대피소","ridge");
            MapLabel(786,364,94,"D 발전기","generator");
            MapLabel(185,610,104,"E 정비구","entry");
            if(document=="entry") DrawStationInset();
            // Compact compass rose; no decorative coordinates masquerading as clues.
            Text(929,298,25,25,"N",15,mapInk,FontStyle.Bold);
            Line(938,331,938,359,mapInk,2); Line(928,341,948,341,mapInk,1);
            Line(938,316,932,332,mapInk,2); Line(938,316,944,332,mapInk,2);
        }
        void DrawStationInset()
        {
            Box(179,301,240,177,new Color(.12f,.19f,.16f,.15f)); Box(175,297,240,177,new Color(.88f,.88f,.76f,.98f));
            Text(188,309,218,24,"E 정비구 / 관제소 출입 상세",15,mapInk,FontStyle.Bold);
            Line(188,338,400,338,new Color(.29f,.37f,.29f,.4f),1);
            Box(243,354,92,65,new Color(.38f,.43f,.34f)); Box(250,360,78,52,new Color(.77f,.79f,.67f));
            Text(257,376,70,23,"관제실",15,mapInk);
            Line(219,386,242,386,mapRed,5); Text(188,358,56,24,"정문",14,mapRed);
            Line(336,386,345,386,mapGreen,5);
            Line(232,424,344,424,new Color(.29f,.37f,.29f,.55f),1);
            Text(232,429,121,23,"벽을 따라 30m",13,mapInk);
            Box(343,350,62,76,new Color(.19f,.38f,.32f,.13f));
            Disc(366,368,12,mapGreen); Text(360,357,23,24,"E",18,new Color(.95f,.94f,.83f),FontStyle.Bold);
            Text(345,392,67,34,"동쪽\n정비구",13,mapInk,FontStyle.Bold);
            Text(188,452,215,19,"서로의 확인등은 보이지 않음",12,mapRed);
        }
        void DrawMapInspector()
        {
            var selected=Array.Find(story.dossiers,d=>d.id==document&&d.category=="map");
            if(selected==null) { OpenDocument("routes"); selected=Array.Find(story.dossiers,d=>d.id==document); }
            Box(1013,209,425,582,new Color(.025f,.055f,.06f));
            Text(1034,226,381,25,"현장 기록 / 지점 선택",16,teal);
            string[] ids={"routes","bridge","ridge","generator","entry"};
            string[] titles={"A 남쪽 임도","B 북쪽 다리","C 대피소","D 발전기 우회","E 동쪽 정비구"};
            int old=button.fontSize; button.fontSize=17;
            for(int i=0;i<ids.Length;i++) if(InterfaceButton(new Rect(1033+(i%2)*193,264+(i/2)*43,184,36),titles[i],true,ControlTone.Neutral,17,document==ids[i])) InspectMapLocation(ids[i]);
            button.fontSize=old;
            Line(1034,402,1418,402,new Color(.20f,.36f,.33f),1);
            Text(1034,421,379,39,selected.title,26,paper,FontStyle.Bold);
            Text(1034,465,379,42,selected.subtitle,15,teal);
            small.fontSize=ReadingSize(20); small.normal.textColor=paper; WarmText(selected.text,small.fontSize);
            float height=small.CalcHeight(new GUIContent(selected.text),339)+25;
            docScroll=GUI.BeginScrollView(new Rect(1034,515,383,205),docScroll,new Rect(0,0,339,height));
            GUI.Label(new Rect(0,0,339,height),selected.text,small); GUI.EndScrollView();
            if(Btn(1034,742,383,36,"기록 원문 크게 읽기")) ExpandMapRecord();
        }
        void DrawLargeMapRecord()
        {
            var selected=Array.Find(story.dossiers,d=>d.id==document&&d.category=="map");
            if(selected==null) { mapReading=false; return; }
            Box(145,211,1290,571,paper);
            Text(177,233,880,45,selected.title,31,ink,FontStyle.Bold); Text(179,288,1120,36,selected.subtitle,18,mapGreen);
            if(Btn(1112,235,293,43,"지도로 돌아가기  [M]")) ReturnToMap();
            Line(179,337,1402,337,new Color(.31f,.40f,.31f),1);
            small.fontSize=ReadingSize(23); small.normal.textColor=ink; WarmText(selected.text,small.fontSize);
            float height=small.CalcHeight(new GUIContent(selected.text),1168)+30;
            mapLargeScroll=GUI.BeginScrollView(new Rect(179,360,1226,390),mapLargeScroll,new Rect(0,0,1168,height));
            GUI.Label(new Rect(0,0,1168,height),selected.text,small); GUI.EndScrollView();
        }
    }
}
