# 家长同意页：手机键盘不再挡住回答者ID — 2026-10-20

版本 `2026.10.20-consent-keyboard`（只改网页外壳，游戏本体仍是 `2026.10.16-hammer-slab-fix`）。

## 背景

2026-10-20 テスティー的小川さん来信：iPhone 上在家长同意页输入回答者ID时，「输入栏」盖在ID输入框上，不好输入（`screenshots/0-testee-iphone.png`）。Android 上没有这个问题。用户在自己的 iPhone 上也遇到了：输完家长姓名后，必须先收起键盘，才能看到下面的回答者ID输入框。

## 原因

在 iOS 26.5 模拟器（iPhone 14，和小川さん的机型、系统一致）的真实 Safari 中复现（`screenshots/1-before-ios26.png`）：

- Safari 只把聚焦的输入框滚到键盘正上方，而 iOS 26 在键盘上方浮着一条「∧ ∨ ✓」栏，Safari 没有把这条栏算进去，所以输入框正好被它盖住。
- 表单在长页面的最底部，同意页（`#guardian-consent`）这个滚动层已经滚到底，没法再往上。
- 输入姓名时，回答者ID在那条栏下面，点也点不中，只能先收起键盘。

日志还显示，键盘弹出时 Safari 会先把整页上移约 150px，`index.html` 里的 `window.scrollTo(0, 0)` 随即又拉回来。只有在两者都停下之后再测量位置才准确。

## 改动

`Assets/WebGLTemplates/FixedAspect/TemplateData/guardian-consent.js`、`style.css`（同步到 `Build/WebGL/TemplateData/`）：

- 只对触屏（`pointer: coarse`）生效：聚焦「保護者氏名」或「お子様の回答者ID」时，把该输入框连同标签移到可见区域顶部（留 12px）。
  - 输入姓名时，回答者ID、同意日、「回答する」都在键盘上方。
  - 输入回答者ID时，「回答する」也在键盘上方。
- 表单底部临时加一段留白（CSS 变量 `--consent-keyboard-space`），最后一个输入框也能移到顶部。离开两个输入框，或 Android 用返回键收起键盘（输入框仍保持焦点）时，留白去掉。
- 在聚焦时先移动，键盘弹出、或从假名键盘换到数字键盘后，等视口稳定 80ms 再校正一次。
- 桌面（鼠标＋实体键盘）的行为不变。

## 验证

| 范围 | 结果 |
|---|---|
| iOS 26.5 Safari（iPhone 14 模拟器，XCUITest 驱动真实软键盘） | 修改前：两种流程都复现（按「次へ」后ID被栏盖住；直接点ID点不中）。修改后：两种流程都通过。聚焦字段的标签稳定在顶部 12px；输完ID、提示文字消失时页面不跳；按 ✓ 后恢复原样（`2-after-ios26.png`） |
| 同上，后台加载 Unity | 通过（`3-after-ios26-unity-loading.png`） |
| iOS 18.5（键盘上方是旧式「Done」栏）、iOS 27.0（日文假名键盘） | 通过（`4-after-ios18-ios27.png`，上排 18.5，下排 27.0） |
| Android 16 Chrome（模拟器） | 输入姓名、按 Enter 进入ID、输入ID：通过。返回键收起键盘后留白消失，再点ID会重新移到顶部（`5-after-android.png`） |
| `tools/qa/guardian-consent-browser.py` | 12/12（新增：手机尺寸下聚焦字段移到顶部、失焦后留白归零；桌面点击不加留白） |
| `node --test tools/qa/*.test.cjs` | 26/26 |

模拟器测试用的是临时 XCUITest 工程和调试代理（只在 scratchpad 中，不进仓库）：Unity 构建被拦截，并记录 `visualViewport` 的变化。全程没有提交表单，也没有访问 Supabase。

未覆盖：iPhone 实机（模拟器的 Safari 与实机相同，但键盘高度可能因机型和输入法略有差异；顶部对齐的方式本身不依赖键盘高度）。
