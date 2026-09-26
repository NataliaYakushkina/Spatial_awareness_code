"""User-study condition 2 — SPEECH + HIGHLIGHT.

The avatar answers verbally and highlights the referenced object in the scene,
telling the user it has done so. No on-screen path.

Run this file (instead of main.py) to serve this condition, then launch the
Unity build. Start this backend BEFORE the Unity app so it can connect.
"""
import asyncio
import config
from server.websocket_server import start_server

config.CONDITION_LABEL = "2_highlight"
config.ACTIVE_TOOL_NAMES = [
    "find_objects", "get_objects_in_room", "get_nearby_objects",
    "get_user_context", "get_direction_to", "get_description",
    "check_affordance", "get_path_to_object",
    "highlight_object",
]
config.CONDITION_NOTE = (
    "After you identify the target object, call highlight_object and tell the "
    "user that you have highlighted it so they can spot it. Do NOT draw an "
    "on-screen path and do not claim to have shown one."
)

if __name__ == "__main__":
    print(f"[Condition] {config.CONDITION_LABEL}")
    print(f"  Tools:  {', '.join(config.ACTIVE_TOOL_NAMES)}")
    print(f"  Server: ws://{config.WEBSOCKET_HOST}:{config.WEBSOCKET_PORT}")
    asyncio.run(start_server(config.WEBSOCKET_HOST, config.WEBSOCKET_PORT))
