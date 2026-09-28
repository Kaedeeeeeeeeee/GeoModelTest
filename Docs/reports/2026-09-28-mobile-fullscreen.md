# Mobile/tablet fullscreen implementation

## Confirmed source behavior

- The active Unity WebGL template is `PROJECT:FixedAspect` (`ProjectSettings/ProjectSettings.asset` and `Assets/Editor/WebGLBuildSetup.cs`).
- Before this change, `index.html` explicitly exited iPad fullscreen on every fullscreen event and at 250 ms / 1000 ms after startup. `git blame` and `git log -S'exitIPadFullscreenIfNeeded'` trace this to `31a15882b502c3d7676e517ff9c8386c75a048a8` (2026-07-09, “Sync GeoModel gameplay and backend updates”). That commit also introduced orientation and gesture protections. Neither the commit message nor the searched repository Markdown documentation records a particular iPad rendering/input bug that required this exit behavior.
- The retained safeguards are fixed 16:9 letterboxing, safe area padding, portrait orientation blocking, gesture prevention and the independent capped render buffer.

## Resulting behavior

- Mobile/tablet input requests fullscreen from the first real completed tap on the game canvas or portrait overlay. Page loading alone and synthetic events do not request fullscreen. Desktop behavior is unchanged.
- The document root enters fullscreen, keeping the orientation overlay and browser-side controls visible. Dynamic viewport units fit the browser viewport as its controls appear/disappear; fullscreen events also refresh orientation blocking.
- The request is synchronous inside the user gesture. Standard API requests ask to hide navigation UI; older prefixed WebKit APIs are supported when exposed.
- The automatic attempt occurs once. User exit gestures are respected. Failure or exit presents a small manual retry only when the API is available. The helper is dismissible and disappears after 10 seconds; successful fullscreen shows no helper. Unsupported APIs or denied iframe policy receive a temporary explanation without a nonfunctional retry button.
- There are no new tabs, navigation, iframe permission changes or orientation locks.

## Browser and hosting limits

The browser must allow fullscreen and the player must first interact. Cross-origin hosting also needs the enclosing iframe and its permissions policy to allow fullscreen. These are platform constraints; JavaScript cannot guarantee removing browser chrome on every iPhone/iPad/browser combination. Feature detection is used instead of promising support based on an OS name.

The itch.io documentation says mobile games use click-to-launch fullscreen and recommends a responsive viewport. An iframe-hosted build still depends on the host's fullscreen permissions; maximizing the page viewport is not always equivalent to hiding browser UI. This source change does not modify itch.io project settings or publish a release.

Primary references checked on 2026-09-28:

- [WHATWG Fullscreen API specification](https://fullscreen.spec.whatwg.org/): user activation, fullscreen policy and user exit behavior.
- [WebKit Safari 16.4 announcement](https://webkit.org/blog/13966/webkit-features-in-safari-16-4/#fullscreen-api): unprefixed API on macOS/iPadOS, including user exit gestures. This is historical support evidence, not a claim that every current Apple browser supports the same modes.
- [itch.io HTML5 hosting documentation](https://itch.io/docs/creators/html5): iframe hosting, mobile launch mode and dynamic viewport requirements.
- [MDN requestFullscreen reference](https://developer.mozilla.org/en-US/docs/Web/API/Element/requestFullscreen): transient activation, navigation UI request and asynchronous rejection.

## Validation

`node --test tools/qa/fullscreen-template.test.cjs tools/qa/performance-template.test.cjs` passes 16 tests: 11 fullscreen cases and the existing 5 render-performance cases. Coverage includes trusted taps, desktop exclusion, iPad with trackpad, user/ancestor exit behavior, missing API, iframe denial, rejected/throwing/stalled requests, prefixed Safari events, standalone mode and untouched game input. `node --check` and `git diff --check` pass.

The parent task also verified the following in Ego Lite's desktop Chromium, using browser emulation rather than a physical mobile device:

- An existing WebGL build with the current template entered document-root fullscreen on its first real CDP-dispatched touch with an iPad user agent. The helper remained hidden on success. After `document.exitFullscreen()`, another real touch left the game outside fullscreen and the optional retry was visible.
- At a portrait viewport of 390 × 844, the orientation overlay remained visible inside document-root fullscreen.
- A minimal cross-origin local iframe fixture loaded the actual `fullscreen.js`. With `allowfullscreen`, the request succeeded. Without fullscreen permission, it stayed outside fullscreen, showed the helper, and delivered the original game tap exactly once. This fixture explicitly enabled mobile feature detection because CDP device emulation did not propagate into Chromium's out-of-process iframe; it does not constitute mobile browser or Unity gameplay acceptance.

These checks do not establish physical iPad/iPhone acceptance, successful compilation of a new Unity build, or an itch.io deployment. Live itch.io embed settings were not tested. The parent task records Unity build checks separately.

## Physical iPhone X follow-up

The 2026-09-28 iPhone X / iOS 16.7.16 Safari run confirmed both fullscreen request APIs are absent. The native Hide Toolbar action expands the visible canvas but does not enter Fullscreen API mode. The unsupported old-iPhone Safari message now explains that action, and the hint uses its available width on portrait screens. The 12 fullscreen unit tests pass. See [physical device evidence and launch limitations](2026-09-28-mobile-guidance/physical-iphone-x.md).
