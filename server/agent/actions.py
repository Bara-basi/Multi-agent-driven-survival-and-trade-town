"""
动作执行逻辑（ActionMethod）。

动作相关的 Pydantic 数据模型已迁移到 `agent.models.actions`，避免 schema 和业务
代码混在同一文件里导致的循环导入。
"""
import json
import logging
import random
import time
from dataclasses import dataclass
from typing import Any, Dict,List,Optional

from .agent_config import TIME_RATIO
from .models.actions import ActionList
from .models.schema import Container, Item, Market
from .world import World
logger = logging.getLogger(__name__)

__all__ = ["ActionList", "ActionMethod"]






@dataclass
class ActionMethod:
    
    async def method_action(self,ctx,action:Dict[str,Any]) -> Dict[str,Any]:

        player = ctx.player
        dispatch = ctx.dispatch
        world = ctx.world
        agent_id = ctx.agent_id
        world_lock = ctx.world_lock
        action_type = action.get("type")
        try:
            if action_type == "consume":
                return await self.consume(action, player, world, dispatch, agent_id)
            elif action_type == "cook":
                return await self.cook(action, world, player, agent_id, dispatch)
            elif action_type == "trade":
                return await self.trade(player, action, agent_id, dispatch, world, world_lock)
            elif action_type == "talk":
                return await self.talk()
            elif action_type == "wait":
                return await self.wait(dispatch, agent_id, action, player)
            elif action_type == "store":
                return await self.store(action, world, player)
            elif action_type == "retrieve":
                return await self.retrieve(action, world, player)
            elif action_type == "fishing":
                return await self.fishing(action, player, dispatch, agent_id, world)
            elif action_type == "move":
                return await self._move(world, player, agent_id, dispatch, target=action["to"])
            elif action_type == "sleep":
                return {}
            else:
                return {"action": action_type or "unknown", "OK": False, "MSG": "未知的动作类型"}
        except Exception:
            logger.exception("Action execution failed: %s", action)
            return {"action": action_type or "unknown", "OK": False, "MSG": "服务端处理动作时出现异常"}

    
    async def _move(self,world,player,agent_id,dispatch,target:str,inner_target:str|None=None) -> Dict[str,Any]:
        '''玩家移动
        1) 时间消耗
        2) 改变位置
        3) 扣减状态值
        4) 同步记忆
        '''
        if inner_target is None:
            inner_target = target # 屋内导航点不存在则改为使用默认导航点
        if target not in world.locations:
            return {
                'action':"move",
                'target':target,
                'OK':False,
                'MSG':f"移动失败,目标{target}不存在"
            }
        if player.accessible[target] == 1: # 1表示未知，0代表已知
            player.accessible[target] = 0
        
        begin_time = time.time()     
        msg = await dispatch.action(agent_id=agent_id,cmd="go_to",target=inner_target,cur_location=player.cur_location)
        if not msg or not msg.get("OK"):
            return {"action": "move", "target": target, "OK": False, "MSG": "前端移动失败或超时"}
        time_cost=round((time.time() - begin_time))*TIME_RATIO # 实际时间消耗
        logger.info("移动耗时: %s", time_cost)
        res = self._update_attribute(player,time_cost/3600)
        if not res:
            return {
                'action':"move",
                'target':target,
                'OK':False,
                'MSG':f"玩家死亡，游戏结束"
            }
        memory = {world.get_time().strftime("%Y-%m-%d %H:%M"):f"你离开{player.cur_location},来到{target}"}
        player.memory.append(json.dumps(memory,ensure_ascii=False))
        player.cur_location = target
        return {
            'action':"move",
            'target':target,
            'OK':True,
            'MSG':f"移动成功,消耗{time_cost/60:.1f}分钟时间",
            'cost':time_cost
        }

    
    async def consume(self,action,player,world,dispatch,agent_id) -> Dict[str,Any]:
        """消耗物品:包括食物、背包（用于扩容）、
        - 减少背包中物品
        - 触发物品对应效果
        - 回复背包容量
        - 加入记忆
        """
        
        item:str = action['item']
        qty:int = action.get('qty',1)
        msg = self._ensure_item(action,world,player.inventory,item,qty)
        if msg is not None:
            return msg
        item_data:Dict[str,Any] = world.item_data[item]
        if item_data.get('consumable') is not None:

            # 前端动画
            msg = await dispatch.action(agent_id=agent_id,cmd="consume",target=item,cur_location=player.cur_location)
            if not msg or not msg.get("OK"):
                return {"action": "consume", "target": item, "OK": False, "MSG": "前端使用物品动画失败或超时"}
            effect_data:Dict[str,Any] = item_data['consumable']['effect']
            # 触发属性回复
            for attr,value in effect_data.items():
                player.attribute[attr].current = min(player.attribute[attr].current + value,100)

            
           
            # 减少背包物品
            self._decreace_qty(world,player.inventory,item,qty)
            # 加入记忆
            memory = {world.get_time().strftime("%Y-%m-%d %H:%M"):f"你消耗了{qty}个{item}"}
            player.memory.append(json.dumps(memory,ensure_ascii=False))
            return {
                'action':"consume",
                'item':item,
                'OK':True,
                'MSG':f"消耗{item}成功,",
                'effect':effect_data,
                'qty':qty,
                'capacity':qty*item_data.get('unit_capacity',0)
            }
        elif item_data.get('equipment'):
            msg = await dispatch.action(agent_id=agent_id,cmd="equip",target=item,cur_location=player.cur_location)
            if not msg or not msg.get("OK"):
                return {"action": "consume", "OK": False, "MSG": "前端装备物品动画失败或超时"}
            # 装备背包（一次性扩容），回复容量= 扩容值+背包本身占用空间     
            player.inventory.capacity += (item_data["equipment"]['capacity_bonus'])*qty
            self._decreace_qty(world,player.inventory,item,qty)
            memory = {world.get_time().strftime("%Y-%m-%d %H:%M"):f"你装备了{item}"}
            player.memory.append(json.dumps(memory,ensure_ascii=False))
            return {
                'action':"consume",
                'item':item,
                'OK':True,
                'MSG':f"装备{item}成功,",
                'qty':qty,
                'capacity':qty*item_data.get('unit_capacity',0)
            }
        else:
            return {
                'action':"consume",
                'item':item,
                'OK':False,
                'MSG':f"消耗失败,物品{item}没有效果或效果未定义"
            }
            
    
    
    async def cook(self,action,world,player,agent_id,dispatch) -> Dict[str,Any]:
        """烹饪
        - 物品消耗
        - 得到物品
        - 消耗燃料
        - 属性消耗
        """
        item = action['input']
        tool = action.get('tool', "锅")
        item_meta = world.item_data.get(item) or {}
        ingredient = item_meta.get("ingredient") or {}
        cooked_item = ingredient.get("result_item")
        if not cooked_item or cooked_item not in world.item_data:
            return {
                "action": "cook",
                "input": item,
                "OK": False,
                "MSG": "烹饪结果物品未配置",
            }
        if not self._has_capacity(world, player.inventory, cooked_item, 1):
            return {
                "action": "cook",
                "input": item,
                "OK": False,
                "MSG": "背包空间不足，无法烹饪",
            }
        msg = self._ensure_item(action,world,player.inventory,item,1)
        if msg is not None:
            return msg
        msg = self._ensure_item(action,world,player.inventory,"燃料罐",1)
        if msg is not None:
            return msg
        if tool != "锅" and tool != "便携炉":
            return {
                'action':"cook",   
                'input':item,
                'OK':False,
                'MSG':f"工具{tool}不存在"
            }
        if tool == "锅":
            msg =await self._move(world,player,agent_id,dispatch,target="家",inner_target="锅")
            if not msg.get('OK'):
                return msg

        msg = await dispatch.action(agent_id=agent_id,cmd="cook",target=item,cur_location=player.cur_location)
        if not msg or not msg.get("OK"):
            return {"action": "cook", "OK": False, "MSG": "前端烹饪动画失败或超时"}

        if not self._decreace_qty(world, player.inventory, item, 1):
            return {"action": "cook", "OK": False, "MSG": "食材不足"}
        if not self._decreace_qty(world, player.inventory, "燃料罐", 1):
            return {"action": "cook", "OK": False, "MSG": "燃料不足"}
        res = self._update_attribute(player,world.item_data[item]['ingredient']['cook_time']/60)# cook_time单位为分钟
        if not res:
            return {
                'action':"cook",
                'input':item,
                'OK':False,
                'MSG':f"玩家死亡，游戏结束"
            }
        cooked_meta = world.item_data.get(cooked_item, {})
        cooked_desc = cooked_meta.get('description', '')
        cooked_func = cooked_meta.get('function') or {}
        if not isinstance(cooked_func, dict):
            cooked_func = {}
        if not self._increase_qty(world, player.inventory, cooked_item, 1, cooked_desc, cooked_func):
            return {"action": "cook", "input": item, "OK": False, "MSG": "背包已满，无法放入成品"}
        memory = {world.get_time().strftime("%Y-%m-%d %H:%M"):f"你烹饪了{item}"}
        player.memory.append(json.dumps(memory,ensure_ascii=False))
        effect = cooked_meta.get("consumable", {}).get("effect", {})
        return {
            'action':"cook",
            'input':item,
            'OK':True,
            'MSG':f"烹饪{item}成功,得到{cooked_item}",
            'qty':1,
            'effect':effect
        }


    
    async def trade(self,player,action,agent_id,dispatch,world,world_lock) -> Dict[str,Any]:
        """交易"""
        if player.cur_location != "集市":
            msg = await self._move(world,player,agent_id,dispatch,target="集市",inner_target="收银台")
            if not msg.get('OK'):
                return msg
        mode = action['mode']
        item = action['item']
        qty = action['qty']
        market: Market | None = world.locations.get("集市") if isinstance(world.locations, dict) else None
        if not isinstance(market, Market):
            return {"action": "trade", "OK": False, "MSG": "市场未初始化"}

        if mode == "buy":
            if item not in world.item_data:
                return {
                    "action": "trade",
                    "mode": mode,
                    "item": item,
                    "OK": False,
                    "MSG": f"{item} 未在商品列表中配置",
                }
            if not self._has_capacity(world, player.inventory, item, qty):
                return {
                    "action": "trade",
                    "mode": mode,
                    "item": item,
                    "OK": False,
                    "MSG": "背包容量不足，无法购买",
                }
            async with world_lock:
                market_item = market.items.get(item)
                if not market_item or market_item.get("quantity", 0) < qty:
                    return {
                        'action':"trade",
                        'mode':mode,
                        'item':item,
                        'OK':False,
                        'MSG':f"集市没有或没有足够的{item}出售"
                    }
                price = float(market_item.get("cur_price", 0))
                cost = price * qty
                if player.money < cost:
                    return {
                        'action':"trade",
                        'mode':mode,
                        'item':item,
                        'OK':False,
                        'MSG':f"金币不足，无法购买{qty}个{item}"
                    }
                func = world.item_data[item].get('function') or {}
                if not isinstance(func, dict):
                    func = {}
                if not self._increase_qty(
                    world,
                    player.inventory,
                    item,
                    qty,
                    world.item_data[item]['description'],
                    func,
                ):
                    return {
                        "action": "trade",
                        "mode": mode,
                        "item": item,
                        "OK": False,
                        "MSG": "背包空间不足，购买失败",
                    }
                player.money -= cost
                market_item["quantity"] -= qty
            memory = {world.get_time().strftime("%Y-%m-%d %H:%M"):f"你购买了{qty}个{item}"}
            player.memory.append(json.dumps(memory,ensure_ascii=False))
            return {
                'action':"trade",
                'mode':mode,
                'item':item,
                'OK':True,
                'MSG':f"购买{qty}个{item}成功,花费{cost}金币",
                'qty':qty,
                'price':cost
            }
        elif mode == "sell":
            msg = self._ensure_item(action,world,player.inventory,item,qty)
            if msg is not None:
                return msg
            async with world_lock:
                market_item = market.items.get(item)
                if market_item is None:
                    return {
                        'action':"trade",
                        'mode':mode,
                        'item':item,
                        'OK':False,
                        'MSG':f"集市不收购{item}"
                    }
                price = float(market_item.get("cur_price", 0)) * 0.5
                if not self._decreace_qty(world,player.inventory,item,qty):
                    return {
                        "action": "trade",
                        "mode": mode,
                        "item": item,
                        "OK": False,
                        "MSG": "物品数量不足，出售失败",
                    }
                player.money += price*qty
                market_item["quantity"] = market_item.get("quantity", 0) + qty
            memory = {world.get_time().strftime("%Y-%m-%d %H:%M"):f"你出售了{qty}个{item}"}
            player.memory.append(json.dumps(memory,ensure_ascii=False))
            return {
                'action':"trade",
                'mode':mode,
                'item':item,
                'OK':True,
                'MSG':f"出售{qty}个{item}成功,获得{price*qty}金币",
                'qty':qty,
                'price':price*qty
            }

        else:
            return {
                'action':"trade",
                'mode':mode,
                'item':item,
                'OK':False,
                'MSG':f"交易模式{mode}不存在"
            }

        

    
    async def fishing(self,action,player,dispatch,agent_id,world,rate:float=0.6) -> Dict[str,Any]:
        """钓鱼
        - 时间消耗
        - 判定是否钓到
        - 得到物品
        - 减少背包容量
        - 加入记忆
        """
        time_cost = 10 #min
        msg = self._ensure_item(action,world,container=player.inventory,item="钓鱼竿",qty=1)
        if msg is not None:
            return msg
        msg = self._ensure_item(action,world,container=player.inventory,item="鱼饵",qty=1)
        if msg is not None:
            return msg
        if not self._has_capacity(world, player.inventory, "鱼", 1):
            return {
                "action": "fishing",
                "OK": False,
                "MSG": "背包空间不足，无法钓鱼获取战利品",
            }
        if player.cur_location != "河流":
            msg = await self._move(world,player,agent_id,dispatch,target="河流",inner_target="钓鱼点")
            if not msg.get('OK'):
                return msg
        # 消耗鱼饵
        if not self._decreace_qty(world,container=player.inventory,item="鱼饵",qty=1):
            return {"action": "fishing", "OK": False, "MSG": "鱼饵不足"}
        msg = await dispatch.action(agent_id=agent_id,cmd="fish",target="鱼",cur_location=player.cur_location)
        if not msg or not msg.get("OK"):
            # 返还鱼饵，避免白扣
            bait_meta = world.item_data.get("鱼饵", {})
            self._increase_qty(
                world,
                player.inventory,
                "鱼饵",
                1,
                bait_meta.get("description", ""),
                bait_meta.get("function") or {},
            )
            return {"action": "fishing", "OK": False, "MSG": "前端钓鱼动画失败或超时"}
        if random.random() < rate:
            # 钓到鱼
            if not self._increase_qty(world,player.inventory,"鱼",1,description=world.item_data['鱼']['description']):
                return {"action": "fishing", "OK": False, "MSG": "背包已满，无法放入鱼"}
            msg = await dispatch.action(agent_id=agent_id,cmd="catch_fish",target="鱼",cur_location=player.cur_location)
            if not msg or not msg.get("OK"):
                return {"action": "fishing", "OK": False, "MSG": "前端捕鱼动画失败或超时"}
            if not self._update_attribute(player,time_cost/60):
                return {
                    'action':"fishing",
                    'OK':False,
                    'MSG':f"玩家钓鱼时体力耗尽，游戏结束"
                }
            memory = {world.get_time().strftime("%Y-%m-%d %H:%M"):f"你钓到了鱼"}
            player.memory.append(json.dumps(memory,ensure_ascii=False))
            return {
                'action':"fishing",
                'OK':True,
                'MSG':f"钓鱼成功,获得鱼",
                'qty':1
            }
        else:
            msg = await dispatch.action(agent_id=agent_id,cmd="catch_nothing",target="鱼",cur_location=player.cur_location)
            if not msg or not msg.get("OK"):
                return {"action": "fishing", "OK": False, "MSG": "前端捕鱼动画失败或超时"} 
            # 钓不到鱼
            self._update_attribute(player,time_cost/60)
            memory = {world.get_time().strftime("%Y-%m-%d %H:%M"):f"你没钓到鱼"}
            player.memory.append(json.dumps(memory,ensure_ascii=False))
            return {
                'action':"fishing",
                'OK':True,
                'MSG':f"你没钓到鱼",
                'qty':0
            }
        
    

    
    async def talk(self) -> Dict[str,Any]:
        """与玩家对话
            暂未完成
        """
        return {}

    
    async def wait(self,dispatch,agent_id,action,player,) -> Dict[str,Any]:
        """等待"""
        msg = await dispatch.action(agent_id=agent_id,cmd="wait",target=action['seconds'],cur_location=player.cur_location)
        if not msg or not msg.get("OK"):
            return {"action": "wait", "OK": False, "MSG": "前端等待动画失败或超时"}
        if not self._update_attribute(player,action['seconds']*TIME_RATIO/3600):
            return {
                'action':"wait",
                'OK':False,
                'MSG':f"玩家等待时体力耗尽，游戏结束"
            }
 
        return {
            'action':"wait",
            'OK':True,
            'MSG':f"等待{action['seconds']}秒成功",
            'cost':action['seconds']*TIME_RATIO
        }
        
        
    
        
        

    
    async def store(self,action,world,player,) -> Dict[str,Any]:
        """
        存储
        减少背包物品
        增加背包容量
        增加对应容器内物品
        减少对应容器容量
        加入记忆
        """
        item = action['item']
        qty = action['qty']
        container = world.players_home[player.id].inner_facilities.get(action['container'])
        if container is None:
            return {
                'action':"store",
                'item':item,
                'OK':False,
                'MSG':f"容器{action['container']}不存在"
            }
        msg = self._ensure_item(action,world,player.inventory,item,qty)
        if msg is not None:
            return msg
        func = world.item_data[item].get('function') or {}
        if not isinstance(func, dict):
            func = {}
        if not self._increase_qty(world,container, item, qty, world.item_data[item]['description'], func):
            return {
                'action':"store",
                'item':item,
                'OK':False,
                'MSG':f"容器{action['container']}容量不足"
            }
        if not self._decreace_qty(world,player.inventory,item,qty):
            return {
                'action':"store",
                'item':item,
                'OK':False,
                'MSG':f"你没有足够的{item}"
            }
        memory = {world.get_time().strftime("%Y-%m-%d %H:%M"):f"你存储了{qty}个{item}到{action['container']}中"}
        player.memory.append(json.dumps(memory,ensure_ascii=False))
        return{
            'action':"store",
            'item':item,
            'OK':True,
            'MSG':f"存储{item}成功",
            'qty':qty
        }
        


        

    
    async def retrieve(self,action,world,player) -> Dict[str,Any]:
        """取出"""
        item = action['item']
        qty = action['qty']
        container = world.players_home[player.id].inner_facilities.get(action['container'])
        if container is None:
            return {
                'action':"retrieve",
                'item':item,
                'OK':False,
                'MSG':f"容器{action['container']}不存在"
            }
        msg = self._ensure_item(action,world,container,item,qty)
        if msg:
            return msg
        func = world.item_data[item].get('function') or {}
        if not isinstance(func, dict):
            func = {}
        if not self._increase_qty(world,player.inventory, item, qty, world.item_data[item]['description'], func):
            return {
                'action':"retrieve",
                'item':item,
                'OK':False,
                'MSG':f"你背包已满"
            }
        if not self._decreace_qty(world,container,item,qty):
            return {
                'action':"retrieve",
                'item':item,
                'OK':False,
                'MSG':f"容器{action['container']}没有足够的{item}"
            }
        memory = {world.get_time().strftime("%Y-%m-%d %H:%M"):f"你从{action['container']}中取出了{qty}个{item}"}
        player.memory.append(json.dumps(memory,ensure_ascii=False))
        return {
            'action':"retrieve",
            'item':item,
            'OK':True,
            'MSG':f"取出{item}成功",
            'qty':qty
        }

    
    def _update_attribute(self,player,time_cost:float,attr_names:List[str] = ["hunger","thirst","fatigue"]) -> bool:
        for name in attr_names:
            attr = player.attribute.get(name)
            if not attr:
                continue
            decay = attr.decay_per_hour * (time_cost)
            attr.current -= decay
            if attr.current < 0:
                return False
        return True
    
    
    def _decreace_qty(self,world,container:Container,item:str,qty:int) -> bool:
        if container.items.get(item) is None or container.items[item].quantity < qty:
            return False
        container.items[item].quantity -= qty
        if container.items[item].quantity <= 0:
            del container.items[item]
        container.capacity += qty * self._unit_capacity(world, item)
        return True
    
    def _increase_qty(self,world,container:Container,item:str,qty:int,description:str="",function:Dict[str,Any]|None = None) -> bool:
        if function is None or not isinstance(function, dict):
            function = {}
        unit_capacity = self._unit_capacity(world, item)
        if container.capacity - qty * unit_capacity < 0:
            return False
        if container.items.get(item) is None:
            # 初始化为 0，再统一叠加，避免重复计数
            container.items[item] = Item(name=item, quantity=0, description=description, function=function)
        container.items[item].quantity += qty
        container.capacity -= qty * unit_capacity
        
        return True
        
    
    def _ensure_item(self,action:Dict,world:World,container:Container,item:str,qty:int) -> Optional[Dict[str,Any]]:        
        if world.item_data.get(item) is None:
            return {
                'action':action['type'],
                'item':item,
                'OK':False,
                'MSG':f"物品{item}不存在或不是可消耗物品"
            }
        if container.items.get(item) is None or container.items[item].quantity < qty:
            return {
                'action':action['type'],
                'item':item,
                'OK':False,
                'MSG':f"你没有足够的{item}"
            }
        return None

    def _unit_capacity(self, world: World, item: str) -> float:
        return float(world.item_data.get(item, {}).get("unit_capacity", 0) or 0)

    def _has_capacity(self, world: World, container: Container, item: str, qty: int) -> bool:
        unit_cap = self._unit_capacity(world, item)
        return container.capacity - unit_cap * qty >= 0
      
    def sleep(self,player,action,world,dispatch,agent_id) :
        """睡觉"""
        pass
