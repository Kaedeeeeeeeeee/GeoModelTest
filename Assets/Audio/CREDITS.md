# Audio Credits

All audio assets in this project are **CC0 (Public Domain)** — free for any use, no attribution required. Attributions below are provided as a courtesy to the original creators.

## BGM (Background Music)

| File | Source | License | Original Artist |
|---|---|---|---|
| `Resources/Audio/BGM/bgm_field.mp3` | [Peaceful Forest](https://opengameart.org/content/peaceful-forest) on OpenGameArt | CC0 | Samza |
| `Resources/Audio/BGM/bgm_lab.mp3` | [First Light Particles](https://opengameart.org/content/first-light-particles-%E2%80%93-cc0-atmospheric-pianoambient-track) on OpenGameArt | CC0 | Yoiyami |
| `Resources/Audio/BGM/bgm_story.mp3` | [At the End of Hope](https://opengameart.org/content/at-the-end-of-hope) on OpenGameArt | CC0 | Emma_MA |

All BGM tracks re-encoded to MP3 128 kbps for file-size optimization (originals were 25–63 MB WAV / 320 kbps MP3).

## SFX (Sound Effects)

All SFX from [Kenney.nl](https://kenney.nl/) under CC0:

| File | Source Pack | Original Filename |
|---|---|---|
| `sfx_drill_loop.ogg` | [Impact Sounds](https://kenney.nl/assets/impact-sounds) | `impactMining_002.ogg` |
| `sfx_drill_complete.ogg` | Impact Sounds | `impactMetal_heavy_002.ogg` |
| `sfx_hammer_hit.ogg` | Impact Sounds | `impactWood_heavy_003.ogg` |
| `sfx_sample_spawn.ogg` | Impact Sounds | `impactMining_000.ogg` |
| `sfx_sample_drop.ogg` | Impact Sounds | `impactSoft_heavy_001.ogg` |
| `sfx_sample_bounce.ogg` | Impact Sounds | `impactSoft_medium_002.ogg` |
| `sfx_sample_pickup.ogg` | [UI Audio](https://kenney.nl/assets/ui-audio) | `click4.ogg` |
| `sfx_tool_place.ogg` | UI Audio | `switch20.ogg` |
| `sfx_scene_switch.ogg` | [Sci-fi Sounds](https://kenney.nl/assets/sci-fi-sounds) | `forceField_002.ogg` |
| `sfx_scene_teleport.ogg` | Sci-fi Sounds | mix of `laserLarge_001.ogg` + `lowFrequency_explosion_000.ogg` (200ms 延迟，模拟穿越闪现+落地共鸣) |
| `sfx_drone_loop.ogg` | Sci-fi Sounds | `spaceEngineLow_001.ogg` |
| `sfx_drillcar_loop.ogg` | Sci-fi Sounds | `engineCircular_002.ogg` |
| `sfx_footstep_01.ogg` … `_04.ogg` | Impact Sounds | `footstep_grass_000.ogg` … `_003.ogg` |
| `sfx_ui_tab_open.ogg` | UI Audio | `switch22.ogg` |
| `sfx_ui_tab_close.ogg` | UI Audio | `switch23.ogg` |
| `sfx_ui_click.ogg` | UI Audio | `click1.ogg` |
| `sfx_ui_hover.ogg` | UI Audio | `rollover1.ogg` |
| `sfx_ui_panel_open.ogg` | UI Audio | `switch18.ogg` |
| `sfx_ui_panel_close.ogg` | UI Audio | `switch19.ogg` |

## How to Replace

To swap any track, just drop a new file with the same name into `Assets/Resources/Audio/BGM/` or `Assets/Resources/Audio/SFX/`. The `AudioManager` loads by name, no code changes needed.

Recommended alternative sources for replacement audio (all permissive licenses):
- [OpenGameArt.org CC0 Music](https://opengameart.org/content/cc0-music-0)
- [Kenney.nl Audio Packs](https://kenney.nl/assets?q=audio)
- [Pixabay Music](https://pixabay.com/music/) (Pixabay Content License — free for commercial)
- [Sonniss GDC Game Audio Bundle](https://sonniss.com/gameaudiogdc) (yearly free pro audio bundle)
