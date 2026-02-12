from __future__ import annotations
import asyncio,logging
from dataclasses import dataclass,field
from typing import Any, Dict, List, Optional, Tuple
import time 
from model.state.action_result import ActionResult
from model.state.WorldState import WorldState
from model.brains.AgentBrain import Agent
from actions.executor import ActionExecutor
from config.runtime_config import AgentRuntimeConfig
from model.definitions.LocationDef import LocationId
from model.state.ActorState import ActorState  
from  model.definitions.Action import Action


logger = logging.getLogger(__name__)
logging.basicConfig(level=logging.INFO,format='%(asctime)s %(levelname)s %(message)s')
logging.getLogger('httpx').setLevel(logging.WARNING)
logging.getLogger('openai').setLevel(logging.WARNING)
logging.getLogger('urllib3').setLevel(logging.WARNING)
# 世界快照，用于模型提示词
@dataclass 
class Observation:
    act_id:int
    actor_snapshot:Dict[str,Any]
    day:int 
    location_snapshot:Dict[str,Any]
    catalog_snapshot:Dict[str,Any]
    working_events:List[str] = field(default_factory=list)
    # effects:List[str] = field(default_factory=list)

class AgentRuntime:
    def __init__(
        self,
        *,
        world:WorldState, 
        agent:Agent,
        executor:ActionExecutor,
        config:Optional[AgentRuntimeConfig] = None,
        logger:Optional[Any] = None,
    ):  
        self.world = world
        self.agent = agent
        self.executor = executor
        self.config = config or AgentRuntimeConfig()
        self.logger = logger

        self._actors:Dict[int,ActorState] = {}

        self._agent_sem = asyncio.Semaphore(self.config.max_concurrent_agents)
        self._llm_sem = asyncio.Semaphore(self.config.max_concurrent_llm)
        self._unity_sem = asyncio.Semaphore(self.config.max_concurrent_unity)

    def _st(self,actor_id:int) -> ActorState:
        st = self._actors.get(actor_id)
        if st is None:
            st = ActorState(actor_id=actor_id)
            self._actors[actor_id] = st
        return st
    
    def _obs(self,actor_id:int) -> Observation:
        s = self.world.observe(actor_id)
        return Observation(
            actor_id = actor_id,
            actor_snapshot = s["actor_snapshot"],
            day = s["day"],
            location_snapshot = s["location_snapshot"],
            catalog_snapshot = s["catalog_snapshot"],
            working_events = s["working_events"],
            # effects = s["effects"]
        )
    
    def _should_plan(self,st:ActorState,obs:Observation) -> bool:
        if st.plan is None:
            return True
        if st.step - st.last_plan_step > self.config.plan_min_interval_steps:
            return True
        if st.last_result and (not st.last_result.ok) and st.last_result.code in  ("FORBIDDEN", "INVALID", "BLOCKED", "NO_ACTION"):
            return True
        return False
    
    def _should_reflect(self,st:ActorState,obs:Observation) -> bool:
        if st.last_result is None:
            return True
        if obs.day - st.last_result.day >= 1:
            return True
        if self.step - st.last_reflect_step > self.config.reflect_min_interval_steps:
            return True
        return False
    
    async def tick_actor(self,actor_id:int) -> ActionResult:
        async with self._agent_sem:
            st = self._st(actor_id)
            st.step += 1
            obs = self._obs(actor_id)
            if self._should_plan(st,obs):
                async with self._llm_sem:
                    try:
                        st.plan = await asyncio.wait_for(
                            self.brain.plan(obs),timeout=self.config.llm_timeout_s
                        )
                    except asyncio.TimeoutError:
                        self.memory.append_working_event(actor_id, "[系统] 规划超时，沿用旧计划")
                        logger.error(f"LLM timeout for actor {actor_id}")
                    except Exception as e:
                        self.memory.append_working_event(actor_id, f"[系统] 规划异常: {e}")
                        logger.error(f"LLM error for actor {actor_id}: {e}")

            last_err:Optional[ActionResult] = None
            for _ in range(self.config.max_action_retries + 1):
                async with self._llm_sem:
                    try:
                        proposal = await asyncio.wait_for(
                            self.agent.act(obs),timeout=self.config.llm_timeout_s
                        )
                    except asyncio.TimeoutError:
                        last_err = ActionResult(
                            False,code="TIMEOUT",message="动作生成超时"
                        )
                        proposal = None
                    except Exception as e:
                        last_err = ActionResult(
                            False,code="CRASH",message=f"动作生成异常: {e}"
                        )
                        proposal = None
                
                if proposal is None:
                    continue

            if proposal is None:
                res = last_err or ActionResult(False, code="NO_ACTION", msg="No valid action")
                st.last_result = res
                self._ledger(actor_id, obs, None, res)
                return res
            async with self._unity_sem:
                res = await self.executor.execute(actor_id, proposal,timeout_s=self.config.unity_ack_timeout_s)
            
            st.last_result = res
            self._ledger(actor_id, obs, proposal, res)
            self.memory.append_working_event(actor_id, res.event or f"{proposal.name} -> {res.code}")

            if self._should_reflect(st, obs):
                async with self._llm_sem:
                    try:
                        patch = await asyncio.wait_for(self.brain.reflect(obs, res), timeout=self.cfg.llm_timeout_s)
                        if patch:
                            self.memory.apply_reflection_patch(actor_id, patch)
                        st.last_reflect_step = st.step
                    except Exception as e:
                        self.memory.append_working_event(actor_id, f"[系统] 反思异常: {e}")

            return res

    def _ledger(self,actor_id:int,obs:Observation,action:Optional[Action],result:ActionResult):
        self.memory.append_ledger(actor_id,obs,{
            "day":str(obs.day),
            "actor_id":actor_id,
            "location": obs,
            "action": None if action is None else {"name": action.name, "params": action.params, "rationale": action.rationale},
            "result": {"ok": result.ok, "code": result.code, "msg": result.msg, "delta": result.delta, "event": result.event},
            "snapshot": {"money": obs.money, "attrs": obs.attrs, "inventory": obs.inventory},
        })

    async def run_tick(self, *, dt: float = 1.0) -> None:

        actor_ids = self.world.list_actor_ids()
        await asyncio.gather(*(self.tick_actor(aid) for aid in actor_ids))
        self.world.step_time(dt)