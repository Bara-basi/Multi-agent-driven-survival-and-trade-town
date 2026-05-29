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
  ink: "#2B241E",
  muted: "#6E6258",
  red: "#A94034",
  green: "#276B55",
  blue: "#315E8A",
  gold: "#C39234",
  brown: "#74482C",
  purple: "#6A5685",
  line: "#D8C7AA",
  soft: "#FCFAF6",
  softGreen: "#F0F7F2",
  softBlue: "#EEF5FA",
  softGold: "#FFF7E8",
  white: "#FFFFFF",
};

function rect(slide, x, y, w, h, fill = C.white, line = C.line, radius = 8) {
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

function text(slide, value, x, y, w, h, opt = {}) {
  return ctx.addText(slide, {
    text: value,
    left: x,
    top: y,
    width: w,
    height: h,
    fontSize: opt.size ?? 15,
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

function title(slide, main, sub) {
  text(slide, main, 54, 40, 930, 42, { size: 29, bold: true });
  text(slide, sub, 56, 88, 1010, 24, { size: 14.5, color: C.muted });
  rect(slide, 28, 28, W - 56, H - 56, "#00000000", "#E7D9C3", 0);
}

function lane(slide, x, y, w, label, color) {
  rect(slide, x, y, w, 34, color, null, 6);
  text(slide, label, x + 6, y + 7, w - 12, 20, {
    size: 13.5,
    color: C.white,
    bold: true,
    align: "center",
  });
}

function node(slide, x, y, w, h, no, head, body, color, fill = C.soft) {
  rect(slide, x, y, w, h, fill, C.line, 8);
  rect(slide, x, y, w, 6, color, null, 0);
  text(slide, no, x + 12, y + 23, 40, 28, { size: 18, color, bold: true, align: "center" });
  text(slide, head, x + 58, y + 22, w - 70, 25, { size: 17, bold: true });
  text(slide, body, x + 20, y + 62, w - 40, h - 68, { size: 12.6, color: C.muted });
}

function arrow(slide, x, y, w = 38, color = C.brown) {
  text(slide, "→", x, y, w, 28, {
    size: 24,
    color,
    bold: true,
    align: "center",
    valign: "middle",
    insets: { left: 0, right: 0, top: 0, bottom: 0 },
  });
}

function down(slide, x, y, color = C.green) {
  text(slide, "↓", x, y, 32, 32, {
    size: 25,
    color,
    bold: true,
    align: "center",
    valign: "middle",
    insets: { left: 0, right: 0, top: 0, bottom: 0 },
  });
}

function pill(slide, x, y, w, value, color = C.ink) {
  rect(slide, x, y, w, 28, C.white, "#E1C99F", 5);
  text(slide, value, x + 8, y + 5, w - 16, 17, { size: 11.6, color });
}

function note(slide, x, y, w, head, lines, color, fill) {
  rect(slide, x, y, w, 118, fill, C.line, 8);
  text(slide, head, x + 16, y + 14, w - 32, 23, {
    size: 16,
    bold: true,
    color,
    align: "center",
  });
  lines.forEach((line, i) => {
    rect(slide, x + 22, y + 52 + i * 22, 7, 7, color, null, 4);
    text(slide, line, x + 38, y + 45 + i * 22, w - 58, 18, { size: 12.2, color: C.ink });
  });
}

function footer(slide, value) {
  rect(slide, 66, 674, 1148, 28, "#F8F3EA", "#E7D9C3", 4);
  text(slide, value, 84, 681, 1110, 14, { size: 12.3, color: C.ink, align: "center" });
}

function slideOne() {
  const s = presentation.slides.add();
  rect(s, 0, 0, W, H, C.white, null, 0);
  title(
    s,
    "client.buy(...) 到 WebSocket 消息准备完成",
    "这一页只展示 Python 决策层如何把业务动作拆成可发送的前端表现命令，不混入 Unity 执行和回执。"
  );

  lane(s, 66, 132, 244, "Action Handler", C.red);
  lane(s, 370, 132, 244, "ActionLayer Client", C.green);
  lane(s, 674, 132, 244, "表现动作封装", C.blue);
  lane(s, 978, 132, 224, "send(...) 入参", C.gold);

  node(
    s,
    66,
    194,
    244,
    130,
    "1",
    "handle_buy(ctx, act)",
    "先通过 validators：\n在市场、有库存、有足够资金。",
    C.red
  );
  arrow(s, 322, 244);
  node(
    s,
    370,
    194,
    244,
    130,
    "2",
    "计算业务参数",
    "读取 actor / item / qty；\nmarket.price(item_id) 算单价；\ntotal = unit_price * qty。",
    C.red
  );
  arrow(s, 626, 244);
  node(
    s,
    674,
    194,
    244,
    130,
    "3",
    "调用 client.buy",
    "await ctx.world.client.buy(\nactor.id, qty, total, source,\nitem_id)",
    C.green
  );
  arrow(s, 930, 244);
  node(
    s,
    978,
    194,
    224,
    130,
    "4",
    "Server.buy",
    "拆成三段表现动作；\n不改 Python 权威状态。",
    C.green
  );

  down(s, 1080, 334, C.green);
  rect(s, 126, 386, 470, 198, C.softGreen, C.line, 8);
  text(s, "buy(...) 被拆成 3 个顺序表现动作", 164, 406, 394, 24, {
    size: 18,
    bold: true,
    color: C.green,
    align: "center",
  });
  pill(s, 164, 452, 360, "1. show_animation(actor_id, item, +qty)", C.ink);
  pill(s, 164, 492, 360, "2. move(actor_id, source, \"收银台\")", C.ink);
  pill(s, 164, 532, 360, "3. show_animation(actor_id, \"money\", -total)", C.ink);
  text(s, "每一步都 await，前一步失败就影响最终 result。", 164, 562, 360, 18, {
    size: 12.2,
    color: C.muted,
    align: "center",
  });

  arrow(s, 610, 478, 46, C.green);
  note(s, 674, 392, 244, "move(...) 准备", [
    "actor_id → agent_id",
    "type=command, cmd=go_to",
    "target + cur_location",
  ], C.blue, C.softBlue);
  note(s, 958, 392, 244, "show_animation(...) 准备", [
    "actor_id → agent_id",
    "type = animation",
    "target + value",
  ], C.gold, C.softGold);

  rect(s, 346, 612, 588, 38, "#FFF7E8", "#E1C99F", 6);
  text(s, "到这里为止：send(...) 已拿到 type / agent_id / cmd / target / value / cur_location，下一步才生成 WebSocket payload。", 360, 623, 560, 16, {
    size: 13.1,
    color: C.brown,
    bold: true,
    align: "center",
  });

  footer(s, "注意：Python 世界状态的更新发生在 client.buy(...) 成功返回之后，不属于“消息发送前”的流程。");
  return s;
}

function wsNode(slide, x, y, w, h, no, head, body, color) {
  rect(slide, x, y, w, h, C.soft, C.line, 8);
  text(slide, no, x + 14, y + 14, 34, 24, { size: 17, bold: true, color, align: "center" });
  text(slide, head, x + 56, y + 15, w - 72, 22, { size: 16, bold: true });
  text(slide, body, x + 22, y + 48, w - 44, h - 54, { size: 12.4, color: C.muted });
}

function slideTwo() {
  const s = presentation.slides.add();
  rect(s, 0, 0, W, H, C.white, null, 0);
  title(
    s,
    "WebSocket 发送、等待与 complete 回填",
    "这一页单独展示 send(...) 内部机制，以及 Unity complete 如何让 await 的 Python 调用继续执行。"
  );

  lane(s, 76, 132, 226, "连接与消息构造", C.blue);
  lane(s, 376, 132, 226, "挂起等待", C.gold);
  lane(s, 676, 132, 226, "Unity 执行", C.brown);
  lane(s, 976, 132, 226, "回执恢复", C.green);

  wsNode(
    s,
    76,
    194,
    226,
    122,
    "1",
    "查连接",
    "ws = connections[agent_id]\n找不到连接则返回 None。",
    C.blue
  );
  arrow(s, 318, 240);
  wsNode(
    s,
    376,
    194,
    226,
    122,
    "2",
    "生成 payload",
    "action_id = uuid4()\n写入 type / agent_id / target / value / cmd / info。",
    C.blue
  );
  arrow(s, 618, 240);
  wsNode(
    s,
    676,
    194,
    226,
    122,
    "3",
    "挂 Future",
    "fut = create_future()\npending[action_id] = fut。",
    C.gold
  );
  arrow(s, 918, 240);
  wsNode(
    s,
    976,
    194,
    226,
    122,
    "4",
    "发送 JSON",
    "await ws.send(...)\n从这里消息离开 Python。",
    C.gold
  );

  down(s, 1080, 326, C.brown);
  wsNode(
    s,
    976,
    374,
    226,
    120,
    "5",
    "Unity 路由执行",
    "WsAgentClient 根据 type / cmd 分发：\ncommand go_to、animation 等。",
    C.brown
  );
  arrow(s, 918, 420, 38, C.brown);
  wsNode(
    s,
    676,
    374,
    226,
    120,
    "6",
    "发送 complete",
    "动作完成后回传：\ntype = complete\nstatus = ok\naction_id = 同一个 id",
    C.brown
  );
  arrow(s, 618, 420, 38, C.green);
  wsNode(
    s,
    376,
    374,
    226,
    120,
    "7",
    "_handle 收到回执",
    "pending.pop(action_id)\n找到对应 fut 后：\nfut.set_result(msg)。",
    C.green
  );
  arrow(s, 318, 420, 38, C.green);
  wsNode(
    s,
    76,
    374,
    226,
    120,
    "8",
    "await 恢复",
    "wait_for(fut, timeout=20)\n拿到 msg；超时则清理 pending 并返回 None。",
    C.green
  );

  rect(s, 168, 548, 944, 66, "#FFF7E8", "#E1C99F", 7);
  text(s, "返回路径", 194, 564, 96, 22, { size: 17, bold: true, color: C.brown });
  text(
    s,
    "send(...) 返回 msg → move/show_animation 调 is_success(result) → buy(...) 继续下一段表现动作 → 三段都成功后 handle_buy 才更新 Python 权威状态。",
    292,
    563,
    790,
    26,
    { size: 13.5, color: C.ink }
  );

  footer(s, "核心机制：action_id 把一次发送和一次 complete 回执绑定起来，pending Future 让 Python 写法看起来像同步调用。");
  return s;
}

const slideA = slideOne();
const slideB = slideTwo();

const out = path.join(WORKSPACE, "clear_websocket_rpc_flow_editable.pptx");
const previewA = path.join(WORKSPACE, "clear_websocket_rpc_flow_slide1.png");
const previewB = path.join(WORKSPACE, "clear_websocket_rpc_flow_slide2.png");
const pptx = await PresentationFile.exportPptx(presentation);
await pptx.save(out);
await saveBlobToFile(await presentation.export({ slide: slideA, format: "png", scale: 1 }), previewA);
await saveBlobToFile(await presentation.export({ slide: slideB, format: "png", scale: 1 }), previewB);

console.log(JSON.stringify({ output: out, previews: [previewA, previewB] }, null, 2));
