# 主场景加载内存优化

本轮按用户确认的方案，先处理图鉴启动加载和地层网格复制。此前 iPhone X 的内存终止基线见 [实体设备验收记录](2026-09-28-mobile-guidance/physical-iphone-x.md)。本轮修改尚未发布。

## 修改范围

- 图鉴启动只解析文字、分类和收集所需元数据。现有两套列表都是纯文字，不加载图片或模型；详情页面才加载当前模型，切换、关闭、禁用或销毁时清理预览和资源引用。
- 缺失模型的占位体也改为按需生成；清理时释放其自建材质。
- 地层只读 bounds 和 Gizmo 使用 `sharedMesh`。打洞目前只创建独立圆柱标记，因此删除启动和清空时对地层网格的无用复制，保留孔洞视觉与采样碰撞。
- 图鉴生命周期测试暴露预览相机销毁时仍绑定 RenderTexture 的清理错误，一并修正解绑顺序。

本轮不修改模型纹理导入器、自动画质默认值或生产发布目录。

## 已完成的实际场景检查

Unity 6000.0.51f1 的实际 MainScene 验收通过 55 个检查点，覆盖地质锤、钻塔、采样碰撞、样本拾取、PC／触屏道具菜单、推荐工具和提示卡入口。新增启动检查结果：

| 检查 | 结果 |
| --- | --- |
| 图鉴有效元数据条目 | 76 |
| 图鉴已加载图片引用 | 0 |
| 图鉴已加载模型引用 | 0 |
| 网格与地形碰撞器共享源引用的地层 | 8 |

当前 8 个唯一地层网格的 Unity 运行时估算合计为 63,643,092 字节。这不是 iPhone 网页进程的驻留内存，也不能直接当作本次实际节省量。

修复后的独立 PlayMode 测试 15 项全部通过、无跳过：图鉴资源与两套详情 UI 8 项、地形网格所有权与碰撞 3 项、手机／平板交互提示 4 项。图片成功测试使用实际按 Sprite 导入的资源；现有图鉴照片是普通 Texture，原代码也无法通过 `Resources.Load<Sprite>` 读到，因此另行验证其安全空结果，不修改图片导入行为。

首次组合测试已识别图片测试资源类型不匹配、预览相机纹理清理错误，以及完整场景与隔离移动 UI 测试混跑造成的单例状态干扰。原始失败报告保留；专项测试在独立进程中复测，未屏蔽错误或跳过失败用例。

- [两次测试结果](2026-09-28-memory-optimization/unity-test-results.json)
- [实际场景启动资源检查](2026-09-28-memory-optimization/startup-resources.txt)
- [实际场景 55 项检查记录](2026-09-28-memory-optimization/field-checks.txt)

本地证据目录：`Logs/memory-optimization-20260928/`。

## 第一轮 iPhone X 复测

2026-09-28 19:21–19:38 JST，在已配对的 iPhone X／iOS 16.7.16／Safari 上运行独立局域网测试包。使用与失败基线一致的非开发版、DiskSizeLTO、IL2CPP OptimizeSize 配置，未更改纹理导入器。构建成功，WASM 解压后为 37,953,051 字节。产物哈希与模板核对记录见 [第一轮构建清单](2026-09-28-memory-optimization/memory-only-build-summary.json)。

真实点击／触控完成：

- 新游戏进入 MainScene，显示教室及开场对话；随后 MainScene → Laboratory Scene → MainScene 均成功。
- 用虚拟摇杆靠近研究员，提示中显示「調べる」图标和「右側のボタンをタップ」；点击实际右侧按钮打开对话。
- 沿蓝线走到采样点，提示卡出现可点击的「道具」图标；点击后打开道具轮盘，地质锤高亮；选中后提示切换为锤击说明。
- 真实点击三次「調べる」产生 1/3、2/3、3/3 采样状态，再点击拾取，岩石消失并进入采样后剧情。

场景、剧情和奖励检查点通过只在本地测试页注入的诊断桥准备；上述摇杆移动、提示卡点击、道具选择、锤击和拾取均为手机原生触控。研究同意框未勾选，测试页阻断外部研究与分析连接。本轮不是从头到尾的剧情通关。

同一页面运行约 17 分钟，`performance.timeOrigin` 仅有 iOS 计时抖动（约 8 毫秒），未观察到重载。对应时间窗内已可读取的 Jetsam 报告没有前台 WebContent 终止项；该空结果只作补充，不能证明所有系统报告均已生成。WASM 堆容量最高观测为 288,751,616 字节，**不是 Safari 进程驻留内存**，不用于计算与旧 Jetsam 记录的节省比例。

画质在启动时为自动 Low／1280×720，采样后为自动 Very Low／960×540。源码确认既有自动降档基于合格游戏帧的平均 FPS：8 秒预热后每 4 秒评估，连续两个窗口低于 26 FPS，或一个窗口低于 18 FPS，降一级。仅凭前后快照无法判断本次属于哪一种触发，也没有证明持续 30 FPS。本轮未修改自动画质逻辑。

旧版 Safari 的 `requestFullscreen`／`webkitRequestFullscreen` 都不存在；通过原生“隐藏工具栏”扩大画面后验收，顶部仍有窄地址栏，不算系统全屏。

图鉴按钮在第一轮未通过：实际 MainScene 的图鉴面板位于 inactive、缩放为零的旧 `MobileControlsCanvas` 下，原生点击不能显示。场景文件与 HEAD 一致，确认为本轮前已有的层级问题。该问题随后补充修复，见下节；第一轮不能标记图鉴入口通过。

第一轮结束已清理独立测试 origin 的存储，恢复 Safari 原有 4 个标签页和竖屏；本机 33 个设置文件、8 个进度文件及 PlayerPrefs 均恢复并核对。

- [第一轮页面诊断记录](2026-09-28-memory-optimization/first-device-retest.json)
- [限定时间窗 Jetsam 摘要](2026-09-28-memory-optimization/jetsam-first-retest.json)
- [真机人物提示](2026-09-28-memory-optimization/13-npc-nearer.png)
- [真机道具入口](2026-09-28-memory-optimization/18-field-approach.png)
- [由提示卡打开的道具轮盘](2026-09-28-memory-optimization/19-tool-wheel-from-hint.png)
- [真机三次采样完成](2026-09-28-memory-optimization/24-hammer-hit-3.png)

## 图鉴入口补充修复

图鉴初始化时把原面板迁移到独立屏幕 Canvas，复用现有按钮、列表与详情，不激活旧的隐藏 Canvas。首次打开前完成一次性初始化，避免首次 `Start` 又将面板关闭。打开时使用项目现有模态输入及层级管理，并暂时隐藏真实虚拟控件；关闭时恢复。保留 InventoryUISystem 原有移动输入事件链，不增加第二个订阅，同帧重复的切换请求只处理一次。

补充后专项 PlayMode 16/16 全通过，实际 MainScene 的 67 个检查点通过。实际场景测试先强制显示真实移动控件，明确验证图鉴按钮存在、移动输入打开面板、首次 Start 后仍打开、虚拟控件隐藏、关闭按钮释放输入并恢复控件和视角、移动输入再次打开同一 Canvas。随后切回 Desktop 执行原有 PC 与触屏采样流程。列表开关始终保持图鉴图片／模型预载数量为零。

- [最终实际场景 67 项检查](2026-09-28-memory-optimization/field-final-checks.txt)
- [Unity 实际场景图鉴截图](2026-09-28-memory-optimization/editor-encyclopedia-open.png)

## 第二轮 iPhone X 复测（图鉴）

2026-09-28 21:24–21:41 JST，同一台 iPhone X 使用包含入口修复的最终测试包：非开发版、DiskSizeLTO、IL2CPP OptimizeSize，WASM 解压后 37,954,463 字节，与第一轮不是同一份代码。见[最终构建清单](2026-09-28-memory-optimization/build-summary.json)。

- 新游戏进入 MainScene，再去研究室、回野外，全程没有重载。见[主场景](2026-09-28-memory-optimization/device-final-main-scene.png)和[野外](2026-09-28-memory-optimization/device-final-field.png)。
- 手机「図鑑」按钮能打开图鉴，第一轮的入口问题已修复。
- **未通过：图鉴文字不显示。** 列表、地层分类、筛选和统计中的日文全部缺失，只剩「--」和数字。见[真机截图](2026-09-28-memory-optimization/device-encyclopedia-missing-cjk-text.png)。这些 Text 没有使用项目的 CJK 字体（`UIFontResolver`），WebGL 也没有系统字体回退；编辑器里有系统字体回退，所以 Unity 测试没有发现。
- **未通过：打开条目详情会让页面被 iOS 终止。** 点开任一条目后约 1 秒，Safari 页面以 `highwater` 被终止并重载，共复现两次：21:28:53，1,551.5 MiB；21:40:28，1,536.2 MiB。用 DVT sysmontap 按 0.5 秒采样测试标签页进程：野外约 1,296–1,319 MiB；打开图鉴列表后仍约 1,297 MiB，说明按需加载有效；点条目 0.5 秒后为 1,378.9 MiB，约 1.0 秒后进程消失。见[内存记录](2026-09-28-memory-optimization/encyclopedia-detail-memory.json)。模型本身不大：嵌入纹理导入时已缩到 1024²，约 3 千顶点。具体是哪部分分配越过上限尚未确认；未验证的推测是 WASM 堆从 275.4 MiB 扩容时的复制峰值。

用户确认本次发布不需要图鉴，因此不修复这两点，改为隐藏图鉴入口，见下节。测试用的独立 origin 存储已清空，没有错误，见[清理记录](2026-09-28-memory-optimization/final-origin-cleanup.json)。

## 本次发布：隐藏图鉴，正式构建使用实机验证过的设置

- 新增 `ResearchExperienceSettings.EncyclopediaEnabled`，与仓库开关同一模式，本次关闭。关闭后不创建手机「図鑑」按钮，「道具」按钮移到它原来的位置；手机图鉴事件和 PC 的 O 键都不再打开图鉴。操作说明中不再提图鉴，日／中／英两套本地化文件都已修改。收集记录照常在后台运行，图鉴元数据仍按需加载。重新开放时，需要把 `ui.guide.*menus` 文案改回来。
- `WebGLBuildSetup.BuildWebGL` 每次正式构建都会设置 IL2CPP OptimizeSize 和 WebGL DiskSizeLTO，即手机上通过的配置。`codeOptimization` 保存在 Library 中、不随仓库保存，所以必须在构建时设置。当前平台不是 WebGL 时构建直接失败，避免发布未经实机验证的配置。之前的正式包是速度优化版，WASM 解压后约 67 MB；内存优化后没有在手机上验证过。
- Unity 测试：独立 PlayMode 15 项通过，1 项可选截图用例按设计跳过。实际 MainScene 验收 61 个检查点全部通过，包含三项新检查：没有图鉴按钮、「道具」占用原位置、触屏和键盘输入都不会打开图鉴。见[检查记录](2026-09-28-memory-optimization/release-field-checks.txt)和[触屏布局截图](2026-09-28-memory-optimization/release-touch-layout.png)。

## 正式包构建与 iPhone X 复测（初始内存 64 MB）

正式脚本 `WebGLBuildSetup.BuildWebGL` 于 22:13 构建成功，耗时 17 分 48 秒，0 错误，版本为 `2026.09.28-mobile-guidance`。实际 Bee 输入确认是 ReleaseSize、optimizeForSize 的非开发版，日志也确认应用了 OptimizeSize 和 DiskSizeLTO。WASM 解压后 37,954,624 字节，原正式包约 67 MB。见[正式包清单](2026-09-28-memory-optimization/release-build-summary-64mb.json)。`Build/WebGL/release.json` 是发布清单，目前仍描述 2026.09.25 的旧版本，发布时需要重新生成，并加入新增的 `TemplateData/fullscreen.js`。

2026-09-28 22:14–22:21 JST，在同一台 iPhone X 上用独立来源 55933 运行正式包，全程使用原生触控：

- 加载画面显示 `2026.09.28-mobile-guidance`。新游戏进入 MainScene 后，左上只有「バッグ」和「道具」，没有图鉴按钮。见[截图](2026-09-28-memory-optimization/release-device-main-no-encyclopedia.png)。
- 操作说明第 3 页的「持ちもの・設定」已不再提到图鉴。见[截图](2026-09-28-memory-optimization/release-device-guide-menus.png)。
- 研究室：用摇杆靠近研究员，提示显示「調べる」图标和「右側のボタンをタップ」；点击右侧实际按钮后打开对话。见[提示](2026-09-28-memory-optimization/release-device-npc-prompt.png)和[对话](2026-09-28-memory-optimization/release-device-npc-dialogue.png)。
- 野外：沿蓝线走到采样点，提示卡出现「道具」入口。点它打开轮盘，地质锤高亮；选中后提示换成锤击说明；三次「調べる」达到 3/3，再点一次拾取样本。见[入口](2026-09-28-memory-optimization/release-device-tool-hint.png)、[轮盘](2026-09-28-memory-optimization/release-device-wheel-from-hint.png)、[装备](2026-09-28-memory-optimization/release-device-hammer-equipped.png)和 [3/3](2026-09-28-memory-optimization/release-device-hammer-3of3.png)。
- 移到原图鉴位置的「道具」按钮，实际点击能打开轮盘。见[截图](2026-09-28-memory-optimization/release-device-moved-tools-button.png)。
- 页面连续运行 6.4 分钟，`performance.timeOrigin` 没有变化，该时间窗内也没有 WebContent 的 Jetsam 记录。见 [Jetsam](2026-09-28-memory-optimization/release-jetsam.json) 和[诊断记录](2026-09-28-memory-optimization/release-device-retest.json)。测试页拦截了 Unity Analytics 和 Supabase 的连接，研究同意框没有勾选。

**内存余量很薄。** 测试标签页进程平时约 1,230–1,310 MiB，但切换到研究室时出现了不到 1 秒的尖峰：从 1,238 MiB 升到 1,507.3 MiB。此前观测到的终止点在 1,536–1,653 MiB 之间，余量只有约 30 MiB。同一时刻 WASM 堆从 229.4 MiB 增长到 275.4 MiB，尖峰幅度与旧堆大小相当，符合“iOS Safari 扩容时会复制整块内存”的推测，但尚未证实；图鉴详情的崩溃也符合同样的模式。见[内存记录](2026-09-28-memory-optimization/release-device-memory.json)。

收尾：已清空 55933 测试来源的存储（删除 1 个 IndexedDB 数据库），关闭测试标签页，手机恢复原有 4 个标签页和竖屏。WDA 会话、测试服务和采样进程都已停止，复查端口也没有残留监听。见[来源清理](2026-09-28-memory-optimization/release-origin-cleanup.json)和[手机收尾](2026-09-28-memory-optimization/release-phone-cleanup.json)。

## 初始内存调到 384 MB 后的复测（最终版本）

按用户确认，把 `WebGLBuildSetup` 设置的 WebGL 初始内存从 64 MB 改为 384 MB，其他设置不变。实机主流程的 WASM 堆最高为 275.4 MiB，384 MB 足够全程使用而不需要扩容。22:51 构建成功，耗时 20 分 33 秒，0 错误。解析 WASM 内存段确认初始为 6,144 页（384 MiB），最大 2,048 MiB。见[正式包清单](2026-09-28-memory-optimization/release-build-summary.json)。

2026-09-28 22:53–22:57 JST，在同一台 iPhone X 上用新的独立来源 55934 执行与上一轮相同的步骤，并用同样的方法采样测试标签页的内存：

- 功能：新游戏、研究室研究员对话（「調べる」提示和实际按钮）、野外提示卡「道具」入口、轮盘地质锤、三次锤击、拾取样本，全部通过，页面没有重载。见[主场景](2026-09-28-memory-optimization/release-384-main.png)、[研究员提示](2026-09-28-memory-optimization/release-384-npc-prompt.png)、[对话](2026-09-28-memory-optimization/release-384-npc-dialogue.png)、[轮盘](2026-09-28-memory-optimization/release-384-wheel-from-hint.png)、[3/3](2026-09-28-memory-optimization/release-384-hammer-3of3.png)、[拾取后](2026-09-28-memory-optimization/release-384-sample-picked.png)和[诊断记录](2026-09-28-memory-optimization/release-384-device-retest.json)。
- WASM 堆全程保持 402,653,184 字节，没有扩容。开始菜单时测试标签页约 783 MiB，与 64 MB 版在同一时点的 735–975 MiB 相当，说明更大的初始内存没有被预先占用。
- 该时间窗内没有 WebContent 的 Jetsam 记录。见 [Jetsam](2026-09-28-memory-optimization/release-384-jetsam.json)。

| 测试标签页内存峰值 | 初始 64 MB | 初始 384 MB |
| --- | ---: | ---: |
| 新游戏进入 MainScene | 1,279 MiB | 1,280 MiB |
| 切换到研究室 | 1,507 MiB | 1,303 MiB |
| 切回野外 | 1,417 MiB | 1,313 MiB |
| 全程最高 | 1,507 MiB | 1,313 MiB |
| 与最低已观测终止点（1,536 MiB）的余量 | 约 29 MiB | 约 223 MiB |

切换到研究室时约 270 MiB 的瞬时尖峰已经消失，与“扩容时复制整块内存”的解释一致。数据见[对比](2026-09-28-memory-optimization/initial-memory-comparison.json)。

边界：以上只覆盖同一条主流程。钻塔、无人机、钻探车和研究室显微镜等功能没有在手机上测过；如果它们让堆超过 384 MiB，仍会发生一次扩容。平板等其他设备也尚未测试。

收尾：已清空 55934 测试来源的存储（删除 1 个 IndexedDB 数据库），关闭测试标签页，手机恢复原有 4 个标签页、竖屏和标签页总览。WDA、测试服务、清理服务和采样进程都已停止，复查端口也没有残留监听。见[来源清理](2026-09-28-memory-optimization/release-384-origin-cleanup.json)和[手机收尾](2026-09-28-memory-optimization/release-384-phone-cleanup.json)。
