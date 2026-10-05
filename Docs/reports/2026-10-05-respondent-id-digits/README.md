# 回答者ID改为 1〜10 位数字 + 学生同意移到网页 — 2026-10-05

版本 `2026.10.05-consent-id`，itch.io 构建 **`2067724`**，数据库迁移 `20261005015841_respondent_id_digits`。本次同时发布 10/1 完成的学生同意网页化和 Safari 工具栏提示（提交 `f1a27db`，截图见 [`../2026-10-01-web-consent-safari-hint/`](../2026-10-01-web-consent-safari-hint/)）。

## 背景

2026-10-02，テスティー的倉田さん在与稲垣老师的邮件中（抄送用户）更正了回答者ID格式：

- 只有数字，长度因人而异；
- 最少 1 位，最多 10 位。

之前按「p + 7 位数字」实现是对方最初的说法有误。旧规则下，真实的 7 位数字ID会被自动加上「p」后保存（数据错误），其他位数直接被拒绝。

家长同意书和本人同意书只写了「（株）テスティーから提供される回答者ID」，没有写格式，文案不变。

## 改动（提交 `074ec24`）

| 位置 | 内容 |
|---|---|
| 家长同意页 `guardian-consent.js`、`index.html` | 只接受 1〜10 位数字。全角数字、空格、破折号仍自动整理；不再转小写、不再补「p」。输入框 `inputmode="numeric"`（手机弹出数字键盘），占位符「数字のみ（1〜10桁）」，错误提示「回答者IDは数字（1〜10桁）で入力してください。」 |
| 问卷 `survey.js`、老师预览 `preview.js` 及两个 html | 同上 |
| Edge 函数 `research-participation` | 同样的规范化和校验 |
| 数据库 | `guardian_consents`、`survey_responses` 的 `respondent_id` 约束改为 `^[0-9]{1,10}$`；`record_guardian_consent`、`use_survey_ticket` 只改了格式这一行（改前用哈希确认线上函数体与本地迁移文件一致） |

- ID 按文本保存和比对：开头的 0 会保留，`0012345` 与 `12345` 视为不同的ID（问卷中会触发不一致提示）。
- 约束为 NOT VALID：9/29〜9/30 试用时留下的「p」格式记录（家长同意 8 条中 7 条、问卷 2 条）原样保留。这些参加者如果继续答问卷，新输入的数字ID与旧记录不一致，会提示后仍可提交。

## 验证

| 范围 | 结果 |
|---|---|
| Node 测试 | 26/26（家长页：无「p」、超过 10 位、小数点等被拒；1 位、10 位、开头为 0 被接受；全角／空格／破折号整理。问卷：新提示文字、开头 0 保留、草稿保存） |
| 家长页浏览器测试 | 11/11（模板预览与正式构建各跑一次；全角「０１２３ ４５６７」整理为 `01234567`） |
| 数据库（本地测试库） | `guardian_consent_test` 32/32（新增「最多 10 位」）；`survey_respondent_id_test` 通过（新增：旧「p」格式被拒、去掉开头 0 视为不一致）；`survey_limits_test`、`open_play_test`、`research_entry_release_test`、`research_foundation_v2_test` 通过 |
| Unity EditMode | 85/85 |
| 内置浏览器（手机尺寸） | 占位符和错误提示显示正常 |

## 发布

| 项目 | 结果 |
|---|---|
| 源码提交 | `074ec24`（`release.json` 记录的提交；迁移文件改名在发布提交中） |
| Unity WebGL 构建 | 成功，0 错误，114 MB，16 分 8 秒。`GEOMODEL_WEBGL_VERSION=2026.10.05-consent-id` |
| 本地成品 | 版本号正确，Unity 实例加载，家长同意页占位符为新文字，控制台无错误 |
| 上传 | `butler push … :html5 --userversion 2026.10.05-consent-id`，20 个文件 114.97 MiB |
| 线上数据库 | 通过 Supabase MCP `apply_migration` 应用，记录版本 `20261005015841`，本地文件已改为同名。两个约束为新规则（NOT VALID）；两个函数与本地文件哈希一致；anon 不能执行，service_role 可以 |
| 线上函数 | `supabase functions deploy research-participation --use-api`（第 5 版）。从线上读回的源码为新规则 |
| 线上构建 | 等 `html_url` 从 `extracting` 变为 `complete`（上传后约 15 分钟）后才访问 CDN。公开页面嵌入 `17462165-2067724`；20 个发布文件与本地构建一致（两个 `index.html` 只多了 itch.io 注入的 `htmlgame.js`，见 `published-assets.json`）。直接打开嵌入地址：版本 `2026.10.05-consent-id`，Unity 实例加载，家长同意页为新占位符和数字键盘，学生同意页存在，控制台无错误 |

上线顺序：先上传游戏，在 itch 解包期间应用数据库迁移并部署函数，新页面和新服务器几乎同时生效。线上只做了加载检查，没有在公开版本中提交同意表单或开始新游戏（避免产生研究数据）。真实ID从家长页到问卷的完整流程，以及 iPhone / iPad 实机，仍需在实机上测一遍。
