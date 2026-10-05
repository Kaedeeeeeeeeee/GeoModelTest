# 老师第 2 轮反馈 + 任务卡 9 步 + 四等分道具轮盘 — 2026-10-05

版本 `2026.10.05-teacher-review-2`，itch.io 构建 **`2069453`**。源码提交 `ae9aa06`。

## 改动

### 任务卡与剧情

| 问题 | 改动 |
|---|---|
| 第 3 步显示「異常な試料を観察する」，实际要做的是再和 Kaede 说一次话 | quest2.1 与 quest3.1 合并为一段对话（5 句），删除 `q.lab.anomaly`；任务卡改为 **9 步**（`InvestigationProgress.StepTotal = 9`） |
| 第 6 步（原）「博士と掘削の準備をする」措辞含糊 | 改为「博士から掘削の説明を聞く」 |
| 到达钻探地点时的 quest4.2 是旧文本（无注音、出现みなと） | 重写为 4 句，带注音，承接 beat2 的「地层歪斜」和 quest3.4 的「要用钻塔」 |
| 「ねえあのがけをみて」在研究室里说 | beat4 しゅう曲题干改为「野外で見たがけ、覚えてる？……」，前一句「あんなふうに」改为「この図のように」。题目 ID、选项、顺序、计分不变 |

### 野外（老师第 2 轮反馈）

| 反馈 | 改动 |
|---|---|
| 锤子只采了一次，研究室却检验了三块 | 野外改为 A→B→C 三处各采一次。引导卡显示「採集地点 n/3」，采集提示带「（n/3）」，敲击计数改称「たたいた回数」；第 3 块之后才播放 beat2。在采集点外采样时显示琥珀色「!」警告提示 |
| 切到「何も持たない」钻塔还在 | 钻塔是放置后的固定设备（设计如此）；钻满最大深度后，提示框显示「調査できる最大の深さに達しました／ドリルタワーをしまう」，按 F（触屏「使う」）收起。未拾取的岩芯保留，可再次放置。G 键回收仍关闭 |
| 调查时提示框字太小 | 拾取、钻塔、Kaede 对话三个提示统一为 `UISystem.InteractionPrompt`：不透明深色框 + E/F 键帽，960×540 下 ≥18px；触屏显示真实按钮图标；拾取提示不再显示内部 ID（「岩石サンプルを拾う」等）。左上采集引导卡同步放大 |

### 道具轮盘（TAB）

- 簡易ボーリング装置（1000）、无人机（1100）、钻探车（1101）任何时候都不显示（旧存档也不显示，也不会被恢复为当前道具）。
- 顺序：何も持たない → フェーズシフター → 地質ハンマー → ドリルタワー。
- 轮盘改为按道具数等分的扇形（现为四等分，「×」形分隔），中心死区；按指针方向选择，包括圆盘外。推荐道具「呼吸」提示，当前道具有小圆点标记。

`ResearchContentVersion` 升级为 `ja-ui-story-2026-10-05-v0.6-teacher-review-2` / `story-nine-step-three-sites-v4`（服务器只校验长度）。

## 验证

Unity 6000.0.51f1，macOS，批处理模式。实现由 Codex（gpt-6.1-sol）按方案分 4 轮完成，每轮由 Claude 审查代码与截图并独立重跑测试。

| 范围 | 结果 |
|---|---|
| EditMode 全量 | 146/146 |
| PlayMode 全量（非 opt-in） | 66/66，6 个 opt-in 单独运行 |
| `TeacherReviewFlowCaptureTests`（EXPECT=after） | 1/1，146 项检查：A/B/C 真实锤击与拾取、研究室 3 题、钻满 5 次后收起钻塔且岩芯保留、轮盘顺序与隐藏、beat4 新题干 |
| `ToolWheelSceneTests` | 1/1：三种分辨率 × 三语下文字与图标都在本扇区内（距分隔线 ≥6px），鼠标/触屏在圆盘外也能选中，死区不选 |
| `FieldFeedbackAcceptanceTests`、`FirstControlExperienceTests`、`MobileInteractionHintTests`、`QuestAttentionSceneTests`、`IllustrationPreviewTests` | 全部通过 |
| 剧情自检 `StoryQuizTests.RunAll` | 678/0 |

测试前备份编辑器 PlayerPrefs 与存档目录，测试后恢复，逐项一致。

## 发布

| 项目 | 结果 |
|---|---|
| 源码提交 | `ae9aa06`（`release.json` 记录的提交） |
| Unity WebGL 构建 | 成功，0 错误，114 MB，21 分 14 秒。`GEOMODEL_WEBGL_VERSION=2026.10.05-teacher-review-2` |
| 本地成品 | 版本号正确，Unity 实例加载，家长同意页显示，控制台无错误 |
| 上传 | `butler push Build/WebGL …:html5 --userversion 2026.10.05-teacher-review-2`，20 个文件 114.98 MiB |
| 线上构建 | 上传约 1 分钟后 butler 显示构建 `2069453` 生效；页面的 `html_url` 从 `extracting` 变为 `complete` 之后才访问 CDN。公开页面嵌入 `17462165-2069453`；20 个发布文件与本地构建一致（两个 `index.html` 只多了 itch.io 注入的 `htmlgame.js`，见 `published-assets.json`）。直接打开嵌入地址：版本 `2026.10.05-teacher-review-2`，Unity 实例加载，家长同意页与学生同意页存在，控制台无错误 |

线上只做了加载检查，没有在公开版本中提交同意表单或开始新游戏（避免产生研究数据）。iPhone / iPad 实机未测。
