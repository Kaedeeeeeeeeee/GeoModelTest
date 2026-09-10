/* All participant/run binding is server-side. The only credential is a scoped survey ticket. */
(() => {
  'use strict';
  const $ = id => document.getElementById(id);
  const storage = {get(key){try{return sessionStorage.getItem(key);}catch{return null;}},set(key,value){try{sessionStorage.setItem(key,value);}catch{}},remove(key){try{sessionStorage.removeItem(key);}catch{}}};
  const fragment = new URLSearchParams(location.hash.slice(1));
  const incoming = fragment.get('ticket');
  // Scrub the bearer credential before fetching anything. It never enters query strings/referrers.
  if (location.hash) history.replaceState(null,'',location.pathname + location.search);
  let ticket = incoming || storage.get('geomodel.survey.ticket') || '';
  if (incoming) storage.set('geomodel.survey.ticket',incoming);
  let definition, step = 0, answers = {}, busy = false;
  const draftKey = 'geomodel.survey.draft.' + ticket;
  function screen(name){for(const id of ['loading','access-error','complete','questionnaire']) $(id).hidden=id!==name;}
  async function call(action, extra={}) {
    const endpoint = new URL(window.GEOMODEL_SURVEY.apiUrl);
    if (endpoint.protocol !== 'https:' && !(endpoint.hostname === '127.0.0.1' && location.hostname === '127.0.0.1')) throw new Error('config');
    const controller = new AbortController(), timer = setTimeout(()=>controller.abort(),20000);
    try {
      const response = await fetch(endpoint,{method:'POST',cache:'no-store',credentials:'omit',referrerPolicy:'no-referrer',headers:{'Content-Type':'application/json',Authorization:'Bearer '+ticket},body:JSON.stringify({action,...extra}),signal:controller.signal});
      const data = await response.json();
      if (!response.ok || !data.ok) {const error=new Error(data.error || 'network');error.status=response.status;throw error;}
      return data;
    } finally {clearTimeout(timer);}
  }
  function saveDraft(){storage.set(draftKey,JSON.stringify({version:definition.version,step,answers}));updateCount();}
  function updateCount(){const count=definition.questions.filter(q=>answers[q.id]?.trim()).length;$('answered-label').textContent=`${count} / 14 問`;}
  function render(focus=false) {
    const section=definition.sections[step];
    $('step-label').textContent=`${step+1} / ${definition.sections.length} ページ`;
    $('progress').value=step+1;$('section-number').textContent=`${String(step+1).padStart(2,'0')}`;
    $('section-title').textContent=section.title;$('section-description').textContent=section.description;
    $('questions').replaceChildren();$('form-error').hidden=true;
    for(const id of section.questions){
      const question=definition.questions.find(q=>q.id===id);
      const field=document.createElement('fieldset');field.id='field-'+id;
      const legend=document.createElement('legend');const number=document.createElement('span');number.className='number';number.textContent=id.slice(1)+'.';legend.append(number,document.createTextNode(question.text));
      if(question.optional){const optional=document.createElement('span');optional.className='optional';optional.textContent='自由回答';legend.append(optional);}
      field.append(legend);
      if(question.scale){
        const options=document.createElement('div');options.className='options';
        for(const [value,text] of definition.scales[question.scale]){
          const label=document.createElement('label');label.className='option';const input=document.createElement('input');input.type='radio';input.name=id;input.value=value;input.checked=answers[id]===value;
          input.addEventListener('change',()=>{
            answers[id]=value;field.classList.remove('invalid');field.removeAttribute('aria-invalid');saveDraft();
            if(section.questions.every(key=>definition.questions.find(q=>q.id===key).optional||answers[key])) $('form-error').hidden=true;
          });
          label.append(input,document.createTextNode(text));options.append(label);
        }field.append(options);
      }else{
        const input=document.createElement('textarea');input.name=id;input.maxLength=question.maxLength;input.placeholder='書かなくてもかまいません';input.value=answers[id]||'';input.setAttribute('aria-label',question.text);
        const count=document.createElement('p');count.className='counter';const update=()=>count.textContent=`${input.value.length} / ${question.maxLength} 文字`;
        input.addEventListener('input',()=>{answers[id]=input.value;update();saveDraft();});update();field.append(input,count);
      }$('questions').append(field);
    }
    $('previous').hidden=step===0;$('next').textContent=step===definition.sections.length-1?'回答を送信する':'次へ →';updateCount();
    if(focus){$('section-title').focus();$('progress').scrollIntoView({block:'start'});}
  }
  function fail(text){$('form-error').textContent=text;$('form-error').hidden=false;$('form-error').focus();}
  function finished(){storage.remove(draftKey);screen('complete');$('complete').setAttribute('tabindex','-1');$('complete').focus();window.scrollTo(0,0);}
  $('previous').onclick=()=>{if(busy)return;step--;saveDraft();render(true);};
  $('survey').onsubmit=async event=>{
    event.preventDefault();if(busy)return;
    const missing=definition.sections[step].questions.find(id=>!definition.questions.find(q=>q.id===id).optional&&!answers[id]);
    if(missing){$('field-'+missing).classList.add('invalid');$('field-'+missing).setAttribute('aria-invalid','true');fail('まだ選んでいない質問があります。「答えたくない」も選べます。');return;}
    if(step<definition.sections.length-1){step++;saveDraft();render(true);return;}
    busy=true;$('next').disabled=true;$('previous').disabled=true;$('next').textContent='送信しています…';$('form-error').hidden=true;
    try{await call('submit',{answers});finished();}
    catch(error){fail(error.status===403?'回答の受付期限が切れたか、受付が停止しています。ゲームの調査報告から開き直してください。':'送信を確認できませんでした。回答はこのページに残っています。通信を確認して、もう一度送信してください。');}
    finally{busy=false;$('next').disabled=false;$('previous').disabled=false;$('next').textContent='回答を送信する';}
  };
  async function start(){
    screen('loading');
    if(!/^[0-9a-f]{64}$/.test(ticket)){screen('access-error');$('access-retry').hidden=true;return;}
    try{
      const response=await call('open');
      if(response.submitted){finished();return;}
      definition=window.GEOMODEL_QUESTIONS;
      if(response.surveyVersion!==definition.version)throw new Error('version');
      try{const draft=JSON.parse(storage.get(draftKey)||'null');if(draft?.version===definition.version){answers=draft.answers||{};step=Math.min(Math.max(Number(draft.step)||0,0),definition.sections.length-1);}}catch{}
      $('intro').textContent=definition.intro;$('privacy').textContent=definition.privacy;render();screen('questionnaire');
    }catch(error){screen('access-error');$('access-text').textContent=error.status===403?'このリンクの受付期限が切れたか、受付が停止しています。ゲームの調査報告から開き直してください。':'接続を確認できませんでした。通信を確認して、もう一度お試しください。';$('access-retry').hidden=false;}
  }
  $('access-retry').onclick=start;
  start();
})();
