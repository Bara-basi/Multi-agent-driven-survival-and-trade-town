"""游戏世界与玩家状态的结构化表示（State Normalizer 用到）"""
from __future__ import annotations
from typing import Any,Optional,Dict
from pydantic import BaseModel,Field

class Attribute(BaseModel):
    """人物属性，随时间下降"""
    name:str
    current:float
    decay_per_hour:float
    max_value:float = 100.0


class Location(BaseModel):
    name:str
    # distance:int # 单位（h）
    description:str
    inner_facilities:Dict[str,Any] = Field(default_factory=dict) # 内部设施


class Container(BaseModel):
    name:str
    capacity:float = Field(default=0)
    items:  Dict[str,Item] = Field(default_factory=dict)


class Item(BaseModel):
    """道具信息"""
    name:str
    quantity:int 
    description:str = ""
    function:Dict[str,Any] = {}


class Market(BaseModel):
    description:str
    items:Dict[str,Dict[str,Any]] = Field(...,description="商品列表")