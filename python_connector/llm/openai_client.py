import json
from openai import AsyncOpenAI
import config
from llm.tool_definitions import TOOL_DEFINITIONS, select_tools
from llm.prompt_builder import build_system_prompt, build_baseline_prompt


client = AsyncOpenAI(api_key=config.OPENAI_API_KEY)


async def run_query(messages: list, tool_executor, metrics=None, transcript=None) -> str:
    """
    Send messages to GPT-4o-mini with tools. If it calls tools, execute them
    via tool_executor and feed results back. Repeat until final text answer.

    Args:
        messages: conversation history [{"role": ..., "content": ...}, ...]
        tool_executor: async callable(function_name, arguments, call_id) -> result_string
        metrics: optional QueryMetrics collector (tokens, tool count, iterations)
        transcript: optional list; each tool call is appended as
            {"tool": name, "arguments": args, "result": result} for logging

    Returns:
        The assistant's final text response.
    """
    for iteration in range(config.MAX_TOOL_ITERATIONS):
        response = await client.chat.completions.create(
            model=config.LLM_MODEL,
            messages=messages,
            tools=select_tools(config.ACTIVE_TOOL_NAMES),
            temperature=config.TEMPERATURE
        )

        if metrics:
            metrics.record_usage(response.usage)

        choice = response.choices[0]

        # Final text answer — no more tool calls
        if choice.finish_reason != "tool_calls":
            assistant_text = choice.message.content or ""
            messages.append({"role": "assistant", "content": assistant_text})
            return assistant_text

        # LLM wants to call tools
        assistant_msg = choice.message
        messages.append(assistant_msg.model_dump())

        for tool_call in assistant_msg.tool_calls:
            fn_name = tool_call.function.name
            fn_args = json.loads(tool_call.function.arguments)
            call_id = tool_call.id

            if metrics:
                metrics.record_tool_call(fn_name)

            print(f"  [Tool] {fn_name}({fn_args})")

            result = await tool_executor(fn_name, fn_args, call_id)

            if transcript is not None:
                transcript.append({
                    "tool": fn_name,
                    "arguments": fn_args,
                    "result": result,
                })

            messages.append({
                "role": "tool",
                "tool_call_id": call_id,
                "content": json.dumps(result) if not isinstance(result, str) else result
            })

    return "Sorry, I couldn't complete this request (too many tool calls)."


def init_conversation(context: dict, scenario: str = "enhanced") -> list:
    """Create initial messages list with system prompt from scene context."""
    if scenario == "baseline":
        system_prompt = build_baseline_prompt()
    else:
        system_prompt = build_system_prompt(context)

    # Per user-study condition: append the feedback-mode guidance (empty by default)
    if config.CONDITION_NOTE:
        system_prompt += f"\n\nFEEDBACK MODE:\n{config.CONDITION_NOTE}"

    language_rule = (
        f"CRITICAL LANGUAGE RULE: You MUST respond ONLY in {config.RESPONSE_LANGUAGE}. "
        f"Regardless of the language the user speaks or writes in, always reply in "
        f"{config.RESPONSE_LANGUAGE}, and never use any other language."
    )
    system_prompt = language_rule + "\n\n" + system_prompt

    return [{"role": "system", "content": system_prompt}]
