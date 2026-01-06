from __future__ import annotations
from typing import List,Dict,Any
from .models.schema import Attribute, Container
from .agent_config import INVENTORY_SIZE,DECAY_PER_HOUR
from .utils import to_attr
from .agent import Agent
from dataclasses import dataclass, field
from typing import Dict, List, Any


@dataclass(slots=True)
class Player:
    """玩家状态（运行期业务对象）"""
    id: int
    agent:Agent
    
    identity: str = ""
    info: str = ""
    skill: str = ""
    money: float = 1000.0
    cur_location: str = "家"
    # 去掉三个属性，合并为attribute属性
    attribute: Dict[str, Attribute] = field(default_factory=dict)

    inventory: Container = field(
        default_factory=lambda: Container(name="背包", capacity=INVENTORY_SIZE)
    )
    home: str = "家"
    # 0=已知地区, 1=可访问, -1=不可访问
    accessible: Dict[str, int] = field(
        default_factory=dict
    )
    memory: List[str] = field(default_factory=list)

    @classmethod
    def from_raw(
        cls,
        id: int,
        raw: Dict[str, Any],
        locations: List[str] = [],
        player_num = 1,
    ) -> Player:
        """从原始数据构造玩家状态"""
        accessible_dict = {"集市":1,"森林":1,"河流":1}
        for location in locations or []:
            accessible_dict[location] = 1
        for i in range(1, player_num + 1):
            accessible_dict[f"玩家{i}的家"] = 0 if i == id else 1
        hunger = to_attr("饥饿值", current=100, decay_per_hour=DECAY_PER_HOUR["hunger"])
        thirst = to_attr("口渴值", current=100, decay_per_hour=DECAY_PER_HOUR["thirst"])
        fatigue = to_attr("疲劳值", current=100, decay_per_hour=DECAY_PER_HOUR["fatigue"])
        return cls(
            id=id,
            agent = Agent(f"Agent-{id}"),
            identity=str(raw.get("identity", "")),
            info=str(raw.get("info", "")),
            skill=str(raw.get("skill", "")),
            money=float(raw.get("money", 1000)),
            cur_location=f"家",
            home=f"玩家{id}的家",
            accessible=accessible_dict,
            attribute={"hunger":hunger,"thirst":thirst,"fatigue":fatigue},
        )
