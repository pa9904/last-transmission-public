using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastTransmission
{
    public sealed partial class Game
    {
        readonly Dictionary<string,Vector2> documentPositions=new Dictionary<string,Vector2>();
        readonly Dictionary<(int,FontStyle,Color),GUIStyle> textStyles=new Dictionary<(int,FontStyle,Color),GUIStyle>();
        string lastMap="routes", lastRecord="handover", overlayReturn="", collectedDetail="";
        int archiveCall;
        Vector2 collectionScroll;
        EndingCollection collection;
        bool verificationMode;
        GUIStyle sliderTrack, sliderThumb;
        internal int CollectedCount { get { return collection.unlocked.Count; } }
        internal Vector2 ReadingPosition { get { return docScroll; } set { docScroll=value; } }
        internal string DocumentName { get { return document; } }
        internal int ArchiveCall { get { return archiveCall; } }
        void InitializeQuality(string[] args)
        {
            verificationMode=Array.IndexOf(args,"--verify-player")>=0||Array.IndexOf(args,"--verify-resume")>=0;
            collection=store.Read<EndingCollection>("collection.json")??new EndingCollection();
            if(collection.unlocked==null) collection.unlocked=new List<string>();
            collection.unlocked.RemoveAll(id=>!Array.Exists(story.endings,e=>e.id==id));
            collection.unlocked=new List<string>(new HashSet<string>(collection.unlocked));
            RememberEnding(resume);
        }
        void RememberEnding(Progress progress)
        {
            if(progress==null||progress.phase!=3||!collection.Add(story,progress.ending)) return;
            try { store.Write("collection.json",collection); }
            catch(Exception e) { Toast("엔딩 기록을 저장하지 못했습니다."); Debug.LogError(e); }
        }
        void ResetReadingLocations()
        {
            documentPositions.Clear(); lastMap="routes"; lastRecord="handover"; document="handover";
            docScroll=listScroll=archiveScroll=radioScroll=endingScroll=Vector2.zero; archiveCall=0; mapReading=false;
        }
        void RememberDocumentScroll() { documentPositions[document]=docScroll; }
        int ReadingSize(int size) { return Mathf.RoundToInt(size*prefs.readingScale); }
        void OnApplicationFocus(bool focused)
        {
            if(!focused&&ready&&!verificationMode&&playing&&screen=="play"&&overlay=="")
            { overlay="pause"; Save(); }
        }
        void OnApplicationQuit() { if(!ready) return; if(playing&&state!=null) Save(); SavePrefs(); }
        string NextTask()
        {
            if(state.phase>=2) return "출입 제어기에서\n마지막 신원을 판단하십시오.";
            if(state.phase==1) return "응답 결과를 읽고\n다음 교신을 수신하십시오.";
            var call=story.calls[state.call];
            if(!Tuned()||state.page<call.pages.Length-1) return "무전기의 주파수를 맞추고\n수신 내용을 끝까지 읽으십시오.";
            foreach(var id in call.documents) if(!state.Read(id)) return "대조할 기록\n"+Array.Find(story.dossiers,d=>d.id==id).title;
            for(int q=0;q<call.findings.Length;q++) if(state.Finding(state.call,q)<0) return "상황판에서 두 가지\n판단 근거를 정리하십시오.";
            return "무전기로 돌아가\n응답을 결정하십시오.";
        }
        internal void OpenNextTask()
        {
            if(state.phase>=2) { OpenPanel("door"); return; }
            var call=story.calls[state.call];
            if(state.phase==1||!Tuned()||state.page<call.pages.Length-1) { OpenPanel("radio"); return; }
            foreach(var id in call.documents) if(!state.Read(id))
            { var d=Array.Find(story.dossiers,x=>x.id==id); OpenPanel(d.category=="map"?"map":"records"); OpenDocument(id); return; }
            for(int q=0;q<call.findings.Length;q++) if(state.Finding(state.call,q)<0) { OpenPanel("board"); return; }
            OpenPanel("radio");
        }
        void DrawTaskCard()
        {
            PaperSurface(45,94,293,166); Box(139,88,89,13,new Color(.50f,.49f,.34f,.8f));
            Text(65,112,248,27,"당직 메모 / 교신 "+(state.call+1).ToString("00"),15,ink,FontStyle.Bold);
            Line(65,143,316,143,new Color(.49f,.54f,.43f),1);
            Text(65,154,251,59,NextTask(),16,ink);
            if(InterfaceButton(new Rect(65,222,251,27),"다음 확인  [N]",true,ControlTone.Paper,14)) OpenNextTask();
        }

        void DrawDeviceNavigation() { DrawInstrumentDock(); }

        internal void SelectArchiveCall(int chapter)
        { if(chapter>=0&&chapter<=state.call) { archiveCall=chapter; archiveScroll=Vector2.zero; } }
        void DrawIndexedArchive()
        {
            Frame("관제소 / 교신 보관철","교신 기록과 판단 결과");
            for(int c=0;c<story.calls.Length;c++)
            {
                string name=(c+1).ToString("00")+" / "+(c<=state.call?story.calls[c].time:"미수신");
                if(Btn(145+c*219,202,207,43,(archiveCall==c?"● ":"")+name,c<=state.call)) SelectArchiveCall(c);
            }
            var call=story.calls[archiveCall];
            string text=call.time+" / "+call.title+"\n"+call.caller+" · "+call.frequency+" MHz\n\n";
            int pages=archiveCall<state.call||state.phase>0?call.pages.Length:state.page+1;
            for(int p=0;p<pages;p++) text+="[수신 "+(p+1)+"]\n"+Expand(call.pages[p])+"\n\n";
            if(archiveCall<state.notes.Count) text+="[관제 판단 및 결과]\n"+state.notes[archiveCall]+"\n\n";
            else text+="[관제 판단]\n아직 응답하지 않았습니다.\n";
            var observations=state.events.FindAll(entry=>entry.StartsWith(call.time+" /",StringComparison.Ordinal)||
                (archiveCall==story.calls.Length-1&&entry.StartsWith("03:34 /",StringComparison.Ordinal)));
            if(observations.Count>0) text+="\n[장치 및 관측 기록]\n"+string.Join("\n\n",observations);
            small.fontSize=ReadingSize(21); small.normal.textColor=paper;
            float height=small.CalcHeight(new GUIContent(text),1190)+30;
            WarmText(text,small.fontSize);
            archiveScroll=GUI.BeginScrollView(new Rect(164,269,1265,505),archiveScroll,new Rect(0,0,1190,height));
            GUI.Label(new Rect(0,0,1190,height),text,small); GUI.EndScrollView();
        }
        internal void ShowSettings() { overlayReturn=overlay=="pause"?"pause":""; overlay="settings"; }
        void ShowCredits() { overlayReturn=overlay=="pause"?"pause":""; overlay="credits"; }
        internal void ShowCollection() { overlayReturn=overlay=="pause"?"pause":""; collectedDetail=""; collectionScroll=Vector2.zero; overlay="collection"; }
        internal void CloseQualityOverlay()
        {
            if(overlay=="collection"&&collectedDetail!="") { collectedDetail=""; collectionScroll=Vector2.zero; return; }
            if(overlay=="settings") SavePrefs(); overlay=overlayReturn; overlayReturn="";
        }
        float QualitySlider(float x,float y,float width,float value,float min,float max)
        {
            if(sliderTrack==null)
            {
                sliderTrack=new GUIStyle { fixedHeight=32, stretchWidth=true };
                sliderThumb=new GUIStyle(GUI.skin.horizontalSliderThumb); sliderThumb.fixedWidth=20; sliderThumb.fixedHeight=30;
                sliderThumb.normal.background=sliderThumb.hover.background=sliderThumb.active.background=Texture2D.whiteTexture;
                sliderThumb.margin=new RectOffset(0,0,0,0);
            }
            Box(x,y+12,width,8,new Color(.16f,.28f,.28f)); Box(x,y+12,width*Mathf.InverseLerp(min,max,value),8,new Color(.31f,.59f,.54f));
            var old=GUI.backgroundColor; GUI.backgroundColor=teal;
            float result=GUI.HorizontalSlider(new Rect(x,y,width,35),value,min,max,sliderTrack,sliderThumb);
            GUI.backgroundColor=old; return result;
        }
        void DrawQualitySettings()
        {
            MenuBackdrop(); DeviceSurface(177,233,580,476); DeviceSurface(837,233,580,476);
            Text(200,103,1200,65,"설정",42,paper); Text(200,183,1200,34,"읽기 속도에 맞춰 조정하세요. 돌아가면 설정을 보관합니다.",20,teal);
            Line(800,250,800,685,new Color(.2f,.35f,.33f),1);
            Text(200,250,530,35,"음향",25,teal); Text(860,250,530,35,"읽기 · 화면",25,teal);
            Text(200,313,530,35,"전체 음량  "+Mathf.RoundToInt(prefs.master*100)+"%",23,paper);
            prefs.master=QualitySlider(200,357,530,prefs.master,0,1);
            Text(200,416,530,35,"효과음  "+Mathf.RoundToInt(prefs.effects*100)+"%",23,paper);
            prefs.effects=QualitySlider(200,460,530,prefs.effects,0,1);
            Text(200,519,530,35,"바람 · 무전 잡음  "+Mathf.RoundToInt(prefs.ambience*100)+"%",23,paper);
            prefs.ambience=QualitySlider(200,563,530,prefs.ambience,0,1);
            Text(200,610,530,35,"배경 음악  "+Mathf.RoundToInt(prefs.music*100)+"%",23,paper); prefs.music=QualitySlider(200,654,530,prefs.music,0,1);
            Text(860,313,530,35,"텍스트 속도  "+Mathf.RoundToInt(prefs.textSpeed)+"자/초",23,paper);
            prefs.textSpeed=QualitySlider(860,357,530,prefs.textSpeed,15,100);
            Text(860,416,530,35,"본문 글자 크기  "+Mathf.RoundToInt(prefs.readingScale*100)+"%",23,paper);
            prefs.readingScale=QualitySlider(860,460,530,prefs.readingScale,1,1.3f);
            if(Btn(860,514,530,48,"교신 즉시 표시: "+(prefs.instantText?"켜짐":"꺼짐"))) prefs.instantText=!prefs.instantText;
            if(Btn(860,575,530,48,"눈보라 움직임: "+(prefs.motion?"켜짐":"꺼짐"))) prefs.motion=!prefs.motion;
            if(Btn(860,636,530,48,"화면 모드: "+(Screen.fullScreen?"전체 화면":"창 모드"))) { prefs.fullscreen=!Screen.fullScreen; Screen.fullScreen=prefs.fullscreen; }
            if(Primary(200,751,1190,58,"설정 저장 후 돌아가기")) CloseQualityOverlay();
        }
        internal bool SelectCollectedEnding(int index)
        {
            if(index<0||index>=story.endings.Length||!collection.unlocked.Contains(story.endings[index].id)) return false;
            collectedDetail=story.endings[index].id; collectionScroll=Vector2.zero; return true;
        }
        void DrawCollection()
        {
            MenuBackdrop(true);
            Line(150,235,1440,235,edge,1);
            if(collectedDetail!="")
            {
                var end=Array.Find(story.endings,e=>e.id==collectedDetail);
                PaperSurface(150,247,1290,466);
                Text(177,269,1220,65,end.title,34,ink); Text(177,333,1220,35,end.subtitle,20,new Color(.30f,.43f,.33f));
                small.fontSize=ReadingSize(23); small.normal.textColor=ink;
                float height=small.CalcHeight(new GUIContent(end.text),1210)+20;
                WarmText(end.text,small.fontSize);
                collectionScroll=GUI.BeginScrollView(new Rect(177,399,1236,295),collectionScroll,new Rect(0,0,1210,height));
                GUI.Label(new Rect(0,0,1210,height),end.text,small); GUI.EndScrollView();
            }
            else for(int i=0;i<story.endings.Length;i++)
            {
                float x=150+i*440; var end=story.endings[i]; bool unlocked=collection.unlocked.Contains(end.id);
                PaperSurface(x,278,410,398); Box(x+26,306,67,5,unlocked?new Color(.26f,.44f,.32f):new Color(.51f,.55f,.45f));
                Text(x+26,331,350,35,"근무 기록 "+(i+1).ToString("00"),16,new Color(.30f,.42f,.33f));
                Text(x+26,391,350,80,unlocked?end.title:"미확인 결말",29,ink,FontStyle.Bold);
                Text(x+26,487,350,80,unlocked?end.subtitle:"다른 판단이 남긴 기록은\n그 근무를 마친 뒤 열립니다.",19,new Color(.30f,.42f,.33f));
                if(InterfaceButton(new Rect(x+26,586,358,56),unlocked?"기록 다시 읽기":"아직 기록되지 않음",unlocked,ControlTone.Paper,19)) SelectCollectedEnding(i);
            }
            Text(150,106,1290,60,"엔딩 기록",42,paper); Text(150,183,1290,40,"기록된 결말 "+collection.unlocked.Count+" / "+story.endings.Length+" · 새 근무를 시작해도 보관됩니다.",21,teal);
            if(Primary(150,751,1290,58,collectedDetail!=""?"결말 목록으로":"돌아가기")) CloseQualityOverlay();
        }
    }
}
