from dataclasses import dataclass
from model.definitions.ItemDef import ItemId, ItemDef
from model.definitions.ActorDef import ActorId, ActorDef
from model.definitions.LocationDef import LocationId, LocationDef
from typing import Dict, Any


@dataclass(slots=True)
class Catalog:
    items: Dict[ItemId, ItemDef]
    locations: Dict[LocationId, LocationDef]
    actors: Dict[ActorId, ActorDef]

    def item(self, item_id: ItemId) -> ItemDef:
        return self.items[item_id]

    def loc(self, loc_id: LocationId) -> LocationDef:
        return self.locations[loc_id]
    
    def actor(self,actor_id:ActorId) -> ActorDef:
        return self.actors[actor_id]    
    def snapshot(self) -> Dict[str, Any]:
        return {
            "items": {k: v.snapshot() for k, v in self.items.items()},
            "locations": {k: v.snapshot() for k, v in self.locations.items()},
            "actors": {k: v.snapshot() for k, v in self.actors.items()},
        }