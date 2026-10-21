# テスティー用回收状况页 — 2026-10-21

## 背景

2026-10-21 17:13 テスティーの小川さん来信：10/22 10:00 前后开始配信（全国／12〜14岁／男女，目标完了 20 名）。monitor 跳到 geo-q.jp 以后，テスティー那边看不到回收情况，所以请我们每次都告知完了者ID和回收状况。请款只按两边ID对得上的完了者计算。

用户对稲垣先生说的方案是：做一个实时更新的网页，做好后分享给テスティー。本报告是这个页面，目前只在本地做完、验证完，**还没有部署**。

## 页面

`https://geo-q.jp/status/<64位token>`，只读，1 分钟自动刷新。

- 完了者数 / 目标（进度条）
- 各阶段人数：保護者同意 → ゲームクリア（发了问卷票，即通关）→ アンケート回答
- 完了者ID一覧：回答者ID、完了日時（日本时间）、备注（问卷里再次输入的ID和保护者同意书上的不同时，写出同意书上的ID）
- 「IDをコピー」（每行一个ID）、「CSVをダウンロード」（UTF-8 BOM，Excel 可直接打开）
- 要確認：同一参与者（同一浏览器）在问卷中输入了两个不同的ID时列在这里，不计入完了数，由我们确认后再联系
- 不显示保护者姓名、参与者 UUID 等其他信息

截图（本地演示数据，ID 都是假的）：`screenshots/01-desktop.png`、`02-iphone.png`、`03-desktop-dark.png`、`04-invalid-link.png`。

## 完了的计算方式

和 `tools/research-admin.py completers` 一致：

- 只算 `counted_from` 之后提交的保护者同意书（预定 2026-10-22 00:00 JST，以排除之前我们和老师的测试）。
- 每个参与者报一个ID：优先用问卷里再次输入的ID（テスティー 2026-10-02 的规格），双重确认之前的回答用同意书上的ID。
- 输入过两个不同ID的参与者进「要確認」，不计数。
- 两个参与者是同一个ID（同一个孩子换了浏览器）只算 1 名。

## 结构

| 部分 | 文件 | 作用 |
| --- | --- | --- |
| 数据库 | `supabase/migrations/20261021120000_research_status_page.sql` | `research_status_links`（只存 token 的 SHA-256、study、`counted_from`、目标数、作废时间）和 `research_status(p_token_hash)`，只有 service_role 能执行 |
| Edge Function | `supabase/functions/research-status/index.ts` | 用 Bearer token 查询；每个链接每分钟最多 30 次；service 密钥不离开 Supabase |
| Worker | `hosting/cloudflare/src/status.js`（`worker.js` 中调用） | 页面、`/data`（JSON）、`/completers.csv`；`no-store`、`noindex`、`no-referrer`、带 nonce 的 CSP |
| 管理工具 | `tools/research-admin.py status-link` / `revoke-status-link` | 发行链接（URL 只显示一次）／作废 |

## 验证（本地）

- 数据库测试 `supabase/tests/research_status_test.sql`：统计起点之前的记录、其他 study、ID 不一致、双重确认之前的回答、两个不同ID进入要確認、同一ID去重、只通关未回答、作废/未知链接（42501）、anon/authenticated 无权限、返回内容里没有姓名和参与者 UUID。本地隔离栈（geomodel-remediation）上全部通过。
- Edge Function：未知 token 返回 403，有效 token 返回统计结果。
- Worker（`wrangler dev`）：页面/JSON/CSV 为 200，格式错误或无效 token 为 404，POST 为 405，原有游戏路径不受影响。
- 浏览器：点「更新」后新完成者出现在表里，计数从 6 变为 7；手机 390px 宽没有横向滚动；深色模式；控制台无错误。

## 上线步骤（尚未执行，需用户确认）

1. `supabase db push --project-ref jrqmoxbzbjxrellvsvwn --skip-vault --dry-run`，确认只多出 `20261021120000` 一个迁移后再正式执行。
2. `supabase functions deploy research-status --project-ref jrqmoxbzbjxrellvsvwn --use-api`
3. Worker：`.stage` 已经是当前版本 `2026.10.20-consent-keyboard`，不用重新上传 R2：`wrangler deploy --config hosting/cloudflare/wrangler.jsonc --var RELEASE:2026.10.20-consent-keyboard`
4. 发行链接：`python3 tools/research-admin.py status-link --study-key geoquest-open-play-2026-09 --label 'テスティー様 中学生モニター調査' --counted-from 2026-10-22T00:00+09:00`
5. 打开生产页面确认（10/22 之前应显示 0 名），再把 URL 写进给小川さん的回信。
