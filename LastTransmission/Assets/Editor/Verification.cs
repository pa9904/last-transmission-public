using System;
using System.IO;
using UnityEngine;

namespace LastTransmission.Editor
{
    public static class Verification
    {
        static int assertions;
        static void Check(bool ok,string message) { assertions++; if(!ok) throw new Exception("Verification failed: "+message); }
        public static void Run()
        {
            assertions=0;
            var story=JsonUtility.FromJson<Story>(Resources.Load<TextAsset>("story").text);
            var atmosphere=JsonUtility.FromJson<AtmosphereData>(Resources.Load<TextAsset>("atmosphere").text);
            Check(atmosphere.stages.Length==story.calls.Length,"atmosphere has one stage per transmission");
            var ids=new System.Collections.Generic.HashSet<string>();
            foreach(var cue in atmosphere.cues)
                Check(ids.Add(cue.id)&&cue.call>=0&&cue.call<story.calls.Length&&cue.gain>=0&&cue.gain<=.22f&&
                    Array.IndexOf(new[]{"none","air","relay","scrape","carrier","knock"},cue.sound)>=0,"valid room cue "+cue.id);
            var room=new Progress{call=3,phase=1,roomCues=null};
            Rules.Migrate(story,room);
            Check(room.roomCues!=null&&AtmosphereRules.Next(atmosphere,room).id=="technician-lost","older save and failed rescue");
            float dim=AtmosphereRules.Evaluate(atmosphere,room).darkness;
            room.flags.Add("technician");
            var observation=AtmosphereRules.Next(atmosphere,room);
            Check(observation.id=="technician-connected"&&AtmosphereRules.Evaluate(atmosphere,room).darkness<dim,"power restored on rescue");
            Check(AtmosphereRules.Consume(atmosphere,room,observation,"03:00")&&AtmosphereRules.Next(atmosphere,room)==null&&room.events.Count==1,"latest observation replaces older pending cue");
            Check(story.calls.Length>0,"story exists"); Check(story.endings.Length>=2,"multiple endings");
            foreach(var weight in new[]{"Regular","Bold"})
            {
                var face=Resources.Load<Font>("Fonts/SUIT-"+weight);
                Check(face!=null,"SUIT "+weight+" bundled");
                foreach(char glyph in "마지막교신수색지도✓→–")
                    Check(face.HasCharacter(glyph),"SUIT "+weight+" glyph "+glyph);
            }
            Check(Resources.Load<Texture2D>("Art/ControlRoom-v2")!=null,"painted room bundled");
            var terrain=Resources.Load<Texture2D>("Art/RescueTerrain-v1");
            Check(terrain!=null,"original rescue terrain bundled");
            Check(terrain.width==1600&&terrain.height==900,"terrain linework imported at full resolution");
            foreach(var name in new[]{"computerNoise_000","doorClose_000"}) Check(Resources.Load<AudioClip>("Audio/"+name)!=null,"Audio "+name);
            foreach(var call in story.calls)
            {
                Check(call.pages.Length>0&&call.choices.Length==3,"call shape");
                Check(call.findings!=null&&call.findings.Length==2,"two deductions per call");
                foreach(var id in call.documents) Check(Array.Exists(story.dossiers,d=>d.id==id),"document reference "+id);
            }
            int paths=1; for(int c=0;c<story.calls.Length;c++) paths*=3;
            int[] ends=new int[3];
            for(int path=0;path<paths;path++)
            {
                var s=new Progress(); int p=path;
                for(int c=0;c<story.calls.Length;c++)
                {
                    var call=story.calls[c]; Check(!Rules.CanRespond(story,s),"investigation gate");
                    s.frequency=call.frequency; s.page=call.pages.Length-1;
                    foreach(var d in call.documents) if(!s.Read(d)) s.read.Add(d);
                    Check(!Rules.CanRespond(story,s),"reading alone does not complete investigation");
                    for(int q=0;q<call.findings.Length;q++) s.SetFinding(c,q,call.findings[q].answer);
                    Check(Rules.CanRespond(story,s),"response available");
                    int choice=p%3; p/=3; Rules.Choose(story,s,choice); Check(Rules.Valid(story,s),"chosen state valid");
                    Rules.Advance(story,s); Check(Rules.Valid(story,s),"advanced state valid");
                    var copy=JsonUtility.FromJson<Progress>(JsonUtility.ToJson(s)); Check(Rules.Valid(story,copy),"JSON roundtrip");
                    Check(copy.echo==s.echo&&copy.rescued==s.rescued&&copy.flags.Count==s.flags.Count,"state preserved");
                }
                Check(!s.Has("verified"),"radio response alone cannot verify identity");
                if(s.Has("technician")&&s.Has("verificationrequested"))
                {
                    Check(Rules.Pulse(s,0)&&Rules.Pulse(s,1)&&Rules.Pulse(s,1),"manual challenge sends");
                    Check(!Rules.Pulse(s,0),"fourth pulse rejected");
                    Check(!Rules.ConfirmObservation(s,0)&&!s.Has("verified"),"wrong observer cannot verify");
                    Check(Rules.ConfirmObservation(s,1),"matching live observer verifies");
                    if(Rules.Exposure(s)>=2) { Check(!Rules.CanOpenHatch(s),"prior approvals must be revoked"); s.authorizationRevoked=true; }
                }
                for(int action=0;action<3;action++)
                {
                    var branch=JsonUtility.FromJson<Progress>(JsonUtility.ToJson(s)); string end=Rules.Door(branch,action);
                    Check(Rules.Valid(story,branch),"ending state valid");
                    Check(action!=0||end=="voice","unsafe main door");
                    Check(action!=1||end=="silence","all rejected costs rescue");
                    Check(end!="dawn"||(branch.Has("technician")&&branch.Has("verified")),"dawn prerequisites");
                    ends[Array.IndexOf(new[]{"voice","silence","dawn"},end)]++;
                }
            }
            Check(ends[0]>0&&ends[1]>0,"two endings reachable");
            if(story.calls.Length==6) Check(ends[2]>0,"dawn reachable");
            var weak=new Progress(); var first=story.calls[0]; weak.page=first.pages.Length-1;
            foreach(var id in first.documents) weak.read.Add(id);
            for(int q=0;q<first.findings.Length;q++) weak.SetFinding(0,q,(first.findings[q].answer+1)%3);
            Check(Rules.CanRespond(story,weak)&&!Rules.Supported(story,weak,0),"player may commit a mistaken interpretation");
            Rules.Choose(story,weak,0); Check(weak.events.Count>0,"mistaken interpretation receives explicit feedback");
            var mono=new Progress{call=5,phase=2}; mono.flags.Add("technician"); mono.flags.Add("verificationrequested");
            Rules.Pulse(mono,0); Rules.Pulse(mono,0); Rules.Pulse(mono,0);
            Check(!Rules.ConfirmObservation(mono,1),"one-color challenge is ambiguous");
            var legacy=new Progress{version=1}; Rules.Migrate(story,legacy); Check(Rules.Valid(story,legacy),"v1 save migration");
            var root=Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName,"Artifacts","Verification");
            Directory.CreateDirectory(root); var store=new SaveStore(Path.Combine(root,"SaveTests")); var valid=new Progress();
            store.Write("progress.json",valid); Check(store.ReadProgress(story)!=null,"disk load");
            store.Write("progress.json",valid); File.WriteAllText(store.PathFor("progress.json"),"{corrupted"); Check(store.ReadProgress(story)!=null,"backup recovery");
            var invalid=new Progress{call=999}; Check(!Rules.Valid(story,invalid),"invalid chapter rejected");
            invalid=new Progress{phase=2}; Check(!Rules.Valid(story,invalid),"invalid phase rejected");
            var prefs=new Preferences{master=.32f,effects=.14f,textSpeed=80,motion=false}; store.Write("settings.json",prefs);
            Check(Mathf.Approximately(store.Read<Preferences>("settings.json").master,.32f),"settings persist");
            prefs.Normalize();
            Check(Mathf.Approximately(prefs.master,.32f)&&Mathf.Approximately(prefs.effects,.14f)&&prefs.textSpeed==80&&!prefs.motion,"legacy preferences retained");
            Check(prefs.settingsVersion==4&&Mathf.Approximately(prefs.music,.65f)&&prefs.readingScale==1&&!prefs.instantText&&Mathf.Approximately(prefs.ambience,.7f),"new preferences migrate");
            prefs.readingScale=5; prefs.ambience=-1; prefs.Normalize();
            Check(Mathf.Approximately(prefs.readingScale,1.3f)&&prefs.ambience==0,"reading and ambience bounds");
            prefs.instantText=true; store.Write("settings.json",prefs); var restored=store.Read<Preferences>("settings.json"); restored.Normalize();
            Check(restored.instantText&&Mathf.Approximately(restored.readingScale,1.3f),"new reading preferences persist");
            var collection=new EndingCollection();
            Check(!collection.Add(story,"unknown"),"unknown ending cannot unlock");
            foreach(var e in story.endings) Check(collection.Add(story,e.id),"ending unlock "+e.id);
            Check(!collection.Add(story,story.endings[0].id)&&collection.unlocked.Count==3,"ending collection unique");
            store.Write("collection.json",collection); Check(store.Read<EndingCollection>("collection.json").unlocked.Count==3,"ending collection disk roundtrip");
            var report="EDITOR_VERIFICATION_PASS\nAssertions "+assertions+"\nChoice paths "+paths+"\nDoor branches "+paths*3+"\nEnding voice/silence/dawn "+string.Join("/",ends)+"\nUnity "+Application.unityVersion;
            File.WriteAllText(Path.Combine(root,"editor-report.txt"),report); Debug.Log(report);
        }
    }
}
