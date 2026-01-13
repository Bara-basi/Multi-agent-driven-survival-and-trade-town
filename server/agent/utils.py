"""
字符串解析与通用工具
"""
from .models.schema import Attribute, Item, Location
from typing import Dict
from typing import Dict,Any


# def format_item(item:List[Item]) -> str:
#     """
#     格式化物品列表为字符串
#     """
#     if len(item)==1 and item[0].quality==-1:
#         return f"  - **{item[0].name}**: {item[0].function}\n"
#     result = ""
#     for i in item:
#         result +=f"\n- **{i.name}**,数量:{i.quality},空间占用:{i.occupied if i.occupied!=-1 else '-'},功能:{i.function}"
#     result += "\n"
#     return result

# def format_commodity(commodity:List[Commodity]) -> str:
#     """
#     格式化商品列表为字符串
#     """
#     result = ""
#     for i in commodity:
#         result +=f"\n- **{i.name}**,剩余数量:{i.quality},空间占用:{i.occupied if i.occupied!=-1 else '-'},功能:{i.function},均价:{i.base_price},当前价:{i.cur_price},日涨跌幅:{i.daily_fluctuation},描述:{i.description}"
#     result += "\n"
#     return result

# def format_container(container:List[Container]) -> str:
#     """
#     格式化容器列表为字符串
#     """
#     result = ""
#     for c in container:
#         if c.capacity!=-1:
#             result += f"  - **{c.name}**: \n    - 剩余容量{c.capacity}\n"
#             result += f"    - 当前物品:"
#         result += "\n\n"+format_item(list(c.items.values())) if len(c.items)>0  else "空\n"
#     return result


# def format_location(locations:Dict[str,Location]|List[str],distance:Dict[str,float]) -> str:
#     """
#     格式化地点字符串
#     """
#     # 地点包含名称、距离、描述、内部设施
#     result = ""
#     if isinstance(locations,dict):
#         for name,l in locations.items():
#                 result += f"**{name}**:距离{distance.get(name,-1)}分钟,{l.description}\n"
#                 if isinstance(l,Market):
#                     result += format_commodity(list(l.commodity.values()))
#                 else:
#                     for i in l.inner_things.values():
#                         if isinstance(i,Item):
#                             result += format_item([i])
#                         elif isinstance(i,Container):
#                             result += format_container([i])
                    
#                 result += "\n\n---\n\n"
#         return result
#     if isinstance(locations,list):
#         for l in locations:
#             result += f"**{l}**:距离{distance.get(l,0)}分钟,未知区域，探索后解锁\n"
#         return result   

# def update_attr(player:Player,distance:float) -> bool:
#     """
#     更新玩家状态
#     """
#     player.fatigue.current -= distance/60*player.fatigue.decay_per_hour
#     player.hunger.current -= distance/60*player.hunger.decay_per_hour
#     player.thirst.current -= distance/60*player.thirst.decay_per_hour
#     if player.fatigue.current<0 or player.hunger.current<0 or player.thirst.current<0:
#         return False
#     return True






def to_item(name:str,raw:Dict[str,Any]) -> Item:
    return Item(
        name=name,
        quantity=int(raw.get("quantity",0)),
        function=raw.get("function",""),
    
    )


def to_location(name:str,raw:Dict[str,Any]) -> Location:
    return Location(
        name=name,
        description=raw.get("description",""),
        inner_things=raw.get("inner_things",{})
    )

def to_attr(name:str,current:float=100,decay_per_hour:float=0) -> Attribute:
    return Attribute(
        name=name,
        current=current,
        decay_per_hour=decay_per_hour,
    )


