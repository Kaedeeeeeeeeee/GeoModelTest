#!/usr/bin/env python3
"""Private research administration. Never place its config or invitation exports in Git."""
import argparse
import csv
import datetime as dt
import hashlib
import hmac
import json
import os
from pathlib import Path
import secrets
import sys
import urllib.error
import urllib.parse
import urllib.request
import uuid

ALPHABET = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789'
DEFAULT_CONFIG = Path.home()/'.config/geomodel-research/sendai-glab.json'


def private_write(path, content):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True, mode=0o700)
    temporary = path.with_suffix(path.suffix+'.tmp')
    fd = os.open(temporary, os.O_WRONLY | os.O_CREAT | os.O_TRUNC, 0o600)
    with os.fdopen(fd, 'w', encoding='utf-8', newline='') as stream:
        stream.write(content)
    temporary.replace(path)
    path.chmod(0o600)


def csv_text(rows, fields):
    import io
    stream = io.StringIO(newline='')
    writer = csv.DictWriter(stream, fieldnames=fields, extrasaction='ignore')
    writer.writeheader()
    for row in rows:
        # Treat participant free text as data when researchers open the CSV in Excel.
        writer.writerow({key: ("'"+value if isinstance(value,str) and value.startswith(('=','+','-','@','\t','\r')) else value)
                         for key,value in row.items()})
    return '\ufeff'+stream.getvalue()


class Admin:
    def __init__(self, config):
        self.config = json.loads(Path(config).expanduser().read_text())
        self.url = self.config['url'].rstrip('/')
        if not self.url.startswith('https://') and not self.url.startswith('http://127.0.0.1:'):
            raise ValueError('Use HTTPS for a remote backend.')

    def request(self, path, body=None, method=None, prefer=None):
        headers = {'apikey':self.config['service_role_key'], 'Authorization':'Bearer '+self.config['service_role_key']}
        if prefer: headers['Prefer'] = prefer
        data = None
        if body is not None:
            headers['Content-Type'] = 'application/json'
            data = json.dumps(body).encode()
        request = urllib.request.Request(self.url+'/rest/v1/'+path, data=data, headers=headers, method=method)
        try:
            with urllib.request.urlopen(request, timeout=45) as response:
                raw = response.read()
                return json.loads(raw) if raw else None
        except urllib.error.HTTPError as error:
            # Do not print request headers, invitation codes, or credentials.
            raise RuntimeError(f'Backend request failed (HTTP {error.code}). No credentials were printed.') from None

    def rows(self, table, filters):
        result = []
        offset = 0
        while True:
            page = self.request(table+'?'+urllib.parse.urlencode({**filters,'limit':500,'offset':offset}))
            result.extend(page)
            if len(page) < 500: return result
            offset += len(page)

    def study(self, key):
        rows = self.rows('studies', {'study_key':'eq.'+key,'select':'*'})
        if len(rows) != 1: raise ValueError('Study not found.')
        return rows[0]

    def create_batch(self, args):
        if not 1 <= args.count <= 200: raise ValueError('Count must be between 1 and 200.')
        output = Path(args.output).expanduser().resolve()
        manifest = output.with_suffix('.private.json')
        if manifest.exists():
            batch = json.loads(manifest.read_text())
            expected = (args.study_key,args.mode,args.protocol,args.condition,args.cohort,args.count,self.url)
            actual = tuple(batch[k] for k in ['study_key','mode','protocol','condition','cohort','count','backend_url'])
            if expected != actual: raise ValueError('Existing batch parameters differ; use a new output path.')
        else:
            if output.exists(): raise ValueError('Refusing to overwrite an existing CSV.')
            participants = []
            seen = set()
            for index in range(args.count):
                while True:
                    code = '-'.join(''.join(secrets.choice(ALPHABET) for _ in range(4)) for _ in range(3))
                    if code not in seen: break
                seen.add(code)
                participants.append({'number':index+1,'participation_code':code,'participant_id':str(uuid.uuid4())})
            batch = {'backend_url':self.url,'study_key':args.study_key,'mode':args.mode,'protocol':args.protocol,
                     'condition':args.condition,'cohort':args.cohort,'count':args.count,'participants':participants,
                     'created_at':dt.datetime.now(dt.timezone.utc).isoformat(),'state':'prepared'}
            private_write(manifest,json.dumps(batch,ensure_ascii=False,indent=2))
        studies = self.rows('studies',{'study_key':'eq.'+args.study_key,'select':'*'})
        if not studies:
            self.request('studies?on_conflict=study_key', {'study_key':args.study_key,'status':args.mode,
                'research_entry_enabled':True,'protocol_version':args.protocol}, prefer='resolution=ignore-duplicates')
        study = self.study(args.study_key)
        if study['status'] != args.mode or study['protocol_version'] != args.protocol:
            raise ValueError('Existing study mode/protocol differs from this batch.')
        records = []
        pepper = self.config['participant_code_pepper']
        if len(pepper) < 32: raise ValueError('The administrator code pepper is invalid.')
        for item in batch['participants']:
            records.append({'id':item['participant_id'],'study_id':study['id'],
                'participant_code_hash':hmac.new(pepper.encode(),item['participation_code'].encode(),hashlib.sha256).hexdigest(),
                'condition':args.condition,'protocol_version':args.protocol,'cohort':args.cohort})
        self.request('study_participants?on_conflict=id',records,prefer='resolution=ignore-duplicates')
        saved = {p['id']:p for p in self.rows('study_participants',{'study_id':'eq.'+study['id'],'select':'*'})}
        for record in records:
            actual = saved.get(record['id'],{})
            if any(actual.get(k) != value for k,value in record.items()):
                raise ValueError('Stored participant does not match the prepared batch.')
        rows = [{**item,'study_id':study['id'],'study_key':args.study_key,'condition':args.condition,'cohort':args.cohort}
                for item in batch['participants']]
        private_write(output,csv_text(rows,['number','participation_code','participant_id','study_id','study_key','condition','cohort']))
        batch.update(state='issued',study_id=study['id'])
        private_write(manifest,json.dumps(batch,ensure_ascii=False,indent=2))
        print(f'{len(rows)} invitation codes verified and saved to {output}. Codes were not printed.')
        if args.mode == 'active': print('Active-study codes require recorded consent metadata before first use.')

    def export(self, args):
        study = self.study(args.study_key)
        participants = self.rows('study_participants',{'study_id':'eq.'+study['id'],'select':'*','order':'created_at,id'})
        register = {}
        if args.register:
            with Path(args.register).expanduser().open(encoding='utf-8-sig',newline='') as stream:
                register = {row['participant_id']:row['participation_code'] for row in csv.DictReader(stream)}
        rows = []
        for participant in participants:
            pid = participant['id']
            sessions = self.rows('game_sessions',{'participant_id':'eq.'+pid,'select':'id,started_at,ended_at','order':'id'})
            events = self.rows('telemetry_events',{'participant_id':'eq.'+pid,'select':'id','order':'id'})
            responses = self.rows('survey_responses',{'participant_id':'eq.'+pid,'select':'*','order':'submitted_at,id'})
            attempts = self.rows('quiz_attempts',{'participant_id':'eq.'+pid,'select':'run_id,question_id,attempt_index,is_correct','order':'event_id'})
            for response in responses or [{}]:
                same_run = [x for x in attempts if x['run_id']==response.get('run_id')]
                rows.append({'participation_code':register.get(pid,''),'participant_id':pid,'study_key':args.study_key,
                    'cohort':participant['cohort'],'condition':participant['condition'],'status':participant['status'],
                    'session_count':len(sessions),'event_count':len(events),'run_id':response.get('run_id',''),
                    'response_id':response.get('id',''),'completion_session_id':response.get('session_id',''),
                    'submitted_at':response.get('submitted_at',''),
                    'first_correct':len({x['question_id'] for x in same_run if x['attempt_index']==1 and x['is_correct']}),
                    'wrong_attempts':sum(not x['is_correct'] for x in same_run),
                    'mastered':len({x['question_id'] for x in same_run if x['is_correct']}),**response.get('answers',{})})
        fields=['participation_code','participant_id','study_key','cohort','condition','status','session_count','event_count',
                'run_id','response_id','completion_session_id','submitted_at','first_correct','wrong_attempts','mastered']+[f'q{i}' for i in range(1,15)]
        private_write(args.output,csv_text(rows,fields))
        print(f'Exported {len(rows)} participant/response rows to {Path(args.output).expanduser().resolve()}.')


def timestamp(value):
    parsed = dt.datetime.fromisoformat(value.replace('Z','+00:00'))
    if parsed.tzinfo is None or parsed > dt.datetime.now(dt.timezone.utc):
        raise ValueError('Consent timestamps must include a timezone and must not be in the future.')
    return parsed.isoformat()


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--config',default=str(DEFAULT_CONFIG))
    commands=parser.add_subparsers(dest='command',required=True)
    create=commands.add_parser('create-batch')
    create.add_argument('--study-key',required=True);create.add_argument('--count',type=int,default=20)
    create.add_argument('--mode',choices=['development','active'],default='development')
    create.add_argument('--protocol',default='research-v1');create.add_argument('--condition',default='game')
    create.add_argument('--cohort',default='internal');create.add_argument('--output',required=True)
    export=commands.add_parser('export');export.add_argument('--study-key',required=True)
    export.add_argument('--register');export.add_argument('--output',required=True)
    gate=commands.add_parser('entry');gate.add_argument('--study-key',required=True)
    gate.add_argument('--state',choices=['open','closed'],required=True)
    consent=commands.add_parser('record-consent');consent.add_argument('--participant-id',required=True)
    consent.add_argument('--consent-version',required=True);consent.add_argument('--guardian-at',type=timestamp,required=True)
    consent.add_argument('--student-at',type=timestamp,required=True)
    args=parser.parse_args(); admin=Admin(args.config)
    if args.command=='create-batch': admin.create_batch(args)
    elif args.command=='export': admin.export(args)
    elif args.command=='entry':
        study=admin.study(args.study_key)
        admin.request('studies?id=eq.'+study['id'],{'research_entry_enabled':args.state=='open'},method='PATCH')
        assert admin.study(args.study_key)['research_entry_enabled']==(args.state=='open')
        print('Study entry is '+args.state+'.')
    elif args.command=='record-consent':
        pid=str(uuid.UUID(args.participant_id))
        if not 1<=len(args.consent_version.strip())<=64: raise ValueError('Invalid consent version.')
        body={'consent_version':args.consent_version.strip(),'guardian_consent_at':args.guardian_at,'student_assent_at':args.student_at}
        result=admin.request('study_participants?id=eq.'+pid,body,method='PATCH',prefer='return=representation')
        if not result: raise ValueError('Participant not found.')
        print('Researcher-provided consent metadata recorded. No consent documents were uploaded.')


if __name__=='__main__':
    try: main()
    except (RuntimeError,ValueError,OSError) as error:
        print(str(error),file=sys.stderr);sys.exit(1)
