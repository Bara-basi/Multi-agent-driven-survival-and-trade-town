"""World/player state schemas (Pydantic).

Moved from `agent/schema.py` to keep all data models under `agent/models`.
"""

from __future__ import annotations

from typing import Any, Dict

from pydantic import BaseModel, ConfigDict, Field


class Attribute(BaseModel):
    """人物属性，随时间下降"""

    model_config = ConfigDict(extra="forbid", validate_assignment=True)

    name: str
    current: float
    decay_per_hour: float
    max_value: float = 100.0


class Location(BaseModel):
    model_config = ConfigDict(extra="forbid", validate_assignment=True)

    name: str
    description: str
    inner_things: Dict[str, Any] = Field(default_factory=dict)  # 内部设施
    


class Container(BaseModel):
    model_config = ConfigDict(extra="forbid", validate_assignment=True)

    name: str
    # capacity: float = Field(default=0)
    items: Dict[str, "Item"] = Field(default_factory=dict)
    description: str = ""


class Item(BaseModel):
    """道具信息"""

    model_config = ConfigDict(extra="forbid", validate_assignment=True)
    name: str
    quantity: int
    description: str = ""
    function: Dict[str, Any] = Field(default_factory=dict)


class Market(BaseModel):
    model_config = ConfigDict(extra="forbid", validate_assignment=True)
    description: str
    items: Dict[str, Dict[str, Any]] = Field(..., description="商品列表")

