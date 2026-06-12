# 植被覆盖 / 露头(露頭)系统 —— 设计文档

- 日期: 2026-06-05
- 状态: 设计已确认,待实现
- 目标平台: WebGL (URP 17.x)
- 影响场景: `MainScene`(野外)

## 1. 背景与问题

这是一款面向中学生(R7 教材地质课程)的地质教学游戏。地质学核心概念之一是
**露头(日文「露頭」)** —— 岩石/地层在地表裸露、未被土壤或植被覆盖的区域。
真实地质调查中,地表绝大部分被植被覆盖,露头只出现在特定侵蚀/裸露位置:
崖(がけ)、道路切面(切り通し)、河岸冲刷面(川岸)、采石场等。

**当前缺陷**:地图地表完全空无植被,导致**处处都是露头**。当一切都是露头时,
"露头"这个概念反而无法被教 —— 因为失去了"图与地"的对比,也失去了"寻找露头"
这件需要技能的事。

**解决方向**:加入植被,但植被不是装饰,而是一个**教学装置** —— 它让露头重新
变得稀少、需要主动寻找,从而恢复学习意义。

## 2. 已确认的关键决策

1. **植被角色 = 功能门控(宽容版)**:只有在露头(裸岩)处才能钻探/采样;
   覆盖区采样被拒绝并给明确引导。把"找露头"变成真正的玩法循环。
2. **露头定义 = 坡度/规则驱动**:陡坡/崖面自动裸露成露头,平缓地长植被。
   贴合"侵蚀剥露"的真实地貌原理,自维护、无需手工标注。
3. **地图前提 = 已有明显起伏**(山坡/河谷/崖),坡度驱动开箱即用,无需额外造地形。
4. **渲染方式 = 着色器打底 + 稀疏 GPU 实例化点缀**;可分两步上线
   (先着色器,再点缀)。三种地形坡度下,陡坡始终保留地层岩面。

## 3. 核心架构 —— 单一真相 + 共享判定

整套系统围绕**一个单一真相:脚下地面的倾角**。

### `OutcropSurface`(静态工具类)
```
bool  IsOutcrop(Vector3 surfaceNormal)   // 是否露头
float GetCoverage01(Vector3 surfaceNormal) // 0~1 覆盖度,用于过渡带柔化
```
判定核心:`Vector3.Angle(normal, Vector3.up)` 与阈值比较。

### `OutcropConfig`(ScriptableObject / 场景单例)
集中持有所有可调参数(见 §8 参数表),编辑器内实时调。

**关键原则:渲染层与门控层共用同一套判定与阈值。**
着色器、植被散布、工具采样校验,三者都问同一个 `OutcropSurface`、读同一个
`OutcropConfig`。于是"看起来是草的地方一定采不了样" —— 视觉与机制永远一致,
无需任何烘焙数据或额外存储,纯运行时计算,WebGL 友好。

### 数据流
```
地面 raycast → hit.normal
                  │
                  ▼
        OutcropSurface (读 OutcropConfig 阈值)
                  │
        ┌─────────┴──────────┐
        ▼                    ▼
   渲染:草 vs 地层        门控:允许 vs 拒绝采样
 (着色器 / 点缀散布)    (采样工具的预览 + 校验)
```

现有工具本就在朝地面打 raycast 取命中点(`PlaceableTool.cs` 约 L144),
`hit.normal` 顺手即得,接入成本极低。

## 4. 渲染层

### 4.1 着色器打底:`GeoSurfaceLit`(URP Shader Graph)
- 把 `GeologyLayer` 现用地层材质统一换成坡度感知 Lit 着色器。
- 输入:`StrataBase`(沿用每层现有颜色/贴图)、`GrassCoat`(草色 albedo + 可选法线)、
  `slopeThreshold`、`transitionBand`。
- 片元:世界法线 → 倾角 → 过渡带 `smoothstep` → `lerp(GrassCoat, StrataBase)`。
- 结果:平的顶面长草;被侵蚀的陡崖面 / 河岸切面露出地层。
- 草色顶投影(world XZ)采样,避免拉伸;零额外几何。

### 4.2 稀疏点缀:`VegetationScatter` + 编辑器烘焙
- 编辑器烘焙器:在地形 XZ 包围盒撒候选点 → 向下 raycast 命中 `groundLayers`
  → 仅保留倾角 < 阈值(被覆盖)的点,越靠近过渡带密度越低 → 把 transform 烘焙进组件。
- 运行时:`Graphics.DrawMeshInstanced` 按 mesh+材质批量绘制 + 距离裁剪 + 硬上限;
  不在加载时 raycast,WebGL 零启动负担。
- 资产:几种低面草丛 / 灌木 / 偶尔一棵树,材质开 GPU Instancing。
- 点缀不带碰撞体(不挡移动、不挡工具 raycast),纯视觉。

## 5. 门控层

只门控"采样类"工具,载具不限制:

| 工具 | 门控 | 接入点 |
|---|---|---|
| `SimpleDrillTool` | ✅ | 钻探前 `OutcropSurface.IsOutcrop(hit.normal)` |
| `DrillTowerTool` | ✅ | override `CanPlaceAtPosition` 要求露头 |
| `HammerTool` | ✅ | 敲击前查同一判定 |
| `DroneTool` / `DrillCarTool` | ❌ | 维持现状(高度+重叠校验) |

- 复用现有绿/红预览材质(`SimpleDrillTool.cs` L128/L132)。覆盖区 → 红,露头 → 绿。
- "宽容版"语义:阈值稍宽松 + 过渡带偏岩一端即算通过 + 永远给明确提示,绝不沉默失败。

## 6. UX、可读性与本地化

- 主要线索 = 天然草/岩对比 + 绿/红预览,不另造 HUD。
- 瞄准覆盖区:红预览 + 浮动本地化提示「这里被植被覆盖,去找裸露的岩石」。
- 可选拉伸项(不进 v1):露头在范围内时给微弱描边高光。
- 本地化:新增 key 进 `zh-CN / en-US / ja-JP`;日文遵循 **漢字+ふりがな(ruby)** 约定。
  建议 key:`outcrop.covered.hint`、`outcrop.covered.preview`。
- 遵守项目"多语言优先"铁律。

## 7. 分阶段落地

每阶段独立可测:

1. `OutcropSurface` + `OutcropConfig` —— 规则地基
2. `GeoSurfaceLit` 着色器 —— 视觉真相(草/地层对比)
3. 门控三件采样工具 —— 机制上线(视觉与机制此刻一致)
4. `VegetationScatter` 点缀 —— 加汁水
5. UX 提示 + 本地化 + 调参 + WebGL 性能过一遍

跑完 1–3 即为可玩、自洽的「找露头 → 读地层 → 采样」循环;4–5 是打磨。

## 8. 参数表(`OutcropConfig`)

| 参数 | 默认 | 说明 |
|---|---|---|
| `slopeThreshold` | ~30° | 露头/覆盖分界 |
| `transitionBand` | 25°–35° | 柔化边,避免硬环 |
| `grassCoat` 颜色/贴图 | — | 草色外观 |
| `accentDensity` | — | 点缀密度 |
| `maxInstances` | — | WebGL 实例硬上限 |

## 9. 风险与缓解(按重要性)

1. **⚠️ 与剧情/课程采样点的交叉(最关键)**:15 分钟线性剧情若把学生引到固定采样点,
   这些点现在必须落在露头(陡坡)上。需核对关键剧情点,或反过来把露头摆在课程
   想让学生采的位置。与正在进行的 story 工作直接咬合。
2. 阈值处硬边 → 过渡带化解。
3. WebGL 性能 → 烘焙散布 + 上限 + 裁剪;着色器本身极廉价。
4. 可走性 → 点缀不带碰撞。
5. 露头顶面平、长草,而剖面陡峭露岩 —— 真实且正确(学生读剖面),调阈值使剖面清晰裸露即可。

## 10. 新增 / 改动文件

**新增**
- `Assets/Scripts/GeologySystem/Outcrop/OutcropSurface.cs`
- `Assets/Scripts/GeologySystem/Outcrop/OutcropConfig.cs`
- `Assets/Scripts/GeologySystem/Outcrop/VegetationScatter.cs`
- `Assets/Scripts/Editor/VegetationScatterBaker.cs`
- `Assets/Art/Shaders/GeoSurfaceLit.shadergraph`

**改动**
- `Assets/Scripts/Tools/SimpleDrillTool.cs`
- `Assets/Scripts/DrillTowerSystem/DrillTowerTool.cs`
- `Assets/Scripts/Tools/HammerTool.cs`
- `Assets/Scripts/GeologySystem/GeologyLayer.cs`(材质赋值改用 `GeoSurfaceLit`)
- `Assets/Resources/Localization/Data/zh-CN.json` / `en-US.json` / `ja-JP.json`

## 11. 待实现时核对

- 各工具的实际方法签名(钻探/采样动作的具体入口),实现时以现有代码为准。
- `GeologyLayer` 当前材质赋值路径,确认改用新着色器不破坏现有地层颜色语义。
- `groundLayers` LayerMask 的实际取值,散布器与门控复用同一 mask。
