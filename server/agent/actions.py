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
from typing import Any, Dict, Optional

from .agent_config import ACTION_FATIGUE_COST, TIME_RATIO, WORLD_PLACE_MAP, SLEEP_RECOVER
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
            # elif action_type == "fishing":
            #     return await self.fishing(action, player, dispatch, agent_id, world)
            elif action_type == "move":
                return await self._move(world, player, agent_id, dispatch, target=action["target"])
            # elif action_type == "pick_up":
            #     return await self.pick_up(action, player, dispatch, agent_id, world)
            elif action_type == "sleep":
                return await self.sleep(player,action,world,dispatch,agent_id)
            else:
                return {"action": action_type or "unknown", "OK": False, "MSG": "未知的动作类型"}
        except Exception:
            logger.exception("Action execution failed: %s", action)
            return {"action": action_type or "unknown", "OK": False, "MSG": "服务端处理动作时出现异常"}

    
    async def _move(self,world,player,agent_id,dispatch,target:str,inner_target:str|None=None) -> Dict[str,Any]:
        '''玩家移动
        1) 时间消耗
        2) 改变位置
        3) 无体力消耗
        4) 同步记忆
        '''
        resolved_target = player.home if target == "家" else target
        if not inner_target and target in WORLD_PLACE_MAP:
            inner_target = WORLD_PLACE_MAP[target]
        if player.cur_location == target:
            return {
                'action':"move",
                'target':target,
                'OK':True,
                'MSG':"你已经在该位置了"
            }
        if inner_target is None:
            inner_target = target # 屋内导航点不存在则改为使用默认导航点
        print(target)
        print(resolved_target)
        if resolved_target not in world.locations and target != "家":
            return {
                'action':"move",
                'target':target,
                'OK':False,
                'MSG':f"移动失败,目标{target}不存在"
            }
        if resolved_target in player.accessible and player.accessible[resolved_target] == 1: # 1表示未知，0代表已知
            player.accessible[resolved_target] = 0
        
        begin_time = time.time()     
        msg = await dispatch.action(agent_id=agent_id,cmd="go_to",target=inner_target,cur_location=player.cur_location)
        if not msg or not msg.get("status") !="ok":
            return {"action": "move", "target": target, "OK": False, "MSG": "前端移动失败或超时"}
        time_cost=round((time.time() - begin_time))*TIME_RATIO # 实际时间消耗
        logger.info("移动耗时: %s", time_cost)
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
        """消耗物品:包括食物等
        - 减少背包中物品
        - 触发物品对应效果
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
            msg = await dispatch.action(agent_id=agent_id,type="animation",target="item",value = -1 *qty)
            if not msg or not msg.get("status") != "ok":
                return {"action": "consume", "target": item, "OK": False, "MSG": "前端使用物品动画失败或超时"}
            effect_data:Dict[str,Any] = item_data['consumable']['effect']
            # 触发属性回复
            for attr,value in effect_data.items():
                player.attribute[attr].current = min(player.attribute[attr].current + value,100)
                msg = await dispatch.action(agent_id=agent_id,type="animation",target=attr,value=value)
                if not msg or not msg.get("status")!="ok":
                    return {"action": "consume", "target": item, "OK": False, "MSG": "前端更新属性动画失败或超时"}

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
                'qty':qty
            }
        elif item_data.get('equipment'):
            msg = await dispatch.action(agent_id=agent_id,type = "animation",target=item,value=1)
            if not msg or not msg.get("status")!="ok":
                return {"action": "consume", "OK": False, "MSG": "前端装备物品动画失败或超时"}
            self._decreace_qty(world,player.inventory,item,qty)
            memory = {world.get_time().strftime("%Y-%m-%d %H:%M"):f"你装备了{item}"}
            player.memory.append(json.dumps(memory,ensure_ascii=False))
            return {
                'action':"consume",
                'item':item,
                'OK':True,
                'MSG':f"装备{item}成功,",
                'qty':qty
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

        # msg = await dispatch.action(agent_id=agent_id,cmd="cook",target=item,cur_location=player.cur_location)
        # if not msg or not msg.get("status")!="ok":
        #     return {"action": "cook", "OK": False, "MSG": "前端烹饪动画失败或超时"}

        if not self._decreace_qty(world, player.inventory, item, 1):
            return {"action": "cook", "OK": False, "MSG": "食材不足"}
        if not self._apply_fatigue_cost(player, "cook"):
            return {
                'action':"cook",
                'input':item,
                'OK':False,
                'MSG':f"玩家烹饪时体力耗尽，游戏结束"
            }
        cooked_meta = world.item_data.get(cooked_item, {})
        cooked_desc = cooked_meta.get('description', '')
        cooked_func = cooked_meta.get('function') or {}
        if not isinstance(cooked_func, dict):
            cooked_func = {}
        if not self._increase_qty(world, player.inventory, cooked_item, 1, cooked_desc, cooked_func):
            return {"action": "cook", "input": item, "OK": False, "MSG": "成品放入背包失败"}
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
                        "MSG": "购买失败",
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

        

    
    # async def fishing(self,action,player,dispatch,agent_id,world,rate:float=0.6) -> Dict[str,Any]:
    #     """钓鱼
    #     - 固定体力消耗
    #     - 判定是否钓到
    #     - 得到物品
    #     - 加入记忆
    #     """
    #     # if player.cur_location != "河流":
    #     #     msg = await self._move(world,player,agent_id,dispatch,target="河流",inner_target="钓鱼点")
    #     #     if not msg.get('OK'):
    #     #         return msg
    #     msg = await dispatch.action(agent_id=agent_id,cmd="fish",target="鱼",cur_location=player.cur_location)
    #     if not msg or not msg.get("status")!="ok":
    #         return {"action": "fishing", "OK": False, "MSG": "前端钓鱼动画失败或超时"}
    #     if not self._apply_fatigue_cost(player, "fishing"):
    #         return {
    #             'action':"fishing",
    #             'OK':False,
    #             'MSG':f"玩家钓鱼时体力耗尽，游戏结束"
    #         }
    #     if random.random() < rate:
    #         # 钓到鱼
    #         if not self._increase_qty(world,player.inventory,"鱼",1,description=world.item_data['鱼']['description']):
    #             return {"action": "fishing", "OK": False, "MSG": "放入鱼失败"}
    #         msg = await dispatch.action(agent_id=agent_id,cmd="animation",target="fish",value=1)
    #         if not msg or not msg.get("status")!="ok":
    #             return {"action": "fishing", "OK": False, "MSG": "前端捕鱼动画失败或超时"}
    #         memory = {world.get_time().strftime("%Y-%m-%d %H:%M"):f"你钓到了鱼"}
    #         player.memory.append(json.dumps(memory,ensure_ascii=False))
    #         return {
    #             'action':"fishing",
    #             'OK':True,
    #             'MSG':f"钓鱼成功,获得鱼",
    #             'qty':1
    #         }
    #     else:
    #         # msg = await dispatch.action(agent_id=agent_id,cmd="animation",target="fish",value=1)
    #         # if not msg or not msg.get("status")!="ok":
    #         #     return {"action": "fishing", "OK": False, "MSG": "前端捕鱼动画失败或超时"} 
    #         # 钓不到鱼
    #         memory = {world.get_time().strftime("%Y-%m-%d %H:%M"):f"你没钓到鱼"}
    #         player.memory.append(json.dumps(memory,ensure_ascii=False))
    #         return {
    #             'action':"fishing",
    #             'OK':True,
    #             'MSG':f"你没钓到鱼",
    #             'qty':0
    #         }
        
    

    
    # async def talk(self) -> Dict[str,Any]:
    #     """与玩家对话
    #         暂未完成
    #     """
    #     return {}

    
    async def wait(self,dispatch,agent_id,action,player,) -> Dict[str,Any]:
        """等待"""
        msg = await dispatch.action(agent_id=agent_id,cmd="waiting",target=action['seconds'],cur_location=player.cur_location)
        if not msg or msg.get("status")!="ok":
            return {"action": "wait", "OK": False, "MSG": "前端等待动画失败或超时"}
        if not self._apply_fatigue_cost(player, "wait"):
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
        增加对应容器内物品
        加入记忆
        """
        item = action['item']
        qty = action['qty']
        container = world.players_home[player.id].inner_things.get(action['container'])
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
        self._increase_qty(world,container, item, qty, world.item_data[item]['description'], func)
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
        container = world.players_home[player.id].inner_things.get(action['container'])
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
        self._increase_qty(world,player.inventory, item, qty, world.item_data[item]['description'], func)
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

    async def sleep(self,player,action,world,dispatch,agent_id) :
        """睡觉"""
        # 前端发送睡觉指令
        msg = await dispatch.action(agent_id=agent_id,cmd="sleeping",value=action['minutes']*60/TIME_RATIO)
        if not msg or msg.get("status")!="ok":
            return {"action": "sleep", "OK": False, "MSG": "前端睡觉动画失败或超时"}
        if not self._apply_attribute_delta(player, {"fatigue": SLEEP_RECOVER}):
            return {
                'action':"sleep",
                'OK':False,
                'MSG':f"玩家睡觉时体力耗尽，游戏结束"
        }
        memory = {world.get_time().strftime("%Y-%m-%d %H:%M"):f"你睡了{action['minutes']}分钟"}
        player.memory.append(json.dumps(memory,ensure_ascii=False))
        return {
            'action':"sleep",
            'OK':True,
            'MSG':f"你睡了{action['minutes']}分钟"
        }

    
    # async def pick_up(self,player,action,world,dispatch,agent_id):
    #     """采集
    #     - 向前端发送采集动作
    #     - 背包中增加物品
    #     - 动作消耗任务属性
    #     - 加入记忆
    #     """
    #     msg = await dispatch.action(agent_id=agent_id,cmd="pick_up",value=0.5)
    #     if not msg or msg.get("status")!="ok":
    #         return {"action": "pick_up", "OK": False, "MSG": "前端采集动画失败或超时"}
    #     item_name = action['item']
    #     if item_name not in world.locations['forest'].inner_things or world.location['forest'].inner_things[item_name].quantity <= 0:
    #         return {
    #             'action':"pick_up",
    #             'item':item_name,
    #             'OK':False,
    #             'MSG':f"物品{item}不存在或不是可采集物品"
    #         }
    #     
    #     item = world.item_data[item_name]
    #     if not self._increase_qty(world,player.inventory,item,1,item.get("description"), item.get("function")):
    #         return {
    #             'action':"pick_up",
    #             'item':item_name,
    #             'OK':False,
    #             'MSG':f"背包已满"
    #         }
    #     world.location['forest'].inner_things[item_name] -= 1
    #     memory = {world.get_time().strftime("%Y-%m-%d %H:%M"):f"你采集了{item_name}"}
    #     player.memory.append(json.dumps(memory,ensure_ascii=False))
    #     return {
    #         'action':"pick_up",
    #         'item':item_name,
    #         'OK':True,
    #         'MSG':f"采集{item_name}成功"
    #     }
    

    
        
            

    def _apply_attribute_delta(self,player,delta:Dict[str,float]) -> bool:
        for name,value in (delta or {}).items():
            attr = player.attribute.get(name)
            if not attr:
                continue
            attr.current = min(attr.max_value, attr.current + value)
            if attr.current < 0:
                return False
        return True

    def _apply_fatigue_cost(self,player,action_type:str) -> bool:
        cost = ACTION_FATIGUE_COST.get(action_type, 0)
        if cost <= 0:
            return True
        return self._apply_attribute_delta(player, {"fatigue": -cost})
    
    
    def _decreace_qty(self,world,container:Container,item:str,qty:int) -> bool:
        if container.items.get(item) is None or container.items[item].quantity < qty:
            return False
        container.items[item].quantity -= qty
        if container.items[item].quantity <= 0:
            del container.items[item]
        return True
    
    def _increase_qty(self,world,container:Container,item:str,qty:int,description:str="",function:Dict[str,Any]|None = None) -> bool:
        if function is None or not isinstance(function, dict):
            function = {}
        if container.items.get(item) is None:
            # 初始化为 0，再统一叠加，避免重复计数
            container.items[item] = Item(name=item, quantity=0, description=description, function=function)
        container.items[item].quantity += qty
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
