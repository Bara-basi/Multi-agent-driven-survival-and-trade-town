from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parent
OUT = ROOT / "buy_action_websocket_rpc_flow_clean.png"
W, H = 1920, 1080

FONT_REG = "C:/Windows/Fonts/msyh.ttc"
FONT_BOLD = "C:/Windows/Fonts/msyhbd.ttc"


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(FONT_BOLD if bold else FONT_REG, size)


F = {
    "title": font(50, True),
    "subtitle": font(24),
    "section": font(24, True),
    "cardTitle": font(25, True),
    "body": font(20),
    "small": font(17),
    "code": font(18),
    "chip": font(18, True),
    "big": font(30, True),
}

C = {
    "ink": "#2E241B",
    "muted": "#766657",
    "red": "#A34032",
    "green": "#2F6B55",
    "gold": "#C6963D",
    "brown": "#70452A",
    "line": "#D8C7AA",
    "paper": "#FFFFFF",
    "soft": "#F8F3EA",
    "soft2": "#FCFAF5",
    "dark": "#4D3324",
}


def text(draw: ImageDraw.ImageDraw, xy, value: str, f, fill=C["ink"], max_width: int | None = None, gap: int = 6) -> int:
    x, y = xy
    lines: list[str] = []
    for raw in str(value).splitlines():
        if max_width is None:
            lines.append(raw)
            continue
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
        b = draw.textbbox((x, y), line, font=f)
        y += b[3] - b[1] + gap
    return y


def center(draw, box, value, f, fill=C["ink"], gap=6):
    x1, y1, x2, y2 = box
    lines = str(value).splitlines()
    heights = [draw.textbbox((0, 0), line, font=f)[3] for line in lines]
    total = sum(heights) + gap * (len(lines) - 1)
    y = y1 + (y2 - y1 - total) / 2
    for line, h in zip(lines, heights):
        tw = draw.textlength(line, font=f)
        draw.text((x1 + (x2 - x1 - tw) / 2, y), line, font=f, fill=fill)
        y += h + gap


def rr(draw, box, fill, outline=C["line"], width=2, radius=18):
    draw.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)


def arrow(draw, start, end, color=C["brown"], width=4, dashed=False):
    x1, y1 = start
    x2, y2 = end
    length = math.hypot(x2 - x1, y2 - y1)
    if length == 0:
        return
    dx, dy = (x2 - x1) / length, (y2 - y1) / length
    if dashed:
        d = 0
        while d < length - 22:
            d2 = min(d + 14, length - 22)
            draw.line((x1 + dx * d, y1 + dy * d, x1 + dx * d2, y1 + dy * d2), fill=color, width=width)
            d += 26
    else:
        draw.line((x1, y1, x2, y2), fill=color, width=width)
    angle = math.atan2(y2 - y1, x2 - x1)
    size = 15
    p1 = (x2, y2)
    p2 = (x2 - size * math.cos(angle - 0.45), y2 - size * math.sin(angle - 0.45))
    p3 = (x2 - size * math.cos(angle + 0.45), y2 - size * math.sin(angle + 0.45))
    draw.polygon([p1, p2, p3], fill=color)


def stage(draw, x, y, w, h, num, title, body, color):
    rr(draw, (x, y, x + w, y + h), C["soft2"], C["line"], 2, 20)
    draw.rounded_rectangle((x, y, x + w, y + 12), radius=14, fill=color)
    center(draw, (x + 22, y + 26, x + 80, y + 70), f"{num:02d}", F["big"], color)
    text(draw, (x + 92, y + 28), title, F["cardTitle"], C["ink"], max_width=w - 116)
    text(draw, (x + 30, y + 88), body, F["body"], C["muted"], max_width=w - 60)


img = Image.new("RGB", (W, H), C["paper"])
draw = ImageDraw.Draw(img)

# subtle frame
draw.rectangle((0, 0, W, H), fill="#FFFFFF")
draw.rectangle((42, 42, W - 42, H - 42), outline="#E6D6BF", width=3)
draw.rectangle((58, 58, W - 58, H - 58), outline="#F3E8D5", width=2)

text(draw, (86, 84), "把 Unity 前端包装成 Python 可 await 的“远端函数”", F["title"], C["ink"])
text(draw, (90, 148), "以 buy 动作为例：后端只调用 client.buy(...)，内部自动拆成动画、移动、消息发送和回执等待。", F["subtitle"], C["muted"])

# layer headers
headers = [
    (98, 220, 360, C["red"], "Python 动作层"),
    (560, 220, 360, C["green"], "WebSocketServer 抽象层"),
    (1022, 220, 318, C["gold"], "统一 send()"),
    (1442, 220, 360, C["brown"], "Unity 执行层"),
]
for x, y, w, color, label in headers:
    draw.rounded_rectangle((x, y, x + w, y + 52), radius=14, fill=color)
    center(draw, (x, y, x + w, y + 52), label, F["chip"], "#FFFFFF")

stage(
    draw,
    98,
    328,
    360,
    188,
    1,
    "handle_buy(ctx, act)",
    "完成规则校验后，计算单价和总价，然后调用：\nctx.world.client.buy(...)",
    C["red"],
)
stage(
    draw,
    560,
    328,
    360,
    188,
    2,
    "WebSocketServer.buy(...)",
    "顶层动作函数，不直接处理 JSON。\n它把一次购买拆成 3 个前端表现调用。",
    C["green"],
)
stage(
    draw,
    1022,
    328,
    318,
    188,
    3,
    "send(...)",
    "所有通信最终进入这里：\n生成 action_id，挂起 Future，发送 payload。",
    C["gold"],
)
stage(
    draw,
    1442,
    328,
    360,
    188,
    4,
    "WsAgentClient",
    "Unity 根据 type / cmd 路由：\n移动走 navigator，动画进入 animationQueue。",
    C["brown"],
)

arrow(draw, (458, 422), (560, 422), C["brown"])
arrow(draw, (920, 422), (1022, 422), C["brown"])
arrow(draw, (1340, 422), (1442, 422), C["brown"])

# buy expansion
rr(draw, (520, 606, 944, 822), C["soft"], C["line"], 2, 18)
text(draw, (592, 632), "buy(...) 的三个子调用", F["section"], C["green"])
sub = [
    "show_animation(item, +qty)",
    "move(source, \"收银台\")",
    "show_animation(\"money\", -total)",
]
for i, a in enumerate(sub):
    y = 682 + i * 42
    draw.rounded_rectangle((592, y, 872, y + 30), radius=8, fill="#FFFFFF", outline="#E1C99F", width=1)
    text(draw, (606, y + 5), a, F["small"], C["ink"])
text(draw, (876, 684), "物品增加\n移动到收银台\n资金扣减", F["small"], C["muted"], gap=13)

rr(draw, (1022, 606, 1340, 822), C["soft"], C["line"], 2, 18)
text(draw, (1054, 632), "send(...) 做了什么", F["section"], C["gold"])
send_steps = ["uuid 生成 action_id", "pending[action_id] = Future", "ws.send(payload)", "等待 complete 或超时"]
for i, s in enumerate(send_steps):
    y = 684 + i * 34
    draw.ellipse((1058, y + 7, 1070, y + 19), fill=C["gold"])
    text(draw, (1082, y), s, F["body"], C["ink"])

rr(draw, (1442, 606, 1802, 822), C["soft"], C["line"], 2, 18)
text(draw, (1474, 632), "Unity 回执", F["section"], C["brown"])
text(draw, (1474, 684), "动作完成后发送：", F["body"], C["muted"])
text(draw, (1474, 724), "complete(status=\"ok\", action_id=同一个)", F["body"], C["ink"])
text(draw, (1474, 770), "后端用 action_id 找回 Future", F["body"], C["muted"])

arrow(draw, (740, 516), (740, 606), C["green"])
arrow(draw, (944, 714), (1022, 714), C["brown"])
arrow(draw, (1340, 714), (1442, 714), C["brown"])

# return path
arrow(draw, (1622, 822), (1622, 910), C["brown"], dashed=True)
arrow(draw, (1622, 910), (270, 910), C["brown"], dashed=True)
arrow(draw, (270, 910), (270, 516), C["brown"], dashed=True)

rr(draw, (98, 858, 1338, 976), "#FFF7E8", "#E1C99F", 2, 18)
text(draw, (132, 884), "回到 Python 后再更新权威状态", F["section"], C["red"])
text(draw, (132, 930), "actor.inventory.add(...)  ·  actor.money -= total  ·  market.remove_stock(...)  ·  broadcast_market_information(...)", F["body"], C["ink"])

rr(draw, (98, 1000, 1802, 1034), "#F8F3EA", "#E6D6BF", 1, 12)
text(draw, (128, 1007), "核心表达：这不是 RPC 框架，但通过顶层动作函数 + 中层表现函数 + 统一 send()，实现了 RPC 风格的调用体验。", F["body"], C["ink"])

img.save(OUT)
print(OUT)
