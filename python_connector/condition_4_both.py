"""User-study condition 4 — SPEECH + HIGHLIGHT + PATH (together).

The avatar answers verbally, and for the target object highlights it AND draws
the route at the same time, telling the user it has done both.

Run this file (instead of main.py) to serve this condition, then launch the
Unity build. Start this backend BEFORE the Unity app so it can connect.
"""
import asyncio
import config
from server.websocket_server import start_server

config.CONDITION_LABEL = "4_both"
config.ACTIVE_TOOL_NAMES = [
    "find_objects", "get_objects_in_room", "get_nearby_objects",
    "get_user_context", "get_direction_to", "get_description",
    "check_affordance", "get_path_to_object",
    "highlight_object", "show_path_to_user",
]
config.CONDITION_NOTE = (
    "For the target object, call BOTH highlight_object AND show_path_to_user in "
    "the same reply, so the object is highlighted and the route is drawn at the "
    "same time. Tell the user that you have highlighted it and are showing the "
    "way."
)

if __name__ == "__main__":
    print(f"[Condition] {config.CONDITION_LABEL}")
    print(f"  Tools:  {', '.join(config.ACTIVE_TOOL_NAMES)}")
    print(f"  Server: ws://{config.WEBSOCKET_HOST}:{config.WEBSOCKET_PORT}")
    asyncio.run(start_server(config.WEBSOCKET_HOST, config.WEBSOCKET_PORT))
