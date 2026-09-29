# 取消全屏 — 2026-09-29

版本 `2026.09.29-no-fullscreen`，itch.io 构建 **`2035500`**。

## 为什么不再全屏

- iPhone（Safari、Chrome）没有网页全屏 API，无法进入全屏（本机上的提示也证实了这一点）。
- iPad 进入全屏后，向下滑一定会退出。这个手势由 Safari 控制，网页无法拦住。
- 在原来的 itch.io「Click to launch in fullscreen」设置下，退出全屏后游戏会被 itch.io 隐藏，只剩「Restore game」按钮。
- 画面在全屏和页面之间来回切换，大小忽大忽小，测试时容易搞不清楚。所以决定不再使用全屏。

## 改动

| 位置 | 之前 | 之后 |
| --- | --- | --- |
| itch.io Embed options | Click to launch in fullscreen，640×360 | Embed in page，960×540，Fullscreen button 关闭 |
| 网页模板 | `fullscreen.js`：手机/平板第一次点击时自动请求全屏；退出后显示「全画面にする」按钮；旧 iPhone Safari 显示「ツールバーを非表示」提示 | 删除，模板不再请求全屏 |
| 测试 | `fullscreen-template.test.cjs`（12 项） | `no-fullscreen-template.test.cjs`：模板中不能出现全屏请求或全屏提示 |

竖屏提示（`orientation-overlay`）仍监听 `fullscreenchange`，因为 itch.io 在部分设备上仍会自己请求全屏（见下）。

## 各设备表现

- **电脑**：点「Run game」后，游戏在页面中的 960×540 窗口里运行，没有全屏按钮。
- **iPhone**：itch.io 发现不能全屏，会把游戏铺满浏览器窗口（只是 CSS，不是全屏），和之前一样。
- **Android、以及以移动版网页访问的 iPad**（浏览器标识里有 iPad／Android）：点「Run game」时 itch.io 会自己请求一次全屏。这是 itch.io 对移动设备的固定行为，后台设置关不掉。退出全屏后，游戏留在页面里的 960×540 窗口继续运行，不会被隐藏，也不会再回到全屏。
- **以桌面版网页访问的 iPad**（Safari 默认）：和电脑相同。

要让所有设备都完全不进入全屏，只能离开 itch.io 自己托管（研究过 Cloudflare Workers + R2，未实施）。

## 验证

| 范围 | 结果 |
| --- | --- |
| Node 模板测试 | 21/21 |
| Unity WebGL 构建 | 成功，0 错误，27.7 秒（`GEOMODEL_WEBGL_VERSION=2026.09.29-no-fullscreen`） |
| 本地成品（内置浏览器模拟 Android 触屏，740×360） | 版本显示正确；家长同意页可填写（测试值，仅存 sessionStorage，未点 New Game）；进入游戏后多次点击画面，没有任何全屏请求，控制台无错误 |
| itch.io 设置 | 公开页面已无全屏按钮，没有 `start_maximized`，窗口 960×540。模拟移动设备时，全屏结束后游戏仍显示在页面中（在 Fullscreen button 开启时验证，隐藏规则只和 `start_maximized` 有关） |
| 线上构建 | 公开页面嵌入 `17462165-2035500`。20 个发布文件与 `release.json` 一致（`index.html` 只多了 itch.io 注入的 `htmlgame.js`，见 `published-assets.json`）；`TemplateData/fullscreen.js` 返回 404 |

未在 iPad、Android 实机上验证。
