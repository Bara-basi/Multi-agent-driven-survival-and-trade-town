import json,os
from datetime import datetime
from .world import World
from typing import Any
from .models.schema import Container, Item, Location, Market


class PromptModule:
    def __init__(self) -> None:
        self.summary = "今天是第一天，没有总结。\n"
        self.plan = "暂无计划。\n"
        # 如果错误日志不为空，说明上一步动作运行失败，需要把错误日志传入提示词
        self.error_log = ""
        


    def _get_base_prompt(self,player,world) -> str:
        # 永驻提示词模块
        title = """## 背景与基本信息
        """
        game_background = """
        你受邀参加了一个贸易游戏，你被带到一个封闭的小镇，小镇中只有一个集市和几个玩家的住所。游戏目标：在不死亡的前提下尽快赚到 ￥10,000。
        """
        rules = (
            "## 重要规则\n"
            "- 只能使用本提示中的信息、记忆和地点描述，不要猜测或编造任何物品/地点/NPC/规则。\n"
            "- 技能描述不代表已拥有物品；只有“背包物品”“地点设施/商店列表”里出现的物品才可直接使用。\n"
            "- 市场价格/库存是实时信息，仅以本次提示为准。\n"
            "- 信息不足或不确定时，优先选择移动到相关地点\n"
            
        )
        player_identity = f"你的身份是：{player.identity},{player.info} 你的技能是：{player.skill}目前，你身上有 ￥{player.money}。\n"
        location =  f"你现在所在的位置是：{player.cur_location}。\n"
        time = f"当前的时间是：{world.get_format_time()}。\n"
        attr = f"你的生存属性有：饥饿值 {round(player.attribute['hunger'].current,2)}，疲劳值 {round(player.attribute['fatigue'].current,2)}，口渴值 {round(player.attribute['thirst'].current,2)}。属性值越低，你的生存状态越差，请注意补充。\n"
        if len(player.inventory.items) == 0:
            backpack_zipped = "背包物品：你的背包里现在没有物品。\n"
        else:
            backpack_zipped = "你的背包里有："+",".join([f"{item.name}*{item.quantity}" for item in player.inventory.items.values()])+"\n"
        return title+game_background+rules+player_identity+location+time+attr+backpack_zipped


    def _format_locations_info(self, player, world) -> str:
        merged_locations = {}
        if isinstance(world.locations, dict):
            merged_locations.update(world.locations)
        if isinstance(world.players_home, dict):
            for home in world.players_home.values():
                if getattr(home, "name", None):
                    merged_locations[home.name] = home
        lines = []
        for location, accessible in player.accessible.items():
            display_name = location
            if location == player.home:
                display_name = f"家({location})"
            if accessible == -1:
                lines.append(f"{display_name}:不可访问\n")
                continue
            if accessible == 0 and location in merged_locations:
                lines.append(f"{display_name}:{merged_locations[location].description}\n")
            elif accessible == 1:
                lines.append(f"{display_name}:未知区域，访问后得知详情\n")
        return "".join(lines)


    def _build_action_guide(self, player, world) -> str:
        move_targets = []
        for location, accessible in player.accessible.items():
            if accessible != -1:
                if location == player.home:
                    move_targets.append("家")
                else:
                    move_targets.append(location)
        move_targets = sorted(set(move_targets))
        move_targets_text = "、".join(move_targets) if move_targets else "暂无可移动地点"
        containers = []
        if player.cur_location in ("家", player.home):
            home = world.players_home.get(player.id)
            if home:
                for name, facility in home.inner_things.items():
                    if isinstance(facility, Container):
                        containers.append(name)
        containers_text = "、".join(containers) if containers else ""

        available_actions = ["move", "consume", "cook", "sleep", "trade", "wait", "finish"]
        if containers:
            available_actions.extend(["store", "retrieve"])
        guide = (
            "## 动作输出要求\n"
            "- 只输出 JSON 对象或 JSON 数组，不要任何额外文本，不要代码块。\n"
            "- 若输出数组，最多 3 步；若包含 move，则只能输出这一条动作。\n"
            f"- 只能使用以下动作类型：{', '.join(available_actions)}。\n"
            "- 字段名与字符串值必须使用双引号，数值为数字类型。\n"
            "- 若没有安全动作或计划已完成，输出 {\"type\":\"finish\"} 或等待。\n"
            f"- 可移动地点(target)：{move_targets_text}。\n"
            "- 交易只能在集市执行，且物品必须来自集市商品列表。\n"
            "- 消耗/烹饪只能使用背包里已有物品；烹饪通常需要燃料罐。\n"
            f"- 可用容器(container)：{containers_text}。\n"
            "### 动作格式示例\n"
            "{\"type\":\"move\",\"target\":\"集市\"}\n"
            "{\"type\":\"consume\",\"item\":\"面包\",\"qty\":1}\n"
            "{\"type\":\"cook\",\"input\":\"鱼\",\"tool\":\"锅\"}\n"
            "{\"type\":\"sleep\",\"minutes\":30}\n"
            "{\"type\":\"trade\",\"mode\":\"buy\",\"item\":\"面包\",\"qty\":2}\n"
            "{\"type\":\"trade\",\"mode\":\"sell\",\"item\":\"面包\",\"qty\":1}\n"
        )
        if containers:
            guide += (
                "{\"type\":\"store\",\"item\":\"面包\",\"qty\":1,\"container\":\""
                + containers[0]
                + "\"}\n"
                "{\"type\":\"retrieve\",\"item\":\"面包\",\"qty\":1,\"container\":\""
                + containers[0]
                + "\"}\n"
            )
        guide += (
            "{\"type\":\"wait\",\"seconds\":10}\n"
            "{\"type\":\"finish\"}\n"
        )
        return guide


    def get_top_level_plan(self,player,world) -> str:
        # 计划制定模块
        base_prompt = self._get_base_prompt(player,world)
        last_summary = "## 上一次的总结\n"+self.summary
        locations_info_zipped = "## 地点信息\n" + self._format_locations_info(player, world)
        home = world.players_home.get(player.id)
        if home:
            formatted_facilities = self.format_facilities(player,world)
            if player.cur_location in ("家", player.home):
                locations_info_zipped += f"你当前家中设施:\n{formatted_facilities}"
            else:
                locations_info_zipped += f"你家设施(已知):\n{formatted_facilities}"
        
        
  
        title = "## 计划制定(你现在的任务)\n"
        top_level_plan_module = (
            "请根据已知信息制定接下来几个小时内的计划，确保计划可执行。只说短期内能实现的事，不用给出类似“我要赚够￥10000”此类宏大的叙\n"
            "- 先保证生存（饥饿/口渴/疲劳），再考虑赚钱。\n"
            "- 不要假设未知信息；若关键信息缺失，计划中写“先去××查看”。\n"
            "- 输出 3-6 条计划，不用刻意分点，第一条以“我接下来应该……”开头。\n"
        )
        prompt = base_prompt + last_summary + locations_info_zipped + title + top_level_plan_module
        self.write_prompt_log("plan",prompt,player) 
        return prompt
        
        


    def get_local_action(self,player,world) -> str:
        # 局域动作模块
        base_prompt = self._get_base_prompt(player,world)
        title = "## 动作规划(你现在的任务)\n"
        local_action_prompt = (
            "请根据计划和记忆判断哪些事情已经完成，接下来要做什么。\n"
            "输出最近 1-3 步动作；如计划已完成或无安全动作，输出 {\"type\":\"finish\"} 或等待。\n"
        )
        memory_title = "## 你的记忆\n"
        if len(player.memory) == 0:
            memory = "你刚来到这里，对周围还不熟悉。\n"
        else:
            memory = "近10条动作记录:\n"+"\n".join(player.memory[-10:])+"\n"
        plan_title = "## 当前计划\n"

        location = player.cur_location
        if location == "集市":
            item_list = self.format_market_item_list(world)
            location_info = f"你当前所在的位置是 {location}，地点信息：{world.locations[location].description}\n" + item_list
        elif location == "家" or location == f"玩家{player.id}的家":
            formated_facilities = self.format_facilities(player,world)
            location_info = f"你当前所在的位置是 {location}，地点信息：{world.players_home[player.id].description}\n" + formated_facilities
            
        else:
            location_info = f"你当前所在的位置是 {location}，地点信息：{world.locations[location].description}\n"




        action_guide = self._build_action_guide(player, world)
        prompt = base_prompt + location_info + memory_title + memory + plan_title + self.plan + title + local_action_prompt + action_guide + self.error_log
        self.write_prompt_log("act",prompt,player)
        return prompt    
            


    def get_reflection_and_summary(self,player,world) -> str:
        # 反思总结模块
        reflection_and_summary_module = (
            "你之前的计划是否已经实现？请仅根据记忆总结你至今为止做的事情，"
            "并反思这段时间的收获和问题（如“通过差价成功盈利”“没有注意饱食度差点饿死”等）。"
            "不要新增未发生的事件，回答尽量精简。\n"
        )
        base_prompt = self._get_base_prompt(player,world)
        title = "## 总结反思(你现在的任务)\n"
        plan_title = "## 最近的计划\n"
        memory_title = "## 你的记忆\n"
        if len(player.memory) == 0:
            memory = "你刚来到这里，对周围还不熟悉。\n"
        else:
            memory = "近20条动作记录:\n"+"\n".join(player.memory[-20:])
        prompt = base_prompt + memory_title + memory + plan_title + self.plan + title + reflection_and_summary_module
        self.write_prompt_log("summary",prompt,player)
        return prompt


    def chat_with_npc(self,player,anther_player) -> str:
        # 聊天模块
        title = "## 聊天记录\n"
        return ""
        
        
    def format_market_item_list(self,world) -> str:
        # 根据分类提取更加适合模型阅读的商品列表
        title = "### 商品列表\n"
        items = world.locations["集市"].items
        formated_items = ""

        for name,item in items.items():
            quantity = item['quantity']
            if quantity == 0:
                continue
            price = item['cur_price']
            description = item['description']
            ratio = price / item['avg_price']
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
            
            formated_items += f"{name},描述:{description}，商店存货:{quantity},价格:{price}，{price_info}\n"
        prompt = title + formated_items
        return prompt
    
    def format_facilities(self,player,world) -> str:
        # 格式化设施列表
        title = "### 室内设施列表\n"
        facilities = world.players_home[player.id].inner_things
        formated_facilities = ""
        for name,facility in facilities.items():
            description = facility.description
            formated_facilities += f"{name},描述:{description}\n"
            if isinstance(facility,Container):
                if len(facility.items) == 0:
                    formated_facilities += f"{name}内物品：{name}内现在没有物品。\n"
                else:
                    formated_facilities += f"{name}内物品:{','.join([f'{item.name}*{item.quantity}' for item in facility.items.values()])}\n"
        prompt = title + formated_facilities
        return prompt
    def write_prompt_log(self,prompt_type:str,prompt:str,player):
        if os.path.exists("debug_log/prompt") == False:
            os.makedirs("debug_log/prompt")
        with open(f"debug_log/prompt/{prompt_type}_{player.id}_{datetime.now().strftime('%Y%m%d%H%M%S')}.txt","w",encoding="utf-8") as f:
            f.write(json.dumps(prompt,ensure_ascii=False,indent=4))
