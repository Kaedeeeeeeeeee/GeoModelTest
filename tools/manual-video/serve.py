"""Serve the frozen recording build with Unity gzip headers."""
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import urlsplit

root = Path(__file__).resolve().parents[2] / 'Logs/manual-video/player'
class Handler(SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=str(root), **kwargs)
    def end_headers(self):
        if urlsplit(self.path).path.endswith('.unityweb'):
            self.send_header('Content-Encoding', 'gzip')
        self.send_header('Cache-Control', 'no-store')
        super().end_headers()
    def log_message(self, *args): pass

ThreadingHTTPServer(('127.0.0.1', 55903), Handler).serve_forever()
