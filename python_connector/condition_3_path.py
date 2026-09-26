"""User-study condition 3 — SPEECH + PATH.

The avatar answers verbally and draws the route to the target on the floor,
telling the user it is showing the way. No object highlighting.

Run this file (instead of main.py) to serve this condition, then launch the
Unity build. Start this backend BEFORE the Unity app so it can connect.
"""
import asyncio
import config
from server.websocket_server import start_server

config.CONDITION_LABEL = "3_path"
config.ACTIVE_TOOL_NAMES = [
    "find_objects", "get_objects_in_room", "get_nearby_objects",
    "get_user_context", "get_direction_to", "get_description",
    "check_affordance", "get_path_to_object",
    "show_path_to_user",
]
config.CONDITION_NOTE = (
    "When guiding the user to a place or object, call show_path_to_user to draw "
    "the route on the floor and tell the user you are showing the way. Do NOT "
    "highlight objects and do not claim to have highlighted anything."
)

if __name__ == "__main__":
    print(f"[Condition] {config.CONDITION_LABEL}")
    print(f"  Tools:  {', '.join(config.ACTIVE_TOOL_NAMES)}")
    print(f"  Server: ws://{config.WEBSOCKET_HOST}:{config.WEBSOCKET_PORT}")
    asyncio.run(start_server(config.WEBSOCKET_HOST, config.WEBSOCKET_PORT))
