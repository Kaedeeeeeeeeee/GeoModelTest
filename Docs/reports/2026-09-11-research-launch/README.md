# 邀请码、游戏日志与问卷后端上线验收

研究数据链路已部署到现有 Sendai Supabase 项目。邀请码、游戏记录和问卷通过后台绑定的 participant、session 和 run 关联，问卷没有让玩家修改参与者编号的输入框。

| 发布项 | 结果 |
| --- | --- |
| 公开游戏 | [itch.io 游戏页面](https://kaedeeeeeeeeee.itch.io/geo-model-geological-drilling-simulator) |
| 游戏版本 | `2026.09.11-research` |
| itch 构建 | `1966928`，html5 通道已激活 |
| 应用源码 | `1a593be` |
| 构建提交 | `91a69ad` |
| Git 分支 | `codex/experiment-validity-foundation`，已推送 GitHub |
| 云端接口 | research-participation / game-ingest-v2 / game-survey，均已激活 |

## 已完成的功能

| 环节 | 行为 |
| --- | --- |
| 开始游戏 | 「研究に参加」输入分配的代码，验证成功后开始研究记录 |
| 记录游戏 | 事件、答题尝试、进度、心跳均与参与者及游戏会话关联 |
| 游戏结束 | 结算按钮先确认日志及完成进度上传，再取得本次游戏的问卷链接 |
| 填写问卷 | 日文手机网页，后台识别参与者，不接受前端指定另一位参与者 |
| 网络重试 | 保留问卷草稿；重复日志及问卷提交不会新增重复记录或覆盖首次回答 |
| 研究者导出 | 私有邀请码登记表可合并游戏轮次、答题统计、日志数量和 q1–q14 |

普通游戏入口仍不自动登录或上传研究数据。内部试跑使用 `development` 批次，实际学生研究使用独立的 `active` 批次，并按真实流程记录同意信息。入口、撤回、协议版本和截止时间由服务器检查。

## 截图

结算界面的「アンケートに答える」使用真实云端接口取得了问卷链接。此图使用专门的编辑器结算测试存档，所以没有完整答题分数；它不是一次完整通关的成绩截图。

![真实 Unity 结算按钮](screenshots/01-live-unity-report.png)

问卷在 itch 的实际静态页面运行并向真实云端提交。页面没有参加者编号输入框，桌面和手机均已测试。

![日文问卷桌面页面](screenshots/02-survey-desktop.png)

[手机整页截图](screenshots/03-survey-mobile.png) · [问卷末页](screenshots/04-survey-final-page.png) · [提交成功](screenshots/05-survey-complete.png)

新版公开游戏显示「研究に参加」。在全新的浏览器会话中，实际输入独立测试邀请码后成功进入游戏，邀请码验证和游戏日志上传均返回 HTTP 200。服务器查到对应参与者的 9 条实际 WebGL 事件。

![公开版本研究入口](screenshots/06-live-title.png)

[公开版本代码输入框（空）](screenshots/07-live-code-entry.png) · [本次发布的问卷提交确认页](screenshots/08-current-release-survey-complete.png)

## 验证范围

- Unity 6000.0.51f1：43 项 EditMode、11 项 PlayMode 测试通过。
- PostgreSQL：18 项原有研究合同检查和 14 项新入口资格检查通过，包括正式批次同意信息、撤回、截止、协议版本与角色权限。
- 本地真实 HTTP：16 项邀请码/日志、24 项问卷检查通过。
- 远程独立 `release-qa` 批次：31 项 HTTP 检查通过，包括身份绑定、冒用拦截、重复重试、未完成游戏拒绝发券、跨游戏轮次拒绝、提交不可覆盖。
- 实际浏览器：9 项问卷检查通过；另对真实 Unity 按钮发出的链接再次执行同样 9 项检查。
- Unity UI → 云端完成记录 → 问卷票据 → 浏览器提交 → 数据库导出的链路已核对。该参与者的会话以 `post_game_survey` 正常结束，回答对应原邀请码。
- 导出按同一次 `run_id` 统计答题结果，已验证另一轮游戏不会串分。日志数量与会话数量字段是该参与者的跨轮次累计值。
- 20 码生成重试仍得到同一批；更改数量后重试被拒绝，原 CSV 保持不变。
- 断线会话 Cron 已在远程执行成功。其范围仅包含有研究参与者绑定的会话。
- 关闭这次公开 WebGL 测试浏览器后，远程 Cron 实际将该会话标记为 `heartbeat_timeout`。
- 公开 itch 页面加载了预期新版本，JS 错误为 0；进入研究模式前没有研究认证或接口请求。公开 `release.json` 与上传包一致，问卷脚本、样式及题目文件逐一匹配；itch 仅在问卷 HTML 末尾添加了其平台脚本。
- WebGL 发布构建成功，编译错误 0。测试后原 Unity 偏好设置及 8 个持久化存档文件已恢复并逐项核对。

HTTP 完成进度是明确标注的合成测试数据；编辑器结算检查也使用测试存档。上述验证不等于在手机上从头到尾重玩全部教学内容。手机网页检查使用 Chromium 触屏模拟，不能替代真机 Safari 验收。

本机编辑器 UI 验收首次遇到 Unity Metal 原生渲染崩溃，使用单线程渲染参数 `-force-gfx-direct` 重试后完成。公开 WebGL 的独立浏览器验收已通过。[Unity 渲染启动参数说明](https://docs.unity3d.com/cn/2022.3/Manual/PlayerCommandLineArguments.html)

## 旧数据与发布隔离

上线前已在私有目录备份云端结构、数据及旧函数源码。备份中的 5 张业务表在独立本地数据库成功恢复并升级，升级后的内容摘要与云端升级前一致。云端升级后、加入本次 QA 数据之前，五张表的数量和内容摘要也全部一致：

| 原有表 | 行数 |
| --- | ---: |
| game_sessions | 77 |
| telemetry_events | 2596 |
| player_profiles | 52 |
| progress_snapshots | 52 |
| sessions | 5 |

旧 `game-ingest` 的源码及部署包 SHA-256 保持不变，验证 JWT 的配置保持启用。Supabase 更新全局 secret 后其内部部署版本递增，代码没有被新接口替换。新版游戏单独使用 `game-ingest-v2`。

20 个分发码属于 `geomodel-internal-2026-09-11`，与验收数据隔离。原始代码、发行登记表、服务密钥、pepper 和完整数据备份均不在 Git 中。操作步骤见 [后端运行说明](../../../supabase/README.md)。

验收结束时，`release-qa-2026-09-11` 的入口已关闭；内部试跑批次保持开放，20 位参与者均为 ready、0 个码被使用。

## 安全检查记录

上线后运行 Supabase Security Advisor。研究表启用了 RLS，并明确撤销玩家角色的直接读写权限；因此它们的 [RLS 无策略信息提示](https://supabase.com/docs/guides/database/database-linter?lint=0008_rls_enabled_no_policy) 是服务端专用访问模式的预期结果。

Advisor 还提示原有 own-session 策略和 cron 自带策略允许匿名身份角色，以及未启用泄露密码检测和更多 MFA 选项。此次没有改动原有账户认证配置；研究玩家使用匿名账户，新增研究表不对这些角色开放直接访问。相关说明：[匿名访问策略检查](https://supabase.com/docs/guides/database/database-advisors?queryGroups=lint&lint=0012_auth_allow_anonymous_sign_ins)、[密码保护](https://supabase.com/docs/guides/auth/password-security#password-strength-and-leaked-password-protection)、[MFA](https://supabase.com/docs/guides/auth/auth-mfa)。

问卷票据是有时效的持有者链接，不应互相转发。持有该链接只能回答它所绑定的问卷，不能指定其他参与者或读取其他玩家的数据。
