from __future__ import annotations
from typing import Any 
from actions.action_registry import ActionContext,get_handler,resolve_name
from model.state.action_result import ActionResult
from action_registry import get_entry

class ActionExecutor:
    def __init__(self,world,dispatch,config,catalog,logger):
        self.ctx = ActionContext(world,dispatch,config,catalog,logger)

    def execute(self,action:Any) -> ActionResult:
        name = action.name
        if not name:
            return ActionResult(ok=False,code="INVALID",message="Action 没有 name 属性")
        entry = get_entry(name)
        for validator in entry.validators:
            maybe = validator(self.ctx,action)
            if maybe is not None:
                return maybe
        try:
            
            return entry.handler(self.ctx,action)
        except KeyError as e:
            return ActionResult(ok=False,code="NOT_FOUND",message=str(e))
        except Exception as e:
            self.ctx.logger.exception("执行动作失败")
            return ActionResult(ok=False,code="CRASH",message=f"执行动作失败："+str(e))
        