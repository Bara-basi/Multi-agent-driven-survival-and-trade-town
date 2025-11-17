import json,random
from datetime import datetime
from typing import List,Dict,Any
from .schema import Item,Container,Location,Market
from .agent_config import PLAYER_INFO,TIME_RATIO
# from .player import Player
from dataclasses import dataclass, field
from typing import Dict, List, Any

@dataclass(slots=True)
class World:
    players: List[Any]
    product_list_path: str = "server/agent/product_list.json"
    time: datetime = field(default_factory=datetime.now)
    players_home:Dict[int,Location] = field(init=False)
    locations: Dict[str, Any] = field(init=False)
    item_data: Dict[str, Any] = field(init=False)

    def __post_init__(self) -> None:
        self.locations = {}
        self.players_home = {}
        self.locations["河流"] = self._init_river()
        self.locations["集市"] = self._init_market()
        self.locations["森林"] = self._init_forest()
        self._init_players_home()


    # --- 初始化逻辑 ---
    def _init_river(self) -> Location:
        """初始化河流"""
        return Location(
            name="河流",
            description=(
                "一条长长的河流，如果你的背包中有钓鱼竿和鱼饵，可以在这钓鱼，"
                "60%概率获得一条鱼，每次钓鱼消耗10分钟，操作为{使用:钓鱼竿}，"
                "无需其它参数；如果没有钓鱼竿仍进行操作，你将犯规并被淘汰。"
            ),
        )

    def _init_market(self) -> Market:
        """初始化市场（从 JSON 加载），并进行首轮价格更新"""
        with open(self.product_list_path, "r", encoding="utf-8") as f:
            data = json.load(f)["market"]
        self.item_data = data["items"]
        market = Market(description=data["description"], items=self.item_data)
        self.update_market(market)
        return market
    
    def _init_forest(self):
        """初始化森林"""
        description = "暂未开放丛林区域，请以后再来"
        forest = Location(name = "森林", description = description, inner_facilities = {})
        return forest
        
    def _init_players_home(self):
        """初始化玩家的家"""
        for i in range(len(self.players)):
            name = f"玩家{i+1}的家"
            description = f"这是玩家{i+1}的家,只有他才可以进入。"
            fridge = Container(name="冰箱",capacity=100)
            locker = Container(name="储物柜",capacity=100)
            bed = Item(name="床",quantity=-1,description=r"你可以在此睡觉，回复疲劳值(+20疲劳值/小时),使用的指令为{使用：床,使用时长：60}，单位分钟") 
            pot = Item(name="锅",quantity=-1,description=r"你可以消耗燃料罐来烹饪,花费时间由烹饪的物品决定,使用的指令格式为{使用:锅,使用目标:鱼}")
            inner_home_facilities = {"冰箱":fridge,"床":bed,"锅":pot,"储物柜":locker}
            self.players_home[i+1] = Location(
                name = name,
                description = description,
                inner_facilities = inner_home_facilities
            )
            

    # --- 市场逻辑 ---
    @staticmethod
    def update_market(market: Market) -> None:
        """
        按“日波动率”为幅度，对当前价格围绕均价做均匀扰动；
        更新后价格限制在均价的 ±20% 区间。
        """
        for item in market.items.values():
            avg = float(item["avg_price"])
            vol = float(item["daily_volatility"])
            new_price = avg + vol * (2 * random.random() - 1) 
            new_price = max(avg * 0.8, min(new_price, avg * 1.2))
            item["cur_price"] = round(new_price, 2)



   
    """游戏时间处理"""
    def get_time(self) -> datetime:
        """实际时间1分钟等于游戏时间1小时"""
        cur_time = self.time +(datetime.now() - self.time) * TIME_RATIO
        return cur_time 
    
    "获取世界快照"
    def get_snapshot(self,id:int) -> Dict[str,Any]:
        """获取当前世界快照
        - 各地区状态
        - 市场信息
        """
        accessible = self.players[id-1].accessible
        locations_accessible = {}
        for name,acc in accessible.items():
            if name not in self.locations:
                continue
            if acc == 0:
                locations_accessible[name] = self.locations[name]
            elif acc == 1:
                locations_accessible[name] = "未知区域，访问后开启"
        return locations_accessible
        

# if __name__ == "__main__":
#     import time
#     players = [Player.from_raw(id=id,raw=raw,player_num=len(PLAYER_INFO)) for id,raw in enumerate(PLAYER_INFO.values())]
#     world = World(players=players)
#     # 测试时间
#     print(world.get_time().strftime("%Y-%m-%d %H:%M:%S"))
#     time.sleep(2)
#     print(world.get_time().strftime("%Y-%m-%d %H:%M:%S"))
#     locations = world.get_snapshot(0)
#     for location in locations.items():
#         print(location)