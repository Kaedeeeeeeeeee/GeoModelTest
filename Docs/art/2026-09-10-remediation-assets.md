# 本轮 UI 素材来源

本轮使用 Codex 内置 image_gen 生成主视觉，并基于项目原有角色图编辑手机姿态。所有验收截图来自实际 Unity / WebGL 运行画面，未使用生成图冒充运行截图。

| 资源 | 用途 | 来源 |
|---|---|---|
| Assets/Resources/UI/TitleLandscape.png | 标题背景 | image_gen 新生成；右侧呈现地层露头、钻塔与研究室，左侧保留深色文字区域 |
| Assets/Resources/Tachie/player-phone.png | 玩家野外通信立绘 | 以原有 player.png 为身份参考进行姿态编辑 |
| Assets/Resources/Tachie/kaede-phone.png | 博士野外通信立绘 | 以原有 kaede.png 为身份参考进行姿态编辑，并去除透明边缘光晕 |

主视觉生成说明摘要：16:9 地质调查主题风景；右侧为层状海岸露头、小型钻塔和研究室，左侧安静、偏暗；使用青绿、灰绿与赭石色，不含文字或角色。

手机立绘提示词（角色名按各自素材替换）：

> Use case: identity-preserve. Asset: transparent Unity visual-novel character sprite. Edit the supplied [name] character: preserve the exact character identity, face, hair color and hairstyle, golden eyes, chibi proportions, pale coat and blue dog-print clothing, clean softly shaded 3D anime look. Change only the arm pose: hold a small dark teal smartphone at the side of the face in one hand, as if speaking remotely during a geology field investigation. Keep full body and centered framing with generous transparent margins. No scenery, no text, no logos, actual transparent alpha background. Keep the same cheerful neutral expression and original clothes.

导入时保留透明 alpha、关闭 mipmaps，并保持原图比例；原角色图保留，研究室使用原立绘，野外通信使用手机姿态。
