## 背景
你在一个小镇生存经营环境中行动。目标是保持生存属性安全，并逐步提升资金与资源。

## 规则
- 仅依据当前提示中的信息决策，不要虚构物品、地点或规则。
- 优先保证生存属性（饥饿、口渴、疲劳）不进入危险状态。
- 若关键信息不足，先移动到可获取信息的位置再行动。

## 角色状态
日期：2
角色：A
当前位置：market
金钱：10
属性：饥饿 80.0，口渴 70.0，疲劳 60.0
背包：breadx1

## 当前地点信息
你当前在：Market (market)
地点描述：trade place

### 市场商品
- Bread(bread) | 库存 2 | 价格 5.00 | 接近常规价格 | food

## 近期事件
- moved to market

## 当前计划
暂无

## 任务
请基于当前计划与近期事件，输出下一步动作。
输出 1-3 步；若计划完成或不安全，输出 finish 或 wait。

## 动作输出要求
- 只输出 JSON 对象或 JSON 数组，不要输出额外文本。
- 数组最多 3 步；若包含 move，建议只输出 move。
- 允许动作类型：move, consume, cook, sleep, trade, wait, finish。
- 无安全动作时输出 {"type":"finish"}。
- 当前位置：market；家：home；可移动目标：home，market。
- trade 仅在市场地点执行，item 必须来自市场商品列表。
- consume/cook 仅可使用背包已有物品。
## 示例
{"type":"move","target":"market"}
{"type":"consume","item":"bread","qty":1}
{"type":"sleep","minutes":30}
{"type":"trade","mode":"buy","item":"bread","qty":2}
{"type":"wait","seconds":10}
{"type":"finish"}