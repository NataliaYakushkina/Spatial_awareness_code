import json
from dataclasses import dataclass, field


@dataclass
class TestCase:
    id: str
    question: str
    category: str  # "identification" | "direction_distance" | "description" | "navigation"
    expected_keywords: list = field(default_factory=list)
    mock_tool_results: dict = field(default_factory=dict)


# --- Shared mock scene context (mirrors BuildRoomContext output) ---

MOCK_CONTEXT = {
    "currentRoom": "Room_01",
    "connectedRooms": ["Corridor"],
    "availableTypes": ["Painting", "Lamp", "Sofa", "Piano", "Window", "Pouf", "Robot"],
    "rooms": [
        {
            "name": "Room_01",
            "objects": [
                {"id": "Vincent van Gogh the night cafe", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh the large plane trees road menders at saint-remy", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh landscape at saint-remy enclosed field with peasant", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh roses in green vase", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh brown vase with roses", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh corridor in the asylum", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh self-portrait with bandaged ear", "type": "Painting", "affordances": ["view"]},
                {"id": "piano_01", "type": "Piano", "affordances": ["play", "view"]},
                {"id": "Vincent van Gogh bouquet of flowers in a vase", "type": "Painting", "affordances": ["view"]},
                {"id": "sofa_01", "type": "Sofa", "affordances": ["sit", "rest", "view"]},
                {"id": "Vincent van Gogh woman walking in garden", "type": "Painting", "affordances": ["view"]},
                {"id": "pouf_01", "type": "Pouf", "affordances": ["sit", "rest", "view"]},
                {"id": "Vincent van Gogh tree roots", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh self-portrait 1887 paris", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh wheat fields with cypresses", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh the night cafe terrase", "type": "Painting", "affordances": ["view"]},
                {"id": "window_01", "type": "Window", "affordances": ["open", "view"]},
                {"id": "Vincent van Gogh portrait of joseph roulin", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh women picking olives", "type": "Painting", "affordances": ["view"]},
                {"id": "sofa_02", "type": "Sofa", "affordances": ["sit", "rest", "view"]},
                {"id": "Vincent van Gogh seascape saintes-maries", "type": "Painting", "affordances": ["view"]},
                {"id": "lamp_01", "type": "Lamp", "affordances": ["toggle"]},
                {"id": "Vincent van Gogh cypresses", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh irises", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh wheatfield under thunderclouds", "type": "Painting", "affordances": ["view"]},
            ]
        },
        {
            "name": "Room_02",
            "objects": [
                {"id": "lamp_02", "type": "Lamp", "affordances": ["toggle"]},
                {"id": "Dali sans titre", "type": "Painting", "affordances": ["view"]},
                {"id": "Dali dream caused by flight of bee", "type": "Painting", "affordances": ["view"]},
                {"id": "window_02", "type": "Window", "affordances": ["open", "view"]},
                {"id": "Dali swans reflecting elephants", "type": "Painting", "affordances": ["view"]},
                {"id": "Dali the temptation of saint anthony", "type": "Painting", "affordances": ["view"]},
                {"id": "Dali family of marsupial centaurs", "type": "Painting", "affordances": ["view"]},
                {"id": "sofa_04", "type": "Sofa", "affordances": ["sit", "rest", "view"]},
                {"id": "Dali the persistence of memory", "type": "Painting", "affordances": ["view"]},
                {"id": "Dali geopoliticus child", "type": "Painting", "affordances": ["view"]},
                {"id": "Dali the elephants", "type": "Painting", "affordances": ["view"]},
                {"id": "Dali the great masturbator", "type": "Painting", "affordances": ["view"]},
            ]
        },
        {
            "name": "Corridor",
            "objects": [
                {"id": "Vincent van Gogh self-portrait with a hat", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh madame joseph-michel ginoux", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh the old town in the fields", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh starry night over the rhone", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh self-portrait with straw hat", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh almond blossom", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh farmhouse in provence", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh the potato easters", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh wheatfield with crows", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh sunflowers aries vase series", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh bouquet of flowers in a vase with black background", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh self-portrait saint-remy", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh the starry night", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh fishing in springpont de clichy", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh two cut sunflowers", "type": "Painting", "affordances": ["view"]},
                {"id": "Vincent van Gogh old man in sorrow", "type": "Painting", "affordances": ["view"]},
            ]
        },
        {
            "name": "Room_03",
            "objects": [
                {"id": "sofa_06", "type": "Sofa", "affordances": []},
                {"id": "Dali galatea of the spheres", "type": "Painting", "affordances": ["view"]},
                {"id": "Dali microphysical madonna", "type": "Painting", "affordances": ["view"]},
                {"id": "Dali eucharistic still life", "type": "Painting", "affordances": ["view"]},
                {"id": "Painting_3.08", "type": "Painting", "affordances": ["view"]},
                {"id": "sofa_05", "type": "Sofa", "affordances": ["sit", "rest", "view"]},
                {"id": "Dali disintegration persistence of memory", "type": "Painting", "affordances": ["view"]},
                {"id": "Dali portrait young woman landscape", "type": "Painting", "affordances": ["view"]},
                {"id": "Dali madonna of port lligat first version", "type": "Painting", "affordances": ["view"]},
                {"id": "Dali port lligat at sunset", "type": "Painting", "affordances": ["view"]},
                {"id": "window_03", "type": "Window", "affordances": ["view"]},
                {"id": "piano_02", "type": "Piano", "affordances": ["view"]},
            ]
        },
    ]
}


# Simulated user position: Room_01, facing "self-portrait with bandaged ear"
MOCK_SPATIAL_CONTEXT = {
    "room": "Room_01",
    "nearest": [
        {"id": "Vincent van Gogh self-portrait with bandaged ear", "type": "Painting", "distance": 1.2, "direction": "ahead"},
        {"id": "Vincent van Gogh corridor in the asylum", "type": "Painting", "distance": 2.1, "direction": "to your left"},
        {"id": "Vincent van Gogh roses in green vase", "type": "Painting", "distance": 3.5, "direction": "to your right"},
        {"id": "piano_01", "type": "Piano", "distance": 4.0, "direction": "behind you"},
        {"id": "Vincent van Gogh bouquet of flowers in a vase", "type": "Painting", "distance": 4.8, "direction": "ahead and to your right"},
    ]
}


# --- Mock tool results ---
# These simulate what Unity's FunctionDispatcher would return.
# Keys are function names; values are JSON strings.

_MOCK_FIND_PAINTING = json.dumps([
    {"objectId": "Vincent van Gogh self-portrait with bandaged ear", "type": "Painting", "room": "Room_01", "distance": 1.2, "relativeDirection": "ahead", "isVisible": True},
    {"objectId": "Vincent van Gogh corridor in the asylum", "type": "Painting", "room": "Room_01", "distance": 2.1, "relativeDirection": "to your left", "isVisible": True},
    {"objectId": "Vincent van Gogh roses in green vase", "type": "Painting", "room": "Room_01", "distance": 3.5, "relativeDirection": "to your right", "isVisible": True},
    {"objectId": "Vincent van Gogh the night cafe", "type": "Painting", "room": "Room_01", "distance": 5.0, "relativeDirection": "behind you", "isVisible": False},
    {"objectId": "Dali the persistence of memory", "type": "Painting", "room": "Room_02", "distance": 18.3, "relativeDirection": "behind you and to your right", "isVisible": False},
])

_MOCK_FIND_SOFA = json.dumps([
    {"objectId": "sofa_01", "type": "Sofa", "room": "Room_01", "distance": 5.2, "relativeDirection": "to your right", "isVisible": True},
    {"objectId": "sofa_02", "type": "Sofa", "room": "Room_01", "distance": 6.8, "relativeDirection": "behind you and to your left", "isVisible": False},
    {"objectId": "sofa_04", "type": "Sofa", "room": "Room_02", "distance": 19.5, "relativeDirection": "behind you", "isVisible": False},
])

_MOCK_FIND_PIANO = json.dumps([
    {"objectId": "piano_01", "type": "Piano", "room": "Room_01", "distance": 4.0, "relativeDirection": "behind you", "isVisible": True},
    {"objectId": "piano_02", "type": "Piano", "room": "Room_03", "distance": 22.1, "relativeDirection": "behind you and to your right", "isVisible": False},
])

_MOCK_FIND_WINDOW = json.dumps([
    {"objectId": "window_01", "type": "Window", "room": "Room_01", "distance": 6.5, "relativeDirection": "to your left", "isVisible": True},
    {"objectId": "window_02", "type": "Window", "room": "Room_02", "distance": 20.0, "relativeDirection": "behind you", "isVisible": False},
    {"objectId": "window_03", "type": "Window", "room": "Room_03", "distance": 24.0, "relativeDirection": "behind you and to your right", "isVisible": False},
])

_MOCK_NEARBY = json.dumps([
    {"objectId": "Vincent van Gogh self-portrait with bandaged ear", "type": "Painting", "distance": 1.2, "room": "Room_01"},
    {"objectId": "Vincent van Gogh corridor in the asylum", "type": "Painting", "distance": 2.1, "room": "Room_01"},
    {"objectId": "Vincent van Gogh roses in green vase", "type": "Painting", "distance": 3.5, "room": "Room_01"},
    {"objectId": "piano_01", "type": "Piano", "distance": 4.0, "room": "Room_01"},
    {"objectId": "Vincent van Gogh bouquet of flowers in a vase", "type": "Painting", "distance": 4.8, "room": "Room_01"},
])

_MOCK_USER_CONTEXT = json.dumps({
    "currentRoom": "Room_01",
    "facing": "north",
    "nearestObjects": [
        {"objectId": "Vincent van Gogh self-portrait with bandaged ear", "type": "Painting", "distance": 1.2, "direction": "ahead"},
        {"objectId": "Vincent van Gogh corridor in the asylum", "type": "Painting", "distance": 2.1, "direction": "to your left"},
        {"objectId": "Vincent van Gogh roses in green vase", "type": "Painting", "distance": 3.5, "direction": "to your right"},
    ]
})

_MOCK_DIR_BANDAGED_EAR = json.dumps({
    "objectId": "Vincent van Gogh self-portrait with bandaged ear",
    "distance": 1.2, "relativeDirection": "ahead", "isVisible": True, "room": "Room_01"
})

_MOCK_DIR_CORRIDOR = json.dumps({
    "objectId": "Vincent van Gogh corridor in the asylum",
    "distance": 2.1, "relativeDirection": "to your left", "isVisible": True, "room": "Room_01"
})

_MOCK_DIR_ROSES = json.dumps({
    "objectId": "Vincent van Gogh roses in green vase",
    "distance": 3.5, "relativeDirection": "to your right", "isVisible": True, "room": "Room_01"
})

_MOCK_DIR_PIANO_01 = json.dumps({
    "objectId": "piano_01",
    "distance": 4.0, "relativeDirection": "behind you", "isVisible": True, "room": "Room_01"
})

_MOCK_DIR_WINDOW_01 = json.dumps({
    "objectId": "window_01",
    "distance": 6.5, "relativeDirection": "to your left", "isVisible": True, "room": "Room_01"
})

_MOCK_DIR_STARRY_NIGHT = json.dumps({
    "objectId": "Vincent van Gogh the starry night",
    "distance": 12.4, "relativeDirection": "behind you", "isVisible": False, "room": "Corridor"
})

_MOCK_DESC_BANDAGED_EAR = json.dumps({
    "objectId": "Vincent van Gogh self-portrait with bandaged ear",
    "type": "Painting",
    "description": "Self-Portrait with Bandaged Ear, painted in January 1889. A three-quarter-length self-portrait of Van Gogh wearing a blue-green fur cap and a green overcoat, with a thick white bandage covering his right ear. Oil on canvas, 60 x 49 cm. Located at The Courtauld Gallery, London."
})

_MOCK_DESC_CORRIDOR = json.dumps({
    "objectId": "Vincent van Gogh corridor in the asylum",
    "type": "Painting",
    "description": "Corridor in the Asylum, painted in September 1889. A long, arched corridor in the asylum of Saint-Paul-de-Mausole in Saint-Remy-de-Provence, seen in sharply receding one-point perspective. Oil and essence over black chalk on pink laid paper, 65.1 x 49.1 cm. Located at Metropolitan Museum of Art, New York."
})

_MOCK_DESC_PERSISTENCE = json.dumps({
    "objectId": "Dali the persistence of memory",
    "type": "Painting",
    "description": "The Persistence of Memory, painted in 1931 by Salvador Dali. Depicts a barren, twilight coastal landscape populated by four pocket watches in various states of limp, melting distortion. Oil on canvas, 24.1 x 33 cm. Located at Museum of Modern Art (MoMA), New York."
})

_MOCK_DESC_PIANO = json.dumps({
    "objectId": "piano_01",
    "type": "Piano",
    "description": "A grand piano placed in Room_01 of the gallery."
})

_MOCK_OBJECTS_ROOM_01 = json.dumps([
    {"objectId": "Vincent van Gogh the night cafe", "type": "Painting"},
    {"objectId": "Vincent van Gogh the large plane trees road menders at saint-remy", "type": "Painting"},
    {"objectId": "Vincent van Gogh landscape at saint-remy enclosed field with peasant", "type": "Painting"},
    {"objectId": "Vincent van Gogh roses in green vase", "type": "Painting"},
    {"objectId": "Vincent van Gogh brown vase with roses", "type": "Painting"},
    {"objectId": "Vincent van Gogh corridor in the asylum", "type": "Painting"},
    {"objectId": "Vincent van Gogh self-portrait with bandaged ear", "type": "Painting"},
    {"objectId": "piano_01", "type": "Piano"},
    {"objectId": "Vincent van Gogh bouquet of flowers in a vase", "type": "Painting"},
    {"objectId": "sofa_01", "type": "Sofa"},
    {"objectId": "Vincent van Gogh woman walking in garden", "type": "Painting"},
    {"objectId": "pouf_01", "type": "Pouf"},
    {"objectId": "Vincent van Gogh tree roots", "type": "Painting"},
    {"objectId": "Vincent van Gogh self-portrait 1887 paris", "type": "Painting"},
    {"objectId": "Vincent van Gogh wheat fields with cypresses", "type": "Painting"},
    {"objectId": "Vincent van Gogh the night cafe terrase", "type": "Painting"},
    {"objectId": "window_01", "type": "Window"},
    {"objectId": "Vincent van Gogh portrait of joseph roulin", "type": "Painting"},
    {"objectId": "Vincent van Gogh women picking olives", "type": "Painting"},
    {"objectId": "sofa_02", "type": "Sofa"},
    {"objectId": "Vincent van Gogh seascape saintes-maries", "type": "Painting"},
    {"objectId": "lamp_01", "type": "Lamp"},
    {"objectId": "Vincent van Gogh cypresses", "type": "Painting"},
    {"objectId": "Vincent van Gogh irises", "type": "Painting"},
    {"objectId": "Vincent van Gogh wheatfield under thunderclouds", "type": "Painting"},
])

_MOCK_PATH_STARRY_NIGHT = json.dumps({
    "reachable": True,
    "totalDistance": 12.4,
    "directions": ["Go straight and walk 3.5m", "Turn left and walk 5.2m", "Turn right and walk 3.7m"],
    "destinationRoom": "Corridor"
})

_MOCK_PATH_ROOM_03 = json.dumps({
    "reachable": True,
    "totalDistance": 18.6,
    "directions": ["Go straight and walk 3.5m", "Turn left and walk 8.0m", "Turn right and walk 7.1m"],
    "destinationRoom": "Room_03"
})

_MOCK_PATH_PIANO_02 = json.dumps({
    "reachable": True,
    "totalDistance": 22.1,
    "directions": ["Go straight and walk 3.5m", "Turn left and walk 8.0m", "Turn right and walk 10.6m"],
    "destinationRoom": "Room_03"
})

_MOCK_HIGHLIGHT_OK = json.dumps({"success": True})

_MOCK_SHOW_PATH_OK = json.dumps({"success": True})


# --- Default mock tool handler ---
# Maps (function_name) -> result string.
# The runner will use these as fallback when a test case doesn't override.

DEFAULT_MOCK_RESULTS = {
    "find_objects": {
        "Painting": _MOCK_FIND_PAINTING,
        "Sofa": _MOCK_FIND_SOFA,
        "Piano": _MOCK_FIND_PIANO,
        "Window": _MOCK_FIND_WINDOW,
    },
    "get_objects_in_room": {
        "Room_01": _MOCK_OBJECTS_ROOM_01,
    },
    "get_nearby_objects": _MOCK_NEARBY,
    "get_user_context": _MOCK_USER_CONTEXT,
    "get_direction_to": {
        "Vincent van Gogh self-portrait with bandaged ear": _MOCK_DIR_BANDAGED_EAR,
        "Vincent van Gogh corridor in the asylum": _MOCK_DIR_CORRIDOR,
        "Vincent van Gogh roses in green vase": _MOCK_DIR_ROSES,
        "piano_01": _MOCK_DIR_PIANO_01,
        "window_01": _MOCK_DIR_WINDOW_01,
        "Vincent van Gogh the starry night": _MOCK_DIR_STARRY_NIGHT,
    },
    "get_description": {
        "Vincent van Gogh self-portrait with bandaged ear": _MOCK_DESC_BANDAGED_EAR,
        "Vincent van Gogh corridor in the asylum": _MOCK_DESC_CORRIDOR,
        "Dali the persistence of memory": _MOCK_DESC_PERSISTENCE,
        "piano_01": _MOCK_DESC_PIANO,
    },
    "highlight_object": _MOCK_HIGHLIGHT_OK,
    "show_path_to_user": _MOCK_SHOW_PATH_OK,
    "get_path_to_object": {
        "Vincent van Gogh the starry night": _MOCK_PATH_STARRY_NIGHT,
        "Room_03": _MOCK_PATH_ROOM_03,
        "piano_02": _MOCK_PATH_PIANO_02,
    },
    "check_affordance": json.dumps({"possible": True, "reason": "Yes, you can play the piano."}),
}


# --- Test Cases (20 total, 5 per category) ---

TEST_CASES = [
    # ==================== IDENTIFICATION ====================
    TestCase(
        id="ident_01",
        question="What painting is in front of me?",
        category="identification",
        expected_keywords=["bandaged ear", "self-portrait"],
    ),
    TestCase(
        id="ident_02",
        question="Are there any Dalí paintings in this room?",
        category="identification",
        expected_keywords=["no", "Room_02", "Room_03"],
    ),
    TestCase(
        id="ident_03",
        question="What is the nearest painting to my left?",
        category="identification",
        expected_keywords=["corridor", "asylum"],
    ),
    TestCase(
        id="ident_04",
        question="How many sofas are in this room?",
        category="identification",
        expected_keywords=["2", "two"],
    ),
    TestCase(
        id="ident_05",
        question="What objects besides paintings are in this room?",
        category="identification",
        expected_keywords=["piano", "sofa", "pouf"],
    ),

    # ==================== DIRECTION & DISTANCE ====================
    TestCase(
        id="dir_01",
        question="How far is the piano?",
        category="direction_distance",
        expected_keywords=["4", "behind"],
    ),
    TestCase(
        id="dir_02",
        question="Where is the nearest sofa?",
        category="direction_distance",
        expected_keywords=["sofa_01", "5", "right"],
    ),
    TestCase(
        id="dir_03",
        question="Can I see the Van Gogh roses painting from here?",
        category="direction_distance",
        expected_keywords=["yes", "visible", "right"],
    ),
    TestCase(
        id="dir_04",
        question="What direction is the window?",
        category="direction_distance",
        expected_keywords=["left", "window_01"],
    ),
    TestCase(
        id="dir_05",
        question="What is the closest object to me?",
        category="direction_distance",
        expected_keywords=["bandaged ear", "1.2"],
    ),

    # ==================== DESCRIPTION ====================
    TestCase(
        id="desc_01",
        question="Tell me about that painting",
        category="description",
        expected_keywords=["bandaged ear", "1889", "self-portrait"],
    ),
    TestCase(
        id="desc_02",
        question="What is The Persistence of Memory about?",
        category="description",
        expected_keywords=["melting", "watches", "Dalí"],
    ),
    TestCase(
        id="desc_03",
        question="Describe the painting to my left",
        category="description",
        expected_keywords=["corridor", "asylum", "Saint-R"],
    ),
    TestCase(
        id="desc_04",
        question="Tell me about the piano",
        category="description",
        expected_keywords=["piano"],
    ),
    TestCase(
        id="desc_05",
        question="What painting is nearest to me and what does it depict?",
        category="description",
        expected_keywords=["bandaged ear", "self-portrait"],
    ),

    # ==================== NAVIGATION ====================
    TestCase(
        id="nav_01",
        question="How do I get to the Dalí paintings?",
        category="navigation",
        expected_keywords=["Room_02", "Room_03", "corridor"],
    ),
    TestCase(
        id="nav_02",
        question="Take me to The Starry Night",
        category="navigation",
        expected_keywords=["starry night", "corridor"],
    ),
    TestCase(
        id="nav_03",
        question="Where is the nearest exit from this room?",
        category="navigation",
        expected_keywords=["corridor"],
    ),
    TestCase(
        id="nav_04",
        question="Guide me to Room_03",
        category="navigation",
        expected_keywords=["corridor", "18", "Room_03"],
    ),
    TestCase(
        id="nav_05",
        question="How do I get to the other piano?",
        category="navigation",
        expected_keywords=["piano_02", "Room_03"],
    ),
]
