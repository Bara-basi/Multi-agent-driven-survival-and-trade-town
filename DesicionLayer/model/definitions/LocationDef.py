from dataclasses import dataclass
from typing import Dict,Any 

LocationId = str

@dataclass(forzen=True,slots=True)
class LocationDef: 
    id: LocationId
    name: str
    description: str

    def snapshot(self):
        return {
            "name": self.name,
            "description": self.description
        }