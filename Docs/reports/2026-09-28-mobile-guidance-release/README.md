# Mobile guidance release

Version: `2026.09.28-mobile-guidance` · Unity `6000.0.51f1`.

## Changes

- Touch NPC prompts and the controls guide show the actual 「調べる」 button artwork with "tap the matching button on the right"; desktop keeps the E prompt.
- First tool use on touch: the collection hint card has a tappable 「道具」 entry that opens the tool wheel with the recommended tool highlighted, and the real tools button glows until the tool is equipped.
- Fullscreen: the first touch requests fullscreen on mobile browsers that support it. iPhone Safari without the Fullscreen API shows a dismissible hide-toolbar hint instead. The legacy iPad fullscreen exit was removed, and a user's exit from fullscreen is respected.
- Memory: encyclopedia images and models load on demand, terrain layers keep their shared source mesh, and the model preview releases its RenderTexture safely.
- Release builds now always use the phone-verified size settings (IL2CPP OptimizeSize, DiskSizeLTO; wasm 38 MB instead of 67 MB) and a 384 MB initial WebGL heap, so the heap no longer grows during play.
- The encyclopedia is hidden for this release (`ResearchExperienceSettings.EncyclopediaEnabled` is off): no 図鑑 button, the tools button takes its slot, the O key is ignored and the controls guide no longer mentions it. Collection tracking keeps running.

## Validation

- Release EditMode: 76 passed. Release PlayMode: 62 passed, 3 opt-in scene tests skipped in that run; the opt-in full MainScene acceptance passed separately with 61 checkpoints, including the hidden encyclopedia and the moved tools button. Node regression: 23 passed. Localization mirrors synchronized. See the XML files and `acceptance-checks.txt`.
- Physical iPhone X (iOS 16.7.16, Safari) with this exact package: New Game → MainScene → laboratory → field, the NPC 「調べる」 dialogue, the hint-card tool entry, the recommended hammer, three hits and sample pickup, all by native touch. No page reload and no WebContent Jetsam in the test window. Peak page memory was 1,313 MiB, down from 1,507 MiB with the former 64 MB initial heap; the lowest observed kill point is about 1,536 MiB. See [the device report](../2026-09-28-memory-optimization.md).
- Not covered: tablets and other phones, devices with real fullscreen support, and the drilling tower, drone, drilling car and laboratory microscope on a phone.

## Publication

- Source commit: `67763ef`; build commit: `e7980d9`.
- The published package is byte-identical to the one verified on iPhone X. It was built before the source commit, from a working tree whose build inputs equal `67763ef`, so `build-provenance.json` records the tree as dirty.
- Butler pushed 20 files (114.11 MiB) to `kaedeeeeeeeeee/geo-model-geological-drilling-simulator:html5` as `2026.09.28-mobile-guidance`. itch.io build `2030265` finished processing and the [public game page](https://kaedeeeeeeeeee.itch.io/geo-model-geological-drilling-simulator) embed resolves to `17462165-2030265`.
- All 20 published files match the release package: 15 byte-identical, the three Unity gzip resources identical after decompression, and both `index.html` files differ only by itch.io's `htmlgame.js` insertion. Normal asset URLs were used without cache-busting parameters. See `published-assets.json`.
- In the Claude desktop built-in browser the public build loaded to `ready` at 1280×720 with the new version and `fullscreen.js`. The consent 「戻る」 button returned to the title menu with New Game still disabled, and an ordinary reload reached `ready` again. No research consent was accepted, no research session was started and no questionnaire was submitted. See `online-runtime.json`, `online-consent.jpg` and `online-title.jpg`.
