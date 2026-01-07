"""Action schemas (Pydantic) for LLM <-> server JSON protocol.

Extracted from `agent/actions.py` so schema definitions don't sit with business
logic (which is a common source of circular imports).
"""

from __future__ import annotations

from typing import Annotated, List, Literal, Optional, Union

from pydantic import BaseModel, Field, RootModel


class Move(BaseModel):
    type: Literal["move"]
    target: str


class Consume(BaseModel):
    type: Literal["consume"]
    item: str
    qty: int | None = None


class Sleep(BaseModel):
    type: Literal["sleep"]
    minutes: float


class Cook(BaseModel):
    type: Literal["cook"]
    input: str
    tool: str | None = None


class Fishing(BaseModel):
    type: Literal["fishing"]
    minutes: float | None = 10


class Trade(BaseModel):
    type: Literal["trade"]
    mode: Literal["buy", "sell", "exchange"]
    item: str
    qty: Optional[int]
    with_: str | None = None
    get_item: str | None = None
    get_qty: Optional[int]


class Store(BaseModel):
    type: Literal["store"]
    item: str
    qty: int
    container: str


class Retrieve(BaseModel):
    type: Literal["retrieve"]
    item: str
    qty: int
    container: str


class Talk(BaseModel):
    type: Literal["talk"]
    to: str
    content: str


class Wait(BaseModel):
    type: Literal["wait"]
    seconds: float


Action = Annotated[
    Union[Move, Consume, Sleep, Cook, Fishing, Trade, Store, Retrieve, Talk, Wait],
    Field(discriminator="type"),
]


class ActionList(RootModel[Union[Action, List[Action]]]):
    """允许返回单个动作或动作列表"""

    pass

