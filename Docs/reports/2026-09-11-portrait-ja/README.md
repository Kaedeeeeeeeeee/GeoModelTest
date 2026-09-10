# Japanese portrait orientation prompt

Changed the WebGL template and published HTML to show Japanese before the Unity player starts:

- 横向きで遊んでください
- スマートフォンを横向きにすると、ゲームを続けられます。

The description breaks after the comma, and the overlay declares `lang="ja"` for assistive technology. Future Unity builds inherit the same wording.

Published to [itch.io](https://kaedeeeeeeeeee.itch.io/geo-model-geological-drilling-simulator) as `2026.09.11-portrait-ja`, active HTML5 build **1965646**. This is an HTML patch to `2026.09.11-survey-ui`; all four Unity runtime assets retain their verified SHA-256 hashes, and the original runtime build metadata/cache version is preserved.

Verification: Butler validation passed; the public game page points to the new build; deployed HTML and release metadata match the local package. Mobile Chromium emulation on the deployed CDN page confirmed Japanese in portrait, no horizontal overflow at 320 px, and a hidden overlay with canvas input restored in landscape. Desktop portrait remains unblocked in the local check. The Unity loader was blocked for these focused HTML checks; this is not a new full gameplay or physical iPhone test.

![Deployed page in phone portrait emulation](portrait-japanese.png)
