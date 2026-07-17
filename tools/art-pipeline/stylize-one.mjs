// Generate ONE knowledge-illustration banner via Codex imagegen.
// Usage: node stylize-one.mjs <id> [--force]
// Exit codes: 0 = file produced, 2 = generation produced no file, 1 = thread error.
//
// This script intentionally has NO internal wall-timeout: the batch runner spawns it
// as a child and kills it on timeout (the recommended quota-hang mitigation).

import { Codex } from "@openai/codex-sdk";
import { existsSync, statSync, readdirSync, copyFileSync } from "node:fs";
import { join } from "node:path";
import { homedir } from "node:os";
import { SPECS, buildPrompt, ANCHOR, OUT_DIR } from "./specs.mjs";

const id = process.argv[2];
const force = process.argv.includes("--force");
if (!id) {
  console.error("usage: node stylize-one.mjs <id> [--force]");
  console.error("ids:\n  " + SPECS.map((s) => s.id).join("\n  "));
  process.exit(1);
}
const spec = SPECS.find((s) => s.id === id);
if (!spec) {
  console.error(`unknown id: ${id}`);
  process.exit(1);
}

const target = join(OUT_DIR, `${spec.id}.png`);
if (existsSync(target) && !force) {
  console.log(`SKIP (exists): ${target}`);
  process.exit(0);
}

// Newest png anywhere under the imagegen cache — used as a fallback if the agent
// forgot to copy the file to the target path.
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

console.log(`>> ${spec.id}  (${spec.questionId})  -> ${target}`);
const startedAt = Date.now();

const codex = new Codex();
const thread = codex.startThread({
  workingDirectory: process.cwd(),
  sandboxMode: "workspace-write",
  approvalPolicy: "never",
  networkAccessEnabled: true,
});

const { events } = await thread.runStreamed([
  { type: "text", text: buildPrompt(spec, target) },
  { type: "local_image", path: ANCHOR },
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

// Verify / recover the output file.
if (!existsSync(target)) {
  const cached = newestCachePng();
  if (cached && statSync(cached).mtimeMs >= startedAt - 1000) {
    copyFileSync(cached, target);
    console.log(`(recovered from cache) ${cached} -> ${target}`);
  }
}

if (existsSync(target)) {
  console.log(`OK ${target}`);
  process.exit(0);
}
console.error(`NO FILE produced for ${spec.id}`);
process.exit(2);
