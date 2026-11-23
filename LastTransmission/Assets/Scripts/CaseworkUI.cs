using System;
using UnityEngine;

namespace LastTransmission
{
    public sealed partial class Game
    {
        Texture2D roomArt;
        int boardCall;
        int comparison=-1, transcriptPage;
        Vector2 statementScroll, evidenceScroll;
        int LastReceivedPage => boardCall<state.call||state.phase>0 ? story.calls[boardCall].pages.Length-1 : state.page;

        internal void CompareEvidence(int question)
        {
            var finding=story.calls[boardCall].findings[question];
            var document=Array.Find(story.dossiers,item=>item.id==finding.source);
            if(document==null||document.unlock>state.call) return;
            comparison=question;
            transcriptPage=LastReceivedPage;
            statementScroll=evidenceScroll=Vector2.zero;
            if(!state.Read(document.id)) { state.read.Add(document.id); Save(); }
        }
        void DrawComparison()
        {
            var call=story.calls[boardCall];
            var finding=call.findings[comparison];
            var source=Array.Find(story.dossiers,item=>item.id==finding.source);
            Frame("상황판 / 원문 대조",call.time+"   "+call.caller);
            Text(156,197,1280,48,finding.question,ReadingSize(23),ivory,FontStyle.Bold);
            PaperSurface(149,254,603,451); PaperSurface(778,254,655,451);
            Text(176,278,545,32,"수신 녹취   /   "+call.frequency+" MHz",19,ink,FontStyle.Bold);
            Text(808,278,595,32,(source.category=="map"?"지도 기록":"근무 기록")+"   /   "+source.title,19,ink,FontStyle.Bold);
            Line(176,319,724,319,muted,1); Line(807,319,1403,319,muted,1);
            string statement=boardCall==state.call&&state.phase==0&&state.page==0&&!Tuned()?"무전 주파수를 맞춰 첫 진술을 수신하십시오.":Expand(call.pages[transcriptPage]);
            ComparisonText(new Rect(175,339,554,270),statement,ref statementScroll);
            ComparisonText(new Rect(806,339,602,331),source.text,ref evidenceScroll);
            if(InterfaceButton(new Rect(174,630,140,43),"이전 진술",transcriptPage>0,ControlTone.Paper,17)) { transcriptPage--; statementScroll=Vector2.zero; }
            Text(344,641,195,36,"수신 "+(transcriptPage+1)+" / "+(LastReceivedPage+1),18,ink);
            if(InterfaceButton(new Rect(564,630,163,43),"다음 진술",transcriptPage<LastReceivedPage,ControlTone.Paper,17)) { transcriptPage++; statementScroll=Vector2.zero; }
            if(Primary(149,748,603,49,"판단 카드로 돌아가기  [Esc]")) comparison=-1;
        }
        void ComparisonText(Rect rect,string body,ref Vector2 scroll)
        {
            small.fontSize=ReadingSize(21); small.normal.textColor=ink; small.wordWrap=true;
            WarmText(body,small.fontSize);
            float width=rect.width-26;
            float height=Mathf.Max(rect.height,small.CalcHeight(new GUIContent(body),width)+12);
            scroll=GUI.BeginScrollView(rect,scroll,new Rect(0,0,width,height));
            GUI.Label(new Rect(0,0,width,height),body,small);
            GUI.EndScrollView();
        }
        string hardwareMessage="";
        public void NoteFinding(int question,int answer)
        {
            var q=story.calls[state.call].findings;
            if(state.phase!=0||question<0||question>=q.Length||answer<0||answer>=q[question].options.Length) return;
            state.SetFinding(state.call,question,answer); Save();
        }
        public bool SendPulse(int color)
        {
            bool sent=Rules.Pulse(state,color);
            if(sent) { hardwareMessage=""; Save(); }
            return sent;
        }
        public bool ConfirmPerson(int door)
        {
            bool match=Rules.ConfirmObservation(state,door);
            hardwareMessage=match?"관측 일치. 동쪽 정비구의 서윤 신원 확인 완료.":"관측 불일치. 잠금은 유지됩니다. 표시와 보고를 다시 비교하십시오.";
            if(match) ResetRoomPresentation(); Save(); return match;
        }
        public void RevokeAuthorization()
        {
            if(state.phase!=2) return;
            state.authorizationRevoked=true;
            state.events.Add("03:34 / 미확인 정문 우선 대기·신원 사전 승인 "+Rules.Exposure(state)+"건 철회. 정문은 수동 잠금으로 고정.");
            hardwareMessage="기존 정문 승인을 철회했습니다. 수동 잠금 유지."; ResetRoomPresentation(); Save();
        }
        void DrawPaintedRoom()
        {
            GUI.DrawTexture(new Rect(0,0,1600,900),roomArt,ScaleMode.StretchToFill);
            Box(0,0,1600,900,new Color(.015f,.055f,.06f,.11f));
            if(prefs.motion) for(int i=0;i<57;i++)
            {
                float x=320+Mathf.Repeat(i*127.1f-elapsed*(19+i%8*3),780);
                float y=117+Mathf.Repeat(i*41.3f+elapsed*(8+i%5),335);
                Line(x,y,x+6,y-2,new Color(.77f,.85f,.84f,.14f),1);
            }
            Box(591,556,132,36,new Color(.025f,.06f,.055f,.9f));
            Text(598,563,124,23,(playing&&state!=null?state.frequency:"83.4")+" MHz",17,teal);
            Disc(1225,397,6,playing&&state!=null&&Rules.Exposure(state)>0?red:teal);
            if(playing&&state!=null)
            {
                Box(78,303,169,142,new Color(.25f,.2f,.13f,.95f));
                Box(87,311,150,122,new Color(.76f,.73f,.6f)); Disc(162,318,4,red);
                Text(98,332,131,34,"관제 상황판",18,ink,FontStyle.Bold);
                int n=0; for(int i=0;i<2;i++) if(state.Finding(state.call,i)>=0) n++;
                Text(98,372,132,49,"교신 "+(state.call+1)+"\n판단 메모 "+n+" / 2",16,ink);
                if(state.Has("firstrescue")) { Text(303,716,168,36,"구조 연결 2명",15,ink); }
                if(screen=="ending"&&state.ending=="voice") { Disc(1310,747,32,new Color(.23f,.27f,.23f)); Disc(1310,744,23,new Color(.035f,.04f,.027f)); }
                if(screen=="ending"&&state.ending=="dawn") Box(0,62,1600,788,new Color(.36f,.57f,.58f,.12f));
            }
            var old=GUI.color; GUI.color=new Color(.7f,.8f,.76f,.025f);
            GUI.DrawTextureWithTexCoords(new Rect(0,62,1600,788),noise,new Rect(0,0,6,3)); GUI.color=old;
            DrawRoomAtmosphere();
        }
        void DrawCasework()
        {
            boardCall=Mathf.Clamp(boardCall,0,state.call);
            if(comparison>=0) { DrawComparison(); return; }
            Frame("관제 상황판 / 교신과 원문을 대조","야간 당직 판단 메모");
            for(int c=0;c<story.calls.Length;c++) if(InterfaceButton(new Rect(147+c*214,201,200,38),"교신 "+(c+1).ToString("00"),c<=state.call,ControlTone.Neutral,17,c==boardCall)) boardCall=c;
            bool locked=boardCall<state.call||state.phase>0;
            var call=story.calls[boardCall];
            Box(141,252,1298,477,new Color(.21f,.17f,.115f)); Outline(141,252,1298,477,new Color(.41f,.34f,.22f),4);
            for(int q=0;q<call.findings.Length;q++)
            {
                var finding=call.findings[q]; float x=151+q*644;
                PaperSurface(x,262,626,456); Box(x+259,256,105,17,new Color(.52f,.52f,.37f,.9f));
                var pencil=new Color(.32f,.33f,.28f);
                Text(x+24,284,330,24,"교신 "+(boardCall+1).ToString("00")+"   /   판단 근거 "+(q+1),15,pencil,FontStyle.Bold);
                if(locked)
                {
                    var stamp=new Color(.47f,.22f,.16f);
                    var matrix=GUI.matrix; Rotate(-3,new Vector2(x+514,299));
                    Outline(x+431,279,173,35,stamp,2);
                    Text(x+443,286,154,24,call.time+"   송신 완료",17,stamp,FontStyle.Bold);
                    GUI.matrix=matrix;
                }
                Text(x+22,318,580,48,finding.question,22,ink,FontStyle.Bold);
                int selected=state.Finding(boardCall,q);
                for(int a=0;a<finding.options.Length;a++)
                    if(PaperOption(x+20,380+a*61,586,53,finding.options[a],selected==a,!locked)) NoteFinding(q,a);
                var d=Array.Find(story.dossiers,z=>z.id==finding.source);
                if(InterfaceButton(new Rect(x+20,locked?566:634,586,46),"진술과 대조  /  "+d.title,true,ControlTone.Paper,17)) CompareEvidence(q);
                if(locked)
                {
                    Text(x+24,621,578,25,"송신 후 원문 대조",15,pencil,FontStyle.Bold);
                    Text(x+24,648,578,65,finding.explanation,ReadingSize(16),ink);
                }
                else Text(x+24,592,579,26,"참조 문서   /   "+(d.category=="map"?"수색 지도":"근무 기록"),15,pencil);
            }
            if(Primary(150,748,625,49,locked?"이 교신의 송신 기록 열기":"무전기로 돌아가기  [R]"))
            {
                OpenPanel(locked?"archive":"radio");
                if(locked) SelectArchiveCall(boardCall);
            }
            int marked=(state.Finding(boardCall,0)>=0?1:0)+(state.Finding(boardCall,1)>=0?1:0);
            Text(804,751,608,50,locked?"송신 당시의 판단을 보관한 기록입니다.":"메모 "+marked+" / 2     무전으로 응답하기 전까지 고칠 수 있습니다.",18,teal);
        }
        string ColorSequence()
        {
            string text=""; foreach(int p in state.pulses) text+=(text==""?"":" → ")+(p==0?"빨강":"초록");
            return text;
        }
        void DrawInteractiveDoor() { DrawDoorConsole(); }

    }
}
