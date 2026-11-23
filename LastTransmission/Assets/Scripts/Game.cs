using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace LastTransmission
{
    public sealed partial class Game : MonoBehaviour
    {
        Story story; Progress state, resume; Preferences prefs; SaveStore store;
        Font font, headingFont; Texture2D noise, circle, shade; GUIStyle label, button, small;
        string screen = "title", panel = "", document = "handover", overlay = "", notice = "";
        string confirm = ""; Vector2 docScroll, listScroll, archiveScroll, radioScroll, endingScroll;
        float visible, elapsed, toastUntil;
        bool ready, playing; AudioSource wind, effects, staticSource;
        AudioClip knock;
        float feedbackGain = 1;
        internal AudioSource FeedbackSource { get { return effects; } }
        internal int EffectPlaybackCount { get; private set; }
        readonly Color teal = new Color(.53f,.82f,.77f), ink = new Color(.07f,.13f,.14f), paper = new Color(.76f,.76f,.66f), red = new Color(.81f,.32f,.27f);
        public Progress Current { get { return state; } }
        public Story Content { get { return story; } }
        public Preferences Settings { get { return prefs; } }
        public string ScreenName { get { return screen; } }
        public string PanelName { get { return panel; } }
        public int FrameCount { get; private set; }

        void Awake()
        {
            Application.targetFrameRate = 60;
            story = JsonUtility.FromJson<Story>(Resources.Load<TextAsset>("story").text);
            var args=Environment.GetCommandLineArgs();
            bool verification=Array.IndexOf(args,"--verify-player")>=0;
            string saveRoot=verification?Path.Combine(Application.persistentDataPath,"Verification"):Application.persistentDataPath;
            for(int i=0;i<args.Length-1;i++) if(args[i]=="--profile-dir") saveRoot=args[i+1];
            store = new SaveStore(saveRoot);
            prefs = store.Read<Preferences>("settings.json") ?? new Preferences();
            prefs.Normalize();
            resume = store.ReadProgress(story);
            font = Resources.Load<Font>("Fonts/SUIT-Regular");
            headingFont = Resources.Load<Font>("Fonts/SUIT-Bold");
            InitializeQuality(args);
            InitializeAtmosphere();
            if(prefs.fullscreen) Screen.fullScreen=true;
            MakeTextures(); MakeAudio(); ready = true;
            roomArt=Resources.Load<Texture2D>("Art/ControlRoom-v2");
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--verify-player") >= 0) StartCoroutine(PlayerVerification());
            if (Array.IndexOf(args, "--verify-resume") >= 0) StartCoroutine(ResumeVerification());
        }
        void Update()
        {
            FrameCount++; elapsed += Time.unscaledDeltaTime;
            if(playing&&screen=="play"&&overlay=="") state.playedSeconds+=Time.unscaledDeltaTime;
            if (overlay == "" && confirm == "" && screen == "play" && panel == "radio" && state.phase<2 && Tuned())
                visible += Time.unscaledDeltaTime * prefs.textSpeed;
            UpdateReceiverNeedle(Time.unscaledDeltaTime);
            AudioListener.volume = prefs.master;
            effects.volume = prefs.effects * feedbackGain;
            var mood=CurrentRoomMood;
            wind.volume = (playing && overlay == "" ? mood.wind : .055f) * prefs.ambience;
            staticSource.volume = playing && overlay == "" ? (panel=="radio" ? (Tuned()?mood.radio:.1f) : VisibleRoom&&state.call>=4?mood.radio*.22f:0)*prefs.ambience : 0;
            UpdateRoomAtmosphere(Time.unscaledDeltaTime); UpdateScore(Time.unscaledDeltaTime);
        }
        void MakeTextures()
        {
            noise = new Texture2D(256,256,TextureFormat.RGBA32,false); var rng = new System.Random(219);
            var pixels = new Color32[256*256];
            for (int i = 0; i < pixels.Length; i++) { byte n = (byte)rng.Next(0,255); pixels[i] = new Color32(n,n,n,255); }
            noise.SetPixels32(pixels); noise.Apply(); noise.wrapMode = TextureWrapMode.Repeat;
            circle = new Texture2D(128,128,TextureFormat.RGBA32,false); pixels = new Color32[128*128];
            for (int y=0;y<128;y++) for(int x=0;x<128;x++) { float d=Vector2.Distance(new Vector2(x,y),new Vector2(63.5f,63.5f)); pixels[y*128+x]=new Color(1,1,1,Mathf.Clamp01(64-d)); }
            circle.SetPixels32(pixels); circle.Apply();
            shade=new Texture2D(256,1,TextureFormat.RGBA32,false);
            for(int x=0;x<256;x++) shade.SetPixel(x,0,new Color(.015f,.03f,.034f,.98f*(1-x/255f)));
            shade.Apply(); shade.wrapMode=TextureWrapMode.Clamp;
        }
        AudioClip NoiseClip(string title, int seconds, bool low)
        {
            int rate=22050; var data=new float[rate*seconds]; var rng=new System.Random(48); float f=0;
            for(int i=0;i<data.Length;i++) { float n=(float)rng.NextDouble()*2-1; f=Mathf.Lerp(f,n,low?.018f:.43f); float phase=(float)i/rate; data[i]=f*(low ? .65f+.25f*Mathf.Sin(phase*1.1f) : .22f); }
            var c=AudioClip.Create(title,data.Length,1,rate,false); c.SetData(data,0); return c;
        }
        void MakeAudio()
        {
            effects=gameObject.AddComponent<AudioSource>(); wind=gameObject.AddComponent<AudioSource>(); staticSource=gameObject.AddComponent<AudioSource>();
            wind.clip=NoiseClip("Wind through the aerial",8,true); wind.loop=true; wind.Play();
            staticSource.clip=NoiseClip("Radio carrier",3,false); staticSource.loop=true; staticSource.Play();

            int rate=22050; var data=new float[rate];
            for(int i=0;i<data.Length;i++) { float t=(float)i/rate; data[i]=.45f*Mathf.Sin(t*2*Mathf.PI*89)*Mathf.Exp(-t*24) + .22f*Mathf.Sin(t*2*Mathf.PI*61)*Mathf.Exp(-Mathf.Max(0,t-.38f)*30)*(t>.38f?1:0); }
            knock=AudioClip.Create("Two knocks at the main door",rate,1,rate,false); knock.SetData(data,0);
            MakeRoomAudio(); StartScore();
        }
        void Play(AudioClip clip, float volume=.35f)
        {
            if(clip==null) return;
            // One foreground effect at a time; never accumulate one-shot voices.
            effects.Stop(); effects.clip=clip; feedbackGain=volume;
            effects.volume=prefs.effects*feedbackGain; effects.Play(); EffectPlaybackCount++;
        }
        bool Tuned() { return state!=null && state.frequency==story.calls[state.call].frequency; }
        void Save()
        {
            try { store.Write("progress.json",state); resume=store.ReadProgress(story); }
            catch(Exception e) { Toast("저장 실패. 폴더 쓰기 권한을 확인해 주세요."); Debug.LogError(e); }
        }
        void SavePrefs() { try { prefs.Normalize(); store.Write("settings.json",prefs); } catch(Exception e) { Toast("설정을 저장하지 못했습니다."); Debug.LogError(e); } }
        void Toast(string text) { notice=text; toastUntil=elapsed+5; }
        public void NewGame()
        {
            echoNeedleSeen=false;
            echoNeedleHold=0;
            state=new Progress(); playing=true; screen="play"; panel="intro"; overlay=""; hardwareMessage=""; boardCall=0; visible=0; ResetRoomPresentation(true); Save();
            ResetReadingLocations();
        }
        public void Continue()
        {
            state=store.ReadProgress(story);
            hardwareMessage="";
            if(state==null) { Toast("유효한 저장 기록이 없습니다."); return; }
            echoNeedleSeen=false;
            echoNeedleHold=0;
            playing=true; screen=state.phase==3?"ending":"play"; panel=state.phase==2?"door":"radio"; overlay=""; visible=0;
            ResetRoomPresentation(true);
            ResetReadingLocations(); archiveCall=state.call; RememberEnding(state);
        }
        public void OpenPanel(string name)
        {
            if(panel==name) { if(name=="board") comparison=-1; return; }
            comparison=-1;
            RememberDocumentScroll(); panel=name; listScroll=Vector2.zero;
            if(name=="board") boardCall=state.call;
            if(name=="archive") { archiveCall=state.call; archiveScroll=Vector2.zero; }
            if(name=="map") mapReading=false;
            if(name=="records") OpenDocument(lastRecord);
            if(name=="map") OpenDocument(lastMap);
        }
        public void OpenDocument(string id)
        {
            var d=Array.Find(story.dossiers,x=>x.id==id);
            if(d==null||state.call<d.unlock) return;
            RememberDocumentScroll();
            document=id; docScroll=documentPositions.ContainsKey(id)?documentPositions[id]:Vector2.zero;
            if(d.category=="map") lastMap=id; else lastRecord=id;
            if(!state.Read(id)) { state.read.Add(id); Save(); }
        }
        public void Respond(int index)
        {
            Rules.Choose(story,state,index); ResetRoomPresentation(); visible=0; radioScroll=Vector2.zero; Save();
        }
        public void NextCall()
        {
            Rules.Advance(story,state); ResetRoomPresentation(); visible=0; radioScroll=Vector2.zero; Save(); panel=state.phase==2?"door":"";
        }
        public void DecideDoor(int choice)
        {
            Rules.Door(state,choice); ResetRoomPresentation(); Save(); RememberEnding(state); screen="ending"; panel=""; endingScroll=Vector2.zero;
        }
        public void RevealAll() { if(state!=null&&state.phase<2) visible=CurrentPage().Length; }
        public void PersistSettings() { SavePrefs(); }
        public void PersistProgress() { Save(); }
        public float LargestRadioTextHeight()
        {
            if(small==null) return 0; small.fontSize=ReadingSize(23); float max=0;
            foreach(var c in story.calls) foreach(var text in c.pages) max=Mathf.Max(max,small.CalcHeight(new GUIContent(Expand(text)),RadioTextWidth));
            return max;
        }
        Rect viewport;
        bool NarrowWindow => Screen.width < Screen.height*1.12f;
        float DockTop => viewport.yMax-(NarrowWindow?235:79);
        Rect DeviceBounds()
        {
            float top=viewport.yMin+(NarrowWindow?115:83);
            float bottom=panel=="intro"?viewport.yMax-22:DockTop-15;
            float scale=Mathf.Min(1.14f,(viewport.width-72)/1372f,(bottom-top)/735f);
            float width=1372*scale,height=735*scale;
            return new Rect((1600-width)/2,(top+bottom-height)/2,width,height);
        }
        RenderTexture roomSurround;
        void OnDestroy() { if(roomSurround!=null) { roomSurround.Release(); Destroy(roomSurround); } }
        void OnGUI()
        {
            if(!ready) return;
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            if(scale<=0) return;
            float width=Screen.width/scale,height=Screen.height/scale;
            viewport=new Rect((1600-width)*.5f,(900-height)*.5f,width,height);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)/2,(Screen.height-900*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
            if(Event.current.type==EventType.Repaint && roomArt!=null && (width>1601||height>901))
            {
                if(roomSurround==null) roomSurround=new RenderTexture(24,14,0){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                if(!roomSurround.IsCreated()) { var target=RenderTexture.active; Graphics.Blit(roomArt,roomSurround); RenderTexture.active=target; }
                var tint=GUI.color; GUI.color=new Color(.46f,.51f,.5f);
                GUI.DrawTexture(viewport,roomSurround,ScaleMode.ScaleAndCrop); GUI.color=tint;
            }
            if(label==null) InitStyles();
            Keyboard(); GUI.enabled=overlay==""&&confirm=="";
            // Opaque menus replace the scene UI instead of rendering hidden text.
            if(overlay=="")
            {
                DrawRoom();
                if(screen=="title") DrawTitle();
                else if(screen=="ending") DrawEnding();
                else
                {
                    DrawStatus();
                    if(panel=="") { DrawHotspots(); DrawRoomObservation(); }
                    else
                    {
                        var canvas=GUI.matrix;
                        if(!NarrowWindow||(panel!="map"&&panel!="radio"))
                        {
                            var bounds=DeviceBounds();
                            float factor=bounds.width/1372f;
                            float maskTop=viewport.yMin+(NarrowWindow?99:67);
                            Box(viewport.xMin,maskTop,viewport.width,(panel=="intro"?viewport.yMax:DockTop)-maskTop,new Color(.006f,.015f,.016f,.50f));
                            GUI.matrix=canvas*Matrix4x4.TRS(new Vector3(bounds.x-108*factor,bounds.y-80*factor,0),Quaternion.identity,new Vector3(factor,factor,1));
                        }
                        if(panel=="intro") DrawIntro();
                        else if(panel=="radio") DrawRadio();
                        else if(panel=="map"||panel=="records") DrawDocuments();
                        else if(panel=="door") DrawDoor();
                        else if(panel=="archive") DrawArchive();
                        else if(panel=="board") DrawCasework();
                        GUI.matrix=canvas;
                    }
                    if(panel!="intro") DrawDeviceNavigation();
                }
            }
            GUI.enabled=confirm=="";
            if(overlay!="") DrawOverlay();
            GUI.enabled=true;
            if(confirm!="") DrawConfirmation();
            if(elapsed<toastUntil) { Box(340,viewport.yMax-130,920,46,new Color(.04f,.1f,.11f,.97f)); Text(365,viewport.yMax-120,870,30,notice,18,teal); }
        }
        void InitStyles()
        {
            label=new GUIStyle(GUI.skin.label) { font=font, wordWrap=true, richText=false, padding=new RectOffset(0,0,0,0) };
            small=new GUIStyle(label);
            InitializeInterface();
            button=new GUIStyle(GUI.skin.button) { font=font, fontSize=20, wordWrap=true, richText=false, alignment=TextAnchor.MiddleLeft, padding=new RectOffset(18,14,8,8), border=new RectOffset(0,0,0,0) };
            button.normal.background=Texture2D.whiteTexture; button.hover.background=Texture2D.whiteTexture; button.active.background=Texture2D.whiteTexture;
            button.normal.textColor=teal; button.hover.textColor=Color.white; button.active.textColor=Color.white;
        }
        void Keyboard()
        {
            var e=Event.current;
            if(e.type!=EventType.KeyDown) return;
            if(confirm!="") { if(e.keyCode==KeyCode.Escape) { confirm=""; e.Use(); } return; }
            if(e.keyCode==KeyCode.Escape)
            {
                if(overlay=="pause") overlay="";
                else if(overlay!="") CloseQualityOverlay();
                else if(screen=="play" && panel=="map"&&mapReading) ReturnToMap();
                else if(screen=="play"&&panel=="board"&&comparison>=0) comparison=-1;
                else if(screen=="play" && panel!="") panel="";
                else if(screen=="play") overlay="pause";
                e.Use(); return;
            }
            if(overlay!=""||screen!="play"||panel=="intro") return;
            bool handled=true;
            if(e.keyCode==KeyCode.R) OpenPanel("radio");
            else if(e.keyCode==KeyCode.M) { if(panel=="map"&&mapReading) ReturnToMap(); else OpenPanel("map"); }
            else if(e.keyCode==KeyCode.L) OpenPanel("records");
            else if(e.keyCode==KeyCode.D) OpenPanel("door");
            else if(e.keyCode==KeyCode.H) OpenPanel("archive");
            else if(e.keyCode==KeyCode.B) OpenPanel("board");
            else if(e.keyCode==KeyCode.N) OpenNextTask();
            else if(e.keyCode==KeyCode.Space && panel=="radio") RevealOrNext();
            else handled=false;
            if(handled) e.Use();
        }
        void Box(float x,float y,float w,float h,Color c) { var old=GUI.color; GUI.color=c; GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture); GUI.color=old; }
        void Disc(float x,float y,float r,Color c) { var old=GUI.color; GUI.color=c; GUI.DrawTexture(new Rect(x-r,y-r,r*2,r*2),circle); GUI.color=old; }
        void Line(float x,float y,float tx,float ty,Color c,float width=2)
        {
            var m=GUI.matrix; var d=new Vector2(tx-x,ty-y);
            GUI.matrix=m*Matrix4x4.TRS(new Vector3(x,y,0),Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg),Vector3.one);
            Box(0,0,d.magnitude,width,c); GUI.matrix=m;
        }
        void Rotate(float degrees,Vector2 pivot)
        {
            GUI.matrix=GUI.matrix*Matrix4x4.Translate(new Vector3(pivot.x,pivot.y,0))*Matrix4x4.Rotate(Quaternion.Euler(0,0,degrees))*Matrix4x4.Translate(new Vector3(-pivot.x,-pivot.y,0));
        }
        void Text(float x,float y,float w,float h,string text,int size,Color c,FontStyle style=FontStyle.Normal)
        {
            WarmText(text,size,style);
            var key=(size,style,c);
            if(!textStyles.TryGetValue(key,out var textStyle))
            {
                textStyle=new GUIStyle(label) { font=style==FontStyle.Bold?headingFont:font, fontSize=size,fontStyle=style==FontStyle.Bold?FontStyle.Normal:style,alignment=TextAnchor.UpperLeft };
                textStyle.normal.textColor=c; textStyles.Add(key,textStyle);
            }
            GUI.Label(new Rect(x,y,w,h),text,textStyle);
        }
        void WarmText(string text,int size,FontStyle style=FontStyle.Normal)
        {
            // Populate dynamic glyphs during layout, before any text is drawn.
            if(Event.current.type==EventType.Layout)
                (style==FontStyle.Bold?headingFont:font).RequestCharactersInTexture(text+"0123456789%.",size,style==FontStyle.Bold?FontStyle.Normal:style);
        }
        bool Btn(float x,float y,float w,float h,string text,bool enabled=true,bool warning=false)
        { return InterfaceButton(new Rect(x,y,w,h),text,enabled,warning?ControlTone.Danger:ControlTone.Neutral,button.fontSize); }

        bool Hit(float x,float y,float w,float h,string caption,string sub)
        {
            Rect r=new Rect(x,y,w,h); bool hover=r.Contains(Event.current.mousePosition);
            if(hover)
            {
                float tagWidth=Mathf.Max(212,Mathf.Min(320,w));
                Outline(x,y,w,h,new Color(.53f,.82f,.77f,.35f));
                Box(x,y+h-59,tagWidth,59,new Color(.025f,.045f,.043f,.97f)); Box(x,y+h-59,3,59,amber);
                Text(x+14,y+h-49,tagWidth-25,28,caption,21,ivory);
                Text(x+14,y+h-23,tagWidth-25,23,sub,13,muted);
            }
            return GUI.Button(r,GUIContent.none,GUIStyle.none);
        }
        void Frame(string eyebrow,string title) { DrawInstrumentFrame(eyebrow,title); }

        void DrawStatus() { DrawInstrumentStatus(); }

        void DrawRoom()
        {
            if(roomArt!=null) { DrawPaintedRoom(); return; }
            Box(0,0,1600,900,new Color(.035f,.065f,.075f));
            for(int i=0;i<12;i++) Box(0,80+i*40,1600,40,new Color(.045f-i*.0015f,.075f-i*.002f,.082f-i*.002f));
            // Frosted mountain window, drawn as a room fixture rather than a screen.
            Box(302,94,805,390,new Color(.018f,.03f,.035f)); Box(318,110,773,357,new Color(.08f,.16f,.19f));
            for(int x=0;x<773;x+=2)
            {
                float distant=116+Mathf.PerlinNoise(x*.0045f,7.2f)*115;
                float near=203+Mathf.PerlinNoise(x*.007f,19.2f)*120;
                Box(318+x,110+distant,2,357-distant,new Color(.19f,.29f,.31f));
                Box(318+x,110+near,2,357-near,new Color(.075f,.14f,.16f));
            }
            for(int i=0;i<95;i++)
            {
                float t=prefs!=null&&prefs.motion?elapsed:0;
                float x=318+Mathf.Repeat(i*83.7f-t*(28+i%7*7),755); float y=113+Mathf.Repeat(i*47.1f+t*(9+i%4),348);
                Line(x,y,x+5+i%8,y-2,new Color(.69f,.82f,.82f,.17f),1);
            }
            Box(698,104,13,369,new Color(.025f,.045f,.05f)); Box(310,281,786,10,new Color(.025f,.045f,.05f));
            for(int i=0;i<8;i++) Box(320+i*98,454,88,10,new Color(.46f,.61f,.61f,.22f));
            // Clock, emergency shelf, and a solid door with a wired access controller.
            Box(70,143,170,94,new Color(.016f,.027f,.029f)); Text(87,163,145,45,playing&&state!=null?story.calls[state.call].time:"02:41",37,teal);
            Text(82,210,150,20,"NORTH / STATION 04",11,paper);
            Box(60,295,185,13,new Color(.17f,.22f,.22f)); Box(82,264,30,30,new Color(.31f,.38f,.34f)); Box(127,250,21,44,new Color(.3f,.23f,.19f));
            Box(1245,117,283,464,new Color(.018f,.028f,.031f)); Box(1260,130,253,440,new Color(.1f,.15f,.16f));
            Box(1275,148,224,216,new Color(.075f,.12f,.13f)); Line(1275,373,1500,373,new Color(.19f,.25f,.24f));
            Box(1280,410,14,58,new Color(.43f,.46f,.4f)); Box(1190,380,74,119,new Color(.2f,.27f,.25f));
            Disc(1227,405,6,playing&&state!=null&&state.call>=4?red:teal); Text(1203,424,56,65,"출입\n잠금",14,paper);
            Box(1323,84,135,27,new Color(.2f,.1f,.08f)); Text(1342,90,110,24,"출입 제한",14,new Color(.8f,.52f,.39f));
            // Desk edge, lamp, papers, receiver, logbook and cabling.
            Box(0,542,1600,358,new Color(.08f,.1f,.095f));
            for(int i=0;i<28;i++) Line(0,553+i*12,1600,548+i*12,new Color(.17f,.2f,.17f,.25f),i%3==0?2:1);
            Box(0,808,1600,92,new Color(.035f,.052f,.05f)); Box(0,806,1600,5,new Color(.27f,.3f,.24f));
            Line(381,554,420,325,new Color(.19f,.25f,.23f),13); Line(420,325,542,282,new Color(.19f,.25f,.23f),13);
            Disc(375,554,49,new Color(.1f,.15f,.14f)); Disc(543,282,37,new Color(.17f,.24f,.21f));
            for(int i=0;i<6;i++) Disc(540,540,175-i*16,new Color(.64f,.68f,.39f,.012f));
            var matrix=GUI.matrix; Rotate(-6,new Vector2(290,661));
            Box(103,577,337,202,new Color(.62f,.65f,.54f)); Box(116,588,310,176,new Color(.69f,.71f,.6f));
            for(int i=0;i<9;i++) Line(120,600+i*17,421,600+i*17,new Color(.33f,.42f,.36f,.35f),1);
            Line(143,732,234,656,ink); Line(234,656,376,711,ink); Line(234,656,296,608,red); Line(296,608,387,634,ink);
            Disc(234,656,7,red); Text(133,608,175,25,"북부 수색 지도",18,ink); GUI.matrix=matrix;
            Box(547,557,460,222,new Color(.02f,.035f,.036f)); Box(558,546,441,217,new Color(.21f,.28f,.27f));
            Box(578,566,286,83,new Color(.027f,.075f,.071f)); Text(598,582,250,42,(playing&&state!=null?state.frequency:"83.4")+" MHz",34,teal);
            for(int i=0;i<8;i++) Box(582,675+i*8,208,3,new Color(.075f,.12f,.12f));
            Disc(906,599,34,new Color(.055f,.09f,.091f)); Disc(906,599,23,new Color(.24f,.31f,.28f)); Line(906,599,919,580,teal,3);
            Disc(913,704,23,new Color(.055f,.09f,.091f)); Text(810,681,77,40,"송수신",14,paper);
            Box(519,566,26,146,new Color(.025f,.045f,.046f)); for(int i=0;i<16;i++) Disc(540+i*4,766+Mathf.Sin(i)*4,4,new Color(.025f,.035f,.035f));
            matrix=GUI.matrix; Rotate(5,new Vector2(1170,661));
            Box(1060,564,246,214,new Color(.17f,.21f,.18f)); Box(1073,569,230,204,paper); Box(1080,569,9,204,new Color(.35f,.37f,.29f));
            Text(1110,597,178,60,"야간 근무\n인수인계 기록",24,ink); Line(1110,674,1266,674,ink,1); Text(1110,695,175,45,"11 / 17\n당직자 서명 ______",13,ink); GUI.matrix=matrix;
            Disc(1414,709,45,new Color(.025f,.04f,.039f)); Disc(1414,704,36,new Color(.33f,.38f,.32f)); Disc(1414,704,26,new Color(.065f,.075f,.055f));
            // Fine grain and restrained alarm change; no flashing.
            var old=GUI.color; GUI.color=new Color(.6f,.74f,.69f,.035f); GUI.DrawTextureWithTexCoords(new Rect(0,62,1600,788),noise,new Rect(0,0,6,3)); GUI.color=old;
            if(playing&&state!=null&&state.call>=3) Box(1120,62,480,480,new Color(.35f,.04f,.025f,.07f));
        }
        void DrawHotspots()
        {
            DrawTaskCard();
            if(roomArt!=null)
            {
                if(Hit(80,630,405,153,"수색 지도","지형 · 통제 구역  [M]")) OpenPanel("map");
                if(Hit(530,510,475,270,"무전기","교신 "+(state.call+1)+" · 수신 대기  [R]")) OpenPanel("radio");
                if(Hit(1035,635,363,145,"근무 기록","인원 · 암호 · 정비  [L]")) OpenPanel("records");
                if(Hit(1179,370,86,130,"출입 제어","잠금 · 확인등  [D]")) OpenPanel("door");
                if(Hit(78,303,169,142,"상황판","증거를 대조한다  [B]")) OpenPanel("board");
                return;
            }
            if(Hit(100,575,341,204,"수색 지도","지형 · 통제 구역  [M]")) OpenPanel("map");
            if(Hit(544,545,458,237,"무전기","수신 대기  /  교신 "+(state.call+1)+"  [R]")) OpenPanel("radio");
            if(Hit(1066,565,242,211,"근무 기록","인원 · 암호 · 정비 내역  [L]")) OpenPanel("records");
            if(Hit(1184,378,87,130,"출입 제어","잠금 상태  [D]")) OpenPanel("door");
        }
        void DrawTitle()
        {
            GUI.DrawTexture(new Rect(0,0,1080,900),shade);
            Text(96,182,650,32,"북부 산악구조 관제소",20,teal,FontStyle.Bold);
            Text(89,220,820,115,"마지막 교신",96,ivory,FontStyle.Bold);
            Text(96,328,690,40,"LAST TRANSMISSION",26,teal);
            Line(96,395,496,395,new Color(.4f,.6f,.55f),1);
            Text(96,426,515,90,"새벽 3시 이후에는\n구조대원의 목소리를 믿지 말 것.",26,new Color(.83f,.83f,.74f));
            if(InterfaceButton(new Rect(96,561,400,54),"새 근무 시작",true,resume==null?ControlTone.Primary:ControlTone.Neutral,22)) { if(resume!=null) confirm="new"; else NewGame(); }
            if(InterfaceButton(new Rect(96,628,400,54),"이어서 근무",resume!=null,resume!=null?ControlTone.Primary:ControlTone.Neutral,22)) Continue();
            if(Btn(96,704,123,44,"설정")) ShowSettings();
            if(Btn(233,704,126,44,"엔딩 기록")) ShowCollection();
            if(Btn(373,704,123,44,"크레딧")) ShowCredits();
            if(Btn(96,763,400,42,"게임 종료")) Application.Quit();
            Text(96,839,900,30,"헤드폰 권장  ·  v0.12.0",16,new Color(.65f,.74f,.70f));
        }
        void DrawIntro()
        {
            Frame("야간 근무 / 인수인계","당신의 판단이 문을 연다");
            Text(170,215,1190,230,story.intro,24,paper);
            Text(170,464,1180,160,"1. 무전기의 수신 주파수를 맞추고 교신을 읽습니다.\n2. 지도와 근무 기록을 대조하고 상황판 [B]에 판단 근거를 남깁니다.\n3. 응답 뒤에는 결과와 기록된 근거를 다시 비교할 수 있습니다.\n4. 마지막 확인등은 직접 조작합니다. 표시한 순서와 상대의 보고를 비교하십시오.",22,teal);
            Text(170,665,1150,50,"대기 시간 제한은 없습니다. Space로 문장을 즉시 표시합니다. 진행은 자동 저장됩니다.",19,paper);
            if(Btn(170,737,1190,48,"관제실로 들어간다")) panel="";
        }
        string Expand(string value)
        {
            return value.Replace("{echo}",state.echo)
                .Replace("{first}",state.Has("firstrescue")?"지훈: 구조 차량에 두 분 모두 있습니다. 언니 발목은 고정했고, 동생 손전등도 켜졌습니다. 남쪽 임도를 열어 주셔서 감사합니다.":"지훈: 능선 대피소에서 추가 지원을 기다립니다. 두 사람 모두 체온이 떨어지고 있습니다. 차량까지 내려가지 못했습니다.")
                .Replace("{tech}",state.Has("technician")?"서윤: 발전기 우회로를 열어 주셔서 고맙습니다. 지금 정비구 앞입니다. 약속한 불빛을 확인할 수 있어요.":"83.4 MHz는 조용하다. 발전기에서 마지막으로 들렸던 서윤의 기침도 사라졌다. 정비구의 확인등은 전원이 없다.");
        }
        string CurrentPage()
        {
            if(state.phase==1) return Expand(story.calls[state.call].choices[state.decisions[state.call]].result);
            return Expand(story.calls[state.call].pages[state.page]);
        }
        internal void RevealOrNext()
        {
            if(state.phase>=2||!Tuned()) return;
            string text=CurrentPage(); if(!prefs.instantText&&visible<text.Length) { visible=text.Length; return; }
            if(state.phase==0&&state.page<story.calls[state.call].pages.Length-1) { state.page++; visible=0; radioScroll=Vector2.zero; Save(); }
        }
        void DrawRadio() { DrawReceiver(); }

        void DrawDocuments()
        { if(panel=="map") DrawRescueMap(); else DrawRecordFolder(); }

        void DrawDoor()
        {
            DrawInteractiveDoor();
        }
        void DrawArchive()
        {
            DrawIndexedArchive();
        }
        void DrawEnding()
        {
            var end=Array.Find(story.endings,e=>e.id==state.ending);
            Box(0,0,1600,900,new Color(.02f,.04f,.045f,.96f)); PaperSurface(125,95,1350,609);
            Text(164,120,1250,36,"근무 종료 / "+end.subtitle,17,new Color(.31f,.43f,.34f)); Text(160,174,1280,90,end.title,50,ink,FontStyle.Bold);
            small.fontSize=ReadingSize(25); small.normal.textColor=ink; small.wordWrap=true;
            string text=Expand(end.text); float height=small.CalcHeight(new GUIContent(text),1200)+18;
            WarmText(text,small.fontSize);
            endingScroll=GUI.BeginScrollView(new Rect(145,289,1250,335),endingScroll,new Rect(0,0,1200,Mathf.Max(330,height)));
            GUI.Label(new Rect(0,0,1200,height),text,small); GUI.EndScrollView();
            Line(164,626,1434,626,new Color(.53f,.57f,.47f),1);
            Text(164,650,1200,40,"구조 연결: "+state.rescued+"명    /    판단 "+state.decisions.Count+"건    /    이번 근무 "+Mathf.FloorToInt(state.playedSeconds/60)+"분 "+Mathf.FloorToInt(state.playedSeconds%60)+"초    /    엔딩 보관 완료",19,new Color(.30f,.43f,.33f));
            if(Primary(145,739,295,58,"새 근무 시작")) confirm="new";
            if(Btn(455,739,295,58,"타이틀로")) { playing=false; screen="title"; }
            if(Btn(765,739,295,58,"엔딩 기록")) ShowCollection();
            if(Btn(1075,739,295,58,"크레딧")) ShowCredits();
        }
        void DrawOverlay() { DrawMenuOverlay(); }

        void DrawConfirmation() { DrawDecisionConfirmation(); }

        IEnumerator PlayerVerification()
        {
            // Opt-in test mode uses a separate persistent-data directory, never the player's save.
            yield return null;
            var probe=gameObject.AddComponent<PlayerProbe>(); probe.Begin(this,store);
        }
        IEnumerator ResumeVerification()
        {
            yield return new WaitForSecondsRealtime(.5f);
            Continue(); yield return new WaitForSecondsRealtime(.5f);
            bool pass=state!=null&&Rules.Valid(story,state)&&state.call==3&&state.phase==0&&Mathf.Approximately(prefs.master,.32f)&&!prefs.motion
                &&Mathf.Approximately(prefs.readingScale,1.3f)&&Mathf.Approximately(prefs.ambience,.18f)&&Mathf.Approximately(prefs.music,.27f)&&prefs.instantText&&CollectedCount==3&&state.roomCues.Contains("rescue-beacons");
            var report=(pass?"RESUME_VERIFICATION_PASS":"RESUME_VERIFICATION_FAIL")+"\nFresh process true\nCall "+(state==null?-1:state.call)+"\nSettings master "+prefs.master+"\nReading 130%, instant text, ambience 18%, endings "+CollectedCount+"\nFrames "+FrameCount;
            File.WriteAllText(store.PathFor("resume-report.txt"),report); Debug.Log(report); Application.Quit(pass?0:1);
        }
    }
}
