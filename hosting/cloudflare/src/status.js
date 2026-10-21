// Read-only collection status for Testee at /status/<token>. Testee cannot see anything after a monitor
// leaves for geo-q.jp, so this page lists the completers' respondent IDs and the counts per stage.
// The Supabase function research-status checks the token and builds the numbers; this only renders them.
const PATH = /^\/status\/([0-9a-f]{64})(\/data|\/completers\.csv)?$/;

const jst = (value, options) =>
  new Intl.DateTimeFormat("ja-JP", { timeZone: "Asia/Tokyo", ...options }).format(new Date(value));
const csvDate = (value) =>
  jst(value, { year: "numeric", month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit", hourCycle: "h23" });

function headers(type, extra = {}) {
  return {
    "Content-Type": type,
    "Cache-Control": "no-store",
    "X-Robots-Tag": "noindex, nofollow",
    "Referrer-Policy": "no-referrer",
    "X-Content-Type-Options": "nosniff",
    ...extra,
  };
}

async function loadStatus(env, token) {
  const response = await fetch(env.STATUS_API, { method: "POST", headers: { Authorization: `Bearer ${token}` } });
  if (response.status === 403) return { missing: true };
  if (!response.ok) return { failed: true };
  const { ok, ...status } = await response.json();
  return ok ? { status } : { failed: true };
}

function csv(status) {
  const rows = [["回答者ID", "完了日時（日本時間）", "備考"]];
  for (const item of status.completers) {
    const note = item.guardianFormIds.length ? `保護者同意フォームのID: ${item.guardianFormIds.join(" / ")}` : "";
    rows.push([item.respondentId, csvDate(item.completedAt), note]);
  }
  // Excel opens a UTF-8 CSV correctly only with the BOM.
  return "﻿" + rows.map((row) => row.map((cell) => `"${cell.replaceAll('"', '""')}"`).join(",")).join("\r\n") + "\r\n";
}

export async function handleStatus(request, env) {
  const match = new URL(request.url).pathname.match(PATH);
  if (!match) return null;
  if (request.method !== "GET") return new Response(null, { status: 405, headers: headers("text/plain", { Allow: "GET" }) });
  const [, token, part] = match;
  const { status, missing } = await loadStatus(env, token);

  if (part === "/data") {
    if (!status) return new Response(JSON.stringify({ ok: false }), { status: missing ? 404 : 503, headers: headers("application/json") });
    return new Response(JSON.stringify(status), { headers: headers("application/json") });
  }
  if (!status) {
    const message = missing ? "このリンクは無効です。" : "ただいま表示できません。しばらくしてから再読み込みしてください。";
    return new Response(page(null, message), { status: missing ? 404 : 503, headers: headers("text/html; charset=utf-8", security()) });
  }
  if (part === "/completers.csv") {
    const day = jst(status.generatedAt, { year: "numeric", month: "2-digit", day: "2-digit" }).replaceAll("/", "");
    return new Response(csv(status), {
      headers: headers("text/csv; charset=utf-8", { "Content-Disposition": `attachment; filename="completers-${day}.csv"` }),
    });
  }
  const nonce = crypto.randomUUID().replaceAll("-", "");
  return new Response(page(status, null, nonce), { headers: headers("text/html; charset=utf-8", security(nonce)) });
}

function security(nonce) {
  const script = nonce ? `'nonce-${nonce}'` : "'none'";
  return {
    "Content-Security-Policy": `default-src 'none'; script-src ${script}; style-src 'unsafe-inline'; connect-src 'self'; ` +
      "base-uri 'none'; form-action 'none'; frame-ancestors 'none'",
  };
}

function page(status, message, nonce) {
  // The first view is rendered by the same script that refreshes it; the data rides along as JSON.
  const initial = status ? JSON.stringify(status).replaceAll("<", "\\u003c") : "null";
  return `<!doctype html>
<html lang="ja">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="robots" content="noindex, nofollow">
<meta name="referrer" content="no-referrer">
<title>回収状況</title>
<style>
:root {
  color-scheme: light;
  --bg: #f6f5f1; --surface: #ffffff; --line: #e3e0d8; --ink: #22231f; --muted: #6b6c64;
  --accent: #2f6b55; --accent-soft: #e3efe8; --track: #e8e6df; --warn: #9a5b12; --warn-soft: #fbf0df;
}
@media (prefers-color-scheme: dark) {
  :root:not([data-theme="light"]) {
    color-scheme: dark;
    --bg: #161714; --surface: #1f201c; --line: #34352f; --ink: #ecebe5; --muted: #a3a49b;
    --accent: #7cc4a4; --accent-soft: #23362d; --track: #2c2d28; --warn: #e3b26a; --warn-soft: #3a2f1e;
  }
}
:root[data-theme="dark"] {
  color-scheme: dark;
  --bg: #161714; --surface: #1f201c; --line: #34352f; --ink: #ecebe5; --muted: #a3a49b;
  --accent: #7cc4a4; --accent-soft: #23362d; --track: #2c2d28; --warn: #e3b26a; --warn-soft: #3a2f1e;
}
* { box-sizing: border-box; }
body {
  margin: 0; background: var(--bg); color: var(--ink);
  font: 15px/1.7 "Hiragino Sans", "Hiragino Kaku Gothic ProN", "Noto Sans JP", "Yu Gothic UI", system-ui, sans-serif;
  -webkit-font-smoothing: antialiased;
}
main { max-width: 760px; margin: 0 auto; padding: 32px 16px 56px; }
header { display: flex; flex-wrap: wrap; align-items: flex-end; justify-content: space-between; gap: 12px 24px; margin-bottom: 20px; }
.eyebrow { margin: 0; color: var(--muted); font-size: 13px; letter-spacing: .08em; }
h1 { margin: 2px 0 0; font-size: 22px; line-height: 1.4; font-weight: 700; }
.meta { margin: 0; color: var(--muted); font-size: 13px; text-align: right; }
.meta button { margin-left: 8px; }
.card { background: var(--surface); border: 1px solid var(--line); border-radius: 14px; padding: 20px; margin-top: 16px; }
.hero .label { margin: 0; color: var(--muted); font-size: 14px; }
.hero .count { margin: 2px 0 12px; font-size: 44px; line-height: 1.1; font-weight: 700; font-variant-numeric: tabular-nums; }
.hero .count small { font-size: 18px; font-weight: 500; color: var(--muted); margin-left: 6px; }
.bar { height: 10px; border-radius: 999px; background: var(--track); overflow: hidden; }
.bar span { display: block; height: 100%; width: 0; background: var(--accent); border-radius: inherit; transition: width .4s ease; }
.hero .note { margin: 10px 0 0; font-size: 14px; color: var(--muted); }
.done { color: var(--accent) !important; font-weight: 600; }
.stages { display: grid; grid-template-columns: repeat(3, 1fr); gap: 12px; margin-top: 16px; }
.stage { background: var(--surface); border: 1px solid var(--line); border-radius: 12px; padding: 14px 16px; }
.stage b { display: block; font-size: 26px; line-height: 1.2; font-variant-numeric: tabular-nums; }
.stage span { font-size: 13px; color: var(--muted); }
h2 { margin: 0 0 4px; font-size: 16px; }
.sub { margin: 0 0 14px; color: var(--muted); font-size: 13px; }
.row { display: flex; flex-wrap: wrap; align-items: center; justify-content: space-between; gap: 8px 16px; }
.actions { display: flex; gap: 8px; flex-wrap: wrap; }
button, .button {
  appearance: none; border: 1px solid var(--line); background: var(--surface); color: var(--ink);
  font: inherit; font-size: 13px; padding: 6px 12px; border-radius: 8px; cursor: pointer; text-decoration: none; white-space: nowrap;
}
button:hover, .button:hover { border-color: var(--accent); }
.button.primary { background: var(--accent); border-color: var(--accent); color: var(--surface); }
table { width: 100%; border-collapse: collapse; font-variant-numeric: tabular-nums; }
th, td { text-align: left; padding: 9px 8px; border-top: 1px solid var(--line); vertical-align: top; }
th { font-size: 12px; font-weight: 600; color: var(--muted); border-top: 0; }
td.num { color: var(--muted); width: 2.5em; }
td.id { font-family: ui-monospace, "SF Mono", Menlo, monospace; font-size: 15px; letter-spacing: .02em; }
td.when { white-space: nowrap; }
td.note { font-size: 13px; color: var(--warn); }
.empty { color: var(--muted); text-align: center; padding: 24px 8px; }
.review { border-color: var(--warn); background: var(--warn-soft); }
.review h2 { color: var(--warn); }
.review ul { margin: 0; padding-left: 1.2em; }
.review li + li { margin-top: 8px; }
.mono { font-family: ui-monospace, "SF Mono", Menlo, monospace; }
.alert { margin-top: 16px; padding: 10px 14px; border-radius: 10px; background: var(--warn-soft); color: var(--warn); font-size: 14px; }
footer { margin-top: 28px; color: var(--muted); font-size: 12.5px; }
footer ul { margin: 0; padding-left: 1.2em; }
@media (max-width: 560px) {
  .meta { text-align: left; }
  .hero .count { font-size: 38px; }
  .stages { gap: 8px; }
  .stage { padding: 12px; }
  .stage b { font-size: 22px; }
  .stage span { font-size: 12px; }
  td.when { white-space: normal; }
}
</style>
</head>
<body>
<main>
${message ? `<header><div><p class="eyebrow">回収状況</p><h1>${message}</h1></div></header>` : `
<header>
  <div><p class="eyebrow">回収状況</p><h1 id="label"></h1></div>
  <p class="meta"><span id="updated"></span><button type="button" id="refresh">更新</button><br><span id="since"></span></p>
</header>
<div id="alert" class="alert" hidden></div>
<section class="card hero">
  <p class="label">完了者数</p>
  <p class="count"><span id="completed"></span><small id="target"></small></p>
  <div class="bar" role="progressbar" aria-label="目標に対する完了者数" aria-valuemin="0" id="progress"><span></span></div>
  <p class="note" id="remaining"></p>
</section>
<div class="stages">
  <div class="stage"><b id="consented"></b><span>保護者同意</span></div>
  <div class="stage"><b id="cleared"></b><span>ゲームクリア</span></div>
  <div class="stage"><b id="surveyed"></b><span>アンケート回答</span></div>
</div>
<section class="card review" id="review" hidden>
  <h2>要確認（完了者数に含めていません）</h2>
  <p class="sub">同じ端末から2回以上回答があり、異なるIDが入力されました。こちらで確認のうえご連絡いたします。</p>
  <ul id="review-list"></ul>
</section>
<section class="card">
  <div class="row">
    <div><h2>完了者ID一覧</h2><p class="sub">完了日時の早い順。同じIDは1名として数えています。</p></div>
    <div class="actions"><button type="button" id="copy">IDをコピー</button><a class="button primary" id="csv" href="" download>CSVをダウンロード</a></div>
  </div>
  <table>
    <thead><tr><th>#</th><th>回答者ID</th><th>完了日時</th><th>備考</th></tr></thead>
    <tbody id="rows"></tbody>
  </table>
</section>
<footer>
  <ul>
    <li>保護者同意：保護者同意フォームを送信した人数です。</li>
    <li>ゲームクリア：ゲームを最後まで進め、アンケートを開ける状態になった人数です。</li>
    <li>完了：ゲームクリア後にアンケートを送信した人です。IDはアンケートで再入力されたものを使います。保護者同意フォームと異なる場合は備考に記載しています。</li>
    <li>このページは1分ごとに自動で更新されます。保護者の氏名などの個人情報は表示していません。</li>
    <li>URLを知っている方は誰でも閲覧できます。関係者以外には共有しないでください。</li>
  </ul>
</footer>
<script type="application/json" id="initial">${initial}</script>
<script nonce="${nonce}">
(() => {
  const $ = (id) => document.getElementById(id);
  const base = location.pathname.replace(/\\/$/, "");
  const format = (value, options) => new Intl.DateTimeFormat("ja-JP", { timeZone: "Asia/Tokyo", ...options }).format(new Date(value));
  const when = (value) => format(value, { month: "numeric", day: "numeric", weekday: "short", hour: "2-digit", minute: "2-digit", hourCycle: "h23" });
  const cell = (text, className) => { const td = document.createElement("td"); td.textContent = text; if (className) td.className = className; return td; };
  let ids = [];

  function render(data) {
    document.title = "回収状況 – " + data.label;
    $("label").textContent = data.label;
    $("since").textContent = "集計開始 " + format(data.countedFrom, { year: "numeric", month: "numeric", day: "numeric", hour: "2-digit", minute: "2-digit", hourCycle: "h23" });
    $("updated").textContent = "最終更新 " + format(data.generatedAt, { hour: "2-digit", minute: "2-digit", second: "2-digit", hourCycle: "h23" });
    $("completed").textContent = data.completed;
    $("target").textContent = "/ 目標 " + data.target + " 名";
    const progress = $("progress");
    progress.setAttribute("aria-valuemax", data.target);
    progress.setAttribute("aria-valuenow", data.completed);
    progress.firstElementChild.style.width = Math.min(100, data.completed / data.target * 100) + "%";
    const left = data.target - data.completed;
    $("remaining").textContent = left > 0 ? "目標まであと " + left + " 名" : "目標数に達しました";
    $("remaining").classList.toggle("done", left <= 0);
    $("consented").textContent = data.consented;
    $("cleared").textContent = data.gameCleared;
    $("surveyed").textContent = data.surveyed;

    const rows = $("rows");
    rows.replaceChildren();
    data.completers.forEach((item, index) => {
      const tr = document.createElement("tr");
      const note = item.guardianFormIds.length ? "保護者同意フォームのID: " + item.guardianFormIds.join(" / ") : "";
      tr.append(cell(String(index + 1), "num"), cell(item.respondentId, "id"), cell(when(item.completedAt), "when"), cell(note, "note"));
      rows.append(tr);
    });
    if (!data.completers.length) {
      const tr = document.createElement("tr");
      const td = cell("まだ完了者はいません。", "empty"); td.colSpan = 4; tr.append(td); rows.append(tr);
    }
    ids = data.completers.map((item) => item.respondentId);
    $("copy").disabled = !ids.length;

    const list = $("review-list");
    list.replaceChildren();
    for (const item of data.review) {
      const li = document.createElement("li");
      li.append("保護者同意フォームのID ");
      const form = document.createElement("span"); form.className = "mono"; form.textContent = item.guardianFormId; li.append(form);
      li.append("／アンケートで入力: " + item.entries.map((entry) => entry.respondentId + "（" + when(entry.submittedAt) + "）").join("、"));
      list.append(li);
    }
    $("review").hidden = !data.review.length;
  }

  async function refresh() {
    try {
      const response = await fetch(base + "/data", { cache: "no-store", credentials: "omit" });
      if (response.status === 404) { location.reload(); return; }
      if (!response.ok) throw new Error(String(response.status));
      render(await response.json());
      $("alert").hidden = true;
    } catch (_) {
      $("alert").textContent = "更新できませんでした（" + format(Date.now(), { hour: "2-digit", minute: "2-digit", hourCycle: "h23" }) + "）。表示は前回の内容のままです。";
      $("alert").hidden = false;
    }
  }

  $("csv").href = base + "/completers.csv";
  $("refresh").addEventListener("click", refresh);
  $("copy").addEventListener("click", async () => {
    try { await navigator.clipboard.writeText(ids.join("\\n")); $("copy").textContent = "コピーしました"; }
    catch (_) { $("copy").textContent = "コピーできませんでした"; }
    setTimeout(() => { $("copy").textContent = "IDをコピー"; }, 2000);
  });
  setInterval(() => { if (!document.hidden) refresh(); }, 60000);
  document.addEventListener("visibilitychange", () => { if (!document.hidden) refresh(); });
  render(JSON.parse($("initial").textContent));
})();
</script>`}
</main>
</body>
</html>`;
}
