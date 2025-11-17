import asyncio 
import json
from agent.player import Player
from agent.agent_config import PLAYER_INFO
from agent.prompts import format_prompt
from agent.actions import ActionMethod
from agent.world import World
from server import AgentServer
from typing import Dict,Any,List
from agent.runtime import AgentManager,AgentRuntimeCtx,WsDispatcher
import logging

logger = logging.getLogger(__name__)
logging.basicConfig(level=logging.INFO,format='%(asctime)s %(levelname)s %(message)s')

async def observe_snapshot(ctx:AgentRuntimeCtx) -> Dict[str,Any]:
    obs = await format_prompt(player=ctx.player,action_history=ctx.actions_history,world=ctx.world)
    return obs

async def plan_by_llm(ctx:AgentRuntimeCtx,obs:Dict[str,Any]) -> List[Dict[str,Any]]:
    prompts = json.dumps(obs,ensure_ascii=False)
    # Run blocking LLM call off the event loop to preserve concurrency
    actions = await asyncio.to_thread(ctx.player.agent.act, prompts)
    return actions
    
    
async def action_with_ws(ctx:AgentRuntimeCtx,action:Dict[str,Any]):
    status = await ctx.actionMethod.method_action(ctx,action)
    return status

async def main():
    # 开启ws服务
    wsserver = AgentServer()
    await wsserver.start()

    # 初始化玩家
    players:List[Player] = [Player.from_raw(id=id+1,raw=raw,player_num=len(PLAYER_INFO)) for id,raw in enumerate(PLAYER_INFO.values())]
    
    # 初始化世界
    world = World(players=players)
    world_lock = asyncio.Lock()
    
    # 等待所有agent连接
    needed = [f"agent-{p.id}" for p in players]
    logger.info(f"Waiting for agents: {needed}")
    while not all(wsserver.is_connected(k) for k in needed):
        await asyncio.sleep(0.5)
    
    logger.info("All agents connected: {}".format(wsserver.connected_ids()))


    # 创建agent运行环境
    dispatcher = WsDispatcher(wsserver)

    ctxs = [AgentRuntimeCtx(actionMethod=ActionMethod(),agent_id=f"agent-{p.id}",player=p,world=world,world_lock=world_lock,dispatch=dispatcher,actions_history=[],last_result=None,observe_fn=observe_snapshot,Plan_fn=plan_by_llm,act_fn=action_with_ws) for p in players]

    # 启动agent运行环境，并保持主协程存活
    mgr = AgentManager()
    await mgr.start(ctxs,tick_sleep=0.1)

    try:
        while True:
            await asyncio.sleep(3600)
    except (KeyboardInterrupt, asyncio.CancelledError):
        pass
    finally:
        await mgr.stop()
        await wsserver.stop()

if __name__ == '__main__':
    asyncio.run(main())
