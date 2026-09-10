# Remediation QA

These tools run only on a local development project. They never publish a build or target the production research database.

- `unity_command.py`: send an explicit command to the existing local Unity MCP bridge.
- `review_command.py` / `review_api.py`: drive the editor-only `RemediationReview` command queue while Play mode is active. The `reset`, `prepare_core`, `core`, `report`, and `long_dialogue` commands use test fixtures and can replace local learning progress. Back up the project's Unity PlayerPrefs and persistent data first.
- `review_story.py`: operate the actual dialogue UI; `--wrong-once` checks correction scoring and `--stop-at-quiz` pauses at a question. The reviewed story currently uses choice 0 as the correct answer; update the driver if the content changes.
- `research-smoke.py`: real local Supabase HTTP checks. It explicitly accepts only `http://127.0.0.1:55321` in the isolated remediation project. Local service credentials are never printed.
- `webgl-bridge-test.py`: Chromium checks of the actual `.jslib`, including queued persistence and a restricted iframe. The destination page is intercepted locally; this test does not navigate to a real published game.

Unity tests are under `Assets/Tests/EditMode/Backend` and `Assets/Tests/PlayMode`. `ResearchRecoveryTests` uses a local in-process HTTP stub to exercise the actual Unity client through connection failure and recovery; pgTAP and HTTP smoke tests separately verify the real local Supabase implementation.

`RemediationReview.BuildValidation` builds the enabled scenes into `Build/RemediationWebGL` as a development player without changing the production research-entry configuration.

Localization: edit `Assets/Resources/Localization/Data/*.json`, then run `python3 tools/sync-localization.py`. The `Assets/Scripts/Localization/Data` files are generated copies for existing editor tooling. Use `--check` to verify identical copies and unique keys. Existing untranslated legacy-story placeholders are preserved; the new UI keys are populated in all three languages.

- `webgl-runtime-test.py`: actual development WebGL player checks on macOS Chromium with ANGLE Metal. The default run uses no player test hooks; optional `QA_SYNC_DEBUG=1` instruments filesystem callback logging in the served framework.
- `build-review-report.py`: generates the local screenshot gallery and Markdown report from `items.json` and `verification.md`.

For editor research UI tests, apply `backend_local` before each new login because unused Resources assets may be reloaded after a scene transition. Local test settings are never written to the BackendSettings asset.

- `review-speaker.py`: while the editor is playing in the Laboratory Scene, exercise the actual coral question in Japanese, English, and Chinese. Checks prompt/hint/wrong/correct speaker visibility and persisted history attribution (15 checks). Uses reset test progress; back up and restore PlayerPrefs and persistent data.

- `survey-smoke.py`: 24 real local Supabase questionnaire checks; also creates a fresh, unsubmitted browser fixture in ignored logs.
- `serve-survey.py --local-backend`: serve the actual bundled page at port 55883 with a local API configuration supplied in memory.
- `survey-browser.py`: 9 browser checks against that real backend; consumes the fresh fixture and saves desktop/mobile screenshots.
- `survey-bridge.py`: test the actual WebGL survey navigation bridge after transient user activation expires, without popup permission.
- Editor command `survey_local` reads the isolated survey fixture, temporarily overrides backend and questionnaire settings, and captures the game's issued URL to ignored logs. No settings asset is saved. Restore PlayerPrefs and persistent data after editor QA.

- `itch-release-review.py --url <published game page> --expect-version <version>`: operate the actual published itch.io embed in an isolated Chromium profile. `itch-review-command.py` sends explicit JSON commands for screenshots, canvas clicks, keys, state and reload. Commands/results/profile remain in ignored release logs. Canvas clicks scroll into view before dispatch; itch.io may auto-start on reload, so the driver supports both the Run button and an already-created game iframe.
