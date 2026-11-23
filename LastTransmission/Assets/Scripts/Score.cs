using System;
using UnityEngine;

namespace LastTransmission
{
    internal enum Cue { Watch, Doubt, Shelter, Echo, Door, Dawn, Silence }

    public sealed partial class Game
    {
        AudioSource musicSource;
        AudioClip[] arrangements;
        float musicEnvelope;
        internal Cue PlayingCue {get; private set;}
        internal AudioSource MusicSource => musicSource;

        internal static Cue ChooseMusic(Progress progress)
        {
            if(progress==null) return Cue.Watch;
            if(progress.phase==3)
            {
                if(progress.ending=="dawn") return Cue.Dawn;
                return progress.ending=="voice"?Cue.Echo:Cue.Silence;
            }
            if(progress.call>=5) return Rules.CanOpenHatch(progress)?Cue.Shelter:Cue.Door;
            if(progress.call==4 && !progress.Has("isolated")) return Cue.Echo;
            if(progress.Has("technician")) return Cue.Shelter;
            return progress.call==0?Cue.Watch:Cue.Doubt;
        }

        void StartScore()
        {
            var cues=(Cue[])Enum.GetValues(typeof(Cue));
            arrangements=new AudioClip[cues.Length];
            foreach(var cue in cues) arrangements[(int)cue]=Resources.Load<AudioClip>("Score/"+cue);
            musicSource=gameObject.AddComponent<AudioSource>();
            musicSource.loop=true;
            musicSource.volume=0;
            musicSource.clip=arrangements[(int)Cue.Watch];
            musicSource.Play();
        }

        void UpdateScore(float seconds)
        {
            var target=ChooseMusic(playing?state:null);
            bool changing=target!=PlayingCue;
            float level=changing?0:overlay==""?1:.45f;
            musicEnvelope=Mathf.MoveTowards(musicEnvelope,level,seconds*(changing?1.8f:.85f));
            musicSource.volume=musicEnvelope*prefs.music*.65f;
            if(!changing||musicEnvelope>0) return;
            // Cues share tempo and length. Carry the playhead across an arrangement change.
            int sample=musicSource.timeSamples;
            musicSource.Stop();
            musicSource.clip=arrangements[(int)target];
            musicSource.timeSamples=sample%musicSource.clip.samples;
            musicSource.Play();
            PlayingCue=target;
        }
    }
}
