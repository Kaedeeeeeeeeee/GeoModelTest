#!/usr/bin/env python3
"""Local-only HTTP smoke test. Requires the isolated remediation Supabase stack."""
import datetime
import hashlib
import hmac
import json
from pathlib import Path
import subprocess
import urllib.error
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[2]
WORK = ROOT / "Logs/remediation/backend-local"
status = json.loads(subprocess.check_output(["supabase", "status", "--workdir", str(WORK), "-o", "json"], stderr=subprocess.DEVNULL))
URL = status["API_URL"]
assert URL == "http://127.0.0.1:55321", "Only the isolated local stack is allowed"
KEY = status["ANON_KEY"]
PEPPER = "local-remediation-only-pepper-at-least-32-chars"
CONTAINER = "supabase_db_geomodel-remediation"
results = []

def sql(query):
    return subprocess.check_output(["docker", "exec", "-i", CONTAINER, "psql", "-U", "postgres", "-d", "postgres", "-At", "-v", "ON_ERROR_STOP=1"], input=query.encode()).decode().strip()

def request(path, body, token=None):
    req = urllib.request.Request(URL + path, data=json.dumps(body).encode(), headers={"Content-Type":"application/json", "apikey":KEY, "Authorization":"Bearer " + (token or KEY)})
    try:
        with urllib.request.urlopen(req, timeout=30) as res: return res.status, json.load(res)
    except urllib.error.HTTPError as error:
        return error.code, json.load(error)

def check(name, actual, expected):
    assert actual == expected, (name, actual, expected)
    results.append({"name":name, "result":"passed"})

def uid(): return str(uuid.uuid4())

study, participant, closed_study, closed_participant = [uid() for _ in range(4)]
code, closed_code = "REVIEW-" + uuid.uuid4().hex[:12].upper(), "CLOSED-" + uuid.uuid4().hex[:12].upper()
hash_code = lambda value: hmac.new(PEPPER.encode(), value.encode(), hashlib.sha256).hexdigest()
sql(f"""insert into studies(id,study_key,status,research_entry_enabled,protocol_version) values
('{study}','smoke-{study}','development',true,'protocol-v1'),
('{closed_study}','smoke-{closed_study}','locked',false,'protocol-v1');
insert into study_participants(id,study_id,participant_code_hash,condition,protocol_version,status) values
('{participant}','{study}','{hash_code(code)}','A','protocol-v1','ready'),
('{closed_participant}','{closed_study}','{hash_code(closed_code)}','A','protocol-v1','ready');""")
http, auth = request("/auth/v1/signup", {"data":{"purpose":"local-remediation-test"}})
check("anonymous sign-in", http, 200)
token = auth["access_token"]
activate = lambda value, identity=token: request("/functions/v1/research-participation", {"participantCode":value,"gameVersion":"qa","contentVersion":"qa","storyRoute":"qa"}, identity)
check("invalid code rejected", activate("INVALID-CODE")[0], 404)
check("closed study rejected", activate(closed_code)[0], 403)
http, context = activate(code)
check("valid code accepted", http, 200)
check("bound participant matches", context["participantId"], participant)
check("same identity can re-enter", activate(code)[0], 200)
http, refreshed = request("/auth/v1/token?grant_type=refresh_token", {"refresh_token":auth["refresh_token"]})
check("token refresh", http, 200)
check("refresh preserves identity", refreshed["user"]["id"], auth["user"]["id"])
check("refreshed identity can re-enter", activate(code, refreshed["access_token"])[0], 200)
_, another = request("/auth/v1/signup", {"data":{"purpose":"local-remediation-second-device"}})
check("other identity cannot claim code", activate(code, another["access_token"])[0], 409)
session, install, event, attempt, run = [uid() for _ in range(5)]
now = datetime.datetime.now(datetime.timezone.utc).isoformat()
binding = dict(participantId=participant, studyId=study, condition="A", sessionId=session)
payload = dict(binding, installId=install, protocolVersion="protocol-v1",gameVersion="qa",platform="Editor",buildTarget="StandaloneOSX",language="Japanese",currentScene="MainScene",contentVersion="qa",storyRoute="qa",events=[dict(binding,id=event,name="session_started",occurredAt=now,sceneName="MainScene",props={})],quizAttempts=[dict(binding,eventId=attempt,runId=run,questionId="q.weathering_order",questionVersion="story-formative-v1",choiceId="wrong",attemptIndex=1,isCorrect=False,usedHint=False,responseTimeMs=1200,occurredAt=now,gameVersion="qa",contentVersion="qa",storyRoute="qa")])
for label in ["ingest accepts bound events", "duplicate retry accepted"]:
    check(label, request("/functions/v1/game-ingest", payload, refreshed["access_token"])[0], 200)
check("duplicate retry stores one answer", sql(f"select count(*) from quiz_attempts where event_id='{attempt}';"), "1")
check("cross-user ingest rejected", request("/functions/v1/game-ingest", payload, another["access_token"])[0], 403)
payload["events"], payload["quizAttempts"] = [], []
payload["sessionEnd"] = {"endedAt":now,"reason":"teaching_completed"}
check("session end accepted", request("/functions/v1/game-ingest", payload, refreshed["access_token"])[0], 200)
check("re-entry after session end", activate(code, refreshed["access_token"])[0], 200)
(ROOT / "Logs/remediation/backend-http-results.json").write_text(json.dumps(results, indent=2))
print(f"{len(results)} local HTTP checks passed; no production database accessed.")
