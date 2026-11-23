using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastTransmission
{
    [Serializable] public class AtmosphereData { public AtmosphereStage[] stages; public RoomCue[] cues; }
    [Serializable] public class AtmosphereStage { public float darkness,frost,wind,radio,presence; }
    [Serializable] public class RoomCue
    {
        public string id,caption,sound,require,exclude;
        public int call,minPhase,priority;
        public float gain;
        public bool revoked;
    }
    public struct RoomMood
    {
        public int chapter,approvals;
        public float darkness,frost,wind,radio,presence;
        public bool rescueLights,eastPower,echoPaper,isolated,verified;
    }
    public static class AtmosphereRules
    {
        public static RoomMood Evaluate(AtmosphereData data,Progress state)
        {
            int chapter=state==null?0:Mathf.Clamp(state.call,0,data.stages.Length-1);
            var stage=data.stages[chapter];
            var mood=new RoomMood {chapter=chapter,darkness=stage.darkness,frost=stage.frost,wind=stage.wind,radio=stage.radio,presence=stage.presence};
            if(state==null) return mood;
            mood.approvals=state.authorizationRevoked?0:Rules.Exposure(state);
            mood.rescueLights=state.Has("firstrescue"); mood.eastPower=state.Has("technician");
            mood.echoPaper=chapter>=4; mood.isolated=state.Has("isolated"); mood.verified=state.Has("verified");
            mood.presence=Mathf.Clamp01(mood.presence+mood.approvals*.08f+(state.Has("invitedecho")?.1f:0));
            if(mood.eastPower) { mood.darkness=Mathf.Max(.04f,mood.darkness-.065f); mood.radio*=.72f; }
            if(mood.isolated) mood.radio*=.4f;
            return mood;
        }
        public static bool Eligible(RoomCue cue,Progress state)
        {
            return state!=null&&state.phase<3&&cue.call==state.call&&state.phase>=cue.minPhase
                &&(string.IsNullOrEmpty(cue.require)||state.Has(cue.require))
                &&(string.IsNullOrEmpty(cue.exclude)||!state.Has(cue.exclude))
                &&(!cue.revoked||state.authorizationRevoked);
        }
        public static RoomCue Next(AtmosphereData data,Progress state)
        {
            RoomCue selected=null;
            foreach(var cue in data.cues)
                if(Eligible(cue,state)&&(state.roomCues==null||!state.roomCues.Contains(cue.id))&&(selected==null||cue.priority>selected.priority)) selected=cue;
            return selected;
        }
        public static bool Consume(AtmosphereData data,Progress state,RoomCue selected,string time)
        {
            if(selected==null||!Eligible(selected,state)) return false;
            if(state.roomCues==null) state.roomCues=new List<string>();
            if(state.roomCues.Contains(selected.id)) return false;
            // Only the current observation is presented. Superseded cues do not
            // build a backlog or replay an earlier chapter after fast progress.
            foreach(var cue in data.cues) if(Eligible(cue,state)&&cue.priority<=selected.priority&&!state.roomCues.Contains(cue.id)) state.roomCues.Add(cue.id);
            string text=selected.caption.Replace("{echo}",state.echo);
            state.events.Add(time+" / 관제실 관측\n"+text); return true;
        }
    }
}
