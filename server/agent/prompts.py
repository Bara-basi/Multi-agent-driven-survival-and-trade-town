# -*- coding: utf-8 -*-
"""
提示词模板：System/Developer/User 三段拼装。
"""
import json
from datetime import datetime
from typing import Dict,Any
from .player import Player
from .world import World

loop = 0
async def format_prompt(player:Player,action_history:list,world:World) -> Dict[str,Any]:
    def _dump_model(value: Any) -> Dict[str, Any]:
        if value is None:
            return {}
        return value.model_dump() if hasattr(value, "model_dump") else {}

    inner_home_facilities = world.players_home[player.id].inner_facilities

    prompts ={
        "system_rules": "你收邀参加了一个贸易游戏，你被带到一个封闭的小镇，小镇中只有一个集市、一片森林、一条小河和几个玩家的住所。目标：在不死亡的前提下尽快赚到 ¥10,000。只准输出 JSON（单个动作或动作数组），不得包含任何其他文字。不得输出解释或中间计算过程。",
        "output_requirements": [
            "只输出 JSON（对象或对象数组），不含任何额外文字。",
            "同一响应最多 5 条动作，且不得对同一物品重复多条购买。"
        ],
        
        "world_state": {
            "角色卡": {
                "身份": player.identity,
                "资金": round(player.money,2),
                "当前位置": player.cur_location,
                "当前时间": world.get_time().strftime("%Y-%m-%d %H:%M:%S"),
               
            },
            
            "生存属性": {
                "饥饿值": {
                    "当前值":round(player.attribute['hunger'].current,2),
                    "下降速度(小时)":player.attribute['hunger'].decay_per_hour,
                },
                "疲劳值": {
                    "当前值":round(player.attribute['fatigue'].current,2),
                    "下降速度(小时)":player.attribute['fatigue'].decay_per_hour,
                },
                "水分值": {
                    "当前值":round(player.attribute['thirst'].current,2),
                    "下降速度(小时)":player.attribute['thirst'].decay_per_hour,
                }
            },
            "资产与设施": {
                "家": {
                    "冰箱": _dump_model(inner_home_facilities.get("冰箱")),
                    "床": _dump_model(inner_home_facilities.get("床")),
                    "锅": _dump_model(inner_home_facilities.get("锅")),
                    "储物柜": _dump_model(inner_home_facilities.get("储物柜")),
                },
                "背包": player.inventory.model_dump(),
                "地点与路途":{location_name:(location if isinstance(location,str) else _dump_model(location)) for location_name,location in world.get_snapshot(player.id).items()},
                "记忆": player.memory,
            }
        },
        "action_history":action_history,
        "warnings":["请勿输出任何与游戏规则无关的文字，否则将受到警告惩罚。","商店quanitity=0时无法购买物品","你的资金不够时不可以购买物品"],     
    }
    with open(f"debug_log/prompt{datetime.now().strftime('%Y%m%d%H%M%S')}.json","w",encoding="utf-8") as f:
        f.write(json.dumps(prompts,ensure_ascii=False))
    return prompts
    # return json.dumps(prompts,ensure_ascii=False)
