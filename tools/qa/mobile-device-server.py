"""Serve only the current local validation build for isolated physical-device QA.

Use a dedicated port so browser saves are separate from both itch.io and other
local runs. This server never proxies requests or exposes the repository root.
External research/analytics connections are blocked by CSP and a page guard.
"""
import argparse
import gzip
import hashlib
import io
import json
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
import secrets
import shutil
import threading
import time
from urllib.parse import unquote, urlsplit


REPO = Path(__file__).resolve().parents[2]
BUILDS = {
    "validation": (REPO / "Build/RemediationWebGL").resolve(),
    "device": (REPO / "Build/MobileDeviceWebGL").resolve(),
    "optimized": (REPO / "Build/MobileDeviceOptimizedWebGL").resolve(),
    "astc": (REPO / "Build/MobileDeviceAstcWebGL").resolve(),
    "release": (REPO / "Build/WebGL").resolve(),
}
BUILD = BUILDS["validation"]
CACHE = REPO / "Logs/mobile-device-qa/gzip"
QUEUE = REPO / "Logs/mobile-device-qa"
MAX_COMMAND_BYTES = 65536
MAX_RESULT_BYTES = 65536
MIMES = {
    ".html": "text/html; charset=utf-8",
    ".js": "application/javascript; charset=utf-8",
    ".css": "text/css; charset=utf-8",
    ".json": "application/json; charset=utf-8",
    ".wasm": "application/wasm",
    ".data": "application/octet-stream",
    ".unityweb": "application/octet-stream",
    ".png": "image/png",
    ".jpg": "image/jpeg",
    ".jpeg": "image/jpeg",
    ".ico": "image/x-icon",
    ".svg": "image/svg+xml",
}
CSP = (
    "default-src 'self' blob: data:; "
    "script-src 'self' 'unsafe-inline' 'unsafe-eval' blob:; "
    "style-src 'self' 'unsafe-inline'; "
    "connect-src 'self' blob: data:; "
    "worker-src 'self' blob:; frame-src 'none'; object-src 'none'; "
    "base-uri 'none'; form-action 'none'"
)
GUARD = b"""(() => {
  const state = window.geoModelDeviceQA = { isolated: true, blocked: [] };
  function permitted(value) {
    const url = new URL(value, location.href);
    if (url.origin === location.origin || /^(blob|data):$/.test(url.protocol)) return true;
    // Record only host names, never credentials, query strings or request bodies.
    state.blocked.push(url.hostname);
    return false;
  }
  const originalFetch = window.fetch.bind(window);
  window.fetch = (input, init) => permitted(typeof input === 'string' || input instanceof URL ? input : input.url)
    ? originalFetch(input, init) : Promise.reject(new TypeError('External connection blocked for device QA'));
  const originalOpen = XMLHttpRequest.prototype.open;
  XMLHttpRequest.prototype.open = function(method, url, ...rest) {
    if (!permitted(url)) throw new DOMException('External connection blocked for device QA', 'SecurityError');
    return originalOpen.call(this, method, url, ...rest);
  };
  // Only the local filesystem can enqueue a command. This timer intentionally
  // does not generate a user gesture, click, touch, or transient activation.
  let busy = false;
  let pendingResult = null;
  state.bridge = 'polling';
  setInterval(async () => {
    if (busy) return;
    busy = true;
    try {
      if (!pendingResult) {
        const response = await originalFetch('/__qa-command', {cache: 'no-store'});
        if (response.status === 204) return;
        if (!response.ok) return;
        const command = await response.json();
        let result;
        try {
          result = await (0, eval)(command.expression);
          pendingResult = JSON.stringify({id: command.id, nonce: command.nonce,
            result: result === undefined ? null : result});
          if (new TextEncoder().encode(pendingResult).length > 64000)
            throw new Error('QA result exceeded 64000 bytes; request a smaller snapshot');
        } catch (error) {
          pendingResult = JSON.stringify({id: command.id, nonce: command.nonce,
            error: String(error && error.message || error).slice(0, 4000)});
        }
      }
      const posted = await originalFetch('/__qa-result', {method: 'POST',
        headers: {'Content-Type': 'application/json'}, body: pendingResult});
      if (posted.ok || posted.status === 409) pendingResult = null;
    } catch (_) {
      // A transient LAN interruption retries the same result, never the command.
    } finally { busy = false; }
  }, 500);
})();
"""


def prepare_gzip():
    """Cache compressed copies keyed by source bytes; production files stay intact."""
    CACHE.mkdir(parents=True, exist_ok=True)
    compressed = {}
    for source in sorted(BUILD.rglob("*")):
        if not source.is_file() or source.suffix not in {".data", ".wasm", ".js"}:
            continue
        if not source.resolve().is_relative_to(BUILD):
            continue
        digest = hashlib.sha256()
        with source.open("rb") as stream:
            for chunk in iter(lambda: stream.read(1024 * 1024), b""):
                digest.update(chunk)
        target = CACHE / (digest.hexdigest() + ".gz")
        if not target.exists():
            temporary = target.with_suffix(".tmp")
            with source.open("rb") as src, temporary.open("wb") as raw:
                with gzip.GzipFile(filename="", fileobj=raw, mode="wb", compresslevel=3, mtime=0) as dst:
                    shutil.copyfileobj(src, dst, length=1024 * 1024)
            temporary.replace(target)
        compressed[source.resolve()] = target
        print(json.dumps({"file": str(source.relative_to(BUILD)), "bytes": source.stat().st_size,
                          "gzip_bytes": target.stat().st_size}), flush=True)
    return compressed


def handler_factory(compressed):
    delivery_lock = threading.Lock()
    delivered_id = None
    delivered_nonce = None
    completed_id = None

    class Handler(BaseHTTPRequestHandler):
        def do_HEAD(self):
            self.serve(head=True)

        def do_GET(self):
            self.serve(head=False)

        def qa_response(self, status, value=None):
            body = json.dumps(value, ensure_ascii=False).encode("utf-8") if value is not None else b""
            self.send_response(status)
            self.send_header("Content-Type", MIMES[".json"])
            self.send_header("Content-Length", str(len(body)))
            self.send_header("Cache-Control", "no-store")
            self.send_header("Content-Security-Policy", CSP)
            self.send_header("X-Content-Type-Options", "nosniff")
            self.end_headers()
            if self.command != "HEAD":
                self.wfile.write(body)

        def read_command(self):
            nonlocal delivered_id, delivered_nonce
            if self.command != "GET":
                self.qa_response(405)
                return
            with delivery_lock:
                command_path = QUEUE / "command.json"
                try:
                    if command_path.stat().st_size > MAX_COMMAND_BYTES:
                        raise ValueError("Command is too large")
                    queued = json.loads(command_path.read_text(encoding="utf-8"))
                    valid = (isinstance(queued, dict) and set(queued) == {"id", "expression"}
                             and isinstance(queued["id"], str) and 0 < len(queued["id"]) <= 128
                             and isinstance(queued["expression"], str))
                    if not valid:
                        raise ValueError("Expected id and expression strings")
                except FileNotFoundError:
                    self.qa_response(204)
                    return
                except (ValueError, OSError):
                    self.qa_response(422, {"error": "Invalid local command file"})
                    return
                if queued["id"] == delivered_id or delivered_id != completed_id:
                    self.qa_response(204)
                    return
                delivered_id = queued["id"]
                delivered_nonce = secrets.token_urlsafe(24)
                self.qa_response(200, {**queued, "nonce": delivered_nonce})

        def do_POST(self):
            nonlocal completed_id
            if urlsplit(self.path).path != "/__qa-result":
                self.qa_response(405)
                return
            if self.headers.get("Content-Type", "").split(";", 1)[0].strip() != "application/json":
                self.qa_response(415)
                return
            try:
                length = int(self.headers.get("Content-Length", "0"))
            except ValueError:
                length = 0
            if not 0 < length <= MAX_RESULT_BYTES:
                self.qa_response(413)
                return
            try:
                payload = json.loads(self.rfile.read(length))
                valid = (isinstance(payload, dict) and set(payload) in
                         ({"id", "nonce", "result"}, {"id", "nonce", "error"})
                         and isinstance(payload["id"], str) and isinstance(payload["nonce"], str))
                if not valid:
                    raise ValueError("Invalid result shape")
            except (UnicodeError, ValueError):
                self.qa_response(422)
                return
            with delivery_lock:
                if (payload["id"] != delivered_id or delivered_nonce is None or
                        not secrets.compare_digest(payload["nonce"], delivered_nonce)):
                    self.qa_response(409)
                    return
                if payload["id"] != completed_id:
                    # Persist only the explicitly requested result, not protocol
                    # secrets, console output, browser saves or response headers.
                    result = {key: value for key, value in payload.items() if key != "nonce"}
                    serialized = json.dumps(result, ensure_ascii=False)
                    QUEUE.mkdir(parents=True, exist_ok=True)
                    temporary = QUEUE / "result.tmp"
                    temporary.write_text(serialized + "\n", encoding="utf-8")
                    temporary.replace(QUEUE / "result.json")
                    with (QUEUE / "results.jsonl").open("a", encoding="utf-8") as stream:
                        stream.write(serialized + "\n")
                    completed_id = payload["id"]
                self.qa_response(204)

        def serve(self, head):
            try:
                relative = unquote(urlsplit(self.path).path, errors="strict")
            except (UnicodeError, ValueError):
                self.send_error(400)
                return
            if "\x00" in relative or "\\" in relative or ".." in relative.split("/"):
                self.send_error(400)
                return
            if relative == "/__qa-command":
                self.read_command()
                return
            if relative == "/__device-qa.js":
                source = io.BytesIO(GUARD)
                length, mime, encoding = len(GUARD), MIMES[".js"], None
            else:
                requested = BUILD / (relative.lstrip("/") or "index.html")
                path = requested.resolve()
                if not path.is_relative_to(BUILD) or not path.is_file() or path.suffix not in MIMES:
                    self.send_error(404)
                    return
                mime, encoding = MIMES[path.suffix], None
                if path.suffix == ".unityweb":
                    uncompressed_suffix = Path(path.name[:-len(".unityweb")]).suffix
                    mime = MIMES.get(uncompressed_suffix, "application/octet-stream")
                if path == BUILD / "index.html":
                    html = path.read_text(encoding="utf-8")
                    html = html.replace("<head>", '<head><script src="/__device-qa.js"></script>', 1)
                    body = html.encode("utf-8")
                    source, length = io.BytesIO(body), len(body)
                else:
                    chosen = path
                    accepts_gzip = any(
                        item.split(";", 1)[0].strip() == "gzip" and
                        not any(part.strip() in {"q=0", "q=0.0", "q=0.00", "q=0.000"} for part in item.split(";")[1:])
                        for item in self.headers.get("Accept-Encoding", "").lower().split(",")
                    )
                    if accepts_gzip and path in compressed:
                        chosen, encoding = compressed[path], "gzip"
                    elif path.suffix == ".unityweb":
                        # Only announce an encoding supported by the actual bytes.
                        with path.open("rb") as raw:
                            if raw.read(2) == b"\x1f\x8b":
                                encoding = "gzip"
                    length, source = chosen.stat().st_size, chosen.open("rb")
            with source:
                self.send_response(200)
                self.send_header("Content-Type", mime)
                self.send_header("Content-Length", str(length))
                if encoding:
                    self.send_header("Content-Encoding", encoding)
                self.send_header("Vary", "Accept-Encoding")
                self.send_header("Cache-Control", "no-store")
                self.send_header("Content-Security-Policy", CSP)
                self.send_header("X-Content-Type-Options", "nosniff")
                self.send_header("Referrer-Policy", "no-referrer")
                self.end_headers()
                if not head:
                    track_transfer = relative.startswith("/Build/")
                    started = time.monotonic()
                    written = 0
                    if track_transfer:
                        print(json.dumps({"event": "transfer_start", "path": relative,
                                          "bytes": length, "encoding": encoding}), flush=True)
                    try:
                        for chunk in iter(lambda: source.read(1024 * 1024), b""):
                            self.wfile.write(chunk)
                            written += len(chunk)
                    except (BrokenPipeError, ConnectionResetError):
                        if track_transfer:
                            print(json.dumps({"event": "transfer_interrupted", "path": relative,
                                              "bytes_written": written,
                                              "seconds": round(time.monotonic() - started, 2)}), flush=True)
                    else:
                        if track_transfer:
                            # Server writes do not establish browser decode/load success.
                            print(json.dumps({"event": "transfer_written", "path": relative,
                                              "bytes_written": written,
                                              "seconds": round(time.monotonic() - started, 2)}), flush=True)

        def log_message(self, format_string, *args):
            # Request paths are sufficient for local QA; omit query strings.
            if urlsplit(self.path).path in {"/__qa-command", "/__qa-result"}:
                return
            print(json.dumps({"path": urlsplit(self.path).path, "status": str(args[1]) if len(args) > 1 else ""}), flush=True)

    return Handler


def main():
    global BUILD
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", choices=tuple(BUILDS), default="validation",
                        help="Fixed local output: validation=RemediationWebGL, device=MobileDeviceWebGL, optimized=MobileDeviceOptimizedWebGL, astc=MobileDeviceAstcWebGL, release=WebGL")
    parser.add_argument("--port", type=int, default=55928)
    parser.add_argument("--bind", default="0.0.0.0")
    parser.add_argument("--no-gzip", action="store_true")
    args = parser.parse_args()
    BUILD = BUILDS[args.build]
    if not (BUILD / "index.html").is_file():
        parser.error(f"{BUILD.relative_to(REPO)}/index.html is missing; create that build first")
    compressed = {} if args.no_gzip else prepare_gzip()
    print(f"Isolated device QA: http://<this-Mac-LAN-IP>:{args.port}/ (root: {BUILD})", flush=True)
    print("External research/analytics requests blocked; only this local origin can connect.", flush=True)
    server = ThreadingHTTPServer((args.bind, args.port), handler_factory(compressed))
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()


if __name__ == "__main__":
    main()
