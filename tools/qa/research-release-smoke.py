"""Explicit remote release QA using only a separately issued release-qa batch.

Requires --config and --register; never consumes the participant distribution batch.
Private tokens stay beside the ignored output fixture. Synthetic completion is
explicitly labelled; the Unity/browser checks cover the real UI separately.
"""
import argparse
import datetime as dt
import importlib.util
import json
from pathlib import Path
import urllib.error
import urllib.request
import uuid

parser = argparse.ArgumentParser()
parser.add_argument('--config', required=True)
parser.add_argument('--register', required=True, help='Private JSON manifest from create-batch')
parser.add_argument('--output', required=True)
args = parser.parse_args()
root = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('research_admin', root/'tools/research-admin.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
admin = module.Admin(args.config)
batch = json.loads(Path(args.register).read_text())
assert batch['cohort']=='release-qa' and batch['study_key'].startswith('release-qa-')
assert batch['backend_url']==admin.url and batch['mode']=='development'
assert admin.study(batch['study_key'])['status']=='development'
output = Path(args.output)
fixture = json.loads(output.read_text()) if output.exists() else {}
key = admin.config['publishable_key']
checks = []


def request(path, body, token=None):
    req = urllib.request.Request(admin.url+path, data=json.dumps(body).encode(),
        headers={'apikey':key,'Authorization':'Bearer '+(token or key),'Content-Type':'application/json'})
    try:
        with urllib.request.urlopen(req,timeout=45) as response:
            return response.status, json.load(response)
    except urllib.error.HTTPError as error:
        try: payload=json.load(error)
        except ValueError: payload={}
        return error.code,payload


def check(name, actual, expected):
    assert actual==expected, (name,actual,expected)
    checks.append(name)


def persist(): module.private_write(output,json.dumps(fixture,indent=2))
def uid(): return str(uuid.uuid4())
def survey(action,token,**extra): return request('/functions/v1/game-survey',dict(action=action,**extra),token)


for i in range(2):
    name='auth'+str(i)
    if name not in fixture:
        http,auth=request('/auth/v1/signup',{'data':{'purpose':'geomodel-release-qa'}})
        check('anonymous authentication '+str(i),http,200)
        fixture[name]=auth
        persist()
    else:
        http,auth=request('/auth/v1/token?grant_type=refresh_token',{'refresh_token':fixture[name]['refresh_token']})
        check('anonymous refresh '+str(i),http,200)
        fixture[name]=auth
        persist()
auth,other=fixture['auth0'],fixture['auth1']
participant=batch['participants'][0]['participant_id']
code=batch['participants'][0]['participation_code']
activate=lambda value,token:request('/functions/v1/research-participation',{'participantCode':value},token)
check('unknown invitation rejected',activate('QA-INVALID-INVITE',auth['access_token'])[0],404)
http,context=activate(code,auth['access_token'])
check('invitation accepted',http,200)
check('invitation binds expected participant',context.get('participantId'),participant)
check('same identity re-enters',activate(code,auth['access_token'])[0],200)
check('different identity cannot claim code',activate(code,other['access_token'])[0],409)
check('second invitation accepted',activate(batch['participants'][1]['participation_code'],other['access_token'])[0],200)
session,run,install=uid(),uid(),uid()
now=dt.datetime.now(dt.timezone.utc).isoformat()
binding=dict(participantId=participant,studyId=batch['study_id'],condition=batch['condition'],sessionId=session)
payload=dict(binding,installId=install,protocolVersion=batch['protocol'],gameVersion='release-qa',
    platform='Editor',buildTarget='StandaloneOSX',language='Japanese',currentScene='Laboratory Scene',
    contentVersion='release-qa',storyRoute='story-lab-analysis-v2',
    events=[dict(binding,id=uid(),name='session_started',occurredAt=now,props={'fixture':'release-qa'})],
    quizAttempts=[dict(binding,eventId=uid(),runId=run,questionId='q.weathering_order',
        questionVersion='story-formative-v1',choiceId='qa',attemptIndex=1,isCorrect=True,
        usedHint=False,responseTimeMs=1200,occurredAt=now,gameVersion='release-qa',
        contentVersion='release-qa',storyRoute='story-lab-analysis-v2')])
ingest=lambda body,token=auth['access_token']:request('/functions/v1/game-ingest-v2',body,token)
check('participant-bound log accepted',ingest(payload)[0],200)
check('log retry accepted',ingest(payload)[0],200)
rows=admin.rows('quiz_attempts',{'event_id':'eq.'+payload['quizAttempts'][0]['eventId'],'select':'event_id'})
check('retry writes one answer',len(rows),1)
check('cross-player log rejected',ingest(payload,other['access_token'])[0],403)
check('unfinished game cannot issue survey',survey('issue',auth['access_token'],sessionId=session,runId=run)[0],409)
payload['events']=[];payload['quizAttempts']=[]
payload['progressSnapshot']=dict(binding,eventId=uid(),currentScene='Laboratory Scene',updatedAt=now,
    payload=dict(runId=run,investigationComplete=True,fixture='synthetic-release-qa'))
check('synthetic completed-run snapshot accepted',ingest(payload)[0],200)
check('other player cannot issue survey',survey('issue',other['access_token'],sessionId=session,runId=run)[0],403)
check('run substitution rejected',survey('issue',auth['access_token'],sessionId=session,runId=uid())[0],409)
http,issued=survey('issue',auth['access_token'],sessionId=session,runId=run)
check('scoped survey ticket issued',http,200)
ticket=issued['ticket']
check('ticket opens own survey',survey('open',ticket)[1].get('submitted'),False)
check('invalid ticket rejected',survey('open','f'*64)[0],403)
check('editable identity rejected',survey('open',ticket,participantId=uid())[0],400)
check('required answers validated',survey('submit',ticket,answers={})[0],400)
answers={f'q{i}':'4' for i in range(1,12)}
answers.update(q12='none',q13='リリース動作確認用の回答です。',q14='')
check('identity in answer body rejected',survey('submit',ticket,answers=dict(answers,participantId=uid()))[0],400)
http,receipt=survey('submit',ticket,answers=answers)
check('answer submission acknowledged',http,200)
_,repeat=survey('submit',ticket,answers=dict(answers,q1='1'))
check('duplicate submission returns same receipt',repeat.get('receiptId'),receipt.get('receiptId'))
rows=admin.rows('survey_responses',{'run_id':'eq.'+run,'select':'*'})
check('one immutable response',len(rows),1)
check('response linked to invitation',rows[0]['participant_id'],participant)
check('response linked to game session',rows[0]['session_id'],session)
check('retry cannot overwrite answer',rows[0]['answers']['q1'],'4')
payload['sessionEnd']={'endedAt':now,'reason':'teaching_completed'}
check('session end acknowledged',ingest(payload)[0],200)
# Leave a separate fresh ticket for the actual browser questionnaire test.
browser_run=uid()
payload.pop('sessionEnd')
payload['progressSnapshot']['eventId']=uid()
payload['progressSnapshot']['payload']['runId']=browser_run
check('browser fixture snapshot accepted',ingest(payload)[0],200)
http,browser=survey('issue',auth['access_token'],sessionId=session,runId=browser_run)
check('browser fixture ticket issued',http,200)
fixture.update(url=admin.url,key=key,ticket=browser['ticket'],participant=participant,
    study=batch['study_id'],run=browser_run,session=session,checks=checks,
    http_run=run,http_receipt=receipt['receiptId'])
persist()
module.private_write(output.with_name('remote-http-results.json'),json.dumps({'passed':True,'checks':checks},indent=2))
print(f'{len(checks)} remote release checks passed; distribution invitations were not used.')
