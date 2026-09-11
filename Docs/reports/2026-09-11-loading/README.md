# 加载提示与下载优化

已部署到 [itch.io](https://kaedeeeeeeeeee.itch.io/geo-model-geological-drilling-simulator)，HTML5 build **1967075**。版本：`2026.09.11-loading`。源码提交：`f72d4cb`。Unity：`6000.0.51f1`。

## 玩家会看到什么

- 打开游戏后立即出现日文状态：准备、下载游戏数据、启动游戏。进度数字来自真实 Unity 加载回调。
- 前台约 15 秒没有收到数据时，显示「データが届くのを待っています…」，可以继续等待或重新加载；恢复传输后自动继续。
- 下载约 90 秒没有活动时，显示失败说明和「もう一度読み込む」。启动阶段使用较长的 180 秒阈值；后台停留时间不计入超时。
- 脚本加载失败、HTTP 错误、传输中断、损坏的数据、Unity 启动错误都有恢复入口。技术诊断留在控制台，不覆盖日文提示。
- 重试重新加载当前游戏 iframe，并尝试绕过本次启动的资源缓存，只清理当前构建 URL 对应的下载响应，避免再次读到损坏缓存。不会清除参与码身份、PlayerPrefs 或存档。游戏成功启动后恢复原始 fetch，后端请求不经过加载监控。

### 下载中

![手机横屏：下载状态和百分比](screenshots/01-downloading.png)

### 传输暂时停顿

![手机横屏：继续等待或重试](screenshots/02-waiting.png)

### 加载失败

![手机横屏：日文失败提示与重试按钮](screenshots/03-error.png)

以上加载截图来自本地最终 WebGL 构建的真实加载过程；停顿和失败由测试服务器主动模拟。

## 下载量

这里的 MB 为十进制，统计首次启动的 4 个 Unity Build 文件，不包含很小的 HTML/CSS、浏览器协议开销或游戏结束时才打开的问卷页面。

| 项目 | 修改前 | 修改后 |
|---|---:|---:|
| 首次下载 | 215.35 MB | 197.99 MB |
| data 文件下载 | 199.10 MB | 181.74 MB |
| data 解压后 | 232.37 MB | 208.16 MB |

减少 **17.36 MB，约 8.06%**。钻车、无人机、显微镜、钻探道具和场景切换器共 5 个大模型开启 Low 网格压缩；原始 FBX 文件与纹理分辨率保留。Wasm、framework 和 Unity loader 文件的校验和与上一版本相同，游戏逻辑未发生变化。

[Unity 文档](https://docs.unity3d.com/cn/6000.0/ScriptReference/ModelImporterMeshCompression.html)说明网格压缩会减少打包体积，同时降低部分顶点数据精度，因此本次采用 Low。资源缓存绕过使用 Unity 支持的 [`cacheControl: no-store`](https://docs.unity3d.com/ja/6000.0/Manual/webgl-caching.html)。

首轮下载仍接近 198 MB，慢速网络仍可能需要等待。本次提供可见状态与恢复入口，并减少一部分下载；不能据此确定昨天那次停在 20% 的唯一原因。

## 验证

- Unity 正式构建成功，构建错误 0。
- 无人机、钻车 PlayMode 6/6：包含真实模型和控制器、触控移动、升降、钻车生成岩芯、退出与回收恢复。
- 最终构建浏览器测试：正常启动、缓存刷新、加载界面脚本失败、Unity loader 失败、HTTP 503、HTTP 200 返回错误页面、损坏缓存重试后再次刷新、连续两次失败、传输中断、暂停 20 秒后恢复、90 秒无活动超时及重试。
- 手机尺寸 844 × 390 下实际触摸重试成功；所有故障重试后都进入标题画面，模拟身份与 IndexedDB 存档保留。
- 故意注入的损坏数据等会产生预期控制台异常，界面会显示日文恢复提示；正常启动与缓存刷新没有 JavaScript 页面错误。
- Unity 测试后已恢复并核对本机原有 PlayerPrefs 和 8 个持久化文件。
- 手机测试使用 Chromium 触控模拟，未连接实体 iPhone。浏览器进程被系统终止时，页面 JavaScript 本身无法继续显示提示。

详细数据：[下载对比](size-comparison.json)、[浏览器故障测试](browser-results.json)。

## 线上加载

![已部署版本的标题画面](screenshots/04-online-title.png)

公开游戏页的 Run game 已进入新版本；线上 release.json、加载脚本和样式校验一致，data 文件由 CDN 以 gzip 返回。[线上核对结果](public-results.json)。

## 压缩后道具的线上检查

从新游戏进入研究室，再前往野外，使用实际手机触控事件完成检查。无人机放置、接管、升空和回收正常；钻车放置、接管、生成岩芯和回收正常。全程 JavaScript 页面错误为 0，研究后端请求为 0，未使用老师的邀请码。

![压缩后的无人机已在线上升空](screenshots/05-online-drone.png)

![压缩后的钻车在线上生成岩芯](screenshots/06-online-drill-car.png)
