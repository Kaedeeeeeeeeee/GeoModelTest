# Code-free research experience — 2026-09-14

Local source changes; not yet deployed to the public game or production database.

- The title screen contains New Game, Settings and Quit. Continue and code entry are removed. Existing-progress overwrite confirmation remains.
- New Game closes the previous session, resets the game run, and starts anonymous enrollment in the background. The opening does not wait for an available network.
- Every completed run can open the questionnaire. If initial enrollment failed, the questionnaire action retries enrollment without resetting completion or quiz answers, then synchronizes that run before requesting its scoped ticket.
- The new open-play study accepts anonymous game identities without invitation codes. Existing invitation data, consent rules, withdrawal and ownership checks remain intact. No consent metadata is fabricated.
- Questionnaire copy explains voluntary answering and anonymous linkage to this game run. The 14 questions and answer scale are unchanged.
- Open-play exports include actual submitted questionnaires only. Responses remain linked by participant and run; these are not verified unique students or recruitment-source identities.

## Verification

- Unity 6000.0.51f1 EditMode: 57/57 passed.
- Targeted PlayMode: 7/7 passed (ordinary-player questionnaire availability, report controls, connection recovery, code-free activation, preserving completion and recovering quiz answers).
- Local Supabase: 14 open-play SQL contracts and 14 legacy invitation/consent contracts passed.
- Real local HTTP flow: 27 open-play checks and 25 legacy invitation checks passed. Covers anonymous registration, unfinished/foreign-run denial, submission validation, concurrent duplicate receipt, expiry and withdrawal.
- Local administrator export verified against submitted response count; unsubmitted play records were excluded without a code register.
- Local security advisor: no warning/error findings. Localization and questionnaire generated-copy checks passed.
- Ego Lite verified the real questionnaire with an open-play test ticket: required answers, draft reload, five-page progression, failed offline submission retaining answers, successful retry acknowledged by the local backend. Native pointer input and both screenshot paths timed out; DOM-based actions were used for this portion.

- Development WebGL build succeeded (Unity 6000.0.51f1). The actual built title shows only New Game, Settings and Quit; clicking New Game enters the opening story and receives HTTP 200 from anonymous enrollment, with requests redirected to the isolated local backend.
- Playwright fallback captured the built player after Ego Lite screenshot timeouts. The first local static-server transfer exhausted a socket buffer; throttling the local test server resolved it. The successful run still logs existing missing-asset/script warnings and a favicon 404; this was a title/start-flow check, not a new full-game traversal.
- Restored the pre-test Unity PlayerPrefs. No production study, recruitment correspondence or public deployment was changed.

![Simplified title screen](screenshots/title.png)

![New Game enters the opening without a code](screenshots/new-game.png)

## Release order

1. Apply `supabase/migrations/20260914042127_open_play_research.sql` to the intended backend after reviewing its new `geoquest-open-play-2026-09` study.
2. Deploy the updated `research-participation` function. Existing ingestion and survey endpoints retain their ownership/completion rules.
3. Rebuild and publish the Unity/WebGL player, including the updated questionnaire assets.
4. Verify the published New Game → completed report → questionnaire path. The local validation build is not a production release.

Export after deployment:

```sh
python3 tools/research-admin.py export --study-key geoquest-open-play-2026-09 --output /private/research/respondents.csv
```

The existing Testee correspondence and recruitment materials have not been sent or rewritten by this change.
