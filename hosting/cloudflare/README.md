# 自有域名托管（Cloudflare Workers + R2）

游戏在 itch.io 之外，再放一份在自己的域名 `geo-q.jp` 下。itch.io 不撤，作为备用。

- 正式地址：https://geo-q.jp
- 测试地址：https://geo-q.podnote-api.workers.dev

## 结构

| 内容 | 放在哪里 | 原因 |
| --- | --- | --- |
| `index.html`、`TemplateData/`、`WebGL.loader.js`、`StreamingAssets/`、`release.json` | Workers 静态资源 | 普通静态文件 |
| `Build/WebGL.{data,framework.js,wasm}.unityweb` | R2 桶 `geo-q-builds` 的 `releases/<版本>/Build/` | data 有 108 MB，超过静态资源单文件 25 MiB 的上限；三个文件都是 gzip，需要返回 `Content-Encoding: gzip` 让浏览器原生解压（itch 也是这样返回的，JS 解压在 iPhone X 上太吃内存） |

`src/worker.js` 只处理这三个 `.unityweb`：返回 `ETag`/`Last-Modified` + `Cache-Control: no-cache`，Unity 的数据缓存（Data Caching）第二次打开时会走 304，不会重新下载。所有响应都带 `X-Robots-Tag: noindex`，不被搜索引擎收录。

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
