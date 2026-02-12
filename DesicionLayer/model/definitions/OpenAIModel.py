from __future__ import annotations
import os
from typing import Optional, Any
from openai import OpenAI


class LLM:

    def __init__(
        self,
        model_name: str = "gpt-4.1-mini-2025-04-14",
        api_key: str = os.getenv("OPENAI_API_KEY"),
    ):
        self.client = OpenAI(api_key=api_key)
        self.model_name = model_name

    def generate(self, prompt: str, restrict: Optional[str] = None) -> Any:
        """
        restrict:
            - None: 普通文本输出
            - "json": 强制输出合法 JSON（API 级约束）
        """

        # 基础参数
        kwargs = {
            "model": self.model_name,
            "messages": [
                {"role": "user", "content": prompt}
            ],
        }

        # JSON 强约束
        if restrict == "json":
            kwargs["response_format"] = {
                "type": "json_object"
            }

        response = self.client.chat.completions.create(**kwargs)

        content = response.choices[0].message.content

        # 如果是 JSON 模式，直接反序列化更安全
        if restrict == "json":
            import json
            return json.loads(content)

        return content



if __name__ == "__main__":
    llm = LLM()
    print(llm.generate("你好,请用json和我打招呼",restrict="json"))