# 四批修改验收与截图

本轮覆盖 BUG-001～010，以及 BUG-011 的对话记录和参与者登录两项。下面每项附实际运行截图和简短说明。原图来自需求文档，仅代表记录问题时的旧版状态。

截图说明：Unity 6000.0.51f1；桌面 1366×768、平板比例 1024×768。截图使用独立测试进度；钻塔阶段通过编辑器测试夹具进入，问答则逐题操作实际按钮，故意答错两次。长文分页使用重复句子的压力测试文本。平板比例视口不等于实际 iPad Safari 测试。

## BUG-001 · 标题页与退出

加入地层、钻塔与研究室主题背景；继续、新游戏、设置和退出均随语言切换。网页退出会先保存，再返回游戏页面。

![日语标题页：主次按钮更清楚，继续游戏根据存档显示。](/Users/user/Unity/GeoModelTest/Docs/reports/2026-09-10/screenshots/01-title-ja.png)

日语标题页：主次按钮更清楚，继续游戏根据存档显示。

## BUG-002 · Esc 与暂停

设置、对话记录和确认框使用同一套暂停规则。打开时释放光标并暂停，关闭最后一层界面后恢复。

![研究室中的设置面板，增加对话记录和返回标题入口。](/Users/user/Unity/GeoModelTest/Docs/reports/2026-09-10/screenshots/02-settings-paused.png)

研究室中的设置面板，增加对话记录和返回标题入口。

## BUG-003 · 更大的道具图标

图标随轮盘和槽位一起缩放，工具名称缩短，选取位置按画布坐标计算。

![轮盘中的工具图标更大；点击区域与显示位置对应。](/Users/user/Unity/GeoModelTest/Docs/reports/2026-09-10/screenshots/03-tool-wheel.png)

轮盘中的工具图标更大；点击区域与显示位置对应。

## BUG-004 · 当前工具一眼可见

右下角显示当前工具的图标和名称，切换或取消装备时立即更新；地质锤也移回镜头可见范围。

![手持地质锤与右下角当前工具提示。](/Users/user/Unity/GeoModelTest/Docs/reports/2026-09-10/screenshots/04-equipped-hammer.png)

手持地质锤与右下角当前工具提示。

## BUG-005 · 对话、注音与长文分页

说话人、正文、注音和操作提示分开排布。长文先翻页，读完末页后再推进剧情；答题讲解和提示沿用出题角色姓名，无角色内容显示旁白、提示或反馈标签。

![实际研究室对白：注音保留在正文上方。](/Users/user/Unity/GeoModelTest/Docs/reports/2026-09-10/screenshots/07-lab-analysis.png)

实际研究室对白：注音保留在正文上方。

![长文压力测试：当前为第 2/4 页，使用重复句子构造测试文本。](/Users/user/Unity/GeoModelTest/Docs/reports/2026-09-10/screenshots/05-dialogue-page-2.png)

长文压力测试：当前为第 2/4 页，使用重复句子构造测试文本。

![修正遗漏：珊瑚题答错后的讲解，现在保留 Dr.Kaede 姓名和完整注音。](/Users/user/Unity/GeoModelTest/Docs/reports/2026-09-10/screenshots/05-wrong-feedback-speaker.png)

修正遗漏：珊瑚题答错后的讲解，现在保留 Dr.Kaede 姓名和完整注音。

![点击提示后，仍可看清是博士在提供引导。](/Users/user/Unity/GeoModelTest/Docs/reports/2026-09-10/screenshots/05-hint-speaker.png)

点击提示后，仍可看清是博士在提供引导。

## BUG-006 · 调查进度 1/10

左上角改为高对比任务卡，同时显示十步调查进度和当前行动；答题总数仍独立按 11 题统计。

![1024×768 平板比例视口：完成后显示 10/10。](/Users/user/Unity/GeoModelTest/Docs/reports/2026-09-10/screenshots/06-tablet-progress.png)

1024×768 平板比例视口：完成后显示 10/10。

## BUG-007 · 采得岩芯后返回研究室

只有当前任务中的有效钻塔岩芯才触发返程提示。野外使用手机立绘，回到研究室后才开始分析题与总结。

![野外：博士打电话提示返回研究室。](/Users/user/Unity/GeoModelTest/Docs/reports/2026-09-10/screenshots/07-field-phone-return.png)

野外：博士打电话提示返回研究室。

![研究室：任务进入 9/10，开始分析岩芯。](/Users/user/Unity/GeoModelTest/Docs/reports/2026-09-10/screenshots/07-lab-analysis.png)

研究室：任务进入 9/10，开始分析岩芯。

## BUG-008 · 完成报告与成绩

分别呈现首次答对、错误次数、最终答对和实践调查成绩。点击报告或返回标题按钮即可保存离开。

![测试答题结果：首次 9/11、错误 2 次、最终 11/11；实践 4/4。](/Users/user/Unity/GeoModelTest/Docs/reports/2026-09-10/screenshots/08-completion-report.png)

测试答题结果：首次 9/11、错误 2 次、最终 11/11；实践 4/4。

## BUG-009 · 保存、返回标题与继续

设置中新增保存返回入口和确认框。继续会回到已保存的场景和教学检查点；已完成存档可再次查看报告。

![确认前说明保存范围；取消仍保留原来的暂停界面。](/Users/user/Unity/GeoModelTest/Docs/reports/2026-09-10/screenshots/09-save-return-confirm.png)

确认前说明保存范围；取消仍保留原来的暂停界面。

![返回后的标题页，继续游戏入口仍然可用。](/Users/user/Unity/GeoModelTest/Docs/reports/2026-09-10/screenshots/09-saved-continue.png)

返回后的标题页，继续游戏入口仍然可用。

![本地 WebGL：保存后刷新页面，继续游戏仍可用。](/Users/user/Unity/GeoModelTest/Docs/reports/2026-09-10/screenshots/webgl-saved-after-refresh.png)

本地 WebGL：保存后刷新页面，继续游戏仍可用。

![软件渲染压力测试：同步超时时保留原界面，提供重试。](/Users/user/Unity/GeoModelTest/Docs/reports/2026-09-10/screenshots/09-save-timeout-guard.png)

软件渲染压力测试：同步超时时保留原界面，提供重试。

## BUG-010 · 三语新游戏确认

日语、中文和英文确认框各自完整显示对应语言。取消不清档；开始新游戏会重置本轮进度、答题与历史，保留语言和音量偏好。

![日语确认框。](/Users/user/Unity/GeoModelTest/Docs/reports/2026-09-10/screenshots/10-new-game-ja.png)

日语确认框。

![英文确认框。](/Users/user/Unity/GeoModelTest/Docs/reports/2026-09-10/screenshots/10-new-game-en.png)

英文确认框。

![中文确认框。](/Users/user/Unity/GeoModelTest/Docs/reports/2026-09-10/screenshots/10-new-game-zh.png)

中文确认框。

## BUG-011A · 对话记录

回看已经显示的对白、选择、提示和反馈，可按野外或研究室筛选。关闭记录后回到原句，阅读期间不推进任务或增加答题次数。 提示和讲解同时显示角色姓名与内容类型，玩家选项仍标为“选择的答案”。

![可滚动的对话记录，按场景筛选已阅读内容。](/Users/user/Unity/GeoModelTest/Docs/reports/2026-09-10/screenshots/11a-history.png)

可滚动的对话记录，按场景筛选已阅读内容。

![对话记录中显示“Dr.Kaede · 解説”，与玩家所选答案明确区分。](/Users/user/Unity/GeoModelTest/Docs/reports/2026-09-10/screenshots/11a-history-speaker.png)

对话记录中显示“Dr.Kaede · 解説”，与玩家所选答案明确区分。

## BUG-011B · 参与码与研究会话恢复

登录错误提示统一翻译。正常结束保留身份；断网退出保留待传记录，原参与者重连后补传；换人时隔离身份和学习进度。 已通过实际登录界面验证 A → A → B → A，并核对真实本地数据库中的身份与会话。

![研究参与码入口，提示换码会重置学习进度。](/Users/user/Unity/GeoModelTest/Docs/reports/2026-09-10/screenshots/11b-research-login.png)

研究参与码入口，提示换码会重置学习进度。

![无效参与码的日语错误提示。](/Users/user/Unity/GeoModelTest/Docs/reports/2026-09-10/screenshots/11b-invalid-code.png)

无效参与码的日语错误提示。

## 验证与使用

39 项 Unity 测试通过：34 项 EditMode、5 项 PlayMode。
另通过 18 项本地数据库断言、16 项真实本地 Supabase HTTP 检查、5 项 WebGL 桥接浏览器检查、8 项完整 WebGL 运行检查。

调查与 UI：11 道题逐题操作，其中故意答错两次，报告为首次正确 9/11、错误 2 次、最终正确 11/11。实践测试进度为 4/4。钻塔返程提示 → 研究室分析 → 报告通过；标题与完成存档往返 3 次通过；长日语分页 1/4 → 2/4 通过；确认框取消后仍保持原有暂停。

参与者：实际 Unity 登录界面完成 A 登录、A 再次进入、切换 B、恢复 A。随后在 A 的额外会话中实际答题，数据库收到同一题的第 1 次错误、第 2 次正确，内容版本为 ja-ui-story-2026-09-10-v0.2，路线为 story-lab-analysis-v2。两个匿名身份独立，A 的 4 个会话及 B 的 1 个会话均记录了结束。研究验证使用独立本地配置和数据。

离线：真实 Unity 客户端在连接失败时保留原身份、队列和会话结束记录，重连后先补传旧会话。刷新失败不会创建替代匿名身份。切换身份同时检查队列内的真实参与者绑定，包括缺少旧上下文的情况。

WebGL：Unity 6000.0.51f1 开发构建成功；在本机 Chromium 145、ANGLE Metal / Apple M1 Max 上复测。一次 Esc 打开设置并释放光标；暂停期间输入移动和转动后，框外游戏画面像素保持一致；保存完成后刷新，继续按钮仍可用并能重新进入场景；退出触发页面跳转；普通游玩认证和研究上传请求均为 0。复测路径无着色器空引用、未启用控制器移动或图鉴初始化错误。

嵌入限制：另用受限 iframe 验证了自动跳转被阻止时的真实链接及用户点击跳转。跳转目标被本地测试拦截，没有发布或更改线上游戏。

边界：SwiftShader 软件渲染压力测试触发了 8 秒保存等待超时，界面按设计留在原处并提供重试；该环境不记为保存成功。硬件加速浏览器复测通过。实体 iPad Safari、正式 itch.io 嵌入页尚未验收；1024×768 截图仅代表平板比例视口。

数据恢复：原有 17 个 Unity 偏好键及 8 个持久化文件已恢复并逐项核对。测试进度单独保存在忽略的 Logs/remediation 中。

复现工具：tools/qa/README.md。构建输出：Build/RemediationWebGL（本地开发构建，已忽略）。原始测试记录位于 Logs/remediation；本页保留已核对的结果摘要。

说话人补修：已在实际 Unity 珊瑚题中通过中、英、日三语共 15 项检查，覆盖题目、提示、错误讲解、正确讲解的姓名显示，以及提示和讲解保存到历史后的署名；另核对历史界面截图中的姓名及内容类型。此补修已编译并在编辑器运行，本节之前的 WebGL 构建与 39 项测试结果属于四批修改验收，未在此次文字署名补修后重跑。


代码与素材来源见 [素材说明](../../art/2026-09-10-remediation-assets.md)，本地复现方法见 [QA 工具说明](../../../tools/qa/README.md)。
