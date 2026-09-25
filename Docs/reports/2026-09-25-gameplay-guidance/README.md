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

Build and live verification details will be recorded here after publication to the existing itch.io HTML5 channel.
