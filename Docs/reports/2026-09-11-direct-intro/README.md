# Direct opening story

Starting a new game now plays the classroom opening directly. Removed the disaster notice dialog, its watch/skip branch, the corresponding decision telemetry, and unused localization entries. The three opening lines still lead to the laboratory sequence; existing story completion flags remain compatible.

Published to [itch.io](https://kaedeeeeeeeeee.itch.io/geo-model-geological-drilling-simulator) as `2026.09.11-direct-intro`, active HTML5 build **1965690**. Source commit: `15a811e`.

Validation: 7 localization EditMode tests passed. Unity 6000.0.51f1 release WebGL build succeeded with 0 errors; Butler validation passed. Mobile Chromium emulation at 844 × 390 confirmed a fresh game starts without the notice, touch advances all three opening lines, and the story transitions to the laboratory. The deployed CDN player was then loaded with its new version and checked through the first opening dialogue. No JavaScript page errors or checked C# runtime exceptions occurred in those flows. The public itch page points to the new build and its deployed HTML/manifest match the package. These are browser emulation checks, not physical iPhone tests.

The Japanese portrait orientation prompt is retained in the rebuilt package.

## Opening directly (deployed player)

![The first opening line appears without the notice](01-direct-opening.png)

## Laboratory transition (local release player)

![The opening still leads to the laboratory dialogue](02-laboratory.png)
