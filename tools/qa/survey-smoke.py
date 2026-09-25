"""Real local Supabase tests for survey ownership, validation and idempotency. Never targets production."""
import datetime, hashlib, hmac, json, subprocess, uuid, urllib.request, urllib.error, argparse
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
parser=argparse.ArgumentParser()
parser.add_argument('--open-play',action='store_true')
args=parser.parse_args()
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
if not args.open_play:
 sql(f"insert into studies(id,study_key,status,research_entry_enabled,protocol_version) values ('{study}','survey-{study}','development',true,'survey-qa'); insert into study_participants(id,study_id,participant_code_hash,condition,protocol_version) values ('{participant}','{study}','{hash_code}','A','survey-qa');")
http,auth=req('/auth/v1/signup',{});check('anonymous auth',http,200)
entry={'entryMode':'open_play'} if args.open_play else {'participantCode':code}
http,context=req('/functions/v1/research-participation',entry,auth['access_token']);check('participant activation',http,200)
if args.open_play:
 participant=context['participantId'];study=context['studyId'];code=None
 check('no code or fabricated consent',sql(f"select (participant_code_hash is null and consent_version is null and guardian_consent_at is null and student_assent_at is null)::text from study_participants where id='{participant}'"),'true')
 check('starting alone is not a survey respondent',sql(f"select count(*) from survey_responses where participant_id='{participant}'"),'0')
 check('activation retry reuses anonymous participant',req('/functions/v1/research-participation',entry,auth['access_token'])[1]['participantId'],participant)
else:
 check('invitation resolves participant',context.get('participantId'),participant)
_,other=req('/auth/v1/signup',{})
now=datetime.datetime.now(datetime.timezone.utc).isoformat()
binding=dict(participantId=participant,studyId=study,condition=context['condition'],sessionId=session)
payload=dict(binding,installId=install,protocolVersion=context['protocolVersion'],gameVersion='qa',platform='Editor',buildTarget='StandaloneOSX',language='Japanese',currentScene='Laboratory Scene',contentVersion='survey-qa',storyRoute='story-lab-analysis-v2',events=[dict(binding,id=uid(),name='session_started',occurredAt=now,props={})],quizAttempts=[])
http,_=req('/functions/v1/game-ingest-v2',payload,auth['access_token']);check('real game session ingested',http,200)
http,_=survey('issue',auth['access_token'],sessionId=session,runId=run);check('unfinished game rejected',http,409)
payload['events']=[]
payload['progressSnapshot']=dict(binding,eventId=uid(),currentScene='Laboratory Scene',updatedAt=now,payload=dict(runId=run,investigationComplete=True))
http,_=req('/functions/v1/game-ingest-v2',payload,auth['access_token']);check('completion snapshot ingested',http,200)
check('foreign identity cannot issue a ticket',survey('issue',other['access_token'],sessionId=session,runId=run)[0],403)
check('foreign run cannot be substituted',survey('issue',auth['access_token'],sessionId=session,runId=uid())[0],409)
http,issued=survey('issue',auth['access_token'],sessionId=session,runId=run);check('completed owner receives scoped ticket',http,200)
ticket=issued['ticket'];check('new tickets use the revised questionnaire',issued['surveyVersion'],'post-game-ja-v2');check('only ticket hash stored',sql(f"select count(*) from survey_tickets where token_hash='{ticket}'"),'0')
check('valid ticket opens form',survey('open',ticket)[1]['submitted'],False)
check('random ticket rejected',survey('open','f'*64)[0],403)
check('identity parameters rejected',survey('open',ticket,participantId=uid())[0],400)
check('missing required answers rejected',survey('submit',ticket,answers={})[0],400)
answers={f'q{i}':'4' for i in range(1,12)};answers.update(q13='测试回答，用于本地验收。',q14='')
check('removed anxiety answer rejected',survey('submit',ticket,answers=dict(answers,q12='none'))[0],400)
check('text length answer remains required',survey('submit',ticket,answers={k:v for k,v in answers.items() if k!='q10'})[0],400)
check('invalid text length choice rejected',survey('submit',ticket,answers=dict(answers,q10='6'))[0],400)
check('unknown answer fields rejected',survey('submit',ticket,answers=dict(answers,participantId=uid()))[0],400)
check('removed skip choice rejected',survey('submit',ticket,answers=dict(answers,q1='skip'))[0],400)
check('301 characters in first free answer rejected',survey('submit',ticket,answers=dict(answers,q13='あ'*301))[0],400)
check('301 characters in second free answer rejected',survey('submit',ticket,answers=dict(answers,q14='あ'*301))[0],400)
answers.update(q13='あ'*300,q14='い'*300)
_,second=survey('issue',auth['access_token'],sessionId=session,runId=run)
with ThreadPoolExecutor(2) as pool:
 replies=list(pool.map(lambda value:survey('submit',value,answers=answers),[ticket,second['ticket']]))
check('parallel submissions succeed', [r[0] for r in replies],[200,200])
check('parallel submissions share one receipt',replies[0][1]['receiptId'],replies[1][1]['receiptId'])
check('one response per completed run',sql(f"select count(*) from survey_responses where run_id='{run}'"),'1')
check('response matches anonymous game identity',sql(f"select participant_id::text from survey_responses where run_id='{run}'"),participant)
check('reopening recognizes submitted response',survey('open',ticket)[1]['submitted'],True)
survey('submit',ticket,answers=dict(answers,q1='1'))
check('retry never replaces first answers',sql(f"select answers->>'q1' from survey_responses where run_id='{run}'"),'4')
check('direct response table access denied',req('/rest/v1/survey_responses?select=id',token=auth['access_token'])[0],403)
sql(f"update survey_tickets set expires_at=now()-interval '1 second' where token_hash='{hashlib.sha256(ticket.encode()).hexdigest()}'")
check('expired link rejected',survey('open',ticket)[0],403)
sql(f"update study_participants set withdrawn_at=now() where id='{participant}'")
check('withdrawn participant ticket rejected',survey('open',second['ticket'])[0],403)
sql(f"update study_participants set withdrawn_at=null where id='{participant}'")
# A still-valid legacy ticket must keep its original 14-question contract.
legacy_ticket=uuid.uuid4().hex+uuid.uuid4().hex
legacy_hash=hashlib.sha256(legacy_ticket.encode()).hexdigest()
ticket_hash=hashlib.sha256(ticket.encode()).hexdigest()
sql(f"insert into survey_tickets(token_hash,participant_id,study_id,session_id,run_id,survey_version) select '{legacy_hash}',participant_id,study_id,session_id,run_id,'post-game-ja-v1' from survey_tickets where token_hash='{ticket_hash}'")
check('legacy ticket retains its version',survey('open',legacy_ticket)[1]['surveyVersion'],'post-game-ja-v1')
check('legacy anxiety answer still required',survey('submit',legacy_ticket,answers=answers)[0],400)
check('legacy anxiety values still validated',survey('submit',legacy_ticket,answers=dict(answers,q12='3'))[0],400)
check('legacy questionnaire can still submit',survey('submit',legacy_ticket,answers=dict(answers,q12='none'))[0],200)
check('revisions have separate response records',sql(f"select count(distinct survey_version) from survey_responses where run_id='{run}'"),'2')
check('new response has no deleted answer',sql(f"select (not (answers ? 'q12'))::text from survey_responses where run_id='{run}' and survey_version='post-game-ja-v2'"),'true')
# Fresh, unfinished questionnaire for the browser test. Create its record through the real ingest API.
browser_run=uid();payload['progressSnapshot']['eventId']=uid();payload['progressSnapshot']['payload']['runId']=browser_run
req('/functions/v1/game-ingest-v2',payload,auth['access_token'])
_,browser_link=survey('issue',auth['access_token'],sessionId=session,runId=browser_run)
out.mkdir(parents=True,exist_ok=True)
(out/'fixture.json').write_text(json.dumps(dict(url=url,key=key,code=code,participant=participant,study=study,run=browser_run,session=session,ticket=browser_link['ticket'],auth=auth),indent=2))
(out/'http-results.json').write_text(json.dumps(dict(passed=True,checks=checks),indent=2))
print(f'{len(checks)} real Supabase survey checks passed')
