# Supabase research backend v2

This backend is opt-in research telemetry for the Unity/WebGL game. Ordinary play remains local and does not authenticate with Supabase or upload gameplay data.

## Safety boundary

- The production research entry is disabled in `Assets/Resources/BackendSettings.asset`.
- `research-participation` currently accepts only studies whose status is `development` and whose `research_entry_enabled` flag was explicitly enabled by an administrator.
- This development entry is for internal testing only. It is not a substitute for guardian consent, student assent, or ethics approval.
- Never store names, school identifiers, contact details, SNS accounts, raw participation codes, or signed consent forms in these tables.

## Deploy

1. Enable anonymous sign-in in Supabase Authentication.
2. Apply all migrations in `supabase/migrations` in timestamp order.
3. Set server-only secrets. Supabase supplies `SUPABASE_SERVICE_ROLE_KEY` automatically to hosted Edge Functions; never copy it into the game or questionnaire. Use a randomly generated pepper of at least 32 characters:

   ```bash
   supabase secrets set PARTICIPANT_CODE_PEPPER=...
   ```

4. Deploy the Edge Functions. The survey endpoint implements its own authentication: a verified game JWT for issuance and a scoped, expiring ticket for answering.

   ```bash
   supabase functions deploy research-participation
   supabase functions deploy game-ingest
   supabase functions deploy game-survey --no-verify-jwt
   ```

5. Configure Supabase Cron to run the following SQL once per minute. A session with no heartbeat for 120 seconds is then marked as ended:

   ```sql
   select public.infer_stale_research_sessions(now() - interval '120 seconds');
   ```

6. After applying migrations to the local stack, run the database contract tests:

   ```bash
   supabase test db --local supabase/tests/research_foundation_v2_test.sql
   ```

## Create an internal development participant

Create a high-entropy random participation code outside the database. Normalize it to uppercase, compute `HMAC-SHA256(PARTICIPANT_CODE_PEPPER, normalized_code)` in the restricted research administration environment, and store only the 64-character lowercase hex digest.

Create the study with a locked entry first, add the participant, and enable entry only for the test window:

```sql
insert into public.studies (
  study_key, status, research_entry_enabled, protocol_version, retention_until
) values (
  'internal-foundation-test', 'development', false, 'foundation-v2', now() + interval '30 days'
)
returning id;

insert into public.study_participants (
  study_id, participant_code_hash, cohort, condition, protocol_version
) values (
  '<study uuid>', '<hmac sha256 hex>', 'internal', 'A', 'foundation-v2'
);

update public.studies
set research_entry_enabled = true
where study_key = 'internal-foundation-test' and status = 'development';
```

Disable entry immediately after the internal test:

```sql
update public.studies
set research_entry_enabled = false
where study_key = 'internal-foundation-test';
```

## Data flow

1. A development tester explicitly opens the research connection entry and enters a participation code.
2. Unity signs in anonymously and sends the code only to `research-participation` over TLS.
3. The Edge Function HMACs the normalized code, rate-limits attempts, and atomically binds the pseudonymous participant to the anonymous auth user.
4. Only after successful validation does Unity start a research session and enqueue events, quiz attempts, heartbeats, and progress checkpoints.
5. `game-ingest` validates real request bytes, IDs, lengths, integer ranges, client times, and research bindings.
6. One database RPC writes the session, events, quiz attempts, current progress, and progress history in a short transaction. Stable event UUIDs make retries idempotent.

Direct access for `anon` and `authenticated` remains revoked. Only the Edge Functions use `service_role` to execute the restricted RPCs.

## Post-game questionnaire

`20260910150016_post_game_survey.sql` requires the research foundation v2 migration. Apply it after the existing migrations, then deploy `game-survey`. Do not replace an active v1 game-ingest deployment without coordinating the client release: the v2 endpoint requires participant-bound requests.

The static Japanese questionnaire is bundled at `Assets/StreamingAssets/Survey` and ships in every player build. `SurveySettings.asset` can override its page URL for a separately hosted HTTPS page. `config.js` contains only the public Edge Function URL; no API secret belongs in these assets. Keep its endpoint aligned with BackendSettings. For native builds, the bundled page uses an absolute file URL; for WebGL it uses the build's StreamingAssets URL.

The game first acknowledges all pending telemetry and its completed-run snapshot, then obtains a 24-hour random ticket. The server binds that ticket to the participant, study, session, run and questionnaire version. The game ends the research session and completes persistent saving before leaving. The page scrubs the ticket fragment and keeps its draft in sessionStorage for the current tab. No participant identifier is accepted from editable form fields.

`survey_responses` permits one immutable submission per participant/run/version. The page reports success only after a server acknowledgement. `survey-export.sql` is a read-only administrator export with q1–q14 plus run-level quiz metrics. Match `participant_id` against the separately secured invitation issuance register to correlate distributed codes. Do not expose service credentials or this export to survey respondents.

Edit `questions.json`, then run `python3 tools/sync-survey.py` to update its browser bundle. Any changes to IDs, accepted values or question meaning require corresponding server validation and a new questionnaire version; never reinterpret previously collected answers in place.
