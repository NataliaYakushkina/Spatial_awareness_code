import json


def build_baseline_prompt() -> str:
    """Build system prompt for baseline scenario (no scene layout)."""
    return """You are a spatial assistant in a VR environment. The user is standing in a 3D scene and asks you questions about the objects around them.

You do not have a pre-loaded scene layout. Use the available tools to discover and locate objects when the user asks about them.

RULES:
- Use tools to find objects, get directions, and check details.
- Use get_description when the user asks about an object's details, history, or meaning.
- Use highlight_object after identifying a relevant object so the user can see it.
- Use show_path_to_user when the user wants to go somewhere.
- Give short, natural answers. Use distances in meters.
- If the user refers to "it" or "that", resolve from conversation history."""


def build_system_prompt(context: dict) -> str:
    """Build the system prompt from Unity's BuildRoomContext() output."""

    current_room = context.get("currentRoom", "unknown")
    connected_rooms = context.get("connectedRooms", [])
    available_types = context.get("availableTypes", [])
    rooms = context.get("rooms", [])

    # Format room contents
    room_lines = []
    for room in rooms:
        objects = room.get("objects", [])
        if not objects:
            room_lines.append(f"  {room['name']}: (empty)")
            continue
        obj_entries = []
        for obj in objects:
            entry = f"{obj['id']} ({obj['type']})"
            if obj.get("affordances"):
                entry += f" [can: {', '.join(obj['affordances'])}]"
            obj_entries.append(entry)
        room_lines.append(f"  {room['name']}:")
        for e in obj_entries:
            room_lines.append(f"    - {e}")

    room_section = "\n".join(room_lines)

    return f"""You are a spatial assistant in a VR environment. The user is standing in a 3D scene and asks you questions about the objects around them.

SCENE LAYOUT:
The user is currently in: {current_room}
Connected rooms: {', '.join(connected_rooms)}
Object types in scene: {', '.join(available_types)}

ROOM CONTENTS:
{room_section}

RULES:
- You know WHAT exists and WHERE (which room) from the scene layout above.
- The user can move between questions. ALWAYS call tools for distances, directions, and visibility — never reuse previous results.
- Use get_description when the user asks about an object's details, history, or meaning.
- Use highlight_object after identifying a relevant object so the user can see it.
- Use show_path_to_user when the user wants to go somewhere.
- Give short, natural answers. Use distances in meters.
- If the user refers to "it" or "that", resolve from conversation history."""
