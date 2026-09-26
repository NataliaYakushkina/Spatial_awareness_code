import base64
from llm.openai_client import client   # reuse the existing AsyncOpenAI instance
import config


async def synthesize_speech(text: str) -> str | None:
    """
    Convert assistant text to speech via OpenAI TTS.

    Returns base64-encoded raw PCM (24 kHz, 16-bit signed, mono, little-endian),
    ready to embed in a JSON WebSocket message — or None if TTS is disabled,
    the text is empty, or synthesis fails (so the app still works text-only).
    """
    if not config.TTS_ENABLED or not text.strip():
        return None
    try:
        async with client.audio.speech.with_streaming_response.create(
            model=config.TTS_MODEL,
            voice=config.TTS_VOICE,
            input=text,
            response_format="pcm",   # raw 24 kHz PCM — trivial for Unity to play
        ) as response:
            audio_bytes = await response.read()
        return base64.b64encode(audio_bytes).decode("ascii")
    except Exception as e:
        print(f"[TTS] synthesis failed: {e}")
        return None
