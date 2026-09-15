import fs from 'node:fs/promises';
import path from 'node:path';
const root = path.resolve(import.meta.dirname, '../..');
export async function domImage(page,name){
 const css=await fs.readFile(path.join(root,'Logs/manual-video/player/manual-survey/survey.css'),'utf8');
 const data=await page.evaluate(async(css)=>{
  const html=document.documentElement.cloneNode(true);html.setAttribute('xmlns','http://www.w3.org/1999/xhtml');
  html.querySelectorAll('script,link,meta,title').forEach(e=>e.remove());
  const st=document.createElement('style');st.textContent=css;html.querySelector('head').append(st);
  const width=1280,height=Math.max(800,document.body.scrollHeight);
  const svg=`<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}"><foreignObject width="100%" height="100%">${new XMLSerializer().serializeToString(html)}</foreignObject></svg>`;
  const img=new Image();img.src='data:image/svg+xml;charset=utf-8,'+encodeURIComponent(svg);await img.decode();
  const c=document.createElement('canvas');c.width=width;c.height=height;c.getContext('2d').drawImage(img,0,0);return c.toDataURL();
 },css);
 const directory=path.join(root,'Logs/manual-video/ego');await fs.mkdir(directory,{recursive:true});
 const file=path.join(directory,name+'.png');await fs.writeFile(file,Buffer.from(data.split(',')[1],'base64'));return file;
}
