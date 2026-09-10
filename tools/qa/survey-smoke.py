"""Real local Supabase tests for survey ownership, validation and idempotency. Never targets production."""
import datetime, hashlib, hmac, json, subprocess, uuid, urllib.request, urllib.error
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
root=Path(__file__).resolve().parents[2]
work=root/'Logs/remediation/backend-local';out=root/'Logs/remediation/survey'
status=json.loads(subprocess.check_output(['supabase','status','--workdir',str(work),'-o','json'],stderr=subprocess.DEVNULL))
url=status['API_URL'];assert url=='http://127.0.0.1:55321'
key=status['ANON_KEY'];checks=[]
pepper=next(line.split('=',1)[1].strip().strip('\"') for line in (work/'functions.env').read_text().splitlines() if line.startswith('PARTICIPANT_CODE_PEPPER='))
def uid():return str(uuid.uuid4())
def sql(query):
 return subprocess.check_output(['docker','exec','-i','supabase_db_geomodel-remediation','psql','-U','postgres','-d','postgres','-At','-v','ON_ERROR_STOP=1'],input=query.encode()).decode().strip()
def req(path,body=None,token=None):
 headers={'apikey':key,'Authorization':'Bearer '+(token or key)}
 data=None
 if body is not None:data=json.dumps(body).encode();headers['Content-Type']='application/json'
 try:
  with urllib.request.urlopen(urllib.request.Request(url+path,data=data,headers=headers),timeout=30) as res:return res.status,json.load(res)
 except urllib.error.HTTPError as error:
  try:payload=json.load(error)
  except Exception:payload={}
  return error.code,payload

def check(name,actual,expected):
 assert actual==expected,(name,actual,expected)
 checks.append(name)

def survey(action,token,**extra):return req('/functions/v1/game-survey',dict(action=action,**extra),token)
study,participant,run,session,install=[uid() for _ in range(5)]
code='SURVEY-'+uuid.uuid4().hex[:16].upper()
hash_code=hmac.new(pepper.encode(),code.encode(),hashlib.sha256).hexdigest()
sql(f"insert into studies(id,study_key,status,research_entry_enabled,protocol_version) values ('{study}','survey-{study}','development',true,'survey-qa'); insert into study_participants(id,study_id,participant_code_hash,condition,protocol_version) values ('{participant}','{study}','{hash_code}','A','survey-qa');")
http,auth=req('/auth/v1/signup',{});check('anonymous auth',http,200)
http,context=req('/functions/v1/research-participation',{'participantCode':code},auth['access_token']);check('invitation resolves participant',context.get('participantId'),participant)
_,other=req('/auth/v1/signup',{})
now=datetime.datetime.now(datetime.timezone.utc).isoformat()
binding=dict(participantId=participant,studyId=study,condition='A',sessionId=session)
payload=dict(binding,installId=install,protocolVersion='survey-qa',gameVersion='qa',platform='Editor',buildTarget='StandaloneOSX',language='Japanese',currentScene='Laboratory Scene',contentVersion='survey-qa',storyRoute='story-lab-analysis-v2',events=[dict(binding,id=uid(),name='session_started',occurredAt=now,props={})],quizAttempts=[])
http,_=req('/functions/v1/game-ingest',payload,auth['access_token']);check('real game session ingested',http,200)
http,_=survey('issue',auth['access_token'],sessionId=session,runId=run);check('unfinished game rejected',http,409)
payload['events']=[]
payload['progressSnapshot']=dict(binding,eventId=uid(),currentScene='Laboratory Scene',updatedAt=now,payload=dict(runId=run,investigationComplete=True))
http,_=req('/functions/v1/game-ingest',payload,auth['access_token']);check('completion snapshot ingested',http,200)
check('foreign identity cannot issue a ticket',survey('issue',other['access_token'],sessionId=session,runId=run)[0],403)
check('foreign run cannot be substituted',survey('issue',auth['access_token'],sessionId=session,runId=uid())[0],409)
http,issued=survey('issue',auth['access_token'],sessionId=session,runId=run);check('completed owner receives scoped ticket',http,200)
ticket=issued['ticket'];check('only ticket hash stored',sql(f"select count(*) from survey_tickets where token_hash='{ticket}'"),'0')
check('valid ticket opens form',survey('open',ticket)[1]['submitted'],False)
check('random ticket rejected',survey('open','f'*64)[0],403)
check('identity parameters rejected',survey('open',ticket,participantId=uid())[0],400)
check('missing required answers rejected',survey('submit',ticket,answers={})[0],400)
answers={f'q{i}':'4' for i in range(1,12)};answers.update(q12='none',q13='测试回答，用于本地验收。',q14='')
check('unknown answer fields rejected',survey('submit',ticket,answers=dict(answers,participantId=uid()))[0],400)
check('oversize text rejected',survey('submit',ticket,answers=dict(answers,q13='あ'*2001))[0],400)
_,second=survey('issue',auth['access_token'],sessionId=session,runId=run)
with ThreadPoolExecutor(2) as pool:
 replies=list(pool.map(lambda value:survey('submit',value,answers=answers),[ticket,second['ticket']]))
check('parallel submissions succeed', [r[0] for r in replies],[200,200])
check('parallel submissions share one receipt',replies[0][1]['receiptId'],replies[1][1]['receiptId'])
check('one response per completed run',sql(f"select count(*) from survey_responses where run_id='{run}'"),'1')
check('server binding matches invitation',sql(f"select participant_id::text from survey_responses where run_id='{run}'"),participant)
check('reopening recognizes submitted response',survey('open',ticket)[1]['submitted'],True)
survey('submit',ticket,answers=dict(answers,q1='1'))
check('retry never replaces first answers',sql(f"select answers->>'q1' from survey_responses where run_id='{run}'"),'4')
check('direct response table access denied',req('/rest/v1/survey_responses?select=id',token=auth['access_token'])[0],403)
sql(f"update survey_tickets set expires_at=now()-interval '1 second' where token_hash='{hashlib.sha256(ticket.encode()).hexdigest()}'")
check('expired link rejected',survey('open',ticket)[0],403)
sql(f"update study_participants set withdrawn_at=now() where id='{participant}'")
check('withdrawn participant ticket rejected',survey('open',second['ticket'])[0],403)
sql(f"update study_participants set withdrawn_at=null where id='{participant}'")
# Fresh, unfinished questionnaire for the browser test. Create its record through the real ingest API.
browser_run=uid();payload['progressSnapshot']['eventId']=uid();payload['progressSnapshot']['payload']['runId']=browser_run
req('/functions/v1/game-ingest',payload,auth['access_token'])
_,browser_link=survey('issue',auth['access_token'],sessionId=session,runId=browser_run)
(out/'fixture.json').write_text(json.dumps(dict(url=url,key=key,code=code,participant=participant,study=study,run=browser_run,session=session,ticket=browser_link['ticket'],auth=auth),indent=2))
(out/'http-results.json').write_text(json.dumps(dict(passed=True,checks=checks),indent=2))
print(f'{len(checks)} real Supabase survey checks passed')
