import os
from dotenv import load_dotenv

load_dotenv()

WEBSOCKET_HOST = "localhost"
WEBSOCKET_PORT = 8765

OPENAI_API_KEY = os.getenv("OPENAI_API_KEY")
LLM_MODEL = "gpt-4o-mini"
MAX_TOOL_ITERATIONS = 10
TEMPERATURE = 0.3
RESPONSE_LANGUAGE = "English"   # LLM must always reply in this language

# --- Text-to-speech (avatar voice) ---
TTS_ENABLED = True
TTS_MODEL   = "gpt-4o-mini-tts"   # cheapest good option; or "tts-1" / "tts-1-hd"
TTS_VOICE   = "alloy"             # alloy, echo, fable, onyx, nova, shimmer, coral, sage…

# Evaluation scenario: "enhanced" (full system) | "baseline" (no scene layout, no spatial context)
SCENARIO = "enhanced"

# --- User-study feedback condition (set by the condition_*.py launchers) ---
# Defaults apply when run via main.py: all tools available, no extra prompt.
CONDITION_LABEL = "dev_all"     # label written to each transcript line
ACTIVE_TOOL_NAMES = None        # None = expose all tools; else a list of tool names
CONDITION_NOTE = ""             # extra per-condition guidance appended to the system prompt
