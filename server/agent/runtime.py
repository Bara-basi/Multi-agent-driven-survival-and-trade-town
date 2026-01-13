from __future__ import annotations
import asyncio
from dataclasses import dataclass
import logging
from typing import Any, Dict, Awaitable, Callable, Optional, List

from .world import World
logger = logging.getLogger(__name__)

class ActionDispatcher:
    async def action(self,agent_id:str,cmd:str,target:str,cur_location:str,timeout:float = 25.0) -> Dict[str,Any]:
        raise NotImplementedError("This method should be overridden by subclasses.")

class WsDispatcher(ActionDispatcher):
    def __init__(self,server_module):
        self.server = server_module

    async def action(self,agent_id:str,cmd:str,target:str,cur_location:str,timeout:float = 25.0) -> Dict[str,Any]:
    
        msg = await self.server.send_action(
            agent_id=agent_id,
            cmd=cmd,
            target=target,
            cur_location=cur_location,
            timeout=timeout,
        )
        ok = bool(msg) and (msg.get('status') == 'ok' or msg.get('OK') is True)
        if ok:
            return {"OK": True, "MSG": "ok", "type": "complete"}
        return {"OK": False, "MSG": "failed or timeout", "type": "complete"}

# ObserveFn = Callable[['AgentRuntimeCtx'], Awaitable[Dict[str, Any]]]
PlanFn    = Callable[['AgentRuntimeCtx', str], Awaitable[str]]
SummaryFn    = Callable[['AgentRuntimeCtx', str], Awaitable[str]]
ActFn    = Callable[['AgentRuntimeCtx', str], Awaitable[List[Dict[str, Any]]]]
LinkFn     = Callable[['AgentRuntimeCtx', Dict[str, Any]], Awaitable[Dict[str, Any]]]


@dataclass
class AgentRuntimeCtx:
    actionMethod:Any
    agent_id:str
    player:Any
    world:World
    world_lock:asyncio.Lock
    dispatch:ActionDispatcher
    actions_history:List[Dict[str,Any]] 
  

    # observe_fn:ObserveFn
    plan:PlanFn
    act:ActFn
    summary:SummaryFn
    link:LinkFn

async def agent_loop(ctx:AgentRuntimeCtx,stop_event:asyncio.Event,tick_sleep:float=0.1):
    """Agent 主循环"""
    today = ctx.world.get_time().day
    summary = None
    plan = None
    max_step = 20
    try:
        while not stop_event.is_set():
            try:
                plan = await ctx.plan(ctx,summary)
            except Exception:
                logger.exception("大模型计划出错: %s", ctx.agent_id)
            step = 0
            while step < max_step:      
                actions = await ctx.act(ctx,plan)
                for action in actions or []:
                    if action.get("type") == "finish":
                        break
                    try:
                        res = await ctx.link(ctx,action)
                    except Exception:
                        logger.exception("action step failed for %s", ctx.agent_id)
                        res = {"OK": False, "MSG": "action 执行异常"}
                    if res.get("OK") != True:
                        # 仅测试，出问题直接终止
                        print("动作执行出错：",action)
                        print("返回结果：",res)
                        raise Exception("测试中断")
                        
                    
                    ctx.actions_history.append(action)
                    if not res.get("OK",False):
                        if res.get("MSG","") == "玩家死亡，游戏结束":
                            logger.info(f"Agent {ctx.agent_id} 死亡")
                            stop_event.set()
                        break
                    await asyncio.sleep(1.5)  # 动作间隔
                step += 1
            if step >= max_step:
                logger.error(f"Agent {ctx.agent_id} 达到最大步骤数 {max_step}，结束本轮行动")
            summary = await ctx.summary(ctx,plan)
                
            # 如果时间到了第二天，刷新商店库存
            day = ctx.world.get_time().day
            if today!= day:
                today = day
                async with ctx.world_lock:
                    ctx.world.update_market(ctx.world.locations['集市'])
            await asyncio.sleep(tick_sleep)
    except asyncio.CancelledError:
        logger.error(f"Agent {ctx.agent_id} 取消")
        raise
    except Exception as e:
        logger.exception(f"Agent {ctx.agent_id} 异常")

class AgentManager:
    def __init__(self):
        self._tasks:Dict[str,asyncio.Task] = {}
        self._stop_events = asyncio.Event()

    async def start(self,contexts:List[AgentRuntimeCtx],tick_sleep:float=0.1):
        for ctx in contexts:
            t = asyncio.create_task(agent_loop(ctx,self._stop_events,tick_sleep),name=f"agent-loop-{ctx.agent_id}")
            self._tasks[ctx.agent_id] = t

    async def stop(self):
        self._stop_events.set()
        await asyncio.sleep(0)
        for t in self._tasks.values():
            if not t.done():
                t.cancel()
        await asyncio.gather(*self._tasks.values(),return_exceptions=True)

    def task(self) -> Dict[str,asyncio.Task]:
        return self._tasks
