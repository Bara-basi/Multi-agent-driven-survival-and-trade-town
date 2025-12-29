from agent.agent_config import PLAYER_INFO
from agent.player import Player
from agent.world import World
from agent.new_prompt import PromptModule


# 测试除了chat_with_npc方法外的其他方法
player:Player = Player.from_raw(id=1,raw=PLAYER_INFO['player3'],player_num=len(PLAYER_INFO)) 
world = World(players=[player])
prompt_module = PromptModule(player,world)
plan = prompt_module.get_top_level_plan()
# prompt_module.plan = plan
print(plan)
action = prompt_module.get_local_action()
print(action)
summary = prompt_module.get_reflection_and_summary()
# prompt_module.summary = summary
print(summary)

