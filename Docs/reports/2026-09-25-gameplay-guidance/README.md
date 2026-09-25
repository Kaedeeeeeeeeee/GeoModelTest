# Gameplay guidance release

Version: `2026.09.25-gameplay-guidance` · Unity `6000.0.51f1`.

## Changes

- Hammer and drilling-tower collection show the next action, including tool selection, placement, progress and sample pickup. Desktop and touch instructions follow their actual controls.
- Story illustrations show a magnifier and click/tap hint, with a visible close button in the enlarged view. The view blocks gameplay input until closed.
- All 11 formative questions use the same authored choice order for every player, language, retry and reload. Stable question/choice IDs and scoring are retained. See [fixed choice order](../../quiz-fixed-choice-order.md).
- When the investigation step displayed in the upper-left task card changes, a mint highlight, slight scale pulse and arrow guide attention for about 3.2 seconds. The cue waits for dialogue/menus to close and does not block input. Repeated refreshes of the same displayed objective do not retrigger it; completion has its own label.
- Fixed immediate touch pickup on the final hammer press, tool-switch animation access to destroyed objects, missing dust material, long tool-name clipping and touch-layout overlap.

## Validation

- Release EditMode: 76 passed, 0 failed.
- Release PlayMode: 47 passed, 0 failed; 2 opt-in scene capture tests skipped in this run and separately passed beforehand.
- Earlier full MainScene acceptance: 1 passed with 45 checkpoints, including physical hammer sampling, tower drilling, sample inventory, enlarged image controls, wrong/correct quiz answers and virtual touch-button flows. See `full-scene.xml` and `acceptance-checks.txt`.
- Earlier task cue acceptance: 4 passed, including an actual quest transition in MainScene, duplicate suppression, modal deferral and fade/size restoration. See `task-cue.xml`.
- Node regression: 11 passed across survey options and performance template tests.
- Three localization mirrors synchronized and checked; `git diff --check` passed.
- Local Unity PlayerPrefs restored and compared with the pre-test backup.

The scene acceptance uses prepared quest checkpoints and simulated desktop/touch controls. It is not a complete playthrough or a physical mobile-device test. Test XML omits console output.

## Publication

- Source commit: `2ca9079`; build commit: `ab5dd5a`. Both pushed to `origin/main`.
- Release build: 117 MB, 6 minutes 5 seconds, 0 BuildReport errors. StartScene remains the entry scene. See `build-summary.txt` and `build-provenance.json` (clean source revision).
- Butler recognizes `index.html` as the HTML5 launch target. Upload contains 19 files and is published to `kaedeeeeeeeeee/geo-model-geological-drilling-simulator:html5`.
- itch.io build `2014267` completed processing. The [public game page](https://kaedeeeeeeeeee.itch.io/geo-model-geological-drilling-simulator) points to `17462165-2014267` and displays the new version.
- All 19 published files match the release package. Ordinary files are byte-identical; Unity gzip resources match after decompression; the two `index.html` files differ only by itch.io's official `htmlgame.js` insertion. Checks use the normal asset URLs without cache-busting parameters. See `published-assets.json`.
- Ego Lite loaded the public build to `ready` at 1280×720 and verified the new version. The consent cancel button returned to the title menu with New Game still disabled. A subsequent ordinary reload also reached `ready`. See `online-runtime.json`, `online-consent.png` and `online-title.png`.
- The first main-data transfer was truncated at 71,363,417 bytes (expected 130,924,138 decompressed bytes), and Unity cached that incomplete response. A fresh download from the same public URL matched the complete local data. Only that test-created response and its matching UnityCache metadata were removed, after which startup and an ordinary reload succeeded. Game saves, consent state, cookies and other cache entries were not cleared. No republish or game-code change was needed; the exact cause of the interrupted transfer was not established.
- Screenshots are actual WebGL canvas exports; the verification page temporarily enabled `preserveDrawingBuffer` for capture. That setting is not part of the published package. No research consent was accepted, research session started or questionnaire submitted during this live smoke test. New feature flows were validated in the Unity scene tests above, not replayed end-to-end on the public site.
