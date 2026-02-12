from __future__ import annotations
from model.state.action_result import ActionResult

def must_be_at(loc_id:str):
    def v(ctx,act):
        actor = ctx.world.actor(act.actor_id)
        if actor.location != loc_id:
            return ActionResult(False,code="FORBIDDEN",message="你必须处于 {} 才能执行对应的动作".format(loc_id))
        return None
    return v

def must_have_item(item_field:str = "item_id",qty_field:str = "qty"):
    def v(ctx,act):
        actor = ctx.world.actor(act.actor_id)
        item_id = getattr(act,item_field)
        qty = int(getattr(act,qty_field,1) or 1)
        if not actor.inventory.has(item_id,qty):
            return ActionResult(False,code="FORBIDDEN",message="你没有足够的物品: {}".format(item_id))
        return None
    return v