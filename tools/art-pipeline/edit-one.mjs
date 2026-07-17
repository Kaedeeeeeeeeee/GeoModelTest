// Edit ONE banner per the teacher's review, via Codex imagegen (img2img edit).
// Usage: node edit-one.mjs <id> [--force]
// Exit codes: 0 = file produced, 2 = generation produced no file, 1 = thread error.
//
// Feeds the EXISTING banner (Assets/.../Illustrations/<id>.png) as the base image and
// asks Codex to change only the flagged spot. Output -> tools/art-pipeline/revised/<id>.png
// (staging — never overwrites the original).
//
// No internal wall-timeout: edit-batch.mjs (or a foreground caller) kills on timeout.

import { Codex } from "@openai/codex-sdk";
import { existsSync, statSync, readdirSync, copyFileSync, mkdirSync } from "node:fs";
import { join } from "node:path";
import { homedir } from "node:os";
import { EDITS, buildEditPrompt, srcFor, OUT_DIR } from "./edits.mjs";

const id = process.argv[2];
const force = process.argv.includes("--force");
if (!id) {
  console.error("usage: node edit-one.mjs <id> [--force]");
  console.error("ids:\n  " + EDITS.map((s) => s.id).join("\n  "));
  process.exit(1);
}
const spec = EDITS.find((s) => s.id === id);
if (!spec) { console.error(`unknown id: ${id}`); process.exit(1); }

const src = srcFor(spec.id);
if (!existsSync(src)) { console.error(`missing source image: ${src}`); process.exit(1); }

mkdirSync(OUT_DIR, { recursive: true });
const target = join(OUT_DIR, `${spec.id}.png`);
if (existsSync(target) && !force) { console.log(`SKIP (exists): ${target}`); process.exit(0); }

function newestCachePng() {
  const root = join(homedir(), ".codex", "generated_images");
  if (!existsSync(root)) return null;
  let best = null;
  const walk = (dir) => {
    for (const e of readdirSync(dir, { withFileTypes: true })) {
      const p = join(dir, e.name);
      if (e.isDirectory()) walk(p);
      else if (e.name.toLowerCase().endsWith(".png")) {
        const m = statSync(p).mtimeMs;
        if (!best || m > best.m) best = { p, m };
      }
    }
  };
  try { walk(root); } catch {}
  return best?.p ?? null;
}

console.log(`>> EDIT ${spec.id}  [${spec.kind}]  base=${src}\n   -> ${target}`);
const startedAt = Date.now();

const codex = new Codex();
const thread = codex.startThread({
  workingDirectory: process.cwd(),
  sandboxMode: "workspace-write",
  approvalPolicy: "never",
  networkAccessEnabled: true,
});

const { events } = await thread.runStreamed([
  { type: "text", text: buildEditPrompt(spec, target) },
  { type: "local_image", path: src },
]);

for await (const ev of events) {
  if (ev.type === "item.completed") {
    const it = ev.item;
    if (it.type === "agent_message") console.log("[agent]", it.text);
    else if (it.type === "command_execution") console.log("[cmd]", (it.command || "").slice(0, 120));
    else if (it.type === "error") console.error("[item-error]", it.message || it);
  } else if (ev.type === "turn.failed") {
    console.error("TURN FAILED:", ev.error?.message ?? ev.error);
    process.exit(1);
  }
}

if (!existsSync(target)) {
  const cached = newestCachePng();
  if (cached && statSync(cached).mtimeMs >= startedAt - 1000) {
    copyFileSync(cached, target);
    console.log(`(recovered from cache) ${cached} -> ${target}`);
  }
}

if (existsSync(target)) { console.log(`OK ${target}`); process.exit(0); }
console.error(`NO FILE produced for ${spec.id}`);
process.exit(2);
