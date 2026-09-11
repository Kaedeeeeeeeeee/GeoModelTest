"""Local real-build loading fault server. No research backend or player test hooks.

Each URL's first segment selects a reproducible failure; retry then serves good bytes.
--preview renders the checked-in template against existing build files (no Unity rebuild).
"""
import argparse
import io
from pathlib import Path
import re
import time
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlsplit

parser = argparse.ArgumentParser()
parser.add_argument('--preview', action='store_true')
parser.add_argument('--port', type=int, default=55888)
args = parser.parse_args()
root = Path(__file__).resolve().parents[2]
build = root/'Build/WebGL'
template = root/'Assets/WebGLTemplates/FixedAspect'
requests = {}

class Handler(SimpleHTTPRequestHandler):
    def translate_path(self, path):
        parts = urlsplit(path).path.strip('/').split('/')
        relative = '/'.join(parts[1:]) or 'index.html'
        base = template if args.preview and relative.startswith('TemplateData/') else build
        return str(base/relative)

    def send_head(self):
        parts = urlsplit(self.path).path.strip('/').split('/')
        scenario = parts[0]
        filename = parts[-1]
        key = (scenario, filename)
        requests[key] = requests.get(key, 0) + 1
        count = requests[key]
        failed = (scenario == 'fail-loader' and filename == 'WebGL.loader.js' and count == 1
            or scenario == 'fail-ui' and filename == 'loading.js' and count == 1
            or scenario == 'fail-data' and filename == 'WebGL.data.unityweb' and count == 1
            or scenario == 'fail-twice' and filename == 'WebGL.data.unityweb' and count <= 2)
        if failed or (scenario == 'invalid-data' and filename == 'WebGL.data.unityweb' and count == 1):
            body = b'<html>Temporary server error</html>'
            self.send_response(503 if failed else 200)
            self.send_header('Content-Type', 'text/html')
            self.send_header('Content-Length', str(len(body)))
            self.end_headers()
            return io.BytesIO(body)
        if args.preview and (filename in ('index.html', '') or len(parts) == 1):
            html = (template/'index.html').read_text()
            html = re.sub(r'#if (?:MEMORY_FILENAME|SYMBOLS_FILENAME).*?#endif', '', html, flags=re.S)
            values = {'PRODUCT_NAME': 'GeoModelTest', 'WIDTH': '1920', 'HEIGHT': '1080',
                'LOADER_FILENAME': 'WebGL.loader.js', 'DATA_FILENAME': 'WebGL.data.unityweb',
                'FRAMEWORK_FILENAME': 'WebGL.framework.js.unityweb', 'CODE_FILENAME': 'WebGL.wasm.unityweb',
                'JSON.stringify(COMPANY_NAME)': '"ZHANGSHIFENG"',
                'JSON.stringify(PRODUCT_NAME)': '"GeoModelTest"',
                'JSON.stringify(PRODUCT_VERSION)': '"2026.09.11-loading-preview"'}
            for key, value in values.items(): html = html.replace('{{{ '+key+' }}}', value)
            assert '{{{' not in html
            body = html.encode()
            self.send_response(200)
            self.send_header('Content-Type', 'text/html; charset=utf-8')
            self.send_header('Content-Length', str(len(body)))
            self.end_headers()
            return io.BytesIO(body)
        return super().send_head()

    def guess_type(self, path):
        return 'application/octet-stream' if path.endswith('.unityweb') else super().guess_type(path)

    def end_headers(self):
        # Fault HTML bodies are uncompressed. Real Unity files use native gzip.
        if urlsplit(self.path).path.endswith('.unityweb'):
            parts = urlsplit(self.path).path.strip('/').split('/')
            count = requests[(parts[0], parts[-1])]
            bad = parts[-1] == 'WebGL.data.unityweb' and (
                parts[0] in ('fail-data', 'invalid-data') and count == 1 or parts[0] == 'fail-twice' and count <= 2)
            if not bad: self.send_header('Content-Encoding', 'gzip')
        self.send_header('Cache-Control', 'no-cache')
        super().end_headers()

    def copyfile(self, source, outputfile):
        parts = urlsplit(self.path).path.strip('/').split('/')
        if parts[-1] == 'WebGL.data.unityweb' and requests[(parts[0], parts[-1])] == 1:
            if parts[0] in ('stall', 'timeout', 'disconnect'):
                outputfile.write(source.read(4*1024*1024))
                outputfile.flush()
                if parts[0] == 'disconnect':
                    self.close_connection = True
                    return
                time.sleep(20 if parts[0] == 'stall' else 95)
        try: super().copyfile(source, outputfile)
        except (BrokenPipeError, ConnectionResetError): pass

    def log_message(self, *args): pass

ThreadingHTTPServer(('127.0.0.1', args.port), Handler).serve_forever()
