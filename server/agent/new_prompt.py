from datetime import datetime
from .player import Player
from .world import World
from .schema import Item,Container,Location,Market


class PromptModule:
    def __init__(self,player:Player,world:World) -> None:
        self.player = player
        self.world = world
        self.summary = "今天是第一天，没有总结\n"
        self.plan = "暂无计划，请任意发挥\n"
        self.action_map = "以下是允许的指令：\n吃/喝/读/大小背包/使用任何消耗品:{type:consume,item:风干肉,qty:1}\n烹饪:{type:cook,input:鱼,tool:锅}\n睡觉:{type:sleep, minutes:30}\n购买:{type:trade,mode:buy,item:面包,qty:2}\n出售:{type:trade,mode:sell,item:书,qty:1}\n交谈:{type:talk,target:玩家1,content:你好}\n原地等待,时间按照现实时间消耗:{type:wait,seconds:10}\n存储物品到容器:{type:store,item:鱼,qty:1,container:储物柜}\n取出物品:{type:retrieve,item:鱼,qty:1,container:冰箱}\n钓鱼:{type:fishing}\n结束计划：{type:finish}"
        # 如果错误日志不为空，说明上一步动作运行失败，需要把错误日志传入提示词
        self.error_log = ""
        


    def _get_base_prompt(self) -> str:
        # 永驻提示词模块
        title = "## 背景与基本信息\n"
        game_background = "你受邀参加了一个贸易游戏，你被带到一个封闭的小镇，小镇中只有一个集市、一片森林、一条小河和几个玩家的住所。游戏目标：在不死亡的前提下尽快赚到 ¥10,000。\n"
        player_identity = f"你的身份是：{self.player.identity},{self.player.info} 你的技能是：{self.player.skill}目前,你身上有 ¥{self.player.money}。"
        location =  f"你现在所在的位置是：{self.player.cur_location}。\n"
        time = f"当前的时间是：{self.world.get_format_time()}。\n"
        attr = f"你的生存属性有：饥饿值 {round(self.player.attribute['hunger'].current,2)}，疲劳值 {round(self.player.attribute['fatigue'].current,2)}，水分值 {round(self.player.attribute['thirst'].current,2)}。属性值越低，你的生存状态越差，请注意补充。\n"
        if len(self.player.inventory.items) == 0:
            backpack_zipped = "背包物品：你的背包里现在没有物品。\n"
        else:
            backpack_zipped = "你的背包里有："+",".join([f"{item.name}*{item.quantity}" for item in self.player.inventory.items.values()])+"\n"
        return title+game_background+player_identity+location+time+attr+backpack_zipped


    def get_top_level_plan(self) -> str:
        # 计划制定模块
        base_prompt = self._get_base_prompt()
        last_summary = "## 上一次的总结\n"+self.summary
        locations_info_zipped = "## 地点信息\n"
        for location,accessible in self.player.accessible.items():
            if accessible == 0:
                locations_info_zipped += f"{location}:未知地区，访问后得知详情\n"
            elif accessible == 1  and location in self.world.locations:
                locations_info_zipped += f"{location}:{self.world.locations[location].description}\n"
        for player_id,location in self.world.players_home.items():
            if player_id == self.player.id:
                formatted_facilities = self.format_facilities()
                locations_info_zipped += f"你的家:{location.description}\n"+formatted_facilities
            else:
                locations_info_zipped += f"玩家{player_id}的家:{location.description}\n"
            
        
  
        title = "## 计划制定(你现在的任务)\n"
        top_level_plan_module = "现在，请你根据周围的环境、自身的情况和对前段时间的总结，制定接下来的计划,并以“我接下来应该……”作为开头。\n" 
        return base_prompt + last_summary + locations_info_zipped + title + top_level_plan_module 
        
        
        


    def get_local_action(self) -> str:
        # 局域动作模块
        base_prompt = self._get_base_prompt()
        title = "## 动作规划(你现在的任务)\n"
        local_action_prompt ="请你根据计划和记忆判断现在应该做什么？输出成一个json格式的指令，注意，你只允许输出纯json内容,如果你认为计划的内容都已经完成了，输出{type:finish}即可。\n"
        memory_title = "## 你的记忆\n"
        if len(self.player.memory) == 0:
            memory = "你刚来到这里，对周围还不熟悉。\n"
        else:
            memory = "近10条动作记录"+"\n".join(self.player.memory[-10:])+"\n"
        plan_title = "## 当前计划\n"

        location = self.player.cur_location
        if location == "集市":
            item_list = self.format_market_item_list()
            location_info = f"你当前所在的位置是 {location}，地点信息：{self.world.locations[location].description}\n" + item_list
        elif location == "家" or location == f"玩家{self.player.id}的家":
            formated_facilities = self.format_facilities()
            location_info = f"你当前所在的位置是 {location}，地点信息：{self.world.players_home[self.player.id].description}\n" + formated_facilities
            
        else:
            location_info = f"你当前所在的位置是 {location}，地点信息：{self.world.locations[location].description}\n"




        return base_prompt + location_info + memory_title + memory + plan_title + self.plan + title + local_action_prompt + self.action_map + self.error_log
            
            


    def get_reflection_and_summary(self) -> str:
        # 反思总结模块
        reflection_and_summary_module = "你之前的计划是否已经实现了？请你根据记忆总结你至今为止做的事情,并反思你在这段时间里的收获和问题,比如“通过差价成功盈利”、“没有注意饱食度差点饿死”等,回答最好精简一些\n"
        base_prompt = self._get_base_prompt()
        title = "## 总结反思(你现在的任务)\n"
        plan_title = "## 最近的计划\n"
        memory_title = "## 你的记忆\n"
        if len(self.player.memory) == 0:
            memory = "你刚来到这里，对周围还不熟悉。\n"
        else:
            memory = "近20条动作记录"+"\n".join(self.player.memory[-20:])
        return base_prompt + memory_title + memory + plan_title + self.plan + title + reflection_and_summary_module


    def chat_with_npc(self,anther_player:Player) -> str:
        # 聊天模块
        title = "## 聊天记录\n"
        return ""
        
        
    def format_market_item_list(self):
        # 根据分类提取更加适合模型阅读的商品列表
        title = "### 商品列表\n"
        items = self.world.locations["集市"].market.items
        formated_items = ""

        for item in items:
            if item.quantity == 0:
                continue
            name = item.name
            price = item.cur_price
            quantity = item.quantity
            description = item.description
            cost_capacity = item.cost_capacity
            
            ratio = price / item.avg_price
            # 远高于市场价
            if ratio >= 1.30:
                price_info =  "远高于市场价，不推荐购买，如有库存可以考虑趁高卖出。"
            # 略高于市场价
            elif ratio >= 1.15:
                price_info = "略高于市场价，性价比偏低，如非刚需建议观望。"
            # 接近市场价（正常区间）
            elif ratio >= 0.95:
                price_info = "接近市场价，属于正常水平，可按实际需求少量买入。"
            # 略低于市场价（小折扣，更适合即时消费）
            elif ratio >= 0.85:
                price_info = "略低于市场价，算是小幅优惠，适合当前需求型购买。"
            # 明显低于市场价（有套利空间）
            elif ratio >= 0.70:
                price_info = "较市场价偏低，若看好后续价格回升，可以适当囤货。"
            # 远低于市场价（极佳抄底区）
            else:
                price_info = "远低于市场价，是难得的低价机会，在资金允许的前提下可以重点买入。"
            
            formated_items += f"{name},描述:{description}，占用背包容量:{cost_capacity}，商店存货:{quantity},价格:{price}，{price_info}\n"
        return title + formated_items
    
    def format_facilities(self):
        # 格式化设施列表
        title = "### 室内设施列表\n"
        facilities = self.world.players_home[self.player.id].inner_facilities
        formated_facilities = ""
        for name,facility in facilities.items():
            description = facility.description
            formated_facilities += f"{name},描述:{description}\n"
            if isinstance(facility,Container):
                if len(facility.items) == 0:
                    formated_facilities += f"{name}内物品：{name}内现在没有物品。\n"
                else:
                    formated_facilities += f"{name}内物品:{','.join([f'{item.name}*{item.quantity}' for item in facility.items.values()])}\n"
        return title + formated_facilities


    