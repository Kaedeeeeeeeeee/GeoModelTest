# 学校电脑与 iPad 性能优化分析

分析日期：2026-09-11。Unity：6000.0.51f1。

本轮检查源码、资源导入配置、最近的构建资源报告和本地 WebGL 构建，只新增分析文档，没有修改游戏。暂按浏览器版为主要测试渠道；尚未获得学生设备型号、具体卡顿场景和真机性能记录。

**结论**

可以降低 texture 质量，而且有明确的优化对象。但项目同时存在高 DPI 渲染、高面数模型和昂贵的显微镜预览，单独压缩贴图未必解决持续低帧率。建议先做一版可切换的“流畅”画质，优先处理实际渲染分辨率、环境贴图和显微镜预览，再进行模型减面。

**已确认的发现与证据**

| 项目 | 当前情况 | 对体验的影响与建议 |
|---|---|---|
| WebGL 高 DPI | 本机 Chrome 中，CSS 显示 1200×675，DPR=2，canvas 与 WebGL drawing buffer 都是 2400×1350；游戏加载完成后仍如此 | 实际像素数是 CSS 尺寸对应像素数的 4 倍。网页配置未设置 `devicePixelRatio` / `matchWebGLToCanvasSize`，应显式控制渲染尺寸并设置像素上限 |
| 画质默认值 | `GamePerformanceSettings` 将非移动设备直接推荐为 High；Low、Medium 的 `textureQuality` 都为 0；Very Low 为 3 | 学校低配置电脑也会得到 High。应提供保守的默认档，分别定义贴图和渲染尺寸，不依靠画质档名称推断实际开销 |
| 分辨率降档代码 | 当前调用 `resolutionScalingFixedDPIFactor` 和 `ScalableBufferManager.ResizeBuffers`；玩家相机 `m_AllowDynamicResolution: 0` | 不能将这段代码视为 WebGL 降分辨率已经生效的证据。Unity 6 文档所列动态分辨率平台不包含 WebGL，应接通网页 canvas 的实际渲染尺寸控制 |
| 环境贴图 | 最近构建资源报告包含 18 张 TerrainSampleAssets 地表贴图，合计约 80.0 MiB；这些贴图的导入最大尺寸均为 2048，开启 mipmap | 是优先减重对象。环境主色贴图可先试 1024，法线与遮罩先试 512–1024，并核对实际材质是否需要这些纹理 |
| 显微镜预览 | 场景中 `previewSize: 2048`；脚本默认 `previewAntiAliasing = 8`，独立 RenderTexture 使用该值 | 打开预览时可能有明显 GPU 和内存开销。流畅档先试 512–1024、关闭或降到 2× MSAA；关闭界面时相机已有停用逻辑，应保留。实际 MSAA 支持/回退仍需真机验证 |
| 高面数模型 | 源 FBX 的三角化面数统计：显微镜约 634,985；地质模型约 610,239；场景切换器约 153,323；无人机约 136,590；钻车约 115,360；锤子约 75,896 | 这些是源资产总面数，不能等同于某帧实际可见面数。优先对显微镜和普通道具做减面、法线烘焙与距离 LOD，再测实际渲染三角面和耗时 |
| 地质交互网格 | `0629final.prefab` 有 8 个 MeshCollider，未见 LODGroup；源 FBX 开启 Read/Write | 地质切割、取样可能依赖网格。减面时应区分显示网格与逻辑/碰撞网格，不能直接统一关闭 Read/Write 或随意合并地层 |
| GLB 导入压缩 | 自定义 GLTFUtility 导入逻辑已将大纹理缩至最长边 1024，并硬编码 DXT5Crunched | 矿物纹理已经有优化；不要先牺牲关键辨认细节。iPad 上应检测压缩格式支持，排查软件解压回退；修改 Web 构建格式时也必须处理此自定义导入逻辑 |
| 图鉴加载 | `EncyclopediaData.Awake()` 在初始化时遍历条目，同步加载图片和模型，管理器使用 DontDestroyOnLoad | 可能增加启动/切场景停顿和常驻资源量。可先加载文字与必要缩略图，打开具体条目时再加载模型，配置缓存上限 |
| 绘制调用 | WebGL 的静态、动态合批都关闭；研究室有大量 prefab 实例 | 后续用实际 draw call 数据决定是否共享材质、合并静态物件或启用合批；静态合批也会增加内存，不宜一键全开 |

本机画布测量来自 `Build/WebGL` 的 `2026.09.11-survey-return` 构建，通过 localhost 启动。它证明高 DPI 放大在当前构建中存在，但不是 iPad 帧率实测，也不能证明所有学生设备只有这一项瓶颈。

**贴图减重方案**

| 资源用途 | 第一轮建议 | 需要检查 |
|---|---|---|
| 地面、墙面与环境主色 | 最大尺寸先设 1024；次要远景可试 512 | 重复纹理是否过糊，地层颜色是否仍能区分 |
| 环境法线、遮罩 | 512–1024；低档可减少非必要贴图采样 | 材质光照是否异常，避免误删有效通道 |
| 显微镜外壳、钻车、无人机、锤子等普通道具 | 通常先试 512–1024 | 近距离轮廓与标签可读性 |
| 用于辨认的矿物、化石 | 第一轮优先保留 1024；逐项截图对比后再决定 | 晶体纹理、化石纹理、颜色等教学线索 |
| 对话立绘、插图与 UI | 根据实际显示尺寸分别设置；字体保持清晰 | 日文小字、按钮、问卷入口和图鉴辨认 |

同一压缩格式、mipmap 设置和正方形尺寸下，2048→1024 的像素和纹理数据量约降至 1/4；2048→512 约降至 1/16。按此估算，80 MiB 的地表纹理若都改为 1024，可接近 20 MiB，即减少约 60 MiB 的对应纹理数据。**这是估算，不是已经完成的新构建结果，也不代表帧率提升 4 倍。**

这里的 80 MiB 来自 `Logs/webgl/asset-sizes.json` 的 packed asset 字节统计，既不是网络下载体积，也不是实测 GPU 常驻内存。压缩包更小主要改善下载；降低 GPU 纹理分辨率和使用设备原生压缩才与运行时内存、带宽直接相关。

GLB 的自定义缩图分支还使用 `Reinitialize(..., false)` 创建无 mipmap 纹理，因此不能假设全局 mip 降档覆盖所有 GLB 贴图。应审计最终导入纹理的 mip 数、压缩格式和 Read/Write 状态，通过导入设置保证减重实际发生。

**建议的实施顺序**

1. 在代表性的学校电脑和实体 iPad 上建立基线：进入野外、旋转视角、进入研究室、打开显微镜、使用无人机/钻车。区分持续掉帧、操作时瞬间停顿、运行数分钟后变卡和首次下载慢。记录设备、浏览器、实际画布大小、画质档、帧耗时、内存与纹理格式回退。
2. 第一批低风险调整：流畅档默认目标 30 FPS，3D/游戏渲染上限先试 1280×720，必要时 960×540；限制高 DPI 倍率；关闭实时阴影和非必要抗锯齿；环境贴图 512–1024；显微镜预览 512–1024、0/2× MSAA。UI 缩放、触控坐标、横竖屏和日文可读性需要一起复核。
3. 在同一设备、同一场景逐项 A/B：先只改分辨率，再只改贴图，再改预览开销。这样能够判断 GPU 像素开销、纹理压力与模型/CPU 开销分别占多大比例。
4. 若仍慢，优先减面显微镜和普通道具，再处理地质显示网格。为模型增加合理的距离 LOD 和裁剪。前一轮 Low 网格压缩主要改善包体，不减少三角面数量。
5. 最后处理图鉴按需加载、重复绘制、UI 更新和脚本分配。当前没有 Profiler 证据证明某个 Update 是主要瓶颈，不先展开大规模逻辑重写。

30 FPS 对应约 33.3 ms/帧。应关注 95% 帧耗时和最长停顿，并在实体设备上持续运行至少 10 分钟检查温度升高后的表现；不能仅用电脑浏览器的触控模拟评价 iPad 性能。

**关键文件**

- [画质选择与运行时覆盖](/Users/user/Unity/GeoModelTest/Assets/Scripts/MobileSystem/GamePerformanceSettings.cs:98)
- [Low 贴图质量设置](/Users/user/Unity/GeoModelTest/ProjectSettings/QualitySettings.asset:45)
- [WebGL 模板](/Users/user/Unity/GeoModelTest/Assets/WebGLTemplates/FixedAspect/index.html:60)
- [显微镜预览设置](/Users/user/Unity/GeoModelTest/Assets/Scripts/WorkbenchSystem/MicroscopeController.cs:29)
- [GLB 纹理优化代码](/Users/user/Unity/GeoModelTest/Assets/Plugins/GLTFUtility/Scripts/Editor/GLTFAssetUtility.cs:124)
- [图鉴同步加载](/Users/user/Unity/GeoModelTest/Assets/Scripts/Encyclopedia/EncyclopediaData.cs:328)
- [构建资源报告](/Users/user/Unity/GeoModelTest/Logs/webgl/asset-sizes.json)

Unity 官方说明了 [Web Canvas 的 CSS 尺寸、实际渲染尺寸与 DPI 控制](https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-canvas-size.html)，以及 [动态分辨率支持的平台](https://docs.unity3d.com/6000.0/Documentation/Manual/DynamicResolution-introduction.html)。

关于 iPad 压缩格式，Unity 建议分别提供适合桌面和移动浏览器的纹理格式，并说明不支持的格式会在运行时软件解压，增加内存并可能降低速度。应检测设备扩展、验证当前 Unity 版本和实体 Safari 后再决定是否提供 ASTC 资源版本；详见 [Web 纹理压缩](https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-texture-compression.html)。
