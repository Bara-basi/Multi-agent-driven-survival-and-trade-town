from __future__ import annotations
from dataclasses import dataclass,field
from typing import Dict,Any,Optional

@dataclass 
class ActionResult:
    success:bool
    message:str = ""
    code:str = "OK"
    delta:Dict[str,Any] = field(default_factory=dict)
    event:Optional[str] = None