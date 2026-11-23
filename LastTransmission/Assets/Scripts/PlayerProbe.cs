using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace LastTransmission
{
    public sealed class PlayerProbe : MonoBehaviour
    {
        Game game; SaveStore store;
        public void Begin(Game target, SaveStore saves) { game=target; store=saves; StartCoroutine(Run()); }
        IEnumerator ExpectMusic(Cue cue)
        {
            float deadline=Time.realtimeSinceStartup+3;
            while((game.PlayingCue!=cue||game.MusicSource.volume<.01f)&&Time.realtimeSinceStartup<deadline) yield return null;
            if(game.MusicSource.clip.name!=cue.ToString()||!game.MusicSource.isPlaying||game.MusicSource.volume<.01f)
                throw new Exception("Expected playing score "+cue);
        }
        IEnumerator Observe(string cue,string capture)
        {
            game.OpenPanel("");
            float deadline=Time.realtimeSinceStartup+5;
            while(game.RoomCueId!=cue&&Time.realtimeSinceStartup<deadline) yield return null;
            if(game.RoomCueId!=cue||!game.Current.roomCues.Contains(cue)) throw new Exception("Missing room observation "+cue);
            yield return new WaitForSecondsRealtime(.25f);
            ScreenCapture.CaptureScreenshot(capture);
            yield return new WaitForSecondsRealtime(.25f);
        }
        IEnumerator Run()
        {
            var root=Path.Combine(Application.persistentDataPath,"Verification");
            var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++) if(args[i]=="--verification-output") root=args[i+1];
            Directory.CreateDirectory(root);
            yield return new WaitForSecondsRealtime(1);
            ScreenCapture.CaptureScreenshot(Path.Combine(root,"01-title.png"));
            yield return new WaitForSecondsRealtime(.5f);
            game.NewGame();
            yield return ExpectMusic(Cue.Watch);
            float previousVolume=game.Settings.music;
            game.Settings.music=0; yield return null; yield return null;
            if(game.MusicSource.volume!=0) throw new Exception("Music mute failed");
            game.Settings.music=previousVolume;
            game.OpenNextTask(); if(game.PanelName!="radio") throw new Exception("New shift task did not open radio");
            game.SelectFrequency("91.2"); game.SelectFrequency("invalid");
            if(game.Current.frequency!="91.2"||game.EffectPlaybackCount!=0) throw new Exception("Tuning input validation or silence failed");
            game.SelectFrequency(game.Content.calls[0].frequency);
            yield return new WaitForSecondsRealtime(.35f);
            if(game.ReceiverNeedle<.60f||game.ReceiverNeedleHeld) throw new Exception("Ordinary signal did not settle during reception");
            ScreenCapture.CaptureScreenshot(Path.Combine(root,"26-radio-needle.png"));
            yield return null;
            game.RevealAll();
            yield return new WaitForSecondsRealtime(.5f); ScreenCapture.CaptureScreenshot(Path.Combine(root,"18-radio-received.png")); yield return new WaitForSecondsRealtime(.3f);
            if(game.ReceiverNeedle>.1f||game.ReceiverNeedleHeld) throw new Exception("Ordinary signal did not return to idle");
            game.Settings.instantText=true; game.RevealOrNext();
            if(game.Current.page!=1) throw new Exception("Instant text required an extra reveal before advancing");
            game.Settings.instantText=false; game.Current.page=0;
            game.OpenPanel("records"); game.OpenDocument("handover"); game.ReadingPosition=new Vector2(0,120);
            game.OpenPanel("map"); game.OpenPanel("records");
            if(game.DocumentName!="handover"||game.ReadingPosition.y!=120) throw new Exception("Document position did not survive device switching");
            game.OpenDocument("handover"); if(game.ReadingPosition.y!=120) throw new Exception("Selecting same document reset reading position");
            game.ReadingPosition=Vector2.zero; yield return new WaitForSecondsRealtime(.3f); ScreenCapture.CaptureScreenshot(Path.Combine(root,"17-records.png")); yield return new WaitForSecondsRealtime(.3f);
            game.Current.page=game.Content.calls[0].pages.Length-1;
            foreach(var id in game.Content.calls[0].documents) game.OpenDocument(id);
            game.OpenNextTask(); if(game.PanelName!="board") throw new Exception("Investigation task skipped missing findings");
            for(int i=0;i<8;i++)
            {
                game.OpenPanel("map"); game.OpenPanel("records"); game.OpenPanel("board");
                for(int q=0;q<2;q++) game.NoteFinding(q,game.Content.calls[0].findings[q].answer);
            }
            if(game.EffectPlaybackCount!=0||game.FeedbackSource.isPlaying) throw new Exception("Ordinary UI navigation or casework produced a sound");
            game.OpenNextTask(); if(game.PanelName!="radio") throw new Exception("Completed investigation did not return to response");
            game.RevealAll(); yield return new WaitForSecondsRealtime(.3f); ScreenCapture.CaptureScreenshot(Path.Combine(root,"19-radio-choices.png")); yield return new WaitForSecondsRealtime(.3f);
            game.OpenPanel(""); yield return new WaitForSecondsRealtime(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(root,"00-room.png")); yield return new WaitForSecondsRealtime(.3f);
            game.OpenPanel("map"); yield return new WaitForSecondsRealtime(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(root,"02-map.png"));
            yield return new WaitForSecondsRealtime(.5f);
            int mapAudio=game.EffectPlaybackCount;
            foreach(var id in new[]{"routes","bridge","ridge","generator","entry"})
            {
                game.InspectMapLocation(id);
                if(game.DocumentName!=id||!game.Current.Read(id)||game.PanelName!="map") throw new Exception("Map location did not open its source: "+id);
            }
            yield return new WaitForSecondsRealtime(.35f);
            ScreenCapture.CaptureScreenshot(Path.Combine(root,"25-map-entry.png"));
            yield return new WaitForSecondsRealtime(.25f);
            game.InspectMapLocation("bridge"); game.InspectMapLocation("handover");
            if(game.DocumentName!="bridge"||game.EffectPlaybackCount!=mapAudio||game.Current.decisions.Count!=0) throw new Exception("Map navigation changed judgement or played UI sounds");
            yield return new WaitForSecondsRealtime(.3f); ScreenCapture.CaptureScreenshot(Path.Combine(root,"14-map-bridge.png")); yield return new WaitForSecondsRealtime(.3f);
            game.Settings.readingScale=1.3f; game.ExpandMapRecord();
            if(!game.MapReadingExpanded) throw new Exception("Map source expansion failed");
            yield return new WaitForSecondsRealtime(.3f); ScreenCapture.CaptureScreenshot(Path.Combine(root,"15-map-detail.png")); yield return new WaitForSecondsRealtime(.3f);
            game.ReturnToMap(); game.InspectMapLocation("generator");
            yield return new WaitForSecondsRealtime(.3f); ScreenCapture.CaptureScreenshot(Path.Combine(root,"16-map-large-text.png")); yield return new WaitForSecondsRealtime(.3f);
            if(game.MapReadingExpanded) throw new Exception("Return from map source failed");
            game.Settings.readingScale=1;
            for(int c=0;c<game.Content.calls.Length;c++)
            {
                var s=game.Current; var call=game.Content.calls[c];
                game.OpenPanel("radio"); s.frequency=call.frequency;
                s.page=call.pages.Length-1;
                foreach(var d in call.documents) game.OpenDocument(d);
                for(int q=0;q<call.findings.Length;q++) game.NoteFinding(q,call.findings[q].answer);
                if(c==0) { game.OpenPanel("board"); game.Settings.readingScale=1.3f; game.CompareEvidence(1); yield return new WaitForSecondsRealtime(.35f); ScreenCapture.CaptureScreenshot(Path.Combine(root,"23-comparison.png")); yield return new WaitForSecondsRealtime(.35f); game.Settings.readingScale=1; game.OpenPanel("board"); yield return new WaitForSecondsRealtime(.3f); ScreenCapture.CaptureScreenshot(Path.Combine(root,"08-casework.png")); yield return new WaitForSecondsRealtime(.3f); game.OpenPanel("radio"); }
                if(!Rules.CanRespond(game.Content,s)) throw new Exception("Player response gate failed at "+c);
                yield return new WaitForSecondsRealtime(.4f);
                string[] desired={"firstrescue","bridgechallenged","passwordchallenged","technician","isolated","verified"};
                int choice=Array.FindIndex(call.choices,r=>r.flag==desired[c]);
                if(c==1) yield return ExpectMusic(Cue.Doubt);
                if(c==4) yield return ExpectMusic(Cue.Echo);
                if(c==5) yield return ExpectMusic(Cue.Door);
                game.Respond(choice);
                if(c==0) {
                    game.OpenPanel("board"); game.Settings.readingScale=1.3f;
                    yield return new WaitForSecondsRealtime(.35f); ScreenCapture.CaptureScreenshot(Path.Combine(root,"24-board-filed.png"));
                    yield return new WaitForSecondsRealtime(.35f); game.Settings.readingScale=1; game.OpenPanel("radio");
                }
                if(c==3) yield return ExpectMusic(Cue.Shelter);
                if(c==0&&game.EffectPlaybackCount!=0) throw new Exception("Radio response played a button sound");
                game.RevealAll(); yield return new WaitForSecondsRealtime(.5f);
                if(c==0) { ScreenCapture.CaptureScreenshot(Path.Combine(root,"03-radio.png")); yield return new WaitForSecondsRealtime(.3f); }
                if(c==0) yield return Observe("rescue-beacons",Path.Combine(root,"20-rescue.png"));
                game.NextCall(); yield return null;
                if(c==3)
                {
                    yield return Observe("returning-phrase",Path.Combine(root,"21-echo.png"));
                    if(!game.Current.events.Exists(e=>e.Contains(game.Current.echo)&&e.Contains("관제실 관측"))) throw new Exception("Echo missing from observation archive");
                    int seen=game.RoomCueCount;
                    game.OpenPanel("radio"); float remaining=game.RoomCueSeconds;
                    yield return new WaitForSecondsRealtime(.2f);
                    if(game.RoomCueSeconds!=remaining) throw new Exception("Observation elapsed behind device");
                    game.Continue(); game.OpenPanel(""); yield return new WaitForSecondsRealtime(.25f);
                    if(game.RoomCueCount!=seen||game.RoomCueId!="") throw new Exception("Observation repeated after loading");
                    game.OpenPanel("radio");
                    game.SelectFrequency(game.Content.calls[4].frequency);
                    game.RevealAll();
                    yield return new WaitForSecondsRealtime(.4f);
                    if(!game.ReceiverNeedleHeld||game.ReceiverNeedle<.8f) throw new Exception("Echo did not hold the needle after reveal");
                    ScreenCapture.CaptureScreenshot(Path.Combine(root,"27-radio-echo.png"));
                    yield return new WaitForSecondsRealtime(.2f);
                    game.RevealOrNext();
                    if(game.Current.page!=1) throw new Exception("Echo meter delayed the next page");
                    yield return new WaitForSecondsRealtime(.25f);
                    if(game.ReceiverNeedleHeld) throw new Exception("Echo meter hold leaked into the next page");
                }
                if(c==2)
                {
                    var checkpoint=new SaveStore(Path.Combine(root,"ResumeProfile"));
                    checkpoint.Write("progress.json",game.Current);
                    checkpoint.Write("settings.json",new Preferences{settingsVersion=4,music=.27f,master=.32f,effects=.25f,textSpeed=75,motion=false,readingScale=1.3f,instantText=true,ambience=.18f});
                    game.OpenPanel("archive"); game.SelectArchiveCall(0);
                    if(game.ArchiveCall!=0) throw new Exception("Archive chapter selection failed");
                    game.SelectArchiveCall(5); if(game.ArchiveCall!=0) throw new Exception("Future archive chapter unlocked");
                    yield return new WaitForSecondsRealtime(.3f); ScreenCapture.CaptureScreenshot(Path.Combine(root,"10-archive.png")); yield return new WaitForSecondsRealtime(.3f);
                }
            }
            game.PersistProgress(); var before=JsonUtility.ToJson(game.Current); var loaded=store.ReadProgress(game.Content);
            if(loaded==null||JsonUtility.ToJson(loaded)!=before) throw new Exception("Player save roundtrip failed");
            game.OpenPanel("door"); yield return new WaitForSecondsRealtime(.5f);
            if(game.Current.Has("verified")) throw new Exception("Identity automatically verified");
            int beforePulses=game.EffectPlaybackCount;
            if(!game.SendPulse(0)||!game.SendPulse(1)||!game.SendPulse(1)) throw new Exception("Hardware pulse failed");
            if(game.EffectPlaybackCount!=beforePulses) throw new Exception("Indicator buttons produced a click sound");
            if(game.ConfirmPerson(0)) throw new Exception("False observer passed");
            if(!game.ConfirmPerson(1)) throw new Exception("Live observer failed");
            if(Rules.Exposure(game.Current)>=2) game.RevokeAuthorization();
            ScreenCapture.CaptureScreenshot(Path.Combine(root,"04-door.png"));
            yield return new WaitForSecondsRealtime(.5f);
            game.DecideDoor(2); yield return new WaitForSecondsRealtime(.5f);
            yield return ExpectMusic(Cue.Dawn);
            ScreenCapture.CaptureScreenshot(Path.Combine(root,"05-ending.png"));
            yield return new WaitForSecondsRealtime(.5f);
            if(game.Content.calls.Length==6 && game.Current.ending!="dawn") throw new Exception("Expected dawn in player");
            // Exercise two more endings in the same built player, using complete alternate paths.
            for(int ending=0;ending<2;ending++)
            {
                game.NewGame();
                for(int c=0;c<game.Content.calls.Length;c++)
                {
                    var s=game.Current; var call=game.Content.calls[c]; s.frequency=call.frequency; s.page=call.pages.Length-1;
                    foreach(var d in call.documents) game.OpenDocument(d);
                    for(int q=0;q<call.findings.Length;q++) game.NoteFinding(q,(call.findings[q].answer+1)%3);
                    game.Respond(2); game.NextCall(); yield return null;
                }
                game.DecideDoor(ending); yield return new WaitForSecondsRealtime(.3f);
                yield return ExpectMusic(ending==0?Cue.Echo:Cue.Silence);
                ScreenCapture.CaptureScreenshot(Path.Combine(root,ending==0?"06-voice.png":"07-silence.png"));
                yield return new WaitForSecondsRealtime(.3f);
                if(game.Current.ending!=(ending==0?"voice":"silence")) throw new Exception("Alternate player ending failed");
            }
            // An actual alternate route must leave accumulated unsafe approvals.
            if(game.CollectedCount!=3) throw new Exception("All completed endings were not collected");
            game.NewGame();
            if(game.CollectedCount!=3) throw new Exception("New shift erased ending collection");
            string[] approvals={"firstrescue","trustedbridge","trustedpassword","technician","invitedecho","verified"};
            for(int c=0;c<game.Content.calls.Length;c++)
            {
                var s=game.Current; var call=game.Content.calls[c]; s.frequency=call.frequency; s.page=call.pages.Length-1;
                foreach(var d in call.documents) game.OpenDocument(d);
                for(int q=0;q<call.findings.Length;q++) game.NoteFinding(q,call.findings[q].answer);
                game.Respond(Array.FindIndex(call.choices,r=>r.flag==approvals[c])); game.NextCall();
                if(c==4) game.OpenPanel("radio");
                yield return null;
            }
            var finalProfile=new SaveStore(Path.Combine(root,"FinalProfile"));
            finalProfile.Write("progress.json",game.Current); finalProfile.Write("settings.json",new Preferences());
            if(Rules.Exposure(game.Current)!=3) throw new Exception("Past approvals did not accumulate");
            yield return Observe("at-the-door",Path.Combine(root,"22-door.png"));
            if(game.CurrentRoomMood.approvals!=3) throw new Exception("Controller lost queued approvals");
            game.OpenPanel("door"); yield return new WaitForSecondsRealtime(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(root,"09-approvals.png")); yield return new WaitForSecondsRealtime(.3f);
            game.SendPulse(1); game.SendPulse(0); game.SendPulse(1); game.ConfirmPerson(1);
            if(Rules.CanOpenHatch(game.Current)) throw new Exception("Unrevoked approvals allowed hatch opening");
            int beforeButtons=game.EffectPlaybackCount;
            game.RevokeAuthorization(); if(!Rules.CanOpenHatch(game.Current)) throw new Exception("Approval revocation did not restore safe hatch");
            if(game.CurrentRoomMood.approvals!=0) throw new Exception("Controller retained revoked approvals");
            game.DecideDoor(2); yield return null;
            if(game.EffectPlaybackCount!=beforeButtons) throw new Exception("Revoke or door button emitted an effect");
            var f=Resources.Load<Font>("Fonts/SUIT-Regular");
            foreach(char ch in "마지막교신서윤구조솔새") if(!f.HasCharacter(ch)) throw new Exception("Missing Korean glyph "+ch);
            game.Settings.master=.32f; game.Settings.effects=.25f; game.Settings.textSpeed=75; game.Settings.motion=false;
            game.Settings.readingScale=1.3f; game.Settings.instantText=true; game.Settings.ambience=.18f; game.PersistSettings();
            new SaveStore(Path.Combine(root,"ResumeProfile")).Write("collection.json",store.Read<EndingCollection>("collection.json"));
            game.ShowCollection(); yield return new WaitForSecondsRealtime(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(root,"11-collection.png")); yield return new WaitForSecondsRealtime(.3f);
            if(!game.SelectCollectedEnding(0)) throw new Exception("Unlocked ending cannot be read");
            yield return new WaitForSecondsRealtime(.3f); ScreenCapture.CaptureScreenshot(Path.Combine(root,"12-collected-ending.png")); yield return new WaitForSecondsRealtime(.3f);
            game.CloseQualityOverlay(); game.CloseQualityOverlay();
            game.ShowSettings(); yield return new WaitForSecondsRealtime(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(root,"13-settings.png")); yield return new WaitForSecondsRealtime(.3f); game.CloseQualityOverlay();
            if(game.LargestRadioTextHeight()<=0 || !File.Exists(Path.Combine(root,"05-ending.png"))) throw new Exception("Player did not render; use a visible window");
            var report="PLAYER_VERIFICATION_PASS\nUnity "+Application.unityVersion+"\nFrames "+game.FrameCount+"\nCalls per route "+game.Current.decisions.Count+"\nEndings dawn,voice,silence\nCasework 12 selections via automated game methods\nHardware mixed sequence, reject false observer, verify true observer\nPrior approvals 3: hatch blocked, revoke, hatch unlocked\nSaveRoundTrip true\nKoreanGlyphs true\nLongestRadioTextHeight "+game.LargestRadioTextHeight()+" (scroll viewport 296)\nResolution "+Screen.width+"x"+Screen.height;
            report+="\nAudio regression: rapid UI/casework silent, indicator buttons silent, transmission silent";
            report+="\nQuality: next-task navigation, document scroll memory, same-document selection, instant text page advance, indexed archive future lock\nEnding collection 3, preserved by new shift; detail and settings rendered at 130% reading size";
            report+="\nMap: five locations open their source, invalid location ignored, silent navigation, no response chosen; expanded original and map inspector rendered at 130%";
            report+="\nInterface: tuning validation and silence, received transcript, response choices, folder records, paper casework, access console, settings and ending collection rendered";
            report+="\nRoom: rescue beacons, archived returning phrase, device pauses caption, save reload suppresses replay, two knocks and controller approval state";
            report+="\nMusic: all seven synchronized arrangements selected and playing, music volume mute, silent revoke and door confirmation";
            report+="\nReceiver: active signal, ordinary release, echo hold after reveal and immediate next-page input";
            File.WriteAllText(Path.Combine(root,"player-report.txt"),report);
            Debug.Log(report); Application.Quit(0);
        }
    }
}
