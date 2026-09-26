# Spatial Awareness in AI Avatars

Prototype from the bachelor thesis *Spatial Awareness in AI Avatars*. The user
walks through a virtual room in Unity and asks an LLM-driven agent (GPT-4o mini)
where objects are and how to reach them. The agent answers by voice and can
highlight objects or draw a path on the floor.

The system has two parts that talk over a WebSocket (`ws://localhost:8765`):

| Folder | Role |
|---|---|
| `Spatial_Awareness/` | Unity project: the room, the scene graph, the player and the visual cues. It executes the agent's tool calls (distances, directions, paths, highlighting). |
| `python_connector/` | Python backend: builds the prompt, calls the OpenAI API, runs the tool-calling loop and generates the avatar voice. |

## Requirements

- **Unity 6000.3.10f1** (install via Unity Hub)
- **Python 3.10+**
- An **OpenAI API key** with access to `gpt-4o-mini`, `gpt-4o-mini-tts` and `whisper-1`
- A microphone (for speech input)

## Setup

### 1. Python backend

```bash
cd python_connector
python -m venv .venv
# Windows:
.venv\Scripts\activate
# macOS/Linux:
source .venv/bin/activate

pip install -r requirements.txt
```

Copy `python_connector/.env.example` to `python_connector/.env` and enter your key:

```
OPENAI_API_KEY=sk-...
```

### 2. Unity project

1. In Unity Hub, choose **Add → Add project from disk** and select the `Spatial_Awareness` folder.
2. Open it with Unity **6000.3.10f1**. The first import takes a few minutes because Unity downloads the packages and rebuilds the `Library` folder.
3. Open the scene `Assets/Scenes/SceneGraph.unity`.

### 3. API key for speech input (Whisper)

Speech input is transcribed in Unity by the `WhisperTranscriber` component, which
calls the OpenAI Whisper API directly. It needs its own copy of the key:

1. In the Hierarchy, find the GameObject that has the **WhisperTranscriber** component
   (use the Hierarchy search: `t:WhisperTranscriber`).
2. In the Inspector, paste your OpenAI key into the **Api Key** field.

> **Do not commit the scene after entering the key.** The key is stored in plain
> text inside `SceneGraph.unity`. Clear the field again before committing.

## Running

1. **Start the backend first:**

   ```bash
   cd python_connector
   python main.py
   ```

   `main.py` enables all tools (highlighting and path). To run one of the four
   user-study conditions instead, start the matching launcher:

   | Script | Condition |
   |---|---|
   | `condition_1_verbal.py` | Speech only, no visual cues |
   | `condition_2_highlight.py` | Speech + object highlighting |
   | `condition_3_path.py` | Speech + path on the floor |
   | `condition_4_both.py` | Speech + highlighting + path |

2. **Press Play in Unity.** The Unity client connects to the backend on startup.

## Controls

| Input | Action |
|---|---|
| `W` `A` `S` `D` | Move |
| Mouse | Look around |
| **Hold right mouse button** | Speak a question (released = sent) |
| `Left Alt` | Unlock / lock the mouse cursor |

## Output

The backend writes one transcript per session to `python_connector/results/`
(`transcript_<date>_<time>.jsonl`). This folder is excluded from git.
