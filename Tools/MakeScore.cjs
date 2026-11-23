// Original eight-bar cues at 64 bpm. Run with Node to reproduce the mono PCM masters.
const fs = require('node:fs');
const path = require('node:path');
const folder = path.join(__dirname, '../LastTransmission/Assets/Resources/Score');
fs.mkdirSync(folder, { recursive: true });
const rate = 22050, beat = 60 / 64, length = rate * 30;
const sine = Float64Array.from({length:8192}, (_, n) => Math.sin(2*Math.PI*n/8192));
const wave = phase => sine[Math.floor(phase*8192)&8191];
// A D–A–E radio interval returns in every cue. Each arrangement shares bar lines.
const sheets = {
  Watch:   {bass:[38,38,34,36], keys:[57,64,62,0,53,60,57,0], step:2,  pulse:0, shimmer:.04},
  Doubt:   {bass:[38,39,34,37], keys:[57,58,0,52,55,56,0,51], step:2,  pulse:.10, shimmer:.10},
  Shelter: {bass:[38,34,41,36], keys:[62,65,69,0,64,60,57,0], step:2,  pulse:0, shimmer:.12},
  Echo:    {bass:[38,37,38,39], keys:[57,64,63,57,56,63,62,56], step:1, pulse:.11, shimmer:.22},
  Door:    {bass:[26,27,26,25], keys:[50,0,51,0,49,0,51,0], step:2, pulse:.17, shimmer:.06},
  Dawn:    {bass:[38,43,35,38], keys:[62,66,69,71,67,64,62,0], step:2, pulse:0, shimmer:.18},
  Silence: {bass:[38,38,34,33], keys:[62,0,57,0,53,0,52,0], step:4, pulse:0, shimmer:.015}
};
for(const [name, sheet] of Object.entries(sheets)){
  const mix = new Float64Array(length);
  function note(start, beats, midi, level, pad=false){
    if(!midi) return;
    const hz=440*2**((midi-69)/12), duration=beats*beat;
    for(let i=0;i<duration*rate;i++){
      const time=i/rate, phase=time*hz;
      const attack=Math.min(1,time/(pad?.65:.09)), tail=Math.min(1,(duration-time)/(pad?1.2:.45));
      const envelope=attack*tail*(pad?.84+.16*wave(time*.31):Math.exp(-1.6*time));
      const tone=pad?(wave(phase)+.30*wave(phase*1.0014)+.14*wave(phase*2)) :
        wave(phase)+sheet.shimmer*wave(phase*3)+.09*wave(phase*2);
      mix[(Math.round(start*beat*rate)+i)%length]+=level*envelope*tone;
    }
  }
  for(let bar=0;bar<8;bar++){
    const root=sheet.bass[Math.floor(bar/2)];
    note(bar*4,4.5,root,.12,true); note(bar*4+.25,4,root+7,.05,true);
    if(sheet.pulse) for(const offset of [0,1.5,3]) note(bar*4+offset,.8,root+12,sheet.pulse);
  }
  for(let b=0;b<32;b+=sheet.step){
    const key=sheet.keys[(b/sheet.step)%8];
    note(b+.5,sheet.step*1.5,key,.19);
    if(name==='Echo') note(b+1.25,1.4,key,.055);
  }
  const out=Buffer.alloc(44+2*length);
  out.write('RIFF'); out.writeUInt32LE(out.length-8,4); out.write('WAVEfmt ',8);
  out.writeUInt32LE(16,16);out.writeUInt16LE(1,20);out.writeUInt16LE(1,22);
  out.writeUInt32LE(rate,24);out.writeUInt32LE(2*rate,28);out.writeUInt16LE(2,32);
  out.writeUInt16LE(16,34);out.write('data',36);out.writeUInt32LE(2*length,40);
  for(let i=0;i<length;i++) out.writeInt16LE(Math.round(mix[i]*32767),44+2*i);
  fs.writeFileSync(path.join(folder,name+'.wav'),out);
}
