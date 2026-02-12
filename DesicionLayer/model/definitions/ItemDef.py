from dataclasses import dataclass 
from typing import Dict,Any

ItemId = str 

@dataclass(frozen=True,slots=True)
class ItemDef:
    id: ItemId
    name: str
    description: str = ""
    effects: Dict[str, str] = None  
    base_price: float = 0.0
    

    def snapshot(self) -> Dict[str, Any]:
        snapshot = {
            "name": self.name,
            "description":self.description,
            "base_price":self.base_price,
        }
        for effect,value in self.effects.items():
            snapshot[effect] = value




