import json
import logging
from typing import Any, Dict, List

from langchain_openai import ChatOpenAI
from langchain_core.messages import HumanMessage, SystemMessage
from langchain_core.output_parsers import PydanticOutputParser
from langchain_classic.output_parsers import (
    OutputFixingParser,
    PydanticOutputParser,
    RetryWithErrorOutputParser,
)
from .actions import ActionList

logger = logging.getLogger(__name__)


class Agent:
    def __init__(self, name: str, model: str = "Qwen/Qwen3-VL-32B-Instruct", **kwargs):
        self.llm = ChatOpenAI(model=model, temperature=0)
        self.name = name
        self.parser = PydanticOutputParser(pydantic_object=ActionList)
        self.fixer = OutputFixingParser.from_llm(parser=self.parser, llm=self.llm)
        self.retry = RetryWithErrorOutputParser.from_llm(
            parser=self.parser, llm=self.llm
        )
        self.state = kwargs.get("state", {})
        self.cfg = kwargs

    def act(self, message: str) -> List[Dict[str, Any]]:
        msgs = [
            SystemMessage(content="只输出动作JSON（可单个或数组），不要任何解释文本"),
            HumanMessage(content=message),
        ]
        resp = self.llm.invoke(msgs).content
        actions = None
        if isinstance(resp, str):
            draft = self._strip_code_fence(resp)
            try:
                actions_obj = self.fixer.parse(draft)
                actions = actions_obj.root
            except Exception:
                try:
                    single_parser = PydanticOutputParser(pydantic_object=ActionList)
                    single_fixer = OutputFixingParser.from_llm(
                        parser=single_parser, llm=self.llm
                    )
                    actions_obj = single_fixer.parse(draft)
                    actions = actions_obj.root
                except Exception:
                    return [{"type": "wait", "seconds": 1}]

            if isinstance(actions, list):
                return [
                    a.model_dump(exclude_none=True) if hasattr(a, "model_dump") else a
                    for a in actions
                ]
            elif actions is not None:
                return [
                    actions.model_dump(exclude_none=True)
                    if hasattr(actions, "model_dump")
                    else actions
                ]
        return [{"type": "wait", "seconds": 1}]

    def _strip_code_fence(self, s: str) -> str:
        s = s.strip()
        if s.startswith("```"):
            s = s.strip("` \n")
            if "\n" in s:
                s = s.split("\n", 1)[1]
            if s.endswith("```"):
                s = s[:-3].strip()
        return s


if __name__ == "__main__":
    a = Agent("test")
    print("Agent initialized:", a.name)

