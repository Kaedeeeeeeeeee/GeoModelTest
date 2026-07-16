Original prompt: Mobile WebGL controls now slide correctly on the left joystick; continue optimizing touch look speed, mobile button language, and the iPad itch fullscreen swipe-down behavior.

## 2026-07-03
- Lowered mobile touch-look sensitivity for iPad/WebGL.
- Updated mobile control labels to follow the active localization language (zh-CN, en-US, ja-JP).
- Added another WebGL template mitigation for iPad fullscreen swipe-down behavior: prevent browser gestures, fixed viewport/overscroll lock, and an iPad fullscreen-exit attempt.
- Verification completed: Unity WebGL build succeeded with 0 errors, butler validate passed, local HTTP smoke check passed, itch html5 build #1767913 uploaded as 2026.07.03-touch-speed-i18n.

## 2026-07-03 tool menu follow-up
- Changed desktop Tab from hold-to-open/release-to-select into press-to-toggle so it does not fight MobileInputManager's shortcut event.
- Routed mobile tool button input to the bottom toolbar only, instead of opening both the radial wheel and toolbar.
- Mobile toolbar selection now equips the selected tool, starts placement preview when appropriate, closes the toolbar, and restores look control.
- Added CollectionTool.RequestPrimaryUse so the mobile Use/Interact button can use the currently equipped tool without faking a mouse click.
- Unity compile passed. WebGL build succeeded with 0 errors, butler validate passed, local HTTP smoke check passed, itch html5 build #1767954 uploaded as 2026.07.03-tool-menu-mobile.

## 2026-07-03 mobile toolbar layout follow-up
- User reported the mobile tool buttons appear partly off-screen at the lower left after tapping Tools.
- Removed SafeAreaPanel from the runtime MobileToolbar and replaced it with explicit safe-area-aware floating tray positioning.
- The tool tray is now centered inside the screen, raised above the joystick/action-control area, width-limited, horizontally scrollable, and clipped with a Mask.
- Tool labels are positioned inside each button instead of below the button bounds.
- Unity compile passed. WebGL build succeeded with 0 errors, butler validate passed, local HTTP smoke check passed, itch html5 build #1767992 uploaded as 2026.07.03-mobile-toolbar-layout.

## 2026-07-03 mobile toolbar visible-row follow-up
- User reported the tool tray now appears only as a small black strip with no visible tool buttons.
- Replaced the masked ScrollRect tray with a simple visible centered horizontal button row to avoid WebGL/iPad clipping.
- Buttons are created after the toolbar is active and the layout is forced to rebuild immediately.
- Increased the toolbar/button minimum size and made ToolManager lookup include inactive objects.
- Unity compile passed. WebGL build succeeded with 0 errors, butler validate passed, local HTTP smoke check passed, itch html5 build #1768056 uploaded as 2026.07.03-mobile-toolbar-visible.

## 2026-07-03 same wheel mobile follow-up
- User reported the mobile tool squares are still not usable and do not match the desktop Tab UI.
- Mobile Tools now opens the same radial wheel UI as desktop Tab; the legacy MobileToolbar path is disabled at runtime.
- Mobile wheel selection now directly hit-tests the wheel slot RectTransforms from the touch position, instead of relying on hover/selectedSlot state.
- Desktop mouse click selection also uses the same direct slot hit-test fallback.
- Mobile Use/Interact still calls the equipped tool's RequestPrimaryUse when no tool menu is open.
- Unity compile passed. WebGL build succeeded with 0 errors, butler validate passed, local HTTP smoke check passed, itch html5 build #1768072 uploaded as 2026.07.03-same-tool-wheel-mobile.

## 2026-07-03 input priority fix
- User reported both mobile and web character controls were no longer usable after the tool-wheel follow-up.
- FirstPersonController now uses MobileInputManager's WebGL-aware mobile detection instead of only Application.isMobilePlatform, keeping desktop Web keyboard/mouse as the primary input path.
- MobileInputManager now tracks active touch/gameplay input and processes live touchscreen input as an overlay in desktop auto mode, giving WebGL/iPad a fallback without classifying every touch-capable desktop as mobile.
- WebGLBuildSetup.BuildWebGL now applies ConfigureWebGLSettings before building, so the uploaded package carries the requested productVersion and avoids stale Unity cache metadata.
- Unity compile passed. WebGL build succeeded with 0 errors, butler validate passed, local HTTP smoke check passed, itch html5 build #1768104 uploaded as 2026.07.03-input-priority-fix.

## 2026-07-09 mobile tool wheel tap-select follow-up
- User confirmed movement works on desktop and mobile, and mobile long-press Tools opens the same wheel as desktop.
- Mobile wheel selection is being changed to tap-to-open, press-to-highlight, and release-to-confirm so users can see which tool is selected before equipping.
- Wheel background now uses a generated circular sprite instead of a rectangular Image fill.
- Unity compile passed with 0 C# errors.
- WebGL build succeeded with 0 errors, productVersion 2026.07.09-mobile-tool-wheel-tap, and butler validate passed.
- Headed Playwright WebGL check passed: Unity loaded with WebGL2, the tool wheel renders with a circular background, slot hover highlights the slot/icon/text, and clicking a slot selects it and closes the wheel.
- itch html5 build #1782964 is active as 2026.07.09-mobile-tool-wheel-tap.

## 2026-07-09 mobile wild tool button tap-toggle follow-up
- User reported the laboratory Tools button stays open after tap, but the wild scene Tools button behaves like hold-to-show.
- Root cause: runtime MobileControlsUI click buttons invoked on PointerDown, then the raw touch fallback invoked again on the same touch Ended, toggling the tool wheel closed on release.
- MobileControlsUI click buttons now use PointerDown only for pressed visuals; touch clicks are executed once by the raw release fallback, while mouse/desktop-test clicks execute on pointer release.
- Unity compile passed with 0 C# errors.
- WebGL build succeeded with 0 errors, productVersion 2026.07.09-mobile-wild-tool-toggle, and butler validate passed.
- Headed Chromium WebGL check passed: Unity loaded, WebGL2 is available, and the built page contains productVersion 2026.07.09-mobile-wild-tool-toggle.
- itch html5 build #1783059 is active as 2026.07.09-mobile-wild-tool-toggle.

## 2026-07-09 mobile menu and quality settings release
- Added the mobile pause/menu surface with Resume, Settings, and Exit Game, plus Settings controls for camera look sensitivity and manual/auto graphics quality.
- WebGL build succeeded with 0 errors and productVersion 2026.07.09-mobile-menu-quality.
- Butler validate passed and local HTTP smoke check returned 200 for index.html, loader, wasm, data, and framework files.
- itch html5 build #1783091 is active as 2026.07.09-mobile-menu-quality.

## 2026-07-10 mobile menu touch and layout fix
- Added raw-touch release handling for the Resume, Settings, and Exit Game menu actions, while blocking touches from reaching gameplay controls behind the open menu.
- Expanded and clipped the mobile menu panel, kept long settings labels inside their rows, and made the settings content opaque so underlying UI cannot bleed through it.
- Added a modal canvas layer guard so story/report canvases cannot render or receive input above the pause menu or settings; their original sort order is restored on close.
- Unity compile and WebGL build passed with 0 errors; Butler validation and local HTTP checks passed.
- iPad-emulated Playwright touch flow passed: open Menu -> open Settings -> close Settings.
- itch html5 build #1784712 is active as 2026.07.10-mobile-menu-touch-layout-fix.
