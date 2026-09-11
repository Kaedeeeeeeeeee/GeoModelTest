# Main branch consolidation — 2026-09-11

`main` is the primary development branch again. It was fast-forwarded from
`3f51ed4` to `aa8884b`, bringing in all 25 commits from
`codex/experiment-validity-foundation` without changing their contents.
The included WebGL release is `2026.09.11-survey-return`.

## Branch audit

| Branch | Finding | Treatment |
| --- | --- | --- |
| `codex/experiment-validity-foundation` | 25 commits ahead of the former `main`, already on GitHub | All commits included in `main` |
| `claude/amazing-matsumoto-6767b3` | Its committed tip, `8c36731`, is already an ancestor of `main` | No committed code missing; uncommitted source preserved separately below |
| `gh-pages` | Old history from before the LFS rewrite; its tip `034ba08` and main-history commit `3e7248c` differ only in `.gitattributes` | Retained as history; do not combine unrelated pre-migration Git history into the current repository |

All 20 preceding release commits in `gh-pages` also have corresponding commits
in the `main` history, differing only in `.gitattributes`. There is no unique
game implementation to port from that branch. Branch names have been retained;
no branches or worktrees were deleted or force-pushed.

## Uncommitted older worktree

[claude-uncommitted-source.patch](claude-uncommitted-source.patch) preserves
the tracked source changes and new source/meta files from the older Claude
worktree, based on commit `8c36731`. It includes 26 source paths. The patch was
applied to a temporary Git index at that base and verified to reproduce the
source file bytes exactly, without altering the worktree.

This is an archive, not an enabled gameplay update. The older implementation
must not overwrite the current, published implementation wholesale:

| Older changes | Current implementation / decision |
| --- | --- |
| Cursor lock, settings pause, return-to-menu handling | Current modal input management and save-aware return flow supersede these changes |
| Reset quest state for a new game | Already handled by `ResetProgressForNewGame` |
| Restore drone and drill car resource loading | Current release uses dedicated `Prefabs/Vehicles` resources and tested placement/control logic |
| Restore hammer model loading | Current runtime includes a fallback hammer visual; the old resource relocation remains archived |
| Avoid missing-shader crashes in collection markers | Current code checks multiple supported shaders before creating a material |
| New circular tool-wheel artwork and smaller selection dead zone | Unpublished visual/input experiment archived; retain the current published wheel |
| Additional microscope proximity component and localization keys | Archived: the current initializer already attaches `WorkbenchController`, which handles keyboard/mobile interaction; avoid two competing interaction handlers |
| Player scale and mouse-sensitivity changes | Archived tuning experiment; preserve the current published player configuration |
| Deployment notes in `CLAUDE.md` | Preserved as historical notes in the patch |

All 17 apparent `ProjectSettings` changes in that worktree have identical raw
file bytes to its committed base; their apparent diffs come from LFS cleaning.
They are not missing project-setting edits. Old build directories/archives are
generated output and were not copied into `main`. The original worktree and
its uncommitted files remain untouched. A full local backup is also retained
under the ignored `Logs/main-consolidation/` directory.

Temporary PDF-page images and downloaded preview fonts under `tmp/` are now
ignored. Unrelated collaboration drafts and newly generated PDF deliverables
remain local pending the owner's publishing preference.

## Validation

- Fast-forward only: no game-code merge conflicts or runtime changes introduced.
- The old-worktree archive applies to its recorded base and reproduces all
  archived source paths exactly.
- Main includes the development branch tip and the old Claude branch tip.
- Localization synchronization passed (828 / 873 / 828 unique entries).
- All four build files listed in the release manifest matched their recorded
  sizes and SHA-256 hashes.
- `git lfs fsck --objects HEAD` passed.
- Runtime files exactly match `aa8884b`; no Unity rebuild was needed for this
  branch consolidation and documentation archive.

Machine-readable archive and branch evidence: [verification.json](verification.json).
