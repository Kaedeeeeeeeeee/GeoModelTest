# 自有域名托管（Cloudflare Workers + R2）

游戏在 itch.io 之外，再放一份在自己的域名 `geo-q.jp` 下。itch.io 不撤，作为备用。

- 正式地址：https://geo-q.jp
- 测试地址：https://geo-q.podnote-api.workers.dev

## 结构

| 内容 | 放在哪里 | 原因 |
| --- | --- | --- |
| `index.html`、`TemplateData/`、`WebGL.loader.js`、`StreamingAssets/`、`release.json` | Workers 静态资源 | 普通静态文件 |
| `Build/WebGL.{data,framework.js,wasm}.unityweb` | R2 桶 `geo-q-builds` 的 `releases/<版本>/Build/` | data 有 108 MB，超过静态资源单文件 25 MiB 的上限；三个文件都是 gzip，需要返回 `Content-Encoding: gzip` 让浏览器原生解压（itch 也是这样返回的，JS 解压在 iPhone X 上太吃内存） |

`src/worker.js` 处理这三个 `.unityweb`，另外还有テスティー用的回收状况页 `/status/<token>`（`src/status.js`，见下文）。`.unityweb` 的处理：返回 `ETag`/`Last-Modified` + `Cache-Control: no-cache`，Unity 的数据缓存（Data Caching）第二次打开时会走 304，不会重新下载。所有响应都带 `X-Robots-Tag: noindex`，不被搜索引擎收录。

后端（Supabase edge functions）的 CORS 是 `*`，换域名不需要改。玩家的存档在浏览器里按域名分开存，所以 itch 上的进度不会带到新域名上。

## 发布

Unity 构建出 `Build/WebGL`（含 `release.json`）以后：

```sh
hosting/cloudflare/deploy.sh
```

脚本会先按 `release.json` 核对每个文件的大小和 sha256，再把三个 `.unityweb` 上传到 R2 的 `releases/<版本>/`，最后部署 Worker，并把 `RELEASE` 切到这个版本。itch.io 那边照旧用 `butler push`。

回滚：`wrangler rollback --config hosting/cloudflare/wrangler.jsonc`。Worker 的旧版本里带着当时的静态资源和 `RELEASE`，R2 上的旧版本文件也不会删除。每个版本在 R2 上约占 120 MB，免费额度是 10 GB，偶尔需要清理一下旧版本。

## 域名 geo-q.jp

- 2026-10-06 在 XServerドメイン 购买，设为自动续费，到期日 2027/10/31。第一年 0 円，之后 3,102 円/年。
- XServer 的 nameserver 设定已改为 `mckenzie.ns.cloudflare.com` 和 `randall.ns.cloudflare.com`。DNS 在 Cloudflare 上（Free 计划）。
- `.jp` 的 whois 一律公开注册人姓名，联系信息显示的是 XServer。
- `wrangler.jsonc` 里的 `routes` 把 `geo-q.jp` 绑为 Worker 的 Custom Domain，DNS 记录和证书由 Cloudflare 自动管理。`www.geo-q.jp` 没有绑定。
- 上课前用 iPhone、iPad 和 PC 各打开一次，最好也在学校的网络里试一次（有些过滤服务会拦截新注册的域名）。确认没问题后，可以把 `workers_dev` 改为 `false`，关掉测试地址。

## 回收状况页 `/status/<token>`（2026-10-21）

给テスティー看的只读页面：完了者的回答者ID、各阶段人数（保护者同意 → 游戏通关 → 问卷回答），1 分钟自动刷新，可下载 CSV（`/status/<token>/completers.csv`）。数据来自 Supabase 函数 `research-status`（地址是 `wrangler.jsonc` 里的 `STATUS_API`）。token 的校验和统计都在 Supabase 那边做，Worker 只负责显示，不持有任何 Supabase 密钥。

- 发行链接：`python3 tools/research-admin.py status-link --study-key geoquest-open-play-2026-09 --label 'テスティー様 中学生モニター調査' --counted-from 2026-10-22T00:00+09:00`。URL 只在这时显示一次，数据库里只存 token 的 SHA-256。
- 作废：`python3 tools/research-admin.py revoke-status-link --link-id <ID>`，之后该页面返回 404。
- 本地预览：`.claude/launch.json` 的 `geo-q-status-local`（`wrangler dev`，`STATUS_API` 指向本地 Supabase 栈 `127.0.0.1:55321`）。
- 只改 Worker 代码时不必重新上传 R2：在 `.stage` 是当前版本的前提下，`wrangler deploy --config hosting/cloudflare/wrangler.jsonc --var RELEASE:<当前版本>`。

### 达到目标时的通知邮件

- Worker 的 Cron Trigger 每 5 分钟运行一次（`src/notify.js`）。用 secret `STATUS_NOTIFY_TOKEN`（回收状况链接的 token）请 Supabase 函数 `research-status` 执行 `claim`。链接 token 是给テスティー的，所以 `claim`/`release` 还要带 `X-Notify-Secret`（Worker 和 Supabase 两边都设了 secret `STATUS_NOTIFY_SECRET`）；只有链接 token 时只能读取。完了数达到目标、且还没发过时，数据库给出一个 10 分钟有效的占位（`claimId`），Worker 从 `status@geo-q.jp` 发一封邮件给 `NOTIFY_TO`，成功后执行 `confirm`，这时才记下 `target_notified_at`。发信失败时执行 `release` 立即退回占位；响应丢失、Worker 中断或撤销失败时，占位 10 分钟后过期，下一轮重试。所以不会漏发；只有「发信成功但确认失败」时可能重复一封。
- 发信用 Email Routing 的 `send_email` 绑定 `NOTIFY_EMAIL`，`destination_address` 限定为 f.shera.09@gmail.com。这样不需要 Workers Paid，但 geo-q.jp 要启用 Email Routing，收件地址要先验证。
- 本地测试：`.claude/launch.json` 的 `geo-q-notify-local`（`--test-scheduled`），访问 `/__scheduled?cron=*/5+*+*+*+*` 触发；本地不会真的发信，邮件写到 miniflare 的临时文件里。
- 想重新发：`update research_status_links set target_notified_at = null, target_notice_claim = null, target_notice_claimed_at = null where id = ...`。

