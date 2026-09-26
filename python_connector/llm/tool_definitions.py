TOOL_DEFINITIONS = [
    {
        "type": "function",
        "function": {
            "name": "find_objects",
            "description": "Find objects in the scene by their semantic type (e.g. 'painting', 'sofa', 'lamp'). Returns all matching objects with their distance and direction relative to the user.",
            "parameters": {
                "type": "object",
                "properties": {
                    "object_type": {
                        "type": "string",
                        "description": "The type of object to search for (e.g. 'painting', 'sofa', 'window')"
                    }
                },
                "required": ["object_type"]
            }
        }
    },
    {
        "type": "function",
        "function": {
            "name": "get_objects_in_room",
            "description": "List all objects in a specific room.",
            "parameters": {
                "type": "object",
                "properties": {
                    "room_name": {
                        "type": "string",
                        "description": "The room name (e.g. 'Room_01', 'Room_02', 'Room_03', 'Corridor')"
                    }
                },
                "required": ["room_name"]
            }
        }
    },
    {
        "type": "function",
        "function": {
            "name": "get_nearby_objects",
            "description": "Find objects near the user within a given radius, sorted by distance.",
            "parameters": {
                "type": "object",
                "properties": {
                    "radius": {
                        "type": "number",
                        "description": "Search radius in meters (default: 5.0)"
                    }
                },
                "required": []
            }
        }
    },
    {
        "type": "function",
        "function": {
            "name": "get_user_context",
            "description": "Get the user's current position, which room they are in, and what direction they are facing.",
            "parameters": {
                "type": "object",
                "properties": {},
                "required": []
            }
        }
    },
    {
        "type": "function",
        "function": {
            "name": "get_direction_to",
            "description": "Get the distance and relative direction from the user to a specific object. Also reports if the object is visible and accessible.",
            "parameters": {
                "type": "object",
                "properties": {
                    "object_id": {
                        "type": "string",
                        "description": "The unique ID of the target object (e.g. 'table_01')"
                    }
                },
                "required": ["object_id"]
            }
        }
    },
    {
        "type": "function",
        "function": {
            "name": "highlight_object",
            "description": "Highlight an object in the VR scene so the user can see it. Use this after identifying the relevant object.",
            "parameters": {
                "type": "object",
                "properties": {
                    "object_id": {
                        "type": "string",
                        "description": "The unique ID of the object to highlight"
                    }
                },
                "required": ["object_id"]
            }
        }
    },
    {
        "type": "function",
        "function": {
            "name": "get_path_to_object",
            "description": "Get the navigation path from the user to an object. The path respects walls and goes through doors and corridors. Returns waypoints and total distance.",
            "parameters": {
                "type": "object",
                "properties": {
                    "object_id": {
                        "type": "string",
                        "description": "The unique ID of the destination object"
                    }
                },
                "required": ["object_id"]
            }
        }
    },
    {
        "type": "function",
        "function": {
            "name": "show_path_to_user",
            "description": "Visually display the navigation path to an object on the floor of the VR scene. Use this when the user wants to be guided somewhere.",
            "parameters": {
                "type": "object",
                "properties": {
                    "object_id": {
                        "type": "string",
                        "description": "The unique ID of the destination object"
                    }
                },
                "required": ["object_id"]
            }
        }
    },
    {
        "type": "function",
        "function": {
            "name": "check_affordance",
            "description": "Check if a specific action is possible with an object (e.g., can the user sit on it, read it, open it).",
            "parameters": {
                "type": "object",
                "properties": {
                    "object_id": {
                        "type": "string",
                        "description": "The unique ID of the object"
                    },
                    "action": {
                        "type": "string",
                        "description": "The action to check (e.g. 'sit', 'read', 'open', 'view', 'play')"
                    }
                },
                "required": ["object_id", "action"]
            }
        }
    },
    {
        "type": "function",
        "function": {
            "name": "get_description",
            "description": "Get the detailed description of a specific object. Use this when the user asks about what an object is, its history, or wants more details.",
            "parameters": {
                "type": "object",
                "properties": {
                    "object_id": {
                        "type": "string",
                        "description": "The unique ID of the object (e.g. 'piano_01')"
                    }
                },
                "required": ["object_id"]
            }
        }
    }
]


def select_tools(names=None):
    """Return the tool definitions whose names are in `names`.

    Used to restrict the tools offered to the LLM per user-study condition.
    `names=None` (or empty) returns the full tool set (default behaviour).
    """
    if not names:
        return TOOL_DEFINITIONS
    allowed = set(names)
    return [t for t in TOOL_DEFINITIONS if t["function"]["name"] in allowed]
