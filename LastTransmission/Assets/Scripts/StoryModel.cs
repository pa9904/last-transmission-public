using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LastTransmission
{
    [Serializable] public class Story { public Transmission[] calls; public Dossier[] dossiers; public Ending[] endings; public string intro; }
    [Serializable] public class Transmission
    {
        public string title, time, caller, frequency, objective;
        public string[] pages;
        public string[] documents;
        public Response[] choices;
        public Finding[] findings;
    }
    [Serializable] public class Finding { public string question, source, explanation; public string[] options; public int answer; }
    [Serializable] public class Response { public string text, result, echo; public int rescue; public string flag; }
    [Serializable] public class Dossier { public string id, title, category, subtitle, text; public int unlock; }
    [Serializable] public class Ending { public string id, title, subtitle, text; }
    [Serializable] public class Progress
    {
        public int version = 2, call, page, phase, rescued;
        public string frequency = "83.4", echo = "", ending = "";
        public List<int> decisions = new List<int>();
        public List<string> flags = new List<string>();
        public List<string> read = new List<string>();
        public List<string> notes = new List<string>();
        public List<int> findings = new List<int>();
        public List<int> pulses = new List<int>();
        public List<string> events = new List<string>();
        public List<string> roomCues = new List<string>();
        public bool authorizationRevoked;
        public float playedSeconds;
        public bool Has(string flag) { return flags.Contains(flag); }
        public bool Read(string id) { return read.Contains(id); }
        public int Finding(int chapter,int question) { int i=chapter*2+question; return findings!=null&&i<findings.Count?findings[i]:-1; }
        public void SetFinding(int chapter,int question,int answer)
        {
            if(findings==null) findings=new List<int>();
            while(findings.Count<12) findings.Add(-1);
            findings[chapter*2+question]=answer;
        }
    }
    [Serializable] public class Preferences
    {
        public float master = .7f, effects = .65f, textSpeed = 42;
        public bool motion = true, fullscreen;
        public int settingsVersion;
        public float music = .65f, ambience = .7f, readingScale = 1;
        public bool instantText;
        public void Normalize()
        {
            if(settingsVersion<3) { ambience=.7f; readingScale=1; instantText=false; settingsVersion=3; }
            if(settingsVersion<4) { music=.65f; settingsVersion=4; }
            music=Mathf.Clamp01(music); master=Mathf.Clamp01(master); effects=Mathf.Clamp01(effects); ambience=Mathf.Clamp01(ambience);
            textSpeed=Mathf.Clamp(textSpeed,15,100); readingScale=Mathf.Clamp(readingScale,1,1.3f);
        }
    }
    [Serializable] public class EndingCollection
    {
        public List<string> unlocked = new List<string>();
        public bool Add(Story story,string id)
        {
            if(unlocked==null) unlocked=new List<string>();
            if(!Array.Exists(story.endings,e=>e.id==id)||unlocked.Contains(id)) return false;
            unlocked.Add(id); return true;
        }
    }
    public static class Rules
    {
        public static bool CanRespond(Story story, Progress s)
        {
            if (s.call < 0 || s.call >= story.calls.Length || s.phase != 0) return false;
            var c = story.calls[s.call];
            if (s.page < c.pages.Length - 1 || s.frequency != c.frequency) return false;
            foreach (var d in c.documents) if (!s.Read(d)) return false;
            if(c.findings!=null) for(int i=0;i<c.findings.Length;i++) if(s.Finding(s.call,i)<0) return false;
            return true;
        }
        public static bool Supported(Story story,Progress s,int call)
        {
            var questions=story.calls[call].findings;
            if(questions==null) return true;
            for(int i=0;i<questions.Length;i++) if(s.Finding(call,i)!=questions[i].answer) return false;
            return true;
        }
        public static int Exposure(Progress s)
        {
            return (s.Has("trustedbridge")?1:0)+(s.Has("trustedpassword")?1:0)+(s.Has("invitedecho")?1:0);
        }
        public static void Choose(Story story, Progress s, int choice)
        {
            if (!CanRespond(story, s)) throw new InvalidOperationException("Investigate before responding");
            var c = story.calls[s.call];
            if (choice < 0 || choice >= c.choices.Length) throw new ArgumentOutOfRangeException("choice");
            var r = c.choices[choice];
            s.decisions.Add(choice);
            s.rescued += r.rescue;
            string flag=r.flag=="verified"?"verificationrequested":r.flag;
            if (!string.IsNullOrEmpty(flag) && !s.Has(flag)) s.flags.Add(flag);
            if (s.call == 0) s.echo = r.echo;
            s.phase = 1;
            s.notes.Add(c.time + " / " + c.caller + "\n관제: " + r.text + "\n" + r.result);
            if(c.findings!=null)
            {
                string findings=""; for(int i=0;i<c.findings.Length;i++) { var q=c.findings[i]; findings+="\n판단 메모: "+q.options[s.Finding(s.call,i)]+"\n대조 결과: "+q.explanation+"\n"; }
                s.notes[s.notes.Count-1]+=findings;
                if(!Supported(story,s,s.call)) s.events.Add(c.time+" / 판단 메모에 원문과 다른 해석이 남았다. 교신 기록에서 근거를 다시 대조할 수 있다.");
            }
        }
        public static bool Pulse(Progress s,int color)
        {
            if(s.phase!=2||!s.Has("verificationrequested")||!s.Has("technician")||color<0||color>1||s.pulses.Count>=3||s.Has("verified")) return false;
            s.pulses.Add(color); return true;
        }
        public static bool ConfirmObservation(Progress s,int door)
        {
            if(s.phase!=2||!s.Has("technician")||!s.Has("verificationrequested")||s.pulses.Count!=3||!s.pulses.Contains(0)||!s.pulses.Contains(1)) return false;
            if(door!=1) { s.events.Add("03:34 / 정문은 표시한 색과 다른 순서를 보고했다. 실시간 신원 확인 실패."); return false; }
            if(!s.Has("verified")) { s.flags.Add("verified"); s.events.Add("03:34 / 서윤의 동쪽 정비구 관측이 내부 전용 확인등과 일치. 신원 확인 완료."); }
            return true;
        }
        public static bool CanOpenHatch(Progress s) { return s.Has("technician")&&s.Has("verified")&&(Exposure(s)<2||s.authorizationRevoked); }
        public static void Advance(Story story, Progress s)
        {
            if (s.phase != 1) return;
            if (s.call == story.calls.Length - 1) { s.phase = 2; return; }
            s.call++; s.page = 0; s.phase = 0;
        }
        public static string Door(Progress s, int action)
        {
            if (s.phase != 2) throw new InvalidOperationException("Final decision is not ready");
            if (action < 0 || action > 2) throw new ArgumentOutOfRangeException("action");
            s.ending = action == 0 ? "voice" : action == 2 && CanOpenHatch(s) ? "dawn" : "silence";
            s.phase = 3;
            return s.ending;
        }
        public static bool Valid(Story story, Progress s)
        {
            if (s == null || s.version != 2 || s.call < 0 || s.call >= story.calls.Length || s.phase < 0 || s.phase > 3) return false;
            if (s.page < 0 || s.page >= story.calls[s.call].pages.Length || s.decisions == null || s.flags == null || s.read == null || s.notes == null) return false;
            int count = s.call + (s.phase == 0 ? 0 : 1);
            if (s.decisions.Count != count || s.notes.Count != count || (s.phase >= 2 && s.call != story.calls.Length - 1)) return false;
            for (int i = 0; i < count; i++) if (s.decisions[i] < 0 || s.decisions[i] >= story.calls[i].choices.Length) return false;
            if(s.findings==null||s.findings.Count>12||s.pulses==null||s.events==null||s.pulses.Count>3) return false;
            for(int i=0;i<s.findings.Count;i++) if(s.findings[i]<-1||s.findings[i]>2) return false;
            foreach(var p in s.pulses) if(p<0||p>1) return false;
            for(int c=0;c<count;c++) if(story.calls[c].findings!=null) for(int q=0;q<story.calls[c].findings.Length;q++) if(s.Finding(c,q)<0) return false;
            if(s.phase!=3&&s.Has("verified")&&(!s.Has("technician")||!s.Has("verificationrequested")||s.pulses.Count!=3||!s.pulses.Contains(0)||!s.pulses.Contains(1))) return false;
            return s.phase != 3 || Array.Exists(story.endings, e => e.id == s.ending);
        }
        public static void Migrate(Story story,Progress s)
        {
            if(s!=null&&s.roomCues==null) s.roomCues=new List<string>();
            if(s==null||s.version!=1) return;
            s.version=2; s.findings=new List<int>(); s.pulses=new List<int>(); s.events=new List<string>();
            int done=s.call+(s.phase==0?0:1);
            for(int c=0;c<Math.Min(done,story.calls.Length);c++) if(story.calls[c].findings!=null)
                for(int q=0;q<story.calls[c].findings.Length;q++) s.SetFinding(c,q,story.calls[c].findings[q].answer);
            if(s.Has("verified")&&s.phase!=3) { s.flags.Remove("verified"); s.flags.Add("verificationrequested"); }
        }
    }
    public sealed class SaveStore
    {
        readonly string root;
        public SaveStore(string path) { root = path; Directory.CreateDirectory(root); }
        public string PathFor(string file) { return Path.Combine(root, file); }
        public void Write<T>(string file, T data)
        {
            var path = PathFor(file); var temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(data, true), System.Text.Encoding.UTF8);
            for(int attempt=0;;attempt++)
            {
                try
                {
                    if(File.Exists(path)) File.Replace(temp,path,path+".bak");
                    else File.Move(temp,path);
                    return;
                }
                catch(IOException) when(attempt<3&&File.Exists(temp))
                {
                    // A briefly locked destination or backup can be retried with the same complete temp file.
                    System.Threading.Thread.Sleep(20*(attempt+1));
                }
            }
        }
        public T Read<T>(string file) where T : class
        {
            foreach (var candidate in new[] { PathFor(file), PathFor(file) + ".bak" })
                try { if (File.Exists(candidate)) { var value = JsonUtility.FromJson<T>(File.ReadAllText(candidate)); if (value != null) return value; } } catch (Exception) { }
            return null;
        }
        public Progress ReadProgress(Story story)
        {
            foreach (var candidate in new[] { PathFor("progress.json"), PathFor("progress.json.bak") })
                try { if (File.Exists(candidate)) { var s = JsonUtility.FromJson<Progress>(File.ReadAllText(candidate)); Rules.Migrate(story,s); if (Rules.Valid(story, s)) return s; } } catch (Exception) { }
            return null;
        }
    }
}
