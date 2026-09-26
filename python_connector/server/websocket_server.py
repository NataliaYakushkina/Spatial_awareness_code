import asyncio
import json
import os
import re
import time
from datetime import datetime
import websockets
import config
from llm.openai_client import run_query, init_conversation
from llm.tts import synthesize_speech
from evaluation.metrics import QueryMetrics

RESULTS_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "results")


def update_room_in_prompt(prompt: str, new_room: str) -> str:
    """Replace 'The user is currently in: X' in the system prompt with the new room."""
    return re.sub(
        r"The user is currently in: .+",
        f"The user is currently in: {new_room}",
        prompt
    )


class UnityConnection:
    def __init__(self, websocket):
        self.ws = websocket
        self.pending_tool_results = {}  # call_id -> asyncio.Future
        self.messages = None  # conversation history, initialized on first query
        self.current_stop = "unlabeled"  # set via "#stop <label>" chat command
        os.makedirs(RESULTS_DIR, exist_ok=True)
        self.log_path = os.path.join(
            RESULTS_DIR, f"transcript_{datetime.now().strftime('%Y%m%d_%H%M%S')}.jsonl"
        )

    def _log(self, record: dict):
        """Append one query record to the session transcript (JSONL)."""
        record["timestamp"] = datetime.now().isoformat(timespec="seconds")
        record["stop"] = self.current_stop
        record["condition"] = config.CONDITION_LABEL
        with open(self.log_path, "a", encoding="utf-8") as f:
            f.write(json.dumps(record, ensure_ascii=False) + "\n")

    async def handle_messages(self):
        async for raw_message in self.ws:
            message = json.loads(raw_message)
            msg_type = message.get("type")

            if msg_type == "query":
                asyncio.create_task(self.handle_query(message))
            elif msg_type == "tool_result":
                call_id = message["call_id"]
                if call_id in self.pending_tool_results:
                    self.pending_tool_results[call_id].set_result(message["result"])

    async def handle_query(self, message):
        user_text = message["text"]
        context = message.get("context")

        print(f"[Query] {user_text}")

        # First query (or first message carrying context): initialize conversation.
        # Done before the #stop check so a "#stop" first message still seeds context.
        if self.messages is None and context:
            self.messages = init_conversation(context, config.SCENARIO)
            print(f"\n{'='*80}")
            print(f"[SYSTEM PROMPT]\n{self.messages[0]['content']}")
            print(f"{'='*80}\n")

        # Evaluation labelling command: "#stop <label>" tags subsequent log lines
        # with the current stop and does not query the model.
        stripped = user_text.strip()
        if stripped.lower().startswith("#stop"):
            self.current_stop = stripped[len("#stop"):].strip() or "unlabeled"
            print(f"[Eval] Current stop set to: {self.current_stop}")
            await self.ws.send(json.dumps({
                "type": "final_response",
                "text": f"[eval] stop set to: {self.current_stop}",
                "actions": []
            }))
            return

        # Safety: if somehow no messages yet, create empty conversation
        if self.messages is None:
            self.messages = [{"role": "system", "content": "You are a spatial assistant."}]

        # Build user message with spatial context (enhanced only)
        spatial = message.get("spatial_context")
        if spatial:
            room = spatial.get("room", "unknown")
            nearest = spatial.get("nearest", [])
            parts = [f"{o['id']} ({o['type']}) {o['distance']}m {o['direction']}" for o in nearest]
            context_line = f"[User is in {room}. Nearest: {', '.join(parts)}]"
            full_text = f"{context_line}\n{user_text}"

            # Also update system prompt room
            if self.messages:
                self.messages[0]["content"] = update_room_in_prompt(
                    self.messages[0]["content"], room
                )
        else:
            full_text = user_text

        # Add user message
        self.messages.append({"role": "user", "content": full_text})

        print(f"\n{'-'*80}")
        print(f"[TO LLM] {full_text}")
        print(f"{'-'*80}\n")

        # Run LLM with tool-calling loop, collecting metrics + a tool transcript
        metrics = QueryMetrics()
        transcript = []
        t0 = time.perf_counter()
        response_text = await run_query(
            self.messages, self.execute_tool_in_unity, metrics, transcript
        )
        latency_s = time.perf_counter() - t0

        print(f"[Response] {response_text}")

        # Persist a structured record for grading the RQ1 benchmark
        self._log({
            "query": user_text,
            "sent_to_llm": full_text,
            "spatial_context": spatial,
            "answer": response_text,
            "tools": transcript,
            "tokens": metrics.total_tokens,
            "tool_count": metrics.tool_call_count,
            "llm_iterations": metrics.llm_iterations,
            "latency_s": round(latency_s, 2),
        })

        audio_b64 = await synthesize_speech(response_text)

        await self.ws.send(json.dumps({
            "type": "final_response",
            "text": response_text,
            "audio": audio_b64,                 # base64 PCM, or null if TTS off/failed
            "audio_format": "pcm_s16le_24000",  # 24 kHz, 16-bit mono, little-endian
            "actions": []
        }))

    async def execute_tool_in_unity(self, function_name, arguments, call_id):
        """Forward a tool call to Unity and wait for the result."""
        future = asyncio.get_event_loop().create_future()
        self.pending_tool_results[call_id] = future

        await self.ws.send(json.dumps({
            "type": "tool_call",
            "call_id": call_id,
            "function": function_name,
            "arguments": arguments
        }))

        result = await asyncio.wait_for(future, timeout=10.0)
        del self.pending_tool_results[call_id]
        return result


async def start_server(host="localhost", port=8765):
    async with websockets.serve(
        lambda ws: UnityConnection(ws).handle_messages(),
        host, port
    ):
        print(f"WebSocket server running on ws://{host}:{port}")
        await asyncio.Future()  # run forever
