# 老师反馈（2026-09-30）：TAB、采集提示、岩石题移到研究室、珊瑚化石

完整报告（含截图与录屏）：[老师反馈修改报告](https://claude.ai/code/artifact/b63d3f5a-bd54-4872-8b9b-cba2e26dc273)

## 改动

| # | 老师反馈 | 改动 |
|---|---|---|
| 1 | 右下道具栏显示「TAB」 | `CurrentToolHUD`：电脑端加 TAB 键帽 +「で切りかえ」，触屏端隐藏 |
| 2 | 采到岩石要有成功提示 | 新增 `GameToast`；`SampleCollector` 入库时显示「✓ 岩石サンプルを採取できました！」（岩芯为「ボーリングコアを採取できました！」）。`QuestManager` 把采集后的对话延后 1 秒，其间 `CollectionGuidanceHUD` 与钻塔提示隐藏 |
| 3 | 岩石题回研究室再做 | 3 道岩石题及讲解从 `beat2`（野外）移到 `quest3.4`（回研究室自动播放）；`beat2`、`quest3.1` 台词改为“带回研究室调查”；反馈里的「ここ」改为「あの露頭のあたり」 |
| 4 | 「よくやった」→「うまくできたね！」 | `core-return`：Kaede 台词替换并补注音，中英同步；前面加主角发现白色花纹的伏笔 |
| 5 | 不需要回收就别放进菜单 | `DrillTowerInteractionUI` 不再列出回收；`DrillTowerTool.allowRecall`（默认 false）关闭 G 键回收 |
| 6 | 回研究室后交代珊瑚化石 | `beat3` 开头加入剖开岩芯 → 发现珊瑚化石 → 与石灰岩呼应；珊瑚插图提前到揭示句 |

题目 ID、选项顺序和计分不变（仍为 11 题）。`ResearchContentVersion` 改为 `ja-ui-story-2026-09-30-v0.5-teacher-review` / `story-lab-rock-analysis-v3`。

## 验证

Unity 6000.0.51f1，macOS，批处理模式。

- 新增整条路线实机验收 `TeacherReviewFlowCaptureTests`（`GEOMODEL_TEACHER_REVIEW_CAPTURE=<目录>` 开启，`GEOMODEL_TEACHER_REVIEW_EXPECT=after` 启用改后断言）：1/1，41 项检查。改前代码上同一测试也跑过一次作为对照。
- 原有 `FieldFeedbackAcceptanceTests`：1/1（岩石题改在 `quest3.4` 中验证；触屏放置钻塔前等过对话关闭保护帧）。
- PlayMode 全量：63 通过，0 失败，4 个需手动开启的场景测试跳过（其中两个即上面两项，已单独运行）。
- EditMode 全量：84/84，含新增 `TeachingRouteContentTests`。
- 本机 PlayerPrefs 与存档目录测试前备份、测试后恢复，逐字节一致。

`images/` 为改前/改后对比图，`videos/` 为录屏，`logs/` 为两次运行的对白记录、观测值和测试结果。

## 发布

版本 `2026.09.30-teacher-review`，itch.io 构建 **`2040900`**。

| 范围 | 结果 |
| --- | --- |
| 源码提交 | `ebe81c2`（`release.json` 记录的是干净提交，构建本身只改了 `ProjectSettings` 的版本号） |
| Unity WebGL 构建 | 成功，0 错误，114 MB，29 分 8 秒（代码改动触发了完整的 DiskSizeLTO 链接）。`GEOMODEL_WEBGL_VERSION=2026.09.30-teacher-review` |
| 本地成品 | 内置浏览器打开 `tools/qa/loading-server.py` 提供的成品：版本号正确，Unity 实例加载，家长同意页显示，控制台无错误 |
| 上传 | `butler push Build/WebGL kaedeeeeeeeeee/geo-model-geological-drilling-simulator:html5`，20 个文件 114.14 MiB |
| 线上构建 | 等 `html_url` 返回 `complete` 后再访问 CDN。公开页面嵌入 `17462165-2040900`；20 个发布文件与 `release.json` 一致（`index.html` 只多了 itch.io 注入的 `htmlgame.js`，见 `published-assets.json`）。点「Run game」后显示家长同意页；直接打开嵌入地址读取到版本 `2026.09.30-teacher-review`，控制台无错误 |

线上只做了加载检查，没有在公开版本里走完同意流程或开始新游戏（避免产生研究数据）；新功能的实机验证见上方编辑器测试。iPhone / iPad 实机未测。
