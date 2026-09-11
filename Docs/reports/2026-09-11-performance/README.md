# 流畅优先优化版

版本：`2026.09.11-performance`。Unity：6000.0.51f1。已完成正式 WebGL 构建、本地验证与线上发布。

**线上版本**

[打开游戏](https://kaedeeeeeeeeee.itch.io/geo-model-geological-drilling-simulator)。2026-09-11 发布到原有 `html5` 渠道，itch.io build `1967721`，替换 `2026.09.11-survey-return`（build `1967562`）。

从公开页面点击 Run game 后，使用全新浏览器会话验证：版本号正确，设置显示 `Low（自动）`，DPR 2 下实际 WebGL 渲染缓冲为 **1280×720**，页面 JavaScript 错误 0。线上 `release.json` 与已测试包完全一致；加载器、画质脚本、样式和问卷资源也已核对。大体积 data / wasm 文件由真实游戏启动验证加载。

[线上验证记录](online-results.json)。已有页面需要刷新后进入新版；若之前保存过手动高画质，可在设置中点击“画质自动调整”恢复默认 Low。

![线上默认低画质](online-settings.png)

**最终画质策略**

按最新要求，首次自动画质统一为 **Low**，不再根据电脑/平板分类、CPU 核心数或内存猜测高画质。自动模式以 30 FPS 为目标，只允许降档，不会主动提高画质。玩家已有的手动画质选择仍会保留，也可以随时点击“画质自动调整”回到 Low。

进入场景或恢复游戏后先等待 8 秒，再按 4 秒窗口观察实际帧间隔。连续两个窗口低于 26 FPS，或一个窗口低于 18 FPS 时降档。标题界面、暂停、加载、剧情、后台和竖屏提示不参与判断；单个极端加载停顿不会直接当作持续性能不足。降到 Very Low 后本次自动会话不会自行升回 Low。

| 项目 | 本次结果 |
|---|---|
| WebGL 实际渲染 | Low 及以上最高 1280×720；Very Low 为 960×540。CSS 仍等比显示，DPR 不再放大渲染缓冲 |
| Low 贴图 | 使用 mip 限制减小支持 mipmap 的纹理；Very Low 再降一级。文字与不带 mipmap 的图像不会通过这项设置一起缩小 |
| 环境贴图 | 18 张地表贴图添加 WebGL 专用覆盖：主色 1024，法线和遮罩 512 |
| 道具贴图 | 无人机、钻车、显微镜、锤子四张贴图 WebGL 上限为 1024 |
| 显微镜预览 | Low / Very Low 为 512，无 MSAA；Medium 为 1024，无 MSAA；更高的手动档最多 1024、2× MSAA。关闭时停用相机，更换预览纹理时释放旧纹理 |
| 阴影 | 自动所用的低档关闭实时阴影和常规抗锯齿；Medium 也关闭阴影 |
| 设置界面 | 显示当前实际画质；自动降档不会被界面适配流程重置；手动选择停止自动调整 |
| 暂停 | 移除了旧“动画优化”中重置 Time.timeScale 的副作用，画质和屏幕尺寸变化会保留暂停状态 |

本轮保留矿物、化石源贴图与模型，未做模型减面，也未切换为 ASTC 构建。

**构建体积**

| 统计口径 | 修改前 | 修改后 |
|---|---:|---:|
| 18 张地表纹理的 packed asset 数据 | 80.00 MiB | 8.00 MiB |
| 四张道具纹理的 packed asset 数据 | 10.67 MiB | 2.67 MiB |
| 首次下载的四个 Unity Build 文件合计 | 197.99 MB | 123.58 MB |

首次下载减少约 **74.42 MB（37.59%）**。MB 为十进制，MiB 为二进制。Packed asset 数据和网络下载量不是同一口径，也不等同于 GPU 常驻内存；这些结果不能换算为帧率提升百分比。[原始体积对比](size-comparison.json)。

**验证**

- EditMode 14/14：持续掉帧、严重低帧率、短暂停顿、后台/暂停、恢复后的观察期、不自动升档和贴图平台覆盖。
- PlayMode 14/14：新画质逻辑与显微镜纹理释放测试，以及原有暂停输入、无人机和钻车相关回归测试。
- 网页模板测试 5/5：高 DPI、实际画布尺寸切换、分辨率上限、后台/竖屏排除和硬件信息缺失。
- 正式 WebGL 构建成功，构建错误数 0。
- 实际 WebGL 浏览器验证：Retina 桌面与平板触控模拟均通过。读取的是 WebGL `drawingBufferWidth/Height`，不是只看 CSS。默认 720p、最低 540p、手动 High、刷新后保留手动选择、恢复自动 Low 均符合预期；另外验证了桌面暂停时移动输入不会改变世界画面，以及平板横竖屏提示。
- 浏览器 JavaScript 页面错误 0，研究身份/上传请求 0。没有使用实验参与码。Unity 验证后已恢复本机原有偏好设置和存档。

[浏览器结果](browser-results.json)、[EditMode 结果](editmode.xml)、[PlayMode 结果](playmode.xml)。

![默认低画质](retina-desktop-settings-low.png)

![平板触控最低画质](tablet-settings-very-low.png)

![平板低画质进入游戏](tablet-gameplay.png)

浏览器验证使用 Chromium 和触控模拟，并非实体 iPad/Safari 性能测试。下一轮应在实际学校电脑和 iPad 上分别测试野外、研究室、显微镜和载具，持续运行至少 10 分钟，确认低画质能否稳定接近 30 FPS。若仍明显卡顿，再优先处理高面数显微镜与其他道具。

**本地测试包**

[下载已发布的 WebGL 包](/Users/user/Unity/GeoModelTest/Build/GeoModel-performance-20260911.zip)。压缩包根目录包含 `index.html`，与本次上线的文件一致。ZIP SHA-256：`733bc7dcf819a7db694698a13a4b85c76d86c2d4ce50e707ce5d5e93c9df399f`。

复核命令：

```sh
node --test tools/qa/performance-template.test.cjs
python3 tools/qa/loading-server.py --port 55889
# 在另一个终端运行：
python3 tools/qa/performance-browser.py
```

Unity 测试过滤器分别为 `AutomaticQualityPolicyTests;WebGLTextureSettingsTests` 与 `GamePerformanceSettingsTests;ModalInputTests;VehicleToolTests`。正式构建入口仍为 `WebGLBuildSetup.BuildWebGL`。
