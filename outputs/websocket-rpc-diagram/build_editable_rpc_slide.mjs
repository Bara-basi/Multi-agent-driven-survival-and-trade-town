import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const WORKSPACE = path.dirname(__filename);
const SKILL = "C:/Users/DELL/.codex/plugins/cache/openai-primary-runtime/presentations/26.521.10419/skills/presentations";
const UTILS = path.join(SKILL, "scripts", "artifact_tool_utils.mjs");
const { ensureArtifactToolWorkspace, importArtifactTool, createSlideContext, saveBlobToFile } =
  await import(pathToFileURL(UTILS).href);

await ensureArtifactToolWorkspace(WORKSPACE);
const artifact = await importArtifactTool(WORKSPACE);
const { Presentation, PresentationFile } = artifact;

const W = 1280;
const H = 720;
const presentation = Presentation.create({ slideSize: { width: W, height: H } });
const ctx = createSlideContext(artifact, {
  slideSize: { width: W, height: H },
  workspaceDir: WORKSPACE,
  assetDir: path.join(WORKSPACE, "assets"),
  outputDir: WORKSPACE,
  titleFont: "Microsoft YaHei",
  bodyFont: "Microsoft YaHei",
});

const C = {
  ink: "#2D241C",
  muted: "#75685B",
  red: "#A34032",
  green: "#2F6B55",
  gold: "#C6963D",
  brown: "#70452A",
  line: "#D9C8AC",
  soft: "#FCFAF6",
  soft2: "#F8F3EA",
  white: "#FFFFFF",
};

function box(slide, x, y, w, h, fill = C.white, line = C.line, radius = 8) {
  return ctx.addShape(slide, {
    left: x,
    top: y,
    width: w,
    height: h,
    radius,
    fill,
    line: line ? { style: "solid", fill: line, width: 1.1 } : ctx.line(),
  });
}

function label(slide, value, x, y, w, h, opt = {}) {
  return ctx.addText(slide, {
    text: value,
    left: x,
    top: y,
    width: w,
    height: h,
    fontSize: opt.size ?? 16,
    color: opt.color ?? C.ink,
    bold: Boolean(opt.bold),
    align: opt.align ?? "left",
    valign: opt.valign ?? "top",
    typeface: "Microsoft YaHei",
    insets: opt.insets ?? { left: 6, right: 6, top: 3, bottom: 3 },
    fill: "#00000000",
    line: ctx.line(),
  });
}

function laneTitle(slide, x, y, w, title, color) {
  box(slide, x, y, w, 36, color, null, 7);
  label(slide, title, x + 8, y + 7, w - 16, 21, {
    size: 14,
    color: C.white,
    bold: true,
    align: "center",
  });
}

function stepCard(slide, x, y, w, h, no, title, body, color) {
  box(slide, x, y, w, h, C.soft, C.line, 8);
  box(slide, x, y, w, 7, color, null, 0);
  label(slide, String(no).padStart(2, "0"), x + 12, y + 24, 42, 30, {
    size: 20,
    color,
    bold: true,
    align: "center",
  });
  label(slide, title, x + 60, y + 22, w - 74, 28, {
    size: 18,
    color: C.ink,
    bold: true,
  });
  label(slide, body, x + 22, y + 66, w - 44, h - 72, {
    size: 13.2,
    color: C.muted,
  });
}

function arrow(slide, x, y, color = C.brown) {
  label(slide, "→", x, y, 42, 28, {
    size: 24,
    color,
    bold: true,
    align: "center",
    valign: "middle",
    insets: { left: 0, right: 0, top: 0, bottom: 0 },
  });
}

function downArrow(slide, x, y, color = C.green) {
  label(slide, "↓", x, y, 42, 34, {
    size: 25,
    color,
    bold: true,
    align: "center",
    valign: "middle",
    insets: { left: 0, right: 0, top: 0, bottom: 0 },
  });
}

function miniCall(slide, x, y, value, note) {
  box(slide, x, y, 278, 28, C.white, "#E1C99F", 5);
  label(slide, value, x + 10, y + 5, 212, 17, { size: 11.4, color: C.ink });
  label(slide, note, x + 220, y + 5, 50, 17, { size: 11.4, color: C.green, bold: true, align: "right" });
}

function bullet(slide, x, y, textValue, color = C.gold) {
  box(slide, x, y + 7, 8, 8, color, null, 4);
  label(slide, textValue, x + 16, y, 166, 21, { size: 12.4, color: C.ink });
}

const slide = presentation.slides.add();
box(slide, 0, 0, W, H, C.white, null, 0);
box(slide, 28, 28, W - 56, H - 56, "#00000000", "#E7D9C3", 0);

label(slide, "把 Unity 前端封装成 Python 可 await 的“远端函数”", 54, 52, 900, 42, {
  size: 30,
  bold: true,
});
label(
  slide,
  "以 buy 动作为例：后端只调用 client.buy(...)，内部自动拆成动画、移动、消息发送与回执等待。",
  56,
  100,
  870,
  24,
  { size: 15, color: C.muted },
);

laneTitle(slide, 66, 148, 240, "Python 动作层", C.red);
laneTitle(slide, 368, 148, 252, "WebSocketServer 抽象层", C.green);
laneTitle(slide, 690, 148, 212, "统一 send()", C.gold);
laneTitle(slide, 962, 148, 240, "Unity 执行层", C.brown);

stepCard(
  slide,
  66,
  226,
  240,
  126,
  1,
  "handle_buy",
  "校验库存、位置与资金后，计算总价并调用：\nctx.world.client.buy(...)",
  C.red,
);
stepCard(
  slide,
  368,
  226,
  252,
  126,
  2,
  "client.buy",
  "顶层动作函数，不直接拼 JSON。\n它把一次购买拆成 3 个前端表现调用。",
  C.green,
);
stepCard(
  slide,
  690,
  226,
  212,
  126,
  3,
  "send",
  "统一出口：生成 action_id，挂起 Future，发送 payload。",
  C.gold,
);
stepCard(
  slide,
  962,
  226,
  240,
  126,
  4,
  "WsAgentClient",
  "Unity 根据 type / cmd 路由。\n移动走 navigator，动画进入 animationQueue。",
  C.brown,
);

arrow(slide, 316, 270);
arrow(slide, 634, 270);
arrow(slide, 912, 270);

downArrow(slide, 470, 360, C.green);

box(slide, 332, 408, 324, 154, C.soft2, C.line, 8);
label(slide, "buy(...) 的三段前端调用", 362, 428, 264, 24, {
  size: 17,
  bold: true,
  color: C.green,
  align: "center",
});
miniCall(slide, 354, 472, "show_animation(item, +qty)", "物品 +");
miniCall(slide, 354, 510, "move(source, \"收银台\")", "移动");
miniCall(slide, 354, 548, "show_animation(money, -money)", "资金 -");

box(slide, 690, 408, 212, 154, C.soft2, C.line, 8);
label(slide, "send(...) 内部机制", 708, 428, 176, 24, {
  size: 17,
  bold: true,
  color: C.gold,
  align: "center",
});
bullet(slide, 712, 473, "uuid 生成 action_id");
bullet(slide, 712, 499, "pending 中挂 Future");
bullet(slide, 712, 525, "websocket.send(payload)");
bullet(slide, 712, 551, "等待 complete / 超时");

box(slide, 962, 408, 240, 154, C.soft2, C.line, 8);
label(slide, "Unity 回执", 990, 428, 180, 24, {
  size: 17,
  bold: true,
  color: C.brown,
  align: "center",
});
label(slide, "动作执行完成后发送：", 990, 474, 180, 18, { size: 13, color: C.muted });
label(slide, "complete(status=\"ok\",\naction_id=同一个 id)", 990, 506, 186, 42, {
  size: 13.2,
  color: C.ink,
});

arrow(slide, 660, 486);
arrow(slide, 912, 486);
label(
  slide,
  "回执路径：Unity complete → WebSocketServer._handle() → fut.set_result(msg) → send() 恢复执行",
  186,
  586,
  906,
  24,
  { size: 14, color: C.brown, bold: true, align: "center" },
);

box(slide, 66, 622, 828, 50, "#FFF7E8", "#E1C99F", 7);
label(slide, "回到 Python 后更新权威状态", 88, 633, 226, 20, {
  size: 16,
  color: C.red,
  bold: true,
});
label(
  slide,
  "inventory.add(...)  ·  money -= total  ·  market.remove_stock(...)  ·  broadcast_market_information(...)",
  322,
  635,
  540,
  18,
  { size: 12.8, color: C.ink },
);

box(slide, 66, 684, 1136, 25, C.soft2, "#E7D9C3", 4);
label(
  slide,
  "核心表达：这不是 RPC 框架，但通过“顶层动作函数 + 中层表现函数 + 统一 send()”，实现了 RPC 风格的调用体验。",
  84,
  689,
  1100,
  14,
  { size: 12.5, color: C.ink },
);

const out = path.join(WORKSPACE, "editable_websocket_rpc_buy_slide.pptx");
const previewPath = path.join(WORKSPACE, "editable_websocket_rpc_buy_slide_preview.png");
const pptx = await PresentationFile.exportPptx(presentation);
await pptx.save(out);
const preview = await presentation.export({ slide, format: "png", scale: 1 });
await saveBlobToFile(preview, previewPath);

console.log(JSON.stringify({ output: out, preview: previewPath }, null, 2));
