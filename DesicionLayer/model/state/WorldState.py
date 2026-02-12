from typing import Dict
from datetime import datetime
from dataclasses import dataclass,field
from model.definitions.Catalog import Catalog
from model.state.ActorState import ActorState
from model.definitions.LocationDef import LocationId
from model.definitions.ActorDef import ActorId
from model.state.LocationState import LocationState
from actions.hooks import ON_DAILY_SETTLE

@dataclass(slots=True)
class WorldState:
    day:int = 0
    catalog:Catalog
    actors:Dict[ActorId,ActorState] = field(default_factory=dict)
    locations:Dict[LocationId,LocationState] = field(default_factory=dict)

    def actor(self, actor_id:ActorId) -> ActorState:
        return self.actors[actor_id]
    
    def loc(self, loc_id:LocationId) -> LocationState:
        return self.locations[loc_id]
    
    def update_day(self):

        """更新日期并处理每日结算
        
        该方法用于推进游戏中的日期，并对所有地点进行每日更新，
        最后执行每日结算操作。
        """
        self.day += 1  # 增加游戏天数
        for location in self.locations.values():  # 遍历所有地点
            location.update_day()  # 对每个地点进行每日更新
        ON_DAILY_SETTLE(self)

    def observe(self,actor_id:ActorId):
        actor = self.actor(actor_id)
        actor_snapshot = {
            "name":actor.name,
            "home":actor.home,
            "cur_location":actor.location,
            "money":actor.money,
            "thirst":actor.attrs["thirst"].current,
            "hunger":actor.attrs["hunger"].current,
            "fatigue":actor.attrs["fatigue"].current,
            "inventory":actor.inventory.snapshot(),
        }
        location_snapshot = {}
        for loc_id,location in self.locations.items():
            location_snapshot[loc_id] = location.observe()
        catalog_snapshot = self.catalog.snapshot()
        working_events = [e.name for e in actor.working_events]
        return {
            "actor_snapshot": actor_snapshot,
            "day": self.day,
            "location_snapshot": location_snapshot,
            "catalog_snapshot": catalog_snapshot,
            "working_events": working_events
        }
         
        