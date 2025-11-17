# 玩家初始状态
DECAY_PER_HOUR = {"hunger": 2, "thirst": 4, "fatigue": 3}
INVENTORY_SIZE = 100
MAX_HEALTH = {"hunger": 100, "thirst": 100, "fatigue": 100}
# 游戏内时间

COOK_MAP={
    "鱼":"烤鱼"
    
}
AGENTS = {}
PENDING = {}

PLAYER_INFO = {
    "player1":{
        "identity":"Barabasi(male,21)",
        "info":"你是一个画家。",
        "skill":"制书：消耗8张纸以制作1副画作,耗时5分钟,使用技能的指令为{'使用','技能'}，仅当你的背包中有足够的纸时才能使用技能，如果集市中纸的quantity为0,你无法从集市购买纸，否则，随意输出技能将受到饥饿值惩罚"
    },
    "player2":{
        "identity":"Davis(male,57)",
        "info":"你是一个黑心商贩,你喜欢搅局，阻碍其它玩家成功，当然，获胜仍然是你的第一目标。",
        "skill":"口才(被动技能)：购买任何商品的价格降低5%。"
    },
    "player3":{
        "identity":"Rosenberg(male,32)",
        "info":"你是一个富豪，你深谙赚钱的门道,你有能力和自信在短时间内赚取大量金钱。",
        "skill":"利滚利(被动技能)：每天0点，你可以获得当前资金20%的收益。"
    },
    "player4":{
        "identity":"Klein(male,28)",
        "info":"你是一个钓鱼佬，你的钓鱼技术超群。",
        "skill":"钓鱼：当前位置为湖边时可使用，且有20%获得双倍鱼,5%概率获得5倍鱼,0.6%概率获得传说鱼,获得传说鱼直接赢得比赛。"
    }
}
# 现实时间与游戏时间转化比例
TIME_RATIO = 120