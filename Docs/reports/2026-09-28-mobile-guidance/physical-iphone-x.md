# iPhone X 实体设备验收

2026-09-28，在用户已配对的 iPhone X（iPhone10,3，iOS 16.7.16，Safari 16.6.2）上操作真实 Safari。通过已有 WebDriverAgent 发送真实触摸并读取截图；USB 中途断开后复用已授权的 Wi-Fi 调试连接。

**结论：本轮实体设备验收未通过。** 开始菜单可以加载，进入 MainScene 则反复触发 iOS 内存终止。普通发布、代码体积优化、ASTC 和最低画质组合均未解决。NPC 对话和首次道具操作尚未到达可测试状态，不能标记为真机通过。所有修改和构建仍在本地，未发布到线上。

**后续（同日）：** 做了图鉴按需加载、去掉地层网格复制、隐藏图鉴，并让正式构建使用体积优化设置之后，同一台 iPhone X 用正式包完成了 NPC「調べる」、首次道具入口和地质锤采样，全程没有重载。把 WebGL 初始内存调到 384 MB 之后，全程峰值从 1,507 MiB 降到 1,313 MiB，与已观测终止点的余量从约 30 MiB 增加到约 220 MiB。详见[内存优化与正式包验收](../2026-09-28-memory-optimization.md)。

本次使用独立局域网来源 `http://192.168.0.4:55928/`，测试服务器仅提供指定的本地构建目录。CSP 和请求拦截禁止访问外部研究／分析后台，原线上来源的存档不受影响。只新增一个测试标签页。手机的 Web Inspector 未开启，未改动该设置；诊断脚本仅注入本地响应，定时执行诊断与检查点设置，不伪造用户激活，不修改打包文件。

## 已确认的浏览器行为

- 实体 Safari 上 `document.documentElement.requestFullscreen` 和 `webkitRequestFullscreen` 都为 `undefined`。第一次实际触摸后显示不支持提示，未进入全屏。
- 原生 Safari 菜单的 `HideToolbar` 操作有效。游戏画布由约 481.77 × 271 CSS 像素扩大到 632.88 × 356；顶部仍保留精简栏，因此不能将其称为网页全屏。
- 根据真机结果，将旧 iPhone Safari 的无全屏能力提示改为可执行的「Safariの『ぁあ』→『ツールバーを非表示』」说明。其他浏览器继续按全屏 API 能力判断。提示仍可关闭，10 秒后自动消失。
- 修正提示框的收缩宽度，使竖屏时不局限于半个屏幕。

## 开发构建加载失败

`Build/RemediationWebGL` 为 Development 构建，未压缩 WASM 为 134,584,970 字节。一次未进行导航操作的加载中，页面启动时间由 `1790582982401` 变为 `1790583077060`，确认发生重新加载。

手机 `JetsamEvent-2026-09-28-171116.ips` 中，17:11:16.790 的前台 `com.apple.WebKit.WebContent` 因 `highwater` 被终止，常驻内存为 1,536.05 MiB。新的页面启动时间为 17:11:17.060，相差 0.27 秒。这说明该次重载源于系统终止超出内存上限的网页进程，不是进度条估算回退。只保存了脱敏摘要，没有保存原始系统报告或其他应用信息。

## 普通发布配置对照

`Build/MobileDeviceWebGL` 构建成功：`Development=false`、IL2CPP Release、关闭调试，WASM 解压大小降至 67,011,686 字节。但 17:20:12.910 仍出现 WebContent `highwater`，驻留约 1,605.36 MiB；0.673 秒后出现新导航。

后续重试约 101 秒后进入真实开始菜单，取消研究参与对话框，再以本地检查点进入新游戏。进入 MainScene 时再次发生页面重载；系统记录为 17:25:50.030 的 WebContent `highwater`，驻留约 1,548.09 MiB，0.522 秒后重新导航。

最低画质对照：实际设置界面显示「Very Low（手動）」，画布确认 960 × 540；再次开始新游戏后仍重载：17:28:41.930 的 WebContent `highwater`，驻留约 1,567.45 MiB，0.521 秒后重新导航。因此没有将所有移动设备的自动默认画质改为 Very Low。

## 代码体积优化对照

已用 `DiskSizeLTO` 与 IL2CPP `OptimizeSize` 生成独立构建 `Build/MobileDeviceOptimizedWebGL`。WASM 解压大小从 67,011,686 降至 37,953,195 字节，减少 43.36%；资产数据包仍为 130,866,297 字节（解压后）。此策略依据 [Unity 6 的移动 Web 优化建议](https://docs.unity3d.com/cn/6000.0/Manual/web-optimization-mobile.html)。构建后 27 个项目／编辑器设置文件已恢复并逐字节校验。

在全新本地来源 `http://192.168.0.4:55929/` 上，默认 Low、自动画质成功加载 StartScene，约 89 秒时确认 `unityInstance` 就绪；此时 Unity WASM heap 为 96,665,600 字节。取消真实研究同意弹窗后，以本地检查点进入新游戏，MainScene 再次触发内存终止：17:49:56.260 的 WebContent `highwater`，驻留 1,652.67 MiB；0.494 秒后新页面导航。因此，缩小代码尚未解决主场景加载问题。

对话与首次道具入口的实体验收仍待完成。不得将此前桌面触屏模拟或 Unity PlayMode 测试记为本次实体设备通过。

## ASTC 纹理对照

当前 Player Settings 的 WebGL 纹理格式为 `DXTC=5`，Web 平台配置为 `Generic=0`（沿用 Player Settings），构建方法没有覆盖。Unity 文档说明，不支持的纹理压缩格式会在软件中解压，增加内存占用；移动 Web 应考虑 ASTC，而桌面 DXT 应单独保留。参见 [Unity Web 纹理压缩](https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-texture-compression.html)。

已查询真实 Unity WebGL context：`WEBGL_compressed_texture_s3tc` 和 `s3tc_srgb` 为 false，`astc` 和 `etc` 为 true。

独立构建 `Build/MobileDeviceAstcWebGL` 在新来源 `http://192.168.0.4:55930/` 测试。WASM、framework、loader 与代码优化版逐字节相同；实际数据包中有 108 张 ASTC 纹理。构建产物生成成功，但随后导出诊断时 `BuildSummary.GetSubtarget<T>` 不支持 WebGL，Editor 因该诊断异常退出为 6；诊断源码随后修正，未重建或改写手机测试的产物。33 个项目／编辑器设置文件均恢复至构建前。不能将该次完整 Editor 命令描述为零退出码成功。

- 默认 Low：约 77 秒时确认 StartScene 就绪，Unity heap 为 98,828,288 字节；进入 MainScene 后，17:59:43.500 WebContent `highwater`，驻留 1,539.47 MiB，0.458 秒后重新导航。
- 手动 Very Low：真实设置界面和诊断都确认质量为 0、画布为 960 × 540；进入 MainScene 后，18:02:35.700 WebContent `highwater`，驻留 1,608.72 MiB，0.119 秒后重新导航。

未将生产默认纹理或画质配置改成该对照设置。

## 已定位的后续整改范围

这些是代码与打包证据确认的资源加载行为，不代表已经测量各自的驻留内存或证明单独的崩溃因果关系。本轮未修改这些系统：

1. `Assets/Plugins/GLTFUtility/Scripts/Editor/GLTFAssetUtility.cs:161` 无条件把 GLB 嵌入纹理压为 `DXT5Crunched`。32 个 GLB 共含 80 张 1024²、无 mip 的纹理子资源，ASTC 构建中仍保留 DXT。所有纹理同时展开为 RGBA32 的理论值为 320 MiB，实际同时驻留量未测量。应先让移动构建的嵌入纹理采用支持的格式。
2. `EncyclopediaData.Awake` 在主场景启动时解析所有条目，并逐条加载全部图片和模型，由单例持有。78 条目对应 34 个不同资源名，并非 78 份独立模型实例。应改为先读元数据，在列表显示和详情打开时分别加载需要的图片、模型。
3. 8 个地层启用 `TerrainHoleSystem`，启动时先访问 `meshFilter.mesh`，再复制网格；`GeologyLayer` 只读 bounds 时也访问 `.mesh`。当前打洞只生成圆柱视觉，可先去掉无用网格复制并改读 `sharedMesh`，再回归打洞、清空与采样碰撞。
4. `GameInitializer` 在任务锁定之前创建无人机和钻探车工具，其 `Start` 会提前加载车辆预制体。可改为首次装备或放置时沿已有 `GetTemplateObject()` 路径加载，再回归解锁和放置流程。

## 验收范围

| 项目 | 实体 iPhone X 结果 |
| --- | --- |
| 识别设备、连接并实际操作 Safari | 已确认 |
| 检测全屏能力并显示旧 Safari 操作指引 | 已确认 |
| 原生隐藏工具栏扩大画布 | 已确认，仍有浏览器精简栏 |
| 开始菜单与设置、最低画质实际应用 | 已确认 |
| 稳定进入主场景 | 失败，系统内存终止 |
| 靠近 NPC、核对图标、点击調べる | 被主场景加载失败阻塞，未验收 |
| 首次道具入口、选工具并使用 | 被主场景加载失败阻塞，未验收 |
| iPad / Android 实体设备 | 未覆盖 |

## 收尾

已清除本次 55928／55929／55930 三个独立来源的 localStorage、sessionStorage 和各自的一个 IndexedDB 数据库，未清除线上来源或其他网站的数据。已关闭新增测试标签页，确认恢复原有 4 个标签页及竖屏。局域网测试／清理服务器、USB 转发、系统日志读取和 WebDriverAgent 测试会话均已停止，端口复核无监听；手机 8100 已不可连接。既有配对、信任和 WDA 安装保留，未修改 Web Inspector 设置。参见 [设备收尾记录](device-cleanup.json)。

日志与脱敏诊断：`Logs/physical-mobile-20260928/`；逐次本地诊断结果：`Logs/mobile-device-qa/results.jsonl`。

## 可复核证据

- [开发构建内存终止摘要](jetsam-summary.json)
- [普通发布配置内存终止摘要](release-jetsam-summary.json)
- [两档画质进入主场景的内存终止摘要](main-scene-jetsam.json)
- [代码体积优化后的内存终止摘要](optimized-jetsam.json)
- [代码体积优化构建与设置恢复记录](optimized-build-summary.json)
- [ASTC 默认画质内存终止摘要](astc-jetsam.json)
- [ASTC 最低画质内存终止摘要](astc-very-low-jetsam.json)
- [ASTC 构建、最终纹理格式与设置恢复记录](astc-build-summary.json)
- [残留 DXT 嵌入纹理的来源范围](embedded-dxt-source-scope.json)

![实体 iPhone 的新 Safari 提示](iphone-x-safari-guidance.png)

![已实际应用最低画质进行对照](iphone-x-very-low.png)

![ASTC 构建也实际应用最低画质进行对照](iphone-x-astc-very-low.png)
