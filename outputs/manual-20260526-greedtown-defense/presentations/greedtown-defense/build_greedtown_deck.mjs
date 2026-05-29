import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { spawnSync } from "node:child_process";

const __filename = fileURLToPath(import.meta.url);
const WORKSPACE = path.dirname(__filename);
const REPO = path.resolve(WORKSPACE, "..", "..", "..", "..");
const SKILL = "C:/Users/DELL/.codex/plugins/cache/openai-primary-runtime/presentations/26.521.10419/skills/presentations";
const UTILS = path.join(SKILL, "scripts", "artifact_tool_utils.mjs");
const { ensureArtifactToolWorkspace, importArtifactTool, createSlideContext, saveBlobToFile } =
  await import(pathToFileURL(UTILS).href);

const W = 1280;
const H = 720;
const OUT = path.join(WORKSPACE, "output", "GreedTown-毕业设计答辩PPT-目录页重做.pptx");
const PREVIEW = path.join(WORKSPACE, "preview");
const CONTACT = path.join(WORKSPACE, "output", "GreedTown-答辩PPT-目录页重做-预览总览.png");
const ASSET = (...parts) => path.join(REPO, "PPT素材", ...parts);

await ensureArtifactToolWorkspace(WORKSPACE);
const artifact = await importArtifactTool(WORKSPACE);
const { Presentation, PresentationFile } = artifact;
const presentation = Presentation.create({ slideSize: { width: W, height: H } });
const ctx = createSlideContext(artifact, {
  slideSize: { width: W, height: H },
  workspaceDir: WORKSPACE,
  assetDir: path.join(WORKSPACE, "assets"),
  outputDir: path.join(WORKSPACE, "output"),
  titleFont: "Microsoft YaHei",
  bodyFont: "Microsoft YaHei",
  monoFont: "Consolas",
});

const C = {
  ink: "#392819",
  brown: "#70452A",
  red: "#A34032",
  green: "#355F4C",
  gold: "#C6963D",
  cream: "#F7EBD0",
  paper: "#FFF8E8",
  pale: "#F2DEBA",
  line: "#D4B57B",
  muted: "#7D6A53",
  white60: "#FFFFFFCC",
  white75: "#FFFFFFD9",
  white88: "#FFFFFFE8",
  dark70: "#24170FCC",
};

function slide(title, opts = {}) {
  const s = presentation.slides.add();
  const bg = opts.bg || "背景3（含校徽）.png";
  return ctx.addImage(s, { path: ASSET(bg), left: 0, top: 0, width: W, height: H, fit: "cover" }).then(() => s);
}

function rect(s, x, y, w, h, fill = C.white75, line = C.line, name) {
  return ctx.addShape(s, {
    left: x, top: y, width: w, height: h, fill,
    line: line ? { style: "solid", fill: line, width: 1.2 } : ctx.line(),
    name,
  });
}

function text(s, value, x, y, w, h, opt = {}) {
  return ctx.addText(s, {
    text: value,
    left: x, top: y, width: w, height: h,
    fontSize: opt.size ?? 24,
    color: opt.color ?? C.ink,
    bold: opt.bold ?? false,
    align: opt.align ?? "left",
    valign: opt.valign ?? "top",
    typeface: opt.face ?? "Microsoft YaHei",
    insets: opt.insets ?? { left: 8, right: 8, top: 4, bottom: 4 },
    fill: opt.fill ?? "#00000000",
    line: opt.line ?? ctx.line(),
  });
}

async function img(s, file, x, y, w, h, fit = "contain") {
  return ctx.addImage(s, { path: ASSET(file), left: x, top: y, width: w, height: h, fit, alt: file });
}

function titleBlock(s, num, titleValue, claim) {
  rect(s, 54, 34, 9, 54, C.red, null);
  text(s, String(num).padStart(2, "0"), 72, 36, 54, 28, { size: 15, color: C.red, bold: true });
  text(s, titleValue, 122, 26, 680, 42, { size: 29, bold: true });
  if (claim) text(s, claim, 122, 66, 820, 31, { size: 15, color: C.muted });
}

function footer(s, n) {
  text(s, "GreedTown · LLM Agent 经营博弈系统", 58, 676, 420, 20, { size: 11, color: C.muted });
  text(s, String(n).padStart(2, "0"), 1188, 675, 40, 20, { size: 12, color: C.red, bold: true, align: "right" });
}

function bullet(s, lines, x, y, w, gap = 42, opt = {}) {
  lines.forEach((line, i) => {
    const yy = y + i * gap;
    rect(s, x, yy + 8, 9, 9, opt.dot ?? C.red, null);
    text(s, line, x + 22, yy, w - 22, 34, { size: opt.size ?? 19, color: opt.color ?? C.ink });
  });
}

function pill(s, value, x, y, w, color = C.green) {
  rect(s, x, y, w, 34, "#FFFFFFD9", color);
  text(s, value, x + 8, y + 6, w - 16, 22, { size: 13, color, bold: true, align: "center" });
}

function callout(s, head, body, x, y, w, h, accent = C.red) {
  rect(s, x, y, w, h, C.white88, C.line);
  rect(s, x, y, 7, h, accent, null);
  text(s, head, x + 22, y + 18, w - 40, 28, { size: 20, bold: true, color: accent });
  text(s, body, x + 22, y + 56, w - 40, h - 70, { size: 16, color: C.ink });
}

function metric(s, value, label, x, y, w, accent = C.red) {
  rect(s, x, y, w, 108, "#FFFFFFD9", C.line);
  text(s, value, x + 10, y + 18, w - 20, 40, { size: 28, color: accent, bold: true, align: "center" });
  text(s, label, x + 12, y + 64, w - 24, 24, { size: 13, color: C.muted, align: "center" });
}

function nodeBox(s, label, sub, x, y, w, h, fill, stroke = C.line) {
  rect(s, x, y, w, h, fill, stroke);
  text(s, label, x + 14, y + 14, w - 28, 26, { size: 17, bold: true, color: C.ink, align: "center" });
  if (sub) text(s, sub, x + 14, y + 44, w - 28, h - 48, { size: 12.5, color: C.muted, align: "center" });
}

function arrowLine(s, x1, y1, x2, y2, color = C.brown) {
  if (Math.abs(y2 - y1) < 4) {
    rect(s, Math.min(x1, x2), y1 - 2, Math.abs(x2 - x1), 4, color, null);
  } else {
    rect(s, x1 - 2, Math.min(y1, y2), 4, Math.abs(y2 - y1), color, null);
  }
}

async function cover() {
  const s = await slide("", { bg: "背景1（含校徽）.png" });
  rect(s, 72, 82, 760, 470, "#FFFFFFCC", C.line);
  rect(s, 72, 82, 14, 470, C.red, null);
  await img(s, "插画1.png", 770, 160, 410, 300);
  text(s, "本科毕业设计答辩", 116, 112, 360, 34, { size: 24, color: C.red, bold: true });
  text(s, "GreedTown：基于 LLM Agent 的\n2D 商店经营与生存博弈系统", 112, 162, 650, 112, { size: 35, bold: true, color: C.ink });
  text(s, "从 AI 自主决策到人机经济博弈的闭环仿真", 116, 292, 600, 32, { size: 20, color: C.green, bold: true });
  const fields = [
    ["答辩人", "姓名"],
    ["专业", "专业名称"],
    ["导师", "导师姓名"],
    ["学院", "学院名称"],
    ["答辩时间", "2026 年  月  日"],
  ];
  fields.forEach((r, i) => {
    const y = 358 + i * 35;
    text(s, r[0], 118, y, 92, 24, { size: 15, color: C.muted, bold: true });
    text(s, r[1], 222, y, 260, 24, { size: 15, color: C.ink });
  });
  await img(s, "图案1.png", 1022, 480, 82, 74);
  await img(s, "图案7.png", 1135, 520, 76, 62);
  footer(s, 1);
}

async function toc() {
  const s = await slide("目录", { bg: "背景2（含校徽）.png" });
  titleBlock(s, 2, "答辩目录", "围绕“系统闭环”和“人机经济博弈”展开");
  const items = [
    "系统目标和\n核心玩法",
    "总体架构",
    "流程闭环",
    "Agent 决策链\n设计",
    "动作系统与\n动作空间",
    "WebSocket\n伪 RPC 通信",
    "经济系统",
    "技能模组与\n随机事件",
  ];
  const cardW = 244;
  const cardH = 142;
  const startX = 92;
  const gapX = 44;
  const rowY = [164, 382];
  const accents = [C.red, C.green, C.gold, C.brown, C.red, C.green, C.gold, C.brown];
  for (let i = 0; i < items.length; i++) {
    const col = i % 4;
    const row = Math.floor(i / 4);
    const x = startX + col * (cardW + gapX);
    const y = rowY[row];
    rect(s, x, y, cardW, cardH, "#FFFFFFE6", C.line);
    rect(s, x, y, cardW, 8, accents[i], null);
    text(s, String(i + 1).padStart(2, "0"), x + 20, y + 26, 54, 34, {
      size: 24,
      color: accents[i],
      bold: true,
      align: "left",
    });
    await img(s, `图案${(i % 8) + 1}.png`, x + cardW - 66, y + 22, 42, 40);
    text(s, items[i], x + 22, y + 74, cardW - 44, 48, {
      size: items[i].includes("WebSocket") ? 22 : 23,
      color: C.ink,
      bold: true,
      align: "center",
      valign: "middle",
    });
  }
  rect(s, 230, 594, 820, 42, "#F2DEBAE8", C.gold);
  text(s, "目录只承担导航功能：两排八项，对应后续技术叙事的主要章节。", 254, 604, 772, 22, {
    size: 17,
    color: C.ink,
    bold: true,
    align: "center",
  });
  footer(s, 2);
}

async function problem() {
  const s = await slide("问题定义");
  titleBlock(s, 3, "项目要解决的问题", "不是把大模型接进游戏，而是让 Agent 在规则世界里稳定行动");
  await img(s, "插画2.png", 820, 146, 340, 250);
  callout(s, "问题一：LLM 如何持续行动？", "Agent 必须读懂世界状态，生成计划、动作和反思，并在失败后重新规划，而不是只完成一次文本回答。", 86, 150, 350, 150, C.red);
  callout(s, "问题二：动作边界由谁裁决？", "模型输出只能作为动作提案，真正能否进入世界，由确定性代码、库存、资金、位置等规则共同裁决。", 464, 150, 350, 150, C.green);
  callout(s, "问题三：玩家如何进入闭环？", "玩家不是旁观者，而是市场供给和价格的控制节点；Unity 收集经营操作后回传后端结算。", 86, 338, 350, 150, C.gold);
  callout(s, "问题四：系统如何可解释？", "市场价格、库存恢复、交易摩擦和胜负条件都有明确参数，方便展示、调试和答辩追问。", 464, 338, 350, 150, C.brown);
  await img(s, "图案3.png", 1110, 86, 58, 56);
  footer(s, 3);
}

async function goals() {
  const s = await slide("系统目标");
  titleBlock(s, 4, "系统目标与核心玩法", "玩家经营唯一商店，四个 AI Agent 自主生存、消费、交易与投机");
  rect(s, 82, 132, 510, 400, C.white88, C.line);
  text(s, "胜负条件", 112, 158, 180, 34, { size: 25, bold: true, color: C.red });
  bullet(s, [
    "玩家资金达到 10000：胜利",
    "玩家破产：失败",
    "任一 AI 生存属性归零：失败",
    "任一 AI 先达到 10000：失败",
  ], 118, 220, 420, 48, { size: 20 });
  metric(s, "4", "AI Agent", 642, 145, 148, C.green);
  metric(s, "5", "商品类型", 812, 145, 148, C.gold);
  metric(s, "2", "核心地点", 982, 145, 148, C.red);
  rect(s, 642, 300, 488, 178, "#FFFFFFD9", C.line);
  text(s, "核心体验", 672, 326, 150, 30, { size: 23, bold: true, color: C.green });
  text(s, "这不是单纯的经营模拟，也不是 AI 自动对战，而是玩家与 Agent 在同一市场中争夺节奏的“不对称博弈”。", 672, 370, 408, 72, { size: 19, color: C.ink });
  await img(s, "图案8.png", 1054, 504, 68, 74);
  footer(s, 4);
}

async function tension() {
  const s = await slide("设计矛盾");
  titleBlock(s, 5, "核心设计矛盾：戴着镣铐跳舞", "答辩主线：系统真正的难点在于动态平衡，而不是堆功能");
  const xs = [114, 402, 690, 978];
  const heads = ["赚得更多", "不能压垮 AI", "供给充足", "不能让 AI 暴富"];
  const bodies = ["玩家需要通过定价和进货扩大现金流。", "AI 饥饿、水分、精神归零会直接导致失败。", "库存不足会破坏生存链和交易链。", "AI 也会套利并抢先达到胜利资金。"];
  const colors = [C.red, C.green, C.gold, C.brown];
  for (let i = 0; i < 4; i++) {
    rect(s, xs[i], 176, 210, 245, "#FFFFFFD9", C.line);
    await img(s, `图案${i + 4}.png`, xs[i] + 70, 196, 70, 66);
    text(s, heads[i], xs[i] + 18, 282, 174, 30, { size: 20, color: colors[i], bold: true, align: "center" });
    text(s, bodies[i], xs[i] + 20, 328, 170, 62, { size: 15, color: C.ink, align: "center" });
  }
  rect(s, 260, 488, 760, 74, "#F7EBD0E6", C.gold);
  text(s, "设计目标不是找到单一最优策略，而是让玩家持续在利润、库存、生存供给和 AI 反制之间权衡。", 298, 508, 684, 34, { size: 20, bold: true, color: C.ink, align: "center" });
  footer(s, 5);
}

async function architecture() {
  const s = await slide("总体架构");
  titleBlock(s, 6, "总体架构：DecisionLayer + ExecutionLayer", "Python 决策层负责状态和规则，Unity 执行层负责可视化和交互");
  rect(s, 82, 142, 480, 380, "#FFF8E8E8", C.line);
  rect(s, 718, 142, 480, 380, "#FFF8E8E8", C.line);
  text(s, "DecisionLayer / Python", 114, 166, 260, 28, { size: 22, bold: true, color: C.red });
  text(s, "ExecutionLayer / Unity 2D", 750, 166, 300, 28, { size: 22, bold: true, color: C.green });
  nodeBox(s, "WorldState", "角色 / 地点 / 商品 / 事件 / 玩家资金", 130, 220, 170, 88, "#FFFFFFD9");
  nodeBox(s, "AgentRuntime", "plan → act → execute → reflect", 330, 220, 178, 88, "#FFFFFFD9");
  nodeBox(s, "ActionExecutor", "注册表 / 校验器 / handler", 130, 346, 170, 88, "#FFFFFFD9");
  nodeBox(s, "Market Settlement", "价格推进 / 库存 / 胜负判断", 330, 346, 178, 88, "#FFFFFFD9");
  nodeBox(s, "2D Town", "场景实体 / 移动 / 动画", 766, 220, 170, 88, "#FFFFFFD9");
  nodeBox(s, "Shop UI", "定价 / 进货 / 资金 / 回合", 966, 220, 178, 88, "#FFFFFFD9");
  nodeBox(s, "HUD Feedback", "状态条 / 消息 / 游戏结束", 766, 346, 170, 88, "#FFFFFFD9");
  nodeBox(s, "Player Input", "经营操作回传后端", 966, 346, 178, 88, "#FFFFFFD9");
  rect(s, 584, 272, 112, 118, "#F2DEBAD9", C.gold);
  text(s, "WebSocket\nRPC", 596, 306, 88, 44, { size: 22, color: C.brown, bold: true, align: "center" });
  arrowLine(s, 510, 330, 584, 330);
  arrowLine(s, 696, 330, 766, 330);
  pill(s, "后端生成世界与 Agent 决策", 184, 548, 270, C.red);
  pill(s, "Unity 展示并收集玩家经营操作", 828, 548, 286, C.green);
  footer(s, 6);
}

async function roundFlow() {
  const s = await slide("回合闭环");
  titleBlock(s, 7, "回合闭环：从商店阶段到日结算", "系统不是模块拼接，而是一个可回执、可重规划、可结算的循环");
  const steps = [
    ["商店阶段", "玩家提交价格与进货"],
    ["AI 规划", "基于状态生成计划"],
    ["AI 行动", "输出结构化动作"],
    ["动作校验", "位置 / 库存 / 资金"],
    ["Unity 执行", "动画和反馈回执"],
    ["回合结算", "价格 / 库存 / 事件 / 胜负"],
  ];
  const pos = [[98, 170], [386, 170], [674, 170], [674, 378], [386, 378], [98, 378]];
  steps.forEach((st, i) => {
    const [x, y] = pos[i];
    nodeBox(s, st[0], st[1], x, y, 210, 100, i === 0 ? "#F2DEBAE8" : "#FFFFFFD9", i === 0 ? C.gold : C.line);
  });
  arrowLine(s, 308, 220, 386, 220);
  arrowLine(s, 596, 220, 674, 220);
  arrowLine(s, 779, 270, 779, 378);
  arrowLine(s, 674, 428, 596, 428);
  arrowLine(s, 386, 428, 308, 428);
  arrowLine(s, 203, 378, 203, 270);
  rect(s, 954, 176, 214, 282, "#FFFFFFD9", C.line);
  text(s, "结算内容", 986, 202, 160, 28, { size: 22, color: C.red, bold: true, align: "center" });
  bullet(s, ["玩家当日收入", "市场价格推进", "AI 生存消耗", "随机事件触发", "决策点影响"], 990, 254, 150, 34, { size: 15, dot: C.gold });
  footer(s, 7);
}

async function agentChain() {
  const s = await slide("Agent决策链");
  titleBlock(s, 8, "Agent 决策链：plan → act → execute → reflect", "LLM 不是单次问答，而是被放进持续运行的状态机");
  const items = [
    ["PLAN", "生成阶段性计划\n理解目标与资源"],
    ["ACT", "输出 JSON 动作\n降低解析失败"],
    ["EXECUTE", "规则校验后执行\n同步 Unity 回执"],
    ["REFLECT", "记录反思\n进入下一轮上下文"],
  ];
  for (let i = 0; i < items.length; i++) {
    const x = 92 + i * 286;
    rect(s, x, 210, 222, 178, "#FFFFFFD9", C.line);
    text(s, items[i][0], x + 18, 232, 186, 34, { size: 28, color: [C.red, C.green, C.gold, C.brown][i], bold: true, align: "center" });
    text(s, items[i][1], x + 24, 292, 174, 62, { size: 16, color: C.ink, align: "center" });
    if (i < items.length - 1) arrowLine(s, x + 222, 299, x + 286, 299);
  }
  rect(s, 176, 468, 928, 72, "#FFF8E8E8", C.gold);
  text(s, "失败恢复策略：动作非法、重复循环、交易震荡、拆分动作等情况会触发错误反馈和强制重规划。", 210, 490, 860, 28, { size: 19, bold: true, color: C.ink, align: "center" });
  footer(s, 8);
}

async function actionSystem() {
  const s = await slide("动作裁决");
  titleBlock(s, 9, "动作系统：LLM 只提议，规则系统来裁决", "把模型输出变成“待审查动作提案”，而不是直接执行的命令");
  rect(s, 86, 154, 340, 360, "#FFFFFFD9", C.line);
  rect(s, 470, 154, 340, 360, "#FFFFFFD9", C.line);
  rect(s, 854, 154, 340, 360, "#FFFFFFD9", C.line);
  text(s, "动作注册表", 132, 184, 250, 32, { size: 23, color: C.red, bold: true, align: "center" });
  bullet(s, ["move", "buy / sell", "consume", "sleep / wait", "skill"], 146, 246, 200, 38, { size: 18, dot: C.red });
  text(s, "Validator", 516, 184, 250, 32, { size: 23, color: C.green, bold: true, align: "center" });
  bullet(s, ["是否在市场", "库存是否足够", "资金是否足够", "背包是否有物品", "是否触发保护机制"], 532, 246, 230, 38, { size: 18, dot: C.green });
  text(s, "Handler", 900, 184, 250, 32, { size: 23, color: C.gold, bold: true, align: "center" });
  bullet(s, ["修改世界状态", "扣除/增加资金", "更新库存", "广播市场信息", "通知 Unity 动画"], 916, 246, 230, 38, { size: 18, dot: C.gold });
  rect(s, 210, 552, 860, 54, "#F2DEBAE8", C.gold);
  text(s, "答辩表达：系统可靠性来自 PromptBuilder + JSON 输出约束 + ActionExecutor 确定性校验三层结构。", 236, 568, 808, 24, { size: 18, color: C.ink, bold: true, align: "center" });
  footer(s, 9);
}

async function websocket() {
  const s = await slide("通信机制");
  titleBlock(s, 10, "WebSocket 伪 RPC：把 Unity 当作远端动作服务", "每个动作有唯一 action_id，后端等待 Unity 回执后继续推进");
  const labels = [
    ["Python 后端", "生成 command + action_id"],
    ["pending 表", "挂起 Future，等待 complete"],
    ["Unity 客户端", "执行移动、交易、动画、UI"],
    ["complete 回执", "同 action_id 返回状态"],
  ];
  labels.forEach((v, i) => {
    const y = 156 + i * 105;
    nodeBox(s, v[0], v[1], i % 2 === 0 ? 120 : 760, y, 360, 76, i % 2 === 0 ? "#FFFFFFD9" : "#F2DEBAE8");
  });
  arrowLine(s, 480, 194, 760, 194);
  arrowLine(s, 940, 232, 940, 261);
  arrowLine(s, 760, 299, 480, 299);
  arrowLine(s, 300, 337, 300, 366);
  arrowLine(s, 480, 404, 760, 404);
  rect(s, 142, 584, 996, 44, "#FFFFFFD9", C.line);
  text(s, "协议价值：决策层与执行层解耦；超时、失败、日志、重试都能放在通信层处理。", 170, 596, 940, 24, { size: 18, color: C.ink, bold: true, align: "center" });
  await img(s, "图案10.png", 596, 265, 70, 68);
  footer(s, 10);
}

async function economy() {
  const s = await slide("经济模型");
  titleBlock(s, 11, "经济系统：不是随机涨跌，而是可解释市场", "均值回归 + 对数噪声 + 库存恢复 + 交易摩擦");
  rect(s, 78, 138, 600, 212, "#FFFFFFD9", C.line);
  text(s, "价格模型", 112, 166, 160, 30, { size: 24, color: C.red, bold: true });
  text(s, "log Pₜ₊₁ = log Pₜ + κ(log P* − log Pₜ) + εₜ", 118, 218, 520, 34, { size: 24, face: "Cambria Math", color: C.ink, bold: true });
  text(s, "εₜ ~ N(0, σ²)，在对数空间建模：价格天然为正，控制相对波动率。", 118, 276, 510, 28, { size: 17, color: C.muted });
  metric(s, "κ = 0.11", "约 6 天偏离半衰期", 728, 146, 160, C.red);
  metric(s, "σ = 0.15", "消耗品约 ±16%", 914, 146, 160, C.green);
  metric(s, "σ = 0.4", "贵重品接近 ±50%", 1100, 146, 160, C.gold);
  const rows = [
    ["基本面锚点", "basePrice"],
    ["交易摩擦", "sellRatio"],
    ["供给约束", "stock + restock"],
    ["品类风险", "consumable / valuable"],
  ];
  rows.forEach((r, i) => {
    const y = 410 + i * 46;
    rect(s, 156, y, 390, 34, i % 2 ? "#FFF8E8CC" : "#FFFFFFD9", C.line);
    text(s, r[0], 176, y + 6, 150, 20, { size: 15, color: C.ink, bold: true });
    text(s, r[1], 348, y + 6, 160, 20, { size: 15, color: C.red, face: "Consolas" });
  });
  callout(s, "设计结果", "消耗品偏稳定，支撑生存节奏；贵重品偏投机，提供财富博弈窗口。市场会持续给出机会，但不会长期失控。", 650, 402, 450, 150, C.green);
  footer(s, 11);
}

async function gameTheory() {
  const s = await slide("不对称博弈");
  titleBlock(s, 12, "玩家与 AI 的不对称博弈", "玩家控制市场供给，AI 控制行动选择，并通过决策点反制玩家");
  await img(s, "插画3.png", 470, 168, 340, 244);
  callout(s, "玩家掌握", "商品定价、进货数量、现金流与库存节奏。玩家必须赚钱，但不能让市场生态崩溃。", 86, 176, 330, 170, C.red);
  callout(s, "AI 掌握", "行动选择、购买/出售、消费生存品、投机贵重品，并基于记忆与状态持续规划。", 864, 176, 330, 170, C.green);
  rect(s, 228, 472, 824, 90, "#FFF8E8E8", C.gold);
  text(s, "决策点机制", 250, 494, 150, 28, { size: 22, bold: true, color: C.gold });
  text(s, "锁定商品价格 · 获取价格情报 · 改变后续行动策略 · 让玩家不能只依赖固定定价套路", 412, 498, 600, 24, { size: 18, color: C.ink });
  await img(s, "图案12.png", 1018, 484, 52, 44);
  footer(s, 12);
}

async function dataConfig() {
  const s = await slide("数据配置");
  titleBlock(s, 13, "技能模组与随机事件", "事件和技能都以数据形式进入结算 hook，支撑玩法扩展");
  const cols = [
    ["角色", ["林墨墨：手艺人", "江凡：平民", "钟启恒：银行家", "石老谋：商人"]],
    ["商品", ["瓶装水 / 面包 / 烤肉", "银戒指 / 黄金", "消耗品与贵重品分层"]],
    ["随机事件", ["下雨天：移动消耗增加", "过路费：移动扣除现金"]],
    ["技能模组", ["利滚利：回合结算触发", "通过 hooks 接入规则系统"]],
  ];
  cols.forEach((col, i) => {
    const x = 80 + i * 294;
    rect(s, x, 166, 244, 330, "#FFFFFFD9", C.line);
    text(s, col[0], x + 24, 194, 190, 30, { size: 23, color: [C.red, C.green, C.gold, C.brown][i], bold: true, align: "center" });
    bullet(s, col[1], x + 28, 258, 190, 44, { size: 15.5, dot: [C.red, C.green, C.gold, C.brown][i] });
  });
  rect(s, 170, 548, 940, 48, "#F2DEBAE8", C.gold);
  text(s, "答辩价值：内容配置与运行状态分离，新增技能或事件时主要修改数据与 hook，不侵入 Agent 主循环。", 194, 562, 892, 22, { size: 17, color: C.ink, bold: true, align: "center" });
  footer(s, 13);
}

async function frontend() {
  const s = await slide("前端展示");
  titleBlock(s, 14, "Unity 前端：把后端状态变成可操作界面", "前端不是装饰层，而是玩家输入和动作反馈的执行层");
  await img(s, "插画4.png", 754, 154, 360, 258);
  rect(s, 86, 150, 560, 364, "#FFFFFFD9", C.line);
  text(s, "已实现交互", 122, 178, 180, 30, { size: 24, color: C.red, bold: true });
  bullet(s, [
    "商店库存界面：商品、价格、库存、进货",
    "玩家资金：当前资金与当日收入",
    "AI 状态面板：饥饿、水分、精神、资金",
    "消息流：交易、事件、决策点反馈",
    "回合动画：开始、结束、游戏胜负",
  ], 126, 246, 460, 42, { size: 17 });
  rect(s, 720, 456, 430, 84, "#FFF8E8E8", C.gold);
  text(s, "Unity 侧通过 WsAgentClient 接收 command，并在动作完成后发送 complete 回执，保证后端决策链可继续推进。", 744, 476, 382, 38, { size: 17, color: C.ink, bold: true, align: "center" });
  footer(s, 14);
}

async function summary() {
  const s = await slide("总结");
  titleBlock(s, 15, "总结与展望", "项目价值在于把 LLM 放进有状态、有规则、有反馈的运行系统");
  callout(s, "已完成", "LLM Agent 决策闭环、Unity 可视化执行、动作校验、市场结算、玩家商店阶段、随机事件与胜负判断。", 88, 162, 340, 170, C.red);
  callout(s, "项目价值", "从“能回答问题的大模型”转向“能在规则世界中持续行动的 Agent”，并通过玩家经营形成可展示的博弈系统。", 470, 162, 340, 170, C.green);
  callout(s, "不足", "经济参数仍以经验调参为主；Agent 策略稳定性受模型影响；场景规模和内容数量仍有扩展空间。", 852, 162, 340, 170, C.gold);
  rect(s, 158, 420, 964, 98, "#FFFFFFD9", C.line);
  text(s, "后续方向", 190, 448, 130, 28, { size: 23, bold: true, color: C.brown });
  text(s, "更丰富的职业技能 · 长期记忆 · 自动化回放测试 · 策略评估指标 · 更大规模小镇场景", 336, 452, 736, 24, { size: 19, color: C.ink, bold: true });
  await img(s, "图案16.png", 1050, 550, 76, 74);
  footer(s, 15);
}

const builders = [
  cover,
  toc,
  problem,
  goals,
  tension,
  architecture,
  roundFlow,
  agentChain,
  actionSystem,
  websocket,
  economy,
  gameTheory,
  dataConfig,
  frontend,
  summary,
];

await fs.mkdir(path.join(WORKSPACE, "output"), { recursive: true });
await fs.mkdir(PREVIEW, { recursive: true });
await fs.writeFile(path.join(WORKSPACE, "profile-plan.txt"), [
  "task mode: create",
  "primary deck-profile: engineering-platform",
  "required proof objects: architecture map, workflow loop, action validation model, WebSocket RPC sequence, economic model, product proof UI slide",
  "asset rule: use supplied backgrounds, illustrations, and icons selectively; do not overcrowd panels",
].join("\n"), "utf8");
await fs.writeFile(path.join(WORKSPACE, "source-notes.txt"), [
  "Local source files used: README.md, docs/technical-implementation.md, docs/economy-system.md, DecisionLayer source files, ExecutionLayer Unity scripts.",
  "Local image assets used from PPT素材: background images, selected illustrations, and selected small decorative patterns supplied by user.",
].join("\n"), "utf8");

for (const build of builders) {
  await build();
}

const previewPaths = [];
for (let i = 0; i < presentation.slides.count; i++) {
  const slideObj = presentation.slides.getItem(i);
  const out = path.join(PREVIEW, `slide-${String(i + 1).padStart(2, "0")}.png`);
  const preview = await presentation.export({ slide: slideObj, format: "png", scale: 1 });
  await saveBlobToFile(preview, out);
  previewPaths.push(out);
}

const pptx = await PresentationFile.exportPptx(presentation);
await pptx.save(OUT);
const makeContactSheet = path.join(SKILL, "scripts", "make_contact_sheet.py");
const py = "C:/Users/DELL/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe";
const result = spawnSync(py, [makeContactSheet, "--output", CONTACT, ...previewPaths], { encoding: "utf8" });
if (result.status !== 0) {
  throw new Error(`contact sheet failed\n${result.stdout}\n${result.stderr}`);
}
console.log(JSON.stringify({ output: OUT, contactSheet: CONTACT, slideCount: presentation.slides.count, previewDir: PREVIEW }, null, 2));
