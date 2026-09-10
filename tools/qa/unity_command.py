"""Send a command to the project's installed, local Unity MCP editor bridge."""
import json
import socket
import sys

with socket.create_connection(("127.0.0.1", 6400), timeout=20) as client:
    client.sendall(json.dumps(json.loads(sys.argv[1])).encode())
    data = b""
    while True:
        data += client.recv(8192)
        try:
            print(json.dumps(json.loads(data), ensure_ascii=False))
            break
        except json.JSONDecodeError:
            continue
