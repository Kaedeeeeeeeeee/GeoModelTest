"""Local survey preview. The optional backend override never changes build assets."""
import argparse
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('--port', type=int, default=55883)
parser.add_argument('--local-backend', action='store_true')
args = parser.parse_args()
root = Path(__file__).resolve().parents[2] / 'Assets/StreamingAssets/Survey'

class Handler(SimpleHTTPRequestHandler):
    def __init__(self, *a, **kw):
        super().__init__(*a, directory=str(root), **kw)

    def do_GET(self):
        if args.local_backend and self.path.split('?')[0] == '/config.js':
            content = b'window.GEOMODEL_SURVEY={apiUrl:"http://127.0.0.1:55321/functions/v1/game-survey"};'
            self.send_response(200)
            self.send_header('Content-Type', 'application/javascript')
            self.send_header('Cache-Control', 'no-store')
            self.end_headers()
            self.wfile.write(content)
        else:
            super().do_GET()

ThreadingHTTPServer(('127.0.0.1', args.port), Handler).serve_forever()
