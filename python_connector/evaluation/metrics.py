from dataclasses import dataclass, field


@dataclass
class QueryMetrics:
    """Collects metrics during a single run_query() call."""

    prompt_tokens: int = 0
    completion_tokens: int = 0
    total_tokens: int = 0
    tool_call_count: int = 0
    tool_calls_made: list = field(default_factory=list)
    llm_iterations: int = 0
    total_time_s: float = 0.0

    def record_usage(self, usage):
        """Called after each LLM API call with response.usage object."""
        self.prompt_tokens += usage.prompt_tokens
        self.completion_tokens += usage.completion_tokens
        self.total_tokens += usage.total_tokens
        self.llm_iterations += 1

    def record_tool_call(self, function_name: str):
        self.tool_call_count += 1
        self.tool_calls_made.append(function_name)

    def to_dict(self) -> dict:
        return {
            "prompt_tokens": self.prompt_tokens,
            "completion_tokens": self.completion_tokens,
            "total_tokens": self.total_tokens,
            "tool_call_count": self.tool_call_count,
            "tool_calls_made": self.tool_calls_made,
            "llm_iterations": self.llm_iterations,
            "total_time_s": round(self.total_time_s, 3),
        }
