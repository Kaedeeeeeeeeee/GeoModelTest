# art-pipeline — knowledge-illustration banners

Generates the 11 knowledge-explanation banners for the story quiz flow, using the
Codex CLI's built-in `imagegen` (billed to the ChatGPT subscription, no API key).

Output → `Assets/Resources/Story/Illustrations/<id>.png` (1536×1024).
Each `id` maps to a story `questionId` (see `specs.mjs`).

## Setup (one-time)
```bash
cd tools/art-pipeline
npm install
codex --version   # CLI must be installed + logged in (codex login)
```

## Generate
```bash
# all 11 (run in YOUR terminal — quota pauses can exceed Claude Code's bg-task limit)
node stylize-batch.mjs

# a subset
node stylize-batch.mjs 03_limestone 04_chert

# a single image (no quota retry wrapper)
node stylize-one.mjs 02_rock_grainsize          # add --force to overwrite
```

## Notes
- **Quota**: ~9 images/hour. On the 10th the imagegen call hangs silently; the batch
  runner times out at 6 min, waits 60 min, retries once. Total wall time for 11 ≈ just
  over an hour. It's idempotent — re-run to fill in any missing files.
- **Style anchor**: `Assets/Resources/Tachie/kaede.png`, used for palette/line only
  (the prompt forbids drawing any character).
- Edit subjects / Japanese labels in `specs.mjs`, then re-run with `--force`.
