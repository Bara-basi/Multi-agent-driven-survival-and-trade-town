from dataclasses import dataclass 
from typing import Optional,Dict,Any 

ActorId = str 

@dataclass(frozen=True,slots=True)
class ActorDef:
    id:ActorId
    name:str 
    description:str=""
    skill:function=None