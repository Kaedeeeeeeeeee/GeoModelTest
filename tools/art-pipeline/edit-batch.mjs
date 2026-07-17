// Batch-apply the teacher's edits to the 11 banners, sequentially, with the same five
// quota-safety features as stylize-batch.mjs:
//   1. per-image wall timeout (quota hang is silent)   2. 60-min pause + 1 retry on timeout
//   3. idempotent skip-if-exists   4. consecutive non-timeout failures abort   5. per-image logs
//
// RUN THIS IN YOUR OWN TERMINAL (long quota pauses outlive a Claude Code bg task):
//   cd tools/art-pipeline && node edit-batch.mjs
// Subset:  node edit-batch.mjs 08_index_fossil_eras 02_rock_grainsize
// Re-do an already-revised one: delete revised/<id>.png (or pass --force via edit-one) and re-run.

import { spawn } from "node:child_process";
import { existsSync, createWriteStream, mkdirSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { EDITS, OUT_DIR } from "./edits.mjs";

const HERE = dirname(fileURLToPath(import.meta.url));
const LOG_DIR = join(HERE, ".logs");
mkdirSync(LOG_DIR, { recursive: true });

const PER_IMAGE_TIMEOUT_MS = 6 * 60 * 1000;
const QUOTA_PAUSE_MS = 60 * 60 * 1000;
const GAP_MS = 3 * 1000;
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

const filter = process.argv.slice(2).filter((a) => !a.startsWith("--"));
const queue = filter.length ? EDITS.filter((s) => filter.includes(s.id)) : EDITS;

function runOne(id) {
  return new Promise((resolve) => {
    const log = createWriteStream(join(LOG_DIR, `edit_${id}.log`), { flags: "a" });
    log.write(`\n===== ${new Date().toISOString()}  EDIT ${id} =====\n`);
    const child = spawn("node", ["edit-one.mjs", id], { cwd: HERE });
    let timedOut = false;
    const timer = setTimeout(() => { timedOut = true; child.kill("SIGKILL"); }, PER_IMAGE_TIMEOUT_MS);
    child.stdout.on("data", (d) => { process.stdout.write(d); log.write(d); });
    child.stderr.on("data", (d) => { process.stderr.write(d); log.write(d); });
    child.on("close", (code) => {
      clearTimeout(timer); log.end();
      if (timedOut) resolve({ status: "timeout" });
      else if (code === 0) resolve({ status: "ok" });
      else resolve({ status: "fail", code });
    });
  });
}

let done = 0, failed = 0, consecutiveHardFails = 0;
const failures = [];
console.log(`Edit batch: ${queue.length} image(s). Staging -> ${OUT_DIR}\n`);

for (const spec of queue) {
  const target = join(OUT_DIR, `${spec.id}.png`);
  if (existsSync(target)) { console.log(`SKIP (exists): ${spec.id}`); done++; continue; }

  let r = await runOne(spec.id);
  if (r.status === "timeout") {
    console.log(`\n⏳ ${spec.id} timed out (likely the ~9/hour quota). Pausing 60 min, then one retry…\n`);
    await sleep(QUOTA_PAUSE_MS);
    r = await runOne(spec.id);
  }

  if (r.status === "ok") {
    done++; consecutiveHardFails = 0; console.log(`✅ ${spec.id}\n`);
  } else {
    failed++; failures.push(spec.id);
    if (r.status === "timeout") {
      consecutiveHardFails = 0;
      console.log(`❌ ${spec.id}: timed out twice — skipping.\n`);
    } else {
      consecutiveHardFails++;
      console.log(`❌ ${spec.id}: exited ${r.code}.\n`);
      if (consecutiveHardFails >= 3) {
        console.error(`\n🛑 3 consecutive non-timeout failures. Stopping — check .logs/ (auth? network? bug?).`);
        break;
      }
    }
  }
  await sleep(GAP_MS);
}

console.log(`\n==== done: ${done}/${queue.length}  failed: ${failed} ====`);
if (failures.length) console.log("failed ids: " + failures.join(", "));
console.log("Re-run the same command to retry only the missing ones (skip-if-exists).");
