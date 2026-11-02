# 达到目标人数后显示「募集終了」— 2026-11-02

## 背景

2026-11-02 Testee 小川さん来信问：达到目标回收数后，参加者跳转到 geo-q.jp 时，是否有"不能再玩游戏、不能再答问卷"之类的限制。

核对代码后确认，之前没有这种限制：达到目标时只会给研究者发一封邮件（`hosting/cloudflare/src/notify.js`）。用户决定**只在入口拦截**：

- 达标后新打开页面的人，看到的不是保护者同意书，而是「募集終了」画面，进不了游戏。
- 服务器端的参加登记（New Game 时）**不拦截**。达标时已经在玩的人，以及达标前就已打开同意页的人，都能照常玩完、答完问卷。因此可能超额 1～2 人，但不会出现"白玩一局却答不了问卷"的情况。
- 不改 Unity，不需要重新 build。

## 画面

| 桌面 | iPhone |
| --- | --- |
| ![](screenshots/01-closed-desktop.png) | ![](screenshots/02-closed-iphone.png) |

文字：

> **モニター調査の募集は終了しました**
> 目標人数に達したため、募集を終了いたしました。ご協力ありがとうございました。
> このページを閉じてください。

画面上没有按钮，也没有进入游戏的入口。

## 判断规则

迁移 `supabase/migrations/20261102120000_research_entry_status.sql`：

- `research_status_links.closes_entry`（新列，默认 false）。迁移里把 Testee 的正式链接（`d006abf0…`，目标 20）设为 true。
- `research_entry_status()` 返回 `{"open": true|false}`，只返回这一个值。满足以下任一条件时为 `false`：
  - 没有正在接受报名的 open-play study（`research_entry_enabled` 关闭、study 不是 active，或已过保存期限）。
  - 某个 `closes_entry` 且未作废的链接，其完成数达到 `target_count`。完成数和 Testee 状态页面上的口径完全一致（复用 `research_status()`），要確認的人不计入。
- 函数是 `security definer`，允许 anon 调用；人数和 ID 仍然只有 service_role 能读。

## 页面行为

`Assets/WebGLTemplates/FixedAspect/index.html`、`TemplateData/guardian-consent.js`、`TemplateData/style.css`：

- 新访问者：先显示「読み込んでいます…」，用 publishable key 调用 `POST /rest/v1/rpc/research_entry_status`，等结果出来后再显示同意书。
- 服务器明确返回 `open: false` 时才显示「募集終了」。网络错误、HTTP 错误、返回内容异常、5 秒超时，都照常显示同意书。网络不好时不会误挡还在募集期间的人。
- 本标签页里已经同意过的人（`sessionStorage` 有记录，比如刷新、闪退恢复）：不做检查，直接进入学生同意页。
- 「募集終了」时页面保持 pending，Unity 不会开始游戏。

## 验证（本地）

- 数据库：新测试 `supabase/tests/research_entry_status_test.sql` 通过（没有 closes_entry 链接时开放；未标记的链接不关闭；未达标时开放，要確認不计入；达标后关闭，只返回 `open`；作废的链接不再关闭；study 停止报名时关闭；anon 能调用这个函数，但不能调用 `research_status`）。`research_status_test`、`guardian_consent_test`、`open_play_test` 照常通过。
- HTTP：在本地栈上以匿名身份调用 RPC 返回 200，`Access-Control-Allow-Origin: *`。
- Node：`tools/qa/guardian-consent.test.cjs` 14/14 通过，新增 4 项（新访问者先检查、已同意者不检查；只有 `open:false` 才显示募集終了；各种错误照常显示同意书；不响应时 5 秒后显示同意书；页面上的画面和配置）。`tools/qa/*.test.cjs` 共 30/30 通过。
- 浏览器（本地页面接本地 Supabase）：开放时显示同意书；建一个 `closes_entry` 链接、目标设为本地已有的完成数后，刷新即显示「募集終了」；已同意的人刷新后直接进入学生同意页，也不发检查请求；控制台无错误。截图用 Playwright 生成（桌面 1280×800、iPhone 13），测试链接用完已删除。

## 上线步骤（未执行，需用户确认）

1. 数据库：`supabase db push --project-ref jrqmoxbzbjxrellvsvwn --skip-vault --dry-run`，确认只多出 `20261102120000` 后正式执行。之后用 service key 确认正式链接的 `closes_entry` 已变为 true，`research_entry_status()` 返回 `open: true`（当前 7/20）。
2. 页面：按 shell-only 方式发布（只改模板，不 build Unity）。**注意**：`Build/WebGL/Build/*` 现在是还没发布的 first-trip-coach 版本，不能直接上传。要以 HEAD 中已提交的已发布文件（`2026.10.16-hammer-slab-fix` 二进制，`release.json` 版本 `2026.10.20-consent-keyboard`）为基础，套用本次对 `index.html`、`TemplateData/*` 的修改，新版本号例如 `2026.11.02-entry-closed`，再走 itch.io 和 geo-q.jp 的发布流程。
3. 生产确认：页面正常显示同意书；向 RPC 发请求返回 `{"open": true}`。
