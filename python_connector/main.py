import asyncio
from server.websocket_server import start_server
import config

if __name__ == "__main__":
    print(f"Starting Python connector...")
    print(f"  WebSocket: ws://{config.WEBSOCKET_HOST}:{config.WEBSOCKET_PORT}")
    print(f"  LLM Model: {config.LLM_MODEL}")
    asyncio.run(start_server(config.WEBSOCKET_HOST, config.WEBSOCKET_PORT))
