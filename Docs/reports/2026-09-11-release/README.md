# itch.io 发布验证 · 2026-09-11

已发布 [Geo Model](https://kaedeeeeeeeeee.itch.io/geo-model-geological-drilling-simulator)，版本 **2026.09.11-survey-ui**，HTML5 构建 **1965578**，替换原构建 1784885。

源码提交为 `23084b3bc50f91a94842a66008cbc49940aa2ac0`，位于 GitHub 的 `codex/experiment-validity-foundation` 分支。发布包附带 `release.json`，包含 Unity 版本、源码提交及四个运行时文件的 SHA-256；已核对 itch.io CDN 实际文件。

Unity 6000.0.51f1 发布构建成功，0 个构建错误，包大小约 189 MiB。Butler 包校验通过。本轮功能此前已通过 42 项 Unity EditMode 测试；发布包额外经过本地启动和公开 itch.io 页面验收。

## 线上实际检查

在 macOS Chromium 中使用独立测试浏览器资料，检查公开的 itch.io 嵌入页：

- 新版标题页加载，发布版本与源码记录一致。
- 新游戏进入剧情，跳过灾害场景后到达实验室。
- 对话显示说话人；第一道测验能答错、查看反馈、重新选择正确答案。
- 设置页能打开并暂停角色移动；结束对话后键盘移动正常，工具轮盘可开关。
- 保存并返回标题后，IndexedDB 同步成功。
- 刷新 itch.io 页面后「继续」可用，再次进入恢复到实验室已保存的对话位置。
- 「游戏结束」卸载原游戏 iframe，并回到公开游戏页。
- 普通游玩没有发起研究登录或日志上传请求。
- 问卷网页文件随包发布；没有游戏凭证时显示正确的访问提示。

共记录 15 项发布与游玩检查。详细原始结果在本机被 Git 忽略的 `Logs/release-2026-09-11/`。

## 实际截图

新版线上标题页：

![标题](screenshots/01-title.png)

答错反馈有说话人名称，能继续修改答案：

![测验反馈](screenshots/02-quiz-feedback.png)

刷新后「继续」仍然可用：

![刷新后继续](screenshots/03-refresh-continue.png)

恢复到已保存的实验室对话位置：

![恢复对话](screenshots/04-resumed-dialogue.png)

完成初始对话和测验后可以移动，调查进度到达 2/10：

![移动](screenshots/05-movement.png)

## 验证边界与后端状态

本轮覆盖桌面 Chromium 的启动、初始剧情/测验、输入、保存、刷新恢复和退出流程。完整剧情通关与实体 iPad Safari 不在本轮线上验收范围。

刷新阶段捕获过两条浏览器异常：`The root document of this element is not valid for pointer lock.` 本次角色移动和刷新恢复未被阻断，记录为后续页面切换兼容性排查项；不能把此次结果表述为完全没有浏览器异常。新的退出复核过程没有页面异常。未发现 NullReferenceException、InvalidOperationException 或 ArgumentNullException。

本次发布的是游戏客户端。远程 Supabase 仍为旧版日志接口，研究 foundation v2、邀请码接口和问卷接口尚未部署；当前正式研究入口仍关闭。普通游玩可用，研究数据采集与问卷关联仍需按 `supabase/README.md` 完成后端上线。

原有 Unity 游戏偏好设置和存档保持不变。`tmp/pdfs/` 中已有的临时文档图片留在本机，没有上传至 GitHub。
