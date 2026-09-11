# Supabase research backend

The published Unity/WebGL client exposes an opt-in research entry. Ordinary play stays local: it does not sign in or upload gameplay data. Study eligibility is enforced by the backend at activation, log ingestion, questionnaire issuance and submission.

## Eligibility and identity

- `development` batches are for internal trials. They do not pretend that consent has been collected.
- `active` batches require matching protocol versions, a nonempty consent version, recorded guardian consent and student assent timestamps, and an open entry. Future timestamps, withdrawn participants and expired studies are rejected.
- The researcher records consent metadata after completing the actual consent process. Do not store signed forms, names, school identifiers or contact details in the game database.
- A code is bound atomically to one anonymous account. Re-entering from the same saved browser identity works. Clearing browser storage or changing devices requires researcher support; do not casually distribute one code to multiple players.
- The database stores only HMAC-SHA256 code hashes. The raw-code issuance register remains a private researcher file. Preserve the code pepper: changing it invalidates existing codes.

## Deployment

1. Enable anonymous sign-in in Supabase Auth. The existing Sendai project already has it enabled.
2. Back up schema and data before upgrading. Apply pending migrations with `supabase db push --project-ref PROJECT_REF --skip-vault`; use `--dry-run` first. Baseline migration filenames match the cloud's original history. Do not replay or reset those migrations.
3. Put `PARTICIPANT_CODE_PEPPER` (random, at least 32 characters) in a private environment file and run `supabase secrets set --project-ref PROJECT_REF --env-file PRIVATE_FILE`. Supabase supplies the service-role credential to hosted functions. Never place service credentials or the pepper in Unity assets, browser JavaScript or Git.
4. Deploy `research-participation`, `game-ingest-v2` and `game-survey` with `supabase functions deploy research-participation game-ingest-v2 game-survey --project-ref PROJECT_REF --use-api`. The committed config keeps gateway JWT checks enabled on activation/logging; the survey endpoint validates a game JWT for issuance and a scoped ticket for answering.
5. Leave the existing `game-ingest` v1 deployment intact for old clients. The new Unity asset explicitly calls `game-ingest-v2`.
6. The timeout migration schedules a once-per-minute Cron job. It closes abandoned research sessions after 120 seconds without a heartbeat, using the last heartbeat as the end time. Legacy sessions without a participant binding are excluded.
7. Build WebGL and deploy the complete build, including `StreamingAssets/Survey`. Verify the public itch embed against the deployed backend.

## Private administration

`tools/research-admin.py` uses a researcher-only JSON configuration. The default location is `~/.config/geomodel-research/sendai-glab.json`, outside the repository, with directory mode 700 and file mode 600. Required fields are `url`, `service_role_key`, `publishable_key` and `participant_code_pepper`. Never copy this configuration into a participant-facing page.

Create 20 internal codes:

```sh
python3 tools/research-admin.py create-batch --study-key internal-trial --count 20 --mode development --protocol research-v1 --condition game --cohort internal --output /private/research/invitations.csv
```

The command writes a private CSV and adjacent `.private.json` manifest. Retrying with the same path and parameters resumes/verifies the same batch without making new codes. Use a new output path for another batch. Codes contain three groups of four unambiguous random characters.

For actual participants, use a separate `--mode active` batch and meaningful cohort. After collecting consent, record the real dates with `record-consent --participant-id UUID --consent-version VERSION --guardian-at ISO_TIMESTAMP --student-at ISO_TIMESTAMP`. The command records metadata, not consent itself. Active codes cannot be used until these requirements are met.

Close or reopen a batch with `entry --study-key KEY --state closed` or `open`. Close the entry at the end of the trial. `retention_until` can define an access cutoff; it is not an automatic deletion schedule.

Export linked results:

```sh
python3 tools/research-admin.py export --study-key internal-trial --register /private/research/invitations.csv --output /private/research/results.csv
```

Each row identifies the code, participant, completed run and response, with q1–q14 and quiz scores from that exact run. `session_count` and `event_count` are totals for that participant across runs. Participants without a response are included. Free-text spreadsheet formulas are escaped. The export and issuance register are private research records.

## Data flow and questionnaire

1. The player chooses research participation and enters a distributed code. Unity signs in anonymously, then validates the code over TLS.
2. The server binds the participant to that anonymous identity. Only successful activation starts log collection.
3. `game-ingest-v2` validates ownership and payloads. A transaction stores sessions, events, quiz attempts and progress. Stable UUIDs make retries idempotent.
4. At completion, the questionnaire button first uploads pending logs and the completed-run snapshot. The server issues a random 24-hour ticket bound to the participant, study, session, run and questionnaire version.
5. The Japanese questionnaire bundled in `Assets/StreamingAssets/Survey` opens with that ticket. There is no editable participant field. The page removes the ticket from the displayed URL and keeps drafts in session storage.
6. The server accepts one immutable response per participant/run/version and derives identity from the ticket. Offline failures retain answers; success appears only after acknowledgment. A retry returns the original receipt without replacing answers.

A survey ticket is a bearer link: anyone given that link can answer that same questionnaire. It cannot select a different participant or read another player's data. Do not share links between children.

All research tables have RLS enabled and direct access revoked from `anon` and `authenticated`; only server functions/admin tools access them. The advisor's no-policy informational notices are intentional for these service-only tables. Legacy own-session policies and anonymous/passwordless account settings are separate from research data access; see the release report for the reviewed findings.

## Validation

- Database contracts: `supabase test db --local supabase/tests` after applying migrations locally.
- Local HTTP checks: `tools/qa/research-smoke.py` and `tools/qa/survey-smoke.py` require the isolated remediation stack and refuse remote targets.
- Explicit remote QA: `tools/qa/research-release-smoke.py --config PRIVATE_CONFIG --register QA_BATCH.private.json --output Logs/research-launch/remote-fixture.json`. It requires a separate development study whose key starts with `release-qa-` and cohort `release-qa`; never use the distribution batch. Completed runs created by this HTTP fixture are synthetic and labelled accordingly.
- `tools/qa/survey-browser.py` accepts a fresh QA fixture and page URL to test mobile layout, draft recovery, offline retry and real backend acknowledgment.
- Unity EditMode/PlayMode tests cover gates, ownership and retry state. The editor-only review driver can observe the real completion button separately; it is excluded from player builds.

Edit `questions.json`, then run `python3 tools/sync-survey.py` to update the browser bundle. Changing IDs, accepted values or question meaning requires corresponding server validation and a new questionnaire version. Never reinterpret existing responses in place.
