# Repository consolidation — September 15, 2026

The published game source and WebGL build were already committed and pushed
through `b18126d`. The remaining additions in the desktop Changes panel were
untracked guide materials, exports, browser captures, a previous build archive
and private correspondence. They were not missing gameplay changes.

## Branch audit

| Local branch | Unique committed game work | Result |
| --- | --- | --- |
| `main` | Current published implementation | Primary development branch |
| `codex/experiment-validity-foundation` | None; its tip is an ancestor of `main` | Already integrated |
| `claude/amazing-matsumoto-6767b3` | None; its tip is an ancestor of `main` | Already integrated; older working files checked below |
| `gh-pages` | None; 21 commits from before the LFS migration have equivalent trees in `main`, except `.gitattributes` | Retained as legacy history |

No merge commit is needed to import game features. Combining the pre-LFS
history would reintroduce redundant history without adding implementation.
Branch names and the older worktree remain available; none were deleted or
force-pushed. Normal development continues on `main`.

The old Claude worktree still contains uncommitted experiments. Its current
26 source files and six original-path deletions exactly match the
[previously committed archive](../2026-09-11-main-consolidation/claude-uncommitted-source.patch).
Its 17 apparent project-setting changes remain byte-identical to the old base.
There are no newly discovered source edits outside the archive. The
[earlier consolidation report](../2026-09-11-main-consolidation/README.md)
explains which experiments were superseded by the current implementation.
The archive preserves those experiments; it does not enable them in the game.

## Remaining files

- Version the Japanese PDF guide sources, screenshots, video editing plan,
  narration, production tools and finished PDF/video/subtitle/cover files.
  Media uses the repository's existing Git LFS rules.
- Mark the guide documentation and export index as historical: these guides
  predate the September 15 UI and survey updates and need revision before
  distribution for the current release.
- Make the DOM capture helper resolve paths relative to this repository and
  require explicit local runtime/profile/model paths for narration.
- Ignore old `/Build/*.zip` packages and temporary `/output/playwright/`
  captures. They remain on disk.
- Keep the private collaboration reply and participant-flow draft local,
  using `.git/info/exclude`. They remain on disk and are not published.

No Unity source, scenes, settings, backend or published WebGL assets change
in this consolidation. The live version remains
[`2026.09.15-teacher-feedback`](../2026-09-15-teacher-feedback/release/README.md).

Detailed branch equivalence and archive checks: [verification.json](verification.json).

## Validation

- Parsed all eight Python scripts, two JavaScript modules and both editing /
  narration JSON files; the narration CLI help works without loading the model.
- All 19 edited chapters have matching narration and existing local source media.
- All 15 new LFS media files match their staged pointer hashes, sizes and local
  LFS objects.
- Gameplay, project settings, packages, backend and published WebGL assets are
  unchanged, so this documentation consolidation does not require a Unity build.
- Excluded private drafts, the old build package and temporary captures still
  exist locally.
