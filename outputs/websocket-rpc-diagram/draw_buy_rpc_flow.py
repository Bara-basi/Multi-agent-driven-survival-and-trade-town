from __future__ import annotations

import math
from pathlib import Path
from textwrap import wrap

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parent
OUT = ROOT / "buy_action_websocket_rpc_flow.png"
W, H = 1920, 1080

FONT_REG = "C:/Windows/Fonts/msyh.ttc"
FONT_BOLD = "C:/Windows/Fonts/msyhbd.ttc"
FONT_MONO = "C:/Windows/Fonts/consola.ttf"


def font(size: int, bold: bool = False, mono: bool = False) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(FONT_MONO if mono else (FONT_BOLD if bold else FONT_REG), size)


F = {
    "title": font(46, True),
    "sub": font(24),
    "h": font(25, True),
    "h2": font(21, True),
    "body": font(18),
    "small": font(15),
    "code": font(16),
    "chip": font(16, True),
    "label": font(18, True),
}

C = {
    "ink": "#392819",
    "muted": "#715D49",
    "brown": "#70452A",
    "red": "#A34032",
    "green": "#355F4C",
    "gold": "#C6963D",
    "line": "#D3B579",
    "paper": "#FFF8EC",
    "pale": "#F2DFBD",
    "white": "#FFFFFF",
    "dark": "#4F3222",
}


def rounded(draw: ImageDraw.ImageDraw, box, fill, outline=C["line"], width=2, radius=14):
    draw.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)


def text(draw: ImageDraw.ImageDraw, xy, s: str, f, fill=C["ink"], max_width: int | None = None, line_gap: int = 5) -> int:
    x, y = xy
    lines: list[str] = []
    for raw in str(s).splitlines():
        if max_width is None:
            lines.append(raw)
            continue
        # Mixed CJK/ASCII wrapping by measured width.
        cur = ""
        for ch in raw:
            trial = cur + ch
            if draw.textlength(trial, font=f) <= max_width or not cur:
                cur = trial
            else:
                lines.append(cur)
                cur = ch
        if cur:
            lines.append(cur)
    for line in lines:
        draw.text((x, y), line, font=f, fill=fill)
        bbox = draw.textbbox((x, y), line, font=f)
        y += (bbox[3] - bbox[1]) + line_gap
    return y


def centered(draw, box, s, f, fill=C["ink"], line_gap=6):
    x1, y1, x2, y2 = box
    raw_lines = str(s).splitlines()
    line_heights = []
    for line in raw_lines:
        b = draw.textbbox((0, 0), line, font=f)
        line_heights.append(b[3] - b[1])
    total_h = sum(line_heights) + line_gap * (len(raw_lines) - 1)
    y = y1 + (y2 - y1 - total_h) / 2
    for line, lh in zip(raw_lines, line_heights):
        tw = draw.textlength(line, font=f)
        draw.text((x1 + (x2 - x1 - tw) / 2, y), line, font=f, fill=fill)
        y += lh + line_gap


def arrow(draw, start, end, color=C["brown"], width=4, dash=False):
    x1, y1 = start
    x2, y2 = end
    if dash:
        length = math.hypot(x2 - x1, y2 - y1)
        if length == 0:
            return
        dx, dy = (x2 - x1) / length, (y2 - y1) / length
        d = 0
        while d < length - 18:
            seg1 = d
            seg2 = min(d + 12, length - 18)
            draw.line((x1 + dx * seg1, y1 + dy * seg1, x1 + dx * seg2, y1 + dy * seg2), fill=color, width=width)
            d += 22
    else:
        draw.line((x1, y1, x2, y2), fill=color, width=width)
    angle = math.atan2(y2 - y1, x2 - x1)
    size = 14
    p1 = (x2, y2)
    p2 = (x2 - size * math.cos(angle - 0.45), y2 - size * math.sin(angle - 0.45))
    p3 = (x2 - size * math.cos(angle + 0.45), y2 - size * math.sin(angle + 0.45))
    draw.polygon([p1, p2, p3], fill=color)


def box(draw, xywh, title, lines, accent=C["red"], fill="#FFFFFFE8"):
    x, y, w, h = xywh
    rounded(draw, (x, y, x + w, y + h), fill)
    draw.rounded_rectangle((x, y, x + 8, y + h), radius=8, fill=accent)
    text(draw, (x + 24, y + 20), title, F["h"], accent)
    yy = y + 62
    for line in lines:
        font_obj = F["code"] if line.startswith("`") else F["body"]
        clean = line.strip("`")
        yy = text(draw, (x + 24, yy), clean, font_obj, C["ink"], max_width=w - 48) + 7


img = Image.new("RGB", (W, H), C["paper"])
draw = ImageDraw.Draw(img)

# background and frame
for y in range(H):
    r = int(255 - y / H * 13)
    g = int(248 - y / H * 25)
    b = int(236 - y / H * 47)
    draw.line((0, y, W, y), fill=(r, g, b))
draw.rectangle((34, 34, W - 34, H - 34), outline="#8C4D76", width=8)
draw.rectangle((52, 52, W - 52, H - 52), outline="#B981A7", width=2)

text(draw, (82, 80), "buy 动作的 WebSocket 伪 RPC 调用链", F["title"], C["ink"])
text(
    draw,
    (84, 132),
    "通过多层封装，Python 后端把 Unity 场景调用写成近似函数调用：client.buy(...) → send(...) → Unity 执行 → complete 回执",
    F["sub"],
    C["muted"],
)

# column chips
chips = [
    (82, 166, 344, C["red"], "Python 后端：动作与世界状态"),
    (486, 166, 448, C["green"], "WebSocketServer：函数式抽象层"),
    (996, 166, 330, C["gold"], "统一 send()：伪 RPC 内核"),
    (1388, 166, 448, C["brown"], "Unity 执行层：WsAgentClient"),
]
for x, y, w, color, label in chips:
    draw.rounded_rectangle((x, y, x + w, y + 48), radius=10, fill=color)
    centered(draw, (x, y, x + w, y + 48), label, F["chip"], "#FFFFFF")

# Python side
box(draw, (82, 238, 344, 194), "1. ActionExecutor", [
    "LLM 输出动作提案后，规则系统校验：",
    "must_be_at(location:market)",
    "must_have_stock()",
    "must_have_enough_money()",
], C["red"])
box(draw, (82, 472, 344, 174), "2. handle_buy(ctx, act)", [
    "`unit_price = market.price(item)`",
    "`total = unit_price * qty`",
    "`await ctx.world.client.buy(...)`",
], C["red"], "#FFF8ECEE")
arrow(draw, (254, 432), (254, 472), C["red"], 4)

# WebSocketServer side
box(draw, (486, 258, 448, 104), "3. 顶层动作封装：buy(...)", [
    "`WebSocketServer.buy(...)`",
    "把一次购买表现为后端可 await 的函数调用。",
], C["green"])
rounded(draw, (486, 402, 934, 652), "#FFF8ECEE")
draw.rounded_rectangle((486, 402, 494, 652), radius=8, fill=C["green"])
text(draw, (516, 432), "4. buy 内部拆成 3 个前端调用", F["h"], C["green"])
subcalls = [
    ("show_animation(item, +qty)", "物品数量动画"),
    ("move(source, \"收银台\")", "移动到收银台"),
    ("show_animation(\"money\", -total)", "资金扣减动画"),
]
for i, (call, note) in enumerate(subcalls):
    y = 470 + i * 64
    rounded(draw, (520, y, 894, y + 46), "#FFFFFF", C["line"], 2, 8)
    text(draw, (540, y + 8), call, F["code"], C["ink"])
    text(draw, (540, y + 29), note, F["small"], C["muted"])
arrow(draw, (426, 550), (486, 310), C["red"], 4)

# send core
box(draw, (996, 248, 330, 156), "5. 中层函数 → send()", [
    "show_animation / move 只负责补充参数：",
    "type、cmd、target、value。",
    "返回值统一：is_success(result)",
], C["gold"])
box(draw, (996, 444, 330, 272), "6. send() 伪 RPC 内核", [
    "`action_id = uuid.uuid4()`",
    "`pending[action_id] = Future`",
    "`ws.send(json.dumps(payload))`",
    "`wait_for(fut, timeout=20)`",
], C["gold"], "#FFF8ECEE")
draw.rounded_rectangle((1026, 642, 1296, 688), radius=8, fill=C["dark"])
centered(draw, (1026, 642, 1296, 688), "等待 Unity complete 回填 Future", F["chip"], "#FFFFFF")
for y in (493, 557, 621):
    arrow(draw, (934, y), (996, y), C["brown"], 3)

# Unity side
box(draw, (1388, 246, 448, 192), "7. 实际发出的消息示例", [
    "`animation：target=\"bread\", value=+2`",
    "`command：cmd=\"go_to\", target=\"收银台\"`",
    "`animation：target=\"money\", value=-14`",
    "每条消息都有 agent_id 和 action_id",
], C["brown"])
box(draw, (1388, 478, 448, 258), "8. Unity 侧路由与执行", [
    "WsAgentClient.HandleRoutedMessage(msg)",
    "go_to → navigator.AddCommand(...)",
    "animation → animationQueue.Enqueue(...)",
    "完成后回传 complete(status=\"ok\")",
], C["brown"], "#FFF8ECEE")
arrow(draw, (1326, 580), (1388, 340), C["brown"], 3)
arrow(draw, (1612, 478), (1612, 438), "#B89056", 2, dash=True)

# return path
rounded(draw, (996, 784, 1836, 930), C["dark"], C["dark"])
text(draw, (1028, 818), "回执路径：Unity complete → WebSocketServer._handle() → pending.pop(action_id) → fut.set_result(msg)", F["chip"], "#FFFFFF")
text(draw, (1028, 858), "send() 恢复执行 → is_success(result) → buy 子调用继续下一步", F["chip"], "#FFFFFF")
text(draw, (1028, 896), "三个子调用均成功后，handle_buy() 才修改 Python 世界状态并广播市场 / Agent 信息", F["chip"], "#FFFFFF")
arrow(draw, (1612, 736), (1510, 784), "#B89056", 2, dash=True)
arrow(draw, (996, 850), (650, 652), "#B89056", 2, dash=True)
arrow(draw, (650, 784), (254, 642), "#B89056", 2, dash=True)

# world state update
rounded(draw, (82, 720, 932, 950), "#FFFFFFE8")
draw.rounded_rectangle((82, 720, 90, 950), radius=8, fill=C["red"])
text(draw, (112, 756), "9. Unity 动作成功后，Python 再结算真实世界状态", F["h"], C["red"])
left_lines = [
    "actor.inventory.add(item_id, qty)",
    "actor.money -= total",
    "market.remove_stock(item_id, qty)",
    "shop_assistant.money += total",
]
right_lines = [
    "_broadcast_market_information(ctx)",
    "_broadcast_message(ctx, ...)",
    "_broadcast_agent_information(ctx)",
]
for i, line in enumerate(left_lines):
    text(draw, (112, 800 + i * 30), line, F["code"], C["ink"])
for i, line in enumerate(right_lines):
    text(draw, (512, 800 + i * 30), line, F["code"], C["ink"])
text(draw, (112, 928), "关键点：Unity 只负责表现与回执，权威状态仍在 Python WorldState 中更新。", F["label"], C["red"])

# bottom thesis
draw.rounded_rectangle((82, 986, 1836, 1028), radius=12, fill="#FFF1C8", outline=C["line"], width=2)
text(draw, (114, 998), "这页要讲的核心：", F["h2"], C["ink"])
text(draw, (310, 1000), "我没有引入 RPC 框架，但通过顶层动作函数、中层通信函数和统一 send()，把 Unity 前端包装成了 Python 后端可 await 的“远端函数”。", F["body"], C["ink"])

img.save(OUT)
print(OUT)
