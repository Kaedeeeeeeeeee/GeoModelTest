# 2026-09-15 发布与线上验收

已发布 [Geo Model](https://kaedeeeeeeeeee.itch.io/geo-model-geological-drilling-simulator)。

| 项目 | 结果 |
| --- | --- |
| 版本 | `2026.09.15-teacher-feedback` |
| Unity | `6000.0.51f1` |
| 应用源码 | `042b4c3` |
| 发布包提交 | `af8f14d`，已推送 GitHub `main` |
| itch.io | HTML5 构建 `1980722`，已激活 |
| WebGL 构建 | 成功，0 个错误，约 117 MiB，6 分 35 秒 |
| 包校验 | Butler 识别并通过 HTML5 包检查 |
| GitHub Actions | 发布包提交的 Pages 工作流成功；正式游戏入口为上方 itch.io 链接 |

## 发布内容

包括四页操作教学、H 常驻帮助入口、首次野外说明、露头解释、NPC 任务感叹号、对话结束与重复提醒、道具选择手势隔离、取消场景选择后收起、空手轮盘项、钻车图标方向修正。

同时发布此前完成的免邀请码新游戏入口与 13 题问卷。已先备份现有后端的结构、数据和注册函数，再应用 `20260914042127_open_play_research`、`20260914060638_post_game_survey_v2` 两项迁移并部署 `research-participation`。迁移前后、加入本轮测试数据之前，原有 26 个参与记录、3 份问卷、3 张票据数量一致。旧邀请码接口、旧问卷版本及既有数据保留。

## 验证结果

- 本次功能在构建前已通过 22 项 PlayMode 和 7 项本地化 EditMode 测试。
- 真实线上后端 27 项检查通过：匿名注册、重复注册复用身份、上传、未完成游戏拒绝发券、跨身份／跨轮次拒绝、新版问卷校验、提交回执和重复提交不覆盖。
- 公开游戏页的 Run game 指向构建 `1980722` 并创建游戏画布。线上 `release.json` 与本地相同；四个核心文件解压后的 SHA-256 及问卷脚本、样式、题目文件逐项匹配。
- 在该公开页面提供的实际 CDN 游戏中，通过鼠标／键盘完成：标题页 → 新游戏 → 开场 → 研究室 → 第一题正确作答 → 结束介绍 → Tab 道具轮盘。
- H 打开四页帮助，前后翻页正常；关闭后原对话继续。末页显示「話を終える」。第二阶段 NPC 显示黄色感叹号。
- 实际点击场景切换器只选中；下一次使用才打开目的地窗口；点击「キャンセル」后窗口关闭，HUD 显示「何も持たない」。轮盘空手项可见，钻车车轮位于图标下方。
- 实际 WebGL 的匿名认证、注册、日志上传均收到 HTTP 200。
- 公开问卷显示 13 题，漏答必填项被拦截。使用明确标注的测试回答提交成功，刷新后仍显示已提交；数据库确认该轮次仅有一份 v2 回答，未写入已删除的 q12。390px 模拟视口的页面宽度与内容宽度均为 390px。

后端合成完成记录和测试回答均归入独立的 `development` 验收批次，验收后已关闭该批次入口。真实浏览器产生的未完成游戏记录标记为 `release-qa-webgl-20260915`，没有伪造完整通关。正式免邀请码入口保持开启。

## 范围与记录

浏览器为 Ego Lite。整页 `Page.captureScreenshot` 超时，因此游戏截图取自实际 WebGL 画布，交互验收在公开 CDN 页面继续完成。恢复画布录制期间记录到两次 `WrongDocumentError`（pointer lock）；后续选取、使用和取消操作均完成，收集的日志未出现 C# 运行时异常。原始错误保留在 `runtime-verification.json`。

本轮没有从头到尾重玩全部任务，也没有连接实体手机；手机检查为页面布局模拟。完整新增逻辑的自动化测试结果见上级目录。

编辑器本机偏好设置与持久化存档已恢复校验。浏览器仅清理本次新构建产生的测试存档目录，其他版本存档保留；清理前的测试存档、后端凭证与备份保存在 Git 之外。

数据库 Advisor 的提醒与上次发布记录一致：服务端专用表的 [RLS 无策略提示](https://supabase.com/docs/guides/database/database-linter?lint=0008_rls_enabled_no_policy)、原有 [匿名角色策略](https://supabase.com/docs/guides/database/database-advisors?lint=0012_auth_allow_anonymous_sign_ins)、[密码保护设置](https://supabase.com/docs/guides/auth/password-security#password-strength-and-leaked-password-protection)及 [MFA 设置](https://supabase.com/docs/guides/auth/auth-mfa)。本次未更改这些原有账户配置。

机器可读记录：`published-verification.json`、`backend-verification.json`、`runtime-verification.json`、`survey-browser.json`。

## 发布版截图

### 标题页

![标题页](public-title.png)

### NPC 与对话说明

![帮助第二页](help-page-2.png)

### 蓝色引导与露头

![帮助第四页](help-page-4.png)

### 空手与钻车图标

![道具轮盘](tool-wheel.png)

### 选中道具后，目的地窗口尚未打开

![选中场景切换器与 NPC 感叹号](scene-tool-selected.png)

### 使用后打开目的地窗口

![目的地选择](scene-destinations.png)

### 取消后恢复空手

![取消后空手](scene-cancel-empty-hands.png)
