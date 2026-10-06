#!/usr/bin/env bash
# Publishes a Unity WebGL build to the geo-q Worker.
# Usage: hosting/cloudflare/deploy.sh [path/to/Build/WebGL]
# The .unityweb files go to R2 under releases/<version>/ first; the Worker deploy
# that follows switches RELEASE to that version, so old releases stay for rollback.
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
build="$(cd "${1:-$here/../../Build/WebGL}" && pwd)"
bucket=geo-q-builds

# Refuse a build whose files don't match its release.json.
version="$(node - "$build" <<'JS'
const fs = require("fs"), path = require("path"), crypto = require("crypto");
const build = process.argv[2];
const release = JSON.parse(fs.readFileSync(path.join(build, "release.json"), "utf8"));
for (const group of ["assets", "webAssets", "surveyAssets", "streamingAssets"]) {
  for (const [file, meta] of Object.entries(release[group] || {})) {
    const data = fs.readFileSync(path.join(build, file));
    const sha = crypto.createHash("sha256").update(data).digest("hex");
    if (data.length !== meta.bytes || sha !== meta.sha256) throw new Error(`${file} does not match release.json`);
  }
}
if (!/^[0-9A-Za-z._-]+$/.test(release.version)) throw new Error(`bad version ${release.version}`);
console.log(release.version);
JS
)"
echo "Publishing $version from $build"

for file in WebGL.data.unityweb WebGL.framework.js.unityweb WebGL.wasm.unityweb; do
  wrangler r2 object put "$bucket/releases/$version/Build/$file" --file "$build/Build/$file" --remote
done

stage="$here/.stage"
rm -rf "$stage"
mkdir -p "$stage"
rsync -a --exclude '/Build/*.unityweb' --exclude '.DS_Store' "$build/" "$stage/"
cp "$here/_headers" "$stage/_headers"

wrangler deploy --config "$here/wrangler.jsonc" --var "RELEASE:$version"
