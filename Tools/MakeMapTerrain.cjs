// Original fictional terrain artwork. Node built-ins only; deterministic PNG.
// Labels, routes and interactive landmarks are rendered separately by MapUI.cs.
const fs=require('node:fs'), path=require('node:path'), zlib=require('node:zlib');
const width=1600, height=900;
function hill(x,y,cx,cy,sx,sy,amp){return amp*Math.exp(-((x-cx)**2/sx**2+(y-cy)**2/sy**2));}
function elevation(x,y){
  return 230+hill(x,y,.64,.20,.22,.26,680)+hill(x,y,.89,.14,.18,.20,460)
    +hill(x,y,.19,.23,.20,.26,350)+hill(x,y,.62,.66,.16,.24,430)
    +hill(x,y,.91,.76,.19,.29,300)+hill(x,y,.09,.76,.15,.30,210)
    -hill(x,y,.41,.36,.075,.5,140)+24*Math.sin(x*24+y*9)+12*Math.sin(x*53-y*23);
}
function hash(x,y){let n=Math.imul(x+37,374761393)^Math.imul(y+71,668265263);n=Math.imul(n^(n>>>13),1274126177);return ((n^(n>>>16))>>>0)/4294967295;}
const rgba=Buffer.alloc(width*height*4);
for(let y=0;y<height;y++)for(let x=0;x<width;x++){
  const u=x/(width-1),v=y/(height-1),h=elevation(u,v);
  const dx=elevation(u+.001,v)-elevation(u-.001,v),dy=elevation(u,v+.001)-elevation(u,v-.001);
  const shade=Math.max(-18,Math.min(18,(-dx-dy)*1.4));
  const grad=Math.max(.4,Math.hypot(dx/3.2,dy/1.8));
  const nearest=Math.abs(h/38-Math.round(h/38))*38;
  const major=Math.abs(h/190-Math.round(h/190))*190;
  const contour=Math.max(0,1-nearest/(grad*.9));
  const index=Math.max(0,1-major/(grad*1.3));
  const grain=(hash(x,y)-.5)*7;
  const edge=Math.max(0,1-Math.min(u,1-u,v,1-v)*15)*9;
  const snow=Math.min(1,Math.max(0,(h-630)/340));
  const wash=5*Math.sin(u*9+v*4)+3*Math.sin(v*13);
  let color=[190+snow*23+shade+wash,197+snow*18+shade+wash,169+snow*29+shade+wash];
  for(let c=0;c<3;c++)color[c]+=grain-edge-contour*24-index*18;
  // Subtle folds and registration grid printed into the paper.
  for(const fold of [.33,.67])if(Math.abs(u-fold)<.003)for(let c=0;c<3;c++)color[c]+=(u<fold?-6:5);
  if(Math.abs(v-.51)<.004)for(let c=0;c<3;c++)color[c]+=(v<.51?-5:4);
  const i=(y*width+x)*4;rgba[i]=color[0];rgba[i+1]=color[1];rgba[i+2]=color[2];rgba[i+3]=255;
}
function pixel(x,y,c,alpha){x=Math.round(x);y=Math.round(y);if(x<0||y<0||x>=width||y>=height)return;const i=(y*width+x)*4;for(let k=0;k<3;k++)rgba[i+k]=rgba[i+k]*(1-alpha)+c[k]*alpha;}
function line(x,y,tx,ty,c,alpha){const steps=Math.ceil(Math.hypot(tx-x,ty-y));for(let s=0;s<=steps;s++){let t=s/steps;pixel(x+(tx-x)*t,y+(ty-y)*t,c,alpha);}}
// Restrained engraved conifer groups, avoiding a dense illustrative backdrop.
for(let y=75;y<height-45;y+=23)for(let x=45;x<width-35;x+=29){
  const u=x/width,v=y/height,h=elevation(u,v),r=hash(x,y);
  if(r>.43||h>700||h<310)continue;
  const cx=x+(hash(y,x)-.5)*18,cy=y+(r-.2)*14,s=4+r*4,c=[58,83,66];
  line(cx,cy-8,cx,cy+6,c,.27);line(cx,cy-8,cx-s,cy+2,c,.28);line(cx,cy-8,cx+s,cy+2,c,.28);
  line(cx-s,cy+2,cx+s,cy+2,c,.2);
}
const table=Array.from({length:256},(_,n)=>{for(let i=0;i<8;i++)n=n&1?0xedb88320^(n>>>1):n>>>1;return n>>>0;});
function chunk(type,data){const t=Buffer.from(type),body=Buffer.concat([t,data]);let crc=0xffffffff;for(const b of body)crc=table[(crc^b)&255]^(crc>>>8);const out=Buffer.alloc(data.length+12);out.writeUInt32BE(data.length);body.copy(out,4);out.writeUInt32BE((crc^0xffffffff)>>>0,out.length-4);return out;}
const rows=Buffer.alloc((width*4+1)*height);for(let y=0;y<height;y++)rgba.copy(rows,y*(width*4+1)+1,y*width*4,(y+1)*width*4);
const header=Buffer.alloc(13);header.writeUInt32BE(width);header.writeUInt32BE(height,4);header[8]=8;header[9]=6;
const png=Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',header),chunk('IDAT',zlib.deflateSync(rows,{level:9})),chunk('IEND',Buffer.alloc(0))]);
const target=path.join(__dirname,'../LastTransmission/Assets/Resources/Art/RescueTerrain-v1.png');
fs.writeFileSync(target,png);console.log(JSON.stringify({target,width,height,bytes:png.length}));
