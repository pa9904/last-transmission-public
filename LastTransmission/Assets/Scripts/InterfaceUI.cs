using System;
using UnityEngine;

namespace LastTransmission
{
    public sealed partial class Game
    {
        enum ControlTone { Neutral, Primary, Danger, Paper }
        Texture2D interfacePaper, scrollRail, scrollThumb;
        readonly Color ivory=new Color(.88f,.87f,.78f), muted=new Color(.58f,.66f,.63f), amber=new Color(.83f,.67f,.40f);
        readonly Color metal=new Color(.16f,.21f,.21f), edge=new Color(.28f,.36f,.34f), dark=new Color(.025f,.045f,.047f);
        void InitializeInterface()
        {
            interfacePaper=new Texture2D(128,128,TextureFormat.RGB24,false);
            var rng=new System.Random(1704); var colors=new Color[128*128];
            for(int i=0;i<colors.Length;i++) { float v=.96f+(float)rng.NextDouble()*.04f; colors[i]=new Color(v,v,v); }
            interfacePaper.SetPixels(colors); interfacePaper.Apply(); interfacePaper.wrapMode=TextureWrapMode.Repeat;
            scrollRail=Solid(new Color(.09f,.13f,.13f)); scrollThumb=Solid(new Color(.48f,.60f,.54f));
            var rail=GUI.skin.verticalScrollbar; rail.normal.background=scrollRail; rail.fixedWidth=12;
            rail.padding=new RectOffset(2,2,2,2); rail.margin=new RectOffset(0,0,0,0);
            var thumb=GUI.skin.verticalScrollbarThumb; thumb.normal.background=scrollThumb;
            thumb.hover.background=thumb.active.background=scrollThumb; thumb.border=new RectOffset();
            thumb.fixedWidth=8; thumb.fixedHeight=0; thumb.padding=new RectOffset();
            GUI.skin.verticalScrollbarUpButton.fixedHeight=0; GUI.skin.verticalScrollbarDownButton.fixedHeight=0;
        }
        Texture2D Solid(Color color) { var t=new Texture2D(1,1); t.SetPixel(0,0,color); t.Apply(); return t; }
        void Outline(float x,float y,float w,float h,Color c,float thickness=1)
        { Box(x,y,w,thickness,c); Box(x,y+h-thickness,w,thickness,c); Box(x,y,thickness,h,c); Box(x+w-thickness,y,thickness,h,c); }
        void Screw(float x,float y)
        { Disc(x,y,4,new Color(.06f,.09f,.09f)); Line(x-2,y+1,x+2,y-1,new Color(.38f,.43f,.40f),1); }
        void DeviceSurface(float x,float y,float w,float h)
        {
            Box(x+5,y+6,w,h,new Color(0,0,0,.4f)); Box(x,y,w,h,metal); Outline(x,y,w,h,edge);
            Line(x+1,y+1,x+w-1,y+1,new Color(.39f,.45f,.42f),1); Box(x+9,y+9,w-18,h-18,new Color(.115f,.16f,.16f));
            Screw(x+7,y+7); Screw(x+w-7,y+7); Screw(x+7,y+h-7); Screw(x+w-7,y+h-7);
        }
        void PaperSurface(float x,float y,float w,float h)
        {
            Box(x+5,y+6,w,h,new Color(0,0,0,.28f));
            var old=GUI.color; GUI.color=ivory; GUI.DrawTextureWithTexCoords(new Rect(x,y,w,h),interfacePaper,new Rect(0,0,w/256f,h/256f)); GUI.color=old;
            Outline(x,y,w,h,new Color(.61f,.61f,.51f)); Line(x+2,y+2,x+w-2,y+2,new Color(.96f,.94f,.84f),1);
        }
        bool InterfaceButton(Rect r,string caption,bool enabled,ControlTone tone,int size=20,bool selected=false)
        {
            bool available=GUI.enabled&&enabled, hover=available&&r.Contains(Event.current.mousePosition);
            string name="control-"+r.x+"-"+r.y; bool focus=GUI.GetNameOfFocusedControl()==name;
            Color fill=new Color(.11f,.17f,.17f), stroke=edge, foreground=ivory;
            if(tone==ControlTone.Primary) { fill=new Color(.66f,.53f,.32f); stroke=amber; foreground=new Color(.06f,.10f,.09f); }
            if(tone==ControlTone.Danger) { fill=new Color(.25f,.105f,.09f); stroke=new Color(.55f,.26f,.20f); foreground=new Color(.94f,.78f,.67f); }
            if(tone==ControlTone.Paper) { fill=new Color(.78f,.79f,.69f); stroke=new Color(.52f,.58f,.47f); foreground=ink; }
            if(selected) { fill=new Color(.19f,.34f,.29f); stroke=teal; foreground=ivory; }
            if(hover) { fill=Color.Lerp(fill,Color.white,.10f); stroke=tone==ControlTone.Primary?ivory:teal; }
            if(!available)
            {
                fill=new Color(.09f,.13f,.13f); stroke=new Color(.17f,.23f,.21f); foreground=new Color(.44f,.51f,.47f);
                if(tone==ControlTone.Paper) { fill=selected?new Color(.62f,.69f,.59f):new Color(.78f,.78f,.68f); stroke=new Color(.56f,.60f,.50f); foreground=new Color(.28f,.35f,.29f); }
            }
            Box(r.x,r.y+3,r.width,r.height,new Color(0,0,0,.24f)); Box(r.x,r.y,r.width,r.height,fill); Outline(r.x,r.y,r.width,r.height,stroke);
            Line(r.x+2,r.y+2,r.x+r.width-2,r.y+2,new Color(1,1,1,available?.065f:.015f),1);
            if(hover||focus||selected) Box(r.x,r.y,3,r.height,tone==ControlTone.Primary?ivory:teal);
            var textKey=(size,FontStyle.Normal,foreground);
            if(!textStyles.TryGetValue(textKey,out var captionStyle))
            {
                captionStyle=new GUIStyle(label) { fontSize=size,fontStyle=FontStyle.Normal,alignment=TextAnchor.UpperLeft };
                captionStyle.normal.textColor=foreground; textStyles.Add(textKey,captionStyle);
            }
            float textHeight=Mathf.Min(r.height-10,Mathf.Max(size+4,captionStyle.CalcHeight(new GUIContent(caption),r.width-30)));
            Text(r.x+16,r.y+(r.height-textHeight)/2,r.width-30,textHeight,caption,size,foreground);
            bool previous=GUI.enabled; GUI.enabled=available; GUI.SetNextControlName(name);
            bool clicked=GUI.Button(r,GUIContent.none,GUIStyle.none); GUI.enabled=previous; return clicked;
        }
        bool Primary(float x,float y,float w,float h,string text,bool enabled=true)
        { return InterfaceButton(new Rect(x,y,w,h),text,enabled,ControlTone.Primary,20); }
        bool PaperOption(float x,float y,float w,float h,string text,bool selected,bool editable)
        {
            var row=new Rect(x,y,w,h);
            Color graphite=new Color(.19f,.20f,.18f);
            if(editable&&GUI.enabled&&row.Contains(Event.current.mousePosition))
                Box(x,y,w,h,new Color(.22f,.21f,.15f,.065f));
            Line(x+5,y+h-1,x+w-4,y+h-1,new Color(.27f,.27f,.20f,.32f),1);
            Outline(x+9,y+17,17,17,new Color(.34f,.35f,.29f));
            if(selected)
            {
                Line(x+10,y+23,x+17,y+31,graphite,2.6f);
                Line(x+17,y+31,x+32,y+11,graphite,2.8f);
            }
            Text(x+45,y+8,w-53,h-8,text,ReadingSize(19),ink,selected?FontStyle.Bold:FontStyle.Normal);
            return editable&&GUI.Button(row,GUIContent.none,GUIStyle.none);
        }
        bool NumberedResponse(float x,float y,float w,float h,int number,string text)
        {
            var r=new Rect(x,y,w,h); bool hover=GUI.enabled&&r.Contains(Event.current.mousePosition);
            Box(x,y,w,h,hover?new Color(.17f,.27f,.24f):new Color(.085f,.14f,.14f)); Outline(x,y,w,h,hover?teal:edge);
            Box(x+9,y+9,30,h-18,new Color(.21f,.30f,.26f)); Text(x+17,y+12,23,28,number.ToString("00"),16,amber,FontStyle.Bold);
            Text(x+54,y+10,w-70,h-14,text,20,ivory);
            return GUI.Button(r,GUIContent.none,GUIStyle.none);
        }
        void Section(float x,float y,string index,string title,Color color)
        { Text(x,y,38,25,index,13,color); Line(x+34,y+2,x+34,y+18,color,1); Text(x+48,y-1,600,28,title,17,color); }
        void DeviceIcon(float x,float y,string id,float s,Color color)
        {
            if(id=="radio") { Outline(x,y+s*.3f,s,s*.62f,color,2); Line(x+s*.2f,y+s*.3f,x+s*.1f,y,color,2); Disc(x+s*.74f,y+s*.61f,s*.09f,color); for(int i=0;i<3;i++)Line(x+s*.12f,y+s*(.48f+i*.12f),x+s*.49f,y+s*(.48f+i*.12f),color,1); }
            else if(id=="map") { Outline(x,y+s*.13f,s,s*.75f,color,1); Line(x+s*.33f,y+s*.13f,x+s*.33f,y+s*.88f,color,1); Line(x+s*.66f,y+s*.13f,x+s*.66f,y+s*.88f,color,1); Line(x+s*.1f,y+s*.7f,x+s*.8f,y+s*.3f,color,2); }
            else if(id=="door") { Outline(x+s*.15f,y,s*.7f,s,color,2); Disc(x+s*.66f,y+s*.53f,2,color); }
            else if(id=="board") { Outline(x,y+s*.1f,s,s*.8f,color,1); Disc(x+s*.24f,y+s*.32f,3,color); Disc(x+s*.74f,y+s*.65f,3,color); Line(x+s*.24f,y+s*.32f,x+s*.74f,y+s*.65f,color,1); }
            else if(id=="archive") { Disc(x+s*.5f,y+s*.5f,s*.46f,color); Disc(x+s*.5f,y+s*.5f,s*.36f,dark); Line(x+s*.5f,y+s*.5f,x+s*.5f,y+s*.23f,color,2); Line(x+s*.5f,y+s*.5f,x+s*.72f,y+s*.61f,color,2); }
            else { Outline(x+s*.1f,y,s*.8f,s,color,1); for(int i=0;i<3;i++)Line(x+s*.26f,y+s*(.3f+i*.2f),x+s*.75f,y+s*(.3f+i*.2f),color,1); }
        }
        void DrawInstrumentFrame(string eyebrow,string title)
        {
            DeviceSurface(108,80,1372,735); Box(121,93,1346,709,new Color(.038f,.065f,.068f));
            Line(143,181,1442,181,new Color(.26f,.35f,.31f),1);
            Text(146,101,890,25,eyebrow,15,muted); Text(145,128,930,49,title,35,ivory,FontStyle.Bold);
            if(Btn(1330,109,112,42,"닫기  Esc")) OpenPanel("");
            if(panel!="intro"&&InterfaceButton(new Rect(1100,109,212,42),"다음 확인  [N]",true,ControlTone.Neutral,17)) OpenNextTask();
        }
        void DrawInstrumentStatus()
        {
            var canvas=GUI.matrix;
            GUI.matrix=canvas*Matrix4x4.Translate(new Vector3(0,viewport.yMin,0));
            if(NarrowWindow)
            {
                Box(viewport.xMin,0,viewport.width,99,new Color(.018f,.034f,.036f,.98f));
                Line(viewport.xMin,98,viewport.xMax,98,edge,1);
                Text(44,22,580,49,"북부 산악구조 관제소",32,ivory,FontStyle.Bold);
                Text(626,22,225,49,story.calls[state.call].time,41,ivory,FontStyle.Bold);
                Text(865,32,270,43,"교신 "+(state.call+1).ToString("00")+" / 06",29,teal);
                if(InterfaceButton(new Rect(1210,16,344,65),"일시정지  [Esc]",true,ControlTone.Neutral,30)) overlay="pause";
                GUI.matrix=canvas; return;
            }
            Box(viewport.xMin,0,viewport.width,67,new Color(.018f,.034f,.036f,.98f)); Line(viewport.xMin,66,viewport.xMax,66,new Color(.25f,.32f,.29f),1);
            Disc(48,34,5,teal); Text(65,11,420,26,"북부 산악구조 관제소",20,ivory,FontStyle.Bold); Text(65,39,440,20,"야간 당직 / 폭설로 외부 이동 제한",14,muted);
            Text(696,10,200,44,story.calls[state.call].time,35,ivory,FontStyle.Bold); Text(855,24,200,26,"교신 "+(state.call+1).ToString("00")+" / 06",17,teal);
            Text(1110,14,240,22,"예비 전원  "+(98-state.call*9)+"%",13,muted);
            for(int i=0;i<12;i++) Box(1111+i*17,41,12,7,i<12-state.call?teal:new Color(.15f,.22f,.21f));
            if(InterfaceButton(new Rect(1380,17,178,35),"일시정지  [Esc]",true,ControlTone.Neutral,16)) overlay="pause";
            GUI.matrix=canvas;
            if(panel=="intro") return;
            GUI.matrix=canvas*Matrix4x4.Translate(new Vector3(0,viewport.yMax-900,0));
            Text(111,883,900,18,panel=="radio"?"Space 즉시 표시 / 다음 수신 내용    ·    긴 본문은 휠로 이동":"장치를 눌러 조사합니다    ·    N 다음에 확인할 기록",12,muted);
            Text(1260,883,240,18,"● 진행 자동 저장",12,teal);
            GUI.matrix=canvas;
        }
        void DrawInstrumentDock()
        {
            string[] ids={"radio","map","records","board","archive","door"}; string[] titles={"무전","수색 지도","근무 기록","상황판","교신 기록","출입 제어"};
            string[] keys={"R","M","L","B","H","D"};
            var canvas=GUI.matrix;
            GUI.matrix=canvas*Matrix4x4.Translate(new Vector3(0,viewport.yMax-900,0));
            if(NarrowWindow)
            {
                Box(viewport.xMin,665,viewport.width,235,new Color(.018f,.034f,.036f,.98f));
                for(int i=0;i<6;i++)
                    if(InterfaceButton(new Rect(36+i%3*516,677+i/3*102,496,91),titles[i]+"    ["+keys[i]+"]",true,ControlTone.Neutral,31,panel==ids[i]))
                    {
                        if(ids[i]=="map"&&mapReading) ReturnToMap(); else OpenPanel(ids[i]);
                    }
                GUI.matrix=canvas; return;
            }
            var bounds=DeviceBounds();
            float segment=(bounds.width-12)/6;
            Box(viewport.xMin,821,viewport.width,57,new Color(.018f,.034f,.036f,.98f));
            Box(bounds.x,824,bounds.width,53,new Color(.025f,.045f,.046f,.97f)); Outline(bounds.x,824,bounds.width,53,edge);
            for(int i=0;i<6;i++)
            {
                float x=bounds.x+6+i*segment; bool selected=panel==ids[i];
                var r=new Rect(x,829,segment-5,43); bool hover=GUI.enabled&&r.Contains(Event.current.mousePosition);
                if(selected||hover) Box(x,829,r.width,43,selected?new Color(.15f,.27f,.23f):new Color(.10f,.17f,.16f));
                if(selected) Box(x,870,r.width,2,amber);
                DeviceIcon(x+13,840,ids[i],21,selected?ivory:muted); Text(x+47,839,r.width-82,29,titles[i],19,selected?ivory:muted,selected?FontStyle.Bold:FontStyle.Normal);
                Outline(r.xMax-36,839,25,24,selected?teal:edge); Text(r.xMax-29,842,23,22,keys[i],15,selected?ivory:muted);
                if(GUI.Button(r,GUIContent.none,GUIStyle.none)) { if(ids[i]=="map"&&mapReading) ReturnToMap(); else OpenPanel(ids[i]); }
            }
            GUI.matrix=canvas;
        }
        float receiverNeedle,echoNeedleHold;
        bool echoNeedleSeen;
        internal float ReceiverNeedle => receiverNeedle;
        internal bool ReceiverNeedleHeld => echoNeedleHold>0;

        void UpdateReceiverNeedle(float seconds)
        {
            if(screen!="play"||panel!="radio"||overlay!=""||confirm!="") return;
            bool tuned=Tuned();
            bool receiving=tuned&&state.phase<2&&!prefs.instantText&&visible<CurrentPage().Length;
            // The copied first response leaves a carrier behind after its text has finished.
            if(tuned&&state.phase==0&&state.call==4&&state.page==0)
            {
                if(!receiving&&!echoNeedleSeen)
                {
                    echoNeedleSeen=true;
                    echoNeedleHold=1.65f;
                }
                else echoNeedleHold=Mathf.Max(0,echoNeedleHold-seconds);
            }
            else echoNeedleHold=0;

            float target=.035f;
            if(!tuned) target=prefs.motion?.18f+.13f*Mathf.Sin(elapsed*13)*Mathf.Cos(elapsed*7):.18f;
            else if(ReceiverNeedleHeld) target=.84f;
            else if(receiving) target=prefs.motion?.73f+.035f*Mathf.Sin(elapsed*10):.73f;
            float rate=target>receiverNeedle?4f:1.35f;
            receiverNeedle=prefs.motion?Mathf.MoveTowards(receiverNeedle,target,rate*seconds):target;
        }

        void DrawReceiverMeter()
        {
            var bezel=new Rect(338,312,121,91);
            Box(bezel.x,bezel.y,bezel.width,bezel.height,new Color(.013f,.027f,.025f));
            Outline(bezel.x,bezel.y,bezel.width,bezel.height,new Color(.29f,.38f,.31f),2);
            Box(344,318,109,64,new Color(.74f,.77f,.64f));
            var pivot=new Vector2(398,378);
            for(int i=0;i<=12;i++)
            {
                float angle=Mathf.Lerp(-63,63,i/12f)*Mathf.Deg2Rad;
                var direction=new Vector2(Mathf.Sin(angle),-Mathf.Cos(angle));
                Vector2 outer=pivot+direction*54,inner=pivot+direction*(i%3==0?46:50);
                Line(inner.x,inner.y,outer.x,outer.y,i>=10?red:ink,i%3==0?2:1);
            }
            float needleAngle=Mathf.Lerp(-63,63,receiverNeedle)*Mathf.Deg2Rad;
            var tip=pivot+new Vector2(Mathf.Sin(needleAngle),-Mathf.Cos(needleAngle))*51;
            Line(pivot.x,pivot.y,tip.x,tip.y,new Color(.36f,.14f,.10f),2);
            Disc(pivot.x,pivot.y,4,ink);
            Text(362,385,91,18,"수신 강도",12,muted);
        }
        const float RadioTextWidth=842;
        void DrawReceiver()
        {
            var call=story.calls[state.call];
            if(NarrowWindow) { DrawStackedReceiver(call); return; }
            Frame("무선 / 수신·송신 기록 · "+call.time,call.title);
            DeviceSurface(145,210,342,583); Section(168,229,"01","무전 수신기",muted);
            Box(167,271,300,144,new Color(.018f,.036f,.031f)); Outline(167,271,300,144,new Color(.29f,.38f,.31f));
            Disc(188,294,4,Tuned()?teal:red); Text(202,285,285,27,state.phase==1?"송신 기록":Tuned()?"수신 대역 연결":"수신 대역 불일치",15,Tuned()?teal:amber);
            Text(184,327,149,50,state.frequency,43,teal,FontStyle.Bold); Text(187,378,70,23,"MHz",15,muted);
            DrawReceiverMeter();
            Text(170,433,328,25,"수신 안내  "+call.frequency+" MHz",16,ivory);
            string[] freq={"83.4","91.2","104.6"};
            for(int i=0;i<3;i++) if(InterfaceButton(new Rect(167+i*102,470,96,46),freq[i],state.phase<2,ControlTone.Neutral,20,state.frequency==freq[i])) SelectFrequency(freq[i]);
            Line(167,540,467,540,edge,1); Section(170,561,"02","응답 전 대조",muted);
            int row=0;
            foreach(var id in call.documents)
            {
                var record=Array.Find(story.dossiers,d=>d.id==id);
                if(InterfaceButton(new Rect(167,603+row*46,300,39),(state.Read(id)?"✓ ":"· ")+record.title,true,ControlTone.Neutral,17)) { OpenPanel(record.category=="map"?"map":"records"); OpenDocument(id); } row++;
            }
            int findings=0; for(int q=0;q<call.findings.Length;q++) if(state.Finding(state.call,q)>=0) findings++;
            if(InterfaceButton(new Rect(167,711,300,49),"상황판 근거  "+findings+" / 2  [B]",true,ControlTone.Neutral,18)) OpenPanel("board");
            DrawReceiverTranscript(call);
        }

        void DrawStackedReceiver(Transmission call)
        {
            const float width=970,height=874;
            float top=viewport.yMin+115,bottom=DockTop-15;
            float scale=Mathf.Min((viewport.width-72)/width,(bottom-top)/height);
            var canvas=GUI.matrix;
            GUI.matrix=canvas*Matrix4x4.TRS(new Vector3(800-width*scale/2,(top+bottom-height*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
            DeviceSurface(0,0,width,height);
            Text(22,17,683,43,call.title,33,ivory,FontStyle.Bold);
            if(InterfaceButton(new Rect(710,13,118,40),"다음 [N]",true,ControlTone.Neutral,19)) OpenNextTask();
            if(InterfaceButton(new Rect(840,13,108,40),"닫기 Esc",true,ControlTone.Neutral,19)) OpenPanel("");
            Box(20,68,930,136,new Color(.026f,.054f,.050f));
            Text(35,78,295,54,state.frequency+" MHz",38,teal,FontStyle.Bold);
            Text(37,132,295,27,Tuned()?"수신 대역 연결":"수신 대역 불일치",19,Tuned()?teal:amber);
            Text(323,84,468,31,"수신 안내  "+call.frequency+" MHz",19,muted);
            string[] frequencies={"83.4","91.2","104.6"};
            for(int i=0;i<frequencies.Length;i++)
                if(InterfaceButton(new Rect(323+i*148,127,136,52),frequencies[i],state.phase<2,ControlTone.Neutral,23,state.frequency==frequencies[i])) SelectFrequency(frequencies[i]);
            var local=GUI.matrix;
            GUI.matrix=local*Matrix4x4.Translate(new Vector3(476,-222,0));
            DrawReceiverMeter();
            GUI.matrix=local;
            for(int i=0;i<call.documents.Length;i++)
            {
                var record=Array.Find(story.dossiers,d=>d.id==call.documents[i]);
                if(InterfaceButton(new Rect(20+310*i,217,300,47),(state.Read(record.id)?"✓  ":"")+record.title,true,ControlTone.Neutral,21))
                { OpenPanel(record.category=="map"?"map":"records"); OpenDocument(record.id); }
            }
            if(InterfaceButton(new Rect(640,217,310,47),"상황판  [B]",true,ControlTone.Neutral,21)) OpenPanel("board");
            GUI.matrix=local*Matrix4x4.Translate(new Vector3(-489,67,0));
            DrawReceiverTranscript(call);
            GUI.matrix=canvas;
        }

        void DrawReceiverTranscript(Transmission call)
        {
            PaperSurface(509,210,928,392); Box(529,226,4,33,new Color(.28f,.42f,.34f));
            Text(546,225,560,33,call.caller,24,ink,FontStyle.Bold);
            Text(1210,229,202,24,state.phase==1?"송신 결과":"수신 "+(state.page+1)+" / "+call.pages.Length,16,new Color(.31f,.42f,.34f));
            Line(541,272,1408,272,new Color(.55f,.59f,.48f),1);
            if(state.phase>=2)
            {
                Text(541,310,865,180,"모든 교신이 기록되었습니다.\n\n마지막 신원과 출입 경로를 출입 제어기에서 판단하십시오.",26,ink);
                if(Primary(531,689,884,62,"출입 제어기로 이동  [D]")) OpenPanel("door"); return;
            }
            if(!Tuned())
            {
                Text(541,312,865,58,"수신 대역을 확인하십시오.",29,ink,FontStyle.Bold);
                Text(541,400,865,111,"현재 주파수에서는 교신이 식별되지 않습니다.\n왼쪽 수신기의 주파수를 "+call.frequency+" MHz로 맞추십시오.",23,ink);
                Section(531,632,"03","주파수를 맞춘 뒤 교신을 읽으십시오",muted); return;
            }
            string value=CurrentPage(); int count=prefs.instantText?value.Length:Mathf.Min(value.Length,(int)visible);
            small.fontSize=ReadingSize(23); small.normal.textColor=ink; small.wordWrap=true; WarmText(value,small.fontSize);
            float height=small.CalcHeight(new GUIContent(value.Substring(0,count)),RadioTextWidth)+18;
            radioScroll=GUI.BeginScrollView(new Rect(541,289,873,296),radioScroll,new Rect(0,0,RadioTextWidth,Mathf.Max(296,height)));
            GUI.Label(new Rect(0,0,RadioTextWidth,height),value.Substring(0,count),small); GUI.EndScrollView();
            Section(531,621,"03",state.phase==1?"송신 완료 / 현장 응답":"관제 응답",muted);
            if(count<value.Length) { Text(531,670,544,60,"교신을 읽는 중입니다.\nSpace로 전체 내용을 바로 표시할 수 있습니다.",20,muted); if(Primary(1120,710,295,56,"즉시 표시  [Space]")) RevealAll(); return; }
            if(state.phase==1)
            {
                Text(531,663,884,60,Rules.Supported(story,state,state.call)?"응답과 판단 근거를 교신 보관철에 기록했습니다.":"판단 메모에 원문과 다른 해석이 남았습니다. 상황판과 교신 기록에서 대조할 수 있습니다.",20,Rules.Supported(story,state,state.call)?teal:amber);
                if(Primary(531,735,884,57,state.call==5?"마지막 출입 판단으로  →":"기록 보관 · 관제실로 돌아가기  →")) NextCall(); return;
            }
            if(state.page<call.pages.Length-1) { Text(531,669,884,42,"계속되는 교신을 확인하십시오.",21,muted); if(Primary(531,735,884,57,"다음 수신 내용  [Space]")) RevealOrNext(); return; }
            if(!Rules.CanRespond(story,state))
            {
                Text(531,667,884,53,"관련 기록을 열람하고 상황판에 판단 근거 두 가지를 남긴 뒤 응답할 수 있습니다.",21,ivory);
                if(Primary(531,735,884,57,"다음에 확인할 기록으로  [N]")) OpenNextTask(); return;
            }
            for(int i=0;i<call.choices.Length;i++) if(NumberedResponse(531,648+i*49,884,43,i+1,call.choices[i].text)) Respond(i);
        }
        internal void SelectFrequency(string frequency)
        { if(frequency!="83.4"&&frequency!="91.2"&&frequency!="104.6") return; state.frequency=frequency; visible=0; Save(); }
        void DrawRecordFolder()
        {
            Frame("기록 / 인수인계 서류철","야간 근무 기록");
            DeviceSurface(145,211,322,581); Section(167,233,"01","보관 문서",muted);
            int available=Array.FindAll(story.dossiers,d=>d.category=="records"&&state.call>=d.unlock).Length;
            Text(167,271,278,35,"열람 가능  "+available+"건",15,teal);
            listScroll=GUI.BeginScrollView(new Rect(161,318,291,441),listScroll,new Rect(0,0,270,available*68));
            int row=0;
            foreach(var d in story.dossiers)
            {
                if(d.category!="records"||state.call<d.unlock) continue;
                if(InterfaceButton(new Rect(0,row*68,265,59),(state.Read(d.id)?"✓ ":"· ")+d.title,true,ControlTone.Neutral,19,document==d.id)) OpenDocument(d.id); row++;
            }
            GUI.EndScrollView();
            var selected=Array.Find(story.dossiers,d=>d.id==document&&d.category=="records");
            if(selected==null) { OpenDocument("handover"); selected=Array.Find(story.dossiers,d=>d.id==document); }
            Box(494,219,937,575,new Color(.35f,.39f,.31f)); PaperSurface(483,211,946,575);
            Line(523,232,523,759,new Color(.57f,.30f,.24f,.24f),1);
            Text(548,239,690,45,selected.title,30,ink,FontStyle.Bold);
            Text(548,296,814,43,selected.subtitle,18,new Color(.30f,.40f,.32f));
            Outline(1303,240,99,36,new Color(.32f,.45f,.34f)); Text(1326,246,78,27,"열람",19,new Color(.30f,.43f,.32f),FontStyle.Bold);
            Line(548,352,1397,352,new Color(.54f,.57f,.46f),1);
            small.fontSize=ReadingSize(22); small.normal.textColor=ink; WarmText(selected.text,small.fontSize);
            float height=small.CalcHeight(new GUIContent(selected.text),793)+30;
            docScroll=GUI.BeginScrollView(new Rect(548,377,853,323),docScroll,new Rect(0,0,793,height));
            GUI.Label(new Rect(0,0,793,height),selected.text,small); GUI.EndScrollView();
            if(InterfaceButton(new Rect(548,725,849,42),"상황판에 근거 정리  [B]",true,ControlTone.Paper,19)) OpenPanel("board");
        }
        void MenuBackdrop(bool wide=false)
        {
            Box(viewport.xMin,viewport.yMin,viewport.width,viewport.height,new Color(.018f,.032f,.034f));
            float x=wide?108:143, width=wide?1372:1314;
            DeviceSurface(x,80,width,742); Box(x+14,94,width-28,714,new Color(.035f,.057f,.058f));
            Text(1070,104,350,23,"NORTH STATION / 야간 근무",12,muted);
        }
        void DrawShiftPause()
        {
            MenuBackdrop(); Text(200,152,1160,64,"근무 일시정지",40,ivory,FontStyle.Bold);
            string[] titles={"계속하기","설정","엔딩 기록","크레딧","저장하고 타이틀로","저장하고 게임 종료"};
            for(int i=0;i<titles.Length;i++)
            {
                bool hit=i==0?Primary(200,251+i*76,413,57,titles[i]):Btn(200,251+i*76,413,57,titles[i]);
                if(!hit) continue;
                if(i==0) overlay=""; else if(i==1) ShowSettings(); else if(i==2) ShowCollection(); else if(i==3) ShowCredits();
                else if(i==4) { if(playing) Save(); playing=false; screen="title"; panel=""; overlay=""; }
                else { if(playing) Save(); SavePrefs(); Application.Quit(); }
            }
            PaperSurface(660,251,735,437); Text(696,283,650,36,"당직 인수인계",27,ink,FontStyle.Bold);
            Line(697,338,1358,338,new Color(.55f,.59f,.48f),1);
            string info=playing&&state!=null?"현재 기록  /  "+story.calls[state.call].time+"\n"+story.calls[state.call].title+"\n\n"+NextTask():"폭설로 고립된 북부 관제소.\n목소리와 기록이 서로 다른 사실을 말한다.";
            Text(697,365,650,188,info,23,ink);
            Text(697,592,649,70,"시간 제한은 없습니다.\n쉬거나 기록을 조사하는 동안 교신 결과는 바뀌지 않습니다.",20,new Color(.30f,.42f,.33f));
        }
        void DrawMenuOverlay()
        {
            if(overlay=="settings") { DrawQualitySettings(); return; }
            if(overlay=="collection") { DrawCollection(); return; }
            if(overlay=="pause") { DrawShiftPause(); return; }
            MenuBackdrop(); Text(200,152,1150,66,"크레딧",40,ivory,FontStyle.Bold);
            PaperSurface(200,248,1190,450);
            Text(237,278,1112,393,"마지막 교신 / Last Transmission\n\n기획 · 이야기 · 코드 · 지도 · 장치 UI · 음악 · 환경음: 프로젝트 제작\n관제실 배경: 프로젝트용 AI 생성 (image_gen)\n\n한국어 글꼴: SUIT Regular · Bold / SUNN / SIL OFL 1.1\n\n외부 에셋은 각각의 라이선스를 유지합니다.\n출처·라이선스·배경 생성 프롬프트: 배포 폴더 ThirdPartyNotices\nUI 참고 작품과 적용 원칙: 저장소 Docs/UI_REFERENCES.md",22,ink);
            if(Primary(200,747,1190,55,"돌아가기")) CloseQualityOverlay();
        }
        void DrawDecisionConfirmation()
        {
            Box(viewport.xMin,viewport.yMin,viewport.width,viewport.height,new Color(.005f,.014f,.016f,.92f)); DeviceSurface(324,228,952,436);
            bool fresh=confirm=="new"; string action="";
            if(!fresh) { int choice=int.Parse(confirm.Substring(4)); action=choice==0?"정문 개방":choice==1?"모든 출입 잠금":"동쪽 정비구 개방"; }
            Section(366,268,"확인",fresh?"기존 진행 교체":"최종 출입 결정",amber);
            Text(366,323,866,70,fresh?"새 근무를 시작할까요?":action+"을 확정할까요?",31,ivory,FontStyle.Bold);
            Text(366,413,866,91,fresh?"현재 이어하기 기록은 새 진행으로 교체됩니다.\n이미 확인한 결말은 엔딩 보관철에 계속 남습니다.":"이 결정으로 이번 근무가 끝나며 결과가 저장됩니다.\n원문을 다시 대조하려면 취소하고 지도와 기록으로 돌아가십시오.",23,ivory);
            if(Btn(366,562,420,57,"취소  [Esc]")) confirm="";
            if(InterfaceButton(new Rect(806,562,420,57),fresh?"새 근무 시작":"결정 확정",true,fresh?ControlTone.Primary:ControlTone.Danger,22))
            { var choice=confirm; confirm=""; if(fresh) NewGame(); else DecideDoor(int.Parse(choice.Substring(4))); }
        }
        bool DoorDecision(Rect rect,string title,string detail,ControlTone tone,bool enabled=true)
        {
            bool pressed=InterfaceButton(rect,"",enabled,tone,20);
            Color c=!enabled?muted:tone==ControlTone.Primary?ink:ivory;
            Text(rect.x+19,rect.y+19,rect.width-38,37,title,23,c,FontStyle.Bold);
            Text(rect.x+19,rect.y+70,rect.width-38,48,detail,17,c);
            return pressed;
        }
        void DrawDoorConsole()
        {
            Frame("출입 / 수동 잠금·독립 확인 회로","신원을 확인하고 출입을 결정한다");
            Text(153,206,1270,35,"정문 수동 잠금    /    동쪽 보조 전원 "+(state.Has("technician")?"연결":"없음")+"    /    미확인 사전 승인 "+(state.authorizationRevoked?0:Rules.Exposure(state))+"건",20,teal);
            if(state.phase!=2)
            {
                DeviceSurface(149,269,1286,451); DeviceIcon(193,319,"door",75,amber);
                Text(320,320,1060,62,"출입 잠금 유지",30,ivory,FontStyle.Bold);
                Text(320,408,1020,137,"현재 교신의 구조 경로와 신원을 먼저 확인하십시오.\n\n정문 우선 대기·신원 승인 기록은 이 장치에 남습니다. 마지막 교신 뒤에는 내부 전용 확인등을 조작할 수 있습니다.",24,ivory);
                if(Primary(320,619,1065,60,"무전기로 돌아가기  [R]")) OpenPanel("radio"); return;
            }
            DeviceSurface(148,259,386,320); DeviceSurface(549,259,432,320); DeviceSurface(997,259,440,320);
            Section(169,281,"01","정문 / 미확인",muted); DeviceIcon(173,333,"door",43,amber);
            Text(240,338,268,40,"잠금 유지",26,ivory,FontStyle.Bold);
            Text(169,404,343,126,"‘솔새. 다리 건너서 왔어.\n빨간 등 아래야.’\n\n다리는 붕괴했고 외부등은 단선이다.",21,ivory);
            Text(169,547,343,24,"사전 승인은 신원 확인을 대신하지 못한다.",14,amber);
            Section(571,281,"02","내부 전용 확인등",muted);
            for(int i=0;i<3;i++)
            {
                float x=614+i*148; Disc(x,351,29,new Color(.04f,.065f,.064f)); Disc(x,351,23,new Color(.27f,.33f,.29f));
                Color c=i<state.pulses.Count?(state.pulses[i]==0?red:teal):new Color(.10f,.15f,.13f);
                Disc(x,351,18,c); if(i<state.pulses.Count) Disc(x-6,345,4,new Color(1,1,1,.28f));
                Text(x-11,386,50,25,(i+1).ToString("00"),13,muted);
            }
            bool active=state.Has("technician")&&state.Has("verificationrequested")&&!state.Has("verified");
            if(Btn(570,420,187,47,"빨강 보내기",active&&state.pulses.Count<3,true)) SendPulse(0);
            if(InterfaceButton(new Rect(773,420,187,47),"초록 보내기",active&&state.pulses.Count<3,ControlTone.Neutral,20)) SendPulse(1);
            Text(573,486,383,48,ColorSequence()==""?"순서는 말하지 않고\n두 색을 섞어 세 번 표시합니다.":ColorSequence(),19,ivory);
            if(InterfaceButton(new Rect(570,538,390,29),"표시 지우고 다시 확인",active,ControlTone.Neutral,15)) { state.pulses.Clear(); hardwareMessage=""; Save(); }
            Section(1018,281,"03","각 출입구의 관측",muted);
            if(state.pulses.Count==3)
            {
                Text(1018,333,392,101,"정문: 빨강 → 빨강 → 빨강\n\n동쪽: "+(state.Has("technician")?ColorSequence()+"\n마지막 등이 늦게 켜졌어요.":"응답 없음"),19,ivory);
                bool mixed=state.pulses.Contains(0)&&state.pulses.Contains(1);
                if(InterfaceButton(new Rect(1018,457,398,43),"정문 보고를 신원 근거로 채택",active&&mixed,ControlTone.Neutral,18)) ConfirmPerson(0);
                if(InterfaceButton(new Rect(1018,512,398,43),"동쪽 보고를 신원 근거로 채택",active&&mixed,ControlTone.Neutral,18)) ConfirmPerson(1);
                if(!mixed) Text(1018,557,390,22,"한 색만으로는 구별할 수 없습니다.",13,amber);
            }
            else Text(1018,346,390,185,!state.Has("technician")?"서윤의 회선이 응답하지 않는다.\n발전기에서 나오는 경로가 완성되지 않았다.":!state.Has("verificationrequested")?"독립 관측을 요청하지 않았다.\n기존 암호와 기억만으로는 이 회로를 검증할 수 없다.":"세 번의 표시가 끝나면\n정문과 동쪽 정비구의 보고를 비교할 수 있습니다.",21,ivory);
            string status=hardwareMessage!=""?hardwareMessage:state.Has("verified")?"실시간 관측 검증 완료. 동쪽 정비구의 서윤을 확인했습니다.":"최종 결정을 확정하기 전에는 어떤 문도 열리지 않습니다.";
            Text(156,600,803,48,status,19,state.Has("verified")?teal:ivory);
            if(InterfaceButton(new Rect(1000,594,434,47),state.authorizationRevoked?"정문 사전 승인 철회됨":"기존 정문 승인 취소",Rules.Exposure(state)>0&&!state.authorizationRevoked,ControlTone.Danger,19)) RevokeAuthorization();
            if(DoorDecision(new Rect(156,665,405,125),"정문 개방","미확인 목소리를 안으로 들입니다.",ControlTone.Danger)) confirm="door0";
            if(DoorDecision(new Rect(579,665,405,125),"모든 출입 잠금","날이 밝을 때까지 버팁니다.",ControlTone.Neutral)) confirm="door1";
            bool hatch=Rules.CanOpenHatch(state);
            string detail=hatch?"검증된 서윤만 들이고 정문은 잠급니다.":state.Has("verified")?"기존 정문 승인을 먼저 취소하십시오.":"보조 전원과 실시간 신원 확인이 필요합니다.";
            if(DoorDecision(new Rect(1002,665,419,125),"동쪽 정비구만 개방",detail,ControlTone.Primary,hatch)) confirm="door2";
        }
    }
}
