from __future__ import annotations
from dataclasses import dataclass
from typing import List, Dict, Any,Callable,Optional
from model.state.action_result import ActionResult

ActionValidator = Callable[["ActionContext",Any],Optional[ActionResult]]
ActionHandler = Callable[["ActionContext",Any], ActionResult]
_REGISTRY:Dict[str,Entry] = {}
_ALIASES:Dict[str,str] = {}

def register(action_name:str,*,aliases:Optional[list[str]] = None):
    def deco(fn:ActionHandler) -> ActionHandler:
        if action_name in _REGISTRY:
            raise ValueError(f"动作 {action_name} 已被注册")
        _REGISTRY[action_name] = fn
        for alias in aliases or []:
            _ALIASES[alias] = action_name
        return fn
    return deco

def resolve_name(name:str) -> str:
    return _ALIASES.get(name,name)

# def get_handler(name:str) -> ActionHandler:
#     name = resolve_name(name)
#     if name not in _REGISTRY:
#         raise ValueError(f"动作 {name} 未被注册")
#     return _REGISTRY[name]


def get_entry(name:str) -> Entry:
    name = resolve_name(name)
    if name not in _REGISTRY:
        raise ValueError(f"动作 {name} 未被注册")
    return _REGISTRY[name]



@dataclass
class ActionContext:
    """动作上下文"""
    world:Any 
    dispatch:Any 
    config:Any
    catalog:Any
    logger:Any

@dataclass
class Entry:
    handler:ActionHandler
    validator:List[ActionValidator]