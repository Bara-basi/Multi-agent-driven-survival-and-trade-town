from __future__ import annotations

"""SiliconFlow/OpenAI-compatible Chat Completions wrapper.

The project no longer uses OpenAI Responses API.  All stages now call the
OpenAI-compatible Chat Completions endpoint exposed by SiliconFlow.
"""

import json
import os
from typing import Any, Optional

try:
    from openai import AsyncOpenAI, OpenAI
except Exception:
    AsyncOpenAI = None
    OpenAI = None


DEFAULT_BASE_URL = "https://api.siliconflow.cn/v1"
DEFAULT_MODEL = "Pro/moonshotai/Kimi-K2.6"
DEFAULT_TEMPERATURE = 0.7
DEFAULT_TOP_P = 0.9


class LLM:
    def __init__(
        self,
        model_name: str = DEFAULT_MODEL,
        api_key: Optional[str] = None,
        base_url: Optional[str] = None,
        reasoning_effort: str = "low",
        verbosity: str = "medium",
        temperature: float = DEFAULT_TEMPERATURE,
        top_p: float = DEFAULT_TOP_P,
    ):
        self.model_name = model_name or DEFAULT_MODEL
        self.api_key = (
            api_key
            or os.getenv("SILICONFLOW_API_KEY")
            or os.getenv("SILICONFLOW_KEY")
            or os.getenv("API_KEY")
        )
        self.base_url = base_url or os.getenv("SILICONFLOW_BASE_URL") or DEFAULT_BASE_URL
        self.reasoning_effort = reasoning_effort
        self.verbosity = verbosity
        self.temperature = temperature
        self.top_p = top_p
        self.client = None
        self.async_client = None

        if OpenAI is None or AsyncOpenAI is None:
            raise RuntimeError(
                "openai package is required for remote LLM calls. "
                "Install it in the Python environment that runs DecisionLayer."
            )

        if not self.api_key:
            raise RuntimeError(
                "Missing SiliconFlow API key. Set SILICONFLOW_API_KEY, "
                "SILICONFLOW_KEY, API_KEY, or pass api_key=... when constructing LLM."
            )

        self.client = OpenAI(api_key=self.api_key, base_url=self.base_url)
        self.async_client = AsyncOpenAI(api_key=self.api_key, base_url=self.base_url)

    @staticmethod
    def _json_loads(content: str) -> Any:
        if not content:
            raise ValueError("model returned empty JSON content")
        return json.loads(content)

    def _build_chat_kwargs(
        self,
        *,
        model: Optional[str],
        prompt: str,
        restrict: Optional[str],
        reasoning_effort: Optional[str],
        thinking: Optional[str],
        stream: bool,
        seed: Optional[int],
    ) -> dict[str, Any]:
        kwargs: dict[str, Any] = {
            "model": model or self.model_name,
            "messages": [{"role": "user", "content": prompt}],
            "temperature": self.temperature,
            "top_p": self.top_p,
            "verbosity": self.verbosity,
            "stream": stream,
        }

        if seed is not None:
            kwargs["seed"] = seed

        if restrict == "json":
            kwargs["response_format"] = {"type": "json_object"}

        thinking_mode = (thinking or "").lower()
        if thinking_mode in {"disabled", "off"}:
            # OpenAI SDK sends extra_body fields as top-level request body keys.
            # SiliconFlow uses enable_thinking to switch GLM reasoning on/off.
            kwargs["extra_body"] = {"enable_thinking": False}
        elif thinking_mode == "low":
            kwargs["extra_body"] = {"enable_thinking": True, "thinking_budget": 1024}
        else:
            kwargs["reasoning_effort"] = reasoning_effort or self.reasoning_effort

        return kwargs

    def generate(
        self,
        prompt: str,
        restrict: Optional[str] = None,
        *,
        model: Optional[str] = None,
        reasoning_effort: Optional[str] = None,
        thinking: Optional[str] = None,
        stream: bool = False,
        seed: Optional[int] = None,
    ) -> Any:
        if self.client is None:
            raise RuntimeError("LLM client is not initialized")

        kwargs = self._build_chat_kwargs(
            model=model,
            prompt=prompt,
            restrict=restrict,
            reasoning_effort=reasoning_effort,
            thinking=thinking,
            stream=stream,
            seed=seed,
        )
        resp = self.client.chat.completions.create(**kwargs)
        content = resp.choices[0].message.content

        if restrict == "json":
            return self._json_loads(content)
        return content

    async def agenerate(
        self,
        model: Optional[str],
        prompt: str,
        restrict: Optional[str] = None,
        resoning: Optional[str] = None,
        reasoning_effort: Optional[str] = None,
        thinking: Optional[str] = None,
        stream: bool = False,
        seed: Optional[int] = None,
    ) -> Any:
        # Keep the old misspelled parameter working because current callers use it.
        effort = reasoning_effort or resoning or self.reasoning_effort

        if self.async_client is None:
            raise RuntimeError("Async LLM client is not initialized")

        kwargs = self._build_chat_kwargs(
            model=model,
            prompt=prompt,
            restrict=restrict,
            reasoning_effort=effort,
            thinking=thinking,
            stream=stream,
            seed=seed,
        )
        resp = await self.async_client.chat.completions.create(**kwargs)
        content = resp.choices[0].message.content

        if restrict == "json":
            return self._json_loads(content)
        return content
