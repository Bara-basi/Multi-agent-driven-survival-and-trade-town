**项目概览**
- 目的：在一个小镇模拟中，通过 LLM 决策驱动多名玩家（Agent）在世界中行动（移动、交易、烹饪、钓鱼、等待等），并通过 WebSocket 与前端进行动作动画/结果的同步。
- 主要流程：
  - `main.py` 启动 WebSocket 服务端（`server.AgentServer`），创建世界（`agent.world.World`）和玩家（`agent.player.Player`）。
  - 等待前端各 Agent 连接后，为每个玩家构建运行上下文 `AgentRuntimeCtx`，启动 `AgentManager` 的 agent 循环。
  - 每个循环：观察（格式化 Prompt）→ 让 LLM 规划动作 → 执行动作（经由 `WsDispatcher` 通过 WS 通知前端执行）→ 更新状态与记忆。

**核心模块**
- `main.py`: 项目入口
  - 创建 `AgentServer` 并 `await start()`。
  - 依据 `agent/agent_config.py:PLAYER_INFO` 生成玩家列表，构建 `World`，等待所有 `agent-{id}` 连接。
  - 通过 `WsDispatcher` 搭桥动作分发，将上下文打包为 `AgentRuntimeCtx` 并交由 `AgentManager` 管理循环。

- `server.py`: WebSocket 服务
  - `AgentServer.start()` 使用 `websockets.serve` 监听；`_handle` 处理每条连接（hello/complete/pong 等）。
  - `send_action(...)` 发送前端动作用于动画/执行，并等待 `complete` 回包（带 `action_id` 对应的 Future 完成）。
  - `is_connected/connected_ids` 提供连接状态查询；`_ping_loop` 周期 ping 保活。

- `agent/runtime.py`: Agent 运行时
  - `AgentRuntimeCtx` 汇集玩家、世界、分发器及回调（observe/plan/act）。
  - `agent_loop`：按 tick 观察→规划→执行，失败时做简单退避；跨天时刷新市场。
  - `WsDispatcher.action(...)` 调用 `AgentServer.send_action(...)` 与前端交互，返回标准化 `{OK:bool, MSG:str}`。
  - `AgentManager` 负责启动/停止所有 agent 任务。

- `agent/actions.py`: 动作实现
  - 定义 Pydantic 动作 Schema（`Move/Consume/Sleep/Cook/Fishing/Trade/Store/Retrieve/Talk/Wait`）。
  - `ActionMethod` 提供各原子动作的服务端状态变更逻辑，必要时通过 `dispatch.action(...)` 触发前端动画并等待完成；同时更新玩家属性、背包、记忆与世界（如市场库存）。

- `agent/world.py`: 世界建模
  - 初始化河流/集市/森林/各玩家的家（内置“冰箱/床/锅/储物柜”等设施），加载 `product_list.json` 构建市场数据，按日波动率刷新价格。
  - 提供 `get_time()`（现实→游戏时间加速，`TIME_RATIO`），`get_snapshot()`（按玩家可见性返回地点快照）。

- `agent/player.py`: 玩家建模
  - 使用 `dataclass` 描述玩家属性（资金、位置、背包、可见地点 accessible、记忆、三维生存属性等）。
  - `from_raw(...)` 依据初始配置构建玩家，设置可访问/可见地点与初始属性。

- 其余模块
  - `agent/prompts.py`：将世界、玩家、历史动作与规则装配为结构化字典（用于 LLM 提示），并落盘到 `debug_log/`。
  - `agent/schema.py`：Pydantic 定义 `Attribute/Location/Container/Item/Market`。
  - `agent/utils.py`：模型转化辅助函数（`to_attr/to_item/to_location` 等）。
  - `agent/agent.py`：封装 LLM（LangChain ChatOpenAI）与动作解析器（`ActionList`），将模型输出解析为标准动作（必要时做格式修复/兜底）。

**数据与交互流**
- 观察：`prompts.format_prompt(player, history, world)` → 字典序列化为 JSON 字符串喂给 LLM。
- 规划：`agent.Agent.act(prompt_str)` → 返回单个或数组动作字典。
- 执行：`ActionMethod.method_action(ctx, action)` →（必要时）`dispatch.action` → `server.send_action` → 前端动画/执行 → WS 回包 `complete`。
- 状态更新：服务端根据行动更新玩家属性、背包/容器、市场库存、时间花费（折算为属性衰减），并记录 `memory`。

**已识别问题与修复项**
- WebSocket 连接清理会误停服务
  - 问题：`server._handle` 在单连接断开时 `await self.stop()`，将整个 WS 服务与所有连接一并关闭。
  - 修复：仅移除该连接与日志记录，保留服务器运行（除非显式调用 `stop()`）。

- WS 分发器参数错位与成功判断保守
  - 问题：`WsDispatcher.action` 以位置参数调用 `send_action`，导致 `timeout` 误传到 `time_cost`；且仅以 `msg.status == 'ok'` 视为成功。
  - 修复：改为关键字参数传递并兼容 `status/OK` 两种字段来判定结果。

- `Player` 可见性与默认工厂
  - 问题：`accessible` 的 `default_factory` 使用了 `typing.Dict`（不可调用）；初始化玩家家可见性存在 0/1 与索引偏移问题。
  - 修复：改为 `default_factory=dict`；构建时将“自己的家”标为已知（0），他人之家为未知（1），并修正索引为 1..N。

- `agent/agent.py` 重复导入冲突
  - 问题：`SystemMessage/HumanMessage` 来自两处模块并重复导入，可能产生冲突。
  - 修复：保留 `langchain_core.messages` 的导入，移除重复导入。

