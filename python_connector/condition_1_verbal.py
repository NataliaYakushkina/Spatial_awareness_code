"""User-study condition 1 — VERBAL ONLY.

Speech only: the avatar describes where objects are and how to reach them in
words. No object highlighting and no on-screen path.

Run this file (instead of main.py) to serve this condition, then launch the
Unity build. Start this backend BEFORE the Unity app so it can connect.
"""
import asyncio
import config
from server.websocket_server import start_server

config.CONDITION_LABEL = "1_verbal"
config.ACTIVE_TOOL_NAMES = [
    "find_objects", "get_objects_in_room", "get_nearby_objects",
    "get_user_context", "get_direction_to", "get_description",
    "check_affordance", "get_path_to_object",
]
config.CONDITION_NOTE = (
    "This session shows NO on-screen visual cues. Do not highlight objects and "
    "do not draw paths, and never tell the user that you have highlighted "
    "anything or shown a path. Describe where the object is and how to reach it "
    "in words only."
)

if __name__ == "__main__":
    print(f"[Condition] {config.CONDITION_LABEL}")
    print(f"  Tools:  {', '.join(config.ACTIVE_TOOL_NAMES)}")
    print(f"  Server: ws://{config.WEBSOCKET_HOST}:{config.WEBSOCKET_PORT}")
    asyncio.run(start_server(config.WEBSOCKET_HOST, config.WEBSOCKET_PORT))
