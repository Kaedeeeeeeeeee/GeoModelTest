# 研究参与确认弹窗

## 行为

- 每次进入 StartScene 自动弹出，三个复选框默认均不勾选，New Game 禁用。
- 全部勾选后可以点击“同意してゲームを開始する”；确认仅解锁 New Game，玩家仍需自行点击开始。
- 返回或 Esc 关闭弹窗，New Game 保持禁用；通过“查看研究参与说明”可重新打开。
- 再次打开时清空全部勾选。返回标题页需要重新确认；本次标题页内切换语言保留已确认状态。
- 新游戏原有的覆盖存档确认继续保留；未同意、取消弹窗都不重置存档。
- 正文支持滚动，复选框和底部操作固定显示。键盘导航限制在弹窗内。

## 原文与后续修改

日文标题、研究负责人信息、说明正文、三项同意声明及按钮文字来自用户提供的 `IMG_3088.HEIC`，文字转录见同目录 `研究参加説明原文.md`。保留原文，不自动翻译成其他语言；正文仅移除纸张排版造成的断行，让界面自行换行。

修改 `Assets/Resources/Localization/Data/{ja-JP,zh-CN,en-US}.json` 中的 `ui.consent.*`，并同步到 `Assets/Scripts/Localization/Data/` 下对应文件。

- `body`：研究负责人信息与说明正文。支持换行，长文案自动滚动。
- `agreement.1`、`agreement.2`、`agreement.3`：三个复选框旁的原文声明。
- `title`、`hint`、`continue`、`cancel`、`review`：相关界面文案。

本次实现的是主菜单 UI 门禁；不保存或上传同意记录，也不修改已有后端研究逻辑。

## 验证

Unity 6000.0.51f1：

- PlayMode：10/10 通过（ResearchConsentTests + ModalInputTests），包括三项勾选的全部组合、任意一项取消后重新锁定，以及暂停期间勾选图形的可见性。
- EditMode：7/7 通过（LocalizationDataTests）。
- `git diff --check` 通过。
- Unity Game View 实际画面检查：1366×768、844×390 横屏；确认勾选标记可见、全选后按钮启用、说明文字能够滚动。截图见 `screenshots/`（不是手机真机测试）。

初次专项测试报告位于 `playmode.xml`、`editmode.xml`。

## 发布验收（2026-09-25）

- 全量 EditMode：57/57 通过；全量 PlayMode：41/41 通过，报告见 `release-editmode.xml`、`release-playmode.xml`。
- 问卷 Node 回归：6/6 通过；三语言本地化镜像校验和 `git diff --check` 通过。
- WebGL Release：Unity 6000.0.51f1，117 MB，构建耗时 5 分 29 秒，BuildReport 错误为 0。见 `build-summary.txt`。
- Butler 包校验识别 `index.html` 为 HTML5 入口。
- Ego Lite 本地实际 WebGL（1280×720）完成：自动弹窗、两项勾选不能确认、取消后 New Game 禁用、重新打开清空勾选、三项全选启用、正文滚动到底、确认后仅解锁主菜单。
- 新包中的全部问卷资源与此前已发布的 `2026.09.25-survey-five-choices-300` 完全一致；本次没有执行后端迁移。
- 浏览器 `Page.captureScreenshot` 接口超时；截图改为从实际运行的 WebGL canvas 导出，见 `screenshots/local-webgl-*.png`。

源代码提交 `bf89734`，构建包提交 `8b615f7`，发布地址刷新记录 `21e0c0b`。

正式版本 **`2026.09.25-research-consent`**，itch.io HTML5 构建 **`2013820`**。

- [正式游戏入口](https://kaedeeeeeeeeee.itch.io/geo-model-geological-drilling-simulator) 的 iframe 已确认指向 `17462165-2013820`。
- Ego Lite 在该公开资源地址正常加载游戏，并实际验证自动弹窗、两项勾选阻止确认、三项全选启用确认、确认后主菜单 New Game 解锁。未在生产创建游戏研究记录或提交问卷，也未做整局通关测试。
- 全部 19 个发布文件核对通过。普通资源逐字节一致；3 个 Unity gzip 文件与本地包解压后的内容一致；2 个 index.html 除 itch.io 自动追加的官方 `htmlgame.js` 脚本外一致。详见 `published-assets.json`。
- 截图 `online-initial.png`、`online-two-blocked.png`、`online-three-enabled.png`、`online-unlocked.png` 均为实际公开版本运行画面。测试页临时启用 WebGL `preserveDrawingBuffer` 以稳定导出画布，发布包不含此测试设置。
- `online-runtime.json` 记录加载状态 ready、版本号和 1280×720 渲染尺寸。保留原有 favicon.ico 缺失的 404，不影响游戏加载。
- 初次发布构建 `2013812` 在平台解包完成前被请求的资源出现 CDN 404 缓存。再次发布相同游戏文件、仅更新 manifest 发布时间以生成新地址；最终 `2013820` 在普通资源 URL 下验证正常，未使用查询参数绕过缓存来判定通过。
- GitHub 推送后的 Pages 工作流 `36082899914` 成功；该工作流为项目介绍占位页，正式游戏发布仍以 itch.io 验收为准。
