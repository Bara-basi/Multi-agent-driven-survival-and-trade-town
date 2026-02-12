class AgentRuntimeConfig:
    
    
    # 触发强规则属性阈值
    hunger_low: float = 15.0
    thirst_low: float = 15.0
    fatigue_low: float = 20.0

    # 最大动作重试次数
    max_action_retries:int = 2

    # 记忆最大长度 
    max_working_events: int = 12
    max_recalled_events: int = 5

    # 最大规划和反思触发步长
    reflect_min_interval_steps = 20
    plan_min_interval_steps = 20