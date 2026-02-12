from __future__ import annotations
from dataclasses import dataclass
from typing import Optional,Dict,Any

@dataclass
class Action:
    name:str 
    params:Dict[str,Any]
    