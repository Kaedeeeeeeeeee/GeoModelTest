// Used from `ego-browser nodejs`; captures only the current task's Unity canvas.
import fs from 'node:fs/promises';
import path from 'node:path';
const root=path.resolve(import.meta.dirname,'../..');
const work=path.join(root,'Logs/manual-video/ego');
await fs.mkdir(work,{recursive:true});

export async function run(page, op, args=[]) {
  await page.cdp('Emulation.setFocusEmulationEnabled',{enabled:true});
  if(op==='state')return page.evaluate(()=>({ready:!!window.unityInstance,quality:window.GeoModelPerformance?.quality,manual:window.GeoModelPerformance?.manual,canvas:[...document.querySelectorAll('canvas')].map(c=>({width:c.width,height:c.height,rect:c.getBoundingClientRect().toJSON()})),recording:window.tutorialRecording?.recorder.state,viewport:[innerWidth,innerHeight]}));
  if(op==='click'){
    const [x,y]=args;
    await page.cdp('Page.bringToFront');
    const p=await page.evaluate(([x,y])=>{const c=document.querySelector('canvas');const r=c.getBoundingClientRect();return {x:r.x+x/1920*r.width,y:r.y+y/1080*r.height};},[x,y]);
    await page.cdp('Input.dispatchMouseEvent',{type:'mouseMoved',x:p.x,y:p.y});
    await page.cdp('Input.dispatchMouseEvent',{type:'mousePressed',x:p.x,y:p.y,button:'left',clickCount:1});
    await new Promise(r=>setTimeout(r,120));
    await page.cdp('Input.dispatchMouseEvent',{type:'mouseReleased',x:p.x,y:p.y,button:'left',clickCount:1});
    return page.evaluate(([x,y])=>{const r=window.tutorialRecording;if(r){const e={type:'click',x,y,t:(performance.now()-r.started)/1000};r.events.push(e);return e;}return 'clicked';},[x,y]);
  }
  if(op==='drag'){
    await page.cdp('Page.bringToFront');
    const [x1,y1,x2,y2,duration=1000,hold=0]=args;
    const r=await page.evaluate(()=>document.querySelector('canvas').getBoundingClientRect().toJSON());
    const pos=(x,y)=>({x:r.x+x/1920*r.width,y:r.y+y/1080*r.height,id:1});
    await page.cdp('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[pos(x1,y1)]});
    const steps=Math.max(2,Math.min(8,Math.round(duration/250)));
    for(let i=1;i<=steps;i++){
      await page.cdp('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[pos(x1+(x2-x1)*i/steps,y1+(y2-y1)*i/steps)]});
      await new Promise(r=>setTimeout(r,duration/steps));
    }
    if(hold)await new Promise(r=>setTimeout(r,hold));
    await page.cdp('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});
    return page.evaluate(([x1,y1,x2,y2,duration,hold])=>{const r=window.tutorialRecording;if(r)r.events.push({type:'drag',x1,y1,x2,y2,duration,hold,t:(performance.now()-r.started)/1000});return 'dragged';},[x1,y1,x2,y2,duration,hold]);
  }
  if(op==='tap'){
    await page.cdp('Page.bringToFront');
    const [x,y]=args;
    const p=await page.evaluate(([x,y])=>{const r=canvas.getBoundingClientRect();return{x:r.x+x/1920*r.width,y:r.y+y/1080*r.height,id:1}},[x,y]);
    await page.cdp('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[p]});
    await new Promise(r=>setTimeout(r,150));
    await page.cdp('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});
    return page.evaluate(([x,y])=>{const r=window.tutorialRecording;if(r)r.events.push({type:'click',x,y,t:(performance.now()-r.started)/1000});return 'tapped'},[x,y]);
  }
  if(op==='shot'){
    const data=await page.evaluate(()=>new Promise(resolve=>{const previous=window.tutorialCaptureFrame;window.tutorialCaptureFrame=()=>{previous?.();const check=document.createElement("canvas");check.width=check.height=1;const cx=check.getContext("2d");cx.drawImage(canvas,0,0,1,1);const v=cx.getImageData(0,0,1,1).data;if(v[0]+v[1]+v[2]<8)return;const url=canvas.toDataURL();window.tutorialCaptureFrame=previous;resolve(url);};}));
    const file=path.join(work,args[0]+'.png');
    await fs.writeFile(file,Buffer.from(data.split(',')[1],'base64'));
    return file;
  }
  if(op==='start')return page.evaluate(()=>{
    if(window.tutorialRecording?.recorder.state==='recording')throw Error('Already recording');
    const canvas=document.querySelector('canvas');
    const mirror=document.createElement('canvas');mirror.width=canvas.width;mirror.height=canvas.height;
    const ctx=mirror.getContext('2d',{alpha:false});
    const stream=mirror.captureStream(0);
    const recorder=new MediaRecorder(stream,{mimeType:'video/webm;codecs=vp9',videoBitsPerSecond:16000000});
    const take=window.tutorialRecording={recorder,stream,mirror,chunks:[],events:[],width:canvas.width,height:canvas.height,started:performance.now(),last:0};
    window.tutorialCaptureFrame=()=>{const now=performance.now();if(recorder.state==='recording'&&now-take.last>=31){ctx.drawImage(canvas,0,0);stream.getVideoTracks()[0].requestFrame();take.last=now}};
    window.requestAnimationFrame=callback=>setTimeout(()=>{callback(performance.now());window.tutorialCaptureFrame?.()},1000/60);
    window.cancelAnimationFrame=id=>clearTimeout(id);
    recorder.ondataavailable=e=>{if(e.data.size)take.chunks.push(e.data)};
    recorder.start(1000);
    return {width:canvas.width,height:canvas.height,quality:window.GeoModelPerformance?.quality};
  });
  if(op==='stop'){
    const result=await page.evaluate(()=>new Promise(resolve=>{const t=window.tutorialRecording;t.recorder.onstop=async()=>{const blob=new Blob(t.chunks,{type:t.recorder.mimeType});const bytes=new Uint8Array(await blob.arrayBuffer());let s='';for(let i=0;i<bytes.length;i+=32768)s+=String.fromCharCode(...bytes.subarray(i,i+32768));t.stream.getTracks().forEach(track=>track.stop());resolve({data:btoa(s),duration:(performance.now()-t.started)/1000,events:t.events,width:t.width,height:t.height});};t.recorder.stop();}));
    const name=args[0];
    const file=path.join(work,name+'.webm');
    await fs.writeFile(file,Buffer.from(result.data,'base64'));
    const info={duration:result.duration,events:result.events,browser:'EgoLite',width:result.width,height:result.height};
    await fs.writeFile(path.join(work,name+'.json'),JSON.stringify(info,null,2));
    return {file,...info};
  }
  if(op==='wait'){await new Promise(r=>setTimeout(r,args[0]));return 'waited';}
  throw Error('Unknown operation '+op);
}
