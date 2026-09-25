# 问卷五级选项与 300 字限制

已发布 `2026.09.25-survey-five-choices-300`，itch.io HTML5 构建为 `2013714`。正式游戏入口已经切换到此版本。

- 11 道选择题各保留 5 个选项，删除「答えたくない」及相关说明和错误提示。
- 第 12、13 题保留可留空的自由回答，输入框和计数器上限均为 300 字。
- 旧草稿中的 `skip` 会被清除并要求重新选择；超过 300 字的旧草稿保留原文，提交前提示缩短。
- 当前 v2 的服务器校验拒绝 `skip` 和超过 300 字的回答。已有回答不会被重写，旧 v1 的历史契约保持兼容。

## 验证

- 6 项 Node 回归测试通过：正式页和预览、旧草稿恢复、漏答、空白自由回答、失败重试、300／301 字边界。
- 本地真实 Supabase HTTP：37 项检查通过，包括两题各 301 字被拒绝、各 300 字被接受、旧版兼容及去重。
- Ego Lite 在本地正式页完成实际交互：漏答拦截、旧 `skip` 草稿重新选择、301 字草稿完整保留并阻止提交、缩短到 300 字后收到真实本地后端回执。
- 线上数据库事务测试通过：300 字接受、两题各 301 字拒绝、`skip` 拒绝、空白自由回答接受、重复提交不覆盖、权限保留。测试数据全部回滚，前后问卷回答均为 5 份，票据均为 6 张。
- 线上预览确认 13 题、55 个选项、两个 `maxlength=300` 输入框及 `0 / 300 文字` 计数器，没有被删除的选项。
- 4 个已发布问卷资源和 `release.json` 与本地验证包逐字节一致。Unity 二进制与上一版 manifest 的哈希一致，本次仅更新独立网页资源。
- Word 的 13 道题、55 个选项、两处 300 字提示与系统定义一致；三页渲染均已检查。

生产迁移：`20260925005559_survey_five_choices_300_chars.sql`。

[线上问卷预览](https://html-classic.itch.zone/html/17462165-2013714/StreamingAssets/Survey/preview.html)

## 后端检查

权限仍为 `SECURITY INVOKER`，只允许 `service_role` 调用提交函数。Advisor 保留原有的 [服务器表无 RLS 策略提示](https://supabase.com/docs/guides/database/database-linter?lint=0008_rls_enabled_no_policy)、[匿名登录策略提示](https://supabase.com/docs/guides/database/database-advisors?queryGroups=lint&lint=0012_auth_allow_anonymous_sign_ins)、[密码泄露保护](https://supabase.com/docs/guides/auth/password-security#password-strength-and-leaked-password-protection)及 [MFA 配置提示](https://supabase.com/docs/guides/auth/auth-mfa)。本次没有调整这些账户设置。

网页验收使用 DOM 和交互结果；Ego Lite 截图接口此前失败，本轮没有把网页截图作为通过依据。
