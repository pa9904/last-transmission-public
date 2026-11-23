using System.Collections.Generic;
using UnityEngine;

namespace LastTransmission
{
    public sealed partial class Game
    {
        AtmosphereData atmosphere;
        Texture2D softLight;
        readonly Dictionary<string,AudioClip> roomSounds=new Dictionary<string,AudioClip>();
        RoomCue activeRoomCue;
        AudioClip activeRoomClip;
        float roomCueRemaining,roomDarkness;
        bool roomClipPaused;
        internal int RoomCueCount { get; private set; }
        internal string RoomCueId { get { return activeRoomCue==null?"":activeRoomCue.id; } }
        internal float RoomCueSeconds { get { return roomCueRemaining; } }
        internal RoomMood CurrentRoomMood { get { return AtmosphereRules.Evaluate(atmosphere,playing?state:null); } }
        bool VisibleRoom { get { return playing&&state!=null&&screen=="play"&&panel==""&&overlay==""&&confirm==""; } }
        void InitializeAtmosphere()
        {
            atmosphere=JsonUtility.FromJson<AtmosphereData>(Resources.Load<TextAsset>("atmosphere").text);
            softLight=new Texture2D(96,96,TextureFormat.RGBA32,false);
            for(int y=0;y<96;y++) for(int x=0;x<96;x++)
            {
                float radius=Vector2.Distance(new Vector2(x,y),new Vector2(47.5f,47.5f))/47.5f;
                softLight.SetPixel(x,y,new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-radius),2)));
            }
            softLight.wrapMode=TextureWrapMode.Clamp; softLight.Apply(false,true);
        }
        void MakeRoomAudio()
        {
            roomSounds["knock"]=knock;
            foreach(string name in new[]{"air","relay","scrape","carrier"})
            {
                const int rate=22050; float seconds=name=="relay"?.65f:1.6f;
                var samples=new float[Mathf.RoundToInt(rate*seconds)]; var rng=new System.Random(name=="air"?198:347); float low=0;
                for(int i=0;i<samples.Length;i++)
                {
                    float t=i/(float)rate,progress=t/seconds;
                    float attack=Mathf.Clamp01(t/.14f),release=Mathf.Clamp01((seconds-t)/.30f);
                    low=Mathf.Lerp(low,(float)rng.NextDouble()*2-1,.035f);
                    float value=low*.46f;
                    if(name=="relay") value=(Mathf.Sin(t*2*Mathf.PI*123)*.32f+low*.24f)*Mathf.Exp(-t*12);
                    if(name=="scrape") value=(low*.60f+Mathf.Sin(t*2*Mathf.PI*(74+progress*9))*.09f)*Mathf.Sin(progress*Mathf.PI);
                    if(name=="carrier") value=(low*.40f+Mathf.Sin(t*2*Mathf.PI*211)*.06f)*(Mathf.Sin(t*17)*.25f+.7f);
                    samples[i]=value*attack*release;
                }
                var clip=AudioClip.Create("Room observation "+name,samples.Length,1,rate,false); clip.SetData(samples,0); roomSounds[name]=clip;
            }
        }
        void ResetRoomPresentation(bool snap=false)
        {
            if(effects!=null&&activeRoomClip!=null&&effects.clip==activeRoomClip) effects.Stop();
            activeRoomCue=null; activeRoomClip=null; roomClipPaused=false; roomCueRemaining=0;
            if(snap&&state!=null) roomDarkness=CurrentRoomMood.darkness;
        }
        void UpdateRoomAtmosphere(float dt)
        {
            bool roomEffect=activeRoomClip!=null&&effects.clip==activeRoomClip;
            if(!VisibleRoom)
            {
                if(roomEffect&&effects.isPlaying) { effects.Pause(); roomClipPaused=true; }
                return;
            }
            if(roomClipPaused) { if(roomEffect) effects.UnPause(); roomClipPaused=false; }
            var mood=CurrentRoomMood;
            roomDarkness=prefs.motion?Mathf.MoveTowards(roomDarkness,mood.darkness,dt*.055f):mood.darkness;
            if(roomCueRemaining>0) { roomCueRemaining=Mathf.Max(0,roomCueRemaining-dt); return; }
            if(effects.isPlaying) return;
            var next=AtmosphereRules.Next(atmosphere,state);
            if(next==null||!AtmosphereRules.Consume(atmosphere,state,next,story.calls[state.call].time)) return;
            activeRoomCue=next; roomCueRemaining=8; activeRoomClip=null; RoomCueCount++; Save();
            if(!string.IsNullOrEmpty(next.sound)&&roomSounds.TryGetValue(next.sound,out var clip))
            { activeRoomClip=clip; Play(clip,Mathf.Clamp(next.gain,0,.22f)); }
        }
        void RoomGlow(float x,float y,float radius,Color color)
        {
            var previous=GUI.color; GUI.color=color;
            GUI.DrawTexture(new Rect(x-radius*3,y-radius*3,radius*6,radius*6),softLight); GUI.color=previous;
            Disc(x,y,Mathf.Max(2,radius*.30f),new Color(color.r,color.g,color.b,Mathf.Min(1,color.a*2)));
        }
        void DrawRoomAtmosphere()
        {
            if(!playing||state==null) return;
            var mood=CurrentRoomMood;
            // Frost stays on the glass edges; it never obscures dossier text.
            for(int i=0;i<43;i++)
            {
                float x=320+(i%2==0?7+(i%7)*6:766-(i%9)*5);
                float y=125+Mathf.Repeat(i*51.7f,328),length=15+(i%5)*9;
                Color c=new Color(.69f,.81f,.80f,mood.frost*.22f);
                Line(x,y,x+(i%2==0?length:-length),y-11,c,1);
                Line(x,y,x+(i%2==0?length*.5f:-length*.5f),y+8,c,1);
            }
            var old=GUI.color; GUI.color=new Color(.57f,.73f,.73f,mood.frost*.12f);
            GUI.DrawTexture(new Rect(843,321,218,116),softLight); GUI.DrawTexture(new Rect(323,310,155,135),softLight); GUI.color=old;
            if(mood.chapter>=1)
            {
                RoomGlow(901,267,7,new Color(.78f,.86f,.82f,.48f));
            }
            if(mood.rescueLights)
            {
                RoomGlow(701,379,5,new Color(.43f,.83f,.56f,.68f)); RoomGlow(725,375,4,new Color(.43f,.83f,.56f,.51f));
            }
            Box(0,67,1600,756,new Color(.01f,.025f,.045f,roomDarkness));
            // A soft shadow at the closed door; no door animation or fake opening.
            GUI.color=new Color(.01f,.015f,.013f,mood.presence*.85f);
            GUI.DrawTexture(new Rect(1250,578,304,49),softLight); GUI.color=old;
            if(mood.chapter>=4)
            {
                old=GUI.color; GUI.color=new Color(.01f,.018f,.017f,mood.presence*.33f);
                GUI.DrawTexture(new Rect(1264,485,46,99),softLight); GUI.DrawTexture(new Rect(1465,515,28,69),softLight); GUI.color=old;
            }
            // This is the interior controller, not the broken exterior red lamp.
            Box(1207,381,36,29,new Color(.08f,.115f,.105f));
            Color controller=mood.approvals>0?new Color(.88f,.42f,.23f):teal;
            RoomGlow(1225,397,6,new Color(controller.r,controller.g,controller.b,.65f));
            Box(1198,430,53,42,new Color(.08f,.12f,.10f,.95f));
            Text(1204,435,50,22,"잠금",12,ivory);
            Text(1203,455,52,20,mood.approvals>0?"대기 "+mood.approvals+"건":"수동",10,mood.approvals>0?amber:teal);
            if(mood.eastPower)
            {
                RoomGlow(1110,596,6,new Color(.43f,.83f,.63f,.7f)); Text(1030,571,220,24,"동쪽 회선 연결",13,teal);
                // A steady recovered desk-lamp pool, with no flash or strobe.
                old=GUI.color; GUI.color=new Color(.89f,.76f,.43f,.035f);
                GUI.DrawTexture(new Rect(382,493,247,156),softLight); GUI.color=old;
            }
            if(mood.echoPaper)
            {
                PaperSurface(1034,666,320,34); Text(1043,674,306,22,"수신 / “"+state.echo+"”",14,ink);
            }
        }
        void DrawRoomObservation()
        {
            if(!VisibleRoom||activeRoomCue==null||roomCueRemaining<=0) return;
            float alpha=prefs.motion?Mathf.Min(1,roomCueRemaining):1;
            Box(395,697,816,109,new Color(.018f,.033f,.034f,.96f*alpha)); Line(395,697,1211,697,new Color(.46f,.64f,.56f,alpha),1);
            Text(416,710,750,24,"관제실 관측  /  교신 기록 [H]에 보관",13,muted);
            Text(416,740,773,57,activeRoomCue.caption.Replace("{echo}",state.echo),ReadingSize(18),ivory);
        }
    }
}
