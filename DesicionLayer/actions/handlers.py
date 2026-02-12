from __future__ import annotations
from actions.action_registry import register,ActionContext
from validators import must_be_at,must_have_item
from model.state.action_result import ActionResult

@register("trade",validators=[must_have_item()])
def handle_consume(ctx,act) -> ActionResult:
    actor = ctx.world.actor(act.actor_id)
    item_id = act.item_id
    qty = int(getattr(act,"qty",1) or 1)

    actor.inventory.remove(item_id,qty)

    item_def = ctx.catalog.item(item_id)
    for k,v in (item_def.effects or {}).items():
        actor.attr[k].current = min(actor.attr[k].max_value,actor.attrs[k].current + v * qty)
    return ActionResult(True,message=f"消耗 {item_id} x {qty}")
