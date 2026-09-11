# ジオクエスト：标题更新

主标题：**ジオクエスト**  
副标题：**地形を読み解くフィールドワーク**

标题画面采用这组日文名称；加载页和游戏 iframe 的浏览器标题也同步更新。英文、中文界面的副标题分别使用对应翻译。

版本 `2026.09.11-geoquest`，Unity `6000.0.51f1`，源码提交 `156bbc9`。

## 手机横屏

![手机横屏标题画面](mobile.png)

## 电脑

![电脑标题画面](desktop.png)

截图来自 itch.io 上已部署的真实 Unity WebGL 构建。手机检查使用 Chromium 的 iPhone 用户代理、手机视口与触控模拟。

## 发布与验证

已更新到 [itch.io](https://kaedeeeeeeeeee.itch.io/geo-model-geological-drilling-simulator)，HTML5 build **1967104**。

- 正式 WebGL 构建成功，错误 0。
- LocalizationDataTests：7/7 通过；三个语言文件及生成副本一致。
- 本地与线上都检查了电脑、手机横屏，主副标题完整可见，没有截断或与按钮重叠。
- 线上两种视口的 JavaScript 页面错误均为 0，研究后端请求为 0。
- 线上 release.json 与本次构建完全一致。
- Unity 测试后已恢复并核对原有 PlayerPrefs 和 8 个持久化文件。

[本地检查](local-results.json) · [线上检查](public-results.json)
