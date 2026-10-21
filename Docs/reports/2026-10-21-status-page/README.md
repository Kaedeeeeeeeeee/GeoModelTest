# テスティー用回收状况页 — 2026-10-21

## 背景

2026-10-21 17:13 テスティーの小川さん来信：10/22 10:00 前后开始配信（全国／12〜14岁／男女，目标完了 20 名）。monitor 跳到 geo-q.jp 以后，テスティー那边看不到回收情况，所以请我们每次都告知完了者ID和回收状况。请款只按两边ID对得上的完了者计算。

用户对稲垣先生说的方案是：做一个实时更新的网页，做好后分享给テスティー。本报告是这个页面。2026-10-21 晚在本地验证后，经用户同意部署上线（见文末「部署记录」）。

## 页面

`https://geo-q.jp/status/<64位token>`，只读，1 分钟自动刷新。

- 完了者数 / 目标（进度条）
- 各阶段人数：保護者同意 → ゲームクリア（发了问卷票，即通关）→ アンケート回答
- 完了者ID一覧：回答者ID、完了日時（日本时间）
- 「IDをコピー」（每行一个ID）、「CSVをダウンロード」（UTF-8 BOM，Excel 可直接打开）
- 要確認：问卷里的 ID 和保护者同意书不一致的人，分「只答一次但 ID 不同」和「多次回答、ID 不同」两组，两边的 ID 都列出来，不计入完了数
- 不显示保护者姓名、参与者 UUID 等其他信息

截图（本地演示数据，ID 都是假的）：`screenshots/01-desktop.png`、`02-iphone.png`、`03-desktop-dark.png`、`04-invalid-link.png`。

## 完了的计算方式

最初和 `tools/research-admin.py completers` 一致：两边 ID 不同时，以问卷里再次输入的 ID 为准（テスティー 2026-10-02 的规格）。2026-10-21 用户决定改掉这一点：我们没办法判断家长填的和孩子填的哪个对，所以两边都显示，等确认后再计数（迁移 `20261021130000_research_status_id_mismatch.sql`）。现在的规则是：

- 只统计 `counted_from` 之后提交的保护者同意书（2026-10-22 00:00 JST），以排除之前我们和老师的测试。
- **完了**：问卷里输入的 ID 和保护者同意书上的一致的参与者。双重确认之前的回答没有再次输入的 ID，用同意书上的 ID。
- **要確認**：问卷里输入的 ID 只要有一次和同意书上的不同，就进这里。同意书上的 ID 和问卷里的每个 ID 都显示，不计入完了数、ID 列表和 CSV。框内分两组（数据库返回 `kind`），各有小标题、人数和说明（`screenshots/07-review-groups.png`）：
  - `guardianMismatch`「保護者同意フォームとアンケートでIDが異なる」：问卷只交了一次，ID 和同意书不同。
  - `multipleAnswers`「複数回のアンケートで異なるIDが入力された」：同一浏览器交了两次以上，每次 ID 不同（可能是兄弟姐妹共用设备）。
- 两个参与者是同一个 ID（同一个孩子换了浏览器）只算 1 名。
- `research-admin.py completers` 仍按 10/2 的规格选 ID，并逐条打印不一致的情况。今后向 Testee 汇报以页面为准。

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

## 上线步骤

1. `supabase db push --project-ref jrqmoxbzbjxrellvsvwn --skip-vault --dry-run`，确认只多出 `20261021120000` 一个迁移后再正式执行。
2. `supabase functions deploy research-status --project-ref jrqmoxbzbjxrellvsvwn --use-api`
3. Worker：`.stage` 已经是当前版本 `2026.10.20-consent-keyboard`，不用重新上传 R2：`wrangler deploy --config hosting/cloudflare/wrangler.jsonc --var RELEASE:2026.10.20-consent-keyboard`
4. 发行链接：`python3 tools/research-admin.py status-link --study-key geoquest-open-play-2026-09 --label 'テスティー様 中学生モニター調査' --counted-from 2026-10-22T00:00+09:00`
5. 打开生产页面确认（10/22 之前应显示 0 名），再把 URL 写进给小川さん的回信。

## 部署记录（2026-10-21，用户同意）

- `db push --dry-run`：只有 `20261021120000_research_status_page.sql`，无 seed、无角色变更。随后正式应用。
- Edge Function `research-status` 已部署；未知 token 由函数本身返回 403（网关未拦截，`verify_jwt = false` 生效）。
- Worker：从 313768c7 更新为 **d0def1e5-bd0e-4610-b80d-2c1b2caf7b74**。静态资源无变化（未上传），`RELEASE` 仍为 `2026.10.20-consent-keyboard`。`/`、`release.json`、`.unityweb` 均 200；无效链接 404。回滚：`wrangler rollback --config hosting/cloudflare/wrangler.jsonc`。
- 链接 ID `d006abf0-ec3e-4060-ad2c-da9a70c867af`，从 2026-10-22 00:00 JST 开始计数，目标 20。URL 只保存在研究者私有文件 `~/.config/geomodel-research/testee-status-link.txt`（权限 600），不进 Git。
- 生产页面确认：各阶段 0，完了 0，「まだ完了者はいません。」，CSV 200，控制台无错误（`screenshots/05-production-empty.png`、`06-production-empty-iphone.png`）。

## 第二次部署（2026-10-21，ID 不一致一律进要確認＋分两组，用户同意）

- `db push --dry-run`：只有 `20261021130000_research_status_id_mismatch.sql`，随后正式应用。
- Worker：d0def1e5 → **492cfa68-a80a-4fd1-a6b3-a869085e0eb2**，静态资源无变化，`RELEASE` 仍为 `2026.10.20-consent-keyboard`。
- 生产确认：完了 0、各阶段 0；HTML 中已有两组（`review-guardianMismatch`、`review-multipleAnswers`），CSV 已无「備考」栏；游戏路径 200；控制台无错误。生产目前没有数据，两组的实际显示以本地截图为准。
- Gmail 回信草稿中「要確認」的说明已改为新规则，未发送。

## 达到目标时自动发邮件（2026-10-21，本地完成，未部署）

用户希望达到目标 20 名时收到一封邮件，以便马上请テスティー停止配信。

- 迁移 `20261021140000_research_status_target_notice.sql`：`research_status_links.target_notified_at`，`claim_research_status_target()`（达到目标且未通知时原子地记下时间，返回状况＋`notify`）、`release_research_status_target()`（发信失败时撤销）。只有 service_role 可执行。
- `research-status` 函数：请求体 `{"action":"claim"}` / `{"action":"release"}`，不带请求体仍是读取。非法动作和非 JSON 返回 400。
- Worker：`src/notify.js` + `scheduled()`，Cron `*/5 * * * *`。发件 `status@geo-q.jp`，收件 f.shera.09@gmail.com（`send_email` 绑定的 `destination_address` 也限定为这个地址）。
- 邮件内容：完了数／目标、各阶段人数、要確認人数、确认时间（日本时间）、页面链接、下一步（`screenshots/08-target-notice-email.png`，本地测试链接）。

### 验证（本地）

- 数据库测试：未达目标不占位；达到目标占位一次、返回状况；再次 claim 不再占位；release 后可重新占位；作废链接 42501；anon 无权限。
- 本地函数：read/claim/release 为 200，`toString` 等非法动作和非 JSON 为 400。
- `wrangler dev --test-scheduled`，用目标 6 名的本地测试链接触发：
  - 第 1 次：发信（本地模拟器截获，收件人 f.shera.09@gmail.com，标题「【回收状况】已达到目标 6 名：…」）。
  - 第 2 次：不再发信。
  - 收件人改为名单外地址时：绑定拒绝（`email to … not allowed`），占位被撤销，下一轮会重试。

### 上线步骤（尚未执行，需用户确认）

1. geo-q.jp 启用 Email Routing（Cloudflare 控制台 → Email → Email Routing。会在 geo-q.jp 根域加 MX 和 SPF 的 TXT 记录；geo-q.jp 目前不收发邮件，没有影响）。
2. 在 Email Routing 的 Destination addresses 加 f.shera.09@gmail.com，用户点 Cloudflare 发来的验证邮件。
3. `supabase db push --dry-run`（只应有 `20261021140000`）→ 正式应用；`supabase functions deploy research-status`。
4. `wrangler secret put STATUS_NOTIFY_TOKEN`（填生产链接的 token，来自 `~/.config/geomodel-research/testee-status-link.txt`）。
5. `wrangler deploy --config hosting/cloudflare/wrangler.jsonc --var RELEASE:2026.10.20-consent-keyboard`，确认 Cron 已注册，Worker 日志里定时任务无错误。
6. （可选）真实发信测试：临时发一个目标 1 名、统计起点较早的测试链接，把 secret 暂时换成它的 token，收到一封邮件后换回正式 token，并作废测试链接。
