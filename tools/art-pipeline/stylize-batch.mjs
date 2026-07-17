// Batch-generate the knowledge-illustration banners, sequentially, with the five
// quota-safety features the codex-imagegen skill calls for:
//   1. per-image wall timeout (quota hang is silent)   2. 60-min pause + 1 retry on timeout
//   3. idempotent skip-if-exists   4. consecutive non-timeout failures abort
//   5. per-image logs under .logs/
//
// RUN THIS IN YOUR OWN TERMINAL, not via a Claude Code background task
// (long quota pauses outlive the bg-task harness):
//   cd tools/art-pipeline && node stylize-batch.mjs
// Optional: pass ids to run a subset:  node stylize-batch.mjs 03_limestone 04_chert

import { spawn } from "node:child_process";
import { existsSync, createWriteStream, mkdirSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { SPECS, OUT_DIR } from "./specs.mjs";

const HERE = dirname(fileURLToPath(import.meta.url));
const LOG_DIR = join(HERE, ".logs");
mkdirSync(LOG_DIR, { recursive: true });

const PER_IMAGE_TIMEOUT_MS = 6 * 60 * 1000;   // 6 min, then assume quota hang
const QUOTA_PAUSE_MS = 60 * 60 * 1000;         // 60 min rolling-window reset
const GAP_MS = 3 * 1000;                        // small gap between images

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

const filter = process.argv.slice(2);
const queue = filter.length ? SPECS.filter((s) => filter.includes(s.id)) : SPECS;

// Run `stylize-one <id>` as a child; resolve {status:'ok'|'timeout'|'fail', code}.
function runOne(id) {
  return new Promise((resolve) => {
    const log = createWriteStream(join(LOG_DIR, `${id}.log`), { flags: "a" });
    log.write(`\n===== ${new Date().toISOString()}  ${id} =====\n`);
    const child = spawn("node", ["stylize-one.mjs", id], { cwd: HERE });
    let timedOut = false;
    const timer = setTimeout(() => {
      timedOut = true;
      child.kill("SIGKILL");
    }, PER_IMAGE_TIMEOUT_MS);

    child.stdout.on("data", (d) => { process.stdout.write(d); log.write(d); });
    child.stderr.on("data", (d) => { process.stderr.write(d); log.write(d); });
    child.on("close", (code) => {
      clearTimeout(timer);
      log.end();
      if (timedOut) resolve({ status: "timeout" });
      else if (code === 0) resolve({ status: "ok" });
      else resolve({ status: "fail", code });
    });
  });
}

let done = 0, failed = 0, consecutiveHardFails = 0;
const failures = [];

console.log(`Batch: ${queue.length} image(s). Output -> ${OUT_DIR}\n`);

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
    done++; consecutiveHardFails = 0;
    console.log(`✅ ${spec.id}\n`);
  } else {
    failed++; failures.push(spec.id);
    if (r.status === "timeout") {
      consecutiveHardFails = 0; // quota, not a hard error
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
